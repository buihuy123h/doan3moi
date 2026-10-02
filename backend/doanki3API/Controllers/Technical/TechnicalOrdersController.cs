using doanki3API.Constants;
using doanki3API.DTOs.Orders;
using doanki3API.Services;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Technical;

[Route("api/Technical")]
public sealed class TechnicalOrdersController : TechnicalApiControllerBase
{
    private readonly TechnicalOrderService _orderService;

    public TechnicalOrdersController(TechnicalOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? storeId,
        [FromQuery] string? keyword,
        [FromQuery] string? customerName,
        [FromQuery] string? phone,
        [FromQuery] string? connectionType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersAsync(
            status,
            storeId,
            keyword,
            customerName,
            phone,
            connectionType,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrderById(
        string id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        return order is null
            ? NotFound(new { message = "Không tìm thấy đơn hàng" })
            : Ok(order);
    }

    [HttpPut("orders/{id}/feasibility")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> UpdateFeasibility(
        string id,
        [FromBody] FeasibilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateFeasibilityAsync(
            id,
            request,
            cancellationToken);

        return FromServiceResult(result);
    }

    [HttpPut("orders/{id}/installation-schedule")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> UpdateInstallationSchedule(
        string id,
        [FromBody] UpdateInstallationScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.UpdateScheduleAsync(
            id,
            request,
            cancellationToken);

        return FromServiceResult(result);
    }

    [HttpPost("orders")]
    [Authorize(Roles = EmployeeRoles.PlaceOrder)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var validationError = TechnicalOrderService.ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var order = await _orderService.CreateOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
    }

    [HttpPut("orders/{id}/status")]
    [Authorize(Roles = EmployeeRoles.UpdateOrderStatus)]
    public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, request, cancellationToken);
        return FromServiceResult(result);
    }
}
