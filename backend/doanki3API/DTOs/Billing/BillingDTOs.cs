using System;
using System.Collections.Generic;

namespace doanki3API.DTOs.Billing;

public sealed record CreateBillRequest(
    string AccountId,
    string BillingMonth,
    decimal SecurityDeposit,
    decimal MonthlyRental,
    decimal HourlyCharges = 0m,
    decimal DiscountPercent = 0m,
    decimal DiscountAmount = 0m,
    decimal Subtotal = 0m,
    decimal ServiceTaxRate = 12.24m,
    decimal ServiceTaxAmount = 0m,
    decimal TotalAmount = 0m,
    DateOnly? DueDate = null,
    string? InvoiceNumber = null);

/// <summary>
/// Cập nhật chi tiết hóa đơn đã phát hành (chỉ Accounts department). Trường nào null
/// thì giữ nguyên giá trị cũ; các khoản tổng (subtotal, thuế, total, công nợ, trạng thái)
/// luôn được tính lại từ các khoản mục sau khi cập nhật.
/// </summary>
public sealed record UpdateBillRequest(
    string? BillingMonth = null,
    DateOnly? DueDate = null,
    decimal? SecurityDeposit = null,
    decimal? MonthlyRental = null,
    decimal? HourlyCharges = null,
    decimal? DiscountPercent = null,
    decimal? DiscountAmount = null,
    decimal? ServiceTaxRate = null,
    decimal? LateFeeAmount = null);

public sealed record PaymentResponse(
    string PaymentId,
    string? ReceiptNumber,
    string BillId,
    string? EmployeeId,
    decimal AmountPaid,
    DateTime PaymentDate,
    string PaymentMode,
    string? ReferenceNumber,
    string Status,
    string? Notes);

public sealed record BillResponse(
    string BillId,
    string InvoiceNumber,
    // "Hóa đơn của cái gì" — mô tả gói cước, kỳ thanh toán và các khoản gồm trong hóa đơn.
    string Description,
    string AccountId,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string InstallationAddress,
    string PlanName,
    string ConnectionType,
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
    IReadOnlyList<PaymentResponse> Payments);

public sealed record RecordPaymentRequest(
    string? InvoiceNumber,
    string? BillId,
    decimal AmountPaid,
    string PaymentMode,
    string? ReferenceNumber,
    string? RecordedBy,
    string? Notes);
