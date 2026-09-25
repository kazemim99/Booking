/**
 * Review types as the ServiceCatalog API serves them (provider-reviews-and-ratings).
 *
 * The host omits null properties, so anything optional may simply be absent on the wire; the API client normalises
 * absent to `null` so components never have to tell the two apart.
 */

/** The four optional dimensions. A null means "not rated" — never zero stars. */
export const DIMENSIONS = ['cleanliness', 'skill', 'punctuality', 'conduct'] as const
export type Dimension = (typeof DIMENSIONS)[number]

export const DIMENSION_LABELS: Record<Dimension, string> = {
  cleanliness: 'نظافت و بهداشت',
  skill: 'مهارت و کیفیت کار',
  punctuality: 'وقت‌شناسی',
  conduct: 'برخورد و رفتار',
}

export type DimensionRatings = Record<Dimension, number | null>

/** What a customer writes: the overall is required; each dimension is optional. */
export interface ReviewInput {
  rating: number
  comment?: string
  dimensions: Partial<Record<Dimension, number>>
}

export type MyVote = 'helpful' | 'notHelpful' | null

/** One review in a provider's public listing. Published reviews only; a reply appears only once approved. */
export interface ProviderReview {
  reviewId: string
  /** How the listing signs it: «مریم ر.» — first name and surname initial — or «مشتری». */
  customerName: string
  rating: number
  comment: string | null
  isVerified: boolean
  providerResponse: string | null
  helpfulCount: number
  notHelpfulCount: number
  createdAt: string
  dimensions: DimensionRatings
  myVote: MyVote
}

/** A dimension's average over the published reviews that rated it. `average: null` when nobody has. */
export interface DimensionStatistic {
  average: number | null
  count: number
}

export interface ReviewStatistics {
  totalReviews: number
  averageRating: number
  ratingDistribution: Record<string, number>
  cleanliness: DimensionStatistic
  skill: DimensionStatistic
  punctuality: DimensionStatistic
  conduct: DimensionStatistic
}

export interface ProviderReviewListing {
  statistics: ReviewStatistics
  reviews: ProviderReview[]
  totalCount: number
}

export type ModerationStatus = 'Pending' | 'Published' | 'Rejected' | 'Hidden'

/** One of the signed-in customer's own reviews, in any moderation state. */
export interface MyReview {
  reviewId: string
  providerId: string
  providerName: string | null
  providerLogoUrl: string | null
  serviceName: string | null
  bookingId: string
  rating: number
  comment: string | null
  dimensions: DimensionRatings
  moderationStatus: ModerationStatus
  moderationReason: string | null
  providerResponse: string | null
  replyModerationStatus: ModerationStatus | null
  createdAt: string
  editedAt: string | null
  canEdit: boolean
}

export interface VoteResult {
  reviewId: string
  helpfulCount: number
  notHelpfulCount: number
  myVote: MyVote
}

export interface SubmittedReview {
  reviewId: string
  moderationStatus: ModerationStatus
}
