## Kế hoạch sửa lỗi đăng nhập mật khẩu nhân viên

### Nguyên nhân đã xác định
- `POST /api/auth/login` đã tìm nhân viên và kiểm tra BCrypt đúng cách; không cần đổi thuật toán xác thực.
- Dữ liệu seed trong `backend/NexusSystem_Database.sql` dùng cùng một BCrypt hash tương ứng với mật khẩu demo `1234567890`.
- API tạo nhân viên mới lại dùng mặc định `Nexus@123` nếu request không truyền mật khẩu, trong khi giao diện và tài khoản demo đang dùng `1234567890`. Vì vậy nhân viên tạo từ một số luồng có thể không đăng nhập được bằng mật khẩu mà giao diện hiển thị.
- `docker-compose.yml` đang mount sai đường dẫn seed (`./frontend/NexusSystem_Database.sql` thay vì `./backend/NexusSystem_Database.sql`).
- `start.bat` chỉ khởi động SQL Server Docker nhưng lại chạy API local với connection string LocalDB, nên API có thể đọc một database khác/không có nhân viên seed và trả về lỗi mật khẩu cho mọi tài khoản.

### Thay đổi dự kiến
1. **Đồng bộ mật khẩu mặc định của nhân viên**
   - Đổi fallback khi tạo nhân viên trong `backend/doanki3API/Controllers/Admin/EmployeeController.cs` thành `1234567890`, đồng nhất với seed và giao diện hiện tại.
   - Cập nhật chú thích DTO trong `backend/doanki3API/DTOs/Admin/EmployeeRequests.cs` để không còn mô tả sai `Nexus@123`.
   - Giữ nguyên cơ chế: nếu admin truyền mật khẩu thì hash đúng mật khẩu đó; chỉ dùng mặc định khi bỏ trống.

2. **Sửa nguồn dữ liệu seed của Docker**
   - Đổi volume mount trong `docker-compose.yml` sang `./backend/NexusSystem_Database.sql` để service khởi tạo thực sự tìm thấy schema và tài khoản nhân viên.

3. **Sửa script khởi động local theo đúng database Docker**
   - Cho `start.bat` khởi động cả `sqlserver`, `mongodb` và bước `sqlserver-init`.
   - Truyền connection string SQL Server Docker (`localhost,1433`, database `NexusSystem`, tài khoản `sa`) cho process backend local, tránh rơi về `(localdb)\\MSSQLLocalDB` không chứa seed đang dùng.
   - Không thay đổi cấu hình LocalDB mặc định trong `appsettings.json`, để không phá luồng chạy thủ công của người dùng đang dùng LocalDB; chỉ sửa launcher đã chọn Docker.

4. **Kiểm tra sau sửa**
   - Build backend bằng `dotnet build`.
   - Chạy `pnpm check` và `pnpm build` cho frontend để bảo đảm các thay đổi hiện có không bị ảnh hưởng.
   - Khởi động stack theo cấu hình đã sửa và gọi `POST /api/auth/login` với cả 5 email seed, mật khẩu `1234567890`; kiểm tra HTTP 200, JWT không rỗng, `accountType = Employee`, role đúng.
   - Kiểm tra mật khẩu sai vẫn trả HTTP 401.
   - Tạo thử một nhân viên không truyền mật khẩu rồi đăng nhập bằng mật khẩu mặc định để xác nhận hai luồng đã dùng cùng một quy ước.

Chỉ các file nêu trên sẽ được chỉnh sửa; các thay đổi/deletions đang có sẵn trong working tree sẽ được giữ nguyên.