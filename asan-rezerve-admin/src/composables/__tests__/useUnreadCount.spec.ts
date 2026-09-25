import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises } from '@vue/test-utils'

/**
 * What the admin header shows. The rule: the number is the server's, and when the server cannot be asked the
 * badge shows NOTHING — a guessed number is the defect being removed, not a fallback to keep.
 *
 * Worth knowing when reading this: no notification in the catalogue is addressed to an administrator, so in
 * practice this reads zero and the badge stays hidden. That is the truthful state; the old "5" was not.
 */

const unreadCount = vi.fn()
vi.mock('../../api/notifications.api', () => ({
  notificationsApi: { unreadCount: (...a: unknown[]) => unreadCount(...a) },
}))

import { useUnreadCount } from '../useUnreadCount'

describe('useUnreadCount', () => {
  // Braces matter: an arrow that RETURNS the mock hands vitest a function, which it treats as a cleanup
  // callback and calls — invoking the mock and awaiting whatever the test configured it to return.
  beforeEach(() => {
    unreadCount.mockReset()
  })

  it('shows the count the server reports', async () => {
    unreadCount.mockResolvedValue(3)

    const { count } = useUnreadCount()
    await flushPromises()

    expect(count.value).toBe(3)
  })

  it('shows nothing when the server cannot be asked', async () => {
    unreadCount.mockRejectedValue(new Error('offline'))

    const { count } = useUnreadCount()
    await flushPromises()

    expect(count.value).toBe(0)
  })

  it('starts at zero, never at a placeholder', () => {
    unreadCount.mockReturnValue(new Promise(() => undefined))

    expect(useUnreadCount().count.value).toBe(0)
  })
})
