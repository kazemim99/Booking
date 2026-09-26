import apiClient from '../utils/axios'

/**
 * System logs, log levels, the system overview, the AI digest and the cache
 * (openspec/changes/add-observability-and-caching). Every route is AdminOnly on the server. Log messages arrive
 * already masked (phones, e-mails, codes, tokens).
 */

/** Stored event levels (Serilog). */
export type LogLevelName = 'Verbose' | 'Debug' | 'Information' | 'Warning' | 'Error' | 'Fatal'

/** Category levels (Microsoft.Extensions.Logging). */
export type CategoryLevel = 'Trace' | 'Debug' | 'Information' | 'Warning' | 'Error' | 'Critical' | 'None'

export const LOG_LEVELS: LogLevelName[] = ['Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal']
export const CATEGORY_LEVELS: CategoryLevel[] = ['Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical', 'None']

export interface LogEventListItem {
  id: number
  timestamp: string
  level: LogLevelName
  message: string
  sourceContext?: string | null
  traceId?: string | null
  requestPath?: string | null
  statusCode?: number | null
  elapsedMs?: number | null
  hasException: boolean
}

export interface LogEventDetail {
  id: number
  timestamp: string
  level: LogLevelName
  message: string
  messageTemplate?: string | null
  exception?: string | null
  sourceContext?: string | null
  traceId?: string | null
  spanId?: string | null
  requestPath?: string | null
  routeTemplate?: string | null
  statusCode?: number | null
  elapsedMs?: number | null
  userId?: string | null
  properties?: Record<string, unknown> | null
}

export interface LogPage {
  items: LogEventListItem[]
  totalCount: number
  page: number
  pageSize: number
}

export interface LogQuery {
  /** UTC ISO instants. Omitted: the last 24 hours. */
  from?: string | null
  to?: string | null
  minLevel?: LogLevelName | 'all'
  search?: string
  source?: string
  traceId?: string
  requestPath?: string
  statusCode?: number | null
  page?: number
  pageSize?: number
}

export interface LogLevelOverride {
  level: CategoryLevel
  expiresAt?: string | null
  updatedBy: string
  updatedAt: string
}

export interface LogLevelRow {
  category: string
  configuredLevel?: CategoryLevel | null
  effectiveLevel: CategoryLevel
  override?: LogLevelOverride | null
}

export interface CacheRegion {
  region: string
  requests: number
  hits: number
  misses: number
  hitRatio: number
}

export interface CacheOverview {
  regions: CacheRegion[]
  l1?: { entries: number; hits: number; misses: number; estimatedSize?: number | null } | null
  l2: {
    store: string
    circuit: 'Closed' | 'Open' | 'HalfOpen' | 'n/a'
    consecutiveFailures: number
    openedAt?: string | null
    lastError?: string | null
    redisEndpoints?: string | null
    redisConnected?: boolean | null
  }
}

export interface LevelCount {
  level: LogLevelName
  count: number
}

export interface ErrorGroup {
  level: LogLevelName
  sourceContext?: string | null
  messageTemplate?: string | null
  exceptionType?: string | null
  count: number
  firstSeen: string
  lastSeen: string
  sampleTraceIds: string[]
}

export interface RouteLatency {
  route: string
  requests: number
  serverErrors: number
  p50Ms: number
  p95Ms: number
  maxMs: number
}

export interface TimelineBucket {
  hour: string
  information: number
  warnings: number
  errors: number
}

export interface LogStoreStats {
  ready: boolean
  enqueued: number
  written: number
  dropped: number
  failedBatches: number
  queueLength: number
  lastError?: string | null
  lastWriteAt?: string | null
}

export interface SystemOverview {
  generatedAt: string
  environment: string
  version: string
  startedAt: string
  process: {
    workingSetMb: number
    gcHeapMb: number
    gen0Collections: number
    gen1Collections: number
    gen2Collections: number
    threadPoolThreads: number
    pendingWorkItems: number
    cpuSeconds: number
  }
  lastHour: { requests: number; serverErrors: number; errorRate: number; p95Ms: number }
  levelsLastHour: LevelCount[]
  last24Hours: TimelineBucket[]
  slowestRoutesLastHour: RouteLatency[]
  topErrorsLastHour: ErrorGroup[]
  cache: CacheOverview
  logStore?: LogStoreStats | null
  counters: { meter: string; name: string; description?: string | null; total: number }[]
}

export interface DigestQuery {
  from?: string | null
  to?: string | null
  source?: string
}

const BASE = '/admin/observability'

/** Only the filters that narrow something go on the wire. */
function logParams(query: LogQuery): Record<string, unknown> {
  const params: Record<string, unknown> = {}
  if (query.from) params.from = query.from
  if (query.to) params.to = query.to
  if (query.minLevel && query.minLevel !== 'all') params.minLevel = query.minLevel
  for (const key of ['search', 'source', 'traceId', 'requestPath'] as const) {
    const value = query[key]?.trim()
    if (value) params[key] = value
  }
  if (query.statusCode != null) params.statusCode = query.statusCode
  if (query.page) params.page = query.page
  if (query.pageSize) params.pageSize = query.pageSize
  return params
}

function digestParams(query: DigestQuery): Record<string, unknown> {
  const params: Record<string, unknown> = {}
  if (query.from) params.from = query.from
  if (query.to) params.to = query.to
  if (query.source?.trim()) params.source = query.source.trim()
  return params
}

export const observabilityApi = {
  async search(query: LogQuery): Promise<LogPage> {
    const response = await apiClient.get<LogPage>(`${BASE}/logs`, { params: logParams(query) })
    return response.data
  },

  async get(id: number): Promise<LogEventDetail> {
    const response = await apiClient.get<LogEventDetail>(`${BASE}/logs/${id}`)
    return response.data
  },

  /** Every stored event of one request, oldest first. */
  async trace(traceId: string): Promise<LogEventDetail[]> {
    const response = await apiClient.get<LogEventDetail[]>(`${BASE}/logs/trace/${encodeURIComponent(traceId.trim())}`)
    return response.data
  },

  /** Newline-delimited JSON of the matching events (at most 50 000), for download or an AI tool. */
  async exportLogs(query: LogQuery): Promise<Blob> {
    const params = logParams({ ...query, page: undefined, pageSize: undefined })
    const response = await apiClient.get<Blob>(`${BASE}/logs/export`, { params, responseType: 'blob' })
    return response.data
  },

  /** The window summarised as Markdown, ready to paste into an AI assistant. */
  async digestMarkdown(query: DigestQuery = {}): Promise<string> {
    const response = await apiClient.get<{ markdown: string }>(`${BASE}/digest`, {
      params: { ...digestParams(query), format: 'markdown' },
    })
    return response.data.markdown
  },

  async overview(): Promise<SystemOverview> {
    const response = await apiClient.get<SystemOverview>(`${BASE}/overview`)
    return response.data
  },

  async logLevels(): Promise<LogLevelRow[]> {
    const response = await apiClient.get<LogLevelRow[]>(`${BASE}/log-levels`)
    return response.data
  },

  /** Overrides a category's level; with `durationMinutes` it reverts by itself. */
  async setLogLevel(category: string, level: CategoryLevel, durationMinutes?: number | null): Promise<LogLevelRow> {
    const response = await apiClient.put<LogLevelRow>(`${BASE}/log-levels`, {
      category,
      level,
      durationMinutes: durationMinutes ?? null,
    })
    return response.data
  },

  async resetLogLevel(category: string): Promise<void> {
    await apiClient.delete(`${BASE}/log-levels/${encodeURIComponent(category)}`)
  },

  async cache(): Promise<CacheOverview> {
    const response = await apiClient.get<CacheOverview>(`${BASE}/cache`)
    return response.data
  },

  /** Evicts one tag (`provider:{id}`, `provider-directory`, `categories`, `locations`) or, with `all`, everything. */
  async invalidateCache(target: { tag: string } | { all: true }): Promise<void> {
    await apiClient.post(`${BASE}/cache/invalidate`, target)
  },
}
