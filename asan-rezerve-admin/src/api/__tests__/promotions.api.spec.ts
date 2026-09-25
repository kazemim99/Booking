import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * The admin's discounts client (openspec/changes/add-discounts-and-campaigns). Routes are relative to the client's
 * `/api/v1` base; the client unwraps the `{ success, data }` envelope itself.
 */

const get = vi.fn()
const post = vi.fn()
const put = vi.fn()
vi.mock('../../utils/axios', () => ({
  default: {
    get: (...a: unknown[]) => get(...a),
    post: (...a: unknown[]) => post(...a),
    put: (...a: unknown[]) => put(...a),
  },
}))

import { promotionsApi, type PromotionTermsInput } from '../promotions.api'

const terms: PromotionTermsInput = {
  title: 'نوروز',
  activation: 'Automatic',
  discountKind: 'Percentage',
  discountValue: 15,
  newCustomersOnly: false,
}

describe('promotionsApi (admin)', () => {
  beforeEach(() => {
    get.mockReset()
    post.mockReset()
    put.mockReset()
  })

  it('searches with only the filters that narrow something', async () => {
    get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } })

    await promotionsApi.search({ owner: 'all', status: 'all', search: '  ', page: 2 })

    expect(get).toHaveBeenCalledWith('/admin/promotions', { params: { page: 2, pageSize: 20 } })
  })

  it('passes owner, status and a trimmed search', async () => {
    get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } })

    await promotionsApi.search({ owner: 'Provider', status: 'Paused', search: ' NOWRUZ ' })

    expect(get).toHaveBeenCalledWith('/admin/promotions', {
      params: { page: 1, pageSize: 20, owner: 'Provider', status: 'Paused', search: 'NOWRUZ' },
    })
  })

  it('creates and edits campaigns on their own routes', async () => {
    post.mockResolvedValue({ data: { id: 'p1' } })
    put.mockResolvedValue({ data: { id: 'p1' } })

    await promotionsApi.createCampaign(terms)
    await promotionsApi.updateCampaign('p1', terms)

    expect(post).toHaveBeenCalledWith('/admin/promotions', terms)
    expect(put).toHaveBeenCalledWith('/admin/promotions/p1', terms)
  })

  it.each(['pause', 'resume', 'end'] as const)('%s posts to its lifecycle endpoint', async (action) => {
    post.mockResolvedValue({ data: { id: 'p1', status: 'Paused' } })

    await promotionsApi.changeStatus('p1', action)

    expect(post).toHaveBeenCalledWith(`/admin/promotions/p1/${action}`, {})
  })

  it('reads a promotion with its participants', async () => {
    get.mockResolvedValue({ data: { promotion: { id: 'p1' }, participants: [{ providerId: 's1' }] } })

    const details = await promotionsApi.details('p1')

    expect(get).toHaveBeenCalledWith('/admin/promotions/p1')
    expect(details.participants).toHaveLength(1)
  })
})
