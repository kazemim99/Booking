# Proposal: booking-slot-integrity

## Why

Validation showed same-slot double-booking is normally prevented by optimistic concurrency on `ProviderAvailability.Version` (`ProviderAvailabilityConfiguration.cs:22`), but three real residual gaps remain:

1. **No-slots hole (reachable by design):** when the requested time has no `ProviderAvailability` rows, `MarkAvailabilityAsBookedAsync` logs a warning and **returns without any guard** (`CreateBookingCommandHandler.cs:255–265`); the booking is created anyway. Two concurrent bookings then both succeed — there is no slot to contend on and **no unique constraint** on `Bookings`.
2. **Ugly failure surface:** the losing writer gets a raw `DbUpdateConcurrencyException` from `SaveChanges` (only the already-Booked-on-read case is caught, `:281`) → likely HTTP 500 instead of a clean 409.
3. **No defense-in-depth:** all meaningful `Bookings` indexes/uniqueness are commented out (`BookingConfiguration.cs:339–365`); correctness rests entirely on availability rows existing and being versioned.

## What Changes

- **Fail closed** when no availability slot covers a requested time (reject instead of creating an unguarded booking), gated behind a flag until slot-generation coverage is confirmed.
- Add a **filtered unique index** on `Bookings` for the effective (staff, start-time) over active statuses as a database-level backstop.
- Map slot-contention (`DbUpdateConcurrencyException` / unique violation) to a clean **409 "slot just taken"** with a caller-friendly message.

## Capabilities

### New Capabilities
- `booking-slot-integrity`: a time slot for a given staff member SHALL be occupied by at most one active booking, guaranteed on all creation and reschedule paths (including when availability rows are absent), and contention SHALL surface as 409.

### Modified Capabilities
- `customer-booking-journey`: booking creation/reschedule now guarantee single-occupancy and return 409 on contention.

## Impact

- **Code**: `CreateBookingCommandHandler`/`RescheduleBookingCommandHandler` fail-closed on missing slots; catch concurrency/unique violations → `ConflictException` (409). Optional shared "occupy slot" domain service.
- **DB**: filtered unique index on `Bookings`; a data-audit + cleanup migration if existing duplicates exist.
- **API**: creating/rescheduling into a slot with no availability → 409/422 (flagged); concurrent loser → 409.
- **Flutter**: slot picker handles 409 by refreshing availability and prompting reselect (coordinates with C4).
- **Depends on**: confirming availability slots are reliably pre-generated for bookable times before enabling fail-closed (the audit's open "Cannot verify"); coordinate the unique index with C6's index restoration.
