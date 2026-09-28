# Hướng dẫn đọc và hiểu module API kỹ thuật

Tài liệu này giải thích cấu trúc code, luồng gọi giữa các file, cú pháp C# và logic xử lý của toàn bộ API dưới route `/api/Technical`.

## 1. Bản đồ module

```text
HTTP request
    ↓
Controller
    ↓ gọi method
Service
    ↓ dùng Entity Framework Core
MyAppDbContext
    ↓ ánh xạ
Models / SQL Server
    ↑
Response DTO
    ↑
HTTP response dạng JSON
```

Vai trò của từng thư mục:

```text
doanki3API/
├── Controllers/         Nhận request và trả HTTP response
├── Services/            Chứa nghiệp vụ và truy vấn database
├── DTOs/                Dữ liệu request/response của API
├── Constants/           Các giá trị trạng thái dùng chung
├── Data/                DbContext và cấu hình Entity Framework
├── Models/              Các entity ánh xạ với bảng database
└── Program.cs           Đăng ký dependency và khởi động ứng dụng
```

## 2. Một request đi qua những file nào?

Ví dụ request cấp kết nối:

```http
POST /api/Technical/orders/ORD001/provide-connection
Content-Type: application/json

{
  "equipmentId": "EQ001",
  "ipAddress": "192.168.1.10",
  "portNumber": "P01",
  "performedBy": "EMP001"
}
```

Luồng xử lý:

```text
ConnectionsController.ProvideConnection()
    ↓
ConnectionService.ProvideConnectionAsync()
    ↓
MyAppDbContext.Orders / Equipment / CustomerConnections
    ↓
SQL Server
    ↓
ProvideConnectionResponse
    ↓
TechnicalApiControllerBase.FromServiceResult()
    ↓
HTTP 200 / 400 / 404 / 409
```

Các file tham gia:

- `Controllers/ConnectionsController.cs`: định nghĩa URL và nhận request.
- `DTOs/Connections/ProvideConnectionRequest.cs`: định nghĩa JSON đầu vào.
- `Services/ConnectionService.cs`: kiểm tra và thực hiện nghiệp vụ.
- `Data/MyAppDbContext.cs`: cung cấp các `DbSet` để truy vấn database.
- `Models/Order.cs`, `Equipment.cs`, `CustomerConnection.cs`: entity database.
- `DTOs/Connections/ConnectionResponses.cs`: định nghĩa JSON trả về.
- `Services/Common/ServiceResult.cs`: biểu diễn thành công hoặc lỗi nghiệp vụ.
- `Controllers/TechnicalApiControllerBase.cs`: đổi lỗi nghiệp vụ thành HTTP status.

## 3. Dependency Injection trong `Program.cs`

Các service được đăng ký như sau:

```csharp
builder.Services.AddScoped<ConnectionService>();
```

`AddScoped` có nghĩa là ASP.NET Core tạo một instance service cho mỗi HTTP request. Trong cùng một request, controller và service sử dụng cùng scope của `MyAppDbContext`.

Controller chỉ cần khai báo service trong constructor:

```csharp
private readonly ConnectionService _connectionService;

public ConnectionsController(ConnectionService connectionService)
{
    _connectionService = connectionService;
}
```

ASP.NET Core tự tìm đăng ký trong `Program.cs`, tạo `ConnectionService`, sau đó truyền nó vào controller. Ta không cần viết:

```csharp
var service = new ConnectionService(...);
```

## 4. Controller dùng để làm gì?

Controller chỉ nên làm ba việc:

1. Nhận route, query string hoặc JSON body.
2. Gọi đúng service.
3. Chuyển kết quả service thành HTTP response.

Ví dụ:

```csharp
[HttpPut("connections/{accountId}/status")]
public async Task<IActionResult> UpdateConnectionStatus(
    string accountId,
    [FromBody] UpdateConnectionStatusRequest request,
    CancellationToken cancellationToken)
{
    var result = await _connectionService.UpdateStatusAsync(
        accountId,
        request,
        cancellationToken);

    return FromServiceResult(result);
}
```

Giải thích cú pháp:

- `[HttpPut(...)]`: method này nhận HTTP PUT.
- `{accountId}`: giá trị động trong URL.
- `[FromBody]`: đọc JSON body và chuyển thành object C#.
- `async Task<IActionResult>`: method bất đồng bộ và trả một HTTP response.
- `await`: đợi tác vụ database hoàn thành mà không khóa thread.
- `CancellationToken`: dừng truy vấn nếu client hủy request.

## 5. DTO request và response

DTO là object dành riêng cho API. Không trả trực tiếp entity database vì entity có navigation property và có thể làm lộ dữ liệu không cần thiết.

Ví dụ request DTO:

```csharp
public sealed class UpdateConnectionStatusRequest
{
    [Required, StringLength(30)]
    public string Status { get; set; } = null!;

    [StringLength(500)]
    public string? Reason { get; set; }
}
```

- `[Required]`: bắt buộc client gửi trường này.
- `[StringLength(30)]`: chuỗi tối đa 30 ký tự.
- `string?`: trường có thể nhận `null`.
- `= null!`: báo với compiler rằng ASP.NET Core sẽ gán giá trị sau khi đọc JSON.
- `sealed`: không cho class khác kế thừa DTO này.

Response được khai báo bằng `record`:

```csharp
public sealed record UpdateConnectionStatusResponse(
    string Message,
    string AccountId,
    string OldStatus,
    string Status);
```

`record` phù hợp cho dữ liệu chỉ dùng để truyền đi. ASP.NET Core tự chuyển tên PascalCase sang camelCase khi trả JSON:

```json
{
  "message": "Cập nhật trạng thái kết nối thành công",
  "accountId": "B064-000000000001",
  "oldStatus": "Active",
  "status": "Temporarily Inactive"
}
```

## 6. `ServiceResult` và HTTP status

Service không trả `IActionResult` vì service không nên phụ thuộc vào HTTP. Service trả `ServiceResult<T>`:

```csharp
return ServiceResult<EquipmentMutationResponse>.NotFound(
    "Không tìm thấy thiết bị");
```

`TechnicalApiControllerBase.FromServiceResult()` chuyển kết quả:

| Service result | HTTP status |
|---|---:|
| `Success` | 200 OK |
| `Validation` | 400 Bad Request |
| `NotFound` | 404 Not Found |
| `Conflict` | 409 Conflict |

Riêng API tạo thiết bị trả `201 Created` và header `Location` bằng `CreatedAtAction()`.

## 7. Cú pháp Entity Framework được sử dụng

### Truy vấn không theo dõi

```csharp
var query = _context.Orders.AsNoTracking();
```

`AsNoTracking()` dùng cho API chỉ đọc. EF không cần giữ entity để cập nhật nên giảm bộ nhớ.

### Xây dựng query từng bước

```csharp
if (!string.IsNullOrWhiteSpace(status))
    query = query.Where(order => order.Status == status.Trim());
```

`IQueryable` chưa chạy SQL ngay. EF ghép các điều kiện lại và chỉ gửi SQL khi gặp `ToListAsync()`, `FirstOrDefaultAsync()` hoặc `CountAsync()`.

### Chọn đúng dữ liệu cần trả

```csharp
.Select(order => new OrderListItemResponse(
    order.OrderId,
    order.CustomerId,
    order.Customer.FullName,
    order.Status))
```

`Select` giúp SQL chỉ lấy các cột cần thiết và ánh xạ sang response DTO.

### Lấy một bản ghi

```csharp
var order = await _context.Orders
    .FirstOrDefaultAsync(order => order.OrderId == id, cancellationToken);
```

Nếu không tìm thấy, kết quả là `null`.

### Kiểm tra tồn tại

```csharp
var exists = await _context.Employees.AnyAsync(
    employee => employee.EmployeeId == employeeId,
    cancellationToken);
```

`AnyAsync` hiệu quả hơn việc tải toàn bộ entity rồi kiểm tra.

### Lưu thay đổi

```csharp
await _context.SaveChangesAsync(cancellationToken);
```

EF theo dõi entity đã được lấy không có `AsNoTracking`. Khi gọi `SaveChangesAsync`, EF sinh câu lệnh INSERT/UPDATE tương ứng.

---

# 8. Giải thích từng API

Base URL khi chạy profile HTTP:

```text
http://localhost:5105
```

## API 1: Dashboard kỹ thuật

```http
GET /api/Technical/dashboard
```

Controller: `TechnicalDashboardController.GetDashboard`

Service: `TechnicalDashboardService.GetAsync`

Logic:

1. Đếm đơn có trạng thái `Pending`.
2. Đếm đơn có trạng thái `Feasible`.
3. Đếm kết nối `Active`.
4. Đếm kết nối không phải `Active`.
5. Đếm thiết bị chưa có account và chưa có `CustomerConnection`.
6. Đếm mặt hàng có tồn kho nhỏ hơn hoặc bằng mức đặt lại.
7. Trả `TechnicalDashboardResponse`.

Response mẫu:

```json
{
  "pendingOrders": 4,
  "feasibleOrders": 2,
  "activeConnections": 20,
  "inactiveConnections": 3,
  "availableEquipment": 8,
  "lowStockItems": 2
}
```

## API 2: Danh sách đơn hàng

```http
GET /api/Technical/orders
GET /api/Technical/orders?status=Pending
GET /api/Technical/orders?storeId=STORE01&keyword=Nguyen
```

Controller: `TechnicalOrdersController.GetOrders`

Service: `TechnicalOrderService.GetOrdersAsync`

Query parameters:

| Tên | Bắt buộc | Ý nghĩa |
|---|---|---|
| `status` | Không | Lọc trạng thái đơn |
| `storeId` | Không | Lọc cửa hàng |
| `keyword` | Không | Tìm theo OrderId, CustomerId hoặc địa chỉ |

Logic:

1. Tạo query từ bảng `Orders`.
2. Thêm từng điều kiện nếu query parameter có giá trị.
3. Sắp xếp theo `ApplicationDate` mới nhất.
4. Join các navigation `Customer`, `Plan`, `Store` thông qua projection.
5. Trả danh sách `OrderListItemResponse`.

## API 3: Chi tiết đơn hàng

```http
GET /api/Technical/orders/ORD001
```

Controller: `TechnicalOrdersController.GetOrderById`

Service: `TechnicalOrderService.GetOrderByIdAsync`

Logic:

1. Tìm order theo `OrderId`.
2. Lấy thông tin khách hàng, gói dịch vụ, cửa hàng và kết quả khả thi.
3. Không tìm thấy thì controller trả `404`.
4. Tìm thấy thì trả `OrderDetailResponse`.

## API 4: Kiểm tra tính khả thi

```http
PUT /api/Technical/orders/ORD001/feasibility
Content-Type: application/json

{
  "isFeasible": true,
  "checkedBy": "EMP001",
  "notes": "Hạ tầng đáp ứng yêu cầu",
  "cableDistanceMeters": 250,
  "dpBoxCapacity": "Port 3 Available / DP-A01",
  "signalLossDbm": -16.5
}
```

Với Dial-Up, phải gửi kết quả của cả hai nhánh:

```json
{
  "isFeasible": true,
  "landlineFeasible": true,
  "internetFeasible": true,
  "checkedBy": "EMP001",
  "notes": "Cả hai nhánh đều đạt"
}
```

Trường hợp không khả thi:

```json
{
  "isFeasible": false,
  "checkedBy": "EMP001",
  "notes": "Khu vực chưa có cáp",
  "rejectionReason": "Khoảng cách tới hộp cáp quá xa"
}
```

Controller: `TechnicalOrdersController.UpdateFeasibility`

Service: `TechnicalOrderService.UpdateFeasibilityAsync`

Logic:

1. Lấy order kèm `ServicePlan`.
2. Không có order → `404`.
3. Nếu `isFeasible = false` mà không có `rejectionReason` → `400`.
4. Kiểm tra `checkedBy` là nhân viên đang `Active`.
5. Với Dial-Up, bắt buộc cả `landlineFeasible` và `internetFeasible` cùng đạt; nhánh Landline được miễn nếu đơn đã có `existingLandlineAccountId`.
6. Cập nhật các số đo khảo sát, người kiểm tra, ghi chú, ngày kiểm tra và trạng thái order.
7. Khi khả thi, sinh và giữ trước Account ID chuẩn `B064-000000000001` ngay trong transaction `Serializable`.
8. Gọi `SaveChangesAsync` và commit transaction.

Trạng thái kết quả:

```text
isFeasible = true  → Feasible
isFeasible = false → Not Feasible
```

## API 5: Lên lịch lắp đặt

```http
PUT /api/Technical/orders/ORD001/installation-schedule
Content-Type: application/json

{
  "scheduledInstallDate": "2026-09-20T09:00:00",
  "technicianId": "EMP001"
}
```

Controller: `TechnicalOrdersController.UpdateInstallationSchedule`

Service: `TechnicalOrderService.UpdateScheduleAsync`

Logic:

1. Tìm order.
2. Chỉ nhận order `Feasible`.
3. Ngày lắp đặt phải lớn hơn thời gian hiện tại.
4. Nếu có `technicianId`, kiểm tra nhân viên tồn tại và đang hoạt động.
5. Gán lịch và nhân viên; trạng thái vẫn là `Feasible` vì schema không có trạng thái `Scheduled`.
6. Lưu database.

## API 6: Cấp kết nối

```http
POST /api/Technical/orders/ORD001/provide-connection
Content-Type: application/json

{
  "equipmentId": "EQ001",
  "ipAddress": "192.168.1.10",
  "portNumber": "P01",
  "installedDate": "2026-09-20T09:30:00",
  "performedBy": "EMP001"
}
```

Controller: `ConnectionsController.ProvideConnection`

Service: `ConnectionService.ProvideConnectionAsync`

Đây là API có luồng nghiệp vụ phức tạp nhất.

Logic:

1. Mở transaction với isolation level `Serializable`.
2. Lấy order, plan và store.
3. Order phải có trạng thái `Feasible`.
4. Đếm số kết nối đã cấp và chặn khi đã đủ `BulkConnectionsCount`.
5. Thiết bị là bắt buộc:
   - Thiết bị phải tồn tại.
   - Chưa được account khác sử dụng.
   - Có trạng thái `In Stock` và thuộc đúng cửa hàng xử lý đơn (nếu thiết bị có Store ID).
6. Kiểm tra nhân viên thực hiện.
7. Kết nối đầu tiên dùng Account ID đã giữ khi duyệt khả thi; các kết nối bulk tiếp theo được sinh theo loại kết nối, mã thành phố và serial toàn hệ thống.
8. Tạo `CustomerConnection` trạng thái `Active`.
9. Tạo một `ConnectionActivityLog`.
10. Khi đã cấp đủ số lượng yêu cầu, cập nhật order thành `Connection Provided`; đơn bulk chưa đủ vẫn giữ `Feasible`.
11. Cập nhật thiết bị thành `In Service` và gán account.
12. Lưu toàn bộ thay đổi.
13. Commit transaction.

`Serializable` giúp hạn chế hai request đồng thời cùng đọc một Account ID cuối rồi sinh ra cùng mã mới.

Ví dụ quy tắc sinh mã:

```text
Broadband + 064 → B064-000000000001
Dial-up + 064   → D064-000000000002
Landline + 081  → T081-000000000003
```

Nếu bất kỳ bước nào lỗi trước `CommitAsync`, transaction bị hủy khi được dispose.

## API 7: Danh sách và tìm kiếm kết nối

```http
GET /api/Technical/connections
GET /api/Technical/connections?status=Active
GET /api/Technical/connections?accountId=B064&equipmentId=eq-01
```

Controller: `ConnectionsController.GetConnections`

Service: `ConnectionService.GetConnectionsAsync`

Logic:

1. Tạo query `CustomerConnections` với `AsNoTracking`.
2. Lọc theo Account ID, trạng thái hoặc Equipment ID.
3. Sắp xếp theo `LastUpdated` mới nhất.
4. Lấy thêm tên khách hàng thông qua order.
5. Trả `ConnectionListItemResponse`.

## API 8: Route tìm kiếm kết nối cũ

```http
GET /api/Technical/connections/search?status=Active
```

Controller: `ConnectionsController.SearchConnections`

Route này gọi lại trực tiếp method `GetConnections()`:

```csharp
GetConnections(accountId, status, equipmentId, cancellationToken);
```

Nó được giữ để frontend cũ vẫn hoạt động. Logic database giống API 7.

## API 9: Chi tiết kết nối

```http
GET /api/Technical/connections/B064-000000000001
```

Controller: `ConnectionsController.GetConnectionById`

Service: `ConnectionService.GetConnectionByIdAsync`

Logic:

1. Tìm theo `AccountId`.
2. Lấy khách hàng, địa chỉ, gói dịch vụ, thiết bị và trạng thái.
3. Không tìm thấy → `404`.
4. Tìm thấy → trả `ConnectionDetailResponse`.

## API 10: Cập nhật trạng thái kết nối

```http
PUT /api/Technical/connections/B064-000000000001/status
Content-Type: application/json

{
  "status": "Temporarily Inactive",
  "reason": "Bảo trì đường truyền",
  "performedBy": "EMP001"
}
```

Các trạng thái cho phép:

```text
Active
Temporarily Inactive
Permanently Inactive
```

Controller: `ConnectionsController.UpdateConnectionStatus`

Service: `ConnectionService.UpdateStatusAsync`

Logic:

1. Tìm connection theo Account ID.
2. Chuẩn hóa trạng thái không phân biệt chữ hoa/chữ thường.
3. Khi ngừng tạm thời hoặc vĩnh viễn, bắt buộc có lý do.
4. Không cho kích hoạt lại connection đã `Permanently Inactive`.
5. Kiểm tra nhân viên thực hiện nếu được truyền vào.
6. Cập nhật các mốc thời gian:
   - `Temporarily Inactive` → đặt `SuspensionStartDate`.
   - Từ tạm ngừng về `Active` → đặt `SuspensionEndDate`.
   - `Permanently Inactive` → đặt `TerminatedDate` và trả thiết bị về `In Stock`.
7. Tạo `ConnectionActivityLog` chứa trạng thái cũ/mới.
8. Lưu database.

## API 11: Lịch sử hoạt động kết nối

```http
GET /api/Technical/connections/B064-000000000001/activity-logs
```

Controller: `ConnectionsController.GetConnectionActivityLogs`

Service: `ConnectionService.GetActivityLogsAsync`

Logic:

1. Kiểm tra connection tồn tại.
2. Lấy các log có cùng Account ID.
3. Sắp xếp log mới nhất trước.
4. Lấy thêm tên nhân viên từ navigation `PerformedByNavigation`.
5. Trả danh sách `ConnectionActivityLogResponse`.

## API 12: Danh sách thiết bị

```http
GET /api/Technical/equipment
GET /api/Technical/equipment?status=In%20Stock
GET /api/Technical/equipment?storeId=STORE01&availableOnly=true
```

Controller: `EquipmentController.GetEquipment`

Service: `EquipmentService.GetEquipmentAsync`

Logic:

1. Lọc theo trạng thái và cửa hàng nếu có.
2. Nếu `availableOnly=true`, chỉ lấy thiết bị chưa có `AssignedAccountId` và chưa có `CustomerConnection`.
3. Method `Project()` ánh xạ entity sang `EquipmentResponse`.
4. Trả danh sách theo Equipment ID.

## API 13: Chi tiết thiết bị

```http
GET /api/Technical/equipment/EQ001
```

Controller: `EquipmentController.GetEquipmentById`

Service: `EquipmentService.GetEquipmentByIdAsync`

Logic:

1. Tìm theo Equipment ID.
2. Trả thêm nhà cung cấp, cửa hàng và kỹ thuật viên.
3. Không tìm thấy → `404`.

## API 14: Tạo thiết bị

```http
POST /api/Technical/equipment
Content-Type: application/json

{
  "equipmentId": "EQ001",
  "inventoryId": "INV001",
  "serialNumber": "SN-001",
  "macAddress": "00:11:22:33:44:55",
  "deviceModel": "Router X1",
  "deviceType": "Router",
  "vendorId": "VEN001",
  "storeId": "STORE01",
  "status": "In Stock",
  "firmwareVersion": "1.0.0"
}
```

Controller: `EquipmentController.CreateEquipment`

Service: `EquipmentService.CreateEquipmentAsync`

Logic:

1. Kiểm tra Equipment ID chưa tồn tại.
2. Kiểm tra Serial Number chưa trùng.
3. Kiểm tra vendor tồn tại.
4. Nếu có Inventory ID, kiểm tra mặt hàng tồn tại.
5. Nếu có Store ID, kiểm tra cửa hàng tồn tại.
6. Tạo entity `Equipment`.
7. Lưu database.
8. Controller trả `201 Created` bằng `CreatedAtAction()`.

`CreatedAtAction` tạo header `Location` trỏ tới API xem chi tiết thiết bị vừa tạo.

## API 15: Cập nhật thiết bị

```http
PUT /api/Technical/equipment/EQ001
Content-Type: application/json

{
  "firmwareVersion": "1.1.0",
  "status": "In Stock",
  "assignedTechnicianId": "EMP001"
}
```

Controller: `EquipmentController.UpdateEquipment`

Service: `EquipmentService.UpdateEquipmentAsync`

Logic:

1. Tìm thiết bị.
2. Kiểm tra Serial Number không trùng thiết bị khác.
3. Kiểm tra Store ID và Technician ID nếu có.
4. Không cho dùng API này để gán thiết bị sang account khác. Việc gán phải đi qua API cấp kết nối để hai bảng được đồng bộ.
5. Chỉ cập nhật các trường request có giá trị.
6. Lưu database.

Cú pháp cập nhật một phần:

```csharp
equipment.DeviceModel = Normalize(request.DeviceModel) ?? equipment.DeviceModel;
```

Nghĩa là nếu request có `deviceModel` hợp lệ thì dùng giá trị mới; nếu null/rỗng thì giữ giá trị cũ.

## API 16: Danh sách tồn kho

```http
GET /api/Technical/inventory
GET /api/Technical/inventory?category=Router
GET /api/Technical/inventory?lowStockOnly=true
```

Controller: `InventoryController.GetInventory`

Service: `InventoryService.GetInventoryAsync`

Logic:

1. Lọc theo category nếu có.
2. Nếu `lowStockOnly=true`, lấy các item có `StockQuantity <= ReorderLevel`.
3. Tính `TotalStockValue = StockQuantity * UnitCost`.
4. Tính cờ `IsLowStock`.
5. Trả `InventoryResponse`.

## API 17: Chi tiết tồn kho

```http
GET /api/Technical/inventory/INV001
```

Controller: `InventoryController.GetInventoryById`

Service: `InventoryService.GetInventoryByIdAsync`

Logic giống API danh sách nhưng chỉ lấy một Inventory ID. Không tìm thấy trả `404`.

## API 18: Cập nhật thông tin tồn kho

```http
PUT /api/Technical/inventory/INV001
Content-Type: application/json

{
  "stockQuantity": 20,
  "reorderLevel": 5,
  "unitCost": 350000,
  "location": "Kho A",
  "vendorId": "VEN001"
}
```

Controller: `InventoryController.UpdateInventory`

Service: `InventoryService.UpdateInventoryAsync`

Logic:

1. Tìm inventory item.
2. Không cho số lượng, reorder level hoặc unit cost âm.
3. Kiểm tra vendor nếu có.
4. Chỉ cập nhật trường được gửi; trạng thái kho được tự tính từ số lượng và mức đặt lại.
5. Lưu database.
6. Trả thêm `IsLowStock`.

API này đặt số lượng thành một giá trị cụ thể. Khi chỉ muốn nhập hoặc xuất thêm một lượng, nên dùng API 19.

## API 19: Điều chỉnh số lượng tồn kho

```http
POST /api/Technical/inventory/INV001/adjust-stock
Content-Type: application/json

{
  "quantityChange": 5,
  "reason": "Nhập thêm từ nhà cung cấp"
}
```

Xuất ba sản phẩm:

```json
{
  "quantityChange": -3,
  "reason": "Xuất lắp đặt cho khách hàng"
}
```

Controller: `InventoryController.AdjustInventoryStock`

Service: `InventoryService.AdjustStockAsync`

Logic:

1. Tìm inventory item.
2. `quantityChange` không được bằng 0.
3. Tính `newQuantity = oldQuantity + quantityChange`.
4. Không cho kết quả nhỏ hơn 0.
5. Tự đổi trạng thái:

```text
newQuantity = 0                    → Out of Stock
newQuantity <= ReorderLevel        → Low Stock
newQuantity > ReorderLevel         → In Stock
```

6. Lưu database.
7. Ghi log ứng dụng gồm số lượng cũ, mới và lý do.

---

## 9. Bảng tra nhanh controller và service

| Route | Controller | Service method |
|---|---|---|
| `GET dashboard` | `TechnicalDashboardController` | `TechnicalDashboardService.GetAsync` |
| `GET orders` | `TechnicalOrdersController` | `TechnicalOrderService.GetOrdersAsync` |
| `GET orders/{id}` | `TechnicalOrdersController` | `TechnicalOrderService.GetOrderByIdAsync` |
| `PUT orders/{id}/feasibility` | `TechnicalOrdersController` | `TechnicalOrderService.UpdateFeasibilityAsync` |
| `PUT orders/{id}/installation-schedule` | `TechnicalOrdersController` | `TechnicalOrderService.UpdateScheduleAsync` |
| `POST orders/{id}/provide-connection` | `ConnectionsController` | `ConnectionService.ProvideConnectionAsync` |
| `GET connections` | `ConnectionsController` | `ConnectionService.GetConnectionsAsync` |
| `GET connections/{accountId}` | `ConnectionsController` | `ConnectionService.GetConnectionByIdAsync` |
| `PUT connections/{accountId}/status` | `ConnectionsController` | `ConnectionService.UpdateStatusAsync` |
| `GET connections/{accountId}/activity-logs` | `ConnectionsController` | `ConnectionService.GetActivityLogsAsync` |
| `GET equipment` | `EquipmentController` | `EquipmentService.GetEquipmentAsync` |
| `GET equipment/{id}` | `EquipmentController` | `EquipmentService.GetEquipmentByIdAsync` |
| `POST equipment` | `EquipmentController` | `EquipmentService.CreateEquipmentAsync` |
| `PUT equipment/{id}` | `EquipmentController` | `EquipmentService.UpdateEquipmentAsync` |
| `GET inventory` | `InventoryController` | `InventoryService.GetInventoryAsync` |
| `GET inventory/{id}` | `InventoryController` | `InventoryService.GetInventoryByIdAsync` |
| `PUT inventory/{id}` | `InventoryController` | `InventoryService.UpdateInventoryAsync` |
| `POST inventory/{id}/adjust-stock` | `InventoryController` | `InventoryService.AdjustStockAsync` |

`connections/search` là alias của `GET connections`, vì vậy hai route dùng cùng service method.

## 10. Cách debug để hiểu luồng

Ví dụ debug API cấp kết nối:

1. Đặt breakpoint tại `ConnectionsController.ProvideConnection`.
2. Đặt breakpoint tại `ConnectionService.ProvideConnectionAsync`.
3. Chạy project bằng Debug.
4. Gửi request từ Swagger hoặc `doanki3API.http`.
5. Dùng `F10` để chạy từng dòng, `F11` để đi vào method.
6. Quan sát `request`, `order`, `equipment`, `accountId` và `connection` trong cửa sổ Locals.
7. Dừng tại `SaveChangesAsync` để xem entity nào đang chuẩn bị được ghi.

Bạn cũng có thể xem SQL do EF sinh bằng cách tăng log level của `Microsoft.EntityFrameworkCore.Database.Command` trong `appsettings.Development.json`.

## 11. Cách thêm một API mới đúng cấu trúc

Ví dụ muốn thêm API lấy thiết bị theo kỹ thuật viên:

1. Tạo response DTO nếu dữ liệu trả về chưa có DTO phù hợp.
2. Thêm method vào `EquipmentService`:

```csharp
public async Task<IReadOnlyList<EquipmentResponse>> GetByTechnicianAsync(
    string technicianId,
    CancellationToken cancellationToken)
{
    return await Project(_context.Equipment
            .AsNoTracking()
            .Where(item => item.AssignedTechnicianId == technicianId))
        .ToListAsync(cancellationToken);
}
```

3. Thêm action mỏng vào `EquipmentController`:

```csharp
[HttpGet("technicians/{technicianId}/equipment")]
public async Task<IActionResult> GetByTechnician(
    string technicianId,
    CancellationToken cancellationToken)
{
    var equipment = await _equipmentService.GetByTechnicianAsync(
        technicianId,
        cancellationToken);

    return Ok(equipment);
}
```

4. Build dự án.
5. Kiểm tra route trong Swagger.
6. Thêm request mẫu vào `doanki3API.http`.

Nguyên tắc cần giữ:

- Controller không chứa nghiệp vụ database dài.
- Service không trả `IActionResult`.
- Input/output API dùng DTO.
- Query chỉ đọc dùng `AsNoTracking()`.
- Luôn truyền `CancellationToken` vào method async của EF.
- Trạng thái dùng class trong `Constants`, tránh viết chuỗi lặp lại.
- Nghiệp vụ cập nhật nhiều bảng nên dùng transaction nếu cần tính nguyên tử.

## 12. Một số giới hạn hiện tại

- API chưa có authentication/authorization, vì vậy chưa xác định người dùng từ JWT.
- `performedBy`, `checkedBy` và `technicianId` hiện được client gửi lên.
- Lý do điều chỉnh kho mới được ghi vào application log, chưa có bảng lịch sử kho riêng.
- Các API danh sách chưa có phân trang.
- `Order.EmployeeId` đang được dùng khi phân công kỹ thuật viên; nếu database phân biệt nhân viên bán hàng và kỹ thuật viên thì nên thêm `AssignedTechnicianId` riêng.

Các giới hạn này không làm thay đổi cách đọc code hiện tại, nhưng cần được xử lý trước khi đưa hệ thống vào môi trường production.
