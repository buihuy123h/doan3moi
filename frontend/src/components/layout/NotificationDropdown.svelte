<script lang="ts">
  // NotificationDropdown.svelte — Chuông thông báo trên thanh header.
  //
  // Dữ liệu lấy từ notificationStore: lịch sử tải một lần bằng REST, còn tin mới do
  // backend đẩy xuống qua SignalR nên xuất hiện ngay (kèm toast) mà không cần polling.
  import { languageStore } from '../../context/LanguageContext';
  import { authStore } from '../../context/AuthContext';
  import { notificationStore } from '../../context/NotificationContext';
  import { navigateTo } from '../../lib/router';
  import type { RoleType } from '../../types/nexus';
  import type { NotificationDto } from '../../lib/api';
  import {
    Bell, CheckCheck, Sparkles, Receipt, AlertTriangle,
    HardDrive, RefreshCw, ArrowRight, ChevronRight,
    Headset, MessageSquare, Package, Wifi, WifiOff,
  } from 'lucide-svelte';
  import { toast } from 'svelte-sonner';

  const { language } = languageStore;
  const { currentUser } = authStore;
  const { notifications, unreadCount, status, isLoading } = notificationStore;

  let isOpen = $state(false);
  let activeFilter = $state<'all' | 'unread'>('all');

  // Nhãn thời gian ("5 phút trước") tự trôi: nhích đồng hồ mỗi 30 giây để tính lại.
  let now = $state(Date.now());

  $effect(() => {
    const timer = setInterval(() => (now = Date.now()), 30000);
    return () => clearInterval(timer);
  });

  const relativeTime = (iso: string, lang: string, reference: number): string => {
    const then = new Date(iso).getTime();
    if (Number.isNaN(then)) return '';

    const minutes = Math.floor(Math.max(0, reference - then) / 60000);
    if (minutes < 1) return lang === 'vi' ? 'Vừa xong' : 'Just now';
    if (minutes < 60) return lang === 'vi' ? `${minutes} phút trước` : `${minutes} mins ago`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return lang === 'vi' ? `${hours} giờ trước` : `${hours} hours ago`;

    const days = Math.floor(hours / 24);
    if (days < 7) return lang === 'vi' ? `${days} ngày trước` : `${days} days ago`;

    return new Date(iso).toLocaleDateString(lang === 'vi' ? 'vi-VN' : 'en-US');
  };

  const displayedNotifications = $derived(
    activeFilter === 'unread' ? $notifications.filter((n) => !n.read) : $notifications
  );

  const toggleDropdown = () => {
    isOpen = !isOpen;
  };

  const closeDropdown = () => {
    isOpen = false;
  };

  const markAllAsRead = async () => {
    await notificationStore.markAllRead();
    toast.success(
      $language === 'vi'
        ? 'Đã đánh dấu tất cả thông báo là đã đọc'
        : 'Marked all notifications as read'
    );
  };

  const handleClickItem = async (item: NotificationDto) => {
    // 1. Đánh dấu đã đọc (đồng bộ luôn sang các tab khác qua SignalR) + đóng dropdown
    void notificationStore.markRead(item.id);
    closeDropdown();

    // 2. Chuyển vai trò nếu thông báo thuộc dashboard khác, để ProtectedRoute cho qua.
    //    Thông báo của khách hàng (targetRole 'user') thì giữ nguyên phiên đang đăng nhập.
    const targetRole = item.targetRole as RoleType | null;
    if (targetRole && targetRole !== 'user' && $currentUser?.role !== targetRole) {
      authStore.loginAsStaff(targetRole);
    }

    // 3. Điều hướng — thông báo chat mở thẳng phiên chat tương ứng
    if (item.type === 'chat' && item.entityId) {
      navigateTo(item.targetPath, item.targetTab, { session: item.entityId });
    } else {
      navigateTo(item.targetPath, item.targetTab);
    }

    // 4. Toast phản hồi
    toast.success(
      $language === 'vi'
        ? `Đã chuyển tới: ${item.sectionVi}`
        : `Navigated to: ${item.sectionEn}`
    );
  };

  // Click outside listener
  const handleWindowClick = (e: MouseEvent) => {
    const target = e.target as HTMLElement | null;
    if (isOpen && target && !target.closest('.notification-container')) {
      isOpen = false;
    }
  };

  // Escape key listener
  const handleKeydown = (e: KeyboardEvent) => {
    if (e.key === 'Escape' && isOpen) {
      isOpen = false;
    }
  };
</script>

<svelte:window onclick={handleWindowClick} onkeydown={handleKeydown} />

<div class="relative notification-container">
  <!-- Bell Button Trigger -->
  <button
    type="button"
    onclick={toggleDropdown}
    class="h-9 w-9 rounded-full bg-[#EDF6FF] dark:bg-[#1E3349] hover:bg-[#DCEEFE] dark:hover:bg-[#253E58] text-[#1B2D40] dark:text-[#E0F1FF] border border-[#CCE4F7] dark:border-[#253D56] flex items-center justify-center transition shadow-xs relative cursor-pointer"
    title={$language === 'vi' ? `Thông báo (${$unreadCount} chưa đọc)` : `Notifications (${$unreadCount} unread)`}
    aria-expanded={isOpen}
  >
    <Bell class="h-4 w-4" />
    {#if $unreadCount > 0}
      <span class="absolute -top-1 -right-1 flex h-4 min-w-4 px-1 items-center justify-center rounded-full bg-rose-500 text-[10px] font-bold text-white ring-2 ring-white dark:ring-[#1E3349] animate-pulse">
        {$unreadCount}
      </span>
    {/if}
  </button>

  <!-- Droplist Popup Menu -->
  {#if isOpen}
    <div
      class="dropdown-popover absolute right-0 top-11 mt-1 w-80 sm:w-96 rounded-2xl bg-white dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] shadow-2xl z-50 overflow-hidden flex flex-col backdrop-blur-md"
    >
      <!-- Droplist Header -->
      <div class="px-4 py-3.5 border-b border-[#CCE4F7] dark:border-[#253D56] bg-slate-50/70 dark:bg-[#1B2D40]/50 flex items-center justify-between">
        <div class="flex items-center space-x-2">
          <h3 class="font-bold text-sm text-[#0F1D2B] dark:text-white">
            {$language === 'vi' ? 'Thông báo hệ thống' : 'System Notifications'}
          </h3>
          {#if $unreadCount > 0}
            <span class="px-2 py-0.5 rounded-full text-[11px] font-bold bg-rose-100 dark:bg-rose-950/80 text-rose-600 dark:text-rose-400 border border-rose-200 dark:border-rose-800">
              {$unreadCount} {$language === 'vi' ? 'mới' : 'new'}
            </span>
          {/if}
        </div>

        {#if $unreadCount > 0}
          <button
            type="button"
            onclick={markAllAsRead}
            class="text-[11px] font-semibold text-sky-600 dark:text-sky-400 hover:text-sky-700 dark:hover:text-sky-300 flex items-center space-x-1 transition active:scale-95 cursor-pointer"
          >
            <CheckCheck class="h-3.5 w-3.5" />
            <span>{$language === 'vi' ? 'Đã đọc tất cả' : 'Mark all read'}</span>
          </button>
        {/if}
      </div>

      <!-- Trạng thái kênh real-time: cho biết thông báo đang chảy về hay đã mất kết nối -->
      <div class="px-4 py-1.5 border-b border-[#CCE4F7] dark:border-[#253D56] bg-white dark:bg-[#152434] flex items-center gap-1.5">
        {#if $status === 'connected'}
          <Wifi class="h-3 w-3 text-emerald-500" />
          <span class="text-[12px] font-semibold text-emerald-600 dark:text-emerald-400">
            {$language === 'vi' ? 'Đang nhận thông báo trực tiếp' : 'Live updates on'}
          </span>
          <span class="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
        {:else if $status === 'connecting'}
          <RefreshCw class="h-3 w-3 text-amber-500 animate-spin" />
          <span class="text-[10px] font-semibold text-amber-600 dark:text-amber-400">
            {$language === 'vi' ? 'Đang kết nối máy chủ thông báo…' : 'Connecting to notification server…'}
          </span>
        {:else}
          <WifiOff class="h-3 w-3 text-slate-400" />
          <span class="text-[10px] font-semibold text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Mất kết nối trực tiếp — sẽ tự nối lại' : 'Live updates offline — retrying'}
          </span>
        {/if}
      </div>

      <!-- Filter Tabs -->
      <div class="flex border-b border-[#CCE4F7] dark:border-[#253D56] bg-white dark:bg-[#152434] text-xs font-semibold px-4 pt-2">
        <button
          type="button"
          onclick={() => (activeFilter = 'all')}
          class="pb-2 mr-4 border-b-2 transition cursor-pointer {activeFilter === 'all'
            ? 'border-sky-600 text-sky-600 dark:text-sky-400'
            : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'}"
        >
          {$language === 'vi' ? 'Tất cả' : 'All'} ({$notifications.length})
        </button>
        <button
          type="button"
          onclick={() => (activeFilter = 'unread')}
          class="pb-2 border-b-2 transition cursor-pointer {activeFilter === 'unread'
            ? 'border-sky-600 text-sky-600 dark:text-sky-400'
            : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'}"
        >
          {$language === 'vi' ? 'Chưa đọc' : 'Unread'} ({$unreadCount})
        </button>
      </div>

      <!-- Notification Items List (Scrollable) -->
      <div class="max-h-80 overflow-y-auto divide-y divide-slate-100 dark:divide-slate-800/60 notification-scroll">
        {#if $isLoading && $notifications.length === 0}
          <div class="py-10 text-center px-4 space-y-2">
            <RefreshCw class="h-7 w-7 mx-auto text-slate-300 dark:text-slate-600 animate-spin" />
            <p class="text-xs text-slate-500 dark:text-slate-400">
              {$language === 'vi' ? 'Đang tải thông báo…' : 'Loading notifications…'}
            </p>
          </div>
        {:else if displayedNotifications.length === 0}
          <div class="py-10 text-center px-4 space-y-2">
            <Bell class="h-8 w-8 mx-auto text-slate-300 dark:text-slate-600" />
            <p class="text-xs text-slate-500 dark:text-slate-400">
              {$language === 'vi'
                ? 'Chưa có thông báo nào. Tin mới sẽ hiện ở đây ngay khi phát sinh.'
                : 'No notifications yet. New ones appear here the moment they happen.'}
            </p>
          </div>
        {:else}
          {#each displayedNotifications as item (item.id)}
            <button
              type="button"
              onclick={() => handleClickItem(item)}
              class="w-full text-left p-3.5 hover:bg-sky-50/50 dark:hover:bg-slate-800/50 transition flex items-start space-x-3 group relative cursor-pointer {!item.read
                ? 'bg-sky-50/20 dark:bg-sky-950/10'
                : ''}"
            >
              <!-- Icon by Type -->
              <div class="shrink-0 mt-0.5">
                {#if item.type === 'order'}
                  <div class="h-8 w-8 rounded-lg bg-sky-100 dark:bg-sky-900/40 text-sky-600 dark:text-sky-400 flex items-center justify-center shadow-xs">
                    <Sparkles class="h-4 w-4" />
                  </div>
                {:else if item.type === 'billing'}
                  <div class="h-8 w-8 rounded-lg bg-emerald-100 dark:bg-emerald-900/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shadow-xs">
                    <Receipt class="h-4 w-4" />
                  </div>
                {:else if item.type === 'alert'}
                  <div class="h-8 w-8 rounded-lg bg-rose-100 dark:bg-rose-900/40 text-rose-600 dark:text-rose-400 flex items-center justify-center shadow-xs">
                    <AlertTriangle class="h-4 w-4" />
                  </div>
                {:else if item.type === 'stock'}
                  <!-- Vật tư thiếu cần bổ sung -->
                  <div class="h-8 w-8 rounded-lg bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 flex items-center justify-center shadow-xs">
                    <Package class="h-4 w-4" />
                  </div>
                {:else if item.type === 'chat'}
                  <!-- Tin nhắn chat trực tuyến từ khách -->
                  <div class="h-8 w-8 rounded-lg bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shadow-xs">
                    <Headset class="h-4 w-4" />
                  </div>
                {:else if item.type === 'feedback'}
                  <!-- Phản hồi khách hàng chờ trả lời -->
                  <div class="h-8 w-8 rounded-lg bg-violet-100 dark:bg-violet-900/40 text-violet-600 dark:text-violet-400 flex items-center justify-center shadow-xs">
                    <MessageSquare class="h-4 w-4" />
                  </div>
                {:else if item.type === 'hardware'}
                  <div class="h-8 w-8 rounded-lg bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 flex items-center justify-center shadow-xs">
                    <HardDrive class="h-4 w-4" />
                  </div>
                {:else}
                  <div class="h-8 w-8 rounded-lg bg-purple-100 dark:bg-purple-900/40 text-purple-600 dark:text-purple-400 flex items-center justify-center shadow-xs">
                    <RefreshCw class="h-4 w-4" />
                  </div>
                {/if}
              </div>

              <!-- Text Content -->
              <div class="flex-1 min-w-0 pr-4">
                <div class="flex items-center justify-between">
                  <h4 class="text-xs font-bold text-slate-900 dark:text-white truncate group-hover:text-sky-600 dark:group-hover:text-sky-400 transition-colors">
                    {$language === 'vi' ? item.titleVi : item.titleEn}
                  </h4>
                  <span class="text-[10px] text-slate-400 whitespace-nowrap ml-2">
                    {relativeTime(item.createdAt, $language, now)}
                  </span>
                </div>
                <p class="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-2 leading-relaxed">
                  {$language === 'vi' ? item.descVi : item.descEn}
                </p>

                <!-- Target section link preview -->
                <div class="mt-2 flex items-center justify-between">
                  <span class="inline-flex items-center text-[10px] font-semibold text-sky-700 dark:text-sky-300 bg-sky-100/70 dark:bg-sky-950/60 px-2 py-0.5 rounded-md border border-sky-200 dark:border-sky-800/60">
                    {$language === 'vi' ? item.sectionVi : item.sectionEn}
                  </span>
                  <span class="text-[10px] font-semibold text-sky-600 dark:text-sky-400 flex items-center group-hover:translate-x-0.5 transition-transform">
                    <span>{$language === 'vi' ? 'Xem mục này' : 'View section'}</span>
                    <ChevronRight class="h-3 w-3 ml-0.5" />
                  </span>
                </div>
              </div>

              <!-- Unread Blue Dot -->
              {#if !item.read}
                <span class="absolute right-3 top-4 h-2 w-2 rounded-full bg-sky-500 ring-2 ring-sky-200 dark:ring-sky-900"></span>
              {/if}
            </button>
          {/each}
        {/if}
      </div>

      <!-- Droplist Footer -->
      <div class="px-4 py-2.5 bg-slate-50 dark:bg-[#1B2D40]/70 border-t border-[#CCE4F7] dark:border-[#253D56] text-center">
        <button
          type="button"
          onclick={async () => {
            await notificationStore.refresh();
            toast.info(
              $language === 'vi'
                ? 'Đã tải lại danh sách thông báo'
                : 'Notification list reloaded'
            );
          }}
          class="text-xs font-bold text-sky-600 dark:text-sky-400 hover:text-sky-700 dark:hover:text-sky-300 inline-flex items-center space-x-1.5 transition cursor-pointer"
        >
          <span>{$language === 'vi' ? 'Tải lại thông báo' : 'Reload notifications'}</span>
          <ArrowRight class="h-3 w-3" />
        </button>
      </div>
    </div>
  {/if}
</div>
