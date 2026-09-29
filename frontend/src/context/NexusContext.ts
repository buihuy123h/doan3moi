import { writable, get } from 'svelte/store';
import {
  CONNECTION_TYPE_LETTER,
  getBulkDiscountPercent,
  isDepositWaived,
  DEPOSIT_WAIVER_MIN_CONNECTIONS,
  type Plan,
  type Employee,
  type Vendor,
  type RetailShop,
  type InventoryItem,
  type Order,
  type Connection,
  type Equipment,
  type Bill,
  type PaymentRecord,
  type Feedback,
  type PurchaseOrder,
  type EquipmentRequest,
  type SystemSettings,
  type OrderStatus,
  type ConnectionStatus,
  type ConnectionType,
  type RoleType,
  isReleasedToTechnical,
  isAwaitingRetailApproval,
  RETAIL_STAGE_STATUSES,
} from '../types/nexus';
import {
  updateEmployeeApi,
  createEmployeeApi,
  deleteEmployeeApi,
  updateInventoryItemApi,
  createInventoryItemApi,
  deleteInventoryItemApi,
  createEquipmentApi,
  createBillApi,
  updateBillApi,
  recordPaymentApi,
  recordRetailPaymentApi,
  fetchBillsApi,
  fetchMyBillsApi,
  createFeedbackApi,
  updateFeedbackApi,
  fetchConnectionsApi,
  createOrUpdateConnectionApi,
  createOrderApi,
  updateOrderStatusApi,
  fetchOrdersApi,
  createPlanApi,
  updatePlanApi,
  deletePlanApi,
  createVendorApi,
  updateVendorApi,
  deleteVendorApi,
  createRetailShopApi,
  updateRetailShopApi,
  deleteRetailShopApi,
  fetchPurchaseOrdersApi,
  createPurchaseOrderApi,
  updatePurchaseOrderStatusApi,
  deletePurchaseOrderApi,
  fetchEquipmentRequestsApi,
  createEquipmentRequestApi,
  updateEquipmentRequestStatusApi,
  deleteEquipmentRequestApi,
  fetchPlansApi,
  fetchRetailShopsApi,
  fetchVendorsApi,
  fetchEmployeesApi,
  fetchInventoryApi,
  API_BASE_URL,
  authHeaders,
} from '../lib/api';

export { getBulkDiscountPercent, isDepositWaived, DEPOSIT_WAIVER_MIN_CONNECTIONS, isReleasedToTechnical, isAwaitingRetailApproval, RETAIL_STAGE_STATUSES };

// 11-char Order ID: prefix D/B/T + 10-digit serial (e.g. D0000000001)
export function generateOrderId(type: ConnectionType, count: number): string {
  const prefix = CONNECTION_TYPE_LETTER[type];
  return `${prefix}${String(count).padStart(10, '0')}`;
}

// Display form: T064-000000000001
export function formatAccountId(raw: string): string {
  const s = (raw || '').toUpperCase().replace(/[^A-Z0-9]/g, '');
  return s.length > 4 ? `${s.slice(0, 4)}-${s.slice(4)}` : s;
}

// 16-char Account ID per spec: [type letter D/B/T][3-digit city code][12-digit serial]
// Returned in the canonical dashed display form (T064-000000000001).
export function generateAccountId(type: ConnectionType, cityCode: string, serial: number): string {
  const letter = CONNECTION_TYPE_LETTER[type];
  const city = (cityCode || '999').replace(/\D/g, '').padStart(3, '0').slice(0, 3);
  return formatAccountId(`${letter}${city}${String(serial).padStart(12, '0')}`);
}

// Bare comparison key (drops the dash) so lookups are format-agnostic.
export function accountIdKey(raw: string): string {
  return (raw || '').toUpperCase().replace(/[^A-Z0-9]/g, '');
}

// ============ INITIAL MOCK DATA (identical to React version) ============

// Security deposit by connection type (spec): Dial-Up 325 / Broadband 500 / Landline 250
const DEPOSIT = { 'Dial-Up': 325, Broadband: 500, Landline: 250 } as const;

// Tariff catalogue transcribed from the spec's Financial pricing tables.
const INITIAL_PLANS: Plan[] = [
  // ---- Dial-Up ----
  {
    id: 'plan-du-h10',
    name: 'Dial-Up Hourly 10 Hrs',
    type: 'Dial-Up',
    speedOrBandwidth: '56 Kbps V.92',
    monthlyRental: 50,
    securityDeposit: DEPOSIT['Dial-Up'],
    dataLimit: '10 Hours',
    status: 'Active',
    billingCycle: 'Hourly Pack',
    validity: '1 Month',
    includedHours: 10,
    description: 'Prepaid 10-hour dial-up pack, valid for one month.',
  },
  {
    id: 'plan-du-h30',
    name: 'Dial-Up Hourly 30 Hrs',
    type: 'Dial-Up',
    speedOrBandwidth: '56 Kbps V.92',
    monthlyRental: 130,
    securityDeposit: DEPOSIT['Dial-Up'],
    dataLimit: '30 Hours',
    status: 'Active',
    billingCycle: 'Hourly Pack',
    validity: '3 Months',
    includedHours: 30,
    description: 'Prepaid 30-hour dial-up pack, valid for three months.',
  },
  {
    id: 'plan-du-h60',
    name: 'Dial-Up Hourly 60 Hrs',
    type: 'Dial-Up',
    speedOrBandwidth: '56 Kbps V.92',
    monthlyRental: 260,
    securityDeposit: DEPOSIT['Dial-Up'],
    dataLimit: '60 Hours',
    status: 'Active',
    billingCycle: 'Hourly Pack',
    validity: '6 Months',
    includedHours: 60,
    description: 'Prepaid 60-hour dial-up pack, valid for six months.',
  },
  {
    id: 'plan-du-28',
    name: 'Dial-Up Unlimited 28 Kbps',
    type: 'Dial-Up',
    speedOrBandwidth: '28 Kbps',
    monthlyRental: 75,
    securityDeposit: DEPOSIT['Dial-Up'],
    dataLimit: 'Unlimited',
    status: 'Active',
    billingCycle: 'Monthly',
    validity: '1 Month',
    description: 'Unlimited dial-up access at 28 Kbps. Quarterly billing also available at $150.',
  },
  {
    id: 'plan-du-56',
    name: 'Dial-Up Unlimited 56 Kbps',
    type: 'Dial-Up',
    speedOrBandwidth: '56 Kbps',
    monthlyRental: 100,
    securityDeposit: DEPOSIT['Dial-Up'],
    dataLimit: 'Unlimited',
    status: 'Active',
    billingCycle: 'Monthly',
    validity: '1 Month',
    description: 'Unlimited dial-up access at 56 Kbps. Quarterly billing also available at $180.',
  },
  // ---- Broadband ----
  {
    id: 'plan-bb-h30',
    name: 'Broadband Hourly 30 Hrs',
    type: 'Broadband',
    speedOrBandwidth: 'Broadband',
    monthlyRental: 175,
    securityDeposit: DEPOSIT.Broadband,
    dataLimit: '30 Hours',
    status: 'Active',
    billingCycle: 'Hourly Pack',
    validity: '1 Month',
    includedHours: 30,
    description: 'Prepaid 30-hour broadband pack, valid for one month.',
  },
  {
    id: 'plan-bb-h60',
    name: 'Broadband Hourly 60 Hrs',
    type: 'Broadband',
    speedOrBandwidth: 'Broadband',
    monthlyRental: 315,
    securityDeposit: DEPOSIT.Broadband,
    dataLimit: '60 Hours',
    status: 'Active',
    billingCycle: 'Hourly Pack',
    validity: '6 Months',
    includedHours: 60,
    description: 'Prepaid 60-hour broadband pack, valid for six months.',
  },
  {
    id: 'plan-bb-64',
    name: 'Broadband Unlimited 64 Kbps',
    type: 'Broadband',
    speedOrBandwidth: '64 Kbps',
    monthlyRental: 225,
    securityDeposit: DEPOSIT.Broadband,
    dataLimit: 'Unlimited',
    status: 'Active',
    billingCycle: 'Monthly',
    validity: '1 Month',
    description: 'Unlimited broadband at 64 Kbps. Quarterly billing also available at $400.',
  },
  {
    id: 'plan-bb-128',
    name: 'Broadband Unlimited 128 Kbps',
    type: 'Broadband',
    speedOrBandwidth: '128 Kbps',
    monthlyRental: 350,
    securityDeposit: DEPOSIT.Broadband,
    dataLimit: 'Unlimited',
    status: 'Active',
    billingCycle: 'Monthly',
    validity: '1 Month',
    description: 'Unlimited broadband at 128 Kbps. Quarterly billing also available at $445.',
  },
  // ---- Landline (telephone only) ----
  {
    id: 'plan-ll-local-y',
    name: 'Landline Local - Unlimited (Yearly)',
    type: 'Landline',
    speedOrBandwidth: 'PSTN Voice',
    monthlyRental: 75,
    securityDeposit: DEPOSIT.Landline,
    dataLimit: 'Unlimited Local',
    status: 'Active',
    billingCycle: 'Yearly',
    validity: '1 Year',
    callRates: 'Local: 55¢/min',
    description: 'Local plan, yearly rental. Call charges billed on top of the rental.',
  },
  {
    id: 'plan-ll-local-m',
    name: 'Landline Local - Monthly',
    type: 'Landline',
    speedOrBandwidth: 'PSTN Voice',
    monthlyRental: 35,
    securityDeposit: DEPOSIT.Landline,
    dataLimit: 'Local Calling',
    status: 'Active',
    billingCycle: 'Monthly',
    validity: '1 Month',
    callRates: 'Local: 75¢/min',
    description: 'Local plan, monthly rental. Call charges billed on top of the rental.',
  },
  {
    id: 'plan-ll-std-m',
    name: 'Landline STD - Monthly',
    type: 'Landline',
    speedOrBandwidth: 'PSTN Voice',
    monthlyRental: 125,
    securityDeposit: DEPOSIT.Landline,
    dataLimit: 'Local + STD',
    status: 'Active',
    billingCycle: 'Monthly',
    validity: '1 Month',
    callRates: 'Local: 70¢/min · STD: $2.25/min · SMS to mobile: $1.00/min',
    description: 'STD plan, monthly rental with local, STD and mobile-messaging call charges.',
  },
  {
    id: 'plan-ll-std-h',
    name: 'Landline STD - Half-Yearly',
    type: 'Landline',
    speedOrBandwidth: 'PSTN Voice',
    monthlyRental: 420,
    securityDeposit: DEPOSIT.Landline,
    dataLimit: 'Local + STD',
    status: 'Active',
    billingCycle: 'Half-Yearly',
    validity: '6 Months',
    callRates: 'Local: 60¢/min · STD: $2.00/min · SMS to mobile: $1.15/min',
    description: 'STD plan, half-yearly rental with reduced call charges.',
  },
  {
    id: 'plan-ll-std-y',
    name: 'Landline STD - Yearly',
    type: 'Landline',
    speedOrBandwidth: 'PSTN Voice',
    monthlyRental: 780,
    securityDeposit: DEPOSIT.Landline,
    dataLimit: 'Local + STD',
    status: 'Active',
    billingCycle: 'Yearly',
    validity: '1 Year',
    callRates: 'Local: 60¢/min · STD: $1.75/min · SMS to mobile: $1.25/min',
    description: 'STD plan, yearly rental with the lowest call charges.',
  },
];

const INITIAL_EMPLOYEES: Employee[] = [
  {
    id: 'emp-01',
    employeeCode: 'EMP-1001',
    name: 'Sarah Jenkins',
    email: 'sarah.jenkins@nexus.telecom',
    phone: '+1 (555) 234-8901',
    role: 'Manager',
    department: 'Administration',
    status: 'Active',
    dateOfJoining: '2022-03-15',
  },
  {
    id: 'emp-02',
    employeeCode: 'EMP-1042',
    name: 'David Chen',
    email: 'david.chen@nexus.telecom',
    phone: '+1 (555) 456-1123',
    role: 'Retail Staff',
    department: 'Retail Outlets',
    retailShopAssigned: 'Downtown Flagship (SH-01)',
    status: 'Active',
    dateOfJoining: '2023-06-10',
  },
  {
    id: 'emp-03',
    employeeCode: 'EMP-1077',
    name: 'Marcus Ramirez',
    email: 'marcus.ramirez@nexus.telecom',
    phone: '+1 (555) 789-3344',
    role: 'Field Engineer',
    department: 'Technical Operations',
    retailShopAssigned: 'Downtown Flagship (SH-01)',
    status: 'Active',
    dateOfJoining: '2021-11-04',
  },
  {
    id: 'emp-04',
    employeeCode: 'EMP-1090',
    name: 'Elena Rostova',
    email: 'elena.rostova@nexus.telecom',
    phone: '+1 (555) 901-5567',
    role: 'Senior Accountant',
    department: 'Finance & Accounts',
    status: 'Active',
    dateOfJoining: '2020-08-20',
  },
  {
    id: 'emp-05',
    employeeCode: 'EMP-1105',
    name: 'Aiden Brooks',
    email: 'aiden.brooks@nexus.telecom',
    phone: '+1 (555) 345-6789',
    role: 'Retail Staff',
    department: 'Retail Outlets',
    retailShopAssigned: 'Metro Uptown Hub (SH-02)',
    status: 'Active',
    dateOfJoining: '2024-01-15',
  },
  {
    id: 'emp-06',
    employeeCode: 'EMP-1120',
    name: 'Alex Tran',
    email: 'alex.tran@nexus.telecom',
    phone: '+1 (555) 882-9911',
    role: 'Field Engineer',
    department: 'Technical Operations',
    retailShopAssigned: 'Metro Uptown Hub (SH-02)',
    status: 'Active',
    dateOfJoining: '2022-04-18',
  },
  {
    id: 'emp-07',
    employeeCode: 'EMP-1135',
    name: 'Liam Nguyen',
    email: 'liam.nguyen@nexus.telecom',
    phone: '+1 (555) 667-4422',
    role: 'Field Engineer',
    department: 'Technical Operations',
    retailShopAssigned: 'Queens Center (SH-03)',
    status: 'Active',
    dateOfJoining: '2023-01-10',
  },
  {
    id: 'emp-08',
    employeeCode: 'EMP-1150',
    name: 'Carlos Mendez',
    email: 'carlos.mendez@nexus.telecom',
    phone: '+1 (555) 991-3377',
    role: 'Field Engineer',
    department: 'Technical Operations',
    retailShopAssigned: 'Brooklyn Depot (SH-04)',
    status: 'Active',
    dateOfJoining: '2023-09-01',
  },
];

const INITIAL_VENDORS: Vendor[] = [
  {
    id: 'vnd-01',
    vendorCode: 'VND-401',
    companyName: 'Corning Optical Systems Ltd',
    contactPerson: 'Gregory Vance',
    category: 'Fiber Optics & Cabling',
    phone: '+1 (800) 522-6789',
    email: 'sales@corning-telecom.com',
    address: '800 Corning Way, Hickory, NC',
    rating: 5,
    status: 'Active',
  },
  {
    id: 'vnd-02',
    vendorCode: 'VND-402',
    companyName: 'Cisco Systems Commercial Hardware',
    contactPerson: 'Linda Morrison',
    category: 'Modems & Routers',
    phone: '+1 (800) 553-6387',
    email: 'enterprise-hw@cisco.com',
    address: '170 West Tasman Dr, San Jose, CA',
    rating: 5,
    status: 'Active',
  },
  {
    id: 'vnd-03',
    vendorCode: 'VND-403',
    companyName: 'Zyxel Communications Corp',
    contactPerson: 'Kenji Sato',
    category: 'Modems & Routers',
    phone: '+1 (714) 632-0882',
    email: 'support-b2b@zyxel.com',
    address: '1130 North Miller St, Anaheim, CA',
    rating: 4,
    status: 'Active',
  },
  {
    id: 'vnd-04',
    vendorCode: 'VND-404',
    companyName: 'Amphenol Telecom Assemblies',
    contactPerson: 'Rachel Ward',
    category: 'Telecom Switches',
    phone: '+1 (203) 265-8900',
    email: 'supply@amphenol-rf.com',
    address: '358 Hall Avenue, Wallingford, CT',
    rating: 4,
    status: 'Active',
  },
];

// 3-digit numeric code assigned to each city within the territory (used in Account IDs).
export const CITY_CODES: Record<string, string> = {
  'New York': '064',
  Queens: '072',
  Brooklyn: '081',
  Manhattan: '064',
};

export function cityCodeFor(city: string): string {
  return CITY_CODES[city] ?? '999';
}

const INITIAL_RETAIL_SHOPS: RetailShop[] = [
  {
    id: 'sh-01',
    shopCode: 'SH-01',
    name: 'Downtown Nexus Flagship Store',
    city: 'New York',
    cityCode: '064',
    address: '452 Broadway, Manhattan, NY 10013',
    managerName: 'David Chen',
    phone: '+1 (212) 555-0144',
    operatingHours: 'Mon-Sat: 08:30 - 20:00, Sun: 10:00 - 18:00',
    activeEmployeesCount: 6,
    totalSubscribersServed: 1420,
  },
  {
    id: 'sh-02',
    shopCode: 'SH-02',
    name: 'Metro Uptown Tech Hub',
    city: 'New York',
    cityCode: '064',
    address: '2190 Broadway, Upper West Side, NY 10024',
    managerName: 'Aiden Brooks',
    phone: '+1 (212) 555-0189',
    operatingHours: 'Mon-Sat: 09:00 - 19:30',
    activeEmployeesCount: 4,
    totalSubscribersServed: 980,
  },
  {
    id: 'sh-03',
    shopCode: 'SH-03',
    name: 'Queens Central Service Center',
    city: 'Queens',
    cityCode: '072',
    address: '70-20 Austin St, Forest Hills, NY 11375',
    managerName: 'Kavita Patel',
    phone: '+1 (718) 555-0199',
    operatingHours: 'Mon-Fri: 09:00 - 18:00, Sat: 09:00 - 15:00',
    activeEmployeesCount: 3,
    totalSubscribersServed: 750,
  },
  {
    id: 'sh-04',
    shopCode: 'SH-04',
    name: 'Brooklyn Nexus Connect Depot',
    city: 'Brooklyn',
    cityCode: '081',
    address: '320 Atlantic Ave, Boerum Hill, NY 11201',
    managerName: 'Robert Gomez',
    phone: '+1 (718) 555-0210',
    operatingHours: 'Mon-Sat: 09:00 - 19:00',
    activeEmployeesCount: 5,
    totalSubscribersServed: 1120,
  },
];

const INITIAL_INVENTORY: InventoryItem[] = [
  {
    id: 'inv-01',
    itemCode: 'EQ-ONT-FBR',
    name: 'Huawei EchoLife HG8245H5 GPON ONT',
    category: 'Fiber ONT',
    stockQuantity: 145,
    reorderLevel: 25,
    unitCost: 55.0,
    location: 'Central Depot Bay 4A',
    supplier: 'Cisco Systems Commercial Hardware',
  },
  {
    id: 'inv-02',
    itemCode: 'EQ-RTR-AX',
    name: 'Nexus Wi-Fi 6 AX3000 Dual-Band Router',
    category: 'Router',
    stockQuantity: 88,
    reorderLevel: 20,
    unitCost: 65.0,
    location: 'Central Depot Bay 2B',
    supplier: 'Zyxel Communications Corp',
  },
  {
    id: 'inv-03',
    itemCode: 'EQ-MDM-V92',
    name: 'USRobotics 56K V.92 Faxmodem USB/PSTN',
    category: 'Modem',
    stockQuantity: 18,
    reorderLevel: 10,
    unitCost: 32.0,
    location: 'Central Depot Bay 7C',
    supplier: 'Amphenol Telecom Assemblies',
  },
  {
    id: 'inv-04',
    itemCode: 'EQ-ATA-VOIP',
    name: 'Grandstream HT802 2-Port Analog VoIP Adapter',
    category: 'VoIP Adapter',
    stockQuantity: 42,
    reorderLevel: 15,
    unitCost: 28.0,
    location: 'Central Depot Bay 3C',
    supplier: 'Amphenol Telecom Assemblies',
  },
  {
    id: 'inv-05',
    itemCode: 'EQ-SPL-1X8',
    name: 'Corning 1x8 PLC Optical Fiber Splitter',
    category: 'Splitter',
    stockQuantity: 9, // Low stock indicator
    reorderLevel: 15,
    unitCost: 14.5,
    location: 'Central Depot Bay 1A',
    supplier: 'Corning Optical Systems Ltd',
  },
];

const INITIAL_ORDERS: Order[] = [
  {
    id: 'D0000000001',
    customerName: 'Arthur Pendelton',
    customerPhone: '+1 (555) 902-1844',
    customerEmail: 'arthur.p@classiccorp.net',
    installationAddress: '144 West 82nd St, Apt 4B, New York, NY 10024',
    idProofType: 'National ID Card',
    idProofNumber: 'ID-US-9918231',
    connectionType: 'Dial-Up',
    planId: 'plan-du-56',
    planName: 'Dial-Up Unlimited 56 Kbps',
    retailOutletCode: 'SH-02',
    retailEmployeeName: 'Aiden Brooks',
    // STAGE 1: freshly submitted, still waiting for the SH-02 desk to check paperwork.
    assignedEmployeeId: 'emp-05',
    assignedBranchName: 'Metro Uptown Tech Hub',
    createdAt: '2026-09-04 10:30',
    status: 'PendingRetail',
    // retailApprovedBy: 'Aiden Brooks',
    // retailApprovedAt: '2026-09-04 10:35',
    assignedTechnician: 'Alex Tran',
    assignedTechnicianId: 'emp-06',
    assignedTechnicianPhone: '+1 (555) 882-9911',
    assignedTechnicianDate: '2026-09-04 10:40',
    cableDistanceMeters: 420,
    dpBoxCapacity: 'Port 6 Available / DP-B12',
    signalLossDbm: -18.5,
    bulkConnectionsCount: 1,
    bulkDiscountPercent: 0,
    // New Dial-Up customer with no Nexus landline yet: both legs need a check.
  },
  {
    id: 'B0000000002',
    customerName: 'Samantha Vance',
    customerPhone: '+1 (555) 301-4477',
    customerEmail: 'samantha.vance@gmail.com',
    installationAddress: '78 Mercer St, Soho, New York, NY 10012',
    idProofType: 'Passport',
    idProofNumber: 'P-98827419',
    connectionType: 'Broadband',
    planId: 'plan-bb-128',
    planName: 'Broadband Unlimited 128 Kbps',
    retailOutletCode: 'SH-01',
    retailEmployeeName: 'David Chen',
    // STAGE 2 done by the SH-01 desk, STAGE 3 passed by Technical.
    assignedEmployeeId: 'emp-02',
    assignedBranchName: 'Downtown Nexus Flagship Store',
    retailApprovedBy: 'David Chen',
    retailApprovedAt: '2026-09-04 15:02',
    retailApprovalNotes: 'Hồ sơ SCTT hợp lệ, đúng địa bàn chi nhánh.',
    createdAt: '2026-09-04 14:15',
    status: 'Feasible',
    assignedAccountId: 'B064-000000000005',
    feasibilityNotes: 'Fiber termination box available within 85m. Signal strength -16.2 dBm (Excellent). Line tested OK.',
    cableDistanceMeters: 85,
    dpBoxCapacity: 'Port 2 Available / DP-S04',
    signalLossDbm: -16.2,
    bulkConnectionsCount: 1,
    bulkDiscountPercent: 0,
    internetFeasible: true,
  },
  {
    id: 'T0000000003',
    customerName: 'Highline Consulting LLC',
    customerPhone: '+1 (555) 777-8899',
    customerEmail: 'office@highlineconsulting.com',
    installationAddress: '55 Hudson Yards, Fl 18, New York, NY 10001',
    idProofType: "Driver's License",
    idProofNumber: 'DL-NY-2940192',
    connectionType: 'Landline',
    planId: 'plan-ll-std-m',
    planName: 'Landline STD - Monthly',
    retailOutletCode: 'SH-01',
    retailEmployeeName: 'David Chen',
    createdAt: '2026-09-02 09:00',
    status: 'Connection Provided',
    assignedAccountId: 'T064-000000000001',
    feasibilityNotes: 'Copper loop line deployed. Line tested and audio quality verified.',
    cableDistanceMeters: 120,
    dpBoxCapacity: 'Port 8 Dedicated',
    signalLossDbm: -15.1,
    bulkConnectionsCount: 50, // corporate order — 50 lines => 75% scheme discount
    bulkDiscountPercent: 75,
    landlineFeasible: true,
  },
  {
    id: 'B0000000004',
    customerName: 'Robert Lewandowski',
    customerPhone: '+1 (555) 621-9988',
    customerEmail: 'robert.lewan@yahoo.com',
    installationAddress: '89-12 Far Rockaway Blvd, Queens, NY 11693',
    idProofType: 'National ID Card',
    idProofNumber: 'ID-US-8827391',
    connectionType: 'Broadband',
    planId: 'plan-bb-64',
    planName: 'Broadband Unlimited 64 Kbps',
    retailOutletCode: 'SH-03',
    retailEmployeeName: 'Aiden Brooks',
    createdAt: '2026-09-03 11:45',
    status: 'Not Feasible',
    feasibilityNotes: 'Distance to nearest fiber distribution box exceeds 1,150 meters. Severe optical attenuation (-34 dBm). Requires main trunk extension.',
    cableDistanceMeters: 1150,
    dpBoxCapacity: 'No Spare Ports',
    signalLossDbm: -34.0,
    bulkConnectionsCount: 1,
    bulkDiscountPercent: 0,
    internetFeasible: false,
  },
  {
    id: 'B0000000005',
    customerName: 'Victoria Sterling',
    customerPhone: '+1 (555) 441-2099',
    customerEmail: 'v.sterling@apexlegal.org',
    installationAddress: '120 E 64th St, Manhattan, NY 10065',
    idProofType: 'National ID Card',
    idProofNumber: 'ID-US-7721890',
    connectionType: 'Broadband',
    planId: 'plan-bb-64',
    planName: 'Broadband Unlimited 64 Kbps',
    retailOutletCode: 'SH-01',
    retailEmployeeName: 'David Chen',
    assignedEmployeeId: 'emp-02',
    assignedBranchName: 'Downtown Nexus Flagship Store',
    createdAt: '2026-08-14 10:00',
    status: 'Connection Provided',
    assignedAccountId: 'B064-000000000002',
    feasibilityNotes: 'Direct splice into riser. Verified gigabit throughput.',
    cableDistanceMeters: 60,
    dpBoxCapacity: 'Port 1 Dedicated / DP-M01',
    signalLossDbm: -14.8,
    bulkConnectionsCount: 1,
    bulkDiscountPercent: 0,
    internetFeasible: true,
  },
  {
    id: 'D0000000006',
    customerName: 'Retro Arcade Lounge LLC',
    customerPhone: '+1 (555) 332-9011',
    customerEmail: 'manager@retroarcadeny.com',
    installationAddress: '31 St Marks pl, East Village, NY 10003',
    idProofType: "Driver's License",
    idProofNumber: 'DL-NY-3391024',
    connectionType: 'Dial-Up',
    planId: 'plan-du-56',
    planName: 'Dial-Up Unlimited 56 Kbps',
    retailOutletCode: 'SH-01',
    retailEmployeeName: 'David Chen',
    assignedEmployeeId: 'emp-02',
    assignedBranchName: 'Downtown Nexus Flagship Store',
    createdAt: '2026-07-08 15:30',
    status: 'Connection Provided',
    assignedAccountId: 'D064-000000000003',
    feasibilityNotes: 'PSTN copper pair active.',
    cableDistanceMeters: 150,
    dpBoxCapacity: 'PSTN Riser Port 3',
    signalLossDbm: -17.2,
    bulkConnectionsCount: 1,
    bulkDiscountPercent: 0,
    landlineFeasible: true,
    internetFeasible: true,
  },
  {
    id: 'B0000000007',
    customerName: 'Jonathan Meyer',
    customerPhone: '+1 (555) 881-2300',
    customerEmail: 'j.meyer@brooklynloft.io',
    installationAddress: '175 Water St, Dumbo, Brooklyn, NY 11201',
    idProofType: 'Passport',
    idProofNumber: 'P-77123984',
    connectionType: 'Broadband',
    planId: 'plan-bb-64',
    planName: 'Broadband Unlimited 64 Kbps',
    retailOutletCode: 'SH-04',
    retailEmployeeName: 'Aiden Brooks',
    assignedEmployeeId: 'emp-05',
    assignedBranchName: 'Brooklyn Nexus Connect Depot',
    createdAt: '2026-05-15 11:00',
    status: 'Connection Provided',
    assignedAccountId: 'B081-000000000004',
    feasibilityNotes: 'Initial install verified.',
    cableDistanceMeters: 90,
    dpBoxCapacity: 'DP-BK-11 Port 4',
    signalLossDbm: -15.5,
    bulkConnectionsCount: 1,
    bulkDiscountPercent: 0,
    internetFeasible: true,
  },
  {
    id: 'B0000000008',
    customerName: 'Công ty Cổ phần AlphaTech',
    customerPhone: '+84 912 345 678',
    customerEmail: 'contact@alphatech.vn',
    installationAddress: 'Tòa nhà Keangnam, Cầu Giấy, Hà Nội',
    idProofType: 'National ID Card',
    idProofNumber: 'ID-VN-010999888777',
    connectionType: 'Broadband',
    planId: 'plan-bb-64',
    planName: 'Broadband Unlimited 64 Kbps',
    retailOutletCode: 'SH-01',
    retailEmployeeName: 'David Chen',
    createdAt: '2026-09-14 10:00',
    status: 'Feasible',
    assignedAccountId: 'B064-000000000010',
    feasibilityNotes: 'Fiber termination box within 80m. Signal strength -15.0 dBm OK. Ready for multi-connection provisioning.',
    cableDistanceMeters: 80,
    dpBoxCapacity: 'Port 1 Available / DP-A01',
    signalLossDbm: -15.0,
    bulkConnectionsCount: 3,
    bulkDiscountPercent: 0,
    internetFeasible: true,
  },
  {
    // STAGE 1 rejected by the SH-01 desk: never reaches Technical.
    id: 'T0000000009',
    customerName: 'Margaret Whitfield',
    customerPhone: '+1 (555) 220-7741',
    customerEmail: 'm.whitfield@outlook.com',
    installationAddress: '311 W 116th St, Apt 2C, New York, NY 10026',
    idProofType: 'National ID Card',
    idProofNumber: 'ID-US-4410227',
    connectionType: 'Landline',
    planId: 'plan-ll-std-m',
    planName: 'Landline STD - Monthly',
    retailOutletCode: 'SH-01',
    retailEmployeeName: 'David Chen',
    assignedEmployeeId: 'emp-02',
    assignedBranchName: 'Downtown Nexus Flagship Store',
    retailApprovedBy: 'David Chen',
    retailApprovedAt: '2026-09-12 09:40',
    retailRejectionReason:
      'Card presented is expired; proof of tenure at the installation address not supplied. Customer asked to re-submit with valid ID.',
    createdAt: '2026-09-11 16:20',
    status: 'Not Approved',
    bulkConnectionsCount: 1,
    bulkDiscountPercent: 0,
  },
  {
    // STAGE 2 cleared by the SH-01 desk and now waiting in the Technical queue.
    id: 'B0000000010',
    customerName: 'Bay Ridge Dental Group',
    customerPhone: '+1 (555) 640-2288',
    customerEmail: 'ops@bayridgedental.com',
    installationAddress: '7420 5th Ave, Brooklyn, NY 11209',
    idProofType: "Driver's License",
    idProofNumber: 'DL-NY-7712055',
    connectionType: 'Broadband',
    planId: 'plan-bb-128',
    planName: 'Broadband Unlimited 128 Kbps',
    retailOutletCode: 'SH-01',
    retailEmployeeName: 'David Chen',
    assignedEmployeeId: 'emp-02',
    assignedBranchName: 'Downtown Nexus Flagship Store',
    retailApprovedBy: 'David Chen',
    retailApprovedAt: '2026-09-15 08:25',
    retailApprovalNotes:
      'Doanh nghiệp xuất trình đủ ĐKKD và giấy hẹn mặt bằng. Chuyển kỹ thuật khảo sát.',
    createdAt: '2026-09-15 08:10',
    status: 'Pending',
    bulkConnectionsCount: 6,
    bulkDiscountPercent: 0,
  },
];

const INITIAL_CONNECTIONS: Connection[] = [
  {
    accountId: 'T064-000000000001',
    orderId: 'T0000000003',
    customerName: 'Highline Consulting LLC',
    customerPhone: '+1 (555) 777-8899',
    customerEmail: 'office@highlineconsulting.com',
    installationAddress: '55 Hudson Yards, Fl 18, New York, NY 10001',
    connectionType: 'Landline',
    planName: 'Landline STD - Monthly',
    monthlyRental: 125,
    securityDeposit: 250,
    status: 'Active',
    ipAddress: '198.51.100.42',
    portNumber: 'VOIP-ETH-1',
    assignedDeviceSerial: 'NX-ATA-881920',
    assignedDeviceModel: 'Grandstream HT802 2-Port Analog VoIP Adapter',
    installedDate: '2026-09-02',
    lastUpdated: '2026-09-02 16:30',
  },
  {
    accountId: 'T064-000000000008',
    orderId: 'T0000000003',
    customerName: 'Highline Consulting LLC',
    customerPhone: '+1 (555) 777-8899',
    customerEmail: 'office@highlineconsulting.com',
    installationAddress: '55 Hudson Yards, Fl 18, New York, NY 10001',
    connectionType: 'Landline',
    planName: 'Landline STD - Monthly',
    monthlyRental: 125,
    securityDeposit: 250,
    status: 'Active',
    ipAddress: '198.51.100.43',
    portNumber: 'VOIP-ETH-2',
    assignedDeviceSerial: 'NX-HW-992813',
    assignedDeviceModel: 'Nexus Wi-Fi 6 AX3000 Dual-Band Router',
    installedDate: '2026-09-02',
    lastUpdated: '2026-09-02 16:30',
  },
  {
    accountId: 'B064-000000000002',
    orderId: 'B0000000005',
    customerName: 'Victoria Sterling',
    customerPhone: '+1 (555) 441-2099',
    customerEmail: 'v.sterling@apexlegal.org',
    installationAddress: '120 E 64th St, Manhattan, NY 10065',
    connectionType: 'Broadband',
    planName: 'Broadband Unlimited 64 Kbps',
    monthlyRental: 225,
    securityDeposit: 500,
    status: 'Active',
    ipAddress: '203.0.113.88',
    portNumber: 'GPON-0/1/4',
    assignedDeviceSerial: 'NX-HW-992810',
    assignedDeviceModel: 'Huawei EchoLife HG8245H5 GPON ONT',
    installedDate: '2026-08-15',
    lastUpdated: '2026-08-15 11:20',
  },
  {
    accountId: 'D064-000000000003',
    orderId: 'D0000000006',
    customerName: 'Retro Arcade Lounge LLC',
    customerPhone: '+1 (555) 332-9011',
    customerEmail: 'manager@retroarcadeny.com',
    installationAddress: '31 St Marks pl, East Village, NY 10003',
    connectionType: 'Dial-Up',
    planName: 'Dial-Up Unlimited 56 Kbps',
    monthlyRental: 100,
    securityDeposit: 325,
    status: 'Temporarily Inactive',
    ipAddress: '192.0.2.14',
    portNumber: 'PSTN-LINE-4',
    assignedDeviceSerial: 'NX-MD-110294',
    assignedDeviceModel: 'USRobotics 56K V.92 Faxmodem USB/PSTN',
    installedDate: '2026-07-10',
    lastUpdated: '2026-09-01 09:15',
    lastStatusReason: 'Customer requested seasonal suspension during venue renovation.',
  },
  {
    accountId: 'B081-000000000004',
    orderId: 'B0000000007',
    customerName: 'Jonathan Meyer',
    customerPhone: '+1 (555) 881-2300',
    customerEmail: 'j.meyer@brooklynloft.io',
    installationAddress: '175 Water St, Dumbo, Brooklyn, NY 11201',
    connectionType: 'Broadband',
    planName: 'Broadband Unlimited 64 Kbps',
    monthlyRental: 225,
    securityDeposit: 500,
    status: 'Permanently Inactive',
    assignedDeviceSerial: 'NX-HW-992811',
    assignedDeviceModel: 'Nexus Wi-Fi 6 AX3000 Dual-Band Router',
    installedDate: '2026-05-18',
    lastUpdated: '2026-08-30 17:00',
    lastStatusReason: 'Tenant relocated outside coverage zone; equipment returned and de-provisioned.',
  },
];

const INITIAL_EQUIPMENTS: Equipment[] = [
  {
    id: 'eq-01',
    serialNumber: 'NX-HW-992810',
    macAddress: 'BC:A9:93:21:44:8E',
    deviceModel: 'Huawei EchoLife HG8245H5 GPON ONT',
    deviceType: 'Fiber ONT Modem',
    assignedAccountId: 'B064-000000000002',
    assignedCustomerName: 'Victoria Sterling',
    firmwareVersion: 'V500R019C20SPC120',
    status: 'In Service',
    assignedTechnician: 'Marcus Ramirez',
    installedDate: '2026-08-15',
  },
  {
    id: 'eq-02',
    serialNumber: 'NX-ATA-881920',
    macAddress: '00:0B:82:76:D4:11',
    deviceModel: 'Grandstream HT802 2-Port Analog VoIP Adapter',
    deviceType: 'Analog Telephone Adapter',
    assignedAccountId: 'T064-000000000001',
    assignedCustomerName: 'Highline Consulting LLC',
    firmwareVersion: '1.0.35.3',
    status: 'In Service',
    assignedTechnician: 'Marcus Ramirez',
    installedDate: '2026-09-02',
  },
  {
    id: 'eq-03',
    serialNumber: 'NX-MD-110294',
    macAddress: 'F8:E4:FB:99:A2:03',
    deviceModel: 'USRobotics 56K V.92 Faxmodem USB/PSTN',
    deviceType: 'VDSL2/ADSL Modem',
    assignedAccountId: 'D064-000000000003',
    assignedCustomerName: 'Retro Arcade Lounge LLC',
    firmwareVersion: 'v2.1.8-PSTN',
    status: 'In Service',
    assignedTechnician: 'Marcus Ramirez',
    installedDate: '2026-07-10',
  },
  {
    id: 'eq-04',
    serialNumber: 'NX-HW-992811',
    macAddress: '00:1A:2B:3C:4D:5E',
    deviceModel: 'Nexus Wi-Fi 6 AX3000 Dual-Band Router',
    deviceType: 'Gigabit Router',
    firmwareVersion: 'v3.2.4-BUILD-921',
    status: 'In Stock',
  },
  {
    id: 'eq-05',
    serialNumber: 'NX-HW-992812',
    macAddress: '54:AF:97:88:B1:00',
    deviceModel: 'Huawei EchoLife HG8245H5 GPON ONT',
    deviceType: 'Fiber ONT Modem',
    firmwareVersion: 'V500R019C20SPC120',
    status: 'In Stock',
  },
  {
    id: 'eq-06',
    serialNumber: 'NX-HW-992813',
    macAddress: 'A0:B1:C2:D3:E4:F5',
    deviceModel: 'Nexus Wi-Fi 6 AX3000 Dual-Band Router',
    deviceType: 'Gigabit Router',
    firmwareVersion: 'v3.2.4-BUILD-921',
    status: 'In Service',
    assignedAccountId: 'T064-000000000008',
    assignedCustomerName: 'Highline Consulting LLC',
    assignedTechnician: 'Marcus Ramirez',
    installedDate: '2026-09-02',
  },
  {
    id: 'eq-07',
    serialNumber: 'NX-HW-992814',
    macAddress: '12:34:56:78:9A:BC',
    deviceModel: 'Nexus Wi-Fi 6 AX3000 Dual-Band Router',
    deviceType: 'Gigabit Router',
    firmwareVersion: 'v3.2.4-BUILD-921',
    status: 'In Stock',
  },
  {
    id: 'eq-08',
    serialNumber: 'NX-HW-992815',
    macAddress: '23:45:67:89:AB:CD',
    deviceModel: 'Huawei EchoLife HG8245H5 GPON ONT',
    deviceType: 'Fiber ONT Modem',
    firmwareVersion: 'V500R019C20SPC120',
    status: 'In Stock',
  },
  {
    id: 'eq-09',
    serialNumber: 'NX-HW-992816',
    macAddress: '34:56:78:9A:BC:DE',
    deviceModel: 'Grandstream HT802 2-Port Analog VoIP Adapter',
    deviceType: 'Analog Telephone Adapter',
    firmwareVersion: '1.0.35.3',
    status: 'In Stock',
  },
  {
    id: 'eq-10',
    serialNumber: 'NX-HW-992817',
    macAddress: '45:67:89:AB:CD:EF',
    deviceModel: 'Nexus Wi-Fi 6 AX3000 Dual-Band Router',
    deviceType: 'Gigabit Router',
    firmwareVersion: 'v3.2.4-BUILD-921',
    status: 'In Stock',
  },
];

const INITIAL_BILLS: Bill[] = [
  {
    id: 'bill-01',
    invoiceNumber: 'NEX-INV-2026-001',
    accountId: 'B064-000000000002',
    customerName: 'Victoria Sterling',
    billingMonth: 'August 2026',
    billingDate: '2026-08-15',
    dueDate: '2026-09-05',
    planName: 'Broadband Unlimited 64 Kbps',
    connectionType: 'Broadband',
    securityDeposit: 500,
    monthlyRental: 225,
    hourlyCharges: 0,
    discountPercent: 0,
    discountAmount: 0,
    subtotal: 725, // 500 + 225 - 0
    serviceTaxRate: 12.24,
    serviceTaxAmount: 88.74, // 725 * 0.1224
    totalAmount: 813.74, // 725 + 88.74
    amountPaid: 813.74,
    dueAmount: 0,
    status: 'Paid',
    paymentHistory: [
      {
        paymentId: 'PAY-89201',
        paymentDate: '2026-08-20',
        amountPaid: 813.74,
        paymentMode: 'Credit/Debit Card',
        referenceNumber: 'TXN-VISA-994821',
        recordedBy: 'Elena Rostova',
      },
    ],
  },
  {
    id: 'bill-02',
    invoiceNumber: 'NEX-INV-2026-002',
    accountId: 'T064-000000000001',
    customerName: 'Highline Consulting LLC',
    billingMonth: 'September 2026',
    billingDate: '2026-09-02',
    dueDate: '2026-09-22',
    planName: 'Landline STD - Monthly',
    connectionType: 'Landline',
    securityDeposit: 250,
    monthlyRental: 125,
    hourlyCharges: 0,
    discountPercent: 25, // 12-line corporate order
    discountAmount: 93.75, // 25% of (250 + 125)
    subtotal: 281.25, // 250 + 125 - 93.75
    serviceTaxRate: 12.24,
    serviceTaxAmount: 34.43, // 281.25 * 0.1224
    totalAmount: 315.68, // 281.25 + 34.43
    amountPaid: 100,
    dueAmount: 215.68,
    status: 'Partially Paid',
    paymentHistory: [
      {
        paymentId: 'PAY-89205',
        paymentDate: '2026-09-03',
        amountPaid: 100,
        paymentMode: 'Bank Transfer/NEFT',
        referenceNumber: 'ACH-CITI-449102',
        recordedBy: 'Elena Rostova',
      },
    ],
  },
];

const INITIAL_FEEDBACKS: Feedback[] = [
  {
    id: 'fb-01',
    accountId: 'B064-000000000002',
    orderId: 'B0000000005',
    customerName: 'Victoria Sterling',
    rating: 5,
    category: 'Installation',
    message: 'Field engineer arrived on time and the fibre line was live within an hour. Very smooth.',
    createdAt: '2026-08-16 09:12',
    response: 'Thank you for the kind words — we have shared this with the SH-01 install team.',
    respondedBy: 'Sarah Jenkins',
    respondedAt: '2026-08-16 15:40',
  },
  {
    id: 'fb-02',
    accountId: 'D064-000000000003',
    orderId: 'D0000000006',
    customerName: 'Retro Arcade Lounge LLC',
    rating: 4,
    category: 'Billing',
    message: 'Deposit structure explained well during corporate sign up.',
    createdAt: '2026-09-01 11:05',
  },
];

const INITIAL_SETTINGS: SystemSettings = {
  serviceTaxRate: 12.24, // As explicitly specified: Service Tax (12.24%)
  latePaymentFeePercent: 5.0,
  defaultSecurityDeposits: {
    Broadband: 500,
    'Dial-Up': 325,
    Landline: 250,
  },
  installationGracePeriodDays: 7,
};

// ============ SVELTE STORE ============

function loadFromStorage<T>(key: string, fallback: T): T {
  try {
    const saved = localStorage.getItem(key);
    if (!saved) return fallback;
    const sanitized = saved
      .replace(/â€“/g, '-')
      .replace(/â€”/g, '-')
      .replace(/—/g, '-')
      .replace(/–/g, '-');
    return JSON.parse(sanitized);
  } catch {
    return fallback;
  }
}

function persist(key: string) {
  return <T,>(value: T) => {
    localStorage.setItem(key, JSON.stringify(value));
  };
}

// ============ STORE CREATION ============

function createNexusStore() {
  // Bumped when the seed schema changes so stale localStorage is not reloaded.
  // v7: added technician assignment workflow for feasibility survey
  const V = '_v7';
  const currentRole = writable<RoleType>('admin');
  const dbConnected = writable<boolean>(false);
  const dbInfo = writable<{
    status: string;
    server: string;
    database: string;
    tableCount: number;
    planCount: number;
    orderCount: number;
  } | null>(null);
  const isSyncing = writable<boolean>(false);

  const plans = writable<Plan[]>(loadFromStorage('nexus_plans' + V, INITIAL_PLANS));
  const employees = writable<Employee[]>(loadFromStorage('nexus_employees' + V, INITIAL_EMPLOYEES));
  const vendors = writable<Vendor[]>(loadFromStorage('nexus_vendors' + V, INITIAL_VENDORS));
  const retailShops = writable<RetailShop[]>(loadFromStorage('nexus_retailShops' + V, INITIAL_RETAIL_SHOPS));
  const inventory = writable<InventoryItem[]>(loadFromStorage('nexus_inventory' + V, INITIAL_INVENTORY));
  const orders = writable<Order[]>(loadFromStorage('nexus_orders' + V, INITIAL_ORDERS));
  const connections = writable<Connection[]>(loadFromStorage('nexus_connections' + V, INITIAL_CONNECTIONS));
  const equipments = writable<Equipment[]>(loadFromStorage('nexus_equipments' + V, INITIAL_EQUIPMENTS));
  const bills = writable<Bill[]>(loadFromStorage('nexus_bills' + V, INITIAL_BILLS));
  const feedbacks = writable<Feedback[]>(loadFromStorage('nexus_feedbacks' + V, INITIAL_FEEDBACKS));
  const purchaseOrders = writable<PurchaseOrder[]>([]);
  const equipmentRequests = writable<EquipmentRequest[]>([]);
  const settings = writable<SystemSettings>(loadFromStorage('nexus_settings' + V, INITIAL_SETTINGS));

  // Auto-persist to localStorage on every change (mirrors React useEffect persistence)
  plans.subscribe(persist('nexus_plans' + V));
  employees.subscribe(persist('nexus_employees' + V));
  vendors.subscribe(persist('nexus_vendors' + V));
  retailShops.subscribe(persist('nexus_retailShops' + V));
  inventory.subscribe(persist('nexus_inventory' + V));
  orders.subscribe(persist('nexus_orders' + V));
  connections.subscribe(persist('nexus_connections' + V));
  equipments.subscribe(persist('nexus_equipments' + V));
  bills.subscribe(persist('nexus_bills' + V));
  feedbacks.subscribe(persist('nexus_feedbacks' + V));
  settings.subscribe(persist('nexus_settings' + V));

  // ---- Plan Handlers ----
  // Ghi xuống SQL Server qua /api/admin/serviceplan (role Manager). PUT backend
  // ghi đè toàn bộ trường nên phải gửi đầy đủ, không chỉ các trường thay đổi.
  // Lỗi mạng thì vẫn lưu local để phiên làm việc không mất dữ liệu.
  const planPayload = (p: Partial<Plan>) => ({
    name: p.name ?? '',
    connectionType: p.type ?? 'Broadband',
    speedOrBandwidth: p.speedOrBandwidth ?? '',
    monthlyRental: p.monthlyRental ?? 0,
    securityDeposit: p.securityDeposit ?? 0,
    hourlyCharge: p.hourlyCharge ?? 0,
    billingCycle: p.billingCycle ?? 'Monthly',
    validity: p.validity ?? '',
    dataLimit: p.dataLimit ?? '',
    status: p.status ?? 'Active',
    description: p.description ?? '',
    callRates: p.callRates ?? '',
    includedHours: p.includedHours ?? 0,
    isPopular: p.isPopular ?? false,
  });

  const addPlan = async (plan: Omit<Plan, 'id'>) => {
    let realId = `plan-${Date.now()}`;
    try {
      const created = await createPlanApi(planPayload(plan));
      realId = created.planId || created.id || realId;
    } catch (err) {
      console.warn('[Nexus] API create plan failed, saving locally:', err);
    }
    plans.update((prev) => [{ ...plan, id: realId }, ...prev]);
  };
  const updatePlan = async (id: string, updated: Partial<Plan>) => {
    const merged = { ...get(plans).find((p) => p.id === id), ...updated } as Plan;
    try {
      await updatePlanApi(id, planPayload(merged));
    } catch (err) {
      console.warn('[Nexus] API update plan failed, saving locally:', err);
    }
    plans.update((prev) => prev.map((p) => (p.id === id ? { ...p, ...updated } : p)));
  };
  const deletePlan = async (id: string) => {
    try {
      await deletePlanApi(id);
    } catch (err) {
      console.warn('[Nexus] API delete plan failed, removing locally:', err);
    }
    plans.update((prev) => prev.filter((p) => p.id !== id));
  };

  // ---- Employee Handlers ----
  const addEmployee = async (emp: Omit<Employee, 'id'>, password?: string) => {
    try {
      const created = await createEmployeeApi({
        employeeCode: emp.employeeCode,
        fullName: emp.name,
        email: emp.email,
        phone: emp.phone,
        role: emp.role,
        department: emp.department,
        retailShopAssigned: emp.retailShopAssigned,
        status: emp.status,
        dateOfJoining: emp.dateOfJoining,
        password,
      });
      const newEmp: Employee = {
        ...emp,
        id: created.employeeId || created.id || `emp-${Date.now()}`,
        employeeCode: created.employeeCode || emp.employeeCode,
      };
      employees.update((prev) => [newEmp, ...prev]);
      return newEmp;
    } catch (err) {
      console.warn('[Nexus] API create employee failed, saving locally:', err);
      const fallbackEmp = { ...emp, id: `emp-${Date.now()}` };
      employees.update((prev) => [fallbackEmp, ...prev]);
      return fallbackEmp;
    }
  };

  const updateEmployee = async (id: string, updated: Partial<Employee>, password?: string) => {
    try {
      const current = get(employees).find((e) => e.id === id);
      const merged = { ...current, ...updated };
      await updateEmployeeApi(id, {
        fullName: merged.name || '',
        email: merged.email || '',
        phone: merged.phone,
        role: merged.role || 'Retail Staff',
        department: merged.department || 'Retail Outlets',
        retailShopAssigned: merged.retailShopAssigned,
        status: merged.status || 'Active',
        dateOfJoining: merged.dateOfJoining,
        password,
      });
    } catch (err) {
      console.warn('[Nexus] API update employee failed, saving locally:', err);
    }
    employees.update((prev) => prev.map((e) => (e.id === id ? { ...e, ...updated } : e)));
  };

  const deleteEmployee = (id: string) => {
    deleteEmployeeApi(id).catch((err) =>
      console.warn('[Nexus] API delete employee failed, removing locally:', err)
    );
    employees.update((prev) => prev.filter((e) => e.id !== id));
  };

  // ---- Vendor Handlers ----
  // PUT backend ghi đè toàn bộ trường → gửi đủ payload; rating phải là số nguyên 1-5.
  const vendorPayload = (v: Partial<Vendor>) => ({
    vendorCode: v.vendorCode ?? '',
    companyName: v.companyName ?? '',
    contactPerson: v.contactPerson ?? '',
    category: v.category ?? 'Modems & Routers',
    phone: v.phone ?? '',
    email: v.email ?? '',
    address: v.address ?? '',
    rating: Math.min(5, Math.max(1, Math.round(v.rating ?? 5))),
    status: v.status ?? 'Active',
  });

  const addVendor = async (vendor: Omit<Vendor, 'id'>) => {
    let realId = `vnd-${Date.now()}`;
    try {
      const created = await createVendorApi(vendorPayload(vendor));
      realId = created.vendorId || created.id || realId;
    } catch (err) {
      console.warn('[Nexus] API create vendor failed, saving locally:', err);
    }
    vendors.update((prev) => [{ ...vendor, id: realId }, ...prev]);
  };
  const updateVendor = async (id: string, updated: Partial<Vendor>) => {
    const merged = { ...get(vendors).find((v) => v.id === id), ...updated } as Vendor;
    try {
      await updateVendorApi(id, vendorPayload(merged));
    } catch (err) {
      console.warn('[Nexus] API update vendor failed, saving locally:', err);
    }
    vendors.update((prev) => prev.map((v) => (v.id === id ? { ...v, ...updated } : v)));
  };
  const deleteVendor = async (id: string) => {
    try {
      await deleteVendorApi(id);
    } catch (err) {
      console.warn('[Nexus] API delete vendor failed, removing locally:', err);
    }
    vendors.update((prev) => prev.filter((v) => v.id !== id));
  };

  // ---- Retail Shop Handlers ----
  // PUT backend ghi đè toàn bộ trường → gửi đủ payload (kèm shopCode để giữ mã
  // hiển thị SH-xx qua các lần sửa; shopCode chỉ sống ở FE).
  // FE hiển thị "Under Renovation" nhưng CHECK DB chỉ nhận "Renovating" → map.
  const shopStatusToDb: Record<string, string> = { 'Under Renovation': 'Renovating' };
  const shopPayload = (s: Partial<RetailShop>) => ({
    shopCode: s.shopCode ?? '',
    name: s.name ?? '',
    city: s.city ?? 'New York',
    cityCode: s.cityCode ?? '999',
    address: s.address ?? '',
    managerName: s.managerName ?? '',
    phone: s.phone ?? '',
    operatingHours: s.operatingHours ?? '',
    activeEmployeesCount: s.activeEmployeesCount ?? 0,
    totalSubscribersServed: s.totalSubscribersServed ?? 0,
    status: shopStatusToDb[s.status ?? 'Active'] ?? s.status ?? 'Active',
  });

  const addRetailShop = async (shop: Omit<RetailShop, 'id'>) => {
    let realId = `sh-${Date.now()}`;
    try {
      const created = await createRetailShopApi(shopPayload(shop));
      realId = created.storeId || created.id || realId;
    } catch (err) {
      console.warn('[Nexus] API create retail shop failed, saving locally:', err);
    }
    retailShops.update((prev) => [{ ...shop, id: realId }, ...prev]);
  };
  const updateRetailShop = async (id: string, updated: Partial<RetailShop>) => {
    const merged = { ...get(retailShops).find((s) => s.id === id), ...updated } as RetailShop;
    try {
      await updateRetailShopApi(id, shopPayload(merged));
    } catch (err) {
      console.warn('[Nexus] API update retail shop failed, saving locally:', err);
    }
    retailShops.update((prev) => prev.map((s) => (s.id === id ? { ...s, ...updated } : s)));
  };
  const deleteRetailShop = async (id: string) => {
    try {
      await deleteRetailShopApi(id);
    } catch (err) {
      console.warn('[Nexus] API delete retail shop failed, removing locally:', err);
    }
    retailShops.update((prev) => prev.filter((s) => s.id !== id));
  };

  // ---- Inventory Handlers ----
  const updateInventoryStock = (id: string, delta: number) => {
    inventory.update((prev) =>
      prev.map((item) =>
        item.id === id ? { ...item, stockQuantity: Math.max(0, item.stockQuantity + delta) } : item
      )
    );
  };

  const addInventoryItem = async (item: Omit<InventoryItem, 'id'>) => {
    try {
      const created = await createInventoryItemApi({
        itemCode: item.itemCode,
        name: item.name,
        category: item.category,
        stockQuantity: item.stockQuantity,
        reorderLevel: item.reorderLevel,
        unitCost: item.unitCost,
        location: item.location,
        supplier: item.supplier,
      });
      const newItem: InventoryItem = {
        ...item,
        id: created.inventoryId || created.id || `inv-${Date.now()}`,
        itemCode: created.itemCode || item.itemCode,
      };
      inventory.update((prev) => [newItem, ...prev]);
      return newItem;
    } catch (err) {
      console.warn('[Nexus] API add inventory item failed, saving locally:', err);
      const fallbackItem = { ...item, id: `inv-${Date.now()}` };
      inventory.update((prev) => [fallbackItem, ...prev]);
      return fallbackItem;
    }
  };

  const updateInventoryItem = async (id: string, updated: Partial<InventoryItem>) => {
    try {
      const current = get(inventory).find((i) => i.id === id);
      const merged = { ...current, ...updated };
      await updateInventoryItemApi(id, {
        itemCode: merged.itemCode,
        name: merged.name || '',
        category: merged.category || 'Modem',
        stockQuantity: merged.stockQuantity || 0,
        reorderLevel: merged.reorderLevel || 0,
        unitCost: merged.unitCost || 0,
        location: merged.location,
        supplier: merged.supplier,
      });
    } catch (err) {
      console.warn('[Nexus] API update inventory item failed, saving locally:', err);
    }
    inventory.update((prev) => prev.map((item) => (item.id === id ? { ...item, ...updated } : item)));
  };

  const deleteInventoryItem = (id: string) => {
    deleteInventoryItemApi(id).catch((err) =>
      console.warn('[Nexus] API delete inventory item failed, removing locally:', err)
    );
    inventory.update((prev) => prev.filter((item) => item.id !== id));
  };

  // ---- Order Handlers ----
  type PlaceOrderInput = Omit<
    Order,
    | 'id'
    | 'createdAt'
    | 'status'
    | 'cableDistanceMeters'
    | 'dpBoxCapacity'
    | 'signalLossDbm'
    | 'bulkConnectionsCount'
    | 'bulkDiscountPercent'
    | 'landlineFeasible'
    | 'internetFeasible'
  > & { bulkConnectionsCount?: number };

  // ---- Two-stage approval helpers ----
  // Timestamp in the same "YYYY-MM-DD HH:mm" shape used by the rest of the store.
  const nowStamp = (): string => {
    const now = new Date();
    return `${now.toISOString().slice(0, 10)} ${now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
  };

  // Resolves the Retail Staff member assigned to a branch so a freshly submitted
  // application lands in that specific person's queue (not in a global pool).
  // Matching is done on the shop code that appears inside Employee.retailShopAssigned
  // (e.g. 'Downtown Flagship (SH-01)'), falling back to the branch's own manager.
  const routeOrderToBranch = (
    shopCode: string,
    fallbackEmployeeName?: string
  ): { assignedEmployeeId?: string; assignedBranchName?: string; retailEmployeeName: string } => {
    const shop = get(retailShops).find((s) => s.shopCode === shopCode);
    const branchName = shop?.name ?? shopCode;

    const staff = get(employees).find(
      (e) =>
        e.role === 'Retail Staff' &&
        e.status === 'Active' &&
        !!e.retailShopAssigned &&
        e.retailShopAssigned.toUpperCase().includes(`(${shopCode.toUpperCase()})`)
    );

    if (staff) {
      return {
        assignedEmployeeId: staff.id,
        assignedBranchName: branchName,
        retailEmployeeName: staff.name,
      };
    }

    // No staff record for the branch: still route by branch code and keep whatever
    // name the caller supplied (e.g. 'Online Self-Service' for customer sign-ups).
    return {
      assignedBranchName: branchName,
      retailEmployeeName: fallbackEmployeeName ?? 'Unassigned Retail Desk',
    };
  };

  // Ghi đơn vào CSDL rồi mới trả về. Trước đây hàm này gọi API kiểu "bắn rồi quên":
  // API trả 401/400/500 thì đơn chỉ nằm trong bộ nhớ trình duyệt, trang đăng ký vẫn báo
  // thành công, còn CSDL và màn hình Kỹ thuật không hề có đơn đó. Nay lỗi được ném ra
  // để trang gọi hiện thông báo, và mã đơn lấy đúng mã do server sinh.
  // options.selfService: đơn từ trang đăng ký công khai — đi qua /api/public/orders nên
  // khách cũ dùng lại email vẫn đặt thêm được mà không cần đăng nhập.
  const placeOrder = async (
    orderData: PlaceOrderInput,
    options: { selfService?: boolean } = {}
  ): Promise<Order> => {
    const now = new Date();
    const dateStr = now.toISOString().slice(0, 10);
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    const bulkConnectionsCount = Math.max(1, Math.floor(orderData.bulkConnectionsCount ?? 1));

    // STAGE 1 routing: pin the application to the retail staff of the branch the
    // customer chose, so it never appears in another outlet's (or Technical's) queue.
    const routing = routeOrderToBranch(
      orderData.retailOutletCode,
      orderData.retailEmployeeName
    );

    const created = await createOrderApi({
      customerName: orderData.customerName,
      customerPhone: orderData.customerPhone,
      // Không có email thì gửi hẳn undefined, để backend không so khớp một chuỗi rỗng.
      customerEmail: orderData.customerEmail?.trim() || undefined,
      installationAddress: orderData.installationAddress,
      idProofType: orderData.idProofType,
      idProofNumber: orderData.idProofNumber,
      connectionType: orderData.connectionType,
      planId: orderData.planId,
      planName: orderData.planName,
      retailOutletCode: orderData.retailOutletCode,
      retailEmployeeName: routing.retailEmployeeName,
      assignedEmployeeId: routing.assignedEmployeeId,
      assignedBranchName: routing.assignedBranchName,
      status: 'PendingRetail',
      bulkConnectionsCount,
      bulkDiscountPercent: getBulkDiscountPercent(bulkConnectionsCount),
      // Dial-Up: khách khai đang giữ landline Nexus → khảo sát chỉ cần lớp
      // internet (miễn landline) — spec mục feasibility.
      existingLandlineAccountId: orderData.existingLandlineAccountId || undefined,
    }, options);

    const newOrder: Order = {
      ...orderData,
      ...routing,
      bulkConnectionsCount,
      bulkDiscountPercent: getBulkDiscountPercent(bulkConnectionsCount),
      depositWaived: created.depositWaived ?? isDepositWaived(bulkConnectionsCount),
      id: created.orderId,
      status: (created.status as Order['status']) ?? 'PendingRetail',
      createdAt: `${dateStr} ${timeStr}`,
    };

    orders.update((prev) => [newOrder, ...prev.filter((o) => o.id !== newOrder.id)]);
    return newOrder;
  };

  // STAGE 2a — a branch's Retail Staff approves the paperwork. The order leaves the
  // retail queue and enters the Technical feasibility queue as 'Pending'.
  const approveOrderByRetail = (
    orderId: string,
    approvedBy: string,
    notes?: string
  ): Order | null => {
    let updatedOrder: Order | null = null;
    const stamp = nowStamp();

    orders.update((prev) =>
      prev.map((ord) => {
        if (ord.id !== orderId) return ord;
        if (ord.status !== 'PendingRetail') return ord;

        updatedOrder = {
          ...ord,
          status: 'Pending',
          retailApprovedBy: approvedBy,
          retailApprovedAt: stamp,
          retailApprovalNotes: notes?.trim() || undefined,
          retailRejectionReason: undefined,
        };
        return updatedOrder;
      })
    );

    if (updatedOrder) {
      updateOrderStatusApi(orderId, {
        status: 'Pending',
        retailApprovedBy: approvedBy,
        retailApprovalNotes: notes?.trim() || undefined,
      }).catch((err) => console.warn('[Nexus] Backend order approval fallback:', err));
    }

    return updatedOrder;
  };

  // STAGE 2b — the Retail Staff rejects the application (wrong branch, bad ID proof,
  // incomplete paperwork...). It never reaches Technical and carries a reason.
  const rejectOrderByRetail = (
    orderId: string,
    rejectedBy: string,
    reason: string
  ): Order | null => {
    let updatedOrder: Order | null = null;
    const stamp = nowStamp();

    orders.update((prev) =>
      prev.map((ord) => {
        if (ord.id !== orderId) return ord;
        if (ord.status !== 'PendingRetail') return ord;

        updatedOrder = {
          ...ord,
          status: 'Not Approved',
          retailApprovedBy: rejectedBy,
          retailApprovedAt: stamp,
          retailRejectionReason: reason.trim(),
        };
        return updatedOrder;
      })
    );

    if (updatedOrder) {
      updateOrderStatusApi(orderId, {
        status: 'Not Approved',
        retailApprovedBy: rejectedBy,
        retailRejectionReason: reason.trim(),
      }).catch((err) => console.warn('[Nexus] Backend order rejection fallback:', err));
    }

    return updatedOrder;
  };

  // Lets a rejected application be edited/resubmitted by the same branch desk.
  const resubmitOrderToRetail = (orderId: string): Order | null => {
    let updatedOrder: Order | null = null;

    orders.update((prev) =>
      prev.map((ord) => {
        if (ord.id !== orderId) return ord;
        if (ord.status !== 'Not Approved') return ord;

        updatedOrder = {
          ...ord,
          status: 'PendingRetail',
          retailApprovedBy: undefined,
          retailApprovedAt: undefined,
          retailRejectionReason: undefined,
        };
        return updatedOrder;
      })
    );

    if (updatedOrder) {
      updateOrderStatusApi(orderId, {
        status: 'PendingRetail',
      }).catch((err) => console.warn('[Nexus] Backend order resubmit fallback:', err));
    }

    return updatedOrder;
  };

  // Guard for the Technical stage: an order must be cleared by its branch's retail
  // staff before a Field Engineer may assess it.
  const canTechnicalAssess = (ord: Order): boolean => isReleasedToTechnical(ord.status);

  // Account ID serial: next strictly increasing number across all connections and orders
  const nextAccountIdSerial = (): number => {
    let maxSerial = 0;
    const checkSerial = (rawId: string | undefined) => {
      if (!rawId) return;
      const clean = accountIdKey(rawId);
      // Format: [type(1)][city(3)][serial(12)] -> last 12 digits
      if (clean.length >= 16) {
        const serialNum = parseInt(clean.slice(4), 10);
        if (!isNaN(serialNum) && serialNum > maxSerial) {
          maxSerial = serialNum;
        }
      }
    };
    get(connections).forEach((c) => checkSerial(c.accountId));
    get(orders).forEach((o) => checkSerial(o.assignedAccountId));
    return Math.max(get(connections).length, maxSerial) + 1;
  };

  const cityCodeForOrder = (ord: Order): string => {
    const shop = get(retailShops).find((s) => s.shopCode === ord.retailOutletCode);
    return shop?.cityCode ?? '064';
  };

  const updateOrderStatus = (
    orderId: string,
    status: OrderStatus,
    feasibilityNotes?: string,
    cableDistanceMeters?: number,
    dpBoxCapacity?: string,
    signalLossDbm?: number,
    // Dial-Up: which of the two feasibility legs passed. Omit for non-Dial-Up.
    feasibilityLegs?: { landline?: boolean; internet?: boolean }
  ): Order | null => {
    let updatedOrder: Order | null = null;

    // STAGE GUARD: a Field Engineer may only move orders that the branch's retail
    // staff already cleared. Anything still sitting in the retail queue
    // ('PendingRetail' / 'Not Approved') is refused here as a second line of defence,
    // even if a caller bypasses the UI.
    const gate = get(orders).find((o) => o.id === orderId);
    if (gate && !isReleasedToTechnical(gate.status)) {
      return null;
    }

    orders.update((prev) =>
      prev.map((ord) => {
        if (ord.id === orderId) {
          // The 16-char Account ID is issued the moment technical confirms the
          // line is feasible — it is the customer's only sign-in credential.
          const assignedAccountId =
            status === 'Feasible' && !ord.assignedAccountId
              ? generateAccountId(ord.connectionType, cityCodeForOrder(ord), nextAccountIdSerial())
              : ord.assignedAccountId;

          // Dial-Up needs BOTH legs; if the customer already holds a Nexus
          // landline, the landline leg is considered satisfied.
          const landlineFeasible =
            feasibilityLegs?.landline ??
            (ord.existingLandlineAccountId ? true : ord.landlineFeasible);
          const internetFeasible = feasibilityLegs?.internet ?? ord.internetFeasible;

          updatedOrder = {
            ...ord,
            status,
            ...(assignedAccountId !== undefined && { assignedAccountId }),
            ...(feasibilityNotes !== undefined && { feasibilityNotes }),
            ...(cableDistanceMeters !== undefined && { cableDistanceMeters }),
            ...(dpBoxCapacity !== undefined && { dpBoxCapacity }),
            ...(signalLossDbm !== undefined && { signalLossDbm }),
            ...(ord.connectionType === 'Dial-Up' && { landlineFeasible, internetFeasible }),
            ...(ord.connectionType !== 'Dial-Up' &&
              status === 'Feasible' && { internetFeasible: true }),
            ...(ord.connectionType !== 'Dial-Up' &&
              status === 'Not Feasible' && { internetFeasible: false }),
          };
          return updatedOrder;
        }
        return ord;
      })
    );

    if (updatedOrder) {
      updateOrderStatusApi(orderId, {
        status,
        reason: feasibilityNotes,
        assignedAccountId: (updatedOrder as Order).assignedAccountId,
        cableDistanceMeters,
        dpBoxCapacity,
        signalLossDbm,
      }).catch((err) => console.warn('[Nexus] Backend order status update fallback:', err));
    }

    return updatedOrder;
  };

  // ---- Assign Technician for Feasibility Survey ----
  const assignTechnicianToOrder = (
    orderId: string,
    technicianName: string,
    technicianId?: string,
    technicianPhone?: string,
    surveyNotes?: string
  ): Order | null => {
    let updatedOrder: Order | null = null;
    const now = new Date();
    const dateStr = now.toISOString().slice(0, 10);
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    const assignedStamp = `${dateStr} ${timeStr}`;

    // Hồ sơ phải được chi nhánh duyệt xong mới được phân công kỹ thuật viên.
    const gate = get(orders).find((o) => o.id === orderId);
    if (!gate || !isReleasedToTechnical(gate.status)) {
      return null;
    }

    orders.update((prev) =>
      prev.map((ord) => {
        if (ord.id === orderId) {
          const notes = surveyNotes?.trim();
          const combinedNotes = notes
            ? ord.feasibilityNotes
              ? `${ord.feasibilityNotes} | ${notes}`
              : notes
            : ord.feasibilityNotes;

          updatedOrder = {
            ...ord,
            assignedTechnician: technicianName,
            assignedTechnicianId: technicianId,
            assignedTechnicianPhone: technicianPhone,
            assignedTechnicianDate: assignedStamp,
            feasibilityNotes: combinedNotes,
          };
          return updatedOrder;
        }
        return ord;
      })
    );

    if (updatedOrder) {
      updateOrderStatusApi(orderId, {
        status: (updatedOrder as Order).status,
        assignedTechnician: technicianName,
        assignedTechnicianId: technicianId,
        assignedTechnicianPhone: technicianPhone,
      }).catch((err) => console.warn('[Nexus] Backend technician assignment fallback:', err));
    }

    return updatedOrder;
  };

  // ---- Connection Provisioning ----
  const provisionConnectionForOrder = (orderId: string, assignedDeviceSerial: string): Connection | null => {
    const order = get(orders).find((o) => o.id === orderId);
    if (!order) return null;

    const plan = get(plans).find((p) => p.id === order.planId);
    const monthlyRate = plan ? plan.monthlyRental : 100;
    const deposit = plan ? plan.securityDeposit : 250;

    const existingForOrder = get(connections).filter((c) => c.orderId === orderId);
    const nextIndex = existingForOrder.length + 1;
    const totalRequired = Math.max(1, order.bulkConnectionsCount || 1);

    // If order.assignedAccountId exists and hasn't been taken by an existing connection yet, use it for Line #1;
    // otherwise generate a brand new unique Account ID for each subsequent connection.
    const isFirstIdAvailable =
      order.assignedAccountId &&
      !get(connections).some((c) => c.accountId === order.assignedAccountId);

    const newAccountId = isFirstIdAvailable
      ? order.assignedAccountId!
      : generateAccountId(order.connectionType, cityCodeForOrder(order), nextAccountIdSerial());

    const now = new Date();
    const dateStr = now.toISOString().slice(0, 10);
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    const device = get(equipments).find((eq) => eq.serialNumber === assignedDeviceSerial);

    const newConnection: Connection = {
      accountId: newAccountId,
      orderId: order.id,
      customerName: order.customerName,
      customerPhone: order.customerPhone,
      customerEmail: order.customerEmail,
      installationAddress: order.installationAddress,
      connectionType: order.connectionType,
      planName: order.planName,
      monthlyRental: monthlyRate,
      securityDeposit: deposit,
      status: 'Active',
      ipAddress: `198.51.${Math.floor(10 + Math.random() * 90)}.${Math.floor(2 + Math.random() * 250)}`,
      portNumber: `ETH-PORT-${nextIndex}`,
      assignedDeviceSerial,
      assignedDeviceModel: device?.deviceModel || 'Nexus Standard CPE',
      installedDate: dateStr,
      lastUpdated: `${dateStr} ${timeStr}`,
    };

    // Mark order as fulfilled only when ALL required bulk connections have been provisioned!
    const isAllFulfilled = nextIndex >= totalRequired;

    orders.update((prev) =>
      prev.map((o) =>
        o.id === orderId
          ? {
              ...o,
              status: isAllFulfilled ? 'Connection Provided' : o.status,
              assignedAccountId: o.assignedAccountId || newAccountId,
            }
          : o
      )
    );

    connections.update((prev) => [newConnection, ...prev]);

    // Persist provisioned connection to SQL Server DB
    createOrUpdateConnectionApi({
      accountId: newConnection.accountId,
      orderId: newConnection.orderId,
      customerId: (order as any).customerId || 'CUST-0001',
      customerName: newConnection.customerName,
      customerPhone: newConnection.customerPhone,
      customerEmail: newConnection.customerEmail,
      installationAddress: newConnection.installationAddress,
      planName: newConnection.planName,
      connectionType: newConnection.connectionType,
      monthlyRental: newConnection.monthlyRental,
      securityDeposit: newConnection.securityDeposit,
      status: newConnection.status,
      ipAddress: newConnection.ipAddress,
      portNumber: newConnection.portNumber,
      assignedDeviceSerial: newConnection.assignedDeviceSerial,
      assignedDeviceModel: newConnection.assignedDeviceModel,
      installedDate: newConnection.installedDate,
    }).catch((err) => console.warn('[NexusContext] Failed to persist connection to DB:', err));

    // Bind equipment to this specific connection (1 connection = 1 router)
    if (assignedDeviceSerial) {
      equipments.update((prev) =>
        prev.map((eq) =>
          eq.serialNumber === assignedDeviceSerial
            ? {
                ...eq,
                status: 'In Service',
                assignedAccountId: newAccountId,
                assignedCustomerName: totalRequired > 1
                  ? `${order.customerName} (Line #${nextIndex})`
                  : order.customerName,
                installedDate: dateStr,
              }
            : eq
        )
      );
    }

    return newConnection;
  };

  // ---- Connection Handlers ----
  const updateConnectionStatus = (accountId: string, status: ConnectionStatus, reason?: string) => {
    const now = new Date();
    const stamp = `${now.toISOString().slice(0, 10)} ${now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
    connections.update((prev) =>
      prev.map((conn) => {
        if (conn.accountId === accountId) {
          const updated = {
            ...conn,
            status,
            lastUpdated: stamp,
            lastStatusReason: reason || conn.lastStatusReason,
          };
          // Persist status change to SQL Server DB
          createOrUpdateConnectionApi({
            accountId: updated.accountId,
            orderId: updated.orderId,
            customerName: updated.customerName,
            customerPhone: updated.customerPhone,
            customerEmail: updated.customerEmail,
            installationAddress: updated.installationAddress,
            planName: updated.planName,
            connectionType: updated.connectionType,
            monthlyRental: updated.monthlyRental,
            securityDeposit: updated.securityDeposit,
            status: updated.status,
            ipAddress: updated.ipAddress,
            portNumber: updated.portNumber,
            assignedDeviceSerial: updated.assignedDeviceSerial,
            assignedDeviceModel: updated.assignedDeviceModel,
            installedDate: updated.installedDate,
            lastStatusReason: updated.lastStatusReason,
          }).catch((err) => console.warn('[NexusContext] Failed to update connection in DB:', err));
          return updated;
        }
        return conn;
      })
    );
  };

  // ---- Equipment Handlers ----
  const addEquipment = async (eq: Omit<Equipment, 'id'>) => {
    try {
      const created = await createEquipmentApi({
        serialNumber: eq.serialNumber,
        macAddress: eq.macAddress,
        deviceModel: eq.deviceModel,
        deviceType: eq.deviceType,
        firmwareVersion: eq.firmwareVersion,
        status: eq.status || 'In Stock',
      });
      const newEq: Equipment = {
        ...eq,
        id: created.equipmentId || `eq-${Date.now()}`,
      };
      equipments.update((prev) => [newEq, ...prev]);
      return newEq;
    } catch (err) {
      console.warn('[Nexus] API add equipment failed, saving locally:', err);
      const fallbackEq = { ...eq, id: `eq-${Date.now()}` };
      equipments.update((prev) => [fallbackEq, ...prev]);
      return fallbackEq;
    }
  };

  const updateEquipment = (id: string, updated: Partial<Equipment>) => {
    equipments.update((prev) => prev.map((eq) => (eq.id === id ? { ...eq, ...updated } : eq)));
  };

  // ---- Billing Handlers ----
  const generateBill = (
    accountId: string,
    securityDeposit: number,
    monthlyRental: number,
    hourlyCharges: number,
    billingMonth: string,
    // Bulk / corporate scheme discount on (securityDeposit + monthlyRental).
    discountPercent = 0
  ): Bill => {
    const conn = get(connections).find((c) => accountIdKey(c.accountId) === accountIdKey(accountId));
    const customerName = conn ? conn.customerName : 'Nexus Valued Subscriber';
    const planName = conn ? conn.planName : 'Telecommunications Plan';
    const connectionType = conn ? conn.connectionType : 'Broadband';

    const pct = Math.min(100, Math.max(0, discountPercent || 0));
    const discountAmount = Number((((securityDeposit + monthlyRental) * pct) / 100).toFixed(2));
    const subtotal = Number(
      (securityDeposit + monthlyRental + hourlyCharges - discountAmount).toFixed(2)
    );
    const taxRate = get(settings).serviceTaxRate; // 12.24%
    const serviceTaxAmount = Number(((subtotal * taxRate) / 100).toFixed(2));
    const totalAmount = Number((subtotal + serviceTaxAmount).toFixed(2));

    const currentBills = get(bills);
    const invoiceNum = `NEX-INV-${new Date().getFullYear()}-${String(currentBills.length + 1).padStart(3, '0')}`;
    const today = new Date().toISOString().slice(0, 10);
    const due = new Date(Date.now() + 20 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

    const newBill: Bill = {
      id: `bill-${Date.now()}`,
      invoiceNumber: invoiceNum,
      accountId,
      customerName,
      billingMonth,
      billingDate: today,
      dueDate: due,
      planName,
      connectionType,
      securityDeposit,
      monthlyRental,
      hourlyCharges,
      discountPercent: pct,
      discountAmount,
      subtotal,
      serviceTaxRate: taxRate,
      serviceTaxAmount,
      totalAmount,
      amountPaid: 0,
      dueAmount: totalAmount,
      status: 'Unpaid',
      paymentHistory: [],
    };

    bills.update((prev) => [newBill, ...prev]);

    // Persist new bill to SQL Server database
    createBillApi({
      accountId,
      billingMonth,
      securityDeposit,
      monthlyRental,
      hourlyCharges,
      discountPercent: pct,
      discountAmount,
      subtotal,
      serviceTaxRate: taxRate,
      serviceTaxAmount,
      totalAmount,
      invoiceNumber: invoiceNum,
    }).then((created) => {
      console.log('[NexusContext] Bill persisted to SQL Server database:', created);
      if (created && created.invoiceNumber && created.invoiceNumber !== invoiceNum) {
        bills.update((prev) =>
          prev.map((b) => (b.invoiceNumber === invoiceNum ? { ...b, invoiceNumber: created.invoiceNumber, id: created.billId || b.id } : b))
        );
      }
    }).catch((err) => {
      console.warn('[NexusContext] Failed to persist bill to SQL Server:', err);
    });

    return newBill;
  };

  // Role của người dùng hiện tại ('admin' | 'retail' | 'technical' | 'accounts' | 'user')
  // lấy từ bản currentUser mà AuthContext đồng bộ ra localStorage. Trả về null nếu
  // chưa đăng nhập hoặc JSON hỏng.
  const readStoredUserRole = (): RoleType | null => {
    try {
      const raw = localStorage.getItem('nexus_auth_user');
      return raw ? (JSON.parse(raw).role ?? null) : null;
    } catch {
      return null;
    }
  };

  const recordPayment = (
    invoiceNumber: string,
    amountPaid: number,
    paymentMode: PaymentRecord['paymentMode'],
    referenceNumber: string,
    recordedBy: string
  ): Bill | null => {
    let updatedBill: Bill | null = null;

    bills.update((prev) =>
      prev.map((bill) => {
        if (bill.invoiceNumber === invoiceNumber) {
          const newTotalPaid = Number((bill.amountPaid + amountPaid).toFixed(2));
          const newDue = Number(Math.max(0, bill.totalAmount - newTotalPaid).toFixed(2));
          const status: Bill['status'] = newDue <= 0.01 ? 'Paid' : newTotalPaid > 0 ? 'Partially Paid' : 'Unpaid';

          const newPayment: PaymentRecord = {
            paymentId: `PAY-${Date.now().toString().slice(-5)}`,
            paymentDate: new Date().toISOString().slice(0, 10),
            amountPaid,
            paymentMode,
            referenceNumber,
            recordedBy,
          };

          updatedBill = {
            ...bill,
            amountPaid: newTotalPaid,
            dueAmount: newDue,
            status,
            paymentHistory: [newPayment, ...bill.paymentHistory],
          };
          return updatedBill;
        }
        return bill;
      })
    );

    // Persist payment to SQL Server database. Endpoint theo role của người ghi:
    // - accounts (Senior Accountant): thu tại văn phòng → /api/billing/bills/payments.
    // - retail (Retail Staff) và user (khách tự trả online) → /api/retail/payments,
    //   endpoint riêng của cửa hàng bán lẻ (RetailPaymentsController) vì /api/billing
    //   đã bị khoá [Authorize(Roles = "Senior Accountant")] — gọi vào là 403.
    // NexusContext không import được AuthContext (AuthContext đã import nexusStore)
    // nên đọc role hiện tại từ bản lưu trong localStorage.
    if (updatedBill) {
      const persistApi =
        readStoredUserRole() === 'accounts' ? recordPaymentApi : recordRetailPaymentApi;
      persistApi({
        invoiceNumber,
        amountPaid,
        paymentMode,
        referenceNumber,
        recordedBy,
        notes: `Recorded by ${recordedBy}`,
      }).then((res) => {
        console.log('[NexusContext] Payment persisted to SQL Server database:', res);
      }).catch((err) => {
        console.warn('[NexusContext] Failed to persist payment to SQL Server:', err);
      });
    }

    // SPEC: "Chỉ postpaid: bill được sinh ra, và trạng thái kết nối phụ thuộc vào bill"
    // When bill is settled in full, automatically restore any Temporarily Inactive connection to Active.
    if (updatedBill && (updatedBill as Bill).status === 'Paid') {
      const targetAcc = (updatedBill as Bill).accountId;
      const now = new Date();
      const stamp = `${now.toISOString().slice(0, 10)} ${now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
      connections.update((prev) =>
        prev.map((conn) =>
          accountIdKey(conn.accountId) === accountIdKey(targetAcc) && conn.status === 'Temporarily Inactive'
            ? {
                ...conn,
                status: 'Active' as ConnectionStatus,
                lastUpdated: stamp,
                lastStatusReason: `Công nợ hóa đơn ${(updatedBill as Bill).invoiceNumber} đã được thanh toán toàn bộ — Tự động kích hoạt lại đường truyền`,
              }
            : conn
        )
      );
    }

    return updatedBill;
  };

  // ---- Update Bill (Accounts department, spec: "updating has to be done by the
  // accounts department alone") — PUT /api/billing/bills/{id}. Trường nào undefined
  // thì backend giữ nguyên; server luôn tính lại subtotal/thuế/total/công nợ/trạng
  // thái, FE cập nhật store từ response để UI khớp số liệu CSDL ngay lập tức.
  const updateBill = async (
    invoiceNumber: string,
    updates: {
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
  ): Promise<Bill | null> => {
    const updated = await updateBillApi(invoiceNumber, updates);
    const r = updated || {};
    let saved: Bill | null = null;
    bills.update((prev) =>
      prev.map((bill): Bill => {
        if (bill.invoiceNumber !== invoiceNumber) return bill;
        saved = {
          ...bill,
          billingMonth: r.billingMonth ?? bill.billingMonth,
          dueDate: r.dueDate ? String(r.dueDate).slice(0, 10) : bill.dueDate,
          securityDeposit: r.securityDeposit ?? bill.securityDeposit,
          monthlyRental: r.monthlyRental ?? bill.monthlyRental,
          hourlyCharges: r.hourlyCharges ?? bill.hourlyCharges,
          discountPercent: r.discountPercent ?? bill.discountPercent,
          discountAmount: r.discountAmount ?? bill.discountAmount,
          subtotal: r.subtotal ?? bill.subtotal,
          serviceTaxRate: r.serviceTaxRate ?? bill.serviceTaxRate,
          serviceTaxAmount: r.serviceTaxAmount ?? bill.serviceTaxAmount,
          totalAmount: r.totalAmount ?? bill.totalAmount,
          amountPaid: r.amountPaid ?? bill.amountPaid,
          dueAmount: r.dueAmount ?? bill.dueAmount,
          status: r.status ?? bill.status,
          customerName: r.customerName ?? bill.customerName,
          planName: r.planName ?? bill.planName,
          paymentHistory: (r.payments || []).length
            ? r.payments.map((p: any) => ({
                paymentId: p.paymentId,
                paymentDate: p.paymentDate ? String(p.paymentDate).slice(0, 10) : '',
                amountPaid: p.amountPaid,
                paymentMode: p.paymentMode,
                referenceNumber: p.referenceNumber || '',
                recordedBy: p.recordedBy || 'Accounts Cashier',
              }))
            : bill.paymentHistory,
        };
        return saved;
      })
    );
    return saved;
  };

  // ---- Purchase Order Handlers (Admin — purchase list theo spec) ----
  // Nạp lại tồn kho từ CSDL sau khi đơn mua Received (backend đã cộng số lượng).
  const refreshInventory = async () => {
    try {
      const res = await fetch(`${API_BASE_URL}/api/admin/inventory`, { headers: authHeaders() });
      if (!res.ok) return;
      const list = await res.json();
      if (Array.isArray(list) && list.length > 0) {
        inventory.set(list.map((i: any) => ({
          id: i.inventoryId,
          itemCode: i.itemCode,
          name: i.name,
          category: i.category,
          stockQuantity: i.stockQuantity ?? 0,
          reorderLevel: i.reorderLevel ?? 10,
          unitCost: i.unitCost ?? 0,
          location: i.location || '',
          vendorId: i.vendorId || '',
          supplier: i.vendorName || i.supplier || '',
          status: i.status || 'In Stock',
        })));
      }
    } catch (err) {
      console.warn('[Nexus] Cannot refresh inventory:', err);
    }
  };

  const mapPurchaseOrder = (p: any): PurchaseOrder => ({
    id: p.purchaseOrderId || p.id,
    vendorId: p.vendorId,
    vendorName: p.vendor?.companyName || p.vendorName || undefined,
    storeId: p.storeId || undefined,
    itemCode: p.itemCode,
    itemName: p.itemName,
    category: p.category,
    quantity: p.quantity,
    unitCost: p.unitCost,
    totalCost: p.totalCost,
    orderDate: p.orderDate ? String(p.orderDate).slice(0, 10) : '',
    expectedDate: p.expectedDate ? String(p.expectedDate).slice(0, 10) : undefined,
    receivedDate: p.receivedDate ? String(p.receivedDate).slice(0, 10) : undefined,
    status: p.status || 'Submitted',
    notes: p.notes || undefined,
    createdBy: p.createdBy || undefined,
  });

  const refreshPurchaseOrders = async () => {
    try {
      const list = await fetchPurchaseOrdersApi();
      purchaseOrders.set((list || []).map(mapPurchaseOrder));
    } catch (err) {
      console.warn('[Nexus] Cannot load purchase orders:', err);
    }
  };

  const addPurchaseOrder = async (
    order: Omit<PurchaseOrder, 'id' | 'totalCost' | 'status' | 'orderDate'>
  ): Promise<PurchaseOrder | null> => {
    try {
      const created = await createPurchaseOrderApi({
        vendorId: order.vendorId,
        storeId: order.storeId,
        itemCode: order.itemCode,
        itemName: order.itemName,
        category: order.category,
        quantity: order.quantity,
        unitCost: order.unitCost,
        expectedDate: order.expectedDate,
        notes: order.notes,
      });
      const mapped = mapPurchaseOrder(created);
      purchaseOrders.update((prev) => [mapped, ...prev]);
      return mapped;
    } catch (err) {
      console.warn('[Nexus] API create purchase order failed:', err);
      throw err;
    }
  };

  const setPurchaseOrderStatus = async (id: string, status: PurchaseOrder['status']) => {
    const updated = await updatePurchaseOrderStatusApi(id, status);
    const mapped = mapPurchaseOrder(updated);
    purchaseOrders.update((prev) => prev.map((p) => (p.id === id ? mapped : p)));
    // Đơn Received → kho được backend cộng tự động; tải lại tồn kho để UI khớp.
    if (status === 'Received') void refreshInventory();
    return mapped;
  };

  const deletePurchaseOrder = async (id: string) => {
    await deletePurchaseOrderApi(id);
    purchaseOrders.update((prev) => prev.filter((p) => p.id !== id));
  };

  // ---- Equipment Request Handlers (Yêu cầu nhập hàng từ KTV gửi Admin) ----
  const mapEquipmentRequest = (r: any): EquipmentRequest => ({
    id: r.requestId || r.id,
    orderId: r.orderId || undefined,
    storeId: r.storeId,
    storeName: r.storeName || r.store?.name || undefined,
    employeeId: r.employeeId,
    employeeName: r.employeeName || r.employee?.fullName || undefined,
    inventoryId: r.inventoryId || undefined,
    itemName: r.itemName,
    deviceType: r.deviceType,
    quantity: r.quantity || 1,
    urgency: r.urgency || 'Normal',
    status: r.status || 'Pending',
    reason: r.reason || undefined,
    adminNotes: r.adminNotes || undefined,
    createdAt: r.createdAt ? String(r.createdAt).slice(0, 19).replace('T', ' ') : '',
    updatedAt: r.updatedAt ? String(r.updatedAt).slice(0, 19).replace('T', ' ') : undefined,
  });

  const refreshEquipmentRequests = async (filters?: { storeId?: string; status?: string }) => {
    try {
      const list = await fetchEquipmentRequestsApi(filters);
      equipmentRequests.set((list || []).map(mapEquipmentRequest));
    } catch (err) {
      console.warn('[Nexus] Cannot load equipment requests:', err);
    }
  };

  const addEquipmentRequest = async (
    data: {
      orderId?: string | null;
      storeId: string;
      inventoryId?: string | null;
      itemName: string;
      deviceType: string;
      quantity: number;
      urgency?: string;
      reason?: string | null;
    }
  ): Promise<EquipmentRequest> => {
    try {
      const created = await createEquipmentRequestApi(data);
      const mapped = mapEquipmentRequest(created);
      equipmentRequests.update((prev) => [mapped, ...prev]);
      return mapped;
    } catch (err) {
      console.warn('[Nexus] API create equipment request failed:', err);
      throw err;
    }
  };

  const setEquipmentRequestStatus = async (
    id: string,
    status: EquipmentRequest['status'],
    adminNotes?: string
  ): Promise<EquipmentRequest> => {
    const updated = await updateEquipmentRequestStatusApi(id, status, adminNotes);
    const mapped = mapEquipmentRequest(updated);
    equipmentRequests.update((prev) => prev.map((r) => (r.id === id ? mapped : r)));
    if (status === 'Fulfilled') {
      void refreshInventory();
      void syncWithDatabase();
    }
    return mapped;
  };

  const deleteEquipmentRequest = async (id: string) => {
    await deleteEquipmentRequestApi(id);
    equipmentRequests.update((prev) => prev.filter((r) => r.id !== id));
  };

  // ---- Feedback Handlers (functional requirement #2) ----
  const addFeedback = async (fb: Omit<Feedback, 'id' | 'createdAt'>): Promise<Feedback> => {
    const now = new Date();
    const created: Feedback = {
      ...fb,
      id: `fb-${Date.now()}`,
      createdAt: `${now.toISOString().slice(0, 10)} ${now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`,
    };
    feedbacks.update((prev) => [created, ...prev]);

    // Persist feedback to SQL Server (POST /api/admin/feedback — endpoint mở
    // cho mọi tài khoản đăng nhập, khách gửi phản hồi theo functional req #2).
    try {
      const saved = await createFeedbackApi({
        feedbackId: created.id,
        customerId: fb.customerId,
        accountId: fb.accountId,
        orderId: fb.orderId,
        customerName: fb.customerName,
        rating: fb.rating,
        category: fb.category,
        message: fb.message,
        status: 'New',
      });
      const realId = saved?.feedbackId;
      if (realId && realId !== created.id) {
        feedbacks.update((prev) =>
          prev.map((f) => (f.id === created.id ? { ...f, id: realId } : f))
        );
        created.id = realId;
      }
    } catch (err) {
      console.warn('[NexusContext] Failed to persist feedback to SQL Server:', err);
    }
    return created;
  };

  const respondFeedback = (id: string, response: string, respondedBy: string) => {
    const now = new Date();
    feedbacks.update((prev) =>
      prev.map((f) =>
        f.id === id
          ? {
              ...f,
              response,
              respondedBy,
              respondedAt: `${now.toISOString().slice(0, 10)} ${now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`,
            }
          : f
      )
    );

    // Persist admin's reply to SQL Server (PUT /api/admin/feedback/{id}).
    updateFeedbackApi(id, { response, respondedBy, status: 'Resolved' }).catch((err) =>
      console.warn('[Nexus] Failed to persist feedback response to SQL Server:', err)
    );
  };

  // ---- SQL Server Database Sync ----
  const syncWithDatabase = async (): Promise<boolean> => {
    isSyncing.set(true);
    try {
      const res = await fetch('/api/nexus/all');
      if (res.ok) {
        const data = await res.json();
        if (data && data.plans && data.plans.length > 0) {
          plans.set(data.plans);
          if (data.retailShops && data.retailShops.length > 0) retailShops.set(data.retailShops);
          if (data.employees && data.employees.length > 0) employees.set(data.employees);
          if (data.vendors && data.vendors.length > 0) vendors.set(data.vendors);
          if (data.inventory && data.inventory.length > 0) inventory.set(data.inventory);
          if (data.orders && data.orders.length > 0) {
            // The SQL bridge is READ-ONLY and the DB's CK_Orders_Status does not know
            // the two-stage statuses, so a plain `orders.set()` would erase every
            // application created or approved in this browser on the next reload.
            // Merge instead: DB rows are the base, but any order this session already
            // owns (new, retail-approved or retail-rejected) keeps its local state.
            const localOrders = get(orders);
            const localById = new Map(localOrders.map((o) => [o.id, o]));
            const mergedFromDb: Order[] = (data.orders as Order[]).map(
              (o) => localById.get(o.id) ?? o
            );
            (data.orders as Order[]).forEach((o) => localById.delete(o.id));
            orders.set([...localById.values(), ...mergedFromDb]);
          }

          if (data.connections && data.connections.length > 0) {
            const localConns = get(connections);
            const localById = new Map(localConns.map((c) => [c.accountId.replace(/-/g, ''), c]));
            const mergedFromDb: Connection[] = (data.connections as Connection[]).map(
              (c) => localById.get(c.accountId.replace(/-/g, '')) ?? c
            );
            (data.connections as Connection[]).forEach((c) => localById.delete(c.accountId.replace(/-/g, '')));
            connections.set([...localById.values(), ...mergedFromDb]);
          }

          if (data.equipments && data.equipments.length > 0) {
            const localEqs = get(equipments);
            const localById = new Map(localEqs.map((e) => [e.serialNumber, e]));
            const mergedFromDb: Equipment[] = (data.equipments as Equipment[]).map(
              (e) => localById.get(e.serialNumber) ?? e
            );
            (data.equipments as Equipment[]).forEach((e) => localById.delete(e.serialNumber));
            equipments.set([...localById.values(), ...mergedFromDb]);
          }

          if (data.bills && data.bills.length > 0) {
            const localBills = get(bills);
            const localById = new Map(localBills.map((b) => [b.invoiceNumber, b]));
            const mergedFromDb: Bill[] = (data.bills as Bill[]).map(
              (b) => localById.get(b.invoiceNumber) ?? b
            );
            (data.bills as Bill[]).forEach((b) => localById.delete(b.invoiceNumber));
            bills.set([...localById.values(), ...mergedFromDb]);
          }

          if (data.feedbacks && data.feedbacks.length > 0) feedbacks.set(data.feedbacks);
          if (data.settings) settings.set(data.settings);

          dbConnected.set(true);
          dbInfo.set({
            status: 'Connected',
            server: data.server || '(localdb)\\MSSQLLocalDB',
            database: data.database || 'NexusSystem',
            tableCount: 14,
            planCount: data.plans.length,
            orderCount: data.orders ? data.orders.length : 7,
          });
          console.log('[Nexus] Successfully hydrated state from Microsoft SQL Server [NexusSystem]');
        }
      }

      // Also hydrate directly from backend API endpoints. Ngoài Connections,
      // Bills, Orders — nạp thêm Plans/Shops/Vendors/Employees/Inventory từ API
      // thật để chạy được cả production (middleware /api/nexus/all chỉ có ở dev).
      try {
        // Khách (role 'user' trong localStorage) không đọc được /api/billing
        // (khoá Senior Accountant) — lấy hoá đơn qua endpoint riêng lọc sở hữu.
        const billsLoader = readStoredUserRole() === 'user' ? fetchMyBillsApi : fetchBillsApi;
        const [apiConns, apiBills, apiOrders, apiPlans, apiShops, apiVendors, apiEmps, apiInv] =
          await Promise.all([
            fetchConnectionsApi().catch(() => null),
            billsLoader().catch(() => null),
            fetchOrdersApi().catch(() => null),
            fetchPlansApi().catch(() => null),
            fetchRetailShopsApi().catch(() => null),
            fetchVendorsApi().catch(() => null),
            fetchEmployeesApi().catch(() => null),
            fetchInventoryApi().catch(() => null),
          ]);

        // Merge theo id: dữ liệu CSDL là nền, bản ghi local mới thêm được giữ lại.
        const mergeById = <T extends { id: string }>(current: T[], incoming: T[]): T[] => {
          if (!incoming || incoming.length === 0) return current;
          const byId = new Map(current.map((x) => [x.id, x]));
          incoming.forEach((x) => byId.set(x.id, { ...(byId.get(x.id) ?? ({} as T)), ...x }));
          return [...byId.values()];
        };

        if (Array.isArray(apiPlans) && apiPlans.length > 0) {
          plans.set(mergeById(get(plans), apiPlans.map((p: any) => ({
            id: p.planId,
            name: p.name,
            type: p.connectionType,
            speedOrBandwidth: p.speedOrBandwidth || '',
            monthlyRental: p.monthlyRental ?? 0,
            hourlyCharge: p.hourlyCharge || undefined,
            securityDeposit: p.securityDeposit ?? 0,
            dataLimit: p.dataLimit || undefined,
            status: p.status || 'Active',
            description: p.description || '',
            billingCycle: p.billingCycle || undefined,
            validity: p.validity || undefined,
            includedHours: p.includedHours || undefined,
            callRates: p.callRates || undefined,
            isPopular: p.isPopular || undefined,
          }))));
        }

        if (Array.isArray(apiShops) && apiShops.length > 0) {
          retailShops.set(mergeById(get(retailShops), apiShops.map((s: any) => ({
            id: s.storeId,
            shopCode: s.storeId || s.shopCode,
            name: s.name,
            city: s.city || '',
            cityCode: s.cityCode || '',
            address: s.address || '',
            managerName: s.managerName || '',
            phone: s.phone || '',
            operatingHours: s.operatingHours || '',
            activeEmployeesCount: s.activeEmployeesCount ?? 0,
            totalSubscribersServed: s.totalSubscribersServed ?? 0,
            status: s.status || 'Active',
          }))));
        }

        if (Array.isArray(apiVendors) && apiVendors.length > 0) {
          vendors.set(mergeById(get(vendors), apiVendors.map((v: any) => ({
            id: v.vendorId,
            vendorCode: v.vendorCode || '',
            companyName: v.companyName,
            contactPerson: v.contactPerson || '',
            category: v.category || 'Modems & Routers',
            phone: v.phone || '',
            email: v.email || '',
            address: v.address || '',
            rating: v.rating ?? 5,
            status: v.status || 'Active',
          }))));
        }

        if (Array.isArray(apiEmps) && apiEmps.length > 0) {
          employees.set(mergeById(get(employees), apiEmps.map((e: any) => ({
            id: e.employeeId,
            employeeCode: e.employeeCode || '',
            name: e.fullName,
            email: e.email || '',
            phone: e.phone || '',
            role: e.role || 'Retail Staff',
            department: e.department || '',
            retailShopAssigned: e.storeId || undefined,
            status: e.status || 'Active',
            dateOfJoining: e.dateOfJoining ? String(e.dateOfJoining).slice(0, 10) : '',
            avatar: e.avatarUrl || undefined,
            address: e.address || undefined,
            gender: (e.gender || '').toLowerCase() || undefined,
            dateOfBirth: e.dateOfBirth ? String(e.dateOfBirth).slice(0, 10) : undefined,
            bio: e.bio || undefined,
          }))));
        }

        if (Array.isArray(apiInv) && apiInv.length > 0) {
          inventory.set(mergeById(get(inventory), apiInv.map((i: any) => ({
            id: i.inventoryId,
            itemCode: i.itemCode,
            name: i.name,
            category: i.category,
            stockQuantity: i.stockQuantity ?? 0,
            reorderLevel: i.reorderLevel ?? 10,
            unitCost: i.unitCost ?? 0,
            location: i.location || '',
            vendorId: i.vendorId || '',
            supplier: '',
            status: i.status || 'In Stock',
          }))));
        }

        if (apiOrders && Array.isArray(apiOrders) && apiOrders.length > 0) {
          const localOrders = get(orders);
          const localById = new Map(localOrders.map((o) => [o.id, o]));
          const mappedOrders: Order[] = apiOrders.map((ao: any) => {
            const local = localById.get(ao.orderId);
            return {
              id: ao.orderId,
              customerName: ao.customerName,
              customerPhone: ao.customerPhone || '',
              customerEmail: ao.customerEmail || '',
              installationAddress: ao.installationAddress || '',
              idProofType: ao.idProofType || local?.idProofType || 'National ID Card',
              idProofNumber: ao.idProofNumber || local?.idProofNumber || '',
              connectionType: ao.connectionType || 'Broadband',
              planId: ao.planId,
              planName: ao.planName,
              retailOutletCode: ao.storeId || local?.retailOutletCode || 'RS-HCM-01',
              assignedBranchName: ao.storeName || local?.assignedBranchName || '',
              retailEmployeeName: local?.retailEmployeeName || 'Retail Staff',
              assignedEmployeeId: ao.employeeId || local?.assignedEmployeeId,
              status: (ao.status as OrderStatus) || 'PendingRetail',
              createdAt: ao.applicationDate ? ao.applicationDate.slice(0, 16).replace('T', ' ') : (local?.createdAt || ''),
              cableDistanceMeters: ao.cableDistanceMeters ?? local?.cableDistanceMeters,
              dpBoxCapacity: ao.dpBoxCapacity || local?.dpBoxCapacity,
              signalLossDbm: ao.signalLossDbm ?? local?.signalLossDbm,
              feasibilityNotes: ao.feasibilityNotes || local?.feasibilityNotes,
              rejectionReason: ao.rejectionReason || local?.rejectionReason,
              retailRejectionReason: local?.retailRejectionReason,
              assignedAccountId: ao.assignedAccountId || local?.assignedAccountId,
              assignedTechnician: ao.assignedTechnicianName || local?.assignedTechnician,
              assignedTechnicianPhone: ao.assignedTechnicianPhone || local?.assignedTechnicianPhone,
              assignedTechnicianDate: ao.assignedTechnicianDate ? ao.assignedTechnicianDate.slice(0, 16).replace('T', ' ') : local?.assignedTechnicianDate,
              scheduledInstallDate: ao.scheduledInstallDate ? ao.scheduledInstallDate.slice(0, 16).replace('T', ' ') : local?.scheduledInstallDate,
              bulkConnectionsCount: ao.bulkConnectionsCount ?? local?.bulkConnectionsCount ?? 1,
              bulkDiscountPercent: ao.bulkDiscountPercent ?? local?.bulkDiscountPercent ?? 0,
              // Orders.DepositWaived (cột tính toán): > 50 kết nối thì miễn tiền cọc.
              depositWaived: ao.depositWaived ?? isDepositWaived(ao.bulkConnectionsCount ?? local?.bulkConnectionsCount ?? 1),
              existingLandlineAccountId: ao.existingLandlineAccountId || local?.existingLandlineAccountId,
              landlineFeasible: ao.landlineFeasible ?? local?.landlineFeasible,
              internetFeasible: ao.internetFeasible ?? local?.internetFeasible,
              retailApprovedBy: ao.retailApprovedBy || local?.retailApprovedBy,
              retailApprovedAt: ao.retailApprovedDate ? ao.retailApprovedDate.slice(0, 16).replace('T', ' ') : local?.retailApprovedAt,
              retailApprovalNotes: ao.retailApprovalNotes || local?.retailApprovalNotes,
            };
          });

          mappedOrders.forEach((o) => {
            localById.set(o.id, { ...(localById.get(o.id) || {}), ...o });
          });
          orders.set([...localById.values()]);
        }

        if (apiConns && Array.isArray(apiConns) && apiConns.length > 0) {
          const localConns = get(connections);
          const localById = new Map(localConns.map((c) => [c.accountId.replace(/-/g, ''), c]));
          const mappedConns: Connection[] = apiConns.map((ac: any) => ({
            accountId: ac.accountId,
            orderId: ac.orderId,
            customerName: ac.customerName,
            customerPhone: ac.customerPhone || '',
            customerEmail: ac.customerEmail || '',
            installationAddress: ac.installationAddress || '',
            connectionType: ac.connectionType || 'Broadband',
            planName: ac.planName || '',
            monthlyRental: ac.monthlyRental ?? 0,
            securityDeposit: ac.securityDeposit ?? 0,
            status: ac.status || 'Active',
            ipAddress: ac.ipAddress || '',
            portNumber: ac.portNumber || '',
            assignedDeviceSerial: ac.assignedDeviceSerial || '',
            assignedDeviceModel: ac.assignedDeviceModel || '',
            installedDate: ac.installedDate ? ac.installedDate.slice(0, 10) : new Date().toISOString().slice(0, 10),
            lastUpdated: ac.lastUpdated ? ac.lastUpdated.slice(0, 10) : new Date().toISOString().slice(0, 10),
            lastStatusReason: ac.lastStatusReason || '',
          }));
          mappedConns.forEach((c) => localById.set(c.accountId.replace(/-/g, ''), { ...localById.get(c.accountId.replace(/-/g, '')), ...c }));
          connections.set([...localById.values()]);
        }

        if (apiBills && Array.isArray(apiBills) && apiBills.length > 0) {
          const localBills = get(bills);
          const localById = new Map(localBills.map((b) => [b.invoiceNumber, b]));
          const mappedBills: Bill[] = apiBills.map((ab: any) => ({
            id: ab.billId || `bill-${Date.now()}`,
            invoiceNumber: ab.invoiceNumber,
            accountId: ab.accountId,
            customerName: ab.customerName,
            billingMonth: ab.billingMonth,
            billingDate: ab.billingDate ? ab.billingDate.slice(0, 10) : '',
            dueDate: ab.dueDate ? ab.dueDate.slice(0, 10) : '',
            planName: ab.planName || '',
            connectionType: ab.connectionType || 'Broadband',
            securityDeposit: ab.securityDeposit ?? 0,
            monthlyRental: ab.monthlyRental ?? 0,
            hourlyCharges: ab.hourlyCharges ?? 0,
            discountPercent: ab.discountPercent ?? 0,
            discountAmount: ab.discountAmount ?? 0,
            subtotal: ab.subtotal ?? 0,
            serviceTaxRate: ab.serviceTaxRate ?? 12.24,
            serviceTaxAmount: ab.serviceTaxAmount ?? 0,
            totalAmount: ab.totalAmount ?? 0,
            amountPaid: ab.amountPaid ?? 0,
            dueAmount: ab.dueAmount ?? 0,
            status: ab.status || 'Unpaid',
            paymentHistory: (ab.payments || []).map((p: any) => ({
              paymentId: p.paymentId,
              paymentDate: p.paymentDate ? p.paymentDate.slice(0, 10) : '',
              amountPaid: p.amountPaid,
              paymentMode: p.paymentMode,
              referenceNumber: p.referenceNumber || '',
              recordedBy: p.recordedBy || 'Accounts Cashier',
            })),
          }));
          mappedBills.forEach((b) => localById.set(b.invoiceNumber, { ...localById.get(b.invoiceNumber), ...b }));
          bills.set([...localById.values()]);
        }
      } catch (e) {
        console.warn('[NexusContext] Background fetch of bills/connections skipped:', e);
      }

      return true;
    } catch (err) {
      console.warn('[Nexus] Could not reach SQL Server bridge, using local storage state:', err);
    } finally {
      isSyncing.set(false);
    }
    return false;
  };

  // Auto-trigger sync on browser startup
  if (typeof window !== 'undefined') {
    setTimeout(() => {
      syncWithDatabase();
      refreshPurchaseOrders();
      refreshEquipmentRequests();
    }, 150);
  }

  // ---- Settings Handlers ----
  const updateSettings = (newSettings: Partial<SystemSettings>) => {
    settings.update((prev) => ({ ...prev, ...newSettings }));
  };

  return {
    currentRole,
    dbConnected,
    dbInfo,
    isSyncing,
    syncWithDatabase,
    plans,
    employees,
    vendors,
    retailShops,
    inventory,
    orders,
    connections,
    equipments,
    bills,
    feedbacks,
    settings,
    // Two-stage approval: branch routing + retail paperwork gate
    routeOrderToBranch,
    approveOrderByRetail,
    rejectOrderByRetail,
    resubmitOrderToRetail,
    canTechnicalAssess,
    addPlan,
    updatePlan,
    deletePlan,
    addEmployee,
    updateEmployee,
    deleteEmployee,
    addVendor,
    updateVendor,
    deleteVendor,
    addRetailShop,
    updateRetailShop,
    deleteRetailShop,
    purchaseOrders,
    refreshPurchaseOrders,
    addPurchaseOrder,
    setPurchaseOrderStatus,
    deletePurchaseOrder,
    equipmentRequests,
    refreshEquipmentRequests,
    addEquipmentRequest,
    setEquipmentRequestStatus,
    deleteEquipmentRequest,
    updateInventoryStock,
    addInventoryItem,
    updateInventoryItem,
    deleteInventoryItem,
    placeOrder,
    assignTechnicianToOrder,
    updateOrderStatus,
    provisionConnectionForOrder,
    updateConnectionStatus,
    addEquipment,
    updateEquipment,
    generateBill,
    recordPayment,
    updateBill,
    addFeedback,
    respondFeedback,
    updateSettings,
  };
}

export const nexusStore = createNexusStore();


