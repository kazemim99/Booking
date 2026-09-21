import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'

/**
 * The notification list a person actually looks at.
 *
 * What it must never do: tell someone they have no notifications because a request failed. "You have none" is
 * a claim about their account; a network error is not evidence for it. And a row whose target is gone must stay
 * readable without taking them anywhere.
 */

const list = vi.fn()
const markRead = vi.fn()
const markAllRead = vi.fn()

vi.mock('@/core/api/services/notification-inbox.service', () => ({
  notificationInboxService: {
    list: (...a: unknown[]) => list(...a),
    unreadCount: vi.fn().mockResolvedValue(0),
    markRead: (...a: unknown[]) => markRead(...a),
    markAllRead: (...a: unknown[]) => markAllRead(...a),
  },
}))

const push = vi.fn()
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }))

import NotificationList from '../NotificationList.vue'

const item = (id: string, overrides: Record<string, unknown> = {}) => ({
  id,
  eventCode: 'BookingConfirmed',
  subject: `موضوع ${id}`,
  body: `متن ${id}`,
  createdAt: '2026-09-21T08:00:00Z',
  readAt: null,
  destinationKind: 'Booking',
  destinationId: `booking-${id}`,
  isActionable: true,
  ...overrides,
})

function givenInbox(items: ReturnType<typeof item>[]) {
  list.mockResolvedValue({
    items,
    totalCount: items.length,
    unreadCount: items.filter((i) => !i.readAt).length,
  })
}

async function render(audience: 'customer' | 'provider' = 'customer') {
  const wrapper = mount(NotificationList, { props: { audience } })
  await flushPromises()
  return wrapper
}

describe('NotificationList', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    list.mockReset()
    markRead.mockReset().mockResolvedValue(undefined)
    markAllRead.mockReset().mockResolvedValue(undefined)
    push.mockReset()
  })

  it('shows what was sent, newest first as the server ordered it', async () => {
    givenInbox([item('a'), item('b')])

    const wrapper = await render()

    const rows = wrapper.findAll('[data-test="notification-row"]')
    expect(rows).toHaveLength(2)
    expect(rows[0].text()).toContain('موضوع a')
  })

  it('says there is nothing when there really is nothing', async () => {
    givenInbox([])

    const wrapper = await render()

    expect(wrapper.find('[data-test="inbox-empty"]').exists()).toBe(true)
  })

  it('does not claim the inbox is empty when loading failed', async () => {
    list.mockRejectedValue(new Error('offline'))

    const wrapper = await render()

    expect(wrapper.find('[data-test="inbox-empty"]').exists()).toBe(false)
    expect(wrapper.find('[data-test="inbox-error"]').exists()).toBe(true)
  })

  it('opening an unread notification marks it read and goes to its booking', async () => {
    givenInbox([item('a')])
    const wrapper = await render()

    await wrapper.find('[data-test="notification-row"]').trigger('click')
    await flushPromises()

    expect(markRead).toHaveBeenCalledWith('a')
    expect(push).toHaveBeenCalledWith({ name: 'BookingDetail', params: { id: 'booking-a' } })
  })

  it('a notification whose target is gone is still read, and goes nowhere', async () => {
    givenInbox([item('a', { isActionable: false })])
    const wrapper = await render()

    await wrapper.find('[data-test="notification-row"]').trigger('click')
    await flushPromises()

    expect(markRead).toHaveBeenCalledWith('a')
    expect(push).not.toHaveBeenCalled()
  })

  it('marks everything read', async () => {
    givenInbox([item('a'), item('b')])
    const wrapper = await render()

    await wrapper.find('[data-test="mark-all-read"]').trigger('click')
    await flushPromises()

    expect(markAllRead).toHaveBeenCalledTimes(1)
    expect(wrapper.findAll('[data-test="unread-dot"]')).toHaveLength(0)
  })

  it('offers no "mark all read" when nothing is unread', async () => {
    givenInbox([item('a', { readAt: '2026-09-21T09:00:00Z' })])

    const wrapper = await render()

    expect(wrapper.find('[data-test="mark-all-read"]').exists()).toBe(false)
  })

  it('marks unread rows so they can be told apart from read ones', async () => {
    givenInbox([item('a'), item('b', { readAt: '2026-09-21T09:00:00Z' })])

    const wrapper = await render()

    expect(wrapper.findAll('[data-test="unread-dot"]')).toHaveLength(1)
  })
})
