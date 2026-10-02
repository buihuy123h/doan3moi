using System.ComponentModel.DataAnnotations;

namespace doanki3API.DTOs.Orders
{
    public sealed class CreateOrderRequest
    {
        public string? OrderId { get; set; }

        [Required]
        [StringLength(150)]
        public string CustomerName { get; set; } = null!;

        [StringLength(50)]
        public string? CustomerPhone { get; set; }

        [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", ErrorMessage = "CustomerEmail must be a valid email address.")]
        public string? CustomerEmail { get; set; }

        [StringLength(255)]
        public string? InstallationAddress { get; set; }

        [StringLength(50)]
        public string? IdProofType { get; set; }

        [StringLength(100)]
        public string? IdProofNumber { get; set; }

        [StringLength(50)]
        public string? ConnectionType { get; set; }

        [StringLength(50)]
        public string? PlanId { get; set; }

        [StringLength(150)]
        public string? PlanName { get; set; }

        [StringLength(50)]
        public string? RetailOutletCode { get; set; }

        [StringLength(100)]
        public string? RetailEmployeeName { get; set; }

        [StringLength(50)]
        public string? AssignedEmployeeId { get; set; }

        [StringLength(150)]
        public string? AssignedBranchName { get; set; }

        // Bị bỏ qua: server luôn tạo đơn ở trạng thái PendingRetail (chờ chi nhánh duyệt).
        [StringLength(50)]
        public string Status { get; set; } = "PendingRetail";

        public int BulkConnectionsCount { get; set; } = 1;

        public int BulkDiscountPercent { get; set; } = 0;

        /// <summary>
        /// Mã tài khoản landline Nexus khách đang giữ — theo spec, khách đã có
        /// landline của cùng nhà cung cấp thì khảo sát Dial-Up chỉ cần lớp
        /// internet (miễn lớp landline). Bỏ trống nếu khách chưa có.
        /// </summary>
        [StringLength(20)]
        public string? ExistingLandlineAccountId { get; set; }
    }

    public sealed class UpdateOrderStatusRequest
    {
        [Required]
        public string Status { get; set; } = null!;
        public string? Reason { get; set; }
        
        // fields for retail approval
        public string? RetailApprovedBy { get; set; }
        public string? RetailApprovalNotes { get; set; }
        public string? RetailRejectionReason { get; set; }
        
        // fields for technician assignment
        public string? AssignedTechnician { get; set; }
        public string? AssignedTechnicianId { get; set; }
        public string? AssignedTechnicianPhone { get; set; }
    }
}
