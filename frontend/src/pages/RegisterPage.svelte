<script lang="ts">
  // Purchase flow: the customer fills in their own details, picks a plan and a
  // branch, and the order issues the Account ID they will sign in with.
  import { authStore } from '../context/AuthContext';
  import { nexusStore } from '../context/NexusContext';
  import { themeStore } from '../context/ThemeContext';
  import { languageStore } from '../context/LanguageContext';
  import LanguageToggle from '../components/layout/LanguageToggle.svelte';
  import { dashboardPathForRole, navigate, queryParam } from '../lib/router';
  import { getBulkDiscountPercent, isDepositWaived, DEPOSIT_WAIVER_MIN_CONNECTIONS } from '../context/NexusContext';
  import type { Order } from '../types/nexus';
  import {
    Layers, Mail, User, Phone, ArrowRight, ArrowLeft, Sun, Moon, Sparkles,
    CheckCircle2, Wifi, HardDrive, ShieldCheck, Receipt, MapPin, Building2,
    Star, IdCard, Store, Package, FileText, Copy, ShoppingCart, Lock, X, Search, Navigation
  } from 'lucide-svelte';
  import { registerCustomerApi, ApiError } from '../lib/api';
  import { isValidCccd, isValidEmail, isValidPhone, isValidPassport, isValidDriverLicense } from '../lib/validation';
  import { toast } from 'svelte-sonner';

  const { currentUser } = authStore;
  const { plans, retailShops, placeOrder } = nexusStore;
  const { theme, toggleTheme } = themeStore;
  const { t, language } = languageStore;

  // Form State
  let customerType = $state<'personal' | 'business'>('personal');
  let name = $state('');
  let email = $state('');
  let phone = $state('');
  let province = $state('');
  let district = $state('');
  let ward = $state('');
  let specificAddress = $state('');
  const address = $derived([specificAddress, ward, district, province].filter(Boolean).join(', '));

  let provincesList = $state<any[]>([]);
  let districtsList = $state<any[]>([]);
  let wardsList = $state<any[]>([]);

  $effect(() => {
    fetch('https://esgoo.net/api-tinhthanh/1/0.htm')
      .then(r => r.json())
      .then(d => {
        if(d.error === 0) provincesList = d.data;
      })
      .catch(e => console.error(e));
  });

  $effect(() => {
    if (province) {
      const p = provincesList.find(x => x.name === province || x.full_name === province);
      if (p) {
        fetch(`https://esgoo.net/api-tinhthanh/2/${p.id}.htm`)
          .then(r => r.json())
          .then(d => {
            if(d.error === 0) districtsList = d.data;
          })
          .catch(e => console.error(e));
      } else {
        districtsList = [];
      }
    } else {
      districtsList = [];
      district = '';
      ward = '';
    }
  });

  $effect(() => {
    if (district) {
      const d = districtsList.find(x => x.name === district || x.full_name === district);
      if (d) {
        fetch(`https://esgoo.net/api-tinhthanh/3/${d.id}.htm`)
          .then(r => r.json())
          .then(d => {
            if(d.error === 0) wardsList = d.data;
          })
          .catch(e => console.error(e));
      } else {
        wardsList = [];
      }
    } else {
      wardsList = [];
      ward = '';
    }
  });

  let idProofType = $state<Order['idProofType']>('National ID Card');
  let idProofNumber = $state('');
  let isSubmitting = $state(false);
  let showMapModal = $state(false);
  let mapSearchQuery = $state('');

  // Plan pre-selected from the landing page (e.g. '#/register?plan=plan-bb-01')
  let planId = $state(queryParam('plan') ?? '');
  let shopCode = $state('');
  let agreeTerms = $state(true);
  let errorMessage = $state('');

  // Bulk / corporate scheme (default 1 connection for personal, adjustable for corporate)
  let bulkConnectionsCount = $state(1);
  // Dial-Up: does the customer already hold a Nexus landline?
  let hasExistingLandline = $state(false);
  let existingLandlineAccountId = $state('');

  // Số kết nối thực sự gửi đi: khách cá nhân luôn 1, chỉ doanh nghiệp mới được đặt nhiều.
  const effectiveBulkCount = $derived(
    customerType === 'business' ? Math.max(1, bulkConnectionsCount || 1) : 1
  );
  const bulkDiscountPercent = $derived(getBulkDiscountPercent(effectiveBulkCount));
  // Trên 50 kết nối: miễn hoàn toàn tiền cọc (lưu ở cột Orders.DepositWaived).
  const depositWaived = $derived(isDepositWaived(effectiveBulkCount));

  // Set once the order goes through — switches the page to the receipt view.
  let placedOrder = $state<Order | null>(null);

  const activePlans = $derived($plans.filter((p) => p.status === 'Active'));
  const selectedPlan = $derived(activePlans.find((p) => p.id === planId) ?? null);
  // Tiền cọc thực thu: 0 khi đơn được miễn cọc (> 50 kết nối).
  const effectiveDeposit = $derived(depositWaived ? 0 : (selectedPlan?.securityDeposit ?? 0));
  const selectedShop = $derived($retailShops.find((s) => s.shopCode === shopCode) ?? null);

  // Already signed in? Send them to their own dashboard instead.
  $effect(() => {
    if ($currentUser && !placedOrder) {
      navigate(dashboardPathForRole($currentUser.role));
    }
  });

  const handlePurchaseSubmit = async (e: SubmitEvent) => {
    e.preventDefault();
    errorMessage = '';

    if (isSubmitting) return;

    if (!name.trim() || !phone.trim() || !address.trim()) {
      errorMessage = $t.auth.fillAll;
      return;
    }

    if (!isValidEmail(email)) {
      errorMessage = $t.auth.invalidEmail;
      return;
    }

    if (!isValidPhone(phone)) {
      errorMessage = $language === 'vi'
        ? 'Số điện thoại phải gồm đúng 10 chữ số.'
        : 'Phone number must contain exactly 10 digits.';
      return;
    }

    if (idProofType === 'National ID Card' && !isValidCccd(idProofNumber)) {
      errorMessage = $language === 'vi'
        ? 'Số CCCD phải gồm đúng 12 chữ số.'
        : 'National ID number must contain exactly 12 digits.';
      return;
    }

    if (idProofType === 'Passport' && !isValidPassport(idProofNumber)) {
      errorMessage = $language === 'vi'
        ? 'Số Passport không hợp lệ (7-9 ký tự chữ và số).'
        : 'Invalid Passport number (7-9 alphanumeric characters).';
      return;
    }

    if (idProofType === "Driver's License" && !isValidDriverLicense(idProofNumber)) {
      errorMessage = $language === 'vi'
        ? 'Số Bằng lái xe phải gồm đúng 12 chữ số.'
        : 'Driver\'s License must contain exactly 12 digits.';
      return;
    }

    if (!idProofNumber.trim()) {
      errorMessage = $t.registrationPage.idProofRequired;
      return;
    }

    if (!selectedPlan) {
      errorMessage = $t.registrationPage.planRequired;
      return;
    }

    if (!selectedShop) {
      errorMessage = $t.registrationPage.branchRequired;
      return;
    }

    if (!agreeTerms) {
      errorMessage = $language === 'vi'
        ? 'Vui lòng đồng ý với Điều khoản dịch vụ để tiếp tục.'
        : 'Please accept the Terms of Service to proceed.';
      return;
    }

    isSubmitting = true;
    const normalizedEmail = email.trim().toLowerCase();

    try {
      // Bước 1 — tạo tài khoản đăng nhập cho khách lần đầu. Backend tự hash mật khẩu và
      // lưu hồ sơ Customer. Xoá JWT còn sót của phiên trước để không lẫn sang khách khác.
      localStorage.removeItem('nexus_jwt_token');

      let isReturningCustomer = false;
      try {
        const regRes = await registerCustomerApi({
          name: name.trim(),
          email: normalizedEmail,
          password: Math.random().toString(36).slice(-8) + 'A1@',
          phoneNumber: phone.trim().replace(/\D/g, ''),
        });
        if (regRes.token) {
          localStorage.setItem('nexus_jwt_token', regRes.token);
        }
      } catch (err) {
        // 409 = email (hoặc số điện thoại) đã có tài khoản: đây là khách cũ mua thêm.
        // Một người được mua nhiều lần và dùng lại email cũ — đơn mới được backend gắn
        // vào đúng hồ sơ khách có sẵn theo email, không tạo tài khoản thứ hai.
        if (err instanceof ApiError && err.status === 409) {
          isReturningCustomer = true;
        } else {
          throw err;
        }
      }

      // Bước 2 — ghi đơn qua /api/public/orders (không cần JWT), nên khách cũ dùng lại
      // email vẫn đặt được. Lỗi thật (thiếu dữ liệu, mất mạng...) sẽ ném ra và hiện lên form.
      placedOrder = await placeOrder({
        customerName: name.trim(),
        customerPhone: phone.trim(),
        customerEmail: normalizedEmail,
        installationAddress: address.trim(),
        idProofType,
        idProofNumber: idProofNumber.trim(),
        connectionType: selectedPlan.type,
        planId: selectedPlan.id,
        planName: selectedPlan.name,
        retailOutletCode: selectedShop.shopCode,
        retailEmployeeName: 'Online Self-Service',
        bulkConnectionsCount: effectiveBulkCount,
        ...(selectedPlan.type === 'Dial-Up' && hasExistingLandline && existingLandlineAccountId.trim()
          ? { existingLandlineAccountId: existingLandlineAccountId.trim() }
          : {}),
      }, { selfService: true });

      if (isReturningCustomer) {
        toast.info($language === 'vi'
          ? `Email ${normalizedEmail} đã có tài khoản — đơn mới đã được thêm vào hồ sơ của bạn.`
          : `${normalizedEmail} already has an account — this order was added to your profile.`);
      }
      toast.success($t.auth.registerSuccess);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (err) {
      errorMessage = err instanceof ApiError && err.message !== 'network'
        ? err.message
        : ($language === 'vi'
            ? 'Không kết nối được máy chủ. Vui lòng thử lại.'
            : 'Could not reach the server. Please try again.');
    } finally {
      isSubmitting = false;
    }
  };

  const handleCopyOrderId = async () => {
    if (!placedOrder) return;
    try {
      await navigator.clipboard.writeText(placedOrder.id);
      toast.success($t.registrationPage.copiedId);
    } catch {
      toast.error($language === 'vi' ? 'Không sao chép được, vui lòng chép tay.' : 'Copy failed, please note it down manually.');
    }
  };

  // No Account ID yet: it is issued when technical confirms feasibility, so the
  // customer follows the order by its code until then.
  const handleTrackOrder = () => {
    if (!placedOrder) return;
    navigate(`/login?order=${placedOrder.id}`);
  };
</script>

<div class="h-full w-full bg-[#E0F1FF] dark:bg-[#1B2D40] text-[#1B2D40] dark:text-[#E0F1FF] font-sans antialiased transition-colors duration-300 flex flex-col relative overflow-y-scroll">
  <!-- Background ambient lighting -->
  <div class="fixed inset-0 pointer-events-none bg-[radial-gradient(circle_at_top,_var(--tw-gradient-stops))] from-blue-900/20 via-slate-950 to-slate-950 opacity-0 dark:opacity-100 transition-opacity"></div>
  <div class="fixed inset-0 pointer-events-none bg-[radial-gradient(circle_at_top,_var(--tw-gradient-stops))] from-blue-200/40 via-[#E0F1FF] to-[#E0F1FF] opacity-100 dark:opacity-0 transition-opacity"></div>
  <div class="fixed inset-0 pointer-events-none bg-[linear-gradient(to_right,#0284c70d_1px,transparent_1px),linear-gradient(to_bottom,#0284c70d_1px,transparent_1px)] dark:bg-[linear-gradient(to_right,#1e293b0a_1px,transparent_1px),linear-gradient(to_bottom,#1e293b0a_1px,transparent_1px)] bg-[size:3rem_3rem]"></div>

  <!-- TOP NAVIGATION BAR -->
  <header class="sticky top-0 z-40 w-full backdrop-blur-md bg-white/85 dark:bg-[#152434]/85 border-b border-[#CCE4F7] dark:border-[#253D56] transition-colors duration-300">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
      <!-- Brand Logo & Back to Home -->
      <div class="flex items-center space-x-3">
        <button
          type="button"
          onclick={() => navigate('/')}
          class="h-9 px-3 rounded-full bg-[#EDF6FF] dark:bg-[#1E3349] hover:bg-[#DCEEFE] dark:hover:bg-[#253E58] text-[#1B2D40] dark:text-[#E0F1FF] border border-[#CCE4F7] dark:border-[#253D56] flex items-center space-x-1.5 transition text-xs font-semibold"
          title={$t.common.home}
        >
          <ArrowLeft class="h-3.5 w-3.5" />
          <span class="hidden sm:inline">{$t.common.home}</span>
        </button>

        <button
          type="button"
          onclick={() => navigate('/')}
          class="flex items-center space-x-2.5 cursor-pointer group"
        >
          <div class="h-9 w-9 rounded-xl bg-gradient-to-tr from-sky-600 to-blue-600 flex items-center justify-center text-white shadow-md shadow-sky-500/20 group-hover:scale-105 transition-transform">
            <Layers class="h-5 w-5" />
          </div>
          <div class="font-extrabold text-base text-sky-950 dark:text-white tracking-tight items-center gap-1.5 hidden sm:flex">
            <span>{$t.common.brandName}</span>
            <span class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-sky-500/15 text-sky-700 dark:text-sky-300 tracking-wider">
              {$t.common.brandTag}
            </span>
          </div>
        </button>
      </div>

      <!-- Right Action Controls -->
      <div class="flex items-center space-x-2.5">
        <LanguageToggle />
        <button
          type="button"
          onclick={toggleTheme}
          class="h-9 w-9 rounded-full bg-[#EDF6FF] dark:bg-[#1E3349] hover:bg-[#DCEEFE] dark:hover:bg-[#253E58] text-[#1B2D40] dark:text-[#E0F1FF] border border-[#CCE4F7] dark:border-[#253D56] flex items-center justify-center transition shadow-xs group"
          title={$theme === 'dark' ? $t.common.themeLight : $t.common.themeDark}
        >
          {#if $theme === 'dark'}
            <Sun class="h-4 w-4 text-amber-400 group-hover:rotate-45 transition-transform" />
          {:else}
            <Moon class="h-4 w-4 text-sky-700 group-hover:-rotate-12 transition-transform" />
          {/if}
        </button>
        <button
          type="button"
          onclick={() => navigate('/login')}
          class="px-3.5 py-1.5 rounded-lg text-xs font-bold text-sky-700 dark:text-sky-300 hover:bg-sky-100/50 dark:hover:bg-slate-800 transition"
        >
          {$t.common.login}
        </button>
      </div>
    </div>
  </header>

  <main class="flex-1 max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-8 sm:py-12 relative z-10">
    {#if placedOrder}
      <!-- ============ PURCHASE RECEIPT: THE ACCOUNT ID IS THE CREDENTIAL ============ -->
      <div class="max-w-2xl mx-auto">
        <div class="bg-white/95 dark:bg-[#152434]/95 border border-[#CCE4F7] dark:border-[#253D56] rounded-2xl p-6 sm:p-8 shadow-xl backdrop-blur-md">
          <div class="text-center">
            <div class="inline-flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 mb-3">
              <CheckCircle2 class="h-8 w-8" />
            </div>
            <h1 class="text-2xl font-extrabold tracking-tight text-[#0F1D2B] dark:text-white">
              {$t.registrationPage.successTitle}
            </h1>
            <p class="text-sm text-[#537292] dark:text-[#8DB0D4] mt-2">
              {$t.registrationPage.successDesc}
            </p>
          </div>

          <!-- Order code: the Account ID comes later, at feasibility confirmation -->
          <div class="mt-6 p-5 rounded-2xl bg-gradient-to-br from-sky-500/10 via-blue-500/5 to-transparent border border-sky-500/25">
            <div class="flex items-center space-x-2 text-xs font-bold uppercase tracking-wider text-sky-700 dark:text-sky-300">
              <FileText class="h-4 w-4" />
              <span>{$t.registrationPage.orderIdIssued}</span>
            </div>
            <div class="mt-2 flex items-center justify-between gap-3">
              <span class="font-mono text-xl sm:text-2xl font-black tracking-wider text-[#0F1D2B] dark:text-white break-all">
                {placedOrder.id}
              </span>
              <button
                type="button"
                onclick={handleCopyOrderId}
                class="shrink-0 px-3 py-1.5 rounded-lg text-xs font-bold bg-sky-600 hover:bg-sky-700 text-white shadow-xs transition active:scale-95 flex items-center space-x-1.5"
              >
                <Copy class="h-3.5 w-3.5" />
                <span>{$t.registrationPage.copyId}</span>
              </button>
            </div>
            <p class="mt-2 text-[11px] text-amber-700 dark:text-amber-300 font-medium">
              {$t.registrationPage.saveOrderNotice}
            </p>
          </div>

          <!-- Order recap -->
          <div class="mt-5 grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
            <div class="p-3 rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56]">
              <div class="text-[#537292] dark:text-[#8DB0D4]">{$t.registrationPage.summaryTitle}</div>
              <div class="font-bold text-[#0F1D2B] dark:text-white mt-0.5">
                {placedOrder.connectionType} · {placedOrder.customerName}
              </div>
            </div>
            <div class="p-3 rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56]">
              <div class="text-[#537292] dark:text-[#8DB0D4]">{$t.registrationPage.planLabel}</div>
              <div class="font-bold text-[#0F1D2B] dark:text-white mt-0.5">{placedOrder.planName}</div>
            </div>
            <div class="p-3 rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56]">
              <div class="text-[#537292] dark:text-[#8DB0D4]">{$t.registrationPage.branchAssigned}</div>
              <div class="font-bold text-[#0F1D2B] dark:text-white mt-0.5">
                {selectedShop?.name ?? placedOrder.retailOutletCode} ({placedOrder.retailOutletCode})
              </div>
            </div>
            <div class="p-3 rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56]">
              <div class="text-[#537292] dark:text-[#8DB0D4]">{$t.registrationPage.retailAgentAssigned}</div>
              <div class="font-bold text-[#0F1D2B] dark:text-white mt-0.5">
                {placedOrder.retailEmployeeName}
                {#if placedOrder.assignedBranchName}
                  <span class="font-medium text-[#537292] dark:text-[#8DB0D4]">· {placedOrder.assignedBranchName}</span>
                {/if}
              </div>
            </div>
            <div class="p-3 rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56]">
              <div class="text-[#537292] dark:text-[#8DB0D4]">{$t.registrationPage.addressLabel}</div>
              <div class="font-bold text-[#0F1D2B] dark:text-white mt-0.5">{placedOrder.installationAddress}</div>
            </div>
            {#if placedOrder.depositWaived}
              <!-- Đơn trên 50 kết nối — CSDL đã ghi Orders.DepositWaived = 1 -->
              <div class="sm:col-span-2 p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/30">
                <div class="text-emerald-700 dark:text-emerald-300 font-extrabold">
                  {$language === 'vi' ? 'Tiền cọc: MIỄN PHÍ' : 'Security deposit: FREE'}
                </div>
                <div class="text-[#537292] dark:text-[#8DB0D4] mt-0.5">
                  {$language === 'vi'
                    ? `Đơn ${placedOrder.bulkConnectionsCount} kết nối (trên ${DEPOSIT_WAIVER_MIN_CONNECTIONS}) được miễn toàn bộ tiền cọc.`
                    : `${placedOrder.bulkConnectionsCount} connections (over ${DEPOSIT_WAIVER_MIN_CONNECTIONS}) — the full deposit is waived.`}
                </div>
              </div>
            {/if}
          </div>

          <!-- Stage 1 notice: the order goes to the branch's retail desk first -->
          <div class="mt-3 p-3 rounded-xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/50 text-[11px] text-amber-800 dark:text-amber-300 flex items-start gap-2">
            <ShieldCheck class="h-3.5 w-3.5 shrink-0 mt-0.5" />
            <span>{$t.registrationPage.retailStageHint}</span>
          </div>

          <!-- Next steps -->
          <div class="mt-5 p-4 rounded-xl bg-white dark:bg-[#1E3349] border border-[#CCE4F7] dark:border-[#253D56]">
            <h3 class="text-xs font-bold uppercase tracking-wider text-[#305070] dark:text-slate-300 mb-2">
              {$t.registrationPage.nextStepsTitle}
            </h3>
            <ol class="space-y-1.5 text-xs text-[#537292] dark:text-[#8DB0D4] list-decimal list-inside">
              <li>{$t.registrationPage.nextStep1}</li>
              <li>{$t.registrationPage.nextStep2}</li>
              <li>{$t.registrationPage.nextStep3}</li>
            </ol>
          </div>

          <button
            type="button"
            onclick={handleTrackOrder}
            class="mt-6 w-full py-3 px-4 rounded-xl font-bold text-sm bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white transition shadow-lg shadow-sky-600/30 flex items-center justify-center space-x-2 active:scale-95"
          >
            <span>{$t.registrationPage.trackOrderBtn}</span>
            <ArrowRight class="h-4 w-4" />
          </button>
        </div>
      </div>
    {:else}
      <!-- ============ PURCHASE FORM ============ -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        <!-- LEFT COLUMN: VALUE PROPOSITION -->
        <div class="lg:col-span-5 space-y-6">
          <div>
            <span class="inline-flex items-center space-x-1.5 px-3 py-1 rounded-full text-xs font-bold bg-sky-500/10 dark:bg-sky-400/10 text-sky-700 dark:text-sky-300 border border-sky-500/20">
              <span class="h-1.5 w-1.5 rounded-full bg-sky-500 animate-ping"></span>
              <span>NEXUS TELECOM ONBOARDING</span>
            </span>
            <h1 class="text-2xl sm:text-3xl lg:text-4xl font-extrabold tracking-tight text-[#0F1D2B] dark:text-white mt-3">
              {$t.registrationPage.title}
            </h1>
            <p class="text-sm text-[#537292] dark:text-[#8DB0D4] mt-2 leading-relaxed">
              {$t.auth.registerDescription}
            </p>
          </div>

          <!-- 4 Benefits Grid -->
          <div class="space-y-3 pt-2">
            <div class="p-4 rounded-xl bg-white/80 dark:bg-[#152434]/80 border border-[#CCE4F7] dark:border-[#253D56] shadow-xs flex items-start space-x-3.5 backdrop-blur-sm transition">
              <div class="h-9 w-9 rounded-lg bg-sky-500/10 dark:bg-sky-400/10 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0 mt-0.5">
                <Wifi class="h-5 w-5" />
              </div>
              <div>
                <h4 class="text-sm font-bold text-[#0F1D2B] dark:text-white">{$t.registrationPage.benefit1Title}</h4>
                <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-0.5">{$t.registrationPage.benefit1Desc}</p>
              </div>
            </div>

            <div class="p-4 rounded-xl bg-white/80 dark:bg-[#152434]/80 border border-[#CCE4F7] dark:border-[#253D56] shadow-xs flex items-start space-x-3.5 backdrop-blur-sm transition">
              <div class="h-9 w-9 rounded-lg bg-emerald-500/10 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0 mt-0.5">
                <HardDrive class="h-5 w-5" />
              </div>
              <div>
                <h4 class="text-sm font-bold text-[#0F1D2B] dark:text-white">{$t.registrationPage.benefit2Title}</h4>
                <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-0.5">{$t.registrationPage.benefit2Desc}</p>
              </div>
            </div>

            <div class="p-4 rounded-xl bg-white/80 dark:bg-[#152434]/80 border border-[#CCE4F7] dark:border-[#253D56] shadow-xs flex items-start space-x-3.5 backdrop-blur-sm transition">
              <div class="h-9 w-9 rounded-lg bg-amber-500/10 dark:bg-amber-400/10 text-amber-600 dark:text-amber-400 flex items-center justify-center shrink-0 mt-0.5">
                <ShieldCheck class="h-5 w-5" />
              </div>
              <div>
                <h4 class="text-sm font-bold text-[#0F1D2B] dark:text-white">{$t.registrationPage.benefit3Title}</h4>
                <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-0.5">{$t.registrationPage.benefit3Desc}</p>
              </div>
            </div>

            <div class="p-4 rounded-xl bg-white/80 dark:bg-[#152434]/80 border border-[#CCE4F7] dark:border-[#253D56] shadow-xs flex items-start space-x-3.5 backdrop-blur-sm transition">
              <div class="h-9 w-9 rounded-lg bg-indigo-500/10 dark:bg-indigo-400/10 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shrink-0 mt-0.5">
                <Receipt class="h-5 w-5" />
              </div>
              <div>
                <h4 class="text-sm font-bold text-[#0F1D2B] dark:text-white">{$t.registrationPage.benefit4Title}</h4>
                <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-0.5">{$t.registrationPage.benefit4Desc}</p>
              </div>
            </div>
          </div>

          <!-- Social Proof Box -->
          <div class="p-4 rounded-xl bg-gradient-to-br from-sky-500/10 via-blue-500/5 to-transparent border border-sky-500/20 flex items-center space-x-3">
            <div class="flex -space-x-1.5 overflow-hidden">
              <div class="h-8 w-8 rounded-full ring-2 ring-white dark:ring-[#152434] bg-sky-600 text-white font-bold text-xs flex items-center justify-center">A</div>
              <div class="h-8 w-8 rounded-full ring-2 ring-white dark:ring-[#152434] bg-emerald-600 text-white font-bold text-xs flex items-center justify-center">T</div>
              <div class="h-8 w-8 rounded-full ring-2 ring-white dark:ring-[#152434] bg-indigo-600 text-white font-bold text-xs flex items-center justify-center">M</div>
            </div>
            <div class="text-xs">
              <div class="flex items-center space-x-1 text-amber-500">
                <Star class="h-3 w-3 fill-current" />
                <Star class="h-3 w-3 fill-current" />
                <Star class="h-3 w-3 fill-current" />
                <Star class="h-3 w-3 fill-current" />
                <Star class="h-3 w-3 fill-current" />
                <span class="font-bold text-slate-800 dark:text-slate-200 ml-1">4.9/5.0</span>
              </div>
              <p class="text-[#537292] dark:text-[#8DB0D4] mt-0.5">
                {$language === 'vi' ? 'Được tin dùng bởi hơn 10.000+ hộ gia đình và doanh nghiệp.' : 'Trusted by 10,000+ households and enterprises.'}
              </p>
            </div>
          </div>
        </div>

        <!-- RIGHT COLUMN: PURCHASE FORM CARD -->
        <div class="lg:col-span-7">
          <div class="bg-white/95 dark:bg-[#152434]/95 border border-[#CCE4F7] dark:border-[#253D56] rounded-2xl p-6 sm:p-8 shadow-xl backdrop-blur-md transition-colors duration-300">
            <!-- Customer Type Selector -->
            <div class="flex rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] p-1 border border-[#CCE4F7] dark:border-[#253D56] mb-6">
              <button
                type="button"
                onclick={() => (customerType = 'personal')}
                class="flex-1 py-2 rounded-lg text-xs font-bold transition flex items-center justify-center space-x-2 {customerType === 'personal'
                  ? 'bg-white dark:bg-[#1E3349] text-sky-600 dark:text-white shadow-xs'
                  : 'text-[#537292] dark:text-slate-400 hover:text-[#0F1D2B] dark:hover:text-white'}"
              >
                <User class="h-4 w-4" />
                <span>{$t.registrationPage.personalType}</span>
              </button>
              <button
                type="button"
                onclick={() => (customerType = 'business')}
                class="flex-1 py-2 rounded-lg text-xs font-bold transition flex items-center justify-center space-x-2 {customerType === 'business'
                  ? 'bg-white dark:bg-[#1E3349] text-sky-600 dark:text-white shadow-xs'
                  : 'text-[#537292] dark:text-slate-400 hover:text-[#0F1D2B] dark:hover:text-white'}"
              >
                <Building2 class="h-4 w-4" />
                <span>{$t.registrationPage.businessType}</span>
              </button>
            </div>

            <!-- Error Message Alert -->
            {#if errorMessage}
              <div class="mb-5 p-3 rounded-lg bg-rose-50 dark:bg-rose-950/50 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-xs flex items-center space-x-2">
                <span class="h-2 w-2 rounded-full bg-rose-500 animate-ping shrink-0"></span>
                <span>{errorMessage}</span>
              </div>
            {/if}

            <form onsubmit={handlePurchaseSubmit} class="space-y-4">
              <!-- SECTION 1: PERSONAL DETAILS -->
              <h3 class="text-xs font-bold uppercase tracking-wider text-sky-700 dark:text-sky-300 flex items-center space-x-1.5">
                <User class="h-3.5 w-3.5" />
                <span>{$t.registrationPage.personalSection}</span>
              </h3>

              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <!-- Full Name -->
                <div>
                  <label for="name" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                    {customerType === 'business' ? ($language === 'vi' ? 'Tên Doanh Nghiệp / Đại Diện' : 'Company / Representative Name') : $t.auth.nameLabel}
                    <span class="text-rose-500">*</span>
                  </label>
                  <div class="relative">
                    <User class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                    <input
                      id="name"
                      type="text"
                      required
                      placeholder={customerType === 'business' ? ($language === 'vi' ? 'VD: Công ty TNHH Nexus (hoặc Tên Cơ sở)' : 'Ex: Nexus Co. (or Facility Name)') : 'Nguyen Van A'}
                      bind:value={name}
                      class="w-full pl-10 pr-4 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition"
                    />
                  </div>
                </div>

                <!-- Email -->
                <div>
                  <label for="email" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                    {$t.auth.emailLabel} <span class="text-rose-500">*</span>
                  </label>
                  <div class="relative">
                    <Mail class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                    <input
                      id="email"
                      type="email"
                      required
                      pattern=".*@.*"
                      title={$language === 'vi' ? 'Email phải chứa ký tự @' : 'Email must contain @ symbol'}
                      placeholder="subscriber@nexus.telecom"
                      bind:value={email}
                      class="w-full pl-10 pr-4 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition"
                    />
                  </div>
                </div>
              </div>



              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <!-- Phone -->
                <div>
                  <label for="phone" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                    {$t.auth.phoneLabel} <span class="text-rose-500">*</span>
                  </label>
                  <div class="relative">
                    <Phone class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                    <input
                      id="phone"
                      type="tel"
                      required
                      inputmode="numeric"
                      
                      maxlength="10"
                      title={$language === 'vi' ? 'Số điện thoại phải gồm đúng 10 chữ số' : 'Phone number must contain exactly 10 digits'}
                      placeholder="0912345678"
                      bind:value={phone}
                      class="w-full pl-10 pr-4 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition"
                    />
                  </div>
                </div>

                <!-- ID Proof Type -->
                <div>
                  <label for="idProofType" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                    {$t.registrationPage.idProofTypeLabel} <span class="text-rose-500">*</span>
                  </label>
                  <select
                    id="idProofType"
                    bind:value={idProofType}
                    class="w-full px-3.5 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white transition cursor-pointer"
                  >
                    <option value="National ID Card">National ID Card</option>
                    <option value="Passport">Passport</option>
                    <option value="Driver's License">Driver's License</option>
                  </select>
                </div>
              </div>

              <!-- ID Proof Number -->
              <div>
                <label for="idProofNumber" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                  {$t.registrationPage.idProofNumberLabel} <span class="text-rose-500">*</span>
                </label>
                <div class="relative">
                  <IdCard class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                  <input
                    id="idProofNumber"
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
                    class="w-full pl-10 pr-4 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition font-mono"
                  />
                </div>
              </div>

              <!-- Installation Address -->
              <div>
                <label class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5 flex justify-between items-end">
                  <span>{$t.registrationPage.addressLabel} <span class="text-rose-500">*</span></span>
                </label>
                <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-3">
                  <!-- Province -->
                  <div class="relative">
                    <select
                      bind:value={province}
                      class="w-full pl-3 pr-8 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white transition cursor-pointer appearance-none"
                    >
                      <option value="">{$language === 'vi' ? 'Tỉnh / Thành phố' : 'Province / City'}</option>
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
                      class="w-full pl-3 pr-8 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white transition cursor-pointer appearance-none disabled:opacity-50"
                    >
                      <option value="">{$language === 'vi' ? 'Quận / Huyện' : 'District'}</option>
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
                      class="w-full pl-3 pr-8 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white transition cursor-pointer appearance-none disabled:opacity-50"
                    >
                      <option value="">{$language === 'vi' ? 'Phường / Xã' : 'Ward'}</option>
                      {#each wardsList as w}
                        <option value={w.name}>{w.name}</option>
                      {/each}
                    </select>
                  </div>
                </div>
                <div class="relative">
                  <MapPin class="absolute left-3.5 top-3 h-4 w-4 text-[#7899B8] dark:text-slate-500" />
                  <input
                    id="address"
                    type="text"
                    required
                    bind:value={specificAddress}
                    placeholder={$language === 'vi' ? 'Địa chỉ cụ thể (số nhà, tên đường)' : 'Specific address (house no, street)'}
                    class="w-full pl-10 pr-12 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white placeholder-[#7899B8] dark:placeholder-slate-500 transition"
                  />
                  <button type="button" onclick={() => { showMapModal = true; mapSearchQuery = address; }} class="absolute right-1.5 top-1.5 px-2.5 py-1.5 bg-sky-600 dark:bg-sky-500 hover:bg-sky-700 dark:hover:bg-[#1C2C3D] text-white rounded-lg transition cursor-pointer flex items-center space-x-1 shadow-sm text-[11px] font-semibold" title={$language === 'vi' ? 'Chọn trên bản đồ' : 'Pick on map'}>
                    <Navigation class="h-3 w-3" />
                    <span>{$language === 'vi' ? 'Bản đồ' : 'Map'}</span>
                  </button>
                </div>
              </div>

              <!-- SECTION 2: PLAN & BRANCH -->
              <h3 class="pt-2 text-xs font-bold uppercase tracking-wider text-sky-700 dark:text-sky-300 flex items-center space-x-1.5">
                <Package class="h-3.5 w-3.5" />
                <span>{$t.registrationPage.planSection}</span>
              </h3>

              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <!-- Plan -->
                <div>
                  <label for="planId" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                    {$t.registrationPage.planLabel} <span class="text-rose-500">*</span>
                  </label>
                  <select
                    id="planId"
                    bind:value={planId}
                    class="w-full px-3.5 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white transition cursor-pointer"
                  >
                    <option value="" disabled>{$language === 'vi' ? 'Vui lòng chọn gói cước' : 'Please select a plan'}</option>
                    {#each activePlans as plan (plan.id)}
                      <option value={plan.id}>{plan.type} — {plan.name} (${plan.monthlyRental}{plan.billingCycle ? ' / ' + plan.billingCycle : $t.plans.perMonth})</option>
                    {/each}
                  </select>
                </div>

                <!-- Branch chosen by the customer -->
                <div>
                  <label for="shopCode" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                    {$t.registrationPage.branchLabel} <span class="text-rose-500">*</span>
                  </label>
                  <select
                    id="shopCode"
                    bind:value={shopCode}
                    class="w-full px-3.5 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 dark:focus:ring-blue-500 text-[#0F1D2B] dark:text-white transition cursor-pointer"
                  >
                    <option value="" disabled>{$language === 'vi' ? 'Vui lòng chọn chi nhánh (cơ sở)' : 'Please select a branch (facility)'}</option>
                    {#each $retailShops as shop (shop.id)}
                      <option value={shop.shopCode}>{shop.shopCode} — {shop.name} ({shop.city})</option>
                    {/each}
                  </select>
                </div>
              </div>

              <!-- Bulk / corporate scheme + Dial-Up existing landline -->
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label for="bulkCount" class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                    {$language === 'vi' ? 'Số lượng kết nối (mặc định 50 kết nối / 50 router thiết bị)' : 'Number of connections (default 50 connections / 50 routers)'}
                  </label>
                  <input
                    id="bulkCount"
                    type="number"
                    min="1"
                    bind:value={bulkConnectionsCount}
                    class="w-full px-3.5 py-2.5 text-sm bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 text-[#0F1D2B] dark:text-white transition"
                  />
                  <p class="mt-1 text-[11px] text-[#537292] dark:text-slate-400">
                    {$language === 'vi'
                      ? '1 connection = 1 router thiết bị · Mặc định 50 kết nối (chiết khấu 75% cước & tiền cọc) · Trên 50: miễn phí 100%.'
                      : '1 connection = 1 router device · Default 50 lines (75% off rental & deposit) · Over 50: 100% free.'}
                  </p>
                </div>

                {#if selectedPlan?.type === 'Dial-Up'}
                  <div>
                    <span class="block text-xs font-semibold text-[#305070] dark:text-slate-300 uppercase tracking-wider mb-1.5">
                      {$language === 'vi' ? 'Đường dây cố định Nexus' : 'Existing Nexus landline'}
                    </span>
                    <label class="flex items-center space-x-2 text-xs text-[#537292] dark:text-slate-300 mb-1.5">
                      <input type="checkbox" bind:checked={hasExistingLandline} class="h-4 w-4 rounded border-[#CCE4F7] text-sky-600 focus:ring-sky-500" />
                      <span>{$language === 'vi' ? 'Khách hàng đã có landline Nexus (chỉ cần kiểm tra khả thi phần internet)' : 'Customer already has a Nexus landline (only the internet leg needs a feasibility check)'}</span>
                    </label>
                    {#if hasExistingLandline}
                      <input
                        type="text"
                        placeholder={$language === 'vi' ? 'Mã tài khoản landline (nếu có)' : 'Landline Account ID (optional)'}
                        bind:value={existingLandlineAccountId}
                        class="w-full px-3.5 py-2 text-sm font-mono bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 text-[#0F1D2B] dark:text-white transition"
                      />
                    {/if}
                  </div>
                {/if}
              </div>

              <!-- Selected branch detail -->
              {#if selectedShop}
                <div class="p-3 rounded-xl bg-[#EDF6FF] dark:bg-[#101C29] border border-[#CCE4F7] dark:border-[#253D56] text-xs text-[#3A5B7E] dark:text-[#8DB0D4] flex items-start space-x-2.5">
                  <Store class="h-4 w-4 text-sky-600 dark:text-sky-400 shrink-0 mt-0.5" />
                  <div>
                    <div class="font-bold text-[#0F1D2B] dark:text-white">{selectedShop.name}</div>
                    <div class="mt-0.5">{selectedShop.address}</div>
                    <div class="mt-0.5">{selectedShop.operatingHours} · {selectedShop.phone}</div>
                  </div>
                </div>
              {/if}

              <!-- Order summary -->
              {#if selectedPlan}
                <div class="p-4 rounded-xl bg-gradient-to-br from-sky-500/10 via-blue-500/5 to-transparent border border-sky-500/20 text-xs">
                  <div class="font-bold uppercase tracking-wider text-sky-700 dark:text-sky-300 mb-2">
                    {$t.registrationPage.summaryTitle}
                  </div>
                  <div class="flex items-center justify-between py-1">
                    <span class="text-[#537292] dark:text-[#8DB0D4]">{selectedPlan.name}</span>
                    <span class="font-mono text-[#0F1D2B] dark:text-white">{selectedPlan.speedOrBandwidth}</span>
                  </div>
                  <div class="flex items-center justify-between py-1">
                    <span class="text-[#537292] dark:text-[#8DB0D4]">{$t.registrationPage.monthlyLabel}</span>
                    <span class="font-bold text-[#0F1D2B] dark:text-white">${selectedPlan.monthlyRental}</span>
                  </div>
                  <div class="flex items-center justify-between py-1">
                    <span class="text-[#537292] dark:text-[#8DB0D4]">{$t.registrationPage.depositLabel}</span>
                    {#if depositWaived}
                      <!-- Trên 50 kết nối: miễn tiền cọc — gạch giá gốc, hiện nhãn MIỄN PHÍ -->
                      <span class="flex items-center gap-1.5">
                        <span class="line-through text-[#8FAAC2]">${selectedPlan.securityDeposit}</span>
                        <span class="px-1.5 py-0.5 rounded-md text-[10px] font-extrabold uppercase bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-700">
                          {$language === 'vi' ? 'Miễn phí' : 'Free'}
                        </span>
                      </span>
                    {:else}
                      <span class="font-bold text-[#0F1D2B] dark:text-white">${selectedPlan.securityDeposit}</span>
                    {/if}
                  </div>
                  {#if depositWaived}
                    <div class="my-1 px-2.5 py-1.5 rounded-lg bg-emerald-500/10 border border-emerald-500/30 text-[11px] font-semibold text-emerald-700 dark:text-emerald-300">
                      {$language === 'vi'
                        ? `Đơn ${effectiveBulkCount} kết nối (trên ${DEPOSIT_WAIVER_MIN_CONNECTIONS}) được MIỄN PHÍ tiền cọc.`
                        : `${effectiveBulkCount} connections (over ${DEPOSIT_WAIVER_MIN_CONNECTIONS}) — security deposit is FREE.`}
                    </div>
                  {/if}
                  {#if bulkDiscountPercent > 0}
                    <div class="flex items-center justify-between py-1 text-emerald-600 dark:text-emerald-400">
                      <span>{$language === 'vi' ? `Chiết khấu gói ${effectiveBulkCount} kết nối` : `Bulk scheme (${effectiveBulkCount} connections)`} −{bulkDiscountPercent}%</span>
                      <span class="font-bold">
                        −${(((selectedPlan.monthlyRental + effectiveDeposit) * bulkDiscountPercent) / 100).toFixed(2)}
                      </span>
                    </div>
                  {/if}
                  <div class="flex items-center justify-between pt-2 mt-1 border-t border-sky-500/20">
                    <span class="font-bold text-[#305070] dark:text-slate-200">{$t.registrationPage.dueTodayLabel}</span>
                    <span class="font-black text-base text-sky-700 dark:text-sky-300">
                      ${((selectedPlan.monthlyRental + effectiveDeposit) * (1 - bulkDiscountPercent / 100)).toFixed(2)}
                    </span>
                  </div>
                </div>
              {/if}

              <!-- Terms Agreement Checkbox -->
              <div class="pt-1">
                <label class="flex items-start space-x-2.5 cursor-pointer">
                  <input
                    type="checkbox"
                    bind:checked={agreeTerms}
                    class="mt-0.5 h-4 w-4 rounded border-[#CCE4F7] dark:border-[#253D56] text-sky-600 focus:ring-sky-500"
                  />
                  <span class="text-xs text-[#537292] dark:text-slate-400">
                    {$t.registrationPage.agreeTerms}
                  </span>
                </label>
              </div>

              <!-- Primary Submit Button -->
              <div class="pt-3">
                <button
                  type="submit"
                  disabled={isSubmitting}
                  class="w-full py-3 px-4 rounded-xl font-bold text-sm bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white transition shadow-lg shadow-sky-600/30 flex items-center justify-center space-x-2 active:scale-95 disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100"
                >
                  <ShoppingCart class="h-4 w-4" />
                  <span>
                    {isSubmitting
                      ? ($language === 'vi' ? 'Đang gửi đăng ký...' : 'Submitting...')
                      : $t.auth.registerButton}
                  </span>
                </button>
              </div>
            </form>

            <!-- Login Redirection Link -->
            <div class="mt-6 pt-5 border-t border-[#CCE4F7] dark:border-slate-800/80 text-center text-xs text-[#537292] dark:text-slate-400 flex items-center justify-center space-x-1.5">
              <span>{$t.auth.haveAccount}</span>
              <button
                type="button"
                onclick={() => navigate('/login')}
                class="font-bold text-sky-600 dark:text-sky-400 hover:underline inline-flex items-center space-x-1"
              >
                <span>{$t.auth.loginLink}</span>
                <ArrowRight class="h-3 w-3" />
              </button>
            </div>
          </div>

          <!-- Note at bottom -->
          <div class="mt-4 text-center text-xs text-[#537292] dark:text-slate-400">
            <span>{$t.auth.securityNotice}</span>
          </div>
        </div>
      </div>
    {/if}
  </main>
</div>

<!-- Map Modal -->
{#if showMapModal}
  <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
    <div class="bg-white dark:bg-[#0B141E] w-full max-w-2xl rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-200">
      <div class="p-4 border-b border-slate-200 dark:border-slate-800 flex justify-between items-center bg-slate-50 dark:bg-[#101C29]">
        <h3 class="font-bold text-slate-800 dark:text-white flex items-center space-x-2">
          <Navigation class="h-4 w-4 text-sky-500" />
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
            class="w-full pl-9 pr-4 py-2.5 text-sm bg-slate-100 dark:bg-[#152333] border border-slate-200 dark:border-slate-700 rounded-xl focus:outline-none focus:ring-2 focus:ring-sky-500 text-slate-800 dark:text-white transition"
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

      <div class="p-4 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-[#101C29] flex justify-end space-x-3">
        <button onclick={() => showMapModal = false} class="px-4 py-2 text-sm font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-xl transition cursor-pointer">
          {$language === 'vi' ? 'Huỷ' : 'Cancel'}
        </button>
        <button
          onclick={() => {
            province = '';
            district = '';
            ward = '';
            specificAddress = mapSearchQuery;
            showMapModal = false;
          }}
          disabled={!mapSearchQuery.trim()}
          class="px-5 py-2 text-sm font-bold text-white bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 rounded-xl shadow-md transition disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
        >
          {$language === 'vi' ? 'Xác nhận địa chỉ' : 'Confirm Address'}
        </button>
      </div>
    </div>
  </div>
{/if}
