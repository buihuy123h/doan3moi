<script lang="ts">
  // Mirrors components/auth/ProtectedRoute.tsx of the React original.
  // Redirects unauthenticated users to /login; wrong-role users to their own dashboard.
  import { get } from 'svelte/store';
  import { authStore } from '../../context/AuthContext';
  import { dashboardPathForRole, navigate } from '../../lib/router';
  import { toast } from 'svelte-sonner';
  import type { RoleType } from '../../types/nexus';
  import type { Snippet } from 'svelte';

  let { requiredRole, children }: { requiredRole: RoleType; children: Snippet } = $props();

  const { currentUser } = authStore;

  // Reactive guard: re-runs whenever auth state or requiredRole changes
  $effect(() => {
    const user = $currentUser;

    if (!user) {
      navigate('/login');
      return;
    }

    // If logged in as another role, redirect to their respective dashboard
    if (user.role !== requiredRole) {
      toast.error('Bạn không có quyền truy cập trang này. Đang chuyển hướng về trang của bạn.');
      navigate(dashboardPathForRole(user.role));
    }
  });
</script>

{@render children()}
