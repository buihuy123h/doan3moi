<script lang="ts">
  // Mirrors pages/AdminDashboard.tsx of the React original.
  import { nexusStore } from "../context/NexusContext";
  import { languageStore } from "../context/LanguageContext";
  import DashboardLayout from "../components/layout/DashboardLayout.svelte";
  import SettingsView from "../components/common/SettingsView.svelte";
  import ProfileView from "../components/common/ProfileView.svelte";
  import type { NavItem } from "../components/layout/DashboardLayout.svelte";
  import {
    Users,
    User,
    Mail,
    Store,
    Truck,
    Layers,
    Package,
    Plus,
    Search,
    Edit2,
    Trash2,
    TrendingUp,
    CheckCircle2,
    Phone,
    MapPin,
    Wifi,
    Radio,
    X,
    Settings,
    MessageSquare,
    KeyRound,
    Eye,
    EyeOff,
    AlertTriangle,
    RefreshCw,
    ArrowUpRight,
    ShieldCheck,
    Headset,
    ShoppingCart,
  } from "lucide-svelte";
  import type {
    Employee,
    Vendor,
    Plan,
    RetailShop,
    InventoryItem,
    PurchaseOrder,
  } from "../types/nexus";
  import { toast } from "svelte-sonner";
  import { queryParam, activeTabOverride } from "../lib/router";
  import {
    onRealtime,
    realtimeStatus,
    RealtimeEvents,
  } from "../lib/realtime";
  import {
    getPlanName,
    getPlanDescription,
    getPlanSpeedOrBandwidth,
    getPlanBillingCycle,
  } from "../lib/planI18n";
  import {
    fetchChatSessionsApi,
    fetchChatMessagesApi,
    replyChatSessionApi,
    markChatSessionReadApi,
    type ChatSessionDto,
    type ChatMessageDto,
  } from "../lib/api";
  import { authStore } from "../context/AuthContext";
  import { fly, fade } from "svelte/transition";

  type AdminTab =
    | "overview"
    | "employees"
    | "stock"
    | "vendors"
    | "purchases"
    | "shops"
    | "plans"
    | "livechat"
    | "feedback"
    | "settings"
    | "profile";

  const {
    employees,
    addEmployee,
    updateEmployee,
    deleteEmployee,
    vendors,
    addVendor,
    updateVendor,
    deleteVendor,
    retailShops,
    addRetailShop,
    updateRetailShop,
    deleteRetailShop,
    purchaseOrders,
    refreshPurchaseOrders,
    addPurchaseOrder,
    setPurchaseOrderStatus,
    deletePurchaseOrder,
    plans,
    addPlan,
    updatePlan,
    deletePlan,
    inventory,
    addInventoryItem,
    updateInventoryItem,
    deleteInventoryItem,
    feedbacks,
    respondFeedback,
  } = nexusStore;
  const { t, language } = languageStore;

  // Active navigation tab
  let activeTab = $state<AdminTab>("overview");

  // Reactively respond to tab overrides from router / notifications
  $effect(() => {
    const override = $activeTabOverride;
    const validTabs: AdminTab[] = [
      "overview",
      "employees",
      "stock",
      "vendors",
      "shops",
      "plans",
      "livechat",
      "feedback",
      "settings",
      "profile",
    ];
    if (override && override.path === "/admin") {
      if (validTabs.includes(override.tab as AdminTab)) {
        activeTab = override.tab as AdminTab;
      }
    } else {
      const qTab = queryParam("tab");
      if (qTab && validTabs.includes(qTab as AdminTab)) {
        activeTab = qTab as AdminTab;
      }
    }
  });

  // Employee Search and Filter
  let employeeSearch = $state("");
  let employeeRoleFilter = $state("All");

  // Employee Modal State
  let isEmployeeModalOpen = $state(false);
  let editingEmployee = $state<Employee | null>(null);
  let employeeFormData = $state({
    employeeCode: "",
    name: "",
    email: "",
    phone: "",
    role: "Retail Staff" as Employee["role"],
    department: "Retail Outlets" as Employee["department"],
    retailShopAssigned: "Downtown Flagship (SH-01)",
    status: "Active" as Employee["status"],
    dateOfJoining: new Date().toISOString().slice(0, 10),
    password: "",
    showPassword: false,
  });

  // Vendor Modal State
  let isVendorModalOpen = $state(false);
  let editingVendor = $state<Vendor | null>(null);
  let vendorFormData = $state({
    vendorCode: "",
    companyName: "",
    contactPerson: "",
    category: "Modems & Routers" as Vendor["category"],
    phone: "",
    email: "",
    address: "",
    rating: 5,
    status: "Active" as Vendor["status"],
    // Ô "Điều khoản thanh toán" (Net 30 / Net 45) có trên form nhưng bảng Vendor
    // trong CSDL chưa có cột tương ứng, nên giá trị này chỉ tồn tại trong phiên làm việc.
    // Muốn lưu thật thì phải thêm cột PaymentTerms vào dbo.Vendor và DTO của backend.
    paymentTerms: "",
  });

  // Vendor Search, Filter, Pagination
  let vendorSearch = $state("");
  let vendorCategoryFilter = $state("All");
  let vendorCurrentPage = $state(1);
  const vendorItemsPerPage = 8;
  
  const vendorCategories = $derived(["All", ...new Set($vendors.map(v => v.category))]);
  
  const filteredVendors = $derived(
    $vendors.filter(vnd => {
      const matchSearch = vnd.companyName.toLowerCase().includes(vendorSearch.toLowerCase()) || 
                          vnd.vendorCode.toLowerCase().includes(vendorSearch.toLowerCase());
      const matchCategory = vendorCategoryFilter === "All" || vnd.category === vendorCategoryFilter;
      return matchSearch && matchCategory;
    })
  );
  
  const totalVendorPages = $derived(Math.ceil(filteredVendors.length / vendorItemsPerPage) || 1);
  
  const paginatedVendors = $derived(
    filteredVendors.slice((vendorCurrentPage - 1) * vendorItemsPerPage, vendorCurrentPage * vendorItemsPerPage)
  );

  $effect(() => {
    vendorSearch;
    vendorCategoryFilter;
    vendorCurrentPage = 1;
  });

  // Plan Search, Filter, Pagination
  let planSearch = $state("");
  let planTypeFilter = $state("All");
  let planCurrentPage = $state(1);
  const planItemsPerPage = 8;
  
  const planTypes = $derived(["All", ...new Set($plans.map(p => p.type))]);
  
  const filteredPlans = $derived(
    $plans.filter(plan => {
      const matchSearch = plan.name.toLowerCase().includes(planSearch.toLowerCase());
      const matchType = planTypeFilter === "All" || plan.type === planTypeFilter;
      return matchSearch && matchType;
    })
  );
  
  const totalPlanPages = $derived(Math.ceil(filteredPlans.length / planItemsPerPage) || 1);
  
  const paginatedPlans = $derived(
    filteredPlans.slice((planCurrentPage - 1) * planItemsPerPage, planCurrentPage * planItemsPerPage)
  );

  $effect(() => {
    planSearch;
    planTypeFilter;
    planCurrentPage = 1;
  });

  // Plan Modal State
  let isPlanModalOpen = $state(false);
  let editingPlan = $state<Plan | null>(null);
  let planFormData = $state({
    name: "",
    type: "Broadband" as Plan["type"],
    speedOrBandwidth: "",
    monthlyRental: 0,
    hourlyCharge: 0,
    securityDeposit: 0,
    dataLimit: "",
    status: "Active" as Plan["status"],
    description: "",
    billingCycle: "Monthly" as NonNullable<Plan["billingCycle"]>,
    validity: "",
    callRates: "",
  });

  // Respond-to-feedback state
  let respondingId = $state<string | null>(null);
  let responseText = $state("");
  const submitResponse = (id: string) => {
    if (!responseText.trim()) return;
    respondFeedback(id, responseText.trim(), "Sarah Jenkins (Admin)");
    responseText = "";
    respondingId = null;
    toast.success("Response sent to the customer.");
  };

  // ---- Live Chat với khách (chatbox ở trang index) — dữ liệu thật từ API /api/chat ----
  const { currentUser } = authStore;
  let chatSessions = $state<ChatSessionDto[]>([]);
  let chatSearch = $state("");
  let activeChatSessionId = $state<string | null>(null);
  let chatMessages = $state<ChatMessageDto[]>([]);
  let chatDraft = $state("");
  let isChatSending = $state(false);
  let isChatLoadingMessages = $state(false);
  let chatThreadRef = $state<HTMLDivElement | null>(null);

  const totalChatUnread = $derived(
    chatSessions.reduce((sum, s) => sum + s.unreadCount, 0),
  );

  const filteredChatSessions = $derived(
    chatSearch.trim()
      ? chatSessions.filter(
          (s) =>
            s.customerName.toLowerCase().includes(chatSearch.toLowerCase()) ||
            s.lastMessage.toLowerCase().includes(chatSearch.toLowerCase()),
        )
      : chatSessions,
  );

  // Kênh real-time đang thông hay đã rớt — quyết định có cần hỏi lại server định kỳ không.
  const isChatLive = $derived($realtimeStatus === "connected");

  // Danh sách phiên chat: tải một lần rồi để server đẩy cập nhật xuống.
  // Đặt ngoài điều kiện tab để huy hiệu "chưa đọc" trên menu luôn đúng.
  $effect(() => {
    let cancelled = false;

    const refresh = async () => {
      try {
        const list = await fetchChatSessionsApi();
        if (!cancelled) chatSessions = list;
      } catch {
        // Mất kết nối tạm thời — lần làm mới sau sẽ thử lại
      }
    };

    refresh();

    // Server đẩy trạng thái mới của phiên (tin cuối, số tin chưa đọc) ngay khi có thay đổi.
    const off = onRealtime<ChatSessionDto>(RealtimeEvents.chatSession, (session) => {
      const isOpenSession = session.sessionId === activeChatSessionId;
      const merged = isOpenSession ? { ...session, unreadCount: 0 } : session;

      chatSessions = [
        merged,
        ...chatSessions.filter((s) => s.sessionId !== session.sessionId),
      ];

      // Phiên đang mở thì đánh dấu đã đọc luôn để huy hiệu không nhấp nháy.
      if (isOpenSession && session.unreadCount > 0) {
        void markChatSessionReadApi(session.sessionId).catch(() => undefined);
      }
    });

    // Dự phòng: mất kênh real-time thì quay lại hỏi server mỗi 5 giây.
    const timer = setInterval(refresh, isChatLive ? 60000 : 5000);

    return () => {
      cancelled = true;
      off();
      clearInterval(timer);
    };
  });

  // Tin nhắn của phiên đang mở — nhận thẳng từ hub, không chờ polling.
  $effect(() => {
    if (!activeChatSessionId) return;
    const sessionId = activeChatSessionId;

    const off = onRealtime<ChatMessageDto>(RealtimeEvents.chatMessage, (message) => {
      if (message.sessionId !== sessionId) return;
      // Tin do chính admin vừa gửi đã được thêm vào danh sách ngay lúc gửi.
      if (chatMessages.some((m) => m.id === message.id)) return;

      chatMessages = [...chatMessages, message];
      requestAnimationFrame(() =>
        chatThreadRef?.scrollTo({
          top: chatThreadRef.scrollHeight,
          behavior: "smooth",
        }),
      );
    });

    // Dự phòng khi kênh real-time đang rớt.
    const timer = setInterval(async () => {
      if (isChatLive) return;
      try {
        chatMessages = await fetchChatMessagesApi(sessionId);
      } catch {
        // Bỏ qua, lần sau thử lại
      }
    }, 5000);

    return () => {
      off();
      clearInterval(timer);
    };
  });

  const openChatSession = async (sessionId: string) => {
    activeChatSessionId = sessionId;
    isChatLoadingMessages = true;
    try {
      chatMessages = await fetchChatMessagesApi(sessionId);
      await markChatSessionReadApi(sessionId);
      // Xóa badge chưa đọc phía danh sách phiên ngay lập tức
      chatSessions = chatSessions.map((s) =>
        s.sessionId === sessionId ? { ...s, unreadCount: 0 } : s,
      );
      requestAnimationFrame(() =>
        chatThreadRef?.scrollTo({ top: chatThreadRef.scrollHeight }),
      );
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Không tải được tin nhắn.",
      );
    } finally {
      isChatLoadingMessages = false;
    }
  };

  const sendAdminReply = async () => {
    const text = chatDraft.trim();
    if (!text || !activeChatSessionId || isChatSending) return;
    try {
      isChatSending = true;
      const adminName = $currentUser?.name
        ? `${$currentUser.name} (Admin)`
        : "Nexus Admin";
      const sent = await replyChatSessionApi(
        activeChatSessionId,
        adminName,
        text,
      );
      chatMessages = [...chatMessages, sent];
      chatDraft = "";
      // Cập nhật luôn tin nhắn cuối trong danh sách phiên bên trái
      chatSessions = chatSessions.map((s) =>
        s.sessionId === activeChatSessionId
          ? { ...s, lastMessage: text, lastSenderType: "admin" as const }
          : s,
      );
      requestAnimationFrame(() =>
        chatThreadRef?.scrollTo({
          top: chatThreadRef.scrollHeight,
          behavior: "smooth",
        }),
      );
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Không gửi được trả lời.",
      );
    } finally {
      isChatSending = false;
    }
  };

  const handleChatReplyKeyDown = (event: KeyboardEvent) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      sendAdminReply();
    }
  };

  // Tự động mở phiên chat khi điều hướng từ thông báo trên header
  // (URL dạng /admin?tab=livechat&session=chat-xx)
  $effect(() => {
    void $activeTabOverride;
    if (activeTab !== "livechat") return;
    const qSession = queryParam("session");
    if (qSession && qSession !== activeChatSessionId) {
      void openChatSession(qSession);
    }
  });

  // Retail Shop Modal State & Handlers
  let isShopModalOpen = $state(false);
  let editingShop = $state<RetailShop | null>(null);
  let shopFormData = $state({
    shopCode: "",
    name: "",
    city: "New York",
    cityCode: "064",
    address: "",
    managerName: "",
    phone: "",
    operatingHours: "08:00 - 20:00",
    activeEmployeesCount: 4,
    totalSubscribersServed: 120,
  });

  const handleOpenShopModal = (shp?: RetailShop) => {
    if (shp) {
      editingShop = shp;
      shopFormData = {
        shopCode: shp.shopCode,
        name: shp.name,
        city: shp.city,
        cityCode: shp.cityCode,
        address: shp.address,
        managerName: shp.managerName,
        phone: shp.phone,
        operatingHours: shp.operatingHours,
        activeEmployeesCount: shp.activeEmployeesCount,
        totalSubscribersServed: shp.totalSubscribersServed,
      };
    } else {
      editingShop = null;
      shopFormData = {
        shopCode: "",
        name: "",
        city: "",
        cityCode: "",
        address: "",
        managerName: "",
        phone: "",
        operatingHours: "08:00 - 20:00",
        activeEmployeesCount: 0,
        totalSubscribersServed: 0,
      };
    }
    isShopModalOpen = true;
  };

  const handleSaveShop = (e: SubmitEvent) => {
    e.preventDefault();
    if (
      !shopFormData.name ||
      !shopFormData.address ||
      !shopFormData.managerName
    ) {
      toast.error(
        $language === "vi"
          ? "Vui lòng điền đầy đủ các thông tin bắt buộc."
          : "Please fill in all required shop details.",
      );
      return;
    }
    if (editingShop) {
      updateRetailShop(editingShop.id, shopFormData);
      toast.success(
        $language === "vi"
          ? `Đã cập nhật chi nhánh ${shopFormData.name}`
          : `Updated shop ${shopFormData.name}`,
      );
    } else {
      addRetailShop(shopFormData);
      toast.success(
        $language === "vi"
          ? `Đã thêm chi nhánh ${shopFormData.name}`
          : `Added new shop ${shopFormData.name}`,
      );
    }
    isShopModalOpen = false;
  };

  const handleDeleteShop = (shp: RetailShop) => {
    showConfirm(
      $language === "vi" ? "Xác nhận xóa" : "Confirm Deletion",
      $language === "vi"
        ? `Xóa chi nhánh "${shp.name}" (${shp.shopCode})?`
        : `Remove shop "${shp.name}"?`,
      () => {
        deleteRetailShop(shp.id);
        toast.success(
          $language === "vi"
            ? `Đã xóa chi nhánh ${shp.name}.`
            : `Shop ${shp.name} removed.`,
        );
      },
    );
  };

  // Stock / Inventory Item Modal State & Handlers
  let isStockModalOpen = $state(false);
  let stockSearch = $state("");
  let stockCategoryFilter = $state("All");
  let stockCurrentPage = $state(1);
  const stockItemsPerPage = 8;
  
  const stockCategories = $derived(["All", ...new Set($inventory.map(i => i.category))]);
  
  const filteredStock = $derived(
    $inventory.filter(item => {
      const matchSearch = item.name.toLowerCase().includes(stockSearch.toLowerCase()) || 
                          item.itemCode.toLowerCase().includes(stockSearch.toLowerCase());
      const matchCategory = stockCategoryFilter === "All" || item.category === stockCategoryFilter;
      return matchSearch && matchCategory;
    })
  );
  
  const totalStockPages = $derived(Math.ceil(filteredStock.length / stockItemsPerPage) || 1);
  
  const paginatedStock = $derived(
    filteredStock.slice((stockCurrentPage - 1) * stockItemsPerPage, stockCurrentPage * stockItemsPerPage)
  );

  $effect(() => {
    stockSearch;
    stockCategoryFilter;
    stockCurrentPage = 1;
  });
  let editingStock = $state<InventoryItem | null>(null);
  let stockFormData = $state({
    itemCode: "",
    name: "",
    category: "Modem" as InventoryItem["category"],
    stockQuantity: 0,
    reorderLevel: 0,
    unitCost: 0,
    location: "",
    supplier: "",
    restockQuantity: 0,
  });

  const handleOpenStockModal = (item?: InventoryItem) => {
    if (item) {
      editingStock = item;
      const needed = Math.max(0, item.reorderLevel - item.stockQuantity);
      stockFormData = {
        itemCode: item.itemCode,
        name: item.name,
        category: item.category,
        stockQuantity: item.stockQuantity,
        reorderLevel: item.reorderLevel,
        unitCost: item.unitCost,
        location: item.location,
        supplier: item.supplier,
        restockQuantity: needed > 0 ? needed : 10,
      };
    } else {
      editingStock = null;
      stockFormData = {
        itemCode: "",
        name: "",
        category: "Modem",
        stockQuantity: 0,
        reorderLevel: 0,
        unitCost: 0,
        location: "",
        supplier: "",
        restockQuantity: 0,
      };
    }
    isStockModalOpen = true;
  };

  let isSavingStock = $state(false);
  const handleSaveStock = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!stockFormData.name || !stockFormData.itemCode) {
      toast.error(
        $language === "vi"
          ? "Vui lòng điền tên và mã thiết bị."
          : "Please enter item name and code.",
      );
      return;
    }
    isSavingStock = true;
    try {
      if (editingStock) {
        await updateInventoryItem(editingStock.id, stockFormData);
        toast.success(
          $language === "vi"
            ? `Đã cập nhật vật tư ${stockFormData.name} (Tồn kho: ${stockFormData.stockQuantity}) vào CSDL`
            : `Updated item ${stockFormData.name}`,
        );
      } else {
        await addInventoryItem(stockFormData);
        toast.success(
          $language === "vi"
            ? `Đã thêm vật tư ${stockFormData.name} vào CSDL`
            : `Added item ${stockFormData.name}`,
        );
      }
      isStockModalOpen = false;
    } catch (err: any) {
      toast.error(
        err.message ||
          ($language === "vi"
            ? "Lỗi khi lưu vật tư vào CSDL."
            : "Error saving item."),
      );
    } finally {
      isSavingStock = false;
    }
  };

  const handleDeleteStock = (item: InventoryItem) => {
    showConfirm(
      $language === "vi" ? "Xác nhận xóa" : "Confirm Deletion",
      $language === "vi"
        ? `Xóa vật tư "${item.name}" (${item.itemCode})?`
        : `Remove item "${item.name}"?`,
      () => {
        deleteInventoryItem(item.id);
        toast.success(
          $language === "vi"
            ? `Đã xóa vật tư ${item.name}.`
            : `Item ${item.name} removed.`,
        );
      },
    );
  };

  // Employee Form Submission Handlers
  const handleOpenEmployeeModal = (emp?: Employee) => {
    if (emp) {
      editingEmployee = emp;
      employeeFormData = {
        employeeCode: emp.employeeCode,
        name: emp.name,
        email: emp.email,
        phone: emp.phone,
        role: emp.role,
        department: emp.department,
        retailShopAssigned:
          emp.retailShopAssigned || "Downtown Flagship (SH-01)",
        status: emp.status,
        dateOfJoining: emp.dateOfJoining,
        password: "",
        showPassword: false,
      };
    } else {
      editingEmployee = null;
      employeeFormData = {
        employeeCode: "",
        name: "",
        email: "",
        phone: "",
        role: "Retail Staff",
        department: "Retail Outlets",
        retailShopAssigned: "Headquarters (General)",
        status: "Active",
        dateOfJoining: new Date().toISOString().slice(0, 10),
        password: "",
        showPassword: false,
      };
    }
    isEmployeeModalOpen = true;
  };

  let isSavingEmployee = $state(false);
  const handleSaveEmployee = async (e: SubmitEvent) => {
    e.preventDefault();
    if (
      !employeeFormData.name ||
      !employeeFormData.email ||
      !employeeFormData.phone
    ) {
      toast.error(
        $language === "vi"
          ? "Vui lòng điền đầy đủ các thông tin bắt buộc."
          : "Please complete all required fields.",
      );
      return;
    }

    isSavingEmployee = true;
    try {
      if (editingEmployee) {
        await updateEmployee(
          editingEmployee.id,
          employeeFormData,
          employeeFormData.password || undefined,
        );
        toast.success(
          $language === "vi"
            ? `Đã cập nhật tài khoản và thông tin nhân viên ${employeeFormData.name}`
            : `Updated employee ${employeeFormData.name}`,
        );
      } else {
        await addEmployee(
          employeeFormData,
          employeeFormData.password || undefined,
        );
        toast.success(
          $language === "vi"
            ? `Đã thêm mới nhân viên ${employeeFormData.name} thành công`
            : `Added new employee ${employeeFormData.name}`,
        );
      }
      isEmployeeModalOpen = false;
    } catch (err: any) {
      toast.error(
        err.message ||
          ($language === "vi"
            ? "Lỗi khi lưu thông tin nhân viên."
            : "Error saving employee."),
      );
    } finally {
      isSavingEmployee = false;
    }
  };

  const handleDeleteEmployee = (emp: Employee) => {
    showConfirm(
      $language === "vi" ? "Xác nhận xóa" : "Confirm Deletion",
      `Are you sure you want to remove employee "${emp.name}" (${emp.employeeCode})?`,
      () => {
        deleteEmployee(emp.id);
        toast.success(
          $language === "vi"
            ? `Đã xóa nhân viên ${emp.name}.`
            : `Employee ${emp.name} removed.`
        );
      },
    );
  };

  // Vendor Form Submission Handlers
  const handleOpenVendorModal = (vnd?: Vendor) => {
    if (vnd) {
      editingVendor = vnd;
      vendorFormData = {
        vendorCode: vnd.vendorCode,
        companyName: vnd.companyName,
        contactPerson: vnd.contactPerson,
        category: vnd.category,
        phone: vnd.phone,
        email: vnd.email,
        address: vnd.address,
        rating: vnd.rating,
        status: vnd.status,
        paymentTerms: "",
      };
    } else {
      editingVendor = null;
      vendorFormData = {
        vendorCode: "",
        companyName: "",
        contactPerson: "",
        category: "Modems & Routers",
        phone: "",
        email: "",
        address: "",
        rating: 5,
        status: "Active",
        paymentTerms: "",
      };
    }
    isVendorModalOpen = true;
  };

  const handleSaveVendor = (e: SubmitEvent) => {
    e.preventDefault();
    if (!vendorFormData.companyName || !vendorFormData.contactPerson) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập tên công ty và người liên hệ chính."
          : "Please enter company name and primary contact."
      );
      return;
    }

    if (editingVendor) {
      updateVendor(editingVendor.id, vendorFormData);
      toast.success(
        $language === "vi"
          ? `Đã cập nhật nhà cung cấp ${vendorFormData.companyName}`
          : `Updated vendor ${vendorFormData.companyName}`
      );
    } else {
      addVendor(vendorFormData);
      toast.success(
        $language === "vi"
          ? `Đã thêm nhà cung cấp ${vendorFormData.companyName}`
          : `Registered vendor ${vendorFormData.companyName}`
      );
    }
    isVendorModalOpen = false;
  };

  const handleDeleteVendor = (vnd: Vendor) => {
    showConfirm(
      $language === "vi" ? "Xác nhận xóa" : "Confirm Deletion",
      `Confirm termination of supplier "${vnd.companyName}"?`,
      () => {
        deleteVendor(vnd.id);
        toast.success(
          $language === "vi"
            ? `Đã xóa nhà cung cấp ${vnd.companyName}.`
            : `Vendor ${vnd.companyName} removed.`
        );
      },
    );
  };

  // ===== Purchase Orders (Purchase List — đặt mua thiết bị từ vendor) =====
  let purchaseSearch = $state("");
  let purchaseStatusFilter = $state<"all" | PurchaseOrder["status"]>("all");
  let showPurchaseModal = $state(false);
  let isSavingPurchase = $state(false);

  const filteredPurchaseOrders = $derived(
    $purchaseOrders.filter((p) => {
      if (purchaseStatusFilter !== "all" && p.status !== purchaseStatusFilter) return false;
      if (!purchaseSearch.trim()) return true;
      const q = purchaseSearch.toLowerCase();
      return (
        p.id.toLowerCase().includes(q) ||
        p.itemCode.toLowerCase().includes(q) ||
        p.itemName.toLowerCase().includes(q) ||
        (p.vendorName || "").toLowerCase().includes(q)
      );
    })
  );

  const purchaseFormData = $state({
    vendorId: "",
    storeId: "",
    itemCode: "",
    itemName: "",
    category: "Modem",
    quantity: 10,
    unitCost: 0,
    expectedDate: "",
    notes: "",
  });

  const purchaseTotalPreview = $derived(
    (Number(purchaseFormData.quantity) || 0) * (Number(purchaseFormData.unitCost) || 0)
  );

  function handleOpenPurchaseModal() {
    purchaseFormData.vendorId = $vendors[0]?.id ?? "";
    purchaseFormData.storeId = "";
    purchaseFormData.itemCode = "";
    purchaseFormData.itemName = "";
    purchaseFormData.category = "Modem";
    purchaseFormData.quantity = 10;
    purchaseFormData.unitCost = 0;
    purchaseFormData.expectedDate = "";
    purchaseFormData.notes = "";
    showPurchaseModal = true;
  }

  async function handleSavePurchaseOrder() {
    if (!purchaseFormData.vendorId || !purchaseFormData.itemCode.trim() || !purchaseFormData.itemName.trim()) {
      toast.error(
        $language === "vi"
          ? "Vui lòng chọn nhà cung cấp và nhập mã/tên vật tư."
          : "Please pick a vendor and enter item code/name."
      );
      return;
    }
    isSavingPurchase = true;
    try {
      await addPurchaseOrder({
        vendorId: purchaseFormData.vendorId,
        storeId: purchaseFormData.storeId || undefined,
        itemCode: purchaseFormData.itemCode.trim(),
        itemName: purchaseFormData.itemName.trim(),
        category: purchaseFormData.category,
        quantity: Number(purchaseFormData.quantity) || 1,
        unitCost: Number(purchaseFormData.unitCost) || 0,
        expectedDate: purchaseFormData.expectedDate || undefined,
        notes: purchaseFormData.notes.trim() || undefined,
      });
      showPurchaseModal = false;
      toast.success(
        $language === "vi" ? "Đã tạo đơn đặt mua thiết bị!" : "Purchase order created!"
      );
    } catch (err) {
      toast.error(
        $language === "vi"
          ? `Không tạo được đơn mua: ${err instanceof Error ? err.message : "lỗi không xác định"}`
          : `Failed to create purchase order: ${err instanceof Error ? err.message : "unknown error"}`
      );
    } finally {
      isSavingPurchase = false;
    }
  }

  async function handleReceivePurchase(po: PurchaseOrder) {
    try {
      await setPurchaseOrderStatus(po.id, "Received");
      toast.success(
        $language === "vi"
          ? `Đã nhận hàng ${po.quantity} × ${po.itemName} — tồn kho được cộng tự động.`
          : `Received ${po.quantity} × ${po.itemName} — inventory restocked automatically.`
      );
    } catch (err) {
      toast.error(
        $language === "vi"
          ? `Không nhận được hàng: ${err instanceof Error ? err.message : "lỗi"}`
          : `Failed to receive: ${err instanceof Error ? err.message : "error"}`
      );
    }
  }

  async function handleCancelPurchase(po: PurchaseOrder) {
    try {
      await setPurchaseOrderStatus(po.id, "Cancelled");
      toast.info($language === "vi" ? `Đã hủy đơn mua ${po.id}.` : `Purchase order ${po.id} cancelled.`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "error");
    }
  }

  function handleDeletePurchase(po: PurchaseOrder) {
    showConfirm(
      $language === "vi" ? "Xác nhận xóa" : "Confirm Deletion",
      `Delete purchase order "${po.id}" for ${po.itemName}?`,
      () => {
        deletePurchaseOrder(po.id).catch((err) =>
          toast.error(err instanceof Error ? err.message : "error"),
        );
        toast.success($language === "vi" ? `Đã xóa đơn mua ${po.id}.` : `Purchase order ${po.id} removed.`);
      },
    );
  }

  // Plan Form Submission Handlers
  const handleOpenPlanModal = (plan?: Plan) => {
    if (plan) {
      editingPlan = plan;
      planFormData = {
        name: plan.name,
        type: plan.type,
        speedOrBandwidth: plan.speedOrBandwidth,
        monthlyRental: plan.monthlyRental,
        hourlyCharge: plan.hourlyCharge || 0,
        securityDeposit: plan.securityDeposit,
        dataLimit: plan.dataLimit || "Unlimited",
        status: plan.status,
        description: plan.description,
        billingCycle: plan.billingCycle || "Monthly",
        validity: plan.validity || "1 Month",
        callRates: plan.callRates || "",
      };
    } else {
      editingPlan = null;
      planFormData = {
        name: "",
        type: "Broadband",
        speedOrBandwidth: "",
        monthlyRental: 0,
        hourlyCharge: 0,
        securityDeposit: 0,
        dataLimit: "Unlimited",
        status: "Active",
        description: "",
        billingCycle: "Monthly",
        validity: "1 Month",
        callRates: "",
      };
    }
    isPlanModalOpen = true;
  };

  const handleSavePlan = (e: SubmitEvent) => {
    e.preventDefault();
    if (!planFormData.name || !planFormData.speedOrBandwidth) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập tên gói cước và thông số tốc độ."
          : "Please enter plan name and speed specifications."
      );
      return;
    }

    if (editingPlan) {
      updatePlan(editingPlan.id, planFormData);
      toast.success(
        $language === "vi"
          ? `Đã cập nhật gói cước ${planFormData.name}`
          : `Updated plan ${planFormData.name}`
      );
    } else {
      addPlan(planFormData);
      toast.success(
        $language === "vi"
          ? `Đã tạo gói cước ${planFormData.name}`
          : `Created plan ${planFormData.name}`
      );
    }
    isPlanModalOpen = false;
  };

  // Calculations for Summary Area
  const totalRetailShops = $derived($retailShops.length);
  const activeEmployees = $derived(
    $employees.filter((e) => e.id === $currentUser?.id).length
  );
  const totalSubscribers = $derived(
    $retailShops.reduce((sum, s) => sum + s.totalSubscribersServed, 0),
  );
  const lowStockItems = $derived(
    $inventory.filter((item) => item.stockQuantity <= item.reorderLevel),
  );

  // Filtering Employees
  const filteredEmployees = $derived(
    $employees.filter((emp) => {
      const matchesSearch =
        emp.name.toLowerCase().includes(employeeSearch.toLowerCase()) ||
        emp.employeeCode.toLowerCase().includes(employeeSearch.toLowerCase()) ||
        emp.email.toLowerCase().includes(employeeSearch.toLowerCase());
      const matchesRole =
        employeeRoleFilter === "All" || emp.role === employeeRoleFilter;
      return matchesSearch && matchesRole;
    }),
  );

  const adminNavItems: NavItem[] = $derived([
    { id: "overview", label: $t.adminNav.overview, icon: TrendingUp },
    {
      id: "employees",
      label: $t.adminNav.employees,
      icon: Users,
      badge: $employees.length,
    },
    {
      id: "stock",
      label: $t.adminNav.stock,
      icon: Package,
      badge:
        lowStockItems.length > 0
          ? $language === "vi"
            ? `${lowStockItems.length} sắp hết`
            : `${lowStockItems.length} low`
          : undefined,
      badgeColor: "bg-amber-100 text-amber-800",
    },
    {
      id: "vendors",
      label: $t.adminNav.vendors,
      icon: Truck,
      badge: $vendors.length,
    },
    {
      id: "purchases",
      label: $language === "vi" ? "Đơn đặt mua" : "Purchase Orders",
      icon: ShoppingCart,
      badge: $purchaseOrders.filter((p) => p.status === "Submitted").length || undefined,
    },
    {
      id: "shops",
      label: $t.adminNav.shops,
      icon: Store,
      badge: $retailShops.length,
    },
    {
      id: "plans",
      label: $t.adminNav.plans,
      icon: Layers,
      badge: $plans.length,
    },
    {
      id: "livechat",
      label: $language === "vi" ? "Chat trực tuyến" : "Live Chat",
      icon: Headset,
      badge: totalChatUnread > 0 ? totalChatUnread : undefined,
      badgeColor: "bg-rose-100 text-rose-800",
    },
    {
      id: "feedback",
      label: $language === "vi" ? "Phản hồi KH" : "Customer Feedback",
      icon: MessageSquare,
      badge: $feedbacks.filter((f) => !f.response).length || undefined,
      badgeColor: "bg-amber-100 text-amber-800",
    },
    { id: "settings", label: $t.adminNav.settings, icon: Settings },
  ]);

  // Global Confirm Modal State
  let confirmDialog = $state({
    isOpen: false,
    title: "",
    message: "",
    onConfirm: () => {},
  });

  function showConfirm(title: string, message: string, onConfirm: () => void) {
    confirmDialog = {
      isOpen: true,
      title,
      message,
      onConfirm: () => {
        onConfirm();
        confirmDialog.isOpen = false;
      },
    };
  }

  const handleWindowKeydown = (e: KeyboardEvent) => {
    if (e.key === "Escape") {
      if (isPlanModalOpen) isPlanModalOpen = false;
      if (isVendorModalOpen) isVendorModalOpen = false;
      if (isEmployeeModalOpen) isEmployeeModalOpen = false;
      if (confirmDialog.isOpen) confirmDialog.isOpen = false;
    }
  };
</script>

<svelte:window onkeydown={handleWindowKeydown} />

<DashboardLayout
  {activeTab}
  onTabChange={(tab) => {
    activeTab = tab as AdminTab;
    isEmployeeModalOpen = false;
    isVendorModalOpen = false;
    isPlanModalOpen = false;
    isShopModalOpen = false;
    isStockModalOpen = false;
  }}
  navItems={adminNavItems}
  roleBadgeTitle={$t.roles.admin}
  pageTitle={activeTab === "overview"
    ? $t.adminNav.overview
    : adminNavItems.find((n) => n.id === activeTab)?.label}
  primaryAction={activeTab === "employees"
    ? {
        label: $t.actions.addEmployee,
        onClick: () => handleOpenEmployeeModal(),
      }
    : activeTab === "vendors"
      ? { label: $t.actions.addVendor, onClick: () => handleOpenVendorModal() }
      : activeTab === "plans"
        ? { label: $t.actions.addPlan, onClick: () => handleOpenPlanModal() }
        : activeTab === "shops"
          ? {
              label:
                $language === "vi" ? "Thêm điểm bán lẻ" : "New Retail Shop",
              onClick: () => handleOpenShopModal(),
            }
          : activeTab === "stock"
            ? {
                label:
                  $language === "vi"
                    ? "Thêm vật tư thiết bị"
                    : "New Equipment Item",
                onClick: () => handleOpenStockModal(),
              }
            : {
                label: $t.actions.newReport,
                onClick: () =>
                  toast.success(
                    $language === "vi"
                      ? "Đang tạo báo cáo tổng hợp mới..."
                      : "Creating summary report...",
                  ),
              }}
>
  <!-- TAB 1: OVERVIEW & SUMMARY -->
  {#if activeTab === "overview"}
    <div class="tab-content-animate space-y-6">
      <!-- Primary Required KPI Summary Cards -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <!-- Total Retail Shops Card -->
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm"
        >
          <div class="flex items-center justify-between">
            <span
              class="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider"
            >
              {$language === "vi"
                ? "Điểm giao dịch bán lẻ"
                : "Total Retail Shops"}
            </span>
            <div
              class="flex h-9 w-9 items-center justify-center rounded-lg bg-blue-100 dark:bg-blue-900/40 text-blue-600 dark:text-blue-400"
            >
              <Store class="h-5 w-5" />
            </div>
          </div>
          <div class="mt-3 flex items-baseline space-x-2">
            <span
              class="text-3xl font-bold text-slate-900 dark:text-white tabular-nums"
              >{totalRetailShops}</span
            >
            <span
              class="text-xs text-emerald-600 dark:text-emerald-400 font-medium"
            >
              {$language === "vi" ? "100% Hoạt động" : "100% Operational"}
            </span>
          </div>
          <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">
            {$language === "vi"
              ? "Phủ khắp các quận đô thị"
              : "Across New York metro regions"}
          </p>
        </div>

        <!-- Active Employees Card -->
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm"
        >
          <div class="flex items-center justify-between">
            <span
              class="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider"
            >
              {$language === "vi"
                ? "Nhân sự đang làm việc"
                : "Active Employees"}
            </span>
            <div
              class="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400"
            >
              <Users class="h-5 w-5" />
            </div>
          </div>
          <div class="mt-3 flex items-baseline space-x-2">
            <span
              class="text-3xl font-bold text-slate-900 dark:text-white tabular-nums"
              >{activeEmployees}</span
            >
            <span class="text-xs text-slate-500 dark:text-slate-400">
              / {$employees.length}
              {$language === "vi" ? "Tổng nhân sự" : "Total Staff"}
            </span>
          </div>
          <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">
            {$language === "vi"
              ? "Khối Bán lẻ, Kỹ thuật & Kế toán"
              : "Retail, Technical & Operations teams"}
          </p>
        </div>

        <!-- Subscribers Served Card -->
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm"
        >
          <div class="flex items-center justify-between">
            <span
              class="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider"
            >
              {$language === "vi"
                ? "Tổng thuê bao phục vụ"
                : "Subscribers Served"}
            </span>
            <div
              class="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-100 dark:bg-emerald-900/40 text-emerald-600 dark:text-emerald-400"
            >
              <CheckCircle2 class="h-5 w-5" />
            </div>
          </div>
          <div class="mt-3 flex items-baseline space-x-2">
            <span
              class="text-3xl font-bold text-slate-900 dark:text-white tabular-nums"
              >{totalSubscribers.toLocaleString()}</span
            >
            <span
              class="text-xs text-emerald-600 dark:text-emerald-400 font-medium"
              >+14% YoY</span
            >
          </div>
          <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">
            {$language === "vi"
              ? "Băng rộng, Quay số & Điện thoại cố định"
              : "Broadband, Dial-Up & Landlines"}
          </p>
        </div>

        <!-- Registered Vendors Card -->
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm"
        >
          <div class="flex items-center justify-between">
            <span
              class="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider"
            >
              {$language === "vi"
                ? "Nhà cung ứng thiết bị"
                : "Registered Vendors"}
            </span>
            <div
              class="flex h-9 w-9 items-center justify-center rounded-lg bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400"
            >
              <Truck class="h-5 w-5" />
            </div>
          </div>
          <div class="mt-3 flex items-baseline space-x-2">
            <span
              class="text-3xl font-bold text-slate-900 dark:text-white tabular-nums"
              >{$vendors.length}</span
            >
            <span class="text-xs text-slate-500 dark:text-slate-400">
              {$language === "vi" ? "Đối tác chính thức" : "Approved Suppliers"}
            </span>
          </div>
          <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">
            Corning, Cisco, Zyxel, Amphenol
          </p>
        </div>
      </div>

      <!-- Quick Actions & Department Snapshot -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Retail Outlet Network Highlights -->
        <div
          class="lg:col-span-2 rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm"
        >
          <div class="flex items-center justify-between mb-4">
            <h3 class="font-semibold text-base text-slate-900 dark:text-white">
              {$language === "vi"
                ? "Mạng lưới điểm giao dịch"
                : "Retail Shop Footprint"}
            </h3>
            <button
              onclick={() => (activeTab = "shops")}
              class="text-xs text-indigo-600 dark:text-indigo-400 hover:underline font-medium"
            >
              {$language === "vi"
                ? "Xem tất cả chi nhánh →"
                : "View All Outlets →"}
            </button>
          </div>

          <div class="divide-y divide-slate-100 dark:divide-slate-800">
            {#each $retailShops as shop (shop.id)}
              <div class="py-3 flex items-center justify-between">
                <div class="flex items-center space-x-3">
                  <div
                    class="h-9 w-9 rounded-lg bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-600 dark:text-slate-300 font-bold text-xs"
                  >
                    {shop.shopCode}
                  </div>
                  <div>
                    <div
                      class="font-medium text-sm text-slate-900 dark:text-white"
                    >
                      {shop.name}
                    </div>
                    <div class="text-xs text-slate-500 dark:text-slate-400">
                      {$language === "vi" ? "Quản lý:" : "Manager:"}
                      {shop.managerName} • {shop.city}
                    </div>
                  </div>
                </div>
                <div class="text-right">
                  <div
                    class="text-sm font-semibold text-slate-900 dark:text-white"
                  >
                    {shop.totalSubscribersServed.toLocaleString()}
                    {$language === "vi" ? "thuê bao" : "subs"}
                  </div>
                  <div class="text-xs text-emerald-600 dark:text-emerald-400">
                    {shop.activeEmployeesCount}
                    {$language === "vi" ? "nhân sự trực" : "on-duty staff"}
                  </div>
                </div>
              </div>
            {/each}
          </div>
        </div>

        <!-- Administrative Quick Actions -->
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm space-y-4"
        >
          <h3 class="font-semibold text-base text-slate-900 dark:text-white">
            {$language === "vi"
              ? "Thao tác quản trị nhanh"
              : "Quick Administration"}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {$language === "vi"
              ? "Phím tắt thao tác nhanh để quản lý nhân sự, đối tác và danh mục gói cước."
              : "Fast management shortcuts to maintain staff and catalog records."}
          </p>

          <div class="space-y-2">
            <button
              onclick={() => handleOpenEmployeeModal()}
              class="w-full text-left p-3 rounded-lg border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/60 transition flex items-center justify-between text-sm"
            >
              <span class="font-medium"
                >{$language === "vi"
                  ? "Thêm nhân viên mới"
                  : "Onboard New Employee"}</span
              >
              <Plus class="h-4 w-4 text-indigo-500" />
            </button>
            <button
              onclick={() => handleOpenVendorModal()}
              class="w-full text-left p-3 rounded-lg border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/60 transition flex items-center justify-between text-sm"
            >
              <span class="font-medium"
                >{$language === "vi"
                  ? "Đăng ký nhà cung cấp"
                  : "Register New Supplier"}</span
              >
              <Plus class="h-4 w-4 text-indigo-500" />
            </button>
            <button
              onclick={() => handleOpenPlanModal()}
              class="w-full text-left p-3 rounded-lg border border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/60 transition flex items-center justify-between text-sm"
            >
              <span class="font-medium"
                >{$language === "vi"
                  ? "Cấu hình gói cước"
                  : "Configure Service Plan"}</span
              >
              <Plus class="h-4 w-4 text-indigo-500" />
            </button>
          </div>
        </div>
      </div>
    </div>
  {/if}

  <!-- TAB 2: EMPLOYEE MANAGEMENT -->
  {#if activeTab === "employees"}
    <div class="space-y-4">
      <!-- Search and Filters toolbar -->
      <div
        class="flex flex-col sm:flex-row gap-3 items-center justify-between bg-white dark:bg-slate-900 p-4 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm"
      >
        <div class="relative w-full sm:w-80">
          <Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
          <input
            type="text"
            bind:value={employeeSearch}
            placeholder={$language === "vi" ? "Tìm nhân viên..." : "Search employees..."}
            class="w-full pl-9 pr-4 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>

        <div class="flex items-center space-x-2 w-full sm:w-auto">
          <span class="text-xs text-slate-500 whitespace-nowrap">
            {$language === "vi" ? "Lọc vai trò:" : "Filter Role:"}
          </span>
          <select
            bind:value={employeeRoleFilter}
            class="text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option value="All"
              >{$language === "vi" ? "Tất cả vai trò" : "All Roles"}</option
            >
            <option value="Manager"
              >{$language === "vi" ? "Quản lý" : "Manager"}</option
            >
            <option value="Retail Staff"
              >{$language === "vi"
                ? "Nhân viên bán lẻ"
                : "Retail Staff"}</option
            >
            <option value="Field Engineer"
              >{$language === "vi"
                ? "Kỹ sư hiện trường"
                : "Field Engineer"}</option
            >
            <option value="Senior Accountant"
              >{$language === "vi"
                ? "Kế toán trưởng"
                : "Senior Accountant"}</option
            >
          </select>
        </div>
      </div>

      <!-- Employee Box Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {#each filteredEmployees as emp (emp.id)}
          <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 shadow-sm hover:shadow-md transition relative flex flex-col group">
            <!-- Header: Name and Status -->
            <div class="flex items-start justify-between mb-3">
              <div class="min-w-0 pr-2">
                <div class="font-bold text-slate-900 dark:text-white truncate" title={emp.name}>
                  {emp.name}
                </div>
                <div class="text-[10px] font-mono text-slate-400">
                  {emp.employeeCode}
                </div>
              </div>
              <span
                class="shrink-0 inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-medium {emp.id === $currentUser?.id
                  ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-500'}"
              >
                {$language === "vi"
                  ? (emp.id === $currentUser?.id ? "Hoạt động" : "Ngoại tuyến")
                  : (emp.id === $currentUser?.id ? "Active" : "Offline")}
              </span>
            </div>

            <!-- Body: Details -->
            <div class="flex-1 space-y-2 text-xs">
              <div class="flex items-center gap-2">
                <div class="h-6 w-6 rounded bg-slate-50 dark:bg-slate-800/50 flex items-center justify-center shrink-0">
                  <User class="h-3 w-3 text-slate-500" />
                </div>
                <div class="truncate">
                  <span class="font-medium text-slate-700 dark:text-slate-300">{emp.role}</span>
                  <span class="text-slate-400 mx-1">•</span>
                  <span class="text-slate-500">{emp.department}</span>
                </div>
              </div>

              <div class="flex items-center gap-2">
                <div class="h-6 w-6 rounded bg-slate-50 dark:bg-slate-800/50 flex items-center justify-center shrink-0">
                  <Mail class="h-3 w-3 text-slate-500" />
                </div>
                <div class="truncate text-slate-600 dark:text-slate-400" title={emp.email}>
                  {emp.email}
                </div>
              </div>

              <div class="flex items-center gap-2">
                <div class="h-6 w-6 rounded bg-slate-50 dark:bg-slate-800/50 flex items-center justify-center shrink-0">
                  <Phone class="h-3 w-3 text-slate-500" />
                </div>
                <div class="truncate text-slate-600 dark:text-slate-400">
                  {emp.phone}
                </div>
              </div>
              
              <div class="flex items-center gap-2">
                <div class="h-6 w-6 rounded bg-slate-50 dark:bg-slate-800/50 flex items-center justify-center shrink-0">
                  <MapPin class="h-3 w-3 text-slate-500" />
                </div>
                <div class="truncate text-slate-600 dark:text-slate-400" title={emp.retailShopAssigned || ($language === "vi" ? "Trụ sở chính" : "Headquarters")}>
                  {emp.retailShopAssigned || ($language === "vi" ? "Trụ sở chính" : "Headquarters")}
                </div>
              </div>
            </div>

            <!-- Footer: Actions & Joined Date -->
            <div class="mt-4 pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
              <div class="text-[10px] text-slate-400">
                {$language === "vi" ? "Vào làm: " : "Joined: "} {emp.dateOfJoining}
              </div>
              
              <div class="flex items-center gap-1 opacity-100 sm:opacity-0 sm:group-hover:opacity-100 transition-opacity">
                <button
                  onclick={() => handleOpenEmployeeModal(emp)}
                  class="p-1.5 rounded hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 hover:text-indigo-600 transition"
                  title={$language === "vi" ? "Chỉnh sửa" : "Edit"}
                >
                  <Edit2 class="h-3.5 w-3.5" />
                </button>
                <button
                  onclick={() => handleDeleteEmployee(emp)}
                  class="p-1.5 rounded hover:bg-rose-50 dark:hover:bg-rose-950/40 text-slate-500 hover:text-rose-600 transition"
                  title={$language === "vi" ? "Xóa" : "Delete"}
                >
                  <Trash2 class="h-3.5 w-3.5" />
                </button>
              </div>
            </div>
          </div>
        {/each}
      </div>
    </div>
  {/if}

  <!-- TAB 3: STOCK / INVENTORY -->
  {#if activeTab === "stock"}
    <div class="space-y-4">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4"
        >
          <div class="text-xs font-semibold text-slate-500 uppercase">
            {$language === "vi"
              ? "Tổng thiết bị tồn kho"
              : "Total Equipment Stock"}
          </div>
          <div class="text-2xl font-bold text-slate-900 dark:text-white mt-1">
            {$inventory.reduce((sum, item) => sum + item.stockQuantity, 0)}
            {$language === "vi" ? "thiết bị" : "Units"}
          </div>
        </div>
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4"
        >
          <div class="text-xs font-semibold text-slate-500 uppercase">
            {$language === "vi" ? "Cảnh báo sắp hết hàng" : "Low Stock Alerts"}
          </div>
          <div class="text-2xl font-bold text-amber-500 mt-1">
            {lowStockItems.length}
            {$language === "vi"
              ? "mặt hàng dưới mức tối thiểu"
              : "Items Below Reorder"}
          </div>
        </div>
        <div
          class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4"
        >
          <div class="text-xs font-semibold text-slate-500 uppercase">
            {$language === "vi" ? "Ước tính giá trị kho" : "Valuation (Est.)"}
          </div>
          <div
            class="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-1"
          >
            ${$inventory
              .reduce(
                (sum, item) => sum + item.stockQuantity * item.unitCost,
                0,
              )
              .toLocaleString()}
          </div>
        </div>
      </div>

      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div
          class="p-4 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row gap-3 items-center justify-between"
        >
          <div class="flex flex-1 items-center gap-3 w-full sm:w-auto">
            <!-- Thanh tìm kiếm thế chỗ heading -->
            <div class="relative flex-1 max-w-md">
              <Search class="absolute left-3 top-2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                bind:value={stockSearch}
                placeholder={$language === "vi" ? "Tìm thiết bị (Mã, Tên)..." : "Search equipment..."}
                class="w-full pl-9 pr-4 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>
            
            <!-- Nút lọc (Filter dropdown) -->
            <select
              bind:value={stockCategoryFilter}
              class="text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500 max-w-[180px]"
            >
              {#each stockCategories as cat}
                <option value={cat}>{cat === 'All' ? ($language === "vi" ? "Tất cả danh mục" : "All Categories") : cat}</option>
              {/each}
            </select>
          </div>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead
              class="bg-slate-50 dark:bg-slate-800/60 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Mã VT" : "Item Code"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Tên thiết bị / Vật tư"
                    : "Device / Equipment Name"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Danh mục" : "Category"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Vị trí kho" : "Depot Location"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Số lượng tồn" : "Stock Units"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Ngưỡng đặt lại"
                    : "Reorder Threshold"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Đơn giá" : "Unit Cost"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Nhà cung cấp" : "Supplier"}</th
                >
                <th class="px-4 py-3 text-right"
                  >{$language === "vi" ? "Thao tác" : "Actions"}</th
                >
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              {#each paginatedStock as item (item.id)}
                {@const isLow = item.stockQuantity <= item.reorderLevel}
                <tr class="hover:bg-slate-50 dark:hover:bg-slate-800/40">
                  <td
                    class="px-4 py-3 font-mono font-medium text-slate-900 dark:text-slate-100"
                    >{item.itemCode}</td
                  >
                  <td
                    class="px-4 py-3 font-medium text-slate-900 dark:text-white"
                    >{item.name}</td
                  >
                  <td class="px-4 py-3">
                    <span
                      class="px-2 py-0.5 rounded text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300"
                      >{item.category}</span
                    >
                  </td>
                  <td
                    class="px-4 py-3 text-xs text-slate-500 dark:text-slate-400"
                    >{item.location}</td
                  >
                  <td class="px-4 py-3">
                    <span
                      class="font-semibold tabular-nums {isLow
                        ? 'text-amber-600 dark:text-amber-400'
                        : 'text-slate-900 dark:text-slate-100'}"
                    >
                      {item.stockQuantity}
                    </span>
                    {#if isLow}
                      <span
                        class="ml-2 text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300 uppercase"
                      >
                        {$language === "vi" ? "Sắp hết" : "Low"}
                      </span>
                    {/if}
                  </td>
                  <td class="px-4 py-3 text-xs text-slate-500 tabular-nums"
                    >{item.reorderLevel}</td
                  >
                  <td class="px-4 py-3 font-mono tabular-nums text-xs"
                    >${item.unitCost.toFixed(2)}</td
                  >
                  <td
                    class="px-4 py-3 text-xs text-slate-500 dark:text-slate-400"
                    >{item.supplier}</td
                  >
                  <td class="px-4 py-3 text-right">
                    <div class="flex items-center justify-end space-x-1">
                      <button
                        onclick={() => handleOpenStockModal(item)}
                        class="p-1.5 rounded-md hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-300 hover:text-indigo-600 transition opacity-80 hover:opacity-100 active:scale-95 cursor-pointer"
                        title={$language === "vi"
                          ? "Chỉnh sửa vật tư"
                          : "Edit Item"}
                      >
                        <Edit2 class="h-4 w-4" />
                      </button>
                      <button
                        onclick={() => handleDeleteStock(item)}
                        class="p-1.5 rounded-md hover:bg-rose-50 dark:hover:bg-rose-950/40 text-slate-400 hover:text-rose-600 transition opacity-80 hover:opacity-100 active:scale-95 cursor-pointer"
                        title={$language === "vi"
                          ? "Xóa vật tư"
                          : "Delete Item"}
                      >
                        <Trash2 class="h-4 w-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>

        <!-- Pagination Controls -->
        <div class="p-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/50 flex flex-col sm:flex-row items-center justify-center gap-3 text-sm">
          <div class="flex items-center space-x-2">
            <button
              disabled={stockCurrentPage === 1}
              onclick={() => stockCurrentPage--}
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-medium"
            >
              {$language === "vi" ? "Trước" : "Prev"}
            </button>
            <span class="px-3 py-1.5 font-medium text-slate-700 dark:text-slate-300">
              {stockCurrentPage} / {totalStockPages}
            </span>
            <button
              disabled={stockCurrentPage >= totalStockPages}
              onclick={() => stockCurrentPage++}
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-medium"
            >
              {$language === "vi" ? "Sau" : "Next"}
            </button>
          </div>
        </div>
      </div>
    </div>
  {/if}

  <!-- TAB 4: VENDOR MANAGEMENT -->
  {#if activeTab === "vendors"}
    <div class="space-y-4">
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div
          class="p-4 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row gap-3 items-center justify-between"
        >
          <div class="flex flex-1 items-center gap-3 w-full sm:w-auto">
            <div class="relative flex-1 max-w-md">
              <Search class="absolute left-3 top-2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                bind:value={vendorSearch}
                placeholder={$language === "vi" ? "Tìm nhà cung cấp..." : "Search vendors..."}
                class="w-full pl-9 pr-4 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>
            
            <select
              bind:value={vendorCategoryFilter}
              class="text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500 max-w-[180px]"
            >
              {#each vendorCategories as cat}
                <option value={cat}>{cat === 'All' ? ($language === "vi" ? "Tất cả danh mục" : "All Categories") : cat}</option>
              {/each}
            </select>
          </div>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead
              class="bg-slate-50 dark:bg-slate-800/60 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Mã NCC" : "Vendor Code"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Tên công ty" : "Company Name"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Người đại diện"
                    : "Contact Person"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Danh mục cung cấp"
                    : "Supply Category"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Số điện thoại & Email"
                    : "Direct Phone & Email"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Địa chỉ" : "Facility Address"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Trạng thái" : "Status"}</th
                >
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              {#each paginatedVendors as vnd (vnd.id)}
                <tr
                  onclick={() => handleOpenVendorModal(vnd)}
                  class="hover:bg-indigo-50/60 dark:hover:bg-indigo-950/30 transition cursor-pointer group"
                  title={$language === "vi" ? "Bấm vào dòng để xem chi tiết và chỉnh sửa nhà cung cấp" : "Click row to view and edit supplier details"}
                >
                  <td
                    class="px-4 py-3 font-mono font-medium text-slate-900 dark:text-slate-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition"
                    >{vnd.vendorCode}</td
                  >
                  <td
                    class="px-4 py-3 font-semibold text-slate-900 dark:text-white group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition"
                    >{vnd.companyName}</td
                  >
                  <td class="px-4 py-3 text-slate-700 dark:text-slate-300"
                    >{vnd.contactPerson}</td
                  >
                  <td class="px-4 py-3">
                    <span
                      class="px-2 py-0.5 rounded text-xs font-medium bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400"
                      >{vnd.category}</span
                    >
                  </td>
                  <td class="px-4 py-3 text-xs space-y-0.5">
                    <div class="text-slate-600 dark:text-slate-300">
                      {vnd.phone}
                    </div>
                    <div class="text-slate-400">{vnd.email}</div>
                  </td>
                  <td class="px-4 py-3 text-xs text-slate-500 max-w-xs truncate"
                    >{vnd.address}</td
                  >
                  <td class="px-4 py-3">
                    <span
                      class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400"
                    >
                      {$language === "vi" ? "Đang hợp tác" : vnd.status}
                    </span>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>

        <!-- Pagination Controls -->
        <div class="p-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/50 flex flex-col sm:flex-row items-center justify-center gap-3 text-sm">
          <div class="flex items-center space-x-2">
            <button
              disabled={vendorCurrentPage === 1}
              onclick={() => vendorCurrentPage--}
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-medium"
            >
              {$language === "vi" ? "Trước" : "Prev"}
            </button>
            <span class="px-3 py-1.5 font-medium text-slate-700 dark:text-slate-300">
              {vendorCurrentPage} / {totalVendorPages}
            </span>
            <button
              disabled={vendorCurrentPage >= totalVendorPages}
              onclick={() => vendorCurrentPage++}
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-medium"
            >
              {$language === "vi" ? "Sau" : "Next"}
            </button>
          </div>
        </div>
      </div>
    </div>
  {/if}

  <!-- TAB 5: RETAIL SHOPS -->
  <!-- TAB: PURCHASE ORDERS (Purchase List — đặt mua thiết bị từ vendor) -->
  {#if activeTab === "purchases"}
    <div class="space-y-6">
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div
          class="p-4 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row gap-3 items-center justify-between"
        >
          <div class="flex flex-1 items-center gap-3 w-full sm:w-auto">
            <div class="relative flex-1 max-w-md">
              <Search class="absolute left-3 top-2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                bind:value={purchaseSearch}
                placeholder={$language === "vi"
                  ? "Tìm mã đơn, vật tư, nhà cung cấp..."
                  : "Search by PO #, item, vendor..."}
                class="w-full pl-9 pr-4 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>
            <select
              bind:value={purchaseStatusFilter}
              class="text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500 max-w-[170px]"
            >
              <option value="all">{$language === "vi" ? "Tất cả trạng thái" : "All Statuses"}</option>
              <option value="Submitted">{$language === "vi" ? "Đã gửi" : "Submitted"}</option>
              <option value="Received">{$language === "vi" ? "Đã nhận hàng" : "Received"}</option>
              <option value="Cancelled">{$language === "vi" ? "Đã hủy" : "Cancelled"}</option>
            </select>
          </div>
          <button
            onclick={handleOpenPurchaseModal}
            class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-700 hover:to-violet-700 text-white text-sm font-bold transition shadow-sm"
          >
            <Plus class="h-4 w-4" />
            {$language === "vi" ? "Tạo đơn đặt mua" : "New Purchase Order"}
          </button>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead
              class="bg-slate-50 dark:bg-slate-800/60 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3">{$language === "vi" ? "Mã đơn" : "PO #"}</th>
                <th class="px-4 py-3">{$language === "vi" ? "Vật tư" : "Item"}</th>
                <th class="px-4 py-3">{$language === "vi" ? "Nhà cung cấp" : "Vendor"}</th>
                <th class="px-4 py-3">{$language === "vi" ? "Chi nhánh nhận" : "Ship To"}</th>
                <th class="px-4 py-3 text-center">{$language === "vi" ? "SL" : "Qty"}</th>
                <th class="px-4 py-3 text-right">{$language === "vi" ? "Đơn giá" : "Unit Cost"}</th>
                <th class="px-4 py-3 text-right">{$language === "vi" ? "Tổng tiền" : "Total"}</th>
                <th class="px-4 py-3">{$language === "vi" ? "Đặt ngày / Dự kiến nhận" : "Ordered / Expected"}</th>
                <th class="px-4 py-3 text-center">{$language === "vi" ? "Trạng thái" : "Status"}</th>
                <th class="px-4 py-3 text-right">{$language === "vi" ? "Thao tác" : "Actions"}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              {#if filteredPurchaseOrders.length > 0}
                {#each filteredPurchaseOrders as po (po.id)}
                  <tr class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition">
                    <td class="px-4 py-3 font-mono font-bold text-indigo-600 dark:text-indigo-400">{po.id}</td>
                    <td class="px-4 py-3">
                      <div class="font-semibold text-slate-900 dark:text-white">{po.itemName}</div>
                      <div class="text-xs font-mono text-slate-500">{po.itemCode} · {po.category}</div>
                    </td>
                    <td class="px-4 py-3 text-slate-700 dark:text-slate-300">{po.vendorName || po.vendorId}</td>
                    <td class="px-4 py-3 text-slate-700 dark:text-slate-300">
                      {po.storeId
                        ? ($retailShops.find((s) => s.id === po.storeId)?.name ?? po.storeId)
                        : ($language === "vi" ? "Kho trung tâm" : "Central Warehouse")}
                    </td>
                    <td class="px-4 py-3 text-center font-mono">{po.quantity}</td>
                    <td class="px-4 py-3 text-right font-mono">${po.unitCost.toFixed(2)}</td>
                    <td class="px-4 py-3 text-right font-mono font-bold">${po.totalCost.toFixed(2)}</td>
                    <td class="px-4 py-3 text-xs text-slate-600 dark:text-slate-400">
                      <div>{po.orderDate}</div>
                      <div class="text-slate-400">{po.expectedDate ?? "—"}</div>
                    </td>
                    <td class="px-4 py-3 text-center">
                      {#if po.status === "Received"}
                        <span class="inline-flex px-2 py-0.5 text-[11px] font-bold rounded-full bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400">
                          {$language === "vi" ? "Đã nhận" : "Received"}
                        </span>
                      {:else if po.status === "Cancelled"}
                        <span class="inline-flex px-2 py-0.5 text-[11px] font-bold rounded-full bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400">
                          {$language === "vi" ? "Đã hủy" : "Cancelled"}
                        </span>
                      {:else}
                        <span class="inline-flex px-2 py-0.5 text-[11px] font-bold rounded-full bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
                          {$language === "vi" ? "Chờ hàng" : "Submitted"}
                        </span>
                      {/if}
                    </td>
                    <td class="px-4 py-3">
                      <div class="flex items-center justify-end gap-1.5">
                        {#if po.status === "Submitted"}
                          <button
                            onclick={() => handleReceivePurchase(po)}
                            class="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-bold bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800 hover:bg-emerald-100 transition"
                            title={$language === "vi" ? "Xác nhận đã nhận hàng (tự cộng kho)" : "Mark received (auto-restock)"}
                          >
                            <CheckCircle2 class="h-3.5 w-3.5" />
                            {$language === "vi" ? "Nhận hàng" : "Receive"}
                          </button>
                          <button
                            onclick={() => handleCancelPurchase(po)}
                            class="px-2.5 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 transition"
                          >
                            {$language === "vi" ? "Hủy" : "Cancel"}
                          </button>
                        {/if}
                        {#if po.status !== "Received"}
                          <button
                            onclick={() => handleDeletePurchase(po)}
                            class="p-2 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-rose-100 hover:text-rose-600 dark:hover:bg-rose-900/40 dark:hover:text-rose-400 transition"
                            title={$language === "vi" ? "Xóa đơn mua" : "Delete PO"}
                          >
                            <Trash2 class="h-4 w-4" />
                          </button>
                        {/if}
                      </div>
                    </td>
                  </tr>
                {/each}
              {:else}
                <tr>
                  <td colspan="10" class="px-4 py-8 text-center text-slate-500 text-xs">
                    <ShoppingCart class="h-8 w-8 mx-auto text-slate-400 mb-2" />
                    <p>
                      {$language === "vi"
                        ? "Chưa có đơn đặt mua nào — tạo đơn để nhập thiết bị từ vendor."
                        : "No purchase orders yet — create one to restock equipment from a vendor."}
                    </p>
                  </td>
                </tr>
              {/if}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  {/if}

  {#if showPurchaseModal}
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm animate-in fade-in duration-200">
      <div class="bg-white dark:bg-slate-900 rounded-2xl max-w-xl w-full border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden flex flex-col max-h-[85vh]">
        <div class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50/70 dark:bg-slate-800/40">
          <div class="flex items-center space-x-2.5">
            <div class="p-2 bg-indigo-500/20 text-indigo-600 dark:text-indigo-400 rounded-lg">
              <ShoppingCart class="h-5 w-5" />
            </div>
            <div>
              <h3 class="font-bold text-base text-slate-900 dark:text-white">
                {$language === "vi" ? "Tạo đơn đặt mua thiết bị" : "New Purchase Order"}
              </h3>
              <p class="text-xs text-slate-500">
                {$language === "vi"
                  ? "Đơn đã gửi cho vendor; khi nhận hàng, tồn kho chi nhánh được cộng tự động."
                  : "Order is sent to the vendor; receiving it restocks the branch inventory automatically."}
              </p>
            </div>
          </div>
          <button type="button" onclick={() => (showPurchaseModal = false)} class="p-1 text-slate-400 hover:text-slate-600 rounded-lg" aria-label="Close">
            <X class="h-5 w-5" />
          </button>
        </div>
        <form onsubmit={(e) => { e.preventDefault(); handleSavePurchaseOrder(); }} class="p-6 overflow-y-auto space-y-4">
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Nhà cung cấp *" : "Vendor *"}</span>
              <select bind:value={purchaseFormData.vendorId} required
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500">
                {#each $vendors as v (v.id)}
                  <option value={v.id}>{v.companyName}</option>
                {/each}
              </select>
            </label>
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Chi nhánh nhận hàng" : "Ship-to Branch"}</span>
              <select bind:value={purchaseFormData.storeId}
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500">
                <option value="">{$language === "vi" ? "Kho trung tâm" : "Central Warehouse"}</option>
                {#each $retailShops as s (s.id)}
                  <option value={s.id}>{s.name} ({s.shopCode})</option>
                {/each}
              </select>
            </label>
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Mã vật tư *" : "Item Code *"}</span>
              <input type="text" bind:value={purchaseFormData.itemCode} required placeholder="EQ-MDM-100"
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Tên vật tư *" : "Item Name *"}</span>
              <input type="text" bind:value={purchaseFormData.itemName} required placeholder="AX-100 Fiber Modem"
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Danh mục" : "Category"}</span>
              <select bind:value={purchaseFormData.category}
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500">
                {#each ["Modem", "Router", "Fiber ONT", "Splitter", "Patch Cord", "VoIP Adapter"] as cat}
                  <option value={cat}>{cat}</option>
                {/each}
              </select>
            </label>
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Số lượng *" : "Quantity *"}</span>
              <input type="number" min="1" bind:value={purchaseFormData.quantity} required
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Đơn giá ($)" : "Unit Cost ($)"}</span>
              <input type="number" step="0.01" min="0" bind:value={purchaseFormData.unitCost}
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
            <label class="block">
              <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Dự kiến nhận hàng" : "Expected Date"}</span>
              <input type="date" bind:value={purchaseFormData.expectedDate}
                class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </label>
          </div>
          <label class="block">
            <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === "vi" ? "Ghi chú" : "Notes"}</span>
            <textarea bind:value={purchaseFormData.notes} rows="2"
              class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"></textarea>
          </label>
          <div class="p-3 rounded-lg bg-indigo-50 dark:bg-indigo-950/30 border border-indigo-200 dark:border-indigo-800/50 flex items-center justify-between text-xs">
            <span class="text-slate-600 dark:text-slate-300">{$language === "vi" ? "Tổng tiền đơn:" : "Order Total:"}</span>
            <strong class="font-mono text-base text-indigo-700 dark:text-indigo-300">${purchaseTotalPreview.toFixed(2)}</strong>
          </div>
          <div class="flex justify-end space-x-2 pt-1">
            <button type="button" onclick={() => (showPurchaseModal = false)}
              class="px-4 py-2 bg-slate-200 hover:bg-slate-300 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-800 dark:text-slate-200 rounded-lg text-xs font-bold transition">
              {$language === "vi" ? "Hủy" : "Cancel"}
            </button>
            <button type="submit" disabled={isSavingPurchase}
              class="px-4 py-2 bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-700 hover:to-violet-700 disabled:opacity-60 text-white rounded-lg text-xs font-bold transition shadow-sm">
              {isSavingPurchase ? ($language === "vi" ? "Đang lưu…" : "Saving…") : ($language === "vi" ? "Tạo đơn" : "Create Order")}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  {#if activeTab === "shops"}
    <div class="space-y-6 animate-fade-in">
      <div
        class="bg-gradient-to-r from-blue-600 to-indigo-700 p-6 rounded-2xl shadow-lg flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 text-white relative overflow-hidden"
      >
        <div class="absolute -right-10 -top-10 opacity-20 pointer-events-none">
          <Store class="h-40 w-40" />
        </div>
        <div class="relative z-10">
          <h3 class="text-xl font-bold">
            {$language === "vi"
              ? "Mạng lưới điểm bán lẻ & Chi nhánh khu vực"
              : "Retail Outlet Network & Regional Centers"}
          </h3>
          <p class="text-blue-100 text-sm mt-1 max-w-2xl">
            {$language === "vi"
              ? "Quản lý điểm giao dịch, mã thành phố, nhân sự phục vụ và chỉ tiêu thuê bao với giao diện trực quan."
              : "Manage outlets, city codes, staffing and subscriber metrics with an intuitive interface."}
          </p>
        </div>
        <button
          onclick={() => handleOpenShopModal()}
          class="relative z-10 px-5 py-2.5 rounded-xl text-sm font-semibold bg-white/20 hover:bg-white text-white hover:text-blue-700 border border-white/30 hover:border-white transition-all shadow-sm backdrop-blur-md flex items-center space-x-2"
        >
          <Plus class="h-5 w-5" />
          <span>{$language === "vi" ? "Thêm điểm bán mới" : "New Outlet"}</span>
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {#each $retailShops as shop (shop.id)}
          <div
            class="group relative rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm hover:shadow-xl hover:shadow-blue-500/10 hover:border-blue-400/50 dark:hover:border-blue-500/50 transition-all duration-300 overflow-hidden flex flex-col justify-between"
          >
            <!-- Decorative accent -->
            <div
              class="absolute top-0 right-0 w-24 h-24 bg-gradient-to-bl from-blue-500/10 to-transparent rounded-bl-full pointer-events-none transition-transform group-hover:scale-110"
            ></div>

            <div class="space-y-4">
              <div class="flex items-start justify-between">
                <div class="flex items-center space-x-3">
                  <div
                    class="h-12 w-12 rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 text-white flex items-center justify-center font-bold text-sm shadow-inner shadow-white/20 relative z-10 group-hover:-translate-y-1 transition-transform"
                  >
                    {shop.shopCode}
                  </div>
                  <div>
                    <h3
                      class="font-bold text-lg text-slate-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors line-clamp-1"
                    >
                      {shop.name}
                    </h3>
                    <p
                      class="text-xs font-medium text-slate-500 dark:text-slate-400 flex items-center mt-0.5"
                    >
                      <MapPin class="h-3 w-3 mr-1 inline-block" />
                      {$language === "vi"
                        ? `Khu vực ${shop.city} · Mã TP: ${shop.cityCode}`
                        : `${shop.city} Region · City: ${shop.cityCode}`}
                    </p>
                  </div>
                </div>
              </div>

              <div class="text-sm space-y-2 text-slate-600 dark:text-slate-300">
                <div
                  class="flex items-center p-2 rounded-lg bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-800"
                >
                  <MapPin class="h-4 w-4 text-blue-500 mr-2 flex-shrink-0" />
                  <span class="truncate" title={shop.address}
                    >{shop.address}</span
                  >
                </div>
                <div
                  class="flex items-center p-2 rounded-lg bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-800"
                >
                  <Phone class="h-4 w-4 text-emerald-500 mr-2 flex-shrink-0" />
                  <span>{shop.phone}</span>
                </div>
                <div
                  class="flex items-center p-2 rounded-lg bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-800"
                >
                  <Users class="h-4 w-4 text-indigo-500 mr-2 flex-shrink-0" />
                  <span
                    ><span class="text-slate-400"
                      >{$language === "vi" ? "Quản lý:" : "Mgr:"}</span
                    >
                    <strong class="text-slate-900 dark:text-white"
                      >{shop.managerName}</strong
                    ></span
                  >
                </div>
              </div>
            </div>

            <div
              class="mt-6 pt-4 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between"
            >
              <div class="flex items-center space-x-4">
                <div class="text-center">
                  <p
                    class="text-[10px] uppercase font-bold tracking-wider text-slate-400"
                  >
                    {$language === "vi" ? "Nhân sự" : "Staff"}
                  </p>
                  <p class="font-bold text-slate-800 dark:text-slate-100">
                    {shop.activeEmployeesCount}
                  </p>
                </div>
                <div class="w-px h-8 bg-slate-200 dark:bg-slate-700"></div>
                <div class="text-center">
                  <p
                    class="text-[10px] uppercase font-bold tracking-wider text-slate-400"
                  >
                    {$language === "vi" ? "Thuê bao" : "Subs"}
                  </p>
                  <p class="font-bold text-blue-600 dark:text-blue-400">
                    {shop.totalSubscribersServed.toLocaleString()}
                  </p>
                </div>
              </div>
              <div
                class="flex items-center space-x-1 opacity-0 group-hover:opacity-100 transition-opacity"
              >
                <button
                  onclick={() => handleOpenShopModal(shop)}
                  class="p-2 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-blue-100 hover:text-blue-600 dark:hover:bg-blue-900/40 dark:hover:text-blue-400 transition"
                  title={$language === "vi" ? "Sửa điểm bán lẻ" : "Edit Shop"}
                >
                  <Edit2 class="h-4 w-4" />
                </button>
                <button
                  onclick={() => handleDeleteShop(shop)}
                  class="p-2 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-rose-100 hover:text-rose-600 dark:hover:bg-rose-900/40 dark:hover:text-rose-400 transition"
                  title={$language === "vi" ? "Xóa điểm bán lẻ" : "Delete Shop"}
                >
                  <Trash2 class="h-4 w-4" />
                </button>
              </div>
            </div>
          </div>
        {/each}
      </div>
    </div>
  {/if}

  <!-- TAB 6: PLAN MANAGEMENT -->
  {#if activeTab === "plans"}
    <div class="space-y-6">
      <!-- Plans Table -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div
          class="p-4 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row gap-3 items-center justify-between"
        >
          <div class="flex flex-1 items-center gap-3 w-full sm:w-auto">
            <div class="relative flex-1 max-w-md">
              <Search class="absolute left-3 top-2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                bind:value={planSearch}
                placeholder={$language === "vi" ? "Tìm gói cước..." : "Search plans..."}
                class="w-full pl-9 pr-4 py-1.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>
            
            <select
              bind:value={planTypeFilter}
              class="text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500 max-w-[180px]"
            >
              {#each planTypes as pt}
                <option value={pt}>{pt === 'All' ? ($language === "vi" ? "Tất cả loại" : "All Types") : pt}</option>
              {/each}
            </select>
          </div>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead
              class="bg-slate-50 dark:bg-slate-800/60 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Tên gói & Chi tiết"
                    : "Plan Name & Details"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Loại kết nối" : "Connection Type"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Tốc độ / Băng thông"
                    : "Speed / Bandwidth"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Tiền cọc ($)"
                    : "Security Deposit ($)"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Cước thuê tháng ($)"
                    : "Monthly Rental ($)"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Cước theo giờ ($)"
                    : "Hourly Charge ($)"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Giới hạn dữ liệu" : "Data Limit"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Trạng thái" : "Status"}</th
                >
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              {#each paginatedPlans as plan (plan.id)}
                <tr
                  onclick={() => handleOpenPlanModal(plan)}
                  class="hover:bg-indigo-50/60 dark:hover:bg-indigo-950/30 transition cursor-pointer group"
                  title={$language === "vi" ? "Bấm vào dòng để xem chi tiết và chỉnh sửa gói cước" : "Click row to view and edit plan details"}
                >
                  <td class="px-4 py-3 max-w-xs">
                    <div class="font-semibold text-slate-900 dark:text-white group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition">
                      {getPlanName(plan, $language)}
                    </div>
                    <div class="text-[11px] text-slate-500">
                      {getPlanBillingCycle(
                        plan.billingCycle,
                        $language,
                      )}{plan.validity ? ` · ${plan.validity}` : ""}
                    </div>
                    <div class="text-xs text-slate-500 truncate">
                      {getPlanDescription(plan, $language)}
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    <span
                      class="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold {plan.type ===
                      'Broadband'
                        ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-300'
                        : plan.type === 'Dial-Up'
                          ? 'bg-amber-100 dark:bg-amber-900/40 text-amber-800 dark:text-amber-300'
                          : 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-800 dark:text-emerald-300'}"
                    >
                      {$language === "vi"
                        ? plan.type === "Broadband"
                          ? "Cáp quang"
                          : plan.type === "Dial-Up"
                            ? "Quay số"
                            : "Cố định"
                        : plan.type}
                    </span>
                  </td>
                  <td
                    class="px-4 py-3 text-xs font-mono text-slate-700 dark:text-slate-300"
                    >{getPlanSpeedOrBandwidth(
                      plan.speedOrBandwidth,
                      $language,
                    )}</td
                  >
                  <td
                    class="px-4 py-3 font-mono tabular-nums font-medium text-slate-900 dark:text-slate-100"
                    >${plan.securityDeposit.toFixed(2)}</td
                  >
                  <td
                    class="px-4 py-3 font-mono tabular-nums font-bold text-indigo-600 dark:text-indigo-400"
                  >
                    ${plan.monthlyRental.toFixed(2)}<span
                      class="text-xs font-normal text-slate-400"
                      >/{$language === "vi" ? "th" : "mo"}</span
                    >
                  </td>
                  <td
                    class="px-4 py-3 font-mono tabular-nums text-xs text-slate-600 dark:text-slate-400"
                  >
                    {plan.hourlyCharge
                      ? `$${plan.hourlyCharge.toFixed(2)}/${$language === "vi" ? "giờ" : "hr"}`
                      : "—"}
                  </td>
                  <td
                    class="px-4 py-3 text-xs text-slate-600 dark:text-slate-400"
                    >{plan.dataLimit ||
                      ($language === "vi" ? "Không giới hạn" : "Unlimited")}</td
                  >
                  <td class="px-4 py-3">
                    <span
                      class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400"
                    >
                      {$language === "vi" ? "Hoạt động" : plan.status}
                    </span>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>

        <!-- Pagination Controls -->
        <div class="p-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/50 flex flex-col sm:flex-row items-center justify-center gap-3 text-sm">
          <div class="flex items-center space-x-2">
            <button
              disabled={planCurrentPage === 1}
              onclick={() => planCurrentPage--}
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-medium"
            >
              {$language === "vi" ? "Trước" : "Prev"}
            </button>
            <span class="px-3 py-1.5 font-medium text-slate-700 dark:text-slate-300">
              {planCurrentPage} / {totalPlanPages}
            </span>
            <button
              disabled={planCurrentPage >= totalPlanPages}
              onclick={() => planCurrentPage++}
              class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-medium"
            >
              {$language === "vi" ? "Sau" : "Next"}
            </button>
          </div>
        </div>
      </div>
    </div>
  {/if}

  <!-- TAB: CUSTOMER FEEDBACK -->
  <!-- TAB: LIVE CHAT với khách từ chatbox trang index -->
  {#if activeTab === "livechat"}
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-5 tab-content-animate">
      <!-- Cột trái: danh sách phiên chat -->
      <div class="lg:col-span-1 flex flex-col h-[calc(100vh-14rem)] min-h-[500px]">
        <div class="relative shrink-0 mb-3">
          <Search
            class="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400 pointer-events-none"
          />
          <input
            type="text"
            bind:value={chatSearch}
            placeholder={$language === "vi" ? "Tìm kiếm cuộc trò chuyện..." : "Search conversations..."}
            class="w-full pl-9 pr-3 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>

        <div class="flex-1 overflow-y-auto space-y-3 pr-1">
          {#if filteredChatSessions.length === 0}
          <div
            class="rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-8 text-center text-sm text-slate-400"
          >
            {$language === "vi"
              ? "Chưa có khách nào mở chat. Hộp chat nằm ở góc phải trang chủ."
              : "No customer chats yet. The chatbox is at the bottom-right of the homepage."}
          </div>
        {:else}
          {#each filteredChatSessions as s (s.sessionId)}
            <button
              type="button"
              onclick={() => openChatSession(s.sessionId)}
              class="w-full text-left rounded-xl bg-white dark:bg-slate-900 border p-3.5 shadow-sm transition hover:shadow-md cursor-pointer {activeChatSessionId ===
              s.sessionId
                ? 'border-indigo-500 dark:border-indigo-400 ring-2 ring-indigo-500/30'
                : 'border-slate-200 dark:border-slate-800'}"
            >
              <div class="flex items-center justify-between gap-2">
                <div
                  class="font-semibold text-sm text-slate-900 dark:text-white truncate"
                >
                  {s.customerName}
                </div>
                <div class="flex items-center gap-1.5 shrink-0">
                  {#if s.unreadCount > 0}
                    <span
                      class="text-[10px] font-bold px-1.5 py-0.5 rounded-full bg-rose-500 text-white"
                    >
                      {s.unreadCount}
                    </span>
                  {/if}
                  <span class="text-[10px] text-slate-400 font-mono"
                    >{s.sessionId}</span
                  >
                </div>
              </div>
              <p
                class="text-xs text-slate-500 dark:text-slate-400 mt-1 truncate"
              >
                {s.lastSenderType === "admin" ? "↩ " : ""}{s.lastMessage}
              </p>
              <div
                class="text-[10px] text-slate-400 mt-1 flex items-center justify-between"
              >
                <span
                  >{s.messageCount}
                  {$language === "vi" ? "tin nhắn" : "messages"}</span
                >
                <span>{s.lastMessageAtDisplay}</span>
              </div>
            </button>
          {/each}
        {/if}
        </div>
      </div>

      <!-- Cột phải: khung hội thoại + ô trả lời -->
      <div
        class="lg:col-span-2 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col h-[calc(100vh-14rem)] min-h-[500px] overflow-hidden"
      >
        {#if activeChatSessionId}
          <!-- Header phiên đang mở -->
          <div
            class="px-4 py-3 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between bg-slate-50 dark:bg-slate-950/50"
          >
            <div class="flex items-center gap-3">
              <div
                class="h-9 w-9 rounded-full bg-gradient-to-tr from-indigo-600 to-sky-600 text-white flex items-center justify-center text-xs font-bold shrink-0"
              >
                {chatSessions
                  .find((s) => s.sessionId === activeChatSessionId)
                  ?.customerName.slice(0, 2)
                  .toUpperCase() ?? "KH"}
              </div>
              <div>
                <div
                  class="font-semibold text-sm text-slate-900 dark:text-white"
                >
                  {chatSessions.find((s) => s.sessionId === activeChatSessionId)
                    ?.customerName ?? "Khách"}
                </div>
                <div class="text-[10px] text-slate-400 font-mono">
                  {activeChatSessionId}
                </div>
              </div>
            </div>
          </div>

          <!-- Vùng tin nhắn -->
          <div
            bind:this={chatThreadRef}
            class="flex-1 overflow-y-auto px-4 py-4 space-y-3 bg-slate-50 dark:bg-slate-950"
          >
            {#if isChatLoadingMessages}
              <div class="text-center text-xs text-slate-400 py-8">
                {$language === "vi"
                  ? "Đang tải tin nhắn…"
                  : "Loading messages…"}
              </div>
            {:else}
              {#each chatMessages as msg (msg.id)}
                {#if msg.senderType === "admin"}
                  <!-- Tin Admin trả lời (bên phải) -->
                  <div class="flex justify-end">
                    <div class="max-w-[75%]">
                      <div
                        class="px-3.5 py-2.5 rounded-2xl rounded-br-md bg-indigo-600 text-white text-[13px] leading-relaxed shadow-sm"
                      >
                        {msg.message}
                      </div>
                      <div
                        class="text-[10px] text-slate-400 mt-1 mr-1 text-right"
                      >
                        {msg.senderName} · {msg.createdAtDisplay}
                      </div>
                    </div>
                  </div>
                {:else}
                  <!-- Tin của khách (bên trái) -->
                  <div class="flex items-end gap-2">
                    <div
                      class="h-7 w-7 rounded-full bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-300 flex items-center justify-center shrink-0 text-[10px] font-bold"
                    >
                      {msg.senderName.slice(0, 1).toUpperCase()}
                    </div>
                    <div class="max-w-[75%]">
                      <div
                        class="px-3.5 py-2.5 rounded-2xl rounded-bl-md bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-200 text-[13px] leading-relaxed shadow-xs"
                      >
                        {msg.message}
                      </div>
                      <div class="text-[10px] text-slate-400 mt-1 ml-1">
                        {msg.senderName} · {msg.createdAtDisplay}
                      </div>
                    </div>
                  </div>
                {/if}
              {/each}
            {/if}
          </div>

          <!-- Ô nhập trả lời của Admin -->
          <div
            class="p-3 border-t border-slate-200 dark:border-slate-800 flex items-center gap-2 bg-white dark:bg-slate-900"
          >
            <input
              type="text"
              bind:value={chatDraft}
              onkeydown={handleChatReplyKeyDown}
              maxlength="1000"
              class="flex-1 px-3.5 py-2.5 rounded-xl text-[13px] bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />
            <button
              type="button"
              onclick={sendAdminReply}
              disabled={isChatSending || !chatDraft.trim()}
              class="px-4 py-2.5 rounded-xl text-xs font-bold bg-indigo-600 hover:bg-indigo-700 text-white shadow-sm transition active:scale-95 disabled:opacity-40 disabled:pointer-events-none cursor-pointer"
            >
              {$language === "vi" ? "Gửi" : "Send"}
            </button>
          </div>
        {:else}
          <div class="flex-1 flex items-center justify-center p-8">
            <div class="text-center space-y-3">
              <div
                class="h-14 w-14 rounded-2xl bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 flex items-center justify-center mx-auto"
              >
                <Headset class="h-7 w-7" />
              </div>
              <p
                class="text-sm font-semibold text-slate-600 dark:text-slate-300"
              >
                {$language === "vi"
                  ? "Chọn một phiên chat để trả lời khách"
                  : "Select a chat session to reply"}
              </p>
              <p class="text-xs text-slate-400">
                {$language === "vi"
                  ? "Danh sách cập nhật tự động mỗi 5 giây."
                  : "The list refreshes automatically every 5 seconds."}
              </p>
            </div>
          </div>
        {/if}
      </div>
    </div>
  {/if}

  {#if activeTab === "feedback"}
    <div class="space-y-3">
      {#if $feedbacks.length === 0}
        <div
          class="rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-8 text-center text-sm text-slate-400"
        >
          {$language === "vi"
            ? "Chưa có phản hồi nào từ khách hàng."
            : "No customer feedback collected yet."}
        </div>
      {/if}
      {#each $feedbacks as f (f.id)}
        <div
          class="rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-4 shadow-sm space-y-2"
        >
          <div class="flex items-start justify-between gap-3">
            <div>
              <div class="font-semibold text-sm text-slate-900 dark:text-white">
                {f.customerName}
                <span class="text-amber-500 ml-1"
                  >{"★".repeat(f.rating)}{"☆".repeat(5 - f.rating)}</span
                >
              </div>
              <div class="text-[11px] text-slate-500 font-mono">
                {f.category} · {f.accountId || f.orderId || "—"} · {f.createdAt}
              </div>
            </div>
            {#if !f.response}
              <span
                class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300 uppercase"
              >
                {$language === "vi" ? "Chờ phản hồi" : "Awaiting reply"}
              </span>
            {/if}
          </div>
          <p class="text-sm text-slate-700 dark:text-slate-300">{f.message}</p>

          {#if f.response}
            <p
              class="text-xs pl-3 border-l-2 border-indigo-400 text-indigo-700 dark:text-indigo-300"
            >
              <strong
                >{$language === "vi" ? "Đã phản hồi" : "Responded"}:</strong
              >
              {f.response}
              <span class="text-slate-400">
                — {f.respondedBy} · {f.respondedAt}</span
              >
            </p>
          {:else if respondingId === f.id}
            <div class="flex gap-2">
              <input
                type="text"
                bind:value={responseText}
                class="flex-1 px-3 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
              <button
                onclick={() => submitResponse(f.id)}
                class="px-3 py-2 rounded-lg text-xs font-bold bg-indigo-600 hover:bg-indigo-700 text-white"
              >
                {$language === "vi" ? "Gửi" : "Send"}
              </button>
              <button
                onclick={() => {
                  respondingId = null;
                  responseText = "";
                }}
                class="px-3 py-2 rounded-lg text-xs border border-slate-200 dark:border-slate-800 text-slate-500"
              >
                {$language === "vi" ? "Hủy" : "Cancel"}
              </button>
            </div>
          {:else}
            <button
              onclick={() => {
                respondingId = f.id;
                responseText = "";
              }}
              class="text-xs font-semibold text-indigo-600 dark:text-indigo-400 hover:underline"
            >
              {$language === "vi"
                ? "Phản hồi khách hàng"
                : "Respond to customer"}
            </button>
          {/if}
        </div>
      {/each}
    </div>
  {/if}

  <!-- TAB 7: SETTINGS -->
  {#if activeTab === "settings"}
    <SettingsView />
  {/if}

  <!-- TAB: PROFILE -->
  {#if activeTab === "profile"}
    <ProfileView />
  {/if}

  <!-- Employee Modal -->
  {#if isEmployeeModalOpen}
    <div
      onclick={(e) => {
        if (e.target === e.currentTarget) isEmployeeModalOpen = false;
      }}
      class="absolute inset-0 !m-0 z-[100] flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4 cursor-pointer"
      role="dialog"
      aria-modal="true"
      tabindex="-1"
    >
      <div
        class="w-full max-w-lg rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl space-y-4 cursor-default"
        onclick={(e) => e.stopPropagation()}
      >
        <div
          class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 pb-3"
        >
          <h3 class="text-lg font-bold text-slate-900 dark:text-white">
            {editingEmployee
              ? $language === "vi"
                ? "Chỉnh sửa thông tin nhân viên"
                : "Edit Employee Details"
              : $language === "vi"
                ? "Tiếp nhận nhân viên mới"
                : "Onboard New Employee"}
          </h3>
          <button
            onclick={() => (isEmployeeModalOpen = false)}
            class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form onsubmit={handleSaveEmployee} class="space-y-4 text-sm">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Mã nhân viên *" : "Employee Code *"}
              </label>
              <input
                type="text"
                required
                bind:value={employeeFormData.employeeCode}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm font-mono"
              />
            </div>
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Họ và tên *" : "Full Name *"}
              </label>
              <input
                type="text"
                required
                bind:value={employeeFormData.name}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          <!-- Account & Security Section -->
          <div
            class="p-3.5 rounded-xl border border-indigo-100 dark:border-indigo-900/40 bg-indigo-50/50 dark:bg-indigo-950/20 space-y-3"
          >
            <div
              class="flex items-center space-x-2 text-indigo-700 dark:text-indigo-400 font-semibold text-xs"
            >
              <KeyRound class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Tài khoản đăng nhập & Mật khẩu"
                  : "Login Account & Security"}</span
              >
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi"
                    ? "Tài khoản (Email) *"
                    : "Account (Email) *"}
                </label>
                <input
                  type="email"
                  required
                  bind:value={employeeFormData.email}
                  class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>

              <div>
                <div class="flex items-center justify-between mb-1">
                  <label
                    class="block text-xs font-semibold text-slate-700 dark:text-slate-300"
                  >
                    {$language === "vi"
                      ? editingEmployee
                        ? "Mật khẩu mới"
                        : "Mật khẩu khởi tạo *"
                      : editingEmployee
                        ? "New Password"
                        : "Initial Password *"}
                  </label>
                </div>
                <div class="relative">
                  <input
                    type={employeeFormData.showPassword ? "text" : "password"}
                    bind:value={employeeFormData.password}
                    class="w-full pl-3 pr-9 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                  />
                  <button
                    type="button"
                    onclick={() =>
                      (employeeFormData.showPassword =
                        !employeeFormData.showPassword)}
                    class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 p-0.5"
                    tabindex="-1"
                  >
                    {#if employeeFormData.showPassword}
                      <EyeOff class="h-4 w-4" />
                    {:else}
                      <Eye class="h-4 w-4" />
                    {/if}
                  </button>
                </div>
              </div>
            </div>

          </div>

          <div>
            <label
              class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Số điện thoại liên hệ *"
                : "Contact Phone Number *"}
            </label>
            <input
              type="tel"
              required
              bind:value={employeeFormData.phone}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
            />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Chức vụ / Vai trò" : "Designation Role"}
              </label>
              <select
                bind:value={employeeFormData.role}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              >
                <option value="Manager"
                  >{$language === "vi" ? "Quản lý" : "Manager"}</option
                >
                <option value="Retail Staff"
                  >{$language === "vi"
                    ? "Nhân viên bán lẻ"
                    : "Retail Staff"}</option
                >
                <option value="Field Engineer"
                  >{$language === "vi"
                    ? "Kỹ sư hiện trường"
                    : "Field Engineer"}</option
                >
                <option value="Senior Accountant"
                  >{$language === "vi"
                    ? "Kế toán trưởng"
                    : "Senior Accountant"}</option
                >
                <option value="Support Agent"
                  >{$language === "vi"
                    ? "Nhân viên hỗ trợ"
                    : "Support Agent"}</option
                >
              </select>
            </div>
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Phòng ban" : "Department"}
              </label>
              <select
                bind:value={employeeFormData.department}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              >
                <option value="Administration"
                  >{$language === "vi"
                    ? "Hành chính / Quản trị"
                    : "Administration"}</option
                >
                <option value="Retail Outlets"
                  >{$language === "vi"
                    ? "Nhân viên chi nhánh"
                    : "Retail Outlets"}</option
                >
                <option value="Technical Operations"
                  >{$language === "vi"
                    ? "Kỹ thuật vận hành"
                    : "Technical Operations"}</option
                >
                <option value="Finance & Accounts"
                  >{$language === "vi"
                    ? "Tài chính & Kế toán"
                    : "Finance & Accounts"}</option
                >
              </select>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Chi nhánh phân công"
                  : "Assigned Retail Shop"}
              </label>
              <select
                bind:value={employeeFormData.retailShopAssigned}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              >
                <option value="Headquarters (General)">
                  {$language === "vi"
                    ? "Trụ sở chính (Tổng bộ)"
                    : "Headquarters (General)"}
                </option>
                {#each $retailShops as s (s.id)}
                  <option value="{s.name} ({s.shopCode})"
                    >{s.name} ({s.shopCode})</option
                  >
                {/each}
              </select>
            </div>
          </div>

          <div
            class="flex justify-end space-x-3 pt-3 border-t border-slate-100 dark:border-slate-800"
          >
            <button
              type="button"
              onclick={() => (isEmployeeModalOpen = false)}
              class="px-4 py-2 rounded-lg text-sm border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
            >
              {$language === "vi" ? "Hủy" : "Cancel"}
            </button>
            <button
              type="submit"
              class="px-4 py-2 rounded-lg text-sm font-semibold bg-indigo-600 hover:bg-indigo-700 text-white transition shadow"
            >
              {editingEmployee
                ? $language === "vi"
                  ? "Lưu thay đổi"
                  : "Save Changes"
                : $language === "vi"
                  ? "Xác nhận tiếp nhận"
                  : "Confirm & Onboard"}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- Vendor Drawer -->
  {#if isVendorModalOpen}
    <div
      transition:fade={{ duration: 200 }}
      onclick={(e) => {
        if (e.target === e.currentTarget) isVendorModalOpen = false;
      }}
      class="absolute inset-0 !m-0 z-[100] flex justify-end bg-slate-950/50 backdrop-blur-sm cursor-pointer"
      role="dialog"
      aria-modal="true"
      tabindex="-1"
    >
      <div
        transition:fly={{ x: 400, duration: 300, opacity: 1 }}
        class="w-full max-w-lg h-full bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 shadow-2xl flex flex-col cursor-default"
        onclick={(e) => e.stopPropagation()}
      >
        <div
          class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 p-6 shrink-0"
        >
          <h3 class="text-lg font-bold text-slate-900 dark:text-white">
            {editingVendor
              ? $language === "vi"
                ? "Chỉnh sửa nhà cung cấp"
                : "Edit Supplier Details"
              : $language === "vi"
                ? "Đăng ký nhà cung cấp mới"
                : "Register New Vendor"}
          </h3>
          <button
            onclick={() => (isVendorModalOpen = false)}
            class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form onsubmit={handleSaveVendor} class="flex flex-col flex-1 min-h-0 text-sm">
          <div class="flex-1 overflow-y-auto p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Mã nhà cung cấp *" : "Vendor Code *"}
                </label>
                <input
                  type="text"
                  required
                  bind:value={vendorFormData.vendorCode}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm font-mono"
                />
              </div>
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Tên công ty / NCC *" : "Company Name *"}
                </label>
                <input
                  type="text"
                  required
                  bind:value={vendorFormData.companyName}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Người liên hệ *" : "Contact Person *"}
                </label>
                <input
                  type="text"
                  required
                  bind:value={vendorFormData.contactPerson}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Lĩnh vực cung cấp *" : "Supply Category *"}
                </label>
                <input
                  type="text"
                  required
                  placeholder="Fiber Optics, Hardware..."
                  bind:value={vendorFormData.category}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Số điện thoại *" : "Phone Number *"}
                </label>
                <input
                  type="tel"
                  required
                  bind:value={vendorFormData.phone}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  Email *
                </label>
                <input
                  type="email"
                  required
                  bind:value={vendorFormData.email}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Điều khoản thanh toán" : "Payment Terms"}
                </label>
                <input
                  type="text"
                  placeholder="Net 30, Net 45..."
                  bind:value={vendorFormData.paymentTerms}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
              <div>
                <label
                  class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Trạng thái hợp tác" : "Status"}
                </label>
                <select
                  bind:value={vendorFormData.status}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                >
                  <option value="Active">{$language === "vi" ? "Đang hợp tác (Active)" : "Active"}</option>
                  <option value="Inactive">{$language === "vi" ? "Tạm ngưng (Inactive)" : "Inactive"}</option>
                </select>
              </div>
            </div>

            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Địa chỉ trụ sở" : "Physical Address"}
              </label>
              <input
                type="text"
                bind:value={vendorFormData.address}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          <div
            class="p-6 border-t border-slate-100 dark:border-slate-800 shrink-0 flex items-center justify-between bg-slate-50 dark:bg-slate-950/50"
          >
            {#if editingVendor}
              <button
                type="button"
                onclick={() => {
                  const v = editingVendor;
                  if (!v) return;
                  isVendorModalOpen = false;
                  handleDeleteVendor(v);
                }}
                class="px-3.5 py-2 rounded-lg text-sm font-medium border border-rose-200 dark:border-rose-900/50 text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition flex items-center gap-1.5"
              >
                <Trash2 class="h-4 w-4" />
                <span>{$language === "vi" ? "Xóa NCC" : "Delete Vendor"}</span>
              </button>
            {:else}
              <div></div>
            {/if}
            <div class="flex items-center space-x-3">
              <button
                type="button"
                onclick={() => (isVendorModalOpen = false)}
                class="px-4 py-2 rounded-lg text-sm border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
              >
                {$language === "vi" ? "Hủy" : "Cancel"}
              </button>
              <button
                type="submit"
                class="px-4 py-2 rounded-lg text-sm font-semibold bg-indigo-600 hover:bg-indigo-700 text-white transition shadow"
              >
                {editingVendor
                  ? $language === "vi"
                    ? "Lưu thay đổi"
                    : "Save Changes"
                  : $language === "vi"
                    ? "Xác nhận nhà cung cấp"
                    : "Confirm Vendor"}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- Plan Drawer -->
  {#if isPlanModalOpen}
    <div
      transition:fade={{ duration: 200 }}
      onclick={(e) => {
        if (e.target === e.currentTarget) isPlanModalOpen = false;
      }}
      class="absolute inset-0 !m-0 z-[100] flex justify-end bg-slate-950/50 backdrop-blur-sm cursor-pointer"
      role="dialog"
      aria-modal="true"
      tabindex="-1"
    >
      <div
        transition:fly={{ x: 400, duration: 300, opacity: 1 }}
        class="w-full max-w-lg h-full bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 shadow-2xl flex flex-col cursor-default"
        onclick={(e) => e.stopPropagation()}
      >
        <div
          class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 p-6 shrink-0"
        >
          <h3 class="text-lg font-bold text-slate-900 dark:text-white">
            {editingPlan
              ? $language === "vi"
                ? "Chỉnh sửa biểu cước gói dịch vụ"
                : "Edit Plan Tariff"
              : $language === "vi"
                ? "Tạo gói dịch vụ mới"
                : "Create New Service Plan"}
          </h3>
          <button
            onclick={() => (isPlanModalOpen = false)}
            class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form onsubmit={handleSavePlan} class="flex flex-col flex-1 min-h-0 text-sm">
          <div class="flex-1 overflow-y-auto p-6 space-y-4">
            <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Tên gói dịch vụ *" : "Plan Name *"}
              </label>
              <input
                type="text"
                required
                bind:value={planFormData.name}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Loại kết nối *" : "Connection Type *"}
              </label>
              <select
                bind:value={planFormData.type}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              >
                <option value="Broadband"
                  >{$language === "vi"
                    ? "Băng rộng (Broadband)"
                    : "Broadband"}</option
                >
                <option value="Dial-Up"
                  >{$language === "vi"
                    ? "Quay số (Dial-Up)"
                    : "Dial-Up"}</option
                >
                <option value="Landline"
                  >{$language === "vi"
                    ? "Cố định (Landline)"
                    : "Landline"}</option
                >
              </select>
            </div>
          </div>

          <div>
            <label
              class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Thông số tốc độ / Băng thông *"
                : "Speed / Bandwidth Specification *"}
            </label>
            <input
              type="text"
              required
              bind:value={planFormData.speedOrBandwidth}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
            />
          </div>

          <div class="grid grid-cols-3 gap-3">
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Tiền cọc ($)" : "Security Deposit ($)"}
              </label>
              <input
                type="number"
                step="0.01"
                min="0"
                required
                bind:value={planFormData.securityDeposit}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm font-mono"
              />
            </div>
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Cước thuê tháng ($)"
                  : "Monthly Rental ($)"}
              </label>
              <input
                type="number"
                step="0.01"
                min="0"
                required
                bind:value={planFormData.monthlyRental}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm font-mono"
              />
            </div>
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Cước giờ ($)" : "Hourly Charge ($)"}
              </label>
              <input
                type="number"
                step="0.01"
                min="0"
                bind:value={planFormData.hourlyCharge}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm font-mono"
              />
            </div>
          </div>

          <div class="grid grid-cols-3 gap-3">
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Kỳ thanh toán" : "Billing Cycle"}
              </label>
              <select
                bind:value={planFormData.billingCycle}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              >
                <option value="Hourly Pack"
                  >{$language === "vi" ? "Gói theo giờ" : "Hourly Pack"}</option
                >
                <option value="Monthly"
                  >{$language === "vi" ? "Hàng tháng" : "Monthly"}</option
                >
                <option value="Quarterly"
                  >{$language === "vi"
                    ? "Hàng quý (3 tháng)"
                    : "Quarterly"}</option
                >
                <option value="Half-Yearly"
                  >{$language === "vi"
                    ? "Nửa năm (6 tháng)"
                    : "Half-Yearly"}</option
                >
                <option value="Yearly"
                  >{$language === "vi" ? "Hàng năm" : "Yearly"}</option
                >
              </select>
            </div>
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Thời hạn hiệu lực" : "Validity"}
              </label>
              <input
                type="text"
                bind:value={planFormData.validity}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Giới hạn dung lượng" : "Data Limit"}
              </label>
              <input
                type="text"
                bind:value={planFormData.dataLimit}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          {#if planFormData.type === "Landline"}
            <div>
              <label
                class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Biểu phí gọi thoại (Cố định)"
                  : "Call Charges (Landline)"}
              </label>
              <input
                type="text"
                bind:value={planFormData.callRates}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          {/if}

          <div>
            <label
              class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Mô tả gói & Tính năng"
                : "Plan Description & Features"}
            </label>
            <textarea
              rows="2"
              bind:value={planFormData.description}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
            ></textarea>
          </div>

          </div>
          <div
            class="p-6 border-t border-slate-100 dark:border-slate-800 shrink-0 flex items-center justify-between bg-slate-50 dark:bg-slate-950/50"
          >
            {#if editingPlan}
              <button
                type="button"
                onclick={() => {
                  const p = editingPlan;
                  if (!p) return;
                  showConfirm(
                    $language === "vi"
                      ? "Xác nhận xóa"
                      : "Confirm Deletion",
                    $language === "vi"
                      ? `Xóa gói cước "${p.name}"?`
                      : `Delete plan "${p.name}"?`,
                    () => {
                      deletePlan(p.id);
                      isPlanModalOpen = false;
                      toast.success(
                        $language === "vi"
                          ? `Đã xóa gói ${p.name}.`
                          : `Plan ${p.name} removed.`,
                      );
                    },
                  );
                }}
                class="px-3.5 py-2 rounded-lg text-sm font-medium border border-rose-200 dark:border-rose-900/50 text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition flex items-center gap-1.5"
              >
                <Trash2 class="h-4 w-4" />
                <span>{$language === "vi" ? "Xóa gói cước" : "Delete Plan"}</span>
              </button>
            {:else}
              <div></div>
            {/if}
            <div class="flex items-center space-x-3">
              <button
                type="button"
                onclick={() => (isPlanModalOpen = false)}
                class="px-4 py-2 rounded-lg text-sm border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
              >
                {$language === "vi" ? "Hủy" : "Cancel"}
              </button>
              <button
                type="submit"
                class="px-4 py-2 rounded-lg text-sm font-semibold bg-indigo-600 hover:bg-indigo-700 text-white transition shadow"
              >
                {editingPlan
                  ? $language === "vi"
                    ? "Lưu thay đổi"
                    : "Save Changes"
                  : $language === "vi"
                    ? "Xác nhận gói cước"
                    : "Confirm Plan"}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  {/if}
  <!-- RETAIL SHOP MODAL -->
  {#if isShopModalOpen}
    <div
      onclick={(e) => {
        if (e.target === e.currentTarget) isShopModalOpen = false;
      }}
      class="absolute inset-0 !m-0 z-[100] flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4 cursor-pointer"
      role="dialog"
      aria-modal="true"
      tabindex="-1"
    >
      <div
        class="w-full max-w-lg rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-6 shadow-xl space-y-4 cursor-default"
        onclick={(e) => e.stopPropagation()}
      >
        <div
          class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 pb-3"
        >
          <div class="flex items-center space-x-2">
            <Store class="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <h3 class="font-bold text-base text-slate-900 dark:text-white">
              {editingShop
                ? $language === "vi"
                  ? "Chỉnh sửa điểm bán lẻ"
                  : "Edit Retail Shop"
                : $language === "vi"
                  ? "Thêm điểm bán lẻ mới"
                  : "Add New Retail Shop"}
            </h3>
          </div>
          <button
            onclick={() => (isShopModalOpen = false)}
            class="p-1 text-slate-400 hover:text-slate-600"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form onsubmit={handleSaveShop} class="space-y-4 text-xs">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Mã chi nhánh *" : "Shop Code *"}
              </label>
              <input
                type="text"
                required
                bind:value={shopFormData.shopCode}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Mã thành phố (3 số) *"
                  : "City Code (3-digit) *"}
              </label>
              <input
                type="text"
                maxlength="3"
                required
                bind:value={shopFormData.cityCode}
                class="w-full px-3 py-2 font-mono bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Tên điểm giao dịch *"
                  : "Shop / Outlet Name *"}
              </label>
              <input
                type="text"
                required
                bind:value={shopFormData.name}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Thành phố *" : "City / Metro Region *"}
              </label>
              <input
                type="text"
                required
                bind:value={shopFormData.city}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          <div>
            <label
              class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi" ? "Địa chỉ chi tiết *" : "Street Address *"}
            </label>
            <input
              type="text"
              required
              bind:value={shopFormData.address}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
            />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Quản lý chi nhánh *" : "Store Manager *"}
              </label>
              <input
                type="text"
                required
                bind:value={shopFormData.managerName}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Số điện thoại *" : "Phone *"}
              </label>
              <input
                type="tel"
                required
                bind:value={shopFormData.phone}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div class="sm:col-span-2 grid grid-cols-2 gap-3">
              <div>
                <label
                  class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Giờ mở cửa" : "Opening Time"}
                </label>
                <input
                  type="time"
                  value={shopFormData.operatingHours.split(" - ")[0] || "08:00"}
                  onchange={(e) => {
                    const parts = shopFormData.operatingHours.split(" - ");
                    shopFormData.operatingHours = `${e.currentTarget.value} - ${parts[1] || "20:00"}`;
                  }}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
              <div>
                <label
                  class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
                >
                  {$language === "vi" ? "Giờ đóng cửa" : "Closing Time"}
                </label>
                <input
                  type="time"
                  value={shopFormData.operatingHours.split(" - ")[1] || "20:00"}
                  onchange={(e) => {
                    const parts = shopFormData.operatingHours.split(" - ");
                    shopFormData.operatingHours = `${parts[0] || "08:00"} - ${e.currentTarget.value}`;
                  }}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
                />
              </div>
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1 flex items-center gap-1"
              >
                {$language === "vi" ? "Nhân sự" : "Staff Count"}
              </label>
              <input
                type="number"
                bind:value={shopFormData.activeEmployeesCount}
                disabled
                class="w-full px-3 py-2 bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-sm text-slate-400 cursor-not-allowed"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1 flex items-center gap-1"
              >
                {$language === "vi" ? "Thuê bao" : "Subscribers"}
              </label>
              <input
                type="number"
                bind:value={shopFormData.totalSubscribersServed}
                disabled
                class="w-full px-3 py-2 bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-sm text-slate-400 cursor-not-allowed"
              />
            </div>
          </div>

          <div
            class="flex justify-end space-x-3 pt-3 border-t border-slate-100 dark:border-slate-800"
          >
            <button
              type="button"
              onclick={() => (isShopModalOpen = false)}
              class="px-4 py-2 rounded-lg text-sm border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
            >
              {$language === "vi" ? "Hủy" : "Cancel"}
            </button>
            <button
              type="submit"
              class="px-4 py-2 rounded-lg text-sm font-semibold bg-blue-600 hover:bg-blue-700 text-white transition shadow"
            >
              {editingShop
                ? $language === "vi"
                  ? "Lưu thay đổi"
                  : "Save Changes"
                : $language === "vi"
                  ? "Thêm điểm bán lẻ"
                  : "Confirm Shop"}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- INVENTORY / STOCK MODAL -->
  {#if isStockModalOpen}
    <div
      onclick={(e) => {
        if (e.target === e.currentTarget) isStockModalOpen = false;
      }}
      class="absolute inset-0 !m-0 z-[100] flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4 cursor-pointer"
      role="dialog"
      aria-modal="true"
      tabindex="-1"
    >
      <div
        class="w-full max-w-lg rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-6 shadow-xl space-y-4"
      >
        <div
          class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 pb-3"
        >
          <div class="flex items-center space-x-2">
            <Package class="h-5 w-5 text-indigo-600 dark:text-indigo-400" />
            <h3 class="font-bold text-base text-slate-900 dark:text-white">
              {editingStock
                ? $language === "vi"
                  ? "Chỉnh sửa vật tư thiết bị"
                  : "Edit Equipment Item"
                : $language === "vi"
                  ? "Thêm vật tư thiết bị mới"
                  : "Add New Equipment Item"}
            </h3>
          </div>
          <button
            onclick={() => (isStockModalOpen = false)}
            class="p-1 text-slate-400 hover:text-slate-600"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form onsubmit={handleSaveStock} class="space-y-4 text-xs">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Mã vật tư *" : "Item Code *"}
              </label>
              <input
                type="text"
                required
                bind:value={stockFormData.itemCode}
                class="w-full px-3 py-2 font-mono bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? "Danh mục thiết bị *"
                  : "Device Category *"}
              </label>
              <select
                bind:value={stockFormData.category}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              >
                <option value="Modem">Modem</option>
                <option value="Router">Router</option>
                <option value="Fiber ONT">Fiber ONT</option>
                <option value="Splitter">Splitter</option>
                <option value="Patch Cord">Patch Cord</option>
                <option value="VoIP Adapter">VoIP Adapter</option>
              </select>
            </div>
          </div>

          <div>
            <label
              class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Tên thiết bị / Vật tư *"
                : "Equipment Name *"}
            </label>
            <input
              type="text"
              required
              bind:value={stockFormData.name}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
            />
          </div>

          <div class="grid grid-cols-3 gap-3">
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Số lượng tồn" : "Stock Qty"}
              </label>
              <input
                type="number"
                min="0"
                bind:value={stockFormData.stockQuantity}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Mức tối thiểu" : "Reorder Level"}
              </label>
              <input
                type="number"
                min="0"
                bind:value={stockFormData.reorderLevel}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Đơn giá ($)" : "Unit Cost ($)"}
              </label>
              <input
                type="number"
                min="0"
                step="0.01"
                bind:value={stockFormData.unitCost}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          <!-- Restock Missing Items Section -->
          <div
            class="p-3.5 rounded-xl border {stockFormData.stockQuantity <=
            stockFormData.reorderLevel
              ? 'border-amber-300 dark:border-amber-800 bg-amber-50/70 dark:bg-amber-950/30'
              : 'border-slate-200 dark:border-slate-800 bg-slate-50/70 dark:bg-slate-950/50'} space-y-3"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center space-x-2">
                {#if stockFormData.stockQuantity <= stockFormData.reorderLevel}
                  <AlertTriangle
                    class="h-4 w-4 text-amber-600 dark:text-amber-400 shrink-0"
                  />
                  <span
                    class="font-bold text-xs text-amber-800 dark:text-amber-300"
                  >
                    {$language === "vi"
                      ? "Nhập thêm mặt hàng đang thiếu"
                      : "Replenish Low / Missing Stock"}
                  </span>
                {:else}
                  <RefreshCw
                    class="h-4 w-4 text-indigo-600 dark:text-indigo-400 shrink-0"
                  />
                  <span
                    class="font-bold text-xs text-slate-800 dark:text-slate-200"
                  >
                    {$language === "vi"
                      ? "Nhập bổ sung kho hàng"
                      : "Restock / Replenish Stock"}
                  </span>
                {/if}
              </div>

              {#if stockFormData.stockQuantity <= stockFormData.reorderLevel}
                <span
                  class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-200 dark:bg-amber-900/60 text-amber-800 dark:text-amber-300"
                >
                  {$language === "vi"
                    ? `Thiếu ít nhất ${Math.max(1, stockFormData.reorderLevel - stockFormData.stockQuantity)} sp`
                    : `Short by ${Math.max(1, stockFormData.reorderLevel - stockFormData.stockQuantity)}`}
                </span>
              {:else}
                <span
                  class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-medium bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-400"
                >
                  {$language === "vi" ? "Tồn kho an toàn" : "Healthy Stock"}
                </span>
              {/if}
            </div>

            {#if stockFormData.stockQuantity <= stockFormData.reorderLevel}
              <div
                class="text-[11px] text-amber-700 dark:text-amber-300/90 leading-relaxed"
              >
                {$language === "vi"
                  ? `Mặt hàng này đang dưới ngưỡng an toàn (Tồn: ${stockFormData.stockQuantity} / Tối thiểu: ${stockFormData.reorderLevel}). Hãy nhập thêm để đảm bảo cung ứng lắp đặt mạng.`
                  : `Item is below minimum safety threshold (In stock: ${stockFormData.stockQuantity} / Reorder level: ${stockFormData.reorderLevel}). Please restock to prevent outages.`}
              </div>
            {/if}

            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1.5 text-[11px]"
              >
                {$language === "vi"
                  ? "Số lượng nhập thêm vào kho"
                  : "Quantity to Add to Stock"}
              </label>
              <div class="flex items-center gap-2">
                <input
                  type="number"
                  min="1"
                  step="1"
                  bind:value={stockFormData.restockQuantity}
                  class="w-32 px-3 py-1.5 bg-white dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm font-semibold"
                />
                <button
                  type="button"
                  onclick={() => {
                    const qty = Number(stockFormData.restockQuantity) || 0;
                    if (qty > 0) {
                      stockFormData.stockQuantity += qty;
                      toast.success(
                        $language === "vi"
                          ? `Đã cộng thêm +${qty} sản phẩm vào tồn kho!`
                          : `Added +${qty} to stock quantity!`,
                      );
                    }
                  }}
                  class="px-3 py-1.5 rounded-lg text-xs font-semibold bg-indigo-600 hover:bg-indigo-700 text-white transition shadow flex items-center space-x-1"
                >
                  <Plus class="h-3.5 w-3.5" />
                  <span
                    >{$language === "vi"
                      ? "Cộng vào tồn kho"
                      : "Add to Stock"}</span
                  >
                </button>
              </div>
            </div>

            <!-- Quick fill buttons -->
            <div class="space-y-1">
              <span
                class="block text-[10px] text-slate-500 dark:text-slate-400 font-medium"
              >
                {$language === "vi"
                  ? "Chọn nhanh số lượng nhập:"
                  : "Quick restock presets:"}
              </span>
              <div class="flex flex-wrap gap-1.5">
                {#if stockFormData.stockQuantity <= stockFormData.reorderLevel}
                  <button
                    type="button"
                    onclick={() => {
                      const needed = Math.max(
                        1,
                        stockFormData.reorderLevel -
                          stockFormData.stockQuantity,
                      );
                      stockFormData.restockQuantity = needed;
                      stockFormData.stockQuantity += needed;
                      toast.success(
                        $language === "vi"
                          ? `Đã bù đủ ${needed} sản phẩm đạt mức an toàn!`
                          : `Added ${needed} to meet safe threshold!`,
                      );
                    }}
                    class="px-2 py-1 rounded bg-amber-200 dark:bg-amber-900/80 hover:bg-amber-300 dark:hover:bg-amber-800 text-amber-900 dark:text-amber-200 text-[11px] font-bold transition flex items-center space-x-1"
                  >
                    <span
                      >⚡ {$language === "vi"
                        ? `Bù đủ định mức (+${Math.max(1, stockFormData.reorderLevel - stockFormData.stockQuantity)})`
                        : `Replenish Deficit (+${Math.max(1, stockFormData.reorderLevel - stockFormData.stockQuantity)})`}</span
                    >
                  </button>
                {/if}
                <button
                  type="button"
                  onclick={() => {
                    stockFormData.restockQuantity = 10;
                    stockFormData.stockQuantity += 10;
                  }}
                  class="px-2 py-1 rounded bg-slate-200/80 dark:bg-slate-800 hover:bg-slate-300 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-[11px] font-medium transition"
                >
                  +10
                </button>
                <button
                  type="button"
                  onclick={() => {
                    stockFormData.restockQuantity = 25;
                    stockFormData.stockQuantity += 25;
                  }}
                  class="px-2 py-1 rounded bg-slate-200/80 dark:bg-slate-800 hover:bg-slate-300 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-[11px] font-medium transition"
                >
                  +25
                </button>
                <button
                  type="button"
                  onclick={() => {
                    stockFormData.restockQuantity = 50;
                    stockFormData.stockQuantity += 50;
                  }}
                  class="px-2 py-1 rounded bg-slate-200/80 dark:bg-slate-800 hover:bg-slate-300 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-[11px] font-medium transition"
                >
                  +50
                </button>
                <button
                  type="button"
                  onclick={() => {
                    stockFormData.restockQuantity = 100;
                    stockFormData.stockQuantity += 100;
                  }}
                  class="px-2 py-1 rounded bg-slate-200/80 dark:bg-slate-800 hover:bg-slate-300 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-[11px] font-medium transition"
                >
                  +100
                </button>
              </div>
            </div>

            <!-- Preview indicator -->
            <div
              class="pt-1 text-[11px] font-medium text-slate-600 dark:text-slate-300 flex items-center justify-between border-t border-slate-200/60 dark:border-slate-800/60"
            >
              <span
                >{$language === "vi"
                  ? "Tổng số lượng sau khi nhập:"
                  : "Total stock after restock:"}</span
              >
              <span
                class="font-bold {stockFormData.stockQuantity >
                stockFormData.reorderLevel
                  ? 'text-emerald-600 dark:text-emerald-400'
                  : 'text-amber-600 dark:text-amber-400'}"
              >
                {stockFormData.stockQuantity}
                {$language === "vi" ? "thiết bị" : "units"}
                {#if stockFormData.stockQuantity > stockFormData.reorderLevel}
                  (Đủ an toàn ✅)
                {:else}
                  (Vẫn dưới mức tối thiểu ⚠️)
                {/if}
              </span>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Vị trí kho" : "Depot Location"}
              </label>
              <input
                type="text"
                bind:value={stockFormData.location}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
            <div>
              <label
                class="block font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi" ? "Nhà cung ứng" : "Supplier"}
              </label>
              <input
                type="text"
                bind:value={stockFormData.supplier}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm"
              />
            </div>
          </div>

          <div
            class="flex justify-end space-x-3 pt-3 border-t border-slate-100 dark:border-slate-800"
          >
            <button
              type="button"
              onclick={() => (isStockModalOpen = false)}
              class="px-4 py-2 rounded-lg text-sm border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
            >
              {$language === "vi" ? "Hủy" : "Cancel"}
            </button>
            <button
              type="submit"
              class="px-4 py-2 rounded-lg text-sm font-semibold bg-indigo-600 hover:bg-indigo-700 text-white transition shadow"
            >
              {editingStock
                ? $language === "vi"
                  ? "Lưu thay đổi"
                  : "Save Changes"
                : $language === "vi"
                  ? "Thêm vật tư"
                  : "Confirm Item"}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  {#if confirmDialog.isOpen}
    <div
      onclick={(e) => {
        if (e.target === e.currentTarget) confirmDialog.isOpen = false;
      }}
      class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm cursor-pointer"
      role="dialog"
      aria-modal="true"
      tabindex="-1"
    >
      <div
        class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl w-full max-w-sm border border-slate-200 dark:border-slate-800 p-6 animate-in fade-in zoom-in duration-200 cursor-default"
        onclick={(e) => e.stopPropagation()}
      >
        <div
          class="flex items-center space-x-3 text-rose-600 dark:text-rose-400 mb-4"
        >
          <div class="p-2 bg-rose-100 dark:bg-rose-900/30 rounded-full">
            <AlertTriangle class="h-6 w-6" />
          </div>
          <h3 class="text-lg font-bold">{confirmDialog.title}</h3>
        </div>
        <p class="text-slate-600 dark:text-slate-300 text-sm mb-6">
          {confirmDialog.message}
        </p>
        <div class="flex justify-end space-x-3">
          <button
            onclick={() => (confirmDialog.isOpen = false)}
            class="px-4 py-2 rounded-lg text-sm border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
          >
            {$language === "vi" ? "Hủy" : "Cancel"}
          </button>
          <button
            onclick={confirmDialog.onConfirm}
            class="px-4 py-2 rounded-lg text-sm font-semibold bg-rose-600 hover:bg-rose-700 text-white transition shadow"
          >
            {$language === "vi" ? "Xác nhận xóa" : "Confirm Delete"}
          </button>
        </div>
      </div>
    </div>
  {/if}
</DashboardLayout>
