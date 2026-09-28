using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using doanki3API.Data;
using doanki3API.Services.Auth;
using doanki3API.Models;

namespace doanki3API.Controllers.Auth
{
    // ===== Request/Response DTOs =====
    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly MyAppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly JwtTokenService _jwt;

        public AuthController(
            MyAppDbContext context,
            IConfiguration configuration,
            JwtTokenService jwt)
        {
            _context = context;
            _configuration = configuration;
            _jwt = jwt;
        }

        // Hàm hỗ trợ kiểm tra định dạng email
        private static bool IsValidEmail(string email)
        {
            var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            return emailRegex.IsMatch(email);
        }

        /// <summary>
        /// Đăng ký tài khoản USER (khách hàng) — port y hệt hàm register trong auth.controller.js
        /// URL: POST /api/auth/register
        /// Body: { email, password, name, phoneNumber }
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var email = request.Email?.Trim() ?? "";
                var password = request.Password ?? "";
                var name = request.Name?.Trim() ?? "";
                var phoneNumber = request.PhoneNumber?.Trim() ?? "";

                // 1. Kiểm tra các trường bắt buộc
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phoneNumber))
                {
                    return BadRequest(new { error = "Email, password, name và phoneNumber là bắt buộc." });
                }

                // 2. Kiểm tra định dạng email
                if (!IsValidEmail(email))
                {
                    return BadRequest(new { error = "Email không hợp lệ." });
                }

                // 3. Kiểm tra độ dài mật khẩu
                if (password.Length < 6)
                {
                    return BadRequest(new { error = "Mật khẩu phải có ít nhất 6 ký tự." });
                }

                // 4. Kiểm tra định dạng số điện thoại (đúng 10 chữ số, không dùng regex)
                if (phoneNumber.Length != 10 || phoneNumber.Any(c => c < '0' || c > '9'))
                {
                    return BadRequest(new { error = "Số điện thoại không hợp lệ." });
                }

                // 5. Kiểm tra email đã tồn tại chưa (cả Employee lẫn Customer — tránh 1 email 2 tài khoản)
                var existingEmail = await _context.Employees.AnyAsync(e => e.Email == email)
                    || await _context.Customers.AnyAsync(c => c.Email == email);
                if (existingEmail)
                {
                    return Conflict(new { error = "Email này đã được sử dụng." });
                }

                // 6. Kiểm tra số điện thoại đã tồn tại chưa
                var existingPhone = await _context.Employees.AnyAsync(e => e.Phone == phoneNumber)
                    || await _context.Customers.AnyAsync(c => c.ContactNumber == phoneNumber);
                if (existingPhone)
                {
                    return Conflict(new { error = "Số điện thoại này đã được sử dụng." });
                }

                // 7. Hash mật khẩu (bcrypt, salt rounds = 10 — tương thích với bcryptjs)
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, 10);

                // 8. Lưu Customer (user) vào database — role mặc định là "Customer" (bên Node là CLIENT)
                // Sinh CustomerID duy nhất vì DB yêu cầu khóa chính NOT NULL
                var customerId = await GenerateUniqueCustomerIdAsync();

                var newCustomer = new Customer
                {
                    CustomerId = customerId,
                    FullName = name,
                    Email = email,
                    ContactNumber = phoneNumber,
                    PasswordHash = hashedPassword,
                    CustomerType = "Individual",        // mặc định theo schema DB
                    IdProofType = "National ID Card",   // mặc định theo schema DB
                    IdProofNumber = customerId,         // tạm dùng CustomerID, admin cập nhật lại sau
                    Address = "",
                    Status = "Active"
                };

                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();

                // 9. Loại bỏ password trước khi trả về. Kèm luôn JWT role Customer để
                //    FE gọi ngay POST /api/Technical/orders sau khi đăng ký (endpoint
                //    đó đã gắn [Authorize(Roles = ...)] nên không token là bị 401).
                string token = _jwt.Generate(newCustomer.CustomerId, "Customer", "Customer");
                return StatusCode(201, new
                {
                    message = "Đăng ký thành công.",
                    token,
                    user = new
                    {
                        id = newCustomer.CustomerId,
                        name = newCustomer.FullName,
                        email = newCustomer.Email,
                        phoneNumber = newCustomer.ContactNumber,
                        role = "Customer",
                        accountType = "Customer"
                    }
                });
            }
            catch (Exception error)
            {
                Console.WriteLine($"Lỗi khi đăng ký: {error}");
                return StatusCode(500, new { error = "Lỗi máy chủ nội bộ." });
            }
        }

        /// <summary>
        /// Đăng nhập — dùng chung cho TẤT CẢ tài khoản:
        /// - Nhân viên (Employee): role lấy từ DB (Manager, Retail Staff, Field Engineer, ...)
        /// - Khách hàng (Customer): role = "Customer"
        /// Mỗi lần đăng nhập thành công đều cấp JWT mới chứa id + role + accountType
        /// URL: POST /api/auth/login
        /// Body: { email, password }
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var email = request.Email?.Trim() ?? "";
                var password = request.Password ?? "";

                if (!IsValidEmail(email))
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                // 1. Kiểm tra thiếu trường
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                // 2. Tìm account theo email — thử Employee trước rồi đến Customer
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == email);

                var customer = employee == null
                    ? await _context.Customers
                        .FirstOrDefaultAsync(c => c.Email == email)
                    : null;

                // 3. Nếu không tồn tại tài khoản nào → thông báo chung để tránh lộ thông tin
                if (employee == null && customer == null)
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                // 4. So sánh mật khẩu với hash tương ứng (bcrypt) — sai cũng trả thông báo chung
                var isPasswordMatch = employee != null
                    ? BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash)
                    : customer!.PasswordHash != null &&
                      BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash);

                if (!isPasswordMatch)
                {
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                // 5. Xác định id + role theo loại tài khoản
                var accountId = employee != null ? employee.EmployeeId : customer!.CustomerId;
                var role = employee != null ? employee.Role : "Customer";
                var accountType = employee != null ? "Employee" : "Customer";

                // 6. Tạo JWT token mới cho mỗi lần đăng nhập (Employee lẫn Customer đều nhận
                //    token — Employee cần token này để gọi các API giới hạn theo role,
                //    VD [Authorize(Roles = EmployeeRoles.Accounts)] trên BillingController)
                string token = _jwt.Generate(accountId, role, accountType);

                // 7. Trả về token + thông tin cơ bản của user (FE lưu localStorage)
                return Ok(new
                {
                    token,
                    user = employee != null
                        ? new
                        {
                            id = employee.EmployeeId,
                            name = employee.FullName,
                            email = employee.Email ?? string.Empty,
                            role = employee.Role,
                            accountType = "Employee"
                        }
                        : new
                        {
                            id = customer!.CustomerId,
                            name = customer.FullName,
                            email = customer.Email ?? string.Empty,
                            role = "Customer",
                            accountType = "Customer"
                        }
                });
            }
            catch (Exception error)
            {
                Console.WriteLine($"Lỗi khi đăng nhập: {error}");
                return StatusCode(500, new { error = "Lỗi máy chủ nội bộ." });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Tạo JWT token ký bằng Jwt:Key trong appsettings.json
        /// Payload chứa:
        /// - NameIdentifier = id tài khoản (EmployeeID hoặc CustomerID)
        /// - Role           = role của tài khoản (Manager, Retail Staff, ..., Customer)
        /// - accountType    = "Employee" hoặc "Customer" để FE/middleware phân biệt
        /// </summary>

        /// <summary>
        /// Sinh CustomerID duy nhất (CUS + 6 số) — DB yêu cầu khóa chính NOT NULL
        /// </summary>
        private async Task<string> GenerateUniqueCustomerIdAsync()
        {
            var random = new Random();
            while (true)
            {
                var candidate = $"CUS{random.Next(100000, 999999)}";
                if (!await _context.Customers.AnyAsync(c => c.CustomerId == candidate))
                    return candidate;
            }
        }

        #endregion
    }
}
