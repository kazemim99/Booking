# Tasks: fix-aggregate-persistence-concurrency

> Production-breaking persistence defect. No DB migration (model-side only). Gates C2 reconciliation convergence.

## 1. Root cause (DONE — empirically proven)
- [x] 1.1 Reproduce the exact production flow (load → append owned child → save) with EF SQL + change-tracker
      logging (`ConcurrencyTokenReproTests`, Testcontainers). Confirmed the failing command is a phantom
      `UPDATE "PaymentTransactions" WHERE Id = <new-guid>` (0 rows), not the `Payments` `Version` check.
- [x] 1.2 Confirm the primary cause: `Transaction.Id` (client GUID) not `ValueGeneratedNever` → new child
      tracked `Modified` instead of `Added`.
- [x] 1.3 Confirm the compounding causes: owned-`Money` instance aliasing (`DuplicateDependentEntityTypeInstanceWarning`)
      and `UpdateAsync` re-stamping tracked graphs.
- [x] 1.4 **Retract** the earlier "`Version` concurrency-token defect" finding — the token is correct on the
      tracked path (WHERE `Version=2`, DB=2, would have matched).

## 2. Fix (DONE)
- [x] 2.1 `Transaction.Id` → `ValueGeneratedNever()` (`PaymentConfiguration`).
- [x] 2.2 `DeliveryAttempt.Id` → `ValueGeneratedNever()` (`NotificationConfiguration`).
- [x] 2.3 `PriceTier.Id` → `ValueGeneratedNever()` (`ServiceConfiguration`).
- [x] 2.4 `Money.Clone()` added; `Transaction` ctor defensively clones incoming `Money`; `Payment`
      `VerifyPayment`/`Capture`/`ProcessCharge` assign `Amount.Clone()` to `PaidAmount`.
- [x] 2.5 `PaymentWriteRepository.UpdateAsync` + shared `EfRepositoryBase.UpdateAsync` → no-op when tracked,
      attach only when detached.

## 3. Validation
- [x] 3.1 Reproduction green: tracked verify (append Transaction) → `Paid`; production refund (tracked +
      `UpdateAsync`) → `Refunded`; SQL/graph shows the new Transaction as `Added` and `SaveChanges OK`.
- [x] 3.2 C2 dedup (3) + reconciliation detection (1) still green.
- [x] 3.3 Domain unit suite (309) green — no behavior regression from `Money.Clone`/`Payment` changes.
- [x] 3.4 Baseline diff of Payments/Notifications/ServiceManagement/Bookings integration filter (96 tests, same
      filter, stash on/off): **baseline 87 failed → post-fix 46 failed; 41 tests repaired; 0 regressions.**
      Newly-green include service CRUD (`ServiceCompleteWorkflow_CreateUpdateDelete`, Create/Update/Delete),
      `CapturePayment_WithPartialAmount`, notification sends, booking/payment reads (their setup appended owned
      children and was poisoned by the failing save). Pre-existing reds catalogued below.
- [x] 3.5 Diagnostic `ConcurrencyTokenReproTests` deleted; promoted to permanent
      `AggregatePersistenceRegressionTests` (4 green): payment verify-appends-transaction → Paid, refund flow via
      `UpdateAsync` → Refunded, notification `Send()` appends a delivery attempt, `Service.AddPriceTier` on an
      existing service.

## 4. Follow-ups (documented, not in this change)
- [ ] 4.1 Migrate multi-field value objects (`Money`, `Price`) to EF Core **complex types** once nullable
      complex types are available (EF 10+). Tracked in `booking-data-and-migration-hygiene`.

## Pre-existing failures observed during validation (NOT caused by this change — catalogued for triage)
- `The property 'Booking.TotalPrice#Price.BookingId' is part of a key and so cannot be modified` — a
  `Booking.TotalPrice` (`Price`) owned-key mapping defect surfacing in Payout/Earnings queries. Separate fix
  (candidate for `financial-ledger-and-settlement` / `booking-data-and-migration-hygiene`).
- Gateway/harness gaps in payment + notification sends (BadRequest/`Success=false` with no real gateway;
  aggregate-state and auth-setup mismatches). Separate from this defect.
