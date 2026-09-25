import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent, h } from 'vue'

/**
 * Writing a review from the web (openspec/changes/_inline/customer-reviews-and-nahal-seed): the form and the endpoint
 * existed, but nothing reached them, so a customer could not leave a review at all.
 */

const showSuccess = vi.fn()
const submit = vi.fn()

vi.mock('@/core/composables/useNotification', () => ({ useNotification: () => ({ showSuccess }) }))
vi.mock('../../api/reviews.api', () => ({ reviewsApi: { submit: (...a: unknown[]) => submit(...a) } }))

const ModalStub = defineComponent({
  props: ['isOpen', 'title'],
  setup: (_, { slots }) => () => h('div', slots.default?.()),
})
const FormStub = defineComponent({
  props: ['submitting'],
  emits: ['submit', 'cancel'],
  setup: (_, { emit }) => () =>
    h('button', {
      'data-test': 'send',
      onClick: () => emit('submit', { rating: 5, comment: 'کار تمیز و دقیقی بود', dimensions: {} }),
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
  })

  it('names the visit being reviewed', () => {
    expect(mountModal().get('[data-test="review-subject"]').text()).toBe('سالن نهال · کوتاهی مو')
  })

  it('sends the review for the booking, says it awaits approval, and hands back its id', async () => {
    submit.mockResolvedValue({ reviewId: 'r9', moderationStatus: 'Pending' })
    const wrapper = mountModal()

    await wrapper.get('[data-test="send"]').trigger('click')
    await flushPromises()

    expect(submit).toHaveBeenCalledWith('b1', { rating: 5, comment: 'کار تمیز و دقیقی بود', dimensions: {} })
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
})
