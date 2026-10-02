using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace doanki3API.Models.Chat;

/// <summary>
/// Một phiên chat giữa khách (kể cả khách chưa đăng nhập) và Admin.
/// Lưu trong MongoDB — collection "chat_sessions".
/// Khác với dữ liệu nghiệp vụ (đơn hàng, hoá đơn...) nằm ở SQL Server, tin nhắn chat
/// có dạng document, số lượng lớn và không cần ràng buộc quan hệ nên để ở Mongo.
/// </summary>
public class ChatSessionDocument
{
    /// <summary>Mã phiên do server sinh, khách giữ trong localStorage để quay lại xem tiếp.</summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string SessionId { get; set; } = null!;

    public string CustomerName { get; set; } = "";

    /// <summary>Nội dung tin nhắn cuối — để hiển thị nhanh ở danh sách phiên bên Admin.</summary>
    public string LastMessage { get; set; } = "";

    /// <summary>"guest" hoặc "admin".</summary>
    public string LastSenderType { get; set; } = "admin";

    public DateTime? LastMessageAt { get; set; }

    /// <summary>Số tin nhắn của khách mà Admin chưa đọc.</summary>
    public int UnreadCount { get; set; }

    public int MessageCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Một tin nhắn trong phiên chat — collection "chat_messages".</summary>
public class ChatMessageDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string SessionId { get; set; } = null!;

    /// <summary>"guest" (khách) hoặc "admin" (nhân viên trả lời).</summary>
    public string SenderType { get; set; } = "guest";

    public string SenderName { get; set; } = "";

    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}
