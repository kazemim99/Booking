/**
 * A small client for AsanRezerve's admin observability API (`/admin/observability/*`, AdminOnly).
 * Unwraps the API's `{ success, data, ... }` envelope and turns failures into messages an assistant can act on.
 */
export class AdminApiError extends Error {
  constructor(message, status) {
    super(message)
    this.name = 'AdminApiError'
    this.status = status
  }
}

export function createAdminClient({ baseUrl, token, fetchImpl = globalThis.fetch }) {
  if (!baseUrl) throw new Error('ASANREZERVE_API_URL is not set (e.g. https://back.nahalkmi.ir/api/v1)')
  if (!token) throw new Error('ASANREZERVE_ADMIN_TOKEN is not set (an admin JWT from the admin panel login)')

  const root = baseUrl.replace(/\/+$/, '') + '/admin/observability'

  async function request(method, path, { query, body } = {}) {
    const url = new URL(root + path)
    for (const [key, value] of Object.entries(query ?? {})) {
      if (value !== undefined && value !== null && value !== '') url.searchParams.set(key, String(value))
    }

    const response = await fetchImpl(url, {
      method,
      headers: {
        Authorization: `Bearer ${token}`,
        Accept: 'application/json',
        ...(body ? { 'Content-Type': 'application/json' } : {}),
      },
      body: body ? JSON.stringify(body) : undefined,
    })

    if (response.status === 401) {
      throw new AdminApiError('The admin token was rejected or has expired: sign in to the admin panel and set ASANREZERVE_ADMIN_TOKEN again.', 401)
    }
    if (response.status === 403) {
      throw new AdminApiError('The token does not belong to an admin (AdminOnly).', 403)
    }
    if (response.status === 204) return null

    const text = await response.text()
    let json = null
    try {
      json = text ? JSON.parse(text) : null
    } catch {
      json = null
    }

    if (!response.ok) {
      const message = json?.error?.message ?? json?.message ?? text ?? response.statusText
      throw new AdminApiError(`${method} ${path} failed with ${response.status}: ${message}`, response.status)
    }

    if (json && typeof json === 'object' && 'success' in json && 'data' in json) return json.data
    return json
  }

  return {
    searchLogs: (query) => request('GET', '/logs', { query }),
    getLogEvent: (id) => request('GET', `/logs/${encodeURIComponent(id)}`),
    getTrace: (traceId) => request('GET', `/logs/trace/${encodeURIComponent(traceId)}`),
    getDigestMarkdown: (query) => request('GET', '/digest', { query: { ...query, format: 'markdown' } }).then((d) => d?.markdown ?? ''),
    getOverview: () => request('GET', '/overview'),
    listLogLevels: () => request('GET', '/log-levels'),
    setLogLevel: (body) => request('PUT', '/log-levels', { body }),
    resetLogLevel: (category) => request('DELETE', `/log-levels/${encodeURIComponent(category)}`),
    getCache: () => request('GET', '/cache'),
    invalidateCache: (body) => request('POST', '/cache/invalidate', { body }),
  }
}
