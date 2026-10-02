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
    [Route("api/admin/serviceplan")]
    [Authorize(Roles = EmployeeRoles.Manager)]
    public class PlanController : ControllerBase
    {
        private readonly MyAppDbContext _context;

        public PlanController(MyAppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 1. LẤY DANH SÁCH TẤT CẢ GÓI CƯỚC VIỄN THÔNG (TAB PLAN MANAGEMENT)
        /// Trả về đầy đủ các cột tương ứng trên giao diện UI:
        /// - PLAN NAME & DETAILS: name, billingCycle, validity, description
        /// - CONNECTION TYPE: type, connectionType ('Broadband', 'Dial-Up', 'Landline')
        /// - SPEED / BANDWIDTH: speedOrBandwidth
        /// - SECURITY DEPOSIT ($): securityDeposit
        /// - MONTHLY RENTAL ($): monthlyRental
        /// - HOURLY CHARGE ($): hourlyCharge
        /// - DATA LIMIT: dataLimit
        /// - STATUS: status
        /// Hỗ trợ tìm kiếm từ khóa và lọc theo loại kết nối (Landline, Broadband, Dial-Up).
        /// URL: GET /api/admin/plan HOẶC GET /api/admin/plan/displayPlans
        /// </summary>
        [HttpGet]
        [HttpGet("displayPlans")]
        [HttpGet("displayPlan")]
        public async Task<ActionResult<IEnumerable<PlanResponseDto>>> DisplayPlans(
            [FromQuery] string? search,
            [FromQuery] string? type,
            [FromQuery] string? connectionType,
            [FromQuery] string? status)
        {
            var query = _context.ServicePlans
                .AsNoTracking()
                .AsQueryable();

            // 1. Tìm kiếm theo từ khóa (Tên gói, Mã gói, Tốc độ, Mô tả, Chu kỳ cước)
            query = ApplySearch(query, search);

            // 2. Lọc theo Loại kết nối (Broadband, Dial-Up, Landline)
            var selectedType = !string.IsNullOrWhiteSpace(type) ? type : connectionType;
            query = ApplyFilter(query, selectedType, status);

            var plans = await query
                .OrderBy(p => p.ConnectionType)
                .ThenBy(p => p.MonthlyRental)
                .ToListAsync();

            var result = plans.Select(MapToResponseDto).ToList();

            return Ok(result);
        }

        /// <summary>
        /// 2. LẤY RIÊNG DANH SÁCH CÁC GÓI CƯỚC ĐIỆN THOẠI CỐ ĐỊNH (LANDLINE PLANS)
        /// URL: GET /api/admin/plan/landline
        /// </summary>
        [HttpGet("landline")]
        public async Task<ActionResult<IEnumerable<PlanResponseDto>>> GetLandlinePlans([FromQuery] string? search)
        {
            var query = _context.ServicePlans
                .AsNoTracking()
                .Where(p => p.ConnectionType == "Landline");

            query = ApplySearch(query, search);

            var plans = await query
                .OrderBy(p => p.MonthlyRental)
                .ToListAsync();

            return Ok(plans.Select(MapToResponseDto));
        }

        /// <summary>
        /// 3. LẤY RIÊNG DANH SÁCH CÁC GÓI CƯỚC CÁP QUANG BĂNG RỘNG (BROADBAND PLANS)
        /// URL: GET /api/admin/plan/broadband
        /// </summary>
        [HttpGet("broadband")]
        public async Task<ActionResult<IEnumerable<PlanResponseDto>>> GetBroadbandPlans([FromQuery] string? search)
        {
            var query = _context.ServicePlans
                .AsNoTracking()
                .Where(p => p.ConnectionType == "Broadband");

            query = ApplySearch(query, search);

            var plans = await query
                .OrderBy(p => p.MonthlyRental)
                .ToListAsync();

            return Ok(plans.Select(MapToResponseDto));
        }

        /// <summary>
        /// 4. LẤY RIÊNG DANH SÁCH CÁC GÓI CƯỚC QUAY SỐ (DIAL-UP PLANS)
        /// URL: GET /api/admin/plan/dial-up
        /// </summary>
        [HttpGet("dial-up")]
        public async Task<ActionResult<IEnumerable<PlanResponseDto>>> GetDialUpPlans([FromQuery] string? search)
        {
            var query = _context.ServicePlans
                .AsNoTracking()
                .Where(p => p.ConnectionType == "Dial-Up");

            query = ApplySearch(query, search);

            var plans = await query
                .OrderBy(p => p.MonthlyRental)
                .ToListAsync();

            return Ok(plans.Select(MapToResponseDto));
        }

        /// <summary>
        /// 5. HÀM TÍNH TOÁN THỐNG KÊ GÓI CƯỚC (PLAN STATS)
        /// Thống kê số lượng gói cước từng loại kết nối: Broadband, Dial-Up, Landline
        /// URL: GET /api/admin/plan/stats HOẶC GET /api/admin/plan/dashboardStats
        /// </summary>
        [HttpGet("stats")]
        [HttpGet("dashboardStats")]
        public async Task<ActionResult<PlanStatsDto>> GetPlanStats()
        {
            var plans = await _context.ServicePlans
                .AsNoTracking()
                .ToListAsync();

            var stats = new PlanStatsDto
            {
                TotalPlans = plans.Count,
                BroadbandPlansCount = plans.Count(p => p.ConnectionType.Equals("Broadband", StringComparison.OrdinalIgnoreCase)),
                DialUpPlansCount = plans.Count(p => p.ConnectionType.Equals("Dial-Up", StringComparison.OrdinalIgnoreCase)),
                LandlinePlansCount = plans.Count(p => p.ConnectionType.Equals("Landline", StringComparison.OrdinalIgnoreCase)),
                ActivePlansCount = plans.Count(p => p.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)),
                ArchivedPlansCount = plans.Count(p => p.Status.Equals("Archived", StringComparison.OrdinalIgnoreCase))
            };

            return Ok(stats);
        }

        /// <summary>
        /// 6. LẤY CHI TIẾT MỘT GÓI CƯỚC THEO ID HOẶC PLAN CODE
        /// URL: GET /api/admin/plan/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PlanResponseDto>> GetPlanById(string id)
        {
            var plan = await _context.ServicePlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlanId == id || p.PlanCode == id);

            if (plan == null)
            {
                return NotFound(new { message = $"Không tìm thấy gói cước với mã: {id}" });
            }

            return Ok(MapToResponseDto(plan));
        }

        [HttpPost]
        public async Task<ActionResult<PlanResponseDto>> CreatePlan([FromBody] CreatePlanRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var planId = request.PlanId ?? $"plan-{DateTime.UtcNow.Ticks.ToString().Substring(8)}";
            var planCode = request.PlanId?.ToUpper();

            var plan = new ServicePlan
            {
                PlanId = planId,
                PlanCode = planCode,
                Name = request.Name,
                ConnectionType = request.ConnectionType,
                SpeedOrBandwidth = request.SpeedOrBandwidth,
                MonthlyRental = request.MonthlyRental,
                SecurityDeposit = request.SecurityDeposit,
                BillingCycle = request.BillingCycle,
                Validity = request.Validity,
                DataLimit = request.DataLimit,
                Status = request.Status,
                IncludedHours = request.IncludedHours,
                HourlyCharge = request.HourlyCharge ?? 0m,
                Description = request.Description,
                CallRates = request.CallRates,
                IsPopular = request.IsPopular,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ServicePlans.Add(plan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlanById), new { id = plan.PlanId }, MapToResponseDto(plan));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlan(string id, [FromBody] CreatePlanRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var plan = await _context.ServicePlans.FirstOrDefaultAsync(p => p.PlanId == id || p.PlanCode == id);

            if (plan == null)
            {
                return NotFound(new { message = $"Không tìm thấy gói cước với mã: {id}" });
            }

            plan.Name = request.Name;
            plan.ConnectionType = request.ConnectionType;
            plan.SpeedOrBandwidth = request.SpeedOrBandwidth;
            plan.MonthlyRental = request.MonthlyRental;
            plan.SecurityDeposit = request.SecurityDeposit;
            plan.BillingCycle = request.BillingCycle;
            plan.Validity = request.Validity;
            plan.DataLimit = request.DataLimit;
            plan.Status = request.Status;
            plan.IncludedHours = request.IncludedHours;
            plan.HourlyCharge = request.HourlyCharge ?? 0m;
            plan.Description = request.Description;
            plan.CallRates = request.CallRates;
            plan.IsPopular = request.IsPopular;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToResponseDto(plan));
        }

        #region Helper Methods (Xử lý Tìm kiếm, Lọc và Ánh xạ Entity -> DTO)

        /// <summary>
        /// Tìm kiếm trên: Mã gói, Tên gói cước, Tốc độ, Mô tả, Chu kỳ cước, Giá cước cuộc gọi
        /// </summary>
        private static IQueryable<ServicePlan> ApplySearch(IQueryable<ServicePlan> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var trimmed = search.Trim();
            return query.Where(p =>
                EF.Functions.Like(p.PlanId, $"%{trimmed}%") ||
                (p.PlanCode != null && EF.Functions.Like(p.PlanCode, $"%{trimmed}%")) ||
                EF.Functions.Like(p.Name, $"%{trimmed}%") ||
                EF.Functions.Like(p.SpeedOrBandwidth, $"%{trimmed}%") ||
                (p.Description != null && EF.Functions.Like(p.Description, $"%{trimmed}%")) ||
                (p.CallRates != null && EF.Functions.Like(p.CallRates, $"%{trimmed}%"))
            );
        }

        /// <summary>
        /// Lọc theo Loại kết nối (Broadband, Dial-Up, Landline) và Trạng thái
        /// </summary>
        private static IQueryable<ServicePlan> ApplyFilter(
            IQueryable<ServicePlan> query,
            string? connectionType,
            string? status)
        {
            if (!string.IsNullOrWhiteSpace(connectionType) &&
                !connectionType.Equals("All", StringComparison.OrdinalIgnoreCase) &&
                !connectionType.Equals("All Plans", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.ConnectionType.ToLower() == connectionType.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Status.ToLower() == status.ToLower());
            }

            return query;
        }

        /// <summary>
        /// Ánh xạ từ Model Entity (ServicePlan trong DB) sang DTO trả về cho FE
        /// Đảm bảo bù đắp sai khác tên trường:
        /// - PlanId <-> id, planId
        /// - ConnectionType <-> type, connectionType ('Broadband', 'Dial-Up', 'Landline')
        /// </summary>
        private static PlanResponseDto MapToResponseDto(ServicePlan p)
        {
            return new PlanResponseDto
            {
                // Khóa chính và mã gói (FE gọi qua plan.id hoặc plan.planId)
                Id = p.PlanId,
                PlanId = p.PlanId,
                PlanCode = p.PlanCode,

                // Cột 1: Tên gói & Thông tin chi tiết
                Name = p.Name,
                BillingCycle = p.BillingCycle,
                Validity = p.Validity,
                Description = p.Description,

                // Cột 2: Loại kết nối (hỗ trợ cả type và connectionType)
                Type = p.ConnectionType,
                ConnectionType = p.ConnectionType,

                // Cột 3: Tốc độ / Băng thông
                SpeedOrBandwidth = p.SpeedOrBandwidth,

                // Cột 4: Tiền cọc ($)
                SecurityDeposit = p.SecurityDeposit,

                // Cột 5: Cước thuê tháng ($)
                MonthlyRental = p.MonthlyRental,

                // Cột 6: Cước theo giờ ($)
                HourlyCharge = p.HourlyCharge > 0 ? p.HourlyCharge : null,

                // Cột 7: Giới hạn dữ liệu
                DataLimit = p.DataLimit ?? "Unlimited",

                // Cột 8: Trạng thái
                Status = p.Status,

                // Thông tin đặc tả khác
                IncludedHours = p.IncludedHours,
                CallRates = p.CallRates,
                IsPopular = p.IsPopular,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }
        /// <summary>
        /// XÓA GÓI CƯỚC (DELETE)
        /// URL: DELETE /api/admin/serviceplan/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlan(string id)
        {
            var plan = await _context.ServicePlans.FindAsync(id);
            if (plan == null)
            {
                return NotFound(new { message = $"Không tìm thấy gói cước với ID = {id}" });
            }

            _context.ServicePlans.Remove(plan);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa gói cước {id} thành công" });
        }

        #endregion
    }
}
