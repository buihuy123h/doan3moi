using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace doanki3API.DTOs.Admin
{
    /// <summary>
    /// DTO đại diện cho một Gói cước dịch vụ viễn thông (Service Plan)
    /// Ánh xạ chuẩn hóa tương thích 100% với giao diện Frontend (Admin Tab Plan Management)
    /// Xử lý sai khác tên thuộc tính:
    /// - DB Model: PlanId <-> FE: id, planId
    /// - DB Model: ConnectionType <-> FE: type, connectionType ('Broadband', 'Dial-Up', 'Landline')
    /// </summary>
    public class PlanResponseDto
    {
        // Khóa chính và mã gói
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("planId")]
        public string PlanId { get; set; } = null!;

        [JsonPropertyName("planCode")]
        public string? PlanCode { get; set; }

        // 1. Cột PLAN NAME & DETAILS / Tên gói & Chi tiết
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("billingCycle")]
        public string BillingCycle { get; set; } = "Monthly";

        [JsonPropertyName("validity")]
        public string? Validity { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        // 2. Cột CONNECTION TYPE / Loại kết nối ('Broadband', 'Dial-Up', 'Landline')
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("connectionType")]
        public string ConnectionType { get; set; } = null!;

        // 3. Cột SPEED / BANDWIDTH / Tốc độ hoặc Băng thông (ví dụ: '56 Kbps V.92', 'PSTN Voice', '128 Kbps')
        [JsonPropertyName("speedOrBandwidth")]
        public string SpeedOrBandwidth { get; set; } = null!;

        // 4. Cột SECURITY DEPOSIT ($) / Tiền đặt cọc thiết bị
        [JsonPropertyName("securityDeposit")]
        public decimal SecurityDeposit { get; set; }

        // 5. Cột MONTHLY RENTAL ($) / Tiền cước thuê bao hàng tháng
        [JsonPropertyName("monthlyRental")]
        public decimal MonthlyRental { get; set; }

        // 6. Cột HOURLY CHARGE ($) / Cước theo giờ (dành cho gói Dial-Up/Broadband theo giờ)
        [JsonPropertyName("hourlyCharge")]
        public decimal? HourlyCharge { get; set; }

        // 7. Cột DATA LIMIT / Giới hạn dữ liệu ('Unlimited', '10 Hours', 'Local Calling')
        [JsonPropertyName("dataLimit")]
        public string? DataLimit { get; set; }

        // 8. Cột STATUS / Trạng thái ('Active', 'Archived')
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        // Các thuộc tính bổ sung theo đặc tả nghiệp vụ viễn thông
        [JsonPropertyName("includedHours")]
        public int IncludedHours { get; set; }

        [JsonPropertyName("callRates")]
        public string? CallRates { get; set; }

        [JsonPropertyName("isPopular")]
        public bool IsPopular { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO thống kê phân loại các gói cước viễn thông
    /// </summary>
    public class PlanStatsDto
    {
        [JsonPropertyName("totalPlans")]
        public int TotalPlans { get; set; }

        [JsonPropertyName("broadbandPlansCount")]
        public int BroadbandPlansCount { get; set; }

        [JsonPropertyName("dialUpPlansCount")]
        public int DialUpPlansCount { get; set; }

        [JsonPropertyName("landlinePlansCount")]
        public int LandlinePlansCount { get; set; }

        [JsonPropertyName("activePlansCount")]
        public int ActivePlansCount { get; set; }

        [JsonPropertyName("archivedPlansCount")]
        public int ArchivedPlansCount { get; set; }
    }
}
