using System;
using System.Collections.Generic;

namespace doanki3API.Models;

public partial class VwInventoryStock
{
    public string InventoryId { get; set; } = null!;

    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string Category { get; set; } = null!;

    public int StockQuantity { get; set; }

    public int ReorderLevel { get; set; }

    public decimal UnitCost { get; set; }

    public decimal? TotalStockValue { get; set; }

    public string? SupplierName { get; set; }

    public string? Location { get; set; }

    public int IsLowStock { get; set; }
}
