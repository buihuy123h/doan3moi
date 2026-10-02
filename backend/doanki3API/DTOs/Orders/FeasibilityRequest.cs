using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Orders;

public sealed class FeasibilityRequest
{
    public bool IsFeasible { get; set; }

    // Dial-Up requires both legs. These fields are ignored for other plan types.
    public bool? LandlineFeasible { get; set; }

    public bool? InternetFeasible { get; set; }

    [Range(0, int.MaxValue)]
    public int? CableDistanceMeters { get; set; }

    [StringLength(100)]
    public string? DpBoxCapacity { get; set; }

    [Range(typeof(decimal), "-999.99", "999.99")]
    public decimal? SignalLossDbm { get; set; }

    [Required, StringLength(20)]
    public string CheckedBy { get; set; } = null!;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }
}
