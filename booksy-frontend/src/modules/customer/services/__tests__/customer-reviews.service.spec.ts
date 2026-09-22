import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * "My reviews" and editing a review, as the customer profile reaches them.
 *
 * Both used to call `/api/v1/customers/{id}/reviews` on the UserManagement client — a route CustomersController
 * never defined, so the modal could only ever fail. They now go through the ServiceCatalog reviews client.
 */

const mine = vi.fn()
const edit = vi.fn()
vi.mock('@/modules/reviews/api/reviews.api', () => ({
  reviewsApi: { mine: (...a: unknown[]) => mine(...a), edit: (...a: unknown[]) => edit(...a) },
}))

const umGet = vi.fn()
const umPatch = vi.fn()
vi.mock('@/core/api/client/http-client', () => ({
  userManagementClient: { get: (...a: unknown[]) => umGet(...a), patch: (...a: unknown[]) => umPatch(...a) },
  serviceCategoryClient: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}))

import { customerService } from '../customer.service'

const myReview = (overrides: Record<string, unknown> = {}) => ({
  reviewId: 'r1',
  providerId: 'p1',
  providerName: 'سالن رز',
  providerLogoUrl: 'https://x/logo.webp',
  serviceName: 'کوتاهی مو',
  bookingId: 'b1',
  rating: 4,
  comment: 'کار تمیز و دقیقی بود',
  dimensions: { cleanliness: 5, skill: null, punctuality: null, conduct: null },
  moderationStatus: 'Rejected',
  moderationReason: 'contains a phone number',
  providerResponse: null,
  replyModerationStatus: null,
  createdAt: '2026-09-20T10:00:00Z',
  editedAt: null,
  canEdit: false,
  ...overrides,
})

describe('customerService reviews', () => {
  beforeEach(() => {
    mine.mockReset()
    edit.mockReset()
    umGet.mockReset()
    umPatch.mockReset()
  })

  it('lists my reviews from the reviews API, never the UserManagement route that does not exist', async () => {
    mine.mockResolvedValue([myReview()])

    const reviews = await customerService.getReviews('c1')

    expect(mine).toHaveBeenCalled()
    expect(umGet).not.toHaveBeenCalled()
    expect(reviews[0]).toMatchObject({
      id: 'r1',
      providerName: 'سالن رز',
      providerLogoUrl: 'https://x/logo.webp',
      serviceName: 'کوتاهی مو',
      rating: 4,
      text: 'کار تمیز و دقیقی بود',
      canEdit: false,
      moderationStatus: 'Rejected',
      moderationReason: 'contains a phone number',
    })
  })

  it('edits through the reviews API with the dimensions, and returns the review as it now stands', async () => {
    edit.mockResolvedValue({ reviewId: 'r1', moderationStatus: 'Pending' })
    mine.mockResolvedValue([myReview({ rating: 2, moderationStatus: 'Pending', canEdit: true })])

    const updated = await customerService.updateReview('c1', 'r1', {
      rating: 2,
      text: 'دیر شروع کردند و عجله داشتند',
      dimensions: { punctuality: 1 },
    })

    expect(edit).toHaveBeenCalledWith('r1', {
      rating: 2,
      comment: 'دیر شروع کردند و عجله داشتند',
      dimensions: { punctuality: 1 },
    })
    expect(umPatch).not.toHaveBeenCalled()
    expect(updated.moderationStatus).toBe('Pending')
    expect(updated.rating).toBe(2)
  })
})
