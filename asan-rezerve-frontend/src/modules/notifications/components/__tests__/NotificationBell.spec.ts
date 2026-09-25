import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'

/**
 * The header badge. It replaces a bell that showed a hardcoded "3" whatever the truth was — so the property
 * worth pinning is simply that the number shown is the number the server reports, and that zero shows nothing.
 */

const unreadCount = vi.fn()

vi.mock('@/core/api/services/notification-inbox.service', () => ({
  notificationInboxService: {
    list: vi.fn(),
    unreadCount: (...a: unknown[]) => unreadCount(...a),
    markRead: vi.fn(),
    markAllRead: vi.fn(),
  },
}))

import NotificationBell from '../NotificationBell.vue'

async function render() {
  const wrapper = mount(NotificationBell, { global: { stubs: { RouterLink: RouterLinkStub } } })
  await flushPromises()
  return wrapper
}

describe('NotificationBell', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    unreadCount.mockReset()
  })

  it('shows the number the server reports', async () => {
    unreadCount.mockResolvedValue(4)

    const wrapper = await render()

    expect(wrapper.find('[data-test="unread-badge"]').text()).toBe((4).toLocaleString('fa-IR'))
  })

  it('shows nothing when nothing is unread', async () => {
    unreadCount.mockResolvedValue(0)

    const wrapper = await render()

    expect(wrapper.find('[data-test="unread-badge"]').exists()).toBe(false)
  })

  it('shows nothing, rather than a guess, when the count cannot be read', async () => {
    unreadCount.mockRejectedValue(new Error('offline'))

    const wrapper = await render()

    expect(wrapper.find('[data-test="unread-badge"]').exists()).toBe(false)
  })

  it('links to the inbox', async () => {
    unreadCount.mockResolvedValue(1)

    const wrapper = await render()

    expect(wrapper.findComponent(RouterLinkStub).props('to')).toEqual({ name: 'Notifications' })
  })
})
