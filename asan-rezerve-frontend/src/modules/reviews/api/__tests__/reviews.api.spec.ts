import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * The client for the review endpoints (provider-reviews-and-ratings).
 *
 * Before this change the web app had three review surfaces and none of them reached the backend: the provider page
 * rendered six hardcoded reviews, the "my reviews" modal called a UserManagement route that does not exist, and the
 * one service that named the real routes was imported by nothing. These tests pin the routes AND exercise them.
 */

const get = vi.fn()
const post = vi.fn()
const put = vi.fn()

vi.mock('@/core/api/client/http-client', () => ({
  serviceCategoryClient: {
    get: (...args: unknown[]) => get(...args),
    post: (...args: unknown[]) => post(...args),
    put: (...args: unknown[]) => put(...args),
  },
}))

import { apiEndpoints } from '@/core/api/config/api-config'
import { reviewsApi } from '../reviews.api'

describe('the declared review routes', () => {
  it('are the routes the backend actually serves', () => {
    expect(apiEndpoints.reviews.forProvider('p1')).toBe('v1/Reviews/providers/p1')
    expect(apiEndpoints.reviews.submit('b1')).toBe('v1/Reviews/bookings/b1')
    expect(apiEndpoints.reviews.edit('r1')).toBe('v1/Reviews/r1')
    expect(apiEndpoints.reviews.vote('r1')).toBe('v1/Reviews/r1/helpful')
    expect(apiEndpoints.reviews.report('r1')).toBe('v1/Reviews/r1/report')
    expect(apiEndpoints.reviews.mine).toBe('v1/Reviews/me')
  })
})

describe('reviewsApi', () => {
  beforeEach(() => {
    get.mockReset()
    post.mockReset()
    put.mockReset()
  })

  it('submits the overall rating and only the dimensions that were given', async () => {
    post.mockResolvedValue({ data: { reviewId: 'r1', moderationStatus: 'Pending' } })

    const created = await reviewsApi.submit('b1', {
      rating: 4.5,
      comment: 'کار تمیز و دقیقی بود',
      dimensions: { cleanliness: 5, punctuality: 3 },
    })

    expect(post).toHaveBeenCalledWith('v1/Reviews/bookings/b1', {
      rating: 4.5,
      comment: 'کار تمیز و دقیقی بود',
      cleanlinessRating: 5,
      punctualityRating: 3,
    })
    expect(created.moderationStatus).toBe('Pending')
  })

  it('edits with the same body shape', async () => {
    put.mockResolvedValue({ data: { reviewId: 'r1', moderationStatus: 'Pending' } })

    await reviewsApi.edit('r1', { rating: 2, dimensions: { conduct: 1 } })

    expect(put).toHaveBeenCalledWith('v1/Reviews/r1', { rating: 2, conductRating: 1 })
  })

  it('lists a provider’s reviews past the GET cache, so a fresh vote shows at once', async () => {
    get.mockResolvedValue({
      data: {
        providerId: 'p1',
        statistics: { totalReviews: 1, averageRating: 5, punctuality: { average: 4, count: 1 } },
        reviews: { items: [{ reviewId: 'r1', rating: 5, myVote: 'helpful' }], totalCount: 1 },
      },
    })

    const listing = await reviewsApi.forProvider('p1', { pageNumber: 2, sortBy: 'helpful' })

    expect(get).toHaveBeenCalledWith(
      'v1/Reviews/providers/p1',
      expect.objectContaining({ cache: false, params: expect.objectContaining({ pageNumber: 2, sortBy: 'helpful' }) }),
    )
    expect(listing.statistics.totalReviews).toBe(1)
    expect(listing.statistics.punctuality).toEqual({ average: 4, count: 1 })
    expect(listing.reviews[0].myVote).toBe('helpful')
  })

  it('treats a dimension nobody rated as absent, not zero', async () => {
    get.mockResolvedValue({ data: { statistics: { totalReviews: 0 }, reviews: { items: [] } } })

    const listing = await reviewsApi.forProvider('p1')

    expect(listing.statistics.cleanliness).toEqual({ average: null, count: 0 })
    expect(listing.reviews).toEqual([])
  })

  it('votes with a PUT and reports the caller’s vote afterwards', async () => {
    put.mockResolvedValue({ data: { reviewId: 'r1', helpfulCount: 3, notHelpfulCount: 0, myVote: 'helpful' } })

    const result = await reviewsApi.vote('r1', true)

    expect(put).toHaveBeenCalledWith('v1/Reviews/r1/helpful', { isHelpful: true })
    expect(result.myVote).toBe('helpful')
  })

  it('reads a withdrawn vote — an absent myVote — as null', async () => {
    put.mockResolvedValue({ data: { reviewId: 'r1', helpfulCount: 0, notHelpfulCount: 0 } })

    expect((await reviewsApi.vote('r1', true)).myVote).toBeNull()
  })

  it('reports with a reason', async () => {
    post.mockResolvedValue({ data: { reportId: 'x', reviewId: 'r1' } })

    await reviewsApi.report('r1', 'توهین به کارکنان')

    expect(post).toHaveBeenCalledWith('v1/Reviews/r1/report', { reason: 'توهین به کارکنان' })
  })

  it('lists the signed-in customer’s own reviews in every state, uncached', async () => {
    get.mockResolvedValue({
      data: { items: [{ reviewId: 'r1', moderationStatus: 'Rejected', moderationReason: 'spam', canEdit: false }] },
    })

    const mine = await reviewsApi.mine()

    expect(get).toHaveBeenCalledWith('v1/Reviews/me', expect.objectContaining({ cache: false }))
    expect(mine[0].moderationStatus).toBe('Rejected')
    expect(mine[0].moderationReason).toBe('spam')
  })
})
