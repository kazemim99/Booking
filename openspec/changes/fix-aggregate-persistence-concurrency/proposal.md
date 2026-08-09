# Proposal: fix-aggregate-persistence-concurrency

## Why

While unblocking the C2 payment-reconciliation convergence write, empirical reproduction (a Testcontainers
integration test with EF command logging) exposed a **production-breaking persistence defect** that was
previously — and **incorrectly** — diagnosed as a "`Version` optimistic-concurrency-token defect."

The `Version` token is **not** the cause. The real cause is that **updating a materialized aggregate that has
an owned child collection fails with `DbUpdateConcurrencyException` ("expected 1 row, affected 0")** because a
newly-added owned child is tracked as `Modified` instead of `Added`, producing a **phantom `UPDATE` of a row
that does not exist yet**.

Concretely, verifying/capturing/refunding a `Payment` (load → domain method that appends a `Transaction` → save)
threw on every call. The same latent defect exists for `Notification.DeliveryAttempts` (recorded on every send
attempt) and `Service.PriceTiers` (added via Service CRUD).

Evidence (captured SQL + change-tracker dump, `Payment.VerifyPayment`):
- `[loaded] Version current=2 original=2` → after mutate `current=3 original=2` — the `Version` token WHERE
  (`Version = 2`) was **correct** and would have matched.
- The failing command was `UPDATE "ServiceCatalog"."PaymentTransactions" … WHERE "Id" = <new-guid>` for a
  Transaction the change tracker held as `State=Modified` — a row that does not exist → **0 rows affected**.
- EF also raised `DuplicateDependentEntityTypeInstanceWarning` ×3 because the owned `Money` value object was
  **instance-aliased** across `Payment.Amount`, `Payment.PaidAmount`, and `Transaction.Amount`.

## Root cause (three compounding defects)

1. **Owned-collection child key not `ValueGeneratedNever`.** `Transaction.Id` (and `DeliveryAttempt.Id`,
   `PriceTier.Id`) is generated in the domain (`Guid.NewGuid()`), but the EF mapping left the key as
   store-generated. EF's graph-attach heuristic ("key value is set ⇒ entity already exists") therefore marked a
   newly-appended child as `Modified` → phantom `UPDATE` → `DbUpdateConcurrencyException`. **This is the primary
   defect.** (The codebase already applies `ValueGeneratedNever` to `ServiceOption.Id` and `GalleryImage.Id` for
   exactly this reason — the pattern was simply missed on these three collections.)

2. **Owned value-object (`Money`) instance aliasing.** `Payment.VerifyPayment`/`Capture`/`ProcessCharge` did
   `PaidAmount = Amount` and passed the same `Amount` instance into `Transaction.Create*`, so one CLR `Money`
   instance was owned by multiple navigations. EF owned entities require reference-distinct instances per owner;
   sharing corrupts the identity map (the `DuplicateDependentEntityTypeInstanceWarning`) and compounds the
   mis-tracking.

3. **`UpdateAsync` re-stamps tracked graphs.** `PaymentWriteRepository.UpdateAsync` (and the shared
   `EfRepositoryBase.UpdateAsync`) unconditionally called `Context.Update(entity)`. On an **already-tracked**
   aggregate (the normal load-then-mutate flow used by `RefundPaymentCommandHandler` and
   `CapturePaymentCommandHandler`) this re-stamps the whole graph `Modified`, forcing even a correctly-`Added`
   new child back to `Modified` — re-introducing the phantom `UPDATE`.

## What Changes

- **`Transaction.Id`, `DeliveryAttempt.Id`, `PriceTier.Id` → `ValueGeneratedNever()`** in their EF
  configurations, matching the existing `ServiceOption`/`GalleryImage` pattern. (Primary fix.)
- **De-alias owned `Money`**: add `Money.Clone()` and assign reference-distinct instances at every owned slot
  (`Transaction` constructor defensively copies; `Payment` self-assignments use `Amount.Clone()`).
- **Tracked-safe `UpdateAsync`**: `UpdateAsync` becomes a no-op for already-tracked entities (change tracking
  already captured the mutation, including new children as `Added`); it only attaches genuinely-detached
  entities. Applied to `PaymentWriteRepository` and the shared `EfRepositoryBase`.

## Non-goals / explicitly NOT changed

- The `Version` optimistic-concurrency token is **left as-is** — reproduction proved it works correctly on the
  tracked path (the only path production uses after this fix). The earlier "Version defect" finding is
  **retracted** and corrected here.
- `Booking` is **not affected**: its only owned collections are `Services` (mapped `ToJson`, a document — no
  child rows) and `History` (commented out / unmapped). Booking cancel/reschedule/complete never append
  relational owned children, so they never hit this defect. The "Payment/Booking" framing was precautionary;
  only `Payment` (and `Notification`, `Service`) were actually broken.

## Capabilities

### New Capabilities
- `aggregate-persistence-integrity`: updating a materialized aggregate that appends owned child entities SHALL
  persist those children as inserts and SHALL NOT raise a false optimistic-concurrency failure; owned
  value-object instances SHALL never be shared across owner navigations; repository `UpdateAsync` SHALL be safe
  on already-tracked aggregates.

## Impact

- **Fixes (were broken in production):** `Payment` verify / capture / refund; `Notification` delivery-attempt
  recording; `Service` price-tier edits on existing services. Unblocks C2 reconciliation convergence.
- **Migration:** none. `ValueGeneratedNever` is a model-side annotation only — the `Id` columns are unchanged
  (client-generated GUIDs already). No schema migration, no data backfill.
- **Rollback:** revert the three config lines + `Money.Clone` usages + `UpdateAsync` guards. No DB state to undo.
- **Risk:** low and contained — no schema change; domain unit tests (309) unchanged and green; behavior is
  strictly more correct (inserts that used to throw now succeed; tracked updates unchanged).
