using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Connections;

public sealed class UpdateConnectionStatusRequest
{
    [Required, StringLength(30)]
    public string Status { get; set; } = null!;

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(20)]
    public string? PerformedBy { get; set; }
}
