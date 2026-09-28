using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Services;

public sealed class TechnicalDashboardService
{
    private readonly MyAppDbContext _context;

    public TechnicalDashboardService(MyAppDbContext context)
    {
        _context = context;
    }

    public async Task<TechnicalDashboardResponse> GetAsync(
        CancellationToken cancellationToken)
    {
        var pendingOrders = await _context.Orders.CountAsync(
            o => o.Status == OrderStatuses.Pending, cancellationToken);
        var feasibleOrders = await _context.Orders.CountAsync(
            o => o.Status == OrderStatuses.Feasible, cancellationToken);
        var activeConnections = await _context.CustomerConnections.CountAsync(
            c => c.Status == ConnectionStatuses.Active, cancellationToken);
        var inactiveConnections = await _context.CustomerConnections.CountAsync(
            c => c.Status != ConnectionStatuses.Active, cancellationToken);
        var availableEquipment = await _context.Equipment.CountAsync(
            e => e.Status == EquipmentStatuses.InStock &&
                 e.AssignedAccountId == null &&
                 e.CustomerConnection == null,
            cancellationToken);
        var lowStockItems = await _context.InventoryItems.CountAsync(
            i => i.StockQuantity <= i.ReorderLevel, cancellationToken);

        return new TechnicalDashboardResponse(
            pendingOrders,
            feasibleOrders,
            activeConnections,
            inactiveConnections,
            availableEquipment,
            lowStockItems);
    }
}
