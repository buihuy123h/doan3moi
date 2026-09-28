using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Inventory;

public sealed class AdjustInventoryStockRequest
{
    [Range(-1_000_000, 1_000_000)]
    public int QuantityChange { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = null!;
}
