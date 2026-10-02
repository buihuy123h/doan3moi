using doanki3API.DTOs.Notifications;
using doanki3API.Hubs;
using doanki3API.Models.Notifications;
using doanki3API.Services.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace doanki3API.Services.Notifications;

/// <summary>
/// Lưu thông báo vào MongoDB rồi đẩy ngay xuống trình duyệt qua SignalR.
///
/// Nguyên tắc: thông báo là việc phụ, KHÔNG được làm hỏng nghiệp vụ chính.
/// Vì vậy mọi lỗi (Mongo sập, SignalR lỗi) đều bị nuốt và chỉ ghi log — đơn hàng
/// hay hoá đơn đã lưu xong trước đó vẫn đứng vững.
///
/// Đăng ký singleton: MongoClient tự quản connection pool, IHubContext cũng là singleton.
/// </summary>
public sealed class NotificationService
{
    /// <summary>Thông báo chỉ giữ 30 ngày, quá hạn Mongo tự xoá bằng TTL index.</summary>
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private const int DefaultLimit = 50;

    private readonly IMongoCollection<NotificationDocument> _notifications;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IOptions<MongoSettings> options,
        IHubContext<NotificationHub> hub,
        ILogger<NotificationService> logger)
    {
        _hub = hub;
        _logger = logger;

        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.Database);
        _notifications = database.GetCollection<NotificationDocument>(settings.NotificationsCollection);

        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        try
        {
            // Truy vấn chính: lấy thông báo của một người nhận, mới nhất lên đầu.
            _notifications.Indexes.CreateOne(new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys
                    .Ascending(n => n.Audience)
                    .Descending(n => n.CreatedAt)));

            // TTL: Mongo xoá document ngay khi đồng hồ vượt qua ExpiresAt.
            _notifications.Indexes.CreateOne(new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(n => n.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
        }
        catch (Exception ex)
        {
            // Mongo chưa bật cũng không chặn API khởi động — thông báo sẽ chỉ chạy "live".
            _logger.LogWarning(ex, "Không tạo được index cho collection thông báo.");
        }
    }

    // ---- Phát thông báo ----

    /// <summary>Gửi một thông báo tới đúng một người nhận.</summary>
    public Task PublishAsync(NotificationDraft draft, string audience, CancellationToken ct = default) =>
        PublishAsync(draft, [audience], ct);

    /// <summary>
    /// Gửi cùng một nội dung tới nhiều người nhận. Mỗi người nhận có bản ghi riêng
    /// nên Admin đọc rồi không làm mất chấm đỏ của Bán lẻ.
    /// </summary>
    public async Task PublishAsync(
        NotificationDraft draft,
        IEnumerable<string> audiences,
        CancellationToken ct = default)
    {
        var targets = audiences
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (targets.Count == 0) return;

        var now = DateTime.UtcNow;

        foreach (var audience in targets)
        {
            var doc = new NotificationDocument
            {
                NotificationId = $"NTF-{Guid.NewGuid():N}",
                Audience = audience,
                Type = draft.Type,
                TitleVi = draft.TitleVi,
                TitleEn = draft.TitleEn,
                DescVi = draft.DescVi,
                DescEn = draft.DescEn,
                TargetPath = draft.TargetPath,
                TargetTab = draft.TargetTab,
                TargetRole = draft.TargetRole,
                SectionVi = draft.SectionVi,
                SectionEn = draft.SectionEn,
                EntityId = draft.EntityId,
                ReplaceKey = draft.ReplaceKey,
                Read = false,
                CreatedAt = now,
                ExpiresAt = now.Add(RetentionPeriod)
            };

            try
            {
                // Tin lặp lại (kho sắp hết, khách đang nhắn tin...) thì dọn bản chưa đọc cũ
                // để hộp thông báo không bị một sự kiện chiếm hết chỗ.
                if (!string.IsNullOrWhiteSpace(doc.ReplaceKey))
                {
                    await _notifications.DeleteManyAsync(
                        n => n.Audience == audience && n.ReplaceKey == doc.ReplaceKey && !n.Read,
                        ct);
                }

                await _notifications.InsertOneAsync(doc, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                // Không lưu được vẫn đẩy đi: người đang mở máy vẫn thấy, chỉ mất lịch sử.
                _logger.LogWarning(ex, "Không lưu được thông báo cho {Audience}.", audience);
            }

            await PushAsync(audience, NotificationHub.EventNotification, ToDto(doc), ct);
        }
    }

    // ---- Đọc / đánh dấu đã đọc ----

    public async Task<List<NotificationDto>> GetAsync(
        string audience,
        int limit = DefaultLimit,
        CancellationToken ct = default)
    {
        var docs = await _notifications
            .Find(n => n.Audience == audience)
            .SortByDescending(n => n.CreatedAt)
            .Limit(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);

        return docs.Select(ToDto).ToList();
    }

    public async Task<NotificationCountDto> CountAsync(string audience, CancellationToken ct = default)
    {
        var total = await _notifications.CountDocumentsAsync(n => n.Audience == audience, cancellationToken: ct);
        var unread = await _notifications.CountDocumentsAsync(
            n => n.Audience == audience && !n.Read, cancellationToken: ct);

        return new NotificationCountDto((int)unread, (int)total);
    }

    /// <summary>
    /// Đánh dấu một thông báo đã đọc, rồi báo cho các tab khác của cùng người nhận
    /// để chấm đỏ tắt đồng loạt.
    /// </summary>
    public async Task<bool> MarkReadAsync(string audience, string notificationId, CancellationToken ct = default)
    {
        var result = await _notifications.UpdateOneAsync(
            n => n.Audience == audience && n.NotificationId == notificationId,
            Builders<NotificationDocument>.Update.Set(n => n.Read, true),
            cancellationToken: ct);

        if (result.MatchedCount == 0) return false;

        await PushAsync(audience, NotificationHub.EventNotificationRead, new { id = notificationId }, ct);
        return true;
    }

    public async Task<long> MarkAllReadAsync(string audience, CancellationToken ct = default)
    {
        var result = await _notifications.UpdateManyAsync(
            n => n.Audience == audience && !n.Read,
            Builders<NotificationDocument>.Update.Set(n => n.Read, true),
            cancellationToken: ct);

        await PushAsync(audience, NotificationHub.EventNotificationsRead, new { audience }, ct);
        return result.ModifiedCount;
    }

    // ---- Các sự kiện real-time khác (không lưu lịch sử) ----

    /// <summary>
    /// Báo cho mọi client "bảng dữ liệu X vừa đổi" để dashboard tự tải lại,
    /// nhờ vậy danh sách đơn hàng / hoá đơn của người khác cập nhật mà không cần F5.
    /// </summary>
    public Task NotifyDataChangedAsync(string entity, string? id = null, CancellationToken ct = default) =>
        SafeSendAsync(
            () => _hub.Clients.Group(NotificationHub.EveryoneGroup)
                .SendAsync(NotificationHub.EventDataChanged, new { entity, id }, ct));

    /// <summary>Đẩy một tin nhắn chat tới đúng phiên của khách và tới Admin.</summary>
    public Task PushChatMessageAsync(string sessionId, object message, CancellationToken ct = default) =>
        SafeSendAsync(() => _hub.Clients
            .Groups(Constants.NotificationAudiences.ForChatSession(sessionId), Constants.NotificationAudiences.Admin)
            .SendAsync(NotificationHub.EventChatMessage, message, ct));

    /// <summary>Đẩy trạng thái mới của một phiên chat (tin cuối, số chưa đọc) cho Admin.</summary>
    public Task PushChatSessionAsync(object session, CancellationToken ct = default) =>
        SafeSendAsync(() => _hub.Clients
            .Group(Constants.NotificationAudiences.Admin)
            .SendAsync(NotificationHub.EventChatSession, session, ct));

    // ---- Hạ tầng ----

    private Task PushAsync(string group, string method, object payload, CancellationToken ct) =>
        SafeSendAsync(() => _hub.Clients.Group(group).SendAsync(method, payload, ct));

    /// <summary>Gói mọi lần gửi SignalR trong try/catch — mất kết nối không được ném lên tầng nghiệp vụ.</summary>
    private async Task SafeSendAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không đẩy được sự kiện real-time xuống client.");
        }
    }

    private static NotificationDto ToDto(NotificationDocument n) => new(
        n.NotificationId,
        n.Audience,
        n.Type,
        n.TitleVi,
        n.TitleEn,
        n.DescVi,
        n.DescEn,
        n.TargetPath,
        n.TargetTab,
        n.TargetRole,
        n.SectionVi,
        n.SectionEn,
        n.EntityId,
        n.ReplaceKey,
        n.Read,
        n.CreatedAt.ToUniversalTime().ToString("o"));
}
