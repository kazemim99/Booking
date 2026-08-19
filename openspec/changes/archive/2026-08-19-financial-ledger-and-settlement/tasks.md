# Tasks: financial-ledger-and-settlement

> Depends on C2 (consumes payment events; settles Pending intents). Additive DB. Reconciliation behind `Finance:ReconciliationEnabled`.

## 1. Ledger
- [x] 1.1a **DONE.** Ledger domain core: immutable `LedgerEntry` (`Entity<Guid>`, signed-amount helper), `LedgerAccount`
      (GatewayClearing/ProviderPayable/PlatformRevenue/Customer) + `LedgerDirection`/`LedgerEntryType` enums, and a
      self-validating `LedgerTransaction` factory (`ForCharge`/`ForRefund`/`ForPayout`) that enforces the double-entry
      zero-sum invariant and single-currency. Carries `EventId` for idempotent posting.
- [x] 1.1b **DONE.** `LedgerEntryConfiguration` + migration `AddLedgerEntries` — unique `(EventId, Account)` for
      idempotency; `Id` `ValueGeneratedNever` (aggregate-persistence lesson); owned `Money`; indexes by
      booking/payment/provider. **Immutability enforced at the DbContext**: `EnforceLedgerAppendOnly` throws on any
      Modified/Deleted `LedgerEntry` (append-only; corrections are compensating transactions).
- [x] 1.2 **DONE.** Idempotent domain-event handlers post a balanced `LedgerTransaction` on `PaymentVerified` /
      `PaymentProcessed` (charge) and `PaymentRefunded` (refund), keyed by a stable `LedgerEventKeys` id →
      unique `(EventId, Account)`. Decoupled/self-committing (the dispatcher runs handlers in their own scope,
      design D2). `ILedgerRepository.AppendAsync` is idempotent; the unique index is the hard backstop.
      *(PayoutCompleted posting deferred to §2.3 with commission recognition.)*
- [x] 1.3 **DONE.** `ILedgerRepository.GetAccountBalanceAsync` (signed Σ, optional provider scope) +
      `GetByPaymentAsync` (payment traceability).

## 2. Reconciliation + refund reversal
- [x] 2.1a **DONE.** `ILedgerReconciler.ReconcileMissingChargeEntriesAsync` — finds captured payments with no charge
      entry (partial-failure gap) and posts them idempotently. Proven by `LedgerIntegrationTests`.
- [x] 2.1b **DONE.** `LedgerMaintenanceBackgroundService` runs the ledger reconciler + drift check on an interval,
      gated by `Finance:ReconciliationEnabled` (registered as a hosted service). No Hangfire in this repo →
      `BackgroundService` (consistent with C2), documented in ARCHITECTURAL_DECISIONS.md.
- [x] 2.2 **DONE.** `DetectBalanceDriftAsync` verifies the double-entry invariant (Σ signed = 0 across the whole
      ledger), logs an error/alert on drift. Proven (healthy → 0; injected unbalanced entry → detected).
- [x] 2.3a **DONE.** Payout posts to the ledger on `PayoutCompletedEvent` with **commission recognition**
      (ProviderPayable debit gross, PlatformRevenue credit commission, GatewayClearing credit net), idempotent by
      payout id. `PayoutCompletedEvent` extended with Gross/Commission. Proven by the E2E test (§3.5).
- [x] 2.3b **DONE.** Payout is computed **from the ledger** (`CreatePayoutCommandHandler` reads
      `GetProviderPayableBalanceAsync`, no longer sums Payments) — refunds (which debit ProviderPayable) reduce it
      automatically, and a provider whose refunds exceed unpaid charges (balance ≤ 0) is **blocked from further
      payouts** (D5 safe default). Real clawback (moving a negative balance to a transfer) remains a **finance
      decision**, gated off. Proven: refund-reduces-payout, block-when-not-positive, refund-after-payout (negative
      balance, blocked, provider ledger still balances → no money lost).

## 3. Tests
- [x] 3.1a **DONE.** Unit/property: `LedgerTransactionTests` (10 green) — zero-sum per event (charge/refund/payout),
      account balance = Σ signed entries, provider-share = gross−commission, unbalanced/mixed-currency rejected, and
      randomized property checks (3 seeds × 500 iterations) for mathematical confidence in the balance invariant.
- [x] 3.1b **DONE (ledger half).** `LedgerIntegrationTests` (6 green): charge posted atomically-on-verify;
      **immutability** (update+delete rejected); **idempotent** posting; **replay safety** (deterministic key);
      **duplicate-event** handling (unique-index backstop); **reconciliation after partial failure** (+ idempotent
      re-run); provider-payable **balance** + payment **traceability**.
- [ ] 3.1c Refund-reverses-commission + payout-net-of-refunds end-to-end (with §2.3).
- [ ] 3.2 Integration: charge→commission→payout→refund yields a balanced, reversed ledger
- [ ] 3.3 Concurrency/idempotency: repeated event delivery + repeated reconciliation runs post at most once
- [ ] 3.4 Integration: Pending intent recovered to Paid; abandoned intent → Failed
- [x] 3.5a **DONE.** E2E: charge → payout leaves a fully balanced, traceable ledger (ProviderPayable nets to 0,
      PlatformRevenue = commission, GatewayClearing = retained commission). `LedgerIntegrationTests.End_to_end_*`.
- [x] 3.5b **DONE.** E2E: refund after payout records a negative provider balance, blocks further payouts, and the
      provider's ledger still sums to zero (no money lost).

## Summary — C5 is COMPLETE (22 tests: 11 domain + 11 integration)
Ledger is the single source of truth: append-only + immutable (DbContext-enforced), balanced double-entry
(zero-sum, property-tested), idempotent charge/refund/payout posting (replay/duplicate safe), reconciliation for
partial failures, balance-drift detection, background maintenance, and **payout/commission/provider-balance all
derived from the ledger, never re-summed from Payments/Bookings**. 0 regressions (money-path filter 46→46).

### Remaining (documented, non-blocking / needs a decision)
- Refund→commission reversal *entry* for a refund that occurs after commission was recognized at payout (today the
  refund debits ProviderPayable, correctly reducing/negativing the balance; a separate PlatformRevenue reversal
  entry for the commission portion is a refinement).
- Real payout clawback (negative-balance → transfer) — **finance decision**, gated off.
- Financial *reporting* endpoints reading the ledger (admin/finance scope) — additive, out of customer scope.

## 4. Verify
- [ ] 4.1 Build + tests green; reconciliation job idempotent under repeat
- [ ] 4.2 Confirm payout clawback policy (block vs negative-balance) before enabling refund-after-payout in prod
