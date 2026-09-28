# Liên kết Backend – Frontend: phân hệ Kỹ thuật

Tài liệu này mô tả luồng dữ liệu thật của trang `/technical`, các API thuộc `api/Technical`, quy tắc trạng thái và vị trí code cần sửa khi thay đổi nghiệp vụ.

## 1. Kiến trúc và cách chạy

```text
Trình duyệt :3000
    │  /api/Technical/*
    ▼
Vite dev proxy (vite.config.ts)
    │  http://localhost:5105
    ▼
ASP.NET Core Controllers
    ▼
Technical services + EF Core
    ▼
SQL Server / NexusSystem
```

Chạy backend:

```powershell
cd be_ki3
dotnet run --project doanki3API/doanki3API.csproj --launch-profile http
```

Chạy frontend:

```powershell
cd doanki3fe
npm.cmd run dev
```

Mở `http://localhost:3000/technical`. Trong môi trường production phải cấu hình reverse proxy cho `/api/Technical` tới backend; cấu hình Vite chỉ áp dụng khi phát triển.

## 2. File liên kết chính

| Tầng | File | Trách nhiệm |
|---|---|---|
| FE UI | `doanki3fe/src/pages/TechnicalDashboard.svelte` | Hiển thị và xử lý thao tác của kỹ thuật viên |
| FE API client | `doanki3fe/src/lib/technicalApi.ts` | Gọi HTTP, chuẩn hóa lỗi và ánh xạ DTO BE sang type FE |
| FE types | `doanki3fe/src/types/nexus.ts` | `Order`, `Connection`, `Equipment`, `InventoryItem` |
| FE proxy | `doanki3fe/vite.config.ts` | Chuyển `/api/Technical/*` sang cổng `5105` |
| BE controllers | `doanki3API/Controllers/*Controller.cs` | Khai báo route và HTTP status |
| BE services | `doanki3API/Services/*Service.cs` | Validation, transaction và nghiệp vụ |
| BE DTO | `doanki3API/DTOs/*` | Hợp đồng JSON request/response |
| Trạng thái chuẩn | `doanki3API/Constants/*Statuses.cs` | Chuỗi trạng thái dùng thống nhất với SQL và FE |

## 3. Bảng ánh xạ API BE – hàm FE – giao diện

| Method và endpoint | Service backend | Hàm frontend | Vị trí sử dụng |
|---|---|---|---|
| `GET /api/Technical/dashboard` | `TechnicalDashboardService.GetAsync` | `loadTechnicalDashboard` | Có thể dùng cho KPI/tổng quan |
| `GET /api/Technical/orders` | `TechnicalOrderService.GetOrdersAsync` | `getTechnicalOrders`, `loadTechnicalWorkspace` | Tab Kiểm tra khả thi |
| `GET /api/Technical/orders/{id}` | `TechnicalOrderService.GetOrderByIdAsync` | `getTechnicalOrderById` | Chi tiết đơn |
| `PUT /api/Technical/orders/{id}/feasibility` | `UpdateFeasibilityAsync` | `updateFeasibility` | Nút Khả thi/Không khả thi |
| `PUT /api/Technical/orders/{id}/installation-schedule` | `UpdateScheduleAsync` | `updateInstallationSchedule` | Modal Lịch lắp đặt |
| `POST /api/Technical/orders/{id}/provide-connection` | `ConnectionService.ProvideConnectionAsync` | `provideConnection` | Modal cấp kết nối |
| `GET /api/Technical/connections` | `GetConnectionsAsync` | `getTechnicalConnections`, `loadTechnicalWorkspace` | Tab Quản lý kết nối |
| `GET /api/Technical/connections/search` | `GetConnectionsAsync` | Tương thích route cũ | Tìm theo query |
| `GET /api/Technical/connections/{accountId}` | `GetConnectionByIdAsync` | `getTechnicalConnectionById` | Chi tiết kết nối |
| `PUT /api/Technical/connections/{accountId}/status` | `UpdateStatusAsync` | `changeConnectionStatus` | Bộ chuyển 3 trạng thái |
| `GET /api/Technical/connections/{accountId}/activity-logs` | `GetActivityLogsAsync` | `getConnectionActivityLogs` | Nhật ký trong chi tiết kết nối |
| `GET /api/Technical/equipment` | `EquipmentService.GetEquipmentAsync` | `getTechnicalEquipment`, `loadTechnicalWorkspace` | Tab Thiết bị CPE |
| `GET /api/Technical/equipment/{id}` | `GetEquipmentByIdAsync` | `getTechnicalEquipmentById` | Chi tiết thiết bị |
| `POST /api/Technical/equipment` | `CreateEquipmentAsync` | `createEquipment` | Modal thêm thiết bị |
| `PUT /api/Technical/equipment/{id}` | `UpdateEquipmentAsync` | `updateEquipment` | Đổi trạng thái thiết bị |
| `GET /api/Technical/inventory` | `InventoryService.GetInventoryAsync` | `getTechnicalInventory`, `loadTechnicalWorkspace` | Bảng tồn kho kỹ thuật |
| `GET /api/Technical/inventory/{id}` | `GetInventoryByIdAsync` | `getTechnicalInventoryById` | Chi tiết vật tư |
| `PUT /api/Technical/inventory/{id}` | `UpdateInventoryAsync` | `updateInventory` | Cập nhật định mức/giá/vị trí |
| `POST /api/Technical/inventory/{id}/adjust-stock` | `AdjustStockAsync` | `adjustInventoryStock` | Modal nhập/xuất kho |

`loadTechnicalWorkspace()` gọi song song bốn API danh sách (`orders`, `connections`, `equipment`, `inventory`) và chỉ cập nhật Svelte store sau khi tất cả thành công. Điều này tránh hiển thị dữ liệu nửa cũ, nửa mới.

## 4. Quy trình nghiệp vụ

### 4.1 Đơn hàng

```text
Pending ── đánh giá đạt ──> Feasible ── cấp đủ kết nối ──> Connection Provided
   │                            │
   └── đánh giá không đạt ──> Not Feasible
                                │
                                └── đánh giá lại đạt ──> Feasible
```

- Đơn `Not Feasible` bắt buộc có `rejectionReason`.
- Đơn Dial-Up phải đạt cả Landline và Internet; nếu đã có tài khoản Landline Nexus thì nhánh Landline được miễn.
- Khi đơn đạt, backend giữ trước một Account ID.
- Chỉ đơn `Feasible` mới được lên lịch hoặc cấp kết nối.
- Đơn số lượng lớn cần một thiết bị riêng cho từng kết nối; trạng thái chỉ thành `Connection Provided` sau khi cấp đủ.

Payload khảo sát:

```json
{
  "isFeasible": true,
  "checkedBy": "emp-03",
  "cableDistanceMeters": 250,
  "dpBoxCapacity": "Port available",
  "signalLossDbm": -16.5,
  "notes": "Field inspection passed",
  "landlineFeasible": true,
  "internetFeasible": true
}
```

Payload lịch lắp đặt:

```json
{
  "scheduledInstallDate": "2026-09-18T09:30:00",
  "technicianId": "emp-03"
}
```

### 4.2 Cấp kết nối

```json
{
  "equipmentId": "eq-001",
  "ipAddress": "198.51.100.42",
  "portNumber": "ETH-PORT-1",
  "performedBy": "emp-03"
}
```

Backend kiểm tra nguyên tử trong transaction:

1. Đơn phải `Feasible` và chưa được cấp đủ.
2. Thiết bị phải `In Stock`, chưa gán, cùng cửa hàng xử lý đơn.
3. Vật tư kho liên kết phải còn số lượng.
4. Tạo `CustomerConnection` và activity log.
5. Chuyển thiết bị sang `In Service`, gán Account ID/kỹ thuật viên.
6. Trừ tồn kho và cập nhật trạng thái kho.

Account ID gồm 16 ký tự chữ/số và một dấu gạch hiển thị: `{B|D|T}{CityCode}-{12 số tuần tự}`, ví dụ `B064-000000000001`.

### 4.3 Trạng thái kết nối

| Trạng thái | Ý nghĩa | Quy tắc |
|---|---|---|
| `Active` | Đang hoạt động | Có thể phục hồi từ tạm ngưng |
| `Temporarily Inactive` | Tạm ngưng | Bắt buộc có lý do |
| `Permanently Inactive` | Ngừng vĩnh viễn | Không được kích hoạt lại; thiết bị được thu hồi và tồn kho được hoàn |

Mọi lần đổi trạng thái đều tạo `ConnectionActivityLog`; FE tải lịch sử qua endpoint `activity-logs`.

### 4.4 Thiết bị và tồn kho

- Trạng thái thiết bị chuẩn: `In Stock`, `In Service`, `Maintenance`, `Faulty`.
- Thiết bị đang phục vụ kết nối chỉ được giữ ở `In Service`; muốn thu hồi phải ngừng kết nối vĩnh viễn.
- Điều chỉnh kho dùng `quantityChange`: số dương là nhập, số âm là xuất, không nhận `0`, không cho tồn kho âm.
- Trạng thái kho được tính tự động: `In Stock`, `Low Stock`, `Out of Stock`.

## 5. Ánh xạ trường dữ liệu quan trọng

| JSON backend | Type/store frontend |
|---|---|
| `orderId` | `Order.id` |
| `storeId` | `Order.retailOutletCode`, `Equipment.storeId` |
| `applicationDate` | `Order.createdAt` |
| `assignedAccountId` | `Order.assignedAccountId`, `Equipment.assignedAccountId` |
| `equipmentId` | `Equipment.id` |
| `inventoryId` | `InventoryItem.id`, `Equipment.inventoryId` |
| `equipmentSerialNumber` | `Connection.assignedDeviceSerial` |
| `equipmentModel` | `Connection.assignedDeviceModel` |
| `vendorName` | `InventoryItem.supplier` |

Chuỗi enum phải giữ đúng chính tả và khoảng trắng; không dùng các biến thể như `NotFeasible`, `Inactive`, hoặc `InService`.

## 6. HTTP status và lỗi FE

| Status | Ý nghĩa |
|---|---|
| `200` | Đọc/cập nhật thành công |
| `201` | Tạo thiết bị thành công |
| `400` | Payload hoặc trạng thái nghiệp vụ không hợp lệ |
| `404` | Không tìm thấy đơn/kết nối/thiết bị/vật tư |
| `409` | Xung đột: thiết bị đã dùng, đơn đã cấp đủ, kết nối không thể phục hồi |
| `500` | Lỗi ngoài dự kiến ở server/SQL |

`technicalApi.ts` chuẩn hóa cả hai dạng lỗi:

```json
{ "message": "Mô tả lỗi nghiệp vụ" }
```

và lỗi validation chuẩn của ASP.NET Core:

```json
{ "errors": { "Field": ["Validation message"] } }
```

## 7. Checklist kiểm thử nhanh

1. `GET orders`, `connections`, `equipment`, `inventory`, `dashboard` đều trả `200`.
2. Đánh dấu một đơn Pending là khả thi; kiểm tra Account ID được tạo.
3. Lên lịch tương lai cho đơn Feasible.
4. Cấp một thiết bị In Stock đúng cửa hàng; kiểm tra connection, equipment và inventory cùng thay đổi.
5. Tạm ngưng rồi mở lại connection; kiểm tra activity log.
6. Ngừng vĩnh viễn; kiểm tra thiết bị về In Stock và tồn kho tăng lại.
7. Chuyển thiết bị rảnh giữa In Stock/Maintenance/Faulty.
8. Nhập/xuất kho và thử xuất vượt tồn để xác nhận lỗi `400`.

## 8. Kết quả xác nhận hiện tại

- Backend Release build: thành công, 0 warning, 0 error.
- Frontend Vite production build: thành công.
- Các API danh sách và dashboard: trả `200` với SQL Server hiện tại.
- Activity log: trả `200` cho Account ID tồn tại.
- Các endpoint chi tiết/mutation với ID không tồn tại: trả đúng `404` và không làm thay đổi dữ liệu.
- `svelte-check` toàn repo còn lỗi có sẵn tại các phần Search/Admin/Accounts/User; không có lỗi TypeScript mới ở hai file tích hợp Kỹ thuật.
