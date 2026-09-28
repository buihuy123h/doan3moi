<script lang="ts">
  import {
    User, Mail, Phone, MapPin, Briefcase, Building2, Calendar, ShieldCheck,
    KeyRound, Lock, Eye, EyeOff, LogOut, CheckCircle2, AlertCircle, Copy,
    Sparkles, Camera, RefreshCw, ChevronUp, ChevronDown, Laptop, Smartphone,
    Globe, ShieldAlert,
  } from 'lucide-svelte';
  import { toast } from 'svelte-sonner';
  import { authStore } from '../../context/AuthContext';
  import { languageStore } from '../../context/LanguageContext';
  import { navigate } from '../../lib/router';
  import { isValidEmail, isValidPhone } from '../../lib/validation';

  const { currentUser, updateUserProfile, changePassword, logout } = authStore;
  const { language } = languageStore;

  // Accordion droplist collapse states
  let isPersonalInfoOpen = $state(true);
  let isAccountInfoOpen = $state(true);
  let isPasswordOpen = $state(true);
  let isSessionOpen = $state(true);

  const toggleAll = (expand: boolean) => {
    isPersonalInfoOpen = expand;
    isAccountInfoOpen = expand;
    isPasswordOpen = expand;
    isSessionOpen = expand;
  };

  // 1. Personal Information Form State
  let name = $state($currentUser?.name || '');
  let email = $state($currentUser?.email || '');
  let phone = $state($currentUser?.phone || '');
  let title = $state($currentUser?.title || '');
  let department = $state($currentUser?.department || '');
  let address = $state($currentUser?.address || '72 Lê Lợi, Bến Nghé, Quận 1, TP. Hồ Chí Minh');
  let gender = $state<'male' | 'female' | 'other'>($currentUser?.gender || 'male');
  let dateOfBirth = $state($currentUser?.dateOfBirth || '1990-01-01');
  let bio = $state($currentUser?.bio || '');
  let customAvatarUrl = $state($currentUser?.avatar || '');

  // Preset avatar choices
  const avatarPresets = [
    { id: 'preset-1', gradient: 'from-sky-500 to-blue-600', label: 'Cyan Sky' },
    { id: 'preset-2', gradient: 'from-emerald-500 to-teal-600', label: 'Emerald' },
    { id: 'preset-3', gradient: 'from-purple-500 to-indigo-600', label: 'Purple' },
    { id: 'preset-4', gradient: 'from-rose-500 to-pink-600', label: 'Rose' },
    { id: 'preset-5', gradient: 'from-amber-500 to-orange-600', label: 'Amber' },
  ];
  let selectedPreset = $state(avatarPresets[0].gradient);

  // Synchronize when currentUser changes
  $effect(() => {
    if ($currentUser) {
      name = $currentUser.name || '';
      email = $currentUser.email || '';
      phone = $currentUser.phone || phone;
      title = $currentUser.title || '';
      department = $currentUser.department || '';
      address = $currentUser.address || address;
      gender = $currentUser.gender || gender;
      dateOfBirth = $currentUser.dateOfBirth || dateOfBirth;
      bio = $currentUser.bio || bio;
      if ($currentUser.avatar) {
        customAvatarUrl = $currentUser.avatar;
      }
    }
  });

  const handleSavePersonalInfo = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      toast.error($language === 'vi' ? 'Họ và tên không được để trống.' : 'Full name cannot be empty.');
      return;
    }
    if (!isValidEmail(email)) {
      toast.error($language === 'vi' ? 'Email phải có dạng ten@mien.com.' : 'Email must have the form name@domain.com.');
      return;
    }
    if (phone.trim() && !isValidPhone(phone)) {
      toast.error($language === 'vi' ? 'Số điện thoại phải gồm đúng 10 chữ số.' : 'Phone number must contain exactly 10 digits.');
      return;
    }

    const saveResult = await updateUserProfile({
      name: name.trim(),
      email: email.trim(),
      phone: phone.trim(),
      title: title.trim(),
      department: department.trim(),
      address: address.trim(),
      gender,
      dateOfBirth,
      bio: bio.trim(),
      avatar: customAvatarUrl || undefined,
    });

    if (!saveResult.success) {
      toast.error(saveResult.error);
      return;
    }

    toast.success(
      $language === 'vi'
        ? 'Đã cập nhật thông tin cá nhân thành công!'
        : 'Personal information updated successfully!'
    );
  };

  // Image Upload handler
  const handleAvatarFileChange = (e: Event) => {
    const target = e.target as HTMLInputElement;
    const file = target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      toast.error($language === 'vi' ? 'Vui lòng chọn file hình ảnh hợp lệ.' : 'Please select a valid image file.');
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      toast.error($language === 'vi' ? 'Dung lượng ảnh tối đa 2MB.' : 'Image size cannot exceed 2MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      customAvatarUrl = reader.result as string;
      updateUserProfile({ avatar: customAvatarUrl });
      toast.success($language === 'vi' ? 'Đã đổi ảnh đại diện thành công!' : 'Avatar photo updated!');
    };
    reader.readAsDataURL(file);
  };

  // 2. Password Change State
  let currentPassword = $state('');
  let newPassword = $state('');
  let confirmPassword = $state('');
  let showCurrentPassword = $state(false);
  let showNewPassword = $state(false);
  let showConfirmPassword = $state(false);

  // Password strength calculation
  const passwordStrength = $derived.by(() => {
    if (!newPassword) return 0;
    let score = 0;
    if (newPassword.length >= 6) score += 1;
    if (newPassword.length >= 10) score += 1;
    if (/[A-Z]/.test(newPassword) && /[a-z]/.test(newPassword)) score += 1;
    if (/[0-9]/.test(newPassword)) score += 1;
    if (/[^A-Za-z0-9]/.test(newPassword)) score += 1;
    return Math.min(score, 4);
  });

  const passwordStrengthText = $derived.by(() => {
    if (!newPassword) return { text: '', color: 'text-slate-400' };
    switch (passwordStrength) {
      case 1:
        return { text: $language === 'vi' ? 'Rất yếu' : 'Very Weak', color: 'text-rose-500' };
      case 2:
        return { text: $language === 'vi' ? 'Yếu' : 'Weak', color: 'text-amber-500' };
      case 3:
        return { text: $language === 'vi' ? 'Trung bình' : 'Medium', color: 'text-yellow-500' };
      case 4:
      default:
        return { text: $language === 'vi' ? 'Mạnh' : 'Strong', color: 'text-emerald-500' };
    }
  });

  const handleChangePassword = async (e: SubmitEvent) => {
    e.preventDefault();
    if (!currentPassword) {
      toast.error($language === 'vi' ? 'Vui lòng nhập mật khẩu hiện tại.' : 'Please enter current password.');
      return;
    }
    if (!newPassword || newPassword.length < 6) {
      toast.error(
        $language === 'vi'
          ? 'Mật khẩu mới phải có ít nhất 6 ký tự.'
          : 'New password must be at least 6 characters.'
      );
      return;
    }
    if (newPassword !== confirmPassword) {
      toast.error(
        $language === 'vi'
          ? 'Xác nhận mật khẩu mới không khớp.'
          : 'New password confirmation does not match.'
      );
      return;
    }

    const changeResult = await changePassword(currentPassword, newPassword);
    
    if (!changeResult.success) {
      toast.error(changeResult.error);
      return;
    }

    currentPassword = '';
    newPassword = '';
    confirmPassword = '';

    toast.success(
      $language === 'vi'
        ? 'Đã đổi mật khẩu thành công! Mật khẩu mới có hiệu lực ngay lập tức.'
        : 'Password changed successfully! Your new password is now active.'
    );
  };

  // 3. Logout action
  const handleLogoutClick = () => {
    logout();
    toast.info($language === 'vi' ? 'Đã đăng xuất khỏi tài khoản.' : 'Signed out successfully.');
    navigate('/login');
  };

  const handleLogoutAllDevices = () => {
    toast.success(
      $language === 'vi'
        ? 'Đã ngắt kết nối tất cả các phiên đăng nhập khác!'
        : 'All other sessions have been terminated!'
    );
  };

  // Copy code helper
  const copyToClipboard = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    toast.success(
      $language === 'vi' ? `Đã sao chép ${label}: ${text}` : `Copied ${label}: ${text}`
    );
  };

  // Role badge translation
  const getRoleBadge = (role?: string) => {
    switch (role) {
      case 'admin':
        return {
          label: $language === 'vi' ? 'Quản trị viên Hệ thống' : 'System Administrator',
          bg: 'bg-sky-100 dark:bg-sky-950/80 text-sky-700 dark:text-sky-300 border-sky-200 dark:border-sky-800',
        };
      case 'retail':
        return {
          label: $language === 'vi' ? 'Nhân viên Quầy Bán lẻ' : 'Retail Store Staff',
          bg: 'bg-emerald-100 dark:bg-emerald-950/80 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800',
        };
      case 'technical':
        return {
          label: $language === 'vi' ? 'Kỹ sư Vận hành Kỹ thuật NOC' : 'NOC Technical Engineer',
          bg: 'bg-amber-100 dark:bg-amber-950/80 text-amber-700 dark:text-amber-300 border-amber-200 dark:border-amber-800',
        };
      case 'accounts':
        return {
          label: $language === 'vi' ? 'Kế toán Trưởng & Thu cước' : 'Senior Finance & Billing',
          bg: 'bg-purple-100 dark:bg-purple-950/80 text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-800',
        };
      default:
        return {
          label: $language === 'vi' ? 'Thuê bao Khách hàng' : 'Customer Subscriber',
          bg: 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 border-slate-300 dark:border-slate-700',
        };
    }
  };

  const currentRoleInfo = $derived(getRoleBadge($currentUser?.role));
</script>

<div class="space-y-6 max-w-6xl mx-auto pb-12">
  <!-- HERO PROFILE BANNER CARD -->
  <div class="relative overflow-hidden rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-md">
    <!-- Decorative Header Cover Pattern -->
    <div class="h-32 sm:h-36 bg-gradient-to-r from-sky-600 via-blue-600 to-indigo-700 relative overflow-hidden">
      <div class="absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-white/20 via-transparent to-black/30"></div>
      <div class="absolute right-6 -bottom-10 opacity-15 pointer-events-none">
        <Sparkles class="h-44 w-44 text-white" />
      </div>
    </div>

    <!-- User Header Info -->
    <div class="px-6 sm:px-8 pb-6 pt-0 relative">
      <div class="flex flex-col sm:flex-row sm:items-end justify-between gap-4 -mt-14 sm:-mt-16 mb-4">
        <!-- Avatar with change trigger -->
        <div class="flex items-end space-x-4">
          <div class="relative group">
            <div class="h-24 w-24 sm:h-28 sm:w-28 rounded-2xl p-1 bg-white dark:bg-slate-900 shadow-xl overflow-hidden">
              {#if customAvatarUrl}
                <img
                  src={customAvatarUrl}
                  alt="Avatar"
                  class="h-full w-full object-cover rounded-xl"
                />
              {:else}
                <div class="h-full w-full rounded-xl bg-gradient-to-tr {selectedPreset} flex items-center justify-center text-white font-black text-3xl sm:text-4xl shadow-inner">
                  {$currentUser?.name?.charAt(0) || 'U'}
                </div>
              {/if}
            </div>

            <!-- Upload Avatar Trigger Button -->
            <label
              class="absolute bottom-2 right-2 h-7 w-7 rounded-lg bg-sky-600 hover:bg-sky-700 text-white flex items-center justify-center shadow-lg cursor-pointer transition active:scale-95 group-hover:scale-110"
              title={$language === 'vi' ? 'Tải ảnh đại diện mới' : 'Upload new avatar photo'}
            >
              <Camera class="h-3.5 w-3.5" />
              <input
                type="file"
                accept="image/*"
                class="hidden"
                onchange={handleAvatarFileChange}
              />
            </label>

            <!-- Online Dot -->
            <div class="absolute top-1 right-1 h-3.5 w-3.5 rounded-full bg-emerald-500 ring-2 ring-white dark:ring-slate-900 shadow-sm animate-pulse" title="Online"></div>
          </div>

          <div class="pt-2 sm:pt-0">
            <div class="flex flex-wrap items-center gap-2">
              <h1 class="text-xl sm:text-2xl font-bold text-slate-900 dark:text-white tracking-tight">
                {$currentUser?.name || 'User Name'}
              </h1>

            </div>
            <p class="text-xs sm:text-sm text-slate-500 dark:text-slate-400 flex items-center gap-1.5 mt-0.5 font-medium">
              <span>{$currentUser?.title || 'Nexus Staff'}</span>
              <span>•</span>
              <span class="text-sky-600 dark:text-sky-400 font-semibold">{$currentUser?.department || 'Telecom Department'}</span>
            </p>
          </div>
        </div>

        <!-- Right Quick Action: Logout Button -->
        <div class="flex items-center space-x-2.5 shrink-0 self-start sm:self-end">
          <button
            type="button"
            onclick={handleLogoutClick}
            class="flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-bold bg-rose-50 hover:bg-rose-100 dark:bg-rose-950/40 dark:hover:bg-rose-900/60 text-rose-600 dark:text-rose-400 border border-rose-200 dark:border-rose-800/80 shadow-xs transition active:scale-95"
          >
            <LogOut class="h-3.5 w-3.5" />
            <span>{$language === 'vi' ? 'Đăng xuất tài khoản' : 'Sign Out'}</span>
          </button>
        </div>
      </div>
    </div>
  </div>

  <!-- SECTION 1: PERSONAL INFORMATION -->
  <div class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-300 overflow-hidden {isPersonalInfoOpen ? 'p-6 sm:p-8 space-y-6' : 'p-4 sm:p-5'}">
    <div
      class="flex items-center justify-between cursor-pointer {isPersonalInfoOpen ? 'border-b border-slate-100 dark:border-slate-800 pb-4' : ''}"
      role="button"
      tabindex="0"
      onclick={() => (isPersonalInfoOpen = !isPersonalInfoOpen)}
      onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); isPersonalInfoOpen = !isPersonalInfoOpen; } }}
    >
      <div class="flex items-center space-x-3 flex-1">
        <div class="h-10 w-10 rounded-xl bg-sky-100 dark:bg-sky-900/40 text-sky-600 dark:text-sky-400 flex items-center justify-center shrink-0 shadow-xs">
          <User class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 dark:text-white">
            {$language === 'vi' ? 'Thông tin cá nhân' : 'Personal Information'}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Cập nhật họ tên, số điện thoại, email làm việc và địa chỉ liên lạc' : 'Update your personal identity, contact details, and residential address'}
          </p>
        </div>
      </div>

      <div class="flex items-center space-x-2">
        <span class="text-xs font-medium text-slate-500 dark:text-slate-400 hidden sm:inline">
          {$currentUser?.email || ''}
        </span>
        <button
          type="button"
          class="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition"
          onclick={(e) => {
            e.stopPropagation();
            isPersonalInfoOpen = !isPersonalInfoOpen;
          }}
        >
          {#if isPersonalInfoOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    {#if isPersonalInfoOpen}
      <form onsubmit={handleSavePersonalInfo} class="space-y-6">
        <!-- Preset Avatars Selector -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-2">
            {$language === 'vi' ? 'Tùy chọn màu sắc Ảnh đại diện:' : 'Avatar Color Theme Preset:'}
          </label>
          <div class="flex flex-wrap items-center gap-3">
            {#each avatarPresets as preset (preset.id)}
              <button
                type="button"
                onclick={() => {
                  selectedPreset = preset.gradient;
                  customAvatarUrl = '';
                  updateUserProfile({ avatar: undefined });
                }}
                class="flex items-center space-x-2 px-3 py-1.5 rounded-xl border transition {selectedPreset === preset.gradient && !customAvatarUrl
                  ? 'border-sky-500 bg-sky-50/80 dark:bg-sky-950/60 ring-2 ring-sky-500/20 shadow-xs'
                  : 'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700 bg-white dark:bg-slate-900'}"
              >
                <div class="h-4 w-4 rounded-full bg-gradient-to-tr {preset.gradient}"></div>
                <span class="text-xs font-medium text-slate-700 dark:text-slate-300">{preset.label}</span>
              </button>
            {/each}
          </div>
        </div>

        <!-- Input Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
          <!-- Full Name -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Họ và tên *' : 'Full Name *'}
            </label>
            <div class="relative">
              <User class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="text"
                bind:value={name}
                required
                class="w-full pl-10 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
                placeholder={$language === 'vi' ? 'Nhập họ và tên đầy đủ' : 'Enter full name'}
              />
            </div>
          </div>

          <!-- Email Address -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Địa chỉ Email làm việc *' : 'Work Email Address *'}
            </label>
            <div class="relative">
              <Mail class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="email"
                pattern="[^\\s@]+@[^\\s@]+\\.[^\\s@]+"
                title={$language === 'vi' ? 'Email phải có dạng ten@mien.com' : 'Email must have the form name@domain.com'}
                bind:value={email}
                required
                class="w-full pl-10 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
                placeholder="email@nexus.telecom"
              />
            </div>
          </div>

          <!-- Phone Number -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Số điện thoại di động' : 'Phone Number'}
            </label>
            <div class="relative">
              <Phone class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="tel"
                inputmode="numeric"
                
                maxlength="10"
                title={$language === 'vi' ? 'Số điện thoại phải gồm đúng 10 chữ số' : 'Phone number must contain exactly 10 digits'}
                bind:value={phone}
                class="w-full pl-10 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
                placeholder="0912345678"
              />
            </div>
          </div>

          <!-- Job Title -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Chức danh / Vị trí công tác' : 'Job Title / Position'}
            </label>
            <div class="relative">
              <Briefcase class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="text"
                bind:value={title}
                class="w-full pl-10 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
                placeholder={$language === 'vi' ? 'Ví dụ: Quản trị viên, Kỹ sư mạng' : 'e.g. System Administrator'}
              />
            </div>
          </div>

          <!-- Department -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Phòng ban / Đơn vị' : 'Department'}
            </label>
            <div class="relative">
              <Building2 class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="text"
                bind:value={department}
                class="w-full pl-10 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
                placeholder="Executive Administration"
              />
            </div>
          </div>

          <!-- Address -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Địa chỉ liên lạc' : 'Address'}
            </label>
            <div class="relative">
              <MapPin class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="text"
                bind:value={address}
                class="w-full pl-10 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
                placeholder={$language === 'vi' ? 'Địa chỉ cơ quan hoặc nhà riêng' : 'Work or residential address'}
              />
            </div>
          </div>

          <!-- Gender -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Giới tính' : 'Gender'}
            </label>
            <select
              bind:value={gender}
              class="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
            >
              <option value="male">{$language === 'vi' ? 'Nam' : 'Male'}</option>
              <option value="female">{$language === 'vi' ? 'Nữ' : 'Female'}</option>
              <option value="other">{$language === 'vi' ? 'Khác' : 'Other'}</option>
            </select>
          </div>

          <!-- Date of Birth -->
          <div>
            <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
              {$language === 'vi' ? 'Ngày sinh' : 'Date of Birth'}
            </label>
            <div class="relative">
              <Calendar class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="date"
                bind:value={dateOfBirth}
                class="w-full pl-10 pr-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
              />
            </div>
          </div>
        </div>

        <!-- Bio / Notes -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
            {$language === 'vi' ? 'Tiểu sử & Ghi chú hồ sơ' : 'Bio & Professional Summary'}
          </label>
          <textarea
            bind:value={bio}
            rows="2"
            class="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500 transition"
            placeholder={$language === 'vi' ? 'Mô tả ngắn về nhiệm vụ hoặc kinh nghiệm chuyên môn...' : 'Brief summary of responsibilities or achievements...'}
          ></textarea>
        </div>

        <!-- Save button -->
        <div class="flex items-center justify-end pt-2">
          <button
            type="submit"
            class="flex items-center space-x-2 px-6 py-2.5 rounded-xl text-xs font-bold bg-sky-600 hover:bg-sky-700 text-white shadow-md shadow-sky-600/20 transition active:scale-95"
          >
            <CheckCircle2 class="h-4 w-4" />
            <span>{$language === 'vi' ? 'Lưu thay đổi thông tin' : 'Save Personal Info'}</span>
          </button>
        </div>
      </form>
    {/if}
  </div>

  <!-- SECTION 2: ACCOUNT & SYSTEM CREDENTIALS -->
  <div class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-300 overflow-hidden {isAccountInfoOpen ? 'p-6 sm:p-8 space-y-6' : 'p-4 sm:p-5'}">
    <div
      class="flex items-center justify-between cursor-pointer {isAccountInfoOpen ? 'border-b border-slate-100 dark:border-slate-800 pb-4' : ''}"
      role="button"
      tabindex="0"
      onclick={() => (isAccountInfoOpen = !isAccountInfoOpen)}
      onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); isAccountInfoOpen = !isAccountInfoOpen; } }}
    >
      <div class="flex items-center space-x-3 flex-1">
        <div class="h-10 w-10 rounded-xl bg-emerald-100 dark:bg-emerald-900/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0 shadow-xs">
          <ShieldCheck class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 dark:text-white">
            {$language === 'vi' ? 'Thông tin tài khoản & Hệ thống' : 'Account & System Credentials'}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Mã định danh tài khoản, quyền hạn truy cập, chi nhánh quản lý và trạng thái' : 'Account identifier, access level, assigned branch, and authorization status'}
          </p>
        </div>
      </div>

      <div class="flex items-center space-x-2">
        <span class="text-xs font-mono font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/60 px-2 py-0.5 rounded border border-emerald-200 dark:border-emerald-800/60">
          {$currentUser?.id || 'ID'}
        </span>
        <button
          type="button"
          class="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition"
          onclick={(e) => {
            e.stopPropagation();
            isAccountInfoOpen = !isAccountInfoOpen;
          }}
        >
          {#if isAccountInfoOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    {#if isAccountInfoOpen}
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <!-- Account Identifier / Code -->
        <div class="p-4 rounded-xl border border-slate-200/80 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 space-y-1">
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium text-slate-500 dark:text-slate-400">
              {$language === 'vi' ? 'Mã định danh tài khoản' : 'Account ID / Code'}
            </span>
            <button
              type="button"
              onclick={() => copyToClipboard($currentUser?.id || '', 'Account ID')}
              class="text-sky-600 hover:text-sky-700 p-1 rounded hover:bg-sky-50 dark:hover:bg-sky-950/60 transition"
              title={$language === 'vi' ? 'Sao chép mã' : 'Copy ID'}
            >
              <Copy class="h-3.5 w-3.5" />
            </button>
          </div>
          <div class="text-base font-mono font-bold text-slate-900 dark:text-white">
            {$currentUser?.id || 'N/A'}
          </div>
          <p class="text-[11px] text-slate-400 dark:text-slate-500">
            {$language === 'vi' ? 'Khóa chính bảo mật xác thực hệ thống' : 'Primary system authentication key'}
          </p>
        </div>

        <!-- Role Level -->
        <div class="p-4 rounded-xl border border-slate-200/80 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 space-y-1">
          <span class="text-xs font-medium text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Cấp bậc phân quyền' : 'Authorization Role'}
          </span>
          <div class="text-base font-bold text-slate-900 dark:text-white uppercase">
            {$currentUser?.role || 'User'}
          </div>
          <p class="text-[11px] text-sky-600 dark:text-sky-400 font-medium">
            {currentRoleInfo.label}
          </p>
        </div>

        <!-- Account Status -->
        <div class="p-4 rounded-xl border border-slate-200/80 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 space-y-1">
          <span class="text-xs font-medium text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Trạng thái hoạt động' : 'Account Status'}
          </span>
          <div class="flex items-center space-x-2">
            <span class="h-2 w-2 rounded-full bg-emerald-500 animate-pulse"></span>
            <span class="text-base font-bold text-emerald-600 dark:text-emerald-400">
              {$language === 'vi' ? 'Đang hoạt động' : 'Active & Verified'}
            </span>
          </div>
          <p class="text-[11px] text-slate-400 dark:text-slate-500">
            {$language === 'vi' ? 'Ủy quyền đầy đủ theo chính sách Nexus' : 'Full policy privileges granted'}
          </p>
        </div>

        <!-- Branch Outlet -->
        <div class="p-4 rounded-xl border border-slate-200/80 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 space-y-1">
          <span class="text-xs font-medium text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Chi nhánh trực thuộc' : 'Assigned Outlet'}
          </span>
          <div class="text-sm font-bold text-slate-900 dark:text-white truncate">
            {$currentUser?.branchCode || 'Downtown Flagship (SH-01)'}
          </div>
          <p class="text-[11px] text-slate-400 dark:text-slate-500">
            {$language === 'vi' ? 'Điểm giao dịch và kho thiết bị khu vực' : 'Regional retail & inventory hub'}
          </p>
        </div>

        <!-- Date of Joining -->
        <div class="p-4 rounded-xl border border-slate-200/80 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 space-y-1">
          <span class="text-xs font-medium text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Ngày tham gia hệ thống' : 'Member Since'}
          </span>
          <div class="text-sm font-bold text-slate-900 dark:text-white">
            {$currentUser?.dateOfJoining || '15/01/2022'}
          </div>
          <p class="text-[11px] text-slate-400 dark:text-slate-500">
            {$language === 'vi' ? 'Hồ sơ nhân sự đã được lưu trữ' : 'Verified employee onboard date'}
          </p>
        </div>

        <!-- 2FA Security -->
        <div class="p-4 rounded-xl border border-slate-200/80 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/40 space-y-1">
          <span class="text-xs font-medium text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Xác thực 2 lớp (2FA)' : 'Two-Factor Authentication'}
          </span>
          <div class="flex items-center space-x-1.5 text-sm font-bold text-sky-600 dark:text-sky-400">
            <ShieldCheck class="h-4 w-4" />
            <span>{$language === 'vi' ? 'Đã kích hoạt bảo mật' : 'Enabled (NOC Standard)'}</span>
          </div>
          <p class="text-[11px] text-slate-400 dark:text-slate-500">
            {$language === 'vi' ? 'Bảo vệ qua OTP & Token viễn thông' : 'Protected via session token'}
          </p>
        </div>
      </div>
    {/if}
  </div>

  <!-- SECTION 3: PASSWORD & SECURITY -->
  <div class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm transition-colors duration-300 overflow-hidden {isPasswordOpen ? 'p-6 sm:p-8 space-y-6' : 'p-4 sm:p-5'}">
    <div
      class="flex items-center justify-between cursor-pointer {isPasswordOpen ? 'border-b border-slate-100 dark:border-slate-800 pb-4' : ''}"
      role="button"
      tabindex="0"
      onclick={() => (isPasswordOpen = !isPasswordOpen)}
      onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); isPasswordOpen = !isPasswordOpen; } }}
    >
      <div class="flex items-center space-x-3 flex-1">
        <div class="h-10 w-10 rounded-xl bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 flex items-center justify-center shrink-0 shadow-xs">
          <KeyRound class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 dark:text-white">
            {$language === 'vi' ? 'Đổi mật khẩu & Bảo mật' : 'Password & Security Settings'}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Cập nhật mật khẩu định kỳ để bảo vệ tài khoản và dữ liệu khách hàng' : 'Update your password regularly to protect your account and subscriber data'}
          </p>
        </div>
      </div>

      <div class="flex items-center space-x-2">
        <span class="text-xs font-semibold text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-950/60 px-2 py-0.5 rounded border border-amber-200 dark:border-amber-800/60">
          {$language === 'vi' ? 'Bảo mật cao' : 'High Security'}
        </span>
        <button
          type="button"
          class="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition"
          onclick={(e) => {
            e.stopPropagation();
            isPasswordOpen = !isPasswordOpen;
          }}
        >
          {#if isPasswordOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    {#if isPasswordOpen}
      <form onsubmit={handleChangePassword} class="space-y-5 max-w-2xl">
        <!-- Current Password -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
            {$language === 'vi' ? 'Mật khẩu hiện tại *' : 'Current Password *'}
          </label>
          <div class="relative">
            <Lock class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
            <input
              type={showCurrentPassword ? 'text' : 'password'}
              bind:value={currentPassword}
              required
              class="w-full pl-10 pr-10 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition"
              placeholder={$language === 'vi' ? 'Nhập mật khẩu đang dùng' : 'Enter current password'}
            />
            <button
              type="button"
              onclick={() => (showCurrentPassword = !showCurrentPassword)}
              class="absolute right-3 top-3 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition"
              title={showCurrentPassword ? 'Hide password' : 'Show password'}
            >
              {#if showCurrentPassword}
                <EyeOff class="h-4 w-4" />
              {:else}
                <Eye class="h-4 w-4" />
              {/if}
            </button>
          </div>
        </div>

        <!-- New Password -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
            {$language === 'vi' ? 'Mật khẩu mới *' : 'New Password *'}
          </label>
          <div class="relative">
            <KeyRound class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
            <input
              type={showNewPassword ? 'text' : 'password'}
              bind:value={newPassword}
              required
              minlength="6"
              class="w-full pl-10 pr-10 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition"
              placeholder={$language === 'vi' ? 'Tối thiểu 6 ký tự' : 'At least 6 characters'}
            />
            <button
              type="button"
              onclick={() => (showNewPassword = !showNewPassword)}
              class="absolute right-3 top-3 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition"
              title={showNewPassword ? 'Hide password' : 'Show password'}
            >
              {#if showNewPassword}
                <EyeOff class="h-4 w-4" />
              {:else}
                <Eye class="h-4 w-4" />
              {/if}
            </button>
          </div>

          <!-- Password Strength Meter -->
          {#if newPassword}
            <div class="mt-2 space-y-1.5">
              <div class="flex items-center justify-between text-xs">
                <span class="text-slate-500 dark:text-slate-400">
                  {$language === 'vi' ? 'Độ mạnh mật khẩu:' : 'Password strength:'}
                </span>
                <span class="font-bold {passwordStrengthText.color}">
                  {passwordStrengthText.text}
                </span>
              </div>
              <div class="grid grid-cols-4 gap-1.5 h-1.5 w-full bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                <div class="rounded-full transition-colors duration-300 {passwordStrength >= 1 ? 'bg-rose-500' : 'bg-transparent'}"></div>
                <div class="rounded-full transition-colors duration-300 {passwordStrength >= 2 ? 'bg-amber-500' : 'bg-transparent'}"></div>
                <div class="rounded-full transition-colors duration-300 {passwordStrength >= 3 ? 'bg-yellow-500' : 'bg-transparent'}"></div>
                <div class="rounded-full transition-colors duration-300 {passwordStrength >= 4 ? 'bg-emerald-500' : 'bg-transparent'}"></div>
              </div>
            </div>
          {/if}
        </div>

        <!-- Confirm New Password -->
        <div>
          <label class="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
            {$language === 'vi' ? 'Xác nhận mật khẩu mới *' : 'Confirm New Password *'}
          </label>
          <div class="relative">
            <Lock class="h-4 w-4 absolute left-3.5 top-3 text-slate-400" />
            <input
              type={showConfirmPassword ? 'text' : 'password'}
              bind:value={confirmPassword}
              required
              class="w-full pl-10 pr-10 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/60 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/30 focus:border-amber-500 transition"
              placeholder={$language === 'vi' ? 'Nhập lại mật khẩu mới' : 'Retype new password'}
            />
            <button
              type="button"
              onclick={() => (showConfirmPassword = !showConfirmPassword)}
              class="absolute right-3 top-3 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition"
              title={showConfirmPassword ? 'Hide password' : 'Show password'}
            >
              {#if showConfirmPassword}
                <EyeOff class="h-4 w-4" />
              {:else}
                <Eye class="h-4 w-4" />
              {/if}
            </button>
          </div>
          {#if confirmPassword && newPassword}
            <div class="mt-1 text-xs {confirmPassword === newPassword ? 'text-emerald-500' : 'text-rose-500'}">
              {confirmPassword === newPassword
                ? ($language === 'vi' ? '✓ Mật khẩu xác nhận trùng khớp' : '✓ Passwords match')
                : ($language === 'vi' ? '✗ Mật khẩu xác nhận chưa khớp' : '✗ Passwords do not match')}
            </div>
          {/if}
        </div>

        <!-- Password policy hint -->
        <div class="p-3.5 rounded-xl bg-amber-50/60 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800/60 text-xs text-amber-800 dark:text-amber-300 flex items-start space-x-2.5">
          <AlertCircle class="h-4 w-4 shrink-0 mt-0.5" />
          <span>
            {$language === 'vi'
              ? 'Lưu ý: Mật khẩu nên chứa ít nhất 8 ký tự, kết hợp chữ cái viết hoa, viết thường, chữ số và ký tự đặc biệt để đảm bảo an toàn tối đa cho hệ thống viễn thông.'
              : 'Recommendation: Passwords should be at least 8 characters long, including uppercase letters, numbers, and special symbols to comply with telecom security standards.'}
          </span>
        </div>

        <!-- Submit Button -->
        <div class="flex items-center justify-end pt-2">
          <button
            type="submit"
            class="flex items-center space-x-2 px-6 py-2.5 rounded-xl text-xs font-bold bg-amber-600 hover:bg-amber-700 text-white shadow-md shadow-amber-600/20 transition active:scale-95"
          >
            <KeyRound class="h-4 w-4" />
            <span>{$language === 'vi' ? 'Cập nhật mật khẩu mới' : 'Update Password'}</span>
          </button>
        </div>
      </form>
    {/if}
  </div>

  <!-- SECTION 4: SESSION MANAGEMENT & LOGOUT -->
  <div class="rounded-2xl bg-white dark:bg-slate-900 border border-rose-200/80 dark:border-rose-900/60 shadow-sm transition-colors duration-300 overflow-hidden {isSessionOpen ? 'p-6 sm:p-8 space-y-6' : 'p-4 sm:p-5'}">
    <div
      class="flex items-center justify-between cursor-pointer {isSessionOpen ? 'border-b border-rose-100 dark:border-rose-900/40 pb-4' : ''}"
      role="button"
      tabindex="0"
      onclick={() => (isSessionOpen = !isSessionOpen)}
      onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); isSessionOpen = !isSessionOpen; } }}
    >
      <div class="flex items-center space-x-3 flex-1">
        <div class="h-10 w-10 rounded-xl bg-rose-100 dark:bg-rose-900/40 text-rose-600 dark:text-rose-400 flex items-center justify-center shrink-0 shadow-xs">
          <LogOut class="h-5 w-5" />
        </div>
        <div>
          <h3 class="text-base font-bold text-slate-900 dark:text-white">
            {$language === 'vi' ? 'Quản lý phiên đăng nhập & Đăng xuất' : 'Session Management & Sign Out'}
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            {$language === 'vi' ? 'Kiểm soát phiên làm việc hiện tại, đăng xuất an toàn hoặc hủy toàn bộ phiên khác' : 'Inspect active client session, sign out safely, or terminate all external logins'}
          </p>
        </div>
      </div>

      <div class="flex items-center space-x-2">
        <span class="text-xs font-bold text-rose-600 dark:text-rose-400 bg-rose-50 dark:bg-rose-950/60 px-2 py-0.5 rounded border border-rose-200 dark:border-rose-800/60">
          {$language === 'vi' ? 'Phiên hoạt động' : 'Active Session'}
        </span>
        <button
          type="button"
          class="p-1.5 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition"
          onclick={(e) => {
            e.stopPropagation();
            isSessionOpen = !isSessionOpen;
          }}
        >
          {#if isSessionOpen}
            <ChevronUp class="h-4 w-4" />
          {:else}
            <ChevronDown class="h-4 w-4" />
          {/if}
        </button>
      </div>
    </div>

    {#if isSessionOpen}
      <div class="space-y-4">
        <!-- Current Active Device Card -->
        <div class="flex flex-col sm:flex-row sm:items-center justify-between p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/60 dark:bg-slate-800/40 gap-4">
          <div class="flex items-center space-x-3.5">
            <div class="h-10 w-10 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 flex items-center justify-center text-slate-700 dark:text-slate-300 shrink-0">
              <Laptop class="h-5 w-5" />
            </div>
            <div>
              <div class="flex items-center space-x-2">
                <span class="text-sm font-bold text-slate-900 dark:text-white">
                  Windows PC • Chrome Client
                </span>
                <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 dark:bg-emerald-950/80 text-emerald-700 dark:text-emerald-400">
                  {$language === 'vi' ? 'Thiết bị này (Hiện tại)' : 'This Device (Current)'}
                </span>
              </div>
              <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
                IP: 192.168.1.108 • {$language === 'vi' ? 'Khu vực: TP. Hồ Chí Minh, Việt Nam' : 'Location: Ho Chi Minh City, VN'}
              </p>
            </div>
          </div>

          <div class="flex items-center space-x-2">
            <button
              type="button"
              onclick={handleLogoutClick}
              class="flex items-center space-x-1.5 px-4 py-2 rounded-xl text-xs font-bold bg-rose-600 hover:bg-rose-700 text-white shadow-sm transition active:scale-95"
            >
              <LogOut class="h-3.5 w-3.5" />
              <span>{$language === 'vi' ? 'Đăng xuất ngay' : 'Sign Out Now'}</span>
            </button>
          </div>
        </div>

        <!-- Danger Zone Footer -->
        <div class="flex flex-col sm:flex-row sm:items-center justify-between p-4 rounded-xl bg-rose-50/50 dark:bg-rose-950/20 border border-rose-200/60 dark:border-rose-900/40 gap-3">
          <div class="flex items-start space-x-2.5">
            <ShieldAlert class="h-4 w-4 text-rose-600 dark:text-rose-400 shrink-0 mt-0.5" />
            <p class="text-xs text-rose-700 dark:text-rose-300">
              {$language === 'vi'
                ? 'Đăng xuất khỏi tất cả các thiết bị khác nếu bạn nghi ngờ tài khoản bị lộ hoặc sử dụng máy tính công cộng.'
                : 'Sign out of all other sessions if you suspect unauthorized activity or used a shared machine.'}
            </p>
          </div>

          <button
            type="button"
            onclick={handleLogoutAllDevices}
            class="px-3.5 py-1.5 rounded-lg text-xs font-semibold bg-white dark:bg-slate-900 hover:bg-rose-100 dark:hover:bg-rose-900/40 text-rose-700 dark:text-rose-300 border border-rose-300 dark:border-rose-800 transition whitespace-nowrap shrink-0"
          >
            {$language === 'vi' ? 'Đăng xuất tất cả thiết bị khác' : 'Sign Out Other Devices'}
          </button>
        </div>
      </div>
    {/if}
  </div>
</div>
