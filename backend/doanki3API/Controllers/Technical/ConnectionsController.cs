using doanki3API.Constants;
using doanki3API.DTOs.Connections;
using doanki3API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Technical;

[Route("api/Technical")]
public sealed class ConnectionsController : TechnicalApiControllerBase
{
    private readonly ConnectionService _connectionService;

    public ConnectionsController(ConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    [HttpPost("orders/{id}/provide-connection")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> ProvideConnection(
        string id,
        [FromBody] ProvideConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _connectionService.ProvideConnectionAsync(
            id,
            request,
            cancellationToken);

        return FromServiceResult(result);
    }

    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections(
        [FromQuery] string? accountId,
        [FromQuery] string? status,
        [FromQuery] string? equipmentId,
        [FromQuery] string? customerName,
        [FromQuery] string? phone,
        [FromQuery] string? connectionType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var connections = await _connectionService.GetConnectionsAsync(
            accountId,
            status,
            equipmentId,
            customerName,
            phone,
            connectionType,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(connections);
    }

    [HttpPost("connections")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> CreateOrUpdateConnection(
        [FromBody] CreateOrUpdateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _connectionService.CreateOrUpdateConnectionAsync(request, cancellationToken);
        return FromServiceResult(result);
    }

    // Giữ route cũ để tương thích với frontend hiện tại.
    [HttpGet("connections/search")]
    public Task<IActionResult> SearchConnections(
        [FromQuery] string? accountId,
        [FromQuery] string? status,
        [FromQuery] string? equipmentId,
        [FromQuery] string? customerName,
        [FromQuery] string? phone,
        [FromQuery] string? connectionType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken) =>
        GetConnections(accountId, status, equipmentId, customerName, phone, connectionType, fromDate, toDate, cancellationToken);

    [HttpGet("connections/{accountId}")]
    public async Task<IActionResult> GetConnectionById(
        string accountId,
        CancellationToken cancellationToken)
    {
        var connection = await _connectionService.GetConnectionByIdAsync(
            accountId,
            cancellationToken);

        return connection is null
            ? NotFound(new { message = "Không tìm thấy kết nối" })
            : Ok(connection);
    }

    [HttpPut("connections/{accountId}/status")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> UpdateConnectionStatus(
        string accountId,
        [FromBody] UpdateConnectionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _connectionService.UpdateStatusAsync(
            accountId,
            request,
            cancellationToken);

        return FromServiceResult(result);
    }


}

