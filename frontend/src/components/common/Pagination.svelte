<script lang="ts">
  import { languageStore } from '../../context/LanguageContext';
  import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-svelte';

  interface Props {
    currentPage?: number;
    totalItems: number;
    pageSize?: number;
    onPageChange?: (page: number) => void;
    className?: string;
    compact?: boolean;
    showSummary?: boolean;
  }

  let {
    currentPage = $bindable(1),
    totalItems = 0,
    pageSize = $bindable(8),
    onPageChange,
    className = "",
    compact = false,
    showSummary = false,
  }: Props = $props();

  const { language } = languageStore;

  const totalPages = $derived(Math.max(1, Math.ceil(totalItems / pageSize)));

  // Ensure currentPage stays within valid range
  $effect(() => {
    if (currentPage > totalPages && totalPages > 0) {
      currentPage = totalPages;
    } else if (currentPage < 1) {
      currentPage = 1;
    }
  });

  function goToPage(page: number) {
    if (page < 1 || page > totalPages || page === currentPage) return;
    currentPage = page;
    onPageChange?.(page);

    // Khi chuyển trang: đưa màn hình lên đầu nội dung
    requestAnimationFrame(() => {
      try {
        const mainEl = document.querySelector('main');
        if (mainEl && mainEl.scrollTop > 0) {
          mainEl.scrollTo({ top: 0, behavior: 'smooth' });
        }
        if (window.scrollY > 0) {
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }
        if (document.documentElement.scrollTop > 0) {
          document.documentElement.scrollTo({ top: 0, behavior: 'smooth' });
        }
        if (document.body.scrollTop > 0) {
          document.body.scrollTo({ top: 0, behavior: 'smooth' });
        }
      } catch {
        window.scrollTo(0, 0);
      }
    });
  }

  const pageNumbers = $derived.by(() => {
    const pages: (number | string)[] = [];
    if (totalPages <= 7) {
      for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
      if (currentPage <= 3) {
        pages.push(1, 2, 3, 4, '...', totalPages);
      } else if (currentPage >= totalPages - 2) {
        pages.push(1, '...', totalPages - 3, totalPages - 2, totalPages - 1, totalPages);
      } else {
        pages.push(1, '...', currentPage - 1, currentPage, currentPage + 1, '...', totalPages);
      }
    }
    return pages;
  });
</script>

{#if totalItems > 0}
  <!-- Centered container without full-width borders/stripes -->
  <div class="w-full flex items-center justify-center py-4 px-2 select-none {className}">
    <!-- Khung ôm sát vừa vặn với cụm nút phân trang -->
    <div class="inline-flex items-center gap-1 sm:gap-1.5 p-1.5 rounded-2xl border border-slate-200 dark:border-slate-700/80 bg-white/95 dark:bg-slate-900/90 shadow-sm backdrop-blur-xs flex-wrap justify-center">
      <!-- First Page -->
      <button
        type="button"
        disabled={currentPage <= 1}
        onclick={() => goToPage(1)}
        title={$language === 'vi' ? 'Trang đầu' : 'First page'}
        class="inline-flex p-1.5 rounded-xl border border-slate-200/60 dark:border-slate-700/60 bg-slate-50/80 dark:bg-slate-800/80 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 cursor-pointer shadow-2xs"
      >
        <ChevronsLeft class="w-4 h-4" />
      </button>

      <!-- Previous Page -->
      <button
        type="button"
        disabled={currentPage <= 1}
        onclick={() => goToPage(currentPage - 1)}
        class="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl border border-slate-200/60 dark:border-slate-700/60 bg-slate-50/80 dark:bg-slate-800/80 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed transition text-slate-700 dark:text-slate-300 font-medium text-xs sm:text-sm cursor-pointer shadow-2xs"
      >
        <ChevronLeft class="w-4 h-4" />
        <span>{$language === 'vi' ? 'Trước' : 'Prev'}</span>
      </button>

      <!-- Number buttons -->
      <div class="flex items-center gap-1">
        {#each pageNumbers as p}
          {#if p === '...'}
            <span class="px-2 py-1 text-slate-400 dark:text-slate-500 text-xs">...</span>
          {:else}
            {@const pageNum = Number(p)}
            <button
              type="button"
              onclick={() => goToPage(pageNum)}
              class="min-w-8 h-8 px-2.5 rounded-xl text-xs sm:text-sm font-semibold transition flex items-center justify-center cursor-pointer shadow-2xs {currentPage === pageNum
                ? 'bg-sky-600 dark:bg-sky-500 text-white shadow-xs font-bold'
                : 'border border-slate-200/60 dark:border-slate-700/60 bg-slate-50/80 dark:bg-slate-800/80 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700'}"
            >
              {pageNum}
            </button>
          {/if}
        {/each}
      </div>

      <!-- Next Page -->
      <button
        type="button"
        disabled={currentPage >= totalPages}
        onclick={() => goToPage(currentPage + 1)}
        class="inline-flex items-center gap-1 px-3 py-1.5 rounded-xl border border-slate-200/60 dark:border-slate-700/60 bg-slate-50/80 dark:bg-slate-800/80 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed transition text-slate-700 dark:text-slate-300 font-medium text-xs sm:text-sm cursor-pointer shadow-2xs"
      >
        <span>{$language === 'vi' ? 'Sau' : 'Next'}</span>
        <ChevronRight class="w-4 h-4" />
      </button>

      <!-- Last Page -->
      <button
        type="button"
        disabled={currentPage >= totalPages}
        onclick={() => goToPage(totalPages)}
        title={$language === 'vi' ? 'Trang cuối' : 'Last page'}
        class="inline-flex p-1.5 rounded-xl border border-slate-200/60 dark:border-slate-700/60 bg-slate-50/80 dark:bg-slate-800/80 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed transition text-slate-600 dark:text-slate-300 cursor-pointer shadow-2xs"
      >
        <ChevronsRight class="w-4 h-4" />
      </button>
    </div>
  </div>
{/if}
