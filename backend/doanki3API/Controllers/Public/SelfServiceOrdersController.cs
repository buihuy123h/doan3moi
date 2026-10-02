using doanki3API.DTOs.Orders;
using doanki3API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Public;

/// <summary>
/// Khách tự đăng ký mua dịch vụ ở trang /register — không cần đăng nhập.
///
/// Vì sao tách khỏi /api/Technical/orders (endpoint đó bắt buộc JWT):
/// khách cũ quay lại mua thêm bằng CHÍNH email đã dùng thì /api/auth/register trả 409
/// và không cấp token, nên đơn thứ hai trở đi luôn bị chặn 401. Ở đây đơn được gắn vào
/// hồ sơ khách có sẵn theo email (xem TechnicalOrderService.CreateOrderAsync), nhờ vậy
/// một người mua bao nhiêu lần cũng được và dùng lại email cũ.
///
/// An toàn: endpoint chỉ TẠO đơn ở trạng thái chờ chi nhánh duyệt (PendingRetail), không
/// đọc hay sửa được dữ liệu nào khác; trạng thái, mã đơn, chiết khấu đều do server tự quyết.
/// </summary>
[ApiController]
[Route("api/public/orders")]
[AllowAnonymous]
public sealed class SelfServiceOrdersController : ControllerBase
{
    private readonly TechnicalOrderService _orderService;

    public SelfServiceOrdersController(TechnicalOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            // Email là thứ duy nhất để nhận ra khách cũ và gửi mã đơn — bắt buộc với khách tự đăng ký.
            return BadRequest(new { message = "Vui lòng nhập email để nhận mã đơn hàng." });
        }

        var validationError = TechnicalOrderService.ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        // Khách tự đăng ký không được tự chọn nhân viên phụ trách.
        request.AssignedEmployeeId = null;

        var order = await _orderService.CreateOrderAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, order);
    }
}
