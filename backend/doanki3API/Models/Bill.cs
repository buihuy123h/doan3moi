using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Bill
{
    public string BillId { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;

    public string AccountId { get; set; } = null!;

    public string BillingMonth { get; set; } = null!;

    public DateOnly? BillingPeriodStart { get; set; }

    public DateOnly? BillingPeriodEnd { get; set; }

    public DateOnly BillingDate { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal SecurityDeposit { get; set; }

    public decimal MonthlyRental { get; set; }

    public decimal HourlyCharges { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ServiceTaxRate { get; set; }

    public decimal ServiceTaxAmount { get; set; }

    public decimal LateFeeAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal DueAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime GeneratedDate { get; set; }

    public virtual CustomerConnection Account { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
