import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * What the moderation page does: load a queue, act on one item, and take it off the list only once the server
 * accepted the decision.
 */

const queue = vi.fn()
const approve = vi.fn()
const reject = vi.fn()
const hide = vi.fn()
const restore = vi.fn()
const approveReply = vi.fn()
const rejectReply = vi.fn()

vi.mock('../../../api/reviews.api', () => ({
  reviewsApi: {
    queue: (...a: unknown[]) => queue(...a),
    approve: (...a: unknown[]) => approve(...a),
    reject: (...a: unknown[]) => reject(...a),
    hide: (...a: unknown[]) => hide(...a),
    restore: (...a: unknown[]) => restore(...a),
    approveReply: (...a: unknown[]) => approveReply(...a),
    rejectReply: (...a: unknown[]) => rejectReply(...a),
  },
}))

import { useReviewModeration, reasonAudience } from '../useReviewModeration'
import type { ModerationItem } from '../../../api/reviews.api'

const item = (id: string, over: Partial<ModerationItem> = {}): ModerationItem => ({
  reviewId: id,
  providerId: 'p1',
  customerId: 'c1',
  rating: 3,
  comment: `نظر ${id}`,
  moderationStatus: 'Pending' as const,
  reviewPending: true,
  replyPending: false,
  wasPublishedBefore: false,
  reportCount: 0,
  reports: [],
  createdAt: '2026-09-20T10:00:00Z',
  ...over,
})

describe('useReviewModeration', () => {
  beforeEach(() => {
    for (const f of [queue, approve, reject, hide, restore, approveReply, rejectReply]) f.mockReset()
  })

  it('loads the pending queue in the order the server gave it — oldest first', async () => {
    queue.mockResolvedValue({ items: [item('old'), item('new')], totalCount: 2 })
    const m = useReviewModeration()

    await m.load()

    expect(queue).toHaveBeenCalledWith('pending', 1, 20)
    expect(m.items.value.map((i) => i.reviewId)).toEqual(['old', 'new'])
  })

  it('switching tabs loads that queue', async () => {
    queue.mockResolvedValue({ items: [], totalCount: 0 })
    const m = useReviewModeration()

    await m.setFilter('reported')

    expect(queue).toHaveBeenLastCalledWith('reported', 1, 20)
  })

  it('approving takes the item off the list once the server accepted it', async () => {
    queue.mockResolvedValue({ items: [item('a'), item('b')], totalCount: 2 })
    approve.mockResolvedValue(undefined)
    const m = useReviewModeration()
    await m.load()

    await m.act('approve', item('a'))

    expect(approve).toHaveBeenCalledWith('a')
    expect(m.items.value.map((i) => i.reviewId)).toEqual(['b'])
  })

  it('a refused decision leaves the item where it was and says why', async () => {
    queue.mockResolvedValue({ items: [item('a')], totalCount: 1 })
    approve.mockRejectedValue({ response: { data: { message: 'already published' } } })
    const m = useReviewModeration()
    await m.load()

    await m.act('approve', item('a'))

    expect(m.items.value).toHaveLength(1)
    expect(m.error.value).toContain('already published')
  })

  it('reject and hide carry the reason the moderator typed', async () => {
    queue.mockResolvedValue({ items: [item('a'), item('b')], totalCount: 2 })
    reject.mockResolvedValue(undefined)
    hide.mockResolvedValue(undefined)
    const m = useReviewModeration()
    await m.load()

    await m.act('reject', item('a'), 'contains a phone number')
    await m.act('hide', item('b'), 'reported and upheld')

    expect(reject).toHaveBeenCalledWith('a', 'contains a phone number')
    expect(hide).toHaveBeenCalledWith('b', 'reported and upheld')
  })

  it('a reply decision acts on the reply', async () => {
    queue.mockResolvedValue({ items: [item('a', { reviewPending: false, replyPending: true })], totalCount: 1 })
    approveReply.mockResolvedValue(undefined)
    const m = useReviewModeration()
    await m.load()

    await m.act('approveReply', item('a'))

    expect(approveReply).toHaveBeenCalledWith('a')
    expect(m.items.value).toHaveLength(0)
  })

  it('a failed load is an error, not an empty queue', async () => {
    queue.mockRejectedValue(new Error('network'))
    const m = useReviewModeration()

    await m.load()

    expect(m.error.value).not.toBe('')
    expect(m.loaded.value).toBe(false)
  })

  // The reason a moderator types is not private: rejecting a review sends it to the customer who wrote it
  // (task 7.7, decided 2026-09-22), and rejecting a reply shows it to the salon. The form has to say so.
  it('says who will read the reason, per decision', () => {
    expect(reasonAudience('reject')).toBe('customer')
    expect(reasonAudience('rejectReply')).toBe('provider')
    // Hiding notifies nobody; the reason stays internal, so promising a reader would be a lie.
    expect(reasonAudience('hide')).toBeNull()
    expect(reasonAudience('approve')).toBeNull()
  })
})
