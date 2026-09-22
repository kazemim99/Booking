import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ReviewForm from '../ReviewForm.vue'

/**
 * Writing or editing a review: one required overall star, four optional dimensions behind a disclosure so the
 * required path stays a single tap, and an optional comment of 10–2000 characters.
 */

async function rate(wrapper: ReturnType<typeof mount>, testId: string, stars: number) {
  await wrapper.get(`[data-test="${testId}"] [data-test="star-${stars}"]`).trigger('click')
}

describe('ReviewForm', () => {
  it('cannot be sent without the overall rating', async () => {
    const wrapper = mount(ReviewForm)

    expect(wrapper.get('[data-test="submit"]').attributes('disabled')).toBeDefined()

    await rate(wrapper, 'overall', 4)

    expect(wrapper.get('[data-test="submit"]').attributes('disabled')).toBeUndefined()
  })

  it('keeps the four dimensions behind a disclosure until asked for', async () => {
    const wrapper = mount(ReviewForm)

    expect(wrapper.find('[data-test="dimension-cleanliness"]').exists()).toBe(false)

    await wrapper.get('[data-test="dimensions-toggle"]').trigger('click')

    for (const d of ['cleanliness', 'skill', 'punctuality', 'conduct']) {
      expect(wrapper.find(`[data-test="dimension-${d}"]`).exists()).toBe(true)
    }
  })

  it('sends the overall rating and only the dimensions that were rated', async () => {
    const wrapper = mount(ReviewForm)
    await rate(wrapper, 'overall', 5)
    await wrapper.get('[data-test="dimensions-toggle"]').trigger('click')
    await rate(wrapper, 'dimension-punctuality', 3)
    await wrapper.get('[data-test="comment"]').setValue('خیلی تمیز بود ولی کمی دیر شروع شد')

    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')![0][0]).toEqual({
      rating: 5,
      comment: 'خیلی تمیز بود ولی کمی دیر شروع شد',
      dimensions: { punctuality: 3 },
    })
  })

  it('never recomputes the overall from the dimensions', async () => {
    const wrapper = mount(ReviewForm)
    await rate(wrapper, 'overall', 5)
    await wrapper.get('[data-test="dimensions-toggle"]').trigger('click')
    await rate(wrapper, 'dimension-skill', 2)

    await wrapper.get('form').trigger('submit')

    expect((wrapper.emitted('submit')![0][0] as { rating: number }).rating).toBe(5)
  })

  it('refuses a comment shorter than 10 characters and says why', async () => {
    const wrapper = mount(ReviewForm)
    await rate(wrapper, 'overall', 4)
    await wrapper.get('[data-test="comment"]').setValue('خوب بود')

    expect(wrapper.get('[data-test="submit"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-test="comment-hint"]').text()).toContain('۱۰')
  })

  it('allows leaving the comment out entirely', async () => {
    const wrapper = mount(ReviewForm)
    await rate(wrapper, 'overall', 3)

    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')![0][0]).toEqual({ rating: 3, comment: undefined, dimensions: {} })
  })

  it('opens pre-filled for an edit, with the disclosure open when dimensions were given', () => {
    const wrapper = mount(ReviewForm, {
      props: { initial: { rating: 4, comment: 'کار تمیز و دقیقی بود', dimensions: { conduct: 5 } } },
    })

    expect(wrapper.find('[data-test="dimension-conduct"]').exists()).toBe(true)
    expect((wrapper.get('[data-test="comment"]').element as HTMLTextAreaElement).value).toBe('کار تمیز و دقیقی بود')
    expect(wrapper.get('[data-test="submit"]').attributes('disabled')).toBeUndefined()
  })
})
