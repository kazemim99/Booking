# Spec: financial-ledger

## ADDED Requirements

### Requirement: Every money movement produces balanced double-entry records
Each money event (charge, deposit, refund, commission, payout) SHALL append immutable double-entry ledger records whose amounts sum to zero for the event, tagged with the booking, payment, and provider, so balances are reconstructable.

#### Scenario: Charge posts balanced entries
- **WHEN** a payment is processed
- **THEN** ledger entries are appended that sum to zero for the event and update the relevant account balances

#### Scenario: Ledger is immutable and idempotent
- **WHEN** the same money event is delivered more than once
- **THEN** the ledger is posted at most once for that event and existing entries are never mutated

### Requirement: Balances are auditable
The provider-payable balance and platform revenue SHALL be derivable from the ledger and reconcilable against the payment and payout aggregates.

#### Scenario: Reconciliation detects drift
- **WHEN** ledger-derived balances disagree with the payment/payout aggregates
- **THEN** the discrepancy is surfaced (alert) rather than silently ignored
