namespace doanki3API.DTOs.Dashboard;

public sealed record TechnicalDashboardResponse(
    int PendingOrders,
    int FeasibleOrders,
    int ActiveConnections,
    int InactiveConnections,
    int AvailableEquipment,
    int LowStockItems);
