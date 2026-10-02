using System.Security.Claims;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Equipment;
using doanki3API.Models;
using doanki3API.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Controllers.Technical;

[ApiController]
[Route("api/technical/equipment-requests")]
[Route("api/admin/equipment-requests")]
[Authorize]
public sealed class EquipmentRequestsController : ControllerBase
{
    private readonly MyAppDbContext _context;
    private readonly NotificationService _notifications;

    public EquipmentRequestsController(MyAppDbContext context, NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    // GET /api/technical/equipment-requests?status=&storeId=&search=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EquipmentImportRequestResponse>>> GetRequests(
        [FromQuery] string? status,
        [FromQuery] string? storeId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.EquipmentRequests
            .Include(r => r.Store)
            .Include(r => r.Employee)
            .Include(r => r.Inventory)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            query = query.Where(r => r.Status == status.Trim());

        if (!string.IsNullOrWhiteSpace(storeId))
            query = query.Where(r => r.StoreId == storeId.Trim());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(r =>
                r.RequestId.ToLower().Contains(keyword) ||
                r.ItemName.ToLower().Contains(keyword) ||
                (r.OrderId != null && r.OrderId.ToLower().Contains(keyword)) ||
                (r.Reason != null && r.Reason.ToLower().Contains(keyword)) ||
                r.Store.Name.ToLower().Contains(keyword) ||
                r.Employee.FullName.ToLower().Contains(keyword));
        }

        var list = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new EquipmentImportRequestResponse(
                r.RequestId,
                r.OrderId,
                r.StoreId,
                r.Store.Name,
                r.EmployeeId,
                r.Employee.FullName,
                r.InventoryId,
                r.ItemName,
                r.DeviceType,
                r.Quantity,
                r.Urgency,
                r.Status,
                r.Reason,
                r.AdminNotes,
                r.CreatedAt,
                r.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    // GET /api/technical/equipment-requests/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentImportRequestResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var r = await _context.EquipmentRequests
            .Include(x => x.Store)
            .Include(x => x.Employee)
            .Include(x => x.Inventory)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RequestId == id, cancellationToken);

        if (r is null)
            return NotFound(new { message = $"Không tìm thấy yêu cầu nhập hàng: {id}" });

        return Ok(new EquipmentImportRequestResponse(
            r.RequestId,
            r.OrderId,
            r.StoreId,
            r.Store.Name,
            r.EmployeeId,
            r.Employee.FullName,
            r.InventoryId,
            r.ItemName,
            r.DeviceType,
            r.Quantity,
            r.Urgency,
            r.Status,
            r.Reason,
            r.AdminNotes,
            r.CreatedAt,
            r.UpdatedAt
        ));
    }

    // POST /api/technical/equipment-requests
    [HttpPost]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<ActionResult<EquipmentImportRequestResponse>> CreateRequest(
        [FromBody] CreateEquipmentImportRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var store = await _context.RetailStores.FirstOrDefaultAsync(s => s.StoreId == request.StoreId, cancellationToken);
        if (store is null)
            return BadRequest(new { message = $"Chi nhánh không tồn tại: {request.StoreId}" });

        var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return Unauthorized(new { message = "Không xác định được nhân viên thực hiện." });
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(
            e => e.EmployeeId == employeeId &&
                 e.Status == EmployeeStatuses.Active &&
                 (e.Role == EmployeeRoles.FieldEngineer || e.Role == EmployeeRoles.Manager),
            cancellationToken);
        if (employee is null)
        {
            return BadRequest(new { message = "Nhân viên thực hiện không tồn tại, đã ngừng hoạt động hoặc không có quyền kỹ thuật." });
        }

        var requestId = $"REQ-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var entity = new EquipmentRequest
        {
            RequestId = requestId,
            OrderId = string.IsNullOrWhiteSpace(request.OrderId) ? null : request.OrderId.Trim(),
            StoreId = request.StoreId.Trim(),
            EmployeeId = employeeId,
            InventoryId = string.IsNullOrWhiteSpace(request.InventoryId) ? null : request.InventoryId.Trim(),
            ItemName = request.ItemName.Trim(),
            DeviceType = request.DeviceType.Trim(),
            Quantity = Math.Max(1, request.Quantity),
            Urgency = string.IsNullOrWhiteSpace(request.Urgency) ? "Normal" : request.Urgency.Trim(),
            Status = "Pending",
            Reason = request.Reason?.Trim(),
            CreatedAt = DateTime.Now
        };

        _context.EquipmentRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Notify Admin of new request
        await _notifications.NotifyDataChangedAsync("equipmentRequests", entity.RequestId, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.RequestId }, new EquipmentImportRequestResponse(
            entity.RequestId,
            entity.OrderId,
            entity.StoreId,
            store.Name,
            entity.EmployeeId,
            employee.FullName,
            entity.InventoryId,
            entity.ItemName,
            entity.DeviceType,
            entity.Quantity,
            entity.Urgency,
            entity.Status,
            entity.Reason,
            entity.AdminNotes,
            entity.CreatedAt,
            entity.UpdatedAt
        ));
    }

    // PUT /api/technical/equipment-requests/{id}/status
    [HttpPut("{id}/status")]
    [Authorize(Roles = EmployeeRoles.Manager)]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateEquipmentImportRequestStatus request,
        CancellationToken cancellationToken)
    {
        var status = request.Status?.Trim();
        if (status is not ("Approved" or "Fulfilled" or "Rejected" or "Pending"))
        {
            return BadRequest(new { message = "Trạng thái chỉ nhận: Approved, Fulfilled, Rejected, Pending." });
        }

        var entity = await _context.EquipmentRequests
            .Include(r => r.Store)
            .Include(r => r.Employee)
            .Include(r => r.Inventory)
            .FirstOrDefaultAsync(r => r.RequestId == id, cancellationToken);

        if (entity is null)
            return NotFound(new { message = $"Không tìm thấy yêu cầu nhập hàng: {id}" });

        if (entity.Status == "Fulfilled" && status != "Fulfilled")
            return Conflict(new { message = "Yêu cầu đã được nhập hàng hoàn tất, không thể thay đổi." });

        entity.Status = status;
        if (!string.IsNullOrWhiteSpace(request.AdminNotes))
            entity.AdminNotes = request.AdminNotes.Trim();
        entity.UpdatedAt = DateTime.Now;

        // If status changed to Fulfilled -> Admin is importing/delivering the stock to the store!
        if (status == "Fulfilled")
        {
            await FulfillEquipmentRequestAsync(entity, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyDataChangedAsync("equipmentRequests", entity.RequestId, cancellationToken);
        await _notifications.NotifyDataChangedAsync("equipment", entity.RequestId, cancellationToken);
        await _notifications.NotifyDataChangedAsync("inventory", entity.RequestId, cancellationToken);

        return Ok(new EquipmentImportRequestResponse(
            entity.RequestId,
            entity.OrderId,
            entity.StoreId,
            entity.Store.Name,
            entity.EmployeeId,
            entity.Employee.FullName,
            entity.InventoryId,
            entity.ItemName,
            entity.DeviceType,
            entity.Quantity,
            entity.Urgency,
            entity.Status,
            entity.Reason,
            entity.AdminNotes,
            entity.CreatedAt,
            entity.UpdatedAt
        ));
    }

    // DELETE /api/technical/equipment-requests/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = EmployeeRoles.TechnicalWrite)]
    public async Task<IActionResult> DeleteRequest(string id, CancellationToken cancellationToken)
    {
        var entity = await _context.EquipmentRequests.FirstOrDefaultAsync(r => r.RequestId == id, cancellationToken);
        if (entity is null)
            return NotFound(new { message = $"Không tìm thấy yêu cầu: {id}" });

        if (entity.Status == "Fulfilled")
            return Conflict(new { message = "Yêu cầu đã được nhập kho hoàn tất, không thể xóa." });

        _context.EquipmentRequests.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyDataChangedAsync("equipmentRequests", id, cancellationToken);

        return Ok(new { message = $"Đã xóa yêu cầu {id} thành công" });
    }

    /// <summary>
    /// Khi Admin bấm nhập hàng (Fulfill): Tự động cộng tồn kho vật tư và tạo các thiết bị
    /// sẵn sàng (In Stock) tại chi nhánh yêu cầu để Kỹ thuật viên có thể cấp kết nối ngay.
    /// </summary>
    private async Task FulfillEquipmentRequestAsync(EquipmentRequest req, CancellationToken cancellationToken)
    {
        // 1. Tăng tồn kho InventoryItem nếu có
        InventoryItem? item = null;
        if (!string.IsNullOrWhiteSpace(req.InventoryId))
        {
            item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.InventoryId == req.InventoryId, cancellationToken);
        }

        if (item is null)
        {
            item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Name == req.ItemName, cancellationToken);
        }

        if (item is not null)
        {
            item.StockQuantity += req.Quantity;
            item.UpdatedAt = DateTime.Now;
        }

        // 2. Tạo thiết bị Equipment "In Stock" tại chi nhánh nhận
        var defaultVendor = await _context.Vendors.FirstOrDefaultAsync(cancellationToken);
        var vendorId = item?.VendorId ?? defaultVendor?.VendorId ?? "vnd-02";

        for (int i = 0; i < req.Quantity; i++)
        {
            var randomMac = string.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}",
                Random.Shared.Next(0x00, 0xFE) & 0xFE, // locally administered
                Random.Shared.Next(0x00, 0xFF),
                Random.Shared.Next(0x00, 0xFF),
                Random.Shared.Next(0x00, 0xFF),
                Random.Shared.Next(0x00, 0xFF),
                Random.Shared.Next(0x00, 0xFF));

            var serialNumber = $"NX-{DateTime.Now:yyMMdd}-{Random.Shared.Next(10000, 99999)}";
            var eq = new Equipment
            {
                EquipmentId = $"eq-{DateTime.Now.Ticks.ToString()[9..]}-{i}",
                InventoryId = item?.InventoryId,
                SerialNumber = serialNumber,
                MacAddress = randomMac,
                DeviceModel = req.ItemName,
                DeviceType = req.DeviceType,
                VendorId = vendorId,
                StoreId = req.StoreId,
                Status = "In Stock",
                FirmwareVersion = "v3.4.1-STABLE",
                CreatedAt = DateTime.Now
            };

            _context.Equipment.Add(eq);
        }
    }
}
