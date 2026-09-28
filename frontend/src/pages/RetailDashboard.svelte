<script lang="ts">
  // Mirrors pages/RetailDashboard.tsx of the React original.
  // NOTE: the React source declares duplicate navItems (hardcoded VI + i18n);
  // here we keep only the i18n entries (intended behavior).
  import { nexusStore } from "../context/NexusContext";
  import { authStore } from "../context/AuthContext";
  import { languageStore } from "../context/LanguageContext";
  import { searchOrdersAdvanced } from "../lib/technicalApi";
  import DashboardLayout from "../components/layout/DashboardLayout.svelte";
  import SettingsView from "../components/common/SettingsView.svelte";
  import ProfileView from "../components/common/ProfileView.svelte";
  import type { NavItem } from "../components/layout/DashboardLayout.svelte";
  import {
    ShoppingBag,
    Search,
    CheckCircle2,
    Clock,
    Wifi,
    Radio,
    Phone,
    FileText,
    CreditCard,
    User,
    Copy,
    Receipt,
    ArrowRight,
    Sparkles,
    Plus,
    Settings,
    X,
    ChevronDown,
    ChevronUp,
    Filter,
    AlertTriangle,
    XCircle,
    Server,
    HardDrive,
    Activity,
    RefreshCw,
    RotateCcw,
    ArrowDownUp,
    Wrench,
    UserPlus,
    UserCheck,
    HardHat,
    Navigation,
    MapPin,
    ShieldCheck,
  } from "lucide-svelte";
  import type {
    ConnectionType,
    ConnectionStatus,
    Connection,
    Order,
    Bill,
    PaymentRecord,
  } from "../types/nexus";
  import { getBulkDiscountPercent } from "../context/NexusContext";
  import { orderStatusLabel, orderStatusTone } from "../types/nexus";
  import { toast } from "svelte-sonner";
  import {
    isValidEmail,
    isValidPhone,
    isValidCccd,
    isValidPassport,
    isValidDriverLicense,
  } from "../lib/validation";
  import { queryParam, activeTabOverride } from "../lib/router";
  import {
    getPlanName,
    getPlanDescription,
    getPlanSpeedOrBandwidth,
  } from "../lib/planI18n";

  type RetailTab =
    | "new-order"
    | "approval-queue"
    | "order-tracking"
    | "connection-details"
    | "payment-records"
    | "settings"
    | "profile";

  const {
    plans,
    placeOrder,
    assignTechnicianToOrder,
    approveOrderByRetail,
    rejectOrderByRetail,
    resubmitOrderToRetail,
    orders,
    connections,
    bills,
    recordPayment,
    employees,
    retailShops,
  } = nexusStore;
  const { currentUser } = authStore;
  const { t, language } = languageStore;

  // Active navigation tab
  let activeTab = $state<RetailTab>("approval-queue");

  // Reactively respond to tab overrides from router / notifications
  $effect(() => {
    const override = $activeTabOverride;
    const validTabs: RetailTab[] = [
      "new-order",
      "approval-queue",
      "order-tracking",
      "connection-details",
      "payment-records",
      "settings",
      "profile",
    ];
    if (override && override.path === "/retail") {
      if (validTabs.includes(override.tab as RetailTab)) {
        activeTab = override.tab as RetailTab;
      }
    } else {
      const qTab = queryParam("tab");
      if (qTab && validTabs.includes(qTab as RetailTab)) {
        activeTab = qTab as RetailTab;
      }
    }
  });

  // FORM STATE: PLACE ORDER
  let customerName = $state("");
  let customerPhone = $state("");
  let customerEmail = $state("");
  let province = $state("");
  let district = $state("");
  let ward = $state("");
  let specificAddress = $state("");
  let installationAddress = $state("");
  let provincesList = $state<any[]>([]);
  let districtsList = $state<any[]>([]);
  let wardsList = $state<any[]>([]);

  let idProofType = $state<Order["idProofType"]>("National ID Card");
  let idProofNumber = $state("");
  let connectionType = $state<ConnectionType>("Broadband");
  let selectedPlanId = $state("");
  let showMapModal = $state(false);
  let mapSearchQuery = $state("");

  // Confirmation modal state for Place Order
  let isConfirmOrderModalOpen = $state(false);
  let isCreatingOrder = $state(false);

  const fullInstallationAddress = $derived(
    [specificAddress, ward, district, province].filter(Boolean).join(", ") ||
      installationAddress,
  );

  $effect(() => {
    fetch("https://esgoo.net/api-tinhthanh/1/0.htm")
      .then((r) => r.json())
      .then((d) => {
        if (d.error === 0) provincesList = d.data;
      })
      .catch((e) => console.error(e));
  });

  $effect(() => {
    if (province) {
      const p = provincesList.find(
        (x) => x.name === province || x.full_name === province,
      );
      if (p) {
        fetch(`https://esgoo.net/api-tinhthanh/2/${p.id}.htm`)
          .then((r) => r.json())
          .then((d) => {
            if (d.error === 0) districtsList = d.data;
          })
          .catch((e) => console.error(e));
      } else {
        districtsList = [];
      }
    } else {
      districtsList = [];
      district = "";
      ward = "";
    }
  });

  $effect(() => {
    if (district) {
      const d = districtsList.find(
        (x) => x.name === district || x.full_name === district,
      );
      if (d) {
        fetch(`https://esgoo.net/api-tinhthanh/3/${d.id}.htm`)
          .then((r) => r.json())
          .then((d) => {
            if (d.error === 0) wardsList = d.data;
          })
          .catch((e) => console.error(e));
      } else {
        wardsList = [];
      }
    } else {
      wardsList = [];
      ward = "";
    }
  });

  // Bulk / corporate scheme + Dial-Up existing landline (default 1 connection, adjustable for corporate)
  let bulkConnectionsCount = $state(1);
  let hasExistingLandline = $state(false);
  let existingLandlineAccountId = $state("");
  const bulkDiscountPercent = $derived(
    getBulkDiscountPercent(bulkConnectionsCount),
  );

  // Success Modal for newly generated Order
  let placedOrder = $state<Order | null>(null);

  // STATE: ORDER TRACKING (11-char Order ID) — matching Technical Registry pattern
  let orderSearchQuery = $state("");
  let orderStatusFilter = $state<"All" | Order["status"]>("All");
  let orderTypeFilter = $state<"All" | ConnectionType>("All");
  let orderBranchFilter = $state<"All" | string>("All");
  let orderSortOrder = $state<string>("newest");
  let expandedOrderId = $state<string | null>(null);

  // ---- STAGE 1: RETAIL APPROVAL QUEUE (xét duyệt hồ sơ tại chi nhánh) ----
  type ApprovalFilter = "Awaiting" | "Approved" | "Rejected" | "All";
  let approvalFilter = $state<ApprovalFilter>("All");
  let approvalSearch = $state("");
  let viewAllBranchesScope = $state(false);

  // ---- ADVANCED SEARCH (server-side, spec: tìm trạng thái đơn/kết nối theo
  // id, tên người đặt, loại kết nối, khoảng ngày, số điện thoại đăng ký) ----
  let showAdvancedSearch = $state(false);
  let advSearching = $state(false);
  let advResults = $state<Order[] | null>(null);
  const advForm = $state({
    orderId: "",
    customerName: "",
    phone: "",
    connectionType: "",
    fromDate: "",
    toDate: "",
  });

  const advActiveCriteriaCount = $derived(
    [advForm.orderId, advForm.customerName, advForm.phone, advForm.connectionType, advForm.fromDate, advForm.toDate]
      .filter((v) => v.trim()).length
  );

  async function runAdvancedSearch() {
    if (advActiveCriteriaCount === 0) {
      toast.error(
        $language === "vi"
          ? "Nhập ít nhất một tiêu chí tìm kiếm."
          : "Enter at least one search criterion."
      );
      return;
    }
    advSearching = true;
    try {
      const results = await searchOrdersAdvanced({
        keyword: advForm.orderId.trim() || undefined,
        customerName: advForm.customerName.trim() || undefined,
        phone: advForm.phone.trim() || undefined,
        connectionType: advForm.connectionType || undefined,
        fromDate: advForm.fromDate || undefined,
        toDate: advForm.toDate || undefined,
      });
      advResults = (results || []).map((o) => ({
        id: o.orderId,
        customerName: o.customerName,
        customerPhone: o.customerPhone || "",
        customerEmail: o.customerEmail || "",
        installationAddress: o.installationAddress || "",
        idProofType: "National ID Card" as const,
        idProofNumber: "",
        connectionType: o.connectionType,
        planId: o.planId,
        planName: o.planName,
        retailOutletCode: o.storeId,
        retailEmployeeName: "",
        createdAt: o.applicationDate ? o.applicationDate.slice(0, 16) : "",
        status: o.status,
        assignedAccountId: o.assignedAccountId || undefined,
        bulkConnectionsCount: o.bulkConnectionsCount ?? 1,
        bulkDiscountPercent: o.bulkDiscountPercent ?? 0,
      }));
      if (advResults.length === 0) {
        toast.info($language === "vi" ? "Không tìm thấy đơn hàng nào khớp tiêu chí." : "No orders match the criteria.");
      }
    } catch (err) {
      toast.error(
        $language === "vi"
          ? `Tìm kiếm thất bại: ${err instanceof Error ? err.message : "lỗi"}`
          : `Search failed: ${err instanceof Error ? err.message : "error"}`
      );
    } finally {
      advSearching = false;
    }
  }

  const statusLabel = (s: string): string => {
    const map: Record<string, { vi: string; en: string; cls: string }> = {
      PendingRetail: { vi: "Chờ duyệt", en: "Awaiting Retail", cls: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300" },
      "Not Approved": { vi: "Không duyệt", en: "Not Approved", cls: "bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300" },
      Pending: { vi: "Chờ khảo sát", en: "Pending Survey", cls: "bg-sky-100 text-sky-700 dark:bg-sky-900/30 dark:text-sky-300" },
      Feasible: { vi: "Khả thi", en: "Feasible", cls: "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300" },
      "Not Feasible": { vi: "Không khả thi", en: "Not Feasible", cls: "bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300" },
      "Connection Provided": { vi: "Đã cấp kết nối", en: "Connection Provided", cls: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300" },
    };
    const entry = map[s] ?? { vi: s, en: s, cls: "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300" };
    return $language === "vi" ? entry.vi : entry.en;
  };

  const statusChipClass = (s: string): string =>
    ({
      PendingRetail: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300",
      "Not Approved": "bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300",
      Pending: "bg-sky-100 text-sky-700 dark:bg-sky-900/30 dark:text-sky-300",
      Feasible: "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300",
      "Not Feasible": "bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300",
      "Connection Provided": "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300",
    })[s] ?? "bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300";


  // Rejection modal (a reason is mandatory before an application can be bounced).
  let isRejectModalOpen = $state(false);
  let orderToReject = $state<Order | null>(null);
  let rejectReason = $state("");

  // Manager/admin roles are not tied to one outlet and may see every branch.
  const canSeeAllBranches = $derived(
    $currentUser?.role === "admin" || $currentUser?.role === "technical",
  );

  // Extracts a shop code like "SH-01" out of free text such as
  // "Downtown Flagship (SH-01)" or "Retail Outlets (SH-01 Flagship)".
  const shopCodeFrom = (text?: string): string | null => {
    const m = (text ?? "").toUpperCase().match(/\b(SH-\d+)\b/);
    return m ? m[1] : null;
  };

  // Which branch does the signed-in staff belong to? Backend logins carry the real
  // EmployeeID (emp-02) but no store code, so resolve it from the employee directory
  // first and fall back to the demo session's department string.
  const activeBranchCode = $derived.by(() => {
    const user = $currentUser;
    if (!user || canSeeAllBranches) return null;
    if (user.branchCode)
      return shopCodeFrom(user.branchCode) ?? user.branchCode;

    const byId = $employees.find(
      (e) => e.id.toLowerCase() === (user.id ?? "").toLowerCase(),
    );
    const byEmail = byId
      ? undefined
      : $employees.find(
          (e) => e.email.toLowerCase() === (user.email ?? "").toLowerCase(),
        );
    const record = byId ?? byEmail;
    if (record) {
      const fromShop = shopCodeFrom(record.retailShopAssigned);
      if (fromShop) return fromShop;
    }
    return shopCodeFrom(user.department) ?? shopCodeFrom(user.title);
  });

  const activeBranchShop = $derived(
    activeBranchCode
      ? ($retailShops.find((s) => s.shopCode === activeBranchCode) ?? null)
      : null,
  );

  // Normalization helper: strips Vietnamese accents and lowercase for fast, accurate text matching
  const normalizeSearchText = (str?: string): string => {
    if (!str) return "";
    return str
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/[đĐ]/g, "d")
      .trim();
  };

  const orderMatchesApprovalSearch = (o: Order, query: string): boolean => {
    const rawQ = query.trim().toLowerCase();
    if (!rawQ) return true;

    const normQ = normalizeSearchText(rawQ);
    const qDigits = rawQ.replace(/\D/g, "");

    // 1. Order ID (case-insensitive & direct match)
    if (o.id.toLowerCase().includes(rawQ)) return true;
    if (o.assignedAccountId && o.assignedAccountId.toLowerCase().includes(rawQ)) return true;

    // 2. Customer Name (accented & unaccented, word-by-word)
    const normCustomer = normalizeSearchText(o.customerName);
    if (normCustomer.includes(normQ)) return true;
    const words = normQ.split(/\s+/).filter(Boolean);
    if (words.length > 1 && words.every((w) => normCustomer.includes(w))) return true;

    // 3. Customer Phone
    if (qDigits && o.customerPhone && o.customerPhone.replace(/\D/g, "").includes(qDigits)) return true;
    if (o.customerPhone && o.customerPhone.toLowerCase().includes(rawQ)) return true;

    // 4. Customer Email
    if (o.customerEmail && o.customerEmail.toLowerCase().includes(rawQ)) return true;

    // 5. ID Proof Number
    if (o.idProofNumber && o.idProofNumber.toLowerCase().includes(rawQ)) return true;
    if (qDigits && o.idProofNumber && o.idProofNumber.replace(/\D/g, "").includes(qDigits)) return true;

    // 6. Installation Address (accented & unaccented)
    const normAddress = normalizeSearchText(o.installationAddress);
    if (normAddress.includes(normQ)) return true;
    if (words.length > 1 && words.every((w) => normAddress.includes(w))) return true;

    // 7. Plan Name (raw + localized name + unaccented)
    if (o.planName && normalizeSearchText(o.planName).includes(normQ)) return true;
    const localizedPlan = getPlanName({ name: o.planName }, $language);
    if (localizedPlan && normalizeSearchText(localizedPlan).includes(normQ)) return true;

    // 8. Connection Type
    if (o.connectionType && normalizeSearchText(o.connectionType).includes(normQ)) return true;

    // 9. Outlet / Branch Code
    if (o.retailOutletCode && o.retailOutletCode.toLowerCase().includes(rawQ)) return true;

    // 10. Status label
    const statusText = orderStatusLabel(o.status, $language);
    if (statusText && normalizeSearchText(statusText).includes(normQ)) return true;

    // 11. Invoice / Bill match
    const billMatch = $bills.some((b) => {
      const isCustomerBill =
        (b.accountId && o.assignedAccountId && b.accountId === o.assignedAccountId) ||
        normalizeSearchText(b.customerName) === normCustomer;
      if (!isCustomerBill) return false;
      return (
        b.invoiceNumber.toLowerCase().includes(rawQ) ||
        b.id.toLowerCase().includes(rawQ) ||
        (qDigits && b.invoiceNumber.replace(/\D/g, "").includes(qDigits))
      );
    });
    if (billMatch) return true;

    return false;
  };

  // Every application that belongs to this desk: matched by the routed employee id,
  // by branch code, or (for self-service sign-ups) by the branch the customer picked.
  // When an employee searches for a customer name or order ID, allow matching across all branches.
  const branchScopedOrders = $derived(
    $orders.filter((o) => {
      if (canSeeAllBranches || viewAllBranchesScope || !activeBranchCode)
        return true;
      const q = approvalSearch.trim();
      if (q && orderMatchesApprovalSearch(o, q)) {
        return true;
      }
      const routed = o.assignedEmployeeId
        ? o.assignedEmployeeId.toLowerCase() ===
          ($currentUser?.id ?? "").toLowerCase()
        : false;
      return routed || o.retailOutletCode?.toUpperCase() === activeBranchCode;
    }),
  );

  const searchedApprovalOrders = $derived(
    branchScopedOrders.filter((o) =>
      orderMatchesApprovalSearch(o, approvalSearch),
    ),
  );

  const awaitingApprovalCount = $derived(
    searchedApprovalOrders.filter((o) => o.status === "PendingRetail").length,
  );
  const approvedByBranchCount = $derived(
    searchedApprovalOrders.filter(
      (o) => o.retailApprovedBy && o.status !== "Not Approved",
    ).length,
  );
  const rejectedByBranchCount = $derived(
    searchedApprovalOrders.filter((o) => o.status === "Not Approved").length,
  );
  const allApprovalOrdersCount = $derived(searchedApprovalOrders.length);

  const approvalQueueOrders = $derived(
    searchedApprovalOrders.filter((o) => {
      const bucket =
        o.status === "PendingRetail"
          ? "Awaiting"
          : o.status === "Not Approved"
            ? "Rejected"
            : "Approved";
      return approvalFilter === "All" || approvalFilter === bucket;
    }),
  );

  const handleApproveOrder = async (order: Order) => {
    const who = $currentUser?.name || order.retailEmployeeName || "Retail Desk";
    const updated = await approveOrderByRetail(order.id, who);
    if (!updated) {
      toast.error(
        $language === "vi"
          ? "Đơn này không còn ở hàng đợi chờ duyệt."
          : "This order is no longer in the approval queue.",
      );
      return;
    }
    toast.success(
      $language === "vi"
        ? `Đã duyệt hồ sơ ${order.id} — chuyển sang hàng đợi khảo sát kỹ thuật.`
        : `Approved ${order.id} — released to the technical feasibility queue.`,
    );
  };

  const handleOpenRejectModal = (order: Order) => {
    orderToReject = order;
    rejectReason = "";
    isRejectModalOpen = true;
  };

  const handleConfirmReject = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!orderToReject) return;
    if (!rejectReason.trim()) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập lý do trả hồ sơ."
          : "A reason is required when bouncing an application.",
      );
      return;
    }
    // Giữ lại đơn vào biến cục bộ: sau await, orderToReject có thể đã bị đặt lại null
    // (người dùng đóng hộp thoại hoặc mở đơn khác trong lúc chờ API).
    const target = orderToReject;
    const who =
      $currentUser?.name || target.retailEmployeeName || "Retail Desk";
    const rejected = await rejectOrderByRetail(target.id, who, rejectReason);
    if (!rejected) {
      toast.error(
        $language === "vi"
          ? "Đơn này không còn ở hàng đợi chờ duyệt."
          : "This order is no longer in the approval queue.",
      );
    } else {
      toast.error(
        $language === "vi"
          ? `Đã trả hồ sơ ${target.id} cho khách hàng.`
          : `Returned ${target.id} to the customer.`,
      );
    }
    isRejectModalOpen = false;
    orderToReject = null;
    rejectReason = "";
  };

  const handleResubmitOrder = async (order: Order) => {
    const updated = await resubmitOrderToRetail(order.id);
    if (updated) {
      toast.success(
        $language === "vi"
          ? `Đã ghi nhận lại hồ sơ ${order.id} vào hàng đợi chờ duyệt.`
          : `Re-queued ${order.id} for approval.`,
      );
    }
  };

  // Pill colours read straight from the template (no {@const} needed inside <td>).
  const statusChip = (s: Order["status"]) => orderStatusTone(s).chip;
  const statusDot = (s: Order["status"]) => orderStatusTone(s).dot;

  // Source orders for Order Tracking: respects branch scope filter or shows all branches by default
  const trackingSourceOrders = $derived(
    orderBranchFilter === "All"
      ? $orders
      : $orders.filter(
          (o) =>
            o.retailOutletCode?.toUpperCase() ===
              orderBranchFilter.toUpperCase() ||
            (activeBranchCode &&
              o.retailOutletCode?.toUpperCase() ===
                activeBranchCode.toUpperCase()),
        ),
  );

  // Latest orders for quick-pick tags: ALWAYS computed from all orders, sorted newest first!
  const latestOrderTags = $derived(
    [...$orders]
      .sort((a, b) => {
        const timeA = a.createdAt || "";
        const timeB = b.createdAt || "";
        return timeB.localeCompare(timeA) || b.id.localeCompare(a.id);
      })
      .slice(0, 10),
  );

  // Dynamic counts for Connection Types (computed from trackingSourceOrders)
  const connectionTypeCounts = $derived({
    All: trackingSourceOrders.length,
    Broadband: trackingSourceOrders.filter(
      (o) =>
        o.connectionType === "Broadband" ||
        o.id.toUpperCase().startsWith("B") ||
        o.connectionType?.toLowerCase().includes("broadband"),
    ).length,
    Landline: trackingSourceOrders.filter(
      (o) =>
        o.connectionType === "Landline" ||
        o.id.toUpperCase().startsWith("T") ||
        o.connectionType?.toLowerCase().includes("landline") ||
        o.connectionType?.toLowerCase().includes("phone"),
    ).length,
    "Dial-Up": trackingSourceOrders.filter(
      (o) =>
        o.connectionType === "Dial-Up" ||
        o.id.toUpperCase().startsWith("D") ||
        o.connectionType?.toLowerCase().includes("dial"),
    ).length,
  });

  // Reset all filters in Order Tracking (used when clicking "Tất cả" / "Làm mới")
  const handleResetAllOrderFilters = () => {
    orderSearchQuery = "";
    orderStatusFilter = "All";
    orderTypeFilter = "All";
    orderBranchFilter = "All";
    orderSortOrder = "newest";
    expandedOrderId = null;
    toast.info(
      $language === "vi"
        ? "Đã hiển thị toàn bộ danh sách đơn hàng"
        : "Showing all orders in registry",
    );
  };

  // Handle change of sort/status select dropdown
  const handleOrderSortChange = (value: string) => {
    orderSortOrder = value;
    if (value === "newest" || value === "oldest") {
      orderStatusFilter = "All";
    } else {
      orderStatusFilter = value as Order["status"];
    }
  };

  // Toggle or select a quick-pick latest order tag
  const handleToggleOrderTag = (orderId: string) => {
    if (orderSearchQuery.trim().toUpperCase() === orderId.toUpperCase()) {
      // Toggle off -> show all
      orderSearchQuery = "";
      expandedOrderId = null;
      toast.info(
        $language === "vi"
          ? "Đã bỏ chọn mã đơn, hiển thị tất cả"
          : "Deselected order tag, showing all",
      );
    } else {
      // Set to clicked order and expand it
      orderSearchQuery = orderId;
      expandedOrderId = orderId;
      // Clear status/type filters that could hide this order
      orderStatusFilter = "All";
      orderTypeFilter = "All";
      orderBranchFilter = "All";
    }
  };

  // When connection type filter changes, if search query was an exact order ID of another type, clear it
  const handleConnectionTypeChange = (newType: "All" | ConnectionType) => {
    orderTypeFilter = newType;
    if (orderSearchQuery) {
      const qUpper = orderSearchQuery.trim().toUpperCase();
      if (
        (newType === "Broadband" && !qUpper.startsWith("B")) ||
        (newType === "Dial-Up" && !qUpper.startsWith("D")) ||
        (newType === "Landline" && !qUpper.startsWith("T"))
      ) {
        orderSearchQuery = "";
      }
    }
  };

  const filteredTrackedOrders = $derived(
    trackingSourceOrders
      .filter((o) => {
        const q = orderSearchQuery.trim().toLowerCase();
        const matchesSearch =
          !q ||
          o.id.toLowerCase().includes(q) ||
          o.customerName.toLowerCase().includes(q) ||
          o.customerPhone.replace(/\D/g, "").includes(q.replace(/\D/g, "")) ||
          o.customerEmail.toLowerCase().includes(q) ||
          o.installationAddress.toLowerCase().includes(q) ||
          (o.assignedAccountId &&
            o.assignedAccountId.toLowerCase().includes(q));

        const matchesStatus =
          orderStatusFilter === "All" || o.status === orderStatusFilter;

        const matchesType =
          orderTypeFilter === "All" ||
          o.connectionType === orderTypeFilter ||
          (orderTypeFilter === "Broadband" &&
            (o.id.toUpperCase().startsWith("B") ||
              o.connectionType?.toLowerCase().includes("broadband"))) ||
          (orderTypeFilter === "Dial-Up" &&
            (o.id.toUpperCase().startsWith("D") ||
              o.connectionType?.toLowerCase().includes("dial"))) ||
          (orderTypeFilter === "Landline" &&
            (o.id.toUpperCase().startsWith("T") ||
              o.connectionType?.toLowerCase().includes("landline") ||
              o.connectionType?.toLowerCase().includes("phone")));

        return matchesSearch && matchesStatus && matchesType;
      })
      .sort((a, b) => {
        const timeA = a.createdAt || "";
        const timeB = b.createdAt || "";
        if (orderSortOrder === "oldest") {
          return timeA.localeCompare(timeB) || a.id.localeCompare(b.id);
        } else {
          return timeB.localeCompare(timeA) || b.id.localeCompare(a.id);
        }
      }),
  );

  const toggleOrderDropdown = (orderId: string) => {
    if (expandedOrderId === orderId) {
      expandedOrderId = null;
    } else {
      expandedOrderId = orderId;
    }
  };

  // ==================== PHÂN CÔNG KỸ THUẬT VIÊN KHẢO SÁT ====================
  let isAssignTechModalOpen = $state(false);
  let targetOrderForTechAssign = $state<Order | null>(null);
  let selectedTechnicianId = $state("");
  let assignSurveyNotes = $state("");
  let techSearchQuery = $state("");

  // Danh sách kỹ thuật viên hiện trường khả dụng (Lọc từ $employees)
  const availableTechnicians = $derived(
    $employees.filter(
      (e) =>
        (e.role === "Field Engineer" ||
          e.department === "Technical Operations") &&
        e.status === "Active",
    ),
  );

  const filteredAvailableTechnicians = $derived(
    availableTechnicians.filter((tech) => {
      if (!techSearchQuery.trim()) return true;
      const q = techSearchQuery.toLowerCase();
      return (
        tech.name.toLowerCase().includes(q) ||
        tech.employeeCode.toLowerCase().includes(q) ||
        (tech.retailShopAssigned &&
          tech.retailShopAssigned.toLowerCase().includes(q))
      );
    }),
  );

  const getTechnicianPendingCount = (techName: string): number => {
    return $orders.filter(
      (o) =>
        o.assignedTechnician === techName &&
        (o.status === "Pending" || o.status === "PendingRetail"),
    ).length;
  };

  const openAssignTechnicianModal = (order: Order) => {
    targetOrderForTechAssign = order;
    if (order.assignedTechnicianId) {
      selectedTechnicianId = order.assignedTechnicianId;
    } else if (order.assignedTechnician) {
      const matched = availableTechnicians.find(
        (t) => t.name === order.assignedTechnician,
      );
      selectedTechnicianId = matched
        ? matched.id
        : availableTechnicians[0]?.id || "";
    } else {
      // Ưu tiên chọn KTV thuộc chi nhánh của đơn hàng nếu có
      const branchTech = availableTechnicians.find(
        (t) =>
          t.retailShopAssigned &&
          t.retailShopAssigned.includes(order.retailOutletCode),
      );
      selectedTechnicianId = branchTech
        ? branchTech.id
        : availableTechnicians[0]?.id || "";
    }
    assignSurveyNotes = "";
    techSearchQuery = "";
    isAssignTechModalOpen = true;
  };

  const handleConfirmAssignTechnician = async () => {
    if (!targetOrderForTechAssign || !selectedTechnicianId) {
      toast.error(
        $language === "vi"
          ? "Vui lòng chọn kỹ thuật viên khảo sát!"
          : "Please select a survey technician!",
      );
      return;
    }
    const tech = availableTechnicians.find(
      (t) => t.id === selectedTechnicianId,
    );
    if (!tech) return;

    // Chốt đơn đang xử lý trước khi gọi API, vì sau await biến state có thể đã đổi.
    const target = targetOrderForTechAssign;

    const res = await assignTechnicianToOrder(
      target.id,
      tech.name,
      tech.id,
      tech.phone,
      assignSurveyNotes.trim() || undefined,
    );

    if (res) {
      toast.success(
        $language === "vi"
          ? `Đã phân công kỹ thuật viên ${tech.name} (${tech.employeeCode}) đảm nhiệm khảo sát đơn ${target.id}!`
          : `Assigned technician ${tech.name} (${tech.employeeCode}) for order ${target.id}!`,
      );
      isAssignTechModalOpen = false;
      targetOrderForTechAssign = null;
    }
  };

  const handleQuickTrackOrder = () => {
    const q = orderSearchQuery.trim().toLowerCase();
    if (!q) {
      toast.info(
        $language === "vi"
          ? "Vui lòng nhập Mã đơn hoặc tên khách hàng."
          : "Please enter an Order ID or customer name.",
      );
      return;
    }
    const found = $orders.find(
      (o) =>
        o.id.toLowerCase().includes(q) ||
        o.customerName.toLowerCase().includes(q),
    );
    if (found) {
      expandedOrderId = found.id;
      toast.success(
        $language === "vi"
          ? `Đã mở chi tiết đơn hàng #${found.id}`
          : `Opened details for Order #${found.id}`,
      );
    } else {
      toast.error(
        $language === "vi"
          ? "Không tìm thấy đơn hàng phù hợp."
          : "Order not found in registry.",
      );
    }
  };

  // STATE: CONNECTION DETAILS (16-char Account ID) — matching Technical Registry pattern
  let connSearchQuery = $state("");
  let connStatusFilter = $state<"All" | string>("All");
  let connTypeFilter = $state<"All" | string>("All");
  let expandedConnAccountId = $state<string | null>("T064-000000000001");

  const filteredTrackedConnections = $derived(
    $connections.filter((c) => {
      const q = connSearchQuery.trim().toLowerCase();
      const cleanQ = q.replace(/-/g, "");
      const matchesSearch =
        !q ||
        c.accountId.toLowerCase().includes(q) ||
        c.accountId.replace(/-/g, "").toLowerCase().includes(cleanQ) ||
        c.customerName.toLowerCase().includes(q) ||
        c.customerPhone.replace(/\D/g, "").includes(q.replace(/\D/g, "")) ||
        c.customerEmail.toLowerCase().includes(q) ||
        c.installationAddress.toLowerCase().includes(q) ||
        (c.portNumber && c.portNumber.toLowerCase().includes(q)) ||
        (c.assignedDeviceSerial &&
          c.assignedDeviceSerial.toLowerCase().includes(q)) ||
        (c.ipAddress && c.ipAddress.toLowerCase().includes(q));
      const matchesStatus =
        connStatusFilter === "All" || c.status === connStatusFilter;
      const matchesType =
        connTypeFilter === "All" || c.connectionType === connTypeFilter;
      return matchesSearch && matchesStatus && matchesType;
    }),
  );

  const toggleConnDropdown = (accountId: string) => {
    if (expandedConnAccountId === accountId) {
      expandedConnAccountId = null;
    } else {
      expandedConnAccountId = accountId;
    }
  };

  const handleQuickTrackConnection = () => {
    const q = connSearchQuery.trim().replace(/-/g, "").toLowerCase();
    if (!q) {
      toast.info(
        $language === "vi"
          ? "Vui lòng nhập Mã tài khoản hoặc tên khách hàng."
          : "Please enter an Account ID or customer name.",
      );
      return;
    }
    const found = $connections.find(
      (c) =>
        c.accountId.replace(/-/g, "").toLowerCase().includes(q) ||
        c.customerName
          .toLowerCase()
          .includes(connSearchQuery.trim().toLowerCase()),
    );
    if (found) {
      expandedConnAccountId = found.accountId;
      toast.success(
        $language === "vi"
          ? `Đã mở chi tiết hồ sơ tài khoản #${found.accountId}`
          : `Retrieved connection profile #${found.accountId}`,
      );
    } else {
      toast.error(
        $language === "vi"
          ? "Không tìm thấy Mã tài khoản trong hệ thống."
          : "Account ID not found in connection registry.",
      );
    }
  };

  // STATE: PAYMENT RECORDS SEARCH
  let paymentAccountQuery = $state("");

  // Payment Modal State for Retail Counter (Cập nhật lịch sử thanh toán vào hệ thống)
  let isPaymentModalOpen = $state(false);
  let targetBillForPayment = $state<Bill | null>(null);
  let retailPaymentAmount = $state(0);
  let retailPaymentMode = $state<PaymentRecord["paymentMode"]>("Cash");
  let retailPaymentRef = $state("");

  const handleOpenRetailPayment = (bill: Bill) => {
    targetBillForPayment = bill;
    retailPaymentAmount = bill.dueAmount;
    retailPaymentRef = `REC-POS-${Date.now().toString().slice(-6)}`;
    isPaymentModalOpen = true;
  };

  const handleConfirmRetailPayment = (e: SubmitEvent) => {
    e.preventDefault();
    if (!targetBillForPayment) return;
    if (retailPaymentAmount <= 0) {
      toast.error(
        $language === "vi"
          ? "Số tiền thanh toán phải lớn hơn 0."
          : "Payment amount must be greater than zero.",
      );
      return;
    }

    const updated = recordPayment(
      targetBillForPayment.invoiceNumber,
      retailPaymentAmount,
      retailPaymentMode,
      retailPaymentRef.trim() || `REC-POS-${Date.now().toString().slice(-6)}`,
      "Retail Counter Cashier",
    );

    if (updated) {
      toast.success(
        $language === "vi"
          ? `Đã cập nhật thanh toán $${retailPaymentAmount.toFixed(2)} cho hóa đơn ${updated.invoiceNumber}. Còn nợ: $${updated.dueAmount.toFixed(2)}`
          : `Payment of $${retailPaymentAmount.toFixed(2)} recorded for ${updated.invoiceNumber}. Remaining: $${updated.dueAmount.toFixed(2)}`,
      );
      isPaymentModalOpen = false;
      targetBillForPayment = null;
    }
  };

  // Due amount and bills helpers for connection search (Spec §9)
  const getConnectionDueAmount = (accId: string): number => {
    if (!accId) return 0;
    const cleanId = accId.replace(/-/g, "").toUpperCase();
    return $bills
      .filter((b) => b.accountId.replace(/-/g, "").toUpperCase() === cleanId)
      .reduce((sum, b) => sum + (b.dueAmount || 0), 0);
  };

  const totalDueAmountAllConnections = $derived(
    $connections.reduce(
      (sum, c) => sum + getConnectionDueAmount(c.accountId),
      0,
    ),
  );

  const getBillsForConnection = (accId: string): Bill[] => {
    if (!accId) return [];
    const cleanId = accId.replace(/-/g, "").toUpperCase();
    return $bills.filter(
      (b) => b.accountId.replace(/-/g, "").toUpperCase() === cleanId,
    );
  };

  // Filter available plans according to selected Connection Type
  const availablePlans = $derived(
    $plans.filter((p) => p.type === connectionType && p.status === "Active"),
  );
  const currentPlan = $derived(
    $plans.find((p) => p.id === selectedPlanId) || availablePlans[0],
  );

  // Default the plan to the first one available for the chosen connection type.
  $effect(() => {
    if (!availablePlans.some((p) => p.id === selectedPlanId)) {
      selectedPlanId = availablePlans[0]?.id ?? "";
    }
  });

  // FORM SUBMISSION LOGIC: PLACE ORDER
  const handlePlaceOrderSubmit = (e: SubmitEvent) => {
    e.preventDefault();

    if (
      !customerName.trim() ||
      !customerPhone.trim() ||
      !customerEmail.trim() ||
      (!specificAddress.trim() && !installationAddress.trim())
    ) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập đầy đủ thông tin khách hàng (họ tên, SĐT, email) và địa chỉ lắp đặt."
          : "Please enter all required customer information (name, phone, email) and installation address."
      );
      return;
    }

    if (!isValidPhone(customerPhone)) {
      toast.error(
        $language === "vi"
          ? "Số điện thoại phải gồm đúng 10 chữ số."
          : "Customer phone must contain exactly 10 digits."
      );
      return;
    }

    if (!customerEmail.trim()) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập địa chỉ email của khách hàng."
          : "Please enter customer email address."
      );
      return;
    }

    if (!isValidEmail(customerEmail)) {
      toast.error(
        $language === "vi"
          ? "Email phải chứa ký tự @"
          : "Email must contain @ symbol."
      );
      return;
    }

    if (!idProofNumber.trim()) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập số giấy tờ tùy thân xác thực."
          : "Please enter customer ID proof verification number."
      );
      return;
    }

    if (idProofType === "National ID Card" && !isValidCccd(idProofNumber)) {
      toast.error(
        $language === "vi"
          ? "Số CCCD phải gồm đúng 12 chữ số."
          : "National ID number must contain exactly 12 digits."
      );
      return;
    }

    if (idProofType === "Passport" && !isValidPassport(idProofNumber)) {
      toast.error(
        $language === "vi"
          ? "Số Hộ chiếu không hợp lệ (7-9 ký tự chữ và số)."
          : "Invalid Passport number (7-9 alphanumeric characters)."
      );
      return;
    }

    if (
      idProofType === "Driver's License" &&
      !isValidDriverLicense(idProofNumber)
    ) {
      toast.error(
        $language === "vi"
          ? "Số Bằng lái xe phải gồm đúng 12 chữ số."
          : "Driver's License must contain exactly 12 digits."
      );
      return;
    }

    if (!currentPlan) {
      toast.error(
        $language === "vi"
          ? "Vui lòng chọn một gói cước dịch vụ."
          : "Please select an active service plan."
      );
      return;
    }

    if (connectionType === "Dial-Up" && (!hasExistingLandline || !existingLandlineAccountId.trim().startsWith("T"))) {
      toast.error(
        $language === "vi"
          ? "Bắt buộc phải cung cấp mã tài khoản Landline (bắt đầu bằng chữ T) để đăng ký mạng Dial-Up!"
          : "An existing Landline account ID (starting with 'T') is compulsory to register for Dial-Up internet!"
      );
      return;
    }

    // Open Confirmation modal before issuing order ID
    isConfirmOrderModalOpen = true;
  };

  const executeConfirmOrder = async () => {
    if (!currentPlan || isCreatingOrder) return;
    isCreatingOrder = true;

    try {
      const finalAddress = fullInstallationAddress.trim();
      const newOrder = await placeOrder({
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim(),
        customerEmail: customerEmail.trim(),
        installationAddress: finalAddress,
        idProofType,
        idProofNumber: idProofNumber.trim(),
        connectionType,
        planId: currentPlan.id,
        planName: currentPlan.name,
        // Route to the desk the clerk actually works at; fall back to the flagship.
        retailOutletCode: activeBranchCode ?? "SH-01",
        retailEmployeeName: $currentUser?.name || "David Chen",
        bulkConnectionsCount: Math.max(1, bulkConnectionsCount || 1),
        ...(connectionType === "Dial-Up" &&
        hasExistingLandline &&
        existingLandlineAccountId.trim()
          ? { existingLandlineAccountId: existingLandlineAccountId.trim() }
          : {}),
      });

      isConfirmOrderModalOpen = false;
      placedOrder = newOrder;
      toast.success(
        $language === "vi"
          ? `Tạo đơn hàng ${newOrder.id} thành công!`
          : `Order ${newOrder.id} successfully created!`
      );

      // Reset Form for next retail customer
      customerName = "";
      customerPhone = "";
      customerEmail = "";
      province = "";
      district = "";
      ward = "";
      specificAddress = "";
      installationAddress = "";
      idProofNumber = "";
      bulkConnectionsCount = 1;
      hasExistingLandline = false;
      existingLandlineAccountId = "";
    } catch (err: any) {
      console.error(err);
      toast.error(
        err?.message ||
          ($language === "vi"
            ? "Có lỗi xảy ra khi tạo đơn hàng."
            : "Failed to create order.")
      );
    } finally {
      isCreatingOrder = false;
    }
  };

  // ORDER TRACKING SEARCH HANDLER
  const handleSearchOrder = (idToSearch?: string) => {
    const targetId = (idToSearch || orderSearchQuery).trim().toUpperCase();
    const found = $orders.find((o) => o.id.toUpperCase() === targetId);
    if (found) {
      orderSearchQuery = found.id;
      expandedOrderId = found.id;
      toast.success(`Found Order ${found.id}`);
    } else {
      toast.error(
        `No order found matching ID "${targetId}". Must be an 11-character ID (e.g. D0000000001)`,
      );
    }
  };

  // CONNECTION DETAILS SEARCH HANDLER
  const handleSearchConnection = (idToSearch?: string) => {
    const targetId = (idToSearch || connSearchQuery).trim();
    const cleanTarget = targetId.replace(/-/g, "").toUpperCase();
    const found = $connections.find(
      (c) => c.accountId.replace(/-/g, "").toUpperCase() === cleanTarget,
    );
    if (found) {
      connSearchQuery = found.accountId;
      expandedConnAccountId = found.accountId;
      toast.success(`Retrieved Account ${found.accountId}`);
    } else {
      toast.error(`No connection found for Account ID "${targetId}"`);
    }
  };

  const copyToClipboard = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    toast.success(`Copied ${label}: ${text}`);
  };

  // Calculate order progress index across the two-stage pipeline. The value is the
  // highest stepper step that is COMPLETE (1-based, see stepperStages).
  //   PendingRetail -> 1 (logged, waiting on the branch desk)
  //   Pending       -> 2 (retail approved, waiting on Technical)
  //   Feasible      -> 3 (surveyed feasible)
  //   Conn. Provided-> 5 (dispatched + live)
  // Rejected at either stage returns -1.
  const getStageIndex = (status: Order["status"]) => {
    switch (status) {
      case "PendingRetail":
        return 1;
      case "Not Approved":
        return -1;
      case "Pending":
        return 2;
      case "Feasible":
        return 3;
      case "Connection Provided":
        return 5;
      case "Not Feasible":
        return -1;
      default:
        return 1;
    }
  };

  const selectConnectionType = (type: ConnectionType) => {
    connectionType = type;
    // Auto select first plan of this type
    const firstPlan = $plans.find((p) => p.type === type);
    if (firstPlan) selectedPlanId = firstPlan.id;
  };

  const fillDemoCustomer = () => {
    customerName = "Eleanor Vance";
    customerPhone = "+1 (555) 712-4490";
    customerEmail = "eleanor.vance@brooklynart.org";
    installationAddress = "240 Bedford Ave, Apt 3A, Williamsburg, NY 11211";
    idProofType = "National ID Card";
    idProofNumber = "ID-NY-9920194";
    connectionType = "Broadband";
    selectedPlanId =
      $plans.find((p) => p.type === "Broadband" && p.status === "Active")?.id ??
      "";
    toast.info("Form pre-filled with demo walk-in customer data");
  };

  const trackThisOrder = () => {
    const id = placedOrder!.id;
    placedOrder = null;
    activeTab = "order-tracking";
    orderSearchQuery = id;
    expandedOrderId = id;
  };

  const connectionTypeCards: {
    type: ConnectionType;
    icon: typeof Wifi;
    desc: string;
    descVi: string;
    labelVi: string;
  }[] = [
    {
      type: "Broadband",
      icon: Wifi,
      desc: "High-Speed Fiber FTTH",
      descVi: "Cáp quang tốc độ cao FTTH",
      labelVi: "Cáp quang (Broadband)",
    },
    {
      type: "Dial-Up",
      icon: Radio,
      desc: "56k Analog PSTN Modem",
      descVi: "Modem quay số tương tự 56k",
      labelVi: "Quay số (Dial-Up)",
    },
    {
      type: "Landline",
      icon: Phone,
      desc: "Fixed Voice / VoIP Phone",
      descVi: "Điện thoại cố định / Thoại VoIP",
      labelVi: "Cố định (Landline)",
    },
  ];

  const stepperStages = [
    {
      step: 1,
      label: "Order Logged",
      labelVi: "Đã ghi nhận đơn",
      sub: "Retail Counter",
      subVi: "Quầy bán lẻ",
    },
    {
      step: 2,
      label: "Retail Approved",
      labelVi: "Bán hàng duyệt hồ sơ",
      sub: "Branch Desk",
      subVi: "Chi nhánh phụ trách",
    },
    {
      step: 3,
      label: "Feasibility Checked",
      labelVi: "Kỹ thuật khảo sát khả thi",
      sub: "Field Telemetry",
      subVi: "Đo lường hiện trường",
    },
    {
      step: 4,
      label: "Tech Dispatch",
      labelVi: "Điều phối kỹ thuật",
      sub: "CPE & Port Bind",
      subVi: "Gắn cổng & CPE",
    },
    {
      step: 5,
      label: "Connection Live",
      labelVi: "Đường truyền hoạt động",
      sub: "16-char Account",
      subVi: "Mã tài khoản 16 ký tự",
    },
  ];

  const retailNavItems: NavItem[] = $derived([
    { id: "new-order", label: $t.retailNav.newOrder, icon: ShoppingBag },
    {
      id: "approval-queue",
      label:
        $language === "vi" ? "Duyệt hồ sơ chi nhánh" : "Branch Approval Queue",
      icon: FileText,
      badge: awaitingApprovalCount,
      badgeColor: "bg-amber-100 text-amber-900",
    },
    {
      id: "order-tracking",
      label: $t.retailNav.orderTracking,
      icon: Clock,
      badge: branchScopedOrders.length,
    },
    {
      id: "connection-details",
      label: $t.retailNav.connectionDetails,
      icon: Wifi,
      badge: $connections.length,
    },
    {
      id: "payment-records",
      label: $t.retailNav.paymentRecords,
      icon: CreditCard,
      badge: $bills.length,
    },
    { id: "settings", label: $t.retailNav.settings, icon: Settings },
  ]);
</script>

<DashboardLayout
  {activeTab}
  onTabChange={(tab) => {
    activeTab = tab as RetailTab;
    isRejectModalOpen = false;
    isAssignTechModalOpen = false;
    isPaymentModalOpen = false;
    expandedOrderId = null;
    expandedConnAccountId = null;
  }}
  navItems={retailNavItems}
  roleBadgeTitle={$t.roles.retail}
  pageTitle={retailNavItems.find((n) => n.id === activeTab)?.label}
  primaryAction={{
    label: $t.actions.newOrder,
    onClick: () => (activeTab = "new-order"),
    icon: Plus,
  }}
>
  <!-- TAB 1: PLACE ORDER FORM -->
  {#if activeTab === "new-order"}
    <div class="tab-content-animate w-full">
      <!-- Main Place Order Form -->
      <form
        onsubmit={handlePlaceOrderSubmit}
        class="w-full space-y-6 bg-white dark:bg-slate-900 p-6 sm:p-8 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm"
      >
        <!-- SECTION A: Customer Personal Details -->
        <div>
          <div
            class="flex items-center space-x-2 border-b border-slate-100 dark:border-slate-800 pb-2 mb-4"
          >
            <User class="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
            <h3
              class="font-semibold text-sm uppercase tracking-wider text-slate-900 dark:text-white"
            >
              {$language === "vi"
                ? "Bước 1: Thông tin cá nhân khách hàng"
                : "Step 1: Customer Personal Details"}
            </h3>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Họ và tên đầy đủ *"
                  : "Full Legal Name *"}
              </label>
              <input
                type="text"
                required
                placeholder={$language === "vi"
                  ? "Ví dụ: Nguyễn Văn Nam"
                  : "e.g. Julian Thorne"}
                bind:value={customerName}
                class="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Số điện thoại liên hệ *"
                  : "Contact Phone *"}
              </label>
              <input
                type="tel"
                required
                
                maxlength="10"
                title={$language === 'vi' ? 'Số điện thoại phải gồm 10 chữ số' : 'Phone number must contain 10 digits'}
                placeholder={$language === "vi"
                  ? "0912345678"
                  : "0912345678"}
                bind:value={customerPhone}
                class="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Địa chỉ Email *"
                  : "Email Address *"}
              </label>
              <input
                type="email"
                required
                pattern=".*@.*"
                title={$language === 'vi' ? 'Email phải chứa ký tự @' : 'Email must contain @ symbol'}
                placeholder="nam.nguyen@example.com"
                bind:value={customerEmail}
                class="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white"
              />
            </div>

            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Giấy tờ tùy thân xác thực *"
                  : "Identity Proof Document *"}
              </label>
              <div class="grid grid-cols-2 gap-2">
                <select
                  bind:value={idProofType}
                  class="px-2 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white"
                >
                  <option value="National ID Card"
                    >{$language === "vi"
                      ? "CCCD (12 số)"
                      : "National ID (12 digits)"}</option
                  >
                  <option value="Passport"
                    >{$language === "vi" ? "Hộ chiếu (7-9 ký tự)" : "Passport (7-9 chars)"}</option
                  >
                  <option value="Driver's License"
                    >{$language === "vi"
                      ? "Giấy phép lái xe (12 số)"
                      : "Driver's License (12 digits)"}</option
                  >
                </select>
                <input
                  type="text"
                  required
                  inputmode={idProofType === 'Passport' ? 'text' : 'numeric'}
                  pattern={idProofType === 'National ID Card' || idProofType === "Driver's License" ? '[0-9]{12}' : idProofType === 'Passport' ? '[A-Za-z0-9]{7,9}' : undefined}
                  maxlength={idProofType === 'National ID Card' || idProofType === "Driver's License" ? 12 : idProofType === 'Passport' ? 9 : undefined}
                  title={idProofType === 'National ID Card'
                    ? ($language === 'vi' ? 'CCCD phải gồm đúng 12 chữ số' : 'National ID must contain exactly 12 digits')
                    : idProofType === 'Passport'
                      ? ($language === 'vi' ? 'Passport phải gồm 7-9 ký tự chữ và số' : 'Passport must be 7-9 alphanumeric characters')
                      : ($language === 'vi' ? 'Bằng lái xe phải gồm đúng 12 chữ số' : "Driver's License must contain exactly 12 digits")}
                  placeholder={idProofType === 'National ID Card' ? '012345678901' : idProofType === 'Passport' ? 'C1234567' : '012345678901'}
                  bind:value={idProofNumber}
                  class="px-3 py-2 text-xs font-mono bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white"
                />
              </div>
            </div>

            <div class="sm:col-span-2">
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1.5 flex justify-between items-end"
              >
                <span>
                  {$language === "vi"
                    ? "Địa chỉ lắp đặt thực tế *"
                    : "Physical Installation Address *"}
                </span>
                {#if fullInstallationAddress}
                  <span class="text-[11px] text-slate-400 font-normal truncate max-w-xs">
                    {fullInstallationAddress}
                  </span>
                {/if}
              </label>

              <!-- Cascaded dropdowns for Province, District, Ward -->
              <div class="grid grid-cols-1 sm:grid-cols-3 gap-2.5 mb-2.5">
                <!-- Province -->
                <div class="relative">
                  <select
                    bind:value={province}
                    class="w-full px-3 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white transition cursor-pointer appearance-none"
                  >
                    <option value="">{$language === 'vi' ? '-- Tỉnh / Thành phố --' : '-- Province / City --'}</option>
                    {#each provincesList as p}
                      <option value={p.name}>{p.name}</option>
                    {/each}
                  </select>
                </div>
                <!-- District -->
                <div class="relative">
                  <select
                    bind:value={district}
                    disabled={!province}
                    class="w-full px-3 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white transition cursor-pointer appearance-none disabled:opacity-50"
                  >
                    <option value="">{$language === 'vi' ? '-- Quận / Huyện --' : '-- District --'}</option>
                    {#each districtsList as d}
                      <option value={d.name}>{d.name}</option>
                    {/each}
                  </select>
                </div>
                <!-- Ward -->
                <div class="relative">
                  <select
                    bind:value={ward}
                    disabled={!district}
                    class="w-full px-3 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white transition cursor-pointer appearance-none disabled:opacity-50"
                  >
                    <option value="">{$language === 'vi' ? '-- Phường / Xã --' : '-- Ward --'}</option>
                    {#each wardsList as w}
                      <option value={w.name}>{w.name}</option>
                    {/each}
                  </select>
                </div>
              </div>

              <!-- Specific Address with Map button inside on the right -->
              <div class="relative">
                <MapPin class="absolute left-3.5 top-2.5 h-4 w-4 text-slate-400 dark:text-slate-500" />
                <input
                  id="specificAddress"
                  type="text"
                  required
                  bind:value={specificAddress}
                  placeholder={$language === 'vi' ? 'Địa chỉ cụ thể (số nhà, tên đường, khu phố)' : 'Specific address (house no, street)'}
                  class="w-full pl-10 pr-24 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 transition"
                />
                <button
                  type="button"
                  onclick={() => { showMapModal = true; mapSearchQuery = fullInstallationAddress || specificAddress; }}
                  class="absolute right-1.5 top-1.5 px-2.5 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-md transition cursor-pointer flex items-center space-x-1 shadow-sm text-[11px] font-semibold"
                  title={$language === 'vi' ? 'Chọn trên bản đồ' : 'Pick on map'}
                >
                  <Navigation class="h-3 w-3" />
                  <span>{$language === 'vi' ? 'Bản đồ' : 'Map'}</span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- SECTION B: Connection Type Selection -->
        <div>
          <div
            class="flex items-center space-x-2 border-b border-slate-100 dark:border-slate-800 pb-2 mb-4"
          >
            <Wifi class="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
            <h3
              class="font-semibold text-sm uppercase tracking-wider text-slate-900 dark:text-white"
            >
              {$language === "vi"
                ? "Bước 2: Loại kết nối mạng"
                : "Step 2: Connection Type"}
            </h3>
          </div>

          <div class="grid grid-cols-3 gap-3">
            {#each connectionTypeCards as item (item.type)}
              {@const isSelected = connectionType === item.type}
              <button
                type="button"
                onclick={() => selectConnectionType(item.type)}
                class="p-3 rounded-xl border text-left transition flex flex-col justify-between {isSelected
                  ? 'border-emerald-500 bg-emerald-50 dark:bg-emerald-950/40 text-slate-900 dark:text-white ring-2 ring-emerald-500/20'
                  : 'border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950 text-slate-600 dark:text-slate-400 hover:border-slate-300 dark:hover:border-slate-700'}"
              >
                <div class="flex items-center justify-between mb-2">
                  <item.icon
                    class="h-5 w-5 {isSelected
                      ? 'text-emerald-600 dark:text-emerald-400'
                      : 'text-slate-400'}"
                  />
                  {#if isSelected}
                    <CheckCircle2
                      class="h-4 w-4 text-emerald-600 dark:text-emerald-400"
                    />
                  {/if}
                </div>
                <div>
                  <div class="font-bold text-sm">
                    {$language === "vi" ? item.labelVi : item.type}
                  </div>
                  <div class="text-[11px] text-slate-500 dark:text-slate-400">
                    {$language === "vi" ? item.descVi : item.desc}
                  </div>
                </div>
              </button>
            {/each}
          </div>
        </div>

        <!-- SECTION C: Plan Selection -->
        <div>
          <div
            class="flex items-center space-x-2 border-b border-slate-100 dark:border-slate-800 pb-2 mb-4"
          >
            <FileText class="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
            <h3
              class="font-bold text-base uppercase tracking-wider text-slate-900 dark:text-white"
            >
              {$language === "vi"
                ? `Bước 3: Chọn gói cước (${connectionType === "Broadband" ? "Cáp quang" : connectionType === "Dial-Up" ? "Quay số" : "Cố định"})`
                : `Step 3: Plan Selection (${connectionType})`}
            </h3>
          </div>

          <div class="space-y-2.5">
            {#each availablePlans as plan (plan.id)}
              {@const isSelected = selectedPlanId === plan.id}
              <div
                onclick={() => (selectedPlanId = plan.id)}
                class="p-4 rounded-xl border cursor-pointer transition flex items-center justify-between {isSelected
                  ? 'border-emerald-500 bg-emerald-50/50 dark:bg-emerald-950/30 ring-1 ring-emerald-500'
                  : 'border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/40'}"
              >
                <div class="space-y-1.5">
                  <div class="flex items-center space-x-2.5">
                    <span
                      class="font-bold text-base text-slate-900 dark:text-white"
                      >{getPlanName(plan, $language)}</span
                    >
                    <span
                      class="text-sm px-2.5 py-0.5 rounded bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-mono"
                    >
                      {getPlanSpeedOrBandwidth(
                        plan.speedOrBandwidth,
                        $language,
                      )}
                    </span>
                  </div>
                  <p class="text-sm text-slate-600 dark:text-slate-300">
                    {getPlanDescription(plan, $language)}
                  </p>
                </div>

                <div class="text-right shrink-0 pl-4">
                  <div
                    class="text-lg font-bold text-emerald-600 dark:text-emerald-400 font-mono"
                  >
                    ${plan.monthlyRental.toFixed(2)}<span
                      class="text-sm font-normal text-slate-400"
                      >/{$language === "vi" ? "th" : "mo"}</span
                    >
                  </div>
                  <div class="text-xs text-slate-400 font-mono">
                    {$language === "vi" ? "Cọc:" : "Deposit:"} ${plan.securityDeposit.toFixed(
                      2,
                    )}
                  </div>
                </div>
              </div>
            {/each}
          </div>
        </div>

        <!-- SECTION D: Bulk scheme & Dial-Up existing landline -->
        <div>
          <div
            class="flex items-center space-x-2 border-b border-slate-100 dark:border-slate-800 pb-2 mb-4"
          >
            <FileText class="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
            <h3
              class="font-bold text-base uppercase tracking-wider text-slate-900 dark:text-white"
            >
              {$language === "vi"
                ? `Bước 4: Gói doanh nghiệp${connectionType === "Dial-Up" ? " & Đường dây cố định" : ""}`
                : `Step 4: Bulk Scheme${connectionType === "Dial-Up" ? " & Landline" : ""}`}
            </h3>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                class="block text-sm font-semibold text-slate-700 dark:text-slate-300 mb-1.5"
              >
                {$language === "vi"
                  ? "Số lượng kết nối (mặc định 50 kết nối / 50 router thiết bị)"
                  : "Number of connections (default 50 connections / 50 routers)"}
              </label>
              <input
                type="number"
                min="1"
                bind:value={bulkConnectionsCount}
                class="w-full px-3.5 py-2.5 text-base bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-white"
              />
            </div>
            {#if connectionType === "Dial-Up"}
              <div>
                <label
                  class="block text-sm font-semibold text-slate-700 dark:text-slate-300 mb-1.5"
                >
                  {$language === "vi"
                    ? "Đã có landline Nexus?"
                    : "Existing Nexus landline?"}
                </label>
                <label
                  class="flex items-center space-x-2 text-sm text-slate-600 dark:text-slate-300 py-1.5"
                >
                  <input
                    type="checkbox"
                    bind:checked={hasExistingLandline}
                    class="h-4 w-4 rounded"
                  />
                  <span
                    >{$language === "vi"
                      ? "Chỉ kiểm tra khả thi phần internet"
                      : "Only the internet leg needs a feasibility check"}</span
                  >
                </label>
                {#if hasExistingLandline}
                  <input
                    type="text"
                    placeholder={$language === "vi"
                      ? "Mã tài khoản landline (nếu có)"
                      : "Landline Account ID (optional)"}
                    bind:value={existingLandlineAccountId}
                    class="w-full px-3.5 py-2.5 text-sm font-mono bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500"
                  />
                {/if}
              </div>
            {/if}
          </div>
        </div>

        <!-- Submit Action -->
        <div
          class="pt-4 border-t border-slate-100 dark:border-slate-800 flex items-center justify-end"
        >
          <button
            type="submit"
            class="px-7 py-3 rounded-lg text-base font-bold bg-emerald-600 hover:bg-emerald-700 text-white transition shadow-md flex items-center space-x-2 cursor-pointer"
          >
            <span
              >{$language === "vi"
                ? "Tạo đơn & Cấp mã"
                : "Place Order & Generate ID"}</span
            >
            <ArrowRight class="h-5 w-5" />
          </button>
        </div>
      </form>
    </div>
  {/if}

  <!-- TAB 1.5: BRANCH RETAIL APPROVAL QUEUE (tầng 1 - bán hàng xét duyệt hồ sơ) -->
  {#if activeTab === "approval-queue"}
    <div class="space-y-5">
      <!-- Desk identity banner -->
      <div
        class="rounded-xl border border-amber-200 dark:border-amber-900/50 bg-amber-50/70 dark:bg-amber-950/20 p-5"
      >
        <div
          class="flex flex-col sm:flex-row sm:items-center justify-between gap-3"
        >
          <div class="flex items-start gap-3">
            <div
              class="p-2.5 rounded-xl bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300 border border-amber-300 dark:border-amber-800"
            >
              <FileText class="h-5 w-5" />
            </div>
            <div>
              <h2 class="text-base font-bold text-slate-900 dark:text-white">
                {$language === "vi"
                  ? "Hàng đợi xét duyệt hồ sơ"
                  : "Application Approval Queue"}
              </h2>
              <p class="text-xs text-slate-600 dark:text-slate-400 mt-0.5">
                {#if activeBranchShop}
                  {$language === "vi" ? "Chi nhánh" : "Branch"}: {activeBranchShop.name}
                  ({activeBranchShop.shopCode}) · {activeBranchShop.address}
                {:else}
                  {$language === "vi"
                    ? "Toàn bộ chi nhánh (vai trò quản lý — xem mọi đơn của mọi chi nhánh)."
                    : "All branches (manager scope — viewing every outlet's applications)."}
                {/if}
              </p>
            </div>
          </div>
          <div class="text-left sm:text-right">
            <div
              class="text-[11px] font-mono uppercase tracking-wider text-amber-700 dark:text-amber-400"
            >
              {$language === "vi" ? "Trạng thái phiên" : "Session"}
            </div>
            <div class="text-sm font-bold text-slate-900 dark:text-white">
              {$currentUser?.name ?? "—"}
            </div>
            <div
              class="text-[11px] text-slate-500 dark:text-slate-400 font-mono"
            >
              {$currentUser?.email ?? ""}
            </div>
          </div>
        </div>
        <p
          class="text-[11px] text-amber-800 dark:text-amber-300 mt-3 leading-relaxed"
        >
          {$language === "vi"
            ? "Đơn chỉ được chuyển sang bộ phận Kỹ thuật sau khi bàn này duyệt. Từ chối sẽ trả hồ sơ về khách hàng và đơn không bao giờ xuất hiện ở hàng đợi khảo sát."
            : "An order only reaches Technical once this desk approves it. Rejecting returns the paperwork to the customer and the order never appears in the survey queue."}
        </p>
      </div>

      <!-- KPI mini cards -->
      <div class="grid grid-cols-3 gap-3">
        <div
          class="p-3.5 rounded-xl bg-white dark:bg-slate-900 border border-amber-200 dark:border-amber-900/40 shadow-sm"
        >
          <div
            class="text-[10px] font-mono uppercase tracking-wider text-amber-600 dark:text-amber-400"
          >
            {$language === "vi" ? "Chờ duyệt" : "Awaiting"}
          </div>
          <div
            class="text-2xl font-bold font-mono text-amber-700 dark:text-amber-400 mt-1"
          >
            {awaitingApprovalCount}
          </div>
        </div>
        <div
          class="p-3.5 rounded-xl bg-white dark:bg-slate-900 border border-emerald-200 dark:border-emerald-900/40 shadow-sm"
        >
          <div
            class="text-[10px] font-mono uppercase tracking-wider text-emerald-600 dark:text-emerald-400"
          >
            {$language === "vi" ? "Đã chuyển kỹ thuật" : "Released"}
          </div>
          <div
            class="text-2xl font-bold font-mono text-emerald-700 dark:text-emerald-400 mt-1"
          >
            {approvedByBranchCount}
          </div>
        </div>
        <div
          class="p-3.5 rounded-xl bg-white dark:bg-slate-900 border border-rose-200 dark:border-rose-900/40 shadow-sm"
        >
          <div
            class="text-[10px] font-mono uppercase tracking-wider text-rose-600 dark:text-rose-400"
          >
            {$language === "vi" ? "Đã trả hồ sơ" : "Returned"}
          </div>
          <div
            class="text-2xl font-bold font-mono text-rose-700 dark:text-rose-400 mt-1"
          >
            {rejectedByBranchCount}
          </div>
        </div>
      </div>

      <!-- Search + bucket filter -->
      <div
        class="bg-white dark:bg-slate-900 p-4 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-4 mb-4"
      >
        <!-- Search bar: Tìm kiếm đơn hàng, khách hàng, hóa đơn -->
        <div class="relative flex-1 min-w-[260px] sm:min-w-[320px] max-w-xl">
          <Search
            class="absolute left-3.5 top-3 h-4 w-4 text-slate-400 dark:text-slate-500 pointer-events-none"
          />
          <input
            type="text"
            placeholder={$language === "vi"
              ? "Nhập từ khóa tìm kiếm (Tên KH, Mã đơn, SĐT, CCCD, Địa chỉ, Hóa đơn...)"
              : "Search by customer name, order ID, phone, ID number, address, invoice..."}
            bind:value={approvalSearch}
            class="w-full pl-10 pr-10 py-2.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl focus:outline-none focus:ring-2 focus:ring-amber-500 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-500 font-sans transition-all"
          />
          {#if approvalSearch}
            <button
              type="button"
              onclick={() => (approvalSearch = "")}
              class="absolute right-3 top-2.5 p-1 rounded-full text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-200 dark:hover:bg-slate-800 transition cursor-pointer"
              title={$language === "vi" ? "Xóa tìm kiếm" : "Clear search"}
            >
              <X class="h-4 w-4" />
            </button>
          {/if}
        </div>

        <!-- Advanced search toggle (server-side, 5 tiêu chí theo spec) -->
        <button
          type="button"
          onclick={() => (showAdvancedSearch = !showAdvancedSearch)}
          class="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-bold rounded-xl transition cursor-pointer whitespace-nowrap {showAdvancedSearch
            ? 'bg-indigo-600 text-white shadow-sm ring-2 ring-indigo-500/20'
            : 'bg-indigo-50 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800 hover:bg-indigo-100'}"
        >
          <Filter class="h-3.5 w-3.5" />
          {$language === "vi" ? "Tìm kiếm nâng cao" : "Advanced Search"}
        </button>

        <!-- Quick filter buttons: aligned in a single horizontal row on the right -->
        <div
          class="flex flex-row items-center gap-2 overflow-x-auto whitespace-nowrap shrink-0 lg:ml-auto"
        >
          {#each [
            { key: "Awaiting", labelVi: "Chờ duyệt", labelEn: "Awaiting", count: awaitingApprovalCount, color: "amber" },
            { key: "Approved", labelVi: "Đã duyệt", labelEn: "Approved", count: approvedByBranchCount, color: "emerald" },
            { key: "Rejected", labelVi: "Đã trả", labelEn: "Returned", count: rejectedByBranchCount, color: "rose" },
            { key: "All", labelVi: "Tất cả", labelEn: "All", count: allApprovalOrdersCount, color: "slate" },
          ] as filterItem (filterItem.key)}
            <button
              type="button"
              onclick={() => (approvalFilter = filterItem.key as ApprovalFilter)}
              class="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-semibold rounded-xl transition-all cursor-pointer whitespace-nowrap {approvalFilter === filterItem.key
                ? (filterItem.color === 'amber'
                    ? 'bg-amber-500 text-white shadow-sm ring-2 ring-amber-500/20'
                    : filterItem.color === 'emerald'
                      ? 'bg-emerald-600 text-white shadow-sm ring-2 ring-emerald-500/20'
                      : filterItem.color === 'rose'
                        ? 'bg-rose-600 text-white shadow-sm ring-2 ring-rose-500/20'
                        : 'bg-slate-800 dark:bg-slate-700 text-white shadow-sm ring-2 ring-slate-500/20')
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
            >
              <span>{$language === "vi" ? filterItem.labelVi : filterItem.labelEn}</span>
              <span
                class="font-mono text-xs px-1.5 py-0.5 rounded-full {approvalFilter === filterItem.key
                  ? 'bg-white/20 text-white'
                  : 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300'}"
              >
                {filterItem.count}
              </span>
            </button>
          {/each}
        </div>
      </div>

      <!-- Advanced Search panel: tra cứu CSDL theo spec (id, tên, loại, khoảng ngày, SĐT) -->
      {#if showAdvancedSearch}
        <div class="bg-white dark:bg-slate-900 p-4 rounded-xl border border-indigo-200 dark:border-indigo-900/60 shadow-sm mb-4 space-y-3 animate-in fade-in duration-150">
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            <label class="block">
              <span class="text-[11px] font-semibold text-slate-500 dark:text-slate-400">{$language === "vi" ? "Mã đơn (Order ID)" : "Order ID"}</span>
              <input type="text" bind:value={advForm.orderId} placeholder="D0000000001"
                class="mt-1 w-full px-3 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-[11px] font-semibold text-slate-500 dark:text-slate-400">{$language === "vi" ? "Tên người đặt đơn" : "Customer Name"}</span>
              <input type="text" bind:value={advForm.customerName}
                class="mt-1 w-full px-3 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-[11px] font-semibold text-slate-500 dark:text-slate-400">{$language === "vi" ? "Số điện thoại đăng ký" : "Contact Number"}</span>
              <input type="text" bind:value={advForm.phone} placeholder="0912…"
                class="mt-1 w-full px-3 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-[11px] font-semibold text-slate-500 dark:text-slate-400">{$language === "vi" ? "Loại kết nối" : "Connection Type"}</span>
              <select bind:value={advForm.connectionType}
                class="mt-1 w-full px-3 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500">
                <option value="">{$language === "vi" ? "Tất cả" : "All"}</option>
                <option value="Broadband">Broadband</option>
                <option value="Dial-Up">Dial-Up</option>
                <option value="Landline">Landline</option>
              </select>
            </label>
            <label class="block">
              <span class="text-[11px] font-semibold text-slate-500 dark:text-slate-400">{$language === "vi" ? "Từ ngày (ngày nộp)" : "From Date (applied)"}</span>
              <input type="date" bind:value={advForm.fromDate}
                class="mt-1 w-full px-3 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-[11px] font-semibold text-slate-500 dark:text-slate-400">{$language === "vi" ? "Đến ngày (ngày nộp)" : "To Date (applied)"}</span>
              <input type="date" bind:value={advForm.toDate}
                class="mt-1 w-full px-3 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
          </div>
          <div class="flex items-center justify-between gap-2 flex-wrap">
            <p class="text-[11px] text-slate-500">
              {$language === "vi"
                ? "Tra cứu trực tiếp trên CSDL — đơn ở mọi trạng thái, không giới hạn chi nhánh hiện tại."
                : "Queries the database directly — orders in any status, not limited to your current branch view."}
            </p>
            <div class="flex items-center gap-2">
              {#if advResults !== null}
                <button
                  type="button"
                  onclick={() => (advResults = null)}
                  class="px-3 py-1.5 text-xs font-semibold text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 border border-slate-200 dark:border-slate-800 rounded-lg"
                >
                  {$language === "vi" ? "Xóa kết quả" : "Clear Results"}
                </button>
              {/if}
              <button
                type="button"
                onclick={runAdvancedSearch}
                disabled={advSearching}
                class="inline-flex items-center gap-2 px-4 py-1.5 text-xs font-bold rounded-lg bg-indigo-600 hover:bg-indigo-700 disabled:opacity-60 text-white transition shadow-sm"
              >
                <Search class="h-3.5 w-3.5" />
                {advSearching ? ($language === "vi" ? "Đang tìm…" : "Searching…") : ($language === "vi" ? "Tìm trên CSDL" : "Search Database")}
              </button>
            </div>
          </div>

          {#if advResults !== null}
            <div class="border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden">
              <div class="px-4 py-2 bg-slate-50 dark:bg-slate-800/60 text-[11px] font-bold uppercase tracking-wider text-slate-500 border-b border-slate-200 dark:border-slate-800">
                {$language === "vi"
                  ? `Kết quả từ CSDL: ${advResults.length} đơn hàng`
                  : `Database results: ${advResults.length} orders`}
              </div>
              <div class="overflow-x-auto max-h-80 overflow-y-auto">
                <table class="w-full text-left text-xs">
                  <thead class="bg-white dark:bg-slate-900 text-slate-500 uppercase border-b border-slate-200 dark:border-slate-800">
                    <tr>
                      <th class="px-4 py-2">{$language === "vi" ? "Mã đơn" : "Order ID"}</th>
                      <th class="px-4 py-2">{$language === "vi" ? "Khách hàng" : "Customer"}</th>
                      <th class="px-4 py-2">{$language === "vi" ? "SĐT" : "Phone"}</th>
                      <th class="px-4 py-2">{$language === "vi" ? "Gói / Loại" : "Plan / Type"}</th>
                      <th class="px-4 py-2">{$language === "vi" ? "Ngày nộp" : "Applied"}</th>
                      <th class="px-4 py-2 text-center">{$language === "vi" ? "Trạng thái" : "Status"}</th>
                      <th class="px-4 py-2">{$language === "vi" ? "Mã tài khoản" : "Account ID"}</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                    {#each advResults as o (o.id)}
                      <tr class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition">
                        <td class="px-4 py-2 font-mono font-bold text-indigo-600 dark:text-indigo-400">{o.id}</td>
                        <td class="px-4 py-2 font-semibold text-slate-900 dark:text-white">{o.customerName}</td>
                        <td class="px-4 py-2 font-mono text-slate-600 dark:text-slate-400">{o.customerPhone}</td>
                        <td class="px-4 py-2 text-slate-700 dark:text-slate-300">{o.planName} · {o.connectionType}</td>
                        <td class="px-4 py-2 text-slate-600 dark:text-slate-400">{o.createdAt.slice(0, 10)}</td>
                        <td class="px-4 py-2 text-center">
                          <span class="inline-flex px-2 py-0.5 text-[11px] font-bold rounded-full {statusChipClass(o.status)}">
                            {statusLabel(o.status)}
                          </span>
                        </td>
                        <td class="px-4 py-2 font-mono text-slate-600 dark:text-slate-400">{o.assignedAccountId ?? "—"}</td>
                      </tr>
                    {/each}
                  </tbody>
                </table>
              </div>
            </div>
          {/if}
        </div>
      {/if}

      <!-- Active search query indicator badge (if search query present) -->
      {#if approvalSearch.trim()}
        <div
          class="flex items-center justify-between px-4 py-2.5 rounded-xl bg-amber-50/80 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/40 text-xs text-amber-900 dark:text-amber-200 mb-4 animate-in fade-in duration-150"
        >
          <div class="flex items-center gap-2">
            <Search
              class="h-4 w-4 text-amber-600 dark:text-amber-400 shrink-0"
            />
            <span>
              {$language === "vi"
                ? `Kết quả tìm kiếm cho "${approvalSearch.trim()}": tìm thấy ${approvalQueueOrders.length} đơn hàng`
                : `Search results for "${approvalSearch.trim()}": found ${approvalQueueOrders.length} orders`}
              {#if approvalFilter !== "All" && allApprovalOrdersCount > approvalQueueOrders.length}
                <span class="text-slate-500 dark:text-slate-400 ml-1">
                  ({$language === "vi"
                    ? `trong tổng số ${allApprovalOrdersCount} đơn trên toàn bộ trạng thái`
                    : `out of ${allApprovalOrdersCount} total across all statuses`})
                </span>
              {/if}
            </span>
          </div>
          <div class="flex items-center gap-3">
            {#if approvalFilter !== "All" && allApprovalOrdersCount > approvalQueueOrders.length}
              <button
                type="button"
                onclick={() => (approvalFilter = "All")}
                class="text-[11px] font-bold text-amber-700 dark:text-amber-300 hover:underline cursor-pointer"
              >
                {$language === "vi" ? "Xem tất cả trạng thái" : "View all statuses"}
              </button>
            {/if}
            <button
              type="button"
              onclick={() => (approvalSearch = "")}
              class="text-[11px] font-bold text-slate-600 dark:text-slate-400 hover:underline cursor-pointer"
            >
              {$language === "vi" ? "Xóa tìm kiếm" : "Clear filter"}
            </button>
          </div>
        </div>
      {/if}

      <!-- Queue table -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm overflow-hidden"
      >
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead
              class="bg-slate-50 dark:bg-slate-950/80 text-[11px] font-mono text-slate-600 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Mã đơn" : "Order ID"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Khách hàng & Địa chỉ"
                    : "Customer & Address"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Giấy tờ tùy thân" : "ID Document"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Gói cước" : "Plan"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Trạng thái" : "Status"}</th
                >
                <th class="px-4 py-3 text-right"
                  >{$language === "vi" ? "Thao tác" : "Actions"}</th
                >
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800/60">
              {#if approvalQueueOrders.length === 0}
                <tr>
                  <td colspan="6" class="px-4 py-14 text-center">
                    {#if approvalSearch.trim()}
                      <Search class="h-9 w-9 mx-auto text-amber-400 mb-2" />
                      <p class="text-sm font-semibold text-slate-800 dark:text-slate-200">
                        {$language === "vi"
                          ? `Không tìm thấy đơn hàng nào khớp với "${approvalSearch}"`
                          : `No orders found matching "${approvalSearch}"`}
                      </p>
                      {#if approvalFilter !== "All" && allApprovalOrdersCount > 0}
                        <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
                          {$language === "vi"
                            ? `Có ${allApprovalOrdersCount} đơn hàng khớp với từ khóa ở các trạng thái khác.`
                            : `There are ${allApprovalOrdersCount} matching orders in other statuses.`}
                        </p>
                        <button
                          type="button"
                          onclick={() => (approvalFilter = "All")}
                          class="mt-3 px-4 py-2 text-xs font-bold text-white bg-amber-500 hover:bg-amber-600 rounded-lg shadow-sm transition cursor-pointer"
                        >
                          {$language === "vi"
                            ? `Xem tất cả ${allApprovalOrdersCount} kết quả`
                            : `View all ${allApprovalOrdersCount} results`}
                        </button>
                      {:else}
                        <p class="text-xs text-slate-400 mt-1">
                          {$language === "vi"
                            ? "Vui lòng kiểm tra lại từ khóa hoặc xóa tìm kiếm"
                            : "Please check your search keyword or clear search"}
                        </p>
                        <button
                          type="button"
                          onclick={() => (approvalSearch = "")}
                          class="mt-3 px-3.5 py-1.5 text-xs font-semibold text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 transition cursor-pointer"
                        >
                          {$language === "vi" ? "Xóa tìm kiếm" : "Clear search"}
                        </button>
                      {/if}
                    {:else}
                      <CheckCircle2
                        class="h-9 w-9 mx-auto text-slate-300 dark:text-slate-700 mb-2"
                      />
                      <div
                        class="text-sm font-semibold text-slate-600 dark:text-slate-400"
                      >
                        {$language === "vi"
                          ? "Không có hồ sơ nào ở trạng thái này."
                          : "No applications in this state."}
                      </div>
                    {/if}
                  </td>
                </tr>
              {:else}
                {#each approvalQueueOrders as o (o.id)}
                  <tr
                    class="hover:bg-amber-50/40 dark:hover:bg-amber-950/10 transition-colors"
                  >
                    <td class="px-4 py-3.5 align-top">
                      <div
                        class="font-mono font-bold text-emerald-600 dark:text-emerald-400"
                      >
                        {o.id}
                      </div>
                      <div
                        class="text-[11px] text-slate-500 dark:text-slate-400 font-mono mt-0.5"
                      >
                        {o.createdAt}
                      </div>
                    </td>
                    <td class="px-4 py-3.5 align-top">
                      <div class="font-semibold text-slate-900 dark:text-white">
                        {o.customerName}
                      </div>
                      <div
                        class="text-xs text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-1"
                      >
                        {o.installationAddress}
                      </div>
                      <div
                        class="text-[11px] font-mono text-slate-500 dark:text-slate-400 mt-0.5"
                      >
                        {o.customerPhone}
                      </div>
                    </td>
                    <td class="px-4 py-3.5 align-top">
                      <div class="text-xs text-slate-700 dark:text-slate-300">
                        {o.idProofType}
                      </div>
                      <div
                        class="text-[11px] font-mono text-slate-500 dark:text-slate-400"
                      >
                        {o.idProofNumber}
                      </div>
                    </td>
                    <td class="px-4 py-3.5 align-top">
                      <div
                        class="text-xs font-medium text-slate-800 dark:text-slate-200"
                      >
                        {getPlanName({ name: o.planName }, $language)}
                      </div>
                      <div
                        class="text-[11px] text-slate-500 dark:text-slate-400"
                      >
                        {o.connectionType} · {o.retailOutletCode}
                      </div>
                    </td>
                    <td class="px-4 py-3.5 align-top">
                      {#if o.status === "PendingRetail"}
                        <span
                          class="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800"
                        >
                          {$language === "vi" ? "Chờ duyệt" : "Awaiting"}
                        </span>
                      {:else if o.status === "Not Approved"}
                        <span
                          class="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-rose-100 dark:bg-rose-950 text-rose-800 dark:text-rose-300 border border-rose-300 dark:border-rose-800"
                        >
                          {$language === "vi" ? "Đã trả hồ sơ" : "Returned"}
                        </span>
                        {#if o.retailRejectionReason}
                          <div
                            class="text-[10px] text-rose-600 dark:text-rose-400 mt-1 max-w-[220px] leading-snug"
                          >
                            {o.retailRejectionReason}
                          </div>
                        {/if}
                      {:else}
                        <span
                          class="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800"
                        >
                          {$language === "vi"
                            ? "Đã chuyển kỹ thuật"
                            : "Released"}
                        </span>
                        {#if o.retailApprovedBy}
                          <div
                            class="text-[10px] text-slate-500 dark:text-slate-400 mt-1"
                          >
                            {o.retailApprovedBy} · {o.retailApprovedAt}
                          </div>
                        {/if}
                      {/if}
                    </td>
                    <td
                      class="px-4 py-3.5 align-top text-right whitespace-nowrap"
                    >
                      {#if o.status === "PendingRetail"}
                        <button
                          onclick={() => handleApproveOrder(o)}
                          class="px-2.5 py-1.5 rounded-lg text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white transition shadow-xs"
                        >
                          {$language === "vi"
                            ? "Duyệt & chuyển kỹ thuật"
                            : "Approve & Release"}
                        </button>
                        <button
                          onclick={() => handleOpenRejectModal(o)}
                          class="ml-1.5 px-2.5 py-1.5 rounded-lg text-xs font-bold bg-rose-50 dark:bg-rose-950/50 hover:bg-rose-100 dark:hover:bg-rose-900/50 text-rose-700 dark:text-rose-300 border border-rose-300 dark:border-rose-800 transition"
                        >
                          {$language === "vi" ? "Từ chối" : "Reject"}
                        </button>
                      {:else if o.status === "Not Approved"}
                        <button
                          onclick={() => handleResubmitOrder(o)}
                          class="px-2.5 py-1.5 rounded-lg text-xs font-bold bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700 transition"
                        >
                          {$language === "vi"
                            ? "Đưa lại vào hàng đợi"
                            : "Re-queue"}
                        </button>
                      {:else}
                        <button
                          onclick={() => {
                            activeTab = "order-tracking";
                            orderSearchQuery = o.id;
                            expandedOrderId = o.id;
                          }}
                          class="px-2.5 py-1.5 rounded-lg text-xs font-bold bg-slate-100 dark:bg-slate-800 hover:bg-emerald-100 dark:hover:bg-emerald-950 text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700 transition"
                        >
                          {$language === "vi" ? "Xem tiến độ" : "View Progress"}
                        </button>
                      {/if}
                    </td>
                  </tr>
                {/each}
              {/if}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  {/if}

  <!-- Rejection reason modal (STAGE 1) -->
  {#if isRejectModalOpen && orderToReject}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm"
    >
      <form
        onsubmit={handleConfirmReject}
        class="w-full max-w-lg bg-white dark:bg-slate-900 rounded-2xl border border-rose-300 dark:border-rose-800 shadow-2xl p-6 space-y-4"
      >
        <div class="flex items-start gap-3">
          <div
            class="p-2.5 rounded-xl bg-rose-100 dark:bg-rose-950 text-rose-700 dark:text-rose-300 border border-rose-300 dark:border-rose-800 shrink-0"
          >
            <XCircle class="h-5 w-5" />
          </div>
          <div>
            <h3 class="text-base font-bold text-slate-900 dark:text-white">
              {$language === "vi" ? "Trả hồ sơ" : "Return application"}
              {orderToReject.id}
            </h3>
            <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
              {$language === "vi"
                ? "Nêu rõ lý do để khách hàng bổ sung giấy tờ. Đơn sẽ không được chuyển sang Kỹ thuật."
                : "Give a clear reason so the customer can fix the paperwork. The order will not reach Technical."}
            </p>
          </div>
        </div>

        <div>
          <label
            class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
          >
            {$language === "vi" ? "Lý do trả hồ sơ *" : "Rejection reason *"}
          </label>
          <textarea
            bind:value={rejectReason}
            rows="4"
            required
            placeholder={$language === "vi"
              ? "VD: Giấy tờ tùy thân hết hạn, thiếu giấy tờ chứng minh quyền sử dụng địa chỉ lắp đặt..."
              : "e.g. ID document expired; proof of tenure at the installation address missing..."}
            class="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-rose-500"
          ></textarea>
        </div>

        <div class="flex items-center justify-end gap-2">
          <button
            type="button"
            onclick={() => {
              isRejectModalOpen = false;
              orderToReject = null;
            }}
            class="px-4 py-2 rounded-lg text-sm font-semibold border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
          >
            {$language === "vi" ? "Hủy" : "Cancel"}
          </button>
          <button
            type="submit"
            class="px-4 py-2 rounded-lg text-sm font-bold bg-rose-600 hover:bg-rose-700 text-white transition shadow"
          >
            {$language === "vi" ? "Xác nhận trả hồ sơ" : "Confirm Rejection"}
          </button>
        </div>
      </form>
    </div>
  {/if}

  <!-- TAB 2: ORDER TRACKING -->
  {#if activeTab === "order-tracking"}
    <div class="space-y-6">
      <!-- 1. KPI Summary Cards (6 tiles: one per pipeline stage) -->
      <div
        class="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-3 sm:gap-4"
      >
        <!-- Total Orders -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi" ? "Tổng số đơn hàng" : "Total Orders"}</span
            >
            <ShoppingBag class="h-3.5 w-3.5 text-slate-400" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-slate-900 dark:text-white mt-1.5"
          >
            {trackingSourceOrders.length}
            <span class="text-xs font-normal text-slate-400 ml-1"
              >{$language === "vi" ? "đơn" : "orders"}</span
            >
          </div>
          <div class="text-[10px] text-slate-500 mt-1">
            {$language === "vi" ? "Cập nhật thời gian thực" : "Real-time sync"}
          </div>
        </div>

        <!-- Connection Provided -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-emerald-200 dark:border-emerald-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-emerald-600 dark:text-emerald-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span>{$language === "vi" ? "Đã cấp kết nối" : "Provided"}</span>
            <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-emerald-600 dark:text-emerald-400 mt-1.5"
          >
            {trackingSourceOrders.filter(
              (o) => o.status === "Connection Provided",
            ).length}
            <span class="text-xs font-normal text-emerald-600/70 ml-1"
              >{$language === "vi" ? "hoàn tất" : "active"}</span
            >
          </div>
          <div
            class="text-[10px] text-emerald-600/80 dark:text-emerald-500 mt-1"
          >
            {$language === "vi"
              ? "Đã phát hành Account ID"
              : "Account ID issued"}
          </div>
        </div>

        <!-- Feasible -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-sky-200 dark:border-sky-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-sky-600 dark:text-sky-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span>{$language === "vi" ? "Khảo sát khả thi" : "Feasible"}</span>
            <Activity class="h-3.5 w-3.5 text-sky-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-sky-600 dark:text-sky-400 mt-1.5"
          >
            {trackingSourceOrders.filter((o) => o.status === "Feasible").length}
            <span class="text-xs font-normal text-sky-600/70 ml-1"
              >{$language === "vi" ? "khả thi" : "ready"}</span
            >
          </div>
          <div class="text-[10px] text-sky-600/80 dark:text-sky-500 mt-1">
            {$language === "vi" ? "Chờ kỹ thuật đấu nối" : "Ready for dispatch"}
          </div>
        </div>

        <!-- Awaiting retail approval (STAGE 1) -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-amber-200 dark:border-amber-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-amber-600 dark:text-amber-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi"
                ? "Chờ bán hàng duyệt"
                : "Awaiting Retail"}</span
            >
            <FileText class="h-3.5 w-3.5 text-amber-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-amber-700 dark:text-amber-400 mt-1.5"
          >
            {trackingSourceOrders.filter((o) => o.status === "PendingRetail")
              .length}
            <span class="text-xs font-normal text-amber-600/70 ml-1"
              >{$language === "vi" ? "hồ sơ" : "apps"}</span
            >
          </div>
          <div class="text-[10px] text-amber-600/80 dark:text-amber-500 mt-1">
            {$language === "vi"
              ? "Bàn chi nhánh kiểm tra hồ sơ"
              : "Branch desk paperwork check"}
          </div>
        </div>

        <!-- Cleared by retail, awaiting Technical (STAGE 2) -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-violet-200 dark:border-violet-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-violet-600 dark:text-violet-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span>{$language === "vi" ? "Chờ kỹ thuật" : "Pending Survey"}</span
            >
            <Clock class="h-3.5 w-3.5 text-violet-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-violet-700 dark:text-violet-400 mt-1.5"
          >
            {trackingSourceOrders.filter((o) => o.status === "Pending").length}
            <span class="text-xs font-normal text-violet-600/70 ml-1"
              >{$language === "vi" ? "chờ duyệt" : "queued"}</span
            >
          </div>
          <div class="text-[10px] text-violet-600/80 dark:text-violet-500 mt-1">
            {$language === "vi"
              ? "Đã duyệt — chờ đo kiểm"
              : "Approved — awaiting telemetry"}
          </div>
        </div>

        <!-- Not Feasible -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-rose-200 dark:border-rose-900/40 shadow-sm col-span-2 sm:col-span-1"
        >
          <div
            class="text-[11px] font-mono text-rose-600 dark:text-rose-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span>{$language === "vi" ? "Không khả thi" : "Not Feasible"}</span>
            <XCircle class="h-3.5 w-3.5 text-rose-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-rose-700 dark:text-rose-400 mt-1.5"
          >
            {trackingSourceOrders.filter((o) => o.status === "Not Feasible")
              .length}
            <span class="text-xs font-normal text-rose-600/70 ml-1"
              >{$language === "vi" ? "từ chối" : "rejected"}</span
            >
          </div>
          <div class="text-[10px] text-rose-600/80 dark:text-rose-500 mt-1">
            {$language === "vi"
              ? "Vượt cự ly / Hết cổng"
              : "Distance / Out of ports"}
          </div>
        </div>
      </div>

      <!-- 2. Search, Filter & Orders Table List (Unified Box) -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm overflow-hidden"
      >
        <!-- Top: Search & Filter Toolbar -->
        <div
          class="p-5 border-b border-slate-200 dark:border-slate-800 space-y-4 bg-slate-50/40 dark:bg-slate-950/30"
        >
          <div
            class="flex flex-col md:flex-row gap-3 items-stretch md:items-center justify-between"
          >
            <!-- Search input -->
            <div class="relative flex-1">
              <Search
                class="absolute left-3.5 top-3 h-4 w-4 text-slate-400 dark:text-slate-500"
              />
              <input
                type="text"
                placeholder={$language === "vi"
                  ? "Tra cứu theo Mã đơn hàng, tên khách hàng, SĐT, email, địa chỉ..."
                  : "Search by Order ID, customer name, phone, email, address..."}
                bind:value={orderSearchQuery}
                class="w-full pl-10 pr-10 py-2.5 text-sm bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-600 font-mono"
              />
              {#if orderSearchQuery}
                <button
                  type="button"
                  onclick={() => (orderSearchQuery = "")}
                  class="absolute right-3 top-2.5 p-1 rounded-full text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition cursor-pointer"
                  title="Xóa tìm kiếm"
                >
                  <X class="h-4 w-4" />
                </button>
              {/if}
            </div>

            <div class="flex items-center gap-2">
              <button
                onclick={handleQuickTrackOrder}
                class="px-4 py-2.5 rounded-lg text-sm font-bold bg-emerald-600 hover:bg-emerald-700 text-white transition shadow flex items-center justify-center space-x-1.5 whitespace-nowrap cursor-pointer"
              >
                <ShoppingBag class="h-4 w-4" />
                <span
                  >{$language === "vi"
                    ? "Tra cứu & Mở chi tiết"
                    : "Track & Open Details"}</span
                >
              </button>
              <button
                onclick={handleResetAllOrderFilters}
                class="px-3 py-2.5 rounded-lg text-sm font-semibold border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 transition flex items-center gap-1.5 whitespace-nowrap cursor-pointer"
                title={$language === "vi"
                  ? "Xem tất cả danh sách đơn hàng và xóa bộ lọc"
                  : "View all orders and reset filters"}
              >
                <RotateCcw class="h-4 w-4 text-emerald-600" />
                <span>{$language === "vi" ? "Làm mới" : "Refresh"}</span>
              </button>
            </div>
          </div>

          <!-- Filter rows: Status & Connection Type & Branch Scope -->
          <div
            class="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-3 pt-1 border-t border-slate-100 dark:border-slate-800/80 pt-3"
          >
            <!-- Status Filter Pills (Horizontal row) -->
            <div
              class="flex items-center gap-1.5 overflow-x-auto whitespace-nowrap pb-1 max-w-full"
            >
              <button
                onclick={handleResetAllOrderFilters}
                class="px-2.5 py-1 text-xs font-semibold rounded-lg transition cursor-pointer {orderStatusFilter ===
                  'All' &&
                !orderSearchQuery &&
                orderTypeFilter === 'All'
                  ? 'bg-emerald-600 text-white shadow-sm ring-2 ring-emerald-400/40'
                  : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}"
                title={$language === "vi"
                  ? "Xem toàn bộ đơn hàng (xóa lọc)"
                  : "View all orders"}
              >
                {$language === "vi" ? "Tất cả" : "All"} ({trackingSourceOrders.length})
              </button>
              <button
                onclick={() => {
                  orderStatusFilter = "Connection Provided";
                  orderSortOrder = "Connection Provided";
                  orderSearchQuery = "";
                }}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition cursor-pointer {orderStatusFilter ===
                'Connection Provided'
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}"
              >
                {$language === "vi" ? "Đã cấp kết nối" : "Provided"} ({trackingSourceOrders.filter(
                  (o) => o.status === "Connection Provided",
                ).length})
              </button>
              <button
                onclick={() => {
                  orderStatusFilter = "Feasible";
                  orderSortOrder = "Feasible";
                  orderSearchQuery = "";
                }}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition cursor-pointer {orderStatusFilter ===
                'Feasible'
                  ? 'bg-sky-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}"
              >
                {$language === "vi" ? "Khảo sát khả thi" : "Feasible"} ({trackingSourceOrders.filter(
                  (o) => o.status === "Feasible",
                ).length})
              </button>
              <button
                onclick={() => {
                  orderStatusFilter = "Pending";
                  orderSortOrder = "Pending";
                  orderSearchQuery = "";
                }}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition cursor-pointer {orderStatusFilter ===
                'Pending'
                  ? 'bg-amber-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}"
              >
                {$language === "vi" ? "Chờ kỹ thuật" : "Pending Survey"} ({trackingSourceOrders.filter(
                  (o) => o.status === "Pending",
                ).length})
              </button>
              <button
                onclick={() => {
                  orderStatusFilter = "PendingRetail";
                  orderSortOrder = "newest";
                  orderSearchQuery = "";
                }}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition cursor-pointer {orderStatusFilter ===
                'PendingRetail'
                  ? 'bg-amber-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}"
              >
                {$language === "vi" ? "Chờ bán hàng duyệt" : "Awaiting Retail"} ({trackingSourceOrders.filter(
                  (o) => o.status === "PendingRetail",
                ).length})
              </button>
              <button
                onclick={() => {
                  orderStatusFilter = "Not Approved";
                  orderSortOrder = "newest";
                  orderSearchQuery = "";
                }}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition cursor-pointer {orderStatusFilter ===
                'Not Approved'
                  ? 'bg-orange-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}"
              >
                {$language === "vi" ? "Hồ sơ bị trả lại" : "Returned"} ({trackingSourceOrders.filter(
                  (o) => o.status === "Not Approved",
                ).length})
              </button>
              <button
                onclick={() => {
                  orderStatusFilter = "Not Feasible";
                  orderSortOrder = "Not Feasible";
                  orderSearchQuery = "";
                }}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition cursor-pointer {orderStatusFilter ===
                'Not Feasible'
                  ? 'bg-rose-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}"
              >
                {$language === "vi" ? "Không khả thi" : "Not Feasible"} ({trackingSourceOrders.filter(
                  (o) => o.status === "Not Feasible",
                ).length})
              </button>
            </div>

            <!-- Connection Type & Scope Dropdowns -->
            <div class="flex flex-wrap items-center gap-2.5 text-xs">
              <!-- Connection Type Filter -->
              <div class="flex items-center space-x-1.5 text-xs">
                <span
                  class="text-slate-500 dark:text-slate-400 whitespace-nowrap"
                >
                  {$language === "vi" ? "Loại kết nối:" : "Connection Type:"}
                </span>
                <select
                  value={orderTypeFilter}
                  onchange={(e) =>
                    handleConnectionTypeChange(
                      e.currentTarget.value as "All" | ConnectionType,
                    )}
                  class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg px-2.5 py-1 text-slate-800 dark:text-slate-200 font-medium focus:outline-none focus:ring-1 focus:ring-emerald-500 text-xs cursor-pointer"
                >
                  <option value="All"
                    >{$language === "vi" ? "Tất cả loại kết nối" : "All Types"} ({connectionTypeCounts.All})</option
                  >
                  <option value="Broadband"
                    >Broadband (Cáp quang) ({connectionTypeCounts.Broadband})</option
                  >
                  <option value="Landline"
                    >Landline (Cố định) ({connectionTypeCounts.Landline})</option
                  >
                  <option value="Dial-Up"
                    >Dial-Up (Quay số) ({connectionTypeCounts["Dial-Up"]})</option
                  >
                </select>
              </div>

              <!-- Branch Scope Filter -->
              <div class="flex items-center space-x-1.5 text-xs">
                <span
                  class="text-slate-500 dark:text-slate-400 whitespace-nowrap"
                >
                  {$language === "vi" ? "Phạm vi:" : "Scope:"}
                </span>
                <select
                  bind:value={orderBranchFilter}
                  class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg px-2.5 py-1 text-slate-800 dark:text-slate-200 font-medium focus:outline-none focus:ring-1 focus:ring-emerald-500 text-xs cursor-pointer"
                >
                  <option value="All"
                    >{$language === "vi"
                      ? "Toàn bộ hệ thống (Tất cả)"
                      : "All Outlets & Online"}</option
                  >
                  {#each $retailShops as shop}
                    <option value={shop.shopCode}
                      >{shop.name} ({shop.shopCode})</option
                    >
                  {/each}
                </select>
              </div>
            </div>
          </div>

          <!-- Fast Pick Tags & Sort Order -->
          <div
            class="flex flex-wrap items-center justify-between gap-2 pt-1 border-t border-slate-100 dark:border-slate-800/80 pt-3 text-xs text-slate-500 dark:text-slate-400 font-mono"
          >
            <div class="flex flex-wrap items-center gap-1.5">
              <span
                class="text-[11px] font-semibold text-slate-700 dark:text-slate-300"
              >
                {$language === "vi" ? "Mã đơn hàng mới nhất:" : "Latest Orders:"}
              </span>
              {#each latestOrderTags as o (o.id)}
                {@const isActive =
                  orderSearchQuery.trim().toUpperCase() === o.id.toUpperCase()}
                <button
                  type="button"
                  onclick={() => handleToggleOrderTag(o.id)}
                  class="px-2 py-0.5 rounded border transition font-semibold text-[11px] flex items-center gap-1 cursor-pointer {isActive
                    ? 'bg-emerald-600 text-white border-emerald-600 ring-2 ring-emerald-400/40 shadow-sm'
                    : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-emerald-400 hover:text-emerald-600'}"
                  title={$language === "vi"
                    ? isActive
                      ? "Bấm lại để bỏ chọn và xem tất cả"
                      : `Lọc mã đơn ${o.id}`
                    : isActive
                      ? "Click again to deselect"
                      : `Filter order ${o.id}`}
                >
                  <span>{o.id}</span>
                  {#if isActive}
                    <X class="h-3 w-3" />
                  {/if}
                </button>
              {/each}
            </div>

            <div class="flex items-center gap-2 ml-auto shrink-0">
              {#if expandedOrderId}
                <button
                  type="button"
                  onclick={() => (expandedOrderId = null)}
                  class="px-2.5 py-1 text-[11px] font-semibold rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-300 hover:text-emerald-600 dark:hover:text-emerald-400 transition flex items-center gap-1 cursor-pointer"
                  title={$language === "vi"
                    ? "Thu gọn chi tiết đơn đang mở"
                    : "Collapse expanded order"}
                >
                  <X class="h-3.5 w-3.5 text-slate-400" />
                  <span
                    >{$language === "vi"
                      ? "Thu gọn chi tiết"
                      : "Collapse Details"}</span
                  >
                </button>
              {/if}

              <!-- Sort / Status Option Select Dropdown -->
              <div class="flex items-center gap-1.5 shrink-0">
                <ArrowDownUp class="h-3.5 w-3.5 text-emerald-600 shrink-0" />
                <select
                  value={orderSortOrder}
                  onchange={(e) => handleOrderSortChange(e.currentTarget.value)}
                  class="px-2.5 py-1 text-[11px] font-semibold rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 transition cursor-pointer focus:outline-none focus:ring-1 focus:ring-emerald-500"
                  title={$language === "vi"
                    ? "Tùy chọn sắp xếp & lọc theo trạng thái"
                    : "Sort and status filter options"}
                >
                  <option value="newest"
                    >{$language === "vi"
                      ? "Sắp xếp: Mới nhất trước"
                      : "Sort: Newest First"}</option
                  >
                  <option value="oldest"
                    >{$language === "vi"
                      ? "Sắp xếp: Cũ nhất trước"
                      : "Sort: Oldest First"}</option
                  >
                  <option value="Feasible"
                    >{$language === "vi"
                      ? "Khảo sát khả thi"
                      : "Feasible"}</option
                  >
                  <option value="Pending"
                    >{$language === "vi"
                      ? "Chờ kĩ thuật khảo sát"
                      : "Pending Survey"}</option
                  >
                  <option value="Not Feasible"
                    >{$language === "vi"
                      ? "Không khả thi"
                      : "Not Feasible"}</option
                  >
                  <option value="Connection Provided"
                    >{$language === "vi"
                      ? "Đã cấp kết nối"
                      : "Connection Provided"}</option
                  >
                </select>
              </div>
            </div>
          </div>
        </div>

        {#if filteredTrackedOrders.length === 0}
          <div class="py-16 text-center space-y-3">
            <ShoppingBag class="h-10 w-10 text-slate-400 mx-auto opacity-50" />
            <div
              class="text-sm font-semibold text-slate-700 dark:text-slate-300 font-mono"
            >
              {$language === "vi"
                ? "Không tìm thấy đơn hàng nào khớp với bộ lọc."
                : "No orders match the current filter criteria."}
            </div>
            <button
              onclick={() => {
                orderSearchQuery = "";
                orderStatusFilter = "All";
                orderTypeFilter = "All";
              }}
              class="px-4 py-1.5 text-xs font-semibold bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition"
            >
              {$language === "vi" ? "Đặt lại bộ lọc" : "Reset Filters"}
            </button>
          </div>
        {:else}
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm border-collapse">
              <thead
                class="bg-slate-50 dark:bg-slate-950/80 text-[11px] font-mono text-slate-600 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
              >
                <tr>
                  <th class="px-4 py-3.5"
                    >{$language === "vi" ? "MÃ ĐƠN HÀNG" : "ORDER ID"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "KHÁCH HÀNG & ĐỊA CHỈ"
                      : "CUSTOMER & ADDRESS"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "LOẠI KẾT NỐI & GÓI CƯỚC"
                      : "CONNECTION & PLAN"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "ĐIỆN THOẠI & XÁC MINH"
                      : "PHONE & ID PROOF"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "TRẠNG THÁI TIẾN ĐỘ"
                      : "FULFILLMENT STATUS"}</th
                  >
                  <th class="px-4 py-3.5 text-right"
                    >{$language === "vi" ? "THAO TÁC" : "ACTIONS"}</th
                  >
                </tr>
              </thead>
              <tbody
                class="divide-y divide-slate-100 dark:divide-slate-800/60 font-sans"
              >
                {#each filteredTrackedOrders as o (o.id)}
                  {@const isExpanded = expandedOrderId === o.id}
                  <!-- Main Table Row -->
                  <tr
                    class="transition-colors hover:bg-emerald-50/40 dark:hover:bg-emerald-950/20 {isExpanded
                      ? 'bg-emerald-50/60 dark:bg-emerald-950/30'
                      : ''}"
                  >
                    <!-- Col 1: Order ID -->
                    <td class="px-4 py-4 align-top">
                      <div class="flex items-center space-x-1.5">
                        <span
                          class="font-mono font-bold text-emerald-600 dark:text-emerald-400 tracking-wider"
                        >
                          {o.id}
                        </span>
                        <button
                          onclick={() => copyToClipboard(o.id, "Order ID")}
                          class="p-1 rounded hover:bg-slate-200 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition"
                          title={$language === "vi"
                            ? "Sao chép mã đơn"
                            : "Copy Order ID"}
                        >
                          <Copy class="h-3.5 w-3.5" />
                        </button>
                      </div>
                      <div
                        class="text-[11px] font-mono text-slate-500 dark:text-slate-400 mt-1"
                      >
                        {o.createdAt}
                      </div>
                    </td>

                    <!-- Col 2: Customer & Address -->
                    <td class="px-4 py-4 align-top">
                      <div class="font-semibold text-slate-900 dark:text-white">
                        {o.customerName}
                      </div>
                      <div
                        class="text-xs text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-1"
                      >
                        {o.installationAddress}
                      </div>
                    </td>

                    <!-- Col 3: Connection & Plan -->
                    <td class="px-4 py-4 align-top">
                      <div class="flex items-center space-x-1.5">
                        {#if o.connectionType === "Broadband"}
                          <span
                            class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-blue-100 dark:bg-blue-950 text-blue-800 dark:text-blue-300 border border-blue-200 dark:border-blue-800"
                          >
                            <Wifi class="h-3 w-3" />
                            <span>Broadband</span>
                          </span>
                        {:else if o.connectionType === "Landline"}
                          <span
                            class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-purple-100 dark:bg-purple-950 text-purple-800 dark:text-purple-300 border border-purple-200 dark:border-purple-800"
                          >
                            <Phone class="h-3 w-3" />
                            <span>Landline</span>
                          </span>
                        {:else}
                          <span
                            class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-800"
                          >
                            <Radio class="h-3 w-3" />
                            <span>Dial-Up</span>
                          </span>
                        {/if}
                      </div>
                      <div
                        class="text-xs font-medium text-slate-800 dark:text-slate-200 mt-1"
                      >
                        {getPlanName({ name: o.planName }, $language)}
                      </div>
                    </td>

                    <!-- Col 4: Phone & ID Proof -->
                    <td class="px-4 py-4 align-top">
                      <div
                        class="font-mono text-xs font-semibold text-slate-800 dark:text-slate-200"
                      >
                        {o.customerPhone}
                      </div>
                      <div
                        class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5 truncate max-w-[170px]"
                      >
                        {o.customerEmail}
                      </div>
                      <div class="text-[10px] text-slate-400 font-mono mt-0.5">
                        {o.idProofType}: {o.idProofNumber}
                      </div>
                    </td>

                    <!-- Col 5: Fulfillment Status & Assigned Technician -->
                    <td class="px-4 py-4 align-top">
                      <div class="space-y-1.5">
                        <span
                          class="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full text-xs font-bold uppercase tracking-wider {statusChip(
                            o.status,
                          )}"
                        >
                          <span
                            class="h-2 w-2 rounded-full {statusDot(o.status)}"
                          ></span>
                          <span>{orderStatusLabel(o.status, $language)}</span>
                        </span>

                        <!-- Technician Assignment Indicator / Fast Button -->
                        <div class="pt-0.5">
                          {#if o.assignedTechnician}
                            <button
                              type="button"
                              onclick={(e) => {
                                e.stopPropagation();
                                openAssignTechnicianModal(o);
                              }}
                              class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-emerald-50 hover:bg-emerald-100 dark:bg-emerald-950/60 dark:hover:bg-emerald-900/60 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800 transition"
                              title={$language === "vi"
                                ? "Bấm để đổi kỹ thuật viên khảo sát"
                                : "Click to change survey technician"}
                            >
                              <Wrench
                                class="h-3 w-3 text-emerald-600 dark:text-emerald-400"
                              />
                              <span
                                class="text-slate-500 dark:text-slate-400 font-normal"
                                >{$language === "vi" ? "KTV:" : "Tech:"}</span
                              >
                              <span class="truncate max-w-[110px] font-semibold"
                                >{o.assignedTechnician}</span
                              >
                            </button>
                          {:else}
                            <button
                              type="button"
                              onclick={(e) => {
                                e.stopPropagation();
                                openAssignTechnicianModal(o);
                              }}
                              class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-50 hover:bg-amber-100 dark:bg-amber-950/40 dark:hover:bg-amber-900/60 text-amber-700 dark:text-amber-400 border border-amber-300 dark:border-amber-700/60 transition shadow-2xs"
                              title={$language === "vi"
                                ? "Bấm để phân công kỹ thuật viên khảo sát"
                                : "Click to assign survey technician"}
                            >
                              <UserPlus class="h-3 w-3" />
                              <span
                                >{$language === "vi"
                                  ? "+ Phân công KTV"
                                  : "+ Assign Tech"}</span
                              >
                            </button>
                          {/if}
                        </div>

                        {#if o.status === "PendingRetail"}
                          <button
                            onclick={() => (activeTab = "approval-queue")}
                            class="block text-[10px] font-semibold text-amber-700 dark:text-amber-400 hover:underline mt-0.5"
                          >
                            {$language === "vi"
                              ? "Mở hàng đợi duyệt →"
                              : "Open approval queue →"}
                          </button>
                        {/if}
                      </div>
                    </td>

                    <!-- Col 6: Action Button -->
                    <td
                      class="px-4 py-4 align-top text-right whitespace-nowrap"
                    >
                      <div class="flex items-center justify-end space-x-2">
                        <button
                          onclick={() => toggleOrderDropdown(o.id)}
                          class="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold transition shadow-xs {isExpanded
                            ? 'bg-emerald-600 text-white shadow-emerald-600/30'
                            : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-emerald-100 dark:hover:bg-emerald-950 hover:text-emerald-800 dark:hover:text-emerald-300 border border-slate-200 dark:border-slate-700'}"
                        >
                          <span
                            >{$language === "vi"
                              ? isExpanded
                                ? "Thu gọn"
                                : "Chi tiết"
                              : isExpanded
                                ? "Collapse"
                                : "Details"}</span
                          >
                          <ChevronDown
                            class="h-4 w-4 transition-transform duration-200 {isExpanded
                              ? 'rotate-180'
                              : ''}"
                          />
                        </button>
                      </div>
                    </td>
                  </tr>

                  <!-- DROPDOWN BOX CHI TIẾT TIẾN ĐỘ ĐƠN HÀNG -->
                  {#if isExpanded}
                    <tr
                      class="bg-gradient-to-b from-emerald-50/50 to-slate-50 dark:from-emerald-950/20 dark:to-slate-950/40 border-b-2 border-emerald-400 dark:border-emerald-600/60"
                    >
                      <td colspan="6" class="p-4 sm:p-6">
                        <div
                          class="space-y-6 rounded-xl border border-emerald-200 dark:border-emerald-900/60 bg-white dark:bg-slate-900 p-5 sm:p-6 shadow-md"
                        >
                          <!-- Box Header -->
                          <div
                            class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-800 pb-4"
                          >
                            <div
                              class="flex items-start sm:items-center space-x-3"
                            >
                              <div
                                class="p-2.5 rounded-xl bg-emerald-100 dark:bg-emerald-950/80 text-emerald-700 dark:text-emerald-400 border border-emerald-300 dark:border-emerald-800"
                              >
                                <ShoppingBag class="h-6 w-6" />
                              </div>
                              <div>
                                <div class="flex items-center space-x-3">
                                  <h4
                                    class="text-lg font-bold font-mono text-slate-900 dark:text-white"
                                  >
                                    {$language === "vi"
                                      ? "Mã đơn hàng:"
                                      : "Order ID:"}
                                    <span
                                      class="text-emerald-600 dark:text-emerald-400 font-extrabold"
                                      >{o.id}</span
                                    >
                                  </h4>
                                  <span
                                    class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider {statusChip(
                                      o.status,
                                    )}"
                                  >
                                    {orderStatusLabel(o.status, $language)}
                                  </span>
                                </div>
                                <p
                                  class="text-xs text-slate-500 dark:text-slate-400 mt-1"
                                >
                                  {$language === "vi"
                                    ? `Đăng ký lúc ${o.createdAt} bởi ${o.retailEmployeeName} (${o.retailOutletCode})`
                                    : `Submitted at ${o.createdAt} by ${o.retailEmployeeName} (${o.retailOutletCode})`}
                                </p>
                                {#if o.retailApprovedBy && o.status !== "Not Approved"}
                                  <p
                                    class="text-[11px] text-emerald-600 dark:text-emerald-400 mt-0.5"
                                  >
                                    {$language === "vi"
                                      ? "Bán hàng duyệt:"
                                      : "Retail approved:"}
                                    {o.retailApprovedBy}
                                    · {o.retailApprovedAt}
                                    {#if o.retailApprovalNotes}— {o.retailApprovalNotes}{/if}
                                  </p>
                                {:else if o.status === "Not Approved" && o.retailRejectionReason}
                                  <p
                                    class="text-[11px] text-rose-600 dark:text-rose-400 mt-0.5"
                                  >
                                    {$language === "vi"
                                      ? "Lý do trả hồ sơ:"
                                      : "Returned because:"}
                                    {o.retailRejectionReason}
                                  </p>
                                {/if}
                              </div>
                            </div>

                            <!-- Header Actions -->
                            <div class="flex flex-wrap items-center gap-2">
                              <button
                                onclick={() =>
                                  copyToClipboard(o.id, "Order ID")}
                                class="inline-flex items-center space-x-1 text-xs border border-slate-200 dark:border-slate-800 px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition font-medium"
                              >
                                <Copy class="h-3.5 w-3.5" />
                                <span
                                  >{$language === "vi"
                                    ? "Sao chép mã đơn"
                                    : "Copy Order ID"}</span
                                >
                              </button>

                              {#if o.assignedAccountId}
                                <button
                                  onclick={() => {
                                    activeTab = "connection-details";
                                    connSearchQuery = o.assignedAccountId!;
                                    expandedConnAccountId =
                                      o.assignedAccountId!;
                                  }}
                                  class="inline-flex items-center space-x-1.5 text-xs bg-emerald-600 hover:bg-emerald-700 text-white px-3 py-1.5 rounded-lg font-semibold transition shadow-xs"
                                >
                                  <Wifi class="h-3.5 w-3.5" />
                                  <span
                                    >{$language === "vi"
                                      ? "Mở chi tiết thuê bao"
                                      : "View Connection"}</span
                                  >
                                </button>
                              {/if}

                              <button
                                onclick={() => (expandedOrderId = null)}
                                class="inline-flex items-center space-x-1 text-xs border border-slate-200 dark:border-slate-800 px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
                                title={$language === "vi"
                                  ? "Đóng hộp chi tiết"
                                  : "Close details"}
                              >
                                <X class="h-3.5 w-3.5" />
                                <span
                                  >{$language === "vi" ? "Đóng" : "Close"}</span
                                >
                              </button>
                            </div>
                          </div>

                          <!-- 5-Stage Visual Progress Stepper (2 tầng xét duyệt) -->
                          <div class="py-2">
                            <div
                              class="text-xs font-semibold uppercase tracking-wider text-slate-500 mb-5 flex items-center space-x-2"
                            >
                              <Clock class="h-3.5 w-3.5 text-emerald-600" />
                              <span
                                >{$language === "vi"
                                  ? "Quy trình xử lý đơn hàng"
                                  : "Fulfillment Lifecycle Stepper"}</span
                              >
                            </div>

                            <div
                              class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2 relative"
                            >
                              {#each stepperStages as st (st.step)}
                                {@const stageIdx = getStageIndex(o.status)}
                                {@const isComplete = stageIdx >= st.step}
                                {@const isFailedRetail =
                                  o.status === "Not Approved" && st.step >= 2}
                                {@const isFailedTech =
                                  o.status === "Not Feasible" && st.step >= 3}
                                {@const isFailed =
                                  isFailedRetail || isFailedTech}
                                <div class="text-center space-y-2">
                                  <div
                                    class="h-10 w-10 mx-auto rounded-full flex items-center justify-center font-bold text-xs transition-colors {isFailed
                                      ? 'bg-rose-500 text-white'
                                      : isComplete
                                        ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/30'
                                        : 'bg-slate-100 dark:bg-slate-800 text-slate-400'}"
                                  >
                                    {#if isFailed}
                                      !
                                    {:else if isComplete}
                                      <CheckCircle2 class="h-5 w-5" />
                                    {:else}
                                      {st.step}
                                    {/if}
                                  </div>
                                  <div>
                                    <div
                                      class="text-xs font-bold text-slate-900 dark:text-white"
                                    >
                                      {$language === "vi"
                                        ? st.labelVi
                                        : st.label}
                                    </div>
                                    <div
                                      class="text-[11px] text-slate-500 dark:text-slate-400"
                                    >
                                      {$language === "vi" ? st.subVi : st.sub}
                                    </div>
                                  </div>
                                </div>
                              {/each}
                            </div>

                            {#if o.status === "PendingRetail"}
                              <div
                                class="mt-4 flex items-start gap-2 p-3 rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/50"
                              >
                                <FileText
                                  class="h-3.5 w-3.5 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5"
                                />
                                <span
                                  class="text-[11px] text-amber-800 dark:text-amber-300"
                                >
                                  {$language === "vi"
                                    ? `Chờ ${o.retailEmployeeName} tại chi nhánh ${o.retailOutletCode} duyệt hồ sơ trước khi kỹ thuật khảo sát.`
                                    : `Waiting for ${o.retailEmployeeName} at branch ${o.retailOutletCode} to approve the paperwork before Technical surveys it.`}
                                </span>
                              </div>
                            {/if}
                          </div>

                          <!-- Details Grid (3 Sections) -->
                          <div
                            class="grid grid-cols-1 md:grid-cols-3 gap-4 pt-2 border-t border-slate-100 dark:border-slate-800"
                          >
                            <!-- Box 1: Customer Profile -->
                            <div
                              class="space-y-2.5 p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                            >
                              <h5
                                class="font-bold text-xs text-slate-700 dark:text-slate-300 uppercase tracking-wider flex items-center space-x-1.5"
                              >
                                <User class="h-3.5 w-3.5 text-emerald-600" />
                                <span
                                  >{$language === "vi"
                                    ? "Hồ sơ khách hàng"
                                    : "Customer Profile"}</span
                                >
                              </h5>
                              <div class="space-y-1.5 text-xs">
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Họ tên:"
                                      : "Name:"}</span
                                  >
                                  <strong
                                    class="text-slate-900 dark:text-white ml-1"
                                    >{o.customerName}</strong
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Điện thoại:"
                                      : "Phone:"}</span
                                  >
                                  <span
                                    class="font-mono font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.customerPhone}</span
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Email:"
                                      : "Email:"}</span
                                  >
                                  <span
                                    class="text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.customerEmail}</span
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Xác minh ID:"
                                      : "ID Proof:"}</span
                                  >
                                  <span
                                    class="font-mono text-slate-700 dark:text-slate-300 ml-1"
                                    >{o.idProofType} ({o.idProofNumber})</span
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Địa chỉ:"
                                      : "Address:"}</span
                                  >
                                  <span
                                    class="text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.installationAddress}</span
                                  >
                                </div>
                              </div>
                            </div>

                            <!-- Box 2: Service & Technical Data -->
                            <div
                              class="space-y-2.5 p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                            >
                              <h5
                                class="font-bold text-xs text-slate-700 dark:text-slate-300 uppercase tracking-wider flex items-center space-x-1.5"
                              >
                                <Server class="h-3.5 w-3.5 text-emerald-600" />
                                <span
                                  >{$language === "vi"
                                    ? "Dữ liệu dịch vụ & Kỹ thuật"
                                    : "Service & Technical"}</span
                                >
                              </h5>
                              <div class="space-y-1.5 text-xs">
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Gói cước:"
                                      : "Plan:"}</span
                                  >
                                  <strong
                                    class="text-slate-900 dark:text-white ml-1"
                                    >{getPlanName(
                                      { name: o.planName },
                                      $language,
                                    )}</strong
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Loại kết nối:"
                                      : "Type:"}</span
                                  >
                                  <span
                                    class="font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.connectionType}</span
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Khoảng cách cáp:"
                                      : "Cable Distance:"}</span
                                  >
                                  <span
                                    class="font-mono font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.cableDistanceMeters || 120} m</span
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Hộp chia DP:"
                                      : "DP Box:"}</span
                                  >
                                  <span
                                    class="text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.dpBoxCapacity ||
                                      ($language === "vi"
                                        ? "Còn cổng khả dụng"
                                        : "Port Available")}</span
                                  >
                                </div>

                                <!-- Kỹ thuật viên đảm nhiệm khảo sát -->
                                <div
                                  class="mt-2.5 pt-2.5 border-t border-slate-200 dark:border-slate-800"
                                >
                                  <div
                                    class="flex items-center justify-between mb-1.5"
                                  >
                                    <span
                                      class="text-[11px] font-semibold text-slate-600 dark:text-slate-400 uppercase tracking-wider flex items-center gap-1"
                                    >
                                      <Wrench
                                        class="h-3 w-3 text-emerald-600"
                                      />
                                      {$language === "vi"
                                        ? "Kỹ thuật viên khảo sát:"
                                        : "Survey Technician:"}
                                    </span>
                                    <button
                                      type="button"
                                      onclick={() =>
                                        openAssignTechnicianModal(o)}
                                      class="text-[11px] font-bold text-emerald-600 dark:text-emerald-400 hover:underline flex items-center gap-1"
                                    >
                                      <UserCheck class="h-3 w-3" />
                                      <span
                                        >{$language === "vi"
                                          ? o.assignedTechnician
                                            ? "Đổi KTV"
                                            : "Phân công"
                                          : o.assignedTechnician
                                            ? "Change"
                                            : "Assign"}</span
                                      >
                                    </button>
                                  </div>

                                  {#if o.assignedTechnician}
                                    <div
                                      class="p-2.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800"
                                    >
                                      <div
                                        class="flex items-center justify-between"
                                      >
                                        <span
                                          class="font-bold text-slate-900 dark:text-white text-xs"
                                        >
                                          {o.assignedTechnician}
                                        </span>
                                        {#if o.assignedTechnicianId}
                                          <span
                                            class="font-mono text-[10px] text-emerald-700 dark:text-emerald-400 bg-emerald-100 dark:bg-emerald-900/60 px-1.5 py-0.5 rounded font-semibold"
                                          >
                                            {o.assignedTechnicianId}
                                          </span>
                                        {/if}
                                      </div>
                                      {#if o.assignedTechnicianPhone}
                                        <div
                                          class="font-mono text-[11px] text-slate-600 dark:text-slate-400 mt-1 flex items-center gap-1"
                                        >
                                          <Phone
                                            class="h-3 w-3 text-emerald-600"
                                          />
                                          <span
                                            >{o.assignedTechnicianPhone}</span
                                          >
                                        </div>
                                      {/if}
                                      {#if o.assignedTechnicianDate}
                                        <div
                                          class="text-[10px] text-slate-400 mt-0.5"
                                        >
                                          {$language === "vi"
                                            ? "Thời điểm phân công:"
                                            : "Assigned at:"}
                                          {o.assignedTechnicianDate}
                                        </div>
                                      {/if}
                                    </div>
                                  {:else}
                                    <div
                                      class="p-2.5 rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800/60 text-amber-800 dark:text-amber-300 flex items-center justify-between"
                                    >
                                      <span class="text-xs">
                                        {$language === "vi"
                                          ? "Chưa phân công kỹ thuật viên"
                                          : "No technician assigned"}
                                      </span>
                                      <button
                                        type="button"
                                        onclick={() =>
                                          openAssignTechnicianModal(o)}
                                        class="px-2.5 py-1 bg-amber-600 hover:bg-amber-700 text-white rounded-md text-xs font-semibold transition shadow-xs flex items-center gap-1"
                                      >
                                        <UserPlus class="h-3 w-3" />
                                        <span
                                          >{$language === "vi"
                                            ? "Chọn người khảo sát"
                                            : "Select Tech"}</span
                                        >
                                      </button>
                                    </div>
                                  {/if}
                                </div>

                                {#if o.feasibilityNotes}
                                  <div
                                    class="text-amber-600 dark:text-amber-400 pt-1"
                                  >
                                    <span class="font-semibold"
                                      >{$language === "vi"
                                        ? "Ghi chú kỹ thuật:"
                                        : "Tech Notes:"}</span
                                    >
                                    <p class="text-[11px] mt-0.5">
                                      {o.feasibilityNotes}
                                    </p>
                                  </div>
                                {/if}
                              </div>
                            </div>

                            <!-- Box 3: Fulfillment & Issued Account -->
                            <div
                              class="space-y-2.5 p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                            >
                              <h5
                                class="font-bold text-xs text-slate-700 dark:text-slate-300 uppercase tracking-wider flex items-center space-x-1.5"
                              >
                                <CheckCircle2
                                  class="h-3.5 w-3.5 text-emerald-600"
                                />
                                <span
                                  >{$language === "vi"
                                    ? "Cấp phát tài khoản & Điểm giao dịch"
                                    : "Issued Account & Retail Outlet"}</span
                                >
                              </h5>
                              <div class="space-y-2 text-xs">
                                {#if o.assignedAccountId}
                                  <div
                                    class="p-2.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800"
                                  >
                                    <span
                                      class="text-[11px] text-emerald-700 dark:text-emerald-400 font-semibold block"
                                    >
                                      {$language === "vi"
                                        ? "Mã tài khoản 16 ký tự đã cấp:"
                                        : "Issued 16-char Account ID:"}
                                    </span>
                                    <div
                                      class="flex items-center justify-between mt-1"
                                    >
                                      <span
                                        class="font-mono text-sm font-bold text-emerald-700 dark:text-emerald-300"
                                      >
                                        {o.assignedAccountId}
                                      </span>
                                      <button
                                        onclick={() =>
                                          copyToClipboard(
                                            o.assignedAccountId!,
                                            "Account ID",
                                          )}
                                        class="p-1 rounded hover:bg-emerald-200 dark:hover:bg-emerald-800 text-emerald-600 dark:text-emerald-400 transition"
                                        title={$language === "vi"
                                          ? "Sao chép mã tài khoản"
                                          : "Copy Account ID"}
                                      >
                                        <Copy class="h-3.5 w-3.5" />
                                      </button>
                                    </div>
                                  </div>
                                {:else}
                                  <div
                                    class="p-2.5 rounded-lg bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 text-amber-700 dark:text-amber-400"
                                  >
                                    <span class="text-xs font-medium">
                                      {$language === "vi"
                                        ? "Chưa cấp Account ID (Đang chờ kỹ thuật phê duyệt hoặc khảo sát)"
                                        : "Awaiting technician provisioning to generate Account ID."}
                                    </span>
                                  </div>
                                {/if}

                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Điểm quầy bán lẻ:"
                                      : "Retail Outlet:"}</span
                                  >
                                  <span
                                    class="font-mono font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.retailOutletCode}</span
                                  >
                                </div>
                                <div>
                                  <span class="text-slate-500"
                                    >{$language === "vi"
                                      ? "Nhân viên tiếp nhận:"
                                      : "Staff:"}</span
                                  >
                                  <span
                                    class="text-slate-800 dark:text-slate-200 ml-1"
                                    >{o.retailEmployeeName}</span
                                  >
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  {/if}
                {/each}
              </tbody>
            </table>
          </div>
        {/if}
      </div>
    </div>
  {/if}

  <!-- TAB 3: CONNECTION DETAILS -->
  {#if activeTab === "connection-details"}
    <div class="space-y-6">
      <!-- 1. KPI Summary Cards -->
      <div
        class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3 sm:gap-4"
      >
        <!-- Managed Accounts -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi"
                ? "Thuê bao quản lý"
                : "Managed Accounts"}</span
            >
            <Wifi class="h-3.5 w-3.5 text-slate-400" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-slate-900 dark:text-white mt-1.5"
          >
            {$connections.length}
            <span class="text-xs font-normal text-slate-400 ml-1"
              >{$language === "vi" ? "tài khoản" : "circuits"}</span
            >
          </div>
          <div class="text-[10px] text-slate-500 mt-1">
            {$language === "vi" ? "16 ký tự chuẩn ISO" : "16-char ISO standard"}
          </div>
        </div>

        <!-- Active -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-emerald-200 dark:border-emerald-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-emerald-600 dark:text-emerald-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi" ? "Đang hoạt động" : "Active Circuits"}</span
            >
            <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-emerald-600 dark:text-emerald-400 mt-1.5"
          >
            {$connections.filter((c) => c.status === "Active").length}
            <span class="text-xs font-normal text-emerald-600/70 ml-1"
              >{$language === "vi" ? "trực tuyến" : "online"}</span
            >
          </div>
          <div
            class="text-[10px] text-emerald-600/80 dark:text-emerald-500 mt-1"
          >
            {$language === "vi"
              ? "Đường truyền thông suốt"
              : "Stable traffic flow"}
          </div>
        </div>

        <!-- Temporarily Inactive -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-amber-200 dark:border-amber-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-amber-600 dark:text-amber-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi"
                ? "Tạm khóa dịch vụ"
                : "Temporarily Inactive"}</span
            >
            <AlertTriangle class="h-3.5 w-3.5 text-amber-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-amber-700 dark:text-amber-400 mt-1.5"
          >
            {$connections.filter((c) => c.status === "Temporarily Inactive")
              .length}
            <span class="text-xs font-normal text-amber-600/70 ml-1"
              >{$language === "vi" ? "tạm khóa" : "on hold"}</span
            >
          </div>
          <div class="text-[10px] text-amber-600/80 dark:text-amber-500 mt-1">
            {$language === "vi" ? "Nợ cước / Tạm ngưng" : "Overdue / Suspended"}
          </div>
        </div>

        <!-- Permanently Inactive -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-rose-200 dark:border-rose-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-rose-600 dark:text-rose-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span>{$language === "vi" ? "Khóa vĩnh viễn" : "Cut Off"}</span>
            <XCircle class="h-3.5 w-3.5 text-rose-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-rose-700 dark:text-rose-400 mt-1.5"
          >
            {$connections.filter((c) => c.status === "Permanently Inactive")
              .length}
            <span class="text-xs font-normal text-rose-600/70 ml-1"
              >{$language === "vi" ? "đã cắt" : "terminated"}</span
            >
          </div>
          <div class="text-[10px] text-rose-600/80 dark:text-rose-500 mt-1">
            {$language === "vi"
              ? "Đã thu hồi cổng CPE"
              : "CPE & Port reclaimed"}
          </div>
        </div>

        <!-- Total Outstanding Due Balance -->
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-emerald-200 dark:border-emerald-900/40 shadow-sm col-span-2 sm:col-span-1"
        >
          <div
            class="text-[11px] font-mono text-emerald-600 dark:text-emerald-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi"
                ? "Tổng nợ cước"
                : "Outstanding Balance"}</span
            >
            <Receipt class="h-3.5 w-3.5 text-emerald-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono {totalDueAmountAllConnections >
            0
              ? 'text-rose-600 dark:text-rose-400'
              : 'text-emerald-600 dark:text-emerald-400'} mt-1.5"
          >
            ${totalDueAmountAllConnections.toFixed(2)}
          </div>
          <div class="text-[10px] text-slate-500 mt-1">
            {$language === "vi"
              ? "Hỗ trợ thu ngân tại quầy"
              : "Payable at counter POS"}
          </div>
        </div>
      </div>

      <!-- 2. Search, Filter & Connections Table List (Unified Box) -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm overflow-hidden"
      >
        <!-- Top: Search & Filter Toolbar -->
        <div
          class="p-5 border-b border-slate-200 dark:border-slate-800 space-y-4 bg-slate-50/40 dark:bg-slate-950/30"
        >
          <div
            class="flex flex-col md:flex-row gap-3 items-stretch md:items-center justify-between"
          >
            <!-- Search input -->
            <div class="relative flex-1">
              <Search
                class="absolute left-3.5 top-3 h-4 w-4 text-slate-400 dark:text-slate-500"
              />
              <input
                type="text"
                placeholder={$language === "vi"
                  ? "Tra cứu theo Mã tài khoản (16 ký tự), tên thuê bao, địa chỉ, SĐT, cổng NOC, IP..."
                  : "Search by 16-char Account ID, subscriber name, address, phone, NOC port, IP..."}
                bind:value={connSearchQuery}
                class="w-full pl-10 pr-4 py-2.5 text-sm bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-600 font-mono"
              />
            </div>

            <div class="flex items-center gap-2">
              <button
                onclick={handleQuickTrackConnection}
                class="px-4 py-2.5 rounded-lg text-sm font-bold bg-emerald-600 hover:bg-emerald-700 text-white transition shadow flex items-center justify-center space-x-1.5 whitespace-nowrap"
              >
                <Wifi class="h-4 w-4" />
                <span
                  >{$language === "vi"
                    ? "Tải hồ sơ & Mở chi tiết"
                    : "Retrieve & Open Details"}</span
                >
              </button>
            </div>
          </div>

          <!-- Filter rows: Status & Connection Type -->
          <div
            class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 pt-1"
          >
            <!-- Status Filter Pills (Horizontal row) -->
            <div
              class="flex items-center gap-1.5 overflow-x-auto whitespace-nowrap pb-1 max-w-full"
            >
              <button
                onclick={() => (connStatusFilter = "All")}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition {connStatusFilter ===
                'All'
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700'}"
              >
                {$language === "vi" ? "Tất cả" : "All"} ({$connections.length})
              </button>
              <button
                onclick={() => (connStatusFilter = "Active")}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition {connStatusFilter ===
                'Active'
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700'}"
              >
                {$language === "vi" ? "Hoạt động" : "Active"} ({$connections.filter(
                  (c) => c.status === "Active",
                ).length})
              </button>
              <button
                onclick={() => (connStatusFilter = "Temporarily Inactive")}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition {connStatusFilter ===
                'Temporarily Inactive'
                  ? 'bg-amber-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700'}"
              >
                {$language === "vi" ? "Tạm khóa" : "Inactive"} ({$connections.filter(
                  (c) => c.status === "Temporarily Inactive",
                ).length})
              </button>
              <button
                onclick={() => (connStatusFilter = "Permanently Inactive")}
                class="px-2.5 py-1 text-xs font-medium rounded-lg transition {connStatusFilter ===
                'Permanently Inactive'
                  ? 'bg-rose-600 text-white shadow-sm'
                  : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700 border border-slate-200 dark:border-slate-700'}"
              >
                {$language === "vi" ? "Khóa vĩnh viễn" : "Cut Off"} ({$connections.filter(
                  (c) => c.status === "Permanently Inactive",
                ).length})
              </button>
            </div>

            <!-- Connection Type Filter & Collapse Details Button -->
            <div class="flex items-center space-x-2 text-xs">
              <span
                class="text-slate-500 dark:text-slate-400 whitespace-nowrap"
              >
                {$language === "vi" ? "Loại dịch vụ:" : "Service Type:"}
              </span>
              <select
                bind:value={connTypeFilter}
                class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg px-2.5 py-1 text-slate-800 dark:text-slate-200 font-medium focus:outline-none focus:ring-1 focus:ring-emerald-500 text-xs"
              >
                <option value="All"
                  >{$language === "vi"
                    ? "Tất cả loại dịch vụ"
                    : "All Types"}</option
                >
                <option value="Broadband">Broadband (Cáp quang)</option>
                <option value="Landline">Landline (Cố định)</option>
                <option value="Dial-Up">Dial-Up (Quay số)</option>
              </select>
            </div>
          </div>
        </div>

        {#if filteredTrackedConnections.length === 0}
          <div class="py-16 text-center space-y-3">
            <Wifi class="h-10 w-10 text-slate-400 mx-auto opacity-50" />
            <div
              class="text-sm font-semibold text-slate-700 dark:text-slate-300 font-mono"
            >
              {$language === "vi"
                ? "Không tìm thấy thuê bao nào khớp với bộ lọc."
                : "No accounts match the current filter criteria."}
            </div>
            <button
              onclick={() => {
                connSearchQuery = "";
                connStatusFilter = "All";
                connTypeFilter = "All";
              }}
              class="px-4 py-1.5 text-xs font-semibold bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition"
            >
              {$language === "vi" ? "Đặt lại bộ lọc" : "Reset Filters"}
            </button>
          </div>
        {:else}
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm border-collapse">
              <thead
                class="bg-slate-50 dark:bg-slate-950/80 text-[11px] font-mono text-slate-600 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
              >
                <tr>
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "MÃ TÀI KHOẢN (16 KÝ TỰ)"
                      : "ACCOUNT ID (16-CHAR)"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "THUÊ BAO & ĐỊA CHỈ"
                      : "SUBSCRIBER & LOCATION"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "LOẠI DỊCH VỤ & GÓI CƯỚC"
                      : "SERVICE & PLAN"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "CỔNG NOC & IP"
                      : "NOC PORT & IP"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "DƯ NỢ CƯỚC ($)"
                      : "OUTSTANDING DUE ($)"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi" ? "TRẠNG THÁI" : "STATUS"}</th
                  >
                  <th class="px-4 py-3.5 text-right"
                    >{$language === "vi" ? "THAO TÁC" : "ACTIONS"}</th
                  >
                </tr>
              </thead>
              <tbody
                class="divide-y divide-slate-100 dark:divide-slate-800/60 font-sans"
              >
                {#each filteredTrackedConnections as conn (conn.accountId)}
                  {@const isExpanded = expandedConnAccountId === conn.accountId}
                  {@const due = getConnectionDueAmount(conn.accountId)}
                  <!-- Main Table Row -->
                  <tr
                    class="transition-colors hover:bg-emerald-50/40 dark:hover:bg-emerald-950/20 {isExpanded
                      ? 'bg-emerald-50/60 dark:bg-emerald-950/30'
                      : ''}"
                  >
                    <!-- Col 1: Account ID -->
                    <td class="px-4 py-4 align-top">
                      <div class="flex items-center space-x-1.5">
                        <span
                          class="font-mono font-bold text-emerald-600 dark:text-emerald-400 tracking-wider"
                        >
                          {conn.accountId}
                        </span>
                        <button
                          onclick={() =>
                            copyToClipboard(conn.accountId, "Account ID")}
                          class="p-1 rounded hover:bg-slate-200 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition"
                          title={$language === "vi"
                            ? "Sao chép mã tài khoản"
                            : "Copy Account ID"}
                        >
                          <Copy class="h-3.5 w-3.5" />
                        </button>
                      </div>
                      <div
                        class="text-[11px] font-mono text-slate-500 dark:text-slate-400 mt-1 flex items-center space-x-1"
                      >
                        <span>{$language === "vi" ? "Đơn gốc:" : "Order:"}</span
                        >
                        <span class="text-slate-700 dark:text-slate-300"
                          >#{conn.orderId}</span
                        >
                      </div>
                    </td>

                    <!-- Col 2: Subscriber & Location -->
                    <td class="px-4 py-4 align-top">
                      <div class="font-semibold text-slate-900 dark:text-white">
                        {conn.customerName}
                      </div>
                      <div
                        class="text-xs text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-1"
                      >
                        {conn.installationAddress}
                      </div>
                      <div
                        class="text-[11px] font-mono text-slate-500 dark:text-slate-400 mt-0.5"
                      >
                        {conn.customerPhone}
                      </div>
                    </td>

                    <!-- Col 3: Service Type & Plan -->
                    <td class="px-4 py-4 align-top">
                      <div class="flex items-center space-x-1.5">
                        {#if conn.connectionType === "Broadband"}
                          <span
                            class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-blue-100 dark:bg-blue-950 text-blue-800 dark:text-blue-300 border border-blue-200 dark:border-blue-800"
                          >
                            <Wifi class="h-3 w-3" />
                            <span>Broadband</span>
                          </span>
                        {:else if conn.connectionType === "Landline"}
                          <span
                            class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-purple-100 dark:bg-purple-950 text-purple-800 dark:text-purple-300 border border-purple-200 dark:border-purple-800"
                          >
                            <Phone class="h-3 w-3" />
                            <span>Landline</span>
                          </span>
                        {:else}
                          <span
                            class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-800"
                          >
                            <Radio class="h-3 w-3" />
                            <span>Dial-Up</span>
                          </span>
                        {/if}
                      </div>
                      <div
                        class="text-xs font-medium text-slate-800 dark:text-slate-200 mt-1"
                      >
                        {conn.planName}
                      </div>
                      <div class="text-[11px] font-mono text-slate-500">
                        ${conn.monthlyRental.toFixed(2)}/{$language === "vi"
                          ? "tháng"
                          : "mo"}
                      </div>
                    </td>

                    <!-- Col 4: NOC Port & IP -->
                    <td class="px-4 py-4 align-top font-mono text-xs">
                      <div class="font-bold text-slate-800 dark:text-slate-200">
                        {conn.portNumber || "PON-01/04"}
                      </div>
                      <div
                        class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5"
                      >
                        IP: {conn.ipAddress || "198.51.100.42"}
                      </div>
                      <div
                        class="text-[10px] text-slate-400 truncate max-w-[140px] mt-0.5"
                      >
                        CPE: {conn.assignedDeviceSerial || "Chưa gán"}
                      </div>
                    </td>

                    <!-- Col 5: Outstanding Due -->
                    <td class="px-4 py-4 align-top">
                      <div
                        class="font-mono text-base font-bold {due > 0
                          ? 'text-rose-600 dark:text-rose-400'
                          : 'text-emerald-600 dark:text-emerald-400'}"
                      >
                        ${due.toFixed(2)}
                      </div>
                      <span
                        class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-semibold mt-1 {due ===
                        0
                          ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300'
                          : 'bg-rose-100 dark:bg-rose-900/40 text-rose-700 dark:text-rose-300'}"
                      >
                        {due === 0
                          ? $language === "vi"
                            ? "Đã quyết toán"
                            : "Paid in Full"
                          : $language === "vi"
                            ? "Còn dư nợ"
                            : "Unpaid Balance"}
                      </span>
                    </td>

                    <!-- Col 6: Status -->
                    <td class="px-4 py-4 align-top">
                      <span
                        class="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full text-xs font-bold uppercase tracking-wider {conn.status ===
                        'Active'
                          ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-700'
                          : conn.status === 'Temporarily Inactive'
                            ? 'bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-700'
                            : 'bg-rose-100 dark:bg-rose-950 text-rose-800 dark:text-rose-300 border border-rose-300 dark:border-rose-700'}"
                      >
                        <span
                          class="h-2 w-2 rounded-full {conn.status === 'Active'
                            ? 'bg-emerald-500 animate-pulse'
                            : conn.status === 'Temporarily Inactive'
                              ? 'bg-amber-500'
                              : 'bg-rose-500'}"
                        ></span>
                        <span>
                          {$language === "vi"
                            ? conn.status === "Active"
                              ? "Hoạt động"
                              : conn.status === "Temporarily Inactive"
                                ? "Tạm khóa"
                                : "Khóa vĩnh viễn"
                            : conn.status}
                        </span>
                      </span>
                    </td>

                    <!-- Col 7: Action Button -->
                    <td
                      class="px-4 py-4 align-top text-right whitespace-nowrap"
                    >
                      <button
                        onclick={() => toggleConnDropdown(conn.accountId)}
                        class="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold transition shadow-xs {isExpanded
                          ? 'bg-emerald-600 text-white shadow-emerald-600/30'
                          : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-emerald-100 dark:hover:bg-emerald-950 hover:text-emerald-800 dark:hover:text-emerald-300 border border-slate-200 dark:border-slate-700'}"
                      >
                        <span
                          >{$language === "vi"
                            ? isExpanded
                              ? "Thu gọn"
                              : "Chi tiết"
                            : isExpanded
                              ? "Collapse"
                              : "Details"}</span
                        >
                        <ChevronDown
                          class="h-4 w-4 transition-transform duration-200 {isExpanded
                            ? 'rotate-180'
                            : ''}"
                        />
                      </button>
                    </td>
                  </tr>

                  <!-- DROPDOWN BOX CHI TIẾT CỦA MÃ TÀI KHOẢN -->
                  {#if isExpanded}
                    {@const connBills = getBillsForConnection(conn.accountId)}
                    <tr
                      class="bg-gradient-to-b from-emerald-50/50 to-slate-50 dark:from-emerald-950/20 dark:to-slate-950/40 border-b-2 border-emerald-400 dark:border-emerald-600/60"
                    >
                      <td colspan="7" class="p-4 sm:p-6">
                        <div
                          class="space-y-6 rounded-xl border border-emerald-200 dark:border-emerald-900/60 bg-white dark:bg-slate-900 p-5 sm:p-6 shadow-md"
                        >
                          <!-- Box Header -->
                          <div
                            class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-800 pb-4"
                          >
                            <div
                              class="flex items-start sm:items-center space-x-3"
                            >
                              <div
                                class="p-2.5 rounded-xl bg-emerald-100 dark:bg-emerald-950/80 text-emerald-700 dark:text-emerald-400 border border-emerald-300 dark:border-emerald-800"
                              >
                                <Wifi class="h-6 w-6" />
                              </div>
                              <div>
                                <div class="flex items-center space-x-3">
                                  <h4
                                    class="text-lg font-bold font-mono text-slate-900 dark:text-white"
                                  >
                                    {$language === "vi"
                                      ? "Mã tài khoản:"
                                      : "Account ID:"}
                                    <span
                                      class="text-emerald-600 dark:text-emerald-400 font-extrabold"
                                      >{conn.accountId}</span
                                    >
                                  </h4>
                                  <span
                                    class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider {conn.status ===
                                    'Active'
                                      ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-700'
                                      : conn.status === 'Temporarily Inactive'
                                        ? 'bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-700'
                                        : 'bg-rose-100 dark:bg-rose-950 text-rose-800 dark:text-rose-300 border border-rose-300 dark:border-rose-700'}"
                                  >
                                    {$language === "vi"
                                      ? conn.status === "Active"
                                        ? "Hoạt động bình thường"
                                        : conn.status === "Temporarily Inactive"
                                          ? "Tạm khóa dịch vụ"
                                          : "Khóa vĩnh viễn"
                                      : conn.status}
                                  </span>
                                </div>
                                <p
                                  class="text-xs text-slate-500 dark:text-slate-400 mt-1"
                                >
                                  {$language === "vi"
                                    ? "Thuê bao:"
                                    : "Subscriber:"}
                                  <strong
                                    class="text-slate-700 dark:text-slate-300"
                                    >{conn.customerName}</strong
                                  >
                                  • {$language === "vi"
                                    ? "Kích hoạt ngày"
                                    : "Activated on"}
                                  {conn.installedDate}
                                  • {$language === "vi"
                                    ? "Điện thoại:"
                                    : "Phone:"}
                                  {conn.customerPhone}
                                </p>
                              </div>
                            </div>

                            <!-- Header Actions -->
                            <div class="flex flex-wrap items-center gap-2">
                              {#if connBills.length > 0 && due > 0}
                                <button
                                  type="button"
                                  onclick={() =>
                                    handleOpenRetailPayment(connBills[0])}
                                  class="inline-flex items-center space-x-1.5 text-xs bg-emerald-600 hover:bg-emerald-700 text-white px-3 py-1.5 rounded-lg font-semibold transition shadow-xs"
                                >
                                  <CreditCard class="h-3.5 w-3.5" />
                                  <span
                                    >{$language === "vi"
                                      ? `Thu tiền ($${due.toFixed(2)})`
                                      : `Pay ($${due.toFixed(2)})`}</span
                                  >
                                </button>
                              {/if}

                              <button
                                onclick={() =>
                                  copyToClipboard(conn.accountId, "Account ID")}
                                class="inline-flex items-center space-x-1 text-xs border border-slate-200 dark:border-slate-800 px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition font-medium"
                              >
                                <Copy class="h-3.5 w-3.5" />
                                <span
                                  >{$language === "vi"
                                    ? "Sao chép Account ID"
                                    : "Copy Account ID"}</span
                                >
                              </button>

                              <button
                                onclick={() => (expandedConnAccountId = null)}
                                class="inline-flex items-center space-x-1 text-xs border border-slate-200 dark:border-slate-800 px-3 py-1.5 rounded-lg text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
                                title={$language === "vi"
                                  ? "Đóng hộp chi tiết"
                                  : "Close details"}
                              >
                                <X class="h-3.5 w-3.5" />
                                <span
                                  >{$language === "vi" ? "Đóng" : "Close"}</span
                                >
                              </button>
                            </div>
                          </div>

                          <!-- 4 Detail Blocks (Spec §9) -->
                          <div
                            class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 text-xs"
                          >
                            <!-- Block 1: Service Parameters -->
                            <div
                              class="p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-2"
                            >
                              <div
                                class="text-slate-400 font-bold uppercase tracking-wider flex items-center space-x-1.5"
                              >
                                <FileText
                                  class="h-3.5 w-3.5 text-emerald-600"
                                />
                                <span
                                  >{$language === "vi"
                                    ? "Thông số dịch vụ"
                                    : "Service Parameters"}</span
                                >
                              </div>
                              <div class="pt-1">
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Gói cước:"
                                    : "Plan:"}</span
                                >
                                <strong
                                  class="text-slate-900 dark:text-white ml-1"
                                  >{getPlanName(
                                    { name: conn.planName },
                                    $language,
                                  )}</strong
                                >
                              </div>
                              <div>
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Loại kết nối:"
                                    : "Connection:"}</span
                                >
                                <span
                                  class="font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                >
                                  {$language === "vi"
                                    ? conn.connectionType === "Broadband"
                                      ? "Cáp quang (Broadband)"
                                      : conn.connectionType === "Dial-Up"
                                        ? "Quay số (Dial-Up)"
                                        : "Cố định (Landline)"
                                    : conn.connectionType}
                                </span>
                              </div>
                              <div>
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Cước thuê tháng:"
                                    : "Monthly Rental:"}</span
                                >
                                <span
                                  class="font-mono font-bold text-slate-900 dark:text-white ml-1"
                                  >${conn.monthlyRental.toFixed(2)}</span
                                >
                              </div>
                              <div>
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Tiền đặt cọc:"
                                    : "Security Deposit:"}</span
                                >
                                <span
                                  class="font-mono text-slate-800 dark:text-slate-200 ml-1"
                                  >${conn.securityDeposit.toFixed(2)}</span
                                >
                              </div>
                            </div>

                            <!-- Block 2: Physical & Circuit Data -->
                            <div
                              class="p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-2"
                            >
                              <div
                                class="text-slate-400 font-bold uppercase tracking-wider flex items-center space-x-1.5"
                              >
                                <HardDrive
                                  class="h-3.5 w-3.5 text-emerald-600"
                                />
                                <span
                                  >{$language === "vi"
                                    ? "Dữ liệu vật lý & Tuyến cáp"
                                    : "Physical & Circuit Data"}</span
                                >
                              </div>
                              <div class="pt-1">
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Địa chỉ IP cấp:"
                                    : "Assigned IP:"}</span
                                >
                                <span
                                  class="font-mono font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                  >{conn.ipAddress || "Dynamic DHCP"}</span
                                >
                              </div>
                              <div>
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Cổng Switch/NOC:"
                                    : "Switch Port:"}</span
                                >
                                <span
                                  class="font-mono font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                  >{conn.portNumber || "PON-01"}</span
                                >
                              </div>
                              <div>
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Thiết bị cấp:"
                                    : "Assigned Device:"}</span
                                >
                                <span
                                  class="text-slate-800 dark:text-slate-200 ml-1"
                                  >{conn.assignedDeviceModel ||
                                    "Standard CPE"}</span
                                >
                              </div>
                              <div class="font-mono">
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Số sê-ri CPE:"
                                    : "Serial:"}</span
                                >
                                <span
                                  class="text-slate-800 dark:text-slate-200 ml-1"
                                  >{conn.assignedDeviceSerial ||
                                    "NX-AUTO-GEN"}</span
                                >
                              </div>
                            </div>

                            <!-- Block 3: Contact & Address -->
                            <div
                              class="p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-2"
                            >
                              <div
                                class="text-slate-400 font-bold uppercase tracking-wider flex items-center space-x-1.5"
                              >
                                <User class="h-3.5 w-3.5 text-emerald-600" />
                                <span
                                  >{$language === "vi"
                                    ? "Liên hệ & Địa chỉ"
                                    : "Contact & Address"}</span
                                >
                              </div>
                              <div class="pt-1">
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Điện thoại:"
                                    : "Phone:"}</span
                                >
                                <span
                                  class="font-mono font-semibold text-slate-800 dark:text-slate-200 ml-1"
                                  >{conn.customerPhone}</span
                                >
                              </div>
                              <div>
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Email:"
                                    : "Email:"}</span
                                >
                                <span
                                  class="text-slate-800 dark:text-slate-200 ml-1"
                                  >{conn.customerEmail}</span
                                >
                              </div>
                              <div>
                                <span class="text-slate-500"
                                  >{$language === "vi"
                                    ? "Địa chỉ lắp đặt:"
                                    : "Installation:"}</span
                                >
                                <span
                                  class="text-slate-800 dark:text-slate-200 ml-1"
                                  >{conn.installationAddress}</span
                                >
                              </div>
                              {#if conn.lastStatusReason}
                                <div class="text-amber-500 pt-1">
                                  <span class="font-semibold"
                                    >{$language === "vi"
                                      ? "Lý do trạng thái:"
                                      : "Reason:"}</span
                                  >
                                  <p class="text-[11px] mt-0.5">
                                    {conn.lastStatusReason}
                                  </p>
                                </div>
                              {/if}
                            </div>

                            <!-- Block 4: Financial & Due Status -->
                            <div
                              class="p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-2"
                            >
                              <div
                                class="text-slate-400 font-bold uppercase tracking-wider flex items-center space-x-1.5"
                              >
                                <Receipt class="h-3.5 w-3.5 text-emerald-600" />
                                <span
                                  >{$language === "vi"
                                    ? "Tài chính & Nợ cước"
                                    : "Billing & Due Status"}</span
                                >
                              </div>
                              <div class="pt-1">
                                <span class="block text-[11px] text-slate-500"
                                  >{$language === "vi"
                                    ? "Số tiền còn nợ:"
                                    : "Outstanding Due:"}</span
                                >
                                <span
                                  class="text-lg font-bold font-mono {due > 0
                                    ? 'text-rose-600 dark:text-rose-400'
                                    : 'text-emerald-600 dark:text-emerald-400'}"
                                >
                                  ${due.toFixed(2)}
                                </span>
                              </div>
                              <div>
                                <span
                                  class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-semibold {due ===
                                  0
                                    ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-300'
                                    : 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-300'}"
                                >
                                  {due === 0
                                    ? $language === "vi"
                                      ? "Đã quyết toán đủ"
                                      : "Paid in Full"
                                    : $language === "vi"
                                      ? "Chưa thanh toán"
                                      : "Balance Outstanding"}
                                </span>
                              </div>
                              <div class="text-[11px] text-slate-500 pt-1">
                                {$language === "vi"
                                  ? `Tổng số hóa đơn: ${connBills.length}`
                                  : `Invoices on file: ${connBills.length}`}
                              </div>
                              {#if connBills.length > 0 && due > 0}
                                <button
                                  type="button"
                                  onclick={() =>
                                    handleOpenRetailPayment(connBills[0])}
                                  class="w-full mt-2 py-1.5 px-2.5 rounded bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs transition flex items-center justify-center space-x-1 shadow-sm"
                                >
                                  <CreditCard class="h-3.5 w-3.5" />
                                  <span
                                    >{$language === "vi"
                                      ? "Thu tiền tại quầy"
                                      : "Pay at Counter"}</span
                                  >
                                </button>
                              {/if}
                            </div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  {/if}
                {/each}
              </tbody>
            </table>
          </div>
        {/if}
      </div>
    </div>
  {/if}

  <!-- TAB 4: PAYMENT RECORDS -->
  {#if activeTab === "payment-records"}
    <div class="space-y-6">
      <div
        class="bg-white dark:bg-slate-900 p-4 rounded-xl border border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-3"
      >
        <div class="relative w-full sm:w-80">
          <Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
          <input
            type="text"
            placeholder={$language === "vi"
              ? "Lọc theo mã tài khoản hoặc họ tên..."
              : "Filter by Account ID or Name..."}
            bind:value={paymentAccountQuery}
            class="w-full pl-9 pr-4 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-emerald-500 font-mono"
          />
        </div>

        <div class="text-xs text-slate-500">
          {$language === "vi"
            ? "Hiển thị sổ cái giao dịch của tất cả khách hàng bán lẻ"
            : "Showing transaction ledgers across all retail customers"}
        </div>
      </div>

      <!-- Invoices & Payments Table -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead
              class="bg-slate-50 dark:bg-slate-800/60 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Mã hóa đơn" : "Invoice Number"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Mã tài khoản 16 ký tự"
                    : "16-character Account ID"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Tên thuê bao" : "Subscriber Name"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Kỳ cước" : "Billing Cycle"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Tổng tiền ($)"
                    : "Total Amount ($)"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Đã thanh toán ($)"
                    : "Amount Paid ($)"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Còn nợ ($)" : "Due Balance ($)"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Trạng thái" : "Payment Status"}</th
                >

              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              {#each $bills.filter((b) => b.accountId
                    .toLowerCase()
                    .includes(paymentAccountQuery.toLowerCase()) || b.customerName
                    .toLowerCase()
                    .includes(paymentAccountQuery.toLowerCase())) as bill (bill.id)}
                <tr
                  class="hover:bg-slate-50 dark:hover:bg-slate-800/40 cursor-pointer"
                  onclick={() => handleOpenRetailPayment(bill)}
                >
                  <td
                    class="px-4 py-3 font-mono font-medium text-slate-900 dark:text-slate-100"
                    >{bill.invoiceNumber}</td
                  >
                  <td
                    class="px-4 py-3 font-mono text-xs text-emerald-600 dark:text-emerald-400"
                    >{bill.accountId}</td
                  >
                  <td
                    class="px-4 py-3 font-semibold text-slate-900 dark:text-white"
                    >{bill.customerName}</td
                  >
                  <td class="px-4 py-3 text-xs text-slate-500"
                    >{bill.billingMonth}</td
                  >
                  <td class="px-4 py-3 font-mono tabular-nums font-medium"
                    >${bill.totalAmount.toFixed(2)}</td
                  >
                  <td
                    class="px-4 py-3 font-mono tabular-nums text-emerald-600 dark:text-emerald-400 font-semibold"
                    >${bill.amountPaid.toFixed(2)}</td
                  >
                  <td
                    class="px-4 py-3 font-mono tabular-nums text-rose-600 dark:text-rose-400 font-semibold"
                    >${bill.dueAmount.toFixed(2)}</td
                  >
                  <td class="px-4 py-3">
                    <span
                      class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold {bill.status ===
                      'Paid'
                        ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300'
                        : bill.status === 'Partially Paid'
                          ? 'bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300'
                          : 'bg-rose-100 dark:bg-rose-900/40 text-rose-700 dark:text-rose-300'}"
                    >
                      {$language === "vi"
                        ? bill.status === "Paid"
                          ? "Đã thanh toán"
                          : bill.status === "Partially Paid"
                            ? "Một phần"
                            : "Chưa thanh toán"
                        : bill.status}
                    </span>
                  </td>

                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  {/if}

  <!-- CONFIRM ORDER & ISSUE CODE MODAL (Màn hình xác nhận tạo đơn và cấp mã) -->
  {#if isConfirmOrderModalOpen && currentPlan}
    {@const base = currentPlan.monthlyRental + currentPlan.securityDeposit}
    {@const disc = (base * bulkDiscountPercent) / 100}
    {@const taxed = (base - disc) * 1.1224}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 backdrop-blur-sm p-4 animate-in fade-in duration-200"
    >
      <div
        class="w-full max-w-xl rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
      >
        <!-- Header -->
        <div
          class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50 dark:bg-slate-950"
        >
          <div class="flex items-center space-x-3">
            <div
              class="h-10 w-10 rounded-xl bg-emerald-100 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 flex items-center justify-center"
            >
              <ShieldCheck class="h-6 w-6" />
            </div>
            <div>
              <h3 class="font-bold text-base text-slate-900 dark:text-white">
                {$language === "vi"
                  ? "Xác nhận tạo đơn hàng & Cấp mã"
                  : "Confirm Order & Generate ID"}
              </h3>
              <p class="text-xs text-slate-500">
                {$language === "vi"
                  ? "Kiểm tra thông tin chi tiết trước khi cấp mã và chuyển Kỹ thuật"
                  : "Verify details before issuing order code to technical team"}
              </p>
            </div>
          </div>
          <button
            type="button"
            onclick={() => (isConfirmOrderModalOpen = false)}
            disabled={isCreatingOrder}
            class="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition cursor-pointer"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <!-- Body -->
        <div class="p-6 overflow-y-auto space-y-4 text-xs">
          <!-- Customer Info Card -->
          <div
            class="rounded-xl border border-slate-200 dark:border-slate-800 p-4 bg-slate-50/50 dark:bg-slate-950/50 space-y-3"
          >
            <div
              class="font-bold text-xs uppercase tracking-wider text-emerald-600 dark:text-emerald-400 flex items-center space-x-1.5"
            >
              <User class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Thông tin khách hàng"
                  : "Customer Information"}</span
              >
            </div>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi" ? "Họ và tên:" : "Full Name:"}</span
                >
                <span class="font-semibold text-slate-900 dark:text-white"
                  >{customerName}</span
                >
              </div>
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi"
                    ? "Số điện thoại:"
                    : "Phone Number:"}</span
                >
                <span
                  class="font-mono font-semibold text-slate-900 dark:text-white"
                  >{customerPhone}</span
                >
              </div>
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi" ? "Email liên hệ:" : "Email:"}</span
                >
                <span class="text-slate-800 dark:text-slate-200 font-medium"
                  >{customerEmail}</span
                >
              </div>
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi"
                    ? "Giấy tờ tùy thân:"
                    : "ID Proof Document:"}</span
                >
                <span
                  class="font-mono font-semibold text-slate-900 dark:text-white"
                >
                  {idProofType === "National ID Card"
                    ? "CCCD"
                    : idProofType === "Passport"
                      ? $language === "vi"
                        ? "Hộ chiếu"
                        : "Passport"
                      : $language === "vi"
                        ? "Bằng lái xe"
                        : "Driver's License"}: {idProofNumber}
                </span>
              </div>
              <div class="sm:col-span-2">
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi"
                    ? "Địa chỉ lắp đặt thực tế:"
                    : "Physical Installation Address:"}</span
                >
                <div
                  class="font-semibold text-slate-900 dark:text-white flex items-start space-x-1.5"
                >
                  <MapPin class="h-4 w-4 text-rose-500 shrink-0 mt-0.5" />
                  <span>{fullInstallationAddress}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Service & Plan Info Card -->
          <div
            class="rounded-xl border border-slate-200 dark:border-slate-800 p-4 bg-slate-50/50 dark:bg-slate-950/50 space-y-3"
          >
            <div
              class="font-bold text-xs uppercase tracking-wider text-emerald-600 dark:text-emerald-400 flex items-center space-x-1.5"
            >
              <Wifi class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Dịch vụ & Gói cước đăng ký"
                  : "Service & Plan Details"}</span
              >
            </div>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi"
                    ? "Loại kết nối:"
                    : "Connection Type:"}</span
                >
                <span class="font-semibold text-slate-900 dark:text-white"
                  >{connectionType}</span
                >
              </div>
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi"
                    ? "Gói cước đã chọn:"
                    : "Selected Plan:"}</span
                >
                <span
                  class="font-semibold text-emerald-600 dark:text-emerald-400"
                  >{getPlanName(currentPlan, $language)}</span
                >
              </div>
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi"
                    ? "Băng thông / Tốc độ:"
                    : "Bandwidth / Speed:"}</span
                >
                <span class="font-mono text-slate-700 dark:text-slate-300"
                  >{getPlanSpeedOrBandwidth(
                    currentPlan.speedOrBandwidth,
                    $language,
                  )}</span
                >
              </div>
              <div>
                <span class="text-slate-500 block mb-0.5"
                  >{$language === "vi"
                    ? "Chi nhánh phụ trách:"
                    : "Processing Branch:"}</span
                >
                <span class="font-medium text-slate-800 dark:text-slate-200">
                  {activeBranchShop
                    ? `${activeBranchShop.name} (${activeBranchShop.shopCode})`
                    : activeBranchCode ?? "SH-01"}
                </span>
              </div>
            </div>
          </div>

          <!-- Cost Breakdown Card -->
          <div
            class="rounded-xl border border-slate-200 dark:border-slate-800 p-4 bg-slate-50/50 dark:bg-slate-950/50 space-y-2"
          >
            <div
              class="font-bold text-xs uppercase tracking-wider text-emerald-600 dark:text-emerald-400 flex items-center space-x-1.5"
            >
              <Receipt class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Bảng kê chi phí ban đầu"
                  : "Initial Cost Breakdown"}</span
              >
            </div>
            <div class="space-y-1.5 pt-1">
              <div
                class="flex justify-between text-slate-600 dark:text-slate-400"
              >
                <span
                  >{$language === "vi"
                    ? "Cước thuê tháng đầu:"
                    : "First Month Rental:"}</span
                >
                <span class="font-mono font-medium"
                  >${currentPlan.monthlyRental.toFixed(2)}</span
                >
              </div>
              <div
                class="flex justify-between text-slate-600 dark:text-slate-400"
              >
                <span
                  >{$language === "vi"
                    ? "Tiền đặt cọc thiết bị:"
                    : "Security Deposit:"}</span
                >
                <span class="font-mono font-medium"
                  >${currentPlan.securityDeposit.toFixed(2)}</span
                >
              </div>
              {#if bulkDiscountPercent > 0}
                <div
                  class="flex justify-between text-emerald-600 dark:text-emerald-400"
                >
                  <span
                    >{$language === "vi"
                      ? `Chiết khấu số lượng (−${bulkDiscountPercent}%):`
                      : `Bulk Discount (−${bulkDiscountPercent}%):`}</span
                  >
                  <span class="font-mono font-medium">−${disc.toFixed(2)}</span>
                </div>
              {/if}
              <div
                class="flex justify-between text-slate-600 dark:text-slate-400"
              >
                <span
                  >{$language === "vi"
                    ? "Thuế dịch vụ (12.24%):"
                    : "Service Tax (12.24%):"}</span
                >
                <span class="font-mono font-medium"
                  >${(((base - disc) * 12.24) / 100).toFixed(2)}</span
                >
              </div>
              <div
                class="border-t border-slate-200 dark:border-slate-800 pt-2 flex justify-between font-bold text-sm text-slate-900 dark:text-white"
              >
                <span
                  >{$language === "vi"
                    ? "Tổng thanh toán ban đầu:"
                    : "Total Initial Due:"}</span
                >
                <span
                  class="font-mono text-emerald-600 dark:text-emerald-400 font-extrabold text-base"
                  >${taxed.toFixed(2)}</span
                >
              </div>
            </div>
          </div>
        </div>

        <!-- Footer Actions -->
        <div
          class="px-6 py-4 border-t border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 flex items-center justify-end space-x-3"
        >
          <button
            type="button"
            onclick={() => (isConfirmOrderModalOpen = false)}
            disabled={isCreatingOrder}
            class="px-4 py-2 rounded-xl text-xs font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-800 transition cursor-pointer"
          >
            {$language === "vi" ? "Quay lại chỉnh sửa" : "Back to Edit"}
          </button>
          <button
            type="button"
            onclick={executeConfirmOrder}
            disabled={isCreatingOrder}
            class="px-5 py-2.5 rounded-xl text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white transition shadow-md flex items-center space-x-2 disabled:opacity-50 cursor-pointer"
          >
            {#if isCreatingOrder}
              <RefreshCw class="h-4 w-4 animate-spin" />
              <span
                >{$language === "vi"
                  ? "Đang xử lý..."
                  : "Processing..."}</span
              >
            {:else}
              <CheckCircle2 class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Xác nhận tạo đơn & Cấp mã"
                  : "Confirm & Issue Order ID"}</span
              >
            {/if}
          </button>
        </div>
      </div>
    </div>
  {/if}

  <!-- ORDER CREATED SUCCESS MODAL (With 11-character Order ID) -->
  {#if placedOrder}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4"
    >
      <div
        class="w-full max-w-md rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl space-y-4 text-center"
      >
        <div
          class="h-12 w-12 rounded-full bg-emerald-100 dark:bg-emerald-900/40 text-emerald-600 dark:text-emerald-400 mx-auto flex items-center justify-center"
        >
          <CheckCircle2 class="h-6 w-6" />
        </div>

        <div>
          <h3 class="text-lg font-bold text-slate-900 dark:text-white">
            {$language === "vi"
              ? "Đã tạo đơn hàng mới thành công!"
              : "New Order Successfully Booked!"}
          </h3>
          <p class="text-xs text-slate-500 mt-1">
            {$language === "vi"
              ? "Đơn hàng đã được chuyển thẳng tới hàng đợi thẩm định kỹ thuật hiện trường."
              : "Order routed directly to the Technical Staff Feasibility Queue."}
          </p>
        </div>

        <!-- Highlighted 11-character Order ID -->
        <div
          class="p-4 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-1"
        >
          <div class="text-xs text-slate-400 font-semibold uppercase">
            {$language === "vi"
              ? "Mã đơn hàng 11 ký tự chính thức"
              : "Official 11-Digit Order ID"}
          </div>
          <div
            class="text-2xl font-mono font-extrabold text-emerald-600 dark:text-emerald-400 tracking-wider"
          >
            {placedOrder.id}
          </div>
          <div class="text-[11px] text-slate-500">
            {$language === "vi" ? "Khách hàng:" : "Customer:"}
            {placedOrder.customerName} • {getPlanName(
              { name: placedOrder.planName },
              $language,
            )}
          </div>
        </div>

        <!-- The 16-character Account ID is issued later, on feasibility confirmation -->
        <div
          class="p-4 rounded-xl bg-sky-50 dark:bg-sky-950/30 border border-sky-200 dark:border-sky-800/60 space-y-1"
        >
          <div
            class="text-xs text-sky-600 dark:text-sky-400 font-semibold uppercase"
          >
            {$language === "vi"
              ? "Đang chờ cấp mã tài khoản"
              : "Account ID pending"}
          </div>
          <div class="text-[11px] text-slate-500">
            {$language === "vi"
              ? "Mã tài khoản 16 ký tự của khách hàng (thông tin đăng nhập duy nhất) sẽ được cấp sau khi bộ phận kỹ thuật xác nhận tuyến cáp khả thi. Trong thời gian này, khách hàng sử dụng mã đơn này để tra cứu."
              : "The customer's 16-character Account ID — their only sign-in credential — is issued once technical confirms the line is feasible. Until then they track this order code."}
          </div>
        </div>

        <div class="flex gap-2 justify-center">
          <button
            onclick={() => copyToClipboard(placedOrder!.id, "Order ID")}
            class="px-4 py-2 rounded-lg text-xs font-medium border border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800 flex items-center space-x-1.5"
          >
            <Copy class="h-3.5 w-3.5" />
            <span
              >{$language === "vi" ? "Sao chép mã đơn" : "Copy Order ID"}</span
            >
          </button>
          <button
            onclick={trackThisOrder}
            class="px-4 py-2 rounded-lg text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white transition flex items-center space-x-1.5"
          >
            <span
              >{$language === "vi"
                ? "Theo dõi đơn này"
                : "Track This Order"}</span
            >
            <ArrowRight class="h-3.5 w-3.5" />
          </button>
        </div>
      </div>
    </div>
  {/if}

  <!-- RETAIL STORE PAYMENT MODAL (Cập nhật lịch sử thanh toán vào hệ thống) -->
  {#if isPaymentModalOpen && targetBillForPayment}
    <div
      class="fixed inset-0 z-[100] flex justify-end bg-slate-950/50 backdrop-blur-sm transition-opacity"
      onclick={(e) => {
        if (e.target === e.currentTarget) isPaymentModalOpen = false;
      }}
    >
      <div
        class="w-full sm:w-[480px] h-full bg-white dark:bg-slate-900 shadow-2xl overflow-y-auto transform transition-transform animate-in slide-in-from-right duration-300 flex flex-col p-6 space-y-5"
        onclick={(e) => e.stopPropagation()}
      >
        <div
          class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 pb-3"
        >
          <div class="flex items-center space-x-2">
            <div
              class="h-8 w-8 rounded-lg bg-emerald-100 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 flex items-center justify-center"
            >
              <CreditCard class="h-4 w-4" />
            </div>
            <div>
              <h3 class="font-bold text-slate-900 dark:text-white text-sm">
                {$language === "vi"
                  ? "Thu tiền tại quầy & Cập nhật thanh toán"
                  : "Counter Payment & Settlement"}
              </h3>
              <p class="text-[11px] text-slate-500 font-mono">
                {targetBillForPayment.invoiceNumber}
              </p>
            </div>
          </div>
          <button
            onclick={() => (isPaymentModalOpen = false)}
            class="text-slate-400 hover:text-slate-600 dark:hover:text-white"
          >
            <X class="h-4 w-4" />
          </button>
        </div>

        <div
          class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-xs space-y-1.5 font-mono"
        >
          <div class="flex justify-between font-sans">
            <span class="text-slate-500"
              >{$language === "vi" ? "Khách hàng:" : "Customer:"}</span
            >
            <strong class="text-slate-900 dark:text-white"
              >{targetBillForPayment.customerName}</strong
            >
          </div>
          <div class="flex justify-between">
            <span class="text-slate-500"
              >{$language === "vi" ? "Mã tài khoản:" : "Account ID:"}</span
            >
            <span class="text-emerald-600 dark:text-emerald-400 font-bold"
              >{targetBillForPayment.accountId}</span
            >
          </div>
          <div
            class="flex justify-between border-t border-slate-200 dark:border-slate-800 pt-1.5"
          >
            <span class="text-slate-500"
              >{$language === "vi" ? "Tổng hóa đơn:" : "Total Bill:"}</span
            >
            <span class="text-slate-900 dark:text-white font-bold"
              >${targetBillForPayment.totalAmount.toFixed(2)}</span
            >
          </div>
          <div class="flex justify-between">
            <span class="text-slate-500"
              >{$language === "vi" ? "Đã thanh toán:" : "Paid:"}</span
            >
            <span class="text-emerald-600 dark:text-emerald-400"
              >${targetBillForPayment.amountPaid.toFixed(2)}</span
            >
          </div>
          <div class="flex justify-between">
            <span class="text-slate-500 font-bold"
              >{$language === "vi" ? "Công nợ còn lại:" : "Due Balance:"}</span
            >
            <span class="text-rose-600 dark:text-rose-400 font-bold"
              >${targetBillForPayment.dueAmount.toFixed(2)}</span
            >
          </div>
        </div>

        {#if targetBillForPayment.status !== 'Paid'}
        <form onsubmit={handleConfirmRetailPayment} class="space-y-4 text-xs border-t border-slate-100 dark:border-slate-800 pt-4 mt-2">
          <div>
            <label
              class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Số tiền thanh toán ($) *"
                : "Payment Amount ($) *"}
            </label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              required
              bind:value={retailPaymentAmount}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg font-mono font-bold text-sm text-slate-900 dark:text-white"
            />
            <button
              type="button"
              onclick={() =>
                (retailPaymentAmount = targetBillForPayment?.dueAmount || 0)}
              class="text-[11px] text-emerald-600 dark:text-emerald-400 hover:underline mt-1 font-medium"
            >
              {$language === "vi" ? "Thanh toán toàn bộ số nợ" : "Pay full due"}
              (${(targetBillForPayment?.dueAmount ?? 0).toFixed(2)})
            </button>
          </div>

          <div>
            <label
              class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Hình thức thanh toán tại quầy"
                : "Payment Tender Mode"}
            </label>
            <select
              bind:value={retailPaymentMode}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white"
            >
              <option value="Cash"
                >{$language === "vi"
                  ? "Tiền mặt (Tại quầy)"
                  : "Cash (Counter Tender)"}</option
              >
              <option value="Credit/Debit Card"
                >{$language === "vi"
                  ? "Thẻ tín dụng / Ghi nợ (POS)"
                  : "Credit/Debit Card (POS)"}</option
              >
              <option value="UPI/Digital Wallet"
                >{$language === "vi"
                  ? "Ví điện tử / QR Code"
                  : "UPI / Digital Wallet"}</option
              >
              <option value="Cheque"
                >{$language === "vi"
                  ? "Séc ngân hàng"
                  : "Cheque / Draft"}</option
              >
              <option value="Bank Transfer/NEFT"
                >{$language === "vi"
                  ? "Chuyển khoản trực tiếp"
                  : "Bank Transfer"}</option
              >
            </select>
          </div>

          <div>
            <label
              class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Mã tham chiếu biên lai / Giao dịch"
                : "Receipt / Reference #"}
            </label>
            <input
              type="text"
              bind:value={retailPaymentRef}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg font-mono text-slate-900 dark:text-white"
            />
          </div>

          <div
            class="flex justify-end gap-2 pt-2 border-t border-slate-100 dark:border-slate-800"
          >
            <button
              type="button"
              onclick={() => (isPaymentModalOpen = false)}
              class="px-4 py-2 rounded-lg font-semibold bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300"
            >
              {$language === "vi" ? "Hủy" : "Cancel"}
            </button>
            <button
              type="submit"
              class="px-5 py-2 rounded-lg font-bold bg-emerald-600 hover:bg-emerald-700 text-white shadow"
            >
              {$language === "vi" ? "Ghi nhận thanh toán" : "Record Payment"}
            </button>
          </div>
        </form>
        {:else}
          <div class="mt-4 p-4 rounded-xl bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800 flex items-center justify-center space-x-2 text-emerald-700 dark:text-emerald-400">
            <CreditCard class="h-5 w-5" />
            <span class="font-bold">
              {$language === "vi" ? "Hóa đơn này đã được thanh toán đầy đủ." : "This bill has been fully paid."}
            </span>
          </div>
        {/if}
      </div>
    </div>
  {/if}

  <!-- ==================== MODAL PHÂN CÔNG KỸ THUẬT VIÊN KHẢO SÁT ==================== -->
  {#if isAssignTechModalOpen && targetOrderForTechAssign}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm animate-fade-in overflow-y-auto"
    >
      <div
        class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-2xl w-full max-w-2xl overflow-hidden my-8"
      >
        <!-- Modal Header -->
        <div
          class="p-5 border-b border-slate-200 dark:border-slate-800 bg-gradient-to-r from-emerald-600 via-teal-600 to-emerald-700 text-white flex items-center justify-between"
        >
          <div class="flex items-center space-x-3">
            <div
              class="p-2.5 rounded-xl bg-white/10 backdrop-blur-sm border border-white/20"
            >
              <Wrench class="h-6 w-6 text-white" />
            </div>
            <div>
              <h3
                class="font-bold text-lg leading-tight flex items-center gap-2"
              >
                <span
                  >{$language === "vi"
                    ? "Phân công kỹ thuật viên khảo sát"
                    : "Assign Survey Field Engineer"}</span
                >
                <span
                  class="px-2 py-0.5 rounded-md text-xs font-mono bg-white/20 font-bold"
                >
                  {targetOrderForTechAssign.id}
                </span>
              </h3>
              <p class="text-xs text-emerald-100 mt-0.5">
                {$language === "vi"
                  ? "Chỉ định kỹ thuật viên hiện trường chịu trách nhiệm đo kiểm cáp & hộp chia DP"
                  : "Designate a field engineer to perform line feasibility and DP box testing"}
              </p>
            </div>
          </div>
          <button
            type="button"
            onclick={() => (isAssignTechModalOpen = false)}
            class="p-1.5 rounded-lg bg-white/10 hover:bg-white/20 text-white transition"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <div class="p-6 space-y-5">
          <!-- Order Summary Card -->
          <div
            class="p-4 rounded-xl bg-slate-50 dark:bg-slate-950/80 border border-slate-200 dark:border-slate-800 text-xs grid grid-cols-1 sm:grid-cols-2 gap-3"
          >
            <div>
              <span class="text-slate-500 block"
                >{$language === "vi" ? "Khách hàng:" : "Customer:"}</span
              >
              <strong class="text-slate-900 dark:text-white text-sm"
                >{targetOrderForTechAssign.customerName}</strong
              >
              <span class="font-mono text-slate-500 block"
                >{targetOrderForTechAssign.customerPhone}</span
              >
            </div>
            <div>
              <span class="text-slate-500 block"
                >{$language === "vi"
                  ? "Địa chỉ lắp đặt:"
                  : "Installation Address:"}</span
              >
              <span class="text-slate-800 dark:text-slate-200 font-medium"
                >{targetOrderForTechAssign.installationAddress}</span
              >
            </div>
            <div>
              <span class="text-slate-500 block"
                >{$language === "vi"
                  ? "Dịch vụ & Gói cước:"
                  : "Plan & Service:"}</span
              >
              <div class="flex items-center gap-2 mt-0.5">
                <span
                  class="px-2 py-0.5 rounded text-[11px] font-semibold bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300"
                >
                  {targetOrderForTechAssign.connectionType}
                </span>
                <strong class="text-slate-900 dark:text-white"
                  >{targetOrderForTechAssign.planName}</strong
                >
              </div>
            </div>
            <div>
              <span class="text-slate-500 block"
                >{$language === "vi"
                  ? "Điểm quầy phụ trách:"
                  : "Branch Desk:"}</span
              >
              <span
                class="font-mono text-slate-800 dark:text-slate-200 font-semibold"
                >{targetOrderForTechAssign.retailOutletCode}</span
              >
              <span class="text-slate-500 text-[11px]">
                ({targetOrderForTechAssign.retailEmployeeName})</span
              >
            </div>
          </div>

          <!-- Section: Select Technician -->
          <div class="space-y-3">
            <div
              class="flex flex-col sm:flex-row sm:items-center justify-between gap-2"
            >
              <label
                class="font-bold text-xs uppercase tracking-wider text-slate-700 dark:text-slate-300 flex items-center gap-1.5"
              >
                <UserCheck class="h-4 w-4 text-emerald-600" />
                <span
                  >{$language === "vi"
                    ? "Chọn kỹ thuật viên đảm nhiệm (*)"
                    : "Select Field Engineer (*)"}</span
                >
              </label>

              <!-- Search tech -->
              <div class="relative max-w-xs w-full">
                <Search
                  class="h-3.5 w-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400"
                />
                <input
                  type="text"
                  bind:value={techSearchQuery}
                  placeholder={$language === "vi"
                    ? "Tìm kỹ thuật viên..."
                    : "Search tech..."}
                  class="w-full pl-8 pr-3 py-1.5 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white"
                />
              </div>
            </div>

            <!-- Technicians Grid -->
            <div
              class="grid grid-cols-1 sm:grid-cols-2 gap-2.5 max-h-60 overflow-y-auto pr-1"
            >
              {#each filteredAvailableTechnicians as tech (tech.id)}
                {@const isSelected = selectedTechnicianId === tech.id}
                {@const pendingCount = getTechnicianPendingCount(tech.name)}
                <button
                  type="button"
                  onclick={() => (selectedTechnicianId = tech.id)}
                  class="p-3 rounded-xl border text-left transition-all flex flex-col justify-between {isSelected
                    ? 'border-emerald-500 bg-emerald-50/70 dark:bg-emerald-950/40 ring-2 ring-emerald-500/30 shadow-sm'
                    : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:border-slate-300 dark:hover:border-slate-700'}"
                >
                  <div class="flex items-start justify-between gap-2">
                    <div class="flex items-center space-x-2.5">
                      <div
                        class="h-8 w-8 rounded-full bg-emerald-100 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300 flex items-center justify-center font-bold text-xs shrink-0 border border-emerald-300 dark:border-emerald-800"
                      >
                        {tech.name
                          .split(" ")
                          .map((n) => n[0])
                          .join("")
                          .slice(0, 2)}
                      </div>
                      <div>
                        <div
                          class="font-bold text-xs text-slate-900 dark:text-white flex items-center gap-1.5"
                        >
                          <span>{tech.name}</span>
                          <span class="font-mono text-[10px] text-slate-400"
                            >({tech.employeeCode})</span
                          >
                        </div>
                        <div
                          class="text-[11px] text-slate-500 dark:text-slate-400 font-mono"
                        >
                          {tech.phone}
                        </div>
                      </div>
                    </div>
                    <div
                      class="h-4 w-4 rounded-full border flex items-center justify-center {isSelected
                        ? 'border-emerald-600 bg-emerald-600 text-white'
                        : 'border-slate-300 dark:border-slate-700'}"
                    >
                      {#if isSelected}
                        <div class="h-1.5 w-1.5 rounded-full bg-white"></div>
                      {/if}
                    </div>
                  </div>

                  <div
                    class="mt-2.5 pt-2 border-t border-slate-100 dark:border-slate-800/80 flex items-center justify-between text-[11px]"
                  >
                    <span class="text-slate-500 truncate max-w-[130px]">
                      {tech.retailShopAssigned || tech.department}
                    </span>
                    <span
                      class="px-2 py-0.5 rounded-full font-medium text-[10px] {pendingCount ===
                      0
                        ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300'
                        : pendingCount <= 2
                          ? 'bg-sky-100 dark:bg-sky-950 text-sky-700 dark:text-sky-300'
                          : 'bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-300'}"
                    >
                      {pendingCount === 0
                        ? $language === "vi"
                          ? "Sẵn sàng (0 việc)"
                          : "Free (0 tasks)"
                        : $language === "vi"
                          ? `${pendingCount} việc đang chờ`
                          : `${pendingCount} active`}
                    </span>
                  </div>
                </button>
              {/each}
            </div>
          </div>

          <!-- Section: Optional Notes -->
          <div>
            <label
              class="block font-semibold text-xs text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Ghi chú khảo sát / Hướng dẫn thêm (Tùy chọn):"
                : "Survey Instructions / Notes (Optional):"}
            </label>
            <textarea
              bind:value={assignSurveyNotes}
              rows="2"
              placeholder={$language === "vi"
                ? "Ví dụ: Khách yêu cầu đo kiểm cáp trước 11h trưa, kiểm tra hộp chia DP tầng 3..."
                : "e.g., Customer requested morning appointment, inspect DP box on 3rd floor..."}
              class="w-full px-3 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-emerald-500"
            ></textarea>
          </div>

          <!-- Modal Actions -->
          <div
            class="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 dark:border-slate-800"
          >
            <button
              type="button"
              onclick={() => (isAssignTechModalOpen = false)}
              class="px-4 py-2 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition"
            >
              {$language === "vi" ? "Hủy bỏ" : "Cancel"}
            </button>
            <button
              type="button"
              onclick={handleConfirmAssignTechnician}
              class="px-5 py-2 text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg transition shadow-md flex items-center gap-1.5"
            >
              <CheckCircle2 class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Xác nhận phân công"
                  : "Confirm Assignment"}</span
              >
            </button>
          </div>
        </div>
      </div>
    </div>
  {/if}

  <!-- SETTINGS TAB -->
  {#if activeTab === "settings"}
    <SettingsView />
  {/if}

  <!-- PROFILE TAB -->
  {#if activeTab === "profile"}
    <ProfileView />
  {/if}
</DashboardLayout>

<!-- Map Modal -->
{#if showMapModal}
  <div class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
    <div class="bg-white dark:bg-slate-900 w-full max-w-2xl rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-200">
      <div class="p-4 border-b border-slate-200 dark:border-slate-800 flex justify-between items-center bg-slate-50 dark:bg-slate-950">
        <h3 class="font-bold text-slate-800 dark:text-white flex items-center space-x-2">
          <Navigation class="h-4 w-4 text-emerald-500" />
          <span>{$language === 'vi' ? 'Cài đặt địa chỉ lắp đặt' : 'Set Installation Address'}</span>
        </h3>
        <button onclick={() => showMapModal = false} class="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-white transition cursor-pointer">
          <X class="h-5 w-5" />
        </button>
      </div>

      <div class="p-4 space-y-4">
        <div class="relative">
          <Search class="absolute left-3 top-3 h-4 w-4 text-slate-400" />
          <input
            type="text"
            bind:value={mapSearchQuery}
            placeholder={$language === 'vi' ? 'Tìm kiếm địa chỉ trên bản đồ...' : 'Search address on map...'}
            class="w-full pl-9 pr-4 py-2.5 text-sm bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl focus:outline-none focus:ring-2 focus:ring-emerald-500 text-slate-800 dark:text-white transition"
          />
        </div>

        <div class="relative w-full h-[300px] sm:h-[400px] bg-slate-200 dark:bg-slate-800 rounded-xl overflow-hidden border border-slate-200 dark:border-slate-700">
          <iframe
            title="Google Maps"
            width="100%"
            height="100%"
            style="border:0;"
            loading="lazy"
            src={`https://maps.google.com/maps?q=${encodeURIComponent(mapSearchQuery || 'Hanoi, Vietnam')}&t=&z=15&ie=UTF8&iwloc=&output=embed`}
          ></iframe>
          <!-- Center Pin overlay -->
          <div class="absolute inset-0 pointer-events-none flex items-center justify-center">
            <button 
              type="button"
              onclick={() => {
                if (mapSearchQuery.trim()) {
                  province = '';
                  district = '';
                  ward = '';
                  specificAddress = mapSearchQuery;
                  installationAddress = mapSearchQuery;
                  showMapModal = false;
                  toast.success($language === 'vi' ? 'Đã lấy vị trí thành công!' : 'Location retrieved successfully!');
                } else {
                  toast.error($language === 'vi' ? 'Vui lòng nhập địa chỉ vào ô tìm kiếm' : 'Please search for an address first');
                }
              }}
              class="mb-8 relative flex flex-col items-center pointer-events-auto cursor-pointer group"
            >
              <div class="bg-rose-500 text-white text-[10px] font-bold px-2 py-1 rounded shadow-md mb-1 group-hover:scale-110 transition-transform">
                {$language === 'vi' ? 'Chọn vị trí này' : 'Pick this location'}
              </div>
              <MapPin class="h-8 w-8 text-rose-500 drop-shadow-md group-hover:scale-110 transition-transform" />
            </button>
          </div>
        </div>
      </div>

      <div class="p-4 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 flex justify-end space-x-3">
        <button onclick={() => showMapModal = false} class="px-4 py-2 text-sm font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-xl transition cursor-pointer">
          {$language === 'vi' ? 'Huỷ' : 'Cancel'}
        </button>
        <button
          onclick={() => {
            province = '';
            district = '';
            ward = '';
            specificAddress = mapSearchQuery;
            installationAddress = mapSearchQuery;
            showMapModal = false;
          }}
          disabled={!mapSearchQuery.trim()}
          class="px-5 py-2 text-sm font-bold text-white bg-emerald-600 hover:bg-emerald-500 rounded-xl shadow-md transition disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
        >
          {$language === 'vi' ? 'Xác nhận địa chỉ' : 'Confirm Address'}
        </button>
      </div>
    </div>
  </div>
{/if}
