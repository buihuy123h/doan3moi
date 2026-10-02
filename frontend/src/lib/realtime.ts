// realtime.ts — Một kết nối SignalR duy nhất dùng chung cho cả ứng dụng.
//
// Trước đây mọi thứ đều "hỏi lại server theo giờ": thông báo 15 giây/lần, chat 4 giây/lần.
// Nay backend chủ động đẩy sự kiện xuống qua WebSocket (hub /hubs/notifications), nên
// tin nhắn và thông báo hiện ra gần như tức thì mà không tốn request rỗng.
//
// Module này chỉ lo phần "đường ống": giữ kết nối, tự nối lại khi rớt mạng, và cho các
// component đăng ký nghe sự kiện. Việc hiển thị do NotificationContext và các trang lo.
import * as signalR from '@microsoft/signalr';
import { writable, type Readable } from 'svelte/store';
import { API_BASE_URL } from './api';

export type RealtimeStatus = 'disconnected' | 'connecting' | 'connected';

/** Tên sự kiện server đẩy xuống — phải khớp hằng số trong Hubs/NotificationHub.cs. */
export const RealtimeEvents = {
  notification: 'notification',
  notificationRead: 'notificationRead',
  notificationsRead: 'notificationsRead',
  chatMessage: 'chatMessage',
  chatSession: 'chatSession',
  dataChanged: 'dataChanged',
} as const;

export type RealtimeEvent = (typeof RealtimeEvents)[keyof typeof RealtimeEvents];

/** Loại group mà client có thể xin vào. */
export type GroupKind = 'role' | 'account' | 'chat';

const statusStore = writable<RealtimeStatus>('disconnected');

/** Trạng thái kết nối, để giao diện hiện chấm "Trực tiếp / Mất kết nối". */
export const realtimeStatus: Readable<RealtimeStatus> = { subscribe: statusStore.subscribe };

// Các hàm gọi lên hub, tra theo loại group.
const HUB_METHODS: Record<GroupKind, { join: string; leave: string }> = {
  role: { join: 'JoinRole', leave: 'LeaveRole' },
  account: { join: 'JoinAccount', leave: 'LeaveAccount' },
  chat: { join: 'JoinChatSession', leave: 'LeaveChatSession' },
};

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;
let retryTimer: ReturnType<typeof setTimeout> | null = null;
let retryDelayMs = 2000;

// Những group đang tham gia. Giữ lại để nối lại sau khi mất mạng thì tự vào lại,
// nếu không người dùng sẽ "im lặng" dù thanh trạng thái báo đã kết nối.
const joinedGroups = new Set<string>();

// Danh sách hàm xử lý theo từng tên sự kiện.
const handlers = new Map<string, Set<(payload: any) => void>>();

const groupKey = (kind: GroupKind, value: string) => `${kind}:${value}`;

function createConnection(): signalR.HubConnection {
  const built = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/notifications`)
    // Rớt mạng thì thử lại ngay, rồi giãn dần để không đập liên tục vào server.
    .withAutomaticReconnect([0, 2000, 5000, 10000, 20000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  // Chỉ đăng ký MỘT lần cho mỗi tên sự kiện, sau đó phát lại cho các hàm đã đăng ký.
  for (const event of Object.values(RealtimeEvents)) {
    built.on(event, (payload: unknown) => dispatch(event, payload));
  }

  built.onreconnecting(() => statusStore.set('connecting'));

  built.onreconnected(async () => {
    statusStore.set('connected');
    await rejoinGroups();
  });

  built.onclose(() => {
    statusStore.set('disconnected');
    scheduleRetry();
  });

  return built;
}

function dispatch(event: string, payload: unknown) {
  const set = handlers.get(event);
  if (!set) return;

  for (const handler of set) {
    try {
      handler(payload);
    } catch (err) {
      // Một component lỗi không được làm chết các component còn lại.
      console.warn(`[realtime] Lỗi khi xử lý sự kiện ${event}:`, err);
    }
  }
}

async function rejoinGroups() {
  if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;

  for (const key of joinedGroups) {
    const separator = key.indexOf(':');
    const kind = key.slice(0, separator) as GroupKind;
    const value = key.slice(separator + 1);
    try {
      await connection.invoke(HUB_METHODS[kind].join, value);
    } catch {
      // Lần nối lại sau sẽ thử tiếp.
    }
  }
}

function scheduleRetry() {
  if (retryTimer) return;

  retryTimer = setTimeout(() => {
    retryTimer = null;
    // Giãn dần tối đa 30 giây: backend đang tắt thì không nên thử lại mỗi giây.
    retryDelayMs = Math.min(retryDelayMs * 2, 30000);
    void startRealtime();
  }, retryDelayMs);
}

/**
 * Mở kết nối (gọi bao nhiêu lần cũng được, chỉ mở một lần).
 * Backend chưa chạy thì im lặng thử lại — giao diện vẫn hoạt động bình thường
 * với dữ liệu tải bằng REST, chỉ là không có cập nhật tức thì.
 */
export async function startRealtime(): Promise<void> {
  if (typeof window === 'undefined') return;

  if (connection?.state === signalR.HubConnectionState.Connected) return;
  if (startPromise) return startPromise;

  connection ??= createConnection();
  statusStore.set('connecting');

  startPromise = connection
    .start()
    .then(async () => {
      statusStore.set('connected');
      retryDelayMs = 2000;
      await rejoinGroups();
    })
    .catch((err) => {
      statusStore.set('disconnected');
      console.warn('[realtime] Chưa kết nối được máy chủ thông báo:', err?.message ?? err);
      scheduleRetry();
    })
    .finally(() => {
      startPromise = null;
    });

  return startPromise;
}

/** Đăng ký nghe một sự kiện. Trả về hàm huỷ đăng ký để gọi trong cleanup của $effect. */
export function onRealtime<T = unknown>(
  event: RealtimeEvent,
  handler: (payload: T) => void
): () => void {
  const set = handlers.get(event) ?? new Set();
  set.add(handler as (payload: any) => void);
  handlers.set(event, set);

  return () => {
    set.delete(handler as (payload: any) => void);
  };
}

/** Vào một group để bắt đầu nhận sự kiện của group đó. */
export async function joinGroup(kind: GroupKind, value: string): Promise<void> {
  if (!value) return;

  const key = groupKey(kind, value);
  if (joinedGroups.has(key)) return;
  joinedGroups.add(key);

  await startRealtime();

  if (connection?.state !== signalR.HubConnectionState.Connected) return;

  try {
    await connection.invoke(HUB_METHODS[kind].join, value);
  } catch (err) {
    console.warn(`[realtime] Không vào được nhóm ${key}:`, err);
  }
}

/** Rời group (VD: nhân viên đăng xuất, hoặc khách đóng phiên chat). */
export async function leaveGroup(kind: GroupKind, value: string): Promise<void> {
  if (!value) return;

  const key = groupKey(kind, value);
  if (!joinedGroups.delete(key)) return;

  if (connection?.state !== signalR.HubConnectionState.Connected) return;

  try {
    await connection.invoke(HUB_METHODS[kind].leave, value);
  } catch {
    // Đang mất kết nối thì thôi, group cũng đã bị server dọn khi ngắt.
  }
}
