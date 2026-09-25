import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import RatingSummary from '../RatingSummary.vue'

/**
 * reviews-and-reschedule-round2 item 2: «۴.۰ ۱ نظر» read as «۱۴». Rating and count are two things, split by a
 * separator, in Persian digits; no reviews is said in words, never as a zero rating.
 */
describe('RatingSummary', () => {
  it('reads «★ ۴.۰ · ۱ نظر» — star, value, separator, count', () => {
    const wrapper = mount(RatingSummary, { props: { rating: 4, count: 1 } })

    expect(wrapper.text()).toBe('★ ۴.۰ · ۱ نظر')
    expect(wrapper.get('[data-test="rating-value"]').text()).toBe('۴.۰')
    expect(wrapper.get('[data-test="rating-count"]').text()).toBe('۱ نظر')
    expect(wrapper.get('.rating-summary__sep').text()).toBe('·')
  })

  it('rounds to one decimal in Persian digits', () => {
    const wrapper = mount(RatingSummary, { props: { rating: 4.66, count: 12 } })

    expect(wrapper.get('[data-test="rating-value"]').text()).toBe('۴.۷')
    expect(wrapper.get('[data-test="rating-count"]').text()).toBe('۱۲ نظر')
  })

  it('says there are no reviews yet instead of a zero rating', () => {
    const cases: Array<{ rating?: number | null; count?: number | null }> = [
      { rating: 0, count: 0 },
      { rating: null, count: null },
      {},
    ]
    for (const props of cases) {
      const wrapper = mount(RatingSummary, { props })
      expect(wrapper.text()).toBe('هنوز نظری ثبت نشده')
      expect(wrapper.find('[data-test="rating-value"]').exists()).toBe(false)
    }
  })

  it('is one labelled image for a screen reader', () => {
    const wrapper = mount(RatingSummary, { props: { rating: 4.5, count: 3 } })

    expect(wrapper.attributes('aria-label')).toBe('امتیاز ۴.۵ از ۵، ۳ نظر')
  })
})
