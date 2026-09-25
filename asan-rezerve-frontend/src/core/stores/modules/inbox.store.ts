import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import {
  notificationInboxService,
  type InboxItem,
} from '@/core/api/services/notification-inbox.service'

/**
 * The signed-in person's notification inbox and its unread badge.
 *
 * Named `inbox` rather than `notification` on purpose: `notification.store.ts` is the toast store for
 * transient UI messages and has nothing to do with this. Two stores both called "notification" is how the
 * first survey of this feature concluded half of it already existed.
 *
 * The invariant every action here protects: the badge equals the truth. Reads and mark-all are applied
 * optimistically — waiting on the server makes the badge lag the tap — and put back EXACTLY as they were if
 * the request fails, so a flaky connection cannot leave the number drifting away from the list.
 */
export const useInboxStore = defineStore('inbox', () => {
  const items = ref<InboxItem[]>([])
  const unreadCount = ref(0)
  const totalCount = ref(0)
  const loading = ref(false)
  const loaded = ref(false)
  const error = ref<string | null>(null)

  /** Empty only once a load has SUCCEEDED and found nothing — not while loading, and not after a failure. */
  const isEmpty = computed(() => loaded.value && !error.value && items.value.length === 0)

  async function load(pageNumber = 1, pageSize = 20): Promise<void> {
    loading.value = true
    error.value = null

    try {
      const page = await notificationInboxService.list(pageNumber, pageSize)
      items.value = page.items
      unreadCount.value = page.unreadCount
      totalCount.value = page.totalCount
      loaded.value = true
    } catch (err) {
      // Deliberately not "loaded": a failure must never read as "you have no notifications".
      error.value = err instanceof Error ? err.message : 'بارگذاری اعلان‌ها انجام نشد'
    } finally {
      loading.value = false
    }
  }

  /** Just the badge — for the header, which should not have to load a page to show a number. */
  async function refreshCount(): Promise<void> {
    unreadCount.value = await notificationInboxService.unreadCount()
  }

  async function markRead(id: string): Promise<void> {
    const target = items.value.find((i) => i.id === id)

    // Already read, or not on this page: nothing to change and nothing to tell the server. Decrementing here
    // for a row that was already read is exactly how the badge drifts below the truth.
    if (!target || target.readAt) return

    target.readAt = new Date().toISOString()
    unreadCount.value = Math.max(0, unreadCount.value - 1)

    try {
      await notificationInboxService.markRead(id)
    } catch (err) {
      target.readAt = null
      unreadCount.value += 1
      throw err
    }
  }

  async function markAllRead(): Promise<void> {
    const unread = items.value.filter((i) => !i.readAt)

    // Nothing unread anywhere: the second tap on "mark all read" is a no-op, not a second request.
    if (unread.length === 0 && unreadCount.value === 0) return

    const previousCount = unreadCount.value
    const now = new Date().toISOString()
    for (const i of unread) i.readAt = now
    unreadCount.value = 0

    try {
      await notificationInboxService.markAllRead()
    } catch (err) {
      for (const i of unread) i.readAt = null
      unreadCount.value = previousCount
      throw err
    }
  }

  return {
    items,
    unreadCount,
    totalCount,
    loading,
    loaded,
    error,
    isEmpty,
    load,
    refreshCount,
    markRead,
    markAllRead,
  }
})
