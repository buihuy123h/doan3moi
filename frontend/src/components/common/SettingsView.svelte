<script lang="ts">
  import { languageStore } from "../../context/LanguageContext";
  import { themeStore } from "../../context/ThemeContext";
  import { nexusStore } from "../../context/NexusContext";
  import { authStore } from "../../context/AuthContext";
  import {
    Settings,
    Globe,
    Moon,
    Sun,
    Store,
    Bell,
    Check,
    MapPin,
    Phone,
    User,
    Shield,
    Mail,
    MessageSquare,
    Volume2,
    Save,
    Sparkles,
    Building2,
    Laptop,
    RefreshCw,
    ShieldCheck,
    Cpu,
    History,
    CheckCircle2,
    Download,
    ArrowUpCircle,
    Clock,
    ChevronDown,
    ChevronUp,
    Lock,
    ShieldAlert,
  } from "lucide-svelte";
  import { toast } from "svelte-sonner";

  const { language, setLanguage, t } = languageStore;
  const { theme, setTheme } = themeStore;
  const { retailShops } = nexusStore;
  const { currentUser } = authStore;

  let { showBranchSwitcher = true } = $props<{ showBranchSwitcher?: boolean }>();

  const isAdmin = $derived($currentUser?.role === "admin");

  // Language select animation state & change handler
  let isLangJustChanged = $state(false);

  const handleLanguageSelect = (e: Event) => {
    const nextLang = (e.currentTarget as HTMLSelectElement).value as "vi" | "en";
    if (nextLang !== $language) {
      setLanguage(nextLang);
      isLangJustChanged = true;
      setTimeout(() => {
        isLangJustChanged = false;
      }, 700);
      toast.success(
        nextLang === "vi"
          ? "Đã chuyển đổi sang Tiếng Việt"
          : "Switched to English",
      );
    }
  };

  // Active branch state (stored in localStorage)
  let selectedBranchCode = $state<string>(
    (typeof window !== "undefined" &&
      localStorage.getItem("nexus_active_branch")) ||
      $currentUser?.branchCode ||
      "SH-01",
  );

  // Notification Preferences State (stored in localStorage)
  interface NotificationPreferences {
    emailAlerts: boolean;
    smsAlerts: boolean;
    soundAlerts: boolean;
    orderUpdates: boolean;
    securityAlerts: boolean;
  }

  const defaultNotifications: NotificationPreferences = {
    emailAlerts: true,
    smsAlerts: true,
    soundAlerts: false,
    orderUpdates: true,
    securityAlerts: true,
  };

  const loadSavedNotifications = (): NotificationPreferences => {
    if (typeof window === "undefined") return defaultNotifications;
    try {
      const saved = localStorage.getItem("nexus_notification_settings");
      return saved
        ? { ...defaultNotifications, ...JSON.parse(saved) }
        : defaultNotifications;
    } catch {
      return defaultNotifications;
    }
  };

  let notifyPrefs = $state<NotificationPreferences>(loadSavedNotifications());

  // Handle Branch Change
  const handleSelectBranch = (code: string) => {
    if (!isAdmin) {
      toast.error(
        $language === "vi"
          ? "Chỉ Quản trị viên (Admin) mới có quyền chuyển đổi cơ sở."
          : "Only Administrators are allowed to switch branches.",
      );
      return;
    }

    selectedBranchCode = code;
    localStorage.setItem("nexus_active_branch", code);
    const shop = $retailShops.find((s) => s.shopCode === code);

    // Update currentUser department if signed in
    if ($currentUser) {
      authStore.currentUser.update((u) => {
        if (!u) return u;
        return {
          ...u,
          branchCode: code,
          department: shop ? `${shop.name} (${shop.shopCode})` : u.department,
        };
      });
    }

    toast.success(
      $language === "vi"
        ? `Đã chuyển sang chi nhánh: ${shop?.name || code}`
        : `Switched active branch to: ${shop?.name || code}`,
    );
  };

  // Handle Save Notifications
  const handleSaveNotifications = () => {
    localStorage.setItem(
      "nexus_notification_settings",
      JSON.stringify(notifyPrefs),
    );
    toast.success(
      $language === "vi"
        ? "Đã lưu cấu hình thông báo thành công!"
        : "Notification preferences saved successfully!",
    );
  };

  const activeShop = $derived(
    $retailShops.find((s) => s.shopCode === selectedBranchCode) ||
      $retailShops[0],
  );

  // Version & System Update State
  const currentVersion = "v2.4.1";
  const buildId = "Build 2026.09-STABLE";
  let lastCheckedVi = $state("Hôm nay lúc 09:15");
  let lastCheckedEn = $state("Today at 09:15 AM");
  let isCheckingUpdate = $state(false);
  let isUpdatingVersion = $state(false);
  let updateProgress = $state(0);
  let updateStatusMessageVi = $state("");
  let updateStatusMessageEn = $state("");
  let autoUpdatePatches = $state(
    typeof window !== "undefined"
      ? localStorage.getItem("nexus_auto_update") !== "false"
      : true,
  );
  let showChangelog = $state(true);

  // Handle Check Updates
  const handleCheckUpdate = () => {
    isCheckingUpdate = true;
    setTimeout(() => {
      isCheckingUpdate = false;
      const now = new Date();
      const timeStrVi = `Hôm nay lúc ${now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}`;
      const timeStrEn = `Today at ${now.toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" })}`;
      lastCheckedVi = timeStrVi;
      lastCheckedEn = timeStrEn;
      toast.success(
        $language === "vi"
          ? "Hệ thống đã cập nhật mới nhất (Nexus OS v2.4.1 - Không có bản cập nhật mới nào)!"
          : "System is up to date (Nexus OS v2.4.1 - No pending updates found)!",
      );
    }, 1200);
  };

  // Handle Perform Update / Re-sync
  const handlePerformUpdate = () => {
    if (isUpdatingVersion) return;
    isUpdatingVersion = true;
    updateProgress = 10;
    updateStatusMessageVi =
      "Đang kết nối kho máy chủ Nexus Enterprise Repository...";
    updateStatusMessageEn = "Connecting to Nexus Enterprise Repository...";

    setTimeout(() => {
      updateProgress = 35;
      updateStatusMessageVi =
        "Đang tải gói bản vá viễn thông, đa ngôn ngữ & thông báo...";
      updateStatusMessageEn =
        "Downloading telecom packages, i18n & interactive alerts...";
    }, 500);

    setTimeout(() => {
      updateProgress = 70;
      updateStatusMessageVi =
        "Đang xác thực chữ ký bảo mật SHA-256 & đồng bộ cơ sở dữ liệu...";
      updateStatusMessageEn =
        "Verifying SHA-256 security checksum & syncing database...";
    }, 1100);

    setTimeout(() => {
      updateProgress = 95;
      updateStatusMessageVi =
        "Đang biên dịch cấu hình NOC & tối ưu hóa hiệu năng giao diện...";
      updateStatusMessageEn =
        "Compiling NOC routes & optimizing frontend UI runtime...";
    }, 1600);

    setTimeout(() => {
      updateProgress = 100;
      updateStatusMessageVi = "Đã hoàn tất cập nhật phiên bản mới nhất!";
      updateStatusMessageEn = "Successfully updated to latest version!";
      setTimeout(() => {
        isUpdatingVersion = false;
        updateProgress = 0;
        toast.success(
          $language === "vi"
            ? "Cập nhật hệ thống Nexus OS v2.4.1 thành công! Đã đồng bộ tất cả phân hệ."
            : "Nexus OS v2.4.1 update completed! All modules successfully synchronized.",
        );
      }, 500);
    }, 2100);
  };

  const handleToggleAutoUpdate = (e: Event) => {
    const checked = (e.currentTarget as HTMLInputElement).checked;
    autoUpdatePatches = checked;
    localStorage.setItem("nexus_auto_update", String(checked));
    toast.success(
      $language === "vi"
        ? checked
          ? "Đã bật tự động cập nhật bản vá bảo mật"
          : "Đã tắt tự động cập nhật bản vá bảo mật"
        : checked
          ? "Enabled automatic security patch updates"
          : "Disabled automatic security patch updates",
    );
  };

  // Accordion / Collapsible section states
  let isAppearanceOpen = $state(false);
  let isBranchOpen = $state(false);
  let isNotificationsOpen = $state(false);
  let isVersionOpen = $state(false);

  const activeNotificationCount = $derived(
    Object.values(notifyPrefs).filter(Boolean).length,
  );

  const collapseAll = () => {
    isAppearanceOpen = false;
    isBranchOpen = false;
    isNotificationsOpen = false;
    isVersionOpen = false;
    toast.info(
      $language === "vi"
        ? "Đã thu gọn tất cả mục cài đặt"
        : "Collapsed all settings sections",
    );
  };

  const expandAll = () => {
    isAppearanceOpen = true;
    isBranchOpen = true;
    isNotificationsOpen = true;
    isVersionOpen = true;
    toast.info(
      $language === "vi"
        ? "Đã mở rộng tất cả mục cài đặt"
        : "Expanded all settings sections",
    );
  };
</script>

<div class="space-y-6 w-full pb-10">
  <!-- UNIFIED APPEARANCE & LANGUAGE CARD (2 ROWS WITH FAST TOGGLE SWITCHES) -->
  <div
    class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-300 overflow-hidden"
  >
    <!-- Card Header -->
    <div
      class="flex items-center justify-between p-5 sm:p-6 cursor-pointer select-none"
      role="button"
      tabindex="0"
      onclick={() => (isAppearanceOpen = !isAppearanceOpen)}
      onkeydown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          isAppearanceOpen = !isAppearanceOpen;
        }
      }}
    >
      <div class="flex items-center space-x-3">
        <div
          class="h-9 w-9 rounded-xl bg-sky-100 dark:bg-sky-900/40 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0"
        >
          <Sparkles class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-sm sm:text-base font-bold text-slate-900 dark:text-white">
            {$language === "vi" ? "Cài đặt giao diện & Ngôn ngữ" : "Interface & Language Settings"}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {$language === "vi"
              ? "Tùy chỉnh chế độ hiển thị sáng/tối và ngôn ngữ hệ thống"
              : "Customize system display theme and interface language"}
          </p>
        </div>
      </div>

      <!-- Right side: Status and Collapse Button -->
      <div class="flex items-center space-x-3">

        <button
          type="button"
          onclick={(e) => {
            e.stopPropagation();
            isAppearanceOpen = !isAppearanceOpen;
          }}
          class="h-8 w-8 rounded-xl border border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 flex items-center justify-center text-slate-500 dark:text-slate-400 transition cursor-pointer"
          title={isAppearanceOpen
            ? $language === "vi" ? "Thu gọn" : "Collapse"
            : $language === "vi" ? "Mở rộng" : "Expand"}
          aria-expanded={isAppearanceOpen}
        >
          {#if isAppearanceOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    <!-- 2 Rows Content (Collapsible) -->
    {#if isAppearanceOpen}
      <div class="border-t border-slate-100 dark:border-slate-800 divide-y divide-slate-100 dark:divide-slate-800">
        <!-- Row 1: Language Switcher -->
        <div class="px-5 sm:px-6 py-4 flex items-center justify-between hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition">
          <div class="flex items-center space-x-3">
            <div class="h-8 w-8 rounded-lg bg-sky-50 dark:bg-sky-950/50 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0">
              <Globe class="h-4 w-4" />
            </div>
            <div>
              <div class="text-sm font-bold text-slate-900 dark:text-white">
                {$language === "vi" ? "Ngôn ngữ hiển thị" : "Display Language"}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {$language === "vi"
                  ? "Chọn ngôn ngữ hiển thị giao diện (Tiếng Việt hoặc Tiếng Anh)"
                  : "Select interface display language (Vietnamese or English)"}
              </div>
            </div>
          </div>

          <!-- Animated Select Options for Language -->
          <div
            class="relative inline-flex items-center lang-select-wrapper group {isLangJustChanged ? 'just-changed' : ''}"
          >
            <!-- Animated Ambient Glow Aura -->
            <div
              class="absolute -inset-0.5 rounded-xl blur-xs opacity-25 group-hover:opacity-75 group-focus-within:opacity-100 transition-all duration-300 lang-aura pointer-events-none"
            ></div>

            <!-- Select Container -->
            <div
              class="relative flex items-center bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-700/80 shadow-xs group-hover:border-sky-400 dark:group-hover:border-sky-500 transition-all duration-200"
            >
              <!-- Animated Flag Icon with Wiggle -->
              <span
                class="pl-3 pr-1 text-base select-none lang-flag transition-transform duration-300 pointer-events-none"
              >
                {$language === "vi" ? "🇻🇳" : "🇬🇧"}
              </span>

              <!-- HTML Select with Options -->
              <select
                id="select-display-language"
                value={$language}
                onchange={handleLanguageSelect}
                class="lang-custom-select py-2 pl-1 pr-9 text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-100 bg-transparent border-0 cursor-pointer focus:outline-none focus:ring-0 appearance-none transition-colors duration-200"
                aria-label={$language === "vi" ? "Chọn ngôn ngữ" : "Select language"}
              >
                <option value="vi" class="bg-white dark:bg-slate-900 text-slate-900 dark:text-white font-medium py-1.5">
                  Tiếng Việt
                </option>
                <option value="en" class="bg-white dark:bg-slate-900 text-slate-900 dark:text-white font-medium py-1.5">
                  Tiếng Anh (English)
                </option>
              </select>

              <!-- Rotating Chevron Arrow Indicator -->
              <div
                class="absolute right-2.5 pointer-events-none text-slate-400 dark:text-slate-500 group-hover:text-sky-500 transition-transform duration-300 group-focus-within:rotate-180"
              >
                <ChevronDown class="h-4 w-4" />
              </div>
            </div>
          </div>
        </div>

        <!-- Row 2: Theme Switcher -->
        <div class="px-5 sm:px-6 py-4 flex items-center justify-between hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition">
          <div class="flex items-center space-x-3">
            <div class="h-8 w-8 rounded-lg bg-amber-50 dark:bg-amber-950/50 text-amber-600 dark:text-amber-400 flex items-center justify-center shrink-0">
              {#if $theme === "dark"}
                <Moon class="h-4 w-4" />
              {:else}
                <Sun class="h-4 w-4" />
              {/if}
            </div>
            <div>
              <div class="text-sm font-bold text-slate-900 dark:text-white">
                {$language === "vi" ? "Giao diện Tối" : "Dark Mode Theme"}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {$language === "vi"
                  ? "Bật chế độ màu tối giảm mỏi mắt ban đêm"
                  : "Enable dark theme to reduce eye strain in low-light"}
              </div>
            </div>
          </div>

          <!-- Quick Toggle Button / Switch for Theme -->
          <div class="flex items-center space-x-3">
            <span class="text-xs font-bold text-slate-700 dark:text-slate-300">
              {$theme === "dark"
                ? ($language === "vi" ? "Bật (Tối)" : "On (Dark)")
                : ($language === "vi" ? "Tắt (Sáng)" : "Off (Light)")}
            </span>
            <button
              type="button"
              role="switch"
              aria-checked={$theme === "dark"}
              onclick={() => {
                const nextTheme = $theme === "dark" ? "light" : "dark";
                setTheme(nextTheme);
                toast.success(
                  nextTheme === "dark"
                    ? ($language === "vi" ? "Đã bật giao diện Tối" : "Activated Dark Mode")
                    : ($language === "vi" ? "Đã bật giao diện Sáng" : "Activated Light Mode"),
                );
              }}
              class="relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none {$theme === 'dark' ? 'bg-amber-500 dark:bg-amber-500' : 'bg-slate-300 dark:bg-slate-700'}"
              title={$theme === "dark" ? ($language === "vi" ? "Chuyển sang Giao diện Sáng" : "Switch to Light Mode") : ($language === "vi" ? "Chuyển sang Giao diện Tối" : "Switch to Dark Mode")}
            >
              <span
                class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-md ring-0 transition duration-200 ease-in-out {$theme === 'dark' ? 'translate-x-5' : 'translate-x-0'}"
              ></span>
            </button>
          </div>
        </div>
      </div>
    {/if}
  </div>

  <!-- 3. BRANCH SWITCHER (CHUYỂN ĐỔI CHI NHÁNH) - Only Admin can switch, other roles have read-only view -->
  {#if showBranchSwitcher && $currentUser?.role !== "user"}
  <div
    class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-300 overflow-hidden {isBranchOpen
      ? 'p-6 sm:p-8 space-y-5'
      : 'p-4 sm:p-5'}"
  >
    <div
      class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 {isBranchOpen
        ? 'border-b border-slate-100 dark:border-slate-800 pb-4'
        : ''}"
    >
      <div
        class="flex items-center space-x-3 cursor-pointer flex-1"
        role="button"
        tabindex="0"
        onclick={() => (isBranchOpen = !isBranchOpen)}
        onkeydown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            isBranchOpen = !isBranchOpen;
          }
        }}
      >
        <div
          class="h-10 w-10 rounded-xl {isAdmin ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-600 dark:text-emerald-400' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400'} flex items-center justify-center shadow-xs shrink-0"
        >
          <Building2 class="h-5 w-5" />
        </div>
        <div>
          <div class="flex items-center space-x-2">
            <h3 class="text-base font-bold text-slate-900 dark:text-white">
              {#if isAdmin}
                {$language === "vi"
                  ? "Chuyển đổi chi nhánh làm việc"
                  : "Active Branch Location & Retail Outlet"}
              {:else}
                {$language === "vi"
                  ? "Chi nhánh làm việc (Cố định theo tài khoản)"
                  : "Current Working Branch (Fixed to Account)"}
              {/if}
            </h3>
            {#if !isAdmin}
              <span class="inline-flex items-center text-[10px] font-bold px-2 py-0.5 rounded-full bg-amber-100 dark:bg-amber-950/80 text-amber-700 dark:text-amber-400 border border-amber-300 dark:border-amber-800">
                <Lock class="h-2.5 w-2.5 mr-1" />
                {$language === "vi" ? "Chỉ Admin được đổi" : "Admin Only"}
              </span>
            {/if}
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {#if isAdmin}
              {$language === "vi"
                ? "Lựa chọn chi nhánh phụ trách để xử lý đơn hàng, kho thiết bị và thanh toán theo khu vực."
                : "Select your operational retail branch to filter local orders, technical stock, and regional subscribers."}
            {:else}
              {$language === "vi"
                ? "Chi nhánh làm việc được chỉ định cố định. Chỉ Quản trị viên (Admin) mới có quyền chuyển đổi cơ sở."
                : "Operational branch is fixed for your account. Only System Administrators can change branches."}
            {/if}
          </p>
        </div>
      </div>

      <!-- Quick Dropdown select & Toggle -->
      <div class="flex items-center space-x-2 shrink-0">
        {#if isAdmin}
          <span
            class="text-xs text-slate-500 dark:text-slate-400 whitespace-nowrap hidden sm:inline"
          >
            {$language === "vi" ? "Chọn nhanh:" : "Quick Select:"}
          </span>
          <select
            value={selectedBranchCode}
            onchange={(e) => handleSelectBranch(e.currentTarget.value)}
            class="text-xs font-semibold px-3 py-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-emerald-500 cursor-pointer"
          >
            {#each $retailShops as shop (shop.id)}
              <option value={shop.shopCode}>
                {shop.shopCode} — {shop.name} ({shop.city})
              </option>
            {/each}
          </select>
        {:else}
          <div
            class="text-xs font-semibold px-3 py-1.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-600 dark:text-slate-300 flex items-center space-x-1.5 select-none"
            title={$language === "vi" ? "Chỉ Quản trị viên (Admin) mới có quyền chuyển đổi cơ sở" : "Only Admin can switch branches"}
          >
            <Lock class="h-3 w-3 text-amber-500" />
            <span>{activeShop?.shopCode || selectedBranchCode} — {activeShop?.name || 'Chi nhánh'}</span>
          </div>
        {/if}
        <button
          type="button"
          onclick={() => (isBranchOpen = !isBranchOpen)}
          class="h-8 w-8 rounded-lg border border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 flex items-center justify-center text-slate-500 dark:text-slate-400 transition cursor-pointer"
          title={isBranchOpen
            ? $language === "vi"
              ? "Thu gọn"
              : "Collapse"
            : $language === "vi"
              ? "Mở rộng"
              : "Expand"}
        >
          {#if isBranchOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    {#if isBranchOpen}
      <!-- Notice banner for non-admin roles -->
      {#if !isAdmin}
        <div class="p-3.5 rounded-xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800/60 flex items-start space-x-3 text-xs text-amber-800 dark:text-amber-300">
          <ShieldAlert class="h-4 w-4 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />
          <div>
            <div class="font-bold">
              {$language === "vi" ? "Chỉ Quản trị viên (Admin) được phép chuyển đổi cơ sở" : "Branch Switching Restricted to Admin"}
            </div>
            <div class="mt-0.5 text-amber-700 dark:text-amber-400">
              {$language === "vi"
                ? "Tài khoản của bạn không có quyền thay đổi cơ sở hoạt động. Dưới đây là thông tin chi nhánh bạn đang trực thuộc."
                : "Your account does not have permission to switch operational branches. Below is your assigned branch information."}
            </div>
          </div>
        </div>
      {/if}

      <!-- Branch Cards Grid -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {#each $retailShops as shop (shop.id)}
          {@const isSelected = selectedBranchCode === shop.shopCode}
          <div
            class="rounded-xl border p-5 transition flex flex-col justify-between space-y-4 {isSelected
              ? 'border-emerald-500 bg-emerald-50/50 dark:bg-emerald-950/30 ring-2 ring-emerald-500/30'
              : 'border-slate-200 dark:border-slate-800 bg-slate-50/40 dark:bg-slate-950/40 hover:border-slate-300 dark:hover:border-slate-700'}"
          >
            <div class="space-y-2">
              <div class="flex items-start justify-between">
                <div>
                  <span
                    class="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded {isSelected
                      ? 'bg-emerald-600 text-white'
                      : 'bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300'}"
                  >
                    {shop.shopCode} • {$language === "vi"
                      ? "Mã vùng"
                      : "Area code"}: {shop.cityCode}
                  </span>
                  <h4
                    class="font-bold text-sm text-slate-900 dark:text-white mt-1.5"
                  >
                    {shop.name}
                  </h4>
                </div>
                {#if isSelected}
                  <span
                    class="inline-flex items-center text-[11px] font-bold text-emerald-700 dark:text-emerald-400 bg-emerald-100 dark:bg-emerald-950/80 px-2 py-0.5 rounded-full border border-emerald-300 dark:border-emerald-800"
                  >
                    <Check class="h-3 w-3 mr-1" />
                    {$language === "vi" ? "Đang hoạt động" : "Active"}
                  </span>
                {/if}
              </div>

              <div
                class="text-xs text-slate-600 dark:text-slate-400 space-y-1 pt-1"
              >
                <div class="flex items-start space-x-1.5">
                  <MapPin class="h-3.5 w-3.5 text-slate-400 shrink-0 mt-0.5" />
                  <span class="line-clamp-2">{shop.address}, {shop.city}</span>
                </div>
                <div class="flex items-center space-x-1.5">
                  <Phone class="h-3.5 w-3.5 text-slate-400 shrink-0" />
                  <span>{shop.phone}</span>
                </div>
                <div class="flex items-center space-x-1.5">
                  <User class="h-3.5 w-3.5 text-slate-400 shrink-0" />
                  <span
                    >{$language === "vi" ? "Quản lý:" : "Manager:"}
                    {shop.managerName}</span
                  >
                </div>
              </div>
            </div>

            {#if isAdmin}
              <button
                type="button"
                onclick={() => handleSelectBranch(shop.shopCode)}
                class="w-full py-2 px-3 rounded-lg text-xs font-semibold transition active:scale-95 flex items-center justify-center space-x-1.5 cursor-pointer {isSelected
                  ? 'bg-emerald-600 hover:bg-emerald-700 text-white shadow-xs'
                  : 'bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-700'}"
              >
                {#if isSelected}
                  <Check class="h-3.5 w-3.5" />
                  <span
                    >{$language === "vi"
                      ? "Chi nhánh hiện tại"
                      : "Current Active Branch"}</span
                  >
                {:else}
                  <span
                    >{$language === "vi"
                      ? "Chuyển sang chi nhánh này"
                      : "Switch to this Branch"}</span
                  >
                {/if}
              </button>
            {:else}
              <div
                class="w-full py-2 px-3 rounded-lg text-xs font-semibold flex items-center justify-center space-x-1.5 select-none {isSelected
                  ? 'bg-emerald-100 dark:bg-emerald-950/60 text-emerald-800 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800'
                  : 'bg-slate-100 dark:bg-slate-800/50 text-slate-400 dark:text-slate-500 border border-slate-200/60 dark:border-slate-800 cursor-not-allowed opacity-75'}"
              >
                {#if isSelected}
                  <Check class="h-3.5 w-3.5 text-emerald-600 dark:text-emerald-400" />
                  <span class="font-bold">
                    {$language === "vi" ? "Chi nhánh của bạn" : "Your Assigned Branch"}
                  </span>
                {:else}
                  <Lock class="h-3.5 w-3.5 text-slate-400" />
                  <span>
                    {$language === "vi" ? "Chỉ Admin được đổi" : "Admin Only"}
                  </span>
                {/if}
              </div>
            {/if}
          </div>
        {/each}
      </div>
    {/if}
  </div>
  {/if}

  <!-- 4. NOTIFICATION PREFERENCES (CÀI ĐẶT THÔNG BÁO) -->
  <div
    class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-300 overflow-hidden {isNotificationsOpen
      ? 'p-6 sm:p-8 space-y-6'
      : 'p-4 sm:p-5'}"
  >
    <div
      class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 {isNotificationsOpen
        ? 'border-b border-slate-100 dark:border-slate-800 pb-4'
        : ''}"
    >
      <div
        class="flex items-center space-x-3 cursor-pointer flex-1"
        role="button"
        tabindex="0"
        onclick={() => (isNotificationsOpen = !isNotificationsOpen)}
        onkeydown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            isNotificationsOpen = !isNotificationsOpen;
          }
        }}
      >
        <div
          class="h-10 w-10 rounded-xl bg-purple-100 dark:bg-purple-900/40 text-purple-600 dark:text-purple-400 flex items-center justify-center shadow-xs shrink-0"
        >
          <Bell class="h-5 w-5" />
        </div>
        <div>
          <div class="flex items-center space-x-2">
            <h3 class="text-base font-bold text-slate-900 dark:text-white">
              {$language === "vi"
                ? "Cài đặt thông báo & cảnh báo"
                : "Notification & Alert Preferences"}
            </h3>
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {$language === "vi"
              ? "Quản lý các kênh nhận thông báo đơn hàng, thanh toán và thông số kỹ thuật hạ tầng."
              : "Choose alert channels for incoming orders, subscriber billing, and network infrastructure alerts."}
          </p>
        </div>
      </div>

      <div class="flex items-center space-x-2 shrink-0">
        <button
          type="button"
          onclick={() => (isNotificationsOpen = !isNotificationsOpen)}
          class="h-8 w-8 rounded-lg border border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 flex items-center justify-center text-slate-500 dark:text-slate-400 transition cursor-pointer"
          title={isNotificationsOpen
            ? $language === "vi"
              ? "Thu gọn"
              : "Collapse"
            : $language === "vi"
              ? "Mở rộng"
              : "Expand"}
        >
          {#if isNotificationsOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    {#if isNotificationsOpen}
      <!-- Toggles List -->
      <div class="space-y-3.5">
        <!-- 1. Email Alerts -->
        <label
          class="flex items-center justify-between p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 hover:bg-slate-50 dark:hover:bg-slate-950 cursor-pointer transition"
        >
          <div class="flex items-center space-x-3.5">
            <div
              class="h-9 w-9 rounded-lg bg-blue-100 dark:bg-blue-950/80 text-blue-600 dark:text-blue-400 flex items-center justify-center shrink-0"
            >
              <Mail class="h-4 w-4" />
            </div>
            <div>
              <div class="text-sm font-semibold text-slate-900 dark:text-white">
                {$language === "vi"
                  ? "Thông báo qua Email"
                  : "Email Notifications"}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {$language === "vi"
                  ? "Gửi tự động hóa đơn điện tử, báo cáo doanh thu và xác nhận hợp đồng qua email"
                  : "Automatically send electronic invoices, revenue statements, and order contracts via email"}
              </div>
            </div>
          </div>
          <input
            type="checkbox"
            bind:checked={notifyPrefs.emailAlerts}
            onchange={handleSaveNotifications}
            class="h-5 w-5 rounded border-slate-300 dark:border-slate-700 text-sky-600 focus:ring-sky-500 cursor-pointer"
          />
        </label>

        <!-- 2. SMS Alerts -->
        <label
          class="flex items-center justify-between p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 hover:bg-slate-50 dark:hover:bg-slate-950 cursor-pointer transition"
        >
          <div class="flex items-center space-x-3.5">
            <div
              class="h-9 w-9 rounded-lg bg-emerald-100 dark:bg-emerald-950/80 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0"
            >
              <MessageSquare class="h-4 w-4" />
            </div>
            <div>
              <div class="text-sm font-semibold text-slate-900 dark:text-white">
                {$language === "vi" ? "Tin nhắn SMS" : "SMS Text Alerts"}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {$language === "vi"
                  ? "Cảnh báo khẩn cấp hiện trường, mã xác thực OTP và thông báo kích hoạt cước viễn thông"
                  : "Critical field emergency alerts, two-factor OTP tokens, and subscriber activation SMS"}
              </div>
            </div>
          </div>
          <input
            type="checkbox"
            bind:checked={notifyPrefs.smsAlerts}
            onchange={handleSaveNotifications}
            class="h-5 w-5 rounded border-slate-300 dark:border-slate-700 text-sky-600 focus:ring-sky-500 cursor-pointer"
          />
        </label>

        <!-- 3. Sound Effects -->
        <label
          class="flex items-center justify-between p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 hover:bg-slate-50 dark:hover:bg-slate-950 cursor-pointer transition"
        >
          <div class="flex items-center space-x-3.5">
            <div
              class="h-9 w-9 rounded-lg bg-amber-100 dark:bg-amber-950/80 text-amber-600 dark:text-amber-400 flex items-center justify-center shrink-0"
            >
              <Volume2 class="h-4 w-4" />
            </div>
            <div>
              <div class="text-sm font-semibold text-slate-900 dark:text-white">
                {$language === "vi"
                  ? "Âm thanh thông báo"
                  : "System Audio Chimes"}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {$language === "vi"
                  ? "Phát âm thanh thông báo khi có đơn hàng mới hoặc có thông báo Sonner Toast bật lên"
                  : "Play an audible ping when new orders arrive or toast notifications appear"}
              </div>
            </div>
          </div>
          <input
            type="checkbox"
            bind:checked={notifyPrefs.soundAlerts}
            onchange={handleSaveNotifications}
            class="h-5 w-5 rounded border-slate-300 dark:border-slate-700 text-sky-600 focus:ring-sky-500 cursor-pointer"
          />
        </label>

        <!-- 4. Order Updates -->
        <label
          class="flex items-center justify-between p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 hover:bg-slate-50 dark:hover:bg-slate-950 cursor-pointer transition"
        >
          <div class="flex items-center space-x-3.5">
            <div
              class="h-9 w-9 rounded-lg bg-indigo-100 dark:bg-indigo-950/80 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shrink-0"
            >
              <Sparkles class="h-4 w-4" />
            </div>
            <div>
              <div class="text-sm font-semibold text-slate-900 dark:text-white">
                {$language === "vi"
                  ? "Cập nhật tiến độ đơn hàng"
                  : "Order Status Updates"}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {$language === "vi"
                  ? "Đẩy thông báo khi kỹ thuật nghiệm thu đường dây, cung cấp thiết bị hoặc cấp Account ID"
                  : "Notify immediately when feasibility is cleared, connection is provided, or Account ID is generated"}
              </div>
            </div>
          </div>
          <input
            type="checkbox"
            bind:checked={notifyPrefs.orderUpdates}
            onchange={handleSaveNotifications}
            class="h-5 w-5 rounded border-slate-300 dark:border-slate-700 text-sky-600 focus:ring-sky-500 cursor-pointer"
          />
        </label>

        <!-- 5. Security & Infrastructure Alerts -->
        <label
          class="flex items-center justify-between p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 hover:bg-slate-50 dark:hover:bg-slate-950 cursor-pointer transition"
        >
          <div class="flex items-center space-x-3.5">
            <div
              class="h-9 w-9 rounded-lg bg-rose-100 dark:bg-rose-950/80 text-rose-600 dark:text-rose-400 flex items-center justify-center shrink-0"
            >
              <Shield class="h-4 w-4" />
            </div>
            <div>
              <div class="text-sm font-semibold text-slate-900 dark:text-white">
                {$language === "vi"
                  ? "Cảnh báo an ninh & Sự cố NOC"
                  : "Security & Network NOC Alerts"}
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400">
                {$language === "vi"
                  ? "Báo động suy hao quang vượt chuẩn (-30 dBm), đầy hộp DP Box hoặc phát hiện truy cập lạ"
                  : "High-priority alerts for optical attenuation exceeding -30 dBm, DP box saturation, or security audits"}
              </div>
            </div>
          </div>
          <input
            type="checkbox"
            bind:checked={notifyPrefs.securityAlerts}
            onchange={handleSaveNotifications}
            class="h-5 w-5 rounded border-slate-300 dark:border-slate-700 text-sky-600 focus:ring-sky-500 cursor-pointer"
          />
        </label>
      </div>
    {/if}
  </div>

  <!-- 5. LATEST VERSION & SYSTEM UPDATES (CẬP NHẬT VERSION MỚI NHẤT) -->
  <div
    class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-300 overflow-hidden {isVersionOpen
      ? 'p-6 sm:p-8 space-y-6'
      : 'p-4 sm:p-5'}"
  >
    <div
      class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 {isVersionOpen
        ? 'border-b border-slate-100 dark:border-slate-800 pb-5'
        : ''}"
    >
      <div
        class="flex items-center space-x-3.5 cursor-pointer flex-1"
        role="button"
        tabindex="0"
        onclick={() => (isVersionOpen = !isVersionOpen)}
        onkeydown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            isVersionOpen = !isVersionOpen;
          }
        }}
      >
        <div
          class="h-11 w-11 rounded-xl bg-gradient-to-br from-sky-500 to-blue-600 text-white flex items-center justify-center shadow-md shadow-sky-500/20 shrink-0"
        >
          <RefreshCw class="h-5 w-5 {isCheckingUpdate ? 'animate-spin' : ''}" />
        </div>
        <div>
          <div class="flex flex-wrap items-center gap-2">
            <h3 class="text-base font-bold text-slate-900 dark:text-white">
              {$language === "vi"
                ? "Cập nhật hệ thống & Phiên bản mới nhất"
                : "System Updates & Latest Software Version"}
            </h3>
          </div>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            {$language === "vi"
              ? "Kiểm tra bản phát hành mới nhất, quản lý cập nhật tự động và theo dõi nhật ký thay đổi của Nexus Telecom OS."
              : "Check for software releases, manage auto-updates, and view upgrade changelogs for Nexus Telecom OS."}
          </p>
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-2.5 shrink-0">
        <button
          type="button"
          onclick={handleCheckUpdate}
          disabled={isCheckingUpdate || isUpdatingVersion}
          class="inline-flex items-center space-x-2 px-4 py-2.5 rounded-xl text-xs font-bold border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-700/80 transition active:scale-95 disabled:opacity-60 cursor-pointer shadow-xs"
        >
          <RefreshCw
            class="h-3.5 w-3.5 {isCheckingUpdate
              ? 'animate-spin text-sky-600 dark:text-sky-400'
              : ''}"
          />
          <span>
            {isCheckingUpdate
              ? $language === "vi"
                ? "Đang kiểm tra..."
                : "Checking..."
              : $language === "vi"
                ? "Kiểm tra cập nhật"
                : "Check for Updates"}
          </span>
        </button>

        <button
          type="button"
          onclick={handlePerformUpdate}
          disabled={isCheckingUpdate || isUpdatingVersion}
          class="inline-flex items-center space-x-2 px-4 py-2.5 rounded-xl text-xs font-bold bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white shadow-md shadow-sky-500/25 transition active:scale-95 disabled:opacity-60 cursor-pointer"
        >
          <Download class="h-3.5 w-3.5" />
          <span>
            {isUpdatingVersion
              ? $language === "vi"
                ? "Đang đồng bộ..."
                : "Updating..."
              : $language === "vi"
                ? "Cài đặt lại bản mới nhất"
                : "Reinstall Latest Version"}
          </span>
        </button>

        <button
          type="button"
          onclick={() => (isVersionOpen = !isVersionOpen)}
          class="h-9 w-9 rounded-xl border border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 flex items-center justify-center text-slate-500 dark:text-slate-400 transition cursor-pointer"
          title={isVersionOpen
            ? $language === "vi"
              ? "Thu gọn"
              : "Collapse"
            : $language === "vi"
              ? "Mở rộng"
              : "Expand"}
        >
          {#if isVersionOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    {#if isVersionOpen}
      <!-- Update Progress Bar (Active when updating) -->
      {#if isUpdatingVersion}
        <div
          class="p-4 rounded-xl border border-sky-200 dark:border-sky-800/80 bg-sky-50/70 dark:bg-sky-950/40 space-y-2.5 animate-in fade-in duration-300"
        >
          <div class="flex items-center justify-between text-xs">
            <span
              class="font-bold text-sky-900 dark:text-sky-200 flex items-center space-x-2"
            >
              <RefreshCw
                class="h-3.5 w-3.5 animate-spin text-sky-600 dark:text-sky-400"
              />
              <span
                >{$language === "vi"
                  ? updateStatusMessageVi
                  : updateStatusMessageEn}</span
              >
            </span>
            <span class="font-mono font-bold text-sky-700 dark:text-sky-300"
              >{updateProgress}%</span
            >
          </div>
          <div
            class="w-full h-2.5 bg-sky-200/60 dark:bg-sky-900/60 rounded-full overflow-hidden"
          >
            <div
              class="h-full bg-gradient-to-r from-sky-500 to-blue-600 rounded-full transition-all duration-300 shadow-sm"
              style="width: {updateProgress}%"
            ></div>
          </div>
        </div>
      {/if}

      <!-- 3 Metrics Cards -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <!-- 1. Version Card -->
        <div
          class="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 flex items-start space-x-3.5"
        >
          <div
            class="h-10 w-10 rounded-xl bg-sky-100 dark:bg-sky-950/80 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0 shadow-xs"
          >
            <Cpu class="h-5 w-5" />
          </div>
          <div class="min-w-0">
            <div
              class="text-[11px] font-medium text-slate-500 dark:text-slate-400"
            >
              {$language === "vi"
                ? "Phiên bản hiện hành"
                : "Current Installed Version"}
            </div>
            <div
              class="font-bold text-sm text-slate-900 dark:text-white mt-0.5 truncate"
            >
              Nexus OS {currentVersion}
            </div>
            <div class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5">
              {buildId}
            </div>
          </div>
        </div>

        <!-- 2. Status Card -->
        <div
          class="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 flex items-start space-x-3.5"
        >
          <div
            class="h-10 w-10 rounded-xl bg-emerald-100 dark:bg-emerald-950/80 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0 shadow-xs"
          >
            <ShieldCheck class="h-5 w-5" />
          </div>
          <div class="min-w-0">
            <div
              class="text-[11px] font-medium text-slate-500 dark:text-slate-400"
            >
              {$language === "vi" ? "Trạng thái phát hành" : "Release Status"}
            </div>
            <div
              class="font-bold text-sm text-emerald-600 dark:text-emerald-400 mt-0.5 flex items-center space-x-1.5"
            >
              <CheckCircle2 class="h-4 w-4" />
              <span
                >{$language === "vi"
                  ? "Bản mới nhất & Ổn định"
                  : "Up to date & Stable"}</span
              >
            </div>
            <div class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5">
              {$language === "vi"
                ? "Kênh doanh nghiệp Enterprise"
                : "Production Enterprise Channel"}
            </div>
          </div>
        </div>

        <!-- 3. Last Checked Card -->
        <div
          class="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 flex items-start space-x-3.5"
        >
          <div
            class="h-10 w-10 rounded-xl bg-purple-100 dark:bg-purple-950/80 text-purple-600 dark:text-purple-400 flex items-center justify-center shrink-0 shadow-xs"
          >
            <Clock class="h-5 w-5" />
          </div>
          <div class="min-w-0">
            <div
              class="text-[11px] font-medium text-slate-500 dark:text-slate-400"
            >
              {$language === "vi" ? "Kiểm tra gần nhất" : "Last Checked"}
            </div>
            <div
              class="font-bold text-sm text-slate-900 dark:text-white mt-0.5 truncate"
            >
              {$language === "vi" ? lastCheckedVi : lastCheckedEn}
            </div>
            <div class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5">
              {$language === "vi"
                ? "Máy chủ phân phối NOC sẵn sàng"
                : "NOC Distribution Node Ready"}
            </div>
          </div>
        </div>
      </div>

      <!-- Auto-update setting toggle -->
      <label
        class="flex items-center justify-between p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/40 hover:bg-slate-50 dark:hover:bg-slate-950 cursor-pointer transition"
      >
        <div class="flex items-center space-x-3.5">
          <div
            class="h-9 w-9 rounded-lg bg-sky-100 dark:bg-sky-950/80 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0"
          >
            <ArrowUpCircle class="h-4 w-4" />
          </div>
          <div>
            <div class="text-sm font-semibold text-slate-900 dark:text-white">
              {$language === "vi"
                ? "Tự động kiểm tra & nạp các bản vá lỗi bảo mật mới nhất"
                : "Automatically download & apply critical security updates"}
            </div>
            <div class="text-xs text-slate-500 dark:text-slate-400">
              {$language === "vi"
                ? "Hệ thống sẽ chạy kiểm tra ngầm và duy trì các bản vá lỗi hạ tầng, đồng bộ phiên bản ở trạng thái tối ưu nhất"
                : "Background service keeps telecom schemas, authentication protocols, and routing updates constantly synced"}
            </div>
          </div>
        </div>
        <input
          type="checkbox"
          checked={autoUpdatePatches}
          onchange={handleToggleAutoUpdate}
          class="h-5 w-5 rounded border-slate-300 dark:border-slate-700 text-sky-600 focus:ring-sky-500 cursor-pointer"
        />
      </label>

      <!-- Changelog Accordion -->
      <div
        class="border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden bg-slate-50/30 dark:bg-slate-950/20"
      >
        <button
          type="button"
          onclick={() => (showChangelog = !showChangelog)}
          class="w-full px-5 py-3.5 bg-slate-50 dark:bg-slate-800/60 hover:bg-slate-100 dark:hover:bg-slate-800 transition flex items-center justify-between cursor-pointer"
        >
          <div class="flex items-center space-x-2.5">
            <History class="h-4 w-4 text-sky-600 dark:text-sky-400" />
            <span class="text-xs font-bold text-slate-900 dark:text-white">
              {$language === "vi"
                ? "Nhật ký thay đổi phiên bản (Release Notes)"
                : "Version Changelog & Release Notes"}
            </span>
            <span
              class="text-[10px] font-mono px-2 py-0.5 bg-sky-100 dark:bg-sky-950/80 text-sky-700 dark:text-sky-300 rounded font-semibold border border-sky-200 dark:border-sky-800"
            >
              {currentVersion}
            </span>
          </div>
          <div
            class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
          >
            {#if showChangelog}
              <ChevronUp class="h-4 w-4" />
            {:else}
              <ChevronDown class="h-4 w-4" />
            {/if}
          </div>
        </button>

        {#if showChangelog}
          <div
            class="p-5 space-y-4 text-xs text-slate-600 dark:text-slate-300 border-t border-slate-200 dark:border-slate-800"
          >
            <!-- Latest Version Item -->
            <div class="space-y-2">
              <div class="flex items-center justify-between">
                <span class="font-bold text-sky-600 dark:text-sky-400 text-sm">
                  v2.4.1 (Bản phát hành chính thức - 2026.09)
                </span>
                <span class="text-[11px] text-slate-400"
                  >{$language === "vi" ? "Mới nhất" : "Latest"}</span
                >
              </div>
              <ul
                class="space-y-1.5 pl-4 list-disc text-slate-600 dark:text-slate-400 leading-relaxed"
              >
                <li>
                  <strong class="text-slate-800 dark:text-slate-200">
                    {$language === "vi"
                      ? "Đa ngôn ngữ toàn diện (Bilingual Support):"
                      : "Comprehensive Bilingual Support:"}
                  </strong>
                  {$language === "vi"
                    ? "Chuyển đổi tức thì giữa Tiếng Việt và Tiếng Anh trên tất cả bảng điều khiển Quản trị, Bán lẻ, Kỹ thuật, Kế toán và Khách hàng."
                    : "Instant switching between Vietnamese and English across Admin, Retail, Technical, Accounts, and Customer portals."}
                </li>
                <li>
                  <strong class="text-slate-800 dark:text-slate-200">
                    {$language === "vi"
                      ? "Thông báo tương tác tức thời (Interactive Notifications):"
                      : "Interactive Direct Notifications:"}
                  </strong>
                  {$language === "vi"
                    ? "Bấm vào từng mục thông báo trên thanh tiêu đề để chuyển ngay tới trang và tab chức năng tương ứng."
                    : "Click on any notification in the header bell droplist to jump directly to its target section and tab."}
                </li>
                <li>
                  <strong class="text-slate-800 dark:text-slate-200">
                    {$language === "vi"
                      ? "Tối ưu thời gian thông báo (Toast 1.5s):"
                      : "Optimized Toast Timing (1.5s):"}
                  </strong>
                  {$language === "vi"
                    ? "Chuẩn hóa thời lượng hiển thị hộp thông báo Sonner Toast tự động đóng sau 1.5 giây nhanh chóng, không gây che khuất."
                    : "Configured global Sonner toast notifications with a smooth 1.5-second duration."}
                </li>
                <li>
                  <strong class="text-slate-800 dark:text-slate-200">
                    {$language === "vi"
                      ? "Thu cước trực tiếp tại quầy (Retail Direct Payment):"
                      : "Retail Direct Payment Settlement:"}
                  </strong>
                  {$language === "vi"
                    ? "Bổ sung chức năng nhân viên quầy bán lẻ có thể thu tiền mặt/chuyển khoản và ghi nhận hóa đơn trực tiếp cho khách hàng."
                    : "Enabled retail store representatives to directly record counter cash/transfer payments for subscribers."}
                </li>
                <li>
                  <strong class="text-slate-800 dark:text-slate-200">
                    {$language === "vi"
                      ? "Chuẩn hóa giao diện & Thanh cuộn (UI Aesthetics & Scrollbars):"
                      : "Standardized UI & Scrollbars:"}
                  </strong>
                  {$language === "vi"
                    ? "Tối ưu thanh cuộn dọc trên tất cả màn hình, đồng bộ hoàn hảo chế độ Sáng/Tối và hiệu ứng đổ bóng glassmorphism."
                    : "Consistent scrollbars across all screen dimensions with enhanced Dark/Light mode theme harmony."}
                </li>
              </ul>
            </div>
          </div>
        {/if}
      </div>
    {/if}
  </div>
</div>

<style>
  @keyframes selectGlowPulse {
    0% {
      box-shadow: 0 0 0 0 rgba(14, 165, 233, 0.7);
      transform: scale(0.97);
    }
    50% {
      box-shadow: 0 0 0 10px rgba(14, 165, 233, 0);
      transform: scale(1.03);
    }
    100% {
      box-shadow: 0 0 0 0 rgba(14, 165, 233, 0);
      transform: scale(1);
    }
  }

  @keyframes flagWiggle {
    0%, 100% {
      transform: rotate(0deg) scale(1);
    }
    25% {
      transform: rotate(-12deg) scale(1.25);
    }
    75% {
      transform: rotate(12deg) scale(1.25);
    }
  }

  @keyframes shimmerAura {
    0% {
      background-position: 0% 50%;
    }
    50% {
      background-position: 100% 50%;
    }
    100% {
      background-position: 0% 50%;
    }
  }

  .lang-select-wrapper {
    position: relative;
    transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1), filter 0.25s ease;
  }

  .lang-select-wrapper:hover {
    transform: translateY(-1.5px);
  }

  .lang-select-wrapper:hover .lang-flag {
    animation: flagWiggle 0.6s ease-in-out;
  }

  .lang-select-wrapper.just-changed {
    animation: selectGlowPulse 0.65s cubic-bezier(0.16, 1, 0.3, 1);
  }

  .lang-aura {
    background: linear-gradient(90deg, #0ea5e9, #6366f1, #f59e0b, #0ea5e9);
    background-size: 300% 300%;
    animation: shimmerAura 4s ease infinite;
  }

  .lang-custom-select {
    background-image: none !important;
    padding-right: 2.25rem !important;
  }
</style>
