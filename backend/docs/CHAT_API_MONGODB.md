# Module Live Chat — MongoDB

Tài liệu cho phần chat trực tuyến giữa khách ở trang chủ và Admin.
Đây là module duy nhất trong hệ thống **không dùng SQL Server**.

## 1. Vì sao chat nằm ở MongoDB?

| Tiêu chí | Dữ liệu nghiệp vụ (SQL Server) | Tin nhắn chat (MongoDB) |
|---|---|---|
| Ràng buộc quan hệ | Nhiều khoá ngoại: Order ↔ Customer ↔ Plan ↔ Bill | Gần như không có |
| Hình dạng | Cột cố định, cần CHECK constraint | Document, dễ thêm trường (ảnh, trạng thái gõ...) |
| Khối lượng ghi | Vừa phải | Rất nhiều, ghi liên tục |
| Giao dịch | Bắt buộc (tiền, hoá đơn) | Không cần |

Tin nhắn chat là dữ liệu dạng document, sinh ra nhanh và không cần toàn vẹn quan hệ,
nên tách sang MongoDB giúp bảng SQL không phình to vì log hội thoại.

## 2. Cấu hình

Trong `appsettings.json`:

```json
"Mongo": {
  "ConnectionString": "mongodb://localhost:27017",
  "Database": "NexusChat",
  "SessionsCollection": "chat_sessions",
  "MessagesCollection": "chat_messages"
}
```

Đăng ký trong `Program.cs`:

```csharp
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.AddSingleton<ChatService>();
```

`ChatService` đăng ký **singleton** vì `MongoClient` đã tự quản lý connection pool bên trong;
tạo lại mỗi request sẽ làm cạn pool.

Gói NuGet: `MongoDB.Driver` 3.12.0.

> MongoDB không chạy thì **API vẫn khởi động bình thường**, chỉ các endpoint chat trả về
> `503` kèm thông báo rõ ràng. Các module SQL Server không bị ảnh hưởng.

## 3. Cấu trúc dữ liệu

### Collection `chat_sessions`

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `_id` (SessionId) | string | `SESSION-<guid>`, khách giữ trong localStorage để quay lại xem tiếp |
| `CustomerName` | string | Tên khách tự khai |
| `LastMessage` | string | Tin cuối, để hiện nhanh ở danh sách bên Admin |
| `LastSenderType` | string | `guest` hoặc `admin` |
| `LastMessageAt` | DateTime (UTC) | Dùng để sắp xếp danh sách |
| `UnreadCount` | int | Số tin của khách mà Admin chưa đọc |
| `MessageCount` | int | Tổng số tin trong phiên |
| `CreatedAt` | DateTime (UTC) | Thời điểm mở phiên |

### Collection `chat_messages`

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `_id` | ObjectId | Khoá chính |
| `SessionId` | string | Trỏ về phiên |
| `SenderType` | string | `guest` hoặc `admin` |
| `SenderName` | string | Tên hiển thị người gửi |
| `Message` | string | Nội dung |
| `CreatedAt` | DateTime (UTC) | Thời điểm gửi |

### Index

- `chat_messages`: `{ SessionId: 1, CreatedAt: 1 }` — lọc tin theo phiên và sắp theo thời gian.
- `chat_sessions`: `{ LastMessageAt: -1 }` — danh sách phiên mới nhất lên đầu.

Index được tạo tự động lần đầu `ChatService` khởi tạo. Nếu Mongo chưa sẵn sàng thì chỉ
ghi cảnh báo, không làm sập API.

## 4. Danh sách endpoint

Tất cả đều **không yêu cầu đăng nhập**, vì khách vãng lai chưa có tài khoản cũng phải chat được.

### `POST /api/chat/sessions`

Khách bắt đầu phiên mới. Server tự chèn lời chào từ Admin.

```json
// Request
{ "customerName": "Nguyen Van A" }

// 200 OK
{
  "sessionId": "SESSION-fcd60f06dbd14f8694b815009e3d25e9",
  "customerName": "Nguyen Van A",
  "lastMessage": "Xin chào, chúng tôi có thể giúp gì cho bạn?",
  "lastSenderType": "admin",
  "lastMessageAt": "2026-09-21T12:56:37.8980589Z",
  "lastMessageAtDisplay": "Vừa xong",
  "unreadCount": 0,
  "messageCount": 1
}
```

### `GET /api/chat/sessions?search=<tên khách>`

Admin xem danh sách phiên, mới nhất lên đầu, tối đa 200 phiên.
Tham số `search` lọc theo tên khách (không phân biệt hoa thường, đã escape ký tự regex).

### `GET /api/chat/sessions/{sessionId}/messages`

Toàn bộ tin nhắn của phiên, cũ trước mới sau. Trả `404` nếu phiên không tồn tại.

### `POST /api/chat/sessions/{sessionId}/messages`

Gửi tin. Dùng chung cho khách và Admin, phân biệt bằng `senderType`.

```json
{ "senderType": "guest", "senderName": "Nguyen Van A", "message": "Cho tôi hỏi gói Broadband" }
```

Quy tắc đếm tin chưa đọc:

- `senderType = "guest"` → `UnreadCount += 1`
- `senderType = "admin"` → `UnreadCount = 0` (Admin trả lời tức là đã đọc)

### `POST /api/chat/sessions/{sessionId}/read`

Admin mở phiên ra đọc → `UnreadCount = 0`. Trả `204 No Content`.

## 5. Mã lỗi

| Mã | Khi nào | Frontend xử lý |
|---|---|---|
| `400` | Thiếu tên khách / tin rỗng / quá 2000 ký tự | Hiện thông báo lỗi |
| `404` | `sessionId` không tồn tại | Xoá phiên hỏng trong localStorage, cho khách nhập tên chat lại |
| `503` | Không kết nối được MongoDB | Báo "dịch vụ chat tạm thời không khả dụng" |

## 6. Phía frontend

Sáu hàm trong `doanki3fe/src/lib/api.ts` gọi các endpoint trên:

| Hàm | Endpoint |
|---|---|
| `startChatSessionApi(customerName)` | `POST /sessions` |
| `fetchChatSessionsApi(search?)` | `GET /sessions` |
| `fetchChatMessagesApi(sessionId)` | `GET /sessions/{id}/messages` |
| `sendChatMessageApi(sessionId, senderName, message)` | `POST /sessions/{id}/messages` (`guest`) |
| `replyChatSessionApi(sessionId, adminName, message)` | `POST /sessions/{id}/messages` (`admin`) |
| `markChatSessionReadApi(sessionId)` | `POST /sessions/{id}/read` |

Nơi dùng: `components/chat/ChatBox.svelte` (khách), `pages/AdminDashboard.svelte` (tab Live chat),
`components/layout/NotificationDropdown.svelte` (chuông báo tin chưa đọc), `pages/UserDashboard.svelte`.

Giao diện **polling 4–5 giây một lần**, chưa dùng WebSocket/SignalR. Nếu sau này cần realtime
thì thay lớp polling bằng SignalR hub, phần lưu trữ MongoDB giữ nguyên.

## 7. Kiểm tra nhanh

```powershell
# Tạo phiên
$s = Invoke-RestMethod -Uri "http://localhost:5105/api/chat/sessions" -Method Post `
     -ContentType 'application/json' -Body '{"customerName":"Test"}'

# Khách gửi tin
Invoke-RestMethod -Uri "http://localhost:5105/api/chat/sessions/$($s.sessionId)/messages" `
  -Method Post -ContentType 'application/json' `
  -Body '{"senderType":"guest","senderName":"Test","message":"Xin chao"}'

# Admin xem danh sách
Invoke-RestMethod -Uri "http://localhost:5105/api/chat/sessions"
```
