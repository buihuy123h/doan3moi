using doanki3API.DTOs.Chat;
using doanki3API.Models.Chat;
using doanki3API.Services.Notifications;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace doanki3API.Services.Chat;

/// <summary>
/// Toàn bộ thao tác đọc/ghi phiên chat trên MongoDB.
/// Đăng ký singleton vì MongoClient đã tự quản lý connection pool bên trong.
/// </summary>
public sealed class ChatService
{
    private const string GreetingMessage = "Xin chào, chúng tôi có thể giúp gì cho bạn?";
    private const string GreetingSender = "Hệ thống";

    private readonly IMongoCollection<ChatSessionDocument> _sessions;
    private readonly IMongoCollection<ChatMessageDocument> _messages;
    private readonly ILogger<ChatService> _logger;
    private readonly NotificationService _notifications;

    public ChatService(
        IOptions<MongoSettings> options,
        ILogger<ChatService> logger,
        NotificationService notifications)
    {
        _logger = logger;
        _notifications = notifications;
        var settings = options.Value;

        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.Database);

        _sessions = database.GetCollection<ChatSessionDocument>(settings.SessionsCollection);
        _messages = database.GetCollection<ChatMessageDocument>(settings.MessagesCollection);

        EnsureIndexes();
    }

    /// <summary>Tạo index để lọc tin nhắn theo phiên và sắp xếp danh sách phiên cho nhanh.</summary>
    private void EnsureIndexes()
    {
        try
        {
            _messages.Indexes.CreateOne(new CreateIndexModel<ChatMessageDocument>(
                Builders<ChatMessageDocument>.IndexKeys
                    .Ascending(m => m.SessionId)
                    .Ascending(m => m.CreatedAt)));

            _sessions.Indexes.CreateOne(new CreateIndexModel<ChatSessionDocument>(
                Builders<ChatSessionDocument>.IndexKeys.Descending(s => s.LastMessageAt)));
        }
        catch (Exception ex)
        {
            // Không chặn khởi động API nếu Mongo chưa sẵn sàng — request chat sẽ báo lỗi riêng.
            _logger.LogWarning(ex, "Không tạo được index cho MongoDB chat.");
        }
    }

    /// <summary>Khách mở chatbox lần đầu: tạo phiên mới kèm lời chào tự động từ Admin.</summary>
    public async Task<ChatSessionDto> StartSessionAsync(string customerName, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var sessionId = $"SESSION-{Guid.NewGuid():N}";

        var session = new ChatSessionDocument
        {
            SessionId = sessionId,
            CustomerName = customerName.Trim(),
            LastMessage = GreetingMessage,
            LastSenderType = "admin",
            LastMessageAt = now,
            // Lời chào là tin của Admin nên khách chưa nhắn gì -> Admin không có gì chưa đọc.
            UnreadCount = 0,
            MessageCount = 1,
            CreatedAt = now
        };

        await _sessions.InsertOneAsync(session, cancellationToken: ct);

        await _messages.InsertOneAsync(new ChatMessageDocument
        {
            Id = ObjectId.GenerateNewId(),
            SessionId = sessionId,
            SenderType = "admin",
            SenderName = GreetingSender,
            Message = GreetingMessage,
            CreatedAt = now
        }, cancellationToken: ct);

        var dto = ToDto(session);

        // Phiên mới hiện ngay trong màn hình Chat trực tuyến của Admin.
        await _notifications.PushChatSessionAsync(dto, ct);

        return dto;
    }

    /// <summary>Admin xem danh sách phiên, mới nhất lên đầu. Có thể lọc theo tên khách.</summary>
    public async Task<List<ChatSessionDto>> GetSessionsAsync(string? search, CancellationToken ct = default)
    {
        var filter = Builders<ChatSessionDocument>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Escape để khách gõ ký tự regex (dấu chấm, ngoặc...) không làm hỏng truy vấn.
            var safe = System.Text.RegularExpressions.Regex.Escape(search.Trim());
            filter = Builders<ChatSessionDocument>.Filter.Regex(
                s => s.CustomerName,
                new BsonRegularExpression(safe, "i"));
        }

        var docs = await _sessions
            .Find(filter)
            .SortByDescending(s => s.LastMessageAt)
            .Limit(200)
            .ToListAsync(ct);

        return docs.Select(ToDto).ToList();
    }

    /// <summary>Lấy toàn bộ tin nhắn của một phiên, cũ trước mới sau.</summary>
    public async Task<List<ChatMessageDto>> GetMessagesAsync(string sessionId, CancellationToken ct = default)
    {
        var docs = await _messages
            .Find(m => m.SessionId == sessionId)
            .SortBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return docs.Select(ToDto).ToList();
    }

    /// <summary>Kiểm tra một phiên có tồn tại không (để trả 404 cho sessionId rác).</summary>
    public Task<bool> SessionExistsAsync(string sessionId, CancellationToken ct = default) =>
        _sessions.Find(s => s.SessionId == sessionId).AnyAsync(ct);

    /// <summary>
    /// Ghi một tin nhắn vào phiên. Khách gửi thì tăng số tin chưa đọc của Admin,
    /// Admin trả lời thì coi như đã đọc hết.
    /// Trả về null nếu phiên không tồn tại.
    /// </summary>
    public async Task<ChatMessageDto?> SendMessageAsync(
        string sessionId,
        string senderType,
        string senderName,
        string message,
        CancellationToken ct = default)
    {
        var isAdmin = string.Equals(senderType, "admin", StringComparison.OrdinalIgnoreCase);
        var normalizedType = isAdmin ? "admin" : "guest";
        var now = DateTime.UtcNow;
        var text = message.Trim();

        var update = Builders<ChatSessionDocument>.Update
            .Set(s => s.LastMessage, text)
            .Set(s => s.LastSenderType, normalizedType)
            .Set(s => s.LastMessageAt, now)
            .Inc(s => s.MessageCount, 1);

        // Admin trả lời -> xoá ô đếm; khách nhắn -> cộng thêm 1 tin chưa đọc.
        update = isAdmin
            ? update.Set(s => s.UnreadCount, 0)
            : update.Inc(s => s.UnreadCount, 1);

        var updated = await _sessions.UpdateOneAsync(
            s => s.SessionId == sessionId, update, cancellationToken: ct);

        if (updated.MatchedCount == 0)
        {
            return null;
        }

        var doc = new ChatMessageDocument
        {
            Id = ObjectId.GenerateNewId(),
            SessionId = sessionId,
            SenderType = normalizedType,
            SenderName = senderName.Trim(),
            Message = text,
            CreatedAt = now
        };

        await _messages.InsertOneAsync(doc, cancellationToken: ct);

        var dto = ToDto(doc);

        // Đẩy tin nhắn thẳng xuống trình duyệt: khách thấy câu trả lời của Admin ngay,
        // Admin thấy tin của khách ngay, không còn phải hỏi lại server mỗi 15 giây.
        await _notifications.PushChatMessageAsync(sessionId, dto, ct);

        var session = await _sessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync(ct);
        if (session is not null)
        {
            await _notifications.PushChatSessionAsync(ToDto(session), ct);

            if (!isAdmin)
            {
                // Khách nhắn tin -> thêm một dòng trong chuông thông báo của Admin.
                await _notifications.ChatMessageFromGuestAsync(
                    sessionId, session.CustomerName, text, ct);
            }
        }

        return dto;
    }

    /// <summary>Admin mở phiên ra đọc -> xoá số tin chưa đọc.</summary>
    public async Task<bool> MarkReadAsync(string sessionId, CancellationToken ct = default)
    {
        var result = await _sessions.UpdateOneAsync(
            s => s.SessionId == sessionId,
            Builders<ChatSessionDocument>.Update.Set(s => s.UnreadCount, 0),
            cancellationToken: ct);

        if (result.MatchedCount == 0) return false;

        var session = await _sessions.Find(s => s.SessionId == sessionId).FirstOrDefaultAsync(ct);
        if (session is not null)
        {
            await _notifications.PushChatSessionAsync(ToDto(session), ct);
        }

        return true;
    }

    // ---- Mapping document -> DTO ----

    private static ChatSessionDto ToDto(ChatSessionDocument s) => new(
        s.SessionId,
        s.CustomerName,
        s.LastMessage,
        s.LastSenderType,
        s.LastMessageAt?.ToUniversalTime().ToString("o"),
        RelativeTime(s.LastMessageAt),
        s.UnreadCount,
        s.MessageCount);

    private static ChatMessageDto ToDto(ChatMessageDocument m) => new(
        m.Id.ToString(),
        m.SessionId,
        m.SenderType,
        m.SenderName,
        m.Message,
        m.CreatedAt.ToUniversalTime().ToString("o"),
        RelativeTime(m.CreatedAt));

    /// <summary>Nhãn thời gian tiếng Việt hiển thị thẳng trên giao diện chat.</summary>
    private static string RelativeTime(DateTime? value)
    {
        if (value is null) return "";

        var delta = DateTime.UtcNow - value.Value.ToUniversalTime();

        if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;
        if (delta < TimeSpan.FromMinutes(1)) return "Vừa xong";
        if (delta < TimeSpan.FromHours(1)) return $"{(int)delta.TotalMinutes} phút trước";
        if (delta < TimeSpan.FromDays(1)) return $"{(int)delta.TotalHours} giờ trước";
        if (delta < TimeSpan.FromDays(7)) return $"{(int)delta.TotalDays} ngày trước";

        return value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }
}
