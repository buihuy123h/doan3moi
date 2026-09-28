using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace doanki3API.DTOs.Admin
{
    /// <summary>
    /// DTO đại diện cho một Nhà cung cấp thiết bị viễn thông (Vendor)
    /// Chuẩn hóa tên trường tương ứng 100% với giao diện Frontend (Admin Tab Vendor Management)
    /// </summary>
    public class VendorResponseDto
    {
        // Khóa định danh chính
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("vendorId")]
        public string VendorId { get; set; } = null!;

        // 1. Cột VENDOR CODE / Mã NCC
        [JsonPropertyName("vendorCode")]
        public string VendorCode { get; set; } = null!;

        // 2. Cột COMPANY NAME / Tên công ty
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = null!;

        // 3. Cột CONTACT PERSON / Người đại diện / liên hệ
        [JsonPropertyName("contactPerson")]
        public string? ContactPerson { get; set; }

        // 4. Cột SUPPLY CATEGORY / Danh mục cung cấp
        [JsonPropertyName("category")]
        public string Category { get; set; } = null!;

        [JsonPropertyName("supplyCategory")]
        public string SupplyCategory { get; set; } = null!;

        // 5. Cột DIRECT PHONE & EMAIL / Số điện thoại & Email trực tiếp
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        // 6. Cột FACILITY ADDRESS / Địa chỉ cơ sở sản xuất - trụ sở
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("facilityAddress")]
        public string? FacilityAddress { get; set; }

        // 7. Cột STATUS / Trạng thái hợp tác ('Active', 'Pending Review', 'Terminated')
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        // Các thuộc tính bổ sung từ Model DB
        [JsonPropertyName("taxNumber")]
        public string? TaxNumber { get; set; }

        [JsonPropertyName("rating")]
        public byte Rating { get; set; } = 5;

        // Số loại vật tư / thiết bị đang cung ứng cho hệ thống
        [JsonPropertyName("totalSuppliedItemsCount")]
        public int TotalSuppliedItemsCount { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO thống kê tổng quan về đối tác nhà cung cấp
    /// </summary>
    public class VendorStatsDto
    {
        [JsonPropertyName("totalVendors")]
        public int TotalVendors { get; set; }

        [JsonPropertyName("activeVendorsCount")]
        public int ActiveVendorsCount { get; set; }

        [JsonPropertyName("pendingReviewCount")]
        public int PendingReviewCount { get; set; }

        [JsonPropertyName("terminatedCount")]
        public int TerminatedCount { get; set; }

        [JsonPropertyName("totalCategoriesCount")]
        public int TotalCategoriesCount { get; set; }
    }
}
