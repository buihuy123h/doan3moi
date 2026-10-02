namespace doanki3API.DTOs.Inventory;

public sealed record InventoryResponse(
    string InventoryId,
    string ItemCode,
    string Name,
    string Category,
    int StockQuantity,
    int ReorderLevel,
    decimal UnitCost,
    decimal TotalStockValue,
    bool IsLowStock,
    string? Location,
    string? VendorId,
    string? VendorName,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record InventoryMutationResponse(
    string Message,
    string InventoryId,
    int StockQuantity,
    int ReorderLevel,
    bool IsLowStock,
    string Status);

public sealed record AdjustInventoryStockResponse(
    string Message,
    string InventoryId,
    int QuantityChange,
    int StockQuantity,
    string Status);
