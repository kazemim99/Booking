import { describe, it, expect } from 'vitest'
import { mapToEnrichedBookingView } from '../booking-dto.mapper'
import type { CustomerBookingDto } from '../../types/booking-api.types'

const REASON = 'تغییر زمان تا 24 ساعت پیش از نوبت ممکن است؛ برای تغییر با سالن تماس بگیرید.'

const dto = (extra: Partial<CustomerBookingDto> = {}): CustomerBookingDto => ({
  bookingId: 'b1',
  customerId: 'c1',
  providerId: 'p1',
  serviceId: 's1',
  staffId: null,
  serviceName: 'اصلاح کامل',
  providerName: 'سالن نهال',
  staffName: null,
  startTime: '2099-01-05T10:00:00',
  endTime: '2099-01-05T10:30:00',
  durationMinutes: 30,
  status: 'Requested',
  totalPrice: 250000,
  currency: 'IRT',
  paymentStatus: 'Pending',
  requestedAt: '2098-12-01T10:00:00',
  confirmedAt: null,
  customerNotes: null,
  ...extra,
})

describe('mapToEnrichedBookingView — why a booking cannot be moved', () => {
  it('carries the server\'s reason through, and the booking stays reschedulable so the button has a place', () => {
    const view = mapToEnrichedBookingView(dto({ rescheduleBlockedReason: REASON }))

    expect(view.rescheduleBlockedReason).toBe(REASON)
    expect(view.canReschedule).toBe(true)
  })

  it('without the field there is no reason', () => {
    expect(mapToEnrichedBookingView(dto()).rescheduleBlockedReason ?? null).toBeNull()
  })
})

// openspec/changes/_inline/customer-reviews-and-nahal-seed: the web had no «ثبت نظر» at all.
describe('mapToEnrichedBookingView — where the review stands', () => {
  const past = { startTime: '2020-01-05T10:00:00', endTime: '2020-01-05T10:30:00' }

  it('a completed visit the server says can be reviewed offers it', () => {
    const view = mapToEnrichedBookingView(dto({ ...past, status: 'Completed', canReview: true }))
    expect(view.canReview).toBe(true)
    expect(view.reviewStatusLabel).toBeNull()
  })

  it('a reviewed visit says its state in Persian and is not offered again', () => {
    const view = mapToEnrichedBookingView(
      dto({ ...past, status: 'Completed', canReview: false, reviewId: 'r1', reviewStatus: 'Published' }),
    )
    expect(view.canReview).toBe(false)
    expect(view.reviewStatusLabel).toBe('منتشر شده')
  })

  it('a visit waiting for the salon carries the reason', () => {
    const reason = 'پس از اینکه سالن این نوبت را «انجام‌شده» ثبت کند، می‌توانید برایش نظر بنویسید.'
    const view = mapToEnrichedBookingView(dto({ ...past, status: 'Confirmed', canReview: false, reviewBlockedReason: reason }))
    expect(view.canReview).toBe(false)
    expect(view.reviewBlockedReason).toBe(reason)
  })

  it('an older server without the fields: a completed visit is offered', () => {
    expect(mapToEnrichedBookingView(dto({ ...past, status: 'Completed' })).canReview).toBe(true)
    expect(mapToEnrichedBookingView(dto({ ...past, status: 'Confirmed' })).canReview).toBe(false)
  })
})
