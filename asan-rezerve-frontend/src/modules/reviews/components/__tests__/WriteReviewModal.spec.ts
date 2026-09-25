import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent, h } from 'vue'

/**
 * Writing a review from the web (openspec/changes/_inline/customer-reviews-and-nahal-seed): the form and the endpoint
 * existed, but nothing reached them, so a customer could not leave a review at all.
 */

const showSuccess = vi.fn()
const submit = vi.fn()
const edit = vi.fn()
const mine = vi.fn()

vi.mock('@/core/composables/useNotification', () => ({ useNotification: () => ({ showSuccess }) }))
vi.mock('../../api/reviews.api', () => ({
  reviewsApi: {
    submit: (...a: unknown[]) => submit(...a),
    edit: (...a: unknown[]) => edit(...a),
    mine: (...a: unknown[]) => mine(...a),
  },
}))

const ModalStub = defineComponent({
  props: ['isOpen', 'title'],
  setup: (_, { slots }) => () => h('div', slots.default?.()),
})
const INPUT = {
  rating: 5,
  comment: 'کار تمیز و دقیقی بود',
  dimensions: { cleanliness: 5, skill: 5, punctuality: 5, conduct: 5 },
  showName: true,
}
const FormStub = defineComponent({
  props: ['submitting', 'initial', 'submitLabel'],
  emits: ['submit', 'cancel'],
  setup: (props, { emit }) => () =>
    h('button', {
      'data-test': 'send',
      'data-initial': JSON.stringify(props.initial ?? null),
      'data-label': props.submitLabel,
      onClick: () => emit('submit', INPUT),
    }),
})

import WriteReviewModal from '../WriteReviewModal.vue'

const mountModal = () =>
  mount(WriteReviewModal, {
    props: { isOpen: true, bookingId: 'b1', subject: 'سالن نهال · کوتاهی مو' },
    global: { stubs: { BaseModal: ModalStub, ReviewForm: FormStub } },
  })

describe('WriteReviewModal', () => {
  beforeEach(() => {
    showSuccess.mockReset()
    submit.mockReset()
    edit.mockReset()
    mine.mockReset()
  })

  it('names the visit being reviewed', () => {
    expect(mountModal().get('[data-test="review-subject"]').text()).toBe('سالن نهال · کوتاهی مو')
  })

  it('sends the review for the booking, says it awaits approval, and hands back its id', async () => {
    submit.mockResolvedValue({ reviewId: 'r9', moderationStatus: 'Pending' })
    const wrapper = mountModal()

    await wrapper.get('[data-test="send"]').trigger('click')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith('b1', INPUT)
    expect(showSuccess).toHaveBeenCalledWith('نظر شما ثبت شد', expect.stringContaining('پس از تأیید'))
    expect(wrapper.emitted('saved')).toEqual([['r9']])
  })

  it('a refusal keeps the form open — its reason is the API client\'s toast', async () => {
    submit.mockRejectedValue(new Error('409'))
    const wrapper = mountModal()

    await wrapper.get('[data-test="send"]').trigger('click')
    await flushPromises()

    expect(wrapper.emitted('saved')).toBeUndefined()
    expect(wrapper.emitted('close')).toBeUndefined()
    expect(showSuccess).not.toHaveBeenCalled()
  })

  // reviews-and-reschedule-round2: one review per salon — a booking card whose salon already has an editable review
  // offers «ویرایش نظر», which opens this modal on that review.
  it('given a review, opens it pre-filled — name choice included — and saves it as an edit', async () => {
    mine.mockResolvedValue([
      {
        reviewId: 'r7',
        rating: 4,
        comment: 'خوب بود ولی دیر شروع شد',
        dimensions: { cleanliness: 4, skill: 4, punctuality: 3, conduct: 5 },
        showName: false,
      },
    ])
    edit.mockResolvedValue({ reviewId: 'r7', moderationStatus: 'Pending' })
    const wrapper = mount(WriteReviewModal, {
      props: { isOpen: true, bookingId: 'b1', reviewId: 'r7' },
      global: { stubs: { BaseModal: ModalStub, ReviewForm: FormStub } },
    })
    await flushPromises()

    const form = wrapper.get('[data-test="send"]')
    expect(JSON.parse(form.attributes('data-initial')!)).toEqual({
      rating: 4,
      comment: 'خوب بود ولی دیر شروع شد',
      dimensions: { cleanliness: 4, skill: 4, punctuality: 3, conduct: 5 },
      showName: false,
    })
    expect(form.attributes('data-label')).toBe('ذخیره تغییرات')

    await form.trigger('click')
    await flushPromises()

    expect(edit).toHaveBeenCalledWith('r7', INPUT)
    expect(submit).not.toHaveBeenCalled()
    expect(wrapper.emitted('saved')).toEqual([['r7']])
  })

  it('a review that is no longer there says so instead of an empty form', async () => {
    mine.mockResolvedValue([])
    const wrapper = mount(WriteReviewModal, {
      props: { isOpen: true, bookingId: 'b1', reviewId: 'gone' },
      global: { stubs: { BaseModal: ModalStub, ReviewForm: FormStub } },
    })
    await flushPromises()

    expect(wrapper.find('[data-test="send"]').exists()).toBe(false)
    expect(wrapper.find('[data-test="review-missing"]').exists()).toBe(true)
  })
})
