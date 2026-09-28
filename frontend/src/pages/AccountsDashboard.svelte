<script lang="ts">
  // Mirrors pages/AccountsDashboard.tsx of the React original.
  import { nexusStore } from '../context/NexusContext';
  import { languageStore } from '../context/LanguageContext';
  import DashboardLayout from '../components/layout/DashboardLayout.svelte';
  import SettingsView from '../components/common/SettingsView.svelte';
  import ProfileView from '../components/common/ProfileView.svelte';
  import type { NavItem } from '../components/layout/DashboardLayout.svelte';
  import {
    Receipt, CreditCard, Settings, Search, CheckCircle2, Printer, X, Building, Plus, Users,
    History, Clock, Calendar, ShieldCheck, Wallet, ArrowUpRight, Eye, RefreshCw, Smartphone, QrCode, Filter, AlertCircle, Bell, Play,
    Network, ArrowDownUp, ExternalLink, FileText, ChevronRight, Phone, Mail, MapPin, Radio, Wifi
  } from 'lucide-svelte';
  import type { Bill, PaymentRecord, Connection } from '../types/nexus';
  import { toast } from 'svelte-sonner';
  import { queryParam, activeTabOverride } from '../lib/router';
  import { cleanPlanName } from '../lib/planI18n';

  type AccountsTab = 'connections' | 'bill-generation' | 'subscriber-tracking' | 'charge-settings' | 'settings' | 'profile';

  const { connections, orders, bills, generateBill, recordPayment, updateBill, updateConnectionStatus, settings, updateSettings } = nexusStore;
  const { t, language } = languageStore;

  // Active navigation tab
  let activeTab = $state<AccountsTab>('connections');

  // Customer Payment History tracking state
  interface CustomerPaymentEntry extends PaymentRecord {
    customerName: string;
    accountId: string;
    invoiceNumber: string;
    planName: string;
  }

  let customerPaymentSearch = $state('');
  let historyModalBill = $state<Bill | null>(null);
  let receiptModalPayment = $state<CustomerPaymentEntry | null>(null);

  // ===== Edit bill details (spec: "updating has to be done by the accounts
  // department alone" — PUT /api/billing/bills/{id} qua updateBill của store) =====
  let editBillModal = $state(false);
  let editingBill = $state<Bill | null>(null);
  let isSavingBill = $state(false);
  const editBillForm = $state({
    billingMonth: '',
    dueDate: '',
    securityDeposit: 0,
    monthlyRental: 0,
    hourlyCharges: 0,
    discountPercent: 0,
    discountAmount: 0,
    serviceTaxRate: 12.24,
    lateFeeAmount: 0,
  });

  function handleOpenEditBill(bill: Bill) {
    editingBill = bill;
    editBillForm.billingMonth = bill.billingMonth || '';
    editBillForm.dueDate = bill.dueDate || '';
    editBillForm.securityDeposit = bill.securityDeposit ?? 0;
    editBillForm.monthlyRental = bill.monthlyRental ?? 0;
    editBillForm.hourlyCharges = bill.hourlyCharges ?? 0;
    editBillForm.discountPercent = bill.discountPercent ?? 0;
    editBillForm.discountAmount = bill.discountAmount ?? 0;
    editBillForm.serviceTaxRate = bill.serviceTaxRate ?? 12.24;
    editBillForm.lateFeeAmount = (bill as any).lateFeeAmount ?? 0;
    editBillModal = true;
  }

  // Dự đoán tổng mới theo đúng công thức backend (cọc + cước tháng + cước giờ
  // − chiết khấu) × (1 + thuế%) + phí trễ, để kế toán xem trước trước khi lưu.
  const editBillPreviewTotal = $derived.by(() => {
    const subtotal =
      editBillForm.securityDeposit + editBillForm.monthlyRental + editBillForm.hourlyCharges - editBillForm.discountAmount;
    const tax = subtotal * (editBillForm.serviceTaxRate / 100);
    return subtotal + tax + editBillForm.lateFeeAmount;
  });

  async function handleSaveEditBill() {
    if (!editingBill) return;
    isSavingBill = true;
    try {
      const saved = await updateBill(editingBill.invoiceNumber, {
        billingMonth: editBillForm.billingMonth.trim() || undefined,
        dueDate: editBillForm.dueDate || undefined,
        securityDeposit: editBillForm.securityDeposit,
        monthlyRental: editBillForm.monthlyRental,
        hourlyCharges: editBillForm.hourlyCharges,
        discountPercent: editBillForm.discountPercent,
        discountAmount: editBillForm.discountAmount,
        serviceTaxRate: editBillForm.serviceTaxRate,
        lateFeeAmount: editBillForm.lateFeeAmount,
      });
      if (!saved) throw new Error('bill not found');
      editBillModal = false;
      toast.success(
        $language === 'vi'
          ? `Đã cập nhật hóa đơn ${editingBill.invoiceNumber} và tính lại tổng cước!`
          : `Invoice ${editingBill.invoiceNumber} updated and totals recalculated!`
      );
    } catch (err) {
      toast.error(
        $language === 'vi'
          ? `Không cập nhật được hóa đơn: ${err instanceof Error ? err.message : 'lỗi không xác định'}`
          : `Failed to update invoice: ${err instanceof Error ? err.message : 'unknown error'}`
      );
    } finally {
      isSavingBill = false;
    }
  }

  const allCustomerPayments = $derived<CustomerPaymentEntry[]>(
    $bills.flatMap((b) =>
      (b.paymentHistory || []).map((p) => ({
        ...p,
        customerName: b.customerName,
        accountId: b.accountId,
        invoiceNumber: b.invoiceNumber,
        planName: b.planName,
      }))
    )
  );

  const totalCollectedRevenue = $derived(
    Number(allCustomerPayments.reduce((acc, p) => acc + p.amountPaid, 0).toFixed(2))
  );

  const filteredCustomerPayments = $derived(
    allCustomerPayments.filter((p) => {
      if (!customerPaymentSearch.trim()) return true;
      const q = customerPaymentSearch.toLowerCase();
      return (
        p.customerName.toLowerCase().includes(q) ||
        p.accountId.toLowerCase().includes(q) ||
        p.invoiceNumber.toLowerCase().includes(q) ||
        p.paymentId.toLowerCase().includes(q) ||
        p.referenceNumber.toLowerCase().includes(q) ||
        p.paymentMode.toLowerCase().includes(q)
      );
    })
  );

  // Subscriber Tracking filter & reminder state
  type BillStatusFilter = 'all' | 'Unpaid' | 'Partially Paid' | 'Paid';
  let subscriberStatusFilter = $state<BillStatusFilter>('all');
  let remindedBillIds = $state<Record<string, boolean>>({});

  const filteredBills = $derived(
    $bills.filter((b) => {
      if (subscriberStatusFilter === 'all') return true;
      return b.status === subscriberStatusFilter;
    })
  );

  function handleSendReminder(bill: Bill) {
    remindedBillIds[bill.id] = true;
    toast.success(
      $language === 'vi'
        ? `Đã tự động gửi thông báo nhắc cước đến khách hàng ${bill.customerName} (${bill.accountId}) qua SMS & Email!`
        : `Automated payment reminder notification sent to ${bill.customerName} (${bill.accountId}) via SMS & Email!`
    );
  }

  function handleSendBulkReminders() {
    const unpaidList = $bills.filter((b) => b.status !== 'Paid');
    unpaidList.forEach((b) => {
      remindedBillIds[b.id] = true;
    });
    toast.success(
      $language === 'vi'
        ? `Đã gửi thông báo nhắc nhở thanh toán tự động đến toàn bộ ${unpaidList.length} khách hàng chưa thanh toán đủ!`
        : `Dispatched automated payment reminder alerts to all ${unpaidList.length} outstanding subscribers!`
    );
  }

  // Automated Reminder & Auto-Pay Scheduling Configuration state
  let showScheduleConfigModal = $state(false);

  // 1. Reminder timing settings
  let reminderEnabled = $state(true);
  let reminderDaysBeforeDue = $state(3); // 1, 3, 5, 7 days
  let remindOnDueDate = $state(true);
  let remindOverdueInterval = $state(3); // 0 (off), 2, 3, 5 days
  let reminderDailyTime = $state('09:00');
  let reminderChannelSms = $state(true);
  let reminderChannelEmail = $state(true);
  let reminderChannelInApp = $state(true);

  // 2. Auto-pay settings
  let autoPayEnabled = $state(true);
  let autoPayScheduleType = $state<'days_before_due' | 'on_due_date' | 'fixed_day' | 'bill_issue_date'>('days_before_due');
  let autoPayDaysBefore = $state(1); // 1, 2, 3 days
  let autoPayFixedDay = $state(5); // day 5 of month
  let autoPayAllowPartial = $state(true); // allow partial debit if insufficient funds
  let autoPayRetryFailed = $state(true); // retry after 24h
  let autoPayNotifyCustomer = $state(true); // send confirmation receipt

  function handleSaveScheduleSettings() {
    showScheduleConfigModal = false;
    toast.success(
      $language === 'vi'
        ? 'Đã lưu cài đặt thời gian nhắc nhở thanh toán và tự động thanh toán thành công!'
        : 'Payment reminder schedule and auto-pay settings saved successfully!'
    );
  }

  function handleTriggerScheduleRun() {
    const unpaidList = $bills.filter((b) => b.status !== 'Paid');
    let reminderCount = 0;
    let autoPaidCount = 0;

    unpaidList.forEach((b) => {
      if (reminderEnabled) {
        remindedBillIds[b.id] = true;
        reminderCount++;
      }
      if (autoPayEnabled && b.dueAmount > 0) {
        const payAmt = autoPayAllowPartial ? Number((b.dueAmount * 0.5).toFixed(2)) : b.dueAmount;
        recordPayment(
          b.invoiceNumber,
          payAmt,
          'Bank Transfer/NEFT',
          `AUTOPAY-${Date.now().toString().slice(-6)}`,
          'Auto-Pay Engine'
        );
        autoPaidCount++;
      }
    });

    toast.success(
      $language === 'vi'
        ? `Hệ thống tự động đã quét xong: Đã gửi nhắc nhở cho ${reminderCount} khách hàng và tự động thanh toán ${autoPaidCount} hóa đơn!`
        : `Automated scan completed: Sent reminders to ${reminderCount} customers and processed auto-pay for ${autoPaidCount} bills!`
    );
    showScheduleConfigModal = false;
  }

  // Reactively respond to tab overrides from router / notifications
  $effect(() => {
    const override = $activeTabOverride;
    const validTabs: AccountsTab[] = ['connections', 'bill-generation', 'subscriber-tracking', 'charge-settings', 'settings', 'profile'];
    if (override && override.path === '/accounts') {
      if (validTabs.includes(override.tab as AccountsTab)) {
        activeTab = override.tab as AccountsTab;
      }
    } else {
      const qTab = queryParam('tab');
      if (qTab && validTabs.includes(qTab as AccountsTab)) {
        activeTab = qTab as AccountsTab;
      }
    }
  });

  // STATE: BILL GENERATION FORM
  let billAccountId = $state('T064-000000000001');
  let billingMonth = $state('September 2026');
  let customSecurityDeposit = $state(250);
  let customMonthlyRental = $state(125);
  let customHourlyCharges = $state(0.0);
  let discountPercent = $state(25); // bulk / corporate scheme discount
  let discountTouched = $state(false);

  // Selected Connection matching the Account ID
  const matchedConnection = $derived(
    $connections.find(
      (c) => c.accountId.replace(/-/g, '') === billAccountId.trim().replace(/-/g, '')
    )
  );

  // The order behind this connection carries the bulk scheme discount.
  const matchedOrder = $derived(
    matchedConnection
      ? $orders.find((o) => o.id === matchedConnection.orderId) ??
          $orders.find((o) => o.assignedAccountId === matchedConnection.accountId)
      : undefined
  );

  // Keep the discount in sync with the order's scheme unless the accountant overrode it.
  $effect(() => {
    if (!discountTouched) discountPercent = matchedOrder?.bulkDiscountPercent ?? 0;
  });

  // Newly provisioned connections that have not received an initial bill yet
  const unbilledConnections = $derived(
    $connections.filter(
      (c) => !$bills.some((b) => b.accountId.replace(/-/g, '') === c.accountId.replace(/-/g, ''))
    )
  );

  // =============================================================
  // RECENT ACCOUNTS (Lưu tối đa 3 tài khoản gần đây nhất)
  // =============================================================
  const STORAGE_KEY_RECENT_ACCOUNTS = 'nexus_recent_billing_accounts';

  let recentAccountIds = $state<string[]>([]);

  $effect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY_RECENT_ACCOUNTS);
      if (stored) {
        const parsed = JSON.parse(stored);
        if (Array.isArray(parsed) && parsed.length > 0) {
          recentAccountIds = parsed.slice(0, 3);
        }
      }
    } catch {}
  });

  function addRecentAccountId(rawId: string) {
    if (!rawId || !rawId.trim()) return;
    const clean = rawId.trim();
    const cleanKey = clean.replace(/-/g, '').toUpperCase();
    const filtered = recentAccountIds.filter((id) => id.replace(/-/g, '').toUpperCase() !== cleanKey);
    const updated = [clean, ...filtered].slice(0, 3);
    recentAccountIds = updated;
    try {
      if (typeof window !== 'undefined') {
        localStorage.setItem(STORAGE_KEY_RECENT_ACCOUNTS, JSON.stringify(updated));
      }
    } catch {}
  }

  // Hiển thị tối đa 3 tài khoản gần đây (nếu chưa có thì lấy 3 tài khoản đầu từ danh sách kết nối)
  const displayRecentAccounts = $derived.by(() => {
    let ids = recentAccountIds.filter(Boolean);
    if (ids.length === 0 && $connections.length > 0) {
      ids = $connections.slice(0, 3).map((c) => c.accountId);
    }
    ids = ids.slice(0, 3);
    return ids.map((id) => {
      const cleanKey = id.replace(/-/g, '').toUpperCase();
      const conn = $connections.find((c) => c.accountId.replace(/-/g, '').toUpperCase() === cleanKey);
      return {
        accountId: id,
        customerName: conn?.customerName || 'Nexus Subscriber',
        planName: conn?.planName || '',
        connectionType: conn?.connectionType || 'Broadband',
        status: conn?.status || 'Active',
      };
    });
  });

  // When connection changes, auto-load its plan rates
  const handleSelectConnectionForBilling = (accountId: string) => {
    billAccountId = accountId;
    discountTouched = false;
    addRecentAccountId(accountId);
    const conn = $connections.find(
      (c) => c.accountId.replace(/-/g, '') === accountId.trim().replace(/-/g, '')
    );
    if (conn) {
      customSecurityDeposit = conn.securityDeposit || 250;
      customMonthlyRental = conn.monthlyRental || 100;
      customHourlyCharges = 0.0;
      toast.info($language === 'vi' ? `Đã tải thông số thuê bao cho ${conn.customerName}` : `Loaded subscriber parameters for ${conn.customerName}`);
    }
  };

  // =============================================================
  // CONNECTIONS LISTING & BILLING PREPARATION STATE (ACCOUNTS)
  // =============================================================
  type ConnectionSortOption =
    | 'newest'
    | 'oldest'
    | 'name-asc'
    | 'name-desc'
    | 'rental-high'
    | 'rental-low'
    | 'unbilled-first'
    | 'active-first';

  let connectionSearch = $state('');
  let connectionSort = $state<ConnectionSortOption>('newest');
  let connectionTypeFilter = $state<string>('all');
  let connectionStatusFilter = $state<string>('all');
  let connectionBillingFilter = $state<'all' | 'unbilled' | 'billed'>('all');
  let detailModalConnection = $state<Connection | null>(null);

  const getBillsForConnection = (accountId: string) => {
    const cleanId = accountId.replace(/-/g, '');
    return $bills.filter((b) => b.accountId.replace(/-/g, '') === cleanId);
  };

  const detailModalBills = $derived(
    detailModalConnection ? getBillsForConnection(detailModalConnection.accountId) : []
  );

  const totalMonthlyRentalRevenue = $derived(
    Number($connections.reduce((sum, c) => sum + (c.monthlyRental || 0), 0).toFixed(2))
  );

  const filteredAndSortedConnections = $derived(
    $connections
      .filter((c) => {
        // Search query matching
        if (connectionSearch.trim()) {
          const q = connectionSearch.toLowerCase().trim();
          const matches =
            c.accountId.toLowerCase().includes(q) ||
            (c.orderId && c.orderId.toLowerCase().includes(q)) ||
            c.customerName.toLowerCase().includes(q) ||
            (c.customerPhone && c.customerPhone.toLowerCase().includes(q)) ||
            (c.customerEmail && c.customerEmail.toLowerCase().includes(q)) ||
            (c.installationAddress && c.installationAddress.toLowerCase().includes(q)) ||
            (c.planName && c.planName.toLowerCase().includes(q)) ||
            (c.connectionType && c.connectionType.toLowerCase().includes(q)) ||
            (c.ipAddress && c.ipAddress.toLowerCase().includes(q)) ||
            (c.assignedDeviceSerial && c.assignedDeviceSerial.toLowerCase().includes(q));
          if (!matches) return false;
        }

        // Connection Type filter
        if (connectionTypeFilter !== 'all' && c.connectionType !== connectionTypeFilter) {
          return false;
        }

        // Line Status filter
        if (connectionStatusFilter !== 'all' && c.status !== connectionStatusFilter) {
          return false;
        }

        // Billing status filter
        if (connectionBillingFilter !== 'all') {
          const hasBill = $bills.some(
            (b) => b.accountId.replace(/-/g, '') === c.accountId.replace(/-/g, '')
          );
          if (connectionBillingFilter === 'unbilled' && hasBill) return false;
          if (connectionBillingFilter === 'billed' && !hasBill) return false;
        }

        return true;
      })
      .sort((a, b) => {
        if (connectionSort === 'newest') {
          return (b.installedDate || '').localeCompare(a.installedDate || '');
        }
        if (connectionSort === 'oldest') {
          return (a.installedDate || '').localeCompare(b.installedDate || '');
        }
        if (connectionSort === 'name-asc') {
          return a.customerName.localeCompare(b.customerName, 'vi');
        }
        if (connectionSort === 'name-desc') {
          return b.customerName.localeCompare(a.customerName, 'vi');
        }
        if (connectionSort === 'rental-high') {
          return (b.monthlyRental || 0) - (a.monthlyRental || 0);
        }
        if (connectionSort === 'rental-low') {
          return (a.monthlyRental || 0) - (b.monthlyRental || 0);
        }
        if (connectionSort === 'unbilled-first') {
          const aHasBill = $bills.some((x) => x.accountId.replace(/-/g, '') === a.accountId.replace(/-/g, ''));
          const bHasBill = $bills.some((x) => x.accountId.replace(/-/g, '') === b.accountId.replace(/-/g, ''));
          if (!aHasBill && bHasBill) return -1;
          if (aHasBill && !bHasBill) return 1;
          return (b.installedDate || '').localeCompare(a.installedDate || '');
        }
        if (connectionSort === 'active-first') {
          if (a.status === 'Active' && b.status !== 'Active') return -1;
          if (a.status !== 'Active' && b.status === 'Active') return 1;
          return (b.installedDate || '').localeCompare(a.installedDate || '');
        }
        return 0;
      })
  );

  const handleGoToBilling = (conn: Connection) => {
    handleSelectConnectionForBilling(conn.accountId);
    activeTab = 'bill-generation';
    detailModalConnection = null;
    toast.success(
      $language === 'vi'
        ? `Đã nạp thông số kết nối ${conn.accountId} (${conn.customerName}) sang trang Tạo hóa đơn cước!`
        : `Preloaded connection ${conn.accountId} (${conn.customerName}) into Bill Generation!`
    );
  };

  // FINANCIAL CALCULATION ENGINE:
  // Discount   = (Security Deposit + Monthly Rental) * discountPercent%
  // Subtotal   = Security Deposit + Monthly Rental + Hourly Charges - Discount
  // Service Tax = Subtotal * (serviceTaxRate / 100) [Default: 12.24%]
  // Grand Total = Subtotal + Service Tax
  const discountAmount = $derived(
    Number(
      (((customSecurityDeposit || 0) + (customMonthlyRental || 0)) * (discountPercent || 0) / 100).toFixed(2)
    )
  );
  const subtotal = $derived(
    Number(
      ((customSecurityDeposit || 0) + (customMonthlyRental || 0) + (customHourlyCharges || 0) - discountAmount).toFixed(2)
    )
  );
  const serviceTaxAmount = $derived(Number(((subtotal * $settings.serviceTaxRate) / 100).toFixed(2)));
  const grandTotal = $derived(Number((subtotal + serviceTaxAmount).toFixed(2)));

  // Invoice Preview Modal State
  let generatedInvoiceModal = $state<Bill | null>(null);

  // STATE: PAYMENT UPDATE FORM
  let selectedInvoiceNumber = $state($bills[0]?.invoiceNumber || '');
  let paymentAmountInput = $state(0);
  let paymentMode = $state<PaymentRecord['paymentMode']>('Credit/Debit Card');
  let paymentRefNumber = $state('');
  let cashierName = $state('Elena Rostova (Accounts)');

  // Selected Bill for Payment Updating
  const activeBillToPay = $derived(
    $bills.find((b) => b.invoiceNumber === selectedInvoiceNumber) || $bills[0] || null
  );

  // Linked subscriber connection for this bill
  const activeBillConnection = $derived(
    activeBillToPay
      ? $connections.find(
          (c) => c.accountId.replace(/-/g, '') === activeBillToPay.accountId.trim().replace(/-/g, '')
        ) || null
      : null
  );

  // Projected Due Amount after Payment Input
  const projectedDueAmount = $derived.by(() => {
    if (!activeBillToPay) return 0;
    const remaining = activeBillToPay.totalAmount - (activeBillToPay.amountPaid + (paymentAmountInput || 0));
    return Number(Math.max(0, remaining).toFixed(2));
  });

  // STATE: CHARGE SETTINGS
  let taxRateSetting = $state($settings.serviceTaxRate);
  let lateFeeSetting = $state($settings.latePaymentFeePercent);
  let broadbandDeposit = $state($settings.defaultSecurityDeposits.Broadband);
  let dialUpDeposit = $state($settings.defaultSecurityDeposits['Dial-Up']);
  let landlineDeposit = $state($settings.defaultSecurityDeposits.Landline);

  // FORM SUBMISSION LOGIC: GENERATE BILL
  const handleGenerateBillSubmit = (e: SubmitEvent) => {
    e.preventDefault();

    if (!billAccountId.trim()) {
      toast.error($language === 'vi' ? 'Vui lòng nhập mã tài khoản 16 ký tự hợp lệ.' : 'Please input a valid 16-character Account ID.');
      return;
    }

    addRecentAccountId(billAccountId);

    const createdBill = generateBill(
      billAccountId,
      customSecurityDeposit,
      customMonthlyRental,
      customHourlyCharges,
      billingMonth,
      discountPercent || 0
    );

    generatedInvoiceModal = createdBill;
    toast.success($language === 'vi' ? `Hóa đơn ${createdBill.invoiceNumber} đã phát hành thành công & lưu vào CSDL!` : `Invoice ${createdBill.invoiceNumber} successfully generated & saved to database!`);
  };

  // FORM SUBMISSION LOGIC: UPDATE PAYMENT STATUS
  const handlePaymentUpdateSubmit = (e: SubmitEvent) => {
    e.preventDefault();

    if (!activeBillToPay) {
      toast.error($language === 'vi' ? 'Chưa chọn hóa đơn nào.' : 'No invoice selected.');
      return;
    }

    if (paymentAmountInput <= 0) {
      toast.error($language === 'vi' ? 'Số tiền thanh toán phải lớn hơn 0.' : 'Payment amount must be greater than zero.');
      return;
    }

    const ref = paymentRefNumber.trim() || `TXN-REC-${Date.now().toString().slice(-6)}`;
    const wasSuspended = activeBillConnection?.status === 'Temporarily Inactive';

    const updated = recordPayment(
      activeBillToPay.invoiceNumber,
      paymentAmountInput,
      paymentMode,
      ref,
      cashierName
    );

    if (updated) {
      const isReactivated = wasSuspended && updated.status === 'Paid';
      toast.success(
        $language === 'vi'
          ? `Đã ghi nhận thanh toán $${paymentAmountInput.toFixed(2)} cho ${updated.invoiceNumber}. Công nợ còn lại: $${updated.dueAmount.toFixed(2)}${isReactivated ? ' (⚡ Đường truyền đã được tự động kích hoạt lại!)' : ''}`
          : `Payment of $${paymentAmountInput.toFixed(2)} recorded for ${updated.invoiceNumber}. Remaining Due: $${updated.dueAmount.toFixed(2)}${isReactivated ? ' (⚡ Connection line automatically reactivated!)' : ''}`
      );
      paymentAmountInput = 0;
      paymentRefNumber = '';
    }
  };

  // SETTINGS FORM SUBMISSION HANDLER
  const handleSaveSettings = (e: SubmitEvent) => {
    e.preventDefault();
    updateSettings({
      serviceTaxRate: taxRateSetting,
      latePaymentFeePercent: lateFeeSetting,
      defaultSecurityDeposits: {
        Broadband: broadbandDeposit,
        'Dial-Up': dialUpDeposit,
        Landline: landlineDeposit,
      },
    });
    toast.success($language === 'vi' ? 'Đã cập nhật cấu hình biểu cước & thuế suất.' : 'Financial charge settings & tax rates updated successfully.');
  };

  const accountsNavItems: NavItem[] = $derived([
    {
      id: 'connections',
      label: $t.accountsNav.connections || ($language === 'vi' ? 'Danh sách kết nối' : 'Customer Connections'),
      icon: Network,
      badge: $connections.length,
      badgeColor: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300',
    },
    {
      id: 'bill-generation',
      label: $t.accountsNav.billGeneration,
      icon: Receipt,
      badge: '12.24%',
      badgeColor: 'bg-blue-100 text-blue-800',
    },
    {
      id: 'subscriber-tracking',
      label: $language === 'vi' ? 'Theo dõi khách hàng' : 'Subscriber Tracking',
      icon: Users,
      badge: $bills.filter((b) => b.status !== 'Paid').length,
    },
    {
      id: 'charge-settings',
      label: $t.accountsNav.chargeSettings,
      icon: Settings,
    },
    { id: 'settings', label: $t.accountsNav.settings, icon: Settings },
  ]);
</script>

<DashboardLayout
  activeTab={activeTab}
  onTabChange={(tab) => {
    activeTab = tab as AccountsTab;
    detailModalConnection = null;
    historyModalBill = null;
    receiptModalPayment = null;
    generatedInvoiceModal = null;
    showScheduleConfigModal = false;
  }}
  navItems={accountsNavItems}
  roleBadgeTitle={$t.roles.accounts}
  pageTitle={accountsNavItems.find((n) => n.id === activeTab)?.label}
  primaryAction={{
    label: $t.actions.newInvoice,
    onClick: () => (activeTab = 'bill-generation'),
    icon: Plus,
  }}
>
  <div class="tab-content-animate space-y-6">
    <!-- TAB: DANH SÁCH KẾT NỐI (CONNECTIONS LIST FOR ACCOUNTANT) -->
    {#if activeTab === 'connections'}
      <div class="space-y-6">
        <!-- Section 1: KPI Stats Summary -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <!-- Total Connections -->
          <div class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xs flex items-center space-x-4">
            <div class="p-3 rounded-xl bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400">
              <Network class="h-6 w-6" />
            </div>
            <div>
              <div class="text-xs font-semibold text-slate-500">
                {$language === 'vi' ? 'Tổng số kết nối' : 'Total Connections'}
              </div>
              <div class="text-2xl font-black font-mono text-slate-900 dark:text-white mt-0.5">
                {$connections.length}
              </div>
              <div class="text-[11px] text-slate-400">
                {$language === 'vi' ? 'Điểm lắp đặt thuê bao' : 'Provisioned subscriber links'}
              </div>
            </div>
          </div>

          <!-- Active Lines -->
          <div class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xs flex items-center space-x-4">
            <div class="p-3 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400">
              <CheckCircle2 class="h-6 w-6" />
            </div>
            <div>
              <div class="text-xs font-semibold text-slate-500">
                {$language === 'vi' ? 'Đang hoạt động' : 'Active Lines'}
              </div>
              <div class="text-2xl font-black font-mono text-emerald-600 dark:text-emerald-400 mt-0.5">
                {$connections.filter((c) => c.status === 'Active').length}
              </div>
              <div class="text-[11px] text-slate-400">
                {$language === 'vi' ? 'Đang cấp tín hiệu & tính cước' : 'Live & generating billing'}
              </div>
            </div>
          </div>

          <!-- Unbilled New Connections (Awaiting Initial Invoice) -->
          <div class="p-4 rounded-xl bg-white dark:bg-slate-900 border {unbilledConnections.length > 0 ? 'border-amber-400/80 dark:border-amber-500/50 bg-amber-50/20' : 'border-slate-200 dark:border-slate-800'} shadow-xs flex items-center space-x-4">
            <div class="p-3 rounded-xl bg-amber-50 dark:bg-amber-950/40 text-amber-600 dark:text-amber-400">
              <AlertCircle class="h-6 w-6" />
            </div>
            <div>
              <div class="text-xs font-semibold text-slate-500">
                {$language === 'vi' ? 'Chờ lập HĐ đầu kỳ' : 'Awaiting Initial Bill'}
              </div>
              <div class="text-2xl font-black font-mono text-amber-600 dark:text-amber-400 mt-0.5 flex items-center gap-1.5">
                <span>{unbilledConnections.length}</span>
                {#if unbilledConnections.length > 0}
                  <span class="inline-flex h-2 w-2 rounded-full bg-amber-500 animate-ping"></span>
                {/if}
              </div>
              <div class="text-[11px] text-slate-400">
                {$language === 'vi' ? 'Cần phát hành hóa đơn mới' : 'Newly provisioned lines'}
              </div>
            </div>
          </div>

          <!-- Recurring Monthly Rental -->
          <div class="p-4 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xs flex items-center space-x-4">
            <div class="p-3 rounded-xl bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400">
              <Wallet class="h-6 w-6" />
            </div>
            <div>
              <div class="text-xs font-semibold text-slate-500">
                {$language === 'vi' ? 'Cước định kỳ/tháng' : 'Monthly Recurring Base'}
              </div>
              <div class="text-2xl font-black font-mono text-indigo-600 dark:text-indigo-400 mt-0.5">
                ${totalMonthlyRentalRevenue.toFixed(2)}
              </div>
              <div class="text-[11px] text-slate-400">
                {$language === 'vi' ? 'Tổng cước gói của thuê bao' : 'Sum of all monthly rentals'}
              </div>
            </div>
          </div>
        </div>

        <!-- Section 2: Main Filter, Search & Sort Control Panel -->
        <div class="bg-white dark:bg-slate-900 p-5 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs space-y-4">
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <h3 class="text-base font-bold text-slate-900 dark:text-white flex items-center space-x-2">
                <Network class="h-5 w-5 text-blue-600 dark:text-blue-400" />
                <span>{$language === 'vi' ? 'Danh sách kết nối & Quản lý cước thuê bao' : 'Customer Connections & Billing Directory'}</span>
              </h3>
              <p class="text-xs text-slate-500 mt-0.5">
                {$language === 'vi'
                  ? 'Tra cứu kết nối đường truyền, kiểm tra thông số kỹ thuật và bấm "Chi tiết & Lập HĐ" để nạp ngay sang giao diện phát hành hóa đơn cước.'
                  : 'Look up customer links, inspect tariff rates, and click "Details & Bill" to jump straight into bill generation.'}
              </p>
            </div>

            <!-- Quick Counter Badge -->
            <div class="px-3 py-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-xs shrink-0">
              <span class="text-slate-500">{$language === 'vi' ? 'Hiển thị:' : 'Showing:'}</span>
              <strong class="font-mono text-blue-600 dark:text-blue-400 ml-1.5">{filteredAndSortedConnections.length}</strong>
              <span class="text-slate-400"> / {$connections.length}</span>
            </div>
          </div>

          <!-- Search Bar & Sort Dropdown -->
          <div class="grid grid-cols-1 md:grid-cols-12 gap-3">
            <!-- Search Bar (md:col-span-8) -->
            <div class="md:col-span-8 relative">
              <Search class="absolute left-3.5 top-2.5 h-4 w-4 text-slate-400" />
              <input
                type="text"
                bind:value={connectionSearch}
                placeholder={$language === 'vi'
                  ? 'Tìm kiếm theo mã tài khoản, tên khách hàng, số ĐT, địa chỉ, gói cước, IP...'
                  : 'Search by Account ID, subscriber name, phone, address, plan, IP...'}
                class="w-full pl-10 pr-10 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {#if connectionSearch}
                <button
                  type="button"
                  onclick={() => (connectionSearch = '')}
                  class="absolute right-3 top-2.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
                  title={$language === 'vi' ? 'Xóa tìm kiếm' : 'Clear search'}
                >
                  <X class="h-4 w-4" />
                </button>
              {/if}
            </div>

            <!-- Sort Select Dropdown (md:col-span-4) -->
            <div class="md:col-span-4 relative flex items-center">
              <div class="absolute left-3 pointer-events-none text-slate-400">
                <ArrowDownUp class="h-4 w-4" />
              </div>
              <select
                bind:value={connectionSort}
                class="w-full pl-9 pr-8 py-2 text-xs font-semibold bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 cursor-pointer"
              >
                <option value="newest">{$language === 'vi' ? '⚡ Mới lắp đặt gần nhất' : '⚡ Newest Installed'}</option>
                <option value="oldest">{$language === 'vi' ? '⏱️ Lắp đặt cũ nhất' : '⏱️ Oldest Installed'}</option>
                <option value="unbilled-first">{$language === 'vi' ? '🔔 Ưu tiên: Chưa lập HĐ đầu kỳ' : '🔔 Priority: Unbilled First'}</option>
                <option value="active-first">{$language === 'vi' ? '🟢 Ưu tiên: Đang hoạt động' : '🟢 Priority: Active Lines'}</option>
                <option value="name-asc">{$language === 'vi' ? '🔤 Tên khách hàng (A → Z)' : '🔤 Customer Name (A → Z)'}</option>
                <option value="name-desc">{$language === 'vi' ? '🔤 Tên khách hàng (Z → A)' : '🔤 Customer Name (Z → A)'}</option>
                <option value="rental-high">{$language === 'vi' ? '💰 Cước tháng: Cao → Thấp' : '💰 Rental: High to Low'}</option>
                <option value="rental-low">{$language === 'vi' ? '💵 Cước tháng: Thấp → Cao' : '💵 Rental: Low to High'}</option>
              </select>
            </div>
          </div>

          <!-- Filter Pills (Technology & Billing State) -->
          <div class="flex flex-wrap items-center justify-between gap-2 pt-1 border-t border-slate-100 dark:border-slate-800/80">
            <!-- Connection Type Pills -->
            <div class="flex flex-wrap items-center gap-1.5 text-xs">
              <span class="text-[11px] text-slate-400 mr-1 font-semibold uppercase tracking-wider">
                {$language === 'vi' ? 'Loại dịch vụ:' : 'Type:'}
              </span>
              <button
                type="button"
                onclick={() => (connectionTypeFilter = 'all')}
                class="px-2.5 py-1 rounded-md font-semibold transition cursor-pointer {connectionTypeFilter === 'all'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
              >
                {$language === 'vi' ? 'Tất cả loại' : 'All Types'}
              </button>
              <button
                type="button"
                onclick={() => (connectionTypeFilter = 'Broadband')}
                class="px-2.5 py-1 rounded-md font-semibold transition cursor-pointer flex items-center space-x-1 {connectionTypeFilter === 'Broadband'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
              >
                <Wifi class="h-3 w-3" />
                <span>Broadband</span>
              </button>
              <button
                type="button"
                onclick={() => (connectionTypeFilter = 'Dial-Up')}
                class="px-2.5 py-1 rounded-md font-semibold transition cursor-pointer flex items-center space-x-1 {connectionTypeFilter === 'Dial-Up'
                  ? 'bg-amber-600 text-white shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
              >
                <Radio class="h-3 w-3" />
                <span>Dial-Up</span>
              </button>
              <button
                type="button"
                onclick={() => (connectionTypeFilter = 'Landline')}
                class="px-2.5 py-1 rounded-md font-semibold transition cursor-pointer flex items-center space-x-1 {connectionTypeFilter === 'Landline'
                  ? 'bg-violet-600 text-white shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
              >
                <Phone class="h-3 w-3" />
                <span>Landline</span>
              </button>
            </div>

            <!-- Billing State Filter Pills -->
            <div class="flex flex-wrap items-center gap-1.5 text-xs">
              <span class="text-[11px] text-slate-400 mr-1 font-semibold uppercase tracking-wider">
                {$language === 'vi' ? 'Hóa đơn:' : 'Billing:'}
              </span>
              <button
                type="button"
                onclick={() => (connectionBillingFilter = 'all')}
                class="px-2.5 py-1 rounded-md font-semibold transition cursor-pointer {connectionBillingFilter === 'all'
                  ? 'bg-slate-900 dark:bg-white text-white dark:text-slate-900 shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
              >
                {$language === 'vi' ? 'Tất cả' : 'All'}
              </button>
              <button
                type="button"
                onclick={() => (connectionBillingFilter = 'unbilled')}
                class="px-2.5 py-1 rounded-md font-semibold transition cursor-pointer flex items-center space-x-1 {connectionBillingFilter === 'unbilled'
                  ? 'bg-amber-600 text-white shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
              >
                <span>{$language === 'vi' ? 'Chưa lập HĐ' : 'Unbilled'}</span>
                {#if unbilledConnections.length > 0}
                  <span class="px-1.5 py-0.2 rounded-full text-[10px] bg-white text-amber-700 font-black">{unbilledConnections.length}</span>
                {/if}
              </button>
              <button
                type="button"
                onclick={() => (connectionBillingFilter = 'billed')}
                class="px-2.5 py-1 rounded-md font-semibold transition cursor-pointer {connectionBillingFilter === 'billed'
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700'}"
              >
                {$language === 'vi' ? 'Đã có HĐ' : 'Billed'}
              </button>
            </div>
          </div>
        </div>

        <!-- Section 3: Connections Master Data Table -->
        <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 dark:bg-slate-800/80 uppercase font-semibold text-slate-500 border-b border-slate-200 dark:border-slate-700">
                <tr>
                  <th class="px-4 py-3">{$language === 'vi' ? 'Mã tài khoản / Đơn hàng' : 'Account & Order ID'}</th>
                  <th class="px-4 py-3">{$language === 'vi' ? 'Khách hàng & Địa chỉ' : 'Subscriber & Address'}</th>
                  <th class="px-4 py-3">{$language === 'vi' ? 'Gói dịch vụ' : 'Service Plan'}</th>
                  <th class="px-4 py-3">{$language === 'vi' ? 'Biểu cước / Cọc' : 'Monthly / Deposit'}</th>
                  <th class="px-4 py-3 text-center">{$language === 'vi' ? 'Trạng thái đường truyền' : 'Line Status'}</th>
                  <th class="px-4 py-3 text-center">{$language === 'vi' ? 'Tình trạng cước' : 'Billing Status'}</th>
                  <th class="px-4 py-3 text-right">{$language === 'vi' ? 'Thao tác kế toán' : 'Actions'}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                {#if filteredAndSortedConnections.length > 0}
                  {#each filteredAndSortedConnections as conn (conn.accountId)}
                    {@const connBills = getBillsForConnection(conn.accountId)}
                    <tr class="hover:bg-slate-50/80 dark:hover:bg-slate-800/50 transition">
                      <!-- Column 1: Account ID & Order ID -->
                      <td class="px-4 py-3.5 align-top">
                        <button
                          type="button"
                          onclick={() => (detailModalConnection = conn)}
                          class="font-mono font-bold text-blue-600 dark:text-blue-400 hover:underline tracking-wider text-left block cursor-pointer"
                          title={$language === 'vi' ? 'Xem chi tiết kết nối' : 'View connection details'}
                        >
                          {conn.accountId}
                        </button>
                        <div class="text-[11px] font-mono text-slate-500 mt-0.5">
                          {$language === 'vi' ? 'Đơn:' : 'Order:'} {conn.orderId}
                        </div>
                        <div class="text-[10px] text-slate-400 mt-0.5">
                          {$language === 'vi' ? 'Lắp ngày:' : 'Installed:'} {conn.installedDate || 'N/A'}
                        </div>
                      </td>

                      <!-- Column 2: Customer & Address -->
                      <td class="px-4 py-3.5 align-top">
                        <div class="font-bold text-slate-900 dark:text-white">
                          {conn.customerName}
                        </div>
                        <div class="text-[11px] text-slate-500 flex items-center space-x-1 mt-0.5">
                          <Phone class="h-3 w-3 text-slate-400 shrink-0" />
                          <span>{conn.customerPhone}</span>
                        </div>
                        <div class="text-[11px] text-slate-400 max-w-xs truncate mt-0.5" title={conn.installationAddress}>
                          {conn.installationAddress}
                        </div>
                      </td>

                      <!-- Column 3: Service Plan -->
                      <td class="px-4 py-3.5 align-top">
                        <div class="flex items-center space-x-1.5 mb-1">
                          <span class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[10px] font-bold {conn.connectionType === 'Broadband'
                            ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                            : conn.connectionType === 'Dial-Up'
                              ? 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300'
                              : 'bg-violet-100 text-violet-800 dark:bg-violet-950 dark:text-violet-300'}">
                            {#if conn.connectionType === 'Broadband'}
                              <Wifi class="h-2.5 w-2.5" />
                            {:else if conn.connectionType === 'Dial-Up'}
                              <Radio class="h-2.5 w-2.5" />
                            {:else}
                              <Phone class="h-2.5 w-2.5" />
                            {/if}
                            <span>{conn.connectionType}</span>
                          </span>
                        </div>
                        <div class="font-semibold text-slate-800 dark:text-slate-200">
                          {cleanPlanName(conn.planName)}
                        </div>
                        {#if conn.ipAddress}
                          <div class="text-[10px] font-mono text-slate-400 mt-0.5">
                            IP: {conn.ipAddress}
                          </div>
                        {/if}
                      </td>

                      <!-- Column 4: Monthly Rental & Deposit -->
                      <td class="px-4 py-3.5 align-top">
                        <div class="font-mono font-bold text-slate-900 dark:text-white">
                          ${(conn.monthlyRental || 0).toFixed(2)}<span class="text-[11px] font-normal text-slate-400">/th</span>
                        </div>
                        <div class="text-[11px] text-slate-500 mt-0.5">
                          {$language === 'vi' ? 'Cọc:' : 'Dep:'} ${(conn.securityDeposit || 0).toFixed(2)}
                        </div>
                      </td>

                      <!-- Column 5: Line Status -->
                      <td class="px-4 py-3.5 align-top text-center">
                        {#if conn.status === 'Active'}
                          <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800">
                            <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 mr-1.5"></span>
                            {$language === 'vi' ? 'Đang hoạt động' : 'Active'}
                          </span>
                        {:else if conn.status === 'Temporarily Inactive'}
                          <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 border border-amber-300 dark:border-amber-800">
                            <span class="w-1.5 h-1.5 rounded-full bg-amber-500 mr-1.5"></span>
                            {$language === 'vi' ? 'Tạm ngưng' : 'Suspended'}
                          </span>
                        {:else}
                          <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300 border border-rose-300 dark:border-rose-800">
                            <span class="w-1.5 h-1.5 rounded-full bg-rose-500 mr-1.5"></span>
                            {$language === 'vi' ? 'Đã hủy' : 'Terminated'}
                          </span>
                        {/if}
                      </td>

                      <!-- Column 6: Billing Status -->
                      <td class="px-4 py-3.5 align-top text-center">
                        {#if connBills.length > 0}
                          <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-300 border border-sky-300 dark:border-sky-800">
                            <CheckCircle2 class="h-3 w-3 mr-1 text-sky-600 dark:text-sky-400" />
                            <span>{$language === 'vi' ? `Đã có ${connBills.length} HĐ` : `Billed (${connBills.length})`}</span>
                          </span>
                        {:else}
                          <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-800 dark:bg-amber-950/80 dark:text-amber-300 border border-amber-300 dark:border-amber-700">
                            <span class="w-1.5 h-1.5 rounded-full bg-amber-500 mr-1.5 animate-ping"></span>
                            <span>{$language === 'vi' ? 'Chưa lập HĐ' : 'Unbilled'}</span>
                          </span>
                        {/if}
                      </td>

                      <!-- Column 7: Actions -->
                      <td class="px-4 py-3.5 align-top text-right">
                        <div class="flex items-center justify-end space-x-1.5">
                          <!-- Chi tiết & Lập HĐ (Navigates to bill-generation with prefilled params) -->
                          <button
                            type="button"
                            onclick={() => handleGoToBilling(conn)}
                            class="px-2.5 py-1.5 rounded-lg text-xs font-bold bg-blue-600 hover:bg-blue-700 text-white shadow-xs transition flex items-center space-x-1 cursor-pointer"
                            title={$language === 'vi' ? 'Bấm để nạp thông số và sang trang Tạo hóa đơn cước' : 'Click to prefill and open Bill Generation'}
                          >
                            <FileText class="h-3.5 w-3.5" />
                            <span>{$language === 'vi' ? 'Chi tiết & Lập HĐ' : 'Details & Bill'}</span>
                          </button>

                          <!-- Xem thông số modal -->
                          <button
                            type="button"
                            onclick={() => (detailModalConnection = conn)}
                            class="p-1.5 rounded-lg text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 transition cursor-pointer"
                            title={$language === 'vi' ? 'Xem thông số kỹ thuật & Lịch sử cước' : 'View full connection specifications'}
                          >
                            <Eye class="h-3.5 w-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  {/each}
                {:else}
                  <tr>
                    <td colspan="7" class="px-4 py-12 text-center text-slate-500">
                      <Network class="h-10 w-10 mx-auto text-slate-300 dark:text-slate-600 mb-2" />
                      <p class="font-semibold text-sm text-slate-700 dark:text-slate-300">
                        {$language === 'vi' ? 'Không tìm thấy kết nối nào phù hợp.' : 'No connections matched your criteria.'}
                      </p>
                      <p class="text-xs text-slate-400 mt-1">
                        {$language === 'vi'
                          ? 'Vui lòng kiểm tra lại từ khóa tìm kiếm hoặc điều chỉnh các bộ lọc dịch vụ và trạng thái.'
                          : 'Try changing your search terms or relaxing service type/billing status filters.'}
                      </p>
                      {#if connectionSearch || connectionTypeFilter !== 'all' || connectionBillingFilter !== 'all' || connectionStatusFilter !== 'all'}
                        <button
                          type="button"
                          onclick={() => {
                            connectionSearch = '';
                            connectionTypeFilter = 'all';
                            connectionBillingFilter = 'all';
                            connectionStatusFilter = 'all';
                          }}
                          class="mt-3 px-3 py-1.5 text-xs font-semibold text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-blue-900 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-950/40 transition cursor-pointer"
                        >
                          {$language === 'vi' ? 'Khôi phục tất cả bộ lọc' : 'Reset all filters'}
                        </button>
                      {/if}
                    </td>
                  </tr>
                {/if}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    {/if}

    <!-- TAB 1: BILL GENERATION -->
    {#if activeTab === 'bill-generation'}
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Bill Generator Form -->
        <form
          onsubmit={handleGenerateBillSubmit}
          class="lg:col-span-2 space-y-6 bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm"
        >
          <!-- Account ID Input Section -->
          <div>
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-700 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Mã tài khoản thuê bao 16 ký tự *' : '16-character Subscriber Account ID *'}
            </label>
            <div class="flex gap-2">
              <div class="relative flex-1">
                <Search class="absolute left-3 top-3 h-4 w-4 text-slate-400" />
                <input
                  type="text"
                  required
                  placeholder="T064-000000000001"
                  bind:value={billAccountId}
                  class="w-full pl-9 pr-4 py-2.5 text-base font-mono bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 tracking-widest text-slate-900 dark:text-white"
                />
              </div>
              <button
                type="button"
                onclick={() => handleSelectConnectionForBilling(billAccountId)}
                class="px-4 py-2.5 rounded-lg text-xs font-semibold bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition"
              >
                {$language === 'vi' ? 'Tải thông số' : 'Fetch Parameters'}
              </button>
            </div>

            <!-- Unbilled New Connections Alert -->
            {#if unbilledConnections.length > 0}
              <div class="p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/30 flex flex-col sm:flex-row sm:items-center justify-between gap-2.5">
                <div class="flex items-center space-x-2 text-xs text-amber-800 dark:text-amber-300">
                  <span class="inline-flex h-2 w-2 rounded-full bg-amber-500 animate-ping"></span>
                  <span class="font-bold">
                    {$language === 'vi'
                      ? `Có ${unbilledConnections.length} kết nối mới cấp chưa xuất hóa đơn đầu kỳ:`
                      : `${unbilledConnections.length} newly provisioned connection(s) awaiting initial bill:`}
                  </span>
                </div>
                <div class="flex flex-wrap gap-1.5">
                  {#each unbilledConnections as uc (uc.accountId)}
                    <button
                      type="button"
                      onclick={() => handleSelectConnectionForBilling(uc.accountId)}
                      class="px-2.5 py-1 text-xs font-mono font-bold rounded-lg bg-amber-600 hover:bg-amber-700 text-white transition shadow-xs flex items-center gap-1.5 cursor-pointer"
                    >
                      <span>{uc.accountId}</span>
                      <span class="text-[10px] font-normal opacity-90">({uc.customerName})</span>
                    </button>
                  {/each}
                </div>
              </div>
            {/if}

            <!-- 3 Recent Accounts Selection (Chỉ lưu lại 3 tài khoản gần đây nhất) -->
            <div class="flex flex-wrap items-center gap-2 pt-2 text-xs">
              <span class="font-semibold text-slate-600 dark:text-slate-400 flex items-center gap-1.5">
                <Clock class="h-3.5 w-3.5 text-blue-500" />
                <span>{$language === 'vi' ? '3 tài khoản gần đây:' : '3 Recent Accounts:'}</span>
              </span>
              {#each displayRecentAccounts as rec (rec.accountId)}
                <button
                  type="button"
                  onclick={() => handleSelectConnectionForBilling(rec.accountId)}
                  class="inline-flex items-center gap-1.5 font-mono text-xs px-2.5 py-1 rounded-lg border transition shadow-2xs cursor-pointer {billAccountId.replace(/-/g, '').toUpperCase() === rec.accountId.replace(/-/g, '').toUpperCase() ? 'bg-blue-600 text-white border-blue-600 font-bold shadow-xs' : 'bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-800 dark:text-slate-200 border-slate-200 dark:border-slate-700'}"
                  title={rec.customerName}
                >
                  <span class="font-bold">{rec.accountId}</span>
                  <span class="text-[11px] opacity-80 font-sans truncate max-w-[130px]">({rec.customerName})</span>
                </button>
              {/each}
            </div>
          </div>

          <!-- Matched Subscriber Comprehensive Details Card (Hiển thị chi tiết hơn thuê bao) -->
          {#if matchedConnection}
            <div class="rounded-xl border border-blue-200 dark:border-blue-900/60 bg-linear-to-br from-blue-50/60 via-white to-slate-50/60 dark:from-slate-900 dark:via-blue-950/20 dark:to-slate-900 p-5 shadow-xs space-y-4">
              <!-- Top Row: Subscriber Title & Badges -->
              <div class="flex flex-col sm:flex-row sm:items-center justify-between pb-3 border-b border-blue-100 dark:border-blue-900/40 gap-3">
                <div class="flex items-center space-x-3">
                  <div class="w-10 h-10 rounded-xl bg-blue-600 text-white flex items-center justify-center font-bold text-base shadow-xs shrink-0">
                    {matchedConnection.customerName.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <div class="flex items-center space-x-2">
                      <h4 class="font-bold text-slate-900 dark:text-white text-base leading-snug">
                        {matchedConnection.customerName}
                      </h4>
                      <span class="px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300 border border-blue-200 dark:border-blue-800">
                        {matchedConnection.connectionType}
                      </span>
                    </div>
                    <div class="flex items-center space-x-3 text-xs text-slate-500 font-mono mt-0.5">
                      <span class="font-bold text-blue-600 dark:text-blue-400">{matchedConnection.accountId}</span>
                      {#if matchedConnection.orderId}
                        <span class="text-slate-400">· Đơn hàng: {matchedConnection.orderId}</span>
                      {/if}
                    </div>
                  </div>
                </div>

                <!-- Status Badge & View Specs Action -->
                <div class="flex items-center space-x-2 shrink-0">
                  {#if matchedConnection.status === 'Active'}
                    <span class="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 dark:bg-emerald-950/80 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800">
                      <span class="w-2 h-2 rounded-full bg-emerald-500 mr-1.5 animate-pulse"></span>
                      {$language === 'vi' ? 'Đang hoạt động' : 'Active Connection'}
                    </span>
                  {:else if matchedConnection.status === 'Temporarily Inactive'}
                    <span class="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 dark:bg-amber-950/80 dark:text-amber-300 border border-amber-300 dark:border-amber-800">
                      <span class="w-2 h-2 rounded-full bg-amber-500 mr-1.5"></span>
                      {$language === 'vi' ? 'Tạm ngưng do nợ cước' : 'Suspended for Arrears'}
                    </span>
                  {:else}
                    <span class="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-rose-100 text-rose-800 dark:bg-rose-950/80 dark:text-rose-300 border border-rose-300 dark:border-rose-800">
                      <span class="w-2 h-2 rounded-full bg-rose-500 mr-1.5"></span>
                      {$language === 'vi' ? 'Đã hủy dịch vụ' : 'Terminated'}
                    </span>
                  {/if}
                  <button
                    type="button"
                    onclick={() => (detailModalConnection = matchedConnection)}
                    class="p-1.5 rounded-lg text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 transition cursor-pointer"
                    title={$language === 'vi' ? 'Xem hồ sơ kỹ thuật chi tiết' : 'View full technical specs'}
                  >
                    <Eye class="h-4 w-4" />
                  </button>
                </div>
              </div>

              <!-- 3-Column Detailed Information Grid -->
              <div class="grid grid-cols-1 md:grid-cols-3 gap-3.5 text-xs">
                <!-- Group 1: Contact & Installation Address -->
                <div class="p-3 rounded-lg bg-white/90 dark:bg-slate-900/90 border border-slate-200/80 dark:border-slate-800 space-y-2">
                  <div class="font-bold text-slate-700 dark:text-slate-300 flex items-center space-x-1.5 pb-1 border-b border-slate-100 dark:border-slate-800 text-[11px] uppercase tracking-wider">
                    <MapPin class="h-3.5 w-3.5 text-rose-500" />
                    <span>{$language === 'vi' ? 'Liên hệ & Địa chỉ lắp đặt' : 'Contact & Installation'}</span>
                  </div>
                  <div>
                    <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Số điện thoại:' : 'Phone:'}</span>
                    <span class="font-semibold text-slate-800 dark:text-slate-200 flex items-center gap-1">
                      <Phone class="h-3 w-3 text-slate-400" />
                      {matchedConnection.customerPhone || 'N/A'}
                    </span>
                  </div>
                  <div>
                    <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Email:' : 'Email:'}</span>
                    <span class="font-semibold text-slate-800 dark:text-slate-200 truncate block">
                      {matchedConnection.customerEmail || 'N/A'}
                    </span>
                  </div>
                  <div>
                    <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Địa chỉ lắp đặt:' : 'Address:'}</span>
                    <span class="text-slate-700 dark:text-slate-300 line-clamp-2" title={matchedConnection.installationAddress}>
                      {matchedConnection.installationAddress || 'N/A'}
                    </span>
                  </div>
                </div>

                <!-- Group 2: Technical & CPE Equipment Specs -->
                <div class="p-3 rounded-lg bg-white/90 dark:bg-slate-900/90 border border-slate-200/80 dark:border-slate-800 space-y-2">
                  <div class="font-bold text-slate-700 dark:text-slate-300 flex items-center space-x-1.5 pb-1 border-b border-slate-100 dark:border-slate-800 text-[11px] uppercase tracking-wider">
                    <Wifi class="h-3.5 w-3.5 text-blue-500" />
                    <span>{$language === 'vi' ? 'Hạ tầng & Thiết bị CPE' : 'Infrastructure & Device'}</span>
                  </div>
                  <div>
                    <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Thiết bị đầu cuối (CPE):' : 'CPE Device Model:'}</span>
                    <span class="font-semibold text-slate-800 dark:text-slate-200 truncate block" title={matchedConnection.assignedDeviceModel}>
                      {matchedConnection.assignedDeviceModel || 'N/A'}
                    </span>
                    {#if matchedConnection.assignedDeviceSerial}
                      <span class="font-mono text-[10px] text-blue-600 dark:text-blue-400">S/N: {matchedConnection.assignedDeviceSerial}</span>
                    {/if}
                  </div>
                  <div class="grid grid-cols-2 gap-2">
                    <div>
                      <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Địa chỉ IP:' : 'IP Address:'}</span>
                      <span class="font-mono text-slate-700 dark:text-slate-300 truncate block">
                        {matchedConnection.ipAddress || 'DHCP Pool'}
                      </span>
                    </div>
                    <div>
                      <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Cổng port:' : 'Port:'}</span>
                      <span class="font-mono text-slate-700 dark:text-slate-300 truncate block">
                        {matchedConnection.portNumber || 'GPON-01'}
                      </span>
                    </div>
                  </div>
                  <div>
                    <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Ngày kích hoạt hòa mạng:' : 'Installed Date:'}</span>
                    <span class="text-slate-700 dark:text-slate-300">
                      {matchedConnection.installedDate || 'N/A'}
                    </span>
                  </div>
                </div>

                <!-- Group 3: Tariff, Discount & Billing History -->
                <div class="p-3 rounded-lg bg-white/90 dark:bg-slate-900/90 border border-slate-200/80 dark:border-slate-800 space-y-2">
                  <div class="font-bold text-slate-700 dark:text-slate-300 flex items-center space-x-1.5 pb-1 border-b border-slate-100 dark:border-slate-800 text-[11px] uppercase tracking-wider">
                    <Receipt class="h-3.5 w-3.5 text-emerald-500" />
                    <span>{$language === 'vi' ? 'Gói cước & Công nợ' : 'Tariff & Billing Status'}</span>
                  </div>
                  <div>
                    <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Gói cước đăng ký:' : 'Service Plan:'}</span>
                    <span class="font-bold text-blue-700 dark:text-blue-300">
                      {cleanPlanName(matchedConnection.planName)}
                    </span>
                  </div>
                  <div class="grid grid-cols-2 gap-2">
                    <div>
                      <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Cước thuê bao:' : 'Monthly Rental:'}</span>
                      <span class="font-bold font-mono text-slate-900 dark:text-white">
                        ${matchedConnection.monthlyRental.toFixed(2)}/tháng
                      </span>
                    </div>
                    <div>
                      <span class="text-slate-400 block text-[10px]">{$language === 'vi' ? 'Đặt cọc gốc:' : 'Deposit:'}</span>
                      <span class="font-bold font-mono text-slate-900 dark:text-white">
                        ${matchedConnection.securityDeposit.toFixed(2)}
                      </span>
                    </div>
                  </div>
                  <div class="pt-1 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
                    <span class="text-[11px] text-slate-500">
                      {$language === 'vi'
                        ? `Lịch sử: ${getBillsForConnection(matchedConnection.accountId).length} hóa đơn`
                        : `History: ${getBillsForConnection(matchedConnection.accountId).length} bill(s)`}
                    </span>
                    {#if matchedOrder && matchedOrder.bulkDiscountPercent > 0}
                      <span class="px-1.5 py-0.5 rounded text-[10px] font-bold bg-purple-100 text-purple-700 dark:bg-purple-950 dark:text-purple-300">
                        Ưu đãi {matchedOrder.bulkDiscountPercent}%
                      </span>
                    {/if}
                  </div>
                </div>
              </div>

              {#if matchedConnection.lastStatusReason}
                <div class="p-2.5 rounded-lg bg-amber-500/10 border border-amber-500/20 text-xs text-amber-800 dark:text-amber-300 flex items-start gap-2">
                  <AlertCircle class="h-4 w-4 text-amber-600 shrink-0 mt-0.5" />
                  <span><strong>{$language === 'vi' ? 'Ghi chú kỹ thuật/công nợ:' : 'Status Notes:'}</strong> {matchedConnection.lastStatusReason}</span>
                </div>
              {/if}
            </div>
          {:else}
            <div class="p-3.5 rounded-xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/40 text-xs text-amber-700 dark:text-amber-300 flex items-center gap-2">
              <AlertCircle class="h-4 w-4 shrink-0 text-amber-600" />
              <span>
                {$language === 'vi'
                  ? 'Mã tài khoản hiện chưa có trong bộ nhớ. Bạn vẫn có thể nhập các số liệu thanh toán thủ công bên dưới.'
                  : 'Account ID not currently bound in memory. You may still input manual billing figures below.'}
              </span>
            </div>
          {/if}

          <!-- Billing Period Selector -->
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              {$language === 'vi' ? 'Kỳ tính cước / Chu kỳ' : 'Billing Period / Cycle'}
            </label>
            <select
              bind:value={billingMonth}
              class="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg"
            >
              <option value="September 2026">
                {$language === 'vi' ? 'Tháng 9/2026 (Kỳ hiện tại)' : 'September 2026 (Current Cycle)'}
              </option>
              <option value="August 2026">{$language === 'vi' ? 'Tháng 8/2026' : 'August 2026'}</option>
              <option value="July 2026">{$language === 'vi' ? 'Tháng 7/2026' : 'July 2026'}</option>
            </select>
          </div>

          <!-- ITEMIZED LINE ITEMS CALCULATION -->
          <div class="space-y-4 border-t border-slate-100 dark:border-slate-800 pt-4">
            <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500">
              {$language === 'vi' ? 'Bảng chi tiết các khoản phí & Thuế dịch vụ' : 'Itemized Charges & Tax Breakdown Table'}
            </h3>

            <div class="rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden text-sm">
              <table class="w-full text-left">
                <thead class="bg-slate-50 dark:bg-slate-800/60 text-xs font-semibold text-slate-500 uppercase border-b border-slate-200 dark:border-slate-800">
                  <tr>
                    <th class="px-4 py-2.5">{$language === 'vi' ? 'Nội dung khoản thu' : 'Line Item Description'}</th>
                    <th class="px-4 py-2.5 text-right w-44">{$language === 'vi' ? 'Số tiền ($)' : 'Charge Amount ($)'}</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-xs">
                  <!-- Line Item 1: Security Deposit -->
                  <tr>
                    <td class="px-4 py-3">
                      <div class="font-semibold text-slate-900 dark:text-white">
                        {$language === 'vi' ? 'Tiền đặt cọc thiết bị (Hoàn lại)' : 'Security Deposit (Refundable)'}
                      </div>
                      <div class="text-slate-500">
                        {$language === 'vi'
                          ? 'Tiền cọc thiết bị viễn thông và bảo đảm đường truyền (hoàn lại khi chấm dứt)'
                          : 'Required refundable deposit for telecom equipment & circuit bond'}
                      </div>
                    </td>
                    <td class="px-4 py-3 text-right">
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        bind:value={customSecurityDeposit}
                        class="w-32 px-2.5 py-1.5 text-right font-mono tabular-nums font-bold bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm text-slate-900 dark:text-white"
                      />
                    </td>
                  </tr>

                  <!-- Line Item 2: Monthly Rentals -->
                  <tr>
                    <td class="px-4 py-3">
                      <div class="font-semibold text-slate-900 dark:text-white">
                        {$language === 'vi' ? 'Cước thuê bao gói dịch vụ hàng tháng' : 'Monthly Plan Rental Charge'}
                      </div>
                      <div class="text-slate-500">
                        {$language === 'vi'
                          ? 'Phí dịch vụ định kỳ hàng tháng cho gói tốc độ/băng thông'
                          : 'Recurring monthly subscriber fee for unlimited/bandwidth tier'}
                      </div>
                    </td>
                    <td class="px-4 py-3 text-right">
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        bind:value={customMonthlyRental}
                        class="w-32 px-2.5 py-1.5 text-right font-mono tabular-nums font-bold bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm text-slate-900 dark:text-white"
                      />
                    </td>
                  </tr>

                  <!-- Line Item 3: Hourly / Metered Charges -->
                  <tr>
                    <td class="px-4 py-3">
                      <div class="font-semibold text-slate-900 dark:text-white">
                        {$language === 'vi' ? 'Cước đo theo giờ / Lưu lượng sử dụng' : 'Hourly / Usage Metered Charges'}
                      </div>
                      <div class="text-slate-500">
                        {$language === 'vi'
                          ? 'Thời gian truy cập quay số hoặc phút gọi thoại quốc tế'
                          : 'Metered dial-up access time or international voice talk minutes'}
                      </div>
                    </td>
                    <td class="px-4 py-3 text-right">
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        bind:value={customHourlyCharges}
                        class="w-32 px-2.5 py-1.5 text-right font-mono tabular-nums bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-sm text-slate-900 dark:text-white"
                      />
                    </td>
                  </tr>

                  <!-- Line Item 3b: Bulk / corporate scheme discount -->
                  <tr class="bg-emerald-50/50 dark:bg-emerald-950/30">
                    <td class="px-4 py-3">
                      <div class="font-semibold text-emerald-700 dark:text-emerald-300">
                        {$language === 'vi' ? 'Chiết khấu gói Doanh nghiệp / Số lượng lớn' : 'Bulk / Corporate Scheme Discount'}
                        {#if matchedOrder}
                          <span class="text-[10px] text-slate-500">
                            ({matchedOrder.bulkConnectionsCount} {$language === 'vi' ? 'kết nối' : 'connections'})
                          </span>
                        {/if}
                      </div>
                      <div class="text-slate-500 dark:text-slate-400 text-[11px] mt-0.5 flex items-center gap-1.5">
                        <input
                          type="number" step="1" min="0" max="100"
                          value={discountPercent}
                          oninput={(e) => { discountPercent = parseFloat((e.currentTarget as HTMLInputElement).value) || 0; discountTouched = true; }}
                          class="w-16 px-2 py-1 text-right font-mono bg-white dark:bg-slate-950 border border-slate-300 dark:border-slate-700 rounded"
                        />
                        <span>{$language === 'vi' ? '% của (tiền cọc + cước thuê)' : '% of (deposit + rental)'}</span>
                      </div>
                    </td>
                    <td class="px-4 py-3 text-right font-mono tabular-nums font-bold text-sm text-emerald-700 dark:text-emerald-400">−${discountAmount.toFixed(2)}</td>
                  </tr>

                  <!-- Line Item 4: Subtotal -->
                  <tr class="bg-slate-50/70 dark:bg-slate-950 font-semibold">
                    <td class="px-4 py-2.5 text-slate-700 dark:text-slate-300">
                      {$language === 'vi' ? 'Tổng phụ (Cơ sở tính thuế)' : 'Subtotal (Taxable Base)'}
                    </td>
                    <td class="px-4 py-2.5 text-right font-mono tabular-nums text-sm text-slate-900 dark:text-white">${subtotal.toFixed(2)}</td>
                  </tr>

                  <!-- Line Item 5: AUTOMATED ROW FOR SERVICE TAX (12.24%) -->
                  <tr class="bg-blue-50/50 dark:bg-blue-950/40">
                    <td class="px-4 py-3">
                      <div class="flex items-center space-x-2">
                        <span class="font-bold text-blue-700 dark:text-blue-300">
                          {$language === 'vi' ? `Thuế dịch vụ (${$settings.serviceTaxRate}%)` : `Service Tax (${$settings.serviceTaxRate}%)`}
                        </span>
                        <span class="text-[10px] font-mono px-1.5 py-0.5 rounded bg-blue-600 text-white font-semibold uppercase">
                          {$language === 'vi' ? 'Quy định tự động' : 'Automated Statutory'}
                        </span>
                      </div>
                      <div class="text-slate-500 dark:text-slate-400 text-[11px] mt-0.5">
                        {$language === 'vi'
                          ? `Tính tự động: $${subtotal.toFixed(2)} × ${$settings.serviceTaxRate}%`
                          : `Automated computation: $${subtotal.toFixed(2)} × ${$settings.serviceTaxRate}%`}
                      </div>
                    </td>
                    <td class="px-4 py-3 text-right font-mono tabular-nums font-bold text-sm text-blue-700 dark:text-blue-400">+${serviceTaxAmount.toFixed(2)}</td>
                  </tr>

                  <!-- Grand Total Row -->
                  <tr class="bg-slate-900 text-white font-bold text-sm">
                    <td class="px-4 py-3 uppercase tracking-wider text-xs">
                      {$language === 'vi' ? 'Tổng cộng thực thanh toán ($)' : 'Grand Total Net Payable ($)'}
                    </td>
                    <td class="px-4 py-3 text-right font-mono text-base tabular-nums text-emerald-400">${grandTotal.toFixed(2)}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Action Button -->
          <div class="flex justify-end pt-2">
            <button
              type="submit"
              class="px-6 py-2.5 rounded-lg text-sm font-bold bg-blue-600 hover:bg-blue-700 text-white transition shadow-md flex items-center space-x-2"
            >
              <Receipt class="h-4 w-4" />
              <span>{$language === 'vi' ? 'Lập hóa đơn khách hàng' : 'Generate Customer Bill'}</span>
            </button>
          </div>
        </form>

        <!-- Financial Ledger Quick View -->
        <div class="space-y-4">
          <div class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm space-y-4">
            <h3 class="font-semibold text-sm uppercase tracking-wider text-slate-900 dark:text-white flex items-center space-x-2">
              <Building class="h-4 w-4 text-blue-500" />
              <span>{$language === 'vi' ? 'Tuân thủ thuế quy định' : 'Statutory Tax Compliance'}</span>
            </h3>

            <p class="text-xs text-slate-500 leading-relaxed">
              {$language === 'vi'
                ? `Theo quy định hệ thống dịch vụ viễn thông, mọi thuê bao phát hành đều áp dụng thuế suất dịch vụ bắt buộc ${$settings.serviceTaxRate}% trên tiền đặt cọc và cước thuê thiết bị.`
                : `Under telecom marketing system regulations, all issued subscriptions apply a statutory ${$settings.serviceTaxRate}% Service Tax across equipment deposit and rental line items.`}
            </p>

            <div class="p-3 rounded-lg bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 space-y-2 text-xs font-mono">
              <div class="flex justify-between">
                <span class="text-slate-500">{$language === 'vi' ? 'Danh mục thuế:' : 'Tax Category:'}</span>
                <span class="text-slate-900 dark:text-white">{$language === 'vi' ? 'Dịch vụ viễn thông' : 'Telecommunication Services'}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">{$language === 'vi' ? 'Mã số thuế:' : 'Tax Identifier:'}</span>
                <span class="text-slate-900 dark:text-white">ST-NEX-FED-1224</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">{$language === 'vi' ? 'Thuế suất dịch vụ:' : 'Service Tax Rate:'}</span>
                <span class="text-blue-600 font-bold">{$settings.serviceTaxRate}%</span>
              </div>
            </div>
          </div>

          <!-- Recent Bills Created -->
          <div class="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-sm space-y-3">
            <div class="flex items-center justify-between">
              <h3 class="font-semibold text-xs uppercase tracking-wider text-slate-500">
                {$language === 'vi' ? 'Hóa đơn vừa phát hành' : 'Recent Generated Invoices'}
              </h3>
              <button
                onclick={() => (activeTab = 'subscriber-tracking')}
                class="text-xs text-blue-600 hover:underline"
              >
                {$language === 'vi' ? 'Theo dõi →' : 'Tracking →'}
              </button>
            </div>

            <div class="divide-y divide-slate-100 dark:divide-slate-800 text-xs">
              {#each $bills.slice(0, 3) as b (b.id)}
                <div class="py-2.5 flex items-center justify-between">
                  <div>
                    <div class="font-mono font-bold text-slate-900 dark:text-white">{b.invoiceNumber}</div>
                    <div class="text-slate-500 truncate max-w-[140px]">{b.customerName}</div>
                  </div>
                  <div class="text-right">
                    <div class="font-mono font-bold text-slate-900 dark:text-white tabular-nums">${b.totalAmount.toFixed(2)}</div>
                    <span class="text-[10px] px-1.5 py-0.5 rounded font-semibold {b.status === 'Paid'
                      ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300'
                      : 'bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-300'}">
                      {$language === 'vi' ? (b.status === 'Paid' ? 'Đã thanh toán' : 'Chờ thanh toán') : b.status}
                    </span>
                  </div>
                </div>
              {/each}
            </div>
          </div>
        </div>
      </div>
    {/if}
    <!-- TAB 2: SUBSCRIBER TRACKING & PAYMENT HISTORY -->
    {#if activeTab === 'subscriber-tracking'}
      <div class="space-y-8">
        <!-- Section 1: Subscriber Invoices & Tracking Table -->
        <div class="bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <h3 class="text-base font-bold text-slate-900 dark:text-white flex items-center space-x-2">
                <Users class="h-5 w-5 text-blue-600 dark:text-blue-400" />
                <span>{$language === 'vi' ? 'Theo dõi khách hàng & Tự động nhắc nhở thanh toán' : 'Subscriber Tracking & Automated Payment Reminders'}</span>
              </h3>
              <p class="text-xs text-slate-500 mt-0.5">
                {$language === 'vi' ? 'Theo dõi công nợ, xem lịch sử thanh toán và tự động gửi thông báo nhắc cước tới khách hàng' : 'Monitor subscriber balances, view customer payment history, and dispatch automated payment reminder notices'}
              </p>
            </div>
            
            <div class="flex items-center space-x-2">
              <button
                type="button"
                onclick={() => (showScheduleConfigModal = true)}
                class="px-3.5 py-1.5 bg-gradient-to-r from-blue-600 to-sky-600 hover:from-blue-700 hover:to-sky-700 text-white rounded-lg text-xs font-bold transition shadow-sm flex items-center space-x-1.5"
              >
                <Clock class="h-3.5 w-3.5" />
                <span>{$language === 'vi' ? 'Cài đặt thời gian nhắc nhở & Tự động thanh toán' : 'Reminder & Auto-Pay Schedule Settings'}</span>
              </button>
            </div>
          </div>

          <!-- Status Filter Tabs: strictly on a single horizontal row -->
          <div class="flex flex-row flex-nowrap items-center gap-2 overflow-x-auto pb-2 border-b border-slate-100 dark:border-slate-800 text-xs whitespace-nowrap">
            <span class="text-slate-400 font-semibold uppercase text-[10px] tracking-wider shrink-0 mr-1 inline-flex items-center space-x-1">
              <Filter class="h-3 w-3" />
              <span>{$language === 'vi' ? 'Trạng thái:' : 'Status:'}</span>
            </span>
            <button
              type="button"
              onclick={() => (subscriberStatusFilter = 'all')}
              class="inline-flex items-center justify-center px-3 py-1.5 rounded-lg text-xs font-semibold transition shrink-0 whitespace-nowrap {subscriberStatusFilter === 'all'
                ? 'bg-blue-600 text-white shadow-xs'
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'}"
            >
              <span>{$language === 'vi' ? `Tất cả (${$bills.length})` : `All (${$bills.length})`}</span>
            </button>
            <button
              type="button"
              onclick={() => (subscriberStatusFilter = 'Unpaid')}
              class="inline-flex items-center justify-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition shrink-0 whitespace-nowrap {subscriberStatusFilter === 'Unpaid'
                ? 'bg-rose-600 text-white shadow-xs'
                : 'bg-rose-50 dark:bg-rose-950/30 text-rose-700 dark:text-rose-400 hover:bg-rose-100 dark:hover:bg-rose-900/40'}"
            >
              <AlertCircle class="h-3 w-3" />
              <span>{$language === 'vi' ? `Chưa thanh toán (${$bills.filter(b => b.status === 'Unpaid').length})` : `Unpaid (${$bills.filter(b => b.status === 'Unpaid').length})`}</span>
            </button>
            <button
              type="button"
              onclick={() => (subscriberStatusFilter = 'Partially Paid')}
              class="inline-flex items-center justify-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition shrink-0 whitespace-nowrap {subscriberStatusFilter === 'Partially Paid'
                ? 'bg-amber-600 text-white shadow-xs'
                : 'bg-amber-50 dark:bg-amber-950/30 text-amber-700 dark:text-amber-400 hover:bg-amber-100 dark:hover:bg-amber-900/40'}"
            >
              <AlertCircle class="h-3 w-3" />
              <span>{$language === 'vi' ? `Thanh toán 1 phần (${$bills.filter(b => b.status === 'Partially Paid').length})` : `Partially Paid (${$bills.filter(b => b.status === 'Partially Paid').length})`}</span>
            </button>
            <button
              type="button"
              onclick={() => (subscriberStatusFilter = 'Paid')}
              class="inline-flex items-center justify-center space-x-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition shrink-0 whitespace-nowrap {subscriberStatusFilter === 'Paid'
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'bg-emerald-50 dark:bg-emerald-950/30 text-emerald-700 dark:text-emerald-400 hover:bg-emerald-100 dark:hover:bg-emerald-900/40'}"
            >
              <CheckCircle2 class="h-3 w-3" />
              <span>{$language === 'vi' ? `Đã thanh toán (${$bills.filter(b => b.status === 'Paid').length})` : `Paid (${$bills.filter(b => b.status === 'Paid').length})`}</span>
            </button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm">
              <thead class="bg-slate-50 dark:bg-slate-800/70 text-xs font-semibold text-slate-500 uppercase border-b border-slate-200 dark:border-slate-700">
                <tr>
                  <th class="px-4 py-3">{$language === 'vi' ? 'Khách hàng' : 'Customer'}</th>
                  <th class="px-4 py-3">{$language === 'vi' ? 'Hóa đơn & Gói cước' : 'Invoice & Plan'}</th>
                  <th class="px-4 py-3 text-right">{$language === 'vi' ? 'Tổng cước' : 'Total'}</th>
                  <th class="px-4 py-3 text-right">{$language === 'vi' ? 'Còn nợ' : 'Due Amount'}</th>
                  <th class="px-4 py-3 text-center">{$language === 'vi' ? 'Trạng thái' : 'Status'}</th>
                  <th class="px-4 py-3 text-center">{$language === 'vi' ? 'Lịch sử thanh toán' : 'Payment History'}</th>
                  <th class="px-4 py-3 text-right">{$language === 'vi' ? 'Tự động nhắc nhở' : 'Auto Reminder'}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                {#if filteredBills.length > 0}
                  {#each filteredBills as bill (bill.id)}
                    <tr class="hover:bg-slate-50 dark:hover:bg-slate-800/50 transition">
                      <td class="px-4 py-3">
                        <div class="font-bold text-slate-900 dark:text-white">{bill.customerName}</div>
                        <div class="text-xs font-mono text-slate-500">{bill.accountId}</div>
                      </td>
                      <td class="px-4 py-3">
                        <div class="font-mono text-blue-600 dark:text-blue-400 font-medium">{bill.invoiceNumber}</div>
                        <div class="text-xs text-slate-500">{bill.planName} · {bill.billingMonth}</div>
                      </td>
                      <td class="px-4 py-3 text-right font-mono font-medium text-slate-700 dark:text-slate-300">
                        ${bill.totalAmount.toFixed(2)}
                      </td>
                      <td class="px-4 py-3 text-right font-mono font-bold {bill.dueAmount > 0 ? 'text-rose-600 dark:text-rose-400' : 'text-emerald-600 dark:text-emerald-400'}">
                        ${bill.dueAmount.toFixed(2)}
                      </td>
                      <td class="px-4 py-3 text-center">
                        {#if bill.status === 'Paid'}
                          <span class="inline-flex px-2 py-0.5 text-[11px] font-bold rounded-full bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400">
                            {$language === 'vi' ? 'Đã thanh toán' : 'Paid'}
                          </span>
                        {:else if bill.status === 'Partially Paid'}
                          <div class="flex flex-col items-center">
                            <span class="inline-flex items-center space-x-1 px-2.5 py-0.5 text-[11px] font-bold rounded-full bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300 border border-amber-300 dark:border-amber-700/50">
                              <AlertCircle class="h-3 w-3 text-amber-600 dark:text-amber-400" />
                              <span>{$language === 'vi' ? 'Thanh toán 1 phần' : 'Partially Paid'}</span>
                            </span>
                            <span class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5 font-mono">
                              {$language === 'vi' ? `Đã trả $${bill.amountPaid.toFixed(2)}` : `Paid $${bill.amountPaid.toFixed(2)}`}
                            </span>
                          </div>
                        {:else}
                          <span class="inline-flex px-2 py-0.5 text-[11px] font-bold rounded-full bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400">
                            {$language === 'vi' ? 'Chưa thanh toán' : 'Unpaid'}
                          </span>
                        {/if}
                      </td>
                      <td class="px-4 py-3 text-center">
                        <div class="flex items-center justify-center space-x-1.5">
                          <button
                            type="button"
                            onclick={() => handleOpenEditBill(bill)}
                            class="inline-flex items-center space-x-1 px-2.5 py-1 rounded-lg text-xs font-semibold border transition bg-violet-50 dark:bg-violet-950/40 text-violet-700 dark:text-violet-300 border-violet-200 dark:border-violet-800 hover:bg-violet-100"
                            title={$language === 'vi' ? 'Sửa chi tiết hóa đơn (kế toán)' : 'Edit invoice details (accounts)'}
                          >
                            <FileText class="h-3.5 w-3.5" />
                            <span>{$language === 'vi' ? 'Sửa' : 'Edit'}</span>
                          </button>
                          <button
                            type="button"
                            onclick={() => (historyModalBill = bill)}
                            class="inline-flex items-center space-x-1 px-2.5 py-1 rounded-lg text-xs font-semibold border transition {bill.paymentHistory && bill.paymentHistory.length > 0
                              ? 'bg-sky-50 dark:bg-sky-950/40 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800 hover:bg-sky-100'
                              : 'bg-slate-50 dark:bg-slate-800 text-slate-500 border-slate-200 dark:border-slate-700 hover:bg-slate-100'}"
                          >
                            <History class="h-3.5 w-3.5" />
                            <span>{$language === 'vi' ? `Lịch sử (${bill.paymentHistory ? bill.paymentHistory.length : 0})` : `History (${bill.paymentHistory ? bill.paymentHistory.length : 0})`}</span>
                          </button>
                        </div>
                      </td>
                      <td class="px-4 py-3 text-right">
                        {#if bill.status !== 'Paid'}
                          {#if remindedBillIds[bill.id]}
                            <div class="inline-flex items-center space-x-1 px-2.5 py-1 rounded-lg text-xs font-semibold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 ml-auto">
                              <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
                              <span>{$language === 'vi' ? 'Đã nhắc nhở' : 'Reminder Sent'}</span>
                            </div>
                          {:else}
                            <button 
                              type="button"
                              class="px-2.5 py-1.5 bg-amber-50 hover:bg-amber-100 dark:bg-amber-950/40 dark:hover:bg-amber-900/40 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800 rounded-lg text-xs font-bold transition shadow-xs inline-flex items-center space-x-1.5 ml-auto"
                              onclick={() => handleSendReminder(bill)}
                            >
                              <Bell class="h-3.5 w-3.5 text-amber-600 dark:text-amber-400 animate-bounce" />
                              <span>{$language === 'vi' ? 'Tự động nhắc nhở' : 'Auto Remind'}</span>
                            </button>
                          {/if}
                        {:else}
                          <span class="inline-flex items-center space-x-1 text-xs text-slate-400 dark:text-slate-500 font-medium ml-auto">
                            <CheckCircle2 class="h-3.5 w-3.5 text-emerald-500" />
                            <span>{$language === 'vi' ? 'Đã thanh toán đủ' : 'Fully Settled'}</span>
                          </span>
                        {/if}
                      </td>
                    </tr>
                  {/each}
                {:else}
                  <tr>
                    <td colspan="7" class="px-4 py-8 text-center text-slate-500 text-xs">
                      <Clock class="h-8 w-8 mx-auto text-slate-400 mb-2" />
                      <p>{$language === 'vi' ? 'Không có hóa đơn nào phù hợp với bộ lọc trạng thái này.' : 'No invoices match this status filter.'}</p>
                    </td>
                  </tr>
                {/if}
              </tbody>
            </table>
          </div>
        </div>

        <!-- Section 2: Master Real-Time Customer Payment History Audit Log -->
        <div class="bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <h3 class="text-base font-bold text-slate-900 dark:text-white flex items-center space-x-2">
                <History class="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
                <span>{$language === 'vi' ? 'Nhật ký lịch sử thanh toán của khách hàng' : 'Customer Payment History & Transaction Audit Log'}</span>
              </h3>
              <p class="text-xs text-slate-500 mt-0.5">
                {$language === 'vi'
                  ? 'Ghi nhận và cập nhật thời gian thực mọi giao dịch thanh toán từ Cổng khách hàng (User Portal), Thu cước tự động và Quầy giao dịch'
                  : 'Real-time transaction stream tracking customer settlements from User Portal, Auto-Pay, and Retail Counters'}
              </p>
            </div>

            <!-- Summary KPI Badges -->
            <div class="flex items-center space-x-3 shrink-0 flex-wrap">
              <div class="px-3 py-1.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-900/50 text-xs">
                <span class="text-slate-500">{$language === 'vi' ? 'Tổng số GD:' : 'Total Payments:'}</span>
                <strong class="font-mono text-emerald-700 dark:text-emerald-300 ml-1">{allCustomerPayments.length}</strong>
              </div>
              <div class="px-3 py-1.5 rounded-lg bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-900/50 text-xs">
                <span class="text-slate-500">{$language === 'vi' ? 'Đã thu:' : 'Revenue Collected:'}</span>
                <strong class="font-mono text-sky-700 dark:text-sky-300 ml-1">${totalCollectedRevenue.toFixed(2)}</strong>
              </div>
            </div>
          </div>

          <!-- Search & Filter Bar -->
          <div class="flex items-center space-x-3">
            <div class="relative flex-1">
              <Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
              <input
                type="text"
                bind:value={customerPaymentSearch}
                placeholder={$language === 'vi' ? 'Tìm theo tên khách hàng, mã tài khoản, số hóa đơn, mã giao dịch hoặc phương thức...' : 'Search by customer, account ID, invoice #, payment ID, or mode...'}
                class="w-full pl-9 pr-3 py-2 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            {#if customerPaymentSearch}
              <button
                type="button"
                onclick={() => (customerPaymentSearch = '')}
                class="px-3 py-2 text-xs font-semibold text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 border border-slate-200 dark:border-slate-800 rounded-lg"
              >
                {$language === 'vi' ? 'Xóa lọc' : 'Clear'}
              </button>
            {/if}
          </div>

          <!-- Transactions Table -->
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 dark:bg-slate-800/70 uppercase font-semibold text-slate-500 border-b border-slate-200 dark:border-slate-700">
                <tr>
                  <th class="px-3 py-2.5">{$language === 'vi' ? 'Mã giao dịch' : 'Payment ID'}</th>
                  <th class="px-3 py-2.5">{$language === 'vi' ? 'Khách hàng' : 'Customer'}</th>
                  <th class="px-3 py-2.5">{$language === 'vi' ? 'Số hóa đơn' : 'Invoice #'}</th>
                  <th class="px-3 py-2.5">{$language === 'vi' ? 'Ngày thanh toán' : 'Date'}</th>
                  <th class="px-3 py-2.5">{$language === 'vi' ? 'Phương thức' : 'Mode'}</th>
                  <th class="px-3 py-2.5 text-right">{$language === 'vi' ? 'Số tiền' : 'Amount'}</th>
                  <th class="px-3 py-2.5">{$language === 'vi' ? 'Mã tham chiếu' : 'Reference No.'}</th>
                  <th class="px-3 py-2.5">{$language === 'vi' ? 'Kênh ghi nhận' : 'Recorded By'}</th>
                  <th class="px-3 py-2.5 text-center">{$language === 'vi' ? 'Biên lai' : 'Receipt'}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                {#if filteredCustomerPayments.length > 0}
                  {#each filteredCustomerPayments as payment (payment.paymentId)}
                    <tr class="hover:bg-slate-50/80 dark:hover:bg-slate-800/40 transition">
                      <td class="px-3 py-2.5 font-mono font-bold text-blue-600 dark:text-blue-400">
                        {payment.paymentId}
                      </td>
                      <td class="px-3 py-2.5">
                        <div class="font-bold text-slate-900 dark:text-white">{payment.customerName}</div>
                        <div class="text-[11px] font-mono text-slate-500">{payment.accountId}</div>
                      </td>
                      <td class="px-3 py-2.5 font-mono text-slate-700 dark:text-slate-300">
                        {payment.invoiceNumber}
                      </td>
                      <td class="px-3 py-2.5 text-slate-600 dark:text-slate-400">
                        {payment.paymentDate}
                      </td>
                      <td class="px-3 py-2.5">
                        <span class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-semibold {payment.paymentMode === 'Bank Transfer/NEFT'
                          ? 'bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-300'
                          : payment.paymentMode === 'Credit/Debit Card'
                            ? 'bg-indigo-100 text-indigo-800 dark:bg-indigo-950 dark:text-indigo-300'
                            : payment.paymentMode === 'UPI/Digital Wallet'
                              ? 'bg-pink-100 text-pink-800 dark:bg-pink-950 dark:text-pink-300'
                              : 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300'}">
                          {#if payment.paymentMode === 'Bank Transfer/NEFT'}
                            <QrCode class="h-3 w-3" />
                          {:else if payment.paymentMode === 'Credit/Debit Card'}
                            <CreditCard class="h-3 w-3" />
                          {:else if payment.paymentMode === 'UPI/Digital Wallet'}
                            <Smartphone class="h-3 w-3" />
                          {:else}
                            <Wallet class="h-3 w-3" />
                          {/if}
                          <span>{payment.paymentMode}</span>
                        </span>
                      </td>
                      <td class="px-3 py-2.5 text-right font-mono font-bold text-emerald-600 dark:text-emerald-400">
                        ${payment.amountPaid.toFixed(2)}
                      </td>
                      <td class="px-3 py-2.5 font-mono text-[11px] text-slate-500">
                        {payment.referenceNumber}
                      </td>
                      <td class="px-3 py-2.5">
                        <span class="px-2 py-0.5 rounded text-[10px] font-semibold {payment.recordedBy.includes('User Portal')
                          ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800'
                          : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}">
                          {payment.recordedBy}
                        </span>
                      </td>
                      <td class="px-3 py-2.5 text-center">
                        <button
                          type="button"
                          onclick={() => (receiptModalPayment = payment)}
                          class="p-1 hover:bg-slate-200 dark:hover:bg-slate-700 rounded text-slate-500 hover:text-blue-600 dark:hover:text-blue-400 transition"
                          title="Xem biên lai"
                        >
                          <Printer class="h-3.5 w-3.5" />
                        </button>
                      </td>
                    </tr>
                  {/each}
                {:else}
                  <tr>
                    <td colspan="9" class="px-4 py-8 text-center text-slate-500">
                      <History class="h-8 w-8 mx-auto text-slate-400 mb-2" />
                      <p>{$language === 'vi' ? 'Không có giao dịch thanh toán nào phù hợp.' : 'No payment records match your search.'}</p>
                    </td>
                  </tr>
                {/if}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    {/if}

    <!-- MODAL: CHI TIẾT KẾT NỐI & SANG TRANG TẠO HÓA ĐƠN CƯỚC -->
    {#if detailModalConnection}
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm animate-in fade-in duration-200">
        <div class="bg-white dark:bg-slate-900 rounded-2xl max-w-2xl w-full border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
          <!-- Modal Header -->
          <div class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50/70 dark:bg-slate-800/40">
            <div class="flex items-center space-x-2.5">
              <div class="p-2 bg-blue-500/20 text-blue-600 dark:text-blue-400 rounded-lg">
                <Network class="h-5 w-5" />
              </div>
              <div>
                <h3 class="font-bold text-base text-slate-900 dark:text-white">
                  {$language === 'vi' ? 'Chi tiết kết nối thuê bao' : 'Customer Connection Details'}
                </h3>
                <p class="text-xs text-slate-500 font-mono">
                  {detailModalConnection.customerName} · {detailModalConnection.accountId}
                </p>
              </div>
            </div>
            <button
              type="button"
              onclick={() => (detailModalConnection = null)}
              class="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg cursor-pointer"
              aria-label="Close"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Modal Body (Scrollable) -->
          <div class="p-6 overflow-y-auto space-y-6 text-xs flex-1">
            <!-- 1. Customer & Location Info -->
            <div class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-800 space-y-3">
              <div class="text-[11px] font-bold uppercase tracking-wider text-slate-500">
                {$language === 'vi' ? 'Thông tin chủ thuê bao' : 'Subscriber Information'}
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Họ và tên:' : 'Customer Name:'}</span>
                  <strong class="text-slate-900 dark:text-white block mt-0.5">{detailModalConnection.customerName}</strong>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Mã tài khoản (Account ID):' : 'Account ID:'}</span>
                  <strong class="font-mono text-blue-600 dark:text-blue-400 block mt-0.5">{detailModalConnection.accountId}</strong>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Số điện thoại:' : 'Phone Number:'}</span>
                  <div class="text-slate-700 dark:text-slate-300 font-semibold mt-0.5">{detailModalConnection.customerPhone}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Mã đơn hàng gốc:' : 'Originating Order ID:'}</span>
                  <div class="font-mono text-slate-700 dark:text-slate-300 mt-0.5">{detailModalConnection.orderId}</div>
                </div>
                <div class="sm:col-span-2">
                  <span class="text-slate-400">{$language === 'vi' ? 'Địa chỉ lắp đặt đường truyền:' : 'Installation Address:'}</span>
                  <div class="text-slate-800 dark:text-slate-200 font-medium mt-0.5">{detailModalConnection.installationAddress}</div>
                </div>
              </div>
            </div>

            <!-- 2. Technical & Tariff Parameters -->
            <div class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-800 space-y-3">
              <div class="text-[11px] font-bold uppercase tracking-wider text-slate-500">
                {$language === 'vi' ? 'Thông số gói cước & Kỹ thuật' : 'Plan & Technical Specifications'}
              </div>
              <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Loại kết nối:' : 'Connection Type:'}</span>
                  <div class="font-bold text-slate-900 dark:text-white mt-0.5">{detailModalConnection.connectionType}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Gói cước:' : 'Service Plan:'}</span>
                  <div class="font-bold text-slate-900 dark:text-white mt-0.5">{cleanPlanName(detailModalConnection.planName)}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Cước thuê bao/tháng:' : 'Monthly Rental:'}</span>
                  <div class="font-mono font-bold text-emerald-600 dark:text-emerald-400 mt-0.5">${(detailModalConnection.monthlyRental || 0).toFixed(2)}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Cọc bảo đảm:' : 'Security Deposit:'}</span>
                  <div class="font-mono font-bold text-slate-900 dark:text-white mt-0.5">${(detailModalConnection.securityDeposit || 0).toFixed(2)}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Địa chỉ IP:' : 'IP Address:'}</span>
                  <div class="font-mono text-slate-700 dark:text-slate-300 mt-0.5">{detailModalConnection.ipAddress || 'DHCP Pool'}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Cổng port:' : 'Port Number:'}</span>
                  <div class="font-mono text-slate-700 dark:text-slate-300 mt-0.5">{detailModalConnection.portNumber || 'GE-0/0/1'}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Mã thiết bị CPE:' : 'Assigned CPE Serial:'}</span>
                  <div class="font-mono text-slate-700 dark:text-slate-300 mt-0.5">{detailModalConnection.assignedDeviceSerial || 'N/A'}</div>
                </div>
                <div>
                  <span class="text-slate-400">{$language === 'vi' ? 'Ngày kích hoạt:' : 'Installed Date:'}</span>
                  <div class="text-slate-700 dark:text-slate-300 mt-0.5">{detailModalConnection.installedDate || 'N/A'}</div>
                </div>
              </div>
            </div>

            <!-- 3. Existing Bills for this connection -->
            <div class="space-y-2">
              <div class="flex items-center justify-between">
                <span class="text-[11px] font-bold uppercase tracking-wider text-slate-500">
                  {$language === 'vi' ? 'Hóa đơn đã phát hành cho kết nối này:' : 'Issued Invoices for this Account:'}
                </span>
                <span class="text-xs text-slate-400">{detailModalBills.length} {$language === 'vi' ? 'hóa đơn' : 'invoice(s)'}</span>
              </div>

              {#if detailModalBills.length > 0}
                <div class="space-y-2">
                  {#each detailModalBills as bill (bill.id)}
                    <div class="p-3 rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900/60 flex items-center justify-between">
                      <div>
                        <div class="font-mono font-bold text-blue-600 dark:text-blue-400">#{bill.invoiceNumber}</div>
                        <div class="text-[11px] text-slate-500">{bill.billingMonth} · {bill.billingDate}</div>
                      </div>
                      <div class="text-right">
                        <div class="font-mono font-bold text-slate-900 dark:text-white">${bill.totalAmount.toFixed(2)}</div>
                        <span class="inline-flex px-2 py-0.2 rounded text-[10px] font-semibold {bill.status === 'Paid'
                          ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                          : 'bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300'}">
                          {bill.status}
                        </span>
                      </div>
                    </div>
                  {/each}
                </div>
              {:else}
                <div class="p-4 rounded-xl border border-dashed border-amber-300 dark:border-amber-900/60 bg-amber-50/40 dark:bg-amber-950/20 text-center">
                  <AlertCircle class="h-6 w-6 text-amber-500 mx-auto mb-1.5" />
                  <p class="font-semibold text-amber-800 dark:text-amber-300">
                    {$language === 'vi' ? 'Kết nối này chưa có hóa đơn cước nào!' : 'No invoices generated for this connection yet.'}
                  </p>
                  <p class="text-[11px] text-slate-500 mt-0.5">
                    {$language === 'vi'
                      ? 'Bấm nút bên dưới để chuyển sang trang tạo hóa đơn cước đầu kỳ với các thông số đã nạp sẵn.'
                      : 'Click the button below to jump to bill generation with these parameters pre-loaded.'}
                  </p>
                </div>
              {/if}
            </div>
          </div>

          <!-- Modal Footer with CTA -->
          <div class="px-6 py-4 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/30 flex items-center justify-between">
            <button
              type="button"
              onclick={() => (detailModalConnection = null)}
              class="px-4 py-2 bg-slate-200 hover:bg-slate-300 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-800 dark:text-slate-200 rounded-lg text-xs font-bold transition cursor-pointer"
            >
              {$language === 'vi' ? 'Đóng' : 'Close'}
            </button>

            <!-- Big CTA: Sang trang tạo hóa đơn cước -->
            <button
              type="button"
              onclick={() => detailModalConnection && handleGoToBilling(detailModalConnection)}
              class="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-xs font-bold transition flex items-center space-x-2 shadow-sm cursor-pointer"
            >
              <FileText class="h-4 w-4" />
              <span>{$language === 'vi' ? 'Sang trang tạo hóa đơn cước →' : 'Proceed to Generate Bill →'}</span>
            </button>
          </div>
        </div>
      </div>
    {/if}

    <!-- MODAL 1: CHI TIẾT LỊCH SỬ THANH TOÁN TỪNG HÓA ĐƠN -->
    {#if editBillModal && editingBill}
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm animate-in fade-in duration-200">
        <div class="bg-white dark:bg-slate-900 rounded-2xl max-w-2xl w-full border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden flex flex-col max-h-[85vh]">
          <!-- Modal Header -->
          <div class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50/70 dark:bg-slate-800/40">
            <div class="flex items-center space-x-2.5">
              <div class="p-2 bg-violet-500/20 text-violet-600 dark:text-violet-400 rounded-lg">
                <FileText class="h-5 w-5" />
              </div>
              <div>
                <h3 class="font-bold text-base text-slate-900 dark:text-white">
                  {$language === 'vi' ? 'Sửa chi tiết hóa đơn' : 'Edit Invoice Details'}
                </h3>
                <p class="text-xs text-slate-500 font-mono">
                  #{editingBill.invoiceNumber} · {editingBill.customerName} ({editingBill.accountId})
                </p>
              </div>
            </div>
            <button
              type="button"
              onclick={() => (editBillModal = false)}
              class="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg"
              aria-label="Close"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Edit Form -->
          <form onsubmit={(e) => { e.preventDefault(); handleSaveEditBill(); }} class="p-6 overflow-y-auto space-y-4">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Kỳ cước' : 'Billing Month'}</span>
                <input type="text" bind:value={editBillForm.billingMonth}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Hạn thanh toán' : 'Due Date'}</span>
                <input type="date" bind:value={editBillForm.dueDate}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Tiền cọc ($)' : 'Security Deposit ($)'}</span>
                <input type="number" step="0.01" min="0" bind:value={editBillForm.securityDeposit}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Cước thuê bao ($)' : 'Monthly Rental ($)'}</span>
                <input type="number" step="0.01" min="0" bind:value={editBillForm.monthlyRental}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Cước theo giờ ($)' : 'Hourly Charges ($)'}</span>
                <input type="number" step="0.01" min="0" bind:value={editBillForm.hourlyCharges}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Chiết khấu (%)' : 'Discount (%)'}</span>
                <input type="number" step="0.01" min="0" max="100" bind:value={editBillForm.discountPercent}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Số tiền chiết khấu ($)' : 'Discount Amount ($)'}</span>
                <input type="number" step="0.01" min="0" bind:value={editBillForm.discountAmount}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Thuế dịch vụ (%) — chuẩn 12.24' : 'Service Tax (%) — standard 12.24'}</span>
                <input type="number" step="0.01" min="0" bind:value={editBillForm.serviceTaxRate}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
              <label class="block">
                <span class="text-xs font-semibold text-slate-600 dark:text-slate-300">{$language === 'vi' ? 'Phí trễ hạn ($)' : 'Late Fee ($)'}</span>
                <input type="number" step="0.01" min="0" bind:value={editBillForm.lateFeeAmount}
                  class="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-violet-500" />
              </label>
            </div>

            <div class="p-3 rounded-lg bg-violet-50 dark:bg-violet-950/30 border border-violet-200 dark:border-violet-800/50 flex items-center justify-between text-xs">
              <span class="text-slate-600 dark:text-slate-300">
                {$language === 'vi'
                  ? 'Tổng dự kiến (tự tính lại: cọc + cước tháng + cước giờ − chiết khấu, rồi cộng thuế + phí trễ):'
                  : 'Projected total (recomputed: deposit + rental + hourly − discount, plus tax + late fee):'}
              </span>
              <strong class="font-mono text-base text-violet-700 dark:text-violet-300">
                ${editBillPreviewTotal.toFixed(2)}
              </strong>
            </div>

            <p class="text-[11px] text-slate-500">
              {$language === 'vi'
                ? 'Tiền đã thu không sửa được tại đây — ghi nhận qua nút thanh toán. Backend luôn tự tính lại subtotal, thuế 12.24%, tổng tiền, công nợ và trạng thái sau khi lưu.'
                : 'Amount paid cannot be edited here — record it via the payment action. The backend always recalculates subtotal, 12.24% tax, total, due amount and status on save.'}
            </p>

            <div class="flex justify-end space-x-2 pt-1">
              <button type="button" onclick={() => (editBillModal = false)}
                class="px-4 py-2 bg-slate-200 hover:bg-slate-300 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-800 dark:text-slate-200 rounded-lg text-xs font-bold transition">
                {$language === 'vi' ? 'Hủy' : 'Cancel'}
              </button>
              <button type="submit" disabled={isSavingBill}
                class="px-4 py-2 bg-gradient-to-r from-violet-600 to-purple-600 hover:from-violet-700 hover:to-purple-700 disabled:opacity-60 text-white rounded-lg text-xs font-bold transition shadow-sm">
                {isSavingBill
                  ? ($language === 'vi' ? 'Đang lưu…' : 'Saving…')
                  : ($language === 'vi' ? 'Lưu và tính lại tổng' : 'Save & Recalculate')}
              </button>
            </div>
          </form>
        </div>
      </div>
    {/if}

    {#if historyModalBill}
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm animate-in fade-in duration-200">
        <div class="bg-white dark:bg-slate-900 rounded-2xl max-w-2xl w-full border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden flex flex-col max-h-[85vh]">
          <!-- Modal Header -->
          <div class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50/70 dark:bg-slate-800/40">
            <div class="flex items-center space-x-2.5">
              <div class="p-2 bg-blue-500/20 text-blue-600 dark:text-blue-400 rounded-lg">
                <History class="h-5 w-5" />
              </div>
              <div>
                <h3 class="font-bold text-base text-slate-900 dark:text-white">
                  {$language === 'vi' ? 'Lịch sử thanh toán của khách hàng' : 'Customer Invoice Payment History'}
                </h3>
                <p class="text-xs text-slate-500 font-mono">
                  {historyModalBill.customerName} ({historyModalBill.accountId}) · #{historyModalBill.invoiceNumber}
                </p>
              </div>
            </div>
            <button
              type="button"
              onclick={() => (historyModalBill = null)}
              class="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg"
              aria-label="Close"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Bill Summary Info -->
          <div class="px-6 py-4 bg-slate-50/40 dark:bg-slate-800/20 border-b border-slate-100 dark:border-slate-800 grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
            <div>
              <div class="text-slate-500">{$language === 'vi' ? 'Gói cước' : 'Plan'}</div>
              <div class="font-bold text-slate-900 dark:text-white mt-0.5">{historyModalBill.planName}</div>
            </div>
            <div>
              <div class="text-slate-500">{$language === 'vi' ? 'Tổng tiền' : 'Total Amount'}</div>
              <div class="font-mono font-bold text-slate-900 dark:text-white mt-0.5">${historyModalBill.totalAmount.toFixed(2)}</div>
            </div>
            <div>
              <div class="text-slate-500">{$language === 'vi' ? 'Đã thanh toán' : 'Amount Paid'}</div>
              <div class="font-mono font-bold text-emerald-600 dark:text-emerald-400 mt-0.5">${historyModalBill.amountPaid.toFixed(2)}</div>
            </div>
            <div>
              <div class="text-slate-500">{$language === 'vi' ? 'Còn nợ' : 'Remaining Due'}</div>
              <div class="font-mono font-bold text-rose-600 dark:text-rose-400 mt-0.5">${historyModalBill.dueAmount.toFixed(2)}</div>
            </div>
          </div>

          <!-- Payments List -->
          <div class="p-6 overflow-y-auto space-y-4">
            <h4 class="text-xs font-bold uppercase tracking-wider text-slate-500">
              {$language === 'vi' ? 'Danh sách các lần thanh toán đã ghi nhận:' : 'Recorded Payment Transactions:'}
            </h4>

            {#if historyModalBill.paymentHistory && historyModalBill.paymentHistory.length > 0}
              <div class="space-y-3">
                {#each historyModalBill.paymentHistory as pay}
                  <div class="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
                    <div class="space-y-1">
                      <div class="flex items-center space-x-2">
                        <span class="font-mono font-bold text-blue-600 dark:text-blue-400">{pay.paymentId}</span>
                        <span class="px-2 py-0.5 rounded text-[11px] font-semibold bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300">
                          {pay.paymentMode}
                        </span>
                      </div>
                      <div class="text-slate-500">
                        <span>{$language === 'vi' ? 'Ngày GD' : 'Date'}: <strong class="text-slate-700 dark:text-slate-300">{pay.paymentDate}</strong></span>
                        <span class="mx-1">•</span>
                        <span>{$language === 'vi' ? 'Mã tham chiếu' : 'Ref'}: <strong class="font-mono text-slate-700 dark:text-slate-300">{pay.referenceNumber}</strong></span>
                      </div>
                      <div class="text-[11px] text-slate-400">
                        {$language === 'vi' ? 'Kênh ghi nhận:' : 'Recorded by:'} <span class="text-sky-600 dark:text-sky-400 font-semibold">{pay.recordedBy}</span>
                      </div>
                    </div>

                    <div class="text-left sm:text-right">
                      <div class="text-[11px] text-slate-500">{$language === 'vi' ? 'Số tiền thanh toán' : 'Amount Paid'}</div>
                      <div class="text-lg font-bold font-mono text-emerald-600 dark:text-emerald-400">
                        ${pay.amountPaid.toFixed(2)}
                      </div>
                    </div>
                  </div>
                {/each}
              </div>
            {:else}
              <div class="p-8 text-center border-2 border-dashed border-slate-200 dark:border-slate-800 rounded-xl">
                <Clock class="h-8 w-8 mx-auto text-slate-400 mb-2" />
                <p class="text-xs text-slate-500">{$language === 'vi' ? 'Chưa có giao dịch thanh toán nào cho hóa đơn này.' : 'No payment records on file for this invoice.'}</p>
              </div>
            {/if}
          </div>

          <!-- Modal Footer -->
          <div class="px-6 py-3 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/30 flex justify-end">
            <button
              type="button"
              onclick={() => (historyModalBill = null)}
              class="px-4 py-2 bg-slate-200 hover:bg-slate-300 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-800 dark:text-slate-200 rounded-lg text-xs font-bold transition"
            >
              {$language === 'vi' ? 'Đóng' : 'Close'}
            </button>
          </div>
        </div>
      </div>
    {/if}

    <!-- MODAL 2: BIÊN LAI THANH TOÁN (RECEIPT MODAL) -->
    {#if receiptModalPayment}
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/50 backdrop-blur-sm animate-in fade-in duration-200">
        <div class="bg-white dark:bg-slate-900 rounded-2xl max-w-md w-full border border-slate-200 dark:border-slate-800 shadow-2xl p-6 space-y-4">
          <div class="text-center border-b border-slate-200 dark:border-slate-800 pb-4">
            <h4 class="font-extrabold text-base text-slate-900 dark:text-white uppercase tracking-wider">
              Nexus Telecom Services
            </h4>
            <p class="text-xs text-slate-500">{$language === 'vi' ? 'Biên lai thanh toán cước viễn thông' : 'Official Payment Receipt'}</p>
            <div class="mt-2 text-xs font-mono text-blue-600 dark:text-blue-400 font-bold">{receiptModalPayment.paymentId}</div>
          </div>

          <div class="space-y-2 text-xs">
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Khách hàng:' : 'Customer:'}</span>
              <strong class="text-slate-900 dark:text-white">{receiptModalPayment.customerName}</strong>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Mã thuê bao:' : 'Account ID:'}</span>
              <strong class="font-mono text-slate-900 dark:text-white">{receiptModalPayment.accountId}</strong>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Số hóa đơn:' : 'Invoice No:'}</span>
              <strong class="font-mono text-slate-900 dark:text-white">{receiptModalPayment.invoiceNumber}</strong>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Gói cước:' : 'Plan:'}</span>
              <span class="text-slate-700 dark:text-slate-300">{receiptModalPayment.planName}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Ngày thanh toán:' : 'Date:'}</span>
              <span class="text-slate-700 dark:text-slate-300">{receiptModalPayment.paymentDate}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Phương thức:' : 'Payment Mode:'}</span>
              <span class="font-semibold text-slate-900 dark:text-white">{receiptModalPayment.paymentMode}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Mã giao dịch tham chiếu:' : 'Reference No:'}</span>
              <span class="font-mono text-slate-700 dark:text-slate-300">{receiptModalPayment.referenceNumber}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-slate-500">{$language === 'vi' ? 'Ghi nhận bởi:' : 'Recorded By:'}</span>
              <span class="text-sky-600 dark:text-sky-400 font-semibold">{receiptModalPayment.recordedBy}</span>
            </div>

            <div class="pt-3 border-t border-slate-200 dark:border-slate-800 flex justify-between items-center text-sm">
              <span class="font-bold text-slate-900 dark:text-white">{$language === 'vi' ? 'Tổng tiền đã thanh toán:' : 'Total Amount Paid:'}</span>
              <span class="font-mono font-black text-emerald-600 dark:text-emerald-400 text-base">${receiptModalPayment.amountPaid.toFixed(2)}</span>
            </div>
          </div>

          <div class="pt-3 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between">
            <button
              type="button"
              onclick={() => (receiptModalPayment = null)}
              class="px-4 py-2 text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
            >
              {$language === 'vi' ? 'Đóng' : 'Close'}
            </button>
            <button
              type="button"
              onclick={() => {
                toast.success($language === 'vi' ? 'Đã gửi lệnh in biên lai tới máy in hệ thống!' : 'Receipt print command sent to system printer!');
              }}
              class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-lg transition flex items-center space-x-1"
            >
              <Printer class="h-3.5 w-3.5" />
              <span>{$language === 'vi' ? 'In biên lai' : 'Print Receipt'}</span>
            </button>
          </div>
        </div>
      </div>
    {/if}

    <!-- TAB 3: CHARGE SETTINGS -->
    {#if activeTab === 'charge-settings'}
      <div class="max-w-2xl bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
        <div>
          <h3 class="text-lg font-bold text-slate-900 dark:text-white">
            {$language === 'vi' ? 'Chính sách biểu cước & Cài đặt thuế suất' : 'Tariff Policy & Statutory Tax Settings'}
          </h3>
          <p class="text-xs text-slate-500 mt-0.5">
            {$language === 'vi'
              ? 'Thiết lập các thông số tiêu chuẩn áp dụng tự động vào công thức phát hành hóa đơn.'
              : 'Set baseline parameters applied globally across bill generation formulas.'}
          </p>
        </div>

        <form onsubmit={handleSaveSettings} class="space-y-4 text-sm">
          <!-- Service Tax % -->
          <div class="p-4 rounded-xl bg-blue-50/60 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-900/50 space-y-2">
            <div class="flex items-center justify-between">
              <div>
                <label class="block text-xs font-bold uppercase tracking-wider text-blue-900 dark:text-blue-200">
                  {$language === 'vi' ? 'Thuế suất dịch vụ chuẩn (%) *' : 'Standard Service Tax Rate (%) *'}
                </label>

              </div>
              <input
                type="number"
                step="0.01"
                min="0"
                max="100"
                required
                bind:value={taxRateSetting}
                class="w-24 px-3 py-1.5 text-right font-mono tabular-nums font-bold bg-white dark:bg-slate-950 border border-slate-300 dark:border-slate-700 rounded-lg text-sm text-blue-600 dark:text-blue-400"
              />
            </div>
          </div>

          <!-- Late Payment Fee % -->
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              {$language === 'vi' ? 'Phí phạt quá hạn thanh toán (%)' : 'Late Payment Surcharge (%)'}
            </label>
            <input
              type="number"
              step="0.1"
              min="0"
              bind:value={lateFeeSetting}
              class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg font-mono tabular-nums text-slate-900 dark:text-white"
            />
          </div>

          <!-- Default Security Deposits -->
          <div class="space-y-3 pt-2">
            <label class="block text-xs font-semibold uppercase tracking-wider text-slate-500">
              {$language === 'vi' ? 'Định mức cọc bảo đảm mặc định ($)' : 'Default Security Deposit Benchmarks ($)'}
            </label>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <span class="text-xs text-slate-600 dark:text-slate-400 block mb-1">Broadband ($)</span>
                <input
                  type="number"
                  step="0.01"
                  bind:value={broadbandDeposit}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg font-mono tabular-nums text-xs text-slate-900 dark:text-white"
                />
              </div>
              <div>
                <span class="text-xs text-slate-600 dark:text-slate-400 block mb-1">Dial-Up ($)</span>
                <input
                  type="number"
                  step="0.01"
                  bind:value={dialUpDeposit}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg font-mono tabular-nums text-xs text-slate-900 dark:text-white"
                />
              </div>
              <div>
                <span class="text-xs text-slate-600 dark:text-slate-400 block mb-1">Landline ($)</span>
                <input
                  type="number"
                  step="0.01"
                  bind:value={landlineDeposit}
                  class="w-full px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg font-mono tabular-nums text-xs text-slate-900 dark:text-white"
                />
              </div>
            </div>
          </div>

          <div class="flex justify-end pt-4 border-t border-slate-100 dark:border-slate-800">
            <button
              type="submit"
              class="px-6 py-2.5 rounded-lg text-sm font-semibold bg-blue-600 hover:bg-blue-700 text-white transition shadow"
            >
              {$language === 'vi' ? 'Lưu cấu hình biểu cước' : 'Save Charge Settings'}
            </button>
          </div>
        </form>
      </div>
    {/if}
  </div>

  <!-- GENERATED INVOICE PRINTABLE PREVIEW MODAL -->
  {#if generatedInvoiceModal}
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 backdrop-blur-sm p-4">
      <div class="w-full max-w-xl rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-2xl space-y-6">
        <!-- Invoice Header -->
        <div class="flex items-start justify-between border-b border-slate-200 dark:border-slate-800 pb-4">
          <div>
            <div class="text-xs font-bold text-blue-600 uppercase tracking-widest">Nexus Service Marketing System</div>
            <h2 class="text-xl font-extrabold text-slate-900 dark:text-white">
              {$language === 'vi' ? 'HÓA ĐƠN THUẾ DỊCH VỤ' : 'TAX INVOICE STATEMENT'}
            </h2>
            <p class="text-xs text-slate-500">
              {$language === 'vi' ? 'Biên lai cước chính thức & Chi tiết thuế GTGT' : 'Official Billing Receipt & Tax Breakdown'}
            </p>
          </div>
          <button
            onclick={() => (generatedInvoiceModal = null)}
            class="text-slate-400 hover:text-slate-600 dark:hover:text-white"
          >
            <X class="h-5 w-5" />
          </button>
        </div>

        <!-- Invoice Details -->
        <div class="grid grid-cols-2 gap-4 text-xs">
          <div class="space-y-1">
            <div class="text-slate-400">{$language === 'vi' ? 'Khách hàng:' : 'Billed To:'}</div>
            <div class="font-bold text-sm text-slate-900 dark:text-white">{generatedInvoiceModal.customerName}</div>
            <div class="font-mono text-blue-600 dark:text-blue-400">
              {$language === 'vi' ? 'Mã TK:' : 'Account ID:'} {generatedInvoiceModal.accountId}
            </div>
          </div>

          <div class="text-right space-y-1">
            <div class="font-mono font-bold text-slate-900 dark:text-white">{generatedInvoiceModal.invoiceNumber}</div>
            <div class="text-slate-500">{$language === 'vi' ? 'Ngày lập:' : 'Billing Date:'} {generatedInvoiceModal.billingDate}</div>
            <div class="text-slate-500">{$language === 'vi' ? 'Hạn nộp:' : 'Due Date:'} {generatedInvoiceModal.dueDate}</div>
          </div>
        </div>

        <!-- Line Items Breakdown -->
        <div class="border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden text-xs">
          <table class="w-full text-left">
            <thead class="bg-slate-50 dark:bg-slate-800/60 uppercase font-semibold text-slate-500 border-b border-slate-200 dark:border-slate-800">
              <tr>
                <th class="px-3 py-2">{$language === 'vi' ? 'Nội dung' : 'Description'}</th>
                <th class="px-3 py-2 text-right">{$language === 'vi' ? 'Số tiền ($)' : 'Amount ($)'}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr>
                <td class="px-3 py-2">{$language === 'vi' ? 'Tiền đặt cọc (Hoàn lại)' : 'Security Deposit (Refundable)'}</td>
                <td class="px-3 py-2 text-right font-mono tabular-nums">${generatedInvoiceModal.securityDeposit.toFixed(2)}</td>
              </tr>
              <tr>
                <td class="px-3 py-2">{$language === 'vi' ? 'Cước thuê bao gói' : 'Monthly Rental'} — {cleanPlanName(generatedInvoiceModal.planName)}</td>
                <td class="px-3 py-2 text-right font-mono tabular-nums">${generatedInvoiceModal.monthlyRental.toFixed(2)}</td>
              </tr>
              {#if generatedInvoiceModal.hourlyCharges > 0}
                <tr>
                  <td class="px-3 py-2">{$language === 'vi' ? 'Cước theo giờ / Cước sử dụng' : 'Hourly / Usage Charges'}</td>
                  <td class="px-3 py-2 text-right font-mono tabular-nums">${generatedInvoiceModal.hourlyCharges.toFixed(2)}</td>
                </tr>
              {/if}
              {#if generatedInvoiceModal.discountAmount > 0}
                <tr class="text-emerald-700 dark:text-emerald-400">
                  <td class="px-3 py-2">{$language === 'vi' ? `Chiết khấu số lượng / Doanh nghiệp (${generatedInvoiceModal.discountPercent}%)` : `Bulk / Corporate Scheme Discount (${generatedInvoiceModal.discountPercent}%)`}</td>
                  <td class="px-3 py-2 text-right font-mono tabular-nums font-bold">−${generatedInvoiceModal.discountAmount.toFixed(2)}</td>
                </tr>
              {/if}
              <tr class="bg-slate-50 dark:bg-slate-950 font-semibold">
                <td class="px-3 py-2">{$language === 'vi' ? 'Tổng tiền trước thuế' : 'Subtotal'}</td>
                <td class="px-3 py-2 text-right font-mono tabular-nums">${generatedInvoiceModal.subtotal.toFixed(2)}</td>
              </tr>
              <tr class="bg-blue-50/50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-300 font-medium">
                <td class="px-3 py-2">{$language === 'vi' ? `Thuế dịch vụ (${generatedInvoiceModal.serviceTaxRate}%)` : `Service Tax (${generatedInvoiceModal.serviceTaxRate}%)`}</td>
                <td class="px-3 py-2 text-right font-mono tabular-nums font-bold">+${generatedInvoiceModal.serviceTaxAmount.toFixed(2)}</td>
              </tr>
              <tr class="bg-slate-900 text-white font-bold text-sm">
                <td class="px-3 py-2.5">{$language === 'vi' ? 'Tổng cộng phải trả ($)' : 'Grand Total Due ($)'}</td>
                <td class="px-3 py-2.5 text-right font-mono tabular-nums text-emerald-400">${generatedInvoiceModal.totalAmount.toFixed(2)}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Actions -->
        <div class="flex justify-between items-center pt-2">
          <span class="text-[11px] text-slate-400">
            {$language === 'vi' ? 'Hóa đơn được phát hành tự động bởi Nexus Core Billing Engine' : 'Statutory invoice generated by Nexus Core Billing Engine'}
          </span>
          <div class="flex gap-2">
            <button
              onclick={() => window.print()}
              class="px-3 py-1.5 rounded-lg text-xs font-semibold border border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800 flex items-center space-x-1.5"
            >
              <Printer class="h-3.5 w-3.5" />
              <span>{$language === 'vi' ? 'In hóa đơn' : 'Print Invoice'}</span>
            </button>
            <button
              onclick={() => (generatedInvoiceModal = null)}
              class="px-4 py-1.5 rounded-lg text-xs font-bold bg-blue-600 hover:bg-blue-700 text-white shadow"
            >
              {$language === 'vi' ? 'Hoàn tất' : 'Done'}
            </button>
            </div>
          </div>
        </div>
      </div>
    {/if}

    <!-- MODAL: CÀI ĐẶT THỜI GIAN NHẮC NHỞ THANH TOÁN & TỰ ĐỘNG THANH TOÁN -->
    {#if showScheduleConfigModal}
      <div class="fixed inset-0 bg-slate-950/50 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-in fade-in duration-200">
        <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl max-w-3xl w-full max-h-[92vh] overflow-hidden shadow-2xl flex flex-col">
          
          <!-- Modal Header -->
          <div class="p-5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50/50 dark:bg-slate-800/40 shrink-0">
            <div class="flex items-center space-x-3">
              <div class="h-10 w-10 rounded-xl bg-gradient-to-tr from-blue-600 to-sky-500 text-white flex items-center justify-center shadow-md">
                <Clock class="h-5 w-5" />
              </div>
              <div>
                <h3 class="font-bold text-base text-slate-900 dark:text-white flex items-center space-x-2">
                  <span>{$language === 'vi' ? 'Cài đặt thời gian nhắc nhở & Tự động thanh toán' : 'Reminder Schedule & Auto-Pay Configuration'}</span>
                  <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-sky-100 text-sky-700 dark:bg-sky-900/30 dark:text-sky-300">
                    {$language === 'vi' ? 'Hệ thống tự động' : 'Core Automation'}
                  </span>
                </h3>
              </div>
            </div>
            <button
              type="button"
              onclick={() => (showScheduleConfigModal = false)}
              class="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Modal Body (Scrollable) -->
          <div class="p-6 overflow-y-auto space-y-6 flex-1 text-xs">
            
            <!-- SECTION 1: Cài đặt thời gian nhắc nhở thanh toán -->
            <div class="p-5 rounded-xl bg-amber-50/40 dark:bg-amber-950/20 border border-amber-200/80 dark:border-amber-900/40 space-y-4">
              <div class="flex items-center justify-between border-b border-amber-200/60 dark:border-amber-900/40 pb-3">
                <div class="flex items-center space-x-2.5">
                  <div class="p-1.5 rounded-lg bg-amber-500 text-white">
                    <Bell class="h-4 w-4" />
                  </div>
                  <div>
                    <h4 class="font-bold text-sm text-slate-900 dark:text-white">
                      {$language === 'vi' ? '1. Thời gian gửi thông báo nhắc nhở cước' : '1. Payment Reminder Timeline & Schedule'}
                    </h4>
                    <!-- <p class="text-[11px] text-slate-500">
                      {$language === 'vi' ? 'Lịch trình gửi SMS, Email và thông báo Cổng người dùng trước và sau ngày đến hạn' : 'Schedule for SMS, Email, and Portal push notifications before and after due date'}
                    </p> -->
                  </div>
                </div>
                <!-- Toggle Reminder Active -->
                <label class="relative inline-flex items-center cursor-pointer">
                  <input type="checkbox" bind:checked={reminderEnabled} class="sr-only peer" />
                  <div class="w-9 h-5 bg-slate-200 peer-focus:outline-none rounded-full peer dark:bg-slate-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all dark:border-slate-600 peer-checked:bg-amber-500"></div>
                </label>
              </div>

              {#if reminderEnabled}
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 pt-1">
                  <!-- Reminder 1: Trước hạn bao nhiêu ngày -->
                  <div>
                    <label class="block font-bold text-slate-700 dark:text-slate-300 mb-1.5">
                      {$language === 'vi' ? 'Nhắc nhở trước hạn thanh toán:' : 'Dispatch reminder before due date:'}
                    </label>
                    <div class="grid grid-cols-4 gap-1.5">
                      {#each [1, 3, 5, 7] as days}
                        <button
                          type="button"
                          onclick={() => (reminderDaysBeforeDue = days)}
                          class="py-1.5 px-2 rounded-lg text-xs font-semibold border transition text-center {reminderDaysBeforeDue === days
                            ? 'bg-amber-600 text-white border-amber-600 shadow-xs'
                            : 'bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 hover:bg-amber-50'}"
                        >
                          {$language === 'vi' ? `${days} ngày` : `${days}d`}
                        </button>
                      {/each}
                    </div>
                    <!-- <p class="text-[10px] text-slate-500 mt-1">
                      {$language === 'vi' ? `* Hệ thống sẽ gửi thông báo cước trước hạn ${reminderDaysBeforeDue} ngày.` : `* System sends reminder notice ${reminderDaysBeforeDue} days prior to due date.`}
                    </p> -->
                  </div>

                  <!-- Reminder 2: Khung giờ gửi thông báo hàng ngày -->
                  <div>
                    <label class="block font-bold text-slate-700 dark:text-slate-300 mb-1.5">
                      {$language === 'vi' ? 'Khung giờ gửi thông báo hàng ngày:' : 'Daily notification dispatch window:'}
                    </label>
                    <select
                      bind:value={reminderDailyTime}
                      class="w-full px-3 py-1.5 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-medium text-slate-800 dark:text-slate-200"
                    >
                      <option value="08:00">08:00 AM — {$language === 'vi' ? 'Đầu giờ sáng' : 'Early Morning'}</option>
                      <option value="09:00">09:00 AM — {$language === 'vi' ? 'Giờ chuẩn (Khuyến nghị)' : 'Standard (Recommended)'}</option>
                      <option value="11:30">11:30 AM — {$language === 'vi' ? 'Buổi trưa' : 'Noon'}</option>
                      <option value="14:00">02:00 PM — {$language === 'vi' ? 'Đầu giờ chiều' : 'Afternoon'}</option>
                      <option value="17:00">05:00 PM — {$language === 'vi' ? 'Cuối ngày làm việc' : 'End of workday'}</option>
                    </select>
                    <!-- <p class="text-[10px] text-slate-500 mt-1">
                      {$language === 'vi' ? 'Khung giờ đảm bảo tỷ lệ đọc và phản hồi cước cao nhất.' : 'Optimal engagement window for bill delivery.'}
                    </p> -->
                  </div>

                  <!-- Reminder 3: Nhắc vào đúng ngày đến hạn -->
                  <div class="flex items-start space-x-2 bg-white dark:bg-slate-800 p-3 rounded-lg border border-slate-200 dark:border-slate-700">
                    <input
                      type="checkbox"
                      id="remindOnDueDate"
                      bind:checked={remindOnDueDate}
                      class="mt-0.5 h-4 w-4 rounded text-amber-600 focus:ring-amber-500 border-slate-300 dark:border-slate-600"
                    />
                    <label for="remindOnDueDate" class="cursor-pointer">
                      <div class="font-bold text-slate-800 dark:text-slate-200">
                        {$language === 'vi' ? 'Thông báo vào đúng ngày đến hạn' : 'Alert on invoice due date'}
                      </div>
                      <div class="text-[10px] text-slate-500">
                        {$language === 'vi' ? 'Gửi cảnh báo khẩn cấp vào ngày cuối cùng để tránh phát sinh phí trễ hạn' : 'Send urgent reminder on the final due date to avoid 5% late penalty fee.'}
                      </div>
                    </label>
                  </div>

                  <!-- Reminder 4: Tần suất nhắc sau quá hạn -->
                  <div class="bg-white dark:bg-slate-800 p-3 rounded-lg border border-slate-200 dark:border-slate-700">
                    <label class="block font-bold text-slate-800 dark:text-slate-200 mb-1">
                      {$language === 'vi' ? 'Định kỳ nhắc sau khi quá hạn cước:' : 'Recurring overdue follow-up:'}
                    </label>
                    <div class="flex items-center space-x-2">
                      <select
                        bind:value={remindOverdueInterval}
                        class="flex-1 px-2.5 py-1 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-md text-xs font-medium text-slate-800 dark:text-slate-200"
                      >
                        <option value={0}>{$language === 'vi' ? 'Không gửi sau quá hạn' : 'Do not follow up'}</option>
                        <option value={2}>{$language === 'vi' ? 'Mỗi 2 ngày một lần' : 'Every 2 days'}</option>
                        <option value={3}>{$language === 'vi' ? 'Mỗi 3 ngày (Khuyến nghị)' : 'Every 3 days (Recommended)'}</option>
                        <option value={5}>{$language === 'vi' ? 'Mỗi 5 ngày một lần' : 'Every 5 days'}</option>
                      </select>
                    </div>
                  </div>
                </div>

                <!-- Channels Selection -->
                <div class="pt-2 border-t border-amber-200/50 dark:border-amber-900/30">
                  <span class="block font-bold text-slate-700 dark:text-slate-300 mb-2">
                    {$language === 'vi' ? 'Kênh truyền thông tin thông báo nhắc cước:' : 'Dispatch communication channels:'}
                  </span>
                  <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
                    <label class="flex items-center space-x-2 p-2.5 rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 cursor-pointer">
                      <input type="checkbox" bind:checked={reminderChannelSms} class="h-4 w-4 text-amber-600 rounded" />
                      <span class="font-medium text-slate-800 dark:text-slate-200">SMS Brandname</span>
                    </label>
                    <label class="flex items-center space-x-2 p-2.5 rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 cursor-pointer">
                      <input type="checkbox" bind:checked={reminderChannelEmail} class="h-4 w-4 text-amber-600 rounded" />
                      <span class="font-medium text-slate-800 dark:text-slate-200">Email Hóa đơn</span>
                    </label>
                    <label class="flex items-center space-x-2 p-2.5 rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 cursor-pointer">
                      <input type="checkbox" bind:checked={reminderChannelInApp} class="h-4 w-4 text-amber-600 rounded" />
                      <span class="font-medium text-slate-800 dark:text-slate-200">User Portal App</span>
                    </label>
                  </div>
                </div>
              {:else}
                <p class="text-xs text-slate-500 italic py-2">
                  {$language === 'vi' ? 'Hệ thống nhắc nhở tự động hiện đang tạm tắt. Khách hàng sẽ không nhận được thông báo nhắc cước định kỳ.' : 'Automated reminder dispatch is currently disabled.'}
                </p>
              {/if}
            </div>

            <!-- SECTION 2: Cài đặt tự động thanh toán cước phí -->
            <div class="p-5 rounded-xl bg-sky-50/40 dark:bg-sky-950/20 border border-sky-200/80 dark:border-sky-900/40 space-y-4">
              <div class="flex items-center justify-between border-b border-sky-200/60 dark:border-sky-900/40 pb-3">
                <div class="flex items-center space-x-2.5">
                  <div class="p-1.5 rounded-lg bg-sky-600 text-white">
                    <CreditCard class="h-4 w-4" />
                  </div>
                  <div>
                    <h4 class="font-bold text-sm text-slate-900 dark:text-white">
                      {$language === 'vi' ? '2. Cài đặt tự động thanh toán cước phí (Auto-Pay)' : '2. Automated Subscription Auto-Debit & Settlement'}
                    </h4>
                    <p class="text-[11px] text-slate-500">
                      {$language === 'vi' ? 'Tự động trừ tiền qua tài khoản ngân hàng hoặc thẻ đã liên kết khi đến mốc thanh toán' : 'Automatically charge linked bank cards/accounts when invoice reaches scheduled date'}
                    </p>
                  </div>
                </div>
                <!-- Toggle Auto-Pay Active -->
                <label class="relative inline-flex items-center cursor-pointer">
                  <input type="checkbox" bind:checked={autoPayEnabled} class="sr-only peer" />
                  <div class="w-9 h-5 bg-slate-200 peer-focus:outline-none rounded-full peer dark:bg-slate-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all dark:border-slate-600 peer-checked:bg-sky-600"></div>
                </label>
              </div>

              {#if autoPayEnabled}
                <div class="space-y-3 pt-1">
                  <div>
                    <label class="block font-bold text-slate-700 dark:text-slate-300 mb-1.5">
                      {$language === 'vi' ? 'Thời điểm thực hiện trừ cước tự động:' : 'Auto-debit execution trigger:'}
                    </label>
                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                      <label class="flex items-start space-x-2 p-3 rounded-lg bg-white dark:bg-slate-800 border transition cursor-pointer {autoPayScheduleType === 'days_before_due' ? 'border-sky-500 ring-1 ring-sky-500' : 'border-slate-200 dark:border-slate-700'}">
                        <input
                          type="radio"
                          name="autoPayScheduleType"
                          value="days_before_due"
                          bind:group={autoPayScheduleType}
                          class="mt-0.5 text-sky-600"
                        />
                        <div>
                          <div class="font-bold text-slate-900 dark:text-white">{$language === 'vi' ? 'Trước ngày hạn chót 1 ngày' : '1 Day Before Due Date'}</div>
                          <div class="text-[10px] text-slate-500">{$language === 'vi' ? 'Khuyến nghị để tránh phí trễ hạn và gián đoạn tín hiệu.' : 'Recommended to prevent late fees and uninterrupted connectivity.'}</div>
                        </div>
                      </label>

                      <label class="flex items-start space-x-2 p-3 rounded-lg bg-white dark:bg-slate-800 border transition cursor-pointer {autoPayScheduleType === 'on_due_date' ? 'border-sky-500 ring-1 ring-sky-500' : 'border-slate-200 dark:border-slate-700'}">
                        <input
                          type="radio"
                          name="autoPayScheduleType"
                          value="on_due_date"
                          bind:group={autoPayScheduleType}
                          class="mt-0.5 text-sky-600"
                        />
                        <div>
                          <div class="font-bold text-slate-900 dark:text-white">{$language === 'vi' ? 'Vào đúng ngày hạn chót' : 'On Exact Due Date'}</div>
                          <div class="text-[10px] text-slate-500">{$language === 'vi' ? 'Trừ cước vào 23:59 ngày đến hạn thanh toán.' : 'Debit occurs at 23:59 on the statutory due date.'}</div>
                        </div>
                      </label>

                      <label class="flex items-start space-x-2 p-3 rounded-lg bg-white dark:bg-slate-800 border transition cursor-pointer {autoPayScheduleType === 'fixed_day' ? 'border-sky-500 ring-1 ring-sky-500' : 'border-slate-200 dark:border-slate-700'}">
                        <input
                          type="radio"
                          name="autoPayScheduleType"
                          value="fixed_day"
                          bind:group={autoPayScheduleType}
                          class="mt-0.5 text-sky-600"
                        />
                        <div>
                          <div class="font-bold text-slate-900 dark:text-white">{$language === 'vi' ? 'Ngày cố định hàng tháng' : 'Fixed Day of Every Month'}</div>
                          <div class="flex items-center space-x-1.5 mt-1">
                            <span class="text-[10px] text-slate-500">{$language === 'vi' ? 'Ngày:' : 'Day:'}</span>
                            <select
                              bind:value={autoPayFixedDay}
                              class="px-2 py-0.5 bg-slate-50 dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded text-[11px] font-bold"
                            >
                              <option value={1}>01 hàng tháng</option>
                              <option value={5}>05 hàng tháng</option>
                              <option value={10}>10 hàng tháng</option>
                              <option value={15}>15 hàng tháng</option>
                              <option value={20}>20 hàng tháng</option>
                            </select>
                          </div>
                        </div>
                      </label>

                      <label class="flex items-start space-x-2 p-3 rounded-lg bg-white dark:bg-slate-800 border transition cursor-pointer {autoPayScheduleType === 'bill_issue_date' ? 'border-sky-500 ring-1 ring-sky-500' : 'border-slate-200 dark:border-slate-700'}">
                        <input
                          type="radio"
                          name="autoPayScheduleType"
                          value="bill_issue_date"
                          bind:group={autoPayScheduleType}
                          class="mt-0.5 text-sky-600"
                        />
                        <div>
                          <div class="font-bold text-slate-900 dark:text-white">{$language === 'vi' ? 'Ngay khi xuất hóa đơn' : 'Upon Invoice Generation'}</div>
                          <div class="text-[10px] text-slate-500">{$language === 'vi' ? 'Trừ tiền tức thì ngay khi Kế toán bấm xuất hóa đơn.' : 'Instant settlement upon bill dispatch.'}</div>
                        </div>
                      </label>
                    </div>
                  </div>

                  <!-- Policies: Partial Debit, Retry Failed, Auto Receipt -->
                  <div class="space-y-2 pt-2 border-t border-sky-200/50 dark:border-sky-900/30">
                    <label class="flex items-center space-x-2.5 cursor-pointer">
                      <input type="checkbox" bind:checked={autoPayAllowPartial} class="h-4 w-4 text-sky-600 rounded" />
                      <div>
                        <span class="font-bold text-slate-800 dark:text-slate-200">
                          {$language === 'vi' ? 'Cho phép trừ cước 1 phần (Partial Auto-Debit)' : 'Enable partial auto-debit if balance is insufficient'}
                        </span>
                        <span class="block text-[10px] text-slate-500">
                          {$language === 'vi' ? 'Nếu số dư khách hàng không đủ toàn bộ hóa đơn, hệ thống vẫn trừ số dư khả dụng và ghi nhận trạng thái "Thanh toán 1 phần".' : 'Debits available funds and updates invoice status to "Partially Paid" instead of failing.'}
                        </span>
                      </div>
                    </label>

                    <label class="flex items-center space-x-2.5 cursor-pointer">
                      <input type="checkbox" bind:checked={autoPayRetryFailed} class="h-4 w-4 text-sky-600 rounded" />
                      <div>
                        <span class="font-bold text-slate-800 dark:text-slate-200">
                          {$language === 'vi' ? 'Tự động thử lại khi giao dịch bị từ chối' : 'Automatic retry policy on failure'}
                        </span>
                        <span class="block text-[10px] text-slate-500">
                          {$language === 'vi' ? 'Tự động quét lại mỗi 24 giờ (tối đa 3 lần) trước khi chuyển sang cảnh báo nợ xấu.' : 'Retries every 24 hours (up to 3 times) before flagging.'}
                        </span>
                      </div>
                    </label>

                    <label class="flex items-center space-x-2.5 cursor-pointer">
                      <input type="checkbox" bind:checked={autoPayNotifyCustomer} class="h-4 w-4 text-sky-600 rounded" />
                      <div>
                        <span class="font-bold text-slate-800 dark:text-slate-200">
                          {$language === 'vi' ? 'Xuất biên lai & Gửi thông báo thanh toán thành công' : 'Issue electronic receipt & push confirmation'}
                        </span>
                        <span class="block text-[10px] text-slate-500">
                          {$language === 'vi' ? 'Ghi nhận ngay vào Nhật ký lịch sử thanh toán toàn hệ thống và gửi email biên lai cho khách.' : 'Instantly logged in master payment audit trail and emailed to subscriber.'}
                        </span>
                      </div>
                    </label>
                  </div>
                </div>
              {:else}
                <p class="text-xs text-slate-500 italic py-2">
                  {$language === 'vi' ? 'Hệ thống tự động trừ cước (Auto-Pay) hiện đang tạm tắt. Khách hàng cần chủ động thanh toán qua cổng hoặc quầy giao dịch.' : 'Auto-Pay system is currently disabled.'}
                </p>
              {/if}
            </div>

            <!-- SECTION 3: Live Automation Engine Status -->
            <div class="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 flex flex-col sm:flex-row items-center justify-between gap-4">
              <div class="flex items-center space-x-3">
                <div class="h-9 w-9 rounded-lg bg-emerald-100 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center font-bold">
                  <ShieldCheck class="h-5 w-5" />
                </div>
                <div>
                  <div class="font-bold text-xs text-slate-900 dark:text-white flex items-center space-x-2">
                    <span>{$language === 'vi' ? 'Trạng thái Nexus Auto-Billing Daemon' : 'Nexus Auto-Billing Daemon Status'}</span>
                    <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[9px] font-bold bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300">
                      ● {$language === 'vi' ? 'Đang hoạt động' : 'Running'}
                    </span>
                  </div>
                  <div class="text-[11px] text-slate-500 mt-0.5">
                    {$language === 'vi'
                      ? `Lần quét kế tiếp: ${reminderDailyTime} ngày mai · Đang theo dõi ${$bills.filter(b => b.status !== 'Paid').length} hóa đơn nợ cước`
                      : `Next scheduled pass: ${reminderDailyTime} tomorrow · Tracking ${$bills.filter(b => b.status !== 'Paid').length} outstanding invoices`}
                  </div>
                </div>
              </div>

              <div class="flex items-center space-x-3 text-center shrink-0">
                <div class="px-3 py-1 bg-white dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700">
                  <div class="text-[10px] text-slate-400">{$language === 'vi' ? 'Chờ nhắc cước' : 'Pending Reminders'}</div>
                  <div class="font-mono font-bold text-amber-600 dark:text-amber-400">
                    {$bills.filter(b => b.status !== 'Paid').length}
                  </div>
                </div>
                <div class="px-3 py-1 bg-white dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700">
                  <div class="text-[10px] text-slate-400">{$language === 'vi' ? 'Đủ điều kiện Auto-Pay' : 'Eligible Auto-Pay'}</div>
                  <div class="font-mono font-bold text-sky-600 dark:text-sky-400">
                    {$bills.filter(b => b.status !== 'Paid').length}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="p-4 border-t border-slate-100 dark:border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-3 bg-slate-50/50 dark:bg-slate-800/30 shrink-0">
            <span class="text-[11px] text-slate-500">
              {$language === 'vi' ? '* Cấu hình sẽ có hiệu lực ngay lập tức cho toàn bộ chu kỳ cước viễn thông.' : '* Settings take immediate effect for all subscriber accounts.'}
            </span>
            <div class="flex items-center space-x-2 w-full sm:w-auto justify-end">
              <button
                type="button"
                onclick={() => (showScheduleConfigModal = false)}
                class="px-3.5 py-1.5 rounded-lg text-xs font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 transition"
              >
                {$language === 'vi' ? 'Đóng' : 'Close'}
              </button>
              <button
                type="button"
                onclick={handleTriggerScheduleRun}
                class="px-3.5 py-1.5 rounded-lg text-xs font-bold bg-amber-500 hover:bg-amber-600 text-white shadow-xs transition flex items-center space-x-1.5"
              >
                <Play class="h-3.5 w-3.5" />
                <span>{$language === 'vi' ? 'Quét & Thực thi ngay' : 'Run Auto-Process Now'}</span>
              </button>
              <button
                type="button"
                onclick={handleSaveScheduleSettings}
                class="px-4 py-1.5 rounded-lg text-xs font-bold bg-blue-600 hover:bg-blue-700 text-white shadow-sm transition flex items-center space-x-1.5"
              >
                <CheckCircle2 class="h-3.5 w-3.5" />
                <span>{$language === 'vi' ? 'Lưu cấu hình' : 'Save Settings'}</span>
              </button>
            </div>
          </div>

        </div>
      </div>
    {/if}

    <!-- SETTINGS TAB -->
    {#if activeTab === 'settings'}
      <SettingsView />
    {/if}

    <!-- PROFILE TAB -->
    {#if activeTab === 'profile'}
      <ProfileView />
    {/if}
</DashboardLayout>

