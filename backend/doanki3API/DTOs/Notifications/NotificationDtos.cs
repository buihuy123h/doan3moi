namespace doanki3API.DTOs.Notifications;

/// <summary>
/// Thông báo trả về cho frontend (khớp interface NotificationDto bên Svelte).
/// Thời gian để dạng ISO, phần "5 phút trước / 5 mins ago" do frontend tự dựng
/// theo ngôn ngữ đang chọn và tự trôi theo đồng hồ.
/// </summary>
public sealed record NotificationDto(
    string Id,
    string Audience,
    string Type,
    string TitleVi,
    string TitleEn,
    string DescVi,
    string DescEn,
    string TargetPath,
    string TargetTab,
    string? TargetRole,
    string SectionVi,
    string SectionEn,
    string? EntityId,
    string? ReplaceKey,
    bool Read,
    string CreatedAt);

/// <summary>
/// Nội dung một thông báo trước khi biết gửi cho ai — tầng nghiệp vụ dựng record này
/// rồi đưa cho NotificationService phát tới một hoặc nhiều người nhận.
/// </summary>
public sealed record NotificationDraft
{
    public required string Type { get; init; }
    public required string TitleVi { get; init; }
    public required string TitleEn { get; init; }
    public required string DescVi { get; init; }
    public required string DescEn { get; init; }
    public required string TargetPath { get; init; }
    public string TargetTab { get; init; } = "";
    public string? TargetRole { get; init; }
    public required string SectionVi { get; init; }
    public required string SectionEn { get; init; }
    public string? EntityId { get; init; }
    public string? ReplaceKey { get; init; }
}

/// <summary>Số thông báo chưa đọc — dùng cho chấm đỏ trên chuông.</summary>
public sealed record NotificationCountDto(int Unread, int Total);
