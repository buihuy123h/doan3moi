namespace doanki3API.Constants;

/// <summary>
/// Khoá "người nhận" của một thông báo. Cũng chính là tên group trên SignalR,
/// nên đặt tên ở một chỗ để backend và frontend không lệch nhau.
/// </summary>
public static class NotificationAudiences
{
    public const string Admin = "role:admin";
    public const string Retail = "role:retail";
    public const string Technical = "role:technical";
    public const string Accounts = "role:accounts";

    /// <summary>Các role nhân viên hợp lệ khi client xin vào group.</summary>
    public static readonly string[] StaffRoles = ["admin", "retail", "technical", "accounts"];

    /// <summary>Group của một role nhân viên: "admin" -> "role:admin".</summary>
    public static string? ForRole(string? role)
    {
        var value = role?.Trim().ToLowerInvariant();
        return StaffRoles.Contains(value) ? $"role:{value}" : null;
    }

    /// <summary>
    /// Group của một khách hàng theo mã tài khoản. Bỏ hết dấu gạch và viết hoa
    /// vì giao diện hiển thị "B064-000000000002" còn CSDL lưu "B064000000000002".
    /// </summary>
    public static string ForAccount(string accountId) =>
        $"account:{NormalizeAccountId(accountId)}";

    /// <summary>Group riêng của một phiên chat — khách vãng lai nghe tin nhắn trả lời ở đây.</summary>
    public static string ForChatSession(string sessionId) =>
        $"chat:{sessionId.Trim()}";

    public static string NormalizeAccountId(string accountId) =>
        new string((accountId ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
