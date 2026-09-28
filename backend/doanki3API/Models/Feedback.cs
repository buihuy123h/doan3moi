using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Feedback
{
    public string FeedbackId { get; set; } = null!;

    public string? AccountId { get; set; }

    public string? OrderId { get; set; }

    public string? CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public byte Rating { get; set; }

    public string Category { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Response { get; set; }

    public string? RespondedBy { get; set; }

    public DateTime? RespondedAt { get; set; }

    public virtual CustomerConnection? Account { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Employee? RespondedByNavigation { get; set; }
}
