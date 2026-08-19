# payment-processing-integrity Specification

## Purpose
TBD - created by archiving change payment-consistency-and-idempotency. Update Purpose after archive.
## Requirements
### Requirement: No charge without a recoverable record
A payment or refund that results in an external gateway side effect SHALL always have a durable database record created **before** the gateway is called, so that a failure after the gateway call leaves a reconcilable record rather than lost money.

#### Scenario: Commit fails after a successful charge
- **WHEN** the gateway charge succeeds but persisting the result fails
- **THEN** a `Pending` payment record (created before the charge) remains in the database and is later resolved by reconciliation — no money is charged without a record

### Requirement: External calls run outside the database transaction and outside retries
Gateway calls SHALL NOT execute inside an open database transaction, and SHALL NOT execute inside a retried execution strategy that could re-invoke them.

#### Scenario: Transient database error does not re-charge
- **WHEN** a transient database error occurs while recording a payment result
- **THEN** the recording is retried but the gateway is charged at most once

#### Scenario: Gateway latency does not hold a database transaction
- **WHEN** a gateway call is in flight
- **THEN** no database transaction is held open for the duration of the external call

### Requirement: Non-functional gateways fail closed
The payment gateway factory SHALL refuse to return a gateway whose implementation is a stub or unimplemented (it MUST NOT report success without charging) unless an explicit stub-gateways flag is enabled.

#### Scenario: Stub gateway is rejected in production
- **WHEN** a payment is routed to a stub/unimplemented gateway with the stub flag disabled
- **THEN** the request fails clearly instead of recording a phantom-successful payment

