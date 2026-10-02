using System.Globalization;
using doanki3API.Constants;
using doanki3API.DTOs.Notifications;

namespace doanki3API.Services.Notifications;

/// <summary>
/// Toàn bộ nội dung thông báo của hệ thống gom về một chỗ, dưới dạng extension method
/// để tầng nghiệp vụ gọi một dòng là xong: <c>await _notifications.OrderPlacedAsync(...)</c>.
///
/// Mỗi sự kiện tự quyết định gửi cho ai, dựa trên quy trình trong đề bài:
/// khách đặt đơn -> Bán lẻ duyệt -> Kỹ thuật khảo sát &amp; cấp kết nối -> Kế toán lập hoá đơn.
/// Người vừa thao tác không nhận lại thông báo của chính mình cho đỡ nhiễu.
/// </summary>
public static class NotificationEvents
{
    private static string Money(decimal value) =>
        "$" + value.ToString("N2", CultureInfo.InvariantCulture);

    private static string Short(string? text, int max = 90)
    {
        var value = (text ?? "").Trim();
        return value.Length > max ? value[..(max - 1)] + "…" : value;
    }

    // ---- 1. Khách đặt đơn mới ----
    // Đơn mới luôn nằm ở PendingRetail nên báo cho cửa hàng bán lẻ (trang Duyệt hồ sơ
    // chi nhánh); nhánh báo Kỹ thuật chỉ còn giữ lại cho trường hợp đơn đã qua duyệt.
    //
    // Ngoài khâu xử lý, còn báo cho:
    //  - Quản trị: theo dõi toàn hệ thống (trỏ về trang Tổng quan của Admin, không bắt đổi vai trò).
    //  - Chính khách hàng: nếu khách đã có mã tài khoản (đặt thêm gói trong cổng khách hàng)
    //    thì lưu một thông báo "đã tiếp nhận đơn" vào CSDL cho từng mã tài khoản của họ.
    public static Task OrderPlacedAsync(
        this NotificationService notifications,
        string orderId,
        string customerName,
        string? planName,
        bool needsRetailApproval,
        IReadOnlyCollection<string>? customerAccountIds = null,
        CancellationToken ct = default)
    {
        var planVi = string.IsNullOrWhiteSpace(planName) ? "." : $" — gói {planName}.";
        var planEn = string.IsNullOrWhiteSpace(planName) ? "." : $" — {planName} plan.";

        var handler = needsRetailApproval
            ? notifications.PublishAsync(new NotificationDraft
            {
                Type = "order",
                TitleVi = "Đơn hàng mới chờ duyệt hồ sơ",
                TitleEn = "New order awaiting paperwork approval",
                DescVi = $"Khách {customerName} vừa đặt đơn {orderId}{planVi}",
                DescEn = $"{customerName} just placed order {orderId}{planEn}",
                TargetPath = "/retail",
                TargetTab = "approval-queue",
                TargetRole = "retail",
                SectionVi = "Bán lẻ • Duyệt hồ sơ chi nhánh",
                SectionEn = "Retail • Branch Approval Queue",
                EntityId = orderId
            }, NotificationAudiences.Retail, ct)
            : notifications.PublishAsync(new NotificationDraft
            {
                Type = "order",
                TitleVi = "Đơn hàng mới cần khảo sát",
                TitleEn = "New order pending assessment",
                DescVi = $"Khách {customerName} vừa đặt đơn {orderId}{planVi}",
                DescEn = $"{customerName} just placed order {orderId}{planEn}",
                TargetPath = "/technical",
                TargetTab = "feasibility-queue",
                TargetRole = "technical",
                SectionVi = "Kỹ thuật • Hàng đợi khảo sát",
                SectionEn = "Technical • Feasibility Queue",
                EntityId = orderId
            }, NotificationAudiences.Technical, ct);

        var admin = notifications.PublishAsync(new NotificationDraft
        {
            Type = "order",
            TitleVi = "Có đơn đăng ký dịch vụ mới",
            TitleEn = "New service order received",
            DescVi = $"Khách {customerName} vừa đặt đơn {orderId}{planVi}",
            DescEn = $"{customerName} just placed order {orderId}{planEn}",
            TargetPath = "/admin",
            TargetTab = "overview",
            TargetRole = "admin",
            SectionVi = "Quản trị • Tổng quan",
            SectionEn = "Admin • Overview",
            EntityId = orderId
        }, NotificationAudiences.Admin, ct);

        var customerAudiences = (customerAccountIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(NotificationAudiences.ForAccount)
            .ToList();

        var customer = customerAudiences.Count == 0
            ? Task.CompletedTask
            : notifications.PublishAsync(new NotificationDraft
            {
                Type = "order",
                TitleVi = "Đã tiếp nhận đơn đăng ký của bạn",
                TitleEn = "Your order has been received",
                DescVi = $"Đơn {orderId}{planVi.TrimEnd('.')} đang chờ chi nhánh duyệt hồ sơ, sau đó kỹ thuật sẽ khảo sát hạ tầng.",
                DescEn = $"Order {orderId}{planEn.TrimEnd('.')} is awaiting branch paperwork review, then a technical survey.",
                TargetPath = "/user",
                TargetTab = "new-service",
                TargetRole = "user",
                SectionVi = "Khách hàng • Đăng ký gói mới",
                SectionEn = "Customer • New Service",
                EntityId = orderId
            }, customerAudiences, ct);

        return Task.WhenAll(handler, admin, customer);
    }

    // ---- 2. Bán lẻ duyệt xong -> kỹ thuật vào khảo sát ----
    public static Task OrderApprovedByRetailAsync(
        this NotificationService notifications,
        string orderId,
        string? approvedBy,
        CancellationToken ct = default) =>
        notifications.PublishAsync(new NotificationDraft
        {
            Type = "order",
            TitleVi = "Đơn đã duyệt, chờ khảo sát kỹ thuật",
            TitleEn = "Order approved, awaiting feasibility check",
            DescVi = $"Đơn {orderId} đã qua khâu hồ sơ"
                + (string.IsNullOrWhiteSpace(approvedBy) ? "" : $" (duyệt bởi {approvedBy})")
                + " và đang chờ kiểm tra khả thi.",
            DescEn = $"Order {orderId} cleared paperwork"
                + (string.IsNullOrWhiteSpace(approvedBy) ? "" : $" (approved by {approvedBy})")
                + " and is waiting for a feasibility check.",
            TargetPath = "/technical",
            TargetTab = "feasibility-queue",
            TargetRole = "technical",
            SectionVi = "Kỹ thuật • Hàng đợi khảo sát",
            SectionEn = "Technical • Feasibility Queue",
            EntityId = orderId
        }, NotificationAudiences.Technical, ct);

    // ---- 3. Bán lẻ từ chối hồ sơ ----
    public static Task OrderRejectedByRetailAsync(
        this NotificationService notifications,
        string orderId,
        string reason,
        string? accountId,
        CancellationToken ct = default)
    {
        // Khách chỉ nhận được thông báo khi đã có mã tài khoản để đăng nhập cổng khách hàng.
        var audiences = new List<string>();
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            audiences.Add(NotificationAudiences.ForAccount(accountId));
        }

        if (audiences.Count == 0) return Task.CompletedTask;

        return notifications.PublishAsync(new NotificationDraft
        {
            Type = "alert",
            TitleVi = "Hồ sơ đăng ký chưa được duyệt",
            TitleEn = "Application was not approved",
            DescVi = $"Đơn {orderId} bị trả lại: {Short(reason)}",
            DescEn = $"Order {orderId} was returned: {Short(reason)}",
            TargetPath = "/user",
            TargetTab = "overview",
            TargetRole = "user",
            SectionVi = "Khách hàng • Tổng quan",
            SectionEn = "Customer • Overview",
            EntityId = orderId
        }, audiences, ct);
    }

    // ---- 4. Hồ sơ được nộp lại sau khi bị trả ----
    public static Task OrderResubmittedAsync(
        this NotificationService notifications,
        string orderId,
        CancellationToken ct = default) =>
        notifications.PublishAsync(new NotificationDraft
        {
            Type = "order",
            TitleVi = "Hồ sơ nộp lại chờ duyệt",
            TitleEn = "Application resubmitted for approval",
            DescVi = $"Đơn {orderId} đã bổ sung giấy tờ và quay lại hàng đợi duyệt.",
            DescEn = $"Order {orderId} was amended and is back in the approval queue.",
            TargetPath = "/retail",
            TargetTab = "approval-queue",
            TargetRole = "retail",
            SectionVi = "Bán lẻ • Duyệt hồ sơ chi nhánh",
            SectionEn = "Retail • Branch Approval Queue",
            EntityId = orderId
        }, NotificationAudiences.Retail, ct);

    // ---- 5. Phân công kỹ thuật viên đi khảo sát ----
    public static Task TechnicianAssignedAsync(
        this NotificationService notifications,
        string orderId,
        string technicianName,
        CancellationToken ct = default) =>
        notifications.PublishAsync(new NotificationDraft
        {
            Type = "order",
            TitleVi = "Có lịch khảo sát mới được phân công",
            TitleEn = "New survey assignment",
            DescVi = $"{technicianName} được phân công khảo sát đơn {orderId}.",
            DescEn = $"{technicianName} has been assigned to survey order {orderId}.",
            TargetPath = "/technical",
            TargetTab = "feasibility-queue",
            TargetRole = "technical",
            SectionVi = "Kỹ thuật • Hàng đợi khảo sát",
            SectionEn = "Technical • Feasibility Queue",
            EntityId = orderId
        }, NotificationAudiences.Technical, ct);

    // ---- 6. Kỹ thuật kết luận khả thi / không khả thi ----
    public static Task OrderFeasibilityAsync(
        this NotificationService notifications,
        string orderId,
        bool feasible,
        string? accountId,
        string? reason,
        CancellationToken ct = default)
    {
        var draft = feasible
            ? new NotificationDraft
            {
                Type = "order",
                TitleVi = "Đơn đã khảo sát khả thi",
                TitleEn = "Order confirmed feasible",
                DescVi = $"Đơn {orderId} đủ điều kiện lắp đặt"
                    + (string.IsNullOrWhiteSpace(accountId) ? "." : $", mã tài khoản cấp cho khách: {accountId}."),
                DescEn = $"Order {orderId} is ready for installation"
                    + (string.IsNullOrWhiteSpace(accountId) ? "." : $", account ID issued: {accountId}."),
                TargetPath = "/retail",
                TargetTab = "order-tracking",
                TargetRole = "retail",
                SectionVi = "Bán lẻ • Tra cứu đơn hàng",
                SectionEn = "Retail • Order Tracking",
                EntityId = orderId
            }
            : new NotificationDraft
            {
                Type = "alert",
                TitleVi = "Đơn không khả thi về kỹ thuật",
                TitleEn = "Order is not technically feasible",
                DescVi = $"Đơn {orderId} không lắp được: {Short(reason ?? "kỹ thuật không nêu lý do")}",
                DescEn = $"Order {orderId} cannot be installed: {Short(reason ?? "no reason given")}",
                TargetPath = "/retail",
                TargetTab = "order-tracking",
                TargetRole = "retail",
                SectionVi = "Bán lẻ • Tra cứu đơn hàng",
                SectionEn = "Retail • Order Tracking",
                EntityId = orderId
            };

        return notifications.PublishAsync(draft, NotificationAudiences.Retail, ct);
    }

    // ---- 7. Cấp kết nối xong -> kế toán lập hoá đơn, khách được báo ----
    public static Task ConnectionProvidedAsync(
        this NotificationService notifications,
        string accountId,
        string customerName,
        string? planName,
        CancellationToken ct = default)
    {
        var staff = notifications.PublishAsync(new NotificationDraft
        {
            Type = "order",
            TitleVi = "Đã cấp kết nối, chờ lập hoá đơn",
            TitleEn = "Connection provided, invoice pending",
            DescVi = $"Tài khoản {accountId} của {customerName} đã đấu nối xong"
                + (string.IsNullOrWhiteSpace(planName) ? "." : $" (gói {planName}).")
                + " Cần lập hoá đơn đầu kỳ.",
            DescEn = $"Account {accountId} for {customerName} is now live"
                + (string.IsNullOrWhiteSpace(planName) ? "." : $" ({planName}).")
                + " First invoice is due.",
            TargetPath = "/accounts",
            TargetTab = "bill-generation",
            TargetRole = "accounts",
            SectionVi = "Kế toán • Lập hoá đơn",
            SectionEn = "Accounts • Bill Generation",
            EntityId = accountId
        }, NotificationAudiences.Accounts, ct);

        var customer = notifications.PublishAsync(new NotificationDraft
        {
            Type = "system",
            TitleVi = "Đường truyền của bạn đã sẵn sàng",
            TitleEn = "Your connection is live",
            DescVi = $"Kết nối cho mã tài khoản {accountId} đã được kích hoạt"
                + (string.IsNullOrWhiteSpace(planName) ? "." : $" với gói {planName}."),
            DescEn = $"Your connection for account {accountId} is active"
                + (string.IsNullOrWhiteSpace(planName) ? "." : $" on the {planName} plan."),
            TargetPath = "/user",
            TargetTab = "overview",
            TargetRole = "user",
            SectionVi = "Khách hàng • Tổng quan",
            SectionEn = "Customer • Overview",
            EntityId = accountId
        }, NotificationAudiences.ForAccount(accountId), ct);

        return Task.WhenAll(staff, customer);
    }

    // ---- 8. Kế toán lập hoá đơn ----
    public static Task BillCreatedAsync(
        this NotificationService notifications,
        string invoiceNumber,
        string accountId,
        string customerName,
        decimal totalAmount,
        CancellationToken ct = default)
    {
        var retail = notifications.PublishAsync(new NotificationDraft
        {
            Type = "billing",
            TitleVi = "Hoá đơn mới cần thu tiền",
            TitleEn = "New invoice ready for collection",
            DescVi = $"Hoá đơn {invoiceNumber} của {customerName} ({accountId}) trị giá {Money(totalAmount)}.",
            DescEn = $"Invoice {invoiceNumber} for {customerName} ({accountId}) totals {Money(totalAmount)}.",
            TargetPath = "/retail",
            TargetTab = "payment-records",
            TargetRole = "retail",
            SectionVi = "Bán lẻ • Thu tiền tại quầy",
            SectionEn = "Retail • Payment Records",
            EntityId = invoiceNumber
        }, NotificationAudiences.Retail, ct);

        var customer = notifications.PublishAsync(new NotificationDraft
        {
            Type = "billing",
            TitleVi = "Bạn có hoá đơn mới",
            TitleEn = "You have a new invoice",
            DescVi = $"Hoá đơn {invoiceNumber} trị giá {Money(totalAmount)} vừa được phát hành.",
            DescEn = $"Invoice {invoiceNumber} for {Money(totalAmount)} has been issued.",
            TargetPath = "/user",
            TargetTab = "billing",
            TargetRole = "user",
            SectionVi = "Khách hàng • Hoá đơn",
            SectionEn = "Customer • Billing",
            EntityId = invoiceNumber
        }, NotificationAudiences.ForAccount(accountId), ct);

        return Task.WhenAll(retail, customer);
    }

    // ---- 9. Ghi nhận thanh toán ----
    public static Task PaymentRecordedAsync(
        this NotificationService notifications,
        string invoiceNumber,
        string accountId,
        string customerName,
        decimal amountPaid,
        bool fullyPaid,
        CancellationToken ct = default)
    {
        var staff = notifications.PublishAsync(new NotificationDraft
        {
            Type = "billing",
            TitleVi = fullyPaid ? "Hoá đơn đã thanh toán đủ" : "Vừa nhận thanh toán một phần",
            TitleEn = fullyPaid ? "Invoice fully settled" : "Partial payment received",
            DescVi = $"{customerName} ({accountId}) thanh toán {Money(amountPaid)} cho hoá đơn {invoiceNumber}.",
            DescEn = $"{customerName} ({accountId}) paid {Money(amountPaid)} for invoice {invoiceNumber}.",
            TargetPath = "/accounts",
            TargetTab = "subscriber-tracking",
            TargetRole = "accounts",
            SectionVi = "Kế toán • Theo dõi thuê bao",
            SectionEn = "Accounts • Subscriber Tracking",
            EntityId = invoiceNumber
        }, NotificationAudiences.Accounts, ct);

        var customer = notifications.PublishAsync(new NotificationDraft
        {
            Type = "billing",
            TitleVi = "Đã ghi nhận thanh toán của bạn",
            TitleEn = "Your payment was recorded",
            DescVi = fullyPaid
                ? $"Hoá đơn {invoiceNumber} đã thanh toán đủ {Money(amountPaid)}. Cảm ơn quý khách."
                : $"Đã nhận {Money(amountPaid)} cho hoá đơn {invoiceNumber}, phần còn lại vui lòng thanh toán trước hạn.",
            DescEn = fullyPaid
                ? $"Invoice {invoiceNumber} is fully settled ({Money(amountPaid)}). Thank you."
                : $"We received {Money(amountPaid)} for invoice {invoiceNumber}; the balance is still due.",
            TargetPath = "/user",
            TargetTab = "billing",
            TargetRole = "user",
            SectionVi = "Khách hàng • Hoá đơn",
            SectionEn = "Customer • Billing",
            EntityId = invoiceNumber
        }, NotificationAudiences.ForAccount(accountId), ct);

        return Task.WhenAll(staff, customer);
    }

    // ---- 10. Kho chạm ngưỡng đặt lại ----
    // ReplaceKey theo từng vật tư: nhập/xuất liên tục cũng chỉ còn một thông báo mới nhất.
    public static Task LowStockAsync(
        this NotificationService notifications,
        string itemId,
        string itemName,
        string itemCode,
        int stockQuantity,
        int reorderLevel,
        CancellationToken ct = default) =>
        notifications.PublishAsync(new NotificationDraft
        {
            Type = "stock",
            TitleVi = "Vật tư cần bổ sung kho",
            TitleEn = "Stock needs restocking",
            DescVi = $"{itemName} ({itemCode}) chỉ còn {stockQuantity}/{reorderLevel} đơn vị — đã chạm ngưỡng đặt lại.",
            DescEn = $"{itemName} ({itemCode}) down to {stockQuantity}/{reorderLevel} units — reorder threshold reached.",
            TargetPath = "/admin",
            TargetTab = "stock",
            TargetRole = "admin",
            SectionVi = "Quản trị • Kho thiết bị",
            SectionEn = "Admin • Stock Inventory",
            EntityId = itemId,
            ReplaceKey = $"stock:{itemId}"
        }, NotificationAudiences.Admin, ct);

    // ---- 11. Khách nhắn tin ở chatbox trang chủ ----
    public static Task ChatMessageFromGuestAsync(
        this NotificationService notifications,
        string sessionId,
        string customerName,
        string message,
        CancellationToken ct = default) =>
        notifications.PublishAsync(new NotificationDraft
        {
            Type = "chat",
            TitleVi = $"Tin nhắn mới từ {customerName}",
            TitleEn = $"New message from {customerName}",
            DescVi = Short(message),
            DescEn = Short(message),
            TargetPath = "/admin",
            TargetTab = "livechat",
            TargetRole = "admin",
            SectionVi = "Quản trị • Chat trực tuyến",
            SectionEn = "Admin • Live Chat",
            EntityId = sessionId,
            // Khách nhắn 10 câu liền vẫn chỉ hiện một dòng thông báo cho phiên đó.
            ReplaceKey = $"chat:{sessionId}"
        }, NotificationAudiences.Admin, ct);

    // ---- 12. Khách gửi phản hồi / đánh giá ----
    public static Task FeedbackCreatedAsync(
        this NotificationService notifications,
        string feedbackId,
        string customerName,
        int rating,
        string message,
        CancellationToken ct = default) =>
        notifications.PublishAsync(new NotificationDraft
        {
            Type = "feedback",
            TitleVi = "Phản hồi khách hàng chờ trả lời",
            TitleEn = "Customer feedback awaiting reply",
            DescVi = $"{customerName} ({new string('★', Math.Clamp(rating, 0, 5))}): {Short(message, 70)}",
            DescEn = $"{customerName} ({new string('★', Math.Clamp(rating, 0, 5))}): {Short(message, 70)}",
            TargetPath = "/admin",
            TargetTab = "feedback",
            TargetRole = "admin",
            SectionVi = "Quản trị • Phản hồi khách hàng",
            SectionEn = "Admin • Customer Feedback",
            EntityId = feedbackId
        }, NotificationAudiences.Admin, ct);

    // ---- 13. Quản trị trả lời phản hồi -> báo lại cho khách ----
    public static Task FeedbackAnsweredAsync(
        this NotificationService notifications,
        string feedbackId,
        string? accountId,
        string response,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return Task.CompletedTask;

        return notifications.PublishAsync(new NotificationDraft
        {
            Type = "feedback",
            TitleVi = "Phản hồi của bạn đã được trả lời",
            TitleEn = "Your feedback has a reply",
            DescVi = Short(response),
            DescEn = Short(response),
            TargetPath = "/user",
            TargetTab = "feedback",
            TargetRole = "user",
            SectionVi = "Khách hàng • Phản hồi",
            SectionEn = "Customer • Feedback",
            EntityId = feedbackId
        }, NotificationAudiences.ForAccount(accountId), ct);
    }

    // ---- 14. Đổi trạng thái đường truyền (tạm ngưng / khôi phục / cắt) ----
    public static Task ConnectionStatusChangedAsync(
        this NotificationService notifications,
        string accountId,
        string customerName,
        string status,
        string? reason,
        CancellationToken ct = default)
    {
        // Đúng 3 giá trị của CK_CustomerConnection_Status (xem Constants/ConnectionStatuses.cs).
        var statusVi = status switch
        {
            ConnectionStatuses.Active => "đang hoạt động",
            ConnectionStatuses.TemporarilyInactive => "tạm ngưng",
            ConnectionStatuses.PermanentlyInactive => "ngừng vĩnh viễn",
            _ => status.ToLowerInvariant()
        };

        var customer = notifications.PublishAsync(new NotificationDraft
        {
            Type = "alert",
            TitleVi = $"Đường truyền chuyển sang trạng thái {statusVi}",
            TitleEn = $"Your connection is now {status}",
            DescVi = $"Tài khoản {accountId}"
                + (string.IsNullOrWhiteSpace(reason) ? "." : $": {Short(reason)}"),
            DescEn = $"Account {accountId}"
                + (string.IsNullOrWhiteSpace(reason) ? "." : $": {Short(reason)}"),
            TargetPath = "/user",
            TargetTab = "overview",
            TargetRole = "user",
            SectionVi = "Khách hàng • Tổng quan",
            SectionEn = "Customer • Overview",
            EntityId = accountId
        }, NotificationAudiences.ForAccount(accountId), ct);

        var staff = notifications.PublishAsync(new NotificationDraft
        {
            Type = "alert",
            TitleVi = $"Thuê bao {accountId} {statusVi}",
            TitleEn = $"Subscriber {accountId} is now {status}",
            DescVi = $"{customerName}"
                + (string.IsNullOrWhiteSpace(reason) ? " vừa đổi trạng thái đường truyền." : $": {Short(reason)}"),
            DescEn = $"{customerName}"
                + (string.IsNullOrWhiteSpace(reason) ? " changed connection status." : $": {Short(reason)}"),
            TargetPath = "/accounts",
            TargetTab = "subscriber-tracking",
            TargetRole = "accounts",
            SectionVi = "Kế toán • Theo dõi thuê bao",
            SectionEn = "Accounts • Subscriber Tracking",
            EntityId = accountId
        }, NotificationAudiences.Accounts, ct);

        return Task.WhenAll(customer, staff);
    }
}
