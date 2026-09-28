using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace doanki3API.DTOs.Admin
{
    /// <summary>
    /// DTO đại diện cho một Điểm bán lẻ / Chi nhánh khu vực (Retail Shop / Retail Store)
    /// Chuẩn hóa tên trường tương ứng 100% với giao diện Frontend (Admin Tab Retail Shops)
    /// Xử lý khác biệt tên thuộc tính:
    /// - DB Model: StoreId <-> FE: id, shopCode, storeId
    /// - DB Model: ActiveEmployeesCount <-> FE: activeEmployeesCount
    /// - DB Model: TotalSubscribersServed <-> FE: totalSubscribersServed
    /// </summary>
    public class RetailShopResponseDto
    {
        // Khóa định danh chính (hỗ trợ cả id, storeId và shopCode)
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = null!;

        // 1. Badge MÃ ĐIỂM BÁN LẺ (e.g., 'SH-01', 'SH-02')
        [JsonPropertyName("shopCode")]
        public string ShopCode { get; set; } = null!;

        // 2. TÊN ĐIỂM BÁN LẺ (e.g., 'Downtown Nexus Flagship Store')
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        // 3. THÀNH PHỐ & MÃ VÙNG ĐIỀU PHỐI (e.g., 'New York', cityCode: '064')
        [JsonPropertyName("city")]
        public string City { get; set; } = null!;

        [JsonPropertyName("cityCode")]
        public string CityCode { get; set; } = null!;

        // Chuỗi hiển thị phụ đề vùng (ví dụ: 'New York Regional Center · City Code: 064')
        [JsonPropertyName("regionSubtitle")]
        public string RegionSubtitle { get; set; } = "";

        // 4. ĐỊA CHỈ TRỤ SỞ CHI NHÁNH
        [JsonPropertyName("address")]
        public string Address { get; set; } = null!;

        // 5. SỐ ĐIỆN THOẠI LIÊN HỆ
        [JsonPropertyName("phone")]
        public string Phone { get; set; } = "";

        // 6. QUẢN LÝ CHI NHÁNH (Store Manager)
        [JsonPropertyName("managerName")]
        public string ManagerName { get; set; } = "Not Assigned";

        // 7. Ô CHỈ SỐ 1 TRÊN CARD: Nhân viên trực (On-Duty Employees)
        [JsonPropertyName("activeEmployeesCount")]
        public int ActiveEmployeesCount { get; set; }

        [JsonPropertyName("onDutyEmployees")]
        public int OnDutyEmployees { get; set; }

        // 8. Ô CHỈ SỐ 2 TRÊN CARD: Tổng thuê bao phục vụ (Total Subscribers)
        [JsonPropertyName("totalSubscribersServed")]
        public int TotalSubscribersServed { get; set; }

        [JsonPropertyName("totalSubscribers")]
        public int TotalSubscribers { get; set; }

        // Các trường bổ sung
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("operatingHours")]
        public string? OperatingHours { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO thống kê dữ liệu mạng lưới điểm bán lẻ (Retail Network Stats)
    /// </summary>
    public class RetailShopStatsDto
    {
        // Tổng số điểm bán lẻ / chi nhánh trong hệ thống
        [JsonPropertyName("totalShops")]
        public int TotalShops { get; set; }

        // Số chi nhánh đang hoạt động bình thường
        [JsonPropertyName("activeShopsCount")]
        public int ActiveShopsCount { get; set; }

        // Tổng số lượng nhân viên đang trực trên toàn bộ mạng lưới chi nhánh
        [JsonPropertyName("totalOnDutyEmployees")]
        public int TotalOnDutyEmployees { get; set; }

        // Tổng số thuê bao được phục vụ trên toàn bộ hệ thống chi nhánh
        [JsonPropertyName("totalSubscribersServed")]
        public int TotalSubscribersServed { get; set; }

        // Số lượng thành phố / khu vực có chi nhánh hoạt động
        [JsonPropertyName("totalCitiesCovered")]
        public int TotalCitiesCovered { get; set; }
    }
}
