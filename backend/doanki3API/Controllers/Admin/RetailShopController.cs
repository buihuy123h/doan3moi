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
using doanki3API.DTOs.Admin;

namespace doanki3API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Route("api/admin/retailstore")]
    [Authorize(Roles = EmployeeRoles.Manager)]
    public class RetailShopController : ControllerBase
    {
        private readonly MyAppDbContext _context;

        public RetailShopController(MyAppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 1. LẤY DANH SÁCH CÁC ĐIỂM BÁN LẺ / CHI NHÁNH (TAB RETAIL SHOPS)
        /// Trả về đầy đủ các trường tương ứng trên giao diện UI:
        /// - Mã chi nhánh: shopCode / storeId (e.g., 'SH-01')
        /// - Tên điểm bán lẻ: name (e.g., 'Downtown Nexus Flagship Store')
        /// - Phụ đề khu vực & Mã TP: city, cityCode, regionSubtitle (e.g., 'New York Regional Center · City Code: 064')
        /// - Địa chỉ trụ sở: address
        /// - Số điện thoại: phone
        /// - Quản lý chi nhánh: managerName (Store Manager)
        /// - Ô chỉ số 1: activeEmployeesCount (Nhân viên trực / On-Duty Employees Staff)
        /// - Ô chỉ số 2: totalSubscribersServed (Tổng thuê bao / Total Subscribers)
        /// URL: GET /api/admin/retailshop HOẶC GET /api/admin/retailshop/displayRetailShops
        /// </summary>
        [HttpGet]
        [HttpGet("displayRetailShops")]
        public async Task<ActionResult<IEnumerable<RetailShopResponseDto>>> DisplayRetailShops(
            [FromQuery] string? search,
            [FromQuery] string? city,
            [FromQuery] string? status)
        {
            var query = _context.RetailStores
                .Include(s => s.Employees)
                .AsNoTracking()
                .AsQueryable();

            // 1. Tìm kiếm theo từ khóa (Mã chi nhánh, Tên cửa hàng, Thành phố, Mã vùng, Tên quản lý, Địa chỉ)
            query = ApplySearch(query, search);

            // 2. Lọc theo thành phố hoặc trạng thái
            query = ApplyFilter(query, city, status);

            // Sắp xếp theo mã cửa hàng (SH-01, SH-02, ...)
            var stores = await query
                .OrderBy(s => s.StoreId)
                .ToListAsync();

            // Ánh xạ sang DTO chuẩn hóa cho Frontend
            var result = stores.Select(MapToResponseDto).ToList();

            return Ok(result);
        }

        /// <summary>
        /// 2. HÀM TÍNH TOÁN DỮ LIỆU THỐNG KÊ MẠNG LƯỚI ĐIỂM BÁN LẺ
        /// - Tổng số điểm bán lẻ (totalShops)
        /// - Số chi nhánh đang hoạt động (activeShopsCount)
        /// - Tổng số nhân viên đang trực trên toàn bộ các shop (totalOnDutyEmployees)
        /// - Tổng lượng thuê bao phục vụ trên toàn hệ thống (totalSubscribersServed)
        /// - Số thành phố có sự hiện diện của chi nhánh (totalCitiesCovered)
        /// URL: GET /api/admin/retailshop/stats HOẶC GET /api/admin/retailshop/dashboardStats
        /// </summary>
        [HttpGet("stats")]
        [HttpGet("dashboardStats")]
        public async Task<ActionResult<RetailShopStatsDto>> GetRetailShopStats()
        {
            var stores = await _context.RetailStores
                .Include(s => s.Employees)
                .AsNoTracking()
                .ToListAsync();

            // Tính toán tổng nhân viên trực (ưu tiên đếm thực tế từ bảng Employee nếu có dữ liệu)
            var totalOnDuty = stores.Sum(s =>
                s.Employees != null && s.Employees.Any()
                    ? s.Employees.Count(e => e.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    : s.ActiveEmployeesCount);

            var totalSubs = stores.Sum(s => s.TotalSubscribersServed);

            var stats = new RetailShopStatsDto
            {
                TotalShops = stores.Count,
                ActiveShopsCount = stores.Count(s => s.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)),
                TotalOnDutyEmployees = totalOnDuty,
                TotalSubscribersServed = totalSubs,
                TotalCitiesCovered = stores.Select(s => s.City).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            };

            return Ok(stats);
        }

        /// <summary>
        /// 3. LẤY CHI TIẾT MỘT ĐIỂM BÁN LẺ THEO MÃ STOREID / SHOPCODE
        /// URL: GET /api/admin/retailshop/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RetailShopResponseDto>> GetRetailShopById(string id)
        {
            var store = await _context.RetailStores
                .Include(s => s.Employees)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StoreId == id);

            if (store == null)
            {
                return NotFound(new { message = $"Không tìm thấy điểm bán lẻ với mã: {id}" });
            }

            return Ok(MapToResponseDto(store));
        }

        [HttpPost]
        public async Task<ActionResult<RetailShopResponseDto>> CreateRetailShop([FromBody] CreateRetailShopRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var storeId = request.StoreId ?? $"sh-{DateTime.UtcNow.Ticks.ToString().Substring(10)}";

            var store = new RetailStore
            {
                StoreId = storeId,
                Name = request.Name,
                City = request.City,
                CityCode = request.CityCode,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                ManagerName = request.ManagerName,
                OperatingHours = request.OperatingHours,
                ActiveEmployeesCount = request.ActiveEmployeesCount,
                TotalSubscribersServed = request.TotalSubscribersServed,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RetailStores.Add(store);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRetailShopById), new { id = store.StoreId }, MapToResponseDto(store));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRetailShop(string id, [FromBody] CreateRetailShopRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var store = await _context.RetailStores.FirstOrDefaultAsync(s => s.StoreId == id);

            if (store == null)
            {
                return NotFound(new { message = $"Không tìm thấy điểm bán lẻ với mã: {id}" });
            }

            store.Name = request.Name;
            store.City = request.City;
            store.CityCode = request.CityCode;
            store.Address = request.Address;
            store.Phone = request.Phone;
            store.Email = request.Email;
            store.ManagerName = request.ManagerName;
            store.OperatingHours = request.OperatingHours;
            store.ActiveEmployeesCount = request.ActiveEmployeesCount;
            store.TotalSubscribersServed = request.TotalSubscribersServed;
            store.Status = request.Status;
            store.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // fetch again to include employees if needed for DTO
            var storeWithEmps = await _context.RetailStores
                .Include(s => s.Employees)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StoreId == id);

            return Ok(MapToResponseDto(storeWithEmps ?? store));
        }

        #region Helper Methods (Xử lý Tìm kiếm, Lọc và Ánh xạ Entity -> DTO)

        /// <summary>
        /// Tìm kiếm không phân biệt chữ hoa thường trên các trường: Mã chi nhánh, Tên cửa hàng, Thành phố, Mã TP, Người quản lý, Địa chỉ, SĐT
        /// </summary>
        private static IQueryable<RetailStore> ApplySearch(IQueryable<RetailStore> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var trimmed = search.Trim();
            return query.Where(s =>
                EF.Functions.Like(s.StoreId, $"%{trimmed}%") ||
                EF.Functions.Like(s.Name, $"%{trimmed}%") ||
                EF.Functions.Like(s.City, $"%{trimmed}%") ||
                EF.Functions.Like(s.CityCode, $"%{trimmed}%") ||
                (s.ManagerName != null && EF.Functions.Like(s.ManagerName, $"%{trimmed}%")) ||
                EF.Functions.Like(s.Address, $"%{trimmed}%") ||
                (s.Phone != null && EF.Functions.Like(s.Phone, $"%{trimmed}%"))
            );
        }

        /// <summary>
        /// Lọc theo Thành phố hoặc Trạng thái
        /// </summary>
        private static IQueryable<RetailStore> ApplyFilter(
            IQueryable<RetailStore> query,
            string? city,
            string? status)
        {
            if (!string.IsNullOrWhiteSpace(city) &&
                !city.Equals("All", StringComparison.OrdinalIgnoreCase) &&
                !city.Equals("All Cities", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.City.ToLower() == city.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.Status.ToLower() == status.ToLower());
            }

            return query;
        }

        /// <summary>
        /// Ánh xạ từ Model Entity (RetailStore trong DB) sang DTO trả về cho FE
        /// Đảm bảo tính toán chính xác và bù đắp sai khác tên trường:
        /// - StoreId <-> id, shopCode, storeId
        /// - ActiveEmployeesCount <-> activeEmployeesCount, onDutyEmployees
        /// - TotalSubscribersServed <-> totalSubscribersServed, totalSubscribers
        /// </summary>
        private static RetailShopResponseDto MapToResponseDto(RetailStore s)
        {
            // Tính số lượng nhân sự trực: nếu có danh sách nhân viên liên kết thì đếm các nhân viên Active, ngược lại dùng ActiveEmployeesCount
            var onDutyCount = (s.Employees != null && s.Employees.Any())
                ? s.Employees.Count(e => e.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                : s.ActiveEmployeesCount;

            return new RetailShopResponseDto
            {
                // Khóa chính và mã cửa hàng (FE có thể gọi qua shop.id, shop.shopCode hoặc shop.storeId)
                Id = s.StoreId,
                StoreId = s.StoreId,
                ShopCode = s.StoreId,

                // Tên điểm bán lẻ
                Name = s.Name,

                // Thành phố & Mã vùng
                City = s.City,
                CityCode = s.CityCode,
                RegionSubtitle = $"{s.City} Regional Center · City Code: {s.CityCode}",

                // Thông tin liên hệ và địa chỉ
                Address = s.Address,
                Phone = s.Phone ?? "",
                ManagerName = !string.IsNullOrWhiteSpace(s.ManagerName) ? s.ManagerName : "Not Assigned",
                Email = s.Email,
                OperatingHours = s.OperatingHours,

                // Ô chỉ số 1: Nhân viên trực (On-Duty Employees Staff)
                ActiveEmployeesCount = onDutyCount,
                OnDutyEmployees = onDutyCount,

                // Ô chỉ số 2: Tổng thuê bao phục vụ (Total Subscribers)
                TotalSubscribersServed = s.TotalSubscribersServed,
                TotalSubscribers = s.TotalSubscribersServed,

                // Trạng thái & Audit fields
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            };
        }
        /// <summary>
        /// XÓA CỬA HÀNG (DELETE)
        /// URL: DELETE /api/admin/retailstore/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRetailShop(string id)
        {
            var shop = await _context.RetailStores.FindAsync(id);
            if (shop == null)
            {
                return NotFound(new { message = $"Không tìm thấy cửa hàng với ID = {id}" });
            }

            _context.RetailStores.Remove(shop);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa cửa hàng {id} thành công" });
        }

        #endregion
    }
}
