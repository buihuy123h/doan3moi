<script lang="ts">
  // Customer sign-in is Account ID only (issued at purchase); staff enter by role.
  import { authStore, normalizeAccountId } from '../context/AuthContext';
  import { nexusStore } from '../context/NexusContext';
  import { themeStore } from '../context/ThemeContext';
  import { languageStore } from '../context/LanguageContext';
  import LanguageToggle from '../components/layout/LanguageToggle.svelte';
  import { dashboardPathForRole, navigate, queryParam } from '../lib/router';
  import type { Order, RoleType } from '../types/nexus';
  import {
    Layers, KeyRound, ShieldCheck,
    ArrowRight, ArrowLeft, Sparkles, Sun, Moon, ShoppingCart, Search,
    User, Lock, Mail, Crown, Store, Wrench, Receipt, Copy, Check, Zap
  } from 'lucide-svelte';
  import { isValidEmail } from '../lib/validation';
  import { toast } from 'svelte-sonner';

  const { loginWithAccountId, loginWithCredentials, loginQuickStaff, requestLoginOtp, loginWithOtp, currentUser } = authStore;
  const { connections, orders } = nexusStore;
  const { theme, toggleTheme } = themeStore;
  const { t, language } = languageStore;

  // Tab switching: 'user' = customer portal, 'staff' = internal staff portal.
  let activeTab = $state<'user' | 'staff'>('user');

  let accountId = $state('');
  let errorMessage = $state('');

  // ---- Đăng nhập khách hàng 2 bước: nhập mã tài khoản -> nhập OTP gửi về email ----
  let otpStep = $state<'account' | 'otp'>('account');
  let otpCode = $state('');
  let otpMaskedEmail = $state('');
  let otpSubmitting = $state(false);
  // Số giây mã OTP còn hiệu lực và số giây phải chờ trước khi được xin mã mới.
  let otpValidSeconds = $state(0);
  let otpResendSeconds = $state(0);

  // Một bộ đếm lùi duy nhất cho cả hai đồng hồ ở trên.
  $effect(() => {
    if (otpStep !== 'otp') return;

    const timer = setInterval(() => {
      if (otpValidSeconds > 0) otpValidSeconds -= 1;
      if (otpResendSeconds > 0) otpResendSeconds -= 1;
    }, 1000);

    return () => clearInterval(timer);
  });

  const formatCountdown = (totalSeconds: number) => {
    const m = Math.floor(totalSeconds / 60);
    const sec = totalSeconds % 60;
    return `${m}:${String(sec).padStart(2, '0')}`;
  };

  // Staff credential login (email + password against the backend API).
  let staffEmail = $state('');
  let staffPassword = $state('');
  let staffSubmitting = $state(false);
  let staffError = $state('');

  // Order lookup: until technical confirms feasibility there is no Account ID,
  // so a fresh customer follows the order by its code.
  let orderQuery = $state(queryParam('order') ?? '');
  let trackedOrder = $state<Order | null>(null);
  let trackError = $state('');

  // A real subscriber ID from the seeded data, offered as a demo shortcut.
  const demoAccountId = $derived($connections[0]?.accountId ?? '');

  // Danh sách các tài khoản Demo chuẩn của nội bộ hệ thống (Staff, Tech, Admin, Accounts)
  interface StaffDemoItem {
    role: Exclude<RoleType, 'user'>;
    name: string;
    email: string;
    badgeName: string;
    title: string;
    department: string;
    scope: string;
    route: string;
    theme: {
      cardBg: string;
      cardBorder: string;
      iconGradient: string;
      badgeBg: string;
      badgeText: string;
      btnBg: string;
    };
  }

  const staffDemoList: StaffDemoItem[] = [
    {
      role: 'admin',
      name: 'Sarah Jenkins',
      email: 'sarah.jenkins@nexus.telecom',
      badgeName: 'Admin / Quản trị',
      title: 'General Manager',
      department: 'Executive Administration',
      scope: 'Toàn quyền: Nhân sự, Bảng giá cước, Kho thiết bị & Doanh thu',
      route: '/admin',
      theme: {
        cardBg: 'bg-purple-50/70 dark:bg-purple-950/25',
        cardBorder: 'border-purple-200 dark:border-purple-800/60 hover:border-purple-400 dark:hover:border-purple-600',
        iconGradient: 'from-purple-600 to-indigo-600',
        badgeBg: 'bg-purple-100 dark:bg-purple-900/60',
        badgeText: 'text-purple-700 dark:text-purple-300',
        btnBg: 'bg-purple-600 hover:bg-purple-700 text-white shadow-purple-600/25',
      },
    },
    {
      role: 'retail',
      name: 'David Chen',
      email: 'david.chen@nexus.telecom',
      badgeName: 'Nhân viên Bán lẻ',
      title: 'Store Representative',
      department: 'Retail Outlets (SH-01 Flagship)',
      scope: 'Tiếp nhận đơn hàng, tra cứu mã đơn & duyệt hồ sơ quầy',
      route: '/retail',
      theme: {
        cardBg: 'bg-sky-50/70 dark:bg-sky-950/25',
        cardBorder: 'border-sky-200 dark:border-sky-800/60 hover:border-sky-400 dark:hover:border-sky-600',
        iconGradient: 'from-sky-500 to-blue-600',
        badgeBg: 'bg-sky-100 dark:bg-sky-900/60',
        badgeText: 'text-sky-700 dark:text-sky-300',
        btnBg: 'bg-sky-600 hover:bg-sky-700 text-white shadow-sky-600/25',
      },
    },
    {
      role: 'technical',
      name: 'Marcus Ramirez',
      email: 'marcus.ramirez@nexus.telecom',
      badgeName: 'Kỹ thuật viên (Tech)',
      title: 'Field Operations Engineer',
      department: 'Technical Operations & NOC',
      scope: 'Khảo sát khả thi (Feasibility) & cấp phát Modem (Provisioning)',
      route: '/technical',
      theme: {
        cardBg: 'bg-amber-50/70 dark:bg-amber-950/25',
        cardBorder: 'border-amber-200 dark:border-amber-800/60 hover:border-amber-400 dark:hover:border-amber-600',
        iconGradient: 'from-amber-500 to-orange-600',
        badgeBg: 'bg-amber-100 dark:bg-amber-900/60',
        badgeText: 'text-amber-800 dark:text-amber-300',
        btnBg: 'bg-amber-600 hover:bg-amber-700 text-white shadow-amber-600/25',
      },
    },
    {
      role: 'accounts',
      name: 'Elena Rostova',
      email: 'elena.rostova@nexus.telecom',
      badgeName: 'Kế toán viên (Finance)',
      title: 'Senior Accountant',
      department: 'Finance & Accounts',
      scope: 'Đối soát công nợ, xuất hóa đơn cước đầu kỳ & thu tiền cọc',
      route: '/accounts',
      theme: {
        cardBg: 'bg-emerald-50/70 dark:bg-emerald-950/25',
        cardBorder: 'border-emerald-200 dark:border-emerald-800/60 hover:border-emerald-400 dark:hover:border-emerald-600',
        iconGradient: 'from-emerald-500 to-teal-600',
        badgeBg: 'bg-emerald-100 dark:bg-emerald-900/60',
        badgeText: 'text-emerald-700 dark:text-emerald-300',
        btnBg: 'bg-emerald-600 hover:bg-emerald-700 text-white shadow-emerald-600/25',
      },
    },
  ];

  let copyingKey = $state<string | null>(null);
  let activeQuickRole = $state<string | null>(null);

  const copyCredential = async (text: string, label: string) => {
    try {
      await navigator.clipboard.writeText(text);
      copyingKey = text;
      toast.success(`Đã sao chép ${label}: ${text}`);
      setTimeout(() => {
        if (copyingKey === text) copyingKey = null;
      }, 2000);
    } catch {
      toast.info(`${label}: ${text}`);
    }
  };

  const fillStaffForm = (email: string, name: string) => {
    staffEmail = email;
    staffPassword = '1234567890';
    staffError = '';
    toast.info(`Đã điền thông tin đăng nhập: ${name}`);
  };

  const handleQuickStaffLogin = async (
    email: string,
    role: Exclude<RoleType, 'user'>,
    name: string
  ) => {
    activeQuickRole = role;
    staffSubmitting = true;
    staffError = '';
    const res = await loginQuickStaff(email, role);
    staffSubmitting = false;
    activeQuickRole = null;

    if (res.success) {
      toast.success(`${$t.auth.loginSuccess} ${name}`);
      navigate(dashboardPathForRole(role));
    } else {
      staffError = res.error;
      toast.error($t.auth.loginFailed);
    }
  };

  // If already logged in, redirect immediately to their dashboard
  $effect(() => {
    if ($currentUser) {
      navigate(dashboardPathForRole($currentUser.role));
    }
  });

  // Arriving from the purchase receipt (#/login?order=...) looks the order up.
  $effect(() => {
    const fromUrl = queryParam('order');
    if (fromUrl && !trackedOrder && !trackError) {
      lookUpOrder(fromUrl);
    }
  });

  const lookUpOrder = (rawId: string) => {
    const id = rawId.trim().toUpperCase();
    console.log("lookUpOrder called with", id, "orders length:", $orders.length);
    trackError = '';
    const found = $orders.find((o) => o.id.toUpperCase() === id) ?? null;
    trackedOrder = found;
    if (!found) trackError = $t.auth.trackNotFound;
  };

  const handleTrackSubmit = (e: SubmitEvent) => {
    e.preventDefault();
    if (!orderQuery.trim()) return;
    lookUpOrder(orderQuery);
  };

  // Keep the field formatted as T064-000000000001 while typing.
  const handleAccountIdInput = (e: Event) => {
    accountId = normalizeAccountId((e.currentTarget as HTMLInputElement).value);
  };

  const signIn = (rawId: string) => {
    errorMessage = '';
    const result = loginWithAccountId(rawId);
    if (result.success) {
      toast.success(`${$t.auth.loginSuccess} ${result.user.name}`);
      navigate('/user');
    } else {
      errorMessage = result.error;
      toast.error($t.auth.loginFailed);
    }
  };

  /** Bước 1 — gửi mã tài khoản, hệ thống gửi OTP về email đã đăng ký của khách. */
  const sendOtp = async () => {
    errorMessage = '';
    otpSubmitting = true;
    try {
      const result = await requestLoginOtp(accountId);
      if (!result.success) {
        errorMessage = result.error;
        toast.error(result.error);
        return false;
      }

      otpMaskedEmail = result.maskedEmail;
      otpValidSeconds = result.expiresInSeconds;
      otpResendSeconds = result.resendAfterSeconds;
      otpCode = '';
      otpStep = 'otp';
      toast.success(
        $language === 'vi'
          ? `Đã gửi mã OTP tới ${result.maskedEmail}`
          : `OTP sent to ${result.maskedEmail}`
      );
      return true;
    } finally {
      otpSubmitting = false;
    }
  };

  const handleLoginSubmit = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!accountId.trim()) {
      errorMessage = $t.auth.accountIdRequired;
      return;
    }
    await sendOtp();
  };

  /** Bước 2 — khách nhập mã OTP 6 chữ số nhận được trong email. */
  const handleOtpSubmit = async (e: SubmitEvent) => {
    e.preventDefault();
    errorMessage = '';

    if (otpCode.trim().length !== 6) {
      errorMessage = $language === 'vi'
        ? 'Mã OTP gồm đúng 6 chữ số.'
        : 'The OTP is exactly 6 digits.';
      return;
    }

    otpSubmitting = true;
    try {
      const result = await loginWithOtp(accountId, otpCode);
      if (result.success) {
        toast.success(`${$t.auth.loginSuccess} ${result.user.name}`);
        navigate('/user');
      } else {
        errorMessage = result.error;
        toast.error(result.error);
      }
    } finally {
      otpSubmitting = false;
    }
  };

  /** Quay lại bước nhập mã tài khoản (khách gõ nhầm mã). */
  const backToAccountStep = () => {
    otpStep = 'account';
    otpCode = '';
    errorMessage = '';
  };

  /** Chỉ nhận chữ số trong ô OTP để khách không dán nhầm ký tự lạ. */
  const handleOtpInput = (e: Event) => {
    otpCode = (e.currentTarget as HTMLInputElement).value.replace(/\D/g, '').slice(0, 6);
  };

  const handleStaffSubmit = async (e: SubmitEvent) => {
    e.preventDefault();
    staffError = '';
    if (!staffEmail.trim()) {
      staffError = $t.auth.staffEmailRequired;
      return;
    }
    if (!isValidEmail(staffEmail)) {
      staffError = $language === 'vi' ? 'Email phải có dạng ten@mien.com.' : 'Email must have the form name@domain.com.';
      return;
    }
    if (!staffPassword) {
      staffError = $t.auth.staffPasswordRequired;
      return;
    }

    staffSubmitting = true;
    const result = await loginWithCredentials(staffEmail, staffPassword);
    staffSubmitting = false;

    if (result.success) {
      toast.success(`${$t.auth.loginSuccess} ${result.user.name}`);
      navigate(dashboardPathForRole(result.user.role));
    } else {
      staffError = result.error;
      toast.error($t.auth.loginFailed);
    }
  };
</script>

<div class="h-full w-full overflow-y-auto bg-[#E0F1FF] dark:bg-[#1B2D40] text-[#1B2D40] dark:text-[#E0F1FF] flex flex-col justify-start py-8 sm:py-12 px-4 sm:px-6 lg:px-8 relative transition-colors duration-300">
  <!-- Top left Back to Home Button -->
  <div class="fixed top-4 left-4 z-50">
    <button
      type="button"
      onclick={() => navigate('/')}
      class="h-10 px-3.5 rounded-full bg-white/85 dark:bg-[#152434]/85 hover:bg-white dark:hover:bg-[#1E3349] text-[#1B2D40] dark:text-[#E0F1FF] border border-[#CCE4F7] dark:border-[#253D56] flex items-center space-x-1.5 transition shadow-md hover:scale-105 active:scale-95 text-xs font-semibold backdrop-blur-md cursor-pointer"
      title={$t.common.home}
    >
      <ArrowLeft class="h-4 w-4" />
      <span class="hidden sm:inline">{$t.common.home}</span>
    </button>
  </div>

  <!-- Top right Controls: Language Toggle & Theme Toggle -->
  <div class="fixed top-4 right-4 z-50 flex items-center space-x-2">
    <LanguageToggle />
    <button
      type="button"
      onclick={toggleTheme}
      class="h-10 w-10 rounded-full bg-white/85 dark:bg-[#152434]/85 hover:bg-white dark:hover:bg-[#1E3349] text-[#1B2D40] dark:text-[#E0F1FF] border border-[#CCE4F7] dark:border-[#253D56] flex items-center justify-center transition shadow-md hover:scale-105 active:scale-95 group backdrop-blur-md cursor-pointer"
      title={$theme === 'dark' ? $t.common.themeLight : $t.common.themeDark}
    >
      {#if $theme === 'dark'}
        <Sun class="h-5 w-5 text-amber-400 group-hover:rotate-45 transition-transform" />
      {:else}
        <Moon class="h-5 w-5 text-sky-700 group-hover:-rotate-12 transition-transform" />
      {/if}
    </button>
  </div>

  <!-- Background ambient lighting -->
  <div class="fixed inset-0 pointer-events-none bg-[radial-gradient(circle_at_top,_var(--tw-gradient-stops))] from-blue-200/40 via-[#E0F1FF] to-[#E0F1FF] dark:from-blue-900/20 dark:via-[#1B2D40] dark:to-[#1B2D40]"></div>
  <div class="fixed inset-0 pointer-events-none bg-[linear-gradient(to_right,#0284c70d_1px,transparent_1px),linear-gradient(to_bottom,#0284c70d_1px,transparent_1px)] dark:bg-[linear-gradient(to_right,#1e293b0a_1px,transparent_1px),linear-gradient(to_bottom,#1e293b0a_1px,transparent_1px)] bg-[size:3rem_3rem]"></div>

  <div class="relative mx-auto w-full max-w-2xl px-2 sm:px-4 my-auto">
    <!-- Brand Header -->
    <div class="text-center space-y-2 mb-8">
      <div class="inline-flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-tr from-blue-600 to-indigo-500 text-white shadow-xl shadow-blue-500/25 mb-1">
        <Layers class="h-8 w-8" />
      </div>
      <h1 class="text-3xl sm:text-4xl font-extrabold tracking-tight text-[#0F1D2B] dark:text-white">NEXUS SYSTEM</h1>
      <p class="text-xs uppercase font-bold tracking-widest text-sky-600 dark:text-blue-400">
        {$t.auth.loginSubtitle}
      </p>

    </div>

    <!-- Single Login Card with 2 Tabs: User | Staff -->
    <div class="bg-white/95 dark:bg-[#152434]/95 border border-[#CCE4F7] dark:border-[#253D56] rounded-2xl shadow-xl dark:shadow-2xl backdrop-blur-xl transition-all duration-300 relative overflow-hidden">

      <!-- ===== TAB BAR ===== -->
      <div
        class="grid grid-cols-2 gap-1.5 p-1.5 bg-[#EDF6FF] dark:bg-[#101C29] border-b border-[#CCE4F7] dark:border-[#253D56]"
        role="tablist"
        aria-label="Login portal selector"
      >
        <!-- Tab 1: User / Customer -->
        <button
          type="button"
          role="tab"
          id="login-tab-user"
          aria-selected={activeTab === 'user'}
          aria-controls="login-panel-user"
          onclick={() => (activeTab = 'user')}
          class="h-11 rounded-xl font-bold text-sm flex items-center justify-center space-x-2 transition-all duration-200 cursor-pointer active:scale-[0.98] {activeTab === 'user'
            ? 'bg-gradient-to-r from-blue-600 to-indigo-600 text-white shadow-lg shadow-blue-600/30'
            : 'text-[#537292] dark:text-slate-400 hover:text-[#0F1D2B] dark:hover:text-white hover:bg-white/70 dark:hover:bg-[#1E3349]/70'}"
        >
          <User class="h-4.5 w-4.5" />
          <span>{$t.auth.tabUser}</span>
        </button>

        <!-- Tab 2: Staff / Employee -->
        <button
          type="button"
          role="tab"
          id="login-tab-staff"
          aria-selected={activeTab === 'staff'}
          aria-controls="login-panel-staff"
          onclick={() => (activeTab = 'staff')}
          class="h-11 rounded-xl font-bold text-sm flex items-center justify-center space-x-2 transition-all duration-200 cursor-pointer active:scale-[0.98] {activeTab === 'staff'
            ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-lg shadow-indigo-600/30'
            : 'text-[#537292] dark:text-slate-400 hover:text-[#0F1D2B] dark:hover:text-white hover:bg-white/70 dark:hover:bg-[#1E3349]/70'}"
        >
          <ShieldCheck class="h-4.5 w-4.5" />
          <span>{$t.auth.tabStaff}</span>
        </button>
      </div>

      <!-- ===== TAB PANEL: USER (Customer & Subscriber) ===== -->
      {#if activeTab === 'user'}
        <div
          id="login-panel-user"
          role="tabpanel"
          aria-labelledby="login-tab-user"
          class="p-6 sm:p-8"
        >
          <!-- Panel header -->
          <div class="flex items-center space-x-3 pb-5 border-b border-[#CCE4F7] dark:border-[#253D56]/80">
            <div class="h-10 w-10 rounded-xl bg-gradient-to-tr from-sky-500 to-blue-600 text-white flex items-center justify-center shadow-lg shadow-sky-500/20 shrink-0">
              <User class="h-5 w-5" />
            </div>
            <div class="min-w-0">
              <div class="flex items-center space-x-2 flex-wrap gap-y-1">
                <h2 class="text-base font-extrabold text-[#0F1D2B] dark:text-white truncate">
                  {$t.auth.userPortalTitle}
                </h2>
                <span class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-300 border border-sky-200 dark:border-sky-800">
                  {$t.auth.userPortalBadge}
                </span>
              </div>
              <p class="text-xs text-[#537292] dark:text-slate-400 mt-0.5">
                {$t.auth.userPortalSubtitle}
              </p>
            </div>
          </div>

          <!-- Error Alert if any -->
          {#if errorMessage}
            <div class="mt-4 p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs flex items-center space-x-2">
              <span class="h-2 w-2 rounded-full bg-rose-500 animate-ping shrink-0"></span>
              <span>{errorMessage}</span>
            </div>
          {/if}

          <!-- BUOC 1: Nhap Ma tai khoan de nhan OTP -->
          {#if otpStep === 'account'}
            <form onsubmit={handleLoginSubmit} class="mt-5 space-y-4">
              <div>
                <label for="accountId" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                  {$t.auth.accountIdLabel}
                </label>
                <div class="relative">
                  <KeyRound class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                  <input
                    id="accountId"
                    type="text"
                    autocomplete="off"
                    maxlength="17"
                    required
                    placeholder={$t.auth.accountIdPlaceholder}
                    value={accountId}
                    oninput={handleAccountIdInput}
                    class="w-full pl-10 pr-4 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition font-mono tracking-wider"
                  />
                </div>
                <p class="mt-1.5 text-[11px] text-[#537292] dark:text-slate-400">
                  {$language === 'vi'
                    ? 'Mã tài khoản được gửi về email của bạn sau khi kế toán xác minh thanh toán.'
                    : 'Your Account ID is emailed to you once Accounts verifies your payment.'}
                </p>
              </div>

              <div class="pt-1">
                <button type="submit" disabled={otpSubmitting} class="w-full py-2.5 px-4 rounded-xl font-bold text-sm bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white transition shadow-lg shadow-blue-600/30 flex items-center justify-center space-x-2 cursor-pointer active:scale-[0.99] disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100">
                  <Mail class="h-4 w-4" />
                  <span>
                    {otpSubmitting
                      ? ($language === 'vi' ? 'Đang gửi mã...' : 'Sending code...')
                      : ($language === 'vi' ? 'Gửi mã OTP về email' : 'Email me a login code')}
                  </span>
                </button>
              </div>
            </form>

          <!-- BUOC 2: Nhap ma OTP 6 chu so -->
          {:else}
            <form onsubmit={handleOtpSubmit} class="mt-5 space-y-4">
              <div class="p-3 rounded-xl bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-900 text-[11px] text-[#305070] dark:text-sky-200 flex items-start space-x-2">
                <Mail class="h-4 w-4 mt-px shrink-0 text-sky-600 dark:text-sky-400" />
                <span>
                  {$language === 'vi' ? 'Mã OTP đã gửi tới' : 'OTP sent to'}
                  <strong class="font-mono">{otpMaskedEmail}</strong>.
                  {#if otpValidSeconds > 0}
                    {$language === 'vi' ? 'Mã hết hạn sau' : 'Expires in'}
                    <strong>{formatCountdown(otpValidSeconds)}</strong>.
                  {:else}
                    <strong class="text-rose-600 dark:text-rose-400">
                      {$language === 'vi' ? 'Mã đã hết hạn, vui lòng gửi lại.' : 'Code expired, please resend.'}
                    </strong>
                  {/if}
                </span>
              </div>

              <div>
                <label for="otpCode" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                  {$language === 'vi' ? 'Mã OTP (6 chữ số)' : 'OTP code (6 digits)'}
                </label>
                <input
                  id="otpCode"
                  type="text"
                  inputmode="numeric"
                  autocomplete="one-time-code"
                  maxlength="6"
                  required
                  placeholder="000000"
                  value={otpCode}
                  oninput={handleOtpInput}
                  class="w-full px-4 py-3 text-center text-2xl font-black bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#B9D4EC] dark:placeholder-slate-700 transition font-mono tracking-[0.5em]"
                />
              </div>

              <div class="pt-1">
                <button
                  type="submit"
                  disabled={otpSubmitting || otpCode.length !== 6}
                  class="w-full py-2.5 px-4 rounded-xl font-bold text-sm bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white transition shadow-lg shadow-blue-600/30 flex items-center justify-center space-x-2 cursor-pointer active:scale-[0.99] disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100"
                >
                  <ShieldCheck class="h-4 w-4" />
                  <span>
                    {otpSubmitting
                      ? ($language === 'vi' ? 'Đang xác thực...' : 'Verifying...')
                      : $t.auth.loginButton}
                  </span>
                </button>
              </div>

              <div class="flex items-center justify-between text-[11px]">
                <button
                  type="button"
                  onclick={backToAccountStep}
                  class="flex items-center space-x-1 text-[#537292] dark:text-slate-400 hover:text-sky-700 dark:hover:text-sky-300 font-semibold transition cursor-pointer"
                >
                  <ArrowLeft class="h-3 w-3" />
                  <span>{$language === 'vi' ? 'Đổi mã tài khoản' : 'Change Account ID'}</span>
                </button>

                <button
                  type="button"
                  onclick={sendOtp}
                  disabled={otpSubmitting || otpResendSeconds > 0}
                  class="font-bold text-sky-700 dark:text-sky-300 hover:underline disabled:text-slate-400 disabled:dark:text-slate-600 disabled:no-underline disabled:cursor-not-allowed cursor-pointer"
                >
                  {otpResendSeconds > 0
                    ? ($language === 'vi' ? 'Gửi lại sau ' + otpResendSeconds + 's' : 'Resend in ' + otpResendSeconds + 's')
                    : ($language === 'vi' ? 'Gửi lại mã' : 'Resend code')}
                </button>
              </div>
            </form>
          {/if}

          <!-- Demo subscriber shortcut -->
          {#if demoAccountId}
            {@const demoConn = $connections.find((c) => c.accountId === demoAccountId)}
            <!-- Nút demo thứ hai phải là tài khoản KHÁC nút đầu. Trước đây dò cứng
                 'B064-000000000002' — trùng đúng $connections[0] nên hai nút hiện y hệt nhau. -->
            {@const indConn = $connections.find((c) => c.accountId !== demoAccountId)}
            <div class="mt-4 p-3 rounded-xl bg-purple-50/70 dark:bg-purple-950/30 border border-purple-200 dark:border-purple-800/50 space-y-2">
              <div class="text-[11px] font-bold text-purple-700 dark:text-purple-300 flex items-center space-x-1.5">
                <Sparkles class="h-3 w-3 text-purple-500" />
                <span>{$t.auth.demoAccountTitle}</span>
              </div>
              <p class="text-[10px] text-purple-600/80 dark:text-purple-400/80 leading-snug">
                {$language === 'vi'
                  ? 'Lối tắt trình diễn — vào thẳng, bỏ qua bước OTP vì email của dữ liệu mẫu không có thật.'
                  : 'Demo shortcut — signs in directly and skips OTP, because the seeded emails are not real.'}
              </p>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                <button
                  type="button"
                  onclick={() => { accountId = demoAccountId; signIn(demoAccountId); }}
                  class="p-2 text-left rounded-lg bg-white dark:bg-slate-900 border border-purple-200 dark:border-purple-800 hover:border-purple-400 transition cursor-pointer"
                >
                  <div class="font-mono text-[11px] font-bold text-purple-700 dark:text-purple-300">{demoAccountId}</div>
                  <div class="text-[11px] text-slate-600 dark:text-slate-400 truncate font-semibold">{demoConn?.customerName || 'Highline Consulting'}</div>
                </button>
                {#if indConn}
                  <button
                    type="button"
                    onclick={() => { accountId = indConn.accountId; signIn(indConn.accountId); }}
                    class="p-2 text-left rounded-lg bg-white dark:bg-slate-900 border border-purple-200 dark:border-purple-800 hover:border-purple-400 transition cursor-pointer"
                  >
                    <div class="font-mono text-[11px] font-bold text-purple-700 dark:text-purple-300">{indConn.accountId}</div>
                    <div class="text-[11px] text-slate-600 dark:text-slate-400 truncate font-semibold">{indConn.customerName}</div>
                  </button>
                {/if}
              </div>
            </div>
          {/if}

          <!-- Order Lookup Section -->
          <div class="mt-6 pt-5 border-t border-[#CCE4F7] dark:border-[#253D56]/80">
            <div class="flex items-center space-x-1.5 text-xs font-semibold text-[#537292] dark:text-slate-400 mb-1">
              <Search class="h-3.5 w-3.5 text-sky-600 dark:text-sky-400" />
              <span>{$t.auth.trackTitle}</span>
            </div>
            <p class="text-[11px] text-[#537292] dark:text-slate-400 mb-2.5">{$t.auth.trackHint}</p>

            <form onsubmit={handleTrackSubmit} class="flex items-center gap-2">
              <input
                type="text"
                autocomplete="off"
                placeholder={$t.auth.trackPlaceholder}
                bind:value={orderQuery}
                class="flex-1 min-w-0 px-3.5 py-2 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition font-mono"
              />
              <button
                type="submit"
                class="shrink-0 px-3.5 py-2 rounded-xl text-xs font-bold bg-[#EDF6FF] dark:bg-[#1E3349] hover:bg-[#D8ECFC] dark:hover:bg-[#253E58] text-[#1B2D40] dark:text-white border border-[#CCE4F7] dark:border-[#253D56] transition active:scale-95 cursor-pointer"
              >
                {$t.auth.trackButton}
              </button>
            </form>

            {#if trackError}
              <p class="mt-2 text-[11px] text-rose-600 dark:text-rose-400">{trackError}</p>
            {:else if trackedOrder}
              <div class="mt-3 p-3 rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] text-xs">
                <div class="flex items-center justify-between gap-2">
                  <span class="font-mono font-bold text-[#0F1D2B] dark:text-white">{trackedOrder.id}</span>
                  <span class="text-[11px] font-semibold text-sky-700 dark:text-sky-300">
                    {$t.auth.trackStatusLabel}: {trackedOrder.status}
                  </span>
                </div>

                {#if trackedOrder.assignedAccountId}
                  <p class="mt-2 text-[11px] text-emerald-600 dark:text-emerald-400 font-semibold">
                    {$t.auth.trackAccountIssued}
                  </p>
                  <div class="mt-1 flex items-center justify-between gap-2">
                    <span class="font-mono font-bold text-[#0F1D2B] dark:text-white break-all">
                      {trackedOrder.assignedAccountId}
                    </span>
                    <button
                      type="button"
                      onclick={() => { accountId = normalizeAccountId(trackedOrder!.assignedAccountId!); sendOtp(); }}
                      class="shrink-0 px-3 py-1.5 rounded-lg text-[11px] font-bold bg-sky-600 hover:bg-sky-700 text-white transition active:scale-95 cursor-pointer"
                    >
                      {$t.common.login}
                    </button>
                  </div>
                {:else if trackedOrder.status === 'Not Feasible'}
                  <p class="mt-2 text-[11px] text-rose-600 dark:text-rose-400">{$t.auth.trackRejected}</p>
                {:else}
                  <p class="mt-2 text-[11px] text-amber-600 dark:text-amber-400">{$t.auth.trackPending}</p>
                {/if}
              </div>
            {/if}
          </div>

          <!-- Purchase flow footer -->
          <div class="mt-6 pt-4 border-t border-[#CCE4F7] dark:border-[#253D56]/80 text-center text-xs text-[#537292] dark:text-slate-400 flex items-center justify-center space-x-1.5">
            <span>{$t.auth.noAccount}</span>
            <button
              type="button"
              onclick={() => navigate('/register')}
              class="font-bold text-sky-600 dark:text-sky-400 hover:underline inline-flex items-center space-x-1 cursor-pointer"
            >
              <ShoppingCart class="h-3.5 w-3.5" />
              <span>{$t.auth.registerLink}</span>
              <ArrowRight class="h-3 w-3" />
            </button>
          </div>
        </div>

      <!-- ===== TAB PANEL: STAFF (Internal Roles) ===== -->
      {:else}
        <div
          id="login-panel-staff"
          role="tabpanel"
          aria-labelledby="login-tab-staff"
          class="p-6 sm:p-8"
        >
          <!-- Panel header -->
          <div class="flex items-center space-x-3 pb-5 border-b border-[#CCE4F7] dark:border-[#253D56]/80">
            <div class="h-10 w-10 rounded-xl bg-gradient-to-tr from-indigo-600 to-purple-600 text-white flex items-center justify-center shadow-lg shadow-indigo-500/20 shrink-0">
              <ShieldCheck class="h-5 w-5" />
            </div>
            <div class="min-w-0">
              <div class="flex items-center space-x-2 flex-wrap gap-y-1">
                <h2 class="text-base font-extrabold text-[#0F1D2B] dark:text-white truncate">
                  {$t.auth.staffPortalTitle}
                </h2>
                <span class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-indigo-100 dark:bg-indigo-950/60 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800">
                  {$t.auth.staffPortalBadge}
                </span>
              </div>
              <p class="text-xs text-[#537292] dark:text-slate-400 mt-0.5">
                {$t.auth.staffPortalSubtitle}
              </p>
            </div>
          </div>

          <!-- Staff credential login form -->
          {#if staffError}
            <div class="mt-4 p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs flex items-center space-x-2">
              <span class="h-2 w-2 rounded-full bg-rose-500 animate-ping shrink-0"></span>
              <span>{staffError}</span>
            </div>
          {/if}

          <form onsubmit={handleStaffSubmit} class="mt-5 space-y-4">
            <div>
              <label for="staffEmail" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                {$t.auth.staffEmailLabel}
              </label>
              <div class="relative">
                <Mail class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                <input
                  id="staffEmail"
                  type="email"
                  autocomplete="username"
                  required
                  pattern=".*@.*"
                  title={$language === 'vi' ? 'Email phải chứa ký tự @' : 'Email must contain @ symbol'}
                  placeholder={$t.auth.staffEmailPlaceholder}
                  bind:value={staffEmail}
                  class="w-full pl-10 pr-4 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-purple-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition"
                />
              </div>
            </div>

            <div>
              <label for="staffPassword" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                {$t.auth.staffPasswordLabel}
              </label>
              <div class="relative">
                <Lock class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                <input
                  id="staffPassword"
                  type="password"
                  autocomplete="current-password"
                  required
                  placeholder="••••••••"
                  bind:value={staffPassword}
                  class="w-full pl-10 pr-4 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-purple-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition"
                />
              </div>
            </div>

            <!-- Submit Button -->
            <div class="pt-1">
              <button
                type="submit"
                disabled={staffSubmitting}
                class="w-full py-2.5 px-4 rounded-xl font-bold text-sm bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white transition shadow-lg shadow-indigo-600/30 flex items-center justify-center space-x-2 cursor-pointer active:scale-[0.99] disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100"
              >
                {#if staffSubmitting}
                  <span class="h-4 w-4 rounded-full border-2 border-white/40 border-t-white animate-spin"></span>
                  <span>{$t.auth.staffLoginButton}</span>
                {:else}
                  <span>{$t.auth.staffLoginButton}</span>
                  <ArrowRight class="h-4 w-4" />
                {/if}
              </button>
            </div>
          </form>

          <!-- ===== KHUNG DEMO TÀI KHOẢN NHÂN VIÊN, ADMIN & TECH ===== -->
          <div class="mt-6 pt-5 border-t border-[#CCE4F7] dark:border-[#253D56]/80 space-y-3">

            <!-- Cards Grid (2x2 on desktop, 1x4 on mobile) -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-2.5 pt-1">
              {#each staffDemoList as staff}
                <div class="p-3 rounded-xl border transition-all duration-200 {staff.theme.cardBg} {staff.theme.cardBorder} flex flex-col justify-between space-y-2.5 relative group shadow-sm hover:shadow-md">
                  <!-- Header: Icon, Badge, Route -->
                  <div>
                    <div class="flex items-center justify-between gap-1.5 mb-1.5">
                      <div class="flex items-center space-x-2 min-w-0">
                        <div class="h-7 w-7 rounded-lg bg-gradient-to-tr {staff.theme.iconGradient} text-white flex items-center justify-center shrink-0 shadow-sm">
                          {#if staff.role === 'admin'}
                            <Crown class="h-4 w-4" />
                          {:else if staff.role === 'retail'}
                            <Store class="h-4 w-4" />
                          {:else if staff.role === 'technical'}
                            <Wrench class="h-4 w-4" />
                          {:else}
                            <Receipt class="h-4 w-4" />
                          {/if}
                        </div>
                        <span class="text-[10px] font-extrabold uppercase px-1.5 py-0.5 rounded-md {staff.theme.badgeBg} {staff.theme.badgeText} truncate">
                          {staff.badgeName}
                        </span>
                      </div>
                      <span class="font-mono text-[9px] font-bold text-slate-500 dark:text-slate-400">
                        {staff.route}
                      </span>
                    </div>

                    <!-- Name and Title -->
                    <div class="text-xs font-bold text-[#0F1D2B] dark:text-white truncate">
                      {staff.name}
                    </div>
                    <div class="text-[10px] text-slate-600 dark:text-slate-300 font-semibold truncate">
                      {staff.title} • {staff.department}
                    </div>

                    <!-- Email with copy -->
                    <div class="mt-1.5 flex items-center justify-between p-1 px-2 rounded-lg bg-white/80 dark:bg-slate-900/80 border border-slate-200/80 dark:border-slate-800/80 text-[10px] font-mono">
                      <span class="text-slate-700 dark:text-slate-300 truncate">{staff.email}</span>
                      <button
                        type="button"
                        onclick={() => copyCredential(staff.email, 'email')}
                        class="ml-1 p-0.5 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 cursor-pointer shrink-0"
                        title="Sao chép email"
                      >
                        {#if copyingKey === staff.email}
                          <Check class="h-3 w-3 text-emerald-600" />
                        {:else}
                          <Copy class="h-3 w-3" />
                        {/if}
                      </button>
                    </div>

                    <!-- Role scope description -->
                    <div class="text-[10px] text-[#537292] dark:text-slate-400 mt-1 line-clamp-2 leading-tight">
                      {staff.scope}
                    </div>
                  </div>

                  <!-- Action Buttons: Fill Form vs Quick Login -->
                  <div class="grid grid-cols-2 gap-1.5 pt-1 border-t border-slate-200/60 dark:border-slate-800/60">
                    <button
                      type="button"
                      onclick={() => fillStaffForm(staff.email, staff.name)}
                      class="py-1.5 px-2 rounded-lg text-[11px] font-semibold bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 transition active:scale-95 cursor-pointer text-center"
                    >
                      {$t.auth.demoStaffFillBtn}
                    </button>
                    <button
                      type="button"
                      disabled={staffSubmitting}
                      onclick={() => handleQuickStaffLogin(staff.email, staff.role, staff.name)}
                      class="py-1.5 px-2 rounded-lg text-[11px] font-bold {staff.theme.btnBg} transition active:scale-95 cursor-pointer flex items-center justify-center space-x-1 text-center disabled:opacity-60"
                    >
                      {#if activeQuickRole === staff.role}
                        <span class="h-3 w-3 rounded-full border-2 border-white/40 border-t-white animate-spin"></span>
                      {:else}
                        <Zap class="h-3 w-3 fill-current" />
                        <span>{$t.auth.demoStaffQuickLoginBtn}</span>
                      {/if}
                    </button>
                  </div>
                </div>
              {/each}
            </div>
          </div>

          <!-- Security & Audit Note Footer -->
          <div class="mt-6 pt-4 border-t border-[#CCE4F7] dark:border-[#253D56]/80 flex items-center justify-between text-[11px] text-[#537292] dark:text-slate-400">
            <div class="flex items-center space-x-1.5">
              <Lock class="h-3.5 w-3.5 text-emerald-600 dark:text-emerald-400 shrink-0" />
              <span class="truncate">{$t.auth.staffSecurityNotice}</span>
            </div>
            <span class="font-mono text-[10px] text-indigo-600 dark:text-indigo-400 font-bold shrink-0 ml-2">v2.4 Enterprise</span>
          </div>
        </div>
      {/if}
    </div>
  </div>
</div>
