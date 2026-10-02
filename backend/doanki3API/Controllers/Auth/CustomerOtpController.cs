using doanki3API.Data;
using doanki3API.DTOs.Auth;
using doanki3API.Models;
using doanki3API.Services.Auth;
using doanki3API.Services.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Controllers.Auth;

/// <summary>
/// Đăng nhập 2 bước cho khách hàng: nhập Mã tài khoản (nhận qua email sau khi kế toán
/// xác minh thanh toán) rồi nhập mã OTP 6 chữ số gửi về chính email đó.
///
/// Nhân viên vẫn đăng nhập bằng email + mật khẩu qua POST /api/auth/login như cũ.
/// </summary>
[ApiController]
[Route("api/auth/otp")]
public class CustomerOtpController : ControllerBase
{
    private readonly MyAppDbContext _context;
    private readonly LoginOtpService _otpService;
    private readonly EmailService _email;
    private readonly JwtTokenService _jwt;
    private readonly ILogger<CustomerOtpController> _logger;

    public CustomerOtpController(
        MyAppDbContext context,
        LoginOtpService otpService,
        EmailService email,
        JwtTokenService jwt,
        ILogger<CustomerOtpController> logger)
    {
        _context = context;
        _otpService = otpService;
        _email = email;
        _jwt = jwt;
        _logger = logger;
    }

    /// <summary>Bước 1 — khách nhập mã tài khoản, hệ thống gửi OTP về email đã đăng ký.</summary>
    [HttpPost("request")]
    public async Task<IActionResult> RequestOtp(
        [FromBody] RequestLoginOtpRequest request,
        CancellationToken ct)
    {
        var connection = await FindConnectionAsync(request.AccountId, ct);

        if (connection is null)
        {
            return NotFound(new { message = "Không tìm thấy mã tài khoản này trong hệ thống." });
        }

        var customer = connection.Order?.Customer;
        var email = customer?.Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            return Conflict(new
            {
                message = "Tài khoản này chưa có email đăng ký nên không gửi được mã OTP. " +
                          "Vui lòng liên hệ cửa hàng bán lẻ để bổ sung email."
            });
        }

        // Chặn bấm gửi lại liên tục làm ngập hộp thư của khách.
        var wait = await _otpService.GetResendWaitSecondsAsync(connection.AccountId, ct);
        if (wait > 0)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = $"Bạn vừa yêu cầu mã OTP. Vui lòng đợi {wait} giây rồi thử lại.",
                resendAfterSeconds = wait
            });
        }

        var customerName = string.IsNullOrWhiteSpace(customer?.FullName)
            ? "Quý khách"
            : customer!.FullName;

        var code = await _otpService.CreateAsync(connection.AccountId, email, customerName, ct);

        _ = Task.Run(() => _email.SendLoginOtpAsync(
            email, customerName, code, LoginOtpService.ValidMinutes, CancellationToken.None));

        return Ok(new RequestLoginOtpResponse(
            EmailService.Mask(email),
            LoginOtpService.ValidMinutes * 60,
            LoginOtpService.ResendCooldownSeconds,
            $"Mã OTP đã được gửi tới {EmailService.Mask(email)}."));
    }

    /// <summary>Bước 2 — khách nhập OTP, đúng thì cấp JWT và cho vào cổng khách hàng.</summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyLoginOtpRequest request,
        CancellationToken ct)
    {
        var connection = await FindConnectionAsync(request.AccountId, ct);

        if (connection is null)
        {
            return NotFound(new { message = "Không tìm thấy mã tài khoản này trong hệ thống." });
        }

        var result = await _otpService.VerifyAsync(connection.AccountId, request.Otp, ct);

        var error = result switch
        {
            OtpVerifyResult.NotFound =>
                "Chưa có mã OTP nào đang hiệu lực. Vui lòng bấm gửi lại mã.",
            OtpVerifyResult.Expired =>
                $"Mã OTP đã hết hạn sau {LoginOtpService.ValidMinutes} phút. Vui lòng bấm gửi lại mã.",
            OtpVerifyResult.TooManyAttempts =>
                "Bạn đã nhập sai quá số lần cho phép. Vui lòng bấm gửi lại mã mới.",
            OtpVerifyResult.Incorrect =>
                "Mã OTP không đúng. Vui lòng kiểm tra lại email.",
            _ => null
        };

        if (error is not null)
        {
            return Unauthorized(new { message = error });
        }

        var customer = connection.Order?.Customer;
        var token = _jwt.Generate(connection.AccountId, "Customer", "Customer");

        return Ok(new VerifyLoginOtpResponse(
            token,
            new OtpLoginUserDto(
                customer?.CustomerId ?? connection.AccountId,
                customer?.FullName ?? "Khách hàng Nexus",
                customer?.Email ?? "",
                "Customer",
                "Customer",
                connection.AccountId)));
    }

    // Chống bấm liên tục làm ngập hộp thư: mỗi email chỉ được yêu cầu lại mã sau 60 giây.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>
        ForgotRequests = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Khách quên / chưa nhận được mã tài khoản: nhập email, hệ thống gửi lại mọi mã tài khoản
    /// gắn với email đó. Luôn trả cùng một câu trả lời dù email có tồn tại hay không, để người
    /// ngoài không dò được email nào là khách hàng.
    /// </summary>
    [HttpPost("forgot-account-id")]
    public async Task<IActionResult> ForgotAccountId(
        [FromBody] ForgotAccountIdRequest request,
        CancellationToken ct)
    {
        var email = request.Email.Trim();
        const string genericMessage =
            "Nếu email này đã đăng ký dịch vụ, mã tài khoản sẽ được gửi tới hộp thư trong ít phút.";

        var now = DateTime.UtcNow;
        if (ForgotRequests.TryGetValue(email, out var last) && now - last < TimeSpan.FromSeconds(60))
        {
            var wait = 60 - (int)(now - last).TotalSeconds;
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = $"Bạn vừa yêu cầu. Vui lòng đợi {wait} giây rồi thử lại.",
                resendAfterSeconds = wait
            });
        }
        ForgotRequests[email] = now;

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email, ct);

        if (customer is not null)
        {
            var accountIds = await _context.CustomerConnections
                .AsNoTracking()
                .Where(c => c.Order.CustomerId == customer.CustomerId)
                .Select(c => c.AccountId)
                .Distinct()
                .ToListAsync(ct);

            if (accountIds.Count > 0)
            {
                var name = string.IsNullOrWhiteSpace(customer.FullName) ? "Quý khách" : customer.FullName;
                _ = Task.Run(() => _email.SendAccountIdReminderAsync(email, name, accountIds, CancellationToken.None));
            }
            else
            {
                _logger.LogInformation(
                    "Khách {CustomerId} yêu cầu gửi lại mã nhưng chưa có đường truyền nào được đấu nối.",
                    customer.CustomerId);
            }
        }

        return Ok(new { message = genericMessage });
    }

    /// <summary>
    /// Tìm kết nối theo mã tài khoản, chấp nhận mọi cách gõ: có gạch nối, không gạch nối,
    /// viết hoa hay viết thường đều ra cùng một tài khoản.
    /// </summary>
    private async Task<CustomerConnection?> FindConnectionAsync(string? accountId, CancellationToken ct)
    {
        var key = LoginOtpService.NormalizeAccountId(accountId);
        if (key.Length == 0) return null;

        // Dạng hiển thị chuẩn: 4 ký tự đầu + gạch nối + phần còn lại (VD T064-000000000001).
        var dashed = key.Length > 4 ? $"{key[..4]}-{key[4..]}" : key;

        return await _context.CustomerConnections
            .Include(c => c.Order)
                .ThenInclude(o => o!.Customer)
            .FirstOrDefaultAsync(
                c => c.AccountId == dashed || c.AccountId == key,
                ct);
    }
}
