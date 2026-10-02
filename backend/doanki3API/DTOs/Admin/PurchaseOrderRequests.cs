using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Admin;

public sealed class CreatePurchaseOrderRequest
{
    /// <summary>Bỏ trống thì server tự sinh mã PO-YYYYMMDD-xxxx.</summary>
    public string? PurchaseOrderId { get; set; }

    [Required(ErrorMessage = "Thiếu nhà cung cấp.")]
    public string VendorId { get; set; } = null!;

    /// <summary>Chi nhánh nhận hàng — bỏ trống nghĩa là kho trung tâm.</summary>
    public string? StoreId { get; set; }

    [Required(ErrorMessage = "Thiếu mã vật tư.")]
    [StringLength(50)]
    public string ItemCode { get; set; } = null!;

    [Required(ErrorMessage = "Thiếu tên vật tư.")]
    [StringLength(150)]
    public string ItemName { get; set; } = null!;

    [StringLength(50)]
    public string Category { get; set; } = "Modem";

    [Range(1, 100000, ErrorMessage = "Số lượng phải từ 1 trở lên.")]
    public int Quantity { get; set; }

    [Range(0, 1000000)]
    public decimal UnitCost { get; set; }

    public DateTime? ExpectedDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class UpdatePurchaseOrderStatusRequest
{
    /// <summary>Submitted | Received | Cancelled.</summary>
    [Required]
    public string Status { get; set; } = null!;
}
