<script lang="ts">
  // Mirrors pages/TechnicalDashboard.tsx of the React original.
  import { onMount } from "svelte";
  import { nexusStore } from "../context/NexusContext";
  import { languageStore } from "../context/LanguageContext";
  import DashboardLayout from "../components/layout/DashboardLayout.svelte";
  import type { NavItem } from "../components/layout/DashboardLayout.svelte";
  import {
    Search,
    Zap,
    Activity,
    Radio,
    HardDrive,
    Plus,
    Power,
    ShieldAlert,
    X,
    Copy,
    Settings,
    ChevronDown,
    ChevronUp,
    Wifi,
    Phone,
    RefreshCw,
    CheckCircle2,
    AlertTriangle,
    XCircle,
    Server,
    Eye,
    Filter,
    Clock,
    ArrowRight,
    ExternalLink,
    FileText,
    ShieldCheck,
    Wrench,
    CalendarClock,
    History,
    PackageOpen,
  } from "lucide-svelte";
  import type {
    Connection,
    Order,
    ConnectionStatus,
    Equipment,
    OrderStatus,
  } from "../types/nexus";
  import { isReleasedToTechnical } from "../types/nexus";
  import { toast } from "svelte-sonner";
  import SettingsView from "../components/common/SettingsView.svelte";
  import ProfileView from "../components/common/ProfileView.svelte";
  import ConnectionTestModal from "../components/modals/ConnectionTestModal.svelte";
  import { queryParam, activeTabOverride } from "../lib/router";
  import { getPlanName } from "../lib/planI18n";
  import {
    TechnicalApiError,
    type ConnectionActivityLog,
    adjustInventoryStock,
    changeConnectionStatus,
    createEquipment,
    getConnectionActivityLogs,
    loadTechnicalWorkspace,
    provideConnection,
    updateEquipment,
    updateFeasibility,
    updateInstallationSchedule,
  } from "../lib/technicalApi";

  type TechTab =
    | "feasibility-queue"
    | "connection-manager"
    | "equipment-tracker"
    | "network-diagnostics"
    | "settings"
    | "profile";

  const { orders, connections, equipments, inventory, employees, updateOrderStatus, addEquipment } = nexusStore;
  const { t, language } = languageStore;

  let isApiLoading = $state(true);
  let apiError = $state("");
  let pendingAction = $state("");
  let hasLoadedApiData = $state(false);

  const technicalEmployeeId = $derived(
    $employees.find(
      (employee) =>
        employee.status === "Active" && employee.role === "Field Engineer",
    )?.id ?? "emp-03",
  );

  const errorMessage = (error: unknown) =>
    error instanceof TechnicalApiError || error instanceof Error
      ? error.message
      : "Không thể kết nối tới API kỹ thuật";

  const refreshTechnicalData = async () => {
    isApiLoading = true;
    try {
      const workspace = await loadTechnicalWorkspace();
      orders.set(workspace.orders);
      connections.set(workspace.connections);
      equipments.set(workspace.equipments);
      inventory.set(workspace.inventory);
      apiError = "";
      hasLoadedApiData = true;

      if (selectedConnection) {
        selectedConnection =
          workspace.connections.find(
            (connection) =>
              connection.accountId === selectedConnection?.accountId,
          ) ??
          workspace.connections[0] ??
          null;
      } else {
        selectedConnection = workspace.connections[0] ?? null;
      }

      return workspace;
    } catch (error) {
      apiError = errorMessage(error);
      if (!hasLoadedApiData) {
        orders.set([]);
        connections.set([]);
        equipments.set([]);
        inventory.set([]);
        selectedConnection = null;
      }
      throw error;
    } finally {
      isApiLoading = false;
    }
  };

  onMount(() => {
    refreshTechnicalData().catch((error) => {
      toast.error(errorMessage(error));
    });
  });

  // Active technical sidebar tab
  let activeTab = $state<TechTab>("feasibility-queue");

  // Reactively respond to tab overrides from router / notifications
  $effect(() => {
    const override = $activeTabOverride;
    const validTabs: TechTab[] = [
      "feasibility-queue",
      "connection-manager",
      "equipment-tracker",
      "settings",
      "profile",
    ];
    if (override && override.path === "/technical") {
      if (validTabs.includes(override.tab as TechTab)) {
        activeTab = override.tab as TechTab;
      }
    } else {
      const qTab = queryParam("tab");
      if (qTab && validTabs.includes(qTab as TechTab)) {
        activeTab = qTab as TechTab;
      }
    }
  });

  // Filter for Feasibility Queue
  let queueStatusFilter = $state<"All" | OrderStatus>("All");
  let queueSearch = $state("");

  // PROVISIONING MODAL STATE ("Connection Provided")
  let isProvisionModalOpen = $state(false);
  let targetOrderForProvision = $state<Order | null>(null);
  let selectedDeviceSerial = $state("");

  // NOT FEASIBLE MODAL STATE
  let isNotFeasibleModalOpen = $state(false);
  let targetOrderForRejection = $state<Order | null>(null);
  let rejectionReason = $state(
    "Distance to DP box exceeds standard copper/fiber specifications (> 850m). Excessive attenuation.",
  );

  // CONNECTION MANAGER STATE (16-char Account ID Search, List & Dropdown Details)
  let techAccountSearch = $state("");
  let connStatusFilter = $state<"All" | ConnectionStatus>("All");
  let connTypeFilter = $state<"All" | string>("All");
  let expandedAccountId = $state<string | null>("T064-000000000001");
  let selectedConnection = $state<Connection | null>(
    $connections.find((c) => c.accountId === "T064-000000000001") ||
      $connections[0] ||
      null,
  );
  let statusChangeReason = $state("");
  let testingPingAccountId = $state<string | null>(null);
  let activityLogs = $state<Record<string, ConnectionActivityLog[]>>({});
  let activityLogsLoading = $state<string | null>(null);

  // INSTALLATION SCHEDULE STATE
  let isScheduleModalOpen = $state(false);
  let targetOrderForSchedule = $state<Order | null>(null);
  let scheduledInstallDate = $state("");

  // INVENTORY ADJUSTMENT STATE
  let isStockModalOpen = $state(false);
  let targetInventoryId = $state("");
  let stockQuantityChange = $state(1);
  let stockAdjustmentReason = $state("");

  // Derived filtered connections for tech management list
  const filteredConnections = $derived(
    $connections.filter((c) => {
      const q = techAccountSearch.trim().toLowerCase();
      const matchesSearch =
        !q ||
        c.accountId.toLowerCase().includes(q) ||
        c.accountId
          .replace(/-/g, "")
          .toLowerCase()
          .includes(q.replace(/-/g, "")) ||
        c.customerName.toLowerCase().includes(q) ||
        c.installationAddress.toLowerCase().includes(q) ||
        (c.assignedDeviceSerial &&
          c.assignedDeviceSerial.toLowerCase().includes(q)) ||
        (c.portNumber && c.portNumber.toLowerCase().includes(q));
      const matchesStatus =
        connStatusFilter === "All" || c.status === connStatusFilter;
      const matchesType =
        connTypeFilter === "All" || c.connectionType === connTypeFilter;
      return matchesSearch && matchesStatus && matchesType;
    }),
  );

  const loadActivityLogs = async (accountId: string) => {
    activityLogsLoading = accountId;
    try {
      const logs = await getConnectionActivityLogs(accountId);
      activityLogs = { ...activityLogs, [accountId]: logs };
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      activityLogsLoading = null;
    }
  };

  const toggleDetailDropdown = (accountId: string) => {
    if (expandedAccountId === accountId) {
      expandedAccountId = null;
    } else {
      expandedAccountId = accountId;
      const found = $connections.find((c) => c.accountId === accountId);
      if (found) {
        selectedConnection = found;
      }
      loadActivityLogs(accountId);
    }
  };

  const handleRunPingTest = (accountId: string) => {
    testingPingAccountId = accountId;
    setTimeout(() => {
      testingPingAccountId = null;
      toast.success(
        $language === "vi"
          ? `Đo kiểm vòng lặp (Loopback Test) mạch #${accountId} hoàn tất: Độ trễ 4.2ms, Tín hiệu tốt -16.8 dBm, 0% mất gói.`
          : `Loopback test for circuit #${accountId} passed: 4.2ms latency, Attenuation -16.8 dBm, 0% packet loss.`,
      );
    }, 900);
  };

  // Dial-Up feasibility has TWO legs (landline + internet). Track which the
  // engineer has cleared per order until both pass (or the landline leg is
  // waived because the customer already holds a Nexus landline).
  let legChecks = $state<
    Record<string, { landline: boolean; internet: boolean }>
  >({});
  const legFor = (o: Order) =>
    legChecks[o.id] ?? { landline: false, internet: false };
  const toggleLeg = (o: Order, leg: "landline" | "internet") => {
    const cur = legFor(o);
    legChecks = { ...legChecks, [o.id]: { ...cur, [leg]: !cur[leg] } };
  };
  // For a Dial-Up order, is the landline leg satisfied (checked or waived)?
  const landlineLegDone = (o: Order) =>
    !!o.existingLandlineAccountId || legFor(o).landline;

  // EQUIPMENT TRACKER STATE (Log new modems/routers)
  let isAddEquipmentModalOpen = $state(false);
  let newEquipmentForm = $state({
    inventoryId: "",
    storeId: "",
    serialNumber: "",
    macAddress: "",
    deviceModel: "Nexus Wi-Fi 6 AX3000 Dual-Band Router",
    deviceType: "Gigabit Router" as Equipment["deviceType"],
    firmwareVersion: "v3.4.1-BUILD-88",
    status: "In Stock" as Equipment["status"],
  });

  // Available in-stock equipments for assignment
  const inStockEquipments = $derived(
    $equipments.filter((eq) => eq.status === "In Stock"),
  );

  const provisioningEquipments = $derived(
    targetOrderForProvision
      ? inStockEquipments.filter(
          (equipment) =>
            !equipment.storeId ||
            equipment.storeId === targetOrderForProvision?.retailOutletCode,
        )
      : inStockEquipments,
  );

  const availableStoreIds = $derived(
    [
      ...new Set(
        $orders.map((order) => order.retailOutletCode).filter(Boolean),
      ),
    ].sort(),
  );

  // ACTION HANDLERS: ORDER FEASIBILITY QUEUE
  const handleMarkFeasible = async (order: Order) => {
    // Dial-Up needs BOTH legs; other types need the single internet check.
    if (order.connectionType === "Dial-Up") {
      const legs = legFor(order);
      const landlineOk = landlineLegDone(order);
      if (!landlineOk || !legs.internet) {
        toast.error(
          `Dial-Up feasibility incomplete — ${!landlineOk ? "landline leg" : ""}${!landlineOk && !legs.internet ? " & " : ""}${!legs.internet ? "internet leg" : ""} still pending.`,
        );
        return;
      }
    }

    const note =
      order.connectionType === "Dial-Up"
        ? order.existingLandlineAccountId
          ? `Customer holds Nexus landline ${order.existingLandlineAccountId}; landline leg waived. Internet (DSLAM port) leg verified OK.`
          : "Both legs verified: landline loop tested OK and internet DSLAM port available. Attenuation -16.5 dBm."
        : "Field inspection verified: line loop within 250m, attenuation -16.5 dBm. DP Box capacity confirmed OK.";

    // Confirming feasibility is what issues the customer's 16-char Account ID.
    // updateOrderStatus là hàm async — phải await, nếu không updated là Promise
    // và nhánh kiểm tra !updated luôn sai.
    const updated = await updateOrderStatus(
      order.id,
      "Feasible",
      note,
      undefined,
      undefined,
      undefined,
      order.connectionType === "Dial-Up"
        ? { landline: landlineLegDone(order), internet: legFor(order).internet }
        : undefined,
    );
    if (!updated) {
      toast.error(
        $language === "vi"
          ? "Từ chối: đơn chưa được bán hàng chi nhánh duyệt hồ sơ."
          : "Blocked: the branch's retail staff has not approved this order yet.",
      );
      return;
    }
    toast.success(
      updated?.assignedAccountId
        ? `Order ${order.id} marked as FEASIBLE. Account ID issued: ${updated.assignedAccountId}`
        : `Order ${order.id} marked as FEASIBLE. Ready for dispatch.`,
    );
  };

  const handleOpenNotFeasibleModal = (order: Order) => {
    targetOrderForRejection = order;
    isNotFeasibleModalOpen = true;
  };

  const handleConfirmNotFeasible = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!targetOrderForRejection) return;

    const rejected = updateOrderStatus(
      targetOrderForRejection.id,
      "Not Feasible",
      rejectionReason,
    );
    if (!rejected) {
      toast.error(
        $language === "vi"
          ? "Từ chối: đơn chưa được bán hàng chi nhánh duyệt hồ sơ."
          : "Blocked: the branch's retail staff has not approved this order yet.",
      );
      return;
    }
    toast.error(`Order ${targetOrderForRejection.id} flagged as NOT FEASIBLE.`);
    isNotFeasibleModalOpen = false;
    targetOrderForRejection = null;
  };

  // Tracking connection progress per order
  const getOrderProvisionedConnections = (orderId: string) => {
    return $connections.filter((c) => c.orderId === orderId);
  };

  const provisionedForCurrentOrder = $derived(
    targetOrderForProvision
      ? $connections.filter((c) => c.orderId === targetOrderForProvision!.id)
      : [],
  );

  const targetOrderTotalConns = $derived(
    targetOrderForProvision
      ? Math.max(1, targetOrderForProvision.bulkConnectionsCount || 1)
      : 1,
  );

  const currentProvisioningIndex = $derived(
    provisionedForCurrentOrder.length + 1,
  );

  const handleOpenProvisionModal = (order: Order) => {
    targetOrderForProvision = order;
    const available = inStockEquipments.filter(
      (equipment) =>
        !equipment.storeId || equipment.storeId === order.retailOutletCode,
    );
    if (available.length > 0) {
      selectedDeviceSerial = available[0].serialNumber;
    } else {
      selectedDeviceSerial = "";
    }
    isProvisionModalOpen = true;
  };

  const handleConfirmConnectionProvided = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!targetOrderForProvision) return;

    if (!selectedDeviceSerial) {
      toast.error(
        $language === "vi"
          ? "Vui lòng chọn 1 thiết bị router/modem từ kho cho kết nối này."
          : "Please assign a modem or router serial number from inventory.",
      );
      return;
    }

    const currentIdx = provisionedForCurrentOrder.length + 1;
    const chosenSerial = selectedDeviceSerial;
    const chosenEquipment = $equipments.find(
      (equipment) => equipment.serialNumber === chosenSerial,
    );
    if (!chosenEquipment) {
      toast.error("Không tìm thấy thiết bị đã chọn trong dữ liệu API.");
      return;
    }

    if (pendingAction) return;
    pendingAction = `provision:${targetOrderForProvision.id}`;
    try {
      const result = await provideConnection(targetOrderForProvision.id, {
        equipmentId: chosenEquipment.id,
        portNumber: `ETH-PORT-${currentIdx}`,
        performedBy: technicalEmployeeId,
      });
      const workspace = await refreshTechnicalData();
      const createdConn = workspace.connections.find(
        (connection) => connection.accountId === result.accountId,
      );

      toast.success(
        $language === "vi"
          ? `Đã duyệt & cấp thành công Kết nối ${result.provisionedConnections}/${result.requiredConnections}! Account ID: ${result.accountId} (Router: ${chosenSerial})`
          : `Connection ${result.provisionedConnections}/${result.requiredConnections} provisioned! Account ID: ${result.accountId} with device ${chosenSerial}`,
      );
      // Auto focus Connection Manager to this account
      selectedConnection = createdConn ?? null;
      techAccountSearch = result.accountId;
      expandedAccountId = result.accountId;

      // If more connections need provisioning for this order:
      if (result.provisionedConnections < result.requiredConnections) {
        targetOrderForProvision =
          workspace.orders.find((item) => item.id === result.orderId) ??
          targetOrderForProvision;
        const remainingStock = workspace.equipments.filter(
          (equipment) =>
            equipment.status === "In Stock" &&
            equipment.serialNumber !== chosenSerial,
        );
        selectedDeviceSerial =
          remainingStock.length > 0 ? remainingStock[0].serialNumber : "";
        return;
      } else {
        toast.success(
          $language === "vi"
            ? `Đơn hàng #${result.orderId} đã hoàn tất đủ ${result.requiredConnections}/${result.requiredConnections} kết nối!`
            : `Order #${result.orderId} is fully provisioned with ${result.requiredConnections}/${result.requiredConnections} connections!`,
        );
      }

      isProvisionModalOpen = false;
      targetOrderForProvision = null;
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      pendingAction = "";
    }
  };

  const toLocalDateTimeInput = (date: Date) => {
    const offset = date.getTimezoneOffset() * 60_000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  };

  const handleOpenScheduleModal = (order: Order) => {
    targetOrderForSchedule = order;
    scheduledInstallDate = order.scheduledInstallDate
      ? toLocalDateTimeInput(new Date(order.scheduledInstallDate))
      : toLocalDateTimeInput(new Date(Date.now() + 24 * 60 * 60 * 1000));
    isScheduleModalOpen = true;
  };

  const handleSaveSchedule = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!targetOrderForSchedule || !scheduledInstallDate || pendingAction)
      return;

    pendingAction = `schedule:${targetOrderForSchedule.id}`;
    try {
      const result = await updateInstallationSchedule(
        targetOrderForSchedule.id,
        {
          scheduledInstallDate,
          technicianId: technicalEmployeeId,
        },
      );
      await refreshTechnicalData();
      toast.success(
        $language === "vi"
          ? `Đã lên lịch lắp đặt cho đơn ${result.orderId}.`
          : `Installation scheduled for order ${result.orderId}.`,
      );
      isScheduleModalOpen = false;
      targetOrderForSchedule = null;
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      pendingAction = "";
    }
  };

  // ACTION HANDLER: CONNECTION STATUS 3-WAY TOGGLE
  const handleToggleConnectionStatusFor = async (
    conn: Connection,
    newStatus: ConnectionStatus,
  ) => {
    const reason =
      statusChangeReason.trim() ||
      ($language === "vi"
        ? `Kỹ thuật viên can thiệp thủ công chuyển trạng thái sang ${newStatus} qua Bảng điều khiển Kỹ thuật.`
        : `Manual technician override to ${newStatus} via Technical Command Console.`);

    if (pendingAction) return;
    pendingAction = `connection-status:${conn.accountId}`;

    try {
      await changeConnectionStatus(conn.accountId, {
        status: newStatus,
        reason,
        performedBy: technicalEmployeeId,
      });
      const workspace = await refreshTechnicalData();
      await loadActivityLogs(conn.accountId);
      selectedConnection =
        workspace.connections.find(
          (connection) => connection.accountId === conn.accountId,
        ) ?? null;

      const statusVi =
        newStatus === "Active"
          ? "Hoạt động bình thường"
          : newStatus === "Temporarily Inactive"
            ? "Tạm ngưng dịch vụ"
            : "Ngắt kết nối vĩnh viễn";

      toast.success(
        $language === "vi"
          ? `Đã cập nhật trạng thái tài khoản ${conn.accountId} thành: ${statusVi}`
          : `Account ${conn.accountId} status updated to ${newStatus}`,
      );
      statusChangeReason = "";
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      pendingAction = "";
    }
  };

  const handleToggleConnectionStatus = (newStatus: ConnectionStatus) => {
    if (selectedConnection) {
      handleToggleConnectionStatusFor(selectedConnection, newStatus);
    }
  };

  // ACTION HANDLER: REGISTER NEW EQUIPMENT
  let isSavingEquipment = $state(false);
  const handleSaveEquipment = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!newEquipmentForm.serialNumber || !newEquipmentForm.macAddress) {
      toast.error(
        $language === "vi"
          ? "Vui lòng nhập số Serial và địa chỉ MAC."
          : "Serial number and MAC address are required.",
      );
      return;
    }

    isSavingEquipment = true;
    try {
      await addEquipment({ ...newEquipmentForm });
      toast.success(
        $language === "vi"
          ? `Thiết bị ${newEquipmentForm.serialNumber} đã được lưu vào CSDL thành công!`
          : `Equipment ${newEquipmentForm.serialNumber} saved to database successfully!`,
      );
      isAddEquipmentModalOpen = false;
      newEquipmentForm = {
        inventoryId: "",
        storeId: "",
        serialNumber: "",
        macAddress: "",
        deviceModel: "Nexus Wi-Fi 6 AX3000 Dual-Band Router",
        deviceType: "Gigabit Router",
        firmwareVersion: "v3.4.1-BUILD-88",
        status: "In Stock",
      };
    } catch (err: any) {
      toast.error(
        err.message ||
          ($language === "vi"
            ? "Lỗi khi lưu thiết bị vào CSDL."
            : "Error saving equipment."),
      );
    } finally {
      isSavingEquipment = false;
    }
  };

  const openAddEquipmentModal = () => {
    newEquipmentForm = {
      inventoryId: $inventory.find((item) => item.vendorId)?.id ?? "",
      storeId: availableStoreIds[0] ?? "",
      serialNumber: `NX-HW-${Math.floor(100000 + Math.random() * 900000)}`,
      macAddress: `${Math.floor(10 + Math.random() * 89)
        .toString(16)
        .toUpperCase()}:${Math.floor(10 + Math.random() * 89)
        .toString(16)
        .toUpperCase()}:${Math.floor(10 + Math.random() * 89)
        .toString(16)
        .toUpperCase()}:44:8E:01`,
      deviceModel: "Nexus Wi-Fi 6 AX3000 Dual-Band Router",
      deviceType: "Gigabit Router",
      firmwareVersion: "v3.4.1-BUILD-88",
      status: "In Stock",
    };
    isAddEquipmentModalOpen = true;
  };

  const handleEquipmentStatusChange = async (
    equipment: Equipment,
    status: Equipment["status"],
  ) => {
    if (equipment.status === status || pendingAction) return;
    pendingAction = `equipment:${equipment.id}`;
    try {
      await updateEquipment(equipment.id, { status });
      await refreshTechnicalData();
      toast.success(
        $language === "vi"
          ? `Đã cập nhật trạng thái thiết bị ${equipment.serialNumber}.`
          : `Equipment ${equipment.serialNumber} status updated.`,
      );
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      pendingAction = "";
    }
  };

  const openStockModal = (inventoryId: string, quantityChange: number) => {
    targetInventoryId = inventoryId;
    stockQuantityChange = quantityChange;
    stockAdjustmentReason =
      quantityChange > 0 ? "Nhập thêm vật tư kỹ thuật" : "Xuất vật tư kỹ thuật";
    isStockModalOpen = true;
  };

  const handleAdjustStock = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!targetInventoryId || stockQuantityChange === 0 || pendingAction)
      return;
    pendingAction = `inventory:${targetInventoryId}`;
    try {
      const result = await adjustInventoryStock(targetInventoryId, {
        quantityChange: stockQuantityChange,
        reason: stockAdjustmentReason,
      });
      await refreshTechnicalData();
      toast.success(
        $language === "vi"
          ? `Tồn kho mới: ${result.stockQuantity}.`
          : `New stock quantity: ${result.stockQuantity}.`,
      );
      isStockModalOpen = false;
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      pendingAction = "";
    }
  };

  const copyToClipboard = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    toast.success(`Copied ${label}: ${text}`);
  };

  const queryTelemetry = () => {
    const q = techAccountSearch.trim().replace(/-/g, "").toLowerCase();
    if (!q) {
      toast.info(
        $language === "vi"
          ? "Vui lòng nhập Mã tài khoản hoặc tên khách hàng cần tra cứu."
          : "Please enter an Account ID or subscriber name.",
      );
      return;
    }
    const found = $connections.find(
      (c) =>
        c.accountId.replace(/-/g, "").toLowerCase().includes(q) ||
        c.customerName
          .toLowerCase()
          .includes(techAccountSearch.trim().toLowerCase()),
    );
    if (found) {
      selectedConnection = found;
      expandedAccountId = found.accountId;
      loadActivityLogs(found.accountId);
      toast.success(
        $language === "vi"
          ? `Đã mở chi tiết đo kiểm mạch thuê bao #${found.accountId}`
          : `Connected to circuit #${found.accountId}`,
      );
    } else {
      toast.error(
        $language === "vi"
          ? "Không tìm thấy Mã tài khoản trong hệ thống đo kiểm kết nối."
          : "Account ID not found in connection telemetry registry.",
      );
    }
  };

  // STAGE 3 queue: only applications the branch retail desk already cleared may be
  // surveyed here. 'PendingRetail' / 'Not Approved' stay invisible to Technical,
  // which enforces the two-stage rule (Khách -> Bán hàng chi nhánh -> Kỹ thuật).
  const filteredOrders = $derived(
    $orders.filter((o) => {
      if (!isReleasedToTechnical(o.status)) return false;
      const matchesStatus =
        queueStatusFilter === "All" || o.status === queueStatusFilter;
      const matchesSearch =
        o.id.toLowerCase().includes(queueSearch.toLowerCase()) ||
        o.customerName.toLowerCase().includes(queueSearch.toLowerCase()) ||
        o.installationAddress.toLowerCase().includes(queueSearch.toLowerCase());
      return matchesStatus && matchesSearch;
    }),
  );

  // State for Customer Network Connection Diagnostics Modal
  let isTestRecordsModalOpen = $state(false);
  let targetConnectionForTestModal = $state<Connection | null>(null);

  function openTestRecordsModal(conn?: Connection | null) {
    targetConnectionForTestModal = conn || null;
    isTestRecordsModalOpen = true;
  }

  const technicalNavItems: NavItem[] = $derived([
    {
      id: "feasibility-queue",
      label: $t.techNav.feasibilityQueue,
      icon: Activity,
      badge: $orders.filter((o) => o.status === "Pending").length,
      badgeColor: "bg-amber-100 text-amber-900",
    },
    {
      id: "connection-manager",
      label: $t.techNav.connectionManager,
      icon: Radio,
      badge: $connections.length,
    },
    {
      id: "equipment-tracker",
      label: $t.techNav.equipmentTracker,
      icon: HardDrive,
      badge: $equipments.length,
    },
    {
      id: "network-diagnostics",
      label: $language === "vi" ? "Các bản ghi đo kiểm mạng" : "Network Test Records",
      icon: Activity,
      badge: $connections.length,
      badgeColor: "bg-sky-100 dark:bg-sky-950 text-sky-800 dark:text-sky-300",
    },
    { id: "settings", label: $t.techNav.settings, icon: Settings },
  ]);
</script>

<DashboardLayout
  {activeTab}
  onTabChange={(tab) => {
    if (tab === "network-diagnostics") {
      openTestRecordsModal(null);
      return;
    }
    activeTab = tab as TechTab;
    isProvisionModalOpen = false;
    isNotFeasibleModalOpen = false;
    isAddEquipmentModalOpen = false;
    expandedAccountId = null;
    selectedConnection = null;
  }}
  navItems={technicalNavItems}
  roleBadgeTitle={$t.roles.technical}
  pageTitle={technicalNavItems.find((n) => n.id === activeTab)?.label}
  primaryAction={activeTab === "equipment-tracker"
    ? {
        label: $language === "vi" ? "Đăng ký thiết bị" : "Register Hardware",
        onClick: openAddEquipmentModal,
        icon: Plus,
      }
    : undefined}
>
  {#snippet customHeaderActions()}
    <button
      type="button"
      onclick={() => openTestRecordsModal(null)}
      class="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white font-bold text-xs shadow-xs transition active:scale-95 cursor-pointer"
      title={$language === "vi" ? "Mở popup hiển thị đầy đủ chi tiết các bản ghi kiểm tra kết nối mạng của khách hàng" : "Open customer network diagnostics modal"}
    >
      <Activity class="h-3.5 w-3.5" />
      <span class="hidden sm:inline">{$language === "vi" ? "Các bản ghi đo kiểm mạng" : "Network Records"}</span>
      <span class="sm:hidden">{$language === "vi" ? "Bản ghi mạng" : "Records"}</span>
      <span class="px-1.5 py-0.2 rounded-full bg-white/20 text-[10px] font-mono">{$connections.length}</span>
    </button>
  {/snippet}
  {#if isApiLoading || apiError}
    <div
      class="mb-4 flex items-center justify-between gap-3 rounded-xl border px-4 py-3 text-sm {apiError
        ? 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950/30 dark:text-rose-300'
        : 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-300'}"
    >
      <div class="flex items-center gap-2">
        <RefreshCw class="h-4 w-4 {isApiLoading ? 'animate-spin' : ''}" />
        <span>
          {isApiLoading
            ? $language === "vi"
              ? "Đang đồng bộ dữ liệu kỹ thuật..."
              : "Syncing technical data..."
            : apiError}
        </span>
      </div>
      {#if apiError && !isApiLoading}
        <button
          type="button"
          class="rounded-lg border border-current px-3 py-1 text-xs font-semibold hover:bg-white/50"
          onclick={() =>
            refreshTechnicalData().catch((error) =>
              toast.error(errorMessage(error)),
            )}
        >
          {$language === "vi" ? "Thử lại" : "Retry"}
        </button>
      {/if}
    </div>
  {/if}

  <!-- TAB 1: ORDER FEASIBILITY QUEUE -->
  {#if activeTab === "feasibility-queue"}
    <div class="tab-content-animate space-y-4">
      <!-- Two-stage rule notice -->
      <div
        class="flex items-start gap-2.5 p-3.5 rounded-xl bg-sky-50 dark:bg-sky-950/30 border border-sky-200 dark:border-sky-900/50"
      >
        <ShieldCheck
          class="h-4 w-4 text-sky-600 dark:text-sky-400 shrink-0 mt-0.5"
        />
        <p class="text-[11px] text-sky-900 dark:text-sky-200 leading-relaxed">
          {$language === "vi"
            ? "Hàng đợi này chỉ chứa đơn đã được nhân viên bán hàng của chi nhánh duyệt hồ sơ. Đơn mới đăng ký (chờ bán hàng duyệt) không hiển thị ở đây."
            : "This queue only lists applications already cleared by the branch's retail staff. Newly submitted orders (awaiting retail approval) never appear here."}
          {#if $orders.filter((o) => o.status === "PendingRetail").length > 0}
            <span
              class="block mt-1 font-semibold text-amber-700 dark:text-amber-400"
            >
              {$language === "vi"
                ? `Hiện có ${$orders.filter((o) => o.status === "PendingRetail").length} hồ sơ đang chờ bán hàng chi nhánh duyệt.`
                : `${$orders.filter((o) => o.status === "PendingRetail").length} application(s) still waiting on branch retail approval.`}
            </span>
          {/if}
        </p>
      </div>

      <!-- Filter Bar -->
      <div
        class="flex flex-col sm:flex-row gap-3 items-center justify-between bg-white dark:bg-slate-900 p-4 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm"
      >
        <div class="relative w-full sm:w-80">
          <Search
            class="absolute left-3 top-2.5 h-4 w-4 text-slate-400 dark:text-slate-500"
          />
          <input
            type="text"
            placeholder={$language === "vi"
              ? "Tìm theo Mã đơn (11 ký tự), khách hàng, địa chỉ..."
              : "Filter by Order ID, subscriber, or location..."}
            bind:value={queueSearch}
            class="w-full pl-9 pr-4 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 font-mono text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-600"
          />
        </div>

        <div class="flex items-center space-x-2 w-full sm:w-auto">
          <span
            class="text-xs text-slate-600 dark:text-slate-400 whitespace-nowrap"
          >
            {$language === "vi" ? "Trạng thái:" : "Filter Status:"}
          </span>
          <select
            bind:value={queueStatusFilter}
            class="text-xs font-mono bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-2 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-amber-500"
          >
            <option value="All"
              >{$language === "vi"
                ? "Tất cả trạng thái"
                : "All Statuses"}</option
            >
            <option value="Pending"
              >{$language === "vi"
                ? "Chờ khảo sát"
                : "Pending Assessment"}</option
            >
            <option value="Feasible"
              >{$language === "vi"
                ? "Khả thi (Sẵn sàng cấp)"
                : "Feasible (Ready for Provision)"}</option
            >
            <option value="Not Feasible"
              >{$language === "vi"
                ? "Không khả thi (Từ chối)"
                : "Not Feasible (Declined)"}</option
            >
            <option value="Connection Provided"
              >{$language === "vi"
                ? "Đã cung cấp (Hoạt động)"
                : "Connection Provided (Active)"}</option
            >
          </select>

          <button
            type="button"
            onclick={() => openTestRecordsModal(null)}
            class="inline-flex items-center space-x-1.5 px-3 py-2 rounded-lg text-xs font-bold bg-sky-50 hover:bg-sky-100 dark:bg-sky-950/60 dark:hover:bg-sky-900 text-sky-700 dark:text-sky-300 border border-sky-300 dark:border-sky-800 transition cursor-pointer shrink-0"
            title={$language === "vi" ? "Mở popup hiển thị đầy đủ chi tiết các bản ghi kiểm tra kết nối mạng của khách hàng" : "Open customer network diagnostics modal"}
          >
            <Activity class="h-3.5 w-3.5 text-sky-600 dark:text-sky-400" />
            <span>{$language === "vi" ? "Các bản ghi đo kiểm" : "Network Records"}</span>
            <span class="px-1.5 py-0.2 rounded-full bg-sky-200/70 dark:bg-sky-800 text-[10px] font-mono font-bold">
              {$connections.length}
            </span>
          </button>
        </div>
      </div>

      <!-- Orders Queue Table -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead
              class="bg-slate-50 dark:bg-slate-950/80 text-xs font-mono text-slate-600 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Mã đơn hàng (11 ký tự)"
                    : "Order ID (11-Char)"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Khách hàng & Địa chỉ"
                    : "Customer & Location"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Gói cước & Loại mạng"
                    : "Plan & Type"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Khoảng cách cáp"
                    : "Loop Distance"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Suy hao tín hiệu" : "Signal Loss"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Trạng thái khả thi"
                    : "Feasibility Status"}</th
                >
                <th class="px-4 py-3 text-right"
                  >{$language === "vi"
                    ? "Thao tác kỹ thuật"
                    : "Actions Required"}</th
                >
              </tr>
            </thead>
            <tbody
              class="divide-y divide-slate-200 dark:divide-slate-800/60 font-sans"
            >
              {#each filteredOrders as order (order.id)}
                {@const provConns = getOrderProvisionedConnections(order.id)}
                {@const totalReq = Math.max(1, order.bulkConnectionsCount || 1)}
                <tr
                  class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition"
                >
                  <td class="px-4 py-3">
                    <span
                      class="font-mono font-bold text-amber-600 dark:text-amber-400 text-sm"
                      >{order.id}</span
                    >
                    <div class="text-[11px] text-slate-500 dark:text-slate-400">
                      {order.createdAt}
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    <div class="font-semibold text-slate-900 dark:text-white">
                      {order.customerName}
                    </div>
                    <div
                      class="text-xs text-slate-500 dark:text-slate-400 truncate max-w-xs"
                    >
                      {order.installationAddress}
                    </div>
                    <div class="text-[11px] text-slate-500 dark:text-slate-400">
                      {order.customerPhone}
                    </div>
                    {#if order.assignedTechnician}
                      <div
                        class="text-[11px] font-semibold text-emerald-600 dark:text-emerald-400 mt-1 flex items-center gap-1 font-mono"
                      >
                        <Wrench class="h-3 w-3" />
                        <span
                          >{$language === "vi" ? "KTV:" : "Tech:"}
                          {order.assignedTechnician}</span
                        >
                      </div>
                    {/if}
                  </td>
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-1.5 flex-wrap">
                      <span
                        class="text-xs font-semibold px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-800 dark:text-slate-200"
                      >
                        {$language === "vi"
                          ? order.connectionType === "Broadband"
                            ? "Cáp quang"
                            : order.connectionType === "Dial-Up"
                              ? "Quay số"
                              : "Cố định"
                          : order.connectionType}
                      </span>
                      {#if totalReq > 1}
                        <span
                          class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-amber-50 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-700/50"
                          title={$language === "vi"
                            ? `Đơn hàng đặt ${totalReq} kết nối`
                            : `Order requesting ${totalReq} connections`}
                        >
                          ⚡ {totalReq}
                          {$language === "vi" ? "kết nối" : "conns"}
                        </span>
                      {/if}
                    </div>
                    <div
                      class="text-xs text-slate-500 dark:text-slate-400 mt-1"
                    >
                      {getPlanName({ name: order.planName }, $language)}
                    </div>
                    {#if order.connectionType === "Dial-Up"}
                      <div
                        class="text-[10px] mt-1 {order.existingLandlineAccountId
                          ? 'text-sky-600 dark:text-sky-400'
                          : 'text-amber-600 dark:text-amber-400'}"
                      >
                        {order.existingLandlineAccountId
                          ? $language === "vi"
                            ? `Đã có cố định ${order.existingLandlineAccountId} (chỉ đo Internet)`
                            : `Checks: internet only (has landline ${order.existingLandlineAccountId})`
                          : $language === "vi"
                            ? "Cần đo: Cố định + Internet"
                            : "Checks: landline + internet"}
                      </div>
                    {/if}
                  </td>
                  <td
                    class="px-4 py-3 font-mono text-xs tabular-nums text-slate-700 dark:text-slate-300"
                  >
                    {order.cableDistanceMeters || 120} m
                    <div class="text-[10px] text-slate-500 dark:text-slate-400">
                      {order.dpBoxCapacity || "DP Box Port OK"}
                    </div>
                  </td>
                  <td class="px-4 py-3 font-mono text-xs tabular-nums">
                    <span
                      class={(order.signalLossDbm || -18) < -30
                        ? "text-rose-600 dark:text-rose-400 font-bold"
                        : "text-emerald-600 dark:text-emerald-400 font-bold"}
                    >
                      {(order.signalLossDbm || -18).toFixed(1)} dBm
                    </span>
                  </td>
                  <td class="px-4 py-3">
                    <span
                      class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold {order.status ===
                      'Connection Provided'
                        ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800/60'
                        : order.status === 'Feasible'
                          ? 'bg-blue-100 dark:bg-blue-950 text-blue-800 dark:text-blue-300 border border-blue-300 dark:border-blue-800/60'
                          : order.status === 'Not Feasible'
                            ? 'bg-rose-100 dark:bg-rose-950 text-rose-800 dark:text-rose-300 border border-rose-300 dark:border-rose-800/60'
                            : 'bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800/60'}"
                    >
                      {$language === "vi"
                        ? order.status === "Pending"
                          ? "Chờ khảo sát"
                          : order.status === "Feasible"
                            ? totalReq > 1 && provConns.length > 0
                              ? `Đang duyệt (${provConns.length}/${totalReq})`
                              : "Khả thi"
                            : order.status === "Not Feasible"
                              ? "Không khả thi"
                              : "Đã cấp kết nối"
                        : order.status}
                    </span>

                    {#if order.retailApprovedBy}
                      <div
                        class="text-[10px] text-emerald-700 dark:text-emerald-400 mt-1"
                      >
                        {$language === "vi"
                          ? "Bán hàng duyệt:"
                          : "Retail approved:"}
                        {order.retailApprovedBy}
                        {#if order.retailOutletCode}· {order.retailOutletCode}{/if}
                      </div>
                    {/if}

                    {#if totalReq > 1}
                      <div class="mt-2 min-w-[120px]">
                        <div
                          class="flex items-center justify-between text-[10px] font-mono text-slate-500 dark:text-slate-400"
                        >
                          <span
                            >{$language === "vi"
                              ? "Tiến độ duyệt:"
                              : "Approved:"}</span
                          >
                          <span
                            class="font-bold {provConns.length === totalReq
                              ? 'text-emerald-600 dark:text-emerald-400'
                              : 'text-amber-600 dark:text-amber-400'}"
                            >{provConns.length}/{totalReq}</span
                          >
                        </div>
                        <div
                          class="w-full bg-slate-200 dark:bg-slate-700 h-1.5 rounded-full overflow-hidden mt-0.5"
                        >
                          <div
                            class="{provConns.length === totalReq
                              ? 'bg-emerald-500'
                              : 'bg-amber-500'} h-full rounded-full transition-all duration-300"
                            style="width: {Math.round(
                              (provConns.length / totalReq) * 100,
                            )}%"
                          ></div>
                        </div>
                      </div>
                    {/if}

                    {#if order.assignedAccountId && order.status !== "Connection Provided"}
                      <div
                        class="text-[10px] font-mono text-slate-500 dark:text-slate-400 mt-1"
                      >
                        Account ID: {order.assignedAccountId}
                      </div>
                    {/if}
                    {#if order.scheduledInstallDate && order.status === "Feasible"}
                      <div
                        class="text-[10px] font-mono text-blue-600 dark:text-blue-400 mt-1"
                      >
                        <CalendarClock class="inline h-3 w-3 mr-0.5" />
                        {new Date(order.scheduledInstallDate).toLocaleString(
                          $language === "vi" ? "vi-VN" : "en-US",
                        )}
                      </div>
                    {/if}
                  </td>
                  <td class="px-4 py-3 text-right">
                    <div
                      class="flex items-center justify-end gap-1.5 flex-wrap"
                    >
                      {#if order.status !== "Connection Provided"}
                        {#if order.connectionType === "Dial-Up" && order.status !== "Feasible"}
                          <button
                            onclick={() => toggleLeg(order, "landline")}
                            disabled={!!order.existingLandlineAccountId}
                            class="px-1.5 py-1 rounded text-[10px] font-semibold border transition {landlineLegDone(
                              order,
                            )
                              ? 'bg-emerald-50 dark:bg-emerald-600/20 text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-600/30'
                              : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700'}"
                            title={$language === "vi"
                              ? "Đo kiểm nhánh đường dây thoại cố định"
                              : "Toggle landline feasibility leg"}
                          >
                            LL {landlineLegDone(order) ? "✓" : "…"}
                          </button>
                          <button
                            onclick={() => toggleLeg(order, "internet")}
                            class="px-1.5 py-1 rounded text-[10px] font-semibold border transition {legFor(
                              order,
                            ).internet
                              ? 'bg-emerald-50 dark:bg-emerald-600/20 text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-600/30'
                              : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700'}"
                            title={$language === "vi"
                              ? "Đo kiểm cổng DSLAM Internet"
                              : "Toggle internet feasibility leg"}
                          >
                            NET {legFor(order).internet ? "✓" : "…"}
                          </button>
                        {/if}
                        {#if order.status !== "Feasible"}
                          <button
                            onclick={() => handleMarkFeasible(order)}
                            class="px-2 py-1 rounded text-xs font-semibold bg-emerald-50 dark:bg-emerald-600/20 hover:bg-emerald-100 dark:hover:bg-emerald-600/30 text-emerald-700 dark:text-emerald-400 border border-emerald-300 dark:border-emerald-600/30 transition"
                            title={$language === "vi"
                              ? "Xác nhận đạt tiêu chuẩn kỹ thuật"
                              : "Mark order as technically feasible"}
                          >
                            {$language === "vi" ? "Khả thi" : "Feasible"}
                          </button>
                          <button
                            onclick={() => handleOpenNotFeasibleModal(order)}
                            class="px-2 py-1 rounded text-xs font-semibold bg-rose-50 dark:bg-rose-600/20 hover:bg-rose-100 dark:hover:bg-rose-600/30 text-rose-700 dark:text-rose-400 border border-rose-300 dark:border-rose-600/30 transition"
                            title={$language === "vi"
                              ? "Báo cáo không khả thi hạ tầng"
                              : "Flag technical infeasibility"}
                          >
                            {$language === "vi"
                              ? "Không khả thi"
                              : "Not Feasible"}
                          </button>
                        {:else}
                          <button
                            onclick={() => handleOpenScheduleModal(order)}
                            class="px-2 py-1 rounded text-xs font-semibold bg-blue-50 dark:bg-blue-600/20 hover:bg-blue-100 text-blue-700 dark:text-blue-300 border border-blue-300 dark:border-blue-700 transition inline-flex items-center gap-1"
                          >
                            <CalendarClock class="h-3.5 w-3.5" />
                            <span
                              >{$language === "vi"
                                ? "Lịch lắp đặt"
                                : "Schedule"}</span
                            >
                          </button>
                          <button
                            onclick={() => handleOpenProvisionModal(order)}
                            class="px-2.5 py-1 rounded text-xs font-bold transition shadow {provConns.length >
                              0 && provConns.length < totalReq
                              ? 'bg-amber-600 hover:bg-amber-700 text-white ring-2 ring-amber-400 animate-pulse'
                              : 'bg-amber-600 hover:bg-amber-700 text-white'}"
                            title={$language === "vi"
                              ? totalReq > 1
                                ? `Duyệt kết nối #${provConns.length + 1} của đơn hàng này`
                                : "Gán thiết bị và cấp tín hiệu"
                              : "Assign modem/router and provision connection"}
                          >
                            {#if totalReq > 1}
                              {$language === "vi"
                                ? provConns.length === 0
                                  ? `Duyệt kết nối (1/${totalReq})`
                                  : `Duyệt tiếp (${provConns.length + 1}/${totalReq})`
                                : `Provision (${provConns.length + 1}/${totalReq})`}
                            {:else}
                              {$language === "vi"
                                ? "Cung cấp kết nối"
                                : "Connection Provided"}
                            {/if}
                          </button>
                        {/if}
                      {:else}
                        <div class="text-right">
                          <span
                            class="text-xs text-emerald-600 dark:text-emerald-400 font-mono font-semibold"
                            >✓ {$language === "vi"
                              ? `Đã cấp đủ ${totalReq} kết nối`
                              : `All ${totalReq} conns provisioned`}</span
                          >
                          {#if provConns.length > 0}
                            <div
                              class="text-[10px] text-slate-500 dark:text-slate-400 font-mono"
                            >
                              {provConns.length === 1
                                ? provConns[0].accountId
                                : `${provConns[0].accountId} … (+${provConns.length - 1})`}
                            </div>
                          {/if}
                        </div>
                      {/if}
                    </div>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  {/if}

  <!-- TAB 2: CONNECTION MANAGER (DANH SÁCH MÃ TÀI KHOẢN & DROPDOWN CHI TIẾT) -->
  {#if activeTab === "connection-manager"}
    <div class="space-y-6 tab-content-animate">
      <!-- 1. KPI Top Stats Summary Cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi"
                ? "Mã tài khoản quản lý"
                : "Managed Accounts"}</span
            >
            <Radio class="h-3.5 w-3.5 text-amber-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-slate-900 dark:text-white mt-1.5"
          >
            {$connections.length}
            <span class="text-xs font-normal text-slate-500 ml-1"
              >{$language === "vi" ? "Mạch" : "Circuits"}</span
            >
          </div>
          <div class="text-[10px] text-slate-500 dark:text-slate-400 mt-1">
            {$language === "vi"
              ? "Đã cấp phát định danh 16 ký tự"
              : "16-char issued circuits"}
          </div>
        </div>

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
            class="text-2xl font-bold font-mono text-emerald-700 dark:text-emerald-400 mt-1.5"
          >
            {$connections.filter((c) => c.status === "Active").length}
            <span class="text-xs font-normal text-emerald-600/70 ml-1"
              >{$language === "vi" ? "Trực tuyến" : "Online"}</span
            >
          </div>
          <div
            class="text-[10px] text-emerald-600/80 dark:text-emerald-500 mt-1"
          >
            {$language === "vi"
              ? "Lưu lượng truyền tải ổn định"
              : "Stable traffic flow"}
          </div>
        </div>

        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-amber-200 dark:border-amber-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-amber-600 dark:text-amber-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi"
                ? "Tạm ngưng dịch vụ"
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
              >{$language === "vi" ? "Tạm dừng" : "On Hold"}</span
            >
          </div>
          <div class="text-[10px] text-amber-600/80 dark:text-amber-500 mt-1">
            {$language === "vi"
              ? "Bảo trì / Giữ chỗ cổng"
              : "Maintenance / Port hold"}
          </div>
        </div>

        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-rose-200 dark:border-rose-900/40 shadow-sm"
        >
          <div
            class="text-[11px] font-mono text-rose-600 dark:text-rose-400 uppercase tracking-wider flex items-center justify-between"
          >
            <span
              >{$language === "vi"
                ? "Ngắt kết nối vĩnh viễn"
                : "Permanently Inactive"}</span
            >
            <XCircle class="h-3.5 w-3.5 text-rose-500" />
          </div>
          <div
            class="text-2xl font-bold font-mono text-rose-700 dark:text-rose-400 mt-1.5"
          >
            {$connections.filter((c) => c.status === "Permanently Inactive")
              .length}
            <span class="text-xs font-normal text-rose-600/70 ml-1"
              >{$language === "vi" ? "Đã ngắt" : "Cut Off"}</span
            >
          </div>
          <div class="text-[10px] text-rose-600/80 dark:text-rose-500 mt-1">
            {$language === "vi"
              ? "Đã thu hồi cổng & CPE"
              : "CPE & Port reclaimed"}
          </div>
        </div>
      </div>

      <!-- 2. Search & Filter Tool Bar -->
      <div
        class="bg-white dark:bg-slate-900 p-5 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4"
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
                ? "Tra cứu theo Mã tài khoản (16 ký tự), tên khách hàng, địa chỉ, cổng NOC, serial thiết bị..."
                : "Search by 16-char Account ID, subscriber name, address, NOC port, device serial..."}
              bind:value={techAccountSearch}
              class="w-full pl-10 pr-4 py-2.5 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 text-slate-900 dark:text-slate-100 placeholder:text-slate-400 dark:placeholder:text-slate-600 font-mono"
            />
          </div>

          <div class="flex items-center gap-2">
            <button
              type="button"
              onclick={() => openTestRecordsModal(null)}
              class="px-4 py-2.5 rounded-lg text-sm font-bold bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white transition shadow flex items-center justify-center space-x-1.5 whitespace-nowrap cursor-pointer"
              title={$language === "vi" ? "Mở popup hiển thị đầy đủ chi tiết các bản ghi kiểm tra kết nối mạng của khách hàng" : "Open customer network diagnostics modal"}
            >
              <Activity class="h-4 w-4" />
              <span>{$language === "vi" ? "Các bản ghi đo kiểm mạng" : "Network Test Records"}</span>
              <span class="px-1.5 py-0.2 rounded-full bg-white/20 text-[10px] font-mono">{$connections.length}</span>
            </button>

            <button
              onclick={queryTelemetry}
              class="px-4 py-2.5 rounded-lg text-sm font-bold bg-amber-600 hover:bg-amber-700 text-white transition shadow flex items-center justify-center space-x-1.5 whitespace-nowrap"
            >
              <Activity class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Đo kiểm & Mở chi tiết"
                  : "Query Telemetry"}</span
              >
            </button>
          </div>
        </div>

        <!-- Filter rows: Status & Connection Type -->
        <div
          class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 pt-1"
        >
          <!-- Status Filter Pills (Single horizontal row) -->
          <div
            class="flex items-center gap-1.5 overflow-x-auto whitespace-nowrap pb-1 max-w-full"
          >
            <button
              onclick={() => (connStatusFilter = "All")}
              class="px-2.5 py-1 text-xs font-medium rounded-lg transition {connStatusFilter ===
              'All'
                ? 'bg-amber-600 text-white shadow-sm'
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
            >
              {$language === "vi" ? "Tất cả" : "All"} ({$connections.length})
            </button>
            <button
              onclick={() => (connStatusFilter = "Active")}
              class="px-2.5 py-1 text-xs font-medium rounded-lg transition {connStatusFilter ===
              'Active'
                ? 'bg-emerald-600 text-white shadow-sm'
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
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
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
            >
              {$language === "vi" ? "Tạm ngưng" : "Inactive"} ({$connections.filter(
                (c) => c.status === "Temporarily Inactive",
              ).length})
            </button>
            <button
              onclick={() => (connStatusFilter = "Permanently Inactive")}
              class="px-2.5 py-1 text-xs font-medium rounded-lg transition {connStatusFilter ===
              'Permanently Inactive'
                ? 'bg-rose-600 text-white shadow-sm'
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
            >
              {$language === "vi" ? "Ngắt kết nối" : "Disconnected"} ({$connections.filter(
                (c) => c.status === "Permanently Inactive",
              ).length})
            </button>
          </div>

          <!-- Connection Type Filter -->
          <div class="flex items-center space-x-2 text-xs">
            <span class="text-slate-500 dark:text-slate-400 whitespace-nowrap">
              {$language === "vi" ? "Loại mạch:" : "Circuit Type:"}
            </span>
            <select
              bind:value={connTypeFilter}
              class="bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg px-2.5 py-1 text-slate-800 dark:text-slate-200 font-medium focus:outline-none focus:ring-1 focus:ring-amber-500 text-xs"
            >
              <option value="All"
                >{$language === "vi" ? "Tất cả loại mạch" : "All Types"}</option
              >
              <option value="Broadband">Broadband (Cáp quang)</option>
              <option value="Landline">Landline (Cố định)</option>
              <option value="Dial-Up">Dial-Up (Quay số)</option>
            </select>
          </div>
        </div>

        <!-- Fast Pick Tags -->
        <div
          class="flex flex-wrap items-center gap-2 pt-1 text-xs text-slate-500 dark:text-slate-400 font-mono"
        >
          <span class="text-[11px]"
            >{$language === "vi"
              ? "Mã tài khoản mới nhất:"
              : "Latest Access Accounts:"}</span
          >
          {#each $connections as c (c.accountId)}
            <button
              onclick={() => {
                techAccountSearch = c.accountId;
                selectedConnection = c;
                expandedAccountId = c.accountId;
              }}
              class="px-2 py-0.5 rounded border transition font-semibold text-[11px] {expandedAccountId ===
              c.accountId
                ? 'bg-amber-100 dark:bg-amber-950/60 border-amber-400 text-amber-800 dark:text-amber-300 ring-1 ring-amber-400'
                : 'bg-slate-100 dark:bg-slate-950 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:border-amber-400'}"
            >
              {c.accountId}
            </button>
          {/each}
        </div>
      </div>

      <!-- 3. BẢNG DANH SÁCH CÁC MÃ TÀI KHOẢN MÀ TÀI KHOẢN TECH QUẢN LÝ -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm overflow-hidden"
      >
        <div
          class="p-5 border-b border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-slate-50/50 dark:bg-slate-950/50"
        >
          <div>
            <h3
              class="text-base font-bold text-slate-900 dark:text-white flex items-center space-x-2"
            >
              <Radio class="h-5 w-5 text-amber-600" />
              <span
                >{$language === "vi"
                  ? "Danh sách mã tài khoản quản lý kết nối & mạch"
                  : "Managed Account Circuits & Connection Registry"}</span
              >
              <span
                class="ml-2 px-2.5 py-0.5 text-xs font-mono font-bold rounded-full bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800"
              >
                {filteredConnections.length}
                {$language === "vi" ? "mã tài khoản" : "accounts"}
              </span>
            </h3>
            <!-- <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
              {$language === "vi"
                ? "Bấm vào 'Chi tiết' tại từng mã tài khoản để mở dropdown box xem thông số đo kiểm tín hiệu, đổi trạng thái cổng mạng."
                : "Click 'Details' on any account ID row to expand the dropdown box for real-time telemetry and circuit status toggles."}
            </p> -->
          </div>

          {#if expandedAccountId}
            <button
              onclick={() => (expandedAccountId = null)}
              class="text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-amber-600 dark:hover:text-amber-400 border border-slate-200 dark:border-slate-800 rounded-lg px-3 py-1.5 transition bg-white dark:bg-slate-900 flex items-center gap-1 self-start sm:self-auto"
            >
              <X class="h-3.5 w-3.5" />
              <span
                >{$language === "vi"
                  ? "Thu gọn tất cả chi tiết"
                  : "Collapse All Details"}</span
              >
            </button>
          {/if}
        </div>

        {#if filteredConnections.length === 0}
          <div class="py-16 text-center space-y-3">
            <Radio class="h-10 w-10 text-slate-400 mx-auto opacity-50" />
            <div
              class="text-sm font-semibold text-slate-700 dark:text-slate-300 font-mono"
            >
              {$language === "vi"
                ? "Không tìm thấy mã tài khoản nào khớp với bộ lọc."
                : "No accounts match the current filter criteria."}
            </div>
            <button
              onclick={() => {
                techAccountSearch = "";
                connStatusFilter = "All";
                connTypeFilter = "All";
              }}
              class="px-4 py-1.5 text-xs font-semibold bg-amber-600 text-white rounded-lg hover:bg-amber-700 transition"
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
                    >{$language === "vi" ? "MÃ TÀI KHOẢN" : "ACCOUNT ID"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "KHÁCH HÀNG & ĐỊA CHỈ"
                      : "SUBSCRIBER & LOCATION"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "LOẠI MẠCH & GÓI CƯỚC"
                      : "CIRCUIT & PLAN"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "CỔNG NOC & IP"
                      : "NOC PORT & IP"}</th
                  >
                  <th class="px-4 py-3.5"
                    >{$language === "vi"
                      ? "TRẠNG THÁI MẠCH"
                      : "CIRCUIT STATUS"}</th
                  >
                  <th class="px-4 py-3.5 text-right"
                    >{$language === "vi" ? "THAO TÁC" : "ACTIONS"}</th
                  >
                </tr>
              </thead>
              <tbody
                class="divide-y divide-slate-100 dark:divide-slate-800/60 font-sans"
              >
                {#each filteredConnections as conn (conn.accountId)}
                  {@const isExpanded = expandedAccountId === conn.accountId}
                  <!-- Table Main Row -->
                  <tr
                    class="transition-colors hover:bg-amber-50/40 dark:hover:bg-amber-950/20 {isExpanded
                      ? 'bg-amber-50/60 dark:bg-amber-950/30'
                      : ''}"
                  >
                    <!-- Column 1: Account ID -->
                    <td class="px-4 py-4 align-top">
                      <div class="flex items-center space-x-1.5">
                        <span
                          class="font-mono font-bold text-amber-600 dark:text-amber-400 tracking-wider"
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

                    <!-- Column 2: Subscriber & Location -->
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

                    <!-- Column 3: Circuit Type & Plan -->
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
                        ${conn.monthlyRental}/{$language === "vi"
                          ? "tháng"
                          : "mo"}
                      </div>
                    </td>

                    <!-- Column 4: NOC Port & IP -->
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

                    <!-- Column 5: Status -->
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
                                ? "Tạm ngưng"
                                : "Ngắt kết nối"
                            : conn.status}
                        </span>
                      </span>
                    </td>

                    <!-- Column 6: Actions / Dropdown trigger -->
                    <td
                      class="px-4 py-4 align-top text-right whitespace-nowrap"
                    >
                      <button
                        type="button"
                        onclick={() => openTestRecordsModal(conn)}
                        class="inline-flex items-center space-x-1 px-2.5 py-1.5 rounded-lg text-xs font-bold bg-sky-50 dark:bg-sky-950/60 hover:bg-sky-100 dark:hover:bg-sky-900 text-sky-700 dark:text-sky-300 border border-sky-300 dark:border-sky-800 transition shadow-xs cursor-pointer mr-1.5"
                        title={$language === "vi" ? "Xem đầy đủ chi tiết các bản ghi kiểm tra kết nối mạng của khách hàng" : "View network test records"}
                      >
                        <Activity class="h-3.5 w-3.5" />
                        <span>{$language === "vi" ? "Các bản ghi" : "Records"}</span>
                      </button>

                      <button
                        onclick={() => toggleDetailDropdown(conn.accountId)}
                        class="inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-bold transition shadow-xs {isExpanded
                          ? 'bg-amber-600 text-white shadow-amber-600/30'
                          : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-amber-100 dark:hover:bg-amber-950 hover:text-amber-800 dark:hover:text-amber-300 border border-slate-200 dark:border-slate-700'}"
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
                    <tr
                      class="bg-gradient-to-b from-amber-50/50 to-slate-50 dark:from-amber-950/20 dark:to-slate-950/40 border-b-2 border-amber-400 dark:border-amber-600/60"
                    >
                      <td colspan="6" class="p-4 sm:p-6">
                        <div
                          class="space-y-6 rounded-xl border border-amber-200 dark:border-amber-900/60 bg-white dark:bg-slate-900 p-5 sm:p-6 shadow-md"
                        >
                          <!-- Box Header -->
                          <div
                            class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-800 pb-4"
                          >
                            <div
                              class="flex items-start sm:items-center space-x-3"
                            >
                              <div
                                class="p-2.5 rounded-xl bg-amber-100 dark:bg-amber-950/80 text-amber-700 dark:text-amber-400 border border-amber-300 dark:border-amber-800"
                              >
                                <Radio class="h-6 w-6" />
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
                                      class="text-amber-600 dark:text-amber-400 font-extrabold"
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
                                          ? "Tạm ngưng dịch vụ"
                                          : "Ngắt kết nối vĩnh viễn"
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
                                  • {conn.installationAddress} • {$language ===
                                  "vi"
                                    ? "Điện thoại:"
                                    : "Phone:"}
                                  {conn.customerPhone}
                                </p>
                              </div>
                            </div>

                            <!-- Header Actions -->
                            <div class="flex flex-wrap items-center gap-2">
                              <button
                                type="button"
                                onclick={() => openTestRecordsModal(conn)}
                                class="inline-flex items-center space-x-1.5 text-xs bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white px-3 py-1.5 rounded-lg font-bold transition shadow-xs cursor-pointer"
                                title={$language === "vi" ? "Mở popup hiển thị đầy đủ chi tiết các bản ghi kiểm tra kết nối mạng của khách hàng" : "Open customer network test records"}
                              >
                                <Activity class="h-3.5 w-3.5" />
                                <span>{$language === "vi" ? "Xem đầy đủ các bản ghi kiểm tra" : "View Full Test Records"}</span>
                              </button>

                              <button
                                onclick={() =>
                                  handleRunPingTest(conn.accountId)}
                                disabled={testingPingAccountId ===
                                  conn.accountId}
                                class="inline-flex items-center space-x-1.5 text-xs bg-slate-100 dark:bg-slate-800 hover:bg-amber-100 dark:hover:bg-amber-950/60 text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700 px-3 py-1.5 rounded-lg font-semibold transition"
                              >
                                <RefreshCw
                                  class="h-3.5 w-3.5 {testingPingAccountId ===
                                  conn.accountId
                                    ? 'animate-spin text-amber-600'
                                    : 'text-slate-500'}"
                                />
                                <span
                                  >{$language === "vi"
                                    ? "Đo kiểm tín hiệu (Ping Loop)"
                                    : "Run Loopback Ping"}</span
                                >
                              </button>

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
                                onclick={() => (expandedAccountId = null)}
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

                          <!-- Content Grid: 3 Sections -->
                          <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">
                            <!-- 1. 3-WAY STATUS TOGGLE CONTROLLER -->
                            <div
                              class="lg:col-span-2 p-5 rounded-xl bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-4"
                            >
                              <div>
                                <h5
                                  class="text-xs font-bold text-slate-900 dark:text-white uppercase tracking-wider flex items-center space-x-2"
                                >
                                  <Power class="h-4 w-4 text-amber-500" />
                                  <span
                                    >{$language === "vi"
                                      ? "Chuyển trạng thái hoạt động đường truyền"
                                      : "Line Operational Status Toggle"}</span
                                  >
                                </h5>
                                <!-- <p class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5">
                                  {$language === "vi"
                                    ? `Thay đổi trạng thái cổng mạch cho thuê bao ${conn.accountId} tức thời trên toàn bộ mạng lưới viễn thông.`
                                    : `Toggle subscriber circuit condition for ${conn.accountId} in real-time across the network.`}
                                </p> -->
                              </div>

                              <div
                                class="grid grid-cols-1 sm:grid-cols-3 gap-3"
                              >
                                <!-- State 1: Active -->
                                <button
                                  onclick={() =>
                                    handleToggleConnectionStatusFor(
                                      conn,
                                      "Active",
                                    )}
                                  class="p-3.5 rounded-xl border text-left transition flex flex-col justify-between {conn.status ===
                                  'Active'
                                    ? 'border-emerald-500 bg-emerald-50 dark:bg-emerald-950/40 ring-1 ring-emerald-500'
                                    : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:border-slate-300 dark:hover:border-slate-700'}"
                                >
                                  <div
                                    class="flex items-center justify-between mb-2"
                                  >
                                    <span
                                      class="h-3 w-3 rounded-full bg-emerald-500 shadow-sm shadow-emerald-500/50"
                                    ></span>
                                    <span
                                      class="text-[10px] font-mono text-emerald-600 dark:text-emerald-400 font-semibold uppercase"
                                    >
                                      {$language === "vi"
                                        ? "Trạng thái 01"
                                        : "State 01"}
                                    </span>
                                  </div>
                                  <div>
                                    <div
                                      class="font-bold text-sm text-emerald-700 dark:text-emerald-400"
                                    >
                                      {$language === "vi"
                                        ? "Hoạt động bình thường"
                                        : "Active"}
                                    </div>
                                    <div
                                      class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5"
                                    >
                                      {$language === "vi"
                                        ? "Trực tuyến, lưu lượng bình thường"
                                        : "Online, packets passing"}
                                    </div>
                                  </div>
                                </button>

                                <!-- State 2: Temporarily Inactive -->
                                <button
                                  onclick={() =>
                                    handleToggleConnectionStatusFor(
                                      conn,
                                      "Temporarily Inactive",
                                    )}
                                  class="p-3.5 rounded-xl border text-left transition flex flex-col justify-between {conn.status ===
                                  'Temporarily Inactive'
                                    ? 'border-amber-500 bg-amber-50 dark:bg-amber-950/40 ring-1 ring-amber-500'
                                    : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:border-slate-300 dark:hover:border-slate-700'}"
                                >
                                  <div
                                    class="flex items-center justify-between mb-2"
                                  >
                                    <span
                                      class="h-3 w-3 rounded-full bg-amber-500 shadow-sm shadow-amber-500/50"
                                    ></span>
                                    <span
                                      class="text-[10px] font-mono text-amber-600 dark:text-amber-400 font-semibold uppercase"
                                    >
                                      {$language === "vi"
                                        ? "Trạng thái 02"
                                        : "State 02"}
                                    </span>
                                  </div>
                                  <div>
                                    <div
                                      class="font-bold text-sm text-amber-700 dark:text-amber-400"
                                    >
                                      {$language === "vi"
                                        ? "Tạm ngưng dịch vụ"
                                        : "Temporarily Inactive"}
                                    </div>
                                    <div
                                      class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5"
                                    >
                                      {$language === "vi"
                                        ? "Bảo trì cáp quang, tạm giữ cước"
                                        : "Soft suspension, maintenance"}
                                    </div>
                                  </div>
                                </button>

                                <!-- State 3: Permanently Inactive -->
                                <button
                                  onclick={() =>
                                    handleToggleConnectionStatusFor(
                                      conn,
                                      "Permanently Inactive",
                                    )}
                                  class="p-3.5 rounded-xl border text-left transition flex flex-col justify-between {conn.status ===
                                  'Permanently Inactive'
                                    ? 'border-rose-500 bg-rose-50 dark:bg-rose-950/40 ring-1 ring-rose-500'
                                    : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:border-slate-300 dark:hover:border-slate-700'}"
                                >
                                  <div
                                    class="flex items-center justify-between mb-2"
                                  >
                                    <span
                                      class="h-3 w-3 rounded-full bg-rose-500 shadow-sm shadow-rose-500/50"
                                    ></span>
                                    <span
                                      class="text-[10px] font-mono text-rose-600 dark:text-rose-400 font-semibold uppercase"
                                    >
                                      {$language === "vi"
                                        ? "Trạng thái 03"
                                        : "State 03"}
                                    </span>
                                  </div>
                                  <div>
                                    <div
                                      class="font-bold text-sm text-rose-700 dark:text-rose-400"
                                    >
                                      {$language === "vi"
                                        ? "Ngắt kết nối vĩnh viễn"
                                        : "Permanently Inactive"}
                                    </div>
                                    <div
                                      class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5"
                                    >
                                      {$language === "vi"
                                        ? "Cổng đã ngắt, thu hồi thiết bị"
                                        : "Port disconnected, CPE reclaimed"}
                                    </div>
                                  </div>
                                </button>
                              </div>

                              <!-- Audit Note for Status Override -->
                              <div class="pt-1">
                                <label
                                  class="block text-xs font-mono text-slate-600 dark:text-slate-400 mb-1"
                                >
                                  {$language === "vi"
                                    ? "Ghi chú kỹ thuật / Lý do chuyển trạng thái (Nhật ký NOC):"
                                    : "Technician Reason / NOC Incident Log:"}
                                </label>
                                <div class="flex gap-2">
                                  <input
                                    type="text"
                                    placeholder={$language === "vi"
                                      ? "VD: Hoàn tất hàn nối sợi quang, mở lại cổng downstream..."
                                      : "e.g. Fiber loop repair complete, re-enabled downstream port..."}
                                    bind:value={statusChangeReason}
                                    class="flex-1 px-3 py-2 text-xs bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-slate-100 focus:outline-none focus:ring-1 focus:ring-amber-500"
                                  />
                                  {#if conn.status !== "Active"}
                                    <button
                                      onclick={() =>
                                        handleToggleConnectionStatusFor(
                                          conn,
                                          "Active",
                                        )}
                                      class="px-3 py-2 text-xs font-bold bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg transition"
                                    >
                                      {$language === "vi"
                                        ? "Mở lại mạch"
                                        : "Restore"}
                                    </button>
                                  {/if}
                                </div>
                              </div>
                            </div>

                            <!-- 2. TELEMETRY DIAGNOSTICS & METRICS -->
                            <div class="space-y-3">
                              <div
                                class="text-xs font-bold font-mono text-slate-700 dark:text-slate-300 uppercase tracking-wider flex items-center justify-between"
                              >
                                <span class="flex items-center gap-1.5">
                                  <Activity class="h-4 w-4 text-amber-500" />
                                  {$language === "vi"
                                    ? "Thông số Telemetry trực tiếp"
                                    : "Real-time Telemetry"}
                                </span>
                                <span
                                  class="text-[10px] text-emerald-600 dark:text-emerald-400 font-semibold animate-pulse"
                                >
                                  ● {$language === "vi" ? "Trực tiếp" : "LIVE"}
                                </span>
                              </div>

                              <div
                                class="grid grid-cols-2 gap-2 text-xs font-mono"
                              >
                                <div
                                  class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                                >
                                  <div
                                    class="text-slate-500 dark:text-slate-400 text-[10px]"
                                  >
                                    {$language === "vi"
                                      ? "ĐỘ TRỄ / PING"
                                      : "PING / LATENCY"}
                                  </div>
                                  <div
                                    class="text-base font-bold text-emerald-600 dark:text-emerald-400 mt-1"
                                  >
                                    4.2 ms
                                  </div>
                                  <div class="text-[10px] text-slate-400">
                                    Jitter: 0.3 ms
                                  </div>
                                </div>

                                <div
                                  class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                                >
                                  <div
                                    class="text-slate-500 dark:text-slate-400 text-[10px]"
                                  >
                                    {$language === "vi"
                                      ? "MẤT GÓI"
                                      : "PACKET LOSS"}
                                  </div>
                                  <div
                                    class="text-base font-bold text-emerald-600 dark:text-emerald-400 mt-1"
                                  >
                                    0.00 %
                                  </div>
                                  <div class="text-[10px] text-slate-400">
                                    10.000 frames OK
                                  </div>
                                </div>

                                <div
                                  class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                                >
                                  <div
                                    class="text-slate-500 dark:text-slate-400 text-[10px]"
                                  >
                                    {$language === "vi"
                                      ? "CÔNG SUẤT Rx"
                                      : "OPTICAL Rx"}
                                  </div>
                                  <div
                                    class="text-base font-bold text-amber-600 dark:text-amber-400 mt-1"
                                  >
                                    -16.8 dBm
                                  </div>
                                  <div class="text-[10px] text-slate-400">
                                    Tx: +2.1 dBm
                                  </div>
                                </div>

                                <div
                                  class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                                >
                                  <div
                                    class="text-slate-500 dark:text-slate-400 text-[10px]"
                                  >
                                    {$language === "vi"
                                      ? "CỔNG ĐỊNH TUYẾN"
                                      : "PORT ROUTING"}
                                  </div>
                                  <div
                                    class="text-xs font-bold text-slate-800 dark:text-slate-200 mt-1 truncate"
                                  >
                                    {conn.portNumber || "PON-01/04"}
                                  </div>
                                  <div
                                    class="text-[10px] text-slate-400 truncate"
                                  >
                                    {conn.ipAddress || "198.51.100.42"}
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>

                          <!-- 3. DETAILED HARDWARE & CONTRACT INFO -->
                          <div
                            class="pt-4 border-t border-slate-100 dark:border-slate-800"
                          >
                            <h5
                              class="text-xs font-mono font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider mb-3 flex items-center space-x-2"
                            >
                              <HardDrive class="h-4 w-4 text-slate-500" />
                              <span
                                >{$language === "vi"
                                  ? "Hồ sơ phần cứng thiết bị CPE & Hợp đồng thuê bao"
                                  : "CPE Hardware Profile & Subscriber Contract Data"}</span
                              >
                            </h5>

                            <div
                              class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs font-mono"
                            >
                              <div
                                class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                              >
                                <div
                                  class="text-slate-500 dark:text-slate-400 text-[10px]"
                                >
                                  {$language === "vi"
                                    ? "THIẾT BỊ GÁN CPE"
                                    : "CPE HARDWARE"}
                                </div>
                                <div
                                  class="font-semibold text-slate-900 dark:text-white mt-1 text-xs truncate"
                                  title={conn.assignedDeviceModel || "Chưa gán"}
                                >
                                  {conn.assignedDeviceModel ||
                                    "Nexus GigaFiber ONT"}
                                </div>
                                <div
                                  class="text-[10px] text-amber-600 dark:text-amber-400 mt-0.5"
                                >
                                  SN: {conn.assignedDeviceSerial || "N/A"}
                                </div>
                              </div>

                              <div
                                class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                              >
                                <div
                                  class="text-slate-500 dark:text-slate-400 text-[10px]"
                                >
                                  {$language === "vi"
                                    ? "GÓI CƯỚC THUÊ BAO"
                                    : "SERVICE PLAN"}
                                </div>
                                <div
                                  class="font-semibold text-slate-900 dark:text-white mt-1 text-xs truncate"
                                >
                                  {conn.planName}
                                </div>
                                <div
                                  class="text-[10px] text-emerald-600 dark:text-emerald-400 mt-0.5"
                                >
                                  ${conn.monthlyRental}/{$language === "vi"
                                    ? "tháng"
                                    : "mo"} • Deposit: ${conn.securityDeposit}
                                </div>
                              </div>

                              <div
                                class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                              >
                                <div
                                  class="text-slate-500 dark:text-slate-400 text-[10px]"
                                >
                                  {$language === "vi"
                                    ? "ĐƠN HÀNG GỐC"
                                    : "SOURCE ORDER"}
                                </div>
                                <div
                                  class="font-semibold text-slate-900 dark:text-white mt-1 text-xs"
                                >
                                  #{conn.orderId}
                                </div>
                                <div class="text-[10px] text-slate-400 mt-0.5">
                                  {$language === "vi"
                                    ? "Lắp đặt:"
                                    : "Installed:"}
                                  {conn.installedDate}
                                </div>
                              </div>

                              <div
                                class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800"
                              >
                                <div
                                  class="text-slate-500 dark:text-slate-400 text-[10px]"
                                >
                                  {$language === "vi"
                                    ? "NHẬT KÝ GẦN NHẤT"
                                    : "LAST AUDIT NOTE"}
                                </div>
                                <div
                                  class="font-semibold text-slate-800 dark:text-slate-200 mt-1 text-xs truncate"
                                  title={conn.lastStatusReason ||
                                    "Hoạt động bình thường"}
                                >
                                  {conn.lastStatusReason ||
                                    ($language === "vi"
                                      ? "Hoạt động bình thường"
                                      : "Normal operations")}
                                </div>
                                <div class="text-[10px] text-slate-400 mt-0.5">
                                  {$language === "vi"
                                    ? "Cập nhật:"
                                    : "Updated:"}
                                  {conn.lastUpdated}
                                </div>
                              </div>
                            </div>

                            <div
                              class="mt-4 rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden"
                            >
                              <div
                                class="px-3 py-2 bg-slate-50 dark:bg-slate-950 flex items-center justify-between"
                              >
                                <span
                                  class="text-xs font-bold text-slate-700 dark:text-slate-300 flex items-center gap-1.5"
                                >
                                  <History class="h-3.5 w-3.5 text-amber-500" />
                                  {$language === "vi"
                                    ? "Nhật ký thao tác kết nối"
                                    : "Connection activity log"}
                                </span>
                                <button
                                  onclick={() =>
                                    loadActivityLogs(conn.accountId)}
                                  disabled={activityLogsLoading ===
                                    conn.accountId}
                                  class="text-[11px] text-amber-600 hover:text-amber-700 disabled:opacity-50"
                                >
                                  {$language === "vi" ? "Tải lại" : "Reload"}
                                </button>
                              </div>
                              {#if activityLogsLoading === conn.accountId && !activityLogs[conn.accountId]}
                                <div class="p-3 text-xs text-slate-500">
                                  {$language === "vi"
                                    ? "Đang tải nhật ký..."
                                    : "Loading logs..."}
                                </div>
                              {:else if (activityLogs[conn.accountId] ?? []).length === 0}
                                <div class="p-3 text-xs text-slate-500">
                                  {$language === "vi"
                                    ? "Chưa có thao tác nào."
                                    : "No activity recorded."}
                                </div>
                              {:else}
                                <div
                                  class="divide-y divide-slate-100 dark:divide-slate-800 max-h-44 overflow-y-auto"
                                >
                                  {#each activityLogs[conn.accountId] ?? [] as log (log.logId)}
                                    <div
                                      class="px-3 py-2 text-xs flex items-start justify-between gap-4"
                                    >
                                      <div>
                                        <div
                                          class="font-semibold text-slate-800 dark:text-slate-200"
                                        >
                                          {log.actionType}: {log.oldValue ??
                                            "—"} → {log.newValue ?? "—"}
                                        </div>
                                        <div
                                          class="text-slate-500 dark:text-slate-400 mt-0.5"
                                        >
                                          {log.reason ?? "—"}
                                        </div>
                                      </div>
                                      <div
                                        class="text-right shrink-0 text-[10px] text-slate-500 font-mono"
                                      >
                                        <div>
                                          {new Date(
                                            log.timestamp,
                                          ).toLocaleString(
                                            $language === "vi"
                                              ? "vi-VN"
                                              : "en-US",
                                          )}
                                        </div>
                                        <div>
                                          {log.performedByName ??
                                            log.performedBy ??
                                            "System"}
                                        </div>
                                      </div>
                                    </div>
                                  {/each}
                                </div>
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

  <!-- TAB 3: EQUIPMENT TRACKER -->
  {#if activeTab === "equipment-tracker"}
    <div class="space-y-4">
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <div
            class="text-xs font-mono text-slate-500 dark:text-slate-400 uppercase"
          >
            {$language === "vi"
              ? "Tổng thiết bị CPE đã ghi nhận"
              : "Total Logged CPE Units"}
          </div>
          <div
            class="text-2xl font-bold font-mono text-slate-900 dark:text-white mt-1"
          >
            {$equipments.length}
            {$language === "vi" ? "Thiết bị" : "Units"}
          </div>
        </div>
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <div
            class="text-xs font-mono text-slate-500 dark:text-slate-400 uppercase"
          >
            {$language === "vi"
              ? "Đã lắp đặt / Trực tuyến"
              : "Deployed / In Service"}
          </div>
          <div
            class="text-2xl font-bold font-mono text-emerald-600 dark:text-emerald-400 mt-1"
          >
            {$equipments.filter((e) => e.status === "In Service").length}
            {$language === "vi" ? "Đang chạy" : "Online"}
          </div>
        </div>
        <div
          class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <div
            class="text-xs font-mono text-slate-500 dark:text-slate-400 uppercase"
          >
            {$language === "vi" ? "Sẵn có trong kho" : "Available In Stock"}
          </div>
          <div
            class="text-2xl font-bold font-mono text-amber-600 dark:text-amber-400 mt-1"
          >
            {$equipments.filter((e) => e.status === "In Stock").length}
            {$language === "vi" ? "Thiết bị" : "Units"}
          </div>
        </div>
      </div>

      <!-- Equipment Grid / Table -->
      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm font-sans">
            <thead
              class="bg-slate-50 dark:bg-slate-950/80 text-xs font-mono text-slate-600 dark:text-slate-400 uppercase tracking-wider border-b border-slate-200 dark:border-slate-800"
            >
              <tr>
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Số Serial phần cứng"
                    : "Hardware Serial"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Địa chỉ MAC" : "MAC Address"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Model & Loại thiết bị"
                    : "Device Model & Type"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Bản dựng Firmware"
                    : "Firmware Build"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Mã Account ID (16 ký tự)"
                    : "Assigned 16-char Account"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi" ? "Thuê bao" : "Subscriber"}</th
                >
                <th class="px-4 py-3"
                  >{$language === "vi"
                    ? "Trạng thái thiết bị"
                    : "Hardware Status"}</th
                >
              </tr>
            </thead>
            <tbody
              class="divide-y divide-slate-200 dark:divide-slate-800/60 text-xs font-mono"
            >
              {#each $equipments as eq (eq.id)}
                <tr
                  class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition"
                >
                  <td
                    class="px-4 py-3 font-bold text-amber-600 dark:text-amber-400"
                    >{eq.serialNumber}</td
                  >
                  <td class="px-4 py-3 text-slate-600 dark:text-slate-400"
                    >{eq.macAddress}</td
                  >
                  <td class="px-4 py-3 font-sans">
                    <div class="font-semibold text-slate-900 dark:text-white">
                      {eq.deviceModel}
                    </div>
                    <div class="text-[11px] text-slate-500 dark:text-slate-400">
                      {eq.deviceType}
                    </div>
                  </td>
                  <td class="px-4 py-3 text-slate-500 dark:text-slate-400"
                    >{eq.firmwareVersion}</td
                  >
                  <td
                    class="px-4 py-3 text-emerald-600 dark:text-emerald-400 font-bold"
                  >
                    {#if eq.assignedAccountId}
                      {eq.assignedAccountId}
                    {:else}
                      <span
                        class="text-slate-400 dark:text-slate-600 font-normal"
                      >
                        {$language === "vi" ? "CHƯA GÁN" : "UNASSIGNED"}
                      </span>
                    {/if}
                  </td>
                  <td
                    class="px-4 py-3 font-sans text-slate-700 dark:text-slate-300"
                    >{eq.assignedCustomerName || "—"}</td
                  >
                  <td class="px-4 py-3 font-sans">
                    <span
                      class="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold {eq.status ===
                      'In Service'
                        ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800'
                        : eq.status === 'In Stock'
                          ? 'bg-blue-100 dark:bg-blue-950 text-blue-800 dark:text-blue-300 border border-blue-300 dark:border-blue-800'
                          : 'bg-rose-100 dark:bg-rose-950 text-rose-800 dark:text-rose-300 border border-rose-300 dark:border-rose-800'}"
                    >
                      {$language === "vi"
                        ? eq.status === "In Service"
                          ? "Đang lắp đặt"
                          : eq.status === "In Stock"
                            ? "Trong kho"
                            : "Bảo hành RMA"
                        : eq.status}
                    </span>
                    {#if eq.status !== "In Service"}
                      <select
                        value={eq.status}
                        onchange={(event) =>
                          handleEquipmentStatusChange(
                            eq,
                            event.currentTarget.value as Equipment["status"],
                          )}
                        disabled={pendingAction === `equipment:${eq.id}`}
                        class="block mt-1.5 w-full min-w-28 px-2 py-1 rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-950 text-[11px] text-slate-700 dark:text-slate-300"
                      >
                        <option value="In Stock">In Stock</option>
                        <option value="Maintenance">Maintenance</option>
                        <option value="Faulty">Faulty</option>
                      </select>
                    {/if}
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>

      <div
        class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
      >
        <div
          class="px-4 py-3 border-b border-slate-200 dark:border-slate-800 flex items-center gap-2"
        >
          <PackageOpen class="h-4 w-4 text-amber-500" />
          <h3 class="text-sm font-bold text-slate-900 dark:text-white">
            {$language === "vi"
              ? "Tồn kho vật tư kỹ thuật"
              : "Technical inventory"}
          </h3>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs">
            <thead
              class="bg-slate-50 dark:bg-slate-950 text-slate-500 uppercase font-mono"
            >
              <tr>
                <th class="px-4 py-2.5"
                  >{$language === "vi" ? "Mã vật tư" : "Item"}</th
                >
                <th class="px-4 py-2.5"
                  >{$language === "vi" ? "Tên" : "Name"}</th
                >
                <th class="px-4 py-2.5"
                  >{$language === "vi" ? "Vị trí" : "Location"}</th
                >
                <th class="px-4 py-2.5 text-right"
                  >{$language === "vi"
                    ? "Tồn / Đặt lại"
                    : "Stock / Reorder"}</th
                >
                <th class="px-4 py-2.5 text-right"
                  >{$language === "vi" ? "Điều chỉnh" : "Adjust"}</th
                >
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              {#each $inventory as item (item.id)}
                <tr
                  class:item-low-stock={item.stockQuantity <= item.reorderLevel}
                >
                  <td class="px-4 py-3 font-mono font-semibold text-amber-600"
                    >{item.itemCode}</td
                  >
                  <td class="px-4 py-3">
                    <div class="font-medium text-slate-900 dark:text-white">
                      {item.name}
                    </div>
                    <div class="text-[10px] text-slate-500">
                      {item.supplier || "—"}
                    </div>
                  </td>
                  <td class="px-4 py-3 text-slate-600 dark:text-slate-400"
                    >{item.location || "—"}</td
                  >
                  <td class="px-4 py-3 text-right font-mono">
                    <span
                      class:item-low-stock={item.stockQuantity <=
                        item.reorderLevel}
                      class="font-bold">{item.stockQuantity}</span
                    >
                    <span class="text-slate-400"> / {item.reorderLevel}</span>
                  </td>
                  <td class="px-4 py-3 text-right space-x-1">
                    <button
                      onclick={() => openStockModal(item.id, -1)}
                      class="px-2 py-1 rounded border border-rose-200 text-rose-600 hover:bg-rose-50"
                      >− Xuất</button
                    >
                    <button
                      onclick={() => openStockModal(item.id, 1)}
                      class="px-2 py-1 rounded border border-emerald-200 text-emerald-600 hover:bg-emerald-50"
                      >+ Nhập</button
                    >
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  {/if}

  <!-- INSTALLATION SCHEDULE MODAL -->
  {#if isScheduleModalOpen && targetOrderForSchedule}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 backdrop-blur-sm p-4"
    >
      <div
        class="w-full max-w-md rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl"
      >
        <div
          class="flex items-center justify-between border-b border-slate-200 dark:border-slate-800 pb-3 mb-4"
        >
          <h3
            class="font-bold text-slate-900 dark:text-white flex items-center gap-2"
          >
            <CalendarClock class="h-5 w-5 text-blue-500" />
            {$language === "vi" ? "Lên lịch lắp đặt" : "Schedule installation"}
          </h3>
          <button
            onclick={() => (isScheduleModalOpen = false)}
            class="text-slate-400 hover:text-slate-700"
            ><X class="h-5 w-5" /></button
          >
        </div>
        <form onsubmit={handleSaveSchedule} class="space-y-4">
          <div class="text-xs text-slate-600 dark:text-slate-400">
            <div>
              <strong>#{targetOrderForSchedule.id}</strong> — {targetOrderForSchedule.customerName}
            </div>
            <div class="mt-1">{targetOrderForSchedule.installationAddress}</div>
          </div>
          <div>
            <label
              for="scheduled-install-date"
              class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Ngày giờ lắp đặt"
                : "Installation date and time"}
            </label>
            <input
              id="scheduled-install-date"
              type="datetime-local"
              required
              min={toLocalDateTimeInput(new Date())}
              bind:value={scheduledInstallDate}
              class="w-full px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm"
            />
          </div>
          <div class="text-xs text-slate-500">
            {$language === "vi"
              ? `Kỹ thuật viên: ${technicalEmployeeId}`
              : `Technician: ${technicalEmployeeId}`}
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onclick={() => (isScheduleModalOpen = false)}
              class="px-4 py-2 rounded-lg border border-slate-200 dark:border-slate-700 text-xs"
              >{$language === "vi" ? "Hủy" : "Cancel"}</button
            >
            <button
              type="submit"
              disabled={pendingAction.startsWith("schedule:")}
              class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold disabled:opacity-50"
              >{$language === "vi" ? "Lưu lịch" : "Save schedule"}</button
            >
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- INVENTORY ADJUSTMENT MODAL -->
  {#if isStockModalOpen}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 backdrop-blur-sm p-4"
    >
      <div
        class="w-full max-w-md rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl"
      >
        <div
          class="flex items-center justify-between border-b border-slate-200 dark:border-slate-800 pb-3 mb-4"
        >
          <h3
            class="font-bold text-slate-900 dark:text-white flex items-center gap-2"
          >
            <PackageOpen class="h-5 w-5 text-amber-500" />
            {$language === "vi" ? "Điều chỉnh tồn kho" : "Adjust inventory"}
          </h3>
          <button
            onclick={() => (isStockModalOpen = false)}
            class="text-slate-400 hover:text-slate-700"
            ><X class="h-5 w-5" /></button
          >
        </div>
        <form onsubmit={handleAdjustStock} class="space-y-4">
          <div>
            <label
              for="stock-change"
              class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
            >
              {$language === "vi"
                ? "Số lượng thay đổi (âm là xuất kho)"
                : "Quantity change (negative removes stock)"}
            </label>
            <input
              id="stock-change"
              type="number"
              required
              min="-1000000"
              max="1000000"
              bind:value={stockQuantityChange}
              class="w-full px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm"
            />
          </div>
          <div>
            <label
              for="stock-reason"
              class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >{$language === "vi" ? "Lý do" : "Reason"}</label
            >
            <textarea
              id="stock-reason"
              required
              maxlength="500"
              rows="3"
              bind:value={stockAdjustmentReason}
              class="w-full px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm"
            ></textarea>
          </div>
          <div class="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onclick={() => (isStockModalOpen = false)}
              class="px-4 py-2 rounded-lg border border-slate-200 dark:border-slate-700 text-xs"
              >{$language === "vi" ? "Hủy" : "Cancel"}</button
            >
            <button
              type="submit"
              disabled={stockQuantityChange === 0 ||
                pendingAction.startsWith("inventory:")}
              class="px-4 py-2 rounded-lg bg-amber-600 hover:bg-amber-700 text-white text-xs font-bold disabled:opacity-50"
              >{$language === "vi" ? "Xác nhận" : "Confirm"}</button
            >
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- PROVISIONING MODAL ("Connection Provided") -->
  {#if isProvisionModalOpen && targetOrderForProvision}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4 overflow-y-auto"
    >
      <div
        class="w-full max-w-xl rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl space-y-4 my-8"
      >
        <div
          class="flex items-center justify-between border-b border-slate-200 dark:border-slate-800 pb-3"
        >
          <div>
            <span
              class="text-xs font-mono text-amber-600 dark:text-amber-400 uppercase font-semibold"
            >
              {$language === "vi"
                ? "Kỹ sư hiện trường duyệt & cấp kết nối"
                : "Field Engineer Circuit Provisioning"}
            </span>
            <h3 class="text-lg font-bold text-slate-900 dark:text-white">
              {$language === "vi"
                ? `Cung cấp kết nối cho Đơn #${targetOrderForProvision.id}`
                : `Provision Connection for Order #${targetOrderForProvision.id}`}
            </h3>
          </div>
          <button
            onclick={() => (isProvisionModalOpen = false)}
            class="text-slate-400 hover:text-slate-700 dark:hover:text-white"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form
          onsubmit={handleConfirmConnectionProvided}
          class="space-y-4 text-sm"
        >
          <!-- Order Summary Card -->
          <div
            class="p-3.5 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-2 text-xs text-slate-600 dark:text-slate-300"
          >
            <div class="grid grid-cols-2 gap-2">
              <div>
                {$language === "vi" ? "Thuê bao:" : "Subscriber:"}
                <div class="font-semibold text-slate-900 dark:text-white">
                  {targetOrderForProvision.customerName}
                </div>
              </div>
              <div>
                {$language === "vi" ? "Gói cước:" : "Plan:"}
                <div class="font-semibold text-amber-600 dark:text-amber-400">
                  {targetOrderForProvision.planName} ({targetOrderForProvision.connectionType})
                </div>
              </div>
            </div>
            <div>
              {$language === "vi" ? "Địa chỉ lắp đặt:" : "Address:"}
              <div class="font-medium text-slate-900 dark:text-white">
                {targetOrderForProvision.installationAddress}
              </div>
            </div>

            <!-- Multi-connection Stepper -->
            {#if targetOrderTotalConns > 1}
              <div
                class="pt-2 border-t border-slate-200 dark:border-slate-800/80 space-y-1.5"
              >
                <div
                  class="flex items-center justify-between font-mono text-[11px]"
                >
                  <span class="text-slate-500 dark:text-slate-400">
                    {$language === "vi"
                      ? "Tiến độ duyệt từng mạch:"
                      : "Circuit approval progress:"}
                  </span>
                  <span class="font-bold text-amber-600 dark:text-amber-400">
                    {provisionedForCurrentOrder.length} / {targetOrderTotalConns}
                    {$language === "vi" ? "đã xong" : "completed"}
                  </span>
                </div>
                <div class="grid grid-flow-col gap-1.5 auto-cols-fr">
                  {#each Array(targetOrderTotalConns) as _, i}
                    <div
                      class="h-2 rounded-full transition-all duration-300 {i <
                      provisionedForCurrentOrder.length
                        ? 'bg-emerald-500'
                        : i === provisionedForCurrentOrder.length
                          ? 'bg-amber-500 ring-2 ring-amber-300 dark:ring-amber-900 animate-pulse'
                          : 'bg-slate-200 dark:bg-slate-700'}"
                      title={$language === "vi"
                        ? `Mạch #${i + 1}`
                        : `Circuit #${i + 1}`}
                    ></div>
                  {/each}
                </div>
              </div>
            {/if}
          </div>

          <!-- Previously Approved Connections for this Order -->
          {#if provisionedForCurrentOrder.length > 0}
            <div
              class="p-3 rounded-lg bg-emerald-50/50 dark:bg-emerald-950/20 border border-emerald-200 dark:border-emerald-800/40 space-y-1.5"
            >
              <div
                class="text-[11px] font-semibold text-emerald-800 dark:text-emerald-300 flex items-center justify-between"
              >
                <span
                  >{$language === "vi"
                    ? `Các kết nối đã duyệt thành công (${provisionedForCurrentOrder.length}/${targetOrderTotalConns}):`
                    : `Approved circuits (${provisionedForCurrentOrder.length}/${targetOrderTotalConns}):`}</span
                >
                <span class="text-[10px] font-mono font-normal">Active</span>
              </div>
              <div class="space-y-1 max-h-28 overflow-y-auto pr-1">
                {#each provisionedForCurrentOrder as c, idx}
                  <div
                    class="flex items-center justify-between text-[11px] font-mono p-1.5 rounded bg-white dark:bg-slate-900 border border-emerald-200/60 dark:border-emerald-800/40"
                  >
                    <div class="flex items-center space-x-1.5">
                      <span
                        class="w-4 h-4 rounded-full bg-emerald-500 text-white text-[9px] flex items-center justify-center font-bold"
                        >✓</span
                      >
                      <span class="font-bold text-slate-900 dark:text-white"
                        >Mạch #{idx + 1}: {c.accountId}</span
                      >
                    </div>
                    <div class="text-[10px] text-slate-500 dark:text-slate-400">
                      Router: <span
                        class="text-amber-600 dark:text-amber-400 font-semibold"
                        >{c.assignedDeviceSerial}</span
                      >
                      ({c.portNumber})
                    </div>
                  </div>
                {/each}
              </div>
            </div>
          {/if}

          <!-- Current Circuit Assignment Section -->
          <div
            class="p-3.5 rounded-lg bg-amber-50/40 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800/60 space-y-3"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center space-x-2">
                <span
                  class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-amber-600 text-white font-mono font-bold text-xs"
                >
                  {currentProvisioningIndex}
                </span>
                <span
                  class="text-xs font-bold text-amber-900 dark:text-amber-200"
                >
                  {$language === "vi"
                    ? `Duyệt Mạch #${currentProvisioningIndex}/${targetOrderTotalConns}`
                    : `Approve Circuit #${currentProvisioningIndex}/${targetOrderTotalConns}`}
                </span>
              </div>
              <span
                class="text-[10px] font-mono px-2 py-0.5 rounded-full bg-amber-100 dark:bg-amber-900/60 text-amber-800 dark:text-amber-200 border border-amber-300 dark:border-amber-700"
              >
                Port: ETH-PORT-{currentProvisioningIndex}
              </span>
            </div>

            <div class="text-[11px] text-slate-600 dark:text-slate-400">
              {$language === "vi"
                ? "Mỗi kết nối yêu cầu 1 thiết bị Modem / Router độc lập từ kho hàng Nexus."
                : "Each connection requires 1 dedicated modem/router unit from inventory."}
            </div>

            <div>
              <label
                class="block text-xs font-mono font-semibold text-slate-700 dark:text-slate-300 mb-1"
              >
                {$language === "vi"
                  ? targetOrderTotalConns > 1
                    ? `Chọn Router / Modem cho Mạch #${currentProvisioningIndex} *`
                    : "Gán Modem / Router từ kho thiết bị *"
                  : targetOrderTotalConns > 1
                    ? `Assign Router / Modem for Circuit #${currentProvisioningIndex} *`
                    : "Assign Modem / Router from Stock *"}
              </label>
              {#if provisioningEquipments.length > 0}
                <select
                  bind:value={selectedDeviceSerial}
                  class="w-full px-3 py-2 bg-white dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-xs font-mono text-slate-900 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-amber-500"
                >
                  {#each provisioningEquipments as eq (eq.id)}
                    <option value={eq.serialNumber}
                      >{eq.serialNumber} — {eq.deviceModel} ({eq.macAddress})</option
                    >
                  {/each}
                </select>
                <div
                  class="text-[10px] text-slate-500 dark:text-slate-400 mt-1"
                >
                  {$language === "vi"
                    ? `Hiện có ${provisioningEquipments.length} thiết bị sẵn sàng tại cửa hàng này.`
                    : `${provisioningEquipments.length} hardware units available at this store.`}
                </div>
              {:else}
                <div
                  class="p-3 rounded bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs"
                >
                  {$language === "vi"
                    ? "Không có thiết bị nào trong kho sẵn sàng. Vui lòng nhập thêm thiết bị."
                    : "No hardware units currently marked as 'In Stock'. Please register or return equipment."}
                </div>
              {/if}
            </div>
          </div>

          <!-- Actions -->
          <div
            class="flex items-center justify-between pt-3 border-t border-slate-200 dark:border-slate-800"
          >
            <button
              type="button"
              onclick={() => (isProvisionModalOpen = false)}
              class="px-4 py-2 rounded-lg text-xs border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
            >
              {$language === "vi"
                ? provisionedForCurrentOrder.length > 0
                  ? "Tạm dừng & Đóng"
                  : "Hủy bỏ"
                : provisionedForCurrentOrder.length > 0
                  ? "Pause & Close"
                  : "Cancel"}
            </button>
            <button
              type="submit"
              disabled={!selectedDeviceSerial}
              class="px-4 py-2 rounded-lg text-xs font-bold bg-amber-600 hover:bg-amber-700 text-white transition shadow disabled:opacity-50 flex items-center space-x-1.5"
            >
              {#if currentProvisioningIndex < targetOrderTotalConns}
                <span>
                  {$language === "vi"
                    ? `Duyệt Mạch #${currentProvisioningIndex} & Tiếp tục (#${currentProvisioningIndex + 1}) ➔`
                    : `Approve #${currentProvisioningIndex} & Next (#${currentProvisioningIndex + 1}) ➔`}
                </span>
              {:else}
                <span>
                  {$language === "vi"
                    ? targetOrderTotalConns > 1
                      ? `Duyệt Mạch #${currentProvisioningIndex} & Hoàn tất Đơn hàng ✓`
                      : "Xác nhận cấp & Kích hoạt ✓"
                    : targetOrderTotalConns > 1
                      ? `Approve #${currentProvisioningIndex} & Complete Order ✓`
                      : "Confirm Provisioning & Activate ✓"}
                </span>
              {/if}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- NOT FEASIBLE REJECTION MODAL -->
  {#if isNotFeasibleModalOpen && targetOrderForRejection}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4"
    >
      <div
        class="w-full max-w-md rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl space-y-4"
      >
        <div
          class="flex items-center justify-between border-b border-slate-200 dark:border-slate-800 pb-3"
        >
          <h3
            class="text-base font-bold text-rose-600 dark:text-rose-400 flex items-center space-x-2"
          >
            <ShieldAlert class="h-5 w-5" />
            <span
              >{$language === "vi"
                ? "Báo cáo không khả thi kỹ thuật"
                : "Flag Technical Infeasibility"}</span
            >
          </h3>
          <button
            onclick={() => (isNotFeasibleModalOpen = false)}
            class="text-slate-400 hover:text-slate-700 dark:hover:text-white"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form onsubmit={handleConfirmNotFeasible} class="space-y-4 text-sm">
          <div class="text-xs text-slate-600 dark:text-slate-400">
            {$language === "vi"
              ? `Ghi nhận lý do kỹ thuật từ chối Đơn hàng #${targetOrderForRejection.id}:`
              : `Record engineering rationale for declining Order #${targetOrderForRejection.id}:`}
          </div>

          <textarea
            rows="3"
            required
            bind:value={rejectionReason}
            class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-xs text-slate-900 dark:text-slate-100 focus:outline-none focus:ring-1 focus:ring-rose-500 font-mono"
          ></textarea>

          <div class="flex justify-end space-x-3 pt-2">
            <button
              type="button"
              onclick={() => (isNotFeasibleModalOpen = false)}
              class="px-4 py-2 rounded-lg text-xs border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
            >
              {$language === "vi" ? "Hủy bỏ" : "Cancel"}
            </button>
            <button
              type="submit"
              class="px-4 py-2 rounded-lg text-xs font-bold bg-rose-600 hover:bg-rose-700 text-white transition shadow"
            >
              {$language === "vi"
                ? "Gửi báo cáo không khả thi"
                : "Submit Infeasibility Flag"}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <!-- REGISTER NEW EQUIPMENT MODAL -->
  {#if isAddEquipmentModalOpen}
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4"
    >
      <div
        class="w-full max-w-md rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl space-y-4"
      >
        <div
          class="flex items-center justify-between border-b border-slate-200 dark:border-slate-800 pb-3"
        >
          <h3 class="text-base font-bold text-slate-900 dark:text-white">
            {$language === "vi"
              ? "Đăng ký thiết bị phần cứng vào kho"
              : "Log Hardware into Equipment Stock"}
          </h3>
          <button
            onclick={() => (isAddEquipmentModalOpen = false)}
            class="text-slate-400 hover:text-slate-700 dark:hover:text-white"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <form
          onsubmit={handleSaveEquipment}
          class="space-y-3 text-xs font-mono"
        >
          <div class="grid grid-cols-2 gap-2 font-sans">
            <div>
              <label class="block text-slate-600 dark:text-slate-400 mb-1">
                {$language === "vi" ? "Mặt hàng kho *" : "Inventory item *"}
              </label>
              <select
                required
                bind:value={newEquipmentForm.inventoryId}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-slate-100"
              >
                <option value="" disabled>
                  {$language === "vi" ? "Chọn mặt hàng" : "Select item"}
                </option>
                {#each $inventory.filter((item) => item.vendorId) as item (item.id)}
                  <option value={item.id}>{item.itemCode} — {item.name}</option>
                {/each}
              </select>
            </div>
            <div>
              <label class="block text-slate-600 dark:text-slate-400 mb-1">
                {$language === "vi" ? "Cửa hàng quản lý *" : "Assigned store *"}
              </label>
              <select
                required
                bind:value={newEquipmentForm.storeId}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-slate-100"
              >
                <option value="" disabled>
                  {$language === "vi" ? "Chọn cửa hàng" : "Select store"}
                </option>
                {#each availableStoreIds as storeId (storeId)}
                  <option value={storeId}>{storeId}</option>
                {/each}
              </select>
            </div>
          </div>

          <div>
            <label class="block text-slate-600 dark:text-slate-400 mb-1">
              {$language === "vi" ? "Số Serial phần cứng *" : "Serial Number *"}
            </label>
            <input
              type="text"
              required
              bind:value={newEquipmentForm.serialNumber}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-amber-600 dark:text-amber-300 font-bold"
            />
          </div>

          <div>
            <label class="block text-slate-600 dark:text-slate-400 mb-1">
              {$language === "vi" ? "Địa chỉ MAC *" : "MAC Address *"}
            </label>
            <input
              type="text"
              required
              bind:value={newEquipmentForm.macAddress}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-slate-100"
            />
          </div>

          <div>
            <label
              class="block text-slate-600 dark:text-slate-400 mb-1 font-sans"
            >
              {$language === "vi" ? "Tên Model thiết bị" : "Device Model Name"}
            </label>
            <input
              type="text"
              required
              bind:value={newEquipmentForm.deviceModel}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-slate-100 font-sans"
            />
          </div>

          <div class="grid grid-cols-2 gap-2">
            <div>
              <label
                class="block text-slate-600 dark:text-slate-400 mb-1 font-sans"
              >
                {$language === "vi" ? "Loại thiết bị" : "Device Type"}
              </label>
              <select
                bind:value={newEquipmentForm.deviceType}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-slate-100 font-sans"
              >
                <option value="Gigabit Router">Gigabit Router</option>
                <option value="Fiber ONT Modem">Fiber ONT Modem</option>
                <option value="VDSL2/ADSL Modem">VDSL2/ADSL Modem</option>
                <option value="Analog Telephone Adapter"
                  >Analog Telephone Adapter</option
                >
              </select>
            </div>
            <div>
              <label class="block text-slate-600 dark:text-slate-400 mb-1">
                {$language === "vi" ? "Phiên bản Firmware" : "Firmware Version"}
              </label>
              <input
                type="text"
                bind:value={newEquipmentForm.firmwareVersion}
                class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-slate-100"
              />
            </div>
          </div>

          <div
            class="flex justify-end space-x-3 pt-3 border-t border-slate-200 dark:border-slate-800 font-sans"
          >
            <button
              type="button"
              onclick={() => (isAddEquipmentModalOpen = false)}
              class="px-4 py-2 rounded-lg text-xs border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
            >
              {$language === "vi" ? "Hủy bỏ" : "Cancel"}
            </button>
            <button
              type="submit"
              disabled={pendingAction === "create-equipment"}
              class="px-4 py-2 rounded-lg text-xs font-bold bg-amber-600 hover:bg-amber-700 text-white transition shadow"
            >
              {pendingAction === "create-equipment"
                ? $language === "vi"
                  ? "Đang lưu..."
                  : "Saving..."
                : $language === "vi"
                  ? "Xác nhận & Lưu kho"
                  : "Confirm & Save"}
            </button>
          </div>
        </form>
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

<!-- POPUP MODAL: CHI TIẾT BẢN GHI KIỂM TRA KẾT NỐI MẠNG CỦA KHÁCH HÀNG -->
<ConnectionTestModal
  isOpen={isTestRecordsModalOpen}
  connection={targetConnectionForTestModal}
  connectionsList={$connections}
  onClose={() => {
    isTestRecordsModalOpen = false;
    targetConnectionForTestModal = null;
  }}
/>
