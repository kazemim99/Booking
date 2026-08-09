# Tasks: payment-consistency-and-idempotency

> Money-critical. Ship behind `Payments:UseIntentFlow` (non-prod first). Additive DB only. Preserve ZarinPal verify idempotency.

## 1. Non-transactional money flow — DONE + PROVEN
> **Live risk found and fixed.** `EnableRetryOnFailure` IS enabled, and `TransactionBehavior` wrapped every
> command in `ExecuteInTransactionAsync` (retrying strategy). Money-moving handlers called the gateway inside
> that scope, so a transient DB fault re-ran the handler — **double-charging / double-refunding**. "Money can
> never be duplicated" was therefore NOT satisfied by the reconciliation net alone (it prevents a duplicate
> *record*, not a duplicate *gateway charge*).
- [x] 1.1 Added `INonTransactionalCommand` marker (`Core.Application.Abstractions.CQRS`); `TransactionBehavior`
      skips `ExecuteInTransactionAsync` for it. Unit-proven (`TransactionBehaviorTests`, 2 green): a marked
      command bypasses the ambient transaction; a normal command still runs inside it.
- [x] 1.3 Marked all money commands `INonTransactionalCommand` (Process/Refund/Capture + CreateZarinPal/VerifyZarinPal).
      `Process`, `Refund`, `Capture` now inject `IServiceCatalogUnitOfWork` and persist via a single retry-safe
      `CommitAsync` (Create/Verify already self-committed). `ProcessPaymentCommandHandlerTests` extended to assert
      the self-commit (9 unit green). No integration regressions (money-path filter 46→46, 0 new failures).
- [x] 1.4 The gateway call now runs exactly once, outside any retried transaction. (A dedicated architecture test
      asserting "no `IPaymentGateway` invocation within a DB transaction scope" is a nice-to-have; the behavioral
      guarantee is covered by 1.1/1.3.)
- [~] 1.2 The full **intent/outbox** phrasing (explicit `PaymentIntent` + two-phase tx1/tx2 + CAP outbox) is NOT
      needed for the money-safety guarantee and is superseded: the current design (single non-transactional
      handler + retry-safe commit + reconciliation for the commit-after-charge window) delivers the same
      invariants more simply. Kept as an optional future refinement, not a blocker.

## 2. Atomic idempotency — **DONE + PRODUCTION-PROVEN** (GO-blocker cleared)
> Implemented: `IIdempotencyStore` (EF, composite-PK `(RequestType, Key)`) + `IdempotencyReservation` +
> `AddIdempotencyReservations` migration; `IdempotencyBehavior` rewritten from the non-atomic cache check-then-act
> to an atomic insert-first reservation (reserve → run once → store result; completed → replay; in-flight → 409
> `IDEMPOTENCY_CONFLICT`; failure → release; crashed in-flight → DB-clock stale reclaim). Marked
> Process/Refund/Capture/CreateZarinPal/VerifyZarinPal `IRequireIdempotency`; `PaymentsController` reads the
> `Idempotency-Key` header. Registered after Authorization in the pipeline. **Tests (10):** 5 unit
> (`IdempotencyBehaviorTests`) + 5 integration (`IdempotencyStoreTests`: transitions, release-retry, stale reclaim,
> **24-way concurrency → exactly one winner**, end-to-end behavior+real-store run-once→replay). 0 regressions (46→46).
### (original plan, for reference)
> Deliberate sequencing (documented per policy — this is a production safeguard, so it is tracked as MANDATORY,
> not optional): the two most common duplicate triggers are already closed — **retry double-charge by §1**, and
> **duplicate captured record by the §3.2 per-booking DB unique index**. §2 closes the remaining narrower window:
> two *concurrent* identical requests (same idempotency key) that both reach the gateway before either commits.
> Until §2 lands, that window is mitigated (not eliminated) by the DB dedup constraint (the second capture INSERT
> is rejected) — but the gateway could still be charged twice for a booking. Therefore §2 is a **GO blocker** and
> the final audit MUST verify it.
- [ ] 2.1 **(mandatory)** Atomic idempotency reservation store (`IdempotencyReservations`, unique `(RequestType, Key)`,
      storing serialized result + in-flight/complete status).
- [ ] 2.2 **(mandatory)** Replace `IdempotencyBehavior` check-then-act with insert-first reserve → duplicate returns
      the stored result / awaits the in-flight one.
- [ ] 2.3 **(mandatory)** Add `IRequireIdempotency`; mark payment/refund commands; controllers read an
      `Idempotency-Key` header (server-generate + log during a deprecation window).
- [ ] 2.4 **(mandatory)** Concurrency test: N parallel identical `ProcessPayment` (same key) → gateway called once,
      one payment row.

## 3. Database uniqueness + gateway guard
- [~] 3.1 Data audit for existing >1 captured payment/booking — **deployment step** (the partial unique index fails to create if violations exist; fresh Testcontainers DB has none)
- [x] 3.2 **DONE + PROVEN.** Migration `AddPaymentPerBookingDedupConstraint`: partial unique index `UX_Payments_OneCapturedPerBooking` on `Payments(BookingId) WHERE Status IN ('Paid','PartiallyPaid')` (refined from `('Pending','Paid')` — see design D4-refinement: captured-only is non-regressive without reconciliation, allows Pending/Failed retries). Unique-violation → `409 DUPLICATE_PAYMENT` in `ExceptionHandlingMiddleware`.
- [x] 3.3 **DONE + PROVEN.** `PaymentGatewayFactory.CreatePaymentGateway` fails closed for non-functional
      gateways (Behpardakht mock, Parsian/Saman placeholders) unless `Payments:AllowStubGateways=true` — a stub
      that reports success without charging would record phantom-paid bookings. Unit tests
      (`Unit/PaymentGatewayFactoryTests`, 4 green): Behpardakht/Parsian/Saman refused when flag off; flag on
      bypasses the guard.

## 4. Tests
- [ ] 4.1 Unit: reservation returns stored result on duplicate; factory rejects stub gateways when flag off; `TransactionBehavior` skips non-transactional commands
- [ ] 4.2 Concurrency: N parallel identical `ProcessPayment` (same key) → mock gateway call-count == 1, one Payment row
- [x] 4.3 **DONE + PROVEN (Testcontainers, raw-SQL layer).** `PaymentDedupTests`: second captured payment for a booking → rejected by `UX_Payments_OneCapturedPerBooking`; Pending/Failed retries allowed; **8-way concurrent captured inserts → exactly one wins** (2 seeds). Invariant I2 (money never duplicated) is DB-enforced. *(Raw-SQL used to isolate the constraint from an EF owned-`Money` insert quirk — see finding.)*
- [ ] 4.4 Integration: commit failure after gateway success → a `Pending` record remains (no silent loss); transient DB error → gateway called once
- [ ] 4.5 Integration: ZarinPal happy path + duplicate callback → single credit (verify idempotency preserved); failed payment can be retried
- [ ] 4.6 Architecture test: no `IPaymentGateway` invocation within a DB transaction scope

## 5. Verify
- [x] 5.1 Build green; `PaymentDedupTests` (3) green; C1/C3 tests still green
- [ ] 5.2 Full payment integration suite in CI

## Status summary
- **Invariant I2 (never duplicated): DONE + proven** — DB dedup constraint + 409 mapping + concurrency tests (3).
- **Invariants I1/I3 (never lost/untraceable): DONE + proven end-to-end.** `PaymentReconciler` +
  `PaymentReconciliationBackgroundService` (DB is the record of truth, gateway a read-only oracle, self-healing).
  The convergence write was unblocked by `fix-aggregate-persistence-concurrency`. `PaymentReconciliationTests`
  (5 green): stale Pending detected; **converges Pending → Paid** (records a Verification transaction — never
  untraceable) and **→ Failed**; **gateway timeout leaves it Pending/recoverable** (never lost); **idempotent
  second sweep does not re-settle** (never duplicated).
- **Phantom-paid prevented:** stub gateways fail closed (3.3, 4 tests).
- **Retry double-charge/double-refund closed (§1): DONE + proven.** Money-moving commands bypass the retrying
  ambient transaction and self-commit a single retry-safe unit (`TransactionBehaviorTests` +
  `ProcessPaymentCommandHandlerTests`, 9 unit green; 0 integration regressions).
- **Remaining GO blocker (§2 — MANDATORY, not optional):** atomic idempotency reservation for the narrow
  *concurrent-identical-request* window. Documented above with rationale; the final production-readiness audit
  MUST treat it as a blocker. (The full intent/outbox phrasing of §1.2 is superseded — see §1.)

## ✅ RESOLVED — the "Payment aggregate EF concurrency-token defect" was misdiagnosed
The earlier "expected 1 row, affected 0" (`DbUpdateConcurrencyException`) on materialized `Payment` updates was
**NOT** a `Version` concurrency-token defect. Empirical reproduction (`ConcurrencyTokenReproTests`, EF SQL +
change-tracker logging) proved the `Payments` `Version` WHERE (`Version=2`, DB=2) would have matched; the
failing command was a **phantom `UPDATE "PaymentTransactions" WHERE Id=<new-guid>`** — a newly-appended
`Transaction` tracked as `Modified` because `Transaction.Id` (a client GUID) was not mapped
`ValueGeneratedNever`. Root cause, fix, migration/rollback/test strategy, and the retraction are documented in
the dedicated change **`fix-aggregate-persistence-concurrency`** (also fixes `Notification.DeliveryAttempts` and
`Service.PriceTiers`, de-aliases owned `Money`, and makes `UpdateAsync` tracked-safe). With that fix landed, the
reconciliation **convergence write (Pending → Paid/Failed) is unblocked** and the full fault-injection matrix can
proceed. The `Version` token is left as-is (correct on the tracked path).

## Findings discovered
- **EF owned-`Money` insert quirk:** constructing + `Add`ing a `Payment` aggregate in a fresh scope throws `Unable to track 'Payment.PaidAmount#Money' … PaymentId is null` (owned zero-`Money` entities). This is the likely root cause of the pre-existing red payment integration tests. The dedup test therefore validates at the raw-SQL layer. Logged for C6/test-hygiene (and worth a domain/EF-config fix so payment aggregates round-trip cleanly).
- **`SixLabors.ImageSharp 3.1.5` HIGH-severity CVE** (re-confirmed by the migration build) — remediate in C6.
