import { z } from 'zod'

const LOG_LEVELS = ['Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal']
const CATEGORY_LEVELS = ['Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical', 'None']

const json = (value) => ({ content: [{ type: 'text', text: JSON.stringify(value, null, 2) }] })
const text = (value) => ({ content: [{ type: 'text', text: value }] })

/**
 * The tools this server offers. Read tools always; tools that change the running system (log levels, cache) only
 * when `allowWrites` is on (ASANREZERVE_MCP_ALLOW_WRITES=true). Log messages arrive masked by the server.
 */
export function defineTools(client, { allowWrites = false } = {}) {
  const tools = [
    {
      name: 'get_digest',
      description:
        'Start here. A Markdown summary of a time window: event counts by level, warnings/errors grouped by source, ' +
        'message template and exception type (with counts, first/last seen and sample trace ids), the slowest routes ' +
        '(p50/p95), cache hit ratios and log-store health. Default window: the last hour. Use `source` to narrow to a ' +
        'namespace such as AsanRezerve.ServiceCatalog.',
      inputSchema: {
        from: z.string().optional().describe('UTC ISO start, e.g. 2026-09-26T08:00:00Z'),
        to: z.string().optional().describe('UTC ISO end; default now'),
        source: z.string().optional().describe('Source-context prefix, e.g. AsanRezerve.ServiceCatalog'),
      },
      readOnly: true,
      handler: async (args) => text(await client.getDigestMarkdown(args)),
    },
    {
      name: 'search_logs',
      description:
        'Search stored log events (newest first, 14 days kept). Filter by window, minimum level, text in the message or ' +
        'exception, source namespace, trace id, request path prefix or HTTP status. Returns { items, totalCount, page, pageSize }.',
      inputSchema: {
        from: z.string().optional().describe('UTC ISO start; default 24 hours ago'),
        to: z.string().optional(),
        minLevel: z.enum(LOG_LEVELS).optional(),
        search: z.string().optional().describe('Case-insensitive text in the message or exception'),
        source: z.string().optional().describe('Source-context prefix'),
        traceId: z.string().optional(),
        requestPath: z.string().optional().describe('Request path prefix, e.g. /api/v1/bookings'),
        statusCode: z.number().int().optional(),
        page: z.number().int().min(1).optional(),
        pageSize: z.number().int().min(1).max(200).optional(),
      },
      readOnly: true,
      handler: async (args) => json(await client.searchLogs(args)),
    },
    {
      name: 'get_log_event',
      description: 'One stored event with its message template, exception, request, user id and all properties.',
      inputSchema: { id: z.number().int().describe('The event id from search_logs') },
      readOnly: true,
      handler: async ({ id }) => json(await client.getLogEvent(id)),
    },
    {
      name: 'get_trace',
      description:
        'Every stored event of one HTTP request, in order — the X-Trace-Id a client received, the traceId in an error ' +
        'response, or a sample trace id from the digest.',
      inputSchema: { traceId: z.string().min(1).describe('32 hex characters') },
      readOnly: true,
      handler: async ({ traceId }) => json(await client.getTrace(traceId)),
    },
    {
      name: 'get_overview',
      description:
        'Current system health: process (memory, GC, thread pool), last hour requests/5xx/error rate/p95, 24-hour ' +
        'event timeline, slowest routes, top errors, cache (per-query hit ratios, L1, Redis circuit), log store, counters.',
      inputSchema: {},
      readOnly: true,
      handler: async () => json(await client.getOverview()),
    },
    {
      name: 'list_log_levels',
      description: 'Log level per category: configured, effective now, and any admin override with its expiry.',
      inputSchema: {},
      readOnly: true,
      handler: async () => json(await client.listLogLevels()),
    },
    {
      name: 'get_cache_stats',
      description: 'Cache hits/misses per query type, the in-process tier and the Redis tier (circuit state, last error).',
      inputSchema: {},
      readOnly: true,
      handler: async () => json(await client.getCache()),
    },
  ]

  if (allowWrites) {
    tools.push(
      {
        name: 'set_log_level',
        description:
          'Change a category\'s log level on the running server (audited under the token\'s admin). Prefer a short ' +
          'durationMinutes: Debug/Trace multiply log volume. Category is a namespace or type name, or "Default".',
        inputSchema: {
          category: z.string().regex(/^[A-Za-z_][A-Za-z0-9_.]*$/),
          level: z.enum(CATEGORY_LEVELS),
          durationMinutes: z.number().int().min(1).max(10080).optional().describe('Omit for "until reset"'),
        },
        readOnly: false,
        handler: async (args) => json(await client.setLogLevel({ durationMinutes: null, ...args })),
      },
      {
        name: 'reset_log_level',
        description: 'Remove an override; the configured level applies again.',
        inputSchema: { category: z.string().regex(/^[A-Za-z_][A-Za-z0-9_.]*$/) },
        readOnly: false,
        handler: async ({ category }) => {
          await client.resetLogLevel(category)
          return text(`Log level for ${category} reset to its configured value.`)
        },
      },
      {
        name: 'invalidate_cache',
        description: 'Evict one cache tag (provider:{id}, provider-directory, categories, locations) or everything.',
        inputSchema: {
          tag: z.string().optional(),
          all: z.boolean().optional(),
        },
        readOnly: false,
        handler: async ({ tag, all }) => {
          if (!all && !tag) throw new Error('Give a tag, or all: true')
          await client.invalidateCache(all ? { all: true } : { tag })
          return text(all ? 'Whole cache purged.' : `Cache tag ${tag} purged.`)
        },
      },
    )
  }

  return tools
}
