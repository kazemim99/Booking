import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import RescheduleAction from '../RescheduleAction.vue'

/**
 * The 24-hour reschedule rule worked in the QA recording (2026-09-24) — but the customer met it only at the very end,
 * after choosing a slot. The booking now says up front why it cannot be moved, and «تغییر زمان» is disabled with that
 * reason instead of being a dead end.
 */
const REASON = 'تغییر زمان تا 24 ساعت پیش از نوبت ممکن است؛ برای تغییر با سالن تماس بگیرید.'

describe('RescheduleAction', () => {
  it('a booking that can be moved has an enabled button and no reason', async () => {
    const wrapper = mount(RescheduleAction, { props: { rescheduleBlockedReason: null } })

    const button = wrapper.get('[data-testid="booking-reschedule-button"]')
    expect(button.attributes('disabled')).toBeUndefined()
    expect(wrapper.text()).not.toContain('24')

    await button.trigger('click')
    expect(wrapper.emitted('reschedule')).toHaveLength(1)
  })

  it('inside the window the button is disabled and the reason is shown', async () => {
    const wrapper = mount(RescheduleAction, { props: { rescheduleBlockedReason: REASON } })

    const button = wrapper.get('[data-testid="booking-reschedule-button"]')
    expect(button.attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain(REASON)

    await button.trigger('click')
    expect(wrapper.emitted('reschedule')).toBeUndefined()
  })

  it('a blank reason is no reason', () => {
    const wrapper = mount(RescheduleAction, { props: { rescheduleBlockedReason: '  ' } })

    expect(wrapper.get('[data-testid="booking-reschedule-button"]').attributes('disabled')).toBeUndefined()
  })
})
