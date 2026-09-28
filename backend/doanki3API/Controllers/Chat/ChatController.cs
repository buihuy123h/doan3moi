using doanki3API.DTOs.Chat;
using doanki3API.Services.Chat;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace doanki3API.Controllers.Chat;

/// <summary>
/// Live chat giữa khách ở trang chủ và Admin. Dữ liệu nằm trong MongoDB
/// (xem <see cref="ChatService"/>), không dùng SQL Server như các module nghiệp vụ khác.
/// Không yêu cầu đăng nhập vì khách vãng lai cũng phải chat được.
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chat;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chat, ILogger<ChatController> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    /// <summary>Khách bắt đầu một phiên chat mới — server tự gửi lời chào từ Admin.</summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession(
        [FromBody] StartChatSessionRequest request,
        CancellationToken ct)
    {
        try
        {
            var session = await _chat.StartSessionAsync(request.CustomerName, ct);
            return Ok(session);
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>Admin xem danh sách phiên chat, lọc theo tên khách nếu có.</summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] string? search, CancellationToken ct)
    {
        try
        {
            return Ok(await _chat.GetSessionsAsync(search, ct));
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>Toàn bộ tin nhắn của một phiên — dùng chung cho khách và Admin.</summary>
    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<IActionResult> GetMessages(string sessionId, CancellationToken ct)
    {
        try
        {
            if (!await _chat.SessionExistsAsync(sessionId, ct))
            {
                return NotFound(new { message = "Không tìm thấy phiên chat." });
            }

            return Ok(await _chat.GetMessagesAsync(sessionId, ct));
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>
    /// Gửi tin nhắn vào phiên. senderType = "guest" khi khách nhắn,
    /// "admin" khi nhân viên trả lời.
    /// </summary>
    [HttpPost("sessions/{sessionId}/messages")]
    public async Task<IActionResult> SendMessage(
        string sessionId,
        [FromBody] SendChatMessageRequest request,
        CancellationToken ct)
    {
        try
        {
            var message = await _chat.SendMessageAsync(
                sessionId, request.SenderType, request.SenderName, request.Message, ct);

            if (message == null)
            {
                return NotFound(new { message = "Không tìm thấy phiên chat." });
            }

            return Ok(message);
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>Admin đánh dấu đã đọc hết tin nhắn của khách trong phiên.</summary>
    [HttpPost("sessions/{sessionId}/read")]
    public async Task<IActionResult> MarkRead(string sessionId, CancellationToken ct)
    {
        try
        {
            if (!await _chat.MarkReadAsync(sessionId, ct))
            {
                return NotFound(new { message = "Không tìm thấy phiên chat." });
            }

            return NoContent();
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>
    /// Mongo không chạy thì trả 503 kèm thông báo rõ ràng, thay vì để lỗi 500 khó hiểu.
    /// Frontend bắt mã này để báo "chat tạm thời không khả dụng".
    /// </summary>
    private IActionResult MongoUnavailable(MongoException ex)
    {
        _logger.LogError(ex, "Không kết nối được MongoDB cho module chat.");
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            message = "Dịch vụ chat tạm thời không khả dụng (không kết nối được MongoDB)."
        });
    }
}
