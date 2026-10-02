## Mục tiêu
Sửa các luồng kỹ thuật để không còn báo thành công hoặc lưu trạng thái giả khi backend chưa ghi nhận:
1. Nút “Khả thi/Không khả thi” phải gọi đúng API feasibility và chỉ cập nhật UI/localStorage sau khi backend trả thành công.
2. Đổi trạng thái equipment phải gọi `PUT /api/Technical/equipment/{id}`; lỗi API phải giữ nguyên trạng thái cũ và báo lỗi.
3. Đăng ký equipment mới không được fallback lưu local khi backend lỗi; chỉ báo thành công sau HTTP 201.
4. Nút tạo EquipmentRequest phải mở form; chỉ submit khi người dùng gửi form hợp lệ.
5. Backend phải từ chối nhân viên thực hiện không tồn tại, không Active, hoặc không phù hợp vai trò; frontend không dùng ID fallback giả để gửi request.
6. Sau các mutation thành công, tải lại dữ liệu server để loại bỏ sai lệch với localStorage.

## Thay đổi dự kiến

### 1. Frontend feasibility
- `frontend/src/pages/TechnicalDashboard.svelte`
  - Thay các handler `handleMarkFeasible` và `handleConfirmNotFeasible` đang gọi `updateOrderStatus` bằng `updateFeasibility` trong `technicalApi.ts`.
  - Gửi đầy đủ `isFeasible`, `checkedBy`, ghi chú/lý do, thông tin hai nhánh Dial-Up và các thông số khảo sát hiện có.
  - Chặn thao tác nếu không tìm thấy nhân viên Field Engineer đang Active; không dùng `emp-03` làm fallback.
  - Chỉ gọi `refreshTechnicalData()` và hiển thị toast thành công sau khi API thành công; khi API lỗi không sửa order local/localStorage.
  - Đóng modal “Không khả thi” chỉ sau khi request thành công.
- `frontend/src/context/NexusContext.ts`
  - Không tiếp tục dùng `updateOrderStatus` cho hai nút feasibility này; nếu còn giữ API tổng quát cho luồng khác thì loại bỏ hành vi optimistic/fake Account ID khỏi đường đi của feasibility.
  - Đảm bảo dữ liệu order chỉ được persist sau dữ liệu server trả về hoặc sau lần refresh thành công.

### 2. Frontend equipment CRUD
- `frontend/src/context/NexusContext.ts`
  - Sửa `addEquipment` truyền đủ `equipmentId` (nếu cần), `inventoryId`, `storeId`, `vendorId`, serial/MAC/model/type/status/firmware lên API.
  - Xóa fallback tạo ID và thêm equipment vào store khi API thất bại; rethrow lỗi để page hiển thị lỗi.
  - Thêm handler cập nhật equipment theo kiểu server-first (hoặc dùng trực tiếp wrapper hiện có), sau khi thành công map response/refresh workspace; không cho `updateEquipment` hiện tại chỉ sửa writable store dùng cho thao tác backend.
- `frontend/src/lib/api.ts` hoặc `frontend/src/lib/technicalApi.ts`
  - Chuẩn hóa payload/response lỗi của create/update equipment, bao gồm lỗi mạng, 401/403, validation 400 và conflict 409.
- `frontend/src/pages/TechnicalDashboard.svelte`
  - `handleSaveEquipment` chỉ đóng form/reset và báo “đã lưu CSDL” sau khi POST thành công.
  - `handleEquipmentStatusChange` chỉ refresh và báo thành công sau PUT thành công; khi thất bại không thay đổi status local.
  - Không hiển thị equipment fallback local như đã đăng ký thành công.

### 3. EquipmentRequest form và đồng bộ
- `frontend/src/pages/TechnicalDashboard.svelte`
  - Sửa `openEquipmentRequestModal` để chỉ khởi tạo `equipmentRequestForm` và đặt `isEquipmentRequestModalOpen = true`; bỏ auto-call `addEquipmentRequest`.
  - Giữ trường hợp mở từ provisioning theo cùng form, nhưng vẫn để nhân viên xem/sửa và bấm submit rõ ràng; tránh tạo request mặc định/duplicate.
  - Gọi `refreshEquipmentRequests()` khi dashboard mount và sau các mutation liên quan.
- `frontend/src/pages/AdminDashboard.svelte`
  - Gọi refresh request khi trang mount/đi vào khu vực request để danh sách không phụ thuộc dữ liệu khởi tạo rỗng.
  - Sau approve/reject/fulfill, chỉ cập nhật UI sau API thành công và refresh danh sách authoritative.
- `frontend/src/context/NexusContext.ts` và `frontend/src/context/NotificationContext.ts` (nếu cần)
  - Bổ sung refresh EquipmentRequest vào đường đồng bộ realtime hoặc giữ refresh chủ động ở các dashboard; không lưu request giả vào localStorage.
  - Không thêm persistence local cho request/equipment mutation thất bại; localStorage chỉ phản ánh dữ liệu đã xác nhận từ backend.

### 4. Backend nhân viên thực hiện
- `backend/doanki3API/Controllers/Technical/EquipmentRequestsController.cs`
  - Trong POST tạo EquipmentRequest, lấy employee ID từ JWT và yêu cầu nhân viên tồn tại, `Status == Active`, và thuộc nhóm quyền kỹ thuật phù hợp.
  - Bỏ fallback sang nhân viên Field Engineer bất kỳ và bỏ hardcode `emp-03`; trả lỗi 400/403 rõ ràng khi claim thiếu, nhân viên không tồn tại, không Active hoặc không được phép thực hiện.
  - Có thể dùng cùng thông báo chuẩn “Nhân viên thực hiện không tồn tại hoặc đã ngừng hoạt động” để frontend hiển thị nhất quán.
- `backend/doanki3API/Services/TechnicalOrderService.cs` và các controller liên quan
  - Giữ/đồng bộ validation `checkedBy` cho feasibility; xác nhận mọi đường đi từ nút feasibility đều qua validation Active hiện có.
  - Nếu endpoint generic status còn được gọi cho nghiệp vụ feasibility, loại bỏ hoặc chặn việc dùng nó để tránh ghi status thiếu audit/account ID; ưu tiên dedicated `/feasibility` endpoint.
- Có thể cập nhật DTO/role validation nếu cần sau khi đối chiếu claim role thực tế, nhưng không thay đổi schema nếu validation hiện có đáp ứng.

## Kiểm thử và xác nhận
1. Chạy `dotnet build backend/doanki3API/doanki3API.csproj`.
2. Chạy `cd frontend && pnpm check && pnpm build`.
3. Manual/API verification với JWT:
   - Feasible/Not Feasible thành công: DB có status, checkedBy/date, notes/rejection và Account ID đúng; reload không mất dữ liệu.
   - Feasibility với employee ID không tồn tại/inactive: API trả lỗi, UI không đổi/localStorage không đổi.
   - Đổi equipment status thành công: DB đổi status; lỗi API: status UI giữ nguyên và không có toast thành công.
   - POST equipment khi backend down/400/409: không có row local-only và không báo “đã lưu CSDL”.
   - Nút New Equipment Request mở form; submit hợp lệ tạo đúng một request; backend lỗi không tạo bản ghi local giả.
   - Tạo request bằng employee không tồn tại/inactive: bị từ chối, không có request orphan/fallback employee.
4. Kiểm tra git diff chỉ gồm các file thuộc các luồng trên và không hoàn tác các thay đổi có sẵn trong working tree.