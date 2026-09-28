using doanki3API.Constants;
using doanki3API.DTOs.Inventory;
using doanki3API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Technical;

[Route("api/Technical")]
public sealed class InventoryController : TechnicalApiControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory(
        [FromQuery] string? category,
        [FromQuery] bool? lowStockOnly,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetInventoryAsync(
            category,
            lowStockOnly == true,
            cancellationToken);

        return Ok(inventory);
    }

    [HttpGet("inventory/{id}")]
    public async Task<IActionResult> GetInventoryById(
        string id,
        CancellationToken cancellationToken)
    {
        var item = await _inventoryService.GetInventoryByIdAsync(id, cancellationToken);
        return item is null
            ? NotFound(new { message = "Không tìm thấy vật tư/tồn kho" })
            : Ok(item);
    }

    [HttpPut("inventory/{id}")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> UpdateInventory(
        string id,
        [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.UpdateInventoryAsync(
            id,
            request,
            cancellationToken);

        return FromServiceResult(result);
    }

    [HttpPost("inventory/{id}/adjust-stock")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> AdjustInventoryStock(
        string id,
        [FromBody] AdjustInventoryStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AdjustStockAsync(
            id,
            request,
            cancellationToken);

        return FromServiceResult(result);
    }
}
