using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Order
{
    public string OrderId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string PlanId { get; set; } = null!;

    public string StoreId { get; set; } = null!;

    public string? EmployeeId { get; set; }

    public string InstallationAddress { get; set; } = null!;

    public DateTime ApplicationDate { get; set; }

    public string Status { get; set; } = null!;

    public int? CableDistanceMeters { get; set; }

    public string? DpBoxCapacity { get; set; }

    public decimal? SignalLossDbm { get; set; }

    public string? FeasibilityNotes { get; set; }

    public string? FeasibilityCheckedBy { get; set; }

    public DateTime? FeasibilityCheckedDate { get; set; }

    public DateTime? ScheduledInstallDate { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>Nhân viên cửa hàng bán lẻ đã duyệt hồ sơ (khâu 1 của quy trình).</summary>
    public string? RetailApprovedBy { get; set; }

    public DateTime? RetailApprovedDate { get; set; }

    public string? RetailApprovalNotes { get; set; }

    /// <summary>Tên kỹ thuật viên được cử đi khảo sát — EmployeeId chỉ giữ mã.</summary>
    public string? AssignedTechnicianName { get; set; }

    public string? AssignedTechnicianPhone { get; set; }

    public DateTime? AssignedTechnicianDate { get; set; }

    public int BulkConnectionsCount { get; set; }

    public decimal BulkDiscountPercent { get; set; }

    public string? ExistingLandlineAccountId { get; set; }

    public bool? LandlineFeasible { get; set; }

    public bool? InternetFeasible { get; set; }

    public string? AssignedAccountId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<CustomerConnection> CustomerConnections { get; set; } = new List<CustomerConnection>();

    public virtual Employee? Employee { get; set; }

    public virtual Employee? FeasibilityCheckedByNavigation { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ServicePlan Plan { get; set; } = null!;

    public virtual RetailStore Store { get; set; } = null!;
}
