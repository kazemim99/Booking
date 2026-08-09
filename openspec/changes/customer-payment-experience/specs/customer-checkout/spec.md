# Spec: customer-checkout

## ADDED Requirements

### Requirement: Customer can pay for or deposit on a booking
The customer app SHALL let a customer pay the full amount or the required deposit for a booking via the gateway redirect flow, submitting an idempotency key so retries do not double-charge, and verifying the outcome server-side on return.

#### Scenario: Successful payment
- **WHEN** a customer completes the gateway redirect for a booking
- **THEN** the app verifies the payment server-side and shows a success/receipt state, and the booking reflects the payment

#### Scenario: Duplicate submission does not double-charge
- **WHEN** a customer retries/resubmits a payment for the same attempt (double-tap, resume)
- **THEN** the same idempotency key is sent and at most one charge occurs

#### Scenario: Payment failure is recoverable
- **WHEN** the gateway reports failure or the return is cancelled
- **THEN** the app shows a failure state with a retry path and the booking is not marked paid

### Requirement: Checkout is resumable
When a booking has a Pending payment (e.g. the app was killed during redirect), reopening the flow SHALL resume verification rather than starting a new charge.

#### Scenario: App killed mid-redirect
- **WHEN** the customer reopens a booking that has a Pending payment
- **THEN** the app resumes the verify step for the existing attempt

### Requirement: Checkout renders all UI states
The checkout screens SHALL render loading, error (retry), and offline states, consistent with the app's standardized state components.

#### Scenario: Offline during checkout
- **WHEN** connectivity is lost during checkout
- **THEN** an offline state is shown and no partial/ambiguous charge is initiated

## MODIFIED Requirements

### Requirement: Deposit-required booking confirmation
Booking confirmation SHALL require a verified deposit payment when the provider's policy requires a deposit; a deposit-required booking is not confirmed until its deposit is paid.

#### Scenario: Deposit required
- **WHEN** a provider policy requires a deposit and the customer confirms a booking
- **THEN** confirmation proceeds through checkout and the booking is confirmed only after the deposit is verified
