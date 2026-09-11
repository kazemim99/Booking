# customer-checkout

## ADDED Requirements

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
