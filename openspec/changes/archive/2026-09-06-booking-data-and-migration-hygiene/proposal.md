# Proposal: booking-data-and-migration-hygiene

## Why

The audit found data-layer hygiene gaps that hurt performance and carry migration risk:

- **Booking query indexes commented out** (`BookingConfiguration.cs:339–365`): `CustomerId`, `ProviderId`, `StaffId`, `Status`, and the availability composite are disabled, so `my-bookings`, provider queries, and the conflict check do full scans under load.
- **Two migration directories** in ServiceCatalog (`.../Migrations/` and `.../Persistence/Migrations/`) with two "Init"-style migrations — a history/drift hazard.
- **Reschedule payment carry-over unverified**: `Reschedule` creates a new booking (`RescheduleBookingCommandHandler.cs:118–128`) that resets to `Requested`; whether `PaymentInfo`/paid amounts carry to the new booking is unconfirmed → risk of orphaned payments once C4 wires payment.

## What Changes

- Restore the commented booking indexes via migration (aligned with C3's unique index to avoid duplication).
- Consolidate to a single migration history after auditing what is actually applied in production.
- Confirm and fix reschedule payment carry-over (or intentionally re-collect), so payment linkage is never silently lost.

## Capabilities

### New Capabilities
- `booking-data-integrity`: booking queries SHALL be index-supported, the migration history SHALL be single and consistent, and rescheduling SHALL preserve (or explicitly re-establish) payment linkage.

### Modified Capabilities
- (none.)

## Impact

- **DB**: add booking indexes; migration-history consolidation (metadata — handled against the deployed `__EFMigrationsHistory`).
- **Code**: `RescheduleBookingCommandHandler`/`Booking.Reschedule` carry `PaymentInfo` to the new booking (or document re-collection).
- **API/Flutter**: none.
- **Depends on**: coordinate the index set with **C3** (unique slot index); reschedule carry-over interacts with **C2/C4** payment model.
