// Base URL của backend ASP.NET Core (profile "http" trong launchSettings.json).
// Ghi đè bằng biến môi trường VITE_API_URL khi deploy (VD: VITE_API_URL=https://api.example.com).
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5105';

export interface LoginApiResponse {
  token?: string;
  user: {
    id: string;
    name: string;
    email: string;
    role: string;
    accountType: 'Employee' | 'Customer';
  };
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * JWT lấy lúc đăng nhập (nhân viên email + mật khẩu, khách OTP, hoặc khách vừa
 * đăng ký). Backend khoá phần lớn API theo [Authorize] nên mọi request nghiệp vụ
 * (billing, admin, Technical...) đều phải đính kèm header này. Endpoint công khai
 * (auth/login, OTP, chat khách vãng lai, notifications) bỏ qua header nếu thiếu.
 */
export function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('nexus_jwt_token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

/** Gọi POST /api/auth/login trên backend, trả về token + thông tin user. */
export async function loginRequest(email: string, password: string): Promise<LoginApiResponse> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
  } catch {
    throw new ApiError('network');
  }

  let body: { token?: string; user?: LoginApiResponse['user']; error?: string } | null = null;
  try {
    body = await response.json();
  } catch {
    body = null;
  }

  if (!response.ok || !body?.user || (body.user.accountType === 'Customer' && !body.token)) {
    throw new ApiError(body?.error ?? `http_${response.status}`, response.status);
  }

  return body as LoginApiResponse;
}

/** Kết quả trả về của POST /api/auth/register. */
export interface RegisterApiResponse {
  message: string;
  /** JWT role Customer — dùng ngay cho POST /api/Technical/orders sau khi đăng ký. */
  token?: string;
  user: {
    id: string;
    name: string;
    email: string;
  };
}

/**
 * Tạo tài khoản đăng nhập cho khách khi họ đăng ký dịch vụ ở trang /register.
 * Backend tự hash mật khẩu bằng BCrypt và lưu vào bảng Customer.
 * Ném ApiError với status 409 nếu email hoặc số điện thoại đã có người dùng.
 */
export async function registerCustomerApi(data: {
  name: string;
  email: string;
  password: string;
  phoneNumber: string;
}): Promise<RegisterApiResponse> {
  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
  } catch {
    throw new ApiError('network');
  }

  let body: { message?: string; error?: string; token?: string; user?: RegisterApiResponse['user'] } | null = null;
  try {
    body = await res.json();
  } catch {
    body = null;
  }

  if (!res.ok || !body?.user) {
    throw new ApiError(body?.error ?? `http_${res.status}`, res.status);
  }

  return body as RegisterApiResponse;
}

// ==================== ĐĂNG NHẬP KHÁCH HÀNG BẰNG OTP QUA EMAIL ====================

export interface RequestLoginOtpResponse {
  /** Email đã che bớt, VD "sa*****@gmail.com" — chỉ để khách nhận ra hộp thư của mình. */
  maskedEmail: string;
  expiresInSeconds: number;
  resendAfterSeconds: number;
  message: string;
}

export interface VerifyLoginOtpResponse {
  token: string;
  user: {
    id: string;
    name: string;
    email: string;
    role: string;
    accountType: string;
    accountId: string;
  };
}

/** Bóc thông báo lỗi tiếng Việt do backend trả về, kèm mã HTTP để nơi gọi phân nhánh. */
async function throwApiError(res: Response, fallback: string): Promise<never> {
  let message = fallback;
  try {
    const body = await res.json();
    if (body?.message) message = body.message;
    else if (body?.error) message = body.error;
  } catch {
    // giữ nguyên thông báo mặc định
  }
  throw new ApiError(message, res.status);
}

/**
 * Bước 1 của đăng nhập khách hàng: gửi mã tài khoản, backend gửi OTP 6 chữ số
 * về email đã đăng ký của khách.
 */
export async function requestLoginOtpApi(accountId: string): Promise<RequestLoginOtpResponse> {
  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}/api/auth/otp/request`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ accountId }),
    });
  } catch {
    throw new ApiError('network');
  }

  if (!res.ok) {
    return throwApiError(res, `Không gửi được mã OTP (HTTP ${res.status})`);
  }
  return (await res.json()) as RequestLoginOtpResponse;
}

/**
 * Khách quên / chưa nhận được mã tài khoản: gửi lại mã về email đã đăng ký.
 * Server luôn trả cùng một câu (không tiết lộ email có tồn tại hay không).
 */
export async function forgotAccountIdApi(email: string): Promise<{ message: string }> {
  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}/api/auth/otp/forgot-account-id`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email }),
    });
  } catch {
    throw new ApiError('network');
  }

  if (!res.ok) {
    return throwApiError(res, `Không gửi được yêu cầu (HTTP ${res.status})`);
  }
  return (await res.json()) as { message: string };
}

/** Bước 2: gửi mã OTP khách nhập, đúng thì nhận JWT và thông tin tài khoản. */
export async function verifyLoginOtpApi(
  accountId: string,
  otp: string
): Promise<VerifyLoginOtpResponse> {
  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}/api/auth/otp/verify`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ accountId, otp }),
    });
  } catch {
    throw new ApiError('network');
  }

  if (!res.ok) {
    return throwApiError(res, `Mã OTP không hợp lệ (HTTP ${res.status})`);
  }
  return (await res.json()) as VerifyLoginOtpResponse;
}

// ==================== EMPLOYEE APIS ====================

export async function updateEmployeeApi(id: string, data: {
  fullName: string;
  email: string;
  phone?: string;
  role: string;
  department: string;
  storeId?: string;
  retailShopAssigned?: string;
  status: string;
  dateOfJoining?: string;
  password?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/admin/employee/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi cập nhật nhân viên (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function createEmployeeApi(data: {
  employeeCode?: string;
  fullName: string;
  email: string;
  phone?: string;
  role: string;
  department: string;
  storeId?: string;
  retailShopAssigned?: string;
  status: string;
  dateOfJoining?: string;
  password?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/admin/employee`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi thêm mới nhân viên (HTTP ${res.status})`);
  }
  return await res.json();
}

// ==================== INVENTORY / STOCK APIS ====================

export async function updateInventoryItemApi(id: string, data: {
  itemCode?: string;
  name: string;
  category: string;
  stockQuantity: number;
  reorderLevel: number;
  unitCost: number;
  location?: string;
  vendorId?: string;
  supplier?: string;
  status?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/admin/inventory/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi cập nhật vật tư kho (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function createInventoryItemApi(data: {
  itemCode?: string;
  name: string;
  category: string;
  stockQuantity: number;
  reorderLevel: number;
  unitCost: number;
  location?: string;
  vendorId?: string;
  supplier?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/admin/inventory`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi tạo vật tư kho (HTTP ${res.status})`);
  }
  return await res.json();
}

// ==================== EQUIPMENT APIS ====================

export async function createEquipmentApi(data: {
  equipmentId?: string;
  serialNumber?: string;
  macAddress?: string;
  deviceModel: string;
  deviceType: string;
  vendorId?: string;
  storeId?: string;
  status?: string;
  firmwareVersion?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/Technical/equipment`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({
      equipmentId: data.equipmentId || `EQ-${Date.now().toString().slice(-8)}`,
      serialNumber: data.serialNumber,
      macAddress: data.macAddress,
      deviceModel: data.deviceModel,
      deviceType: data.deviceType,
      vendorId: data.vendorId || 'vnd-02',
      storeId: data.storeId,
      status: data.status || 'In Stock',
      firmwareVersion: data.firmwareVersion || 'v1.0.0',
    }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi đăng ký thiết bị vào CSDL (HTTP ${res.status})`);
  }
  return await res.json();
}

// ==================== CONNECTIONS APIS ====================

export async function fetchConnectionsApi() {
  const res = await fetch(`${API_BASE_URL}/api/Technical/connections`, {
    headers: authHeaders(),
  });
  if (!res.ok) {
    throw new Error(`Lỗi tải danh sách kết nối (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function createOrUpdateConnectionApi(data: {
  accountId: string;
  customerId?: string;
  orderId?: string;
  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;
  installationAddress?: string;
  connectionType?: string;
  planName?: string;
  monthlyRental?: number;
  securityDeposit?: number;
  status?: string;
  ipAddress?: string;
  portNumber?: string;
  equipmentId?: string;
  assignedDeviceSerial?: string;
  assignedDeviceModel?: string;
  installedDate?: string;
  lastStatusReason?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/Technical/connections`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi lưu kết nối vào CSDL (HTTP ${res.status})`);
  }
  return await res.json();
}

// ==================== BILLING & INVOICE APIS ====================

export async function fetchBillsApi() {
  const res = await fetch(`${API_BASE_URL}/api/billing/bills`, {
    headers: authHeaders(),
  });
  if (!res.ok) {
    throw new Error(`Lỗi tải danh sách hóa đơn (HTTP ${res.status})`);
  }
  return await res.json();
}

/** GET /api/billing/bills/{id} — nhận BillId hoặc số hóa đơn, dùng cho xuất PDF
 *  để hóa đơn luôn phản ánh dữ liệu mới nhất từ CSDL (kể cả payment vừa ghi). */
export async function fetchBillByIdApi(billIdOrInvoiceNumber: string) {
  const res = await fetch(
    `${API_BASE_URL}/api/billing/bills/${encodeURIComponent(billIdOrInvoiceNumber)}`,
    { headers: authHeaders() }
  );
  if (!res.ok) {
    throw new Error(`Lỗi tải hóa đơn ${billIdOrInvoiceNumber} (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function createBillApi(data: {
  accountId: string;
  billingMonth: string;
  securityDeposit: number;
  monthlyRental: number;
  hourlyCharges?: number;
  discountPercent?: number;
  discountAmount?: number;
  subtotal?: number;
  serviceTaxRate?: number;
  serviceTaxAmount?: number;
  totalAmount?: number;
  invoiceNumber?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/billing/bills`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi phát hành hóa đơn vào CSDL (HTTP ${res.status})`);
  }
  return await res.json();
}

/** PUT /api/billing/bills/{id} — kế toán sửa chi tiết hóa đơn đã phát hành.
 *  Bỏ trống trường nào thì backend giữ nguyên giá trị cũ và tính lại tổng cước. */
export async function updateBillApi(
  billIdOrInvoiceNumber: string,
  data: {
    billingMonth?: string;
    dueDate?: string;
    securityDeposit?: number;
    monthlyRental?: number;
    hourlyCharges?: number;
    discountPercent?: number;
    discountAmount?: number;
    serviceTaxRate?: number;
    lateFeeAmount?: number;
  }
) {
  const res = await fetch(
    `${API_BASE_URL}/api/billing/bills/${encodeURIComponent(billIdOrInvoiceNumber)}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify(data),
    }
  );
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi cập nhật hóa đơn (HTTP ${res.status})`);
  }
  return await res.json();
}

/**
 * POST /api/retail/payments — ghi thanh toán thu TẠI CỬA HÀNG BÁN LẺ (spec: retail
 * outlet employee). Endpoint cho role Retail Staff / Manager / Senior Accountant;
 * khách (Customer) tự trả online cũng đi qua đây và bị backend chặn theo quyền sở
 * hữu hóa đơn. Dùng chung BillingService với /api/billing/bills/payments.
 */
export async function recordRetailPaymentApi(data: {
  invoiceNumber?: string;
  billId?: string;
  amountPaid: number;
  paymentMode: string;
  referenceNumber?: string;
  recordedBy?: string;
  notes?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/retail/payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi ghi nhận thanh toán vào CSDL (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function recordPaymentApi(data: {
  invoiceNumber?: string;
  billId?: string;
  amountPaid: number;
  paymentMode: string;
  referenceNumber?: string;
  recordedBy?: string;
  notes?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/billing/bills/payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi ghi nhận thanh toán vào CSDL (HTTP ${res.status})`);
  }
  return await res.json();
}

// ==================== LIVE CHAT WITH ADMIN APIS ====================

export interface ChatMessageDto {
  id: string;
  sessionId: string;
  senderType: 'guest' | 'admin';
  senderName: string;
  message: string;
  createdAt: string;
  createdAtDisplay: string;
}

export interface ChatSessionDto {
  sessionId: string;
  customerName: string;
  lastMessage: string;
  lastSenderType: 'guest' | 'admin';
  lastMessageAt: string | null;
  lastMessageAtDisplay: string;
  unreadCount: number;
  messageCount: number;
}

// --- Gọi API chat thật (dữ liệu tin nhắn nằm trong MongoDB phía backend) ---

/**
 * Gọi một endpoint của /api/chat và quy lỗi về thông báo tiếng Việt dễ hiểu.
 * Mọi hàm chat bên dưới đều ném ApiError khi thất bại — nơi gọi đã có try/catch
 * để hiện toast hoặc bỏ qua (polling).
 */
async function chatFetch<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}/api/chat${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
    });
  } catch {
    throw new ApiError('Không kết nối được máy chủ chat.');
  }

  if (res.status === 404) {
    // Phiên cũ lưu trong localStorage nhưng server không còn -> nơi gọi sẽ reset phiên.
    throw new ApiError('Phiên chat không còn tồn tại.', 404);
  }

  if (res.status === 503) {
    throw new ApiError('Dịch vụ chat tạm thời không khả dụng (chưa kết nối được MongoDB).', 503);
  }

  if (!res.ok) {
    let message = `Lỗi chat (HTTP ${res.status})`;
    try {
      const body = await res.json();
      if (body?.message) message = body.message;
    } catch {
      // giữ nguyên thông báo mặc định
    }
    throw new ApiError(message, res.status);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

/** Khách bắt đầu phiên chat mới — backend tự tạo lời chào từ Admin. */
export async function startChatSessionApi(customerName: string): Promise<ChatSessionDto> {
  return chatFetch<ChatSessionDto>('/sessions', {
    method: 'POST',
    body: JSON.stringify({ customerName }),
  });
}

/** Tải toàn bộ tin nhắn của một phiên chat (dùng chung cho khách và admin). */
export async function fetchChatMessagesApi(sessionId: string): Promise<ChatMessageDto[]> {
  return chatFetch<ChatMessageDto[]>(`/sessions/${encodeURIComponent(sessionId)}/messages`);
}

/** Khách gửi tin nhắn tới Admin. */
export async function sendChatMessageApi(
  sessionId: string,
  senderName: string,
  message: string
): Promise<ChatMessageDto> {
  return chatFetch<ChatMessageDto>(`/sessions/${encodeURIComponent(sessionId)}/messages`, {
    method: 'POST',
    body: JSON.stringify({ senderType: 'guest', senderName, message }),
  });
}

/** Admin xem danh sách tất cả phiên chat đang có, lọc theo tên khách nếu cần. */
export async function fetchChatSessionsApi(search?: string): Promise<ChatSessionDto[]> {
  const keyword = search?.trim();
  const query = keyword ? `?search=${encodeURIComponent(keyword)}` : '';
  return chatFetch<ChatSessionDto[]>(`/sessions${query}`);
}

/** Admin trả lời khách trong một phiên chat. */
export async function replyChatSessionApi(
  sessionId: string,
  adminName: string,
  message: string
): Promise<ChatMessageDto> {
  return chatFetch<ChatMessageDto>(`/sessions/${encodeURIComponent(sessionId)}/messages`, {
    method: 'POST',
    body: JSON.stringify({ senderType: 'admin', senderName: adminName, message }),
  });
}

/** Admin đánh dấu đã đọc mọi tin nhắn của khách trong phiên. */
export async function markChatSessionReadApi(sessionId: string): Promise<void> {
  await chatFetch<void>(`/sessions/${encodeURIComponent(sessionId)}/read`, { method: 'POST' });
}

// ==================== THÔNG BÁO REAL-TIME ====================
// Thông báo MỚI không lấy bằng API — backend đẩy thẳng xuống qua SignalR
// (xem src/lib/realtime.ts). Các hàm dưới đây chỉ dùng lúc vừa mở trang
// để tải lịch sử, và khi bấm "đã đọc".

export type NotificationType =
  | 'order'
  | 'billing'
  | 'alert'
  | 'hardware'
  | 'system'
  | 'stock'
  | 'chat'
  | 'feedback';

export interface NotificationDto {
  id: string;
  /** "role:admin" | "account:B064000000000002" */
  audience: string;
  type: NotificationType;
  titleVi: string;
  titleEn: string;
  descVi: string;
  descEn: string;
  targetPath: string;
  targetTab: string;
  targetRole: string | null;
  sectionVi: string;
  sectionEn: string;
  entityId: string | null;
  /** Thông báo cùng khoá này sẽ thay thế nhau (VD: kho sắp hết, khách đang nhắn tin). */
  replaceKey: string | null;
  read: boolean;
  createdAt: string;
}

async function notificationFetch<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}/api/notifications${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
    });
  } catch {
    throw new ApiError('Không kết nối được máy chủ thông báo.');
  }

  if (!res.ok) {
    let message = `Lỗi thông báo (HTTP ${res.status})`;
    try {
      const body = await res.json();
      if (body?.message) message = body.message;
    } catch {
      // giữ nguyên thông báo mặc định
    }
    throw new ApiError(message, res.status);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

/** Lịch sử thông báo của một người nhận, mới nhất lên đầu. */
export async function fetchNotificationsApi(
  audience: string,
  limit = 50
): Promise<NotificationDto[]> {
  const query = `?audience=${encodeURIComponent(audience)}&limit=${limit}`;
  return notificationFetch<NotificationDto[]>(query);
}

/** Đánh dấu một thông báo đã đọc. */
export async function markNotificationReadApi(audience: string, id: string): Promise<void> {
  await notificationFetch<void>(
    `/${encodeURIComponent(id)}/read?audience=${encodeURIComponent(audience)}`,
    { method: 'POST' }
  );
}

/** Đánh dấu tất cả thông báo của người nhận này là đã đọc. */
export async function markAllNotificationsReadApi(audience: string): Promise<number> {
  const res = await notificationFetch<{ updated: number }>(
    `/read-all?audience=${encodeURIComponent(audience)}`,
    { method: 'POST' }
  );
  return res?.updated ?? 0;
}

// ==================== NEW MODULES API (PLANS, SHOPS, VENDORS, ORDERS) ====================

// --- PLANS ---
export async function createPlanApi(data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/serviceplan`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

export async function updatePlanApi(id: string, data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/serviceplan/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- RETAIL SHOPS ---
export async function createRetailShopApi(data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/retailstore`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

export async function updateRetailShopApi(id: string, data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/retailstore/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- VENDORS ---
export async function createVendorApi(data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/vendor`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

export async function updateVendorApi(id: string, data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/vendor/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- ORDERS ---
/**
 * Ghi một đơn đăng ký dịch vụ vào CSDL. Trả về đơn do server tạo (mã đơn là mã thật).
 * Ném ApiError kèm câu tiếng Việt dễ hiểu để trang gọi hiện thẳng cho người dùng.
 */
export async function createOrderApi(
  data: any,
  options: { selfService?: boolean } = {}
): Promise<{ orderId: string; status?: string; depositWaived?: boolean }> {
  // selfService: trang đăng ký công khai gọi /api/public/orders (không cần JWT), nhờ vậy
  // khách cũ mua thêm bằng email đã dùng không bị chặn 401 như ở /api/Technical/orders.
  const url = options.selfService
    ? `${API_BASE_URL}/api/public/orders`
    : `${API_BASE_URL}/api/Technical/orders`;

  let res: Response;
  try {
    res = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(options.selfService ? {} : authHeaders()),
      },
      body: JSON.stringify(data),
    });
  } catch {
    throw new ApiError('network');
  }

  if (res.status === 401 || res.status === 403) {
    throw new ApiError(
      'Phiên đăng nhập không hợp lệ hoặc đã hết hạn, đơn chưa được lưu. Vui lòng đăng nhập lại rồi đặt đơn.',
      res.status
    );
  }

  if (!res.ok) {
    let message = `Không lưu được đơn hàng (HTTP ${res.status}).`;
    try {
      const body = await res.json();
      const details = body?.errors
        ? Object.values(body.errors as Record<string, string[]>).flat().join('; ')
        : '';
      message = details || body?.message || body?.error || message;
    } catch {
      // Giữ thông báo mặc định khi server không trả JSON
    }
    throw new ApiError(message, res.status);
  }

  return await res.json();
}

export async function updateOrderStatusApi(id: string, data: any) {
  const res = await fetch(`${API_BASE_URL}/api/Technical/orders/${encodeURIComponent(id)}/status`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}


// ==================== FETCH & DELETE APIS (Full DB Sync) ====================

// --- PLANS ---
export async function fetchPlansApi() {
  const res = await fetch(`${API_BASE_URL}/api/admin/serviceplan`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}
export async function deletePlanApi(id: string) {
  const res = await fetch(`${API_BASE_URL}/api/admin/serviceplan/${encodeURIComponent(id)}`, { method: 'DELETE', headers: authHeaders() });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- RETAIL SHOPS ---
export async function fetchRetailShopsApi() {
  const res = await fetch(`${API_BASE_URL}/api/admin/retailstore`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}
export async function deleteRetailShopApi(id: string) {
  const res = await fetch(`${API_BASE_URL}/api/admin/retailstore/${encodeURIComponent(id)}`, { method: 'DELETE', headers: authHeaders() });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- VENDORS ---
export async function fetchVendorsApi() {
  const res = await fetch(`${API_BASE_URL}/api/admin/vendor`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}
export async function deleteVendorApi(id: string) {
  const res = await fetch(`${API_BASE_URL}/api/admin/vendor/${encodeURIComponent(id)}`, { method: 'DELETE', headers: authHeaders() });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- ORDERS ---
export async function fetchOrdersApi() {
  const res = await fetch(`${API_BASE_URL}/api/Technical/orders`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- EMPLOYEES ---
export async function fetchEmployeesApi() {
  const res = await fetch(`${API_BASE_URL}/api/admin/employee`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}
export async function deleteEmployeeApi(id: string) {
  const res = await fetch(`${API_BASE_URL}/api/admin/employee/${encodeURIComponent(id)}`, { method: 'DELETE', headers: authHeaders() });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- INVENTORY ---
export async function fetchInventoryApi() {
  const res = await fetch(`${API_BASE_URL}/api/admin/inventory`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}
export async function deleteInventoryItemApi(id: string) {
  const res = await fetch(`${API_BASE_URL}/api/admin/inventory/${encodeURIComponent(id)}`, { method: 'DELETE', headers: authHeaders() });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}

// --- EQUIPMENT ---
// --- FEEDBACKS ---
export async function createFeedbackApi(data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/feedback`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}
export async function updateFeedbackApi(id: string, data: any) {
  const res = await fetch(`${API_BASE_URL}/api/admin/feedback/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(await res.text());
  return await res.json();
}
// --- SYSTEM SETTINGS ---
// ==================== CỔNG KHÁCH HÀNG (Customer portal) ====================
// /api/billing khoá role Senior Accountant, nên khách đọc hoá đơn của chính
// mình qua 2 endpoint riêng bên dưới (quyền sở hữu lọc tại server).

export async function updateCustomerProfileApi(data: {
  name: string;
  email: string;
  phoneNumber: string;
  address?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/customer/profile`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi cập nhật hồ sơ (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function fetchMyBillsApi() {
  const res = await fetch(`${API_BASE_URL}/api/customer/bills`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(`Lỗi tải hóa đơn của bạn (HTTP ${res.status})`);
  return await res.json();
}

export async function updateProfileApi(data: {
  name?: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/profile`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || `Lỗi cập nhật hồ sơ (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function updatePasswordApi(data: {
  currentPassword?: string;
  newPassword?: string;
}) {
  const res = await fetch(`${API_BASE_URL}/api/profile/password`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || `Lỗi đổi mật khẩu (HTTP ${res.status})`);
  }
  return await res.json();
}

// ==================== PURCHASE LIST (đơn đặt mua thiết bị từ vendor) ====================

export interface PurchaseOrderDto {
  purchaseOrderId: string;
  vendorId: string;
  vendorName?: string | null;
  storeId?: string | null;
  itemCode: string;
  itemName: string;
  category: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  orderDate: string;
  expectedDate?: string | null;
  receivedDate?: string | null;
  status: 'Draft' | 'Submitted' | 'Received' | 'Cancelled';
  notes?: string | null;
  createdBy?: string | null;
}

export async function fetchPurchaseOrdersApi(): Promise<PurchaseOrderDto[]> {
  const res = await fetch(`${API_BASE_URL}/api/admin/purchaseorder`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(`Lỗi tải danh sách đơn đặt mua (HTTP ${res.status})`);
  return await res.json();
}

export async function createPurchaseOrderApi(data: {
  vendorId: string;
  storeId?: string;
  itemCode: string;
  itemName: string;
  category: string;
  quantity: number;
  unitCost: number;
  expectedDate?: string;
  notes?: string;
}): Promise<PurchaseOrderDto> {
  const res = await fetch(`${API_BASE_URL}/api/admin/purchaseorder`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi tạo đơn đặt mua (HTTP ${res.status})`);
  }
  return await res.json();
}

/** Đổi trạng thái đơn mua: Submitted → Received (tự cộng kho) hoặc Cancelled. */
export async function updatePurchaseOrderStatusApi(id: string, status: string): Promise<PurchaseOrderDto> {
  const res = await fetch(`${API_BASE_URL}/api/admin/purchaseorder/${encodeURIComponent(id)}/status`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ status }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi cập nhật đơn đặt mua (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function deletePurchaseOrderApi(id: string): Promise<void> {
  const res = await fetch(`${API_BASE_URL}/api/admin/purchaseorder/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: authHeaders(),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi xóa đơn đặt mua (HTTP ${res.status})`);
  }
}

// ---------------------------------------------------------------------------
// Equipment Requests (Yêu cầu nhập thiết bị / vật tư từ KTV gửi Admin)
// ---------------------------------------------------------------------------

export interface EquipmentRequestDto {
  requestId: string;
  orderId?: string | null;
  storeId: string;
  storeName?: string;
  employeeId: string;
  employeeName?: string;
  inventoryId?: string | null;
  itemName: string;
  deviceType: string;
  quantity: number;
  urgency: 'Low' | 'Normal' | 'High' | 'Urgent';
  status: 'Pending' | 'Approved' | 'Fulfilled' | 'Rejected';
  reason?: string | null;
  adminNotes?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export async function fetchEquipmentRequestsApi(filters?: {
  status?: string;
  storeId?: string;
  search?: string;
}): Promise<EquipmentRequestDto[]> {
  const params = new URLSearchParams();
  if (filters?.status && filters.status !== 'all') params.set('status', filters.status);
  if (filters?.storeId) params.set('storeId', filters.storeId);
  if (filters?.search) params.set('search', filters.search);

  const qs = params.toString();
  const url = `${API_BASE_URL}/api/technical/equipment-requests${qs ? `?${qs}` : ''}`;
  const res = await fetch(url, { headers: authHeaders() });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi tải danh sách yêu cầu nhập hàng (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function createEquipmentRequestApi(data: {
  orderId?: string | null;
  storeId: string;
  inventoryId?: string | null;
  itemName: string;
  deviceType: string;
  quantity: number;
  urgency?: string;
  reason?: string | null;
}): Promise<EquipmentRequestDto> {
  const res = await fetch(`${API_BASE_URL}/api/technical/equipment-requests`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi gửi yêu cầu nhập hàng (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function updateEquipmentRequestStatusApi(
  id: string,
  status: string,
  adminNotes?: string
): Promise<EquipmentRequestDto> {
  const res = await fetch(`${API_BASE_URL}/api/admin/equipment-requests/${encodeURIComponent(id)}/status`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ status, adminNotes }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi cập nhật trạng thái yêu cầu nhập hàng (HTTP ${res.status})`);
  }
  return await res.json();
}

export async function deleteEquipmentRequestApi(id: string): Promise<void> {
  const res = await fetch(`${API_BASE_URL}/api/technical/equipment-requests/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: authHeaders(),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Lỗi xóa yêu cầu nhập hàng (HTTP ${res.status})`);
  }
}

