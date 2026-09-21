import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * The client for a person's notification inbox.
 *
 * The backend has served this for a while — list, unread count, mark read, mark all read, all scoped to the
 * caller — and nothing called it. The endpoint constants that did exist were wrong twice over: the paths
 * (`/notifications`, `/notifications/unread`) were not the routes, and they lacked the `v1/` every other
 * constant carries. They survived because no code ever used them, which is why these tests pin the constants
 * AND exercise them.
 */

const get = vi.fn()
const post = vi.fn()

vi.mock('@/core/api/client/http-client', () => ({
  serviceCategoryClient: {
    get: (...args: unknown[]) => get(...args),
    post: (...args: unknown[]) => post(...args),
  },
}))

import { apiEndpoints } from '@/core/api/config/api-config'
import { notificationInboxService } from '../notification-inbox.service'

const row = (overrides: Record<string, unknown> = {}) => ({
  id: 'n1',
  eventCode: 'BookingConfirmed',
  subject: 'نوبت شما تأیید شد',
  body: 'متن',
  createdAt: '2026-09-21T08:00:00Z',
  readAt: null,
  destinationKind: 'Booking',
  destinationId: 'b1',
  isActionable: true,
  ...overrides,
})

describe('the declared notification routes', () => {
  it('are the routes the backend actually serves', () => {
    expect(apiEndpoints.notifications.inbox).toBe('v1/Notifications/inbox')
    expect(apiEndpoints.notifications.unreadCount).toBe('v1/Notifications/unread-count')
    expect(apiEndpoints.notifications.markRead('abc')).toBe('v1/Notifications/abc/read')
    expect(apiEndpoints.notifications.markAllRead).toBe('v1/Notifications/read-all')
  })
})

describe('notificationInboxService', () => {
  beforeEach(() => {
    get.mockReset()
    post.mockReset()
  })

  it('asks for one page, newest first, past the GET cache', async () => {
    get.mockResolvedValue({
      success: true,
      data: { items: [row()], totalCount: 1, unreadCount: 1, pageNumber: 1, pageSize: 20 },
    })

    await notificationInboxService.list(1, 20)

    expect(get).toHaveBeenCalledWith(
      apiEndpoints.notifications.inbox,
      expect.objectContaining({ cache: false, params: { pageNumber: 1, pageSize: 20 } }),
    )
  })

  it('returns the rows and the counts the page carries', async () => {
    get.mockResolvedValue({
      success: true,
      data: { items: [row(), row({ id: 'n2', readAt: '2026-09-21T09:00:00Z' })], totalCount: 7, unreadCount: 3 },
    })

    const page = await notificationInboxService.list()

    expect(page.items.map((i) => i.id)).toEqual(['n1', 'n2'])
    expect(page.totalCount).toBe(7)
    expect(page.unreadCount).toBe(3)
  })

  it('accepts the page whether or not the envelope was already unwrapped', async () => {
    get.mockResolvedValue({ success: true, data: { data: { items: [row()], totalCount: 1, unreadCount: 1 } } })

    expect((await notificationInboxService.list()).items).toHaveLength(1)
  })

  it('treats a missing list as empty rather than failing', async () => {
    get.mockResolvedValue({ success: true, data: {} })

    const page = await notificationInboxService.list()

    expect(page.items).toEqual([])
    expect(page.unreadCount).toBe(0)
  })

  it('reads the unread count past the GET cache', async () => {
    get.mockResolvedValue({ success: true, data: { unreadCount: 4 } })

    expect(await notificationInboxService.unreadCount()).toBe(4)
    expect(get).toHaveBeenCalledWith(
      apiEndpoints.notifications.unreadCount,
      expect.objectContaining({ cache: false }),
    )
  })

  it('marks one notification read', async () => {
    post.mockResolvedValue({ success: true })

    await notificationInboxService.markRead('n1')

    expect(post).toHaveBeenCalledWith(apiEndpoints.notifications.markRead('n1'))
  })

  it('marks everything read', async () => {
    post.mockResolvedValue({ success: true, data: { markedRead: 3 } })

    await notificationInboxService.markAllRead()

    expect(post).toHaveBeenCalledWith(apiEndpoints.notifications.markAllRead)
  })

  it('counts a 204 with no body as success', async () => {
    // Mark-read answers 204 No Content, and axios hands back an empty string for that body — so `success` is
    // undefined on a request that worked. Treating "not explicitly successful" as failure would report every
    // successful mark-read as an error. HTTP failures are rejected by axios itself; only an explicit
    // `success: false` in a body is a failure here.
    post.mockResolvedValue('')

    await expect(notificationInboxService.markRead('n1')).resolves.toBeUndefined()
  })

  it('reports a failed mark-read as a failure', async () => {
    post.mockResolvedValue({ success: false, message: 'not found' })

    await expect(notificationInboxService.markRead('n1')).rejects.toThrow()
  })
})
