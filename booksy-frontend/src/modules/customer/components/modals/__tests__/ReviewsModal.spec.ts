import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { defineComponent, h } from 'vue'

/**
 * "My reviews" saving an edit (customer-profile "Review Management"): the customer MUST be told that an edited
 * review returns for approval before it is public again — "saved" alone would read as live.
 */

const showSuccess = vi.fn()
const showError = vi.fn()
const updateReview = vi.fn()

vi.mock('@/core/composables/useNotification', () => ({ useNotification: () => ({ showSuccess, showError }) }))
vi.mock('@/core/stores/modules/auth.store', () => ({ useAuthStore: () => ({ user: { id: 'u1' } }) }))
vi.mock('../../../stores/customer.store', () => ({
  useCustomerStore: () => ({
    reviews: [{ id: 'r1', canEdit: true }],
    loading: { reviews: false },
    fetchReviews: vi.fn().mockResolvedValue(undefined),
    updateReview: (...a: unknown[]) => updateReview(...a),
  }),
}))

const slot = defineComponent({ setup: (_, { slots }) => () => h('div', slots.default?.()) })
const CardStub = defineComponent({
  props: ['review'],
  emits: ['edit'],
  setup: (props, { emit }) => () => h('button', { 'data-test': 'edit', onClick: () => emit('edit', props.review) }),
})
const EditStub = defineComponent({
  props: ['review', 'isOpen'],
  emits: ['save', 'close'],
  setup: (_, { emit }) => () =>
    h('button', { 'data-test': 'save', onClick: () => emit('save', 'r1', { rating: 4, text: 'متن تازه و کامل', dimensions: {} }) }),
})

import ReviewsModal from '../ReviewsModal.vue'

describe('ReviewsModal saving an edit', () => {
  beforeEach(() => {
    showSuccess.mockReset()
    showError.mockReset()
    updateReview.mockReset()
  })

  it('says the edited review is saved and awaiting approval', async () => {
    updateReview.mockResolvedValue(undefined)
    const wrapper = mount(ReviewsModal, {
      props: { isOpen: true },
      global: { stubs: { teleport: true, ResponsiveModal: slot, ReviewCard: CardStub, EditReviewModal: EditStub } },
    })
    await flushPromises()
    await wrapper.get('[data-test="edit"]').trigger('click')

    await wrapper.get('[data-test="save"]').trigger('click')
    await flushPromises()

    expect(updateReview).toHaveBeenCalledWith('u1', 'r1', expect.objectContaining({ rating: 4 }))
    expect(showSuccess).toHaveBeenCalledTimes(1)
    expect(String(showSuccess.mock.calls[0][1])).toContain('تأیید')
  })
})
