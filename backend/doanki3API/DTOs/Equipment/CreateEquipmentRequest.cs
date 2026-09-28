using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Equipment;

public sealed class CreateEquipmentRequest
{
    [StringLength(20)]
    public string? EquipmentId { get; set; }

    [StringLength(30)]
    public string? InventoryId { get; set; }

    [Required, StringLength(50)]
    public string SerialNumber { get; set; } = null!;

    [Required, StringLength(50)]
    [RegularExpression("^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$")]
    public string MacAddress { get; set; } = null!;

    [Required, StringLength(150)]
    public string DeviceModel { get; set; } = null!;

    [Required, StringLength(50)]
    public string DeviceType { get; set; } = null!;

    [StringLength(20)]
    public string? VendorId { get; set; }

    [StringLength(20)]
    public string? StoreId { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = "In Stock";

    [StringLength(50)]
    public string? FirmwareVersion { get; set; }
}
