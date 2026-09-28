using doanki3API.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Technical;

[ApiController]
// Base của mọi controller dưới /api/Technical: mặc định phải đăng nhập (JWT).
// Endpoint nào cần hẹp hơn thì khai báo [Authorize(Roles = ...)] ngay trên action —
// nhiều attribute [Authorize] được AND với nhau nên chỉ cần thêm, không ghi đè.
[Authorize]
public abstract class TechnicalApiControllerBase : ControllerBase
{
    protected IActionResult FromServiceResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        var body = new { message = result.Error };
        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(body),
            ServiceErrorType.Conflict => Conflict(body),
            _ => BadRequest(body)
        };
    }
}
