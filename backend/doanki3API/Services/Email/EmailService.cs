using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace doanki3API.Services.Email;

/// <summary>
/// Gửi email cho khách hàng: mã đơn hàng, mã đăng nhập sau khi kế toán xác minh thanh toán,
/// và mã OTP đăng nhập.
///
/// Nếu chưa cấu hình SMTP trong appsettings, mọi email được ghi ra thư mục logs/emails
/// thay vì gửi thật — nhờ vậy vẫn chạy thử được toàn bộ luồng khi chưa có App Password.
/// </summary>
public sealed class EmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;

        if (!_settings.IsConfigured)
        {
            _logger.LogWarning(
                "Chưa cấu hình SMTP (section \"Email\" trong appsettings.json). " +
                "Email sẽ được ghi ra thư mục logs/emails thay vì gửi thật.");
        }
    }

    // ==================== 3 loại email của hệ thống ====================

    /// <summary>1. Khách vừa đặt đơn — gửi mã đơn hàng để tra cứu tiến độ.</summary>
    public Task<bool> SendOrderPlacedAsync(
        string toEmail,
        string customerName,
        string orderId,
        string? planName,
        CancellationToken ct = default)
    {
        var body = Layout(
            title: "Đã tiếp nhận đơn đăng ký",
            greeting: $"Chào {Escape(customerName)},",
            intro: "Nexus Telecom đã tiếp nhận đơn đăng ký dịch vụ của bạn. " +
                   "Vui lòng giữ lại mã đơn hàng bên dưới để tra cứu tiến độ xử lý.",
            codeLabel: "MÃ ĐƠN HÀNG",
            code: orderId,
            details: new[]
            {
                ("Dịch vụ đăng ký", string.IsNullOrWhiteSpace(planName) ? "—" : Escape(planName)),
                ("Trạng thái", "Chờ cửa hàng bán lẻ duyệt hồ sơ")
            },
            footer: "Đơn của bạn sẽ được cửa hàng bán lẻ duyệt hồ sơ, sau đó bộ phận kỹ thuật " +
                    "khảo sát khả thi. Chúng tôi sẽ gửi email khi có kết quả.");

        return SendAsync(toEmail, $"[Nexus] Đã tiếp nhận đơn đăng ký {orderId}", body, ct);
    }

    /// <summary>
    /// 2. Kế toán đã xác minh thanh toán — gửi Mã tài khoản, đây cũng là mã để khách đăng nhập.
    /// </summary>
    public Task<bool> SendAccountActivatedAsync(
        string toEmail,
        string customerName,
        string accountId,
        string invoiceNumber,
        decimal amountPaid,
        bool isFirstPayment,
        CancellationToken ct = default)
    {
        var title = isFirstPayment ? "Tài khoản đã được kích hoạt" : "Đã nhận thanh toán";
        var intro = isFirstPayment
            ? "Bộ phận kế toán đã xác minh khoản thanh toán của bạn. Tài khoản dịch vụ đã được " +
              "kích hoạt — dùng mã bên dưới để đăng nhập vào cổng khách hàng."
            : "Bộ phận kế toán đã ghi nhận khoản thanh toán của bạn. Mã đăng nhập của bạn vẫn như sau.";

        var body = Layout(
            title: title,
            greeting: $"Chào {Escape(customerName)},",
            intro: intro,
            codeLabel: "MÃ ĐĂNG NHẬP (MÃ TÀI KHOẢN)",
            code: FormatAccountId(accountId),
            details: new[]
            {
                ("Số hoá đơn", Escape(invoiceNumber)),
                ("Số tiền đã ghi nhận", $"${amountPaid:0.00}")
            },
            footer: "Khi đăng nhập, bạn nhập mã tài khoản này rồi nhập tiếp mã OTP gồm 6 chữ số " +
                    "mà hệ thống gửi về chính email này. Tuyệt đối không chia sẻ mã cho người khác.");

        var subject = isFirstPayment
            ? $"[Nexus] Kích hoạt tài khoản {accountId}"
            : $"[Nexus] Đã nhận thanh toán hoá đơn {invoiceNumber}";

        return SendAsync(toEmail, subject, body, ct);
    }

    /// <summary>
    /// 2b. Kỹ thuật vừa đấu nối xong — gửi Mã tài khoản ngay lúc này.
    /// Trước đây mã chỉ được gửi khi kế toán ghi nhận thanh toán, nhưng khách lại cần chính
    /// mã đó để đăng nhập (kể cả để thanh toán online), nên khách mới không vào được cổng.
    /// </summary>
    public Task<bool> SendConnectionReadyAsync(
        string toEmail,
        string customerName,
        string accountId,
        string orderId,
        string? planName,
        CancellationToken ct = default)
    {
        var body = Layout(
            title: "Đường truyền đã sẵn sàng",
            greeting: $"Chào {Escape(customerName)},",
            intro: "Bộ phận kỹ thuật đã đấu nối xong dịch vụ của bạn. Dưới đây là Mã tài khoản — " +
                   "đây cũng là mã để đăng nhập vào cổng khách hàng Nexus Telecom.",
            codeLabel: "MÃ ĐĂNG NHẬP (MÃ TÀI KHOẢN)",
            code: FormatAccountId(accountId),
            details: new[]
            {
                ("Mã đơn hàng", Escape(orderId)),
                ("Dịch vụ", string.IsNullOrWhiteSpace(planName) ? "—" : Escape(planName))
            },
            footer: "Khi đăng nhập, bạn nhập mã tài khoản này (có hoặc không có dấu gạch đều được) rồi " +
                    "nhập mã OTP 6 chữ số mà hệ thống gửi về chính email này. Hoá đơn đầu kỳ sẽ được gửi sau.");

        return SendAsync(toEmail, $"[Nexus] Mã tài khoản {FormatAccountId(accountId)} — đường truyền đã sẵn sàng", body, ct);
    }

    /// <summary>
    /// 2c. Khách quên mã tài khoản — gửi lại mọi mã tài khoản gắn với email này.
    /// </summary>
    public Task<bool> SendAccountIdReminderAsync(
        string toEmail,
        string customerName,
        IReadOnlyList<string> accountIds,
        CancellationToken ct = default)
    {
        var formatted = accountIds.Select(FormatAccountId).ToList();

        var body = Layout(
            title: "Mã tài khoản của bạn",
            greeting: $"Chào {Escape(customerName)},",
            intro: "Bạn vừa yêu cầu gửi lại mã tài khoản Nexus Telecom. Dùng mã bên dưới để đăng nhập " +
                   "cổng khách hàng.",
            codeLabel: formatted.Count > 1 ? "CÁC MÃ TÀI KHOẢN" : "MÃ TÀI KHOẢN",
            code: string.Join(" · ", formatted),
            details: new[]
            {
                ("Số tài khoản", formatted.Count.ToString())
            },
            footer: "Nếu bạn không yêu cầu, hãy bỏ qua email này — không ai đăng nhập được nếu thiếu " +
                    "mã OTP gửi về chính hộp thư của bạn.");

        return SendAsync(toEmail, "[Nexus] Mã tài khoản đăng nhập của bạn", body, ct);
    }

    /// <summary>
    /// Mã mới cấp lưu trong CSDL dạng liền "T064000000000010"; hiển thị trong email dạng
    /// "T064-000000000010" cho dễ đọc. Màn hình đăng nhập chấp nhận cả hai dạng.
    /// </summary>
    public static string FormatAccountId(string accountId)
    {
        var key = new string((accountId ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return key.Length > 4 ? $"{key[..4]}-{key[4..]}" : key;
    }

    /// <summary>3. Khách nhập mã tài khoản ở màn hình đăng nhập — gửi OTP 6 chữ số.</summary>
    public Task<bool> SendLoginOtpAsync(
        string toEmail,
        string customerName,
        string otp,
        int validMinutes,
        CancellationToken ct = default)
    {
        var body = Layout(
            title: "Mã OTP đăng nhập",
            greeting: $"Chào {Escape(customerName)},",
            intro: "Đây là mã OTP dùng một lần để đăng nhập vào cổng khách hàng Nexus Telecom.",
            codeLabel: "MÃ OTP",
            code: otp,
            details: new[]
            {
                ("Hiệu lực", $"{validMinutes} phút kể từ lúc nhận email"),
                ("Số lần nhập tối đa", "5 lần")
            },
            footer: "Nếu bạn không yêu cầu đăng nhập, hãy bỏ qua email này — tài khoản của bạn " +
                    "vẫn an toàn vì không ai đăng nhập được nếu thiếu mã OTP.");

        return SendAsync(toEmail, $"[Nexus] Mã OTP đăng nhập: {otp}", body, ct);
    }

    // ==================== Hạ tầng gửi mail ====================

    /// <summary>
    /// Gửi một email. Không bao giờ ném lỗi ra ngoài: gửi mail thất bại thì chỉ ghi log,
    /// để không làm hỏng nghiệp vụ chính (đơn hàng, thanh toán) đã lưu xong trước đó.
    /// Trả về true nếu gửi (hoặc ghi ra file ở chế độ chưa cấu hình) thành công.
    /// </summary>
    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Bỏ qua gửi email \"{Subject}\": không có địa chỉ người nhận.", subject);
            return false;
        }

        if (!_settings.IsConfigured)
        {
            return await WriteToFileAsync(toEmail, subject, htmlBody, ct);
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.ResolvedFromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient
            {
                Timeout = _settings.TimeoutSeconds * 1000
            };

            // Cổng 465 dùng SSL ngay từ đầu, cổng 587 (Gmail) nâng cấp TLS bằng STARTTLS.
            var secureOption = _settings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_settings.Host, _settings.Port, secureOption, ct);
            await client.AuthenticateAsync(_settings.User, _settings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Đã gửi email \"{Subject}\" tới {To}.", subject, Mask(toEmail));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email \"{Subject}\" tới {To} thất bại.", subject, Mask(toEmail));
            return false;
        }
    }

    /// <summary>Chế độ chưa cấu hình SMTP: ghi email ra logs/emails để kiểm tra nội dung.</summary>
    private async Task<bool> WriteToFileAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "logs", "emails");
            Directory.CreateDirectory(folder);

            var safeSubject = string.Join("_", subject.Split(Path.GetInvalidFileNameChars()));
            var file = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{safeSubject}.html");

            await File.WriteAllTextAsync(
                file,
                $"<!-- To: {toEmail} | Subject: {subject} -->\n{htmlBody}",
                ct);

            _logger.LogWarning(
                "SMTP chưa cấu hình — đã ghi email \"{Subject}\" gửi tới {To} ra file {File}.",
                subject, Mask(toEmail), file);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không ghi được email ra file.");
            return false;
        }
    }

    // ==================== Dựng nội dung HTML ====================

    /// <summary>Khung email dùng chung cho cả 3 loại thư.</summary>
    private static string Layout(
        string title,
        string greeting,
        string intro,
        string codeLabel,
        string code,
        (string Label, string Value)[] details,
        string footer)
    {
        var rows = string.Join("", details.Select(d => $"""
            <tr>
              <td style="padding:6px 0;color:#537292;font-size:13px;">{d.Label}</td>
              <td style="padding:6px 0;color:#0F1D2B;font-size:13px;font-weight:600;text-align:right;">{d.Value}</td>
            </tr>
            """));

        return $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:24px 12px;background:#E0F1FF;font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 2px 12px rgba(15,29,43,.08);">
            <tr>
              <td style="background:#0369a1;padding:20px 24px;">
                <div style="color:#ffffff;font-size:18px;font-weight:700;">Nexus Telecom</div>
                <div style="color:#bae6fd;font-size:12px;margin-top:2px;">{title}</div>
              </td>
            </tr>
            <tr>
              <td style="padding:24px;">
                <p style="margin:0 0 12px;color:#0F1D2B;font-size:15px;font-weight:600;">{greeting}</p>
                <p style="margin:0 0 20px;color:#305070;font-size:14px;line-height:1.6;">{intro}</p>

                <div style="background:#EDF6FF;border:1px solid #CCE4F7;border-radius:12px;padding:16px;text-align:center;margin-bottom:20px;">
                  <div style="color:#537292;font-size:11px;letter-spacing:1px;font-weight:700;">{codeLabel}</div>
                  <div style="color:#0369a1;font-size:26px;font-weight:800;letter-spacing:2px;font-family:Consolas,Menlo,monospace;margin-top:6px;">{code}</div>
                </div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-top:1px solid #E2EFFB;">
                  {rows}
                </table>

                <p style="margin:20px 0 0;color:#537292;font-size:12px;line-height:1.6;">{footer}</p>
              </td>
            </tr>
            <tr>
              <td style="background:#F5FAFF;padding:14px 24px;color:#7899B8;font-size:11px;text-align:center;">
                Email tự động từ hệ thống Nexus Service Marketing System — vui lòng không trả lời thư này.
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }

    /// <summary>Chặn HTML injection khi chèn dữ liệu do người dùng nhập vào email.</summary>
    private static string Escape(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>Che bớt email khi ghi log để không lộ thông tin khách hàng.</summary>
    public static string Mask(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "(trống)";

        var at = email.IndexOf('@');
        if (at <= 0) return "***";

        var name = email[..at];
        var domain = email[at..];
        var visible = name.Length <= 2 ? name[..1] : name[..2];

        return $"{visible}{new string('*', Math.Max(1, name.Length - visible.Length))}{domain}";
    }
}
