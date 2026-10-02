using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using doanki3API.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanki3API.Data;
using doanki3API.Models;
using doanki3API.Services.Notifications;
using doanki3API.DTOs.Admin;

namespace doanki3API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = EmployeeRoles.Manager)]
    public class InventoryController : ControllerBase
    {
        private readonly MyAppDbContext _context;
        private readonly NotificationService _notifications;

        public InventoryController(MyAppDbContext context, NotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        /// <summary>
        /// Tồn kho chạm ngưỡng đặt lại thì báo ngay cho Quản trị (ReplaceKey giữ mỗi vật tư
        /// chỉ một thông báo). Gọi sau khi đã SaveChanges.
        /// </summary>
        private async Task NotifyStockAsync(InventoryItem item)
        {
            if (item.StockQuantity <= item.ReorderLevel)
            {
                await _notifications.LowStockAsync(
                    item.InventoryId,
                    item.Name,
                    item.ItemCode,
                    item.StockQuantity,
                    item.ReorderLevel);
            }

            await _notifications.NotifyDataChangedAsync("inventory", item.InventoryId);
        }

        /// <summary>
        /// 1. LẤY DANH SÁCH VẬT TƯ / THIẾT BỊ TRONG KHO (TAB STOCK / INVENTORY)
        /// Trả về đầy đủ các cột tương ứng trên giao diện UI:
        /// - ITEM CODE (itemCode)
        /// - DEVICE / EQUIPMENT NAME (name)
        /// - CATEGORY (category)
        /// - DEPOT LOCATION (location)
        /// - STOCK UNITS (stockQuantity)
        /// - REORDER THRESHOLD (reorderLevel)
        /// - UNIT COST (unitCost)
        /// - SUPPLIER (supplier / supplierName)
        /// Hỗ trợ tìm kiếm từ khóa và lọc danh mục.
        /// URL: GET /api/admin/inventory HOẶC GET /api/admin/inventory/displayInventory
        /// </summary>
        [HttpGet]
        [HttpGet("displayInventory")]
        public async Task<ActionResult<IEnumerable<InventoryItemResponseDto>>> DisplayInventory(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] bool? isLowStock,
            [FromQuery] string? vendorId,
            [FromQuery] string? status)
        {
            var query = _context.InventoryItems
                .Include(i => i.Vendor)
                .AsNoTracking()
                .AsQueryable();

            // 1. Tìm kiếm theo từ khóa (Mã vật tư, Tên thiết bị, Vị trí kho, Tên nhà cung cấp)
            query = ApplySearch(query, search);

            // 2. Lọc theo danh mục hoặc trạng thái cảnh báo
            query = ApplyFilter(query, category, isLowStock, vendorId, status);

            // Sắp xếp theo mã vật tư
            var items = await query
                .OrderBy(i => i.ItemCode)
                .ToListAsync();

            // Ánh xạ sang DTO chuẩn hóa cho Frontend
            var result = items.Select(MapToResponseDto).ToList();

            return Ok(result);
        }

        /// <summary>
        /// 2. HÀM TÍNH TOÁN CÁC DỮ LIỆU Ở 3 Ô PHÍA TRÊN BẢNG (SUMMARY STATS CARDS)
        /// - Ô 1: Tổng thiết bị tồn kho (Total Equipment Stock) = SUM(StockQuantity)
        /// - Ô 2: Cảnh báo sắp hết hàng (Low Stock Alerts) = COUNT(StockQuantity <= ReorderLevel)
        /// - Ô 3: Ước tính giá trị kho (Valuation Est.) = SUM(StockQuantity * UnitCost)
        /// URL: GET /api/admin/inventory/stats HOẶC GET /api/admin/inventory/dashboardStats
        /// </summary>
        [HttpGet("stats")]
        [HttpGet("dashboardStats")]
        public async Task<ActionResult<InventoryStatsDto>> GetInventoryStats()
        {
            // Lấy toàn bộ danh sách kèm nhà cung cấp để tính toán
            var items = await _context.InventoryItems
                .Include(i => i.Vendor)
                .AsNoTracking()
                .ToListAsync();

            // Ô 1: Tổng số thiết bị vật tư đang có trong kho
            var totalStockUnits = items.Sum(i => i.StockQuantity);

            // Ô 2: Lọc các mặt hàng có số lượng tồn kho <= ngưỡng đặt lại (ReorderLevel)
            var lowStockItemsList = items
                .Where(i => i.StockQuantity <= i.ReorderLevel)
                .Select(MapToResponseDto)
                .ToList();
            var lowStockCount = lowStockItemsList.Count;

            // Ô 3: Tổng giá trị định giá của toàn bộ thiết bị trong kho
            var totalValuation = items.Sum(i => (decimal)i.StockQuantity * i.UnitCost);

            // Tổng số danh mục mặt hàng (SKUs)
            var totalCategoriesCount = items.Count;

            var stats = new InventoryStatsDto
            {
                TotalStockUnits = totalStockUnits,
                LowStockCount = lowStockCount,
                TotalValuation = Math.Round(totalValuation, 2),
                TotalCategoriesCount = totalCategoriesCount,
                LowStockItems = lowStockItemsList
            };

            return Ok(stats);
        }

        /// <summary>
        /// 3. API TỔNG HỢP (OVERVIEW)
        /// Trả về cả dữ liệu tính toán 3 ô stats và danh mục table trong 1 request duy nhất
        /// URL: GET /api/admin/inventory/overview
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<InventoryOverviewResponseDto>> GetInventoryOverview([FromQuery] string? search)
        {
            var query = _context.InventoryItems
                .Include(i => i.Vendor)
                .AsNoTracking()
                .AsQueryable();

            query = ApplySearch(query, search);

            var items = await query.OrderBy(i => i.ItemCode).ToListAsync();
            var dtoList = items.Select(MapToResponseDto).ToList();

            var lowStockList = dtoList.Where(x => x.IsLowStock).ToList();

            var overview = new InventoryOverviewResponseDto
            {
                Stats = new InventoryStatsDto
                {
                    TotalStockUnits = items.Sum(i => i.StockQuantity),
                    LowStockCount = lowStockList.Count,
                    TotalValuation = Math.Round(items.Sum(i => (decimal)i.StockQuantity * i.UnitCost), 2),
                    TotalCategoriesCount = items.Count,
                    LowStockItems = lowStockList
                },
                Items = dtoList
            };

            return Ok(overview);
        }

        /// <summary>
        /// 4. LẤY DANH SÁCH CÁC MẶT HÀNG SẮP HẾT HÀNG (DƯỚI MỨC TỐI THIỂU)
        /// URL: GET /api/admin/inventory/low-stock
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<InventoryItemResponseDto>>> GetLowStockItems()
        {
            var lowStockEntities = await _context.InventoryItems
                .Include(i => i.Vendor)
                .AsNoTracking()
                .Where(i => i.StockQuantity <= i.ReorderLevel)
                .OrderBy(i => i.StockQuantity)
                .ToListAsync();

            return Ok(lowStockEntities.Select(MapToResponseDto));
        }

        /// <summary>
        /// 5. LẤY CHI TIẾT MỘT MẶT HÀNG VẬT TƯ THEO ID HOẶC ITEM CODE
        /// URL: GET /api/admin/inventory/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryItemResponseDto>> GetInventoryItemById(string id)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Vendor)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InventoryId == id || i.ItemCode == id);

            if (item == null)
            {
                return NotFound(new { message = $"Không tìm thấy vật tư / thiết bị với mã: {id}" });
            }

            return Ok(MapToResponseDto(item));
        }

        /// <summary>
        /// 6. CẬP NHẬT THÔNG TIN VẬT TƯ THIẾT BỊ (PHẦN CHỈNH SỬA VẬT TƯ THIẾT BỊ)
        /// URL: PUT /api/admin/inventory/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<InventoryItemResponseDto>> UpdateInventoryItem(
            string id,
            [FromBody] UpdateAdminInventoryRequest request)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Vendor)
                .FirstOrDefaultAsync(i => i.InventoryId == id || i.ItemCode == id);

            if (item == null)
            {
                return NotFound(new { message = $"Không tìm thấy vật tư / thiết bị với mã: {id}" });
            }

            if (!string.IsNullOrWhiteSpace(request.ItemCode))
            {
                item.ItemCode = request.ItemCode.Trim();
            }

            item.Name = request.Name.Trim();
            item.Category = request.Category.Trim();
            item.StockQuantity = request.StockQuantity;
            item.ReorderLevel = request.ReorderLevel;
            item.UnitCost = request.UnitCost;
            item.Location = string.IsNullOrWhiteSpace(request.Location) ? "Central Depot NYC" : request.Location.Trim();

            // Phân giải VendorId nếu có
            if (!string.IsNullOrWhiteSpace(request.VendorId))
            {
                item.VendorId = request.VendorId.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(request.Supplier))
            {
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => 
                    request.Supplier.Contains(v.CompanyName) || request.Supplier.Contains(v.VendorId) || request.Supplier.Contains(v.VendorCode));
                if (vendor != null)
                {
                    item.VendorId = vendor.VendorId;
                }
            }

            // Tự động cập nhật trạng thái kho
            item.Status = item.StockQuantity <= item.ReorderLevel ? "Low Stock" : "In Stock";
            item.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await NotifyStockAsync(item);

            await _context.Entry(item).Reference(i => i.Vendor).LoadAsync();

            return Ok(MapToResponseDto(item));
        }

        /// <summary>
        /// 7. NHẬP THÊM HÀNG THIẾU HỤT / BỔ SUNG KHO (RESTOCK MISSING ITEMS)
        /// URL: POST /api/admin/inventory/{id}/restock
        /// </summary>
        [HttpPost("{id}/restock")]
        public async Task<ActionResult<InventoryItemResponseDto>> RestockInventory(
            string id,
            [FromBody] RestockInventoryRequest request)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Vendor)
                .FirstOrDefaultAsync(i => i.InventoryId == id || i.ItemCode == id);

            if (item == null)
            {
                return NotFound(new { message = $"Không tìm thấy vật tư / thiết bị với mã: {id}" });
            }

            item.StockQuantity += request.Quantity;
            if (item.StockQuantity > item.ReorderLevel && item.Status == "Low Stock")
            {
                item.Status = "In Stock";
            }
            item.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await NotifyStockAsync(item);

            await _context.Entry(item).Reference(i => i.Vendor).LoadAsync();

            return Ok(MapToResponseDto(item));
        }

        /// <summary>
        /// 8. THÊM MỚI VẬT TƯ THIẾT BỊ VÀO KHO
        /// URL: POST /api/admin/inventory
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InventoryItemResponseDto>> CreateInventoryItem(
            [FromBody] CreateAdminInventoryRequest request)
        {
            var totalCount = await _context.InventoryItems.CountAsync();
            var inventoryId = $"inv-{totalCount + 1:D2}";
            if (await _context.InventoryItems.AnyAsync(i => i.InventoryId == inventoryId))
            {
                inventoryId = $"inv-{DateTime.UtcNow:yyMMddHHmmss}";
            }

            var itemCode = !string.IsNullOrWhiteSpace(request.ItemCode)
                ? request.ItemCode.Trim()
                : $"EQ-DEV-{totalCount + 1:D3}";

            // Phân giải VendorId
            string? vendorId = null;
            if (!string.IsNullOrWhiteSpace(request.VendorId))
            {
                vendorId = request.VendorId.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(request.Supplier))
            {
                var vendor = await _context.Vendors.FirstOrDefaultAsync(v => 
                    request.Supplier.Contains(v.CompanyName) || request.Supplier.Contains(v.VendorId));
                vendorId = vendor?.VendorId;
            }

            if (string.IsNullOrWhiteSpace(vendorId))
            {
                vendorId = await _context.Vendors.Select(v => v.VendorId).FirstOrDefaultAsync() ?? "vnd-02";
            }

            var newItem = new InventoryItem
            {
                InventoryId = inventoryId,
                ItemCode = itemCode,
                Name = request.Name.Trim(),
                Category = request.Category.Trim(),
                StockQuantity = request.StockQuantity,
                ReorderLevel = request.ReorderLevel,
                UnitCost = request.UnitCost,
                Location = string.IsNullOrWhiteSpace(request.Location) ? "Central Depot NYC" : request.Location.Trim(),
                VendorId = vendorId,
                Status = request.StockQuantity <= request.ReorderLevel ? "Low Stock" : "In Stock",
                CreatedAt = DateTime.Now
            };

            _context.InventoryItems.Add(newItem);
            await _context.SaveChangesAsync();

            await _context.Entry(newItem).Reference(i => i.Vendor).LoadAsync();

            return CreatedAtAction(nameof(GetInventoryItemById), new { id = newItem.InventoryId }, MapToResponseDto(newItem));
        }

        /// <summary>
        /// 9. XÓA VẬT TƯ THIẾT BỊ KHỎI KHO
        /// URL: DELETE /api/admin/inventory/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryItem(string id)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.InventoryId == id || i.ItemCode == id);

            if (item == null)
            {
                return NotFound(new { message = $"Không tìm thấy vật tư / thiết bị với mã: {id}" });
            }

            // Kiểm tra xem có thiết bị Equipment nào đang tham chiếu tới InventoryItem này không
            var hasEquipment = await _context.Equipment.AnyAsync(e => e.InventoryId == item.InventoryId);
            if (hasEquipment)
            {
                item.Status = "Discontinued";
                item.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Vật tư đang được gắn với thiết bị phần cứng, đã chuyển trạng thái sang Discontinued." });
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa vật tư {item.Name} ({item.ItemCode}) khỏi kho thành công." });
        }

        #region Helper Methods (Xử lý Tìm kiếm, Lọc và Ánh xạ Entity -> DTO)

        /// <summary>
        /// Tìm kiếm theo từ khóa trên các trường: Mã vật tư, Tên thiết bị, Danh mục, Vị trí kho, Tên nhà cung cấp
        /// </summary>
        private static IQueryable<InventoryItem> ApplySearch(IQueryable<InventoryItem> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var trimmed = search.Trim();
            return query.Where(i =>
                EF.Functions.Like(i.ItemCode, $"%{trimmed}%") ||
                EF.Functions.Like(i.Name, $"%{trimmed}%") ||
                EF.Functions.Like(i.Category, $"%{trimmed}%") ||
                (i.Location != null && EF.Functions.Like(i.Location, $"%{trimmed}%")) ||
                (i.Vendor != null && EF.Functions.Like(i.Vendor.CompanyName, $"%{trimmed}%")) ||
                EF.Functions.Like(i.InventoryId, $"%{trimmed}%")
            );
        }

        /// <summary>
        /// Lọc theo danh mục thiết bị, trạng thái cảnh báo thấp, hoặc nhà cung cấp
        /// </summary>
        private static IQueryable<InventoryItem> ApplyFilter(
            IQueryable<InventoryItem> query,
            string? category,
            bool? isLowStock,
            string? vendorId,
            string? status)
        {
            if (!string.IsNullOrWhiteSpace(category) &&
                !category.Equals("All", StringComparison.OrdinalIgnoreCase) &&
                !category.Equals("All Categories", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(i => i.Category.ToLower() == category.ToLower());
            }

            if (isLowStock.HasValue && isLowStock.Value)
            {
                query = query.Where(i => i.StockQuantity <= i.ReorderLevel);
            }

            if (!string.IsNullOrWhiteSpace(vendorId) &&
                !vendorId.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(i => i.VendorId == vendorId);
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(i => i.Status.ToLower() == status.ToLower());
            }

            return query;
        }

        /// <summary>
        /// Ánh xạ từ Model Entity (InventoryItem trong DB) sang DTO trả về cho FE
        /// Đảm bảo tính toán chính xác:
        /// - isLowStock = StockQuantity <= ReorderLevel
        /// - totalValue = StockQuantity * UnitCost
        /// - supplier = Vendor.CompanyName (nếu có) hoặc mặc định
        /// </summary>
        private static InventoryItemResponseDto MapToResponseDto(InventoryItem item)
        {
            var isLow = item.StockQuantity <= item.ReorderLevel;
            var supplierName = item.Vendor?.CompanyName ?? "Nexus Central Supply";

            return new InventoryItemResponseDto
            {
                // Khóa chính
                Id = item.InventoryId,
                InventoryId = item.InventoryId,

                // Cột 1: Mã vật tư
                ItemCode = item.ItemCode,

                // Cột 2: Tên thiết bị / vật tư (cung cấp cả 2 key name và deviceName)
                Name = item.Name,
                DeviceName = item.Name,

                // Cột 3: Danh mục
                Category = item.Category,

                // Cột 4: Vị trí kho (cung cấp cả location và depotLocation)
                Location = item.Location ?? "Central Depot",
                DepotLocation = item.Location ?? "Central Depot",

                // Cột 5: Số lượng tồn kho (cung cấp cả stockQuantity và stockUnits)
                StockQuantity = item.StockQuantity,
                StockUnits = item.StockQuantity,

                // Cột 6: Ngưỡng đặt lại (cung cấp cả reorderLevel và reorderThreshold)
                ReorderLevel = item.ReorderLevel,
                ReorderThreshold = item.ReorderLevel,

                // Cột 7: Đơn giá
                UnitCost = item.UnitCost,

                // Cột 8: Nhà cung cấp
                Supplier = supplierName,
                SupplierName = supplierName,
                VendorId = item.VendorId,

                // Thuộc tính tính toán và trạng thái
                IsLowStock = isLow,
                TotalValue = Math.Round((decimal)item.StockQuantity * item.UnitCost, 2),
                Status = item.Status,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        #endregion
    }
}
