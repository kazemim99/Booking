import { onScopeDispose, ref } from 'vue'
import {
  observabilityApi,
  type LogEventDetail,
  type LogEventListItem,
  type LogLevelName,
} from '../../api/observability.api'

/** How far back the explorer looks. The server keeps 14 days. */
export type TimeRange = '15m' | '1h' | '6h' | '24h' | '7d' | '14d'

const RANGE_MS: Record<TimeRange, number> = {
  '15m': 15 * 60_000,
  '1h': 60 * 60_000,
  '6h': 6 * 60 * 60_000,
  '24h': 24 * 60 * 60_000,
  '7d': 7 * 24 * 60 * 60_000,
  '14d': 14 * 24 * 60 * 60_000,
}

export const TIME_RANGES = Object.keys(RANGE_MS) as TimeRange[]

/**
 * The Events tab: search stored log events, open one, follow a whole request by its trace id, export, and
 * auto-refresh while watching. A failed load is an error with a retry, never an empty table (which would read as
 * "nothing happened").
 */
export function useLogExplorer(pageSize = 50, now: () => number = Date.now) {
  const range = ref<TimeRange>('1h')
  const minLevel = ref<LogLevelName | 'all'>('all')
  const search = ref('')
  const source = ref('')
  const traceId = ref('')
  const statusCode = ref<number | null>(null)
  const page = ref(1)

  const items = ref<LogEventListItem[]>([])
  const totalCount = ref(0)
  const loading = ref(false)
  const loaded = ref(false)
  const error = ref('')

  const detail = ref<LogEventDetail | null>(null)
  const detailLoading = ref(false)
  const traceEvents = ref<LogEventDetail[]>([])
  const traceOpen = ref('')
  const traceLoading = ref(false)

  const autoRefresh = ref(false)
  let timer: ReturnType<typeof setInterval> | null = null

  function query() {
    return {
      from: new Date(now() - RANGE_MS[range.value]).toISOString(),
      minLevel: minLevel.value,
      search: search.value,
      source: source.value,
      traceId: traceId.value,
      statusCode: statusCode.value,
      page: page.value,
      pageSize,
    }
  }

  async function load() {
    loading.value = true
    error.value = ''
    try {
      const result = await observabilityApi.search(query())
      items.value = result.items
      totalCount.value = result.totalCount
      loaded.value = true
    } catch (e) {
      loaded.value = false
      error.value = messageOf(e) || 'بارگذاری گزارش‌ها ناموفق بود'
    } finally {
      loading.value = false
    }
  }

  async function applyFilters() {
    page.value = 1
    await load()
  }

  async function goTo(next: number) {
    page.value = next
    await load()
  }

  async function open(id: number) {
    detailLoading.value = true
    try {
      detail.value = await observabilityApi.get(id)
    } catch (e) {
      error.value = messageOf(e) || 'بارگذاری رویداد ناموفق بود'
    } finally {
      detailLoading.value = false
    }
  }

  function closeDetail() {
    detail.value = null
  }

  /** Every event of one request, in order. */
  async function openTrace(id: string) {
    traceOpen.value = id
    traceLoading.value = true
    try {
      traceEvents.value = await observabilityApi.trace(id)
    } catch (e) {
      traceEvents.value = []
      error.value = messageOf(e) || 'بارگذاری درخواست ناموفق بود'
    } finally {
      traceLoading.value = false
    }
  }

  function closeTrace() {
    traceOpen.value = ''
    traceEvents.value = []
  }

  /** Re-reads the first page every `intervalMs` while switched on. */
  function setAutoRefresh(on: boolean, intervalMs = 10_000) {
    autoRefresh.value = on
    if (timer) {
      clearInterval(timer)
      timer = null
    }
    if (on) {
      timer = setInterval(() => {
        if (!loading.value && page.value === 1) void load()
      }, intervalMs)
    }
  }

  async function exportLogs(): Promise<Blob | null> {
    try {
      return await observabilityApi.exportLogs({ ...query(), page: undefined, pageSize: undefined })
    } catch (e) {
      error.value = messageOf(e) || 'دریافت خروجی ناموفق بود'
      return null
    }
  }

  onScopeDispose(() => setAutoRefresh(false))

  return {
    range, minLevel, search, source, traceId, statusCode, page,
    items, totalCount, loading, loaded, error,
    detail, detailLoading, traceEvents, traceOpen, traceLoading, autoRefresh,
    load, applyFilters, goTo, open, closeDetail, openTrace, closeTrace, setAutoRefresh, exportLogs,
  }
}

export function messageOf(e: unknown): string {
  const response = (e as { response?: { data?: { message?: string } } })?.response
  return response?.data?.message ?? ''
}
