using System.Security.Claims;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Admin;
using doanki3API.Models;
using doanki3API.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Controllers.Admin;

/// <summary>
/// Purchase List — đơn đặt mua thiết bị từ vendor (spec: Admin quản lý "purchase
/// list"; công ty "order for the equipments based on the orders received for the
/// connection"). Đơn Received tự cộng tồn kho của chi nhánh nhận hàng: nếu vật tư
/// đã có (cùng ItemCode) thì tăng StockQuantity, chưa có thì tạo dòng kho mới.
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = EmployeeRoles.Manager)]
public class PurchaseOrderController : ControllerBase
{
    private readonly MyAppDbContext _context;
    private readonly NotificationService _notifications;

    public PurchaseOrderController(MyAppDbContext context, NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    // GET /api/admin/purchaseorder?status=&vendorId=&storeId=&search=
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetPurchaseOrders(
        [FromQuery] string? status,
        [FromQuery] string? vendorId,
        [FromQuery] string? storeId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Vendor)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(vendorId))
            query = query.Where(p => p.VendorId == vendorId.Trim());
        if (!string.IsNullOrWhiteSpace(storeId))
            query = query.Where(p => p.StoreId == storeId.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(p =>
                p.PurchaseOrderId.ToLower().Contains(keyword) ||
                p.ItemCode.ToLower().Contains(keyword) ||
                p.ItemName.ToLower().Contains(keyword) ||
                (p.Vendor != null && p.Vendor.CompanyName.ToLower().Contains(keyword)));
        }

        return await query
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrder>> GetPurchaseOrder(string id, CancellationToken cancellationToken)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == id, cancellationToken);

        return order is null
            ? NotFound(new { message = $"Không tìm thấy đơn đặt mua với mã: {id}" })
            : order;
    }

    // POST /api/admin/purchaseorder
    [HttpPost]
    public async Task<ActionResult<PurchaseOrder>> CreatePurchaseOrder(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!await _context.Vendors.AnyAsync(v => v.VendorId == request.VendorId, cancellationToken))
        {
            return BadRequest(new { message = $"Nhà cung cấp không tồn tại: {request.VendorId}" });
        }

        if (!string.IsNullOrWhiteSpace(request.StoreId) &&
            !await _context.RetailStores.AnyAsync(s => s.StoreId == request.StoreId, cancellationToken))
        {
            return BadRequest(new { message = $"Chi nhánh nhận hàng không tồn tại: {request.StoreId}" });
        }

        var order = new PurchaseOrder
        {
            PurchaseOrderId = string.IsNullOrWhiteSpace(request.PurchaseOrderId)
                ? $"PO-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
                : request.PurchaseOrderId.Trim(),
            VendorId = request.VendorId,
            StoreId = string.IsNullOrWhiteSpace(request.StoreId) ? null : request.StoreId.Trim(),
            ItemCode = request.ItemCode.Trim(),
            ItemName = request.ItemName.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Modem" : request.Category.Trim(),
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            TotalCost = request.Quantity * request.UnitCost,
            OrderDate = DateTime.Now,
            ExpectedDate = request.ExpectedDate,
            Status = "Submitted",
            Notes = request.Notes,
            CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "Manager",
            CreatedAt = DateTime.Now,
        };

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // Đơn mua mới -> báo cho Quản trị (ReplaceKey giữ mỗi PO một thông báo).
        await _notifications.NotifyDataChangedAsync("purchaseOrders", order.PurchaseOrderId, cancellationToken);

        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = order.PurchaseOrderId }, order);
    }

    // PUT /api/admin/purchaseorder/{id}/status — Submitted → Received (cộng kho) hoặc Cancelled.
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdatePurchaseOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var status = request.Status?.Trim();
        if (status is not ("Submitted" or "Received" or "Cancelled"))
        {
            return BadRequest(new { message = "Trạng thái chỉ nhận: Submitted, Received hoặc Cancelled." });
        }

        var order = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.PurchaseOrderId == id, cancellationToken);
        if (order is null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn đặt mua với mã: {id}" });
        }

        if (order.Status == "Received" && status != "Received")
        {
            return Conflict(new { message = "Đơn đã nhận hàng, không thể đổi sang trạng thái khác." });
        }
        if (order.Status == "Cancelled")
        {
            return Conflict(new { message = "Đơn đã hủy, không thể đổi trạng thái." });
        }
        if (order.Status == status)
        {
            return Ok(order); // idempotent
        }

        order.Status = status;
        order.UpdatedAt = DateTime.Now;

        if (status == "Received")
        {
            order.ReceivedDate = DateTime.Now;
            await ReceiveIntoInventoryAsync(order, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyDataChangedAsync("purchaseOrders", order.PurchaseOrderId, cancellationToken);

        return Ok(order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePurchaseOrder(string id, CancellationToken cancellationToken)
    {
        var order = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.PurchaseOrderId == id, cancellationToken);
        if (order is null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn đặt mua với mã: {id}" });
        }
        if (order.Status == "Received")
        {
            return Conflict(new { message = "Đơn đã nhận hàng nên không thể xóa (tồn kho đã được cộng)." });
        }

        _context.PurchaseOrders.Remove(order);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyDataChangedAsync("purchaseOrders", order.PurchaseOrderId, cancellationToken);

        return Ok(new { message = $"Đã xóa đơn đặt mua {id} thành công" });
    }

    /// <summary>
    /// Nhận hàng: cộng tồn kho của chi nhánh nhận (StoreId, NULL = kho không gán
    /// chi nhánh). InventoryItem trong schema là theo ItemCode duy nhất (mỗi dòng
    /// có thể gán một chi nhánh qua cột Location/Vendor) — cùng ItemCode đã có thì
    /// tăng StockQuantity, chưa có thì tạo dòng mới kế thừa vendor + đơn giá PO.
    /// </summary>
    private async Task ReceiveIntoInventoryAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ItemCode == order.ItemCode, cancellationToken);

        if (item is null)
        {
            _context.InventoryItems.Add(new InventoryItem
            {
                InventoryId = $"inv-{DateTime.Now.Ticks.ToString()[8..]}",
                ItemCode = order.ItemCode,
                Name = order.ItemName,
                Category = order.Category,
                StockQuantity = order.Quantity,
                ReorderLevel = 10,
                UnitCost = order.UnitCost,
                VendorId = order.VendorId,
                Status = "In Stock",
                CreatedAt = DateTime.Now,
            });
        }
        else
        {
            item.StockQuantity += order.Quantity;
            item.UpdatedAt = DateTime.Now;
        }
    }
}
