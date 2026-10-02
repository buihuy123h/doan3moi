using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class ServicePlan
{
    public string PlanId { get; set; } = null!;

    public string? PlanCode { get; set; }

    public string Name { get; set; } = null!;

    public string ConnectionType { get; set; } = null!;

    public string SpeedOrBandwidth { get; set; } = null!;

    public decimal MonthlyRental { get; set; }

    public decimal HourlyCharge { get; set; }

    public decimal SecurityDeposit { get; set; }

    public string? DataLimit { get; set; }

    public string BillingCycle { get; set; } = null!;

    public string? Validity { get; set; }

    public int IncludedHours { get; set; }

    public string? CallRates { get; set; }

    public string? Description { get; set; }

    public bool IsPopular { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
