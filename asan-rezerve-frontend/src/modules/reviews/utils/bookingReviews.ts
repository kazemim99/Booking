/**
 * The review side of a customer's booking list (reviews-and-reschedule-round2): one review per salon, so saving a
 * review from one visit's card changes every visit to that salon.
 */

interface ReviewableBooking {
  bookingId: string
  providerId: string
  canReview: boolean
  reviewId?: string | null
  reviewBookingId?: string | null
  reviewStatus?: 'Pending' | 'Published' | 'Rejected' | 'Hidden' | null
  reviewStatusLabel: string | null
  reviewEditable?: boolean
  canEditReview: boolean
  reviewFromOtherVisit: boolean
}

const PENDING_LABEL = 'در انتظار تأیید'

/**
 * A review was saved from `from`'s card — written or edited. Every visit to that salon now carries it, awaiting
 * approval, and editable (a fresh or just-edited review is inside its edit window); the visits other than the one it
 * was written on read «برای این سالن قبلاً نظر داده‌اید». The server's copy says the same on the next load.
 */
export function withSavedReview<T extends ReviewableBooking>(list: T[], from: ReviewableBooking, reviewId: string): T[] {
  const writtenOn = from.reviewBookingId ?? from.bookingId
  return list.map((b) =>
    b.providerId === from.providerId || b.bookingId === from.bookingId
      ? {
          ...b,
          canReview: false,
          reviewId,
          reviewBookingId: writtenOn,
          reviewStatus: 'Pending' as const,
          reviewStatusLabel: PENDING_LABEL,
          reviewEditable: true,
          canEditReview: true,
          reviewFromOtherVisit: b.bookingId !== writtenOn,
        }
      : b,
  )
}
