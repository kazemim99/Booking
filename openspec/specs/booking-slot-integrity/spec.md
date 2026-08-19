# booking-slot-integrity Specification

## Purpose
TBD - created by archiving change booking-slot-integrity. Update Purpose after archive.
## Requirements
### Requirement: A staff time slot holds at most one active booking
For a given staff member and start time, the system SHALL guarantee at most one active (Requested or Confirmed) booking, on every creation and reschedule path, enforced at the database level independent of availability-row presence.

#### Scenario: Concurrent bookings for the same slot (availability rows present)
- **WHEN** two customers book the same staff/time concurrently and availability rows exist
- **THEN** exactly one booking is created and the other request fails

#### Scenario: Concurrent bookings for the same slot (no availability rows)
- **WHEN** two customers book the same staff/time concurrently and no availability rows exist for that time
- **THEN** at most one booking is created (the database unique constraint is the backstop)

### Requirement: Booking a time with no availability is handled deterministically
When a requested time has no availability slot covering it, the system SHALL NOT silently create an unguarded booking; it SHALL either reject the request or occupy a generated slot, per configuration.

#### Scenario: No covering availability slot
- **WHEN** a booking is requested for a valid time that has no availability slot and fail-closed is enabled
- **THEN** the request is rejected with a clear error rather than creating an unprotected booking

### Requirement: Slot contention surfaces as 409
When a booking cannot be created because the slot was just taken (optimistic-concurrency loss or unique-constraint violation), the API SHALL respond 409 with a machine-readable "slot taken" indication, not 500.

#### Scenario: Losing writer gets 409
- **WHEN** a concurrent booking loses the slot race
- **THEN** the response is 409 with a "slot taken, choose another time" indication

