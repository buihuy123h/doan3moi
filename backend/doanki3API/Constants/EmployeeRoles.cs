namespace doanki3API.Constants;

/// <summary>
/// Giá trị của cột Employee.Role trong DB (xem NexusSystem_Database.sql) — dùng cho
/// [Authorize(Roles = ...)] nên phải khớp chính xác chuỗi lưu trong bảng Employee.
/// </summary>
public static class EmployeeRoles
{
    public const string Manager = "Manager";
    public const string RetailStaff = "Retail Staff";
    public const string FieldEngineer = "Field Engineer";
    public const string Accounts = "Senior Accountant";

    /// <summary>
    /// Role trong JWT của khách hàng (xem AuthController.Login / CustomerOtpController.VerifyOtp /
    /// AuthController.Register). Khác 4 role nhân viên ở trên nên đặt chung file này để tra cứu.
    /// </summary>
    public const string Customer = "Customer";

    /// <summary>Được phép ĐẶT ĐƠN: retail outlet (spec), Manager (admin) và khách self-service.</summary>
    public const string PlaceOrder = RetailStaff + ", " + Manager + ", " + Customer;

    /// <summary>
    /// Được phép ĐỔI trạng thái đơn: Retail Staff (duyệt/từ chối ở cửa hàng — bước
    /// PendingRetail → Pending/Not Approved) và Field Engineer (feasible/provided).
    /// </summary>
    public const string UpdateOrderStatus = RetailStaff + ", " + FieldEngineer + ", " + Manager;

    /// <summary>
    /// Các thao tác CHỈ kỹ thuật: feasibility, lên lịch lắp, provision kết nối,
    /// đổi trạng thái kết nối, CRUD thiết bị, điều tồn kho.
    /// </summary>
    public const string TechnicalWrite = FieldEngineer + ", " + Manager;

    /// <summary>
    /// Được ghi nhận thanh toán qua /api/retail/payments: nhân viên cửa hàng (spec),
    /// Manager, kế toán và khách tự trả online (bị chặn theo quyền sở hữu hóa đơn).
    /// </summary>
    public const string RecordRetailPayment = RetailStaff + ", " + Manager + ", " + Accounts + ", " + Customer;

    public static readonly string[] All =
    [
        Manager,
        RetailStaff,
        FieldEngineer,
        Accounts
    ];
}
