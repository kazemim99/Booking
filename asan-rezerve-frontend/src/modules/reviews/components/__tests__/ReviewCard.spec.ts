import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ReviewCard from '../ReviewCard.vue'
import type { ProviderReview } from '../../types/reviews.types'

const review = (overrides: Partial<ProviderReview> = {}): ProviderReview => ({
  reviewId: 'r1',
  rating: 4.5,
  comment: 'کار تمیز و دقیقی بود',
  isVerified: true,
  providerResponse: null,
  helpfulCount: 3,
  notHelpfulCount: 1,
  createdAt: '2026-09-20T10:00:00Z',
  dimensions: { cleanliness: 5, skill: null, punctuality: null, conduct: null },
  myVote: null,
  ...overrides,
})

describe('ReviewCard', () => {
  it('shows only the dimensions the customer rated', () => {
    const wrapper = mount(ReviewCard, { props: { review: review() } })

    const chips = wrapper.findAll('[data-test="dimension-chip"]')
    expect(chips).toHaveLength(1)
    expect(chips[0].text()).toContain('نظافت')
  })

  it('shows the salon’s reply when the review carries one', () => {
    const wrapper = mount(ReviewCard, { props: { review: review({ providerResponse: 'ممنون از لطف شما' }) } })

    expect(wrapper.get('[data-test="reply"]').text()).toContain('ممنون از لطف شما')
  })

  it('has no reply block when there is none — the API sends a reply only once it is approved', () => {
    const wrapper = mount(ReviewCard, { props: { review: review() } })

    expect(wrapper.find('[data-test="reply"]').exists()).toBe(false)
  })

  it('renders the reader’s own vote as pressed', () => {
    const wrapper = mount(ReviewCard, { props: { review: review({ myVote: 'helpful' }) } })

    expect(wrapper.get('[data-test="vote-helpful"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('[data-test="vote-not-helpful"]').attributes('aria-pressed')).toBe('false')
  })

  it('asks the parent to vote, and shows the counts it was given', async () => {
    const wrapper = mount(ReviewCard, { props: { review: review() } })

    expect(wrapper.get('[data-test="vote-helpful"]').text()).toContain('۳')
    await wrapper.get('[data-test="vote-not-helpful"]').trigger('click')

    expect(wrapper.emitted('vote')![0]).toEqual([false])
  })
})
