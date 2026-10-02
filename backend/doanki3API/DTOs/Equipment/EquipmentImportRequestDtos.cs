using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Equipment;

public sealed class CreateEquipmentImportRequest
{
    [StringLength(11)]
    public string? OrderId { get; set; }

    [Required, StringLength(20)]
    public string StoreId { get; set; } = null!;

    [StringLength(30)]
    public string? InventoryId { get; set; }

    [Required, StringLength(150)]
    public string ItemName { get; set; } = null!;

    [Required, StringLength(50)]
    public string DeviceType { get; set; } = null!;

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [StringLength(20)]
    public string Urgency { get; set; } = "Normal";

    [StringLength(500)]
    public string? Reason { get; set; }
}

public sealed class UpdateEquipmentImportRequestStatus
{
    [Required, StringLength(30)]
    public string Status { get; set; } = null!; // Approved | Fulfilled | Rejected

    [StringLength(500)]
    public string? AdminNotes { get; set; }
}

public sealed record EquipmentImportRequestResponse(
    string RequestId,
    string? OrderId,
    string StoreId,
    string? StoreName,
    string EmployeeId,
    string? EmployeeName,
    string? InventoryId,
    string ItemName,
    string DeviceType,
    int Quantity,
    string Urgency,
    string Status,
    string? Reason,
    string? AdminNotes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
