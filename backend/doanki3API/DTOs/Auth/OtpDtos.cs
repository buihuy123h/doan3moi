using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Auth;

/// <summary>Bước 1 đăng nhập khách hàng: nhập mã tài khoản để nhận OTP qua email.</summary>
public sealed class RequestLoginOtpRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã tài khoản.")]
    [StringLength(30, ErrorMessage = "Mã tài khoản không hợp lệ.")]
    public string AccountId { get; set; } = null!;
}

/// <summary>Bước 2: nhập mã OTP nhận được trong email.</summary>
public sealed class VerifyLoginOtpRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã tài khoản.")]
    [StringLength(30, ErrorMessage = "Mã tài khoản không hợp lệ.")]
    public string AccountId { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP gồm đúng 6 chữ số.")]
    public string Otp { get; set; } = null!;
}

/// <summary>Khách quên mã tài khoản: nhập email để nhận lại mã.</summary>
public sealed class ForgotAccountIdRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100, ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = null!;
}

/// <summary>Kết quả gửi OTP — email được che bớt để không lộ thông tin khách.</summary>
public sealed record RequestLoginOtpResponse(
    string MaskedEmail,
    int ExpiresInSeconds,
    int ResendAfterSeconds,
    string Message);

/// <summary>Thông tin tài khoản trả về sau khi OTP hợp lệ (khớp LoginApiResponse bên frontend).</summary>
public sealed record OtpLoginUserDto(
    string Id,
    string Name,
    string Email,
    string Role,
    string AccountType,
    string AccountId);

public sealed record VerifyLoginOtpResponse(string Token, OtpLoginUserDto User);
