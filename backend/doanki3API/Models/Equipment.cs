using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class Equipment
{
    public string EquipmentId { get; set; } = null!;

    public string? InventoryId { get; set; }

    public string? SerialNumber { get; set; }

    public string? MacAddress { get; set; }

    public string DeviceModel { get; set; } = null!;

    public string DeviceType { get; set; } = null!;

    public string VendorId { get; set; } = null!;

    public string? StoreId { get; set; }

    public string Status { get; set; } = null!;

    public string? FirmwareVersion { get; set; }

    public string? AssignedAccountId { get; set; }

    public string? AssignedTechnicianId { get; set; }

    public DateTime? InstalledDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee? AssignedTechnician { get; set; }

    public virtual CustomerConnection? CustomerConnection { get; set; }

    public virtual InventoryItem? Inventory { get; set; }

    public virtual RetailStore? Store { get; set; }

    public virtual Vendor Vendor { get; set; } = null!;
}
