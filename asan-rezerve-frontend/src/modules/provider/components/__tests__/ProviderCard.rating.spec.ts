import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ProviderCard from '../ProviderCard.vue'
import type { ProviderSummary } from '../../types/provider.types'

/** reviews-and-reschedule-round2 item 2: the search card showed no rating at all. */

const provider = (extra: Partial<ProviderSummary> = {}): ProviderSummary =>
  ({
    id: 'p1',
    businessName: 'سالن نهال',
    description: 'آرایشگاه زنانه',
    type: 'Individual',
    status: 'Active',
    city: 'تهران',
    state: 'تهران',
    country: 'ایران',
    allowOnlineBooking: true,
    offersMobileServices: false,
    tags: [],
    registeredAt: '2026-01-01T00:00:00Z',
    ...extra,
  }) as ProviderSummary

describe('ProviderCard — rating', () => {
  it('shows the rating and review count as two things', () => {
    const wrapper = mount(ProviderCard, { props: { provider: provider({ averageRating: 4.5, totalReviews: 18 }) } })

    expect(wrapper.get('[data-testid="provider-card-rating"]').text()).toBe('★ ۴.۵ · ۱۸ نظر')
  })

  it('a salon nobody has reviewed says so, not «۰.۰»', () => {
    const wrapper = mount(ProviderCard, { props: { provider: provider({ averageRating: 0, totalReviews: 0 }) } })

    expect(wrapper.get('[data-testid="provider-card-rating"]').text()).toBe('هنوز نظری ثبت نشده')
  })
})
