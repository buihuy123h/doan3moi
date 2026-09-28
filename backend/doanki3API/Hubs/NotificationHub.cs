using doanki3API.Constants;
using Microsoft.AspNetCore.SignalR;

namespace doanki3API.Hubs;

/// <summary>
/// Kênh WebSocket đẩy thông báo xuống trình duyệt ngay khi có sự kiện, thay cho
/// việc frontend cứ 15 giây lại gọi API hỏi "có gì mới không".
///
/// Client sau khi kết nối phải tự xin vào group tương ứng với vai trò đang đăng nhập
/// (JoinRole / JoinAccount). Server chỉ gửi tới đúng group đó.
///
/// LƯU Ý BẢO MẬT: hub không gắn [Authorize] vì tài khoản nhân viên demo đăng nhập
/// bằng nút chọn vai trò nên không có JWT. Nghĩa là về lý thuyết một người có thể
/// tự xin vào group "role:admin" để nghe thông báo. Khi triển khai thật cần bật
/// [Authorize] và lấy role từ Context.User thay vì tin vào tham số client gửi lên.
/// </summary>
public sealed class NotificationHub : Hub
{
    /// <summary>Đường dẫn map hub, dùng chung cho Program.cs và tài liệu.</summary>
    public const string Path = "/hubs/notifications";

    // Tên các sự kiện server đẩy xuống client — frontend lắng nghe đúng những tên này.
    public const string EventNotification = "notification";
    public const string EventNotificationRead = "notificationRead";
    public const string EventNotificationsRead = "notificationsRead";
    public const string EventChatMessage = "chatMessage";
    public const string EventChatSession = "chatSession";
    public const string EventDataChanged = "dataChanged";

    /// <summary>Group mà mọi client đều ở trong — dùng để báo "dữ liệu vừa đổi, tải lại đi".</summary>
    public const string EveryoneGroup = "everyone";

    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, EveryoneGroup);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR tự gỡ connection khỏi mọi group khi ngắt, chỉ cần ghi log lỗi bất thường.
        if (exception is not null)
        {
            _logger.LogDebug(exception, "Client {ConnectionId} ngắt kết nối bất thường.", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Nhân viên vào nghe thông báo của vai trò mình: "admin" | "retail" | "technical" | "accounts".</summary>
    public async Task JoinRole(string role)
    {
        var group = NotificationAudiences.ForRole(role);
        if (group is null) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task LeaveRole(string role)
    {
        var group = NotificationAudiences.ForRole(role);
        if (group is null) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    /// <summary>Khách hàng vào nghe thông báo của riêng mã tài khoản mình.</summary>
    public async Task JoinAccount(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, NotificationAudiences.ForAccount(accountId));
    }

    public async Task LeaveAccount(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NotificationAudiences.ForAccount(accountId));
    }

    /// <summary>
    /// Khách (kể cả chưa đăng nhập) vào nghe tin nhắn của đúng phiên chat đang mở,
    /// nhờ vậy câu trả lời của Admin hiện ra ngay không cần bấm tải lại.
    /// </summary>
    public async Task JoinChatSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, NotificationAudiences.ForChatSession(sessionId));
    }

    public async Task LeaveChatSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NotificationAudiences.ForChatSession(sessionId));
    }
}
