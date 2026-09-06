# Spec: booking-data-integrity

## ADDED Requirements

### Requirement: Booking queries are index-supported
The common booking access paths (by customer, by provider, by staff, by status, and the availability/conflict lookup) SHALL be backed by database indexes so they do not degrade to full scans under load.

#### Scenario: Customer bookings query uses an index
- **WHEN** a customer's bookings are queried
- **THEN** the query is served by a `CustomerId` index rather than a full table scan

### Requirement: Single consistent migration history
The ServiceCatalog context SHALL have one authoritative migration history that applies cleanly from a fresh database and against the currently deployed database.

#### Scenario: Migrations apply from clean and from existing
- **WHEN** migrations are applied to a fresh DB and to a deployed DB
- **THEN** both succeed with no conflicting or duplicate "Init" migrations

### Requirement: Rescheduling preserves payment linkage
Rescheduling a booking SHALL preserve the original booking's payment information on the resulting booking (or explicitly re-establish payment), so a paid booking never loses its payment linkage.

#### Scenario: Paid booking is rescheduled
- **WHEN** a booking with a recorded payment is rescheduled
- **THEN** the new booking retains the payment linkage (or the flow explicitly re-collects), with no orphaned payment
