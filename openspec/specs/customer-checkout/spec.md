# customer-checkout Specification

## Purpose
TBD - created by archiving change customer-payment-experience. Update Purpose after archive.
## Requirements
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

### Requirement: Deposit-required booking confirmation
Booking confirmation SHALL require a verified deposit payment when the provider's policy requires a deposit; a deposit-required booking is not confirmed until its deposit is paid.

#### Scenario: Deposit required
- **WHEN** a provider policy requires a deposit and the customer confirms a booking
- **THEN** confirmation proceeds through checkout and the booking is confirmed only after the deposit is verified

### Requirement: Checkout stays disabled until proven against a real gateway
The customer checkout journey SHALL remain disabled by default (`CHECKOUT_ENABLED` off) and MUST NOT be
enabled in any environment until a real deposit has been taken end to end through the payment gateway's
sandbox — create → redirect → pay → gateway callback → server-side verification → booking confirmed — and
the resulting ledger entries for that payment balance to zero. Passing the deterministic API E2E against a
fake gateway SHALL NOT satisfy this requirement.

#### Scenario: Flag is off by default
- **WHEN** the app is built without an explicit override
- **THEN** `CHECKOUT_ENABLED` resolves false, the checkout entry point is absent, and the server's deposit
  gate still applies to any booking created by other means

#### Scenario: Sandbox verification precedes enablement
- **WHEN** enabling checkout for any environment is proposed
- **THEN** a real sandbox payment has already completed the full chain and its ledger entries sum to zero

#### Scenario: Fake-gateway evidence is insufficient
- **WHEN** only the fake-gateway API E2E has passed
- **THEN** the flag remains off, because a stub that reports success without charging would record a
  phantom-paid booking

### Requirement: Non-functional gateways are refused rather than simulated
In any environment where checkout is enabled, the payment gateway factory SHALL refuse to construct a
non-functional or stub gateway unless stub gateways are explicitly permitted by configuration, so a booking
can never be recorded as paid without a real charge.

#### Scenario: Stub gateway refused in production
- **WHEN** checkout is enabled and the configured gateway resolves to a stub or placeholder implementation
- **THEN** construction fails closed rather than reporting a successful charge

#### Scenario: Callback target is reachable and correct
- **WHEN** checkout is enabled
- **THEN** the configured gateway callback URL is publicly reachable and resolves to the real callback route,
  so the gateway's server-to-server confirmation can actually arrive

