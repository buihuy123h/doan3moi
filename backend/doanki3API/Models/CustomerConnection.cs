using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class CustomerConnection
{
    public string AccountId { get; set; } = null!;

    public string OrderId { get; set; } = null!;

    public string? EquipmentId { get; set; }

    public string Status { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? PortNumber { get; set; }

    public DateTime InstalledDate { get; set; }

    public DateTime? SuspensionStartDate { get; set; }

    public DateTime? SuspensionEndDate { get; set; }

    public DateTime? TerminatedDate { get; set; }

    public DateTime LastUpdated { get; set; }

    public string? LastStatusReason { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();



    public virtual Equipment? Equipment { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual Order Order { get; set; } = null!;
}
