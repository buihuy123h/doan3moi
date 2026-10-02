<script lang="ts">
  import {
    Search, X, Wifi, ShoppingBag, Radio, Phone, ShieldCheck,
    Layers, HardDrive, Users, CreditCard, Store, Truck,
    MessageSquare, Compass, ArrowRight, Zap, User, Globe
  } from 'lucide-svelte';
  import { toast } from 'svelte-sonner';
  import { nexusStore } from '../../context/NexusContext';
  import { languageStore } from '../../context/LanguageContext';
  import { authStore } from '../../context/AuthContext';
  import { navigateTo, navigate } from '../../lib/router';
  import { getPlanName } from '../../lib/planI18n';
  import { roleTypeFromJobTitle } from '../../types/nexus';

  const { language, t, toggleLanguage } = languageStore;
  const { currentUser } = authStore;
  const {
    plans, orders, connections, bills, equipments,
    retailShops, employees, vendors, feedbacks, inventory
  } = nexusStore;

  // Normalization for search (handles Vietnamese diacritics)
  function normalizeVietnamese(str: string): string {
    return (str || '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'D')
      .toLowerCase()
      .trim();
  }

  type SearchCategory =
    | 'all'
    | 'plans'
    | 'orders'
    | 'connections'
    | 'bills'
    | 'inventory'
    | 'shops'
    | 'employees'
    | 'vendors'
    | 'feedback'
    | 'navigation';

  let isExpanded = $state(false);
  let searchQuery = $state('');
  let inputRef = $state<HTMLInputElement | null>(null);

  const toggleExpand = () => {
    isExpanded = !isExpanded;
    if (isExpanded) {
      setTimeout(() => {
        if (inputRef) inputRef.focus();
      }, 70);
    } else {
      searchQuery = '';
    }
  };

  // Close when clicking outside
  const handleWindowClick = (e: MouseEvent) => {
    const target = e.target as HTMLElement | null;
    if (isExpanded && target && !target.closest('.search-dropdown-container')) {
      isExpanded = false;
      searchQuery = '';
    }
  };

  // Keyboard shortcut listeners (Escape and Ctrl+K)
  const handleKeydown = (e: KeyboardEvent) => {
    if (e.key === 'Escape' && isExpanded) {
      isExpanded = false;
      searchQuery = '';
    } else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      toggleExpand();
    }
  };

  interface SearchItem {
    id: string;
    category: SearchCategory;
    categoryLabelVi: string;
    categoryLabelEn: string;
    titleVi: string;
    titleEn: string;
    subtitleVi: string;
    subtitleEn: string;
    badgeTextVi?: string;
    badgeTextEn?: string;
    badgeColor?: string;
    targetPath?: string;
    targetTab?: string;
    actionLabelVi: string;
    actionLabelEn: string;
    icon: any;
    rawText: string;
  }

  // Pre-configured bilingual system navigation points
  const systemPages: SearchItem[] = [
    {
      id: 'nav-home',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Trang chủ & Bảng giá Gói cước',
      titleEn: 'Home & Plan Pricing Showcase',
      subtitleVi: 'Trang giới thiệu công cộng, dịch vụ viễn thông, điểm bán',
      subtitleEn: 'Public portal, telecom service packages, and retail shops',
      badgeTextVi: 'Trang chủ',
      badgeTextEn: 'Home',
      badgeColor: 'bg-sky-500/15 text-sky-700 dark:text-sky-300',
      targetPath: '/',
      actionLabelVi: 'Về Trang chủ',
      actionLabelEn: 'Home',
      icon: Compass,
      rawText: 'trang chu home bang gia goi cuoc gioithieu portal public pricing showcase landing',
    },
    {
      id: 'nav-register',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Đăng ký Mua Gói cước Trực tuyến',
      titleEn: 'Online Plan Registration & Purchase',
      subtitleVi: 'Khởi tạo đơn hàng mới, đăng ký thuê bao viễn thông',
      subtitleEn: 'New order checkout, telecom service subscription',
      badgeTextVi: 'Đăng ký',
      badgeTextEn: 'Register',
      badgeColor: 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300',
      targetPath: '/register',
      actionLabelVi: 'Đăng ký',
      actionLabelEn: 'Register',
      icon: ShoppingBag,
      rawText: 'dang ky register mua goi cuoc tao don hang online dat hang purchase checkout order',
    },
    {
      id: 'nav-login',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Cổng Đăng nhập & Tra cứu Đơn hàng',
      titleEn: 'Sign In & Live Order Tracking Portal',
      subtitleVi: 'Đăng nhập nhân viên, hoặc tra cứu tiến độ đơn hàng thời gian thực',
      subtitleEn: 'Staff sign in, or real-time order status tracking',
      badgeTextVi: 'Đăng nhập',
      badgeTextEn: 'Sign In',
      badgeColor: 'bg-indigo-500/15 text-indigo-700 dark:text-indigo-300',
      targetPath: '/login',
      actionLabelVi: 'Đăng nhập',
      actionLabelEn: 'Login',
      icon: User,
      rawText: 'dang nhap login tra cuu don hang tracking auth signin portal authentication',
    },
    {
      id: 'nav-retail',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Phân hệ Điểm Bán Lẻ (Retail Counter)',
      titleEn: 'Retail Counter Operations (Branch Office)',
      subtitleVi: 'Tạo đơn mới, quản lý đơn hàng, tra cứu nâng cao, thu tiền cước tại quầy',
      subtitleEn: 'New orders, order tracking, subscriber lookup, counter cash billing',
      badgeTextVi: 'Điểm bán lẻ',
      badgeTextEn: 'Retail',
      badgeColor: 'bg-amber-500/15 text-amber-700 dark:text-amber-300',
      targetPath: '/retail',
      targetTab: 'new-order',
      actionLabelVi: 'Điểm bán lẻ',
      actionLabelEn: 'Retail',
      icon: Store,
      rawText: 'ban le retail shop quay giao dich tao don quan ly tra cuu thu cuoc counter branch office',
    },
    {
      id: 'nav-technical',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Phân hệ Kỹ Thuật (Technical Operations)',
      titleEn: 'Technical & Engineering Operations',
      subtitleVi: 'Khảo sát khả thi 2 tầng, cấp mã 16 ký tự, cấp phát CPE 1:1, bật/tắt kết nối',
      subtitleEn: '2-tier feasibility survey, 16-char ID, 1:1 CPE binding, switch port provisioning',
      badgeTextVi: 'Kỹ thuật',
      badgeTextEn: 'Technical',
      badgeColor: 'bg-blue-500/15 text-blue-700 dark:text-blue-300',
      targetPath: '/technical',
      targetTab: 'feasibility-queue',
      actionLabelVi: 'Kỹ thuật',
      actionLabelEn: 'Technical',
      icon: Zap,
      rawText: 'ky thuat technical khao sat kha thi feasibility cpe 1:1 cap phat ket noi switch port engineering',
    },
    {
      id: 'nav-accounts',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Phân hệ Kế Toán & Thuế (Accounts & Billing)',
      titleEn: 'Accounts & Billing Department',
      subtitleVi: 'Sinh hóa đơn cước, chiết khấu bulk, thuế pháp định 12.24%, tự động phục hồi Active',
      subtitleEn: 'Bill generation, bulk corporate discount, 12.24% statutory tax, auto-reactivate',
      badgeTextVi: 'Kế toán',
      badgeTextEn: 'Accounts',
      badgeColor: 'bg-purple-500/15 text-purple-700 dark:text-purple-300',
      targetPath: '/accounts',
      targetTab: 'bill-generation',
      actionLabelVi: 'Kế toán',
      actionLabelEn: 'Accounts',
      icon: CreditCard,
      rawText: 'ke toan accounts hoa don billing thue 12.24% service tax thanh toan payment ledger finance invoice',
    },
    {
      id: 'nav-user',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Cổng Thuê Bao Khách Hàng (Customer Portal)',
      titleEn: 'Customer Self-Service Portal',
      subtitleVi: 'Đăng nhập bằng Account ID 16 ký tự, xem thông số, hóa đơn, gửi phản hồi',
      subtitleEn: 'Log in with 16-char Account ID, telemetry, bills, and feedback',
      badgeTextVi: 'Khách hàng',
      badgeTextEn: 'Customer',
      badgeColor: 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300',
      targetPath: '/user',
      targetTab: 'overview',
      actionLabelVi: 'Khách hàng',
      actionLabelEn: 'Customer',
      icon: ShieldCheck,
      rawText: 'khach hang user subscriber thue bao thong so ip cong thiet bi hoa don feedback self-service portal',
    },
    {
      id: 'nav-admin',
      category: 'navigation',
      categoryLabelVi: 'Trang hệ thống',
      categoryLabelEn: 'Page',
      titleVi: 'Cổng Quản Trị Cấp Cao (Admin Master Data)',
      titleEn: 'Master Administration & Management Portal',
      subtitleVi: 'Tổng quan KPI, CRUD Gói cước, Điểm bán lẻ, Nhân viên, Nhà cung cấp, Kho vật tư, Feedback',
      subtitleEn: 'KPI metrics, CRUD Plans, Retail shops, Employees, Vendors, Stock, Customer Feedback',
      badgeTextVi: 'Quản trị',
      badgeTextEn: 'Admin',
      badgeColor: 'bg-rose-500/15 text-rose-700 dark:text-rose-300',
      targetPath: '/admin',
      targetTab: 'overview',
      actionLabelVi: 'Quản trị',
      actionLabelEn: 'Admin',
      icon: Layers,
      rawText: 'quan tri admin director master data plans retail employees vendors stock kho feedback',
    },
  ];

  // Compile all search items reactively
  const allSearchItems = $derived.by<SearchItem[]>(() => {
    const list: SearchItem[] = [];
    const user = $currentUser;

    // 1. Gói cước (Plans)
    $plans.forEach((p) => {
      const typeColor =
        p.type === 'Broadband'
          ? 'bg-sky-500/15 text-sky-700 dark:text-sky-300'
          : p.type === 'Dial-Up'
          ? 'bg-amber-500/15 text-amber-700 dark:text-amber-300'
          : 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300';

      let targetPath = '/register';
      let targetTab: string | undefined = undefined;
      let actionLabelVi = 'Đăng ký';
      let actionLabelEn = 'Subscribe';

      if (user) {
        if (user.role === 'admin') {
          targetPath = '/admin';
          targetTab = 'plans';
          actionLabelVi = 'Cấu hình';
          actionLabelEn = 'Configure';
        } else if (user.role === 'retail') {
          targetPath = '/retail';
          targetTab = 'new-order';
          actionLabelVi = 'Tạo đơn';
          actionLabelEn = 'Create Order';
        } else if (user.role === 'user') {
          targetPath = '/user';
          targetTab = 'overview';
          actionLabelVi = 'Xem chi tiết';
          actionLabelEn = 'View Details';
        }
      }

      const nameVi = getPlanName(p, 'vi');
      const nameEn = getPlanName(p, 'en');
      const typeVi = p.type === 'Broadband' ? 'Cáp quang' : p.type === 'Dial-Up' ? 'Quay số' : 'Điện thoại cố định';
      const typeEn = p.type;

      list.push({
        id: p.id,
        category: 'plans',
        categoryLabelVi: 'Gói cước',
        categoryLabelEn: 'Plan',
        titleVi: nameVi,
        titleEn: nameEn,
        subtitleVi: `${typeVi} • ${p.speedOrBandwidth} • $${p.monthlyRental}/tháng • Cọc: $${p.securityDeposit}`,
        subtitleEn: `${typeEn} • ${p.speedOrBandwidth} • $${p.monthlyRental}/month • Deposit: $${p.securityDeposit}`,
        badgeTextVi: typeVi,
        badgeTextEn: typeEn,
        badgeColor: typeColor,
        targetPath,
        targetTab,
        actionLabelVi,
        actionLabelEn,
        icon: Wifi,
        rawText: `${p.id} ${p.name} ${nameVi} ${nameEn} ${p.type} ${typeVi} ${p.speedOrBandwidth} ${p.monthlyRental} ${p.securityDeposit} ${p.dataLimit || ''} ${p.description}`,
      });
    });

    // 2. Đơn hàng (Orders)
    $orders.forEach((o) => {
      const statusColor =
        o.status === 'Connection Provided'
          ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300'
          : o.status === 'Feasible'
          ? 'bg-blue-500/15 text-blue-700 dark:text-blue-300'
          : o.status === 'Pending'
          ? 'bg-amber-500/15 text-amber-700 dark:text-amber-300'
          : 'bg-rose-500/15 text-rose-700 dark:text-rose-300';

      let targetPath = '/retail';
      let targetTab = 'order-tracking';
      if (user?.role === 'technical') {
        targetPath = '/technical';
        targetTab = 'feasibility-queue';
      } else if (user?.role === 'user') {
        targetPath = '/user';
        targetTab = 'overview';
      }

      const statusMapVi: Record<string, string> = {
        'Connection Provided': 'Đã cấp kết nối',
        'Feasible': 'Khả thi kỹ thuật',
        'Pending': 'Chờ xử lý',
        'Not Feasible': 'Không khả thi',
      };
      const statusVi = statusMapVi[o.status] || o.status;

      list.push({
        id: o.id,
        category: 'orders',
        categoryLabelVi: 'Đơn hàng',
        categoryLabelEn: 'Order',
        titleVi: `${o.id} — ${o.customerName}`,
        titleEn: `${o.id} — ${o.customerName}`,
        subtitleVi: `Gói: ${o.planName} • SĐT: ${o.customerPhone} • ${statusVi}`,
        subtitleEn: `Plan: ${o.planName} • Phone: ${o.customerPhone} • ${o.status}`,
        badgeTextVi: statusVi,
        badgeTextEn: o.status,
        badgeColor: statusColor,
        targetPath,
        targetTab,
        actionLabelVi: 'Mở đơn hàng',
        actionLabelEn: 'View Order',
        icon: ShoppingBag,
        rawText: `${o.id} ${o.customerName} ${o.customerPhone} ${o.customerEmail} ${o.installationAddress} ${o.planName} ${o.connectionType} ${o.status} ${statusVi} ${o.assignedAccountId || ''} ${o.retailOutletCode}`,
      });
    });

    // 3. Thuê bao / Kết nối (Connections)
    $connections.forEach((c) => {
      const connColor =
        c.status === 'Active'
          ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300'
          : c.status === 'Temporarily Inactive'
          ? 'bg-amber-500/15 text-amber-700 dark:text-amber-300'
          : 'bg-rose-500/15 text-rose-700 dark:text-rose-300';

      let targetPath = '/retail';
      let targetTab = 'connection-details';
      if (user?.role === 'technical') {
        targetPath = '/technical';
        targetTab = 'connection-manager';
      } else if (user?.role === 'accounts') {
        targetPath = '/accounts';
        targetTab = 'bill-generation';
      } else if (user?.role === 'user') {
        targetPath = '/user';
        targetTab = 'overview';
      }

      const connStatusMapVi: Record<string, string> = {
        'Active': 'Đang hoạt động',
        'Temporarily Inactive': 'Tạm ngưng',
        'Terminated': 'Đã chấm dứt',
      };
      const connStatusVi = connStatusMapVi[c.status] || c.status;

      list.push({
        id: c.accountId,
        category: 'connections',
        categoryLabelVi: 'Thuê bao',
        categoryLabelEn: 'Subscriber',
        titleVi: `${c.accountId} — ${c.customerName}`,
        titleEn: `${c.accountId} — ${c.customerName}`,
        subtitleVi: `${c.connectionType} • IP: ${c.ipAddress} • Cổng: ${c.portNumber} • ${connStatusVi}`,
        subtitleEn: `${c.connectionType} • IP: ${c.ipAddress} • Port: ${c.portNumber} • ${c.status}`,
        badgeTextVi: connStatusVi,
        badgeTextEn: c.status,
        badgeColor: connColor,
        targetPath,
        targetTab,
        actionLabelVi: 'Tra cứu thuê bao',
        actionLabelEn: 'View Connection',
        icon: ShieldCheck,
        rawText: `${c.accountId} ${c.orderId} ${c.customerName} ${c.customerPhone} ${c.customerEmail} ${c.installationAddress} ${c.planName} ${c.connectionType} ${c.status} ${connStatusVi} ${c.ipAddress} ${c.portNumber} ${c.assignedDeviceSerial}`,
      });
    });

    // 4. Hóa đơn (Bills & Invoices)
    $bills.forEach((b) => {
      const billColor =
        b.status === 'Paid'
          ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300'
          : b.status === 'Partially Paid'
          ? 'bg-blue-500/15 text-blue-700 dark:text-blue-300'
          : 'bg-rose-500/15 text-rose-700 dark:text-rose-300';

      let targetPath = '/accounts';
      let targetTab = 'payment-updates';
      if (user?.role === 'retail') {
        targetPath = '/retail';
        targetTab = 'payment-records';
      } else if (user?.role === 'user') {
        targetPath = '/user';
        targetTab = 'overview';
      }

      const billStatusMapVi: Record<string, string> = {
        'Paid': 'Đã thanh toán',
        'Partially Paid': 'Thanh toán 1 phần',
        'Unpaid': 'Chưa thanh toán',
      };
      const billStatusVi = billStatusMapVi[b.status] || b.status;

      list.push({
        id: b.invoiceNumber,
        category: 'bills',
        categoryLabelVi: 'Hóa đơn',
        categoryLabelEn: 'Bill',
        titleVi: `${b.invoiceNumber} — ${b.customerName}`,
        titleEn: `${b.invoiceNumber} — ${b.customerName}`,
        subtitleVi: `Mã TK: ${b.accountId} • Kỳ cước: ${b.billingMonth} • Nợ: $${b.dueAmount}`,
        subtitleEn: `Account ID: ${b.accountId} • Period: ${b.billingMonth} • Due: $${b.dueAmount}`,
        badgeTextVi: billStatusVi,
        badgeTextEn: b.status,
        badgeColor: billColor,
        targetPath,
        targetTab,
        actionLabelVi: 'Xem hóa đơn',
        actionLabelEn: 'Open Bill',
        icon: CreditCard,
        rawText: `${b.invoiceNumber} ${b.accountId} ${b.customerName} ${b.billingMonth} ${b.planName} ${b.totalAmount} ${b.dueAmount} ${b.status} ${billStatusVi} invoice bill`,
      });
    });

    // 5. Thiết bị kho CPE (Equipments / Inventory)
    $equipments.forEach((eq) => {
      const eqColor =
        eq.status === 'In Service'
          ? 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300'
          : eq.status === 'In Stock'
          ? 'bg-sky-500/15 text-sky-700 dark:text-sky-300'
          : 'bg-rose-500/15 text-rose-700 dark:text-rose-300';

      let targetPath = '/technical';
      let targetTab = 'equipment-tracker';
      if (user?.role === 'admin') {
        targetPath = '/admin';
        targetTab = 'stock';
      }

      const eqStatusMapVi: Record<string, string> = {
        'In Service': 'Đang sử dụng',
        'In Stock': 'Trong kho',
        'Faulty': 'Hỏng hóc',
      };
      const eqStatusVi = eqStatusMapVi[eq.status] || eq.status;

      list.push({
        id: eq.serialNumber,
        category: 'inventory',
        categoryLabelVi: 'Thiết bị CPE',
        categoryLabelEn: 'CPE Device',
        titleVi: `${eq.serialNumber} (${eq.deviceModel})`,
        titleEn: `${eq.serialNumber} (${eq.deviceModel})`,
        subtitleVi: `${eq.deviceType} • Kho: ${eq.branchOutlet ?? 'Chưa gán'} • Tình trạng: ${eqStatusVi}`,
        subtitleEn: `${eq.deviceType} • Branch: ${eq.branchOutlet ?? 'Unassigned'} • Status: ${eq.status}`,
        badgeTextVi: eqStatusVi,
        badgeTextEn: eq.status,
        badgeColor: eqColor,
        targetPath,
        targetTab,
        actionLabelVi: 'Xem thiết bị',
        actionLabelEn: 'View Equipment',
        icon: HardDrive,
        rawText: `${eq.serialNumber} ${eq.deviceModel} ${eq.deviceType} ${eq.branchOutlet ?? ''} ${eq.status} ${eqStatusVi} ${eq.assignedAccountId || ''} ${eq.assignedCustomerName || ''} modem router cpe`,
      });
    });

    // 5b. Vật tư & Linh kiện kho (Warehouse Inventory Items)
    $inventory.forEach((inv) => {
      let targetPath = '/admin';
      let targetTab = 'stock';
      if (user?.role === 'technical') {
        targetPath = '/technical';
        targetTab = 'equipment-tracker';
      }

      list.push({
        id: inv.id,
        category: 'inventory',
        categoryLabelVi: 'Kho vật tư',
        categoryLabelEn: 'Inventory',
        titleVi: `${inv.name} (${inv.itemCode})`,
        titleEn: `${inv.name} (${inv.itemCode})`,
        subtitleVi: `${inv.category} • Tồn kho: ${inv.stockQuantity} • Vị trí: ${inv.location}`,
        subtitleEn: `${inv.category} • In stock: ${inv.stockQuantity} • Location: ${inv.location}`,
        badgeTextVi: `${inv.stockQuantity} cái`,
        badgeTextEn: `${inv.stockQuantity} pcs`,
        badgeColor: inv.stockQuantity <= inv.reorderLevel ? 'bg-amber-500/15 text-amber-700 dark:text-amber-300' : 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300',
        targetPath,
        targetTab,
        actionLabelVi: 'Xem kho',
        actionLabelEn: 'View Stock',
        icon: HardDrive,
        rawText: `${inv.id} ${inv.name} ${inv.itemCode} ${inv.category} ${inv.location} ${inv.supplier} stock inventory`,
      });
    });

    // 6. Điểm bán lẻ & Chi nhánh (Retail Shops)
    $retailShops.forEach((s) => {
      const shopName = s.name || (s as any).shopName || 'Điểm giao dịch Nexus';
      list.push({
        id: s.id,
        category: 'shops',
        categoryLabelVi: 'Điểm bán',
        categoryLabelEn: 'Retail Shop',
        titleVi: `${shopName} (${s.shopCode})`,
        titleEn: `${shopName} (${s.shopCode})`,
        subtitleVi: `Mã vùng: ${s.cityCode} • ${s.address} • Hotline: ${s.phone}`,
        subtitleEn: `City Code: ${s.cityCode} • ${s.address} • Phone: ${s.phone}`,
        badgeTextVi: `Vùng ${s.cityCode}`,
        badgeTextEn: `City ${s.cityCode}`,
        badgeColor: 'bg-sky-500/15 text-sky-700 dark:text-sky-300',
        targetPath: user?.role === 'retail' ? '/retail' : '/admin',
        targetTab: user?.role === 'retail' ? 'new-order' : 'shops',
        actionLabelVi: 'Điểm bán',
        actionLabelEn: 'Shop',
        icon: Store,
        rawText: `${s.id} ${shopName} ${s.shopCode} ${s.cityCode} ${s.address} ${s.city} ${s.phone} ${s.managerName} store branch shop`,
      });
    });

    // 7. Nhân viên hệ thống (Employees)
    $employees.forEach((emp) => {
      // emp.role lưu chức danh trong CSDL ("Manager", "Field Engineer"...), không phải
      // nhóm quyền của giao diện — phải quy đổi, nếu không mọi so sánh dưới đây đều sai.
      const roleKey = roleTypeFromJobTitle(emp.role);
      const roleColor =
        roleKey === 'admin'
          ? 'bg-rose-500/15 text-rose-700 dark:text-rose-300'
          : roleKey === 'technical'
          ? 'bg-blue-500/15 text-blue-700 dark:text-blue-300'
          : roleKey === 'accounts'
          ? 'bg-purple-500/15 text-purple-700 dark:text-purple-300'
          : 'bg-amber-500/15 text-amber-700 dark:text-amber-300';

      const roleMapVi: Record<string, string> = {
        admin: 'Quản trị viên',
        technical: 'Kỹ thuật viên',
        accounts: 'Kế toán viên',
        retail: 'Nhân viên bán lẻ',
      };
      const roleMapEn: Record<string, string> = {
        admin: 'Administrator',
        technical: 'Technician',
        accounts: 'Accountant',
        retail: 'Retail Officer',
      };
      const roleVi = roleMapVi[roleKey] || emp.role;
      const roleEn = roleMapEn[roleKey] || emp.role;

      list.push({
        id: emp.id,
        category: 'employees',
        categoryLabelVi: 'Nhân sự',
        categoryLabelEn: 'Staff',
        titleVi: `${emp.name} (${emp.id})`,
        titleEn: `${emp.name} (${emp.id})`,
        subtitleVi: `${roleVi} • ${emp.department} • ${emp.retailShopAssigned ?? ''}`,
        subtitleEn: `${roleEn} • ${emp.department} • ${emp.retailShopAssigned ?? ''}`,
        badgeTextVi: roleVi,
        badgeTextEn: roleEn,
        badgeColor: roleColor,
        targetPath: '/admin',
        targetTab: 'employees',
        actionLabelVi: 'Nhân viên',
        actionLabelEn: 'Staff',
        icon: Users,
        rawText: `${emp.id} ${emp.name} ${emp.role} ${roleVi} ${roleEn} ${emp.department} ${emp.retailShopAssigned ?? ''} ${emp.email} ${emp.phone} employee staff`,
      });
    });

    // 8. Nhà cung cấp (Vendors)
    $vendors.forEach((v) => {
      // Interface Vendor chỉ có 'category' — nhóm hàng nhà cung cấp phụ trách.
      const typesStr = v.category || '';
      list.push({
        id: v.id,
        category: 'vendors',
        categoryLabelVi: 'Đối tác',
        categoryLabelEn: 'Vendor',
        titleVi: `${v.companyName} (${v.id})`,
        titleEn: `${v.companyName} (${v.id})`,
        subtitleVi: `Liên hệ: ${v.contactPerson} • SĐT: ${v.phone} • Cung cấp: ${typesStr}`,
        subtitleEn: `Contact: ${v.contactPerson} • Phone: ${v.phone} • Supplies: ${typesStr}`,
        badgeTextVi: 'Đối tác',
        badgeTextEn: 'Vendor',
        badgeColor: 'bg-slate-500/15 text-slate-700 dark:text-slate-300',
        targetPath: '/admin',
        targetTab: 'vendors',
        actionLabelVi: 'Đối tác',
        actionLabelEn: 'Vendor',
        icon: Truck,
        rawText: `${v.id} ${v.companyName} ${v.contactPerson} ${v.phone} ${v.email} ${v.address} ${typesStr} supplier vendor partner`,
      });
    });

    // 9. Ý kiến & Phản hồi (Feedbacks)
    $feedbacks.forEach((fb) => {
      list.push({
        id: fb.id,
        category: 'feedback',
        categoryLabelVi: 'Đánh giá',
        categoryLabelEn: 'Feedback',
        titleVi: `${fb.customerName} (${'★'.repeat(fb.rating)} - ${fb.category})`,
        titleEn: `${fb.customerName} (${'★'.repeat(fb.rating)} - ${fb.category})`,
        subtitleVi: `Mã TK: ${fb.accountId} • "${fb.message}"`,
        subtitleEn: `Account ID: ${fb.accountId} • "${fb.message}"`,
        badgeTextVi: `${fb.rating}★`,
        badgeTextEn: `${fb.rating}★`,
        badgeColor: 'bg-amber-500/15 text-amber-700 dark:text-amber-300',
        targetPath: '/admin',
        targetTab: 'feedback',
        actionLabelVi: 'Đánh giá',
        actionLabelEn: 'Feedback',
        icon: MessageSquare,
        rawText: `${fb.id} ${fb.customerName} ${fb.accountId} ${fb.category} ${fb.message} ${fb.response || ''} ${fb.respondedBy || ''} feedback review rating`,
      });
    });

    // 10. Điều hướng trang hệ thống
    systemPages.forEach((p) => list.push(p));

    return list;
  });

  // Filter items based on active search query across both languages
  const filteredItems = $derived.by<SearchItem[]>(() => {
    const rawQ = searchQuery.trim();
    if (!rawQ) return [];

    const normQ = normalizeVietnamese(rawQ);
    const tokens = normQ.split(/\s+/).filter(Boolean);
    if (tokens.length === 0) return [];

    // Match all tokens in the item's multilingual normalized text
    return allSearchItems.filter((item) => {
      const normItem = normalizeVietnamese(
        `${item.titleVi} ${item.titleEn} ${item.subtitleVi} ${item.subtitleEn} ${item.rawText} ${item.categoryLabelVi} ${item.categoryLabelEn}`
      );
      return tokens.every((token) => normItem.includes(token));
    });
  });

  // Navigate directly to item
  const handleSelectResult = (item: SearchItem) => {
    const targetTitle = $language === 'vi' ? item.titleVi : item.titleEn;
    if (item.targetPath) {
      if (item.targetTab) {
        navigateTo(item.targetPath, item.targetTab);
      } else {
        navigate(item.targetPath);
      }
      toast.success(
        $language === 'vi'
          ? `Đã chuyển tới: ${targetTitle}`
          : `Navigated to: ${targetTitle}`
      );
      isExpanded = false;
      searchQuery = '';
    }
  };
</script>

<svelte:window onclick={handleWindowClick} onkeydown={handleKeydown} />

<div class="relative flex items-center justify-end search-dropdown-container">
  <!-- Expandable Animated Search Input (Expands/slides to the left) -->
  <div
    class="flex items-center h-9 rounded-full transition-all duration-300 ease-out overflow-hidden shadow-xs border {isExpanded
      ? 'w-64 sm:w-80 md:w-96 bg-white dark:bg-[#101C29] border-sky-400 dark:border-sky-500 ring-2 ring-sky-500/20 px-1.5'
      : 'w-9 bg-[#EDF6FF] dark:bg-[#1E3349] border-[#CCE4F7] dark:border-[#253D56] hover:bg-[#DCEEFE] dark:hover:bg-[#253E58]'}"
  >
    <!-- Search Icon Button (Stays pinned, toggles expand) -->
    <button
      type="button"
      onclick={toggleExpand}
      class="shrink-0 h-7 w-7 rounded-full flex items-center justify-center text-[#1B2D40] dark:text-[#E0F1FF] hover:bg-sky-50 dark:hover:bg-[#1E3349] transition cursor-pointer"
      title={$language === 'vi' ? 'Tìm kiếm nhanh (Ctrl+K)' : 'Quick Search (Ctrl+K)'}
      aria-expanded={isExpanded}
    >
      <Search class="h-4 w-4 text-sky-600 dark:text-sky-400" />
    </button>

    <!-- Animated Input (Slides in from right to left) -->
    {#if isExpanded}
      <input
        bind:this={inputRef}
        type="text"
        placeholder={$language === 'vi' ? 'Nhập từ khóa tìm kiếm...' : 'Type to search anything...'}
        bind:value={searchQuery}
        class="bg-transparent border-none outline-none text-xs sm:text-sm text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-400 w-full px-2 animate-in fade-in slide-in-from-right-3 duration-200"
      />
      {#if searchQuery}
        <button
          type="button"
          onclick={() => {
            searchQuery = '';
            if (inputRef) inputRef.focus();
          }}
          class="shrink-0 p-1 ml-0.5 text-slate-400 hover:text-slate-600 dark:hover:text-white transition cursor-pointer"
          title={$language === 'vi' ? 'Xóa từ khóa' : 'Clear search'}
        >
          <X class="h-3.5 w-3.5" />
        </button>
      {/if}
    {/if}
  </div>

  <!-- Droplist Menu: ONLY appears when user is typing (searchQuery.trim().length > 0) -->
  {#if isExpanded && searchQuery.trim().length > 0}
    <div
      class="dropdown-popover absolute right-0 top-11 mt-1 w-80 sm:w-[440px] max-h-96 rounded-2xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-2xl z-50 overflow-hidden flex flex-col backdrop-blur-md animate-in fade-in zoom-in-95"
    >
      <!-- Results Header with Count & Language Switcher -->
      <div class="px-3.5 py-2.5 border-b border-[#CCE4F7] dark:border-[#253D56] bg-slate-50/70 dark:bg-[#1B2D40]/50 flex items-center justify-between text-xs font-bold text-[#0F1D2B] dark:text-white">
        <div class="flex items-center gap-1.5">
          <span>{$language === 'vi' ? 'Kết quả tìm kiếm' : 'Search Results'}</span>
          <span class="text-[11px] font-semibold text-sky-600 dark:text-sky-400">
            • {filteredItems.length} {$language === 'vi' ? 'mục phù hợp' : 'matches'}
          </span>
        </div>
        <div class="flex items-center gap-2">
          <!-- Quick Language Switcher in Dropdown Header -->
          <button
            type="button"
            onclick={(e) => {
              e.stopPropagation();
              toggleLanguage();
              if (inputRef) inputRef.focus();
            }}
            class="text-[10px] font-bold px-2 py-0.5 rounded-full border border-sky-300 dark:border-sky-700 bg-sky-50 dark:bg-sky-950/60 text-sky-700 dark:text-sky-300 hover:bg-sky-100 dark:hover:bg-sky-900 transition flex items-center gap-1 cursor-pointer active:scale-95"
            title={$language === 'vi' ? 'Chuyển đổi ngôn ngữ sang Tiếng Anh' : 'Switch language to Vietnamese'}
          >
            <Globe class="h-2.5 w-2.5 text-sky-600 dark:text-sky-400" />
            <span>{$language === 'vi' ? 'Tiếng Việt' : 'English'}</span>
          </button>
        </div>
      </div>

      <!-- Results Items List -->
      <div class="max-h-80 overflow-y-auto divide-y divide-slate-100 dark:divide-slate-800/60 notification-scroll p-1.5">
        {#if filteredItems.length === 0}
          <div class="py-7 text-center px-4 space-y-1.5">
            <Search class="h-6 w-6 mx-auto text-slate-300 dark:text-slate-600" />
            <p class="text-xs text-slate-500 dark:text-slate-400">
              {$language === 'vi'
                ? `Không có mục nào khớp với "${searchQuery}".`
                : `No items matched "${searchQuery}".`}
            </p>
          </div>
        {:else}
          {#each filteredItems as item (item.id)}
            <button
              type="button"
              onclick={() => handleSelectResult(item)}
              class="w-full text-left p-2.5 rounded-xl hover:bg-sky-50/70 dark:hover:bg-slate-800/60 transition flex items-center space-x-3 group cursor-pointer"
            >
              <!-- Icon -->
              <div class="h-8 w-8 rounded-lg bg-[#EDF6FF] dark:bg-[#1E3349] text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0 border border-[#CCE4F7] dark:border-[#253D56] group-hover:scale-105 transition-transform">
                <item.icon class="h-4 w-4" />
              </div>

              <!-- Content -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-1.5 mb-0.5">
                  <span class="text-[10px] font-bold px-1.5 py-0.2 rounded-full {item.badgeColor || 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300'}">
                    {$language === 'vi' ? item.categoryLabelVi : item.categoryLabelEn}
                  </span>
                  {#if ($language === 'vi' ? item.badgeTextVi : item.badgeTextEn)}
                    <span class="text-[10px] font-medium text-slate-500 dark:text-slate-400">
                      • {$language === 'vi' ? item.badgeTextVi : item.badgeTextEn}
                    </span>
                  {/if}
                </div>
                <div class="text-xs font-bold text-[#0F1D2B] dark:text-white truncate">
                  {$language === 'vi' ? item.titleVi : item.titleEn}
                </div>
                <div class="text-[11px] text-[#537292] dark:text-[#8DB0D4] truncate">
                  {$language === 'vi' ? item.subtitleVi : item.subtitleEn}
                </div>
              </div>

              <!-- Go Arrow -->
              <div class="shrink-0 text-sky-600 dark:text-sky-400 group-hover:translate-x-0.5 transition-transform">
                <ArrowRight class="h-3.5 w-3.5" />
              </div>
            </button>
          {/each}
        {/if}
      </div>

      <!-- Droplist Footer -->
      <div class="px-3.5 py-2 border-t border-[#CCE4F7] dark:border-[#253D56] bg-slate-50/70 dark:bg-[#1B2D40]/50 flex items-center justify-between text-[11px] text-[#537292] dark:text-[#8DB0D4]">
        <span>{$language === 'vi' ? 'Bấm để chuyển trang' : 'Click to navigate'}</span>
        <span class="text-[10px]">{$language === 'vi' ? 'ESC để thu nhỏ' : 'ESC to collapse'}</span>
      </div>
    </div>
  {/if}
</div>
