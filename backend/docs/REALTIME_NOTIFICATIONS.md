# Thông báo real-time — SignalR + MongoDB

Tài liệu cho tính năng thông báo tức thì: mỗi khi có sự kiện nghiệp vụ (khách đặt đơn,
bán lẻ duyệt hồ sơ, kế toán thu tiền, kho sắp hết hàng, khách nhắn tin...), server đẩy
thẳng thông báo xuống trình duyệt của đúng người cần biết.

## 1. Vì sao dùng SignalR thay cho polling?

Trước đây frontend phải hỏi lại server theo giờ: chuông thông báo 15 giây/lần, chat 4 giây/lần.
Cách đó tốn hàng trăm request rỗng mỗi phút và vẫn trễ tới cả chục giây.

SignalR giữ sẵn một kết nối WebSocket giữa trình duyệt và API. Khi có sự kiện, server
"đẩy" (push) xuống ngay. Đo thực tế trên máy local: từ lúc khách bấm gửi tin đến lúc
chuông của Admin đổi số là **318 ms**, không tải lại trang.

SignalR là thư viện có sẵn của ASP.NET Core (`builder.Services.AddSignalR()`), không cần
cài thêm gói NuGet. Phía frontend dùng `@microsoft/signalr`.

## 2. Kiến trúc

```
Nghiệp vụ (TechnicalOrderService, BillingService, ConnectionService,
           InventoryService, ChatService, FeedbackController)
        │  await _notifications.OrderPlacedAsync(...)
        ▼
NotificationService ──► MongoDB "notifications"   (lưu lịch sử, TTL 30 ngày)
        │
        └──────────────► IHubContext<NotificationHub>
                                │  gửi tới đúng group
                                ▼
                     Trình duyệt (src/lib/realtime.ts)
                                │
                                ▼
                     NotificationContext ──► chuông + toast + tự tải lại bảng
```

| Thành phần | Tệp |
|---|---|
| Hub (kênh WebSocket) | `Hubs/NotificationHub.cs` |
| Lưu + đẩy thông báo | `Services/Notifications/NotificationService.cs` |
| Nội dung từng sự kiện | `Services/Notifications/NotificationEvents.cs` |
| Document trong Mongo | `Models/Notifications/NotificationDocument.cs` |
| API đọc/đánh dấu đã đọc | `Controllers/Notifications/NotificationsController.cs` |
| Tên group | `Constants/NotificationAudiences.cs` |
| Kết nối phía frontend | `doanki3fe/src/lib/realtime.ts` |
| Kho thông báo phía frontend | `doanki3fe/src/context/NotificationContext.ts` |
| Giao diện chuông | `doanki3fe/src/components/layout/NotificationDropdown.svelte` |

## 3. Người nhận (Audience) — cũng là tên group trên hub

| Khoá | Ai nhận |
|---|---|
| `role:admin` | Quản trị |
| `role:retail` | Nhân viên cửa hàng bán lẻ |
| `role:technical` | Kỹ thuật |
| `role:accounts` | Kế toán |
| `account:B064000000000002` | Đúng một khách hàng (mã tài khoản đã bỏ dấu gạch) |
| `chat:SESSION-xxxx` | Khách đang mở một phiên chat (kể cả chưa đăng nhập) |

Một sự kiện gửi cho nhiều bên sẽ tạo **nhiều document**, mỗi bên một bản. Nhờ vậy Admin
đọc rồi cũng không làm mất chấm đỏ của Bán lẻ.

## 4. Bảng sự kiện

| Sự kiện nghiệp vụ | Phát tại | Ai nhận | Bấm vào mở tới |
|---|---|---|---|
| Khách đặt đơn mới | `TechnicalOrderService.CreateOrderAsync` | Kỹ thuật (đơn `Pending`) hoặc Bán lẻ (đơn `PendingRetail`) | Hàng đợi khảo sát / Duyệt hồ sơ chi nhánh |
| Bán lẻ duyệt hồ sơ | `UpdateOrderStatusAsync` | Kỹ thuật | `/technical` → Hàng đợi khảo sát |
| Bán lẻ từ chối hồ sơ | `UpdateOrderStatusAsync` | Khách (nếu đã có mã TK) | `/user` → Tổng quan |
| Nộp lại hồ sơ | `UpdateOrderStatusAsync` | Bán lẻ | `/retail` → Duyệt hồ sơ chi nhánh |
| Phân công kỹ thuật viên | `UpdateOrderStatusAsync` | Kỹ thuật | `/technical` → Hàng đợi khảo sát |
| Kết luận khả thi / không khả thi | `UpdateFeasibilityAsync` | Bán lẻ | `/retail` → Tra cứu đơn hàng |
| Cấp kết nối xong | `ConnectionService.ProvideConnectionAsync` | Kế toán + khách | `/accounts` → Lập hoá đơn |
| Đổi trạng thái đường truyền | `ConnectionService.UpdateStatusAsync` | Kế toán + khách | `/accounts` → Theo dõi thuê bao |
| Lập hoá đơn | `BillingService.CreateBillAsync` | Bán lẻ + khách | `/retail` → Thu tiền tại quầy |
| Ghi nhận thanh toán | `BillingService.RecordPaymentAsync` | Kế toán + khách | `/accounts` → Theo dõi thuê bao |
| Kho chạm ngưỡng đặt lại | `InventoryService` | Quản trị | `/admin` → Kho thiết bị |
| Khách nhắn tin chat | `ChatService.SendMessageAsync` | Quản trị | `/admin` → Chat trực tuyến |
| Khách gửi phản hồi | `FeedbackController.CreateFeedback` | Quản trị | `/admin` → Phản hồi KH |
| Trả lời phản hồi | `FeedbackController.UpdateFeedback` | Khách | `/user` → Phản hồi |

### Gộp thông báo trùng (`ReplaceKey`)

Tin lặp lại dùng `ReplaceKey` để bản mới xoá bản cũ **chưa đọc**:

- `chat:{sessionId}` — khách nhắn 10 câu vẫn chỉ một dòng thông báo.
- `stock:{inventoryId}` — nhập xuất kho liên tục không làm ngập chuông.

## 5. Sự kiện hub

Server đẩy xuống (tên trong `NotificationHub`):

| Sự kiện | Nội dung | Ai nhận |
|---|---|---|
| `notification` | Một `NotificationDto` mới | group của người nhận |
| `notificationRead` | `{ id }` — đã đọc ở nơi khác | group của người nhận |
| `notificationsRead` | Đã đọc tất cả | group của người nhận |
| `chatMessage` | Tin nhắn chat mới | group phiên chat + `role:admin` |
| `chatSession` | Trạng thái phiên chat (tin cuối, số chưa đọc) | `role:admin` |
| `dataChanged` | `{ entity, id }` — bảng dữ liệu vừa đổi | tất cả |

Client gọi lên hub: `JoinRole` / `LeaveRole`, `JoinAccount` / `LeaveAccount`,
`JoinChatSession` / `LeaveChatSession`.

`dataChanged` khiến frontend gọi lại `nexusStore.syncWithDatabase()` (gộp trong 700 ms),
nên đơn hàng người khác vừa duyệt tự hiện ra trong bảng mà không cần bấm F5.

## 6. API còn lại (chỉ dùng lúc mở trang và khi bấm "đã đọc")

| Method | Đường dẫn | Ý nghĩa |
|---|---|---|
| GET | `/api/notifications?audience=role:admin&limit=50` | Lịch sử, mới nhất lên đầu |
| GET | `/api/notifications/count?audience=role:admin` | `{ unread, total }` |
| POST | `/api/notifications/{id}/read?audience=role:admin` | Đánh dấu một thông báo đã đọc |
| POST | `/api/notifications/read-all?audience=role:admin` | Đánh dấu tất cả đã đọc |

`audience` sai định dạng sẽ bị trả **400**, Mongo không chạy trả **503**.

## 7. Cấu hình

`appsettings.json` — dùng chung MongoDB với module chat:

```json
"Mongo": {
  "ConnectionString": "mongodb://localhost:27017",
  "Database": "NexusChat",
  "SessionsCollection": "chat_sessions",
  "MessagesCollection": "chat_messages",
  "NotificationsCollection": "notifications"
}
```

`Program.cs`:

```csharp
builder.Services.AddSignalR();
builder.Services.AddSingleton<NotificationService>();
...
app.MapHub<NotificationHub>(NotificationHub.Path);   // /hubs/notifications
```

CORS đã bật `AllowCredentials()` + `SetIsOriginAllowed`, đủ cho WebSocket từ
`http://localhost:3000`.

Index trong Mongo được tạo tự động lúc khởi động:

- `{ Audience: 1, CreatedAt: -1 }` — truy vấn danh sách.
- TTL trên `ExpiresAt` — thông báo quá **30 ngày** tự biến mất, không cần job dọn.

## 8. Chịu lỗi

Thông báo là việc phụ, không được làm hỏng nghiệp vụ chính:

- Mọi lệnh gửi SignalR và ghi Mongo đều bọc `try/catch`, lỗi chỉ ghi log.
- Mongo sập: vẫn đẩy được cho người đang mở máy, chỉ mất phần lịch sử.
- API tắt: frontend tự thử nối lại (0s → 2s → 5s → 10s → 20s → 30s) và tự vào lại group cũ.
- Mất kênh real-time: chat quay về hỏi server mỗi 5–6 giây như cũ, nên không bao giờ "chết".
- Hộp thông báo hiện rõ trạng thái: *Đang nhận thông báo trực tiếp* / *Đang kết nối* / *Mất kết nối*.

## 9. Hạn chế đã biết

Hub **chưa gắn `[Authorize]`** vì tài khoản nhân viên demo đăng nhập bằng nút chọn vai trò
nên không có JWT. Về lý thuyết một người có thể tự xin vào group `role:admin` để nghe
thông báo của Quản trị. Khi triển khai thật cần:

```csharp
[Authorize]
public sealed class NotificationHub : Hub { ... }
```

và lấy vai trò từ `Context.User` thay vì tin vào tham số client gửi lên. API
`/api/notifications` cũng cần chặn theo cùng nguyên tắc.

## 10. Cách kiểm tra nhanh

1. Chạy MongoDB, API (`dotnet run --launch-profile http`) và frontend (`pnpm run dev`).
2. Cửa sổ 1: đăng nhập Admin (`sarah.jenkins@nexus.telecom` / `1234567890`).
3. Cửa sổ 2 (ẩn danh): vào trang chủ, mở chatbox góc phải, nhập tên và gửi một tin.
4. Cửa sổ 1 **không tải lại trang**: chuông tự nhảy số, toast hiện ra, bấm vào là mở thẳng
   phiên chat đó ở tab *Chat trực tuyến*.
5. Admin trả lời → cửa sổ 2 thấy ngay câu trả lời.
