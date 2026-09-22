import apiClient from '../utils/axios'

/**
 * Review moderation (provider-reviews-and-ratings). Every endpoint is admin-only on the server (the `AdminOnly`
 * policy, which accepts Admin, Administrator and SysAdmin). Nothing a customer writes is public until it passes here.
 */

export type ModerationFilter = 'pending' | 'hidden' | 'reported'

export interface ModerationReport {
  reason: string
  reportedByUserId: string
  createdAt: string
}

export interface ModerationItem {
  reviewId: string
  providerId: string
  customerId: string
  bookingId?: string
  rating: number
  cleanlinessRating?: number | null
  skillRating?: number | null
  punctualityRating?: number | null
  conductRating?: number | null
  comment?: string | null
  moderationStatus: 'Pending' | 'Published' | 'Rejected' | 'Hidden'
  moderationReason?: string | null
  /** The review itself awaits a decision. */
  reviewPending: boolean
  /** It had been public before — so this is an EDIT awaiting re-approval, not a first submission. */
  wasPublishedBefore: boolean
  providerResponse?: string | null
  replyModerationStatus?: string | null
  /** The salon's reply awaits a decision. */
  replyPending: boolean
  reportCount: number
  reports: ModerationReport[]
  createdAt: string
  editedAt?: string | null
}

export interface ModerationPage {
  items: ModerationItem[]
  totalCount: number
}

function requireReason(reason: string): string {
  const trimmed = reason.trim()
  if (!trimmed) throw new Error('A reason is required')
  return trimmed
}

const decide = (reviewId: string, path: string, body: object = {}) =>
  apiClient.post(`/admin/reviews/${reviewId}/${path}`, body)

export const reviewsApi = {
  async queue(filter: ModerationFilter, pageNumber = 1, pageSize = 20): Promise<ModerationPage> {
    const response = await apiClient.get<ModerationPage>('/admin/reviews/queue', {
      params: { filter, pageNumber, pageSize },
    })
    const data = (response.data ?? {}) as Partial<ModerationPage>
    return {
      items: (data.items ?? []).map((i) => ({ ...i, reports: i.reports ?? [], reportCount: i.reportCount ?? 0 })),
      totalCount: data.totalCount ?? 0,
    }
  },

  async approve(reviewId: string): Promise<void> {
    await decide(reviewId, 'approve')
  },

  /** Permanent: a rejected review is never published. */
  async reject(reviewId: string, reason: string): Promise<void> {
    await decide(reviewId, 'reject', { reason: requireReason(reason) })
  },

  /** Takes a published review down. Reversible with restore. */
  async hide(reviewId: string, reason: string): Promise<void> {
    await decide(reviewId, 'hide', { reason: requireReason(reason) })
  },

  async restore(reviewId: string): Promise<void> {
    await decide(reviewId, 'restore')
  },

  async approveReply(reviewId: string): Promise<void> {
    await decide(reviewId, 'reply/approve')
  },

  async rejectReply(reviewId: string, reason: string): Promise<void> {
    await decide(reviewId, 'reply/reject', { reason: requireReason(reason) })
  },
}
