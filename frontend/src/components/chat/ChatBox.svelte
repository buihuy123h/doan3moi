<script lang="ts">
  // ChatBox.svelte — Chatbox nổi ở góc phải màn hình (trang index),
  // cho phép khách (kể cả chưa đăng nhập) mở ô chat trực tiếp với Admin.
  // Phiên chat được lưu vào CSDL qua API /api/chat/* và định danh bằng
  // SessionID giữ trong localStorage nên khách quay lại vẫn xem lại lịch sử.
  import { languageStore } from '../../context/LanguageContext';
  import { authStore } from '../../context/AuthContext';
  import { toast } from 'svelte-sonner';
  import { MessageCircle, X, Send, Headset } from 'lucide-svelte';
  import {
    startChatSessionApi,
    fetchChatMessagesApi,
    sendChatMessageApi,
    ApiError,
    type ChatMessageDto,
  } from '../../lib/api';
  import {
    joinGroup,
    leaveGroup,
    onRealtime,
    realtimeStatus,
    RealtimeEvents,
  } from '../../lib/realtime';

  const { language } = languageStore;
  const { currentUser } = authStore;

  const SESSION_KEY = 'nexus_chat_session_id';
  const NAME_KEY = 'nexus_chat_customer_name';
  // Chỉ dùng khi kênh real-time bị rớt; bình thường tin nhắn được đẩy xuống ngay.
  const FALLBACK_POLL_MS = 6000;

  // Trạng thái hiển thị của chatbox
  let isOpen = $state(false);
  let hasUnreadGreeting = $state(false);

  // Phiên chat hiện tại của khách
  let sessionId = $state<string | null>(null);
  let customerName = $state('');
  let nameInput = $state('');

  // Tin nhắn & ô nhập
  let messages = $state<ChatMessageDto[]>([]);
  let draft = $state('');
  let isSending = $state(false);
  let isLoadingHistory = $state(false);
  let messagesContainer = $state<HTMLDivElement | null>(null);

  // Khôi phục phiên chat cũ từ localStorage
  $effect(() => {
    const savedSession = localStorage.getItem(SESSION_KEY);
    const savedName = localStorage.getItem(NAME_KEY);
    if (savedSession) {
      sessionId = savedSession;
      customerName = savedName ?? 'Khách';
    } else if ($currentUser) {
      // Khách đã đăng nhập thì tự điền sẵn tên cho tiện
      nameInput = $currentUser.name;
    }
  });

  // Nghe tin nhắn của phiên này qua SignalR — chạy cả khi hộp chat đang đóng
  // để hiện chấm đỏ báo Admin vừa trả lời.
  $effect(() => {
    if (!sessionId) return;
    const id = sessionId;

    void joinGroup('chat', id);

    const off = onRealtime<ChatMessageDto>(RealtimeEvents.chatMessage, (incoming) => {
      if (incoming.sessionId !== id) return;
      // Tin khách vừa gửi đã được thêm ngay lúc bấm gửi.
      if (messages.some((m) => m.id === incoming.id)) return;

      messages = [...messages, incoming];

      if (incoming.senderType === 'admin' && !isOpen) hasUnreadGreeting = true;

      requestAnimationFrame(() => {
        messagesContainer?.scrollTo({ top: messagesContainer.scrollHeight, behavior: 'smooth' });
      });
    });

    return () => {
      off();
      void leaveGroup('chat', id);
    };
  });

  // Tải lịch sử khi mở hộp chat. Chỉ hỏi lại server định kỳ nếu kênh real-time đang rớt.
  $effect(() => {
    if (!isOpen || !sessionId) return;

    let cancelled = false;
    const isLive = $realtimeStatus === 'connected';

    const reload = async () => {
      try {
        const list = await fetchChatMessagesApi(sessionId!);
        if (cancelled) return;
        const previousCount = messages.length;
        messages = list;
        // Tự cuộn xuống cuối khi có tin nhắn mới
        if (list.length !== previousCount) {
          requestAnimationFrame(() => {
            messagesContainer?.scrollTo({ top: messagesContainer.scrollHeight, behavior: 'smooth' });
          });
        }
      } catch (err) {
        // Phiên lưu trong localStorage không còn trên server (CSDL chat đã bị xoá,
        // hoặc phiên quá cũ) -> bỏ phiên hỏng đi để khách nhập tên chat lại từ đầu.
        if (err instanceof ApiError && err.status === 404) {
          localStorage.removeItem(SESSION_KEY);
          sessionId = null;
          messages = [];
          return;
        }
        // Mất kết nối tạm thời thì để lần sau thử lại
      }
    };

    isLoadingHistory = true;
    reload().finally(() => (isLoadingHistory = false));

    if (isLive) return () => { cancelled = true; };

    const timer = setInterval(reload, FALLBACK_POLL_MS);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  });

  const openChat = () => {
    isOpen = true;
    hasUnreadGreeting = false;
  };

  const closeChat = () => {
    isOpen = false;
  };

  // Khách bắt đầu phiên chat: lưu SessionID + tên vào localStorage
  const handleStartSession = async () => {
    const name = nameInput.trim();
    if (!name) {
      toast.error($language === 'vi' ? 'Vui lòng nhập tên của bạn.' : 'Please enter your name.');
      return;
    }
    try {
      isSending = true;
      const session = await startChatSessionApi(name);
      sessionId = session.sessionId;
      customerName = name;
      localStorage.setItem(SESSION_KEY, session.sessionId);
      localStorage.setItem(NAME_KEY, name);
      const greeting = await fetchChatMessagesApi(session.sessionId);
      messages = greeting;
      requestAnimationFrame(() => {
        messagesContainer?.scrollTo({ top: messagesContainer.scrollHeight });
      });
      toast.success($language === 'vi' ? 'Đã kết nối với Admin.' : 'Connected with Admin.');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Không thể bắt đầu phiên chat.');
    } finally {
      isSending = false;
    }
  };

  const handleSendMessage = async () => {
    const text = draft.trim();
    if (!text || !sessionId || isSending) return;
    try {
      isSending = true;
      const sent = await sendChatMessageApi(sessionId, customerName, text);
      messages = [...messages, sent];
      draft = '';
      requestAnimationFrame(() => {
        messagesContainer?.scrollTo({ top: messagesContainer.scrollHeight, behavior: 'smooth' });
      });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Không gửi được tin nhắn.');
    } finally {
      isSending = false;
    }
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      handleSendMessage();
    }
  };

  // Lưu ý text theo ngôn ngữ
  const t = $derived({
    bubbleTitle: $language === 'vi' ? 'Chat với Admin' : 'Chat with Admin',
    panelTitle: $language === 'vi' ? 'Hỗ trợ Nexus' : 'Nexus Support',
    online: $language === 'vi' ? 'Đang hoạt động' : 'Online',
    welcomeTitle: $language === 'vi' ? 'Xin chào 👋' : 'Hello 👋',
    welcomeText: $language === 'vi'
      ? 'Nhập tên của bạn để bắt đầu trò chuyện với Admin Nexus Telecom.'
      : 'Enter your name to start chatting with the Nexus Telecom Admin.',
    nameLabel: $language === 'vi' ? 'Tên của bạn' : 'Your name',
    namePlaceholder: $language === 'vi' ? 'VD: Minh Anh' : 'e.g. Alex',
    startBtn: $language === 'vi' ? 'Bắt đầu chat' : 'Start chat',
    inputPlaceholder: $language === 'vi' ? 'Nhập tin nhắn…' : 'Type a message…',
    loading: $language === 'vi' ? 'Đang tải tin nhắn…' : 'Loading messages…',
    emptyChat: $language === 'vi' ? 'Hãy gửi tin nhắn đầu tiên!' : 'Send your first message!',
    endSession: $language === 'vi' ? 'Kết thúc phiên' : 'End session',
  });
</script>

<!-- Bong bóng chat nổi góc phải -->
{#if !isOpen}
  <button
    type="button"
    onclick={openChat}
    class="fixed bottom-6 right-6 z-50 h-14 w-14 rounded-full bg-gradient-to-tr from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white shadow-xl shadow-sky-600/40 flex items-center justify-center transition-all duration-200 active:scale-95 cursor-pointer group"
    title={t.bubbleTitle}
    aria-label={t.bubbleTitle}
  >
    <MessageCircle class="h-6 w-6 group-hover:scale-110 transition-transform" />
    {#if hasUnreadGreeting}
      <span class="absolute -top-0.5 -right-0.5 h-3.5 w-3.5 rounded-full bg-rose-500 border-2 border-white dark:border-[#1B2D40]"></span>
    {/if}
  </button>
{/if}

<!-- Hộp chat mở ra -->
{#if isOpen}
  <div
    class="fixed bottom-6 right-6 z-50 w-[calc(100vw-3rem)] sm:w-[370px] rounded-2xl bg-white dark:bg-[#1E3349] border border-[#CCE4F7] dark:border-[#253D56] shadow-2xl shadow-sky-900/20 overflow-hidden flex flex-col transition-colors duration-300"
    role="dialog"
    aria-label={t.bubbleTitle}
  >
    <!-- Header -->
    <div class="bg-gradient-to-r from-sky-600 to-blue-600 px-4 py-3 flex items-center justify-between shrink-0">
      <div class="flex items-center space-x-3">
        <div class="h-9 w-9 rounded-full bg-white/20 flex items-center justify-center text-white shrink-0">
          <Headset class="h-5 w-5" />
        </div>
        <div>
          <div class="text-white font-bold text-sm leading-tight">{t.panelTitle}</div>
          <div class="flex items-center gap-1.5 text-[11px] text-sky-100">
            <span class="h-1.5 w-1.5 rounded-full bg-emerald-300 animate-pulse"></span>
            <span>{t.online}</span>
          </div>
        </div>
      </div>
      <div class="flex items-center gap-1">
        {#if sessionId}
          <button
            type="button"
            onclick={() => {
              // Kết thúc phiên: xóa phiên hiện tại, lần sau hỏi lại tên
              localStorage.removeItem(SESSION_KEY);
              localStorage.removeItem(NAME_KEY);
              sessionId = null;
              customerName = '';
              nameInput = '';
              messages = [];
            }}
            class="text-[11px] font-semibold text-sky-100 hover:text-white hover:bg-white/10 rounded-md px-2 py-1 transition cursor-pointer border-0 bg-transparent"
            title={t.endSession}
          >
            {t.endSession}
          </button>
        {/if}
        <button
          type="button"
          onclick={closeChat}
          class="h-8 w-8 rounded-lg text-white hover:bg-white/15 flex items-center justify-center transition cursor-pointer border-0 bg-transparent"
          aria-label="Đóng"
        >
          <X class="h-4.5 w-4.5" />
        </button>
      </div>
    </div>

    <!-- Chưa có phiên: form nhập tên -->
    {#if !sessionId}
      <div class="p-5 space-y-4">
        <div class="text-center">
          <div class="text-lg font-extrabold text-sky-950 dark:text-white">{t.welcomeTitle}</div>
          <p class="text-xs text-[#537292] dark:text-[#8DB0D4] mt-1.5 leading-relaxed">{t.welcomeText}</p>
        </div>
        <label class="block space-y-1.5">
          <span class="text-xs font-bold text-slate-700 dark:text-slate-200">{t.nameLabel}</span>
          <input
            type="text"
            bind:value={nameInput}
            onkeydown={(e) => e.key === 'Enter' && handleStartSession()}
            placeholder={t.namePlaceholder}
            maxlength="100"
            class="w-full px-3.5 py-2.5 rounded-xl text-sm bg-[#F3F9FF] dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] text-[#1B2D40] dark:text-[#E0F1FF] placeholder:text-[#8FAAC2] focus:outline-none focus:ring-2 focus:ring-sky-500"
          />
        </label>
        <button
          type="button"
          onclick={handleStartSession}
          disabled={isSending}
          class="w-full py-2.5 rounded-xl font-bold text-xs bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white shadow-md shadow-sky-600/25 transition active:scale-95 disabled:opacity-60 cursor-pointer"
        >
          {isSending ? ($language === 'vi' ? 'Đang kết nối…' : 'Connecting…') : t.startBtn}
        </button>
      </div>
    {:else}
      <!-- Có phiên: khung tin nhắn -->
      <div
        bind:this={messagesContainer}
        class="h-[320px] overflow-y-auto px-4 py-4 space-y-3 bg-[#F3F9FF] dark:bg-[#152434] chatbox-scrollbar"
      >
        {#if isLoadingHistory}
          <div class="text-center text-xs text-[#537292] dark:text-[#8DB0D4] py-8">{t.loading}</div>
        {:else if messages.length === 0}
          <div class="text-center text-xs text-[#537292] dark:text-[#8DB0D4] py-8">{t.emptyChat}</div>
        {:else}
          {#each messages as msg (msg.id)}
            {#if msg.senderType === 'admin'}
              <!-- Tin nhắn của Admin (bên trái) -->
              <div class="flex items-end gap-2">
                <div class="h-7 w-7 rounded-full bg-gradient-to-tr from-sky-600 to-blue-600 text-white flex items-center justify-center shrink-0 text-[10px] font-bold">
                  A
                </div>
                <div class="max-w-[78%]">
                  <div class="px-3.5 py-2.5 rounded-2xl rounded-bl-md bg-white dark:bg-[#1E3349] border border-[#CCE4F7] dark:border-[#253D56] text-[#1B2D40] dark:text-[#E0F1FF] text-[13px] leading-relaxed shadow-xs">
                    {msg.message}
                  </div>
                  <div class="text-[10px] text-[#8FAAC2] dark:text-[#5E7B99] mt-1 ml-1">
                    {msg.senderName} · {msg.createdAtDisplay}
                  </div>
                </div>
              </div>
            {:else}
              <!-- Tin nhắn của khách (bên phải) -->
              <div class="flex justify-end">
                <div class="max-w-[78%]">
                  <div class="px-3.5 py-2.5 rounded-2xl rounded-br-md bg-sky-600 text-white text-[13px] leading-relaxed shadow-md shadow-sky-600/20">
                    {msg.message}
                  </div>
                  <div class="text-[10px] text-[#8FAAC2] dark:text-[#5E7B99] mt-1 mr-1 text-right">
                    {msg.createdAtDisplay}
                  </div>
                </div>
              </div>
            {/if}
          {/each}
        {/if}
      </div>

      <!-- Ô nhập tin nhắn -->
      <div class="p-3 border-t border-[#CCE4F7] dark:border-[#253D56] flex items-center gap-2 bg-white dark:bg-[#1E3349] shrink-0">
        <input
          type="text"
          bind:value={draft}
          onkeydown={handleKeyDown}
          placeholder={t.inputPlaceholder}
          maxlength="1000"
          class="flex-1 px-3.5 py-2.5 rounded-xl text-[13px] bg-[#F3F9FF] dark:bg-[#152434] border border-[#CCE4F7] dark:border-[#253D56] text-[#1B2D40] dark:text-[#E0F1FF] placeholder:text-[#8FAAC2] focus:outline-none focus:ring-2 focus:ring-sky-500"
        />
        <button
          type="button"
          onclick={handleSendMessage}
          disabled={isSending || !draft.trim()}
          class="h-10 w-10 rounded-xl bg-gradient-to-tr from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white shadow-md shadow-sky-600/25 flex items-center justify-center transition active:scale-95 disabled:opacity-40 disabled:pointer-events-none cursor-pointer shrink-0"
          aria-label={$language === 'vi' ? 'Gửi' : 'Send'}
        >
          <Send class="h-4 w-4" />
        </button>
      </div>
    {/if}
  </div>
{/if}

<style>
  .chatbox-scrollbar {
    scrollbar-width: thin;
    scrollbar-color: #7cb8e4 transparent;
  }
  .chatbox-scrollbar::-webkit-scrollbar {
    width: 6px;
  }
  .chatbox-scrollbar::-webkit-scrollbar-track {
    background: transparent;
  }
  .chatbox-scrollbar::-webkit-scrollbar-thumb {
    background: #7cb8e4;
    border-radius: 9999px;
  }
  :global(.dark) .chatbox-scrollbar::-webkit-scrollbar-thumb {
    background: #2c4a68;
  }
</style>
