using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Connections;

public sealed class ProvideConnectionRequest
{
    [Required, StringLength(20)]
    public string EquipmentId { get; set; } = null!;

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(50)]
    public string? PortNumber { get; set; }

    public DateTime? InstalledDate { get; set; }

    [StringLength(20)]
    public string? PerformedBy { get; set; }
}
