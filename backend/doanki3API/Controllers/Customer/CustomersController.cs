using System.Security.Claims;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Controllers.CustomerPortal;

// ===== Request/Response DTOs =====
public sealed record UpdateCustomerProfileRequest(
    string FullName,
    string? Email,
    string? ContactNumber,
    string? Address);

public sealed record CustomerPaymentDto(
    string PaymentId,
    string? ReceiptNumber,
    decimal AmountPaid,
    DateTime PaymentDate,
    string PaymentMode,
    string? ReferenceNumber,
    string? Notes);

public sealed record CustomerBillDto(
    string BillId,
    string InvoiceNumber,
    string AccountId,
    string? CustomerName,
    string BillingMonth,
    DateOnly BillingDate,
    DateOnly DueDate,
    decimal SecurityDeposit,
    decimal MonthlyRental,
    decimal HourlyCharges,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal Subtotal,
    decimal ServiceTaxRate,
    decimal ServiceTaxAmount,
    decimal LateFeeAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal DueAmount,
    string Status,
    DateTime GeneratedDate,
    List<CustomerPaymentDto> Payments);

/// <summary>
/// Cổng khách hàng (spec §6: customer xem bill + sửa contact details của mình).
/// /api/billing khoá role Senior Accountant nên khách không đọc hoá đơn của
/// chính mình qua đó được — hai endpoint dưới đây lọc quyền sở hữu tại server.
/// NameIdentifier trong JWT là CustomerId (login mật khẩu / đăng ký) HOẶC
/// AccountId (login OTP qua email), cả hai đều phân giải về hoá đơn của khách.
/// </summary>
[ApiController]
[Route("api/customer")]
[Authorize(Roles = EmployeeRoles.Customer)]
public class CustomersController : ControllerBase
{
    private readonly MyAppDbContext _context;

    public CustomersController(MyAppDbContext context)
    {
        _context = context;
    }

    [HttpGet("bills")]
    public async Task<IActionResult> GetMyBills(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Unauthorized(new { message = "Token không chứa danh tính khách hàng." });
        }

        subject = subject.Trim();
        var (clean, dashed) = AccountIdForms(subject);

        var bills = await _context.Bills
            .Where(b => (b.Account.Order != null && b.Account.Order.CustomerId == subject)
                     || b.AccountId == subject
                     || b.AccountId == clean
                     || (dashed != null && b.AccountId == dashed))
            .OrderByDescending(b => b.GeneratedDate)
            .Select(b => new CustomerBillDto(
                b.BillId,
                b.InvoiceNumber,
                b.AccountId,
                b.Account.Order != null ? b.Account.Order.Customer.FullName : null,
                b.BillingMonth,
                b.BillingDate,
                b.DueDate,
                b.SecurityDeposit,
                b.MonthlyRental,
                b.HourlyCharges,
                b.DiscountPercent,
                b.DiscountAmount,
                b.Subtotal,
                b.ServiceTaxRate,
                b.ServiceTaxAmount,
                b.LateFeeAmount,
                b.TotalAmount,
                b.AmountPaid,
                b.DueAmount,
                b.Status,
                b.GeneratedDate,
                b.Payments
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new CustomerPaymentDto(
                        p.PaymentId,
                        p.ReceiptNumber,
                        p.AmountPaid,
                        p.PaymentDate,
                        p.PaymentMode,
                        p.ReferenceNumber,
                        p.Notes))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Ok(bills);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Unauthorized(new { message = "Token không chứa danh tính khách hàng." });
        }

        var customer = await ResolveCustomerAsync(subject.Trim(), cancellationToken);
        if (customer is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ khách hàng của bạn." });
        }

        var fullName = request.FullName?.Trim() ?? "";
        if (fullName.Length == 0)
        {
            return BadRequest(new { message = "Họ và tên không được để trống." });
        }

        var email = request.Email?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(email))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                return BadRequest(new { message = "Email không hợp lệ." });
            }
            var emailTaken = await _context.Customers.AnyAsync(
                    c => c.Email == email && c.CustomerId != customer.CustomerId, cancellationToken)
                || await _context.Employees.AnyAsync(e => e.Email == email, cancellationToken);
            if (emailTaken)
            {
                return Conflict(new { message = "Email này đã được sử dụng." });
            }
        }

        var phone = request.ContactNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            if (phone.Length != 10 || phone.Any(c => c < '0' || c > '9'))
            {
                return BadRequest(new { message = "Số điện thoại phải gồm đúng 10 chữ số." });
            }
            var phoneTaken = await _context.Customers.AnyAsync(
                    c => c.ContactNumber == phone && c.CustomerId != customer.CustomerId, cancellationToken)
                || await _context.Employees.AnyAsync(e => e.Phone == phone, cancellationToken);
            if (phoneTaken)
            {
                return Conflict(new { message = "Số điện thoại này đã được sử dụng." });
            }
        }

        customer.FullName = fullName;
        if (!string.IsNullOrWhiteSpace(email)) customer.Email = email;
        if (!string.IsNullOrWhiteSpace(phone)) customer.ContactNumber = phone;
        if (request.Address != null) customer.Address = request.Address.Trim();
        customer.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = customer.CustomerId,
            name = customer.FullName,
            email = customer.Email ?? string.Empty,
            phoneNumber = customer.ContactNumber,
            address = customer.Address,
        });
    }

    /// <summary>
    /// JWT của khách mang NameIdentifier là CustomerId (login mật khẩu) hoặc
    /// AccountId (login OTP) — lần lượt thử từng kiểu để về bản ghi Customer.
    /// </summary>
    private async Task<Customer?> ResolveCustomerAsync(string subject, CancellationToken ct)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(
            c => c.CustomerId == subject, ct);
        if (customer is not null) return customer;

        var (clean, dashed) = AccountIdForms(subject);
        var connection = await _context.CustomerConnections
            .Include(x => x.Order)
            .FirstOrDefaultAsync(
                x => x.AccountId == subject || x.AccountId == clean
                  || (dashed != null && x.AccountId == dashed), ct);

        var customerId = connection?.Order?.CustomerId;
        if (string.IsNullOrWhiteSpace(customerId)) return null;

        return await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
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
