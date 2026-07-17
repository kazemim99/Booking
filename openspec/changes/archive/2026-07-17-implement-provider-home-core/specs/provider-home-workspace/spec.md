## ADDED Requirements

### Requirement: Maturity Signal Degradation Bias

When the statistics source that feeds maturity classification is unavailable, the Home SHALL synthesize maturity signals from the day's booking data instead of defaulting to zeroed signals, and the synthesized signals MUST be biased so that a data outage can only promote a provider toward the operational agenda — an established provider MUST never be demoted to the Setup/Growth scaffold by a data gap.

#### Scenario: Statistics outage with bookings today
- **WHEN** the statistics endpoint fails and the provider has bookings today
- **THEN** maturity signals are synthesized from today's bookings (non-zero totals, profile assumed complete)
- **AND** the provider resolves to Traction/Operational, never Setup or Growth

#### Scenario: Statistics outage for a genuinely new provider
- **WHEN** the statistics endpoint fails and the provider has no bookings today
- **THEN** the provider resolves to Growth (profile assumed complete), not Setup
- **AND** no scaffold demotion below the provider's last plausible phase occurs

### Requirement: Superseded Refresh Guard

Concurrent Home refreshes SHALL be sequenced such that only the most recently initiated refresh may apply its result; a slower, superseded request completing later MUST be discarded and MUST NOT overwrite the state produced by a newer request (including marking fresh data as failed or stale).

#### Scenario: Slow stale failure loses to a newer success
- **WHEN** a refresh is initiated, then a second refresh is initiated and succeeds, and the first request later completes with a failure
- **THEN** the Home state remains the newer successful context
- **AND** no staleness or error indication from the superseded request is shown
