using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Orders;

public sealed class UpdateInstallationScheduleRequest
{
    [Required]
    public DateTime ScheduledInstallDate { get; set; }

    [StringLength(20)]
    public string? TechnicianId { get; set; }
}
