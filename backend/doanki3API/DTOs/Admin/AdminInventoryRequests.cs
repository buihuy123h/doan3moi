using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Admin
{
    public sealed class UpdateAdminInventoryRequest
    {
        [StringLength(50)]
        public string? ItemCode { get; set; }

        [Required(ErrorMessage = "Tên vật tư thiết bị không được để trống")]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Danh mục không được để trống")]
        [StringLength(50)]
        public string Category { get; set; } = null!;

        [Range(0, 1000000, ErrorMessage = "Số lượng tồn kho phải >= 0")]
        public int StockQuantity { get; set; }

        [Range(0, 1000000, ErrorMessage = "Ngưỡng đặt hàng lại phải >= 0")]
        public int ReorderLevel { get; set; }

        [Range(0, 1000000000, ErrorMessage = "Đơn giá phải >= 0")]
        public decimal UnitCost { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(50)]
        public string? VendorId { get; set; }

        [StringLength(150)]
        public string? Supplier { get; set; }

        [StringLength(30)]
        public string? Status { get; set; }
    }

    public sealed class RestockInventoryRequest
    {
        [Required]
        [Range(1, 100000, ErrorMessage = "Số lượng nhập thêm phải lớn hơn 0")]
        public int Quantity { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }
    }

    public sealed class CreateAdminInventoryRequest
    {
        [StringLength(50)]
        public string? ItemCode { get; set; }

        [Required(ErrorMessage = "Tên vật tư thiết bị không được để trống")]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Danh mục không được để trống")]
        [StringLength(50)]
        public string Category { get; set; } = "Modem";

        [Range(0, 1000000)]
        public int StockQuantity { get; set; } = 10;

        [Range(0, 1000000)]
        public int ReorderLevel { get; set; } = 5;

        [Range(0, 1000000000)]
        public decimal UnitCost { get; set; } = 50;

        [StringLength(100)]
        public string? Location { get; set; } = "Central Depot NYC";

        [StringLength(50)]
        public string? VendorId { get; set; }

        [StringLength(150)]
        public string? Supplier { get; set; }
    }
}
