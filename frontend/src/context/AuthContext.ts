import { writable, derived, get } from 'svelte/store';
import { roleTypeFromJobTitle } from '../types/nexus';
import type { RoleType } from '../types/nexus';
import { nexusStore, accountIdKey } from './NexusContext';
import { languageStore } from './LanguageContext';
import { loginRequest, requestLoginOtpApi, verifyLoginOtpApi, updateProfileApi, updatePasswordApi, ApiError } from '../lib/api';

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: RoleType;
  title: string;
  department: string;
  // Customer-only fields (populated when signing in with an Account ID)
  accountId?: string;
  orderId?: string;
  branchCode?: string;
  planName?: string;
  // Personal profile fields
  phone?: string;
  address?: string;
  avatar?: string;
  gender?: 'male' | 'female' | 'other';
  dateOfBirth?: string;
  dateOfJoining?: string;
  bio?: string;
}

// Staff accounts are internal: they are picked by role, no email/password prompt.
export interface StaffAccount {
  user: AuthUser;
}

export const STAFF_ACCOUNTS: StaffAccount[] = [
  {
    user: {
      id: 'emp-01',
      name: 'Sarah Jenkins',
      email: 'sarah.jenkins@nexus.telecom',
      role: 'admin',
      title: 'General Manager',
      department: 'Executive Administration',
      phone: '+1 (555) 234-8901',
      address: '742 Evergreen Terrace, NY',
      gender: 'female',
      dateOfBirth: '1988-04-12',
      dateOfJoining: '2022-03-15',
      bio: 'Senior Operations Manager leading overall telecommunication infrastructure and store network.',
    },
  },
  {
    user: {
      id: 'emp-02',
      name: 'David Chen',
      email: 'david.chen@nexus.telecom',
      role: 'retail',
      title: 'Store Representative',
      department: 'Retail Outlets (SH-01 Flagship)',
      phone: '+1 (555) 456-1123',
      address: '120 Broadway, Manhattan, NY',
      gender: 'male',
      dateOfBirth: '1992-09-25',
      dateOfJoining: '2023-06-10',
      bio: 'Flagship Store lead customer representative specialized in corporate bulk schemes.',
    },
  },
  {
    user: {
      id: 'emp-03',
      name: 'Marcus Ramirez',
      email: 'marcus.ramirez@nexus.telecom',
      role: 'technical',
      title: 'Field Operations Engineer',
      department: 'Technical Operations & Field Ops',
      phone: '+1 (555) 789-3344',
      address: '88 Bedford Ave, Brooklyn, NY',
      gender: 'male',
      dateOfBirth: '1990-12-05',
      dateOfJoining: '2021-11-04',
      bio: 'Lead technical supervisor handling fiber splicing, line attenuation testing, and DP box routing.',
    },
  },
  {
    user: {
      id: 'emp-04',
      name: 'Elena Rostova',
      email: 'elena.rostova@nexus.telecom',
      role: 'accounts',
      title: 'Senior Accountant',
      department: 'Finance & Accounts',
      phone: '+1 (555) 901-5567',
      address: '45 Wall Street, Suite 900, NY',
      gender: 'female',
      dateOfBirth: '1985-02-18',
      dateOfJoining: '2020-08-20',
      bio: 'Head of Billing and Financial Accounts handling ledger audits, invoices, and service tax returns.',
    },
  },
];

// ---- Account ID helpers ----
// Customers sign in with the 16-char Account ID issued once the line is feasible.
// Layout per spec: [type letter D/B/T][3-digit city code][12-digit serial].
export function normalizeAccountId(raw: string): string {
  const s = (raw || '').toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 16);
  return s.length > 4 ? `${s.slice(0, 4)}-${s.slice(4)}` : s;
}

export function isCompleteAccountId(raw: string): boolean {
  return (raw || '').toUpperCase().replace(/[^A-Z0-9]/g, '').length === 16;
}

// ---- Stores ----
function createAuthStore() {
  const savedUser = null; // Theo yêu cầu: mặc định khi mới vào trang là chưa đăng nhập

  const currentUser = writable<AuthUser | null>(savedUser);

  // Persist current user
  currentUser.subscribe((u) => {
    if (u) localStorage.setItem('nexus_auth_user', JSON.stringify(u));
    else localStorage.removeItem('nexus_auth_user');
  });

  const isAuthenticated = derived(currentUser, (u) => !!u);

  // Builds the customer session from whatever the Account ID is attached to:
  // a provisioned connection first, otherwise the order that reserved the ID.
  const buildCustomerUser = (accountId: string): AuthUser | null => {
    const key = accountIdKey(accountId);
    const connection = get(nexusStore.connections).find((c) => accountIdKey(c.accountId) === key);
    const order = get(nexusStore.orders).find(
      (o) => o.assignedAccountId && accountIdKey(o.assignedAccountId) === key
    );
    if (!connection && !order) return null;

    const shopCode = order?.retailOutletCode;
    const shop = get(nexusStore.retailShops).find((s) => s.shopCode === shopCode);

    return {
      id: accountId,
      name: connection?.customerName || order!.customerName,
      email: connection?.customerEmail || order!.customerEmail,
      role: 'user',
      title: 'Customer',
      department: shop ? `${shop.name} (${shop.shopCode})` : 'Subscribers',
      accountId,
      orderId: connection?.orderId || order?.id,
      branchCode: shopCode,
      planName: connection?.planName || order?.planName,
    };
  };

  // Customer sign-in: Account ID only, issued at purchase time.
  /**
   * Bước 1 đăng nhập khách hàng: gửi mã tài khoản, backend gửi OTP về email đã đăng ký.
   * Trả về email đã che bớt để giao diện hiện "Mã OTP đã gửi tới sa*****@gmail.com".
   */
  const requestLoginOtp = async (rawAccountId: string) => {
    const t = get(languageStore.t);

    if (!isCompleteAccountId(rawAccountId)) {
      return { success: false as const, error: t.auth.accountIdRequired };
    }

    try {
      const res = await requestLoginOtpApi(normalizeAccountId(rawAccountId));
      return {
        success: true as const,
        maskedEmail: res.maskedEmail,
        expiresInSeconds: res.expiresInSeconds,
        resendAfterSeconds: res.resendAfterSeconds,
      };
    } catch (e) {
      if (e instanceof ApiError) {
        if (e.message === 'network') {
          return { success: false as const, error: t.auth.serverUnreachable };
        }
        return { success: false as const, error: e.message };
      }
      return { success: false as const, error: t.auth.loginFailed };
    }
  };

  /**
   * Bước 2: gửi OTP khách nhập lên backend. Đúng thì lưu JWT và đăng nhập.
   * Thông tin hiển thị ưu tiên lấy từ store cục bộ (có tên cửa hàng, gói cước),
   * thiếu thì dùng dữ liệu tối thiểu mà API trả về.
   */
  const loginWithOtp = async (rawAccountId: string, otp: string) => {
    const t = get(languageStore.t);

    try {
      const res = await verifyLoginOtpApi(normalizeAccountId(rawAccountId), otp.trim());

      if (res.token) {
        localStorage.setItem('nexus_jwt_token', res.token);
      }

      const accountId = normalizeAccountId(res.user.accountId || rawAccountId);

      const user: AuthUser = buildCustomerUser(accountId) ?? {
        id: accountId,
        name: res.user.name,
        email: res.user.email,
        role: 'user',
        title: 'Customer',
        department: 'Subscribers',
        accountId,
      };

      currentUser.set(user);

      // Dữ liệu được tải lúc mở trang khi CHƯA có JWT nên các API bị 401 — khách mới đăng
      // nhập xong không thấy hoá đơn/đường truyền của mình. Tải lại SAU khi đã đặt người
      // dùng: hàm tải dựa vào vai trò 'user' để gọi đúng API hoá đơn của riêng khách.
      // Các ô trong cổng khách hàng là $derived nên tự cập nhật khi dữ liệu về.
      void nexusStore.syncWithDatabase();

      return { success: true as const, user };
    } catch (e) {
      if (e instanceof ApiError) {
        if (e.message === 'network') {
          return { success: false as const, error: t.auth.serverUnreachable };
        }
        return { success: false as const, error: e.message };
      }
      return { success: false as const, error: t.auth.loginFailed };
    }
  };

  const loginWithAccountId = (rawAccountId: string) => {
    const accountId = normalizeAccountId(rawAccountId);

    const t = get(languageStore.t);

    if (!isCompleteAccountId(rawAccountId)) {
      return { success: false as const, error: t.auth.accountIdRequired };
    }

    const user = buildCustomerUser(accountId);
    if (!user) {
      return { success: false as const, error: t.auth.accountNotFound };
    }

    currentUser.set(user);
    return { success: true as const, user };
  };

  // Called right after a purchase so the customer lands in their portal.
  const loginAfterPurchase = (accountId: string) => loginWithAccountId(accountId);

  // Internal staff entry point (demo): pick a role, no credentials.
  const loginAsStaff = (role: Exclude<RoleType, 'user'>): AuthUser => {
    const account = STAFF_ACCOUNTS.find((acc) => acc.user.role === role)!;
    // Không có token thật: xoá token cũ để không gửi kèm JWT sai role lên backend.
    localStorage.removeItem('nexus_jwt_token');
    currentUser.set(account.user);
    return account.user;
  };

  // Staff/customer sign-in through the real backend: POST /api/auth/login
  // with email + password. The backend returns a JWT plus the account's role
  // (Manager / Retail Staff / Field Engineer / Senior Accountant / Customer).
  const mapBackendRole = roleTypeFromJobTitle;

  const loginWithCredentials = async (
    email: string,
    password: string
  ): Promise<{ success: true; user: AuthUser } | { success: false; error: string }> => {
    const t = get(languageStore.t);

    try {
      const res = await loginRequest(email.trim(), password);

      // Lưu JWT để đính kèm vào các request tới backend sau này (nếu có).
      if (res.token) {
        localStorage.setItem('nexus_jwt_token', res.token);
        // Lần tải lúc mở trang chưa có token (401) — tải lại bằng token vừa nhận.
        void nexusStore.syncWithDatabase();
      }

      const role = mapBackendRole(res.user.role);
      const titleByRole: Record<RoleType, string> = {
        admin: 'Manager',
        retail: 'Retail Staff',
        technical: 'Field Engineer',
        accounts: 'Senior Accountant',
        user: 'Customer',
      };

      const user: AuthUser = {
        id: res.user.id,
        name: res.user.name,
        email: res.user.email,
        role,
        title: titleByRole[role],
        department: res.user.accountType === 'Employee' ? 'Nexus Telecom Internal' : 'Subscribers',
      };

      currentUser.set(user);
      return { success: true, user };
    } catch (e) {
      // Không còn fallback đăng nhập demo: mọi đăng nhập đều phải qua backend
      // (JWT). Sai mật khẩu / backend sập → báo lỗi, không tự cấp phiên cục bộ.
      if (e instanceof ApiError) {
        if (e.message === 'network') return { success: false, error: t.auth.serverUnreachable };
        if (e.status === 401) return { success: false, error: t.auth.invalidCredentials };
      }
      return { success: false, error: t.auth.loginFailed };
    }
  };

  // Đăng nhập nhanh 1-chạm cho tài khoản mẫu các role — đi qua backend thật
  // (email + mật khẩu demo khớp hash seed) để nhận JWT đúng role.
  const loginQuickStaff = async (
    email: string,
    role: Exclude<RoleType, 'user'>
  ): Promise<{ success: true; user: AuthUser } | { success: false; error: string }> => {
    return loginWithCredentials(email, '1234567890');
  };

  const updateUserProfile = async (updates: Partial<AuthUser>): Promise<{ success: true } | { success: false; error: string }> => {
    currentUser.update((u) => {
      if (!u) return null;
      const updated: AuthUser = { ...u, ...updates };
      localStorage.setItem('nexus_auth_user', JSON.stringify(updated));
      return updated;
    });

    // Sync name, email, phone, address to database regardless of user role
    const me = get(currentUser);
    if (me) {
      try {
        await updateProfileApi({
          name: me.name,
          email: me.email,
          phoneNumber: me.phone ?? '',
          address: me.address ?? '',
        });
      } catch (e) {
        return { success: false, error: e instanceof Error ? e.message : 'Không lưu được hồ sơ lên máy chủ.' };
      }
    }
    return { success: true };
  };

  const changePassword = async (currentPassword: string, newPassword: string): Promise<{ success: true } | { success: false; error: string }> => {
    try {
      await updatePasswordApi({ currentPassword, newPassword });
      return { success: true };
    } catch (e) {
      return { success: false, error: e instanceof Error ? e.message : 'Lỗi khi đổi mật khẩu.' };
    }
  };

  const logout = () => {
    currentUser.set(null);
    localStorage.removeItem('nexus_auth_user');
    localStorage.removeItem('nexus_jwt_token');
  };

  return {
    currentUser,
    isAuthenticated,
    loginWithAccountId,
    requestLoginOtp,
    loginWithOtp,
    loginAfterPurchase,
    loginAsStaff,
    loginWithCredentials,
    loginQuickStaff,
    updateUserProfile,
    changePassword,
    logout,
  };
}

export const authStore = createAuthStore();
