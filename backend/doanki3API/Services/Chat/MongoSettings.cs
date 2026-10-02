namespace doanki3API.Services.Chat;

/// <summary>Cấu hình MongoDB cho module chat — đọc từ section "Mongo" trong appsettings.json.</summary>
public sealed class MongoSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    public string Database { get; set; } = "NexusChat";

    public string SessionsCollection { get; set; } = "chat_sessions";

    public string MessagesCollection { get; set; } = "chat_messages";

    /// <summary>Collection lưu thông báo real-time (xem Services/Notifications/NotificationService.cs).</summary>
    public string NotificationsCollection { get; set; } = "notifications";
}
