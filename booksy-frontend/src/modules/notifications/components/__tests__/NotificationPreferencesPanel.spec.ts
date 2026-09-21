import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

/**
 * The notification settings a person is shown — now only the ones that change anything.
 *
 * Measured before this was written: on this backend the SMS toggle could never have an effect (SMS is reserved
 * for critical notifications, and critical notifications cannot be switched off), no notification uses email,
 * reminder timing is fixed in code, and quiet hours were stored but never read. The old screens offered all of
 * them. A customer switched SMS off, was told "saved", and kept receiving SMS.
 *
 * What does work is push, on the notifications that are not critical. So that is the one control, and the rest
 * is stated as fact rather than offered as a choice.
 */

const getChannelMask = vi.fn()
const saveChannelMask = vi.fn()

vi.mock('@/core/api/services/notification-preferences.service', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/core/api/services/notification-preferences.service')>()
  return {
    ...actual,
    notificationPreferencesService: {
      getChannelMask: (...a: unknown[]) => getChannelMask(...a),
      saveChannelMask: (...a: unknown[]) => saveChannelMask(...a),
    },
  }
})

import { Channel } from '@/core/api/services/notification-preferences.service'
import NotificationPreferencesPanel from '../NotificationPreferencesPanel.vue'

const everything = Channel.Email | Channel.SMS | Channel.PushNotification | Channel.InApp

async function render(audience: 'customer' | 'provider' = 'customer') {
  const wrapper = mount(NotificationPreferencesPanel, { props: { audience } })
  await flushPromises()
  return wrapper
}

const pushToggle = (w: Awaited<ReturnType<typeof render>>) =>
  w.find<HTMLInputElement>('[data-test="push-toggle"]')

describe('NotificationPreferencesPanel', () => {
  beforeEach(() => {
    getChannelMask.mockReset()
    saveChannelMask.mockReset()
  })

  it('shows push as it is actually set on the server', async () => {
    getChannelMask.mockResolvedValue(Channel.SMS | Channel.InApp)

    const wrapper = await render()

    expect(pushToggle(wrapper).element.checked).toBe(false)
  })

  it('offers no control that cannot change anything', async () => {
    getChannelMask.mockResolvedValue(everything)

    const wrapper = await render()

    expect(wrapper.find('[data-test="sms-toggle"]').exists()).toBe(false)
    expect(wrapper.find('[data-test="email-toggle"]').exists()).toBe(false)
    expect(wrapper.find('select').exists()).toBe(false)
    expect(wrapper.findAll('input[type="checkbox"]')).toHaveLength(1)
  })

  it('says plainly what always arrives by SMS', async () => {
    getChannelMask.mockResolvedValue(everything)

    const wrapper = await render('customer')

    const always = wrapper.find('[data-test="always-sent"]').text()
    expect(always).toContain('پیامک')
    expect(always).toContain('۲ ساعت')
  })

  it("tells a salon about the salon's critical notices, not a customer's", async () => {
    getChannelMask.mockResolvedValue(everything)

    const wrapper = await render('provider')

    expect(wrapper.find('[data-test="always-sent"]').text()).toContain('درخواست نوبت')
  })

  it('switching push off saves a mask without push and with everything else intact', async () => {
    getChannelMask.mockResolvedValue(everything)
    saveChannelMask.mockImplementation(async (mask: number) => mask)
    const wrapper = await render()

    await pushToggle(wrapper).setValue(false)
    await flushPromises()

    const saved = saveChannelMask.mock.calls[0][0] as number
    expect(saved & Channel.PushNotification).toBe(0)
    expect(saved & Channel.InApp).toBe(Channel.InApp)
    expect(saved & Channel.SMS).toBe(Channel.SMS)
  })

  it('a failed save puts the toggle back and says it did not save', async () => {
    getChannelMask.mockResolvedValue(everything)
    saveChannelMask.mockRejectedValue(new Error('offline'))
    const wrapper = await render()

    await pushToggle(wrapper).setValue(false)
    await flushPromises()

    expect(pushToggle(wrapper).element.checked).toBe(true)
    expect(wrapper.find('[data-test="save-error"]').exists()).toBe(true)
  })

  it('a failed load shows no toggle at all rather than a guessed setting', async () => {
    getChannelMask.mockRejectedValue(new Error('offline'))

    const wrapper = await render()

    expect(pushToggle(wrapper).exists()).toBe(false)
    expect(wrapper.find('[data-test="load-error"]').exists()).toBe(true)
  })
})
