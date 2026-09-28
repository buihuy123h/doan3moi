namespace doanki3API.DTOs.Equipment;

public sealed record EquipmentResponse(
    string EquipmentId,
    string? InventoryId,
    string? SerialNumber,
    string? MacAddress,
    string DeviceModel,
    string DeviceType,
    string VendorId,
    string VendorName,
    string? StoreId,
    string? StoreName,
    string Status,
    string? FirmwareVersion,
    string? AssignedAccountId,
    string? AssignedTechnicianId,
    string? TechnicianName,
    DateTime? InstalledDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record EquipmentMutationResponse(
    string Message,
    string EquipmentId,
    string Status);
