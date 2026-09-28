using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Admin
{
    public sealed class CreateRetailShopRequest
    {
        public string? StoreId { get; set; }
        
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = null!;

        [StringLength(50)]
        public string CityCode { get; set; } = "999";

        [StringLength(255)]
        public string Address { get; set; } = null!;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? ManagerName { get; set; }

        [StringLength(100)]
        public string? OperatingHours { get; set; }

        public int ActiveEmployeesCount { get; set; }

        public int TotalSubscribersServed { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";
    }
}
