import { describe, it, expect } from 'vitest'
import { destinationRoute } from '../destination'

/**
 * Where tapping a notification goes.
 *
 * The backend recomputes each row's destination when the inbox is read, and marks a row non-actionable when its
 * target is gone or no longer the reader's (a cancelled-and-purged booking, a booking moved to another salon).
 * The row stays in the list — its text is history — but tapping it must not lead anywhere. These tests hold
 * that, and hold that an unknown destination goes NOWHERE rather than somewhere plausible: a wrong screen is
 * worse than no screen.
 */

const item = (overrides: Record<string, unknown> = {}) => ({
  destinationKind: 'Booking',
  destinationId: 'b1',
  isActionable: true,
  ...overrides,
})

describe('destinationRoute', () => {
  it('opens the booking on the customer booking page for a customer', () => {
    expect(destinationRoute(item(), 'customer')).toEqual({ name: 'BookingDetail', params: { id: 'b1' } })
  })

  it('opens the booking on the shared booking page for a provider', () => {
    expect(destinationRoute(item(), 'provider')).toEqual({ name: 'BookingDetails', params: { id: 'b1' } })
  })

  it('goes nowhere when the target is gone or no longer theirs', () => {
    expect(destinationRoute(item({ isActionable: false }), 'customer')).toBeNull()
  })

  it('goes nowhere when there is no target id', () => {
    expect(destinationRoute(item({ destinationId: null }), 'customer')).toBeNull()
  })

  it('goes nowhere for a destination this client has no screen for, rather than guessing', () => {
    expect(destinationRoute(item({ destinationKind: 'Payout' }), 'provider')).toBeNull()
    expect(destinationRoute(item({ destinationKind: 'None' }), 'customer')).toBeNull()
  })

  it('reads the kind case-insensitively, since the response transform may have re-cased it', () => {
    expect(destinationRoute(item({ destinationKind: 'booking' }), 'customer')).not.toBeNull()
  })
})
