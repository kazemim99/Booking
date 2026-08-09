# Proposal: financial-ledger-and-settlement

## Why

The audit found the money system is **unauditable and can leak funds**:

- **No double-entry ledger** (`ledger`=0 files) — money movements (charge, deposit, refund, commission, payout) have no balanced, immutable record; balances and platform economics cannot be verified.
- **No reconciliation/settlement job** (`settlement`=1 file) — nothing recovers the `Pending` intents that C2 introduces (charged-but-callback-lost), and nothing reconciles gateway state vs our records.
- **Refunds do not reverse payouts/commissions** (`RefundPaymentCommandHandler.cs`) — a refund after a provider payout is a direct platform loss; payouts exist (`Commands/Payout/ExecutePayout`) but are never clawed back.

## What Changes

- Introduce an **append-only, double-entry `Ledger`** written from payment/refund/payout domain (CAP) events, with the invariant that entries per money event balance to zero.
- Add a **reconciliation background job** (Hangfire) that settles `Pending` payment intents against the gateway and reconciles ledger vs payments, idempotently.
- Make refunds **reverse the associated commission/payout** (or block payout while a booking remains refundable).

## Capabilities

### New Capabilities
- `financial-ledger`: every money movement SHALL produce balanced, immutable, double-entry ledger records that make balances and platform revenue auditable.
- `payment-reconciliation`: pending/ambiguous payments SHALL be reconciled against the gateway idempotently, and refunds SHALL reverse the corresponding commission/payout so accounting stays consistent.

### Modified Capabilities
- (none — new financial specs.)

## Impact

- **Code**: `Ledger` aggregate + CAP event handlers on `PaymentProcessed`/`PaymentRefunded`/`PayoutCompleted`; reconciliation Hangfire job; refund → payout/commission reversal.
- **DB**: immutable `LedgerEntries` table (indexed by booking/payment/provider); payout gains a refund-reversal linkage.
- **API**: additive admin/finance read endpoints (out of customer scope).
- **Flutter**: none.
- **Security**: ledger is append-only / tamper-evident; reconciliation closes the charged-but-lost gap.
- **Depends on**: **C2** (consumes its payment events and settles its `Pending` intents).
