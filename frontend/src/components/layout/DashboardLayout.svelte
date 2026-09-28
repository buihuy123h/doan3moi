<script module lang="ts">
  // Mirrors components/layout/DashboardLayout.tsx of the React original.
  import type { Component, Snippet } from "svelte";

  export interface NavItem {
    id: string;
    label: string;
    // lucide-svelte icons are typed as LegacyComponentType; use a permissive type here
    icon: any;
    badge?: string | number;
    badgeColor?: string;
  }
</script>

<script lang="ts">
  import {
    Menu,
    Search,
    Settings,
    Bell,
    LogOut,
    Download,
    Plus,
    Sparkles,
    ShieldCheck,
    ShoppingBag,
    Wrench,
    Calculator,
    User,
    Sun,
    Moon,
    Clock,
    ChevronDown,
    Database,
    RefreshCw,
  } from "lucide-svelte";
  import { toast } from "svelte-sonner";
  import { authStore } from "../../context/AuthContext";
  import { themeStore } from "../../context/ThemeContext";
  import { languageStore } from "../../context/LanguageContext";
  import { nexusStore } from "../../context/NexusContext";
  import LanguageToggle from "./LanguageToggle.svelte";
  import NotificationDropdown from "./NotificationDropdown.svelte";
  import SearchDropdown from "./SearchDropdown.svelte";
  import { navigate } from "../../lib/router";

  let {
    activeTab,
    onTabChange,
    navItems,
    pageTitle,
    roleBadgeTitle,
    children,
    exportAction,
    primaryAction,
    customHeaderActions,
  }: {
    activeTab: string;
    onTabChange: (tabId: string) => void;
    navItems: NavItem[];
    pageTitle?: string;
    roleBadgeTitle?: string;
    children: Snippet;
    exportAction?: { label?: string; onClick: () => void };
    primaryAction?: { label: string; onClick: () => void; icon?: any };
    customHeaderActions?: Snippet;
  } = $props();

  const { currentUser, logout } = authStore;
  const { theme, toggleTheme } = themeStore;
  const { t, language } = languageStore;
  const { dbConnected, dbInfo, isSyncing, syncWithDatabase } = nexusStore;

  // Search expanding state
  let isSearchExpanded = $state(false);
  let searchQuery = $state("");

  // Sidebar open/closed state with 0.3s transition (persisted in localStorage)
  let isSidebarOpen = $state(
    (() => {
      const saved = localStorage.getItem("nexus_sidebar_open");
      return saved !== null ? saved === "true" : true;
    })(),
  );

  $effect(() => {
    localStorage.setItem("nexus_sidebar_open", String(isSidebarOpen));
  });

  const toggleSidebar = () => {
    isSidebarOpen = !isSidebarOpen;
  };

  // Tự động giấu sidebar khi bấm ra vùng main (chỉ áp dụng trên thiết bị di động / tablet)
  const handleMainClick = () => {
    if (isSidebarOpen && window.innerWidth <= 768) {
      isSidebarOpen = false;
    }
  };

  // Live ticking date and time for the greeting banner
  let currentTime = $state(new Date());

  $effect(() => {
    const timer = setInterval(() => {
      currentTime = new Date();
    }, 1000);
    return () => clearInterval(timer);
  });

  // Format greeting based on hour
  const greeting = $derived.by(() => {
    const hour = currentTime.getHours();
    if (hour < 12) return $t.greetings.morning;
    if (hour < 18) return $t.greetings.afternoon;
    return $t.greetings.evening;
  });

  // Format date and time matching screenshot style: localized for EN/VI
  const formattedDateTime = $derived(
    currentTime.toLocaleDateString($language === "vi" ? "vi-VN" : "en-US", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
    }) +
      ($language === "vi" ? " lúc " : " at ") +
      currentTime.toLocaleTimeString($language === "vi" ? "vi-VN" : "en-US", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
        hour12: true,
      }),
  );

  // User Profile Dropdown Menu State
  let isUserMenuOpen = $state(false);

  const handleWindowClick = (e: MouseEvent) => {
    const target = e.target as HTMLElement | null;
    if (isUserMenuOpen && target && !target.closest(".user-menu-container")) {
      isUserMenuOpen = false;
    }
  };

  const handleKeydown = (e: KeyboardEvent) => {
    if (e.key === "Escape" && isUserMenuOpen) {
      isUserMenuOpen = false;
    }
  };

  const handleLogout = () => {
    logout();
    toast.info(
      $language === "vi"
        ? "Đã đăng xuất khỏi phiên làm việc."
        : "Signed out successfully.",
    );
    navigate("/login");
  };

  // Resolve current active item for page title fallback
  const currentTitle = $derived(
    pageTitle ||
      navItems.find((item) => item.id === activeTab)?.label ||
      $t.common.dashboard,
  );

  // Sidebar footer role icon (derived, not {@const}: must live in script scope)
  const SidebarRoleIcon = $derived(
    $currentUser?.role === "admin"
      ? ShieldCheck
      : $currentUser?.role === "retail"
        ? ShoppingBag
        : $currentUser?.role === "technical"
          ? Wrench
          : $currentUser?.role === "accounts"
            ? Calculator
            : User,
  );
</script>

<svelte:window onclick={handleWindowClick} onkeydown={handleKeydown} />

<div
  class="flex h-screen w-full bg-[#E0F1FF] dark:bg-[#1B2D40] text-[#1B2D40] dark:text-[#E0F1FF] font-sans antialiased overflow-hidden transition-colors duration-300"
>
  <!-- 1. LEFT SIDEBAR (Unscrollable, 0.3s animated toggle) -->
  <aside
    class="transition-all duration-300 ease-in-out shrink-0 bg-[#EDF6FF] dark:bg-[#152434] border-r border-[#CCE4F7] dark:border-[#253D56] flex flex-col justify-between z-30 h-full select-none overflow-hidden {isSidebarOpen
      ? 'w-64 min-w-[16rem] opacity-100 shadow-sm'
      : 'w-0 min-w-0 opacity-0 overflow-hidden border-r-0'}"
  >
    <div class="flex flex-col h-full overflow-hidden">
      <!-- Top Brand Bar -->
      <div
        class="h-16 border-b border-[#CCE4F7] dark:border-[#253D56] flex items-center px-5 space-x-3 shrink-0"
      >
        <div
          class="h-9 w-9 rounded-full bg-[#D8ECFC] dark:bg-[#1E3349] border border-[#BBDDF5] dark:border-[#2A4460] flex items-center justify-center text-sky-700 dark:text-sky-300 shadow-xs shrink-0"
        >
          <Sparkles class="h-4 w-4" />
        </div>
        <div class="min-w-0 flex-1 truncate">
          <div
            class="font-bold text-base tracking-tight text-sky-900 dark:text-sky-300 leading-tight flex items-center gap-1"
          >
            <span>Nexus</span>
            <span
              class="text-[10px] uppercase tracking-wider text-sky-800 dark:text-sky-200 font-semibold bg-sky-500/15 px-1 rounded"
            >
              {$currentUser?.role || "SMS"}
            </span>
          </div>
          <div class="text-[11px] text-[#537292] dark:text-[#8DB0D4] truncate">
            Nexus Marketing System
          </div>
        </div>
      </div>

      <!-- Navigation Items List - scrollable on short screens -->
      <nav class="flex-1 py-4 overflow-y-auto space-y-0.5">
        {#each navItems as item (item.id)}
          {@const Icon = item.icon}
          {@const isActive = activeTab === item.id}
          <button
            onclick={() => onTabChange(item.id)}
            class="w-full flex items-center justify-between py-3 px-4 text-sm font-medium transition-all duration-200 group text-left {isActive
              ? 'border-l-4 border-sky-600 dark:border-sky-400 bg-white/90 dark:bg-[#1E3349] text-sky-700 dark:text-sky-300 font-semibold shadow-xs'
              : 'border-l-4 border-transparent text-[#2C4764] dark:text-[#94B5D6] hover:text-[#0F1D2B] dark:hover:text-white hover:bg-white/60 dark:hover:bg-[#1E3349]/50'}"
          >
            <div class="flex items-center space-x-3.5 min-w-0 truncate">
              <Icon
                class="h-4 w-4 shrink-0 transition-colors {isActive
                  ? 'text-sky-600 dark:text-sky-400'
                  : 'text-[#537292] dark:text-[#7A9BBF] group-hover:text-[#0F1D2B] dark:group-hover:text-white'}"
              />
              <span class="truncate">{item.label}</span>
            </div>

            {#if item.badge !== undefined}
              <span
                class="text-xs px-2 py-0.5 rounded-full shrink-0 font-medium {item.badgeColor ||
                  (isActive
                    ? 'bg-sky-600 dark:bg-sky-500 text-white'
                    : 'bg-sky-100 dark:bg-[#1E3349] text-sky-800 dark:text-sky-200 border border-[#CCE4F7] dark:border-[#253D56]')}"
              >
                {item.badge}
              </span>
            {/if}
          </button>
        {/each}
      </nav>

      <!-- Sidebar Footer / Current User Badge -->
      <div
        class="p-3.5 border-t border-[#CCE4F7] dark:border-[#253D56] bg-[#E5F2FC]/70 dark:bg-[#111E2C]/70 text-xs text-[#537292] dark:text-[#88A9CB] space-y-1.5 shrink-0"
      >
        <div
          class="flex items-center space-x-2 text-[#1B2D40] dark:text-[#E0F1FF] font-semibold truncate"
        >
          <SidebarRoleIcon class="h-3.5 w-3.5 text-sky-600 dark:text-sky-400" />
          <span class="truncate"
            >{roleBadgeTitle ||
              ($currentUser?.role && $t.roles[$currentUser.role]) ||
              $currentUser?.title ||
              "Nexus User"}</span
          >
        </div>
        <div
          class="text-[10px] text-[#6B8FB5] dark:text-[#5E7F9F] flex items-center justify-between pt-0.5"
        >
          <span>{$t.common.stationReady}</span>
          <span>v2.4</span>
        </div>
      </div>
    </div>
  </aside>

  <!-- 2. RIGHT COLUMN: FIXED HEADER + SCROLLABLE MAIN -->
  <div class="flex-1 flex flex-col h-full min-w-0 overflow-hidden">
    <!-- Mobile & Tablet Backdrop Overlay when sidebar is open -->
    {#if isSidebarOpen}
      <button
        type="button"
        onclick={() => (isSidebarOpen = false)}
        class="fixed inset-0 bg-slate-950/40 backdrop-blur-sm z-[25] md:hidden transition-opacity duration-300 border-none cursor-pointer w-full h-full text-left p-0 m-0"
        aria-label={$language === "vi"
          ? "Bấm vào đây để giấu menu bên"
          : "Click to hide sidebar"}
      ></button>
    {/if}
    <!-- Top Header Bar - Fixed height, never scrolls -->
    <header
      class="shrink-0 h-16 bg-white/95 dark:bg-[#172738]/95 border-b border-[#CCE4F7] dark:border-[#253D56] flex items-center justify-between px-4 sm:px-6 shadow-xs z-20 backdrop-blur-md transition-colors duration-300"
    >
      <!-- Left: 3-Stripes Hamburger Toggle + Current Page Title -->
      <div class="flex items-center space-x-3 sm:space-x-4 min-w-0">
        <button
          onclick={toggleSidebar}
          class="flex items-center justify-center h-9 w-9 rounded-lg text-[#1B2D40] dark:text-[#E0F1FF] hover:bg-[#EDF6FF] dark:hover:bg-[#1E3349] transition active:scale-95 shrink-0"
          title={isSidebarOpen
            ? $language === "vi"
              ? "Thu gọn menu bên"
              : "Collapse sidebar"
            : $language === "vi"
              ? "Mở rộng menu bên"
              : "Expand sidebar"}
        >
          <Menu class="h-5 w-5" />
        </button>

        <h1
          class="text-base sm:text-lg font-bold text-[#0F1D2B] dark:text-white tracking-tight truncate"
        >
          {currentTitle}
        </h1>
      </div>

      <!-- Right: Circular Action Buttons & User Profile / Logout -->
      <div class="flex items-center space-x-2 sm:space-x-3">
        <!-- Custom Header Actions if passed -->
        {#if customHeaderActions}{@render customHeaderActions()}{/if}

        <!-- Search Droplist (Category Search Dropdown) -->
        <SearchDropdown />

        <!-- Circular Settings Button -->
        <button
          onclick={() => onTabChange("settings")}
          class="h-9 w-9 rounded-full bg-[#EDF6FF] dark:bg-[#1E3349] hover:bg-[#DCEEFE] dark:hover:bg-[#253E58] text-[#1B2D40] dark:text-[#E0F1FF] border border-[#CCE4F7] dark:border-[#253D56] flex items-center justify-center transition shadow-xs"
          title={$t.common.settings}
        >
          <Settings class="h-4 w-4" />
        </button>

        <!-- Language Switcher Button (EN / VI) -->
        <LanguageToggle />

        <!-- Circular Theme Toggle Button (Light/Dark Mode) -->
        <button
          onclick={toggleTheme}
          class="h-9 w-9 rounded-full bg-[#EDF6FF] dark:bg-[#1E3349] hover:bg-[#DCEEFE] dark:hover:bg-[#253E58] text-[#1B2D40] dark:text-[#E0F1FF] border border-[#CCE4F7] dark:border-[#253D56] flex items-center justify-center transition-all duration-200 shadow-xs hover:scale-105 active:scale-95 group"
          title={$theme === "dark" ? $t.common.themeLight : $t.common.themeDark}
        >
          {#if $theme === "dark"}
            <Sun
              class="h-4 w-4 text-amber-400 group-hover:rotate-45 transition-transform"
            />
          {:else}
            <Moon
              class="h-4 w-4 text-sky-700 group-hover:-rotate-12 transition-transform"
            />
          {/if}
        </button>
        <!-- Notification Droplist Menu -->
        <NotificationDropdown />

        <!-- User Profile & Dropdown Menu -->
        {#if $currentUser}
          <div class="relative user-menu-container">
            <button
              type="button"
              onclick={() => (isUserMenuOpen = !isUserMenuOpen)}
              class="flex items-center p-1 pl-1.5 pr-2.5 bg-[#EDF6FF]/90 dark:bg-[#1E3349]/90 border {isUserMenuOpen ||
              activeTab === 'profile'
                ? 'border-sky-500 ring-2 ring-sky-500/20 shadow-sm'
                : 'border-[#CCE4F7] dark:border-[#253D56]'} rounded-full shadow-xs space-x-2 hover:bg-[#DCEEFE] dark:hover:bg-[#253E58] transition cursor-pointer group"
              title={$language === "vi"
                ? "Cài đặt Profile & Tài khoản (Nhấn để mở)"
                : "Profile & Account Settings (Click to open)"}
            >
              <!-- Avatar circle -->
              <div
                class="flex h-7 w-7 items-center justify-center rounded-full bg-gradient-to-tr from-sky-600 to-blue-600 text-white font-bold text-xs shadow-inner ring-1 ring-white/50 group-hover:scale-105 transition-transform overflow-hidden shrink-0"
              >
                {#if $currentUser.avatar}
                  <img
                    src={$currentUser.avatar}
                    alt={$currentUser.name}
                    class="h-full w-full object-cover"
                  />
                {:else}
                  {$currentUser.name.charAt(0)}
                {/if}
              </div>

              <div class="text-left hidden md:block pr-0.5">
                <div
                  class="text-xs font-bold text-[#0F1D2B] dark:text-white leading-none"
                >
                  {$currentUser.name}
                </div>
                <div
                  class="text-[10px] text-[#537292] dark:text-[#8DB0D4] leading-none mt-0.5"
                >
                  {($currentUser.role && $t.roles[$currentUser.role]) || $currentUser.title}
                </div>
              </div>

              <!-- Chevron indicator -->
              <div
                class="text-slate-400 group-hover:text-slate-600 dark:group-hover:text-slate-200 transition"
              >
                <ChevronDown
                  class="h-3 w-3 {isUserMenuOpen
                    ? 'rotate-180'
                    : ''} transition-transform"
                />
              </div>
            </button>

            <!-- User Menu Dropdown -->
            {#if isUserMenuOpen}
              <div
                class="absolute right-0 mt-2 w-72 sm:w-80 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl overflow-hidden z-50 animate-in fade-in zoom-in-95 duration-150"
              >
                <!-- Dropdown Header -->
                <div
                  class="p-4 bg-gradient-to-br from-sky-50/70 to-slate-50 dark:from-sky-950/40 dark:to-slate-900/80 border-b border-slate-100 dark:border-slate-800"
                >
                  <div class="flex items-center space-x-3">
                    <div
                      class="h-11 w-11 rounded-xl bg-gradient-to-tr from-sky-600 to-blue-600 text-white font-black text-base flex items-center justify-center shadow-md overflow-hidden shrink-0"
                    >
                      {#if $currentUser.avatar}
                        <img
                          src={$currentUser.avatar}
                          alt={$currentUser.name}
                          class="h-full w-full object-cover"
                        />
                      {:else}
                        {$currentUser.name.charAt(0)}
                      {/if}
                    </div>
                    <div class="min-w-0 flex-1">
                      <div
                        class="font-bold text-sm text-slate-900 dark:text-white truncate"
                      >
                        {$currentUser.name}
                      </div>
                      <div
                        class="text-xs text-slate-500 dark:text-slate-400 truncate"
                      >
                        {$currentUser.email}
                      </div>
                      <div class="mt-1">
                        <span
                          class="inline-flex items-center px-2 py-0.5 rounded-md text-[10px] font-semibold bg-sky-100 dark:bg-sky-950/80 text-sky-700 dark:text-sky-300 border border-sky-200 dark:border-sky-800 uppercase tracking-wider"
                        >
                          {($currentUser.role && $t.roles[$currentUser.role]) || $currentUser.role}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Dropdown Navigation Options -->
                <div class="p-2 space-y-1">
                  <!-- 1. Go to Profile Settings -->
                  <button
                    type="button"
                    onclick={() => {
                      onTabChange("profile");
                      isUserMenuOpen = false;
                    }}
                    class="w-full flex items-center justify-between px-3 py-2.5 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 hover:bg-sky-50 dark:hover:bg-sky-950/50 hover:text-sky-700 dark:hover:text-sky-300 transition text-left cursor-pointer group"
                  >
                    <div class="flex items-center space-x-2.5">
                      <div
                        class="h-7 w-7 rounded-lg bg-sky-100 dark:bg-sky-900/40 text-sky-600 dark:text-sky-400 flex items-center justify-center group-hover:scale-105 transition-transform"
                      >
                        <User class="h-4 w-4" />
                      </div>
                      <div>
                        <div class="font-bold">
                          {$language === "vi"
                            ? "Cài đặt Profile"
                            : "Profile Settings"}
                        </div>
                        <div class="text-[10px] text-slate-400 font-normal">
                          {$language === "vi"
                            ? "Thông tin cá nhân, tài khoản, mật khẩu"
                            : "Personal info, account, password"}
                        </div>
                      </div>
                    </div>
                    <span
                      class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-sky-100 dark:bg-sky-950 text-sky-600 dark:text-sky-400 border border-sky-200 dark:border-sky-800"
                    >
                      {$language === "vi" ? "Mở" : "Open"}
                    </span>
                  </button>

                  <!-- 2. Go to System Settings -->
                  <button
                    type="button"
                    onclick={() => {
                      onTabChange("settings");
                      isUserMenuOpen = false;
                    }}
                    class="w-full flex items-center justify-between px-3 py-2.5 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition text-left cursor-pointer"
                  >
                    <div class="flex items-center space-x-2.5">
                      <div
                        class="h-7 w-7 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 flex items-center justify-center"
                      >
                        <Settings class="h-4 w-4" />
                      </div>
                      <div>
                        <div class="font-bold">
                          {$language === "vi"
                            ? "Cài đặt hệ thống"
                            : "System Settings"}
                        </div>
                        <div class="text-[10px] text-slate-400 font-normal">
                          {$language === "vi"
                            ? "Giao diện, ngôn ngữ, thông báo, chi nhánh"
                            : "Theme, language, alerts, branch"}
                        </div>
                      </div>
                    </div>
                  </button>

                  <div
                    class="my-1 border-t border-slate-100 dark:border-slate-800"
                  ></div>

                  <!-- 3. Logout Option -->
                  <button
                    type="button"
                    onclick={() => {
                      isUserMenuOpen = false;
                      handleLogout();
                    }}
                    class="w-full flex items-center space-x-2.5 px-3 py-2.5 rounded-xl text-xs font-bold text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition text-left cursor-pointer"
                  >
                    <div
                      class="h-7 w-7 rounded-lg bg-rose-100 dark:bg-rose-950/60 text-rose-600 dark:text-rose-400 flex items-center justify-center"
                    >
                      <LogOut class="h-4 w-4" />
                    </div>
                    <div>
                      <div>
                        {$language === "vi"
                          ? "Đăng xuất tài khoản"
                          : "Sign Out"}
                      </div>
                      <div class="text-[10px] text-rose-400 font-normal">
                        {$language === "vi"
                          ? "Kết thúc phiên làm việc an toàn"
                          : "End current authenticated session"}
                      </div>
                    </div>
                  </button>
                </div>
              </div>
            {/if}
          </div>
        {/if}
      </div>
    </header>

    <!-- 3. MAIN DASHBOARD CONTENT AREA -->
    <!-- KHONG dat z-index o the boc nay. Neu dat (vd z-10) thi no tao ra mot
         stacking context rieng, khien moi modal/drawer cua trang con (fixed
         z-50 / z-[100]) bi "nhot" ben trong va nam DUOI header (z-20) va
         sidebar (z-30) -> phan dau modal bi thanh header che mat. -->
    <div class="relative flex-1 overflow-hidden flex flex-col">
      <main
        class="flex-1 overflow-y-scroll p-6 sm:p-8 space-y-6 bg-[#E0F1FF] dark:bg-[#1B2D40] transition-colors duration-300"
      >
        <!-- Greeting Banner -->
        {#if activeTab !== "settings" && activeTab !== "profile"}
          <div
            class="pb-2 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
          >
            <div>
              <h2
                class="text-xl sm:text-2xl font-bold text-[#0F1D2B] dark:text-white tracking-tight flex items-center gap-2"
              >
                <span>
                  {greeting}, {$currentUser?.name
                    ? $currentUser.name.split(" ")[0]
                    : "Admin"}
                </span>
                <span class="text-xl">👋</span>
              </h2>
              <div
                class="flex items-center space-x-1.5 text-xs text-[#537292] dark:text-[#8DB0D4] mt-1.5 font-medium"
              >
                <Clock
                  class="h-3.5 w-3.5 text-sky-600 dark:text-sky-400 shrink-0"
                />
                <span>{formattedDateTime}</span>
              </div>
            </div>

            <!-- Action buttons (Export + Primary Action) -->
            <div class="flex items-center space-x-2.5">
              <button
                onclick={exportAction?.onClick ||
                  (() =>
                    toast.success(
                      $language === "vi"
                        ? "Đang xuất báo cáo tổng quan..."
                        : "Exporting overview report...",
                    ))}
                class="flex items-center space-x-1.5 px-3.5 py-2 rounded-lg text-xs font-semibold bg-white dark:bg-[#1E3349] hover:bg-sky-50 dark:hover:bg-[#253E58] border border-[#CCE4F7] dark:border-[#253D56] text-[#1B2D40] dark:text-[#E0F1FF] shadow-xs transition active:scale-95"
              >
                <Download
                  class="h-3.5 w-3.5 text-[#537292] dark:text-[#8DB0D4]"
                />
                <span>{exportAction?.label || $t.actions.export}</span>
              </button>

              {#if primaryAction}
                <button
                  onclick={primaryAction.onClick}
                  class="flex items-center space-x-1.5 px-4 py-2 rounded-lg text-xs font-semibold bg-sky-600 hover:bg-sky-700 text-white shadow-xs transition active:scale-95"
                >
                  {#if primaryAction.icon}
                    {@const PrimaryIcon = primaryAction.icon}
                    <PrimaryIcon class="h-3.5 w-3.5" />
                  {:else}
                    <Plus class="h-3.5 w-3.5" />
                  {/if}
                  <span>{primaryAction.label}</span>
                </button>
              {:else}
                <button
                  onclick={() => toast.info($t.actions.newReport)}
                  class="flex items-center space-x-1.5 px-4 py-2 rounded-lg text-xs font-semibold bg-sky-600 hover:bg-sky-700 text-white shadow-xs transition active:scale-95"
                >
                  <Plus class="h-3.5 w-3.5" />
                  <span>{$t.actions.newReport}</span>
                </button>
              {/if}
            </div>
          </div>
        {/if}

        <!-- Page Tab Children -->
        {@render children()}
      </main>
    </div>
  </div>
</div>
