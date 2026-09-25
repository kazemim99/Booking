import { describe, it, expect } from 'vitest'
import { bookingWindowDays, bookingWindowMaxDate, PLATFORM_BOOKING_WINDOW_DAYS } from '../booking-window'

describe('the public booking window', () => {
  it('is a week by default', () => {
    expect(PLATFORM_BOOKING_WINDOW_DAYS).toBe(7)
    expect(bookingWindowDays()).toBe(7)
    expect(bookingWindowDays(null)).toBe(7)
  })

  it('honours a shorter salon window and caps a longer one', () => {
    expect(bookingWindowDays(3)).toBe(3)
    // Every salon's stored policy still says 90; the platform rule has to hold anyway.
    expect(bookingWindowDays(90)).toBe(7)
  })

  it('gives the date input its last bookable day', () => {
    expect(bookingWindowMaxDate(new Date('2026-09-22T09:00:00Z'))).toBe('2026-09-29')
    expect(bookingWindowMaxDate(new Date('2026-09-22T09:00:00Z'), 3)).toBe('2026-09-25')
  })
})
