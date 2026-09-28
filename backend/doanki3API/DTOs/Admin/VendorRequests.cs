using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Admin
{
    public sealed class CreateVendorRequest
    {
        public string? VendorId { get; set; }
        
        [StringLength(50)]
        public string? VendorCode { get; set; }

        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = null!;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [StringLength(50)]
        public string Status { get; set; } = "Active";
    }
}
