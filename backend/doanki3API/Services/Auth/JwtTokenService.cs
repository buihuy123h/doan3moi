using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace doanki3API.Services.Auth;

/// <summary>
/// Sinh JWT cho cả nhân viên (đăng nhập email + mật khẩu) lẫn khách hàng
/// (đăng nhập bằng mã tài khoản + OTP). Tách ra service dùng chung để hai luồng
/// đăng nhập không sinh token theo hai kiểu khác nhau.
/// </summary>
public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(string accountId, string role, string accountType)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings.json");
        var issuer = _configuration["Jwt:Issuer"];
        var expiresInMinutes = int.Parse(_configuration["Jwt:ExpiresInMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId),
            new(ClaimTypes.Role, role),
            new("accountType", accountType)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}
