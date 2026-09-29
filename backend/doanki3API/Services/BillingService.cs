using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Billing;
using doanki3API.Models;
using doanki3API.Services.Email;
using doanki3API.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Services;

public sealed class BillingService
{
    private readonly MyAppDbContext _context;
    private readonly EmailService _email;
    private readonly NotificationService _notifications;

    public BillingService(
        MyAppDbContext context,
        EmailService email,
        NotificationService notifications)
    {
        _context = context;
        _email = email;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<BillResponse>> GetBillsAsync(CancellationToken cancellationToken)
    {
        var bills = await _context.Bills
            .AsNoTracking()
            .Include(b => b.Account)
                .ThenInclude(a => a.Order)
                    .ThenInclude(o => o.Customer)
            .Include(b => b.Account)
                .ThenInclude(a => a.Order)
                    .ThenInclude(o => o.Plan)
            .Include(b => b.Payments)
            .OrderByDescending(b => b.GeneratedDate)
            .ToListAsync(cancellationToken);

        return bills.Select(MapToResponse).ToList();
    }

    public async Task<BillResponse?> GetBillByIdAsync(string billId, CancellationToken cancellationToken)
    {
        var bill = await _context.Bills
            .AsNoTracking()
            .Include(b => b.Account)
                .ThenInclude(a => a.Order)
                    .ThenInclude(o => o.Customer)
            .Include(b => b.Account)
                .ThenInclude(a => a.Order)
                    .ThenInclude(o => o.Plan)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == billId || b.InvoiceNumber == billId, cancellationToken);

        return bill is null ? null : MapToResponse(bill);
    }

    public async Task<BillResponse> CreateBillAsync(CreateBillRequest request, CancellationToken cancellationToken)
    {
        var accountId = request.AccountId?.Trim() ?? string.Empty;

        // Đảm bảo AccountId tồn tại trong CustomerConnection để thỏa mãn Foreign Key
        var connection = await _context.CustomerConnections
            .Include(c => c.Order)
                .ThenInclude(o => o.Customer)
            .Include(c => c.Order)
                .ThenInclude(o => o.Plan)
            .FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken);

        if (connection is null)
        {
            // Nếu chưa có CustomerConnection, tìm Order có AssignedAccountId hoặc Order đầu tiên khả dụng
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Plan)
                .FirstOrDefaultAsync(o => o.AssignedAccountId == accountId, cancellationToken)
                ?? await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Plan)
                    .OrderByDescending(o => o.ApplicationDate)
                    .FirstOrDefaultAsync(cancellationToken);

            if (order != null)
            {
                connection = new CustomerConnection
                {
                    AccountId = accountId,
                    OrderId = order.OrderId,
                    Status = "Active",
                    InstalledDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    LastStatusReason = "Provisioned for billing"
                };
                _context.CustomerConnections.Add(connection);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Tạo số hóa đơn duy nhất
        var count = await _context.Bills.CountAsync(cancellationToken);
        var year = DateTime.Now.Year;
        var invoiceNum = !string.IsNullOrWhiteSpace(request.InvoiceNumber)
            ? request.InvoiceNumber.Trim()
            : $"NEX-INV-{year}-{(count + 1):D3}";

        if (await _context.Bills.AnyAsync(b => b.InvoiceNumber == invoiceNum, cancellationToken))
        {
            invoiceNum = $"NEX-INV-{year}-{(count + 1):D3}-{DateTime.Now.ToString("mmss")}";
        }

        var billId = "bill-" + DateTime.Now.Ticks.ToString();

        // Server LUÔN tự tính các tổng từ các khoản mục — không tin subtotal/thuế/
        // tổng client gửi (những trường đó client chỉ "đoán", kế toán mới là chủ).
        // Cọc: client không gửi (<=0) thì lấy theo Plan của kết nối (325/500/250).
        var securityDeposit = request.SecurityDeposit;
        if (securityDeposit <= 0m)
        {
            securityDeposit = connection?.Order?.Plan?.SecurityDeposit ?? 0m;
        }

        // Đơn trên 50 kết nối được miễn cọc (Orders.DepositWaived) — ghi 0 thẳng vào hoá
        // đơn, kể cả khi màn hình kế toán vẫn gửi lên số tiền cọc gốc của gói cước.
        if (connection?.Order is { } billedOrder &&
            BulkDiscounts.IsDepositWaived(billedOrder.BulkConnectionsCount))
        {
            securityDeposit = 0m;
        }

        var discountAmount = request.DiscountAmount;
        if (discountAmount <= 0m && request.DiscountPercent > 0m)
        {
            // Chiết khấu áp lên cọc + cước ứng trước (advance) theo spec bulk.
            discountAmount = RoundCurrency(
                (securityDeposit + request.MonthlyRental + request.HourlyCharges) * (request.DiscountPercent / 100m));
        }

        var subtotal = RoundCurrency(
            securityDeposit + request.MonthlyRental + request.HourlyCharges - discountAmount);

        var taxRate = request.ServiceTaxRate > 0m ? request.ServiceTaxRate : 12.24m;
        var taxAmount = RoundCurrency(subtotal * (taxRate / 100m));

        var total = Math.Round(subtotal + taxAmount, 2);

        var bill = new Bill
        {
            BillId = billId,
            InvoiceNumber = invoiceNum,
            AccountId = accountId,
            BillingMonth = string.IsNullOrWhiteSpace(request.BillingMonth) ? DateTime.Now.ToString("MMMM yyyy") : request.BillingMonth.Trim(),
            BillingDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = request.DueDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            SecurityDeposit = securityDeposit,
            MonthlyRental = request.MonthlyRental,
            HourlyCharges = request.HourlyCharges,
            DiscountPercent = request.DiscountPercent,
            DiscountAmount = discountAmount,
            Subtotal = subtotal,
            ServiceTaxRate = taxRate,
            ServiceTaxAmount = taxAmount,
            LateFeeAmount = 0m,
            TotalAmount = total,
            AmountPaid = 0m,
            DueAmount = total,
            Status = "Unpaid",
            GeneratedDate = DateTime.Now
        };

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync(cancellationToken);

        // Nạp lại dữ liệu đầy đủ quan hệ để phản hồi
        var response = (await GetBillByIdAsync(bill.BillId, cancellationToken))!;

        await _notifications.BillCreatedAsync(
            bill.InvoiceNumber,
            bill.AccountId,
            response.CustomerName,
            bill.TotalAmount,
            cancellationToken);

        await _notifications.NotifyDataChangedAsync("bills", bill.InvoiceNumber, cancellationToken);

        return response;
    }

    public async Task<BillResponse?> UpdateBillAsync(string billId, UpdateBillRequest request, CancellationToken cancellationToken)
    {
        var key = billId?.Trim() ?? string.Empty;

        var bill = await _context.Bills
            .FirstOrDefaultAsync(b => b.BillId == key || b.InvoiceNumber == key, cancellationToken);

        if (bill is null) return null;

        if (!string.IsNullOrWhiteSpace(request.BillingMonth)) bill.BillingMonth = request.BillingMonth.Trim();
        if (request.DueDate.HasValue) bill.DueDate = request.DueDate.Value;
        if (request.SecurityDeposit.HasValue) bill.SecurityDeposit = request.SecurityDeposit.Value;
        if (request.MonthlyRental.HasValue) bill.MonthlyRental = request.MonthlyRental.Value;
        if (request.HourlyCharges.HasValue) bill.HourlyCharges = request.HourlyCharges.Value;
        if (request.DiscountPercent.HasValue) bill.DiscountPercent = request.DiscountPercent.Value;
        if (request.DiscountAmount.HasValue) bill.DiscountAmount = request.DiscountAmount.Value;
        if (request.ServiceTaxRate.HasValue) bill.ServiceTaxRate = request.ServiceTaxRate.Value;
        if (request.LateFeeAmount.HasValue) bill.LateFeeAmount = request.LateFeeAmount.Value;

        bill.Subtotal = RoundCurrency(
            bill.SecurityDeposit + bill.MonthlyRental + bill.HourlyCharges - bill.DiscountAmount);
        bill.ServiceTaxAmount = RoundCurrency(bill.Subtotal * (bill.ServiceTaxRate / 100m));
        bill.TotalAmount = RoundCurrency(bill.Subtotal + bill.ServiceTaxAmount + bill.LateFeeAmount);
        bill.DueAmount = Math.Max(0m, RoundCurrency(bill.TotalAmount - bill.AmountPaid));
        bill.Status = bill.DueAmount <= 0.01m
            ? "Paid"
            : bill.AmountPaid > 0m ? "Partially Paid" : "Unpaid";

        await _context.SaveChangesAsync(cancellationToken);

        return await GetBillByIdAsync(bill.BillId, cancellationToken);
    }

    public async Task<PaymentResponse?> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var bill = await _context.Bills
            .Include(b => b.Payments)
            .Include(b => b.Account)
                .ThenInclude(a => a.Order)
                    .ThenInclude(o => o.Customer)
            .FirstOrDefaultAsync(b => 
                (!string.IsNullOrEmpty(request.InvoiceNumber) && b.InvoiceNumber == request.InvoiceNumber.Trim()) ||
                (!string.IsNullOrEmpty(request.BillId) && b.BillId == request.BillId.Trim()),
                cancellationToken);

        if (bill is null) return null;

        // Đếm trước khi ghi khoản mới: lần thanh toán đầu tiên của tài khoản này chính là
        // lúc kế toán xác minh xong, khách mới được cấp mã đăng nhập.
        var previousPayments = await _context.Payments
            .CountAsync(p => p.Bill.AccountId == bill.AccountId, cancellationToken);

        var paymentId = "PAY-" + DateTime.Now.Ticks.ToString().Substring(10);
        var refNo = !string.IsNullOrWhiteSpace(request.ReferenceNumber)
            ? request.ReferenceNumber.Trim()
            : "REF-" + DateTime.Now.ToString("yyyyMMddHHmmss");

        var payment = new Payment
        {
            PaymentId = paymentId,
            ReceiptNumber = "REC-" + paymentId,
            BillId = bill.BillId,
            AmountPaid = request.AmountPaid,
            PaymentDate = DateTime.Now,
            PaymentMode = string.IsNullOrWhiteSpace(request.PaymentMode) ? "Bank Transfer/NEFT" : request.PaymentMode.Trim(),
            ReferenceNumber = refNo,
            Status = "Completed",
            Notes = request.Notes
        };

        _context.Payments.Add(payment);

        bill.AmountPaid = Math.Round(bill.AmountPaid + request.AmountPaid, 2);
        bill.DueAmount = Math.Max(0m, Math.Round(bill.TotalAmount - bill.AmountPaid, 2));
        bill.Status = bill.DueAmount <= 0.01m ? "Paid" : "Partially Paid";

        await _context.SaveChangesAsync(cancellationToken);

        // Kế toán đã xác minh thanh toán -> gửi Mã tài khoản về email, đây là mã khách
        // dùng để đăng nhập cổng khách hàng (bước 1 của đăng nhập 2 lớp).
        var customer = bill.Account?.Order?.Customer;
        if (!string.IsNullOrWhiteSpace(customer?.Email))
        {
            _ = Task.Run(() => _email.SendAccountActivatedAsync(
                customer!.Email!,
                string.IsNullOrWhiteSpace(customer.FullName) ? "Quý khách" : customer.FullName,
                bill.AccountId,
                bill.InvoiceNumber,
                request.AmountPaid,
                isFirstPayment: previousPayments == 0,
                CancellationToken.None));
        }

        await _notifications.PaymentRecordedAsync(
            bill.InvoiceNumber,
            bill.AccountId,
            string.IsNullOrWhiteSpace(customer?.FullName) ? "Khách hàng" : customer!.FullName,
            request.AmountPaid,
            bill.Status == "Paid",
            cancellationToken);

        await _notifications.NotifyDataChangedAsync("bills", bill.InvoiceNumber, cancellationToken);

        return new PaymentResponse(
            payment.PaymentId,
            payment.ReceiptNumber,
            payment.BillId,
            payment.EmployeeId,
            payment.AmountPaid,
            payment.PaymentDate,
            payment.PaymentMode,
            payment.ReferenceNumber,
            payment.Status,
            payment.Notes);
    }

    // FE tính tiền bằng Number.toFixed(2) (làm tròn 0.5 lên), còn Math.Round mặc định của
    // .NET làm tròn về số chẵn — lệch 1 cent ở dòng thuế nếu không thống nhất cách làm tròn.
    private static decimal RoundCurrency(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Mô tả hóa đơn "của cái gì" — đọc được cho cả khách hàng lẫn nhân viên:
    /// gói cước nào, kỳ nào, gồm những khoản gì. Dùng chung cho BillResponse (kế toán)
    /// và CustomerBillDto (cổng khách hàng) để mọi nơi diễn đạt thống nhất.
    /// </summary>
    public static string BuildInvoiceDescription(
        string? planName,
        string billingMonth,
        decimal hourlyCharges,
        decimal securityDeposit,
        decimal lateFeeAmount)
    {
        var period = billingMonth;
        if (DateTime.TryParseExact(billingMonth?.Trim(), "MMMM yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthDate))
        {
            // BillingMonth lưu dạng "September 2026" (khách Việt đọc "September" không tự nhiên).
            period = $"tháng {monthDate.Month}/{monthDate.Year}";
        }
        else if (string.IsNullOrWhiteSpace(period))
        {
            period = DateTime.Now.ToString($"'tháng' M/yyyy");
        }

        var components = new List<string> { "cước thuê bao" };
        if (hourlyCharges > 0m) components.Add("phí thuê thiết bị");
        if (securityDeposit > 0m) components.Add("tiền cọc thiết bị");
        if (lateFeeAmount > 0m) components.Add("phí nộp trễ");
        components.Add("thuế dịch vụ");

        var plan = string.IsNullOrWhiteSpace(planName) ? "viễn thông" : planName.Trim();
        return $"Hóa đơn cước phí gói cước {plan} kỳ {period} (gồm {string.Join(", ", components)})";
    }

    private static BillResponse MapToResponse(Bill b)
    {
        var customer = b.Account?.Order?.Customer;
        var plan = b.Account?.Order?.Plan;

        var payments = (b.Payments ?? Enumerable.Empty<Payment>())
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentResponse(
                p.PaymentId,
                p.ReceiptNumber,
                p.BillId,
                p.EmployeeId,
                p.AmountPaid,
                p.PaymentDate,
                p.PaymentMode,
                p.ReferenceNumber,
                p.Status,
                p.Notes))
            .ToList();

        return new BillResponse(
            b.BillId,
            b.InvoiceNumber,
            BuildInvoiceDescription(
                plan?.Name,
                b.BillingMonth,
                b.HourlyCharges,
                b.SecurityDeposit,
                b.LateFeeAmount),
            b.AccountId,
            customer?.FullName ?? "Nexus Valued Subscriber",
            customer?.ContactNumber ?? "N/A",
            customer?.Email ?? "N/A",
            b.Account?.Order?.InstallationAddress ?? "N/A",
            plan?.Name ?? "Telecommunications Service Plan",
            plan?.ConnectionType ?? "Broadband",
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
            payments);
    }
}
