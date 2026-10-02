<script lang="ts">
  import {
    Activity,
    CheckCircle2,
    AlertTriangle,
    XCircle,
    Clock,
    Search,
    RefreshCw,
    X,
    Filter,
    Wifi,
    Radio,
    Zap,
    Download,
    Upload,
    ChevronDown,
    ChevronUp,
    Server,
    ShieldCheck,
    Cpu,
    User,
    MapPin,
    Phone,
    FileSpreadsheet,
    FileText,
    Share2,
    Calendar,
    ArrowUpRight,
    Terminal,
  } from 'lucide-svelte';
  import { toast } from 'svelte-sonner';
  import { languageStore } from '../../context/LanguageContext';
  import { getConnectionActivityLogs, type ConnectionActivityLog } from '../../lib/technicalApi';
  import type { Connection } from '../../types/nexus';

  interface DiagnosticRecord {
    id: string;
    timestamp: string;
    testType: 'Ping Loopback' | 'Optical Power' | 'DNS Latency' | 'Throughput Test' | 'Packet Loss Audit';
    status: 'Pass' | 'Warning' | 'Fail';
    pingMs: number;
    rxPowerDbm: number;
    txPowerDbm: number;
    downloadSpeedMbps: number;
    uploadSpeedMbps: number;
    packetLossPct: number;
    jitterMs: number;
    snrDb: number;
    technicianName: string;
    notes: string;
    recommendation?: string;
  }

  let {
    isOpen = false,
    connection = null,
    connectionsList = [],
    isPage = false,
    onClose,
  }: {
    isOpen?: boolean;
    connection?: Connection | null;
    connectionsList?: Connection[];
    isPage?: boolean;
    onClose?: () => void;
  } = $props();

  const { language } = languageStore;

  // Active selected connection (allow switching inside modal if opened generally or from list)
  let activeConn = $state<Connection | null>(null);

  // Sync activeConn when connection prop updates or falls back to first connection in list
  $effect(() => {
    if (connection) {
      activeConn = connection;
    } else if (connectionsList && connectionsList.length > 0 && !activeConn) {
      activeConn = connectionsList[0];
    }
  });

  // Active view tab in modal
  let activeModalTab = $state<'tests' | 'all-subscribers' | 'activity' | 'topology'>('tests');

  // Filter & Search states
  let searchQuery = $state('');
  let filterType = $state<'All' | DiagnosticRecord['testType']>('All');
  let filterStatus = $state<'All' | DiagnosticRecord['status']>('All');
  let allSubscribersSearch = $state('');

  const filteredSubscribers = $derived(
    (connectionsList || []).filter((c) => {
      const q = allSubscribersSearch.trim().toLowerCase();
      if (!q) return true;
      return (
        c.customerName.toLowerCase().includes(q) ||
        c.accountId.toLowerCase().includes(q) ||
        c.installationAddress.toLowerCase().includes(q) ||
        c.planName.toLowerCase().includes(q) ||
        c.customerPhone.toLowerCase().includes(q)
      );
    })
  );

  // Accordion expanded records
  let expandedRecordIds = $state<Set<string>>(new Set());

  // Running diagnostic state
  let isRunningTest = $state(false);
  let testProgress = $state(0);
  let testStageText = $state('');

  // Activity logs from backend API
  let activityLogs = $state<ConnectionActivityLog[]>([]);
  let isLoadingLogs = $state(false);

  // Generate realistic historical diagnostic records seeded by accountId
  function generateRecordsForAccount(accountId: string): DiagnosticRecord[] {
    const seed = accountId.split('').reduce((acc, c) => acc + c.charCodeAt(0), 0);
    const basePing = 3.5 + (seed % 4);
    const baseRx = -16.0 - ((seed % 10) * 0.4);

    return [
      {
        id: `DIAG-${accountId.slice(-6)}-01`,
        timestamp: '2026-09-23 19:45:10',
        testType: 'Ping Loopback',
        status: 'Pass',
        pingMs: Number(basePing.toFixed(1)),
        rxPowerDbm: Number(baseRx.toFixed(1)),
        txPowerDbm: 2.3,
        downloadSpeedMbps: 295.4,
        uploadSpeedMbps: 288.1,
        packetLossPct: 0.0,
        jitterMs: 0.6,
        snrDb: 35.2,
        technicianName: 'David Chen (NOC-01)',
        notes: 'Đo kiểm tín hiệu cổng quang PON và gateway ONU. Tín hiệu quang suy hao ở mức an toàn, độ trễ thấp.',
        recommendation: 'Đường truyền ổn định, đạt chứng chỉ chuẩn SLA Hạng A.',
      },
      {
        id: `DIAG-${accountId.slice(-6)}-02`,
        timestamp: '2026-09-23 14:12:05',
        testType: 'Throughput Test',
        status: 'Pass',
        pingMs: Number((basePing + 0.8).toFixed(1)),
        rxPowerDbm: Number(baseRx.toFixed(1)),
        txPowerDbm: 2.4,
        downloadSpeedMbps: 298.0,
        uploadSpeedMbps: 290.5,
        packetLossPct: 0.0,
        jitterMs: 0.8,
        snrDb: 34.8,
        technicianName: 'Nguyễn Văn Minh (Tech-02)',
        notes: 'Kiểm tra đo tải băng thông gói cước GPON. Băng thông đạt cam kết > 98%.',
        recommendation: 'Không phát hiện hiện tượng nghẽn cổ chai tại trạm OLT.',
      },
      {
        id: `DIAG-${accountId.slice(-6)}-03`,
        timestamp: '2026-09-22 09:30:22',
        testType: 'Optical Power',
        status: (seed % 3 === 0) ? 'Warning' : 'Pass',
        pingMs: Number((basePing + 1.2).toFixed(1)),
        rxPowerDbm: (seed % 3 === 0) ? -24.8 : Number((baseRx - 0.5).toFixed(1)),
        txPowerDbm: 2.1,
        downloadSpeedMbps: 270.2,
        uploadSpeedMbps: 260.0,
        packetLossPct: (seed % 3 === 0) ? 0.4 : 0.0,
        jitterMs: 1.2,
        snrDb: 31.0,
        technicianName: 'Trần Kỹ Thuật (NOC-03)',
        notes: (seed % 3 === 0)
          ? 'Công suất quang Rx tiệm cận ngưỡng nhạy quang (-24.8 dBm). Có dấu hiệu uốn cong sợi quang tại hộp chia Splitter.'
          : 'Công suất thu phát quang đạt tiêu chuẩn viễn thông quốc tế ITU-T G.984.',
        recommendation: (seed % 3 === 0)
          ? 'Cần bảo trì làm sạch đầu nối SC/APC tại hộp chia hoặc nối lại mối hàn suy hao.'
          : 'Hệ thống cáp quang đạt chuẩn.',
      },
      {
        id: `DIAG-${accountId.slice(-6)}-04`,
        timestamp: '2026-09-20 16:05:44',
        testType: 'DNS Latency',
        status: 'Pass',
        pingMs: Number((basePing + 0.3).toFixed(1)),
        rxPowerDbm: Number(baseRx.toFixed(1)),
        txPowerDbm: 2.3,
        downloadSpeedMbps: 292.0,
        uploadSpeedMbps: 285.0,
        packetLossPct: 0.0,
        jitterMs: 0.5,
        snrDb: 36.0,
        technicianName: 'Hệ thống giám sát tự động (Auto-Probe)',
        notes: 'Kiểm tra phân giải tên miền máy chủ DNS Nexus Primary (1.1.1.1) và Secondary (8.8.8.8).',
        recommendation: 'Thời gian phân giải DNS trung bình 2.1ms.',
      },
      {
        id: `DIAG-${accountId.slice(-6)}-05`,
        timestamp: '2026-09-18 10:15:30',
        testType: 'Packet Loss Audit',
        status: 'Pass',
        pingMs: Number(basePing.toFixed(1)),
        rxPowerDbm: Number(baseRx.toFixed(1)),
        txPowerDbm: 2.2,
        downloadSpeedMbps: 290.0,
        uploadSpeedMbps: 280.0,
        packetLossPct: 0.0,
        jitterMs: 0.7,
        snrDb: 35.5,
        technicianName: 'David Chen (NOC-01)',
        notes: 'Gửi 5000 gói tin ICMP kích thước 1500 bytes (MTU) kiểm tra tính toàn vẹn của đường truyền.',
        recommendation: 'Không phát hiện rớt gói tin trên đường cáp.',
      },
    ];
  }

  // Active diagnostic records list for current selected connection
  let testRecords = $state<DiagnosticRecord[]>([]);

  $effect(() => {
    if (activeConn?.accountId) {
      testRecords = generateRecordsForAccount(activeConn.accountId);
      loadBackendActivityLogs(activeConn.accountId);
    }
  });

  async function loadBackendActivityLogs(accId: string) {
    isLoadingLogs = true;
    try {
      activityLogs = await getConnectionActivityLogs(accId);
    } catch (err) {
      console.warn('Failed to load SQL logs for connection:', err);
      activityLogs = [];
    } finally {
      isLoadingLogs = false;
    }
  }

  // Filtered records
  const filteredRecords = $derived(
    testRecords.filter((rec) => {
      const matchSearch =
        searchQuery.trim() === '' ||
        rec.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
        rec.testType.toLowerCase().includes(searchQuery.toLowerCase()) ||
        rec.technicianName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        rec.notes.toLowerCase().includes(searchQuery.toLowerCase());

      const matchType = filterType === 'All' || rec.testType === filterType;
      const matchStatus = filterStatus === 'All' || rec.status === filterStatus;

      return matchSearch && matchType && matchStatus;
    })
  );

  function toggleRecordExpand(id: string) {
    const next = new Set(expandedRecordIds);
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    expandedRecordIds = next;
  }

  // Run new simulated real-time diagnostic test
  async function handleRunNewDiagnostic() {
    if (isRunningTest || !activeConn) return;

    isRunningTest = true;
    testProgress = 0;
    testStageText = $language === 'vi' ? 'Bắn tín hiệu kiểm tra Loopback Gateway...' : 'Probing Gateway Loopback...';

    const stages = [
      { progress: 25, textVi: 'Đang đo công suất phát và thu quang Rx/Tx qua OLT...', textEn: 'Measuring Optical Rx/Tx Power via OLT...' },
      { progress: 55, textVi: 'Kiểm tra tỷ lệ rớt gói tin và độ trễ phân giải DNS...', textEn: 'Auditing packet loss and DNS resolution...' },
      { progress: 85, textVi: 'Đo tải lưu lượng băng thông thực tế (Throughput test)...', textEn: 'Stress testing throughput bandwidth...' },
      { progress: 100, textVi: 'Hoàn tất đo kiểm, tổng hợp báo cáo kỹ thuật...', textEn: 'Finalizing diagnostic report...' },
    ];

    for (const stage of stages) {
      await new Promise((r) => setTimeout(r, 600));
      testProgress = stage.progress;
      testStageText = $language === 'vi' ? stage.textVi : stage.textEn;
    }

    await new Promise((r) => setTimeout(r, 400));

    // Create a new fresh record at the top
    const now = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');
    const timestamp = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}`;
    const newId = `DIAG-${activeConn.accountId.slice(-6)}-${pad(testRecords.length + 1)}`;

    const newRecord: DiagnosticRecord = {
      id: newId,
      timestamp,
      testType: 'Ping Loopback',
      status: 'Pass',
      pingMs: Number((3.2 + Math.random() * 2).toFixed(1)),
      rxPowerDbm: Number((-16.2 - Math.random() * 2).toFixed(1)),
      txPowerDbm: 2.4,
      downloadSpeedMbps: Number((295 + Math.random() * 5).toFixed(1)),
      uploadSpeedMbps: Number((285 + Math.random() * 5).toFixed(1)),
      packetLossPct: 0.0,
      jitterMs: 0.5,
      snrDb: 35.8,
      technicianName: 'David Chen (NOC Live)',
      notes: 'Đo kiểm toàn trình tự động tức thì. Cổng quang hoạt động bình thường, không suy hao tín hiệu.',
      recommendation: 'Chỉ số đo kiểm đạt xuất sắc, đạt chuẩn chất lượng cam kết SLA.',
    };

    testRecords = [newRecord, ...testRecords];
    expandedRecordIds = new Set([newId, ...expandedRecordIds]);
    isRunningTest = false;

    toast.success(
      $language === 'vi'
        ? `Đo kiểm đường truyền #${activeConn.accountId} hoàn tất thành công!`
        : `Diagnostic test for #${activeConn.accountId} completed successfully!`
    );
  }

  function exportRecordsToCsv() {
    if (!testRecords.length || !activeConn) return;

    const headers = ['Mã bản ghi', 'Thời gian', 'Loại kiểm tra', 'Kết quả', 'Ping (ms)', 'Rx Power (dBm)', 'Download (Mbps)', 'Upload (Mbps)', 'Mất gói (%)', 'Kỹ thuật viên'];
    const rows = testRecords.map((r) => [
      r.id,
      r.timestamp,
      r.testType,
      r.status,
      r.pingMs,
      r.rxPowerDbm,
      r.downloadSpeedMbps,
      r.uploadSpeedMbps,
      r.packetLossPct,
      `"${r.technicianName}"`,
    ]);

    const csvContent = 'data:text/csv;charset=utf-8,\uFEFF' + [headers.join(','), ...rows.map((e) => e.join(','))].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `Nexus_Test_Records_${activeConn.accountId}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    toast.success($language === 'vi' ? 'Đã xuất file CSV bản ghi đo kiểm mạng!' : 'Exported test records to CSV!');
  }
</script>

{#snippet modalBody()}
      <!-- MODAL TOP BAR / HEADER -->
      <div
        class="px-5 py-4 bg-gradient-to-r from-sky-600 via-blue-600 to-indigo-700 text-white flex items-center justify-between shadow-md shrink-0"
      >
        <div class="flex items-center space-x-3 min-w-0">
          <div class="h-10 w-10 rounded-xl bg-white/20 backdrop-blur-md flex items-center justify-center border border-white/30 shrink-0">
            <Activity class="h-5 w-5 text-white" />
          </div>
          <div class="min-w-0">
            <div class="flex items-center space-x-2">
              <h3 class="font-black text-base sm:text-lg tracking-tight truncate">
                {$language === 'vi' ? 'Hồ sơ & Bản ghi đo kiểm kết nối mạng' : 'Customer Network Diagnostic Records'}
              </h3>
              <span class="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-white/25 text-white border border-white/30 shrink-0">
                PRO-DIAG
              </span>
            </div>
            <p class="text-xs text-sky-100/90 truncate">
              {$language === 'vi'
                ? 'Bảng dữ liệu đo kiểm cáp quang, độ trễ Ping, suy hao Rx, băng thông và nhật ký thao tác thuê bao.'
                : 'Real-time telemetry, optical attenuation, latency, throughput and circuit logs.'}
            </p>
          </div>
        </div>

        <div class="flex items-center space-x-2">
          <!-- Run Diagnostic Button -->
          <button
            onclick={handleRunNewDiagnostic}
            disabled={isRunningTest || !activeConn}
            class="hidden sm:inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-white/20 hover:bg-white/30 text-white text-xs font-bold transition active:scale-95 border border-white/30 disabled:opacity-50 cursor-pointer"
          >
            <RefreshCw class="h-3.5 w-3.5 {isRunningTest ? 'animate-spin' : ''}" />
            <span>{$language === 'vi' ? 'Đo kiểm mới' : 'Run Diagnostic'}</span>
          </button>

          <!-- Export CSV -->
          <button
            onclick={exportRecordsToCsv}
            class="hidden sm:inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-white/10 hover:bg-white/20 text-white text-xs font-medium transition border border-white/20 cursor-pointer"
            title={$language === 'vi' ? 'Xuất bản ghi CSV' : 'Export to CSV'}
          >
            <Download class="h-3.5 w-3.5" />
            <span class="hidden md:inline">CSV</span>
          </button>

          {#if !isPage}
            <!-- Close Modal -->
            <button
              onclick={onClose}
              class="h-8 w-8 rounded-full bg-white/20 hover:bg-white/30 text-white flex items-center justify-center transition active:scale-90 cursor-pointer"
              title={$language === 'vi' ? 'Đóng' : 'Close'}
            >
              <X class="h-5 w-5" />
            </button>
          {/if}
        </div>
      </div>

      <!-- CIRCUIT SELECTOR & SUBSCRIBER BANNER -->
      <div class="px-5 py-3.5 bg-slate-50 dark:bg-slate-950/70 border-b border-slate-200 dark:border-slate-800 shrink-0">
        <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
          <!-- Subscriber Selector Dropdown -->
          <div class="flex items-center space-x-3 flex-1 min-w-0">
            <span class="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider shrink-0">
              {$language === 'vi' ? 'Chọn thuê bao:' : 'Subscriber:'}
            </span>

            {#if connectionsList && connectionsList.length > 0}
              <div class="relative flex-1 max-w-md">
                <select
                  value={activeConn?.accountId}
                  onchange={(e) => {
                    const found = connectionsList.find((c) => c.accountId === e.currentTarget.value);
                    if (found) activeConn = found;
                  }}
                  class="w-full text-xs font-bold bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-slate-800 dark:text-slate-100 focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
                >
                  {#each connectionsList as conn}
                    <option value={conn.accountId}>
                      {conn.customerName} - {conn.accountId} ({conn.planName})
                    </option>
                  {/each}
                </select>
              </div>
            {:else if activeConn}
              <div class="font-bold text-sm text-slate-900 dark:text-white font-mono flex items-center space-x-2">
                <span>{activeConn.customerName}</span>
                <span class="text-sky-600 dark:text-sky-400">({activeConn.accountId})</span>
              </div>
            {:else}
              <div class="text-xs text-slate-400">{$language === 'vi' ? 'Không có thuê bao nào.' : 'No active subscriber selected.'}</div>
            {/if}
          </div>

          <!-- Quick Circuit Info Tags -->
          {#if activeConn}
            <div class="flex flex-wrap items-center gap-2 text-xs font-mono">
              <span class="px-2 py-0.5 rounded-md bg-sky-100 dark:bg-sky-950 text-sky-800 dark:text-sky-300 border border-sky-200 dark:border-sky-800 font-semibold flex items-center space-x-1">
                <Wifi class="h-3 w-3" />
                <span>{activeConn.portNumber || 'PON-01/04'}</span>
              </span>

              <span class="px-2 py-0.5 rounded-md bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300 border border-slate-300 dark:border-slate-700">
                IP: {activeConn.ipAddress || '198.51.100.42'}
              </span>

              <span class="px-2 py-0.5 rounded-md {activeConn.status === 'Active' ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800' : 'bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800'} font-bold">
                {activeConn.status === 'Active' ? ($language === 'vi' ? 'Hoạt động' : 'Active') : activeConn.status}
              </span>
            </div>
          {/if}
        </div>
      </div>

      <!-- RUNNING TEST PROGRESS BAR OVERLAY -->
      {#if isRunningTest}
        <div class="p-3 bg-sky-50 dark:bg-sky-950/80 border-b border-sky-200 dark:border-sky-900 animate-in fade-in shrink-0">
          <div class="flex items-center justify-between text-xs font-bold text-sky-800 dark:text-sky-200 mb-1.5">
            <span class="flex items-center space-x-1.5">
              <RefreshCw class="h-3.5 w-3.5 animate-spin text-sky-600" />
              <span>{testStageText}</span>
            </span>
            <span class="font-mono">{testProgress}%</span>
          </div>
          <div class="w-full bg-sky-200 dark:bg-sky-900 rounded-full h-2 overflow-hidden">
            <div
              class="bg-gradient-to-r from-sky-500 to-indigo-600 h-full rounded-full transition-all duration-300"
              style="width: {testProgress}%"
            ></div>
          </div>
        </div>
      {/if}

      <!-- MODAL BODY: SCROLLABLE CONTENT -->
      <div class="p-5 overflow-y-auto space-y-6 flex-1">
        {#if activeConn}
          <!-- TOP TELEMETRY SUMMARY CARDS -->
          <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
            <!-- 1. Ping / Latency -->
            <div class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-800">
              <div class="text-[10px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between">
                <span>{$language === 'vi' ? 'Độ trễ Ping' : 'Ping Latency'}</span>
                <Radio class="h-3.5 w-3.5 text-emerald-500" />
              </div>
              <div class="text-xl font-black font-mono text-emerald-600 dark:text-emerald-400 mt-1">
                {testRecords[0]?.pingMs ?? 4.2} <span class="text-xs font-normal">ms</span>
              </div>
              <div class="text-[10px] text-emerald-600/80 dark:text-emerald-400/80 mt-0.5 font-medium flex items-center space-x-1">
                <CheckCircle2 class="h-2.5 w-2.5 inline" />
                <span>{$language === 'vi' ? 'Rất tốt (SLA A)' : 'Excellent'}</span>
              </div>
            </div>

            <!-- 2. Optical Rx Power -->
            <div class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-800">
              <div class="text-[10px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between">
                <span>{$language === 'vi' ? 'Thu quang Rx' : 'Optical Rx'}</span>
                <Zap class="h-3.5 w-3.5 text-sky-500" />
              </div>
              <div class="text-xl font-black font-mono text-slate-900 dark:text-white mt-1">
                {testRecords[0]?.rxPowerDbm ?? -16.8} <span class="text-xs font-normal">dBm</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5">
                {$language === 'vi' ? 'Chuẩn: -8 đến -27' : 'Norm: -8 to -27'}
              </div>
            </div>

            <!-- 3. Throughput Download -->
            <div class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-800">
              <div class="text-[10px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between">
                <span>{$language === 'vi' ? 'Tốc độ tải về' : 'Download'}</span>
                <Download class="h-3.5 w-3.5 text-blue-500" />
              </div>
              <div class="text-xl font-black font-mono text-blue-600 dark:text-blue-400 mt-1">
                {testRecords[0]?.downloadSpeedMbps ?? 295.4} <span class="text-xs font-normal">Mbps</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5">
                {$language === 'vi' ? 'Đạt 98.5% gói' : '98.5% of SLA'}
              </div>
            </div>

            <!-- 4. Throughput Upload -->
            <div class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-800">
              <div class="text-[10px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between">
                <span>{$language === 'vi' ? 'Tốc độ tải lên' : 'Upload'}</span>
                <Upload class="h-3.5 w-3.5 text-indigo-500" />
              </div>
              <div class="text-xl font-black font-mono text-indigo-600 dark:text-indigo-400 mt-1">
                {testRecords[0]?.uploadSpeedMbps ?? 288.1} <span class="text-xs font-normal">Mbps</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5">
                {$language === 'vi' ? 'Đạt chuẩn GPON' : 'GPON standard'}
              </div>
            </div>

            <!-- 5. Packet Loss -->
            <div class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-800">
              <div class="text-[10px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between">
                <span>{$language === 'vi' ? 'Mất gói tin' : 'Packet Loss'}</span>
                <ShieldCheck class="h-3.5 w-3.5 text-emerald-500" />
              </div>
              <div class="text-xl font-black font-mono text-emerald-600 dark:text-emerald-400 mt-1">
                {testRecords[0]?.packetLossPct ?? 0.0}%
              </div>
              <div class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5">
                {$language === 'vi' ? 'Hoàn hảo (0%)' : 'Zero loss'}
              </div>
            </div>

            <!-- 6. Jitter / Biến thiên trễ -->
            <div class="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-800">
              <div class="text-[10px] font-mono text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between">
                <span>{$language === 'vi' ? 'Độ ổn định Jitter' : 'Jitter'}</span>
                <Cpu class="h-3.5 w-3.5 text-amber-500" />
              </div>
              <div class="text-xl font-black font-mono text-slate-900 dark:text-white mt-1">
                {testRecords[0]?.jitterMs ?? 0.6} <span class="text-xs font-normal">ms</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5">
                SNR: {testRecords[0]?.snrDb ?? 35.2} dB
              </div>
            </div>
          </div>
        {/if}

        <!-- TAB NAVIGATION (TESTS vs SUBSCRIBERS MATRIX vs AUDIT LOGS vs CIRCUIT TOPOLOGY) -->
        <div class="flex items-center space-x-4 border-b border-slate-200 dark:border-slate-800 text-xs font-bold overflow-x-auto">
          <button
            onclick={() => (activeModalTab = 'tests')}
            class="pb-2.5 font-bold transition flex items-center space-x-2 border-b-2 {activeModalTab === 'tests'
              ? 'border-sky-600 text-sky-600 dark:text-sky-400'
              : 'border-transparent text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'} cursor-pointer"
          >
            <Activity class="h-4 w-4" />
            <span>{$language === 'vi' ? 'Bản ghi đo kiểm' : 'Diagnostic Records'}</span>
            <span class="px-1.5 py-0.5 rounded-full text-[10px] font-mono bg-sky-100 dark:bg-sky-950 text-sky-700 dark:text-sky-300 font-bold">
              {testRecords.length}
            </span>
          </button>

          {#if connectionsList && connectionsList.length > 0}
            <button
              onclick={() => (activeModalTab = 'all-subscribers')}
              class="pb-2.5 font-bold transition flex items-center space-x-2 border-b-2 {activeModalTab === 'all-subscribers'
                ? 'border-sky-600 text-sky-600 dark:text-sky-400'
                : 'border-transparent text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'} cursor-pointer"
            >
              <FileSpreadsheet class="h-4 w-4" />
              <span>{$language === 'vi' ? 'Tổng hợp toàn bộ khách hàng' : 'All Subscribers Matrix'}</span>
              <span class="px-1.5 py-0.5 rounded-full text-[10px] font-mono bg-sky-100 dark:bg-sky-950/80 text-sky-700 dark:text-sky-300 font-bold">
                {connectionsList.length}
              </span>
            </button>
          {/if}

          <button
            onclick={() => (activeModalTab = 'activity')}
            class="pb-2.5 font-bold transition flex items-center space-x-2 border-b-2 {activeModalTab === 'activity'
              ? 'border-sky-600 text-sky-600 dark:text-sky-400'
              : 'border-transparent text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'} cursor-pointer"
          >
            <Clock class="h-4 w-4" />
            <span>{$language === 'vi' ? 'Nhật ký thao tác kết nối' : 'Connection Activity Logs'}</span>
            <span class="px-1.5 py-0.5 rounded-full text-[10px] font-mono bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 font-bold">
              {activityLogs.length}
            </span>
          </button>

          <button
            onclick={() => (activeModalTab = 'topology')}
            class="pb-2.5 font-bold transition flex items-center space-x-2 border-b-2 {activeModalTab === 'topology'
              ? 'border-sky-600 text-sky-600 dark:text-sky-400'
              : 'border-transparent text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'} cursor-pointer"
          >
            <Server class="h-4 w-4" />
            <span>{$language === 'vi' ? 'Sơ đồ mạch & Cấu hình thiết bị' : 'Circuit Topology & Hardware'}</span>
          </button>
        </div>

        <!-- TAB CONTENT 1: DIAGNOSTIC TEST RECORDS -->
        {#if activeModalTab === 'tests'}
          <div class="space-y-4">
            <!-- Filter & Search Controls -->
            <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
              <div class="relative flex-1">
                <Search class="h-4 w-4 text-slate-400 absolute left-3 top-2.5" />
                <input
                  type="text"
                  bind:value={searchQuery}
                  placeholder={$language === 'vi' ? 'Tìm theo mã bản ghi, loại kiểm tra, kỹ thuật viên...' : 'Search by test ID, type, technician...'}
                  class="w-full pl-9 pr-3 py-1.5 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
                />
              </div>

              <div class="flex items-center space-x-2 shrink-0">
                <select
                  bind:value={filterType}
                  class="text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl px-2.5 py-1.5 text-slate-700 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-sky-500"
                >
                  <option value="All">{$language === 'vi' ? 'Tất cả loại đo kiểm' : 'All Test Types'}</option>
                  <option value="Ping Loopback">Ping Loopback</option>
                  <option value="Optical Power">Optical Power (Quang)</option>
                  <option value="Throughput Test">Throughput (Tải)</option>
                  <option value="DNS Latency">DNS Latency</option>
                  <option value="Packet Loss Audit">Packet Loss (Mất gói)</option>
                </select>

                <select
                  bind:value={filterStatus}
                  class="text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl px-2.5 py-1.5 text-slate-700 dark:text-slate-200 focus:outline-none focus:ring-1 focus:ring-sky-500"
                >
                  <option value="All">{$language === 'vi' ? 'Mọi kết quả' : 'All Statuses'}</option>
                  <option value="Pass">Pass (Đạt tiêu chuẩn)</option>
                  <option value="Warning">Warning (Cảnh báo)</option>
                  <option value="Fail">Fail (Lỗi mạng)</option>
                </select>
              </div>
            </div>

            <!-- Test Records List Table / Accordion Cards -->
            {#if filteredRecords.length === 0}
              <div class="p-8 text-center bg-slate-50 dark:bg-slate-800/40 rounded-xl border border-dashed border-slate-300 dark:border-slate-700">
                <Activity class="h-8 w-8 text-slate-400 mx-auto mb-2 opacity-50" />
                <p class="text-xs text-slate-500 dark:text-slate-400">
                  {$language === 'vi' ? 'Không tìm thấy bản ghi đo kiểm nào phù hợp với bộ lọc.' : 'No diagnostic test records found matching criteria.'}
                </p>
              </div>
            {:else}
              <div class="space-y-2.5">
                {#each filteredRecords as rec (rec.id)}
                  {@const isExp = expandedRecordIds.has(rec.id)}
                  <div
                    class="rounded-xl border transition overflow-hidden {isExp
                      ? 'border-sky-300 dark:border-sky-800 bg-sky-50/20 dark:bg-sky-950/20 shadow-xs'
                      : 'border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:border-slate-300 dark:hover:border-slate-700'}"
                  >
                    <!-- Row Summary -->
                    <button
                      type="button"
                      onclick={() => toggleRecordExpand(rec.id)}
                      class="w-full p-3.5 text-left flex flex-col md:flex-row md:items-center justify-between gap-3 cursor-pointer"
                    >
                      <div class="flex items-center space-x-3 min-w-0">
                        <!-- Status Icon Indicator -->
                        <div
                          class="h-8 w-8 rounded-lg flex items-center justify-center shrink-0 {rec.status === 'Pass'
                            ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-600 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800'
                            : rec.status === 'Warning'
                              ? 'bg-amber-100 dark:bg-amber-950 text-amber-600 dark:text-amber-400 border border-amber-200 dark:border-amber-800'
                              : 'bg-rose-100 dark:bg-rose-950 text-rose-600 dark:text-rose-400 border border-rose-200 dark:border-rose-800'}"
                        >
                          {#if rec.status === 'Pass'}
                            <CheckCircle2 class="h-4 w-4" />
                          {:else if rec.status === 'Warning'}
                            <AlertTriangle class="h-4 w-4" />
                          {:else}
                            <XCircle class="h-4 w-4" />
                          {/if}
                        </div>

                        <div class="min-w-0">
                          <div class="flex items-center space-x-2">
                            <span class="font-bold text-xs text-slate-900 dark:text-white font-mono">{rec.id}</span>
                            <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider {rec.status === 'Pass' ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300' : rec.status === 'Warning' ? 'bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-300' : 'bg-rose-100 dark:bg-rose-950 text-rose-700 dark:text-rose-300'}">
                              {rec.status}
                            </span>
                            <span class="font-semibold text-xs text-slate-700 dark:text-slate-300 truncate">{rec.testType}</span>
                          </div>
                          <div class="text-[11px] text-slate-400 dark:text-slate-500 font-mono mt-0.5">
                            {rec.timestamp} • {rec.technicianName}
                          </div>
                        </div>
                      </div>

                      <!-- Key Numerical Badges -->
                      <div class="flex items-center space-x-4 shrink-0 text-xs font-mono">
                        <div>
                          <span class="text-slate-400 text-[10px] block">PING</span>
                          <span class="font-bold text-slate-800 dark:text-slate-200">{rec.pingMs} ms</span>
                        </div>
                        <div>
                          <span class="text-slate-400 text-[10px] block">RX POWER</span>
                          <span class="font-bold {rec.rxPowerDbm < -24 ? 'text-amber-600 dark:text-amber-400' : 'text-slate-800 dark:text-slate-200'}">
                            {rec.rxPowerDbm} dBm
                          </span>
                        </div>
                        <div>
                          <span class="text-slate-400 text-[10px] block">SPEED</span>
                          <span class="font-bold text-blue-600 dark:text-blue-400">{rec.downloadSpeedMbps} M</span>
                        </div>
                        <div class="text-slate-400">
                          {#if isExp}
                            <ChevronUp class="h-4 w-4" />
                          {:else}
                            <ChevronDown class="h-4 w-4" />
                          {/if}
                        </div>
                      </div>
                    </button>

                    <!-- Expanded Details Panel -->
                    {#if isExp}
                      <div class="px-4 pb-4 pt-2 border-t border-slate-100 dark:border-slate-800/80 bg-slate-50/50 dark:bg-slate-900/50 space-y-3 text-xs">
                        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 p-3 bg-white dark:bg-slate-950 rounded-xl border border-slate-200 dark:border-slate-800 font-mono">
                          <div>
                            <span class="text-slate-400 text-[10px] block">TX POWER</span>
                            <span class="font-bold text-slate-700 dark:text-slate-300">+{rec.txPowerDbm} dBm</span>
                          </div>
                          <div>
                            <span class="text-slate-400 text-[10px] block">PACKET LOSS</span>
                            <span class="font-bold {rec.packetLossPct > 0 ? 'text-rose-600' : 'text-emerald-600'}">{rec.packetLossPct}%</span>
                          </div>
                          <div>
                            <span class="text-slate-400 text-[10px] block">JITTER</span>
                            <span class="font-bold text-slate-700 dark:text-slate-300">{rec.jitterMs} ms</span>
                          </div>
                          <div>
                            <span class="text-slate-400 text-[10px] block">SNR RATIO</span>
                            <span class="font-bold text-slate-700 dark:text-slate-300">{rec.snrDb} dB</span>
                          </div>
                        </div>

                        <div class="space-y-1.5">
                          <div class="font-semibold text-slate-700 dark:text-slate-300 flex items-center space-x-1.5">
                            <Terminal class="h-3.5 w-3.5 text-sky-500" />
                            <span>{$language === 'vi' ? 'Chi tiết phân tích & Nhật ký kỹ thuật:' : 'Technical Diagnosis & Observation:'}</span>
                          </div>
                          <p class="text-slate-600 dark:text-slate-400 leading-relaxed bg-white dark:bg-slate-950 p-2.5 rounded-lg border border-slate-200 dark:border-slate-800">
                            {rec.notes}
                          </p>

                          {#if rec.recommendation}
                            <div class="p-2.5 rounded-lg bg-sky-50 dark:bg-sky-950/40 border border-sky-200 dark:border-sky-800/60 text-sky-800 dark:text-sky-300 text-[11px]">
                              <span class="font-bold">{$language === 'vi' ? 'Khuyến nghị hành động:' : 'Recommendation:'}</span>
                              <p class="mt-0.5">
                                {rec.recommendation}
                              </p>
                            </div>
                          {/if}
                        </div>
                      </div>
                    {/if}
                  </div>
                {/each}
              </div>
            {/if}
          </div>

        <!-- TAB CONTENT: ALL SUBSCRIBERS MASTER TEST MATRIX -->
        {:else if activeModalTab === 'all-subscribers'}
          <div class="space-y-4">
            <div class="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
              <div class="relative flex-1">
                <Search class="h-4 w-4 text-slate-400 absolute left-3 top-2.5" />
                <input
                  type="text"
                  bind:value={allSubscribersSearch}
                  placeholder={$language === 'vi' ? 'Tìm theo tên khách hàng, mã tài khoản, địa chỉ, gói cước...' : 'Search by subscriber name, account ID, address...'}
                  class="w-full pl-9 pr-3 py-1.5 text-xs bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-sky-500"
                />
              </div>

              <div class="flex items-center space-x-2 text-xs text-slate-500">
                <span class="font-bold text-slate-800 dark:text-slate-200">{filteredSubscribers.length}</span>
                <span>{$language === 'vi' ? 'thuê bao hiển thị' : 'subscribers shown'}</span>
              </div>
            </div>

            <div class="rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-xs">
              <div class="overflow-x-auto">
                <table class="w-full text-left text-xs">
                  <thead class="bg-slate-50 dark:bg-slate-950/80 text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase border-b border-slate-200 dark:border-slate-800">
                    <tr>
                      <th class="py-3 px-4">{$language === 'vi' ? 'Mã TK / Khách hàng' : 'Account / Subscriber'}</th>
                      <th class="py-3 px-3">{$language === 'vi' ? 'Địa chỉ lắp đặt' : 'Address'}</th>
                      <th class="py-3 px-3">{$language === 'vi' ? 'Gói cước / Cổng OLT' : 'Plan / OLT Port'}</th>
                      <th class="py-3 px-3">{$language === 'vi' ? 'Trạng thái' : 'Status'}</th>
                      <th class="py-3 px-3">{$language === 'vi' ? 'Độ trễ Ping' : 'Ping'}</th>
                      <th class="py-3 px-3">{$language === 'vi' ? 'Công suất Rx' : 'Rx Power'}</th>
                      <th class="py-3 px-4 text-right">{$language === 'vi' ? 'Thao tác kỹ thuật' : 'Action'}</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-slate-100 dark:divide-slate-800 bg-white dark:bg-slate-900">
                    {#if filteredSubscribers.length === 0}
                      <tr>
                        <td colspan="7" class="py-8 text-center text-slate-400 text-xs">
                          {$language === 'vi' ? 'Không tìm thấy khách hàng nào khớp với tìm kiếm.' : 'No subscribers found matching filter.'}
                        </td>
                      </tr>
                    {:else}
                      {#each filteredSubscribers as sub (sub.accountId)}
                        <tr class="hover:bg-slate-50 dark:hover:bg-slate-800/50 transition {activeConn?.accountId === sub.accountId ? 'bg-sky-50/50 dark:bg-sky-950/30' : ''}">
                          <td class="py-3 px-4">
                            <div class="font-bold text-slate-900 dark:text-white flex items-center space-x-1.5">
                              <span>{sub.customerName}</span>
                              {#if activeConn?.accountId === sub.accountId}
                                <span class="px-1.5 py-0.2 text-[9px] font-bold bg-sky-100 dark:bg-sky-900 text-sky-700 dark:text-sky-300 rounded">
                                  {$language === 'vi' ? 'Đang chọn' : 'Selected'}
                                </span>
                              {/if}
                            </div>
                            <div class="font-mono text-[11px] text-sky-600 dark:text-sky-400 font-semibold">{sub.accountId}</div>
                            <div class="text-[10px] text-slate-400">{sub.customerPhone}</div>
                          </td>
                          <td class="py-3 px-3 text-slate-600 dark:text-slate-300 max-w-[180px] truncate" title={sub.installationAddress}>
                            {sub.installationAddress}
                          </td>
                          <td class="py-3 px-3">
                            <div class="font-semibold text-slate-800 dark:text-slate-200">{sub.planName}</div>
                            <div class="text-[11px] font-mono text-slate-400">{sub.portNumber || 'PON-01/04'}</div>
                          </td>
                          <td class="py-3 px-3">
                            <span class="px-2 py-0.5 rounded-full text-[10px] font-bold {sub.status === 'Active' ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300' : 'bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300'}">
                              {sub.status === 'Active' ? ($language === 'vi' ? 'Hoạt động' : 'Active') : sub.status}
                            </span>
                          </td>
                          <td class="py-3 px-3 font-mono font-bold text-emerald-600 dark:text-emerald-400">
                            4.2 ms
                          </td>
                          <td class="py-3 px-3 font-mono text-slate-600 dark:text-slate-300">
                            -16.8 dBm
                          </td>
                          <td class="py-3 px-4 text-right whitespace-nowrap">
                            <button
                              type="button"
                              onclick={() => {
                                activeConn = sub;
                                activeModalTab = 'tests';
                              }}
                              class="px-2.5 py-1.5 rounded-lg text-xs font-bold bg-sky-50 dark:bg-sky-950/60 hover:bg-sky-100 dark:hover:bg-sky-900 text-sky-700 dark:text-sky-300 border border-sky-300 dark:border-sky-800 transition mr-1.5 cursor-pointer"
                            >
                              {$language === 'vi' ? 'Xem chi tiết' : 'View Tests'}
                            </button>
                            <button
                              type="button"
                              onclick={async () => {
                                activeConn = sub;
                                activeModalTab = 'tests';
                                await handleRunNewDiagnostic();
                              }}
                              class="px-2.5 py-1.5 rounded-lg text-xs font-bold bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-700 hover:to-blue-700 text-white shadow-xs transition cursor-pointer"
                            >
                              {$language === 'vi' ? 'Đo kiểm ngay' : 'Test Now'}
                            </button>
                          </td>
                        </tr>
                      {/each}
                    {/if}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

        <!-- TAB CONTENT 2: BACKEND ACTIVITY LOGS -->
        {:else if activeModalTab === 'activity'}
          <div class="space-y-4">
            <div class="flex items-center justify-between">
              <div>
                <h4 class="font-bold text-sm text-slate-900 dark:text-white">
                  {$language === 'vi' ? 'Nhật ký thao tác trạng thái đường truyền (SQL Audit)' : 'Connection Status Change & Audit Logs'}
                </h4>
                <p class="text-xs text-slate-500">
                  {$language === 'vi'
                    ? 'Ghi lại mọi thay đổi trạng thái cổng mạch, lý do can thiệp và nhân viên kỹ thuật thực hiện.'
                    : 'Records every port status change, engineering justification and performing technician.'}
                </p>
              </div>

              <button
                onclick={() => activeConn && loadBackendActivityLogs(activeConn.accountId)}
                disabled={isLoadingLogs}
                class="px-3 py-1.5 text-xs font-bold bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-lg transition flex items-center space-x-1.5 cursor-pointer"
              >
                <RefreshCw class="h-3.5 w-3.5 {isLoadingLogs ? 'animate-spin' : ''}" />
                <span>{$language === 'vi' ? 'Tải lại' : 'Refresh'}</span>
              </button>
            </div>

            {#if isLoadingLogs}
              <div class="py-12 text-center text-slate-400">
                <RefreshCw class="h-6 w-6 animate-spin mx-auto mb-2 text-sky-500" />
                <p class="text-xs">{$language === 'vi' ? 'Đang truy vấn lịch sử audit từ SQL Server...' : 'Querying audit history from SQL Server...'}</p>
              </div>
            {:else if activityLogs.length === 0}
              <div class="p-8 text-center bg-slate-50 dark:bg-slate-800/40 rounded-xl border border-dashed border-slate-300 dark:border-slate-700">
                <Clock class="h-8 w-8 text-slate-400 mx-auto mb-2 opacity-50" />
                <p class="text-xs text-slate-500 dark:text-slate-400">
                  {$language === 'vi' ? 'Chưa ghi nhận sự kiện chuyển đổi trạng thái nào cho đường truyền này.' : 'No status change events logged for this connection yet.'}
                </p>
              </div>
            {:else}
              <div class="relative border-l-2 border-slate-200 dark:border-slate-800 ml-4 space-y-4 py-2">
                {#each activityLogs as log}
                  <div class="relative pl-6">
                    <span class="absolute -left-[9px] top-1.5 h-4 w-4 rounded-full border-2 border-white dark:border-slate-900 bg-sky-500"></span>
                    <div class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-800">
                      <div class="flex items-center justify-between text-xs mb-1">
                        <span class="font-bold text-slate-800 dark:text-slate-200">{log.actionType || 'Thay đổi trạng thái cổng'}</span>
                        <span class="font-mono text-[10px] text-slate-400">{log.timestamp ? new Date(log.timestamp).toLocaleString($language === 'vi' ? 'vi-VN' : 'en-US') : ''}</span>
                      </div>
                      <div class="text-[11px] text-slate-600 dark:text-slate-400">
                        {$language === 'vi' ? 'Từ:' : 'From:'} <span class="font-semibold">{log.oldValue || 'N/A'}</span>
                        ➔ {$language === 'vi' ? 'Sang:' : 'To:'} <span class="font-bold text-sky-600 dark:text-sky-400">{log.newValue || 'N/A'}</span>
                      </div>
                      {#if log.reason}
                        <div class="text-[11px] text-slate-500 dark:text-slate-400 mt-1 italic bg-white dark:bg-slate-900 p-2 rounded border border-slate-200 dark:border-slate-800">
                          "{log.reason}"
                        </div>
                      {/if}
                      <div class="text-[10px] font-mono text-slate-400 mt-1.5">
                        {$language === 'vi' ? 'Kỹ thuật viên:' : 'Technician:'} {log.performedByName || log.performedBy || 'David Chen'}
                      </div>
                    </div>
                  </div>
                {/each}
              </div>
            {/if}
          </div>

        <!-- TAB CONTENT 3: CIRCUIT TOPOLOGY & HARDWARE -->
        {:else if activeModalTab === 'topology'}
          <div class="space-y-4">
            <div class="p-4 bg-slate-50 dark:bg-slate-800/40 rounded-xl border border-slate-200 dark:border-slate-800">
              <h4 class="font-bold text-sm text-slate-900 dark:text-white mb-2">
                {$language === 'vi' ? 'Sơ đồ định tuyến vật lý quang học (Optical Circuit Diagram)' : 'Optical Physical Topology Diagram'}
              </h4>

              <!-- Visual Flow Diagram -->
              <div class="grid grid-cols-1 md:grid-cols-4 gap-3 text-center my-4 font-mono text-xs">
                <!-- Node 1: OLT -->
                <div class="p-3 bg-white dark:bg-slate-900 rounded-xl border-2 border-sky-400 dark:border-sky-600 shadow-xs">
                  <Server class="h-6 w-6 text-sky-600 dark:text-sky-400 mx-auto mb-1.5" />
                  <div class="font-bold text-slate-900 dark:text-white">CORE OLT-NOC</div>
                  <div class="text-[10px] text-slate-500">Rack #02 • Port {activeConn?.portNumber || 'PON-01'}</div>
                  <div class="mt-2 text-[10px] text-emerald-600 dark:text-emerald-400 font-bold">TX: +2.4 dBm</div>
                </div>

                <!-- Node 2: Splitter -->
                <div class="p-3 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs relative">
                  <div class="hidden md:block absolute -left-3 top-1/2 -translate-y-1/2 text-slate-400 font-bold">➜</div>
                  <Zap class="h-6 w-6 text-amber-500 mx-auto mb-1.5" />
                  <div class="font-bold text-slate-900 dark:text-white">SPLITTER 1:16</div>
                  <div class="text-[10px] text-slate-500">Hộp phối quang DP-12B</div>
                  <div class="mt-2 text-[10px] text-slate-600 dark:text-slate-400">Suy hao: 13.5 dB</div>
                </div>

                <!-- Node 3: Drop Cable -->
                <div class="p-3 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 shadow-xs relative">
                  <div class="hidden md:block absolute -left-3 top-1/2 -translate-y-1/2 text-slate-400 font-bold">➜</div>
                  <Wifi class="h-6 w-6 text-indigo-500 mx-auto mb-1.5" />
                  <div class="font-bold text-slate-900 dark:text-white">CÁP THUÊ BAO</div>
                  <div class="text-[10px] text-slate-500">G.657.A2 • 120m cáp</div>
                  <div class="mt-2 text-[10px] text-slate-600 dark:text-slate-400">Đầu nối: SC/APC</div>
                </div>

                <!-- Node 4: ONT/CPE -->
                <div class="p-3 bg-white dark:bg-slate-900 rounded-xl border-2 border-emerald-400 dark:border-emerald-600 shadow-xs relative">
                  <div class="hidden md:block absolute -left-3 top-1/2 -translate-y-1/2 text-slate-400 font-bold">➜</div>
                  <Cpu class="h-6 w-6 text-emerald-600 dark:text-emerald-400 mx-auto mb-1.5" />
                  <div class="font-bold text-slate-900 dark:text-white">ONT GATEWAY CPE</div>
                  <div class="text-[10px] text-slate-500 truncate">{activeConn?.assignedDeviceSerial || 'ZXHN-F670L'}</div>
                  <div class="mt-2 text-[10px] text-emerald-600 dark:text-emerald-400 font-bold">
                    RX: {testRecords[0]?.rxPowerDbm ?? -16.8} dBm
                  </div>
                </div>
              </div>
            </div>

            <!-- Customer & Hardware Details Card -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div class="p-4 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 space-y-2.5 text-xs">
                <h5 class="font-bold text-slate-900 dark:text-white flex items-center space-x-1.5">
                  <User class="h-4 w-4 text-sky-500" />
                  <span>{$language === 'vi' ? 'Thông tin thuê bao khách hàng' : 'Customer Account Information'}</span>
                </h5>
                <div class="divide-y divide-slate-100 dark:divide-slate-800 text-[11px]">
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">{$language === 'vi' ? 'Họ và tên:' : 'Full Name:'}</span>
                    <span class="font-bold text-slate-800 dark:text-slate-200">{activeConn?.customerName}</span>
                  </div>
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">{$language === 'vi' ? 'Mã tài khoản:' : 'Account ID:'}</span>
                    <span class="font-mono font-bold text-sky-600 dark:text-sky-400">{activeConn?.accountId}</span>
                  </div>
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">{$language === 'vi' ? 'Số điện thoại:' : 'Phone:'}</span>
                    <span class="font-mono text-slate-700 dark:text-slate-300">{activeConn?.customerPhone}</span>
                  </div>
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">{$language === 'vi' ? 'Địa chỉ lắp đặt:' : 'Address:'}</span>
                    <span class="text-slate-700 dark:text-slate-300 text-right max-w-[200px] truncate" title={activeConn?.installationAddress}>
                      {activeConn?.installationAddress}
                    </span>
                  </div>
                </div>
              </div>

              <div class="p-4 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 space-y-2.5 text-xs">
                <h5 class="font-bold text-slate-900 dark:text-white flex items-center space-x-1.5">
                  <Cpu class="h-4 w-4 text-indigo-500" />
                  <span>{$language === 'vi' ? 'Thông số kỹ thuật & Thiết bị Gateway' : 'Gateway Hardware Specs'}</span>
                </h5>
                <div class="divide-y divide-slate-100 dark:divide-slate-800 text-[11px] font-mono">
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">MODEL ROUTER:</span>
                    <span class="font-bold text-slate-800 dark:text-slate-200">ZTE ZXHN F670Y (Dual-Band)</span>
                  </div>
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">SERIAL NUMBER:</span>
                    <span class="font-bold text-indigo-600 dark:text-indigo-400">{activeConn?.assignedDeviceSerial || 'NX-HW-99824'}</span>
                  </div>
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">MAC ADDRESS:</span>
                    <span class="text-slate-700 dark:text-slate-300">FC:21:B4:88:AC:3E</span>
                  </div>
                  <div class="py-1.5 flex justify-between">
                    <span class="text-slate-400">FIRMWARE VERSION:</span>
                    <span class="text-slate-700 dark:text-slate-300">V9.1.20P4T8 (Stable)</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        {/if}
      </div>

      <!-- MODAL FOOTER -->
      <div class="px-5 py-3.5 bg-slate-50 dark:bg-slate-950/70 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between shrink-0">
        <div class="text-xs text-slate-500 dark:text-slate-400 flex items-center space-x-2">
          <ShieldCheck class="h-4 w-4 text-emerald-500" />
          <span>{$language === 'vi' ? 'Hệ thống giám sát viễn thông cấp NOC hoạt động 24/7' : 'NOC automated monitoring 24/7 active'}</span>
        </div>

        {#if !isPage}
          <div class="flex items-center space-x-2">
            <button
              type="button"
              onclick={onClose}
              class="px-4 py-2 rounded-xl text-xs font-bold bg-slate-200 dark:bg-slate-800 hover:bg-slate-300 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 transition cursor-pointer"
            >
              {$language === 'vi' ? 'Đóng cửa sổ' : 'Close Window'}
            </button>
          </div>
        {/if}
      </div>
{/snippet}

{#if isPage}
  <div
    class="w-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm flex flex-col overflow-hidden text-slate-800 dark:text-slate-100"
  >
    {@render modalBody()}
  </div>
{:else if isOpen}
  <!-- Backdrop -->
  <div
    class="fixed inset-0 bg-slate-950/70 backdrop-blur-xs z-50 flex items-center justify-center p-3 sm:p-5 overflow-y-auto animate-in fade-in duration-200"
    role="dialog"
    aria-modal="true"
  >
    <!-- Modal Card Container -->
    <div
      class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-2xl w-full max-w-6xl max-h-[92vh] flex flex-col overflow-hidden text-slate-800 dark:text-slate-100 transition-all transform scale-100"
    >
      {@render modalBody()}
    </div>
  </div>
{/if}
