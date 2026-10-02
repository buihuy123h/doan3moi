using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class VwBillingSummary
{
    public string BillId { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;

    public string AccountId { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string CustomerType { get; set; } = null!;

    public string BillingMonth { get; set; } = null!;

    public DateOnly BillingDate { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ServiceTaxAmount { get; set; }

    public decimal LateFeeAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal DueAmount { get; set; }

    public string BillStatus { get; set; } = null!;

    public int? DaysOverdue { get; set; }
}
