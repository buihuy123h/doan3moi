using System;
using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Admin
{
    public sealed class UpdateEmployeeRequest
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Chức vụ không được để trống")]
        [StringLength(50)]
        public string Role { get; set; } = null!;

        [Required(ErrorMessage = "Phòng ban không được để trống")]
        [StringLength(50)]
        public string Department { get; set; } = null!;

        [StringLength(20)]
        public string? StoreId { get; set; }

        [StringLength(150)]
        public string? RetailShopAssigned { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public string? DateOfJoining { get; set; }

        /// <summary>
        /// Mật khẩu mới nếu muốn cập nhật. Để trống hoặc null nếu giữ nguyên mật khẩu cũ.
        /// </summary>
        public string? Password { get; set; }
    }

    public sealed class CreateEmployeeRequest
    {
        [StringLength(30)]
        public string? EmployeeCode { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Chức vụ không được để trống")]
        [StringLength(50)]
        public string Role { get; set; } = "Retail Staff";

        [Required(ErrorMessage = "Phòng ban không được để trống")]
        [StringLength(50)]
        public string Department { get; set; } = "Retail Outlets";

        [StringLength(20)]
        public string? StoreId { get; set; }

        [StringLength(150)]
        public string? RetailShopAssigned { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public string? DateOfJoining { get; set; }

        /// <summary>
        /// Mật khẩu ban đầu cho nhân viên. Nếu không truyền sẽ đặt mặc định là "Nexus@123".
        /// </summary>
        public string? Password { get; set; }
    }
}
