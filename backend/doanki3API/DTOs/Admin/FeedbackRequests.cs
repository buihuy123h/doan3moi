using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Admin;

/// <summary>
/// Khách gửi phản hồi từ cổng khách hàng.
/// Dùng DTO thay vì bind thẳng entity Feedback: entity có FeedbackId là bắt buộc
/// (khoá chính) nên mọi request của frontend — vốn để server tự sinh mã — đều bị
/// trả 400 trước khi vào được controller.
/// </summary>
public sealed class CreateFeedbackRequest
{
    /// <summary>Bỏ trống thì server tự sinh mã.</summary>
    public string? FeedbackId { get; set; }

    /// <summary>Mã tài khoản khách (có hoặc không có dấu gạch đều được).</summary>
    public string? AccountId { get; set; }

    public string? OrderId { get; set; }

    public string? CustomerId { get; set; }

    [Required(ErrorMessage = "Thiếu tên khách hàng.")]
    [StringLength(100, ErrorMessage = "Tên khách hàng không được quá 100 ký tự.")]
    public string CustomerName { get; set; } = null!;

    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    public byte Rating { get; set; } = 5;

    /// <summary>Phải nằm trong CK_Feedback_Category — xem FeedbackCategories.</summary>
    [Required(ErrorMessage = "Thiếu danh mục phản hồi.")]
    public string Category { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung phản hồi không được để trống.")]
    public string Message { get; set; } = null!;

    /// <summary>Bỏ trống thì mặc định "New".</summary>
    public string? Status { get; set; }
}

/// <summary>Quản trị trả lời hoặc đổi trạng thái một phản hồi.</summary>
public sealed class UpdateFeedbackRequest
{
    public string? Response { get; set; }

    /// <summary>Mã nhân viên trả lời (khoá ngoại sang Employee).</summary>
    public string? RespondedBy { get; set; }

    public string? Status { get; set; }
}
