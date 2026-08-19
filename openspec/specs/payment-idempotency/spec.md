# payment-idempotency Specification

## Purpose
TBD - created by archiving change payment-consistency-and-idempotency. Update Purpose after archive.
## Requirements
### Requirement: Payment and refund operations are atomically idempotent
Payment and refund commands SHALL require an idempotency key and SHALL de-duplicate identical operations atomically for both concurrent and sequential duplicates, producing at most one gateway side effect per key and returning the original result to duplicates.

#### Scenario: Concurrent duplicate charge
- **WHEN** two identical payment requests with the same idempotency key arrive concurrently
- **THEN** the gateway is charged exactly once and both callers receive the same result

#### Scenario: Sequential retry
- **WHEN** a payment request is retried with the same idempotency key after the first completed
- **THEN** no new charge occurs and the stored result is returned

#### Scenario: Missing idempotency key
- **WHEN** a payment/refund command is submitted without an idempotency key
- **THEN** the request is rejected (or, during the deprecation window, a server key is generated and logged)

### Requirement: One successful payment per booking
The system SHALL enforce at the database level that a booking has at most one non-failed (Pending or Paid) payment, so a duplicate charge cannot be recorded even if application-level checks race.

#### Scenario: Second charge for a booking is rejected
- **WHEN** a second payment for a booking that already has a Pending or Paid payment is attempted
- **THEN** it is rejected (409) and no second charge is recorded

#### Scenario: Retry after failure is allowed
- **WHEN** a booking's only prior payment is Failed
- **THEN** a new payment attempt is permitted

