using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Inventory;

public sealed class UpdateInventoryRequest
{
    [Range(0, int.MaxValue)]
    public int? StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int? ReorderLevel { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? UnitCost { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [StringLength(20)]
    public string? VendorId { get; set; }

}
