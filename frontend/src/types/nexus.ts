// Nexus Service Marketing System - Core Type Definitions

export type RoleType = 'admin' | 'retail' | 'technical' | 'accounts' | 'user';

/**
 * Quy đổi chức danh lưu trong CSDL (Employee.role — "Manager", "Retail Staff",
 * "Field Engineer", "Senior Accountant"...) sang nhóm quyền dùng trong giao diện.
 * Dùng chung cho đăng nhập và cho ô tìm kiếm toàn hệ thống để chỉ có một nguồn quy ước.
 */
export function roleTypeFromJobTitle(jobTitle: string): RoleType {
  const r = (jobTitle || '').toLowerCase();
  if (r.includes('retail')) return 'retail';
  if (r.includes('field') || r.includes('engineer') || r.includes('technic')) return 'technical';
  if (r.includes('account') || r.includes('billing') || r.includes('finance')) return 'accounts';
  if (r.includes('manager') || r.includes('admin') || r.includes('director')) return 'admin';
  return 'user';
}

// "Landline" is the spec's "only telephone connection" (Account/Order ID prefix = T).
export type ConnectionType = 'Broadband' | 'Dial-Up' | 'Landline';

// Order ID prefix per spec: D = Dial-Up, B = Broadband, T = Telephone-only (Landline).
export const CONNECTION_TYPE_LETTER: Record<ConnectionType, 'D' | 'B' | 'T'> = {
  'Dial-Up': 'D',
  Broadband: 'B',
  Landline: 'T',
};

// Two-stage approval pipeline (2 tầng xét duyệt):
//   1. Khách đăng ký (trang Register / quầy retail)            -> 'PendingRetail'
//      Đơn được route về NHÂN VIÊN BÁN HÀNG của đúng chi nhánh
//      (StoreID) mà khách đã chọn.
//   2. Retail Staff kiểm tra hồ sơ (SCTT/CMND, địa chỉ, gói cước, đúng địa bàn)
//        - Duyệt  -> 'Pending'        (đẩy sang hàng đợi Technical khảo sát)
//        - Từ chối-> 'Not Approved'   (trả hồ sơ, kèm lý do)
//   3. Field Engineer đo kiểm hạ tầng -> 'Feasible' / 'Not Feasible'
//   4. Đấu nối thiết bị -> 'Connection Provided'
//
// NOTE: 'Pending' is deliberately reused for "retail đã duyệt, chờ technical"
// because the DB check constraint CK_Orders_Status only whitelists
// ('Pending','Feasible','Not Feasible','Connection Provided'). 'PendingRetail'
// and 'Not Approved' therefore exist FE-side only until the constraint is
// widened (see docs/ORDER_APPROVAL_FLOW.md).
export type OrderStatus =
  | 'PendingRetail'
  | 'Pending'
  | 'Not Approved'
  | 'Feasible'
  | 'Not Feasible'
  | 'Connection Provided';

/** Statuses that still belong to the Retail (bán hàng) stage. */
export const RETAIL_STAGE_STATUSES: readonly OrderStatus[] = [
  'PendingRetail',
  'Not Approved',
];

/** True while an order has not been cleared by the branch's retail staff yet. */
export function isAwaitingRetailApproval(status: OrderStatus): boolean {
  return status === 'PendingRetail';
}

/** True once the branch's retail staff approved it -> Technical may assess it. */
export function isReleasedToTechnical(status: OrderStatus): boolean {
  return (
    status === 'Pending' ||
    status === 'Feasible' ||
    status === 'Not Feasible' ||
    status === 'Connection Provided'
  );
}

const ORDER_STATUS_LABEL: Record<OrderStatus, { vi: string; en: string }> = {
  PendingRetail: { vi: 'Chờ bán hàng duyệt', en: 'Awaiting retail approval' },
  Pending: { vi: 'Chờ kỹ thuật khảo sát', en: 'Awaiting feasibility survey' },
  'Not Approved': { vi: 'Hồ sơ bị trả lại', en: 'Returned by retail' },
  Feasible: { vi: 'Khảo sát khả thi', en: 'Feasible' },
  'Not Feasible': { vi: 'Không khả thi', en: 'Not Feasible' },
  'Connection Provided': { vi: 'Đã cấp kết nối', en: 'Connection Provided' },
};

/** Localised label for any order status; falls back to the raw value. */
export function orderStatusLabel(status: OrderStatus, language: string): string {
  const entry = ORDER_STATUS_LABEL[status];
  if (!entry) return status;
  return language === 'vi' ? entry.vi : entry.en;
}

/** Tailwind classes for the status pill: returns [chip, dot] colour pairs. */
export function orderStatusTone(status: OrderStatus): {
  chip: string;
  dot: string;
} {
  switch (status) {
    case 'Connection Provided':
      return {
        chip: 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-700',
        dot: 'bg-emerald-500 animate-pulse',
      };
    case 'Feasible':
      return {
        chip: 'bg-sky-100 dark:bg-sky-950 text-sky-800 dark:text-sky-300 border border-sky-300 dark:border-sky-700',
        dot: 'bg-sky-500',
      };
    case 'Not Feasible':
      return {
        chip: 'bg-rose-100 dark:bg-rose-950 text-rose-800 dark:text-rose-300 border border-rose-300 dark:border-rose-700',
        dot: 'bg-rose-500',
      };
    case 'Not Approved':
      return {
        chip: 'bg-orange-100 dark:bg-orange-950 text-orange-800 dark:text-orange-300 border border-orange-300 dark:border-orange-700',
        dot: 'bg-orange-500',
      };
    case 'PendingRetail':
      return {
        chip: 'bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-700',
        dot: 'bg-amber-500',
      };
    default:
      return {
        chip: 'bg-violet-100 dark:bg-violet-950 text-violet-800 dark:text-violet-300 border border-violet-300 dark:border-violet-700',
        dot: 'bg-violet-500',
      };
  }
}

// Bulk / corporate scheme: discount on the advance (first rental) and the security
// deposit, based on how many connections the order covers.
//   10–15 => 25%   15–25 => 50%   25–50 => 75%   >50 => 100%
export function getBulkDiscountPercent(connectionCount: number): number {
  const n = Math.max(1, Math.floor(connectionCount || 1));
  if (n > 50) return 100;
  if (n >= 25) return 75;
  if (n >= 15) return 50;
  if (n >= 10) return 25;
  return 0;
}

export type ConnectionStatus = 'Active' | 'Temporarily Inactive' | 'Permanently Inactive';

export type BillStatus = 'Paid' | 'Partially Paid' | 'Unpaid';

export type EquipmentStatus = 'In Service' | 'In Stock' | 'Maintenance' | 'Faulty';

// 1. Service Plans
export type PlanBillingCycle =
  | 'Hourly Pack'
  | 'Monthly'
  | 'Quarterly'
  | 'Half-Yearly'
  | 'Yearly';

export interface Plan {
  id: string;
  name: string;
  type: ConnectionType;
  speedOrBandwidth: string; // e.g., "56 Kbps", "128 Kbps", "PSTN Voice"
  monthlyRental: number; // headline charge for the plan's billing cycle / pack price
  hourlyCharge?: number; // for dial-up or metered
  securityDeposit: number; // 500 Broadband / 325 Dial-Up / 250 Landline (spec)
  dataLimit?: string; // e.g., "Unlimited" or "60 Hours"
  status: 'Active' | 'Archived';
  description: string;
  billingCycle?: PlanBillingCycle; // spec pricing tables are per cycle
  validity?: string; // e.g., "1 Month", "6 Months", "1 Year"
  includedHours?: number; // hourly dial-up / broadband packs
  callRates?: string; // landline call charges summary (per spec)
  /** Đánh dấu gói nổi bật để gắn nhãn "Phổ biến" ở cổng khách hàng. */
  isPopular?: boolean;
}

// 2. Employees (Admin Management)
export interface Employee {
  id: string;
  employeeCode: string; // e.g., "EMP-1001"
  name: string;
  email: string;
  phone: string;
  role: 'Manager' | 'Retail Staff' | 'Field Engineer' | 'Senior Accountant' | 'Support Agent';
  department: 'Administration' | 'Retail Outlets' | 'Technical Operations' | 'Finance & Accounts';
  retailShopAssigned?: string; // e.g., "Downtown Flagship (SH-01)"
  status: 'Active' | 'Inactive';
  dateOfJoining: string;
}

// 3. Vendors (Admin Management)
export interface Vendor {
  id: string;
  vendorCode: string; // e.g., "VND-401"
  companyName: string;
  contactPerson: string;
  category: 'Fiber Optics & Cabling' | 'Modems & Routers' | 'Telecom Switches' | 'Field Tooling';
  phone: string;
  email: string;
  address: string;
  rating: number; // 1-5
  status: 'Active' | 'Pending Review' | 'Terminated';
}

// 3.1 Purchase Orders (Admin — đặt mua thiết bị từ vendor; đơn Received tự cộng kho)
export interface PurchaseOrder {
  id: string;
  vendorId: string;
  vendorName?: string;
  storeId?: string;
  storeName?: string;
  itemCode: string;
  itemName: string;
  category: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  orderDate: string;
  expectedDate?: string;
  receivedDate?: string;
  status: 'Draft' | 'Submitted' | 'Received' | 'Cancelled';
  notes?: string;
  createdBy?: string;
}

// 4. Retail Shops / Outlets (Admin Management)
export interface RetailShop {
  id: string;
  shopCode: string; // e.g., "SH-01"
  name: string;
  city: string;
  cityCode: string; // 3-digit numeric code of the city, used inside the Account ID
  address: string;
  managerName: string;
  phone: string;
  operatingHours: string;
  activeEmployeesCount: number;
  totalSubscribersServed: number;
  status?: 'Active' | 'Under Renovation' | 'Closed';
}

// 5. Stock / Inventory (Admin & Technical)
export interface InventoryItem {
  id: string;
  itemCode: string; // e.g., "EQ-FBR-01"
  name: string;
  category: 'Modem' | 'Router' | 'Fiber ONT' | 'Splitter' | 'Patch Cord' | 'VoIP Adapter';
  stockQuantity: number;
  reorderLevel: number;
  unitCost: number;
  location: string;
  supplier: string;
  vendorId?: string;
  status?: string;
}

// 6. Orders (Retail & Technical Feasibility)
export interface Order {
  id: string; // 11-char alphanumeric: prefix D/B/T + 10-digit serial (e.g. "D0000000001")
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  installationAddress: string;
  idProofType: 'National ID Card' | 'Passport' | "Driver's License";
  idProofNumber: string;
  connectionType: ConnectionType;
  planId: string;
  planName: string;
  retailOutletCode: string;
  retailEmployeeName: string;
  // --- Two-stage approval: routing to a specific branch's retail staff ---
  // The employee record (emp-xx) of the Retail Staff this order was routed to,
  // resolved from the branch (retailOutletCode) the customer picked.
  assignedEmployeeId?: string;
  // Human-readable branch name for the routed retail staff (e.g. "Downtown Nexus Flagship Store").
  assignedBranchName?: string;
  // Set by the retail staff when they approve / reject the paperwork.
  retailApprovedBy?: string;
  retailApprovedAt?: string;
  retailApprovalNotes?: string;
  // Populated when the retail staff rejects the application.
  retailRejectionReason?: string;
  createdAt: string;
  status: OrderStatus;
  feasibilityNotes?: string;
  rejectionReason?: string;
  cableDistanceMeters?: number;
  dpBoxCapacity?: string;
  signalLossDbm?: number;
  assignedAccountId?: string; // 16-char Account ID issued once the line is Feasible
  assignedTechnician?: string; // Tên KTV được phân công khảo sát hạ tầng
  assignedTechnicianId?: string; // Mã nhân viên KTV (ví dụ: "emp-03")
  assignedTechnicianPhone?: string; // Số điện thoại liên hệ của KTV
  assignedTechnicianDate?: string; // Thời điểm phân công khảo sát
  scheduledInstallDate?: string;

  // Bulk / corporate scheme
  bulkConnectionsCount: number; // connections covered by this order (>= 1)
  bulkDiscountPercent: number; // derived from getBulkDiscountPercent()

  // Dial-Up: feasibility is checked for BOTH the landline and the internet leg,
  // unless the customer already holds a Nexus landline (then internet only).
  existingLandlineAccountId?: string;
  landlineFeasible?: boolean;
  internetFeasible?: boolean;
}

// 7. Customer Connections (Technical & Retail)
export interface Connection {
  accountId: string; // 16-digit formatted: "XXXX-XXXX-XXXX-XXXX"
  orderId: string; // 11-digit order
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  installationAddress: string;
  connectionType: ConnectionType;
  planName: string;
  monthlyRental: number;
  securityDeposit: number;
  status: ConnectionStatus;
  ipAddress?: string;
  portNumber?: string;
  assignedDeviceSerial?: string;
  assignedDeviceModel?: string;
  installedDate: string;
  suspensionStartDate?: string;
  suspensionEndDate?: string;
  terminatedDate?: string;
  lastUpdated: string;
  lastStatusReason?: string;
}

// 8. Equipment / Product Details (Technical Staff)
export interface Equipment {
  id: string;
  inventoryId?: string;
  vendorId?: string;
  storeId?: string;
  serialNumber: string; // e.g., "NX-HW-992810"
  macAddress: string; // e.g., "BC:A9:93:21:44:8E"
  deviceModel: string; // e.g., "Nexus GigaFiber Dual-Band WiFi-6 ONT"
  deviceType: 'Fiber ONT Modem' | 'VDSL2/ADSL Modem' | 'Gigabit Router' | 'Analog Telephone Adapter';
  assignedAccountId?: string; // 16-digit Account ID if deployed
  assignedCustomerName?: string;
  firmwareVersion: string;
  status: EquipmentStatus;
  assignedTechnician?: string;
  installedDate?: string;
  /** Kho / chi nhánh đang giữ thiết bị — lấy từ StoreName của backend. */
  branchOutlet?: string;
}

// 9. Billing & Invoices (Accounts Department)
export interface BillLineItem {
  description: string;
  amount: number;
}

export interface PaymentRecord {
  paymentId: string;
  paymentDate: string;
  amountPaid: number;
  paymentMode: 'Cash' | 'Cheque' | 'Credit/Debit Card' | 'Bank Transfer/NEFT' | 'UPI/Digital Wallet';
  referenceNumber: string;
  recordedBy: string;
}

export interface Bill {
  id: string;
  invoiceNumber: string; // e.g., "NEX-INV-2025-001"
  accountId: string; // 16-digit
  customerName: string;
  billingMonth: string; // e.g., "September 2026"
  billingDate: string;
  dueDate: string;
  planName: string;
  connectionType: ConnectionType;

  // Financial Breakdowns
  securityDeposit: number;
  monthlyRental: number;
  hourlyCharges: number;
  discountPercent: number; // bulk / corporate scheme discount
  discountAmount: number; // applied to (securityDeposit + monthlyRental)
  subtotal: number; // securityDeposit + monthlyRental + hourlyCharges - discountAmount
  serviceTaxRate: number; // 12.24%
  serviceTaxAmount: number; // subtotal * 12.24%
  totalAmount: number; // subtotal + serviceTaxAmount

  // Payment Status Tracking
  amountPaid: number;
  dueAmount: number; // totalAmount - amountPaid
  status: BillStatus;
  paymentHistory: PaymentRecord[];
}

// 10. Customer Feedback (collected per functional requirement #2)
export type FeedbackCategory =
  | 'Service Quality'
  | 'Installation'
  | 'Billing'
  | 'Support'
  | 'Other';

export interface Feedback {
  id: string;
  /** Mã khách hàng trong CSDL (Feedback.CustomerID) — có thể trống với khách vãng lai. */
  customerId?: string;
  accountId?: string; // 16-char Account ID, if the connection is live
  orderId?: string;
  customerName: string;
  rating: number; // 1..5
  category: FeedbackCategory;
  message: string;
  createdAt: string;
  response?: string; // reply from the admin / retail outlet
  respondedBy?: string;
  respondedAt?: string;
}

// 11. Charge & System Settings
export interface SystemSettings {
  serviceTaxRate: number; // default 12.24
  latePaymentFeePercent: number; // e.g. 5%
  defaultSecurityDeposits: {
    Broadband: number;
    'Dial-Up': number;
    Landline: number;
  };
  installationGracePeriodDays: number;
}

// 12. Connection Activity Logs
export interface ConnectionActivityLog {
  id: string;
  connectionId: string;
  action: string;
  actor: string;
  details: string;
  timestamp: string;
}

