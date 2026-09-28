using System.Security.Cryptography;
using System.Text;
using doanki3API.Services.Chat;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace doanki3API.Services.Email;

/// <summary>Một mã OTP đăng nhập — lưu trong MongoDB, collection "login_otps".</summary>
public sealed class LoginOtpDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>Mã tài khoản đã chuẩn hoá (bỏ dấu gạch, viết hoa).</summary>
    public string AccountKey { get; set; } = null!;

    public string Email { get; set; } = "";

    public string CustomerName { get; set; } = "";

    /// <summary>Chỉ lưu SHA-256 của mã, không lưu mã gốc.</summary>
    public string CodeHash { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    /// <summary>MongoDB tự xoá document khi quá thời điểm này (TTL index).</summary>
    public DateTime ExpiresAt { get; set; }

    public int Attempts { get; set; }

    public bool Used { get; set; }
}

/// <summary>Kết quả kiểm tra OTP.</summary>
public enum OtpVerifyResult
{
    Success,
    NotFound,
    Expired,
    TooManyAttempts,
    Incorrect
}

/// <summary>
/// Sinh và kiểm tra mã OTP đăng nhập cho khách hàng.
/// Dùng chung MongoDB với module chat (xem <see cref="MongoSettings"/>) — OTP là dữ liệu
/// ngắn hạn, hợp với TTL index của Mongo hơn là thêm bảng vào SQL Server.
/// </summary>
public sealed class LoginOtpService
{
    /// <summary>Hiệu lực của một mã OTP.</summary>
    public const int ValidMinutes = 5;

    /// <summary>Số lần nhập sai tối đa trước khi mã bị vô hiệu.</summary>
    private const int MaxAttempts = 5;

    /// <summary>Khoảng cách tối thiểu giữa 2 lần xin mã, tránh spam hộp thư khách.</summary>
    public const int ResendCooldownSeconds = 60;

    private readonly IMongoCollection<LoginOtpDocument> _otps;
    private readonly ILogger<LoginOtpService> _logger;

    public LoginOtpService(IOptions<MongoSettings> options, ILogger<LoginOtpService> logger)
    {
        _logger = logger;
        var settings = options.Value;

        var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.Database);
        _otps = database.GetCollection<LoginOtpDocument>("login_otps");

        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        try
        {
            // TTL index: Mongo tự dọn mã hết hạn, không cần job xoá thủ công.
            _otps.Indexes.CreateOne(new CreateIndexModel<LoginOtpDocument>(
                Builders<LoginOtpDocument>.IndexKeys.Ascending(o => o.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));

            _otps.Indexes.CreateOne(new CreateIndexModel<LoginOtpDocument>(
                Builders<LoginOtpDocument>.IndexKeys
                    .Ascending(o => o.AccountKey)
                    .Descending(o => o.CreatedAt)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không tạo được index cho collection login_otps.");
        }
    }

    /// <summary>Chuẩn hoá mã tài khoản: "t064-000000000001" và "T064000000000001" là một.</summary>
    public static string NormalizeAccountId(string? accountId) =>
        new string((accountId ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>
    /// Kiểm tra khách có vừa xin mã cách đây chưa tới <see cref="ResendCooldownSeconds"/> giây không.
    /// Trả về số giây còn phải chờ, hoặc 0 nếu được phép xin mã mới.
    /// </summary>
    public async Task<int> GetResendWaitSecondsAsync(string accountId, CancellationToken ct = default)
    {
        var key = NormalizeAccountId(accountId);

        var latest = await _otps
            .Find(o => o.AccountKey == key && !o.Used)
            .SortByDescending(o => o.CreatedAt)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        if (latest is null) return 0;

        var elapsed = (DateTime.UtcNow - latest.CreatedAt).TotalSeconds;
        var remaining = ResendCooldownSeconds - (int)elapsed;

        return remaining > 0 ? remaining : 0;
    }

    /// <summary>
    /// Sinh mã OTP mới cho một tài khoản và vô hiệu hoá các mã chưa dùng trước đó.
    /// Trả về mã gốc 6 chữ số để gửi email (mã gốc không được lưu vào CSDL).
    /// </summary>
    public async Task<string> CreateAsync(
        string accountId,
        string email,
        string customerName,
        CancellationToken ct = default)
    {
        var key = NormalizeAccountId(accountId);
        var now = DateTime.UtcNow;

        // Xin mã mới thì mã cũ hết hiệu lực ngay, tránh việc nhiều mã cùng dùng được.
        await _otps.UpdateManyAsync(
            o => o.AccountKey == key && !o.Used,
            Builders<LoginOtpDocument>.Update.Set(o => o.Used, true),
            cancellationToken: ct);

        var code = GenerateCode();

        await _otps.InsertOneAsync(new LoginOtpDocument
        {
            Id = ObjectId.GenerateNewId(),
            AccountKey = key,
            Email = email,
            CustomerName = customerName,
            CodeHash = Hash(code),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(ValidMinutes),
            Attempts = 0,
            Used = false
        }, cancellationToken: ct);

        return code;
    }

    /// <summary>Kiểm tra mã khách nhập. Đúng thì đánh dấu đã dùng để không tái sử dụng được.</summary>
    public async Task<OtpVerifyResult> VerifyAsync(
        string accountId,
        string code,
        CancellationToken ct = default)
    {
        var key = NormalizeAccountId(accountId);

        var otp = await _otps
            .Find(o => o.AccountKey == key && !o.Used)
            .SortByDescending(o => o.CreatedAt)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        if (otp is null) return OtpVerifyResult.NotFound;

        if (otp.ExpiresAt <= DateTime.UtcNow) return OtpVerifyResult.Expired;

        if (otp.Attempts >= MaxAttempts)
        {
            await _otps.UpdateOneAsync(
                o => o.Id == otp.Id,
                Builders<LoginOtpDocument>.Update.Set(o => o.Used, true),
                cancellationToken: ct);

            return OtpVerifyResult.TooManyAttempts;
        }

        // So sánh theo thời gian cố định để không lộ thông tin qua độ trễ phản hồi.
        var matched = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(code ?? "")),
            Encoding.UTF8.GetBytes(otp.CodeHash));

        if (!matched)
        {
            await _otps.UpdateOneAsync(
                o => o.Id == otp.Id,
                Builders<LoginOtpDocument>.Update.Inc(o => o.Attempts, 1),
                cancellationToken: ct);

            return OtpVerifyResult.Incorrect;
        }

        await _otps.UpdateOneAsync(
            o => o.Id == otp.Id,
            Builders<LoginOtpDocument>.Update.Set(o => o.Used, true),
            cancellationToken: ct);

        return OtpVerifyResult.Success;
    }

    /// <summary>Sinh 6 chữ số bằng bộ sinh ngẫu nhiên an toàn mật mã.</summary>
    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
