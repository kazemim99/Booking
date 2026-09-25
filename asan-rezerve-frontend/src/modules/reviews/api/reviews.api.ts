import type { AxiosRequestConfig } from 'axios'
import { serviceCategoryClient } from '@/core/api/client/http-client'
import { apiEndpoints } from '@/core/api/config/api-config'
import {
  DIMENSIONS,
  type DimensionRatings,
  type DimensionStatistic,
  type MyReview,
  type MyVote,
  type ProviderReview,
  type ProviderReviewListing,
  type ReviewInput,
  type SubmittedReview,
  type VoteResult,
} from '../types/reviews.types'

/**
 * The one client for the review endpoints. Every review surface in the web app goes through it — there used to be
 * three, and none reached the backend.
 */

/** Reviews change under a reader's hand (a vote, an approval); never serve them from the five-minute GET cache. */
const UNCACHED = { cache: false } as AxiosRequestConfig

type Json = Record<string, unknown>

/** Payloads arrive in a `{ data }` envelope the client unwraps once — or already unwrapped. Accept both. */
function unwrap(response: unknown): Json {
  const outer = ((response ?? {}) as { data?: unknown }).data ?? {}
  const inner = outer as { data?: unknown }
  return ((inner.data && typeof inner.data === 'object' ? inner.data : outer) ?? {}) as Json
}

const num = (v: unknown, fallback = 0) => (typeof v === 'number' ? v : fallback)
const numOrNull = (v: unknown) => (typeof v === 'number' ? v : null)
const strOrNull = (v: unknown) => (typeof v === 'string' ? v : null)
const vote = (v: unknown): MyVote => (v === 'helpful' || v === 'notHelpful' ? v : null)

function dimensions(item: Json): DimensionRatings {
  return {
    cleanliness: numOrNull(item.cleanlinessRating),
    skill: numOrNull(item.skillRating),
    punctuality: numOrNull(item.punctualityRating),
    conduct: numOrNull(item.conductRating),
  }
}

function statistic(v: unknown): DimensionStatistic {
  const s = (v ?? {}) as Json
  return { average: numOrNull(s.average), count: num(s.count) }
}

/** The request body: the overall, the comment if any, and only the dimensions that were rated. */
function body(input: ReviewInput): Json {
  const out: Json = { rating: input.rating }
  if (input.comment) out.comment = input.comment
  for (const d of DIMENSIONS) {
    const value = input.dimensions[d]
    if (typeof value === 'number') out[`${d}Rating`] = value
  }
  return out
}

function toProviderReview(item: Json): ProviderReview {
  return {
    reviewId: String(item.reviewId),
    customerName: strOrNull(item.customerName)?.trim() || 'مشتری',
    rating: num(item.rating),
    comment: strOrNull(item.comment),
    isVerified: item.isVerified === true,
    providerResponse: strOrNull(item.providerResponse),
    helpfulCount: num(item.helpfulCount),
    notHelpfulCount: num(item.notHelpfulCount),
    createdAt: String(item.createdAt ?? ''),
    dimensions: dimensions(item),
    myVote: vote(item.myVote),
  }
}

function toMyReview(item: Json): MyReview {
  return {
    reviewId: String(item.reviewId),
    providerId: String(item.providerId ?? ''),
    providerName: strOrNull(item.providerName),
    providerLogoUrl: strOrNull(item.providerLogoUrl),
    serviceName: strOrNull(item.serviceName),
    bookingId: String(item.bookingId ?? ''),
    rating: num(item.rating),
    comment: strOrNull(item.comment),
    dimensions: dimensions(item),
    moderationStatus: (item.moderationStatus as MyReview['moderationStatus']) ?? 'Pending',
    moderationReason: strOrNull(item.moderationReason),
    providerResponse: strOrNull(item.providerResponse),
    replyModerationStatus: (strOrNull(item.replyModerationStatus) as MyReview['replyModerationStatus']) ?? null,
    createdAt: String(item.createdAt ?? ''),
    editedAt: strOrNull(item.editedAt),
    canEdit: item.canEdit === true,
  }
}

export interface ListOptions {
  pageNumber?: number
  pageSize?: number
  sortBy?: 'date' | 'rating' | 'helpful'
  sortDescending?: boolean
}

export const reviewsApi = {
  async forProvider(providerId: string, options: ListOptions = {}): Promise<ProviderReviewListing> {
    const response = await serviceCategoryClient.get(apiEndpoints.reviews.forProvider(providerId), {
      ...UNCACHED,
      params: { pageNumber: 1, pageSize: 20, sortBy: 'date', sortDescending: true, ...options },
    })
    const data = unwrap(response)
    const stats = (data.statistics ?? {}) as Json
    const page = (data.reviews ?? {}) as Json
    const items = Array.isArray(page.items) ? (page.items as Json[]) : []

    return {
      statistics: {
        totalReviews: num(stats.totalReviews),
        averageRating: num(stats.averageRating),
        ratingDistribution: (stats.ratingDistribution ?? {}) as Record<string, number>,
        cleanliness: statistic(stats.cleanliness),
        skill: statistic(stats.skill),
        punctuality: statistic(stats.punctuality),
        conduct: statistic(stats.conduct),
      },
      reviews: items.map(toProviderReview),
      totalCount: num(page.totalCount, items.length),
    }
  },

  async submit(bookingId: string, input: ReviewInput): Promise<SubmittedReview> {
    const data = unwrap(await serviceCategoryClient.post(apiEndpoints.reviews.submit(bookingId), body(input)))
    return { reviewId: String(data.reviewId), moderationStatus: (data.moderationStatus as SubmittedReview['moderationStatus']) ?? 'Pending' }
  },

  async edit(reviewId: string, input: ReviewInput): Promise<SubmittedReview> {
    const data = unwrap(await serviceCategoryClient.put(apiEndpoints.reviews.edit(reviewId), body(input)))
    return { reviewId: String(data.reviewId ?? reviewId), moderationStatus: (data.moderationStatus as SubmittedReview['moderationStatus']) ?? 'Pending' }
  },

  async vote(reviewId: string, isHelpful: boolean): Promise<VoteResult> {
    const data = unwrap(await serviceCategoryClient.put(apiEndpoints.reviews.vote(reviewId), { isHelpful }))
    return {
      reviewId: String(data.reviewId ?? reviewId),
      helpfulCount: num(data.helpfulCount),
      notHelpfulCount: num(data.notHelpfulCount),
      myVote: vote(data.myVote),
    }
  },

  async report(reviewId: string, reason: string): Promise<void> {
    await serviceCategoryClient.post(apiEndpoints.reviews.report(reviewId), { reason })
  },

  async mine(pageNumber = 1, pageSize = 50): Promise<MyReview[]> {
    const data = unwrap(await serviceCategoryClient.get(apiEndpoints.reviews.mine, { ...UNCACHED, params: { pageNumber, pageSize } }))
    const items = Array.isArray(data.items) ? (data.items as Json[]) : []
    return items.map(toMyReview)
  },
}
