using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace doanki3API.Models.Notifications;

/// <summary>
/// Một thông báo gửi tới đúng một "người nhận" (Audience) — collection "notifications".
/// Cùng nằm trong MongoDB NexusChat với module chat: thông báo cũng là dữ liệu dạng
/// document, sinh ra liên tục, không cần ràng buộc quan hệ như đơn hàng hay hoá đơn.
///
/// Một sự kiện nghiệp vụ (VD: khách đặt đơn) có thể sinh ra NHIỀU document — mỗi bên
/// nhận một bản riêng, nhờ vậy trạng thái "đã đọc" của Admin không ảnh hưởng tới Bán lẻ.
/// </summary>
public class NotificationDocument
{
    /// <summary>Mã thông báo do server sinh, dạng "NTF-{guid}".</summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string NotificationId { get; set; } = null!;

    /// <summary>
    /// Người nhận: "role:admin" | "role:retail" | "role:technical" | "role:accounts"
    /// hoặc "account:B064000000000002" (khách hàng cụ thể).
    /// Xem <see cref="doanki3API.Constants.NotificationAudiences"/>.
    /// </summary>
    public string Audience { get; set; } = null!;

    /// <summary>order | billing | alert | hardware | system | stock | chat | feedback.</summary>
    public string Type { get; set; } = "system";

    public string TitleVi { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string DescVi { get; set; } = "";
    public string DescEn { get; set; } = "";

    /// <summary>Đường dẫn dashboard mà thông báo trỏ tới, VD "/retail".</summary>
    public string TargetPath { get; set; } = "/";

    /// <summary>Tab bên trong dashboard, VD "approval-queue".</summary>
    public string TargetTab { get; set; } = "";

    /// <summary>Role cần có để vào được TargetPath (frontend tự chuyển role nếu lệch).</summary>
    public string? TargetRole { get; set; }

    public string SectionVi { get; set; } = "";
    public string SectionEn { get; set; } = "";

    /// <summary>Mã bản ghi liên quan (mã đơn, số hoá đơn, mã phiên chat...) để mở thẳng.</summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Khoá gộp: thông báo mới có cùng ReplaceKey sẽ XOÁ thông báo cũ chưa đọc.
    /// Dùng cho tin lặp lại như "kho sắp hết hàng" hay "khách nhắn tin" — tránh
    /// 20 tin nhắn sinh ra 20 thông báo.
    /// </summary>
    public string? ReplaceKey { get; set; }

    public bool Read { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Mốc TTL index tự xoá document — thông báo chỉ giữ 30 ngày.</summary>
    public DateTime ExpiresAt { get; set; }
}
