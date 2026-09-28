using System.Threading;
using System.Threading.Tasks;
using doanki3API.Constants;
using doanki3API.DTOs.Billing;
using doanki3API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Accounts;

/// <summary>
/// API cho Accounts department (theo spec §6): tính cước, sinh bill, cập nhật chi tiết
/// bill — "updating has to be done by the accounts department alone" — và ghi payment
/// thu tại văn phòng thành phố. Toàn bộ endpoint chỉ dành cho role Senior Accountant.
/// </summary>
[ApiController]
[Route("api/billing")]
[Authorize(Roles = EmployeeRoles.Accounts)]
public sealed class BillingController : ControllerBase
{
    private readonly BillingService _billingService;

    public BillingController(BillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet("bills")]
    public async Task<IActionResult> GetBills(CancellationToken cancellationToken)
    {
        var bills = await _billingService.GetBillsAsync(cancellationToken);
        return Ok(bills);
    }

    [HttpGet("bills/{id}")]
    public async Task<IActionResult> GetBillById(string id, CancellationToken cancellationToken)
    {
        var bill = await _billingService.GetBillByIdAsync(id, cancellationToken);
        return bill is null ? NotFound(new { message = "Không tìm thấy hóa đơn" }) : Ok(bill);
    }

    [HttpPost("bills")]
    public async Task<IActionResult> CreateBill([FromBody] CreateBillRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccountId))
        {
            return BadRequest(new { message = "Mã tài khoản (AccountId) là bắt buộc." });
        }

        var bill = await _billingService.CreateBillAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBillById), new { id = bill.BillId }, bill);
    }

    [HttpPut("bills/{id}")]
    public async Task<IActionResult> UpdateBill(string id, [FromBody] UpdateBillRequest request, CancellationToken cancellationToken)
    {
        var bill = await _billingService.UpdateBillAsync(id, request, cancellationToken);
        return bill is null ? NotFound(new { message = "Không tìm thấy hóa đơn cần cập nhật." }) : Ok(bill);
    }

    [HttpPost("bills/payments")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        if (request.AmountPaid <= 0m)
        {
            return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });
        }

        var payment = await _billingService.RecordPaymentAsync(request, cancellationToken);
        return payment is null
            ? NotFound(new { message = "Không tìm thấy hóa đơn cần thanh toán." })
            : Ok(payment);
    }
}
