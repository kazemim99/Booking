## Context

Payments/refunds/payouts already raise domain events and publish via CAP (in-process outbox). C2 adds durable `Pending` payment intents. There is no ledger, no reconciliation, and refunds don't touch payouts/commissions.

## Goals / Non-Goals

**Goals:** balanced immutable ledger for every money movement; idempotent reconciliation that recovers C2's `Pending` intents; refund reverses commission/payout.
**Non-Goals:** a general accounting suite; provider payout scheduling redesign; tax engine changes.

## Decisions

- **D1 — Append-only double-entry ledger.** A `LedgerEntry` is immutable; each money event writes ≥2 entries (debit+credit) that sum to zero per event, tagged with `BookingId`/`PaymentId`/`ProviderId` and an account (customer, provider-payable, platform-revenue, gateway-clearing). *Invariant:* Σ(entries per event) = 0; Σ(entries per account) = account balance. *Rationale:* auditable, reconstructable balances, tamper-evident.
- **D2 — Ledger written from events, not inline.** CAP handlers on `PaymentProcessed`/`PaymentRefunded`/`PayoutCompleted` append entries. *Rationale:* decouples accounting from the transactional command path; the outbox guarantees at-least-once, and D3 makes it idempotent.
- **D3 — Idempotent by event id.** Each ledger write is keyed by a unique `(EventId)` so redelivery does not double-post. Reconciliation is idempotent likewise.
- **D4 — Reconciliation job (Hangfire).** Periodically: (a) find `Pending` payment intents older than T, query the gateway, settle to Paid/Failed (recovers C2's charged-but-lost); (b) assert ledger balances vs payment/payout aggregates and alert on drift.
- **D5 — Refund reverses commission/payout.** On refund, post reversing ledger entries for the associated commission; if a payout already executed, either claw back (reversal entry + provider-negative-balance) or block future payouts up to the refunded amount. Prefer blocking-then-reconcile to avoid negative real transfers.
- **D6 — Payout amount is net of refunds/commission.** Payout computation reads the ledger's provider-payable balance, not gross payment, so refunds already reflected reduce payouts.

## Risks / Trade-offs

- [Double-entry bugs cause silent drift] → Mitigation: property-based tests on the zero-sum invariant; a reconciliation alert on any account/aggregate mismatch.
- [Reconciliation double-posts on redelivery] → Mitigation: idempotency by event id; unique constraint on `(EventId, Account)`.
- [Backfill of historical payments] → Trade-off: start-forward with a documented cutover; optional one-time backfill script (idempotent) if history must be represented.
- [Clawback of an already-sent payout] → Mitigation: prefer block-future-payouts; real reversal only via provider-balance offset, never a negative transfer.

## Migration Plan

Additive `LedgerEntries` + payout reversal linkage. Start-forward; optional idempotent backfill. Reconciliation job behind `Finance:ReconciliationEnabled`. Rollback: pause job; ledger is read-additive.

## Open Questions

- Chart of accounts granularity (single platform-revenue account vs per-fee-type). Lean: minimal set now (customer, provider-payable, platform-revenue, gateway-clearing), extend later. **(Implementation decision.)**
- Payout clawback policy (block vs negative-balance) may have a **business/finance** dimension → flag for confirmation before enabling refund-after-payout reversal in production.
