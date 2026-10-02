namespace doanki3API.Constants;

/// <summary>
/// Trạng thái của đơn hàng — phải khớp cả CK_Orders_Status trong DB
/// lẫn type OrderStatus bên frontend (doanki3fe/src/types/nexus.ts).
/// </summary>
public static class OrderStatuses
{
    /// <summary>Vừa nộp, đang chờ Cửa hàng bán lẻ duyệt hồ sơ.</summary>
    public const string PendingRetail = "PendingRetail";

    /// <summary>Cửa hàng bán lẻ từ chối hồ sơ.</summary>
    public const string NotApproved = "Not Approved";

    /// <summary>Đã qua bán lẻ, đang chờ Kỹ thuật khảo sát tính khả thi.</summary>
    public const string Pending = "Pending";

    public const string Feasible = "Feasible";
    public const string NotFeasible = "Not Feasible";
    public const string ConnectionProvided = "Connection Provided";

    public static readonly string[] All =
    [
        PendingRetail,
        NotApproved,
        Pending,
        Feasible,
        NotFeasible,
        ConnectionProvided
    ];

    /// <summary>Các trạng thái còn thuộc khâu Cửa hàng bán lẻ.</summary>
    public static readonly string[] RetailStage =
    [
        PendingRetail,
        NotApproved
    ];
}
