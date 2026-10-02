using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Connections;

public sealed record ConnectionListItemResponse(
    string AccountId,
    string OrderId,
    string CustomerId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string InstallationAddress,
    string PlanName,
    string ConnectionType,
    decimal MonthlyRental,
    decimal SecurityDeposit,
    string? EquipmentId,
    string? EquipmentSerialNumber,
    string? EquipmentModel,
    string Status,
    string? IpAddress,
    string? PortNumber,
    DateTime InstalledDate,
    DateTime? SuspensionStartDate,
    DateTime? SuspensionEndDate,
    DateTime? TerminatedDate,
    DateTime LastUpdated,
    string? LastStatusReason);

public sealed class CreateOrUpdateConnectionRequest
{
    [Required, StringLength(20)]
    public string AccountId { get; set; } = null!;

    [StringLength(11)]
    public string? OrderId { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? InstallationAddress { get; set; }
    public string? ConnectionType { get; set; }
    public string? PlanName { get; set; }
    public decimal? MonthlyRental { get; set; }
    public decimal? SecurityDeposit { get; set; }

    [StringLength(30)]
    public string? Status { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(50)]
    public string? PortNumber { get; set; }

    [StringLength(20)]
    public string? EquipmentId { get; set; }

    [StringLength(50)]
    public string? AssignedDeviceSerial { get; set; }

    public string? AssignedDeviceModel { get; set; }
    public DateTime? InstalledDate { get; set; }

    [StringLength(500)]
    public string? LastStatusReason { get; set; }
}

public sealed record ConnectionDetailResponse(
    string AccountId,
    string OrderId,
    string CustomerId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string InstallationAddress,
    string PlanName,
    string ConnectionType,
    decimal MonthlyRental,
    decimal SecurityDeposit,
    string? EquipmentId,
    string? EquipmentSerialNumber,
    string? EquipmentModel,
    string Status,
    string? IpAddress,
    string? PortNumber,
    DateTime InstalledDate,
    DateTime? SuspensionStartDate,
    DateTime? SuspensionEndDate,
    DateTime? TerminatedDate,
    DateTime LastUpdated,
    string? LastStatusReason);

public sealed record ProvideConnectionResponse(
    string Message,
    string OrderId,
    string AccountId,
    string ConnectionStatus,
    string? EquipmentId,
    DateTime InstalledDate,
    int ProvisionedConnections,
    int RequiredConnections,
    string OrderStatus);

public sealed record UpdateConnectionStatusResponse(
    string Message,
    string AccountId,
    string OldStatus,
    string Status);

public sealed record ConnectionActivityLogResponse(
    long LogId,
    string ActionType,
    string? OldValue,
    string? NewValue,
    string? Reason,
    string? PerformedBy,
    string? PerformedByName,
    DateTime Timestamp);

