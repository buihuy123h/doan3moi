using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class InventoryItem
{
    public string InventoryId { get; set; } = null!;

    public string ItemCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public int StockQuantity { get; set; }

    public int ReorderLevel { get; set; }

    public decimal UnitCost { get; set; }

    public string? Location { get; set; }

    public string? VendorId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();

    public virtual Vendor? Vendor { get; set; }
}
