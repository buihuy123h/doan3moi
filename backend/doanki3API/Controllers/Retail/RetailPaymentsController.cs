using System.Security.Claims;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Billing;
using doanki3API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Controllers.Retail;

/// <summary>
/// Ghi nhận thanh toán thu TẠI CỬA HÀNG BÁN LẺ — spec: "The details of the payments
/// will be maintained by the employees of the retail outlets and by the accounts
/// department (if the payment is done at the office with in the city)".
///
/// Nhân viên cửa hàng không có quyền gọi /api/billing/* (controller đó khoá
/// [Authorize(Roles = Senior Accountant)]), nên endpoint này cho họ ghi payment
/// qua chung BillingService.RecordPaymentAsync. Khách tự thanh toán online trong
/// cổng khách hàng cũng đi qua đây nhưng bị chặn theo quyền sở hữu hóa đơn.
/// </summary>
[ApiController]
[Route("api/retail")]
[Authorize(Roles = EmployeeRoles.RecordRetailPayment)]
public sealed class RetailPaymentsController : ControllerBase
{
    private readonly BillingService _billingService;
    private readonly MyAppDbContext _context;

    public RetailPaymentsController(BillingService billingService, MyAppDbContext context)
    {
        _billingService = billingService;
        _context = context;
    }

    [HttpPost("payments")]
    public async Task<IActionResult> RecordPayment(
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AmountPaid <= 0m)
        {
            return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });
        }

        // Khách tự trả online: chỉ được trả hóa đơn của chính mình. NameIdentifier
        // trong token là CustomerId (login mật khẩu) hoặc AccountId (login OTP) —
        // so cả hai dạng Account ID có/không gạch nối vì DB lưu "T064-000000000001".
        if (User.IsInRole(EmployeeRoles.Customer))
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Unauthorized(new { message = "Token không chứa danh tính khách hàng." });
            }

            subject = subject.Trim();
            var (clean, dashed) = AccountIdForms(subject);
            var invoice = !string.IsNullOrEmpty(request.InvoiceNumber) ? request.InvoiceNumber.Trim() : null;
            var billId = !string.IsNullOrEmpty(request.BillId) ? request.BillId.Trim() : null;

            var belongs = await _context.Bills.AnyAsync(
                b => ((invoice != null && b.InvoiceNumber == invoice) || (billId != null && b.BillId == billId))
                     && (b.AccountId == subject || b.AccountId == clean ||
                         (dashed != null && b.AccountId == dashed) ||
                         (b.Account.Order != null && b.Account.Order.CustomerId == subject)),
                cancellationToken);

            if (!belongs)
            {
                return Forbid();
            }
        }

        var payment = await _billingService.RecordPaymentAsync(request, cancellationToken);
        return payment is null
            ? NotFound(new { message = "Không tìm thấy hóa đơn cần thanh toán." })
            : Ok(payment);
    }

    /// <summary>
    /// Account ID tồn tại ở hai dạng "T064-000000000001" và "T064000000000001".
    /// Trả về (không-gạch, có-gạch); dạng có gạch chỉ có nghĩa khi chuỗi 16 ký
    /// tự bắt đầu bằng D/B/T — CustomerId (CUSxxxxxx) không bị biến dạng.
    /// </summary>
    private static (string Clean, string? Dashed) AccountIdForms(string value)
    {
        var clean = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        string? dashed = clean.Length == 16 && "DBT".Contains(clean[0])
            ? $"{clean[..4]}-{clean[4..]}"
            : null;
        return (clean, dashed);
    }
}
