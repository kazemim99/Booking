import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ReviewCard from '../ReviewCard.vue'
import type { ProviderReview } from '../../types/reviews.types'

const review = (overrides: Partial<ProviderReview> = {}): ProviderReview => ({
  reviewId: 'r1',
  customerName: 'مریم ر.',
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
  // openspec/changes/_inline/customer-reviews-and-nahal-seed: the listing names the author; the card dropped it.
  it('names its author the way the listing signs them, with their initial', () => {
    const wrapper = mount(ReviewCard, { props: { review: review() } })

    expect(wrapper.get('[data-test="review-author"]').text()).toBe('مریم ر.')
    expect(wrapper.get('.review-card__avatar').text()).toBe('م')
  })

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

  // reviews-and-reschedule-round2 item 7: the reply read as a grey note, not as the salon answering THIS review.
  it('signs the reply with the salon’s name, as an answer nested under the review', () => {
    const wrapper = mount(ReviewCard, {
      props: { review: review({ providerResponse: 'ممنون از لطف شما' }), providerName: 'سالن نهال' },
    })

    const reply = wrapper.get('[data-test="reply"]')
    expect(reply.get('[data-test="reply-author"]').text()).toBe('پاسخ سالن نهال')
    expect(reply.find('.review-card__reply-avatar svg').exists()).toBe(true)
    expect(reply.find('.review-card__reply-icon').exists()).toBe(true)
    // Inside the review's own article, so it is visibly — and for a screen reader — that review's answer.
    expect(reply.attributes('aria-label')).toBe('پاسخ سالن نهال به این نظر')
  })

  it('without the salon’s name the reply is still signed «پاسخ سالن»', () => {
    const wrapper = mount(ReviewCard, { props: { review: review({ providerResponse: 'ممنون' }) } })

    expect(wrapper.get('[data-test="reply-author"]').text()).toBe('پاسخ سالن')
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
