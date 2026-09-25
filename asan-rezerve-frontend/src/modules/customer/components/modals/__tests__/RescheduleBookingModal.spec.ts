import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'

/**
 * reviews-and-reschedule-round2 (item 9): a moved booking is a new request the salon confirms again. The customer is
 * told so before confirming the move — not only afterwards.
 */

vi.mock('@/core/composables/useNotification', () => ({ useNotification: () => ({ showError: vi.fn() }) }))
vi.mock('@/modules/booking/api/availability.service', () => ({ availabilityService: { getAvailableSlots: vi.fn() } }))
vi.mock('vue3-persian-datetime-picker', () => ({ default: defineComponent({ render: () => h('div') }) }))

import RescheduleBookingModal from '../RescheduleBookingModal.vue'

const ModalStub = defineComponent({
  props: ['isOpen', 'title'],
  setup: (props, { slots }) => () => h('div', { 'data-title': props.title }, slots.default?.()),
})

const mountModal = (props: Record<string, unknown> = {}) =>
  mount(RescheduleBookingModal, {
    props: { isOpen: true, booking: null, ...props },
    global: { stubs: { BaseModal: ModalStub } },
  })

describe('RescheduleBookingModal', () => {
  it('says the new time must be confirmed by the salon again, before the move is confirmed', () => {
    const wrapper = mountModal()

    expect(wrapper.get('[data-testid="reschedule-reconfirm-notice"]').text()).toBe(
      'زمان جدید باید دوباره توسط سالن تأیید شود.',
    )
    expect(wrapper.get('[data-testid="reschedule-confirm"]').text()).toContain('تأیید تغییر زمان')
  })

  it('reused to book again, it is not a move — no re-confirmation notice', () => {
    const wrapper = mountModal({ mode: 'rebook' })

    expect(wrapper.find('[data-testid="reschedule-reconfirm-notice"]').exists()).toBe(false)
    expect(wrapper.get('[data-title]').attributes('data-title')).toBe('رزرو مجدد')
  })
})
