import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ReviewForm from '../ReviewForm.vue'

/**
 * Writing or editing a review (reviews-and-reschedule-round2, D1): no separate overall row — the customer rates the
 * four aspects, all required, and the overall is their average to the nearest half star, shown live. A tick box lets
 * the author keep their name off the public review. The comment stays optional, 10–2000 characters.
 */

type Wrapper = ReturnType<typeof mount>

async function rate(wrapper: Wrapper, testId: string, stars: number) {
  await wrapper.get(`[data-test="${testId}"] [data-test="star-${stars}"]`).trigger('click')
}

async function rateAll(wrapper: Wrapper, [c, s, p, d]: [number, number, number, number]) {
  await rate(wrapper, 'dimension-cleanliness', c)
  await rate(wrapper, 'dimension-skill', s)
  await rate(wrapper, 'dimension-punctuality', p)
  await rate(wrapper, 'dimension-conduct', d)
}

const submitDisabled = (wrapper: Wrapper) => wrapper.get('[data-test="submit"]').attributes('disabled') !== undefined

describe('ReviewForm', () => {
  it('has no separate overall row and no disclosure — the four aspects are shown at once', () => {
    const wrapper = mount(ReviewForm)

    expect(wrapper.find('[data-test="overall"]').exists()).toBe(false)
    expect(wrapper.find('[data-test="dimensions-toggle"]').exists()).toBe(false)
    for (const d of ['cleanliness', 'skill', 'punctuality', 'conduct']) {
      expect(wrapper.find(`[data-test="dimension-${d}"]`).exists()).toBe(true)
    }
  })

  it('cannot be sent until all four aspects are rated', async () => {
    const wrapper = mount(ReviewForm)
    expect(submitDisabled(wrapper)).toBe(true)

    await rate(wrapper, 'dimension-cleanliness', 5)
    await rate(wrapper, 'dimension-skill', 5)
    await rate(wrapper, 'dimension-punctuality', 4)
    expect(submitDisabled(wrapper)).toBe(true)

    await rate(wrapper, 'dimension-conduct', 4)
    expect(submitDisabled(wrapper)).toBe(false)
  })

  it('shows the overall live, in Persian digits, as the half-star average', async () => {
    const wrapper = mount(ReviewForm)
    expect(wrapper.find('[data-test="overall-live"]').exists()).toBe(false)

    await rateAll(wrapper, [5, 5, 4, 4])
    expect(wrapper.get('[data-test="overall-live"]').text()).toBe('امتیاز کلی: ۴.۵')

    await rate(wrapper, 'dimension-skill', 3) // 5+3+4+4 = 16 → 4
    expect(wrapper.get('[data-test="overall-live"]').text()).toBe('امتیاز کلی: ۴.۰')
  })

  it('sends the four aspects, the derived overall, the comment, and shows the name by default', async () => {
    const wrapper = mount(ReviewForm)
    await rateAll(wrapper, [4, 3, 3, 3]) // 3.25 → 3.5
    await wrapper.get('[data-test="comment"]').setValue('خیلی تمیز بود ولی کمی دیر شروع شد')

    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')![0][0]).toEqual({
      rating: 3.5,
      comment: 'خیلی تمیز بود ولی کمی دیر شروع شد',
      dimensions: { cleanliness: 4, skill: 3, punctuality: 3, conduct: 3 },
      showName: true,
    })
  })

  it('«نامم در نظر نمایش داده نشود» — unticked by default, ticked sends showName false', async () => {
    const wrapper = mount(ReviewForm)
    const box = wrapper.get('[data-test="hide-name"]')
    expect(box.text()).toContain('نامم در نظر نمایش داده نشود')
    expect((wrapper.get('[data-test="hide-name"] input').element as HTMLInputElement).checked).toBe(false)

    await wrapper.get('[data-test="hide-name"] input').setValue(true)
    await rateAll(wrapper, [5, 5, 5, 5])
    await wrapper.get('form').trigger('submit')

    expect((wrapper.emitted('submit')![0][0] as { showName: boolean }).showName).toBe(false)
  })

  it('refuses a comment shorter than 10 characters and says why', async () => {
    const wrapper = mount(ReviewForm)
    await rateAll(wrapper, [4, 4, 4, 4])
    await wrapper.get('[data-test="comment"]').setValue('خوب بود')

    expect(submitDisabled(wrapper)).toBe(true)
    expect(wrapper.get('[data-test="comment-hint"]').text()).toContain('۱۰')
  })

  it('allows leaving the comment out entirely', async () => {
    const wrapper = mount(ReviewForm)
    await rateAll(wrapper, [3, 3, 3, 3])

    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')![0][0]).toMatchObject({ rating: 3, comment: undefined })
  })

  it('opens pre-filled for an edit, including the name choice', () => {
    const wrapper = mount(ReviewForm, {
      props: {
        initial: {
          rating: 4,
          comment: 'کار تمیز و دقیقی بود',
          dimensions: { cleanliness: 5, skill: 4, punctuality: 3, conduct: 4 },
          showName: false,
        },
      },
    })

    expect(wrapper.get('[data-test="dimension-cleanliness"] [data-test="star-5"]').attributes('aria-checked')).toBe('true')
    expect((wrapper.get('[data-test="comment"]').element as HTMLTextAreaElement).value).toBe('کار تمیز و دقیقی بود')
    expect((wrapper.get('[data-test="hide-name"] input').element as HTMLInputElement).checked).toBe(true)
    expect(submitDisabled(wrapper)).toBe(false)
  })

  it('an older review with only an overall opens with each aspect at that overall, rounded', async () => {
    const wrapper = mount(ReviewForm, { props: { initial: { rating: 4.5, dimensions: {} } } })

    for (const d of ['cleanliness', 'skill', 'punctuality', 'conduct']) {
      expect(wrapper.get(`[data-test="dimension-${d}"] [data-test="star-5"]`).attributes('aria-checked')).toBe('true')
    }
    expect(submitDisabled(wrapper)).toBe(false)
  })

  it('an older review keeps the aspects it did have, and fills only the missing ones', () => {
    const wrapper = mount(ReviewForm, { props: { initial: { rating: 3, dimensions: { conduct: 5, skill: null } } } })

    expect(wrapper.get('[data-test="dimension-conduct"] [data-test="star-5"]').attributes('aria-checked')).toBe('true')
    expect(wrapper.get('[data-test="dimension-skill"] [data-test="star-3"]').attributes('aria-checked')).toBe('true')
  })

  it('an edit without a name choice on file shows the name — the default', () => {
    const wrapper = mount(ReviewForm, { props: { initial: { rating: 4, dimensions: {} } } })

    expect((wrapper.get('[data-test="hide-name"] input').element as HTMLInputElement).checked).toBe(false)
  })
})
