<script lang="ts">
  // App.svelte — HTML5 History routing with dynamic brand title
  import { route } from './lib/router';
  import ProtectedRoute from './components/auth/ProtectedRoute.svelte';
  import IndexPage from './pages/IndexPage.svelte';
  import LoginPage from './pages/LoginPage.svelte';
  import RegisterPage from './pages/RegisterPage.svelte';
  import AdminDashboard from './pages/AdminDashboard.svelte';
  import RetailDashboard from './pages/RetailDashboard.svelte';
  import TechnicalDashboard from './pages/TechnicalDashboard.svelte';
  import AccountsDashboard from './pages/AccountsDashboard.svelte';
  import UserDashboard from './pages/UserDashboard.svelte';
  import NotFoundPage from './pages/NotFoundPage.svelte';
  import { authStore } from './context/AuthContext';
  import { languageStore } from './context/LanguageContext';
  // Nạp sớm kho thông báo: module này tự mở kết nối SignalR và bám theo
  // người đang đăng nhập, nhờ vậy toast thông báo chạy trên mọi trang.
  import './context/NotificationContext';
  import { navigateTo, dashboardPathForRole, navigate } from './lib/router';
  import { Toaster } from 'svelte-sonner';

  const { currentUser } = authStore;
  const { language } = languageStore;

  // Seamless redirect for /profile route
  $effect(() => {
    if ($route === '/profile' || $route === '/ho-so') {
      const user = $currentUser;
      if (user) {
        navigateTo(dashboardPathForRole(user.role), 'profile');
      } else {
        navigate('/login');
      }
    }
  });

  // Dynamic document title synchronized with website name (Nexus Telecom) and current route
  $effect(() => {
    const isVi = $language === 'vi';
    const siteBrand = 'Nexus Telecom';
    const r = $route;
    let pageName = '';

    if (r === '/' || r === '/home') {
      document.title = isVi
        ? `${siteBrand} - Giải Pháp Kết Nối Mạng Viễn Thông Thế Hệ Mới`
        : `${siteBrand} - Next-Gen Telecom Network Solutions`;
      return;
    }

    if (r === '/login' || r === '/dang-nhap') {
      pageName = isVi ? 'Cổng Đăng Nhập' : 'Sign In Gateway';
    } else if (r === '/register' || r === '/dang-ky') {
      pageName = isVi ? 'Đăng Ký Gói Cước' : 'Buy Service Plans';
    } else if (r === '/admin') {
      pageName = isVi ? 'Quản Trị Hệ Thống' : 'Executive Admin';
    } else if (r === '/retail') {
      pageName = isVi ? 'Nhân viên chi nhánh' : 'Retail Operations';
    } else if (r === '/technical' || r === '/tech') {
      pageName = isVi ? 'Kỹ Thuật NOC & Mạng' : 'Field NOC & Ops';
    } else if (r === '/accounts' || r === '/billing') {
      pageName = isVi ? 'Kế Toán & Cước Phí' : 'Billing & Finance';
    } else if (r === '/user' || r === '/subscriber' || r === '/customer') {
      pageName = isVi ? 'Cổng Thuê Bao Tự Phục Vụ' : 'Subscriber Portal';
    } else if (r === '/profile' || r === '/ho-so') {
      pageName = isVi ? 'Hồ Sơ Cá Nhân' : 'User Profile';
    } else {
      pageName = isVi ? 'Không Tìm Thấy Trang' : 'Page Not Found';
    }

    document.title = `${pageName} | ${siteBrand}`;
  });
</script>

<div class="h-screen w-screen overflow-hidden flex flex-col bg-[#E0F1FF] dark:bg-[#1B2D40] text-[#1B2D40] dark:text-[#E0F1FF] antialiased selection:bg-sky-500 selection:text-white transition-colors duration-300">
  <div class="flex-1 h-full overflow-hidden">
    {#if $route === '/' || $route === '/home'}
      <IndexPage />
    {:else if $route === '/login' || $route === '/dang-nhap'}
      <LoginPage />
    {:else if $route === '/register' || $route === '/dang-ky'}
      <RegisterPage />
    {:else if $route === '/admin'}
      <ProtectedRoute requiredRole="admin">
        <AdminDashboard />
      </ProtectedRoute>
    {:else if $route === '/retail'}
      <ProtectedRoute requiredRole="retail">
        <RetailDashboard />
      </ProtectedRoute>
    {:else if $route === '/technical' || $route === '/tech'}
      <ProtectedRoute requiredRole="technical">
        <TechnicalDashboard />
      </ProtectedRoute>
    {:else if $route === '/accounts' || $route === '/billing'}
      <ProtectedRoute requiredRole="accounts">
        <AccountsDashboard />
      </ProtectedRoute>
    {:else if $route === '/user' || $route === '/subscriber' || $route === '/customer'}
      <ProtectedRoute requiredRole="user">
        <UserDashboard />
      </ProtectedRoute>
    {:else if $route === '/profile' || $route === '/ho-so'}
      {#if $currentUser}
        {#if $currentUser.role === 'admin'}
          <ProtectedRoute requiredRole="admin"><AdminDashboard /></ProtectedRoute>
        {:else if $currentUser.role === 'retail'}
          <ProtectedRoute requiredRole="retail"><RetailDashboard /></ProtectedRoute>
        {:else if $currentUser.role === 'technical'}
          <ProtectedRoute requiredRole="technical"><TechnicalDashboard /></ProtectedRoute>
        {:else if $currentUser.role === 'accounts'}
          <ProtectedRoute requiredRole="accounts"><AccountsDashboard /></ProtectedRoute>
        {:else}
          <ProtectedRoute requiredRole="user"><UserDashboard /></ProtectedRoute>
        {/if}
      {:else}
        <LoginPage />
      {/if}
    {:else}
      <!-- Fallback 404 / No connection -->
      <NotFoundPage />
    {/if}
  </div>

  <!-- Global Toast Notifications (1.5s auto dismiss) -->
  <Toaster position="top-right" offset="76px" richColors duration={1500} />
</div>
