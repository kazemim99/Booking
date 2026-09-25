import type { AxiosRequestConfig } from 'axios'
import { serviceCategoryClient } from '@/core/api/client/http-client'
import { apiEndpoints } from '@/core/api/config/api-config'

/**
 * A person's notification inbox — what the backend has actually sent them, newest first.
 *
 * Not to be confused with `core/stores/modules/notification.store.ts`, which is the toast store for transient
 * UI messages ("saved", "failed") and has nothing to do with the server. This is the other thing called a
 * notification: the durable record of what the product told someone.
 */

/** One row, as the backend renders it. `eventCode` is what to pick an icon from — never parse the copy. */
export interface InboxItem {
  id: string
  eventCode: string | null
  subject: string
  body: string
  createdAt: string
  readAt: string | null
  destinationKind: string
  destinationId: string | null
  /** False when the target is gone or no longer the reader's; the row still shows, but must not navigate. */
  isActionable: boolean
}

export interface InboxPage {
  items: InboxItem[]
  totalCount: number
  unreadCount: number
}

/**
 * `cache` is read by the client's request-cache interceptor but is not part of axios's config type. The inbox
 * must never be served from that five-minute cache: a notification that arrived a minute ago, or one the person
 * just read, would be missing or shown in its old state.
 */
const UNCACHED = { cache: false } as AxiosRequestConfig

/** The page arrives in a `{ data }` envelope that the client unwraps once — or already unwrapped. Accept both. */
function unwrap<T extends object>(response: unknown): Partial<T> {
  const outer = ((response ?? {}) as { data?: unknown }).data ?? {}
  const inner = outer as { data?: unknown }
  return ((inner.data && typeof inner.data === 'object' ? inner.data : outer) ?? {}) as Partial<T>
}

/**
 * Only an explicit `success: false` is a failure. Mark-read answers 204 No Content, for which axios returns an
 * empty string — `success` is undefined on a request that worked. HTTP errors are rejected by axios itself.
 */
function assertNotFailed(response: unknown, message: string): void {
  if ((response as { success?: boolean } | null)?.success === false) {
    throw new Error((response as { message?: string }).message || message)
  }
}

export const notificationInboxService = {
  async list(pageNumber = 1, pageSize = 20): Promise<InboxPage> {
    const response = await serviceCategoryClient.get<InboxPage>(apiEndpoints.notifications.inbox, {
      ...UNCACHED,
      params: { pageNumber, pageSize },
    })

    const page = unwrap<InboxPage>(response)
    return {
      items: page.items ?? [],
      totalCount: page.totalCount ?? 0,
      unreadCount: page.unreadCount ?? 0,
    }
  },

  async unreadCount(): Promise<number> {
    const response = await serviceCategoryClient.get<{ unreadCount: number }>(
      apiEndpoints.notifications.unreadCount,
      UNCACHED,
    )
    return unwrap<{ unreadCount: number }>(response).unreadCount ?? 0
  },

  async markRead(id: string): Promise<void> {
    const response = await serviceCategoryClient.post(apiEndpoints.notifications.markRead(id))
    assertNotFailed(response, 'علامت‌گذاری اعلان انجام نشد')
  },

  async markAllRead(): Promise<void> {
    const response = await serviceCategoryClient.post(apiEndpoints.notifications.markAllRead)
    assertNotFailed(response, 'علامت‌گذاری اعلان‌ها انجام نشد')
  },
}
