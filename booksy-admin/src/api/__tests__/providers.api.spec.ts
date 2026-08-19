import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('ant-design-vue', () => ({ message: { error: vi.fn(), success: vi.fn(), warning: vi.fn() } }))

const get = vi.fn()
const post = vi.fn()
vi.mock('../../utils/axios', () => ({ default: { get: (...a: unknown[]) => get(...a), post: (...a: unknown[]) => post(...a) } }))

import { providersApi } from '../providers.api'

const provider = (over: Record<string, unknown> = {}) => ({
  id: 'p1',
  businessName: 'آرایشگاه نهال',
  description: 'آرایشگاه',
  city: 'اسلامشهر',
  status: 'PendingVerification',
  ...over,
})

describe('providersApi.getProviders', () => {
  beforeEach(() => {
    get.mockReset()
    post.mockReset()
  })

  /**
   * `/Providers/search` accepts no status parameter. The admin used to pass `status` to it
   * anyway, where ASP.NET model binding silently discarded it — so every status tab issued
   * an unfiltered query and rendered the complete provider list.
   */
  it('filters by status through the endpoint that can actually filter', async () => {
    get.mockResolvedValue({ data: [provider()] })

    await providersApi.getProviders({ status: 'PendingVerification' })

    expect(get).toHaveBeenCalledWith('/Providers/by-status/PendingVerification', {
      params: { maxResults: 1000 },
    })
  })

  it('never sends a status to the search endpoint, which would discard it', async () => {
    get.mockResolvedValue({ data: { items: [], totalCount: 0, pageNumber: 1, pageSize: 10, totalPages: 0 } })

    await providersApi.getProviders({})

    const call = get.mock.calls[0]!
    expect(call[0]).toBe('/Providers/search')
    expect((call[1] as { params: Record<string, unknown> }).params).not.toHaveProperty('status')
  })

  it('sends the search box value as searchTerm, the name the backend binds', async () => {
    get.mockResolvedValue({ data: { items: [], totalCount: 0, pageNumber: 1, pageSize: 10, totalPages: 0 } })

    await providersApi.getProviders({ search: 'نهال' })

    // Sent as `search` previously — bound to nothing, so the search box did nothing.
    const call = get.mock.calls[0]!
    expect((call[1] as { params: Record<string, unknown> }).params).toMatchObject({ searchTerm: 'نهال' })
  })

  it('paginates the status results, which arrive as one uncapped array', async () => {
    get.mockResolvedValue({ data: Array.from({ length: 25 }, (_, i) => provider({ id: `p${i}` })) })

    const page = await providersApi.getProviders({ status: 'Active', pageNumber: 2, pageSize: 10 })

    expect(page.items).toHaveLength(10)
    expect(page.items[0]!.id).toBe('p10')
    expect(page.totalCount).toBe(25)
    expect(page.totalPages).toBe(3)
  })

  it('reports a total the tab can trust when a status has no providers', async () => {
    get.mockResolvedValue({ data: [] })

    const page = await providersApi.getProviders({ status: 'Suspended' })

    expect(page.items).toEqual([])
    expect(page.totalCount).toBe(0)
  })

  it('searches within a status tab client-side, since that endpoint takes no term', async () => {
    get.mockResolvedValue({
      data: [provider({ id: 'p1', businessName: 'آرایشگاه نهال' }), provider({ id: 'p2', businessName: 'سالن گلستان' })],
    })

    const page = await providersApi.getProviders({ status: 'Active', search: 'نهال' })

    expect(page.items).toHaveLength(1)
    expect(page.items[0]!.id).toBe('p1')
    expect(page.totalCount).toBe(1)
  })

  it('drops a whitespace-only search rather than filtering everything away', async () => {
    get.mockResolvedValue({ data: [provider(), provider({ id: 'p2' })] })

    const page = await providersApi.getProviders({ status: 'Active', search: '   ' })

    expect(page.items).toHaveLength(2)
  })
})

describe('providersApi.getPendingVerificationCount', () => {
  beforeEach(() => get.mockReset())

  it('counts the real pending-verification queue', async () => {
    get.mockResolvedValue({ data: [provider(), provider({ id: 'p2' })] })

    // Previously derived from a totalCount on an unfiltered query, so the badge showed
    // the entire provider population instead of the queue.
    await expect(providersApi.getPendingVerificationCount()).resolves.toBe(2)
  })

  it('reports zero for an empty queue', async () => {
    get.mockResolvedValue({ data: [] })

    await expect(providersApi.getPendingVerificationCount()).resolves.toBe(0)
  })
})

describe('providersApi.activateProvider', () => {
  beforeEach(() => post.mockReset())

  it('posts to the one lifecycle endpoint the backend actually exposes', async () => {
    post.mockResolvedValue({ data: { message: 'ok' } })

    await providersApi.activateProvider('p1')

    expect(post).toHaveBeenCalledWith('/Providers/p1/activate')
  })
})

describe('provider lifecycle surface', () => {
  it('exposes no approve, reject, suspend or reactivate call', () => {
    // These were called by the admin UI but exist neither as HTTP endpoints nor as domain
    // operations. Re-adding a client method without backend support would restore buttons
    // that silently fail, so their absence is pinned here.
    expect(providersApi).not.toHaveProperty('approveProvider')
    expect(providersApi).not.toHaveProperty('rejectProvider')
    expect(providersApi).not.toHaveProperty('suspendProvider')
    expect(providersApi).not.toHaveProperty('reactivateProvider')
  })
})
