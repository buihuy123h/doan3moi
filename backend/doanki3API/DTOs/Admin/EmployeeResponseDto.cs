using System;
using System.Text.Json.Serialization;

namespace doanki3API.DTOs.Admin
{
    public class EmployeeResponseDto
    {
        // Khóa chính và mã nhân viên
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("employeeId")]
        public string EmployeeId { get; set; } = null!;

        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; } = null!;

        // Họ tên (map từ FullName trong DB -> hỗ trợ cả name và fullName ở FE)
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = null!;

        // Thông tin liên hệ
        [JsonPropertyName("email")]
        public string Email { get; set; } = null!;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        // Vai trò và phòng ban
        [JsonPropertyName("role")]
        public string Role { get; set; } = null!;

        [JsonPropertyName("department")]
        public string Department { get; set; } = null!;

        // Chi nhánh phân công (FE hiển thị text: ví dụ "Downtown Flagship (SH-01)" hoặc "Headquarters")
        [JsonPropertyName("retailShopAssigned")]
        public string RetailShopAssigned { get; set; } = "Headquarters";

        [JsonPropertyName("storeId")]
        public string? StoreId { get; set; }

        [JsonPropertyName("storeName")]
        public string? StoreName { get; set; }

        // Trạng thái và ngày vào làm việc
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        [JsonPropertyName("dateOfJoining")]
        public string DateOfJoining { get; set; } = null!;

        // Các trường bổ sung từ Model Employee trong DB
        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public string? DateOfBirth { get; set; }

        [JsonPropertyName("bio")]
        public string? Bio { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
