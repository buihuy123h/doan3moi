using System.Data;
using doanki3API.Data;
using doanki3API.Constants;
using doanki3API.DTOs.Inventory;
using doanki3API.Models;
using doanki3API.Services.Common;
using doanki3API.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Services;

public sealed class InventoryService
{
    private readonly MyAppDbContext _context;
    private readonly ILogger<InventoryService> _logger;
    private readonly NotificationService _notifications;

    public InventoryService(
        MyAppDbContext context,
        ILogger<InventoryService> logger,
        NotificationService notifications)
    {
        _context = context;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<InventoryResponse>> GetInventoryAsync(
        string? category,
        bool lowStockOnly,
        CancellationToken cancellationToken)
    {
        var query = _context.InventoryItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(item => item.Category == category.Trim());

        if (lowStockOnly)
            query = query.Where(item => item.StockQuantity <= item.ReorderLevel);

        return await Project(query.OrderBy(item => item.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<InventoryResponse?> GetInventoryByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return Project(_context.InventoryItems.AsNoTracking().Where(item => item.InventoryId == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<InventoryMutationResponse>> UpdateInventoryAsync(
        string id,
        UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(inventory => inventory.InventoryId == id, cancellationToken);

        if (item is null)
        {
            return ServiceResult<InventoryMutationResponse>.NotFound(
                "Không tìm thấy vật tư/tồn kho");
        }

        if (request.StockQuantity < 0 || request.ReorderLevel < 0 || request.UnitCost < 0)
        {
            return ServiceResult<InventoryMutationResponse>.Validation(
                "Số lượng, mức đặt lại và đơn giá không được âm");
        }

        var vendorId = Normalize(request.VendorId);
        if (vendorId is not null &&
            !await _context.Vendors.AnyAsync(
                vendor => vendor.VendorId == vendorId,
                cancellationToken))
        {
            return ServiceResult<InventoryMutationResponse>.Validation(
                "Nhà cung cấp không tồn tại");
        }

        item.StockQuantity = request.StockQuantity ?? item.StockQuantity;
        item.ReorderLevel = request.ReorderLevel ?? item.ReorderLevel;
        item.UnitCost = request.UnitCost ?? item.UnitCost;
        item.Location = Normalize(request.Location) ?? item.Location;
        item.VendorId = vendorId ?? item.VendorId;
        item.Status = StatusFor(item.StockQuantity, item.ReorderLevel);
        item.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        await WarnIfLowStockAsync(item, cancellationToken);

        return ServiceResult<InventoryMutationResponse>.Success(
            new InventoryMutationResponse(
                "Cập nhật tồn kho thành công",
                item.InventoryId,
                item.StockQuantity,
                item.ReorderLevel,
                item.StockQuantity <= item.ReorderLevel,
                item.Status));
    }

    public async Task<ServiceResult<AdjustInventoryStockResponse>> AdjustStockAsync(
        string id,
        AdjustInventoryStockRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(inventory => inventory.InventoryId == id, cancellationToken);

        if (item is null)
        {
            return ServiceResult<AdjustInventoryStockResponse>.NotFound(
                "Không tìm thấy vật tư/tồn kho");
        }

        if (request.QuantityChange == 0)
        {
            return ServiceResult<AdjustInventoryStockResponse>.Validation(
                "Số lượng điều chỉnh phải khác 0");
        }

        var newQuantity = (long)item.StockQuantity + request.QuantityChange;
        if (newQuantity < 0)
        {
            return ServiceResult<AdjustInventoryStockResponse>.Validation(
                "Số lượng xuất vượt quá tồn kho hiện tại");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ServiceResult<AdjustInventoryStockResponse>.Validation(
                "Phải nhập lý do điều chỉnh tồn kho");
        }

        if (newQuantity > int.MaxValue)
        {
            return ServiceResult<AdjustInventoryStockResponse>.Validation(
                "Số lượng tồn kho vượt quá giới hạn cho phép");
        }

        var oldQuantity = item.StockQuantity;
        item.StockQuantity = (int)newQuantity;
        item.Status = StatusFor(item.StockQuantity, item.ReorderLevel);
        item.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Inventory {InventoryId} adjusted from {OldQuantity} to {NewQuantity}. Reason: {Reason}",
            item.InventoryId,
            oldQuantity,
            newQuantity,
            Normalize(request.Reason) ?? "Not provided");

        await WarnIfLowStockAsync(item, cancellationToken);

        return ServiceResult<AdjustInventoryStockResponse>.Success(
            new AdjustInventoryStockResponse(
                "Điều chỉnh tồn kho thành công",
                item.InventoryId,
                request.QuantityChange,
                item.StockQuantity,
                item.Status));
    }

    /// <summary>
    /// Tồn kho vừa chạm ngưỡng đặt lại thì báo ngay cho Quản trị.
    /// ReplaceKey bên trong LowStockAsync bảo đảm mỗi vật tư chỉ có một thông báo,
    /// nhập xuất liên tục cũng không làm ngập chuông thông báo.
    /// </summary>
    private async Task WarnIfLowStockAsync(InventoryItem item, CancellationToken cancellationToken)
    {
        if (item.StockQuantity > item.ReorderLevel) return;

        await _notifications.LowStockAsync(
            item.InventoryId,
            item.Name,
            item.ItemCode,
            item.StockQuantity,
            item.ReorderLevel,
            cancellationToken);

        await _notifications.NotifyDataChangedAsync("inventory", item.InventoryId, cancellationToken);
    }

    private static IQueryable<InventoryResponse> Project(IQueryable<InventoryItem> query)
    {
        return query.Select(item => new InventoryResponse(
            item.InventoryId,
            item.ItemCode,
            item.Name,
            item.Category,
            item.StockQuantity,
            item.ReorderLevel,
            item.UnitCost,
            item.StockQuantity * item.UnitCost,
            item.StockQuantity <= item.ReorderLevel,
            item.Location,
            item.VendorId,
            item.Vendor == null ? null : item.Vendor.CompanyName,
            item.Status,
            item.CreatedAt,
            item.UpdatedAt));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string StatusFor(int stockQuantity, int reorderLevel) =>
        stockQuantity == 0
            ? InventoryStatuses.OutOfStock
            : stockQuantity <= reorderLevel
                ? InventoryStatuses.LowStock
                : InventoryStatuses.InStock;
}
