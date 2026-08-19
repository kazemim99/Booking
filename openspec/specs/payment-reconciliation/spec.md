# payment-reconciliation Specification

## Purpose
TBD - created by archiving change financial-ledger-and-settlement. Update Purpose after archive.
## Requirements
### Requirement: Pending payments are reconciled against the gateway
A background reconciliation process SHALL periodically resolve payment records left in a Pending state (e.g. a charge whose result was never recorded) by querying the gateway, settling them to Paid or Failed idempotently.

#### Scenario: Charged-but-unrecorded payment is recovered
- **WHEN** a payment intent remains Pending past a threshold and the gateway reports it as paid
- **THEN** reconciliation settles it to Paid and posts the ledger entries, with no double-posting on repeated runs

#### Scenario: Abandoned intent is failed
- **WHEN** a Pending intent is not paid at the gateway past a threshold
- **THEN** reconciliation settles it to Failed

### Requirement: Refunds keep accounting consistent
A refund SHALL reverse the associated commission and prevent the platform from paying out refunded money — either by reversing an executed payout via a balancing entry or by reducing future payouts by the refunded amount.

#### Scenario: Refund before payout
- **WHEN** a booking is refunded before its provider payout
- **THEN** the provider-payable balance is reduced by the refunded net so the payout excludes it

#### Scenario: Refund after payout
- **WHEN** a booking is refunded after its provider payout executed
- **THEN** the refunded amount is recovered via a balancing ledger entry / reduced future payouts, never a silent platform loss

