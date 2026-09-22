import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

/**
 * A provider's reviews on their public page. Replaces a component that rendered six hardcoded reviews, a hardcoded
 * distribution and the constants 4.8 / 127 — so everything here must come from the API, and "no reviews yet" must
 * mean the API said so, never that a request failed.
 */

const forProvider = vi.fn()
const vote = vi.fn()

vi.mock('../../api/reviews.api', () => ({
  reviewsApi: {
    forProvider: (...a: unknown[]) => forProvider(...a),
    vote: (...a: unknown[]) => vote(...a),
  },
}))

import ReviewList from '../ReviewList.vue'

const none = { average: null, count: 0 }

function listing(reviews: unknown[], overrides: Record<string, unknown> = {}) {
  return {
    statistics: {
      totalReviews: reviews.length,
      averageRating: reviews.length ? 4.5 : 0,
      ratingDistribution: {},
      cleanliness: { average: 4.5, count: 2 },
      skill: none,
      punctuality: none,
      conduct: none,
      ...overrides,
    },
    reviews,
    totalCount: reviews.length,
  }
}

const r = (id: string, extra: Record<string, unknown> = {}) => ({
  reviewId: id,
  rating: 4.5,
  comment: `متن نظر ${id} با جزئیات کافی`,
  isVerified: true,
  providerResponse: null,
  helpfulCount: 0,
  notHelpfulCount: 0,
  createdAt: '2026-09-20T10:00:00Z',
  dimensions: { cleanliness: null, skill: null, punctuality: null, conduct: null },
  myVote: null,
  ...extra,
})

async function render() {
  const wrapper = mount(ReviewList, { props: { providerId: 'p1' } })
  await flushPromises()
  return wrapper
}

describe('ReviewList', () => {
  beforeEach(() => {
    forProvider.mockReset()
    vote.mockReset()
  })

  it('says "no reviews yet" when the API says there are none — and shows no rating', async () => {
    forProvider.mockResolvedValue(listing([]))

    const wrapper = await render()

    expect(wrapper.find('[data-test="reviews-empty"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="rating-average"]').exists()).toBe(false)
  })

  it('never claims "no reviews" because a request failed', async () => {
    forProvider.mockRejectedValue(new Error('network'))

    const wrapper = await render()

    expect(wrapper.find('[data-test="reviews-error"]').exists()).toBe(true)
    expect(wrapper.find('[data-test="reviews-empty"]').exists()).toBe(false)
  })

  it('renders what the API returned, not a fixture', async () => {
    forProvider.mockResolvedValue(listing([r('a'), r('b')]))

    const wrapper = await render()

    expect(wrapper.findAll('[data-test="review-card"]')).toHaveLength(2)
    expect(wrapper.text()).toContain('متن نظر a')
    expect(wrapper.get('[data-test="rating-count"]').text()).toContain('۲')
  })

  it('shows a dimension row only for dimensions somebody rated', async () => {
    forProvider.mockResolvedValue(listing([r('a')]))

    const wrapper = await render()

    const rows = wrapper.findAll('[data-test="dimension-row"]')
    expect(rows).toHaveLength(1)
    expect(rows[0].text()).toContain('نظافت')
  })

  it('applies a vote from what the server answered', async () => {
    forProvider.mockResolvedValue(listing([r('a')]))
    vote.mockResolvedValue({ reviewId: 'a', helpfulCount: 1, notHelpfulCount: 0, myVote: 'helpful' })
    const wrapper = await render()

    await wrapper.get('[data-test="vote-helpful"]').trigger('click')
    await flushPromises()

    expect(vote).toHaveBeenCalledWith('a', true)
    expect(wrapper.get('[data-test="vote-helpful"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('[data-test="vote-helpful"]').text()).toContain('۱')
  })

  it('asks a signed-out reader to sign in instead of failing silently', async () => {
    forProvider.mockResolvedValue(listing([r('a')]))
    vote.mockRejectedValue({ response: { status: 401 } })
    const wrapper = await render()

    await wrapper.get('[data-test="vote-helpful"]').trigger('click')
    await flushPromises()

    expect(wrapper.find('[data-test="sign-in-to-vote"]').exists()).toBe(true)
  })
})
