using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Customer
{
    public string CustomerId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string CustomerType { get; set; } = null!;

    public string? Email { get; set; }

    public string ContactNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? BillingAddress { get; set; }

    public string? TaxNumber { get; set; }

    public string IdProofType { get; set; } = null!;

    public string IdProofNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
