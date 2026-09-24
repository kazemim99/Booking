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
