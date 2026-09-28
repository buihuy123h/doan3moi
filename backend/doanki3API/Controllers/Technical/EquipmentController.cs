using doanki3API.Constants;
using doanki3API.DTOs.Equipment;
using doanki3API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Technical;

[Route("api/Technical")]
public sealed class EquipmentController : TechnicalApiControllerBase
{
    private readonly EquipmentService _equipmentService;

    public EquipmentController(EquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    [HttpGet("equipment")]
    public async Task<IActionResult> GetEquipment(
        [FromQuery] string? status,
        [FromQuery] string? storeId,
        [FromQuery] bool? availableOnly,
        CancellationToken cancellationToken)
    {
        var equipment = await _equipmentService.GetEquipmentAsync(
            status,
            storeId,
            availableOnly == true,
            cancellationToken);

        return Ok(equipment);
    }

    [HttpGet("equipment/{id}")]
    public async Task<IActionResult> GetEquipmentById(
        string id,
        CancellationToken cancellationToken)
    {
        var equipment = await _equipmentService.GetEquipmentByIdAsync(id, cancellationToken);
        return equipment is null
            ? NotFound(new { message = "Không tìm thấy thiết bị" })
            : Ok(equipment);
    }

    [HttpPost("equipment")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> CreateEquipment(
        [FromBody] CreateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _equipmentService.CreateEquipmentAsync(
            request,
            cancellationToken);

        if (!result.IsSuccess)
            return FromServiceResult(result);

        return CreatedAtAction(
            nameof(GetEquipmentById),
            new { id = result.Value!.EquipmentId },
            result.Value);
    }

    [HttpPut("equipment/{id}")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> UpdateEquipment(
        string id,
        [FromBody] UpdateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _equipmentService.UpdateEquipmentAsync(
            id,
            request,
            cancellationToken);

        return FromServiceResult(result);
    }

    [HttpDelete("equipment/{id}")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> DeleteEquipment(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _equipmentService.DeleteEquipmentAsync(id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : FromServiceResult(result);
    }
}
