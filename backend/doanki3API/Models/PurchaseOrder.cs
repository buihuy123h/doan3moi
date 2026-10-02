using System;
using System.Collections.Generic;

namespace doanki3API.Models;

/// <summary>
/// Đơn đặt mua thiết bị từ nhà cung cấp — spec: "they even need to track the details
/// of the stock available and as well they check for the requests for connection that
/// they have received, so that they can order for the equipments based on the orders
/// received for the connection" + mục "purchasal lists ... taken care by the Admin".
/// </summary>
public partial class PurchaseOrder
{
    public string PurchaseOrderId { get; set; } = null!;

    public string VendorId { get; set; } = null!;

    /// <summary>Chi nhánh nhận hàng — NULL nghĩa là về kho trung tâm.</summary>
    public string? StoreId { get; set; }

    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string Category { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal TotalCost { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public DateTime? ReceivedDate { get; set; }

    /// <summary>Draft | Submitted | Received | Cancelled.</summary>
    public string Status { get; set; } = "Draft";

    public string? Notes { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Vendor? Vendor { get; set; }

    public virtual RetailStore? Store { get; set; }
}
