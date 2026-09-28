using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Chat;

/// <summary>Dữ liệu một phiên chat trả về cho frontend (khớp ChatSessionDto bên Svelte).</summary>
public sealed record ChatSessionDto(
    string SessionId,
    string CustomerName,
    string LastMessage,
    string LastSenderType,
    string? LastMessageAt,
    string LastMessageAtDisplay,
    int UnreadCount,
    int MessageCount);

/// <summary>Dữ liệu một tin nhắn trả về cho frontend (khớp ChatMessageDto bên Svelte).</summary>
public sealed record ChatMessageDto(
    string Id,
    string SessionId,
    string SenderType,
    string SenderName,
    string Message,
    string CreatedAt,
    string CreatedAtDisplay);

/// <summary>Khách bấm "Bắt đầu chat" ở chatbox trang chủ.</summary>
public sealed class StartChatSessionRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên để bắt đầu trò chuyện.")]
    [StringLength(100, ErrorMessage = "Tên không được quá 100 ký tự.")]
    public string CustomerName { get; set; } = null!;
}

/// <summary>Gửi một tin nhắn vào phiên — dùng chung cho cả khách lẫn Admin.</summary>
public sealed class SendChatMessageRequest
{
    /// <summary>"guest" hoặc "admin". Mặc định là "guest".</summary>
    public string SenderType { get; set; } = "guest";

    [Required(ErrorMessage = "Thiếu tên người gửi.")]
    [StringLength(100, ErrorMessage = "Tên người gửi không được quá 100 ký tự.")]
    public string SenderName { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống.")]
    [StringLength(2000, ErrorMessage = "Tin nhắn không được quá 2000 ký tự.")]
    public string Message { get; set; } = null!;
}
