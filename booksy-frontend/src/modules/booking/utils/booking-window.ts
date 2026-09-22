/**
 * How far ahead a customer may book (QA walkthrough 2026-09-22: "nobody should be able to book more than a week
 * out; the calendar should not even offer those days"). The server refuses later dates — `BookingHorizonPolicy`
 * on the API — so the picker must not offer them.
 *
 * A salon may set a SHORTER window; a longer one is capped, which is what makes the rule true for salons whose
 * stored policy still says 90 days.
 */
export const PLATFORM_BOOKING_WINDOW_DAYS = 7

export function bookingWindowDays(providerWindowDays?: number | null): number {
  return providerWindowDays && providerWindowDays > 0
    ? Math.min(providerWindowDays, PLATFORM_BOOKING_WINDOW_DAYS)
    : PLATFORM_BOOKING_WINDOW_DAYS
}

/** The last bookable date as `YYYY-MM-DD`, for a date input's `max`. */
export function bookingWindowMaxDate(today: Date, providerWindowDays?: number | null): string {
  const last = new Date(today)
  last.setDate(last.getDate() + bookingWindowDays(providerWindowDays))
  return last.toISOString().split('T')[0]
}
