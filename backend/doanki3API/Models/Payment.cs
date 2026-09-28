using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Payment
{
    public string PaymentId { get; set; } = null!;

    public string? ReceiptNumber { get; set; }

    public string BillId { get; set; } = null!;

    public string? EmployeeId { get; set; }

    public decimal AmountPaid { get; set; }

    public DateTime PaymentDate { get; set; }

    public string PaymentMode { get; set; } = null!;

    public string? ReferenceNumber { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Bill Bill { get; set; } = null!;

    public virtual Employee? Employee { get; set; }
}
