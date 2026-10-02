namespace doanki3API.Services.Email;

/// <summary>
/// Cấu hình SMTP — đọc từ section "Email" trong appsettings.json.
/// Với Gmail: Host = smtp.gmail.com, Port = 587, Password là App Password 16 ký tự
/// (Google Account → Security → 2-Step Verification → App passwords), KHÔNG phải mật khẩu Gmail.
/// </summary>
public sealed class EmailSettings
{
    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    /// <summary>Địa chỉ Gmail dùng để gửi.</summary>
    public string User { get; set; } = "";

    /// <summary>App Password 16 ký tự của Gmail.</summary>
    public string Password { get; set; } = "";

    public string FromName { get; set; } = "Nexus Telecom";

    /// <summary>Để trống thì lấy theo <see cref="User"/>.</summary>
    public string FromAddress { get; set; } = "";

    /// <summary>Thời gian chờ SMTP (giây) — tránh treo request khi mạng chậm.</summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Chưa điền User/Password thì coi như chưa cấu hình: hệ thống ghi nội dung mail
    /// ra log và file thay vì gửi thật, để vẫn chạy thử được toàn bộ luồng.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(User) &&
        !string.IsNullOrWhiteSpace(Password);

    public string ResolvedFromAddress =>
        string.IsNullOrWhiteSpace(FromAddress) ? User : FromAddress;
}
