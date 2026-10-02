using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using doanki3API.Data;

namespace doanki3API.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize] // Requires any logged-in user
    public class ProfileController : ControllerBase
    {
        private readonly MyAppDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(MyAppDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class UpdateProfileRequest
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Address { get; set; }
            public string? Title { get; set; }
            public string? Department { get; set; }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Unauthorized user." });
            }

            if (role == "Customer")
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == userId);
                if (customer == null) return NotFound(new { error = "Customer not found." });

                if (!string.IsNullOrEmpty(request.Name)) customer.FullName = request.Name;
                if (!string.IsNullOrEmpty(request.Email)) customer.Email = request.Email;
                if (!string.IsNullOrEmpty(request.PhoneNumber)) customer.ContactNumber = request.PhoneNumber;
                if (!string.IsNullOrEmpty(request.Address)) customer.Address = request.Address;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Customer profile updated successfully.", customer });
            }
            else
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == userId);
                if (employee == null) return NotFound(new { error = "Employee not found." });

                if (!string.IsNullOrEmpty(request.Name)) employee.FullName = request.Name;
                if (!string.IsNullOrEmpty(request.Email)) employee.Email = request.Email;
                if (!string.IsNullOrEmpty(request.PhoneNumber)) employee.Phone = request.PhoneNumber;
                
                // Address, Title, Department might not be in the Employee model, let's verify later.
                // Assuming Name, Email, Phone are standard.
                
                await _context.SaveChangesAsync();
                return Ok(new { message = "Employee profile updated successfully.", employee });
            }
        }

        public class UpdatePasswordRequest
        {
            public string CurrentPassword { get; set; } = null!;
            public string NewPassword { get; set; } = null!;
        }

        [HttpPut("password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Unauthorized user." });
            }

            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { error = "Vui lòng nhập mật khẩu cũ và mới." });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new { error = "Mật khẩu mới phải có ít nhất 6 ký tự." });
            }

            if (role == "Customer")
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == userId);
                if (customer == null) return NotFound(new { error = "Customer not found." });

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, customer.PasswordHash))
                {
                    return BadRequest(new { error = "Mật khẩu hiện tại không chính xác." });
                }

                customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 10);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"[AUDIT] Khách hàng {userId} đã thay đổi mật khẩu thành công lúc {DateTime.Now}.");
                return Ok(new { message = "Đổi mật khẩu thành công." });
            }
            else
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == userId);
                if (employee == null) return NotFound(new { error = "Employee not found." });

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, employee.PasswordHash))
                {
                    return BadRequest(new { error = "Mật khẩu hiện tại không chính xác." });
                }

                employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 10);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[AUDIT] Nhân viên {userId} đã thay đổi mật khẩu thành công lúc {DateTime.Now}.");
                return Ok(new { message = "Đổi mật khẩu thành công." });
            }
        }
    }
}
