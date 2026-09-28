# Gửi email & Đăng nhập OTP

Tài liệu cho 3 luồng email của hệ thống và cơ chế đăng nhập 2 bước của khách hàng.

## 1. Ba luồng email

| # | Thời điểm gửi | Nội dung | Kích hoạt tại |
|---|---|---|---|
| 1 | Khách đặt đơn đăng ký dịch vụ | **Mã đơn hàng** (VD `B0000000009`) để tra cứu tiến độ | `TechnicalOrderService.CreateOrderAsync` |
| 2 | Kế toán ghi nhận thanh toán | **Mã tài khoản** (VD `T064-000000000001`) — cũng là mã đăng nhập | `BillingService.RecordPaymentAsync` |
| 3 | Khách nhập mã tài khoản ở màn đăng nhập | **Mã OTP 6 chữ số**, hiệu lực 5 phút | `CustomerOtpController.RequestOtp` |

Email số 2 có 2 biến thể, phân biệt bằng số lần thanh toán trước đó của tài khoản:

- **Lần thanh toán đầu tiên** → tiêu đề *"Kích hoạt tài khoản"*, nội dung báo tài khoản đã được kích hoạt.
- **Các lần sau** → tiêu đề *"Đã nhận thanh toán"*, vẫn nhắc lại mã đăng nhập.

> Gửi email **không bao giờ làm hỏng nghiệp vụ chính**. Đơn hàng và khoản thanh toán được lưu
> vào CSDL xong xuôi rồi mới gửi mail; mail lỗi chỉ ghi log, dữ liệu vẫn nguyên vẹn.

## 2. Cấu hình Gmail

### Bước 1 — Tạo App Password

Gmail không cho đăng nhập SMTP bằng mật khẩu thường. Phải tạo App Password:

1. Vào **Google Account → Security**
2. Bật **2-Step Verification** (bắt buộc, nếu chưa bật thì không thấy mục App passwords)
3. Vào **App passwords**, tạo mật khẩu mới → nhận chuỗi **16 ký tự**
4. Chép chuỗi đó (bỏ khoảng trắng) vào cấu hình bên dưới

### Bước 2 — Điền vào `appsettings.json`

```json
"Email": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "User": "tendangnhap@gmail.com",
  "Password": "abcdefghijklmnop",
  "FromName": "Nexus Telecom",
  "FromAddress": "",
  "TimeoutSeconds": 15
}
```

- `FromAddress` để trống thì lấy theo `User`.
- Cổng `587` dùng STARTTLS, cổng `465` dùng SSL ngay từ đầu — code tự chọn theo số cổng.

### Đừng commit mật khẩu

App Password nằm trong `appsettings.json` là văn bản thường. An toàn hơn thì dùng **biến môi trường**
(ASP.NET Core tự đọc, không cần sửa code — hai dấu gạch dưới thay cho dấu chấm):

```powershell
$env:Email__User = "tendangnhap@gmail.com"
$env:Email__Password = "abcdefghijklmnop"
dotnet run
```

Hoặc dùng user-secrets khi phát triển:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Email:Password" "abcdefghijklmnop"
```

## 3. Chế độ chưa cấu hình (test không cần Gmail)

Để trống `User` hoặc `Password` thì hệ thống **không gửi mail thật** mà ghi toàn bộ nội dung
email ra file HTML trong:

```
BE/be_ki3/doanki3API/logs/emails/
```

Tên file gồm thời gian + tiêu đề, ví dụ `20260921_202639_307_[Nexus] Mã OTP đăng nhập_ 079386.html`.
Mở bằng trình duyệt để xem đúng giao diện email. Nhờ vậy vẫn chạy thử trọn vẹn luồng OTP
khi chưa có App Password.

## 4. Đăng nhập 2 bước của khách hàng

### Luồng

```
Khách nhập Mã tài khoản  ──POST /api/auth/otp/request──►  Backend gửi OTP về email
        │                                                         │
        ▼                                                         ▼
Khách nhập OTP 6 chữ số  ──POST /api/auth/otp/verify───►  Trả JWT + thông tin tài khoản
```

Nhân viên **không** dùng luồng này — vẫn đăng nhập bằng email + mật khẩu qua `POST /api/auth/login`.

### `POST /api/auth/otp/request`

```json
// Request
{ "accountId": "T064-000000000001" }

// 200 OK
{
  "maskedEmail": "of****@highlineconsulting.com",
  "expiresInSeconds": 300,
  "resendAfterSeconds": 60,
  "message": "Mã OTP đã được gửi tới of****@highlineconsulting.com."
}
```

Mã tài khoản chấp nhận mọi cách gõ: `T064-000000000001`, `t064000000000001`, `T064 0000 0000 0001`.

| Mã lỗi | Khi nào |
|---|---|
| `404` | Không có kết nối nào mang mã tài khoản này |
| `409` | Tài khoản chưa có email đăng ký |
| `429` | Vừa xin mã cách đây chưa tới 60 giây |
| `502` | Không gửi được email (SMTP lỗi) |

### `POST /api/auth/otp/verify`

```json
// Request
{ "accountId": "T064-000000000001", "otp": "079386" }

// 200 OK
{
  "token": "eyJhbGciOi...",
  "user": {
    "id": "CUST-0001",
    "name": "Highline Consulting LLC",
    "email": "office@highlineconsulting.com",
    "role": "Customer",
    "accountType": "Customer",
    "accountId": "T064-000000000001"
  }
}
```

Trả `401` kèm thông báo tiếng Việt khi: chưa xin mã, mã hết hạn, nhập sai, hoặc sai quá 5 lần.

## 5. Cơ chế bảo vệ mã OTP

| Biện pháp | Giá trị | Ghi chú |
|---|---|---|
| Hiệu lực | 5 phút | `LoginOtpService.ValidMinutes` |
| Số lần nhập sai tối đa | 5 | Quá số lần thì mã bị vô hiệu, phải xin mã mới |
| Chờ giữa 2 lần xin mã | 60 giây | Tránh làm ngập hộp thư khách |
| Dùng một lần | ✔ | Xác thực đúng là mã bị đánh dấu đã dùng ngay |
| Xin mã mới | Huỷ mã cũ | Không bao giờ có 2 mã cùng dùng được |
| Lưu trữ | **Chỉ lưu SHA-256**, không lưu mã gốc | Xem CSDL cũng không đọc được mã |
| So sánh | `CryptographicOperations.FixedTimeEquals` | Không lộ thông tin qua độ trễ phản hồi |
| Sinh mã | `RandomNumberGenerator` | Ngẫu nhiên an toàn mật mã, không dùng `Random` |

OTP lưu trong MongoDB (`NexusChat.login_otps`) với **TTL index** trên `ExpiresAt`, nên mã hết hạn
được MongoDB tự xoá — không cần job dọn dẹp.

## 6. Kiểm tra nhanh khi chưa cấu hình SMTP

```powershell
# 1. Xin OTP
Invoke-RestMethod -Uri "http://localhost:5105/api/auth/otp/request" -Method Post `
  -ContentType 'application/json' -Body '{"accountId":"T064-000000000001"}'

# 2. Lấy mã trong tên file email vừa sinh ra
Get-ChildItem "BE\be_ki3\doanki3API\logs\emails" | Sort-Object LastWriteTime | Select-Object -Last 1

# 3. Xác thực
Invoke-RestMethod -Uri "http://localhost:5105/api/auth/otp/verify" -Method Post `
  -ContentType 'application/json' -Body '{"accountId":"T064-000000000001","otp":"079386"}'
```

## 7. Lưu ý về lối tắt Demo ở màn hình đăng nhập

Hai nút tài khoản demo trong trang đăng nhập **đi thẳng vào hệ thống, bỏ qua OTP**. Lý do:
email của dữ liệu mẫu là tên miền không có thật (`apexlegal.org`, `highlineconsulting.com`),
nên khi cấu hình Gmail thật thì mail không tới được và demo sẽ tắc. Nút đã ghi rõ điều này
ngay dưới tiêu đề. Muốn demo cả luồng OTP thì đổi email của khách mẫu trong CSDL sang một
hộp thư thật của bạn.
