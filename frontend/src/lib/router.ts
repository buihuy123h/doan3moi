// HTML5 History-based SPA router for Svelte 5 (clean URLs without '#').
// Routes: '/', '/home', '/login', '/register', '/admin', '/retail', '/technical', '/accounts', '/user' (+ aliases)
import { writable } from 'svelte/store';

function normalizeRoutePath(pathname: string): string {
  if (!pathname || pathname === '') return '/';
  // Strip trailing slash except root
  return pathname.length > 1 && pathname.endsWith('/') ? pathname.slice(0, -1) : pathname;
}

export function parsePath(): string {
  if (typeof window === 'undefined') return '/';

  // Automatically migrate legacy hash URLs if user opens an old bookmark like `/#/login?order=B01`
  const hash = window.location.hash;
  if (hash.startsWith('#/')) {
    const cleanFromHash = hash.replace(/^#/, '');
    window.history.replaceState({}, '', cleanFromHash);
    return normalizeRoutePath(window.location.pathname);
  } else if (hash === '#') {
    window.history.replaceState({}, '', window.location.pathname + window.location.search);
  }

  return normalizeRoutePath(window.location.pathname);
}

export const route = writable<string>(parsePath());

if (typeof window !== 'undefined') {
  window.addEventListener('popstate', () => {
    route.set(parsePath());
  });
}

export function navigate(to: string) {
  if (typeof window === 'undefined') return;
  // Clean up any accidental leading '#'
  const cleanTo = to.startsWith('#') ? to.replace(/^#/, '') : to;
  window.history.pushState({}, '', cleanTo);
  route.set(parsePath());
}

export const activeTabOverride = writable<{ path: string; tab: string } | null>(null);

export function navigateTo(path: string, tab?: string, extraQuery?: Record<string, string>) {
  if (typeof window === 'undefined') return;
  const cleanPath = path.startsWith('#') ? path.replace(/^#/, '') : path;
  if (tab) {
    activeTabOverride.set({ path: cleanPath, tab });
    // extraQuery cho phép kèm tham số bổ sung (VD: session=chat-01 để mở thẳng phiên chat)
    const params = new URLSearchParams({ tab });
    if (extraQuery) {
      for (const [key, value] of Object.entries(extraQuery)) params.set(key, value);
    }
    window.history.pushState({}, '', `${cleanPath}?${params.toString()}`);
  } else {
    activeTabOverride.set(null);
    window.history.pushState({}, '', cleanPath);
  }
  route.set(parsePath());
}

// Reads a query parameter from window.location.search (or fallback to hash if any)
export function queryParam(name: string): string | null {
  if (typeof window === 'undefined') return null;

  const urlParams = new URLSearchParams(window.location.search);
  if (urlParams.has(name)) {
    return urlParams.get(name);
  }

  // Fallback for legacy query inside hash
  const hash = window.location.hash.replace(/^#/, '');
  const q = hash.split('?')[1];
  if (q) {
    const hp = new URLSearchParams(q);
    if (hp.has(name)) return hp.get(name);
  }

  return null;
}

// Build a role-isolated dashboard path (mirrors ProtectedRoute's roleRoutes map)
export function dashboardPathForRole(role: string): string {
  const map: Record<string, string> = {
    admin: '/admin',
    retail: '/retail',
    technical: '/technical',
    accounts: '/accounts',
    user: '/user',
  };
  return map[role] ?? '/user';
}
