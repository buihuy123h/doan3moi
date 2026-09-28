using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace doanki3API.DTOs.Admin
{
    /// <summary>
    /// DTO đại diện cho một danh mục vật tư / thiết bị trong kho (Inventory Item)
    /// Chuẩn hóa tên trường tương ứng 100% với giao diện Frontend (Admin Tab Stock / Inventory)
    /// </summary>
    public class InventoryItemResponseDto
    {
        // Khóa định danh chính
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("inventoryId")]
        public string InventoryId { get; set; } = null!;

        // 1. Cột ITEM CODE / Mã VT
        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; } = null!;

        // 2. Cột DEVICE / EQUIPMENT NAME / Tên thiết bị / Vật tư
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = null!;

        // 3. Cột CATEGORY / Danh mục
        [JsonPropertyName("category")]
        public string Category { get; set; } = null!;

        // 4. Cột DEPOT LOCATION / Vị trí kho
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("depotLocation")]
        public string? DepotLocation { get; set; }

        // 5. Cột STOCK UNITS / Số lượng tồn
        [JsonPropertyName("stockQuantity")]
        public int StockQuantity { get; set; }

        [JsonPropertyName("stockUnits")]
        public int StockUnits { get; set; }

        // 6. Cột REORDER THRESHOLD / Ngưỡng đặt lại
        [JsonPropertyName("reorderLevel")]
        public int ReorderLevel { get; set; }

        [JsonPropertyName("reorderThreshold")]
        public int ReorderThreshold { get; set; }

        // 7. Cột UNIT COST / Đơn giá ($)
        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        // 8. Cột SUPPLIER / Nhà cung cấp
        [JsonPropertyName("supplier")]
        public string Supplier { get; set; } = "Nexus Central Supply";

        [JsonPropertyName("supplierName")]
        public string SupplierName { get; set; } = "Nexus Central Supply";

        [JsonPropertyName("vendorId")]
        public string? VendorId { get; set; }

        // Các trường tính toán và trạng thái bổ trợ
        [JsonPropertyName("isLowStock")]
        public bool IsLowStock { get; set; }

        [JsonPropertyName("totalValue")]
        public decimal TotalValue { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "In Stock";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO chứa dữ liệu tính toán tổng hợp cho 3 ô thống kê phía trên bảng:
    /// - Ô 1: Tổng thiết bị tồn kho (Total Equipment Stock)
    /// - Ô 2: Cảnh báo sắp hết hàng (Low Stock Alerts)
    /// - Ô 3: Ước tính giá trị kho (Valuation Est.)
    /// </summary>
    public class InventoryStatsDto
    {
        // Ô 1: Tổng thiết bị tồn kho (Sum of StockQuantity)
        [JsonPropertyName("totalStockUnits")]
        public int TotalStockUnits { get; set; }

        // Ô 2: Số mặt hàng dưới ngưỡng tối thiểu (Count of items with StockQuantity <= ReorderLevel)
        [JsonPropertyName("lowStockCount")]
        public int LowStockCount { get; set; }

        // Ô 3: Tổng giá trị ước tính toàn bộ kho (Sum of StockQuantity * UnitCost)
        [JsonPropertyName("totalValuation")]
        public decimal TotalValuation { get; set; }

        // Tổng số loại danh mục vật tư trong kho (Count of distinct SKU items)
        [JsonPropertyName("totalCategoriesCount")]
        public int TotalCategoriesCount { get; set; }

        // Danh sách các mặt hàng đang bị cảnh báo sắp hết hàng
        [JsonPropertyName("lowStockItems")]
        public List<InventoryItemResponseDto> LowStockItems { get; set; } = new();
    }

    /// <summary>
    /// DTO tổng hợp trả về cả Stats 3 ô và danh sách Items cho 1 lần gọi duy nhất nếu cần
    /// </summary>
    public class InventoryOverviewResponseDto
    {
        [JsonPropertyName("stats")]
        public InventoryStatsDto Stats { get; set; } = new();

        [JsonPropertyName("items")]
        public List<InventoryItemResponseDto> Items { get; set; } = new();
    }
}
