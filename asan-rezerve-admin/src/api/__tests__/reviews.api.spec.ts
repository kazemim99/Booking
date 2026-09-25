import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * The admin's review-moderation client (provider-reviews-and-ratings). Routes are relative to the client's
 * `/api/v1` base; the client unwraps the `{ success, data }` envelope itself.
 */

const get = vi.fn()
const post = vi.fn()
vi.mock('../../utils/axios', () => ({ default: { get: (...a: unknown[]) => get(...a), post: (...a: unknown[]) => post(...a) } }))

import { reviewsApi } from '../reviews.api'

describe('reviewsApi (admin)', () => {
  beforeEach(() => {
    get.mockReset()
    post.mockReset()
  })

  it('reads one queue at a time', async () => {
    get.mockResolvedValue({ data: { items: [{ reviewId: 'r1', reportCount: 2, reports: [] }], totalCount: 1 } })

    const page = await reviewsApi.queue('reported', 2, 25)

    expect(get).toHaveBeenCalledWith('/admin/reviews/queue', { params: { filter: 'reported', pageNumber: 2, pageSize: 25 } })
    expect(page.items[0]?.reportCount).toBe(2)
    expect(page.totalCount).toBe(1)
  })

  it.each([
    ['approve', 'approve'],
    ['restore', 'restore'],
    ['approveReply', 'reply/approve'],
  ] as const)('%s posts to its endpoint', async (action, path) => {
    post.mockResolvedValue({ data: { reviewId: 'r1', moderationStatus: 'Published' } })

    await reviewsApi[action]('r1')

    expect(post).toHaveBeenCalledWith(`/admin/reviews/r1/${path}`, {})
  })

  it.each([
    ['reject', 'reject'],
    ['hide', 'hide'],
    ['rejectReply', 'reply/reject'],
  ] as const)('%s sends the reason', async (action, path) => {
    post.mockResolvedValue({ data: {} })

    await reviewsApi[action]('r1', '  contains a phone number  ')

    expect(post).toHaveBeenCalledWith(`/admin/reviews/r1/${path}`, { reason: 'contains a phone number' })
  })

  it.each(['reject', 'hide', 'rejectReply'] as const)('%s refuses to send without a reason', async (action) => {
    await expect(reviewsApi[action]('r1', '   ')).rejects.toThrow()
    expect(post).not.toHaveBeenCalled()
  })
})
