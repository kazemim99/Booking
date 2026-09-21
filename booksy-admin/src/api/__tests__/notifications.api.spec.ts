import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * The admin header's unread count. It was a hardcoded `ref(5)` — a number shown to every administrator that no
 * part of the system ever produced.
 */

const get = vi.fn()
vi.mock('../../utils/axios', () => ({ default: { get: (...a: unknown[]) => get(...a) } }))

import { notificationsApi } from '../notifications.api'

describe('notificationsApi.unreadCount', () => {
  beforeEach(() => {
    get.mockReset()
  })

  it('reads the caller-scoped unread count', async () => {
    get.mockResolvedValue({ data: { unreadCount: 2 } })

    expect(await notificationsApi.unreadCount()).toBe(2)
    expect(get).toHaveBeenCalledWith('/Notifications/unread-count')
  })

  it('reads a missing count as zero rather than failing', async () => {
    get.mockResolvedValue({ data: {} })

    expect(await notificationsApi.unreadCount()).toBe(0)
  })
})
