using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Admin
{
    public sealed class CreatePlanRequest
    {
        public string? PlanId { get; set; }
        
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string ConnectionType { get; set; } = "Broadband";

        [StringLength(100)]
        public string SpeedOrBandwidth { get; set; } = "64 Kbps";

        [Range(0, 1000000)]
        public decimal MonthlyRental { get; set; }

        [Range(0, 1000000)]
        public decimal SecurityDeposit { get; set; }

        [StringLength(50)]
        public string BillingCycle { get; set; } = "Monthly";

        [StringLength(50)]
        public string? Validity { get; set; }

        [StringLength(100)]
        public string? DataLimit { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public int IncludedHours { get; set; }

        public decimal? HourlyCharge { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? CallRates { get; set; }
        
        public bool IsPopular { get; set; }
    }
}
