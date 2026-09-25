import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ReviewCard from '../ReviewCard.vue'
import type { CustomerReview } from '../../../types/customer.types'

/**
 * One of the customer's own reviews in "My reviews" (customer-profile "Review Management"): its state, and the
 * administrator's reason when it was rejected or hidden. A review awaiting approval must never read as live.
 */

const review = (overrides: Partial<CustomerReview> = {}): CustomerReview => ({
  id: 'r1',
  providerId: 'p1',
  providerName: 'سالن رز',
  serviceId: '',
  serviceName: 'کوتاهی مو',
  rating: 4,
  text: 'کار تمیز و دقیقی بود',
  createdAt: '2026-09-20T10:00:00Z',
  canEdit: true,
  moderationStatus: 'Published',
  moderationReason: null,
  dimensions: { cleanliness: null, skill: null, punctuality: null, conduct: null },
  showName: true,
  ...overrides,
})

describe('customer ReviewCard', () => {
  it('marks a review that is awaiting approval as such', () => {
    const wrapper = mount(ReviewCard, { props: { review: review({ moderationStatus: 'Pending' }) } })

    expect(wrapper.get('[data-test="moderation-status"]').text()).toContain('در انتظار بررسی')
  })

  it('shows a rejected review as rejected, with the reason, and offers no edit', () => {
    const wrapper = mount(ReviewCard, {
      props: { review: review({ moderationStatus: 'Rejected', moderationReason: 'حاوی شماره تلفن', canEdit: false }) },
    })

    expect(wrapper.get('[data-test="moderation-status"]').text()).toContain('رد شد')
    expect(wrapper.get('[data-test="moderation-reason"]').text()).toContain('حاوی شماره تلفن')
    expect(wrapper.find('[data-test="edit-review"]').exists()).toBe(false)
  })

  it('offers edit only while the review can still be edited', () => {
    expect(mount(ReviewCard, { props: { review: review({ canEdit: true }) } }).find('[data-test="edit-review"]').exists()).toBe(true)
  })

  it('a published review past the edit window says why it can no longer be edited', () => {
    const wrapper = mount(ReviewCard, { props: { review: review({ canEdit: false }) } })

    expect(wrapper.find('[data-test="edit-review"]').exists()).toBe(false)
    expect(wrapper.get('[data-test="edit-window-closed"]').text()).toBe('فقط نظرات کمتر از ۷ روز قابل ویرایش هستند')
  })

  it('a rejected review is explained by its rejection, not by the edit window', () => {
    const wrapper = mount(ReviewCard, {
      props: { review: review({ moderationStatus: 'Rejected', moderationReason: 'x', canEdit: false }) },
    })

    expect(wrapper.find('[data-test="edit-window-closed"]').exists()).toBe(false)
  })
})
