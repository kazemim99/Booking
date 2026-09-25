import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

/**
 * The inbox store: what the list shows, what the badge says, and that the two never disagree.
 *
 * They disagreed once already, on the server — the inbox returned undelivered rows while the unread count did
 * not, and nobody could tell which was lying until a test asserted they matched. The same failure is easy to
 * rebuild on the client: decrement the badge for a notification that was already read, or forget to restore
 * it when a request fails, and the number drifts away from the list for the rest of the session.
 */

const list = vi.fn()
const unreadCount = vi.fn()
const markRead = vi.fn()
const markAllRead = vi.fn()

vi.mock('@/core/api/services/notification-inbox.service', () => ({
  notificationInboxService: {
    list: (...a: unknown[]) => list(...a),
    unreadCount: (...a: unknown[]) => unreadCount(...a),
    markRead: (...a: unknown[]) => markRead(...a),
    markAllRead: (...a: unknown[]) => markAllRead(...a),
  },
}))

import { useInboxStore } from '../inbox.store'

const item = (id: string, read = false) => ({
  id,
  eventCode: 'BookingConfirmed',
  subject: 'موضوع',
  body: 'متن',
  createdAt: '2026-09-21T08:00:00Z',
  readAt: read ? '2026-09-21T09:00:00Z' : null,
  destinationKind: 'Booking',
  destinationId: 'b1',
  isActionable: true,
})

function givenInbox(items: ReturnType<typeof item>[]) {
  list.mockResolvedValue({
    items,
    totalCount: items.length,
    unreadCount: items.filter((i) => !i.readAt).length,
  })
}

const unreadInList = (store: ReturnType<typeof useInboxStore>) =>
  store.items.filter((i) => !i.readAt).length

describe('inbox store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    list.mockReset()
    unreadCount.mockReset()
    markRead.mockReset().mockResolvedValue(undefined)
    markAllRead.mockReset().mockResolvedValue(undefined)
  })

  it('loads the page and the count together', async () => {
    givenInbox([item('a'), item('b', true)])
    const store = useInboxStore()

    await store.load()

    expect(store.items.map((i) => i.id)).toEqual(['a', 'b'])
    expect(store.unreadCount).toBe(1)
  })

  it('says it is empty only once it has actually loaded and found nothing', async () => {
    const store = useInboxStore()
    expect(store.isEmpty).toBe(false) // not loaded yet — that is "loading", not "empty"

    givenInbox([])
    await store.load()

    expect(store.isEmpty).toBe(true)
  })

  it('reading an unread notification takes exactly one off the count', async () => {
    givenInbox([item('a'), item('b')])
    const store = useInboxStore()
    await store.load()

    await store.markRead('a')

    expect(store.unreadCount).toBe(1)
    expect(store.items.find((i) => i.id === 'a')!.readAt).not.toBeNull()
    expect(markRead).toHaveBeenCalledWith('a')
  })

  it('reading an already-read notification changes nothing and asks the server nothing', async () => {
    // The drift bug: decrement for a row that was already read and the badge goes below the truth.
    givenInbox([item('a', true), item('b')])
    const store = useInboxStore()
    await store.load()

    await store.markRead('a')

    expect(store.unreadCount).toBe(1)
    expect(markRead).not.toHaveBeenCalled()
  })

  it('the badge and the list agree after reading', async () => {
    givenInbox([item('a'), item('b'), item('c', true)])
    const store = useInboxStore()
    await store.load()

    await store.markRead('b')

    expect(store.unreadCount).toBe(unreadInList(store))
  })

  it('a failed read is put back exactly as it was', async () => {
    givenInbox([item('a')])
    markRead.mockRejectedValue(new Error('offline'))
    const store = useInboxStore()
    await store.load()

    await expect(store.markRead('a')).rejects.toThrow()

    expect(store.unreadCount).toBe(1)
    expect(store.items[0].readAt).toBeNull()
  })

  it('marking everything read empties the count and reads every row', async () => {
    givenInbox([item('a'), item('b'), item('c', true)])
    const store = useInboxStore()
    await store.load()

    await store.markAllRead()

    expect(store.unreadCount).toBe(0)
    expect(unreadInList(store)).toBe(0)
  })

  it('marking everything read twice is harmless', async () => {
    givenInbox([item('a')])
    const store = useInboxStore()
    await store.load()

    await store.markAllRead()
    await store.markAllRead()

    expect(store.unreadCount).toBe(0)
    expect(markAllRead).toHaveBeenCalledTimes(1)
  })

  it('a failed mark-all is put back exactly as it was', async () => {
    givenInbox([item('a'), item('b', true)])
    markAllRead.mockRejectedValue(new Error('offline'))
    const store = useInboxStore()
    await store.load()

    await expect(store.markAllRead()).rejects.toThrow()

    expect(store.unreadCount).toBe(1)
    expect(store.items.find((i) => i.id === 'a')!.readAt).toBeNull()
    expect(store.items.find((i) => i.id === 'b')!.readAt).not.toBeNull()
  })

  it('refreshing the badge alone does not need the list', async () => {
    unreadCount.mockResolvedValue(5)
    const store = useInboxStore()

    await store.refreshCount()

    expect(store.unreadCount).toBe(5)
    expect(list).not.toHaveBeenCalled()
  })

  it('a failed load surfaces an error instead of an empty inbox', async () => {
    // "You have no notifications" is a claim; a network failure must not make it.
    list.mockRejectedValue(new Error('offline'))
    const store = useInboxStore()

    await store.load()

    expect(store.error).not.toBeNull()
    expect(store.isEmpty).toBe(false)
  })
})
