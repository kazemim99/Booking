import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * The admin observability API (openspec/changes/add-observability-and-caching): logs, log levels, overview,
 * digest, cache. The server is AdminOnly; these tests pin the wire contract the pages rely on.
 */

const get = vi.fn()
const put = vi.fn()
const post = vi.fn()
const del = vi.fn()
vi.mock('../../utils/axios', () => ({
  default: {
    get: (...a: unknown[]) => get(...a),
    put: (...a: unknown[]) => put(...a),
    post: (...a: unknown[]) => post(...a),
    delete: (...a: unknown[]) => del(...a),
  },
}))

import { observabilityApi } from '../observability.api'

describe('observabilityApi', () => {
  beforeEach(() => {
    get.mockReset()
    put.mockReset()
    post.mockReset()
    del.mockReset()
  })

  it('searches with only the filters that narrow something', async () => {
    get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 50 } })

    await observabilityApi.search({ minLevel: 'all', search: '  ', source: ' AsanRezerve ', page: 2, pageSize: 50, statusCode: null })

    expect(get).toHaveBeenCalledWith('/admin/observability/logs', {
      params: { source: 'AsanRezerve', page: 2, pageSize: 50 },
    })
  })

  it('sends the level, window and request filters', async () => {
    get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 50 } })

    await observabilityApi.search({
      from: '2026-09-25T10:00:00.000Z',
      to: '2026-09-25T11:00:00.000Z',
      minLevel: 'Warning',
      traceId: 'abc',
      requestPath: '/api/v1/bookings',
      statusCode: 500,
    })

    expect(get).toHaveBeenCalledWith('/admin/observability/logs', {
      params: {
        from: '2026-09-25T10:00:00.000Z',
        to: '2026-09-25T11:00:00.000Z',
        minLevel: 'Warning',
        traceId: 'abc',
        requestPath: '/api/v1/bookings',
        statusCode: 500,
      },
    })
  })

  it('opens one request by its trace id', async () => {
    get.mockResolvedValue({ data: [] })

    await observabilityApi.trace(' 4bf92f3577b34da6a3ce929d0e0e4736 ')

    expect(get).toHaveBeenCalledWith('/admin/observability/logs/trace/4bf92f3577b34da6a3ce929d0e0e4736')
  })

  it('exports as a blob without paging', async () => {
    get.mockResolvedValue({ data: new Blob(['{}\n']) })

    await observabilityApi.exportLogs({ minLevel: 'Error', page: 3, pageSize: 50 })

    expect(get).toHaveBeenCalledWith('/admin/observability/logs/export', {
      params: { minLevel: 'Error' },
      responseType: 'blob',
    })
  })

  it('reads the digest as markdown', async () => {
    get.mockResolvedValue({ data: { markdown: '# AsanRezerve system digest' } })

    const markdown = await observabilityApi.digestMarkdown({ source: 'AsanRezerve.ServiceCatalog' })

    expect(markdown).toBe('# AsanRezerve system digest')
    expect(get).toHaveBeenCalledWith('/admin/observability/digest', {
      params: { source: 'AsanRezerve.ServiceCatalog', format: 'markdown' },
    })
  })

  it('sets a temporary level and a permanent one', async () => {
    put.mockResolvedValue({ data: { category: 'AsanRezerve', effectiveLevel: 'Debug' } })

    await observabilityApi.setLogLevel('AsanRezerve', 'Debug', 30)
    await observabilityApi.setLogLevel('Default', 'Warning')

    expect(put).toHaveBeenNthCalledWith(1, '/admin/observability/log-levels', {
      category: 'AsanRezerve',
      level: 'Debug',
      durationMinutes: 30,
    })
    expect(put).toHaveBeenNthCalledWith(2, '/admin/observability/log-levels', {
      category: 'Default',
      level: 'Warning',
      durationMinutes: null,
    })
  })

  it('resets a level by category', async () => {
    del.mockResolvedValue({})

    await observabilityApi.resetLogLevel('AsanRezerve.ServiceCatalog')

    expect(del).toHaveBeenCalledWith('/admin/observability/log-levels/AsanRezerve.ServiceCatalog')
  })

  it('purges one tag or everything', async () => {
    post.mockResolvedValue({})

    await observabilityApi.invalidateCache({ tag: 'provider:1' })
    await observabilityApi.invalidateCache({ all: true })

    expect(post).toHaveBeenNthCalledWith(1, '/admin/observability/cache/invalidate', { tag: 'provider:1' })
    expect(post).toHaveBeenNthCalledWith(2, '/admin/observability/cache/invalidate', { all: true })
  })
})
