<script lang="ts">
  import { navigate } from '../lib/router';
  import { languageStore } from '../context/LanguageContext';
  import { ShieldAlert, Home, ArrowLeft } from 'lucide-svelte';

  const { language } = languageStore;
</script>

<div class="min-h-full h-full w-full flex items-center justify-center p-6 relative overflow-hidden bg-gradient-to-br from-[#E0F1FF] to-[#C0E0FF] dark:from-[#1B2D40] dark:to-[#0F1C2E]">
  <!-- Decorative Background Elements -->
  <div class="absolute top-1/4 left-1/4 w-96 h-96 bg-sky-400/20 dark:bg-sky-500/10 rounded-full blur-[100px] pointer-events-none"></div>
  <div class="absolute bottom-1/4 right-1/4 w-96 h-96 bg-blue-500/20 dark:bg-blue-600/10 rounded-full blur-[100px] pointer-events-none"></div>

  <div class="relative z-10 max-w-2xl w-full">
    <!-- Glass Panel -->
    <div class="backdrop-blur-xl bg-white/40 dark:bg-[#1B2D40]/60 border border-white/40 dark:border-white/10 p-10 md:p-16 rounded-3xl shadow-2xl flex flex-col items-center text-center transform transition-all duration-500 hover:shadow-sky-500/10">
      
      <!-- Icon with pulse effect -->
      <div class="relative mb-8">
        <div class="absolute inset-0 bg-sky-500/20 rounded-full blur-xl animate-pulse"></div>
        <div class="relative bg-white dark:bg-[#2A3F54] p-5 rounded-2xl shadow-lg border border-sky-100 dark:border-sky-900/50">
          <ShieldAlert class="w-16 h-16 text-sky-500" strokeWidth={1.5} />
        </div>
      </div>

      <!-- 404 Header -->
      <h1 class="text-7xl md:text-9xl font-extrabold tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-sky-600 to-blue-600 dark:from-sky-400 dark:to-blue-400 mb-4 drop-shadow-sm">
        404
      </h1>

      <h2 class="text-2xl md:text-3xl font-bold text-gray-800 dark:text-gray-100 mb-4">
        {$language === 'vi' ? 'Mất Kết Nối Hoặc Không Tìm Thấy Trang' : 'Connection Lost or Page Not Found'}
      </h2>
      
      <p class="text-gray-600 dark:text-gray-400 mb-10 max-w-md mx-auto text-lg leading-relaxed">
        {$language === 'vi' 
          ? 'Hệ thống không thể tìm thấy trang bạn yêu cầu, hoặc đường truyền đã bị ngắt kết nối. Vui lòng kiểm tra lại đường dẫn.' 
          : 'The system cannot find the page you requested, or the connection has been lost. Please check the URL.'}
      </p>

      <!-- Action Buttons -->
      <div class="flex flex-col sm:flex-row gap-4 w-full sm:w-auto">
        <button 
          onclick={() => window.history.back()}
          class="flex items-center justify-center gap-2 px-6 py-3.5 rounded-xl font-medium text-gray-700 dark:text-gray-200 bg-white/50 dark:bg-white/5 hover:bg-white dark:hover:bg-white/10 border border-gray-200 dark:border-gray-700 transition-all shadow-sm hover:shadow"
        >
          <ArrowLeft class="w-5 h-5" />
          {$language === 'vi' ? 'Quay Lại' : 'Go Back'}
        </button>

        <button 
          onclick={() => navigate('/')}
          class="flex items-center justify-center gap-2 px-6 py-3.5 rounded-xl font-medium text-white bg-gradient-to-r from-sky-500 to-blue-600 hover:from-sky-400 hover:to-blue-500 transition-all shadow-lg hover:shadow-sky-500/25 transform hover:-translate-y-0.5"
        >
          <Home class="w-5 h-5" />
          {$language === 'vi' ? 'Về Trang Chủ' : 'Back to Home'}
        </button>
      </div>

    </div>
  </div>
</div>
