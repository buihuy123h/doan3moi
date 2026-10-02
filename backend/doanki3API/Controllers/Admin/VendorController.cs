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
    [Authorize(Roles = EmployeeRoles.Manager)]
    public class VendorController : ControllerBase
    {
        private readonly MyAppDbContext _context;

        public VendorController(MyAppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 1. LẤY DANH SÁCH NHÀ CUNG CẤP THIẾT BỊ (TAB VENDOR MANAGEMENT)
        /// Trả về đầy đủ các cột tương ứng trên giao diện UI:
        /// - VENDOR CODE (vendorCode)
        /// - COMPANY NAME (companyName)
        /// - CONTACT PERSON (contactPerson)
        /// - SUPPLY CATEGORY (category / supplyCategory)
        /// - DIRECT PHONE & EMAIL (phone, email)
        /// - FACILITY ADDRESS (address / facilityAddress)
        /// - STATUS (status)
        /// Hỗ trợ tìm kiếm từ khóa và lọc danh mục.
        /// URL: GET /api/admin/vendor HOẶC GET /api/admin/vendor/displayVendor
        /// </summary>
        [HttpGet]
        [HttpGet("displayVendor")]
        public async Task<ActionResult<IEnumerable<VendorResponseDto>>> DisplayVendor(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? status)
        {
            var query = _context.Vendors
                .Include(v => v.InventoryItems)
                .AsNoTracking()
                .AsQueryable();

            // 1. Tìm kiếm theo từ khóa (Mã NCC, Tên công ty, Người liên hệ, Email, SĐT, Địa chỉ)
            query = ApplySearch(query, search);

            // 2. Lọc theo danh mục hoặc trạng thái hợp tác
            query = ApplyFilter(query, category, status);

            // Sắp xếp theo mã nhà cung cấp
            var vendors = await query
                .OrderBy(v => v.VendorCode)
                .ToListAsync();

            // Ánh xạ sang DTO chuẩn hóa cho Frontend
            var result = vendors.Select(MapToResponseDto).ToList();

            return Ok(result);
        }

        /// <summary>
        /// 2. HÀM TÍNH TOÁN DỮ LIỆU THỐNG KÊ NHÀ CUNG CẤP (VENDOR STATS)
        /// Thống kê tổng số nhà cung cấp, số đơn vị đang hợp tác, đang chờ duyệt, và số nhóm danh mục cung ứng
        /// URL: GET /api/admin/vendor/stats HOẶC GET /api/admin/vendor/dashboardStats
        /// </summary>
        [HttpGet("stats")]
        [HttpGet("dashboardStats")]
        public async Task<ActionResult<VendorStatsDto>> GetVendorStats()
        {
            var vendors = await _context.Vendors
                .AsNoTracking()
                .ToListAsync();

            var stats = new VendorStatsDto
            {
                TotalVendors = vendors.Count,
                ActiveVendorsCount = vendors.Count(v => v.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)),
                PendingReviewCount = vendors.Count(v => v.Status.Equals("Pending Review", StringComparison.OrdinalIgnoreCase)),
                TerminatedCount = vendors.Count(v => v.Status.Equals("Terminated", StringComparison.OrdinalIgnoreCase)),
                TotalCategoriesCount = vendors.Select(v => v.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            };

            return Ok(stats);
        }

        /// <summary>
        /// 3. LẤY CHI TIẾT MỘT NHÀ CUNG CẤP THEO ID HOẶC VENDOR CODE
        /// URL: GET /api/admin/vendor/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<VendorResponseDto>> GetVendorById(string id)
        {
            var vendor = await _context.Vendors
                .Include(v => v.InventoryItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VendorId == id || v.VendorCode == id);

            if (vendor == null)
            {
                return NotFound(new { message = $"Không tìm thấy nhà cung cấp với mã: {id}" });
            }

            return Ok(MapToResponseDto(vendor));
        }

        [HttpPost]
        public async Task<ActionResult<VendorResponseDto>> CreateVendor([FromBody] CreateVendorRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var vendorId = request.VendorId ?? $"vnd-{DateTime.UtcNow.Ticks.ToString().Substring(10)}";

            var vendor = new Vendor
            {
                VendorId = vendorId,
                VendorCode = request.VendorCode ?? vendorId.ToUpper(),
                CompanyName = request.CompanyName,
                ContactPerson = request.ContactPerson,
                Category = request.Category ?? "Hardware",
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Rating = (byte)request.Rating,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVendorById), new { id = vendor.VendorId }, MapToResponseDto(vendor));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVendor(string id, [FromBody] CreateVendorRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == id || v.VendorCode == id);

            if (vendor == null)
            {
                return NotFound(new { message = $"Không tìm thấy nhà cung cấp với mã: {id}" });
            }

            vendor.VendorCode = request.VendorCode ?? vendor.VendorCode;
            vendor.CompanyName = request.CompanyName;
            vendor.ContactPerson = request.ContactPerson;
            vendor.Category = request.Category ?? vendor.Category;
            vendor.Phone = request.Phone;
            vendor.Email = request.Email;
            vendor.Address = request.Address;
            vendor.Rating = (byte)request.Rating;
            vendor.Status = request.Status;
            vendor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var vendorWithItems = await _context.Vendors
                .Include(v => v.InventoryItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VendorId == vendor.VendorId);

            return Ok(MapToResponseDto(vendorWithItems ?? vendor));
        }

        #region Helper Methods (Xử lý Tìm kiếm, Lọc và Ánh xạ Entity -> DTO)

        /// <summary>
        /// Tìm kiếm không phân biệt chữ hoa thường trên các trường: Mã NCC, Tên công ty, Người liên hệ, Email, SĐT, Địa chỉ, Danh mục
        /// </summary>
        private static IQueryable<Vendor> ApplySearch(IQueryable<Vendor> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var trimmed = search.Trim();
            return query.Where(v =>
                EF.Functions.Like(v.VendorCode, $"%{trimmed}%") ||
                EF.Functions.Like(v.CompanyName, $"%{trimmed}%") ||
                (v.ContactPerson != null && EF.Functions.Like(v.ContactPerson, $"%{trimmed}%")) ||
                EF.Functions.Like(v.Category, $"%{trimmed}%") ||
                (v.Email != null && EF.Functions.Like(v.Email, $"%{trimmed}%")) ||
                (v.Phone != null && EF.Functions.Like(v.Phone, $"%{trimmed}%")) ||
                (v.Address != null && EF.Functions.Like(v.Address, $"%{trimmed}%")) ||
                EF.Functions.Like(v.VendorId, $"%{trimmed}%")
            );
        }

        /// <summary>
        /// Lọc theo danh mục cung ứng (Supply Category) và trạng thái hợp tác (Status)
        /// </summary>
        private static IQueryable<Vendor> ApplyFilter(
            IQueryable<Vendor> query,
            string? category,
            string? status)
        {
            if (!string.IsNullOrWhiteSpace(category) &&
                !category.Equals("All", StringComparison.OrdinalIgnoreCase) &&
                !category.Equals("All Categories", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(v => v.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(v => v.Status.ToLower() == status.ToLower());
            }

            return query;
        }

        /// <summary>
        /// Ánh xạ từ Model Entity (Vendor trong DB) sang DTO trả về cho FE
        /// Đảm bảo map đúng các trường giao diện yêu cầu
        /// </summary>
        private static VendorResponseDto MapToResponseDto(Vendor v)
        {
            return new VendorResponseDto
            {
                // Khóa chính và mã
                Id = v.VendorId,
                VendorId = v.VendorId,
                VendorCode = v.VendorCode,

                // Cột 2: Tên công ty
                CompanyName = v.CompanyName,

                // Cột 3: Người đại diện
                ContactPerson = v.ContactPerson ?? "",

                // Cột 4: Danh mục cung ứng (cung cấp cả 2 key category & supplyCategory)
                Category = v.Category,
                SupplyCategory = v.Category,

                // Cột 5: Điện thoại & Email
                Phone = v.Phone ?? "",
                Email = v.Email ?? "",

                // Cột 6: Địa chỉ cơ sở (cung cấp cả 2 key address & facilityAddress)
                Address = v.Address ?? "",
                FacilityAddress = v.Address ?? "",

                // Cột 7: Trạng thái
                Status = v.Status,

                // Các trường mở rộng
                TaxNumber = v.TaxNumber,
                Rating = v.Rating,
                TotalSuppliedItemsCount = v.InventoryItems?.Count ?? 0,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            };
        }
        /// <summary>
        /// XÓA ĐỐI TÁC (DELETE)
        /// URL: DELETE /api/admin/vendor/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(string id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
            {
                return NotFound(new { message = $"Không tìm thấy đối tác với ID = {id}" });
            }

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa đối tác {id} thành công" });
        }

        #endregion
    }
}
