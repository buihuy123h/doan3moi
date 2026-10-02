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
    public class EmployeeController : ControllerBase
    {
        private readonly MyAppDbContext _context;

        public EmployeeController(MyAppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách nhân viên để hiển thị trên giao diện Admin (Tab Employee Management)
        /// Hỗ trợ cả tìm kiếm (search) và lọc (filter role, department, status, storeId)
        /// URL: GET /api/admin/employee HOẶC GET /api/admin/employee/displayEmployee
        /// </summary>
        [HttpGet]
        [HttpGet("displayEmployee")]
        public async Task<ActionResult<IEnumerable<EmployeeResponseDto>>> DisplayEmployee(
            [FromQuery] string? search,
            [FromQuery] string? role,
            [FromQuery] string? department,
            [FromQuery] string? status,
            [FromQuery] string? storeId)
        {
            var query = _context.Employees
                .Include(e => e.Store)
                .AsNoTracking()
                .AsQueryable();

            // 1. Áp dụng tìm kiếm theo từ khóa (Search theo tên, mã NV, email, sđt)
            query = ApplySearch(query, search);

            // 2. Áp dụng bộ lọc (Filter theo vai trò, phòng ban, trạng thái, chi nhánh)
            query = ApplyFilter(query, role, department, status, storeId);

            // Sắp xếp theo mã nhân viên tăng dần
            var employees = await query
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            // Ánh xạ sang DTO chuẩn hóa với tên trường ở Frontend
            var result = employees.Select(MapToResponseDto).ToList();

            return Ok(result);
        }

        /// <summary>
        /// API chuyên biệt cho chức năng tìm kiếm nhân viên
        /// URL: GET /api/admin/employee/search?keyword=...
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EmployeeResponseDto>>> SearchEmployees([FromQuery] string? keyword)
        {
            var query = _context.Employees
                .Include(e => e.Store)
                .AsNoTracking()
                .AsQueryable();

            query = ApplySearch(query, keyword);

            var employees = await query
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            return Ok(employees.Select(MapToResponseDto));
        }

        /// <summary>
        /// API chuyên biệt cho chức năng lọc nhân viên theo nhiều tiêu chí
        /// URL: GET /api/admin/employee/filter?role=...&department=...&status=...
        /// </summary>
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<EmployeeResponseDto>>> FilterEmployees(
            [FromQuery] string? role,
            [FromQuery] string? department,
            [FromQuery] string? status,
            [FromQuery] string? storeId)
        {
            var query = _context.Employees
                .Include(e => e.Store)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyFilter(query, role, department, status, storeId);

            var employees = await query
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            return Ok(employees.Select(MapToResponseDto));
        }

        /// <summary>
        /// Lấy chi tiết một nhân viên theo ID hoặc Mã nhân viên (EmployeeCode)
        /// URL: GET /api/admin/employee/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeResponseDto>> GetEmployeeById(string id)
        {
            var employee = await _context.Employees
                .Include(e => e.Store)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id || e.EmployeeCode == id);

            if (employee == null)
            {
                return NotFound(new { message = $"Không tìm thấy nhân viên với mã: {id}" });
            }

            return Ok(MapToResponseDto(employee));
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên, tài khoản và mật khẩu (nếu có cung cấp)
        /// URL: PUT /api/admin/employee/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeResponseDto>> UpdateEmployee(string id, [FromBody] UpdateEmployeeRequest request)
        {
            var employee = await _context.Employees
                .Include(e => e.Store)
                .FirstOrDefaultAsync(e => e.EmployeeId == id || e.EmployeeCode == id);

            if (employee == null)
            {
                return NotFound(new { message = $"Không tìm thấy nhân viên với mã: {id}" });
            }

            var cleanEmail = request.Email.Trim();
            // Kiểm tra trùng email nếu email thay đổi
            if (!employee.Email.Equals(cleanEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Employees.AnyAsync(e => e.EmployeeId != employee.EmployeeId && e.Email == cleanEmail)
                    || await _context.Customers.AnyAsync(c => c.Email == cleanEmail);

                if (emailExists)
                {
                    return Conflict(new { message = "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống." });
                }
                employee.Email = cleanEmail;
            }

            employee.FullName = request.FullName.Trim();
            employee.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            employee.Role = request.Role.Trim();
            employee.Department = request.Department.Trim();
            employee.Status = request.Status.Trim();

            // Phân giải StoreId nếu có
            if (!string.IsNullOrWhiteSpace(request.StoreId))
            {
                employee.StoreId = request.StoreId.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(request.RetailShopAssigned))
            {
                var store = await _context.RetailStores.FirstOrDefaultAsync(s => 
                    request.RetailShopAssigned.Contains(s.StoreId) || request.RetailShopAssigned.Contains(s.Name));
                if (store != null)
                {
                    employee.StoreId = store.StoreId;
                }
            }

            // Xử lý ngày gia nhập nếu có
            if (!string.IsNullOrWhiteSpace(request.DateOfJoining) && DateOnly.TryParse(request.DateOfJoining, out var dateOfJoining))
            {
                employee.DateOfJoining = dateOfJoining;
            }

            // Xử lý đổi mật khẩu nếu admin nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim(), 10);
            }

            employee.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Nạp lại dữ liệu Store liên kết
            await _context.Entry(employee).Reference(e => e.Store).LoadAsync();

            return Ok(MapToResponseDto(employee));
        }

        /// <summary>
        /// Tạo nhân viên mới kèm thông tin tài khoản và mật khẩu khởi tạo
        /// URL: POST /api/admin/employee
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<EmployeeResponseDto>> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            var cleanEmail = request.Email.Trim();
            var emailExists = await _context.Employees.AnyAsync(e => e.Email == cleanEmail)
                || await _context.Customers.AnyAsync(c => c.Email == cleanEmail);

            if (emailExists)
            {
                return Conflict(new { message = "Email này đã được sử dụng trong hệ thống." });
            }

            // Sinh EmployeeId và EmployeeCode nếu chưa có
            var nextEmpNum = await _context.Employees.CountAsync() + 1;
            var employeeId = $"emp-{nextEmpNum:D2}";
            if (await _context.Employees.AnyAsync(e => e.EmployeeId == employeeId))
            {
                employeeId = $"emp-{DateTime.UtcNow:yyMMddHHmmss}";
            }

            var employeeCode = !string.IsNullOrWhiteSpace(request.EmployeeCode)
                ? request.EmployeeCode.Trim().ToUpper()
                : $"EMP-{1000 + nextEmpNum}";

            // Phân giải StoreId
            string? storeId = null;
            if (!string.IsNullOrWhiteSpace(request.StoreId))
            {
                storeId = request.StoreId.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(request.RetailShopAssigned))
            {
                var store = await _context.RetailStores.FirstOrDefaultAsync(s => 
                    request.RetailShopAssigned.Contains(s.StoreId) || request.RetailShopAssigned.Contains(s.Name));
                storeId = store?.StoreId;
            }

            // Hash mật khẩu (nếu không có mật khẩu nhập vào thì dùng mật khẩu demo thống nhất)
            var rawPassword = string.IsNullOrWhiteSpace(request.Password) ? "1234567890" : request.Password.Trim();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword, 10);

            var dateOfJoining = DateOnly.FromDateTime(DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(request.DateOfJoining) && DateOnly.TryParse(request.DateOfJoining, out var parsedDate))
            {
                dateOfJoining = parsedDate;
            }

            var newEmployee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = employeeCode,
                FullName = request.FullName.Trim(),
                Email = cleanEmail,
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                PasswordHash = passwordHash,
                Role = request.Role.Trim(),
                Department = request.Department.Trim(),
                StoreId = storeId,
                Status = request.Status.Trim(),
                DateOfJoining = dateOfJoining,
                CreatedAt = DateTime.Now
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();

            await _context.Entry(newEmployee).Reference(e => e.Store).LoadAsync();

            return CreatedAtAction(nameof(GetEmployeeById), new { id = newEmployee.EmployeeId }, MapToResponseDto(newEmployee));
        }

        /// <summary>
        /// Xóa hoặc chuyển trạng thái nhân viên sang Inactive
        /// URL: DELETE /api/admin/employee/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id || e.EmployeeCode == id);

            if (employee == null)
            {
                return NotFound(new { message = $"Không tìm thấy nhân viên với mã: {id}" });
            }

            // Đổi trạng thái sang Inactive để đảm bảo toàn vẹn dữ liệu
            employee.Status = "Inactive";
            employee.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã chuyển trạng thái nhân viên {employee.FullName} sang Inactive thành công." });
        }

        #region Helper Methods (Xử lý Tìm kiếm, Lọc và Ánh xạ Dữ liệu)

        /// <summary>
        /// Hàm áp dụng điều kiện tìm kiếm tương thích với thanh tìm kiếm ở FE
        /// Tìm kiếm không dấu / không phân biệt chữ hoa thường trên các trường: FullName, EmployeeCode, Email, Phone
        /// </summary>
        private static IQueryable<Employee> ApplySearch(IQueryable<Employee> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var trimmedSearch = search.Trim();
            return query.Where(e =>
                EF.Functions.Like(e.FullName, $"%{trimmedSearch}%") ||
                EF.Functions.Like(e.EmployeeCode, $"%{trimmedSearch}%") ||
                EF.Functions.Like(e.Email, $"%{trimmedSearch}%") ||
                (e.Phone != null && EF.Functions.Like(e.Phone, $"%{trimmedSearch}%")) ||
                EF.Functions.Like(e.EmployeeId, $"%{trimmedSearch}%")
            );
        }

        /// <summary>
        /// Hàm áp dụng các bộ lọc tương thích với dropdown bộ lọc trên UI:
        /// - Role (All Roles, Manager, Retail Staff, Field Engineer, Senior Accountant, Support Agent)
        /// - Department (Administration, Retail Outlets, Technical Operations, Finance & Accounts)
        /// - Status (Active, Inactive)
        /// - StoreId
        /// </summary>
        private static IQueryable<Employee> ApplyFilter(
            IQueryable<Employee> query,
            string? role,
            string? department,
            string? status,
            string? storeId)
        {
            if (!string.IsNullOrWhiteSpace(role) &&
                !role.Equals("All", StringComparison.OrdinalIgnoreCase) &&
                !role.Equals("All Roles", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Role.ToLower() == role.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(department) &&
                !department.Equals("All", StringComparison.OrdinalIgnoreCase) &&
                !department.Equals("All Departments", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Department.ToLower() == department.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.Status.ToLower() == status.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(storeId) &&
                !storeId.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e => e.StoreId == storeId);
            }

            return query;
        }

        /// <summary>
        /// Ánh xạ từ Model Entity (Employee trong DB) sang DTO trả về cho FE
        /// Tránh nhầm lẫn giữa:
        /// - Employee.EmployeeId vs FE.id
        /// - Employee.FullName vs FE.name
        /// - Employee.Store vs FE.retailShopAssigned
        /// - Employee.DateOfJoining (DateOnly) vs FE.dateOfJoining (String)
        /// </summary>
        private static EmployeeResponseDto MapToResponseDto(Employee e)
        {
            // Xác định chuỗi hiển thị chi nhánh phân công (Assigned Shop)
            string assignedShop = "Headquarters";
            if (e.Store != null)
            {
                assignedShop = !string.IsNullOrEmpty(e.Store.StoreId) && !string.IsNullOrEmpty(e.Store.Name)
                    ? $"{e.Store.Name} ({e.Store.StoreId})"
                    : (e.Store.Name ?? e.Store.StoreId ?? "Headquarters");
            }

            return new EmployeeResponseDto
            {
                // Ánh xạ khóa và mã
                Id = e.EmployeeId,
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,

                // Ánh xạ họ tên (đáp ứng cả `name` lẫn `fullName` để FE gọi kiểu nào cũng có dữ liệu)
                Name = e.FullName,
                FullName = e.FullName,

                // Liên hệ
                Email = e.Email,
                Phone = e.Phone ?? "",

                // Chức vụ và bộ phận
                Role = e.Role,
                Department = e.Department,

                // Chi nhánh phân công
                RetailShopAssigned = assignedShop,
                StoreId = e.StoreId,
                StoreName = e.Store?.Name,

                // Trạng thái và ngày gia nhập
                Status = e.Status,
                DateOfJoining = e.DateOfJoining.ToString("yyyy-MM-dd"),

                // Các thông tin cá nhân & audit fields
                AvatarUrl = e.AvatarUrl,
                Address = e.Address,
                Gender = e.Gender,
                DateOfBirth = e.DateOfBirth.HasValue ? e.DateOfBirth.Value.ToString("yyyy-MM-dd") : null,
                Bio = e.Bio,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };
        }

        #endregion
    }
}
