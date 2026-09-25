import { describe, it, expect } from 'vitest'
import { withSavedReview } from '../bookingReviews'

/** reviews-and-reschedule-round2: one review per salon — saving it from one visit changes every visit to that salon. */

const booking = (bookingId: string, providerId: string, extra: Record<string, unknown> = {}) => ({
  bookingId,
  providerId,
  canReview: true,
  reviewId: null as string | null,
  reviewBookingId: null as string | null,
  reviewStatusLabel: null as string | null,
  canEditReview: false,
  reviewFromOtherVisit: false,
  ...extra,
})

describe('withSavedReview', () => {
  it('a new review closes «ثبت نظر» on every visit to that salon, and opens «ویرایش نظر»', () => {
    const visits = [booking('b1', 'p1'), booking('b2', 'p1'), booking('b3', 'p2')]

    const next = withSavedReview(visits, visits[0], 'r9')

    expect(next[0]).toMatchObject({
      canReview: false,
      reviewId: 'r9',
      reviewBookingId: 'b1',
      reviewStatusLabel: 'در انتظار تأیید',
      canEditReview: true,
      reviewFromOtherVisit: false,
    })
    expect(next[1]).toMatchObject({ canReview: false, reviewId: 'r9', canEditReview: true, reviewFromOtherVisit: true })
    expect(next[2]).toEqual(visits[2])
  })

  it('an edit made from another visit keeps the review on the visit it was written on', () => {
    const visits = [
      booking('b1', 'p1', { canReview: false, reviewId: 'r9', reviewBookingId: 'b1' }),
      booking('b2', 'p1', { canReview: false, reviewId: 'r9', reviewBookingId: 'b1', reviewFromOtherVisit: true }),
    ]

    const next = withSavedReview(visits, visits[1], 'r9')

    expect(next[0].reviewFromOtherVisit).toBe(false)
    expect(next[1].reviewFromOtherVisit).toBe(true)
    expect(next[1].reviewBookingId).toBe('b1')
  })
})
