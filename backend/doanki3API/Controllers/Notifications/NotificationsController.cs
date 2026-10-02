using doanki3API.Constants;
using doanki3API.DTOs.Notifications;
using doanki3API.Services.Notifications;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace doanki3API.Controllers.Notifications;

/// <summary>
/// Đọc lịch sử thông báo và đánh dấu đã đọc.
///
/// Thông báo MỚI thì không cần gọi API: server tự đẩy qua SignalR (hub /hubs/notifications).
/// Các endpoint ở đây dùng cho lúc vừa mở trang (tải danh sách cũ) và khi bấm "đã đọc".
/// Không yêu cầu đăng nhập vì tài khoản nhân viên demo không có JWT — xem ghi chú
/// bảo mật trong NotificationHub.
/// </summary>
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationService notifications,
        ILogger<NotificationsController> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    /// <summary>Danh sách thông báo của một người nhận, mới nhất lên đầu.</summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string audience,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var key = NormalizeAudience(audience);
        if (key is null) return InvalidAudience();

        try
        {
            return Ok(await _notifications.GetAsync(key, limit, ct));
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>Số thông báo chưa đọc — dùng cho chấm đỏ trên chuông khi vừa tải trang.</summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetCount([FromQuery] string audience, CancellationToken ct)
    {
        var key = NormalizeAudience(audience);
        if (key is null) return InvalidAudience();

        try
        {
            return Ok(await _notifications.CountAsync(key, ct));
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>Đánh dấu một thông báo đã đọc.</summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id, [FromQuery] string audience, CancellationToken ct)
    {
        var key = NormalizeAudience(audience);
        if (key is null) return InvalidAudience();

        try
        {
            if (!await _notifications.MarkReadAsync(key, id, ct))
            {
                return NotFound(new { message = "Không tìm thấy thông báo." });
            }

            return NoContent();
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>Đánh dấu tất cả thông báo của người nhận này là đã đọc.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead([FromQuery] string audience, CancellationToken ct)
    {
        var key = NormalizeAudience(audience);
        if (key is null) return InvalidAudience();

        try
        {
            var updated = await _notifications.MarkAllReadAsync(key, ct);
            return Ok(new { updated });
        }
        catch (MongoException ex)
        {
            return MongoUnavailable(ex);
        }
    }

    /// <summary>
    /// Chỉ chấp nhận đúng dạng "role:&lt;vai trò hợp lệ&gt;" hoặc "account:&lt;mã tài khoản&gt;",
    /// tránh việc client gửi chuỗi tuỳ ý rồi tạo ra vô số nhóm rác trong CSDL.
    /// </summary>
    private static string? NormalizeAudience(string? audience)
    {
        var value = audience?.Trim();
        if (string.IsNullOrEmpty(value)) return null;

        if (value.StartsWith("role:", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationAudiences.ForRole(value[5..]);
        }

        if (value.StartsWith("account:", StringComparison.OrdinalIgnoreCase))
        {
            var accountId = NotificationAudiences.NormalizeAccountId(value[8..]);
            return accountId.Length is >= 8 and <= 24 ? $"account:{accountId}" : null;
        }

        return null;
    }

    private IActionResult InvalidAudience() => BadRequest(new
    {
        message = "Tham số 'audience' phải có dạng role:admin|retail|technical|accounts hoặc account:<mã tài khoản>."
    });

    private IActionResult MongoUnavailable(MongoException ex)
    {
        _logger.LogError(ex, "Không kết nối được MongoDB cho module thông báo.");
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            message = "Dịch vụ thông báo tạm thời không khả dụng (không kết nối được MongoDB)."
        });
    }
}
