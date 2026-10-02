using System;

namespace doanki3API.Models;

/// <summary>
/// Yêu cầu nhập thiết bị / vật tư từ Kỹ thuật viên gửi Admin:
/// Kỹ thuật viên khi hết thiết bị để cấp kết nối (hoặc cần bổ sung thiết bị chi nhánh)
/// sẽ gửi yêu cầu nhập hàng. Admin xem danh sách mặt hàng cần nhập và tiến hành nhập hàng.
/// </summary>
public partial class EquipmentRequest
{
    public string RequestId { get; set; } = null!;

    public string? OrderId { get; set; }

    public string StoreId { get; set; } = null!;

    public string EmployeeId { get; set; } = null!;

    public string? InventoryId { get; set; }

    public string ItemName { get; set; } = null!;

    public string DeviceType { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    /// <summary>Low | Normal | High | Urgent</summary>
    public string Urgency { get; set; } = "Normal";

    /// <summary>Pending | Approved | Fulfilled | Rejected</summary>
    public string Status { get; set; } = "Pending";

    public string? Reason { get; set; }

    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual RetailStore Store { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;

    public virtual InventoryItem? Inventory { get; set; }
}
