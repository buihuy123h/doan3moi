# doanki3API

ASP.NET Core Web API cho hệ thống quản lý dịch vụ và kết nối khách hàng.

## Cấu trúc chính

- `Controllers`: nhận request, gọi service và trả HTTP response.
- `Services`: xử lý nghiệp vụ và truy vấn dữ liệu.
- `DTOs`: request/response được chia theo từng module.
- `Constants`: trạng thái dùng chung, tránh viết chuỗi trực tiếp nhiều nơi.
- `Data`: Entity Framework `DbContext` và cấu hình ánh xạ database.
- `Models`: entity được ánh xạ với SQL Server.

## Chạy dự án

```powershell
dotnet restore
dotnet run --project doanki3API/doanki3API.csproj
```

Swagger mặc định:

```text
https://localhost:7069/swagger
```

Các request mẫu cho module kỹ thuật nằm trong `doanki3API/doanki3API.http`.

Tài liệu giải thích chi tiết code và luồng của từng API:
`docs/TECHNICAL_API_GUIDE.md`.
