<script lang="ts">
  // Subscriber portal: everything is keyed off the Account ID used to sign in.
  import { authStore } from "../context/AuthContext";
  import { nexusStore, accountIdKey } from "../context/NexusContext";
  import { languageStore } from "../context/LanguageContext";
  import { notificationStore } from "../context/NotificationContext";
  import DashboardLayout from "../components/layout/DashboardLayout.svelte";
  import SettingsView from "../components/common/SettingsView.svelte";
  import ProfileView from "../components/common/ProfileView.svelte";
  import type { NavItem } from "../components/layout/DashboardLayout.svelte";
  import {
    User,
    Activity,
    Settings,
    CreditCard,
    LayoutDashboard,
    Store,
    MapPin,
    KeyRound,
    MessageSquare,
    Star,
    Wallet,
    Bell,
    Clock,
    Calendar,
    CheckCircle2,
    AlertCircle,
    ShieldCheck,
    QrCode,
    Smartphone,
    Banknote,
    ArrowRight,
    X,
    ChevronRight,
    Copy,
    Download,
    Sparkles,
    Building,
    RefreshCw,
    Filter,
    ShoppingBag,
    Wifi,
    Radio,
    Phone,
    Plus,
    Check,
    Send,
    Search,
    Mail,
    Navigation,
  } from "lucide-svelte";
  import type {
    FeedbackCategory,
    Bill,
    PaymentRecord,
    Plan,
    Order,
    ConnectionType,
  } from "../types/nexus";
  import { toast } from "svelte-sonner";
  import { queryParam, activeTabOverride } from "../lib/router";
  import {
    isValidCccd,
    isValidEmail,
    isValidPhone,
    isValidPassport,
    isValidDriverLicense,
  } from "../lib/validation";
  import {
    fetchChatMessagesApi,
    startChatSessionApi,
    sendChatMessageApi,
    fetchChatSessionsApi,
    markChatSessionReadApi,
    type ChatMessageDto,
  } from "../lib/api";
  import ConnectionTestModal from "../components/modals/ConnectionTestModal.svelte";

  let isTestRecordsModalOpen = $state(false);

  const { currentUser } = authStore;
  const {
    connections,
    orders,
    bills,
    retailShops,
    feedbacks,
    addFeedback,
    recordPayment,
    plans,
    placeOrder,
  } = nexusStore;
  const { t, language } = languageStore;

  let activeTab = $state("overview");

  // Reactively respond to tab overrides from router / notifications
  $effect(() => {
    const override = $activeTabOverride;
    const validTabs = [
      "overview",
      "new-service",
      "billing",
      "pay-balance",
      "feedback",
      "settings",
      "profile",
      "livechat",
    ];
    if (override && override.path === "/user") {
      if (validTabs.includes(override.tab)) {
        activeTab = override.tab;
      }
    } else {
      const qTab = queryParam("tab");
      if (qTab && validTabs.includes(qTab)) {
        activeTab = qTab;
      }
    }
  });

  // New service order state
  let newOrderCategory = $state<"all" | ConnectionType>("all");
  let orderingPlan = $state<Plan | null>(null);
  let newOrderCustomerName = $state("");
  let newOrderCustomerEmail = $state("");
  let newOrderProvince = $state("");
  let newOrderDistrict = $state("");
  let newOrderWard = $state("");
  let newOrderSpecificAddress = $state("");
  const newOrderAddress = $derived(
    [newOrderSpecificAddress, newOrderWard, newOrderDistrict, newOrderProvince]
      .filter(Boolean)
      .join(", "),
  );

  let showMapModal = $state(false);
  let mapSearchQuery = $state("");

  let provincesList = $state<any[]>([]);
  let districtsList = $state<any[]>([]);
  let wardsList = $state<any[]>([]);

  $effect(() => {
    fetch("https://esgoo.net/api-tinhthanh/1/0.htm")
      .then((r) => r.json())
      .then((d) => {
        if (d.error === 0) provincesList = d.data;
      })
      .catch((e) => console.error(e));
  });

  $effect(() => {
    if (newOrderProvince) {
      const p = provincesList.find(
        (x) => x.name === newOrderProvince || x.full_name === newOrderProvince,
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
      newOrderDistrict = "";
      newOrderWard = "";
    }
  });

  $effect(() => {
    if (newOrderDistrict) {
      const d = districtsList.find(
        (x) => x.name === newOrderDistrict || x.full_name === newOrderDistrict,
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
      newOrderWard = "";
    }
  });
  let newOrderPhone = $state("");
  let newOrderShopCode = $state("SH-01");
  let newOrderIdProofType = $state<Order["idProofType"]>("National ID Card");
  let newOrderIdProofNumber = $state("");
  let newOrderBulkCount = $state(1);
  let newOrderHasLandline = $state(false);
  let newOrderNotifyGmail = $state(true);
  let newOrderNotifyAccount = $state(true);
  let newOrderGmailSent = $state(false);
  let newOrderAccountNotified = $state(false);
  let newOrderPlacedReceipt = $state<Order | null>(
    (() => {
      try {
        const saved = localStorage.getItem("nexus_last_user_order");
        return saved ? JSON.parse(saved) : null;
      } catch {
        return null;
      }
    })(),
  );

  function sendGmailNotification(orderReceipt: Order) {
    const targetEmail =
      newOrderCustomerEmail.trim() ||
      orderReceipt.customerEmail ||
      $currentUser?.email ||
      connection?.customerEmail ||
      "customer@gmail.com";

    newOrderGmailSent = true;
    toast.success(
      $language === "vi"
        ? `Đã gửi thông báo xác nhận đơn hàng #${orderReceipt.id} tới Gmail: ${targetEmail}`
        : `Order #${orderReceipt.id} confirmation sent to Gmail: ${targetEmail}`,
      {
        description:
          $language === "vi"
            ? `Chi tiết gói cước "${orderReceipt.planName}" và hướng dẫn theo dõi tiến độ đã được gửi vào hòm thư.`
            : `Details for plan "${orderReceipt.planName}" sent to your inbox.`,
        icon: Mail as any,
      },
    );
  }

  async function sendAccountNotification(orderReceipt: Order) {
    newOrderAccountNotified = true;
    // Thông báo "đã tiếp nhận đơn" do backend tạo và lưu vào CSDL ngay khi đơn được ghi
    // (xem NotificationEvents.OrderPlacedAsync), rồi đẩy xuống qua SignalR. Ở đây chỉ tải
    // lại danh sách từ CSDL — trước đây hàm này tự thêm một thông báo giả chỉ nằm trên
    // trình duyệt, tải lại trang là mất và trong CSDL không hề có.
    await notificationStore.refresh();

    toast.success(
      $language === "vi"
        ? `Đã gửi thông báo đơn hàng #${orderReceipt.id} vào tài khoản người dùng!`
        : `Order #${orderReceipt.id} notification added to your account!`,
      {
        description:
          $language === "vi"
            ? "Bạn có thể kiểm tra danh sách thông báo trên chuông thông báo Header hoặc trên Dashboard."
            : "You can check it via the notification bell on the top header or your Dashboard.",
        icon: Bell as any,
      },
    );
  }

  function dismissOrderNotification() {
    newOrderPlacedReceipt = null;
    try {
      localStorage.removeItem("nexus_last_user_order");
    } catch {}
  }

  // New plan Search, Filter, Pagination for Subscriber (User role, giống Admin)
  let newPlanSearch = $state("");
  let newPlanCurrentPage = $state(1);
  const newPlanItemsPerPage = 6;

  const filteredNewPlans = $derived(
    $plans.filter((p) => {
      const matchStatus = p.status === "Active";
      const matchCat =
        newOrderCategory === "all" || p.type === newOrderCategory;
      const matchSearch =
        !newPlanSearch.trim() ||
        p.name.toLowerCase().includes(newPlanSearch.trim().toLowerCase()) ||
        (p.description &&
          p.description
            .toLowerCase()
            .includes(newPlanSearch.trim().toLowerCase())) ||
        (p.speedOrBandwidth &&
          p.speedOrBandwidth
            .toLowerCase()
            .includes(newPlanSearch.trim().toLowerCase()));
      return matchStatus && matchCat && matchSearch;
    }),
  );

  const totalNewPlanPages = $derived(
    Math.ceil(filteredNewPlans.length / newPlanItemsPerPage) || 1,
  );

  const paginatedNewPlans = $derived(
    filteredNewPlans.slice(
      (newPlanCurrentPage - 1) * newPlanItemsPerPage,
      newPlanCurrentPage * newPlanItemsPerPage,
    ),
  );

  $effect(() => {
    newOrderCategory;
    newPlanSearch;
    newPlanCurrentPage = 1;
  });

  function handleOpenOrderModal(p: Plan) {
    orderingPlan = p;
    newOrderCustomerName = $currentUser?.name || connection?.customerName || "";
    newOrderCustomerEmail =
      $currentUser?.email || connection?.customerEmail || "";
    newOrderProvince = "";
    newOrderDistrict = "";
    newOrderWard = "";
    newOrderSpecificAddress = connection?.installationAddress || "";
    newOrderPhone = connection?.customerPhone || "";
    newOrderIdProofNumber = order?.idProofNumber || "";
    newOrderShopCode = shop?.shopCode || $retailShops[0]?.shopCode || "SH-01";
    newOrderBulkCount = 1;
    newOrderHasLandline = false;
  }

  // Chặn gửi đơn 2 lần (bấm đúp / Enter + click) khi request trước chưa xong.
  let isPlacingNewOrder = $state(false);

  async function handleConfirmNewServiceOrder(e: SubmitEvent) {
    e.preventDefault();
    if (!orderingPlan || isPlacingNewOrder) return;
    if (
      !newOrderCustomerName ||
      !newOrderPhone ||
      !newOrderAddress ||
      !newOrderIdProofNumber ||
      !newOrderShopCode
    ) {
      toast.error(
        $language === "vi"
          ? "Vui lòng điền các trường bắt buộc"
          : "Please fill required fields",
      );
      return;
    }

    if (newOrderCustomerEmail && !isValidEmail(newOrderCustomerEmail)) {
      toast.error(
        $language === "vi"
          ? "Email phải có dạng ten@mien.com."
          : "Email must have the form name@domain.com.",
      );
      return;
    }
    if (!isValidPhone(newOrderPhone)) {
      toast.error(
        $language === "vi"
          ? "Số điện thoại phải gồm đúng 10 chữ số."
          : "Phone number must contain exactly 10 digits.",
      );
      return;
    }
    if (
      newOrderIdProofType === "National ID Card" &&
      !isValidCccd(newOrderIdProofNumber)
    ) {
      toast.error(
        $language === "vi"
          ? "Số CCCD phải gồm đúng 12 chữ số."
          : "National ID number must contain exactly 12 digits.",
      );
      return;
    }
    if (
      newOrderIdProofType === "Passport" &&
      !isValidPassport(newOrderIdProofNumber)
    ) {
      toast.error(
        $language === "vi"
          ? "Số Passport không hợp lệ (7-9 ký tự chữ và số)."
          : "Invalid Passport number (7-9 alphanumeric characters).",
      );
      return;
    }
    if (
      newOrderIdProofType === "Driver's License" &&
      !isValidDriverLicense(newOrderIdProofNumber)
    ) {
      toast.error(
        $language === "vi"
          ? "Số Bằng lái xe phải gồm đúng 12 chữ số."
          : "Driver's License must contain exactly 12 digits.",
      );
      return;
    }

    if (
      orderingPlan.type === "Dial-Up" &&
      (!newOrderHasLandline || !accountId.startsWith("T"))
    ) {
      toast.error(
        $language === "vi"
          ? "Bạn bắt buộc phải có tài khoản Điện thoại Cố định (Landline) để đăng ký mạng Quay số (Dial-Up)!"
          : "A Landline account is compulsory to register for Dial-Up internet!",
      );
      return;
    }

    // Không còn dùng email giả "customer@nexus.telecom" làm dự phòng: backend nhận diện
    // khách theo email, nên mọi đơn thiếu email từng bị gom chung vào MỘT hồ sơ khách
    // và bên Kỹ thuật hiện tên/SĐT của người khác.
    const orderEmail =
      newOrderCustomerEmail.trim() ||
      $currentUser?.email ||
      connection?.customerEmail ||
      "";

    let created: Order;
    isPlacingNewOrder = true;
    try {
      created = await placeOrder({
        customerName: newOrderCustomerName.trim(),
        customerPhone: newOrderPhone.trim(),
        customerEmail: orderEmail,
        installationAddress: newOrderAddress.trim(),
        idProofType: newOrderIdProofType,
        idProofNumber: newOrderIdProofNumber.trim(),
        connectionType: orderingPlan.type,
        planId: orderingPlan.id,
        planName: orderingPlan.name,
        retailOutletCode: newOrderShopCode,
        retailEmployeeName: "Customer Self-Service",
        bulkConnectionsCount: Math.max(1, newOrderBulkCount || 1),
        ...(orderingPlan.type === "Dial-Up" &&
        newOrderHasLandline &&
        accountId.startsWith("T")
          ? { existingLandlineAccountId: accountId }
          : {}),
      });
    } catch (err) {
      // Đơn KHÔNG vào được CSDL thì phải nói thật, không hiện biên nhận giả.
      toast.error(
        $language === "vi"
          ? "Chưa gửi được đơn hàng"
          : "Order was not submitted",
        {
          description:
            err instanceof Error && err.message !== "network"
              ? err.message
              : $language === "vi"
                ? "Không kết nối được máy chủ. Vui lòng thử lại."
                : "Could not reach the server. Please try again.",
        },
      );
      return;
    } finally {
      isPlacingNewOrder = false;
    }

    newOrderPlacedReceipt = created;
    try {
      localStorage.setItem("nexus_last_user_order", JSON.stringify(created));
    } catch {}
    orderingPlan = null;

    if (newOrderNotifyGmail) {
      sendGmailNotification(created);
    }
    if (newOrderNotifyAccount) {
      void sendAccountNotification(created);
    }

    toast.success(
      $language === "vi"
        ? `Đã gửi đơn hàng #${created.id} thành công! Hồ sơ đang chờ nhân viên chi nhánh xét duyệt.`
        : `Order #${created.id} submitted! Awaiting retail branch verification.`,
    );
  }

  // Feedback form (functional requirement #2)
  let fbRating = $state(5);
  let fbCategory = $state<FeedbackCategory>("Service Quality");
  let fbMessage = $state("");

  // Payment Cycle & Modal State
  type BillingCycle = "monthly" | "quarterly" | "annually";
  let selectedCycle = $state<BillingCycle>("monthly");
  let autoReminderEnabled = $state(true);

  let activePayModalBill = $state<Bill | null>(null);
  let chosenPaymentMethod =
    $state<PaymentRecord["paymentMode"]>("Bank Transfer/NEFT");
  let isProcessingPayment = $state(false);

  // Payment form mock inputs
  let cardHolderName = $state("NGUYEN VAN A");
  let cardNumber = $state("9704 1988 2026 8899");
  let cardExpiry = $state("12/28");
  let cardCvv = $state("888");
  let selectedWallet = $state<"vnpay" | "momo" | "zalopay">("vnpay");

  // --- CHAT STATE ---
  let chatSessionId = $state<string | null>(null);
  let chatMessages = $state<ChatMessageDto[]>([]);
  let chatDraft = $state("");
  let chatThreadRef = $state<HTMLDivElement | null>(null);
  let chatInterval: ReturnType<typeof setInterval>;
  let isChatLoading = $state(false);
  let isChatSending = $state(false);

  async function loadUserChat() {
    if (!$currentUser?.accountId) return;
    isChatLoading = true;
    try {
      // Find existing session for this customer
      const sessions = await fetchChatSessionsApi($currentUser.name);
      let session = sessions.find((s) => s.customerName === $currentUser.name);
      if (!session) {
        session = await startChatSessionApi($currentUser.name || "Customer");
      }
      chatSessionId = session.sessionId;
      const msgs = await fetchChatMessagesApi(chatSessionId);
      chatMessages = msgs;
      await markChatSessionReadApi(chatSessionId); // Mark as read when opened
    } catch (e) {
      console.warn("Error loading user chat", e);
    } finally {
      isChatLoading = false;
    }
  }

  async function refreshUserChat() {
    if (!chatSessionId) return;
    try {
      const msgs = await fetchChatMessagesApi(chatSessionId);
      if (msgs.length > chatMessages.length) {
        chatMessages = msgs;
        setTimeout(() => {
          if (chatThreadRef)
            chatThreadRef.scrollTop = chatThreadRef.scrollHeight;
        }, 50);
        await markChatSessionReadApi(chatSessionId);
      }
    } catch (e) {
      console.warn("Error refreshing user chat", e);
    }
  }

  $effect(() => {
    if (activeTab === "livechat") {
      loadUserChat().then(() => {
        setTimeout(() => {
          if (chatThreadRef)
            chatThreadRef.scrollTop = chatThreadRef.scrollHeight;
        }, 50);
      });
      chatInterval = setInterval(refreshUserChat, 3000);
    } else {
      if (chatInterval) clearInterval(chatInterval);
    }
    return () => {
      if (chatInterval) clearInterval(chatInterval);
    };
  });

  async function sendUserMessage() {
    if (!chatDraft.trim() || !chatSessionId) return;
    isChatSending = true;
    try {
      await sendChatMessageApi(
        chatSessionId,
        $currentUser?.name || "Customer",
        chatDraft.trim(),
      );
      chatDraft = "";
      await refreshUserChat();
    } catch (e) {
      toast.error(
        $language === "vi" ? "Lỗi khi gửi tin nhắn" : "Failed to send message",
      );
    } finally {
      isChatSending = false;
    }
  }

  function handleChatKeyDown(e: KeyboardEvent) {
    if (e.key === "Enter") {
      sendUserMessage();
    }
  }

  // Helper for billing cycle calculations
  function getCycleCalculation(dueAmount: number, cycle: BillingCycle) {
    if (cycle === "quarterly") {
      const base = Number((dueAmount * 3).toFixed(2));
      const discount = Number((base * 0.05).toFixed(2));
      const total = Number((base - discount).toFixed(2));
      return {
        cycle,
        multiplier: 3,
        monthsLabelVi: "3 Tháng (1 Quý)",
        monthsLabelEn: "3 Months (1 Quarter)",
        base,
        discount,
        discountPercent: 5,
        total,
        badgeVi: "Tiết kiệm 5%",
        badgeEn: "Save 5%",
      };
    }
    if (cycle === "annually") {
      const base = Number((dueAmount * 12).toFixed(2));
      const discount = Number((base * 0.1).toFixed(2));
      const total = Number((base - discount).toFixed(2));
      return {
        cycle,
        multiplier: 12,
        monthsLabelVi: "12 Tháng (1 Năm)",
        monthsLabelEn: "12 Months (1 Year)",
        base,
        discount,
        discountPercent: 10,
        total,
        badgeVi: "Ưu đãi lớn -10%",
        badgeEn: "Best Value -10%",
      };
    }
    return {
      cycle,
      multiplier: 1,
      monthsLabelVi: "1 Tháng",
      monthsLabelEn: "1 Month",
      base: dueAmount,
      discount: 0,
      discountPercent: 0,
      total: dueAmount,
      badgeVi: "Chu kỳ chuẩn",
      badgeEn: "Standard",
    };
  }

  const modalCycleInfo = $derived(
    activePayModalBill
      ? getCycleCalculation(activePayModalBill.dueAmount, selectedCycle)
      : null,
  );

  function copyToClipboard(text: string, label: string) {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(text);
      toast.success(
        $language === "vi" ? `Đã sao chép ${label}` : `Copied ${label}`,
      );
    }
  }

  async function handleConfirmPayment() {
    if (!activePayModalBill || !modalCycleInfo) return;
    isProcessingPayment = true;

    const refCode =
      chosenPaymentMethod === "Bank Transfer/NEFT"
        ? `VQR-${Date.now().toString().slice(-6)}`
        : chosenPaymentMethod === "Credit/Debit Card"
          ? `CARD-${Date.now().toString().slice(-6)}`
          : chosenPaymentMethod === "UPI/Digital Wallet"
            ? `${selectedWallet.toUpperCase()}-${Date.now().toString().slice(-6)}`
            : `CASH-${Date.now().toString().slice(-6)}`;

    // Simulate realistic UX confirmation
    await new Promise((resolve) => setTimeout(resolve, 500));

    recordPayment(
      activePayModalBill.invoiceNumber,
      modalCycleInfo.total,
      chosenPaymentMethod,
      refCode,
      $language === "vi"
        ? "User Portal (Khách hàng)"
        : "User Portal (Customer)",
    );

    isProcessingPayment = false;
    toast.success(
      $language === "vi"
        ? `Thanh toán thành công $${modalCycleInfo.total.toFixed(2)} cho hóa đơn ${activePayModalBill.invoiceNumber} qua ${chosenPaymentMethod}!`
        : `Payment of $${modalCycleInfo.total.toFixed(2)} successful for invoice ${activePayModalBill.invoiceNumber} via ${chosenPaymentMethod}!`,
    );
    activePayModalBill = null;
  }

  const accountId = $derived($currentUser?.accountId ?? "");
  // So khớp theo khoá không gạch nối: mã cấp mới lưu trong CSDL dạng "T064000000000010",
  // còn phiên đăng nhập giữ dạng hiển thị "T064-000000000010". So bằng === thì khách mới
  // đăng nhập xong không thấy đường truyền, đơn hàng hay hoá đơn nào của mình.
  const accountKey = $derived(accountIdKey(accountId));
  const sameAccount = (value?: string | null) =>
    !!value && !!accountKey && accountIdKey(value) === accountKey;
  const connection = $derived(
    $connections.find((c) => sameAccount(c.accountId)) ?? null,
  );
  const order = $derived(
    $orders.find((o) => sameAccount(o.assignedAccountId)) ?? null,
  );
  const shop = $derived(
    $retailShops.find((s) => s.shopCode === order?.retailOutletCode) ?? null,
  );
  const myBills = $derived($bills.filter((b) => sameAccount(b.accountId)));
  const latestBill = $derived(myBills[0] ?? null);

  // Unpaid & Billing tab filter state
  type UserBillFilter = "all" | "unpaid" | "paid";
  let billingTabFilter = $state<UserBillFilter>("all");

  const myUnpaidBills = $derived(myBills.filter((b) => b.status !== "Paid"));
  const myPaidBills = $derived(myBills.filter((b) => b.status === "Paid"));
  const nextDueBill = $derived(myUnpaidBills[0] ?? null);
  const totalDueAmount = $derived(
    Number(myUnpaidBills.reduce((acc, b) => acc + b.dueAmount, 0).toFixed(2)),
  );

  const filteredMyBills = $derived(
    myBills.filter((b) => {
      if (billingTabFilter === "unpaid") return b.status !== "Paid";
      if (billingTabFilter === "paid") return b.status === "Paid";
      return true;
    }),
  );

  const myFeedbacks = $derived(
    $feedbacks.filter(
      (f) => sameAccount(f.accountId) || (order && f.orderId === order.id),
    ),
  );

  const submitFeedback = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!fbMessage.trim()) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập nội dung phản hồi."
          : "Please enter your feedback.",
      );
      return;
    }
    await addFeedback({
      accountId: accountId || undefined,
      orderId: order?.id,
      customerName: $currentUser?.name || "Customer",
      rating: fbRating,
      category: fbCategory,
      message: fbMessage.trim(),
    });
    fbMessage = "";
    toast.success(
      $language === "vi"
        ? "Đã gửi phản hồi. Cảm ơn bạn!"
        : "Feedback submitted. Thank you!",
    );
  };

  // The connection is live once technical provisions it; until then the order
  // status is what the customer tracks.
  const serviceStatus = $derived.by(() => {
    if (connection) {
      const map: Record<string, { vi: string; en: string; tone: string }> = {
        Active: {
          vi: "● Đang hoạt động",
          en: "● Online & Provisioned",
          tone: "text-emerald-500",
        },
        "Temporarily Inactive": {
          vi: "● Tạm ngưng dịch vụ",
          en: "● Temporarily Suspended",
          tone: "text-amber-500",
        },
        "Permanently Inactive": {
          vi: "● Đã ngừng vĩnh viễn",
          en: "● Permanently Closed",
          tone: "text-rose-500",
        },
      };
      return map[connection.status] ?? map["Active"];
    }
    const map: Record<string, { vi: string; en: string; tone: string }> = {
      PendingRetail: {
        vi: "● Chi nhánh đang kiểm tra hồ sơ của bạn",
        en: "● Your branch is reviewing your paperwork",
        tone: "text-amber-500",
      },
      "Not Approved": {
        vi: "● Hồ sơ cần bổ sung — vui lòng liên hệ chi nhánh",
        en: "● Paperwork returned — please contact your branch",
        tone: "text-orange-500",
      },
      Pending: {
        vi: "● Hồ sơ đã duyệt, kỹ thuật đang khảo sát khả thi",
        en: "● Approved — engineers surveying feasibility",
        tone: "text-violet-500",
      },
      Feasible: {
        vi: "● Khả thi, chờ lắp đặt",
        en: "● Feasible, awaiting installation",
        tone: "text-sky-500",
      },
      "Not Feasible": {
        vi: "● Chưa thể triển khai tại địa chỉ này",
        en: "● Not feasible at this address",
        tone: "text-rose-500",
      },
      "Connection Provided": {
        vi: "● Đã cấp kết nối",
        en: "● Connection provided",
        tone: "text-emerald-500",
      },
    };
    return map[order?.status ?? "Pending"] ?? map["Pending"];
  });

  const navItems: NavItem[] = $derived([
    { id: "overview", label: $t.userNav.overview, icon: LayoutDashboard },
    {
      id: "new-service",
      label: $language === "vi" ? "Đăng ký gói mới" : "Order New Service",
      icon: ShoppingBag,
    },
    { id: "billing", label: $t.userNav.billing, icon: CreditCard },
    {
      id: "pay-balance",
      label: $language === "vi" ? "Thanh toán" : "Pay Balance",
      icon: Wallet,
    },
    {
      id: "livechat",
      label: $language === "vi" ? "Live Chat" : "Live Chat",
      icon: MessageSquare,
      badge: chatMessages.length > 0 && !chatThreadRef ? "1" : undefined,
    },
    {
      id: "feedback",
      label: $language === "vi" ? "Phản hồi" : "Feedback",
      icon: Send,
      badge: myFeedbacks.length || undefined,
    },
    { id: "settings", label: $t.userNav.settings, icon: Settings },
  ]);
</script>

<DashboardLayout
  {activeTab}
  onTabChange={(id) => {
    activeTab = id;
    activePayModalBill = null;
  }}
  {navItems}
  pageTitle={activeTab === "overview"
    ? $t.userNav.overview
    : navItems.find((n) => n.id === activeTab)?.label}
  roleBadgeTitle={$t.roles.user}
>
  {#if activeTab === "overview"}
    <div class="space-y-6">
      <!-- Notification Banner on Dashboard -->
      {#if newOrderPlacedReceipt}
        <div
          class="p-4 sm:p-5 rounded-2xl bg-gradient-to-r from-sky-500/10 via-indigo-500/10 to-emerald-500/10 border border-sky-400/30 flex flex-col md:flex-row md:items-center justify-between gap-4 shadow-xs"
        >
          <div class="flex items-start space-x-3.5">
            <div
              class="h-11 w-11 rounded-2xl bg-sky-500/20 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0 shadow-xs"
            >
              <Bell class="h-6 w-6" />
            </div>
            <div>
              <div class="flex items-center space-x-2">
                <span
                  class="text-xs font-bold uppercase tracking-wider text-sky-700 dark:text-sky-300"
                >
                  {$language === "vi"
                    ? "Thông báo đăng ký gói cước mới"
                    : "New Service Subscription Alert"}
                </span>
                <span
                  class="text-[10px] px-2 py-0.5 rounded-full bg-sky-100 dark:bg-sky-950 text-sky-700 dark:text-sky-300 font-bold"
                >
                  {$language === "vi" ? "Đã ghi nhận" : "Recorded"}
                </span>
              </div>
              <div
                class="text-base font-extrabold text-[#0F1D2B] dark:text-white mt-0.5"
              >
                {$language === "vi" ? "Đơn hàng:" : "Order:"}
                <span class="font-mono text-sky-600 dark:text-sky-400"
                  >#{newOrderPlacedReceipt.id}</span
                >
                <span class="text-xs font-medium text-slate-500 ml-2"
                  >({newOrderPlacedReceipt.planName})</span
                >
              </div>
              <p
                class="text-xs text-slate-600 dark:text-slate-300 mt-1 max-w-xl"
              >
                {$language === "vi"
                  ? "Hệ thống đã tiếp nhận yêu cầu. Kỹ thuật viên chi nhánh sẽ sớm liên hệ khảo sát hạ tầng."
                  : "Request received. Branch technical team will survey line availability soon."}
              </p>
            </div>
          </div>

          <div class="flex flex-wrap items-center gap-2 shrink-0">
            <button
              type="button"
              onclick={() => sendGmailNotification(newOrderPlacedReceipt!)}
              class="px-3.5 py-2 rounded-xl text-xs font-bold bg-white dark:bg-[#152434] text-sky-700 dark:text-sky-300 border border-sky-300 dark:border-sky-800 hover:bg-sky-50 dark:hover:bg-sky-950/50 transition flex items-center space-x-1.5 shadow-xs cursor-pointer"
            >
              <Mail class="h-4 w-4 text-sky-600" />
              <span
                >{$language === "vi"
                  ? "Thông báo đến Gmail"
                  : "Notify Gmail"}</span
              >
            </button>

            <button
              type="button"
              onclick={() => sendAccountNotification(newOrderPlacedReceipt!)}
              class="px-3.5 py-2 rounded-xl text-xs font-bold bg-white dark:bg-[#152434] text-indigo-700 dark:text-indigo-300 border border-indigo-300 dark:border-indigo-800 hover:bg-indigo-50 dark:hover:bg-indigo-950/50 transition flex items-center space-x-1.5 shadow-xs cursor-pointer"
            >
              <Bell class="h-4 w-4 text-indigo-600" />
              <span
                >{$language === "vi"
                  ? "Thông báo tới tài khoản"
                  : "Notify Account"}</span
              >
            </button>

            <button
              type="button"
              onclick={() => (activeTab = "new-service")}
              class="px-3.5 py-2 rounded-xl text-xs font-bold bg-sky-600 hover:bg-sky-700 text-white transition flex items-center space-x-1 shadow-xs cursor-pointer"
            >
              <span>{$language === "vi" ? "Xem chi tiết" : "Details"}</span>
              <ChevronRight class="h-3.5 w-3.5" />
            </button>

            <button
              type="button"
              onclick={dismissOrderNotification}
              class="p-2 rounded-xl text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition cursor-pointer"
              title={$language === "vi" ? "Ẩn thông báo" : "Dismiss"}
            >
              <X class="h-4 w-4" />
            </button>
          </div>
        </div>
      {/if}

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Account ID Card -->
        <div
          class="p-6 bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl shadow-sm"
        >
          <div
            class="flex items-center space-x-3 text-purple-600 dark:text-purple-400 mb-2"
          >
            <User class="h-5 w-5" />
            <h3 class="font-semibold text-base">
              {$language === "vi" ? "Thông tin tài khoản" : "Account Profile"}
            </h3>
          </div>
          <p class="text-sm font-bold text-[#0F1D2B] dark:text-white">
            {$currentUser?.name || "Customer"}
          </p>
          <p
            class="text-xs text-slate-500 mt-1 font-mono flex items-center space-x-1"
          >
            <KeyRound class="h-3 w-3 shrink-0" />
            <span>{accountId || "—"}</span>
          </p>
          {#if order}
            <p class="text-[11px] text-slate-500 mt-1 font-mono">
              {$language === "vi" ? "Mã đơn" : "Order"}: {order.id}
            </p>
          {/if}
        </div>

        <!-- Subscription Card -->
        <div
          class="p-6 bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl shadow-sm"
        >
          <div
            class="flex items-center space-x-3 text-sky-600 dark:text-sky-400 mb-2"
          >
            <Activity class="h-5 w-5" />
            <h3 class="font-semibold text-base">
              {$language === "vi" ? "Dịch vụ đang dùng" : "Active Subscription"}
            </h3>
          </div>
          <p class="text-sm font-semibold text-slate-700 dark:text-slate-200">
            {connection?.planName ||
              order?.planName ||
              ($language === "vi" ? "Chưa có gói cước" : "No plan yet")}
          </p>
          <p class="text-xs mt-1 font-semibold {serviceStatus.tone}">
            {$language === "vi" ? serviceStatus.vi : serviceStatus.en}
          </p>
        </div>

        <!-- Outstanding Billing Card -->
        <div
          class="p-6 bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl shadow-sm"
        >
          <div
            class="flex items-center space-x-3 text-rose-600 dark:text-rose-400 mb-2"
          >
            <CreditCard class="h-5 w-5" />
            <h3 class="font-semibold text-base">
              {$language === "vi"
                ? "Cước phí chưa thanh toán"
                : "Unpaid Charges"}
            </h3>
          </div>
          {#if totalDueAmount > 0}
            <div class="flex items-baseline justify-between">
              <p
                class="text-xl font-black font-mono text-rose-600 dark:text-rose-400"
              >
                ${totalDueAmount.toFixed(2)}
              </p>
              <span
                class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400"
              >
                {$language === "vi"
                  ? `${myUnpaidBills.length} HĐ nợ cước`
                  : `${myUnpaidBills.length} Due`}
              </span>
            </div>
            <div
              class="flex items-center justify-between mt-2 pt-2 border-t border-slate-100 dark:border-slate-800"
            >
              <span class="text-xs text-slate-500">
                {#if nextDueBill}
                  {$language === "vi"
                    ? `Hạn chót: ${nextDueBill.dueDate}`
                    : `Due: ${nextDueBill.dueDate}`}
                {:else}
                  {$language === "vi" ? "Cần nộp cước" : "Payment required"}
                {/if}
              </span>
              <button
                type="button"
                onclick={() => (activeTab = "pay-balance")}
                class="text-xs font-bold text-sky-600 hover:text-sky-700 dark:text-sky-400 flex items-center space-x-0.5"
              >
                <span>{$language === "vi" ? "Thanh toán" : "Pay Now"}</span>
                <ArrowRight class="h-3 w-3" />
              </button>
            </div>
          {:else}
            <p
              class="text-xl font-bold font-mono text-emerald-600 dark:text-emerald-400"
            >
              $0.00
            </p>
            <p class="text-xs text-slate-500 mt-2">
              {$language === "vi"
                ? "Đã hoàn tất thanh toán cước phí"
                : "All invoices fully settled"}
            </p>
          {/if}
        </div>

        <!-- Serving Branch Card -->
        <div
          class="p-6 bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl shadow-sm"
        >
          <div
            class="flex items-center space-x-3 text-slate-600 dark:text-slate-400 mb-2"
          >
            <Store class="h-5 w-5" />
            <h3 class="font-semibold text-base">
              {$language === "vi" ? "Chi nhánh phục vụ" : "Serving Branch"}
            </h3>
          </div>
          {#if shop}
            <p class="text-sm font-bold text-slate-700 dark:text-slate-200">
              {shop.name}
            </p>
            <p class="text-xs text-slate-500 mt-1">
              {shop.shopCode} · {shop.phone}
            </p>
          {:else}
            <p class="text-xs text-slate-600 dark:text-slate-300">
              {$language === "vi"
                ? "Hotline hỗ trợ 24/7: 1900 6868"
                : "24/7 support hotline: 1900 6868"}
            </p>
          {/if}
        </div>
      </div>

      <!-- Connection / installation details -->
      <div
        class="p-6 bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl shadow-sm"
      >
        <h3
          class="font-bold text-base text-[#0F1D2B] dark:text-white mb-4 flex items-center space-x-2"
        >
          <MapPin class="h-4 w-4 text-sky-600 dark:text-sky-400" />
          <span
            >{$language === "vi"
              ? "Chi tiết thuê bao"
              : "Subscription Details"}</span
          >
        </h3>
        <div
          class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 text-xs"
        >
          <div>
            <div class="text-[#537292] dark:text-[#8DB0D4]">
              {$language === "vi" ? "Địa chỉ lắp đặt" : "Installation address"}
            </div>
            <div class="font-semibold text-[#0F1D2B] dark:text-white mt-0.5">
              {connection?.installationAddress ||
                order?.installationAddress ||
                "—"}
            </div>
          </div>
          <div>
            <div class="text-[#537292] dark:text-[#8DB0D4]">
              {$language === "vi" ? "Loại kết nối" : "Connection type"}
            </div>
            <div class="font-semibold text-[#0F1D2B] dark:text-white mt-0.5">
              {connection?.connectionType || order?.connectionType || "—"}
            </div>
          </div>
          <div>
            <div class="text-[#537292] dark:text-[#8DB0D4]">
              {$language === "vi" ? "Cước hàng tháng" : "Monthly rental"}
            </div>
            <div class="font-semibold text-[#0F1D2B] dark:text-white mt-0.5">
              {connection ? `$${connection.monthlyRental.toFixed(2)}` : "—"}
            </div>
          </div>
          <div>
            <div class="text-[#537292] dark:text-[#8DB0D4]">
              {$language === "vi" ? "Thiết bị đầu cuối" : "CPE device"}
            </div>
            <div class="font-semibold text-[#0F1D2B] dark:text-white mt-0.5">
              {connection?.assignedDeviceModel ||
                ($language === "vi" ? "Chưa lắp đặt" : "Not installed yet")}
            </div>
          </div>
          <div>
            <div class="text-[#537292] dark:text-[#8DB0D4]">
              {$language === "vi" ? "Địa chỉ IP" : "IP address"}
            </div>
            <div
              class="font-mono font-semibold text-[#0F1D2B] dark:text-white mt-0.5"
            >
              {connection?.ipAddress || "—"}
            </div>
          </div>
          <div>
            <div class="text-[#537292] dark:text-[#8DB0D4]">
              {$language === "vi" ? "Ngày lắp đặt" : "Installed date"}
            </div>
            <div class="font-semibold text-[#0F1D2B] dark:text-white mt-0.5">
              {connection?.installedDate ||
                ($language === "vi" ? "Đang chờ xử lý" : "Pending")}
            </div>
          </div>
        </div>
      </div>

      <!-- Network Diagnostics & Test Records Card -->
      <div
        class="p-6 bg-gradient-to-br from-white via-sky-50/30 to-blue-50/20 dark:from-[#152434] dark:via-[#13283f] dark:to-[#0f1d2b] border border-sky-200 dark:border-sky-900/60 rounded-xl shadow-sm"
      >
        <div
          class="flex flex-col sm:flex-row sm:items-center justify-between gap-4"
        >
          <div class="flex items-start space-x-3.5">
            <div
              class="p-2.5 rounded-xl bg-sky-500/10 dark:bg-sky-500/20 text-sky-600 dark:text-sky-400 shrink-0"
            >
              <Activity class="h-6 w-6" />
            </div>
            <div>
              <div class="flex items-center space-x-2">
                <h3 class="font-bold text-base text-slate-900 dark:text-white">
                  {$language === "vi"
                    ? "Giám sát & Các bản ghi đo kiểm kết nối"
                    : "Network Monitoring & Test Records"}
                </h3>
                <span
                  class="px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider {connection?.status ===
                  'Active'
                    ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300'
                    : 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300'}"
                >
                  {connection?.status || "Normal"}
                </span>
              </div>
              <p class="text-xs text-slate-600 dark:text-slate-300 mt-1">
                {$language === "vi"
                  ? "Xem nhật ký kiểm tra định kỳ từ trung tâm NOC: Độ trễ (Ping), tốc độ tải xuống/tải lên, tỷ lệ mất gói và thông số kỹ thuật quang."
                  : "View periodic diagnostic logs from NOC center: Ping latency, download/upload throughput, packet loss and fiber optical metrics."}
              </p>
            </div>
          </div>

          <button
            type="button"
            onclick={() => (isTestRecordsModalOpen = true)}
            class="px-4 py-2.5 rounded-xl text-xs font-bold bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white transition shadow-sm flex items-center justify-center space-x-2 shrink-0 self-start sm:self-auto cursor-pointer"
          >
            <Activity class="h-4 w-4" />
            <span>
              {$language === "vi"
                ? "Xem chi tiết các bản ghi kiểm tra"
                : "View Full Test Records"}
            </span>
          </button>
        </div>

        {#if connection}
          <div
            class="grid grid-cols-2 sm:grid-cols-4 gap-3 mt-4 pt-4 border-t border-sky-100 dark:border-sky-900/40 text-xs"
          >
            <div
              class="bg-white/60 dark:bg-slate-900/40 p-2.5 rounded-lg border border-sky-100 dark:border-slate-800"
            >
              <span class="text-slate-500 dark:text-slate-400 block text-[11px]"
                >{$language === "vi"
                  ? "Độ trễ trung bình"
                  : "Average Latency"}</span
              >
              <span
                class="font-mono font-bold text-sky-700 dark:text-sky-300 text-sm"
                >~12 - 18 ms</span
              >
            </div>
            <div
              class="bg-white/60 dark:bg-slate-900/40 p-2.5 rounded-lg border border-sky-100 dark:border-slate-800"
            >
              <span class="text-slate-500 dark:text-slate-400 block text-[11px]"
                >{$language === "vi"
                  ? "Tỷ lệ mất gói (Loss)"
                  : "Packet Loss"}</span
              >
              <span
                class="font-mono font-bold text-emerald-600 dark:text-emerald-400 text-sm"
                >0.00%</span
              >
            </div>
            <div
              class="bg-white/60 dark:bg-slate-900/40 p-2.5 rounded-lg border border-sky-100 dark:border-slate-800"
            >
              <span class="text-slate-500 dark:text-slate-400 block text-[11px]"
                >{$language === "vi"
                  ? "Băng thông đăng ký"
                  : "Provisioned Speed"}</span
              >
              <span
                class="font-mono font-bold text-slate-800 dark:text-slate-200 text-sm"
                >{connection.planName || "High Speed"}</span
              >
            </div>
            <div
              class="bg-white/60 dark:bg-slate-900/40 p-2.5 rounded-lg border border-sky-100 dark:border-slate-800"
            >
              <span class="text-slate-500 dark:text-slate-400 block text-[11px]"
                >{$language === "vi"
                  ? "Kiểm tra gần nhất"
                  : "Last Tested"}</span
              >
              <span
                class="font-mono font-bold text-slate-700 dark:text-slate-300 text-sm"
                >{connection.installedDate || "Vừa xong"}</span
              >
            </div>
          </div>
        {/if}
      </div>
    </div>
  {:else if activeTab === "billing"}
    <div class="space-y-6">
      <!-- Top Alert: Unpaid & Outstanding Charges Summary -->
      {#if totalDueAmount > 0}
        <div
          class="p-5 rounded-2xl bg-gradient-to-r from-rose-500/10 via-amber-500/10 to-sky-500/10 border border-rose-300 dark:border-rose-800/60 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4"
        >
          <div class="flex items-start space-x-3.5">
            <div
              class="p-2.5 bg-rose-500/20 text-rose-600 dark:text-rose-400 rounded-xl shrink-0 mt-0.5"
            >
              <AlertCircle class="h-6 w-6" />
            </div>
            <div>
              <div class="flex items-center space-x-2">
                <h3 class="font-bold text-base text-slate-900 dark:text-white">
                  {$language === "vi"
                    ? "Hóa đơn & Cước phí chưa thanh toán"
                    : "Unpaid Invoices & Outstanding Charges"}
                </h3>
                <span
                  class="px-2 py-0.5 rounded-full text-xs font-bold bg-rose-500 text-white font-mono"
                >
                  ${totalDueAmount.toFixed(2)}
                </span>
              </div>
              <p
                class="text-xs text-slate-600 dark:text-slate-300 mt-1 max-w-2xl leading-relaxed"
              >
                {$language === "vi"
                  ? `Quý khách hiện có ${myUnpaidBills.length} hóa đơn chưa thanh toán hoặc thanh toán 1 phần. Vui lòng thanh toán cước phí để duy trì dịch vụ liên tục và không phát sinh phí chậm trả.`
                  : `You currently have ${myUnpaidBills.length} unpaid or partially paid invoice(s). Please settle outstanding balance to maintain uninterrupted services.`}
              </p>
            </div>
          </div>
          <button
            type="button"
            onclick={() => (activeTab = "pay-balance")}
            class="px-5 py-2.5 bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white rounded-xl text-xs font-bold transition shadow flex items-center space-x-2 shrink-0 self-start md:self-auto"
          >
            <Wallet class="h-4 w-4" />
            <span
              >{$language === "vi"
                ? "Thanh toán số dư ngay"
                : "Pay Balance Now"}</span
            >
            <ArrowRight class="h-4 w-4" />
          </button>
        </div>
      {/if}

      <!-- Invoices Container -->
      <div
        class="rounded-2xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-sm overflow-hidden p-6 space-y-4"
      >
        <!-- Header: Title & Description -->
        <div>
          <h3
            class="text-base font-bold text-[#0F1D2B] dark:text-white flex items-center space-x-2"
          >
            <CreditCard class="h-5 w-5 text-sky-600 dark:text-sky-400" />
            <span
              >{$language === "vi"
                ? "Lịch sử hóa đơn & cước phí"
                : "Billing & Invoices History"}</span
            >
          </h3>
          <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-0.5">
            {$language === "vi"
              ? "Danh sách toàn bộ các hóa đơn định kỳ, cước phí chưa thanh toán và đã thanh toán"
              : "Complete record of recurring invoices, due amounts, and payment status"}
          </p>
        </div>

        <!-- Status Filter Tabs: full-width dedicated horizontal row -->
        <div
          class="flex flex-row flex-nowrap items-center gap-2 overflow-x-auto pb-2 border-b border-slate-100 dark:border-slate-800 text-xs whitespace-nowrap"
        >
          <span
            class="text-slate-400 font-semibold uppercase text-[10px] tracking-wider shrink-0 mr-1 inline-flex items-center space-x-1"
          >
            <Filter class="h-3 w-3" />
            <span>{$language === "vi" ? "Trạng thái:" : "Status:"}</span>
          </span>
          <button
            type="button"
            onclick={() => (billingTabFilter = "all")}
            class="inline-flex items-center justify-center px-3 py-1.5 rounded-lg font-semibold transition shrink-0 whitespace-nowrap {billingTabFilter ===
            'all'
              ? 'bg-sky-600 text-white shadow-xs'
              : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'}"
          >
            <span
              >{$language === "vi"
                ? `Tất cả (${myBills.length})`
                : `All (${myBills.length})`}</span
            >
          </button>
          <button
            type="button"
            onclick={() => (billingTabFilter = "unpaid")}
            class="inline-flex items-center justify-center space-x-1.5 px-3 py-1.5 rounded-lg font-semibold transition shrink-0 whitespace-nowrap {billingTabFilter ===
            'unpaid'
              ? 'bg-rose-600 text-white shadow-xs'
              : 'bg-rose-50 dark:bg-rose-950/30 text-rose-700 dark:text-rose-400 hover:bg-rose-100 dark:hover:bg-rose-900/40'}"
          >
            <AlertCircle class="h-3 w-3" />
            <span
              >{$language === "vi"
                ? `Chưa thanh toán & Còn nợ (${myUnpaidBills.length})`
                : `Unpaid (${myUnpaidBills.length})`}</span
            >
          </button>
          <button
            type="button"
            onclick={() => (billingTabFilter = "paid")}
            class="inline-flex items-center justify-center space-x-1.5 px-3 py-1.5 rounded-lg font-semibold transition shrink-0 whitespace-nowrap {billingTabFilter ===
            'paid'
              ? 'bg-emerald-600 text-white shadow-xs'
              : 'bg-emerald-50 dark:bg-emerald-950/30 text-emerald-700 dark:text-emerald-400 hover:bg-emerald-100 dark:hover:bg-emerald-900/40'}"
          >
            <CheckCircle2 class="h-3 w-3" />
            <span
              >{$language === "vi"
                ? `Đã thanh toán (${myPaidBills.length})`
                : `Paid (${myPaidBills.length})`}</span
            >
          </button>
        </div>

        {#if filteredMyBills.length > 0}
          <div class="overflow-x-auto">
            <table class="w-full text-xs text-left">
              <thead
                class="bg-[#EDF6FF] dark:bg-[#101C29] text-[#305070] dark:text-slate-300 uppercase tracking-wider"
              >
                <tr>
                  <th class="px-4 py-3 font-semibold"
                    >{$language === "vi" ? "Số hóa đơn" : "Invoice"}</th
                  >
                  <th class="px-4 py-3 font-semibold"
                    >{$language === "vi" ? "Kỳ cước & Gói" : "Month & Plan"}</th
                  >
                  <th class="px-4 py-3 font-semibold text-right"
                    >{$language === "vi" ? "Tổng cước" : "Total"}</th
                  >
                  <th class="px-4 py-3 font-semibold text-right"
                    >{$language === "vi" ? "Đã trả" : "Paid"}</th
                  >
                  <th
                    class="px-4 py-3 font-semibold text-right text-rose-600 dark:text-rose-400"
                    >{$language === "vi"
                      ? "Cước chưa thanh toán"
                      : "Outstanding Due"}</th
                  >
                  <th class="px-4 py-3 font-semibold text-center"
                    >{$language === "vi" ? "Hạn thanh toán" : "Due date"}</th
                  >
                  <th class="px-4 py-3 font-semibold text-center"
                    >{$language === "vi" ? "Trạng thái" : "Status"}</th
                  >
                  <th class="px-4 py-3 font-semibold text-right"
                    >{$language === "vi" ? "Thao tác" : "Action"}</th
                  >
                </tr>
              </thead>
              <tbody class="divide-y divide-[#CCE4F7] dark:divide-[#253D56]">
                {#each filteredMyBills as bill (bill.id)}
                  <tr
                    class="text-[#1B2D40] dark:text-[#E0F1FF] hover:bg-slate-50/60 dark:hover:bg-slate-800/30 transition"
                  >
                    <td
                      class="px-4 py-3 font-mono font-bold text-sky-600 dark:text-sky-400"
                      >{bill.invoiceNumber}</td
                    >
                    <td class="px-4 py-3">
                      <div class="font-bold text-slate-800 dark:text-slate-200">
                        {bill.billingMonth}
                      </div>
                      <div class="text-[11px] text-slate-500">
                        {bill.planName}
                      </div>
                    </td>
                    <td class="px-4 py-3 text-right font-mono font-medium"
                      >${bill.totalAmount.toFixed(2)}</td
                    >
                    <td
                      class="px-4 py-3 text-right font-mono text-emerald-600 dark:text-emerald-400 font-medium"
                    >
                      ${bill.amountPaid.toFixed(2)}
                    </td>
                    <td
                      class="px-4 py-3 text-right font-mono font-black text-sm {bill.dueAmount >
                      0
                        ? 'text-rose-600 dark:text-rose-400'
                        : 'text-emerald-600 dark:text-emerald-400'}"
                    >
                      ${bill.dueAmount.toFixed(2)}
                    </td>
                    <td class="px-4 py-3 text-center font-mono"
                      >{bill.dueDate}</td
                    >
                    <td class="px-4 py-3 text-center">
                      {#if bill.status === "Paid"}
                        <span
                          class="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-500/15 text-emerald-600 dark:text-emerald-400"
                        >
                          {$language === "vi" ? "Đã thanh toán" : "Paid"}
                        </span>
                      {:else if bill.status === "Partially Paid"}
                        <div class="flex flex-col items-center">
                          <span
                            class="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-amber-500/15 text-amber-600 dark:text-amber-400 flex items-center space-x-1"
                          >
                            <AlertCircle class="h-3 w-3" />
                            <span
                              >{$language === "vi"
                                ? "Thanh toán 1 phần"
                                : "Partially Paid"}</span
                            >
                          </span>
                          <span
                            class="text-[10px] text-slate-500 mt-0.5 font-mono"
                          >
                            {$language === "vi"
                              ? `Còn nợ $${bill.dueAmount.toFixed(2)}`
                              : `Due $${bill.dueAmount.toFixed(2)}`}
                          </span>
                        </div>
                      {:else}
                        <span
                          class="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-rose-500/15 text-rose-600 dark:text-rose-400"
                        >
                          {$language === "vi" ? "Chưa thanh toán" : "Unpaid"}
                        </span>
                      {/if}
                    </td>
                    <td class="px-4 py-3 text-right">
                      {#if bill.dueAmount > 0}
                        <button
                          type="button"
                          onclick={() => {
                            activePayModalBill = bill;
                            activeTab = "pay-balance";
                          }}
                          class="px-3 py-1 bg-sky-600 hover:bg-sky-700 text-white rounded-lg text-xs font-bold transition shadow-xs inline-flex items-center space-x-1"
                        >
                          <CreditCard class="h-3 w-3" />
                          <span
                            >{$language === "vi" ? "Thanh toán" : "Pay"}</span
                          >
                        </button>
                      {:else}
                        <span
                          class="text-[11px] text-slate-400 font-semibold flex items-center space-x-1 justify-end"
                        >
                          <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
                          <span
                            >{$language === "vi" ? "Hoàn tất" : "Settled"}</span
                          >
                        </span>
                      {/if}
                    </td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>
        {:else}
          <div class="p-8 text-center">
            <CreditCard class="h-10 w-10 mx-auto text-slate-400 mb-2" />
            <p class="text-xs text-slate-500">
              {$language === "vi"
                ? "Không có hóa đơn nào phù hợp với bộ lọc hiện tại."
                : "No invoices match this filter."}
            </p>
          </div>
        {/if}
      </div>
    </div>
  {:else if activeTab === "pay-balance"}
    {@const unpaidBills = myBills.filter((b) => b.status !== "Paid")}
    {@const nextDueBill = unpaidBills[0] ?? null}

    <div class="space-y-6">
      <!-- 1. MỤC NHẮC NHỞ THANH TOÁN ĐÚNG HẠN (On-Time Payment Reminder) -->
      {#if unpaidBills.length > 0 && nextDueBill}
        <div
          class="relative overflow-hidden rounded-2xl bg-gradient-to-r from-amber-500/10 via-sky-500/10 to-indigo-500/10 border border-amber-300 dark:border-amber-700/60 p-6 shadow-sm"
        >
          <div
            class="flex flex-col md:flex-row md:items-center justify-between gap-4"
          >
            <div class="flex items-start space-x-4">
              <div
                class="p-3 bg-amber-500/20 text-amber-600 dark:text-amber-400 rounded-xl shrink-0 mt-0.5"
              >
                <Bell class="h-6 w-6 animate-pulse" />
              </div>
              <div>
                <div class="flex items-center space-x-2 flex-wrap">
                  <h3
                    class="text-base font-bold text-[#0F1D2B] dark:text-white"
                  >
                    {$language === "vi"
                      ? "Nhắc nhở thanh toán cước đúng hạn"
                      : "On-Time Payment Reminder"}
                  </h3>
                  <span
                    class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-500 text-white flex items-center space-x-1"
                  >
                    <Clock class="h-3 w-3" />
                    <span
                      >{$language === "vi"
                        ? `Hạn chót: ${nextDueBill.dueDate}`
                        : `Due by: ${nextDueBill.dueDate}`}</span
                    >
                  </span>
                </div>
                <p
                  class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-1.5 max-w-2xl leading-relaxed"
                >
                  {$language === "vi"
                    ? "Quý khách vui lòng thanh toán trước ngày đến hạn để đảm bảo đường truyền Internet luôn thông suốt, không bị tạm ngừng dịch vụ và tránh phí phạt trễ hạn (5%/kỳ). Thanh toán đúng hạn còn giúp nâng điểm hội viên Nexus Priority!"
                    : "Please complete your bill payment before the due date to ensure high-speed connection remains uninterrupted and avoid late surcharges (5%/period). On-time payments also upgrade your Nexus Priority tier!"}
                </p>
                <div
                  class="flex flex-wrap items-center gap-3 mt-3 text-xs font-medium text-emerald-700 dark:text-emerald-400"
                >
                  <span class="flex items-center space-x-1">
                    <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
                    <span
                      >{$language === "vi"
                        ? "Kết nối tốc độ cao 24/7"
                        : "24/7 High-speed connection"}</span
                    >
                  </span>
                  <span class="flex items-center space-x-1">
                    <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
                    <span
                      >{$language === "vi"
                        ? "Miễn 100% phí trễ hạn"
                        : "0% Late surcharge"}</span
                    >
                  </span>
                  <span class="flex items-center space-x-1">
                    <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
                    <span
                      >{$language === "vi"
                        ? "Tích lũy điểm thưởng"
                        : "Earn loyalty credits"}</span
                    >
                  </span>
                </div>
              </div>
            </div>

            <!-- Auto-reminder switch -->
            <div
              class="bg-white/80 dark:bg-[#152434]/80 backdrop-blur rounded-xl p-3 border border-slate-200 dark:border-slate-700 flex items-center justify-between sm:justify-start space-x-3 shrink-0"
            >
              <div class="text-left">
                <div class="text-xs font-bold text-[#0F1D2B] dark:text-white">
                  {$language === "vi"
                    ? "Nhắc nhở qua SMS/Email"
                    : "SMS/Email Alerts"}
                </div>
                <div class="text-[11px] text-slate-500">
                  {$language === "vi"
                    ? "Báo trước 3 ngày đến hạn"
                    : "3 days before due date"}
                </div>
              </div>
              <button
                type="button"
                onclick={() => {
                  autoReminderEnabled = !autoReminderEnabled;
                  toast.success(
                    autoReminderEnabled
                      ? $language === "vi"
                        ? "Đã bật nhắc cước tự động qua SMS/Email"
                        : "Auto reminder notifications turned on"
                      : $language === "vi"
                        ? "Đã tắt nhắc cước tự động"
                        : "Auto reminder notifications turned off",
                  );
                }}
                class="relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none {autoReminderEnabled
                  ? 'bg-sky-600'
                  : 'bg-slate-300 dark:bg-slate-600'}"
                role="switch"
                aria-checked={autoReminderEnabled}
                aria-label="Toggle SMS/Email reminder alerts"
              >
                <span
                  class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out {autoReminderEnabled
                    ? 'translate-x-5'
                    : 'translate-x-0'}"
                ></span>
              </button>
            </div>
          </div>
        </div>
      {:else}
        <div
          class="rounded-2xl bg-gradient-to-r from-emerald-500/10 to-sky-500/10 border border-emerald-200 dark:border-emerald-800/60 p-5 shadow-sm flex items-center space-x-4"
        >
          <div
            class="p-3 bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded-xl shrink-0"
          >
            <ShieldCheck class="h-6 w-6" />
          </div>
          <div>
            <h4
              class="font-bold text-sm text-emerald-900 dark:text-emerald-200"
            >
              {$language === "vi"
                ? "Tất cả hóa đơn đã được thanh toán đầy đủ!"
                : "All invoices are fully paid!"}
            </h4>
            <p class="text-xs text-slate-600 dark:text-slate-300 mt-0.5">
              {$language === "vi"
                ? "Cảm ơn quý khách đã luôn duy trì thói quen thanh toán cước đúng hạn cùng Nexus."
                : "Thank you for keeping your account in good standing with on-time payments."}
            </p>
          </div>
        </div>
      {/if}

      <!-- 2. LỰA CHỌN CHU KỲ THANH TOÁN (Monthly, Quarterly, Annually) -->
      <div
        class="rounded-2xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-sm p-6"
      >
        <div
          class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-4"
        >
          <div>
            <h3
              class="font-bold text-base text-[#0F1D2B] dark:text-white flex items-center space-x-2"
            >
              <Calendar class="h-5 w-5 text-sky-600 dark:text-sky-400" />
              <span
                >{$language === "vi"
                  ? "Lựa chọn chu kỳ thanh toán"
                  : "Select Billing Cycle"}</span
              >
            </h3>
            <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-0.5">
              {$language === "vi"
                ? "Lựa chọn thanh toán trước theo quý hoặc năm để nhận ưu đãi chiết khấu trực tiếp"
                : "Prepay quarterly or annually to unlock special discount schemes"}
            </p>
          </div>
          <span
            class="text-xs font-semibold px-3 py-1 bg-sky-100 dark:bg-sky-950/50 text-sky-700 dark:text-sky-300 rounded-full w-fit"
          >
            {$language === "vi"
              ? "Ưu đãi thanh toán dài hạn"
              : "Long-term Prepayment Perks"}
          </span>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <!-- Chu kỳ 1: Theo Tháng -->
          <button
            type="button"
            onclick={() => (selectedCycle = "monthly")}
            class="text-left relative p-4 rounded-xl border-2 transition-all flex flex-col justify-between {selectedCycle ===
            'monthly'
              ? 'border-sky-500 bg-sky-50/50 dark:bg-sky-950/20 shadow-md'
              : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 bg-slate-50/50 dark:bg-slate-800/30'}"
          >
            <div>
              <div class="flex items-center justify-between">
                <span class="font-bold text-sm text-[#0F1D2B] dark:text-white">
                  {$language === "vi" ? "Theo Tháng" : "Monthly"}
                </span>
                <span
                  class="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300"
                >
                  {$language === "vi" ? "1 Tháng" : "1 Month"}
                </span>
              </div>
              <p class="text-xs text-slate-500 mt-2">
                {$language === "vi"
                  ? "Chu kỳ định kỳ tiêu chuẩn"
                  : "Standard recurrent monthly cycle"}
              </p>
            </div>
            <div
              class="mt-4 pt-3 border-t border-slate-200/60 dark:border-slate-700/60 flex items-center justify-between"
            >
              <span class="text-xs font-mono text-slate-500"
                >{$language === "vi" ? "Giá gốc" : "Standard rate"}</span
              >
              <span class="text-xs font-bold text-sky-600 dark:text-sky-400"
                >{$language === "vi" ? "Không chiết khấu" : "No discount"}</span
              >
            </div>
          </button>

          <!-- Chu kỳ 2: Theo Quý -->
          <button
            type="button"
            onclick={() => (selectedCycle = "quarterly")}
            class="text-left relative p-4 rounded-xl border-2 transition-all flex flex-col justify-between {selectedCycle ===
            'quarterly'
              ? 'border-sky-500 bg-sky-50/50 dark:bg-sky-950/20 shadow-md ring-2 ring-sky-500/20'
              : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 bg-slate-50/50 dark:bg-slate-800/30'}"
          >
            <div>
              <div class="flex items-center justify-between">
                <span class="font-bold text-sm text-[#0F1D2B] dark:text-white">
                  {$language === "vi" ? "Theo Quý" : "Quarterly"}
                </span>
                <span
                  class="text-[11px] font-bold px-2 py-0.5 rounded-full bg-amber-500 text-white"
                >
                  {$language === "vi" ? "Tiết kiệm 5%" : "Save 5%"}
                </span>
              </div>
              <p class="text-xs text-slate-500 mt-2">
                {$language === "vi"
                  ? "Thanh toán trọn gói 3 tháng liền cước"
                  : "Prepay full 3-month package"}
              </p>
            </div>
            <div
              class="mt-4 pt-3 border-t border-slate-200/60 dark:border-slate-700/60 flex items-center justify-between"
            >
              <span class="text-xs font-mono text-slate-500"
                >{$language === "vi" ? "Hệ số x3" : "Multiplier x3"}</span
              >
              <span class="text-xs font-bold text-amber-600 dark:text-amber-400"
                >{$language === "vi"
                  ? "Giảm 5% tổng cước"
                  : "5% Off Total"}</span
              >
            </div>
          </button>

          <!-- Chu kỳ 3: Theo Năm -->
          <button
            type="button"
            onclick={() => (selectedCycle = "annually")}
            class="text-left relative p-4 rounded-xl border-2 transition-all flex flex-col justify-between {selectedCycle ===
            'annually'
              ? 'border-emerald-500 bg-emerald-50/50 dark:bg-emerald-950/20 shadow-md ring-2 ring-emerald-500/20'
              : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 bg-slate-50/50 dark:bg-slate-800/30'}"
          >
            <div
              class="absolute -top-2.5 right-3 px-2 py-0.5 bg-gradient-to-r from-emerald-500 to-teal-500 text-white text-[10px] font-extrabold rounded-full uppercase tracking-wider shadow"
            >
              {$language === "vi" ? "Ưu đãi cao nhất" : "Best Value"}
            </div>
            <div>
              <div class="flex items-center justify-between">
                <span class="font-bold text-sm text-[#0F1D2B] dark:text-white">
                  {$language === "vi" ? "Theo Năm" : "Annually"}
                </span>
                <span
                  class="text-[11px] font-bold px-2 py-0.5 rounded-full bg-emerald-600 text-white"
                >
                  {$language === "vi" ? "Giảm 10%" : "Save 10%"}
                </span>
              </div>
              <p class="text-xs text-slate-500 mt-2">
                {$language === "vi"
                  ? "Gia hạn trọn gói 12 tháng tiện lợi"
                  : "Prepay full 12-month annual package"}
              </p>
            </div>
            <div
              class="mt-4 pt-3 border-t border-slate-200/60 dark:border-slate-700/60 flex items-center justify-between"
            >
              <span class="text-xs font-mono text-slate-500"
                >{$language === "vi" ? "Hệ số x12" : "Multiplier x12"}</span
              >
              <span
                class="text-xs font-bold text-emerald-600 dark:text-emerald-400"
                >{$language === "vi"
                  ? "Giảm 10% tổng cước"
                  : "10% Off Total"}</span
              >
            </div>
          </button>
        </div>
      </div>

      <!-- 3. DANH SÁCH HÓA ĐƠN & TIẾN HÀNH THANH TOÁN -->
      <div
        class="rounded-2xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-sm p-6"
      >
        <h3
          class="font-bold text-base text-[#0F1D2B] dark:text-white flex items-center space-x-2 mb-4"
        >
          <Wallet class="h-5 w-5 text-sky-600 dark:text-sky-400" />
          <span
            >{$language === "vi"
              ? "Hóa đơn & Cước phí chưa thanh toán"
              : "Invoices & Outstanding Charges Pending Payment"}</span
          >
        </h3>

        {#if unpaidBills.length > 0}
          <div class="space-y-4">
            {#each unpaidBills as bill}
              {@const calc = getCycleCalculation(bill.dueAmount, selectedCycle)}
              <div
                class="p-5 border border-[#CCE4F7] dark:border-[#253D56] rounded-2xl flex flex-col lg:flex-row lg:justify-between lg:items-center gap-5 bg-gradient-to-br from-[#EDF6FF] to-white dark:from-[#101C29] dark:to-[#152434]"
              >
                <div class="space-y-1.5">
                  <div class="flex items-center space-x-2 flex-wrap">
                    <span
                      class="font-bold text-base text-[#0F1D2B] dark:text-white"
                    >
                      {$language === "vi" ? "Hóa đơn" : "Invoice"} #{bill.invoiceNumber}
                    </span>
                    <span
                      class="px-2 py-0.5 rounded-full text-xs font-mono font-bold bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300"
                    >
                      {bill.planName}
                    </span>
                    {#if bill.status === "Partially Paid"}
                      <span
                        class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300 border border-amber-300 flex items-center space-x-1"
                      >
                        <AlertCircle class="h-3 w-3 text-amber-600" />
                        <span
                          >{$language === "vi"
                            ? "Thanh toán 1 phần"
                            : "Partially Paid"}</span
                        >
                      </span>
                    {:else}
                      <span
                        class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300"
                      >
                        {$language === "vi" ? "Chưa thanh toán" : "Unpaid"}
                      </span>
                    {/if}
                  </div>
                  <div
                    class="text-xs text-[#537292] dark:text-[#8DB0D4] flex items-center space-x-3 flex-wrap"
                  >
                    <span
                      >{$language === "vi" ? "Kỳ cước" : "Billing month"}:
                      <strong class="text-slate-800 dark:text-slate-200"
                        >{bill.billingMonth}</strong
                      ></span
                    >
                    <span>•</span>
                    <span class="text-rose-500 dark:text-rose-400 font-semibold"
                      >{$language === "vi" ? "Hạn thanh toán" : "Due"}: {bill.dueDate}</span
                    >
                    <span>•</span>
                    {#if bill.status === "Partially Paid"}
                      <span
                        >{$language === "vi"
                          ? "Đã thanh toán:"
                          : "Already paid:"}
                        <strong
                          class="text-emerald-600 dark:text-emerald-400 font-mono"
                          >${bill.amountPaid.toFixed(2)}</strong
                        ></span
                      >
                      <span>•</span>
                      <span
                        >{$language === "vi"
                          ? "Cước còn nợ:"
                          : "Remaining due:"}
                        <strong
                          class="text-rose-600 dark:text-rose-400 font-mono"
                          >${bill.dueAmount.toFixed(2)}</strong
                        ></span
                      >
                    {:else}
                      <span
                        >{$language === "vi"
                          ? "Cước phí chưa thanh toán:"
                          : "Outstanding due:"}
                        <strong
                          class="text-rose-600 dark:text-rose-400 font-mono"
                          >${bill.dueAmount.toFixed(2)}</strong
                        ></span
                      >
                    {/if}
                  </div>
                  <div
                    class="text-xs text-sky-700 dark:text-sky-300 font-medium"
                  >
                    {$language === "vi" ? "Chu kỳ áp dụng:" : "Selected cycle:"}
                    <strong>{calc.monthsLabelVi}</strong>
                    {#if calc.discount > 0}
                      <span
                        class="text-emerald-600 dark:text-emerald-400 ml-1 font-semibold"
                      >
                        ({$language === "vi"
                          ? `Chiết khấu: -$${calc.discount.toFixed(2)}`
                          : `Discount: -$${calc.discount.toFixed(2)}`})
                      </span>
                    {/if}
                  </div>
                </div>

                <div
                  class="flex flex-col sm:flex-row sm:items-center gap-4 lg:text-right"
                >
                  <div>
                    <div class="text-xs text-slate-500">
                      {$language === "vi"
                        ? "Tổng thanh toán"
                        : "Payable Amount"}
                    </div>
                    <div
                      class="text-2xl font-black font-mono text-sky-600 dark:text-sky-400"
                    >
                      ${calc.total.toFixed(2)}
                    </div>
                    {#if calc.discount > 0}
                      <div
                        class="text-[11px] text-slate-400 line-through font-mono"
                      >
                        ${calc.base.toFixed(2)}
                      </div>
                    {/if}
                  </div>

                  <button
                    type="button"
                    class="px-5 py-2.5 bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white rounded-xl text-sm font-bold transition-all shadow-md hover:shadow-lg flex items-center justify-center space-x-2 shrink-0"
                    onclick={() => {
                      activePayModalBill = bill;
                    }}
                  >
                    <CreditCard class="h-4 w-4" />
                    <span
                      >{$language === "vi"
                        ? "Thanh toán ngay"
                        : "Pay Now"}</span
                    >
                    <ArrowRight class="h-4 w-4" />
                  </button>
                </div>
              </div>
            {/each}
          </div>
        {:else}
          <div
            class="p-10 text-center border-t border-[#CCE4F7] dark:border-[#253D56]"
          >
            <Wallet
              class="h-12 w-12 mx-auto text-sky-600 dark:text-sky-400 mb-3"
            />
            <p class="text-[#0F1D2B] dark:text-white font-bold text-sm">
              {$language === "vi"
                ? "Không có hóa đơn chưa thanh toán"
                : "No Outstanding Invoices"}
            </p>
            <p class="text-[#537292] dark:text-[#8DB0D4] text-xs mt-1">
              {$language === "vi"
                ? "Tất cả các dịch vụ viễn thông của quý khách đang hoạt động bình thường."
                : "All telecom services are running normally and fully settled."}
            </p>
          </div>
        {/if}
      </div>
    </div>

    <!-- 4. MODAL THANH TOÁN ĐA PHƯƠNG THỨC (PAYMENT GATEWAY MODAL) -->
    {#if activePayModalBill && modalCycleInfo}
      <div
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm animate-in fade-in duration-200"
      >
        <div
          class="bg-white dark:bg-[#152434] rounded-2xl max-w-2xl w-full border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden flex flex-col max-h-[92vh]"
        >
          <!-- Modal Header -->
          <div
            class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50/50 dark:bg-slate-800/40"
          >
            <div class="flex items-center space-x-2.5">
              <div
                class="p-2 bg-sky-500/20 text-sky-600 dark:text-sky-400 rounded-lg"
              >
                <Wallet class="h-5 w-5" />
              </div>
              <div>
                <h3 class="font-bold text-base text-slate-900 dark:text-white">
                  {$language === "vi"
                    ? "Cổng thanh toán cước trực tuyến"
                    : "Nexus Online Payment Gateway"}
                </h3>
                <p class="text-xs text-slate-500 font-mono">
                  {$language === "vi" ? "Hóa đơn" : "Invoice"}: #{activePayModalBill.invoiceNumber}
                  · {modalCycleInfo.monthsLabelVi}
                </p>
              </div>
            </div>
            <button
              type="button"
              onclick={() => (activePayModalBill = null)}
              class="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg"
              aria-label="Close modal"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Modal Body (Scrollable) -->
          <div class="p-6 overflow-y-auto space-y-6">
            <!-- Payment Order Summary Card -->
            <div
              class="p-4 rounded-xl bg-gradient-to-r from-sky-500/10 via-indigo-500/10 to-blue-500/10 border border-sky-200 dark:border-sky-900/50 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
            >
              <div>
                <div
                  class="text-xs text-slate-500 uppercase tracking-wider font-semibold"
                >
                  {$language === "vi"
                    ? "Thông tin thanh toán"
                    : "Payment Details"}
                </div>
                <div
                  class="font-bold text-sm text-slate-900 dark:text-white mt-0.5"
                >
                  {activePayModalBill.planName} · {activePayModalBill.customerName}
                </div>
                <div class="text-xs text-slate-500">
                  {$language === "vi" ? "Chu kỳ:" : "Cycle:"}
                  <strong class="text-sky-600 dark:text-sky-400"
                    >{modalCycleInfo.monthsLabelVi}</strong
                  >
                  {#if modalCycleInfo.discount > 0}
                    <span
                      class="text-emerald-600 dark:text-emerald-400 font-semibold ml-1"
                    >
                      ({$language === "vi"
                        ? `Tiết kiệm $${modalCycleInfo.discount.toFixed(2)}`
                        : `Save $${modalCycleInfo.discount.toFixed(2)}`})
                    </span>
                  {/if}
                </div>
              </div>
              <div class="text-left sm:text-right">
                <div class="text-xs text-slate-500">
                  {$language === "vi" ? "Số tiền thanh toán" : "Total Payable"}
                </div>
                <div
                  class="text-2xl font-black font-mono text-sky-600 dark:text-sky-400"
                >
                  ${modalCycleInfo.total.toFixed(2)}
                </div>
              </div>
            </div>

            <!-- Choose Payment Method Tabs -->
            <div class="space-y-3">
              <label
                class="block text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
              >
                {$language === "vi"
                  ? "Chọn phương thức thanh toán:"
                  : "Choose Payment Method:"}
              </label>

              <div class="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
                <!-- Method 1: Chuyển khoản VietQR -->
                <button
                  type="button"
                  onclick={() => (chosenPaymentMethod = "Bank Transfer/NEFT")}
                  class="p-3 rounded-xl border text-center transition flex flex-col items-center justify-center space-y-1.5 {chosenPaymentMethod ===
                  'Bank Transfer/NEFT'
                    ? 'border-sky-500 bg-sky-50/60 dark:bg-sky-950/30 text-sky-600 dark:text-sky-400 shadow-sm font-bold'
                    : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 text-slate-600 dark:text-slate-300'}"
                >
                  <QrCode class="h-5 w-5" />
                  <span class="text-xs"
                    >{$language === "vi" ? "Mã VietQR" : "VietQR"}</span
                  >
                </button>

                <!-- Method 2: Thẻ tín dụng/ghi nợ -->
                <button
                  type="button"
                  onclick={() => (chosenPaymentMethod = "Credit/Debit Card")}
                  class="p-3 rounded-xl border text-center transition flex flex-col items-center justify-center space-y-1.5 {chosenPaymentMethod ===
                  'Credit/Debit Card'
                    ? 'border-sky-500 bg-sky-50/60 dark:bg-sky-950/30 text-sky-600 dark:text-sky-400 shadow-sm font-bold'
                    : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 text-slate-600 dark:text-slate-300'}"
                >
                  <CreditCard class="h-5 w-5" />
                  <span class="text-xs"
                    >{$language === "vi" ? "Thẻ Quốc tế" : "Credit Card"}</span
                  >
                </button>

                <!-- Method 3: Ví điện tử -->
                <button
                  type="button"
                  onclick={() => (chosenPaymentMethod = "UPI/Digital Wallet")}
                  class="p-3 rounded-xl border text-center transition flex flex-col items-center justify-center space-y-1.5 {chosenPaymentMethod ===
                  'UPI/Digital Wallet'
                    ? 'border-sky-500 bg-sky-50/60 dark:bg-sky-950/30 text-sky-600 dark:text-sky-400 shadow-sm font-bold'
                    : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 text-slate-600 dark:text-slate-300'}"
                >
                  <Smartphone class="h-5 w-5" />
                  <span class="text-xs"
                    >{$language === "vi" ? "Ví điện tử" : "E-Wallet"}</span
                  >
                </button>

                <!-- Method 4: Tiền mặt tại quầy -->
                <button
                  type="button"
                  onclick={() => (chosenPaymentMethod = "Cash")}
                  class="p-3 rounded-xl border text-center transition flex flex-col items-center justify-center space-y-1.5 {chosenPaymentMethod ===
                  'Cash'
                    ? 'border-sky-500 bg-sky-50/60 dark:bg-sky-950/30 text-sky-600 dark:text-sky-400 shadow-sm font-bold'
                    : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 text-slate-600 dark:text-slate-300'}"
                >
                  <Banknote class="h-5 w-5" />
                  <span class="text-xs"
                    >{$language === "vi" ? "Tiền mặt" : "Cash"}</span
                  >
                </button>
              </div>
            </div>

            <!-- TAB CONTENT 1: BANK TRANSFER / VIETQR -->
            {#if chosenPaymentMethod === "Bank Transfer/NEFT"}
              <div
                class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 space-y-4"
              >
                <div class="flex flex-col sm:flex-row items-center gap-5">
                  <!-- Dynamic QR Mockup -->
                  <div
                    class="bg-white p-3 rounded-xl shadow-md border border-slate-200 text-center shrink-0"
                  >
                    <div
                      class="w-36 h-36 border-2 border-dashed border-sky-400 rounded-lg flex flex-col items-center justify-center bg-sky-50/50 relative overflow-hidden"
                    >
                      <QrCode class="h-20 w-20 text-sky-600" />
                      <span
                        class="text-[10px] font-mono font-bold text-sky-800 mt-1 uppercase"
                        >VietQR 247</span
                      >
                      <div
                        class="absolute top-1 right-1 px-1 bg-emerald-500 text-[8px] font-bold text-white rounded"
                      >
                        Live
                      </div>
                    </div>
                    <span
                      class="block text-[10px] text-slate-500 mt-1 font-medium"
                      >{$language === "vi"
                        ? "Quét mã qua app ngân hàng"
                        : "Scan with banking app"}</span
                    >
                  </div>

                  <!-- Bank Info with Copy Buttons -->
                  <div class="space-y-2.5 flex-1 w-full text-xs">
                    <div
                      class="flex items-center justify-between pb-1.5 border-b border-slate-200 dark:border-slate-700"
                    >
                      <span class="text-slate-500"
                        >{$language === "vi"
                          ? "Ngân hàng thụ hưởng"
                          : "Beneficiary Bank"}:</span
                      >
                      <strong class="text-slate-900 dark:text-white"
                        >Techcombank (Hội sở chính)</strong
                      >
                    </div>

                    <div
                      class="flex items-center justify-between pb-1.5 border-b border-slate-200 dark:border-slate-700"
                    >
                      <span class="text-slate-500"
                        >{$language === "vi"
                          ? "Số tài khoản"
                          : "Account Number"}:</span
                      >
                      <div class="flex items-center space-x-2">
                        <strong
                          class="font-mono text-sm text-sky-600 dark:text-sky-400"
                          >9928 6688 0001</strong
                        >
                        <button
                          type="button"
                          class="p-1 hover:bg-slate-200 dark:hover:bg-slate-700 rounded text-slate-500"
                          onclick={() =>
                            copyToClipboard("992866880001", "Số tài khoản")}
                          title="Copy"
                        >
                          <Copy class="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </div>

                    <div
                      class="flex items-center justify-between pb-1.5 border-b border-slate-200 dark:border-slate-700"
                    >
                      <span class="text-slate-500"
                        >{$language === "vi"
                          ? "Tên thụ hưởng"
                          : "Account Name"}:</span
                      >
                      <strong
                        class="text-slate-900 dark:text-white font-mono uppercase"
                        >CTCP VIEN THONG NEXUS</strong
                      >
                    </div>

                    <div
                      class="flex items-center justify-between pb-1.5 border-b border-slate-200 dark:border-slate-700"
                    >
                      <span class="text-slate-500"
                        >{$language === "vi" ? "Số tiền" : "Amount"}:</span
                      >
                      <div class="flex items-center space-x-2">
                        <strong
                          class="font-mono text-sm text-emerald-600 dark:text-emerald-400"
                          >${modalCycleInfo.total.toFixed(2)}</strong
                        >
                        <button
                          type="button"
                          class="p-1 hover:bg-slate-200 dark:hover:bg-slate-700 rounded text-slate-500"
                          onclick={() =>
                            copyToClipboard(
                              modalCycleInfo.total.toFixed(2),
                              "Số tiền",
                            )}
                          title="Copy"
                        >
                          <Copy class="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </div>

                    <div class="flex items-center justify-between">
                      <span class="text-slate-500"
                        >{$language === "vi"
                          ? "Nội dung chuyển khoản"
                          : "Transfer Memo"}:</span
                      >
                      <div class="flex items-center space-x-2">
                        <strong
                          class="font-mono text-rose-600 dark:text-rose-400 font-bold"
                          >NEXUS {activePayModalBill.invoiceNumber}</strong
                        >
                        <button
                          type="button"
                          class="p-1 hover:bg-slate-200 dark:hover:bg-slate-700 rounded text-slate-500"
                          onclick={() =>
                            activePayModalBill &&
                            copyToClipboard(
                              `NEXUS ${activePayModalBill.invoiceNumber}`,
                              "Nội dung chuyển khoản",
                            )}
                          title="Copy"
                        >
                          <Copy class="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <div
                  class="text-[11px] text-slate-500 bg-sky-50/60 dark:bg-sky-950/20 p-2.5 rounded-lg flex items-center space-x-2"
                >
                  <Sparkles class="h-4 w-4 text-sky-500 shrink-0" />
                  <span
                    >{$language === "vi"
                      ? "Hệ thống tự động nhận diện và gạch nợ hóa đơn ngay trong 30 giây sau khi nhận được tiền."
                      : "Instant automated reconciliation: invoice marks as paid within 30s."}</span
                  >
                </div>
              </div>

              <!-- TAB CONTENT 2: CREDIT / DEBIT CARD -->
            {:else if chosenPaymentMethod === "Credit/Debit Card"}
              <div
                class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 space-y-4"
              >
                <div
                  class="flex items-center justify-between pb-2 border-b border-slate-200 dark:border-slate-700"
                >
                  <span
                    class="text-xs font-semibold text-slate-600 dark:text-slate-300"
                  >
                    {$language === "vi"
                      ? "Hỗ trợ thẻ quốc tế & nội địa Napas"
                      : "Supports Visa, MasterCard, JCB, Napas"}
                  </span>
                  <div class="flex space-x-1">
                    <span
                      class="px-1.5 py-0.5 bg-blue-600 text-white rounded text-[10px] font-bold"
                      >VISA</span
                    >
                    <span
                      class="px-1.5 py-0.5 bg-rose-600 text-white rounded text-[10px] font-bold"
                      >MC</span
                    >
                    <span
                      class="px-1.5 py-0.5 bg-emerald-600 text-white rounded text-[10px] font-bold"
                      >NAPAS</span
                    >
                  </div>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                  <div class="sm:col-span-2">
                    <label class="block text-slate-500 mb-1 font-semibold"
                      >{$language === "vi" ? "Số thẻ" : "Card Number"}</label
                    >
                    <input
                      type="text"
                      bind:value={cardNumber}
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg font-mono font-bold text-slate-900 dark:text-white"
                      placeholder="XXXX XXXX XXXX XXXX"
                    />
                  </div>
                  <div>
                    <label class="block text-slate-500 mb-1 font-semibold"
                      >{$language === "vi"
                        ? "Tên chủ thẻ"
                        : "Cardholder Name"}</label
                    >
                    <input
                      type="text"
                      bind:value={cardHolderName}
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg font-mono uppercase text-slate-900 dark:text-white"
                      placeholder="NGUYEN VAN A"
                    />
                  </div>
                  <div class="grid grid-cols-2 gap-2">
                    <div>
                      <label class="block text-slate-500 mb-1 font-semibold"
                        >{$language === "vi" ? "Hạn thẻ" : "Expiry"}</label
                      >
                      <input
                        type="text"
                        bind:value={cardExpiry}
                        class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg font-mono text-center text-slate-900 dark:text-white"
                        placeholder="MM/YY"
                      />
                    </div>
                    <div>
                      <label class="block text-slate-500 mb-1 font-semibold"
                        >CVV / CVC</label
                      >
                      <input
                        type="password"
                        maxlength="4"
                        bind:value={cardCvv}
                        class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg font-mono text-center text-slate-900 dark:text-white"
                        placeholder="•••"
                      />
                    </div>
                  </div>
                </div>
              </div>

              <!-- TAB CONTENT 3: E-WALLET -->
            {:else if chosenPaymentMethod === "UPI/Digital Wallet"}
              <div
                class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 space-y-4"
              >
                <div class="flex items-center space-x-3">
                  <button
                    type="button"
                    onclick={() => (selectedWallet = "vnpay")}
                    class="px-3 py-1.5 rounded-lg text-xs font-bold transition {selectedWallet ===
                    'vnpay'
                      ? 'bg-blue-600 text-white shadow'
                      : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700'}"
                  >
                    VNPay QR
                  </button>
                  <button
                    type="button"
                    onclick={() => (selectedWallet = "momo")}
                    class="px-3 py-1.5 rounded-lg text-xs font-bold transition {selectedWallet ===
                    'momo'
                      ? 'bg-pink-600 text-white shadow'
                      : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700'}"
                  >
                    MoMo
                  </button>
                  <button
                    type="button"
                    onclick={() => (selectedWallet = "zalopay")}
                    class="px-3 py-1.5 rounded-lg text-xs font-bold transition {selectedWallet ===
                    'zalopay'
                      ? 'bg-emerald-600 text-white shadow'
                      : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700'}"
                  >
                    ZaloPay
                  </button>
                </div>

                <div class="flex flex-col sm:flex-row items-center gap-4 pt-2">
                  <div
                    class="p-3 bg-white rounded-xl shadow-sm border border-slate-200 text-center shrink-0"
                  >
                    <div
                      class="w-32 h-32 border border-slate-200 rounded-lg flex flex-col items-center justify-center {selectedWallet ===
                      'momo'
                        ? 'bg-pink-50'
                        : selectedWallet === 'zalopay'
                          ? 'bg-emerald-50'
                          : 'bg-blue-50'}"
                    >
                      <Smartphone
                        class="h-14 w-14 {selectedWallet === 'momo'
                          ? 'text-pink-600'
                          : selectedWallet === 'zalopay'
                            ? 'text-emerald-600'
                            : 'text-blue-600'}"
                      />
                      <span
                        class="text-[10px] font-bold uppercase mt-1 {selectedWallet ===
                        'momo'
                          ? 'text-pink-700'
                          : selectedWallet === 'zalopay'
                            ? 'text-emerald-700'
                            : 'text-blue-700'}">{selectedWallet} Pay</span
                      >
                    </div>
                  </div>
                  <div
                    class="text-xs text-slate-600 dark:text-slate-300 space-y-1.5"
                  >
                    <p class="font-bold text-slate-900 dark:text-white">
                      {$language === "vi"
                        ? `Thanh toán qua ví điện tử ${selectedWallet.toUpperCase()}`
                        : `Pay with ${selectedWallet.toUpperCase()} E-Wallet`}
                    </p>
                    <p class="text-slate-500">
                      {$language === "vi"
                        ? "1. Mở ứng dụng ví trên điện thoại của bạn."
                        : "1. Open the wallet application on your smartphone."}
                    </p>
                    <p class="text-slate-500">
                      {$language === "vi"
                        ? '2. Chọn "Quét mã" và hướng camera vào mã QR hoặc bấm xác nhận.'
                        : '2. Select "Scan QR" and point camera or confirm payment.'}
                    </p>
                    <p class="font-semibold text-sky-600 dark:text-sky-400">
                      {$language === "vi"
                        ? `Số tiền: $${modalCycleInfo.total.toFixed(2)}`
                        : `Amount: $${modalCycleInfo.total.toFixed(2)}`}
                    </p>
                  </div>
                </div>
              </div>

              <!-- TAB CONTENT 4: CASH AT COUNTER -->
            {:else if chosenPaymentMethod === "Cash"}
              <div
                class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 space-y-3 text-xs"
              >
                <div
                  class="flex items-center space-x-2 text-amber-600 dark:text-amber-400 font-bold"
                >
                  <Store class="h-4 w-4" />
                  <span
                    >{$language === "vi"
                      ? "Thanh toán trực tiếp tại quầy giao dịch"
                      : "In-Person Counter Settlement"}</span
                  >
                </div>
                <p class="text-slate-600 dark:text-slate-300 leading-relaxed">
                  {$language === "vi"
                    ? `Quý khách có thể nộp tiền mặt trực tiếp tại bất kỳ phòng giao dịch nào của Nexus Telecom trên toàn quốc. Vui lòng cung cấp mã hóa đơn #${activePayModalBill.invoiceNumber} hoặc mã khách hàng ${activePayModalBill.accountId} cho thu ngân.`
                    : `You can settle in cash directly at any Nexus Telecom retail branch. Simply provide Invoice #${activePayModalBill.invoiceNumber} or Account ID ${activePayModalBill.accountId} to the service agent.`}
                </p>
                <div
                  class="p-3 bg-white dark:bg-slate-900 rounded-lg border border-slate-200 dark:border-slate-700 space-y-1"
                >
                  <div class="font-semibold text-slate-900 dark:text-white">
                    {$language === "vi"
                      ? "Điểm giao dịch đề xuất gần bạn:"
                      : "Suggested Nearest Branch:"}
                  </div>
                  <div class="text-slate-500">
                    Nexus Telecom Tower, 198 Tran Duy Hung, Cau Giay, Hanoi
                    (Hotline: 1900 6868)
                  </div>
                </div>
              </div>
            {/if}
          </div>

          <!-- Modal Footer -->
          <div
            class="px-6 py-4 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 flex items-center justify-between"
          >
            <button
              type="button"
              onclick={() => (activePayModalBill = null)}
              class="px-4 py-2 text-sm font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition"
              disabled={isProcessingPayment}
            >
              {$language === "vi" ? "Hủy bỏ" : "Cancel"}
            </button>

            <button
              type="button"
              onclick={handleConfirmPayment}
              disabled={isProcessingPayment}
              class="px-6 py-2.5 bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white rounded-xl text-sm font-bold transition shadow-md flex items-center space-x-2 disabled:opacity-50"
            >
              {#if isProcessingPayment}
                <RefreshCw class="h-4 w-4 animate-spin" />
                <span
                  >{$language === "vi"
                    ? "Đang xử lý..."
                    : "Processing..."}</span
                >
              {:else}
                <CheckCircle2 class="h-4 w-4" />
                <span>
                  {$language === "vi"
                    ? `Xác nhận thanh toán $${modalCycleInfo.total.toFixed(2)}`
                    : `Confirm Payment $${modalCycleInfo.total.toFixed(2)}`}
                </span>
              {/if}
            </button>
          </div>
        </div>
      </div>
    {/if}
  {:else if activeTab === "feedback"}
    <div class="space-y-6">
      <form
        onsubmit={submitFeedback}
        class="rounded-xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-sm p-6 space-y-4"
      >
        <h3
          class="font-bold text-base text-[#0F1D2B] dark:text-white flex items-center space-x-2"
        >
          <MessageSquare class="h-4 w-4 text-sky-600 dark:text-sky-400" />
          <span
            >{$language === "vi"
              ? "Gửi phản hồi về dịch vụ"
              : "Send service feedback"}</span
          >
        </h3>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <div
              class="text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5"
            >
              {$language === "vi" ? "Đánh giá" : "Rating"}
            </div>
            <div class="flex items-center space-x-1">
              {#each [1, 2, 3, 4, 5] as n (n)}
                <button
                  type="button"
                  onclick={() => (fbRating = n)}
                  class="p-1"
                  aria-label={`${n} star`}
                >
                  <Star
                    class="h-6 w-6 {n <= fbRating
                      ? 'text-amber-500 fill-amber-500'
                      : 'text-slate-300 dark:text-slate-600'}"
                  />
                </button>
              {/each}
            </div>
          </div>
          <div>
            <label
              for="fbCat"
              class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5"
            >
              {$language === "vi" ? "Hạng mục" : "Category"}
            </label>
            <select
              id="fbCat"
              bind:value={fbCategory}
              class="w-full px-3.5 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 text-[#0F1D2B] dark:text-white"
            >
              <option value="Service Quality"
                >{$language === "vi"
                  ? "Chất lượng dịch vụ"
                  : "Service Quality"}</option
              >
              <option value="Installation"
                >{$language === "vi" ? "Lắp đặt" : "Installation"}</option
              >
              <option value="Billing"
                >{$language === "vi" ? "Hóa đơn / Cước" : "Billing"}</option
              >
              <option value="Support"
                >{$language === "vi" ? "Hỗ trợ" : "Support"}</option
              >
              <option value="Other"
                >{$language === "vi" ? "Khác" : "Other"}</option
              >
            </select>
          </div>
        </div>

        <div>
          <label
            for="fbMsg"
            class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5"
          >
            {$language === "vi" ? "Nội dung" : "Message"}
          </label>
          <textarea
            id="fbMsg"
            rows="3"
            bind:value={fbMessage}
            class="w-full px-3.5 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 text-[#0F1D2B] dark:text-white"
          ></textarea>
        </div>

        <button
          type="submit"
          class="px-5 py-2.5 rounded-xl text-sm font-bold bg-sky-600 hover:bg-sky-700 text-white transition shadow"
        >
          {$language === "vi" ? "Gửi phản hồi" : "Submit feedback"}
        </button>
      </form>

      <div
        class="rounded-xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-sm p-6"
      >
        <h3 class="font-bold text-sm text-[#0F1D2B] dark:text-white mb-3">
          {$language === "vi" ? "Phản hồi đã gửi" : "Your feedback history"}
        </h3>
        {#if myFeedbacks.length > 0}
          <div class="divide-y divide-[#CCE4F7] dark:divide-[#253D56] text-xs">
            {#each myFeedbacks as f (f.id)}
              <div class="py-3 space-y-1">
                <div class="flex items-center justify-between">
                  <span class="font-semibold text-[#0F1D2B] dark:text-white">
                    {f.category} · {"★".repeat(f.rating)}{"☆".repeat(
                      5 - f.rating,
                    )}
                  </span>
                  <span class="text-slate-400">{f.createdAt}</span>
                </div>
                <p class="text-[#537292] dark:text-[#8DB0D4]">{f.message}</p>
                {#if f.response}
                  <p
                    class="mt-1 pl-3 border-l-2 border-sky-400 text-sky-700 dark:text-sky-300"
                  >
                    <strong
                      >{$language === "vi"
                        ? "Phản hồi từ Nexus"
                        : "Nexus response"}:</strong
                    >
                    {f.response}
                    <span class="text-slate-400"> — {f.respondedBy}</span>
                  </p>
                {/if}
              </div>
            {/each}
          </div>
        {:else}
          <p class="text-xs text-[#537292] dark:text-[#8DB0D4]">
            {$language === "vi"
              ? "Bạn chưa gửi phản hồi nào."
              : "You have not submitted any feedback yet."}
          </p>
        {/if}
      </div>
    </div>
  {:else if activeTab === "new-service"}
    <div class="space-y-6">
      <!-- Search & Category Filter Toolbar (giống role Admin) -->
      <div
        class="p-4 bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-2xl shadow-xs flex flex-col sm:flex-row items-center justify-between gap-3"
      >
        <div class="relative flex-1 w-full max-w-md">
          <Search class="absolute left-3.5 top-2.5 h-4 w-4 text-slate-400" />
          <input
            type="text"
            bind:value={newPlanSearch}
            placeholder={$language === "vi"
              ? "Tìm gói cước theo tên, tốc độ, mô tả..."
              : "Search plans by name, speed..."}
            class="w-full pl-10 pr-4 py-2 text-xs bg-slate-50 dark:bg-[#1A2C3F] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white placeholder-slate-400 focus:outline-hidden focus:ring-2 focus:ring-sky-500"
          />
        </div>

        <select
          bind:value={newOrderCategory}
          class="text-xs bg-slate-50 dark:bg-[#1A2C3F] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl px-3 py-2 text-[#0F1D2B] dark:text-white focus:outline-hidden focus:ring-2 focus:ring-sky-500 cursor-pointer min-w-[160px]"
        >
          <option value="all"
            >{$language === "vi" ? "Tất cả loại" : "All Types"}</option
          >
          <option value="Broadband"
            >{$language === "vi" ? "Cáp quang" : "Broadband"}</option
          >
          <option value="Dial-Up"
            >{$language === "vi" ? "Quay số" : "Dial-Up"}</option
          >
          <option value="Landline"
            >{$language === "vi" ? "Cố định" : "Landline"}</option
          >
        </select>
      </div>

      <!-- Placed Receipt Notification if any -->
      {#if newOrderPlacedReceipt}
        <div
          class="p-5 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 flex flex-col md:flex-row md:items-center justify-between gap-4 shadow-sm"
        >
          <div class="flex items-start space-x-3.5">
            <div
              class="h-10 w-10 rounded-xl bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0"
            >
              <CheckCircle2 class="h-6 w-6" />
            </div>
            <div>
              <div
                class="text-xs font-bold uppercase tracking-wider text-emerald-700 dark:text-emerald-300"
              >
                {$language === "vi"
                  ? "Đơn hàng mới đã được khởi tạo thành công!"
                  : "Order Placed Successfully!"}
              </div>
              <div
                class="text-base font-extrabold text-[#0F1D2B] dark:text-white mt-0.5"
              >
                {$language === "vi" ? "Mã đơn hàng:" : "Order ID:"}
                <span class="font-mono text-sky-600 dark:text-sky-400"
                  >#{newOrderPlacedReceipt.id}</span
                >
                <span class="text-xs font-medium text-slate-500 ml-2"
                  >({newOrderPlacedReceipt.planName})</span
                >
              </div>
              <p
                class="text-xs text-emerald-800 dark:text-emerald-300/90 mt-1 max-w-xl"
              >
                {$language === "vi"
                  ? "Trạng thái: Đang chờ Kỹ thuật viên chi nhánh đo kiểm hạ tầng (Feasibility Survey). Bạn có thể theo dõi tiến độ bằng mã đơn này."
                  : "Status: Awaiting branch Technical feasibility survey. You can track status with this Order ID."}
              </p>
            </div>
          </div>

          <div class="flex flex-wrap items-center gap-2 shrink-0">
            <button
              type="button"
              onclick={() => sendGmailNotification(newOrderPlacedReceipt!)}
              class="px-3.5 py-2 rounded-xl text-xs font-bold bg-white dark:bg-[#152434] text-sky-700 dark:text-sky-300 border border-sky-300 dark:border-sky-800 hover:bg-sky-50 dark:hover:bg-sky-950/50 transition flex items-center space-x-1.5 shadow-xs cursor-pointer"
            >
              <Mail class="h-4 w-4 text-sky-600" />
              <span
                >{$language === "vi"
                  ? "Thông báo đến Gmail"
                  : "Notify Gmail"}</span
              >
            </button>

            <button
              type="button"
              onclick={() => sendAccountNotification(newOrderPlacedReceipt!)}
              class="px-3.5 py-2 rounded-xl text-xs font-bold bg-white dark:bg-[#152434] text-indigo-700 dark:text-indigo-300 border border-indigo-300 dark:border-indigo-800 hover:bg-indigo-50 dark:hover:bg-indigo-950/50 transition flex items-center space-x-1.5 shadow-xs cursor-pointer"
            >
              <Bell class="h-4 w-4 text-indigo-600" />
              <span
                >{$language === "vi"
                  ? "Thông báo tới tài khoản"
                  : "Notify Account"}</span
              >
            </button>

            <button
              type="button"
              onclick={() =>
                copyToClipboard(newOrderPlacedReceipt!.id, "Mã đơn hàng")}
              class="px-3.5 py-2 rounded-xl text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white transition flex items-center space-x-1.5 shadow-xs cursor-pointer"
            >
              <Copy class="h-4 w-4" />
              <span>{$language === "vi" ? "Sao chép mã" : "Copy ID"}</span>
            </button>

            <button
              type="button"
              onclick={dismissOrderNotification}
              class="p-2 rounded-xl text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition cursor-pointer"
              title={$language === "vi" ? "Ẩn thông báo" : "Dismiss"}
            >
              <X class="h-4 w-4" />
            </button>
          </div>
        </div>
      {/if}

      <!-- Plans Grid with Pagination -->
      {#if paginatedNewPlans.length === 0}
        <div
          class="p-12 text-center bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-2xl text-slate-500 shadow-xs"
        >
          <ShoppingBag
            class="h-10 w-10 mx-auto text-slate-400 mb-2 opacity-50"
          />
          <p class="font-bold text-sm text-[#0F1D2B] dark:text-white">
            {$language === "vi"
              ? "Không tìm thấy gói cước nào phù hợp."
              : "No matching plans found."}
          </p>
          <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-1">
            {$language === "vi"
              ? "Hãy thử thay đổi từ khóa tìm kiếm hoặc chọn danh mục khác."
              : "Try adjusting your search keyword or selecting another category."}
          </p>
        </div>
      {:else}
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {#each paginatedNewPlans as p (p.id)}
            <div
              class="p-6 rounded-2xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-sm hover:shadow-md transition-all flex flex-col justify-between group"
            >
              <div>
                <div class="flex items-center justify-between mb-3">
                  <span
                    class="inline-flex items-center space-x-1 px-2.5 py-1 rounded-full text-[11px] font-bold {p.type ===
                    'Broadband'
                      ? 'bg-blue-100 text-blue-700 dark:bg-blue-950/50 dark:text-blue-300'
                      : p.type === 'Dial-Up'
                        ? 'bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300'
                        : 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300'}"
                  >
                    {#if p.type === "Broadband"}
                      <Wifi class="h-3 w-3" />
                    {:else if p.type === "Dial-Up"}
                      <Radio class="h-3 w-3" />
                    {:else}
                      <Phone class="h-3 w-3" />
                    {/if}
                    <span>{p.type}</span>
                  </span>
                  {#if p.isPopular}
                    <span
                      class="text-[10px] font-bold px-2 py-0.5 rounded bg-amber-500/15 text-amber-600 dark:text-amber-400"
                    >
                      {$language === "vi" ? "Phổ biến" : "Popular"}
                    </span>
                  {/if}
                </div>

                <h3
                  class="text-base font-bold text-[#0F1D2B] dark:text-white group-hover:text-sky-600 transition-colors"
                >
                  {p.name}
                </h3>
                <p
                  class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-1 line-clamp-2"
                >
                  {p.description || p.speedOrBandwidth}
                </p>

                <div
                  class="mt-4 pt-4 border-t border-[#CCE4F7]/60 dark:border-[#253D56]/60 flex items-baseline space-x-1"
                >
                  <span
                    class="text-2xl font-black font-mono text-[#0F1D2B] dark:text-white"
                  >
                    ${p.monthlyRental}
                  </span>
                  <span class="text-xs text-[#537292] dark:text-[#8DB0D4]">
                    /{(p.billingCycle ?? "Monthly").toLowerCase()}
                  </span>
                </div>

                <div
                  class="mt-3 space-y-1.5 text-xs text-slate-600 dark:text-slate-300"
                >
                  <div class="flex justify-between">
                    <span class="text-slate-400"
                      >{$language === "vi"
                        ? "Tiền cọc thiết bị:"
                        : "Security Deposit:"}</span
                    >
                    <span class="font-mono font-bold">${p.securityDeposit}</span
                    >
                  </div>
                  <div class="flex justify-between">
                    <span class="text-slate-400"
                      >{$language === "vi"
                        ? "Tốc độ / Băng thông:"
                        : "Speed:"}</span
                    >
                    <span class="font-semibold">{p.speedOrBandwidth}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-slate-400"
                      >{$language === "vi"
                        ? "Thời hạn cước:"
                        : "Validity:"}</span
                    >
                    <span>{p.validity}</span>
                  </div>
                </div>
              </div>

              <div
                class="mt-5 pt-4 border-t border-[#CCE4F7]/60 dark:border-[#253D56]/60"
              >
                <button
                  type="button"
                  onclick={() => handleOpenOrderModal(p)}
                  class="w-full py-2.5 px-4 rounded-xl text-xs font-bold bg-sky-600 hover:bg-sky-700 active:scale-98 text-white transition shadow-xs flex items-center justify-center space-x-1.5 cursor-pointer"
                >
                  <Plus class="h-3.5 w-3.5" />
                  <span
                    >{$language === "vi"
                      ? "Đăng ký gói này"
                      : "Subscribe Now"}</span
                  >
                </button>
              </div>
            </div>
          {/each}
        </div>

        <!-- Pagination Controls (Thu gọn, căn giữa) -->
        <div class="flex justify-center pt-2">
          <div
            class="inline-flex items-center gap-2.5 px-4 py-2 border border-[#CCE4F7] dark:border-[#253D56] bg-white dark:bg-[#152434] rounded-2xl shadow-xs text-sm"
          >
            <button
              disabled={newPlanCurrentPage === 1}
              onclick={() => newPlanCurrentPage--}
              class="px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-semibold text-xs cursor-pointer"
            >
              {$language === "vi" ? "Trước" : "Prev"}
            </button>
            <span
              class="px-3 py-1 text-xs font-bold text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 rounded-lg"
            >
              {newPlanCurrentPage} / {totalNewPlanPages}
            </span>
            <button
              disabled={newPlanCurrentPage >= totalNewPlanPages}
              onclick={() => newPlanCurrentPage++}
              class="px-3 py-1.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 font-semibold text-xs cursor-pointer"
            >
              {$language === "vi" ? "Sau" : "Next"}
            </button>
          </div>
        </div>
      {/if}

      <!-- Order Customization Modal -->
      {#if orderingPlan}
        <div
          class="fixed inset-0 z-50 bg-slate-950/50 backdrop-blur-sm flex items-center justify-center p-4"
        >
          <div
            class="bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] rounded-2xl max-w-lg w-full p-6 shadow-2xl space-y-4"
          >
            <div
              class="flex items-center justify-between pb-3 border-b border-[#CCE4F7] dark:border-[#253D56]"
            >
              <div>
                <h3 class="text-base font-bold text-[#0F1D2B] dark:text-white">
                  {$language === "vi"
                    ? "Xác nhận đăng ký gói cước"
                    : "Confirm Subscription"}
                </h3>
                <p
                  class="text-xs text-sky-600 dark:text-sky-400 font-semibold mt-0.5"
                >
                  {orderingPlan.name} ({orderingPlan.type} · ${orderingPlan.monthlyRental}/{orderingPlan.billingCycle})
                </p>
              </div>
              <button
                type="button"
                onclick={() => (orderingPlan = null)}
                class="h-8 w-8 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 flex items-center justify-center"
              >
                <X class="h-4 w-4" />
              </button>
            </div>

            <form
              onsubmit={handleConfirmNewServiceOrder}
              class="space-y-3.5 text-xs"
            >
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label
                    class="block font-semibold text-[#305070] dark:text-slate-300 mb-1"
                  >
                    {$language === "vi"
                      ? "Họ và tên khách hàng"
                      : "Customer Full Name"} *
                  </label>
                  <input
                    type="text"
                    bind:value={newOrderCustomerName}
                    required
                    placeholder="Nguyễn Văn A"
                    class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
                <div>
                  <label
                    class="block font-semibold text-[#305070] dark:text-slate-300 mb-1"
                  >
                    {$language === "vi" ? "Email liên hệ" : "Contact Email"}
                  </label>
                  <input
                    type="email"
                    pattern="[^\\s@]+@[^\\s@]+\\.[^\\s@]+"
                    title={$language === "vi"
                      ? "Email phải có dạng ten@mien.com"
                      : "Email must have the form name@domain.com"}
                    bind:value={newOrderCustomerEmail}
                    placeholder="email@example.com"
                    class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              <div class="mb-4">
                <label
                  class="block font-semibold text-[#305070] dark:text-slate-300 mb-1 flex justify-between items-end"
                >
                  <span
                    >{$language === "vi"
                      ? "Địa chỉ lắp đặt"
                      : "Installation Address"} *</span
                  >
                </label>
                <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-3">
                  <div class="relative">
                    <select
                      bind:value={newOrderProvince}
                      class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500 appearance-none"
                    >
                      <option value=""
                        >{$language === "vi"
                          ? "Tỉnh / Thành phố"
                          : "Province / City"}</option
                      >
                      {#each provincesList as p}
                        <option value={p.name}>{p.name}</option>
                      {/each}
                    </select>
                  </div>
                  <div class="relative">
                    <select
                      bind:value={newOrderDistrict}
                      disabled={!newOrderProvince}
                      class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500 appearance-none disabled:opacity-50"
                    >
                      <option value=""
                        >{$language === "vi"
                          ? "Quận / Huyện"
                          : "District"}</option
                      >
                      {#each districtsList as d}
                        <option value={d.name}>{d.name}</option>
                      {/each}
                    </select>
                  </div>
                  <div class="relative">
                    <select
                      bind:value={newOrderWard}
                      disabled={!newOrderDistrict}
                      class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500 appearance-none disabled:opacity-50"
                    >
                      <option value=""
                        >{$language === "vi" ? "Phường / Xã" : "Ward"}</option
                      >
                      {#each wardsList as w}
                        <option value={w.name}>{w.name}</option>
                      {/each}
                    </select>
                  </div>
                </div>
                <div class="relative">
                  <MapPin
                    class="absolute left-3.5 top-2.5 h-4 w-4 text-[#7899B8] dark:text-slate-500"
                  />
                  <input
                    type="text"
                    bind:value={newOrderSpecificAddress}
                    required
                    placeholder={$language === "vi"
                      ? "Địa chỉ cụ thể (số nhà, tên đường)"
                      : "Specific address (house no, street)"}
                    class="w-full pl-9 pr-10 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                  <button
                    type="button"
                    onclick={() => {
                      showMapModal = true;
                      mapSearchQuery = newOrderAddress;
                    }}
                    class="absolute right-1 top-1 px-2 py-1 bg-sky-600 dark:bg-sky-500 hover:bg-sky-700 dark:hover:bg-[#1C2C3D] text-white rounded-md transition cursor-pointer flex items-center space-x-1 shadow-sm text-[10px] font-semibold"
                    title={$language === "vi"
                      ? "Chọn trên bản đồ"
                      : "Pick on map"}
                  >
                    <Navigation class="h-3 w-3" />
                    <span>{$language === "vi" ? "Bản đồ" : "Map"}</span>
                  </button>
                </div>
              </div>

              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label
                    class="block font-semibold text-[#305070] dark:text-slate-300 mb-1"
                  >
                    {$language === "vi"
                      ? "Số điện thoại liên hệ"
                      : "Contact Phone"} *
                  </label>
                  <input
                    type="tel"
                    inputmode="numeric"
                    maxlength="10"
                    title={$language === "vi"
                      ? "Số điện thoại phải gồm đúng 10 chữ số"
                      : "Phone number must contain exactly 10 digits"}
                    bind:value={newOrderPhone}
                    required
                    class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>

                <div>
                  <label
                    class="block font-semibold text-[#305070] dark:text-slate-300 mb-1"
                  >
                    {$language === "vi"
                      ? "Chi nhánh xử lý"
                      : "Servicing Branch"} *
                  </label>
                  <select
                    bind:value={newOrderShopCode}
                    class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  >
                    {#each $retailShops as s (s.shopCode)}
                      <option value={s.shopCode}>{s.name} ({s.shopCode})</option
                      >
                    {/each}
                  </select>
                </div>
              </div>

              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label
                    class="block font-semibold text-[#305070] dark:text-slate-300 mb-1"
                  >
                    {$language === "vi" ? "Loại giấy tờ" : "ID Proof Type"}
                  </label>
                  <select
                    bind:value={newOrderIdProofType}
                    class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium"
                  >
                    <option value="National ID Card"
                      >{$language === "vi"
                        ? "Căn cước công dân (CCCD)"
                        : "National ID Card"}</option
                    >
                    <option value="Passport"
                      >{$language === "vi"
                        ? "Hộ chiếu (Passport)"
                        : "Passport"}</option
                    >
                    <option value="Driver's License"
                      >{$language === "vi"
                        ? "Bằng lái xe"
                        : "Driver's License"}</option
                    >
                  </select>
                </div>

                <div>
                  <label
                    class="block font-semibold text-[#305070] dark:text-slate-300 mb-1"
                  >
                    {$language === "vi"
                      ? "Số giấy tờ (CCCD/Passport)"
                      : "ID Proof Number"} *
                  </label>
                  <input
                    type="text"
                    inputmode={newOrderIdProofType === "Passport"
                      ? "text"
                      : "numeric"}
                    pattern={newOrderIdProofType === "National ID Card" ||
                    newOrderIdProofType === "Driver's License"
                      ? "[0-9]{12}"
                      : newOrderIdProofType === "Passport"
                        ? "[A-Za-z0-9]{7,9}"
                        : undefined}
                    maxlength={newOrderIdProofType === "National ID Card" ||
                    newOrderIdProofType === "Driver's License"
                      ? 12
                      : newOrderIdProofType === "Passport"
                        ? 9
                        : undefined}
                    title={newOrderIdProofType === "National ID Card"
                      ? $language === "vi"
                        ? "CCCD phải gồm đúng 12 chữ số"
                        : "National ID must contain exactly 12 digits"
                      : newOrderIdProofType === "Passport"
                        ? $language === "vi"
                          ? "Passport phải gồm 7-9 ký tự chữ và số"
                          : "Passport must be 7-9 alphanumeric characters"
                        : $language === "vi"
                          ? "Bằng lái xe phải gồm đúng 12 chữ số"
                          : "Driver's License must contain exactly 12 digits"}
                    bind:value={newOrderIdProofNumber}
                    required
                    class="w-full px-3 py-2 text-xs bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl text-[#0F1D2B] dark:text-white font-medium focus:outline-none focus:ring-2 focus:ring-sky-500"
                  />
                </div>
              </div>

              {#if orderingPlan.type === "Dial-Up"}
                <div
                  class="p-3 rounded-xl bg-amber-500/10 border border-amber-500/25 space-y-2"
                >
                  <label
                    class="flex items-center space-x-2 text-amber-800 dark:text-amber-300 font-medium cursor-pointer"
                  >
                    <input
                      type="checkbox"
                      bind:checked={newOrderHasLandline}
                      class="rounded text-amber-600 focus:ring-amber-500"
                    />
                    <span
                      >{$language === "vi"
                        ? "Tôi đã có đường thoại Nexus Landline sẵn tại địa chỉ này"
                        : "I already have an active Nexus Landline here"}</span
                    >
                  </label>
                  {#if newOrderHasLandline}
                    <p
                      class="text-[11px] text-amber-700 dark:text-amber-400 pl-5"
                    >
                      {$language === "vi"
                        ? `Sẽ liên kết với mã thuê bao hiện tại: ${accountId} (chỉ cần khảo sát tầng Internet).`
                        : `Will link to your existing Landline account: ${accountId} (only internet leg surveyed).`}
                    </p>
                  {/if}
                </div>
              {/if}

              <!-- Tùy chọn kênh gửi thông báo xác nhận -->
              <div
                class="p-3.5 rounded-xl bg-slate-50 dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] space-y-2.5"
              >
                <div
                  class="text-[11px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 flex items-center justify-between"
                >
                  <span
                    >{$language === "vi"
                      ? "Kênh gửi thông báo xác nhận"
                      : "Confirmation Channels"}</span
                  >
                  <span
                    class="text-[10px] text-sky-600 dark:text-sky-400 font-medium"
                    >{$language === "vi"
                      ? "Tự động gửi khi hoàn tất"
                      : "Auto-sent after order"}</span
                  >
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-0.5">
                  <!-- Thông báo đến Gmail -->
                  <label
                    class="flex items-start space-x-2.5 p-2 rounded-lg border transition cursor-pointer {newOrderNotifyGmail
                      ? 'bg-sky-50 dark:bg-sky-950/40 border-sky-300 dark:border-sky-800'
                      : 'bg-white dark:bg-[#152434] border-slate-200 dark:border-slate-800'}"
                  >
                    <input
                      type="checkbox"
                      bind:checked={newOrderNotifyGmail}
                      class="mt-0.5 rounded text-sky-600 focus:ring-sky-500 cursor-pointer"
                    />
                    <div class="flex-1 min-w-0">
                      <div
                        class="flex items-center space-x-1.5 text-xs font-bold text-[#0F1D2B] dark:text-white"
                      >
                        <Mail class="h-3.5 w-3.5 text-sky-600 shrink-0" />
                        <span
                          >{$language === "vi"
                            ? "Thông báo đến Gmail"
                            : "Notify via Gmail"}</span
                        >
                      </div>
                      <p
                        class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5 truncate"
                      >
                        {newOrderCustomerEmail ||
                          $currentUser?.email ||
                          "email@gmail.com"}
                      </p>
                    </div>
                  </label>

                  <!-- Thông báo tới tài khoản -->
                  <label
                    class="flex items-start space-x-2.5 p-2 rounded-lg border transition cursor-pointer {newOrderNotifyAccount
                      ? 'bg-indigo-50 dark:bg-indigo-950/40 border-indigo-300 dark:border-indigo-800'
                      : 'bg-white dark:bg-[#152434] border-slate-200 dark:border-slate-800'}"
                  >
                    <input
                      type="checkbox"
                      bind:checked={newOrderNotifyAccount}
                      class="mt-0.5 rounded text-indigo-600 focus:ring-indigo-500 cursor-pointer"
                    />
                    <div class="flex-1 min-w-0">
                      <div
                        class="flex items-center space-x-1.5 text-xs font-bold text-[#0F1D2B] dark:text-white"
                      >
                        <Bell class="h-3.5 w-3.5 text-indigo-600 shrink-0" />
                        <span
                          >{$language === "vi"
                            ? "Thông báo tới tài khoản"
                            : "Notify Account"}</span
                        >
                      </div>
                      <p
                        class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5"
                      >
                        {$language === "vi"
                          ? "Hiện trên Dashboard & Chuông"
                          : "Show on Dashboard & Bell"}
                      </p>
                    </div>
                  </label>
                </div>
              </div>

              <div
                class="pt-3 border-t border-[#CCE4F7] dark:border-[#253D56] flex items-center justify-end space-x-2.5"
              >
                <button
                  type="button"
                  onclick={() => (orderingPlan = null)}
                  class="px-4 py-2 rounded-xl font-semibold bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200"
                >
                  {$language === "vi" ? "Hủy" : "Cancel"}
                </button>
                <button
                  type="submit"
                  disabled={isPlacingNewOrder}
                  class="px-5 py-2 rounded-xl font-bold bg-sky-600 hover:bg-sky-700 text-white shadow-xs transition disabled:opacity-60 disabled:cursor-not-allowed"
                >
                  {isPlacingNewOrder
                    ? $language === "vi"
                      ? "Đang gửi đơn..."
                      : "Submitting..."
                    : $language === "vi"
                      ? "Xác nhận đặt đơn"
                      : "Submit Order"}
                </button>
              </div>
            </form>
          </div>
        </div>
      {/if}
    </div>
  {:else if activeTab === "livechat"}
    <div class="space-y-6">
      <div
        class="rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col h-[600px] overflow-hidden"
      >
        <div
          class="px-4 py-3 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between bg-slate-50 dark:bg-slate-950/50"
        >
          <div class="flex items-center gap-3">
            <div
              class="h-9 w-9 rounded-full bg-gradient-to-tr from-sky-600 to-blue-600 text-white flex items-center justify-center text-xs font-bold shrink-0"
            >
              AD
            </div>
            <div>
              <div class="font-semibold text-sm text-slate-900 dark:text-white">
                {$language === "vi"
                  ? "Hỗ trợ trực tuyến Nexus"
                  : "Nexus Live Support"}
              </div>
              <div class="text-[10px] text-slate-400 font-mono">
                {chatSessionId
                  ? chatSessionId
                  : $language === "vi"
                    ? "Đang kết nối..."
                    : "Connecting..."}
              </div>
            </div>
          </div>
          <span
            class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 uppercase"
          >
            {$language === "vi" ? "Đang hoạt động" : "Live"}
          </span>
        </div>

        <div
          bind:this={chatThreadRef}
          class="flex-1 overflow-y-auto px-4 py-4 space-y-3 bg-slate-50 dark:bg-slate-950"
        >
          {#if isChatLoading}
            <div class="text-center text-xs text-slate-400 py-8">
              {$language === "vi"
                ? "Đang tải hội thoại…"
                : "Loading conversation…"}
            </div>
          {:else if chatMessages.length === 0}
            <div class="text-center text-xs text-slate-400 py-8">
              {$language === "vi"
                ? "Xin chào! Bạn cần hỗ trợ gì không?"
                : "Hello! Do you need any assistance?"}
            </div>
          {:else}
            {#each chatMessages as msg (msg.id)}
              {#if msg.senderType === "guest"}
                <!-- Tin của khách (bên phải) -->
                <div class="flex justify-end">
                  <div class="max-w-[75%]">
                    <div
                      class="px-3.5 py-2.5 rounded-2xl rounded-br-md bg-sky-600 text-white text-[13px] leading-relaxed shadow-sm"
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
                <!-- Tin của Admin (bên trái) -->
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

        <div
          class="p-3 border-t border-slate-200 dark:border-slate-800 flex items-center gap-2 bg-white dark:bg-slate-900"
        >
          <input
            type="text"
            bind:value={chatDraft}
            onkeydown={handleChatKeyDown}
            placeholder={$language === "vi"
              ? "Nhập tin nhắn của bạn…"
              : "Type your message…"}
            maxlength="1000"
            class="flex-1 px-3.5 py-2.5 rounded-xl text-[13px] bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-sky-500"
          />
          <button
            type="button"
            onclick={sendUserMessage}
            disabled={isChatSending || !chatDraft.trim()}
            class="px-4 py-2.5 rounded-xl text-xs font-bold bg-sky-600 hover:bg-sky-700 text-white shadow-sm transition active:scale-95 disabled:opacity-40 disabled:pointer-events-none cursor-pointer flex items-center space-x-1"
          >
            <Send class="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  {:else if activeTab === "settings"}
    <SettingsView showBranchSwitcher={false} />
  {:else if activeTab === "profile"}
    <ProfileView />
  {/if}

  <!-- Map Modal for User Dashboard -->
  {#if showMapModal}
    <div
      class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm"
    >
      <div
        class="bg-white dark:bg-[#0B141E] w-full max-w-2xl rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-200"
      >
        <div
          class="p-4 border-b border-slate-200 dark:border-slate-800 flex justify-between items-center bg-slate-50 dark:bg-[#101C29]"
        >
          <h3
            class="font-bold text-slate-800 dark:text-white flex items-center space-x-2"
          >
            <Navigation class="h-4 w-4 text-sky-500" />
            <span
              >{$language === "vi"
                ? "Cài đặt địa chỉ lắp đặt"
                : "Set Installation Address"}</span
            >
          </h3>
          <button
            onclick={() => (showMapModal = false)}
            class="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-white transition cursor-pointer"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <div class="p-4 space-y-4">
          <div class="relative">
            <Search class="absolute left-3 top-3 h-4 w-4 text-slate-400" />
            <input
              type="text"
              bind:value={mapSearchQuery}
              placeholder={$language === "vi"
                ? "Tìm kiếm địa chỉ trên bản đồ..."
                : "Search address on map..."}
              class="w-full pl-9 pr-4 py-2.5 text-sm bg-slate-100 dark:bg-[#152333] border border-slate-200 dark:border-slate-700 rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 text-slate-800 dark:text-white transition"
            />
          </div>

          <div
            class="relative w-full h-[300px] sm:h-[400px] bg-slate-200 dark:bg-slate-800 rounded-xl overflow-hidden border border-slate-200 dark:border-slate-700"
          >
            <iframe
              title="Google Maps"
              width="100%"
              height="100%"
              style="border:0;"
              loading="lazy"
              src={`https://maps.google.com/maps?q=${encodeURIComponent(mapSearchQuery || "Hanoi, Vietnam")}&t=&z=15&ie=UTF8&iwloc=&output=embed`}
            ></iframe>
            <!-- Center Pin overlay -->
            <div
              class="absolute inset-0 pointer-events-none flex items-center justify-center"
            >
              <button
                type="button"
                onclick={() => {
                  if (mapSearchQuery.trim()) {
                    newOrderProvince = "";
                    newOrderDistrict = "";
                    newOrderWard = "";
                    newOrderSpecificAddress = mapSearchQuery;
                    showMapModal = false;
                    toast.success(
                      $language === "vi"
                        ? "Đã lấy vị trí thành công!"
                        : "Location retrieved successfully!",
                    );
                  } else {
                    toast.error(
                      $language === "vi"
                        ? "Vui lòng nhập địa chỉ vào ô tìm kiếm"
                        : "Please search for an address first",
                    );
                  }
                }}
                class="mb-8 relative flex flex-col items-center pointer-events-auto cursor-pointer group"
              >
                <div
                  class="bg-rose-500 text-white text-[10px] font-bold px-2 py-1 rounded shadow-md mb-1 group-hover:scale-110 transition-transform"
                >
                  {$language === "vi"
                    ? "Chọn vị trí này"
                    : "Pick this location"}
                </div>
                <MapPin
                  class="h-8 w-8 text-rose-500 drop-shadow-md group-hover:scale-110 transition-transform"
                />
              </button>
            </div>
          </div>
        </div>

        <div
          class="p-4 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-[#101C29] flex justify-end space-x-3"
        >
          <button
            onclick={() => (showMapModal = false)}
            class="px-4 py-2 text-sm font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-xl transition cursor-pointer"
          >
            {$language === "vi" ? "Huỷ" : "Cancel"}
          </button>
          <button
            onclick={() => {
              newOrderProvince = "";
              newOrderDistrict = "";
              newOrderWard = "";
              newOrderSpecificAddress = mapSearchQuery;
              showMapModal = false;
            }}
            disabled={!mapSearchQuery.trim()}
            class="px-5 py-2 text-sm font-bold text-white bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 rounded-xl shadow-md transition disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
          >
            {$language === "vi" ? "Xác nhận địa chỉ" : "Confirm Address"}
          </button>
        </div>
      </div>
    </div>
  {/if}
</DashboardLayout>

<!-- POPUP MODAL: CHI TIẾT BẢN GHI KIỂM TRA KẾT NỐI MẠNG CỦA KHÁCH HÀNG -->
<ConnectionTestModal
  isOpen={isTestRecordsModalOpen}
  {connection}
  connectionsList={connection ? [connection] : $connections}
  onClose={() => {
    isTestRecordsModalOpen = false;
  }}
/>
