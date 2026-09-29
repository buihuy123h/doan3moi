# NEXUS-SERVICE-MARKETING-SYSTEM.pdf — Tóm tắt & đối chiếu

Tài liệu này trích lại **toàn bộ nội dung nghiệp vụ** từ `NEXUS-SERVICE-MARKETING-SYSTEM.pdf`
(nguồn sự thật gốc), và đánh dấu những chỗ `BACKEND.md` / `doanki3fe/` **diễn giải hoặc
mở rộng khác spec**. Dùng để đọc lại khi cần quyết định thiết kế.

- **Metadata PDF**: title *"E – COMMUNICATION MARKETING SYSTEM"*, author `aparnat`,
  Word 2016, 7 trang, tạo 2026-09-09. Đây là đề bài dạng project brief (kiểu bài tập
  lớn của trung tâm đào tạo) — văn phong tiếng Anh không chuẩn hoá, nhiều chỗ mơ hồ.
- Tiền tệ trong spec: `$`. Thuế dịch vụ: `12.24%`.

---

## 1. Nghiệp vụ gốc (Introduction + Existing/Proposed Scenario)

- Nexus là nhà cung cấp **viễn thông + Internet** tại địa phương, đang mở rộng ra toàn vùng.
- Spec nói **"two types of connections"**: **Dial-Up** (bắt buộc phải có **landline** cùng
  nhà cung cấp) và **Broadband**. Nhưng phần bảng giá lại có mục riêng **Land Line
  Connection**, và Account/Order ID có prefix `T` = *"only telephone connection"*.
  → Thực chất là **3 loại**: `Dial-Up (D)`, `Broadband (B)`, `Landline/telephone-only (T)`.
- Khách đặt đơn tại **retail shop** ở thành phố của mình (không đến văn phòng chính).
- Sau khi nhận đơn → **khảo sát khả thi (feasibility)**; chỉ khi khu vực khả thi mới cấp
  kết nối. Khảo sát xét **khoảng cách**, **server availability**, v.v. theo "company patterns".
- Thiết bị (modem, router, ...) lấy từ **nhiều vendor sản xuất**. Admin (Manager) quản lý
  product, retail shop, vendor, purchase list, **và nhân viên (employee) — do admin quản lý duy nhất**.
- **Chỉ postpaid**: bill được sinh ra, và **trạng thái kết nối phụ thuộc vào bill**.
- Hệ thống cần thay giấy tờ thủ công bằng ứng dụng web tự động hoá.

---

## 2. CÂU HỎI THEN CHỐT: một đơn hàng có được nhiều loại kết nối không?

**KHÔNG — theo spec, một đơn = đúng một loại kết nối.**

Bằng chứng trong spec:

> *"Once the customer places an order for the connection he will be assigned with a unique
> **11 digit code** (Alpha numeric code in serial order, **the first digit being D for dial up,
> T for only telephone connection, B for broadband connection**, for example: for the first
> order for a dial-up connection it can be `D0000000001`) as the order id"*

- Order ID **mã hoá loại vào ký tự đầu** ⇒ một Order ID chỉ ứng với một loại.
- Account ID (cấp khi đã có kết nối) cũng bắt đầu bằng `D/B/T` ⇒ một kết nối = một loại.
- **"Dial-Up cần cả phone lẫn internet" KHÔNG phải ngoại lệ**: Dial-Up là *một* loại,
  chỉ khảo sát 2 lớp (landline + internet); vẫn 1 Order ID `D...`, 1 Account ID `D...`.
  Nếu khách đã giữ landline Nexus ⇒ chỉ khảo sát lớp internet.

### Hệ quả & khoảng trống của spec

- Không có thực thể "customer" gộp: **đăng nhập khách = theo từng Account ID**. Một công ty
  giữ 3 kết nối ⇒ 3 Account ID / 3 lần login độc lập. Spec không mô tả "tài khoản công ty".
- **Chiết khấu bulk tính theo "số connections taken"** (xem §4). Spec KHÔNG nói các connection
  đó phải cùng loại, nhưng vì mỗi đơn đơn-loại nên deal hỗn hợp tự nhiên vỡ thành nhiều đơn
  ⇒ mỗi đơn tính bậc chiết khấu riêng ⇒ công ty mua hỗn hợp bị thiệt.

### Khuyến nghị thiết kế

- **Cho đồ án: giữ nguyên "đơn đơn-loại"** — đúng spec, đúng định nghĩa Order ID, khớp barem.
- Nếu muốn xử lý deal corporate hỗn hợp: thêm `orderGroupId` (hoặc `corporateBatchId`) chung
  cho các đơn anh em, và tính bậc chiết khấu trên **tổng số kết nối của cả nhóm**. Giữ
  nguyên toàn bộ logic per-line (feasibility / prefix / provision / Account ID).
  → Đây là **mở rộng ngoài spec**, phải ghi chú rõ trong báo cáo, không đổi model ngầm.
- Phương án "Order header + OrderLine[] đa loại" là đúng nghiệp vụ telecom thật hơn nhưng
  **phá vỡ prefix `D/B/T` của Order ID trong spec** ⇒ chỉ làm nếu được phép lệch spec.

---

## 3. Định danh (ID scheme) — theo spec

### Order ID
- **11 ký tự**: `<prefix><10 số serial>`. Prefix: `D` Dial-Up · `B` Broadband · `T` telephone-only.
- Serial **tăng dần theo thứ tự, toàn hệ thống** (không reset theo loại). VD đơn Dial-Up đầu: `D0000000001`.
- Cấp **ngay khi khách đặt đơn**. Dùng để tra cứu trạng thái đơn.

### Account ID
- Spec (Proposed): **"unique 16 digit code as the account id (numeric code)"** — cấp
  **khi khách đã được cấp kết nối**.
- **Coding procedure (Note trong spec)**:
  `<1 ký tự loại D/B/T>` + `<3 số mã thành phố>` + `<12 số serial kết nối>`.
  → 1 + 3 + 12 = 16 ký tự. (Spec ghi "numeric" nhưng chính spec lại bắt đầu bằng chữ
  D/B/T ⇒ hiểu là `[DBT]` + 15 chữ số.)
- Mã thành phố: mỗi thành phố trong vùng có **một mã số 3 chữ số** riêng.
- Dùng để tra cứu: chi tiết liên hệ, chi tiết kết nối, số tiền còn nợ, trạng thái kết nối
  (active / temporarily inactive / permanently inactive).

> ⚠ **Existing Scenario** (hệ thống giấy cũ) nhắc "unique **10 digit** account id" — **KHÔNG dùng**.
> Bản Proposed là 16 ký tự. `sql-server/schema/02_account_id_login.sql` đang theo định dạng
> cũ, cần cập nhật (xem `BACKEND.md` §7).

---

## 4. Chiết khấu Bulk / Corporate — nguyên văn spec

> *"if 10-15 connections are taken, they will be getting a discount of 25% in the advance
> and as well in the security deposit. If it is between 15-25 connections, then 50% discount
> and for 25-50 then 75% discount and if it is above 50 they will avail 100% discount."*

| Số kết nối (spec) | Chiết khấu |
|---|---|
| 10–15 | 25% |
| 15–25 | 50% |
| 25–50 | 75% |
| > 50 | 100% |

- Áp lên **"the advance" (cước ứng trước) + "the security deposit" (tiền cọc)**.
- **Biên chồng lấn mơ hồ** trong spec (10-15 & 15-25 & 25-50 dùng chung mốc). `types/nexus.ts`
  → `getBulkDiscountPercent()` chốt: `>50 ⇒ 100`, `>=25 ⇒ 75`, `>=15 ⇒ 50`, `>=10 ⇒ 25`, còn lại `0`.
- Func Req #3: *"billing ... taking into account the customer entitlements in terms of
  discount and schemes chosen and the payment done previously"*.

---

## 5. Bảng giá đầy đủ (Financial section)

### Security Deposit (hoàn lại khi huỷ/rút)
| Loại | Deposit |
|---|---|
| Dial-Up | **$325** |
| Broadband | **$500** |
| Landline | **$250** |

### Dial-Up Connection
| Gói | Giá | Hiệu lực |
|---|---|---|
| Hourly – 10 Hrs | $50 | 1 Month |
| Hourly – 30 Hrs | $130 | 3 Months |
| Hourly – 60 Hrs | $260 | 6 Months |
| Unlimited 28 Kbps – Monthly | $75 | 1 tháng |
| Unlimited 28 Kbps – Quarterly | $150 | 3 tháng |
| Unlimited 56 Kbps – Monthly | $100 | 1 tháng |
| Unlimited 56 Kbps – Quarterly | $180 | 3 tháng |

### Broadband Connection
| Gói | Giá | Hiệu lực |
|---|---|---|
| Hourly – 30 Hrs | $175 | 1 Month |
| Hourly – 60 Hrs | $315 | 6 Months |
| Unlimited 64 Kbps – Monthly | $225 | 1 tháng |
| Unlimited 64 Kbps – Quarterly | $400 | 3 tháng |
| Unlimited 128 Kbps – Monthly | $350 | 1 tháng |
| Unlimited 128 Kbps – Quarterly | $445 | 3 tháng |

### Land Line Connection — `Rental + Call charges`
| Gói | Rental | Cước gọi |
|---|---|---|
| Local Plan – Unlimited | $75 / năm | 55¢/phút |
| Local Plan – Monthly | $35 / tháng | 75¢/phút |
| STD Plan – Monthly | $125 / tháng | Local 70¢ · STD $2.25 · Mobile msg $1.00 (mỗi phút) |
| STD Plan – Half-Yearly | $420 | Local 60¢ · STD $2.00 · Mobile msg $1.15 |
| STD Plan – Yearly | $ *(spec để trống)* / năm | Local 60¢ · STD $1.75 · Mobile msg $1.25 |

> Spec ghi Half-Yearly *"Valid for a month"* — nhiều khả năng là **lỗi đánh máy**, nên hiểu là 6 tháng.
> Giá gói STD Yearly **bị bỏ trống** trong spec → cần tự chọn khi seed.

### Thuế dịch vụ
> *"The service tax is as per the government (**12.24%**) on **the whole bill generated**
> and will be charged to the customer."*

- `BACKEND.md` §3.4 tính thuế trên **subtotal (SAU khi trừ chiết khấu)**:
  `subtotal = deposit + rental + hourly − discount` → `tax = subtotal × 12.24%`.
  Đây là **diễn giải** cụm "whole bill" = tổng các khoản sau chiết khấu. Chấp nhận được,
  nhưng nếu giáo viên hiểu "whole bill" = trước chiết khấu thì phải đổi.

---

## 6. Phân quyền theo role — theo spec

| Actor | Quyền (trích spec) |
|---|---|
| **Admin (Manager)** | CRUD (insert/update/delete/search): **plans**, retail shops, employees, vendors, stock/equipment, product details, purchase list. Quản lý employee **duy nhất**. |
| **Retail outlet employee** | **Đặt đơn** (*"the employees of the retail outlet can only place the orders"*), tra cứu trạng thái đơn, xem bill / contact / plans. Theo dõi mọi đơn tới hiện tại, trạng thái, chi tiết kết nối, billing & payment tới hiện tại. Ghi **payment thu tại cửa hàng**. |
| **Technical people** | Theo dõi đơn, **cập nhật trạng thái đơn** (feasible hay không, đã cấp kết nối hay chưa), **tạo kết nối mới** khi khu vực khả thi, đặt kết nối inactive (tạm thời / vĩnh viễn), **duy trì chi tiết product / equipment**. |
| **Accounts department** | **Tính cước** theo plan + equipment khách chọn, **sinh bill**, cập nhật chi tiết bill trên web (*"updating has to be done by the accounts department alone"*). Ghi payment thu **tại văn phòng thành phố**. |
| **Customer** | Xem/sửa contact details của mình, **đặt đơn**, theo dõi trạng thái đơn (qua Order ID), xem bill, xem trạng thái kết nối (qua Account ID). **Gửi feedback**. |

> ⚠ **Mâu thuẫn trong spec**: trang "Proposed" nói *customer* có thể "place an order", nhưng
> mục Non-Financial nói *"the members or the employees of the retail outlet **can only** place
> the orders"*. `BACKEND.md` chọn: retail đặt đơn **+ cho phép customer self-service**. Nếu
> chấm chặt theo spec thì customer **không** được đặt đơn trực tiếp.

---

## 7. Khảo sát khả thi (Feasibility) — theo spec

> *"For a customer who has applied for the Dial-Up connection, the feasibility check will be
> conducted for **both the landline and as well for the internet connection** ... and if the
> customer is holding the landline from the same vendor and now applied for the internet
> connection then the feasibility test is to be conducted for **the internet connection only**."*

- **Broadband / Landline**: 1 lớp khảo sát. Khả thi ⇒ cấp kết nối ⇒ cấp Account ID.
- **Dial-Up**: 2 lớp — landline **và** internet. Đủ cả 2 mới `Feasible`.
  - Đã có landline Nexus (`existingLandlineAccountId`) ⇒ miễn lớp landline, chỉ xét internet.
- Khảo sát xét: khoảng cách cáp, server availability, "company patterns".
  FE lưu: `cableDistanceMeters`, `dpBoxCapacity`, `signalLossDbm`, `feasibilityNotes`.
- Vòng đời: `Order Pending → Feasible (cấp Account ID) → Connection Provided (tạo Connection,
  gán Equipment từ kho)`. Connection: `Active ↔ Temporarily Inactive ↔ Permanently Inactive`
  (chỉ technical).

---

## 8. Thiết bị (Equipment)

- Broadband & Dial-Up: *"the connection is provided by either the **Modem or Router** as
  chosen by the customer"*.
- Thiết bị lấy từ nhiều vendor; admin theo dõi tồn kho & đặt hàng theo số đơn nhận được.
- Technical duy trì chi tiết equipment; gán equipment khi provision, chuyển sang `In Service`.
- **Mở rộng ngoài spec**: spec không nói rõ tồn kho (`InventoryItems`) được giữ ở đâu — nhưng
  vì đơn hàng và kỹ thuật viên đều gắn theo từng retail shop, số lượng thiết bị cũng được
  theo dõi **theo từng chi nhánh** thay vì một kho trung tâm duy nhất: mỗi `ItemCode` có thể
  có nhiều dòng `InventoryItems`, mỗi dòng gắn một `RetailShopId` với số lượng riêng
  (`sql-server/schema/06_inventory_per_branch.sql`).
- **1 kết nối = 1 thiết bị** (đúng câu spec trên): `Equipment.AssignedAccountId` là quan hệ
  1:1 với `Connections.AccountId`, và form provision của kỹ thuật viên (`TechnicalDashboard.svelte`)
  chỉ cho chọn **một** serial trong dropdown — không có multi-select thiết bị cho 1 connection.
- **Số lượng kết nối ↔ số lượng thiết bị tồn kho**: `Equipment.ItemCode` (thêm 2026-09-11,
  `sql-server/schema/07_equipment_itemcode_and_cleanup.sql`) nối một thiết bị cụ thể với dòng
  `InventoryItems` cùng `ItemCode` + `RetailShopId` của cửa hàng đặt đơn. `OrderService.ProvisionAsync`
  trừ `StockQuantity` đi 1 đúng lúc thiết bị chuyển `In Stock → In Service` (không trừ lại khi
  provision lại cùng thiết bị đã `In Service`). Trước đó 2 con số này không liên quan gì tới nhau.
  Đồng thời bỏ 2 cột trùng lặp `Connections.AssignedDeviceSerial/AssignedDeviceModel` (dữ liệu
  này vốn đã có ở `Equipment.SerialNumber/DeviceModel`, tra qua `AssignedAccountId`); FE lấy
  thiết bị đã gán của 1 connection bằng cách tìm trong `equipments` store theo `assignedAccountId`.

---

## 9. Advanced search — theo spec

Tra cứu **trạng thái đơn** hoặc **trạng thái kết nối** theo:
- unique id (Order ID / Account ID)
- tên người đặt đơn / người lấy kết nối
- **loại kết nối**
- **ngày / khoảng thời gian** nộp đơn hoặc nhận kết nối
- **số điện thoại** cung cấp lúc đăng ký

Với connection còn kèm: số tiền còn nợ, trạng thái (active / permanently inactive / temporarily inactive).

---

## 10. Bảng đối chiếu: spec vs BACKEND.md / doanki3fe

| # | Spec nói | FE / BACKEND.md làm | Ghi chú |
|---|---|---|---|
| 1 | "two types of connections" | **3 loại** (thêm Landline là công dân hạng nhất) | Hợp lý: bảng giá + prefix `T` yêu cầu vậy |
| 2 | Order ID prefix = loại kết nối | Giữ đúng; 1 đơn = 1 loại | **Không cho 1 đơn nhiều loại** |
| 3 | Account ID "16 digit numeric" | `[DBT]` + 3 số city + 12 số serial | Spec tự mâu thuẫn numeric vs chữ D/B/T |
| 4 | "10 digit account id" (Existing) | Bỏ, dùng 16 ký tự | Đó là hệ thống giấy cũ |
| 5 | Bậc chiết khấu chồng lấn 10-15/15-25/25-50 | `>50→100, ≥25→75, ≥15→50, ≥10→25` | Chốt biên rõ ràng |
| 6 | Thuế 12.24% "on the whole bill" | Tính trên subtotal **sau** chiết khấu | Diễn giải; rủi ro nếu chấm hiểu khác |
| 7 | Customer "place an order" **và** "only retail can place orders" | Retail đặt + customer self-service | Mâu thuẫn nội tại spec |
| 8 | "feedback ... to be collected" | Thêm hẳn entity `Feedback` + response/respondedBy | Mở rộng; spec chỉ nói "thu thập" |
| 9 | STD Yearly rental để trống; Half-Yearly "valid for a month" | Cần tự điền khi seed | Lỗi/thiếu trong spec |
| 10 | City code 3 số | New York=064, Queens=072, Brooklyn=081 (seed) | Giá trị cụ thể do FE/seed tự đặt |
| 11 | — (spec không có) | `orderGroupId` cho corporate hỗn hợp | *Đề xuất* mở rộng, chưa có trong code |
| 12 | — (spec không nói rõ nơi giữ tồn kho) | `InventoryItems.RetailShopId` — số lượng thiết bị theo từng chi nhánh | Mở rộng; đã áp dụng trong schema + seed |
| 13 | "connection is provided by either the Modem **or** Router" → 1 kết nối = 1 thiết bị | Đúng — UI kỹ thuật viên chỉ cho chọn 1 thiết bị (single-select), `Connection`/`Equipment` là quan hệ 1:1 theo `AssignedAccountId` | Xác nhận đúng spec (2026-09-11); không phải giới hạn thiếu sót |
| 14 | — (spec không nói rõ) | Trước đây: cấp phát thiết bị **không** trừ `InventoryItems.StockQuantity`; `Connections.AssignedDeviceSerial/Model` trùng lặp dữ liệu đã có ở `Equipment`. Đã sửa (2026-09-11): thêm `Equipment.ItemCode` (khớp `InventoryItems.ItemCode`), `ProvisionAsync` trừ tồn kho theo chi nhánh khi thiết bị chuyển `In Stock → In Service`; xoá 2 cột trùng lặp trên `Connections`, FE tra thiết bị qua `equipments` store bằng `assignedAccountId` | Mở rộng; xem `sql-server/schema/07_equipment_itemcode_and_cleanup.sql`, `OrderService.ProvisionAsync` |
| 15 | "updating has to be done by the accounts department alone" | `PUT /api/billing/bills/{id}` cho kế toán sửa chi tiết HĐ đã phát hành; toàn bộ `/api/billing/*` yêu cầu JWT role `Senior Accountant` | Đúng spec (2026-09-21); xem §12 |

---

## 11. Checklist bám spec (khi làm backend)

- [ ] Order ID 11 ký tự, prefix `D/B/T` = loại, serial tăng dần toàn hệ thống, cấp lúc đặt đơn.
- [ ] **Một đơn chỉ một loại kết nối** (đừng "tối ưu" thành đa loại nếu không được phép lệch spec).
- [ ] Account ID 16 ký tự `[D/B/T] + city(3) + serial(12)`, cấp **khi kết nối khả thi/được cấp**.
- [ ] Dial-Up: `Feasible` cần cả landline + internet (miễn landline nếu đã có landline Nexus).
- [ ] Chiết khấu bulk 25/50/75/100% lên (cước ứng trước + cọc) theo số kết nối.
- [ ] Deposit 325 / 500 / 250; thuế 12.24%; chỉ postpaid.
- [ ] Chỉ `admin` CRUD plan/employee/vendor/retailshop/stock.
      ~~Chỉ `accounts` sinh bill~~ → **đã làm**: `/api/billing/*` gắn `[Authorize(Roles = "Senior Accountant")]`
      (xem §12). Phần `admin` vẫn chưa khoá quyền.
- [ ] Payment: `retail` (thu tại cửa hàng, **chưa có API**) + `accounts` (thu tại văn phòng, đã có
      `POST /api/billing/bills/payments`).
- [ ] Advanced search theo id / tên / loại / khoảng ngày / số điện thoại.
- [ ] Thu thập feedback từ khách.

---

## 12. API role kế toán (Accounts department) — trạng thái 2026-09-21

`doanki3API/Controllers/Accounts/BillingController.cs` (trước đây nằm trong thư mục `Admin/`),
route giữ nguyên `api/billing` để không phá FE đang gọi.

| Method | Route | Việc theo spec §6 |
|---|---|---|
| GET | `/api/billing/bills` | Xem toàn bộ hóa đơn |
| GET | `/api/billing/bills/{id}` | Tra 1 hóa đơn theo `BillId` **hoặc** `InvoiceNumber` |
| POST | `/api/billing/bills` | Tính cước + **sinh bill** |
| PUT | `/api/billing/bills/{id}` | **Cập nhật chi tiết bill** — "updating ... by the accounts department alone" |
| POST | `/api/billing/bills/payments` | Ghi payment thu tại văn phòng thành phố |

**Phân quyền**: `[Authorize(Roles = EmployeeRoles.Accounts)]` trên cả controller.
`EmployeeRoles.Accounts = "Senior Accountant"` — khớp đúng chuỗi trong cột `Employee.Role`
(`NexusSystem_Database.sql`). Không token → 401; token role khác (VD `Manager`) → 403.

**JWT**: trước đây `AuthController.Login` **chỉ** cấp token cho Customer nên Employee không thể
gọi API có `[Authorize]`. Nay cấp token cho cả hai; `Program.cs` đã thêm `AddJwtBearer` +
`UseAuthentication()` (validate issuer + signing key + lifetime, không validate audience).
FE lưu token ở `localStorage['nexus_jwt_token']` và `lib/api.ts` đính kèm
`Authorization: Bearer ...` cho mọi request billing.

**PUT chi tiết**: body là các trường optional (`billingMonth`, `dueDate`, `securityDeposit`,
`monthlyRental`, `hourlyCharges`, `discountPercent`, `discountAmount`, `serviceTaxRate`,
`lateFeeAmount`); trường nào bỏ trống thì giữ giá trị cũ. Sau khi cập nhật, server **luôn tính lại**:
`subtotal = cọc + cước tháng + cước giờ − chiết khấu` → `thuế = subtotal × rate%` →
`total = subtotal + thuế + phí trễ hạn` → `dueAmount = total − đã trả` → `status`
(`Paid` / `Partially Paid` / `Unpaid`). Không cho sửa `accountId` và `amountPaid`
(tiền đã thu chỉ đi qua endpoint payments).

> ⚠ Làm tròn: `Math.Round` mặc định của .NET làm tròn về số chẵn còn `toFixed(2)` của JS làm tròn
> 0.5 lên → lệch 1 cent ở dòng thuế. `BillingService.RoundCurrency()` đã ép `AwayFromZero` cho khớp FE.

**Còn thiếu**: FE chưa có giao diện sửa hóa đơn (hàm `updateBillApi()` đã sẵn trong `lib/api.ts`
nhưng `AccountsDashboard.svelte` chưa gọi). Các controller `Admin/*`, `Technical/*` vẫn chưa gắn
`[Authorize]` — muốn chặt theo spec thì phải làm tương tự cho từng role.
