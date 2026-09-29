import type {
  Connection,
  ConnectionStatus,
  Equipment,
  InventoryItem,
  Order,
} from "../types/nexus";

type ApiErrorBody = {
  message?: string;
  errors?: Record<string, string[]>;
};

export class TechnicalApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly validationErrors?: Record<string, string[]>,
  ) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  // Backend khoá /api/Technical theo [Authorize] nên mọi request phải mang JWT
  // lấy lúc đăng nhập (KeyClaim: Authorization: Bearer <token>). Request đi qua
  // Vite proxy cùng origin nên header được chuyển tiếp nguyên vẹn.
  const token = localStorage.getItem("nexus_jwt_token");
  const response = await fetch(path, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let body: ApiErrorBody | null = null;
    try {
      body = (await response.json()) as ApiErrorBody;
    } catch {
      // The fallback below also handles empty and non-JSON error responses.
    }

    const validationMessage = body?.errors
      ? Object.values(body.errors).flat().join("; ")
      : undefined;
    throw new TechnicalApiError(
      validationMessage || body?.message || `API request failed (${response.status})`,
      response.status,
      body?.errors,
    );
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export interface TechnicalOrderDto {
  orderId: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string | null;
  planId: string;
  planName: string;
  connectionType: Order["connectionType"];
  storeId: string;
  storeName: string;
  installationAddress: string;
  applicationDate: string;
  status: Order["status"];
  cableDistanceMeters?: number | null;
  dpBoxCapacity?: string | null;
  signalLossDbm?: number | null;
  feasibilityNotes?: string | null;
  rejectionReason?: string | null;
  internetFeasible?: boolean | null;
  landlineFeasible?: boolean | null;
  feasibilityCheckedBy?: string | null;
  feasibilityCheckedDate?: string | null;
  scheduledInstallDate?: string | null;
  assignedAccountId?: string | null;
  provisionedConnections: number;
  bulkConnectionsCount: number;
  bulkDiscountPercent: number;
  existingLandlineAccountId?: string | null;
  // Thông tin duyệt / phân công mà backend đã trả sẵn — trước đây bị bỏ qua khi map.
  retailApprovedBy?: string | null;
  retailApprovedDate?: string | null;
  retailApprovalNotes?: string | null;
  assignedTechnicianName?: string | null;
  assignedTechnicianPhone?: string | null;
  assignedTechnicianDate?: string | null;
  employeeId?: string | null;
  idProofType?: Order["idProofType"] | null;
  idProofNumber?: string | null;
  // Orders.DepositWaived: đơn > 50 kết nối được miễn tiền cọc.
  depositWaived?: boolean | null;
}

export interface TechnicalConnectionDto {
  accountId: string;
  orderId: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string | null;
  installationAddress: string;
  planName: string;
  connectionType: Connection["connectionType"];
  monthlyRental: number;
  securityDeposit: number;
  equipmentId?: string | null;
  equipmentSerialNumber?: string | null;
  equipmentModel?: string | null;
  status: ConnectionStatus;
  ipAddress?: string | null;
  portNumber?: string | null;
  installedDate: string;
  suspensionStartDate?: string | null;
  suspensionEndDate?: string | null;
  terminatedDate?: string | null;
  lastUpdated: string;
  lastStatusReason?: string | null;
}

export interface TechnicalEquipmentDto {
  equipmentId: string;
  inventoryId?: string | null;
  serialNumber?: string | null;
  macAddress?: string | null;
  deviceModel: string;
  deviceType: Equipment["deviceType"];
  vendorId: string;
  storeId?: string | null;
  status: Equipment["status"];
  firmwareVersion?: string | null;
  assignedAccountId?: string | null;
  assignedTechnicianId?: string | null;
  technicianName?: string | null;
  installedDate?: string | null;
}

export interface TechnicalInventoryDto {
  inventoryId: string;
  itemCode: string;
  name: string;
  category: InventoryItem["category"];
  stockQuantity: number;
  reorderLevel: number;
  unitCost: number;
  location?: string | null;
  vendorId?: string | null;
  vendorName?: string | null;
  status: string;
}

export interface TechnicalWorkspace {
  orders: Order[];
  connections: Connection[];
  equipments: Equipment[];
  inventory: InventoryItem[];
}

export interface ConnectionActivityLog {
  logId: number;
  actionType: string;
  oldValue?: string | null;
  newValue?: string | null;
  reason?: string | null;
  performedBy?: string | null;
  performedByName?: string | null;
  timestamp: string;
}

function queryString(values: Record<string, string | boolean | undefined>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== "") params.set(key, String(value));
  });
  const query = params.toString();
  return query ? `?${query}` : "";
}

/** Tiêu chí advanced search theo spec: id (đi qua keyword), tên khách, loại
 *  kết nối, khoảng ngày (from/to), số điện thoại đăng ký. */
export interface AdvancedSearchFilters {
  keyword?: string;
  customerName?: string;
  phone?: string;
  connectionType?: string;
  fromDate?: string; // yyyy-MM-dd
  toDate?: string; // yyyy-MM-dd
}

export function searchOrdersAdvanced(filters: AdvancedSearchFilters = {}) {
  return request<TechnicalOrderDto[]>(`/api/Technical/orders${queryString(filters as Record<string, any>)}`);
}

export function getTechnicalOrders(filters: {
  status?: string;
  storeId?: string;
  keyword?: string;
} = {}) {
  return request<TechnicalOrderDto[]>(`/api/Technical/orders${queryString(filters)}`);
}

export function getTechnicalConnections(filters: {
  accountId?: string;
  status?: string;
  equipmentId?: string;
} = {}) {
  return request<TechnicalConnectionDto[]>(
    `/api/Technical/connections${queryString(filters)}`,
  );
}

export function getTechnicalEquipment(filters: {
  status?: string;
  storeId?: string;
  availableOnly?: boolean;
} = {}) {
  return request<TechnicalEquipmentDto[]>(
    `/api/Technical/equipment${queryString(filters)}`,
  );
}

export function getTechnicalInventory(filters: {
  category?: string;
  lowStockOnly?: boolean;
} = {}) {
  return request<TechnicalInventoryDto[]>(
    `/api/Technical/inventory${queryString(filters)}`,
  );
}

export async function loadTechnicalWorkspace(): Promise<TechnicalWorkspace> {
  const [orderDtos, connectionDtos, equipmentDtos, inventoryDtos] = await Promise.all([
    getTechnicalOrders(),
    getTechnicalConnections(),
    getTechnicalEquipment(),
    getTechnicalInventory(),
  ]);

  const connections: Connection[] = connectionDtos.map((item) => ({
    accountId: item.accountId,
    orderId: item.orderId,
    customerName: item.customerName,
    customerPhone: item.customerPhone,
    customerEmail: item.customerEmail ?? "",
    installationAddress: item.installationAddress,
    connectionType: item.connectionType,
    planName: item.planName,
    monthlyRental: item.monthlyRental,
    securityDeposit: item.securityDeposit,
    status: item.status,
    ipAddress: item.ipAddress ?? undefined,
    portNumber: item.portNumber ?? undefined,
    assignedDeviceSerial: item.equipmentSerialNumber ?? undefined,
    assignedDeviceModel: item.equipmentModel ?? undefined,
    installedDate: item.installedDate,
    suspensionStartDate: item.suspensionStartDate ?? undefined,
    suspensionEndDate: item.suspensionEndDate ?? undefined,
    terminatedDate: item.terminatedDate ?? undefined,
    lastUpdated: item.lastUpdated,
    lastStatusReason: item.lastStatusReason ?? undefined,
  }));

  return {
    orders: orderDtos.map((item) => ({
      id: item.orderId,
      customerName: item.customerName,
      customerPhone: item.customerPhone,
      customerEmail: item.customerEmail ?? "",
      installationAddress: item.installationAddress,
      // Lấy đúng giấy tờ khách đã khai (trước đây gán cứng "National ID Card" + số rỗng).
      idProofType: item.idProofType ?? "National ID Card",
      idProofNumber: item.idProofNumber ?? "",
      connectionType: item.connectionType,
      planId: item.planId,
      planName: item.planName,
      retailOutletCode: item.storeId,
      retailEmployeeName: item.storeName,
      createdAt: item.applicationDate,
      status: item.status,
      feasibilityNotes: item.feasibilityNotes ?? item.rejectionReason ?? undefined,
      cableDistanceMeters: item.cableDistanceMeters ?? undefined,
      dpBoxCapacity: item.dpBoxCapacity ?? undefined,
      signalLossDbm: item.signalLossDbm ?? undefined,
      assignedAccountId: item.assignedAccountId ?? undefined,
      scheduledInstallDate: item.scheduledInstallDate ?? undefined,
      feasibilityCheckedBy: item.feasibilityCheckedBy ?? undefined,
      feasibilityCheckedDate: item.feasibilityCheckedDate ?? undefined,
      bulkConnectionsCount: Math.max(1, item.bulkConnectionsCount),
      bulkDiscountPercent: item.bulkDiscountPercent,
      depositWaived: item.depositWaived ?? undefined,
      existingLandlineAccountId: item.existingLandlineAccountId ?? undefined,
      landlineFeasible: item.landlineFeasible ?? undefined,
      internetFeasible: item.internetFeasible ?? undefined,
      retailApprovedBy: item.retailApprovedBy ?? undefined,
      retailApprovedAt: item.retailApprovedDate ?? undefined,
      retailApprovalNotes: item.retailApprovalNotes ?? undefined,
      assignedTechnician: item.assignedTechnicianName ?? undefined,
      assignedTechnicianPhone: item.assignedTechnicianPhone ?? undefined,
      assignedTechnicianDate: item.assignedTechnicianDate ?? undefined,
    })),
    connections,
    equipments: equipmentDtos.map((item) => {
      const linkedConnection = connectionDtos.find(
        (connection) => connection.equipmentId === item.equipmentId,
      );
      return {
        id: item.equipmentId,
        inventoryId: item.inventoryId ?? undefined,
        vendorId: item.vendorId,
        storeId: item.storeId ?? undefined,
        serialNumber: item.serialNumber ?? "",
        macAddress: item.macAddress ?? "",
        deviceModel: item.deviceModel,
        deviceType: item.deviceType,
        assignedAccountId: item.assignedAccountId ?? undefined,
        assignedCustomerName: linkedConnection?.customerName,
        firmwareVersion: item.firmwareVersion ?? "",
        status: item.status,
        assignedTechnician: item.technicianName ?? undefined,
        installedDate: item.installedDate ?? undefined,
      };
    }),
    inventory: inventoryDtos.map((item) => ({
      id: item.inventoryId,
      itemCode: item.itemCode,
      name: item.name,
      category: item.category,
      stockQuantity: item.stockQuantity,
      reorderLevel: item.reorderLevel,
      unitCost: item.unitCost,
      location: item.location ?? "",
      supplier: item.vendorName ?? "",
      vendorId: item.vendorId ?? undefined,
      status: item.status,
    })),
  };
}

export interface FeasibilityPayload {
  isFeasible: boolean;
  checkedBy: string;
  notes?: string;
  rejectionReason?: string;
  landlineFeasible?: boolean;
  internetFeasible?: boolean;
  cableDistanceMeters?: number;
  dpBoxCapacity?: string;
  signalLossDbm?: number;
}

export interface OrderStatusResult {
  message: string;
  orderId: string;
  status: Order["status"];
  assignedAccountId?: string | null;
}

export function updateFeasibility(orderId: string, payload: FeasibilityPayload) {
  return request<OrderStatusResult>(
    `/api/Technical/orders/${encodeURIComponent(orderId)}/feasibility`,
    { method: "PUT", body: JSON.stringify(payload) },
  );
}

export function updateInstallationSchedule(
  orderId: string,
  payload: { scheduledInstallDate: string; technicianId?: string },
) {
  return request<{
    message: string;
    orderId: string;
    scheduledInstallDate: string;
    technicianId?: string | null;
    status: Order["status"];
  }>(`/api/Technical/orders/${encodeURIComponent(orderId)}/installation-schedule`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export interface ProvideConnectionResult {
  message: string;
  orderId: string;
  accountId: string;
  connectionStatus: ConnectionStatus;
  equipmentId: string;
  installedDate: string;
  provisionedConnections: number;
  requiredConnections: number;
  orderStatus: Order["status"];
}

export function provideConnection(
  orderId: string,
  payload: {
    equipmentId: string;
    portNumber?: string;
    ipAddress?: string;
    installedDate?: string;
    performedBy?: string;
  },
) {
  return request<ProvideConnectionResult>(
    `/api/Technical/orders/${encodeURIComponent(orderId)}/provide-connection`,
    { method: "POST", body: JSON.stringify(payload) },
  );
}

export function changeConnectionStatus(
  accountId: string,
  payload: { status: ConnectionStatus; reason?: string; performedBy?: string },
) {
  return request<{ message: string; accountId: string; oldStatus: string; status: ConnectionStatus }>(
    `/api/Technical/connections/${encodeURIComponent(accountId)}/status`,
    { method: "PUT", body: JSON.stringify(payload) },
  );
}

export function getConnectionActivityLogs(accountId: string) {
  return request<ConnectionActivityLog[]>(
    `/api/Technical/connections/${encodeURIComponent(accountId)}/activity-logs`,
  );
}

export function createEquipment(payload: {
  equipmentId: string;
  inventoryId?: string;
  serialNumber: string;
  macAddress: string;
  deviceModel: string;
  deviceType: Equipment["deviceType"];
  vendorId: string;
  storeId?: string;
  status: Equipment["status"];
  firmwareVersion?: string;
}) {
  return request<{ message: string; equipmentId: string; status: Equipment["status"] }>(
    "/api/Technical/equipment",
    { method: "POST", body: JSON.stringify(payload) },
  );
}


export function updateEquipment(
  equipmentId: string,
  payload: {
    serialNumber?: string;
    macAddress?: string;
    deviceModel?: string;
    deviceType?: Equipment["deviceType"];
    storeId?: string;
    status?: Equipment["status"];
    firmwareVersion?: string;
    assignedTechnicianId?: string;
    installedDate?: string;
  },
) {
  return request<{ message: string; equipmentId: string; status: Equipment["status"] }>(
    `/api/Technical/equipment/${encodeURIComponent(equipmentId)}`,
    { method: "PUT", body: JSON.stringify(payload) },
  );
}

export function updateInventory(
  inventoryId: string,
  payload: {
    stockQuantity?: number;
    reorderLevel?: number;
    unitCost?: number;
    location?: string;
    vendorId?: string;
  },
) {
  return request<{
    message: string;
    inventoryId: string;
    stockQuantity: number;
    reorderLevel: number;
    isLowStock: boolean;
    status: string;
  }>(`/api/Technical/inventory/${encodeURIComponent(inventoryId)}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export function adjustInventoryStock(
  inventoryId: string,
  payload: { quantityChange: number; reason: string },
) {
  return request<{
    message: string;
    inventoryId: string;
    quantityChange: number;
    stockQuantity: number;
    status: string;
  }>(`/api/Technical/inventory/${encodeURIComponent(inventoryId)}/adjust-stock`, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}
