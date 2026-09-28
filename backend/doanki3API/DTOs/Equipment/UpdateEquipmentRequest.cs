using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Equipment;

public sealed class UpdateEquipmentRequest
{
    [StringLength(50)]
    public string? SerialNumber { get; set; }

    [StringLength(50)]
    [RegularExpression("^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$")]
    public string? MacAddress { get; set; }

    [StringLength(150)]
    public string? DeviceModel { get; set; }

    [StringLength(50)]
    public string? DeviceType { get; set; }

    [StringLength(20)]
    public string? StoreId { get; set; }

    [StringLength(30)]
    public string? Status { get; set; }

    [StringLength(50)]
    public string? FirmwareVersion { get; set; }

    [StringLength(20)]
    public string? AssignedAccountId { get; set; }

    [StringLength(20)]
    public string? AssignedTechnicianId { get; set; }

    public DateTime? InstalledDate { get; set; }
}
