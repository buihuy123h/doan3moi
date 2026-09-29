import fs from 'fs';
import path from 'path';

// Plans requiring orders
const plansConfig = [
  // Broadband (5 plans)
  { planId: 'plan-bb-h30', prefix: 'B', existingCount: 1, targetCount: 10 },
  { planId: 'plan-bb-64', prefix: 'B', existingCount: 6, targetCount: 10 },
  { planId: 'plan-bb-h60', prefix: 'B', existingCount: 1, targetCount: 10 },
  { planId: 'plan-bb-128', prefix: 'B', existingCount: 1, targetCount: 10 },
  { planId: 'plan-bb-h120', prefix: 'B', existingCount: 0, targetCount: 10 },

  // Dial-Up (6 plans)
  { planId: 'plan-du-h10', prefix: 'D', existingCount: 0, targetCount: 10 },
  { planId: 'plan-du-28', prefix: 'D', existingCount: 0, targetCount: 10 },
  { planId: 'plan-du-56', prefix: 'D', existingCount: 2, targetCount: 10 },
  { planId: 'plan-du-h30', prefix: 'D', existingCount: 0, targetCount: 10 },
  { planId: 'plan-du-h60', prefix: 'D', existingCount: 0, targetCount: 10 },
  { planId: 'plan-du-h100', prefix: 'D', existingCount: 0, targetCount: 10 },

  // Landline (6 plans)
  { planId: 'plan-ll-loc-a', prefix: 'T', existingCount: 1, targetCount: 10 },
  { planId: 'plan-ll-loc-b', prefix: 'T', existingCount: 1, targetCount: 10 },
  { planId: 'plan-ll-loc-u', prefix: 'T', existingCount: 0, targetCount: 10 },
  { planId: 'plan-ll-std-m', prefix: 'T', existingCount: 1, targetCount: 10 },
  { planId: 'plan-ll-std-h', prefix: 'T', existingCount: 0, targetCount: 10 },
  { planId: 'plan-ll-std-y', prefix: 'T', existingCount: 0, targetCount: 10 },
];

// Customer pool: 40 new customers (CUST-0008 to CUST-0047)
const customersData = [
  { id: 'CUST-0008', name: 'Nguyễn Hoàng Nam', type: 'Individual', email: 'nam.nguyen88@gmail.com', phone: '+84 903 112 445', address: '124 Phố Huế, Hai Bà Trưng, Hà Nội', idType: 'National ID Card', idNum: '001088019234' },
  { id: 'CUST-0009', name: 'Trần Thị Mai Phương', type: 'Individual', email: 'phuong.tran.mai@outlook.com', phone: '+84 912 445 667', address: '45 Lê Duẩn, Bến Nghé, Quận 1, TP.HCM', idType: 'National ID Card', idNum: '079194002819' },
  { id: 'CUST-0010', name: 'Lê Quang Huy', type: 'Individual', email: 'huy.lequang@fpt.edu.vn', phone: '+84 988 776 554', address: '78 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội', idType: 'National ID Card', idNum: '001092003817' },
  { id: 'CUST-0011', name: 'Vũ Thu Trang', type: 'Individual', email: 'trang.vuthu@gmail.com', phone: '+84 934 556 789', address: '210 Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP.HCM', idType: 'Passport', idNum: 'C9921820' },
  { id: 'CUST-0012', name: 'Đặng Quốc Tuấn', type: 'Individual', email: 'tuan.dangquoc@yahoo.com', phone: '+84 976 123 456', address: '15 Trần Phú, Hải Châu 1, Hải Châu, Đà Nẵng', idType: 'National ID Card', idNum: '048085001928' },
  { id: 'CUST-0013', name: 'Bùi Ngọc Lan', type: 'Individual', email: 'ngoclan.bui@gmail.com', phone: '+84 945 678 901', address: '88 Lạch Tray, Ngô Quyền, Hải Phòng', idType: 'Driver\'s License', idNum: 'DL-HP-99218' },
  { id: 'CUST-0014', name: 'Hoàng Văn Phong', type: 'Individual', email: 'phong.hoangvan@techvn.io', phone: '+84 908 998 877', address: '32 Võ Văn Tần, Phường 6, Quận 3, TP.HCM', idType: 'National ID Card', idNum: '079090001234' },
  { id: 'CUST-0015', name: 'Đỗ Hải Yến', type: 'Individual', email: 'haiyen.do@vnu.edu.vn', phone: '+84 918 223 344', address: '144 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội', idType: 'Passport', idNum: 'B8871920' },
  { id: 'CUST-0016', name: 'Công ty CP Công Nghệ Sao Việt', type: 'Corporate', email: 'contact@saoviet-tech.vn', phone: '+84 28 3822 9988', address: 'Tòa nhà Bitexco, 2 Hải Triều, Bến Nghé, Quận 1, TP.HCM', idType: 'Driver\'s License', idNum: 'DL-CORP-0192', taxNum: '0312998811' },
  { id: 'CUST-0017', name: 'Tập đoàn Logistics Á Châu', type: 'Corporate', email: 'procurement@achau-logistics.com', phone: '+84 24 3799 1122', address: 'Tầng 12 Keangnam Landmark 72, Phạm Hùng, Nam Từ Liêm, Hà Nội', idType: 'Passport', idNum: 'P-VN-99281', taxNum: '0105882910' },
  { id: 'CUST-0018', name: 'Marcus Vance', type: 'Individual', email: 'marcus.vance@vancemedia.com', phone: '+1 (555) 301-4491', address: '350 5th Ave, Fl 42, New York, NY 10118', idType: 'Passport', idNum: 'P-US-991203' },
  { id: 'CUST-0019', name: 'Emily Watson', type: 'Individual', email: 'emily.watson@designlab.org', phone: '+1 (555) 772-9910', address: '224 W 30th St, Chelsea, New York, NY 10001', idType: 'Driver\'s License', idNum: 'DL-NY-88271' },
  { id: 'CUST-0020', name: 'Michael Chang', type: 'Individual', email: 'm.chang@apexquant.com', phone: '+1 (555) 819-0022', address: '100 Wall St, Fl 15, New York, NY 10005', idType: 'National ID Card', idNum: 'ID-US-77192' },
  { id: 'CUST-0021', name: 'Sophia Rossi', type: 'Individual', email: 'sophia.rossi@milanocoffee.us', phone: '+1 (555) 991-2345', address: '182 Mulberry St, Little Italy, New York, NY 10012', idType: 'Driver\'s License', idNum: 'DL-NY-33910' },
  { id: 'CUST-0022', name: 'Alexander Wright', type: 'Individual', email: 'a.wright@architectsny.com', phone: '+1 (555) 440-8811', address: '550 W 34th St, Hudson Yards, NY 10001', idType: 'Passport', idNum: 'P-US-440192' },
  { id: 'CUST-0023', name: 'Chloe Bennett', type: 'Individual', email: 'chloe.bennett@brooklyngallery.art', phone: '+1 (555) 234-9090', address: '45 Main St, Dumbo, Brooklyn, NY 11201', idType: 'National ID Card', idNum: 'ID-US-55190' },
  { id: 'CUST-0024', name: 'Daniel Miller', type: 'Individual', email: 'd.miller@financestudio.com', phone: '+1 (555) 662-7711', address: '120 Broadway, Manhattan, NY 10271', idType: 'Driver\'s License', idNum: 'DL-NY-66271' },
  { id: 'CUST-0025', name: 'Olivia Davis', type: 'Individual', email: 'olivia.davis@columbia.edu', phone: '+1 (555) 883-2211', address: '116th and Broadway, Morningside, NY 10027', idType: 'National ID Card', idNum: 'ID-US-88322' },
  { id: 'CUST-0026', name: 'Lucas Martin', type: 'Individual', email: 'lucas.martin@astoriabistro.com', phone: '+1 (555) 331-8899', address: '30-10 Broadway, Astoria, Queens, NY 11106', idType: 'Driver\'s License', idNum: 'DL-NY-33188' },
  { id: 'CUST-0027', name: 'Isabella Garcia', type: 'Individual', email: 'isabella.g@flushingmed.org', phone: '+1 (555) 449-0123', address: '136-20 38th Ave, Flushing, Queens, NY 11354', idType: 'Passport', idNum: 'P-US-449012' },
  { id: 'CUST-0028', name: 'Nexus Cloud Innovations LLC', type: 'Corporate', email: 'ops@nexuscloud.io', phone: '+1 (555) 900-8800', address: '1 World Trade Center, Fl 68, NY 10007', idType: 'Driver\'s License', idNum: 'DL-CORP-9008', taxNum: 'TAX-US-99182' },
  { id: 'CUST-0029', name: 'Gotham Creative Media Corp', type: 'Corporate', email: 'admin@gothamcreative.tv', phone: '+1 (555) 551-0987', address: '500 7th Ave, Fl 10, New York, NY 10018', idType: 'Driver\'s License', idNum: 'DL-CORP-5510', taxNum: 'TAX-US-55109' },
  { id: 'CUST-0030', name: 'Lê Văn Thắng', type: 'Individual', email: 'thang.le@vinaphone.com.vn', phone: '+84 913 889 900', address: '57 Huỳnh Thúc Kháng, Đống Đa, Hà Nội', idType: 'National ID Card', idNum: '001083002918' },
  { id: 'CUST-0031', name: 'Phạm Minh Tuấn', type: 'Individual', email: 'tuan.pm@saigontech.com', phone: '+84 909 334 455', address: '12 Quang Trung, Phường 10, Gò Vấp, TP.HCM', idType: 'National ID Card', idNum: '079089001928' },
  { id: 'CUST-0032', name: 'Trịnh Khánh Vy', type: 'Individual', email: 'vy.trinh@fashionhub.vn', phone: '+84 938 112 233', address: '98 Nguyễn Đình Chiểu, Đa Kao, Quận 1, TP.HCM', idType: 'Passport', idNum: 'C8819201' },
  { id: 'CUST-0033', name: 'Nguyễn Thành Long', type: 'Individual', email: 'long.nt@hust.edu.vn', phone: '+84 966 554 433', address: '1 Đại Cồ Việt, Bách Khoa, Hai Bà Trưng, Hà Nội', idType: 'National ID Card', idNum: '001096001293' },
  { id: 'CUST-0034', name: 'Vũ Đức Anh', type: 'Individual', email: 'anh.vuduc@vietinbank.vn', phone: '+84 982 778 899', address: '108 Trần Hưng Đạo, Cửa Nam, Hoàn Kiếm, Hà Nội', idType: 'Driver\'s License', idNum: 'DL-HN-88291' },
  { id: 'CUST-0035', name: 'Dương Mỹ Linh', type: 'Individual', email: 'linh.duong@danangresort.vn', phone: '+84 905 443 322', address: '280 Võ Nguyên Giáp, Mỹ An, Ngũ Hành Sơn, Đà Nẵng', idType: 'National ID Card', idNum: '048092001827' },
  { id: 'CUST-0036', name: 'Trần Bảo Ngọc', type: 'Individual', email: 'ngoc.tranbao@cinemavn.com', phone: '+84 919 667 788', address: '68 Nam Kỳ Khởi Nghĩa, Bến Thành, Quận 1, TP.HCM', idType: 'Passport', idNum: 'C7719283' },
  { id: 'CUST-0037', name: 'Lâm Chí Dũng', type: 'Individual', email: 'dung.lam@canthoagri.vn', phone: '+84 979 221 100', address: '12 Hòa Bình, Tân An, Ninh Kiều, Cần Thơ', idType: 'National ID Card', idNum: '092087001928' },
  { id: 'CUST-0038', name: 'Chu Đình Trọng', type: 'Individual', email: 'trong.chudinh@haiphongport.vn', phone: '+84 948 119 922', address: '5 Hoàng Diệu, Minh Khai, Hồng Bàng, Hải Phòng', idType: 'National ID Card', idNum: '031089002819' },
  { id: 'CUST-0039', name: 'Công ty TNHH Thương Mại Dịch Vụ Nam Á', type: 'Corporate', email: 'info@nama-trading.vn', phone: '+84 28 3930 1122', address: '40 Phạm Ngọc Thạch, Phường 6, Quận 3, TP.HCM', idType: 'Driver\'s License', idNum: 'DL-CORP-4011', taxNum: '0314882910' },
  { id: 'CUST-0040', name: 'David Kim', type: 'Individual', email: 'david.kim@koreatown.ny', phone: '+1 (555) 234-5544', address: '32 W 32nd St, Koreatown, New York, NY 10001', idType: 'Passport', idNum: 'P-KR-992182' },
  { id: 'CUST-0041', name: 'Hannah Schmidt', type: 'Individual', email: 'hannah.schmidt@berlin-tech.de', phone: '+1 (555) 887-1122', address: '72 Franklin St, Greenpoint, Brooklyn, NY 11222', idType: 'National ID Card', idNum: 'ID-DE-88271' },
  { id: 'CUST-0042', name: 'Ethan Walker', type: 'Individual', email: 'ethan.walker@williamsburg.io', phone: '+1 (555) 345-9876', address: '240 Bedford Ave, Williamsburg, Brooklyn, NY 11211', idType: 'Driver\'s License', idNum: 'DL-NY-24011' },
  { id: 'CUST-0043', name: 'Mia Taylor', type: 'Individual', email: 'mia.taylor@longislandcity.net', phone: '+1 (555) 901-2233', address: '44-02 23rd St, Long Island City, Queens, NY 11101', idType: 'Passport', idNum: 'P-US-901223' },
  { id: 'CUST-0044', name: 'Noah Wilson', type: 'Individual', email: 'noah.wilson@bushwickcraft.com', phone: '+1 (555) 432-8877', address: '119 Ingraham St, Bushwick, Brooklyn, NY 11237', idType: 'National ID Card', idNum: 'ID-US-11928' },
  { id: 'CUST-0045', name: 'Ava Thomas', type: 'Individual', email: 'ava.thomas@parkslope.org', phone: '+1 (555) 678-1122', address: '315 7th Ave, Park Slope, Brooklyn, NY 11215', idType: 'Driver\'s License', idNum: 'DL-NY-31577' },
  { id: 'CUST-0046', name: 'Brooklyn Roasting Company LLC', type: 'Corporate', email: 'orders@brooklynroasting.com', phone: '+1 (555) 888-4422', address: '25 Jay St, Dumbo, Brooklyn, NY 11201', idType: 'Driver\'s License', idNum: 'DL-CORP-2511', taxNum: 'TAX-US-25119' },
  { id: 'CUST-0047', name: 'Manhattan Medical Associates PC', type: 'Corporate', email: 'admin@manhattanmedical.org', phone: '+1 (555) 212-9900', address: '654 Madison Ave, Suite 800, New York, NY 10065', idType: 'Passport', idNum: 'P-CORP-6540', taxNum: 'TAX-US-65401' },
];

const stores = ['SH-01', 'SH-02', 'SH-03', 'SH-04'];
const retailEmployees = ['emp-02', 'emp-05'];
const techEngineer = 'emp-03';

// Status template sequence for 10 orders per plan:
// Index 0: PendingRetail (khách vừa đăng ký qua web, chờ quầy duyệt)
// Index 1: PendingRetail
// Index 2: PendingRetail
// Index 3: Pending (đã qua bán lẻ duyệt, chờ KTV kiểm tra khả thi)
// Index 4: Pending
// Index 5: Feasible (KTV đã kiểm tra khả thi, sẵn sàng cấp kết nối)
// Index 6: Feasible
// Index 7: Connection Provided (đã cấp kết nối thành công, có AccountID)
// Index 8: Connection Provided
// Index 9: Not Feasible (hoặc Not Approved)
const statusTemplate = [
  'PendingRetail',
  'PendingRetail',
  'PendingRetail',
  'Pending',
  'Pending',
  'Feasible',
  'Feasible',
  'Connection Provided',
  'Connection Provided',
  'Not Feasible'
];

let bIndex = 16;
let dIndex = 7;
let tIndex = 12;
let accCounter = 10;

let customerCursor = 0;
const allCustomerIds = [
  'CUST-0001', 'CUST-0002', 'CUST-0003', 'CUST-0004', 'CUST-0005', 'CUST-0006', 'CUST-0007',
  ...customersData.map(c => c.id)
];

const ordersSql = [];
const connectionsSql = [];

plansConfig.forEach((planConfig) => {
  const needed = planConfig.targetCount - planConfig.existingCount;
  if (needed <= 0) return;

  for (let i = 0; i < needed; i++) {
    let orderIdNum;
    let orderId;
    if (planConfig.prefix === 'B') {
      orderIdNum = bIndex++;
      orderId = `B${String(orderIdNum).padStart(10, '0')}`;
    } else if (planConfig.prefix === 'D') {
      orderIdNum = dIndex++;
      orderId = `D${String(orderIdNum).padStart(10, '0')}`;
    } else {
      orderIdNum = tIndex++;
      orderId = `T${String(orderIdNum).padStart(10, '0')}`;
    }

    // Assign status according to the template
    const templateIdx = (planConfig.existingCount + i) % 10;
    const status = statusTemplate[templateIdx];

    const customerId = allCustomerIds[customerCursor % allCustomerIds.length];
    customerCursor++;

    const storeId = stores[(orderIdNum + i) % stores.length];
    const employeeId = retailEmployees[i % retailEmployees.length];

    // Dates
    const day = (10 + ((orderIdNum * 3 + i * 2) % 18));
    const appDate = `2026-09-${String(day).padStart(2, '0')} ${String(8 + (i % 9)).padStart(2, '0')}:${String(10 + (i * 7) % 50).padStart(2, '0')}:00`;

    // Bulk config
    const isBulk = (i === 4 && planConfig.prefix === 'B') || (i === 3 && planConfig.prefix === 'T');
    const bulkCount = isBulk ? 5 : 1;
    const bulkDiscount = isBulk ? 5.00 : 0.00;

    let cableDistance = 'NULL';
    let dpBox = 'NULL';
    let signalLoss = 'NULL';
    let feasibilityNotes = 'NULL';
    let checkedBy = 'NULL';
    let checkedDate = 'NULL';
    let scheduledDate = 'NULL';
    let rejectionReason = 'NULL';
    let assignedAccountId = 'NULL';
    let retailApprovedBy = 'NULL';
    let retailApprovedDate = 'NULL';
    let retailApprovalNotes = 'NULL';
    let assignedTechName = 'NULL';
    let assignedTechPhone = 'NULL';
    let assignedTechDate = 'NULL';
    let internetFeasible = 'NULL';
    let landlineFeasible = 'NULL';

    const cityCode = storeId === 'SH-03' ? '072' : storeId === 'SH-04' ? '081' : '064';

    if (status === 'PendingRetail') {
      // Just submitted online
      retailApprovalNotes = "N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.'";
    } else if (status === 'Pending') {
      retailApprovedBy = "N'David Chen'";
      retailApprovedDate = `'2026-09-${String(day).padStart(2, '0')} 11:30:00'`;
      retailApprovalNotes = "N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.'";
      assignedTechName = "N'Marcus Ramirez'";
      assignedTechPhone = "'+1 (555) 456-7890'";
      assignedTechDate = `'2026-09-${String(day).padStart(2, '0')} 13:00:00'`;
    } else if (status === 'Feasible') {
      retailApprovedBy = "N'David Chen'";
      retailApprovedDate = `'2026-09-${String(day - 1).padStart(2, '0')} 10:00:00'`;
      retailApprovalNotes = "N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.'";
      cableDistance = String(70 + (orderIdNum * 11) % 250);
      dpBox = `N'DP-${storeId}-P${(orderIdNum % 8) + 1}'`;
      signalLoss = (-14.00 - ((orderIdNum % 60) / 10)).toFixed(2);
      feasibilityNotes = "N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.'";
      checkedBy = `'${techEngineer}'`;
      checkedDate = `'2026-09-${String(day).padStart(2, '0')} 14:00:00'`;
      scheduledDate = `'2026-09-${String(Math.min(28, day + 2)).padStart(2, '0')}'`;
      internetFeasible = planConfig.prefix === 'T' ? 'NULL' : '1';
      landlineFeasible = planConfig.prefix === 'B' ? 'NULL' : '1';
      accCounter++;
      assignedAccountId = `'${planConfig.prefix}${cityCode}-${String(accCounter).padStart(12, '0')}'`;
    } else if (status === 'Connection Provided') {
      retailApprovedBy = "N'Aiden Brooks'";
      retailApprovedDate = `'2026-08-${String(15 + (i % 10)).padStart(2, '0')} 09:00:00'`;
      retailApprovalNotes = "N'Đã hoàn tất duyệt hồ sơ và thanh toán.'";
      cableDistance = String(50 + (orderIdNum * 9) % 200);
      dpBox = `N'DP-${storeId}-MAIN-${(orderIdNum % 4) + 1}'`;
      signalLoss = (-13.50 - ((orderIdNum % 50) / 10)).toFixed(2);
      feasibilityNotes = "N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.'";
      checkedBy = `'${techEngineer}'`;
      checkedDate = `'2026-08-${String(16 + (i % 10)).padStart(2, '0')} 10:00:00'`;
      scheduledDate = `'2026-08-${String(18 + (i % 10)).padStart(2, '0')}'`;
      internetFeasible = planConfig.prefix === 'T' ? 'NULL' : '1';
      landlineFeasible = planConfig.prefix === 'B' ? 'NULL' : '1';
      accCounter++;
      const accId = `${planConfig.prefix}${cityCode}-${String(accCounter).padStart(12, '0')}`;
      assignedAccountId = `'${accId}'`;

      // CustomerConnection
      const ip = planConfig.prefix === 'B' 
        ? `103.21.${10 + (accCounter % 200)}.${(accCounter * 7) % 250 + 1}`
        : planConfig.prefix === 'D'
        ? `192.168.${accCounter % 100}.${(accCounter * 3) % 250 + 1}`
        : `198.51.100.${(accCounter % 200) + 1}`;
      const port = `PORT-${storeId}-${(accCounter % 16) + 1}`;
      connectionsSql.push(`
IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = '${accId}')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('${accId}', '${orderId}', NULL, 'Active', '${ip}', '${port}', '2026-08-${String(18 + (i % 10)).padStart(2, '0')} 14:00:00', '2026-08-${String(18 + (i % 10)).padStart(2, '0')} 15:00:00');
END;`);
    } else if (status === 'Not Feasible') {
      retailApprovedBy = "N'David Chen'";
      retailApprovedDate = `'2026-09-${String(day - 1).padStart(2, '0')} 09:30:00'`;
      retailApprovalNotes = "N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.'";
      cableDistance = '1250';
      dpBox = "N'No Spare Ports / OLT Saturated'";
      signalLoss = '-35.50';
      feasibilityNotes = "N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.'";
      checkedBy = `'${techEngineer}'`;
      checkedDate = `'2026-09-${String(day).padStart(2, '0')} 16:30:00'`;
      rejectionReason = "N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.'";
      internetFeasible = '0';
      landlineFeasible = '0';
    }

    const installAddr = `N'Địa chỉ lắp đặt: Số ${10 + (orderIdNum * 7) % 300}, Đường số ${(orderIdNum % 20) + 1}, Chi nhánh ${storeId}'`;

    ordersSql.push(`
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = '${orderId}')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        '${orderId}', '${customerId}', '${planConfig.planId}', '${storeId}', '${employeeId}',
        ${installAddr}, '${appDate}', '${status}',
        ${cableDistance}, ${dpBox}, ${signalLoss}, ${feasibilityNotes},
        ${checkedBy}, ${checkedDate}, ${scheduledDate}, ${rejectionReason},
        ${bulkCount}, ${bulkDiscount.toFixed(2)}, NULL, ${landlineFeasible}, ${internetFeasible},
        ${assignedAccountId}, ${retailApprovedBy}, ${retailApprovedDate}, ${retailApprovalNotes},
        ${assignedTechName}, ${assignedTechPhone}, ${assignedTechDate},
        '${appDate}', '${appDate}'
    );
END;`);
  }
});

const esc = (s) => (s ? String(s).replace(/'/g, "''") : '');

// Build SQL content
const customersSql = customersData.map(c => `
IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = '${c.id}')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        '${c.id}', N'${esc(c.name)}', '${c.type}', '${c.email}', '${c.phone}', N'${esc(c.address)}',
        '${esc(c.idType)}', '${esc(c.idNum)}', ${c.taxNum ? `'${esc(c.taxNum)}'` : 'NULL'}, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;`).join('\n');

// Batch orders every 20 to avoid large batch limits
const batchPrefix = '\nGO\nSET ANSI_NULLS ON;\nSET QUOTED_IDENTIFIER ON;\n';
const batchedOrdersSql = ordersSql.map((sql, idx) => (idx > 0 && idx % 20 === 0 ? `${batchPrefix}${sql}` : sql)).join('\n');
const batchedConnectionsSql = connectionsSql.map((sql, idx) => (idx > 0 && idx % 20 === 0 ? `${batchPrefix}${sql}` : sql)).join('\n');

const fullSql = `
-- ============================================================================
-- SEED SCRIPT: Bổ sung đơn hàng (Orders) cho tất cả 17 gói cước khi đăng ký
-- Mỗi gói cước có chính xác 10 bản ghi đơn hàng (Tổng cộng 170 đơn hàng)
-- ============================================================================

USE NexusSystem;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '1. Thêm khách hàng mẫu vào dbo.Customer...';
${customersSql}
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '2. Thêm đơn hàng mẫu vào dbo.Orders...';
${batchedOrdersSql}
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '3. Thêm tài khoản kết nối cho các đơn đã cấp kết nối vào dbo.CustomerConnection...';
${batchedConnectionsSql}
GO

PRINT '4. Kiểm tra số lượng đơn hàng theo từng gói cước trong dbo.Orders:';
SELECT 
    sp.PlanID,
    sp.ConnectionType,
    sp.Name AS PlanName,
    COUNT(o.OrderID) AS OrderCount
FROM dbo.ServicePlan sp
LEFT JOIN dbo.Orders o ON sp.PlanID = o.PlanID
GROUP BY sp.PlanID, sp.ConnectionType, sp.Name
ORDER BY sp.ConnectionType, sp.PlanID;
GO
`;

const outputPath = path.resolve('scripts/sql/2026-09-28_seed_orders_10_per_plan.sql');
fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, fullSql, 'utf-8');
console.log(`Generated SQL script at: ${outputPath}`);
console.log(`Generated ${ordersSql.length} new orders.`);
