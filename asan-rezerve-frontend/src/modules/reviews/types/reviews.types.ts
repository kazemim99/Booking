/**
 * Review types as the ServiceCatalog API serves them (provider-reviews-and-ratings).
 *
 * The host omits null properties, so anything optional may simply be absent on the wire; the API client normalises
 * absent to `null` so components never have to tell the two apart.
 */

/**
 * The four aspects. The form requires all four (reviews-and-reschedule-round2, D1); a null on a stored review still
 * means "not rated" — never zero stars — because reviews written before D1 may lack them.
 */
export const DIMENSIONS = ['cleanliness', 'skill', 'punctuality', 'conduct'] as const
export type Dimension = (typeof DIMENSIONS)[number]

export const DIMENSION_LABELS: Record<Dimension, string> = {
  cleanliness: 'نظافت و بهداشت',
  skill: 'مهارت و کیفیت کار',
  punctuality: 'وقت‌شناسی',
  conduct: 'برخورد و رفتار',
}

export type DimensionRatings = Record<Dimension, number | null>

/**
 * What a customer writes: the four aspects, and the overall derived from them (D1) — sent too, so an older server that
 * still requires `rating` accepts it. `showName: false` signs the public review «مشتری» instead of the author's name.
 */
export interface ReviewInput {
  rating: number
  comment?: string
  dimensions: Partial<Record<Dimension, number>>
  showName: boolean
}

/**
 * The overall a set of aspects stands for: their average to the nearest half star (3.25 → 3.5, 3.75 → 4).
 * Null until every aspect is rated — a partial average would be a verdict the customer never gave.
 */
export function overallFromAspects(aspects: Partial<Record<Dimension, number | null>>): number | null {
  const values = DIMENSIONS.map((d) => aspects[d])
  if (values.some((v) => typeof v !== 'number' || v <= 0)) return null
  const average = (values as number[]).reduce((sum, v) => sum + v, 0) / values.length
  return Math.round(average * 2) / 2
}

export type MyVote = 'helpful' | 'notHelpful' | null

/** One review in a provider's public listing. Published reviews only; a reply appears only once approved. */
export interface ProviderReview {
  reviewId: string
  /** How the listing signs it: the author's full name, or «مشتری» when they chose not to show it. */
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
  /** Whether the public review carries the author's name; true when the server did not say (the default). */
  showName: boolean
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
