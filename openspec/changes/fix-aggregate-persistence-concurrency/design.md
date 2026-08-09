# Design: fix-aggregate-persistence-concurrency

## 1. Root cause — established empirically, not by inspection

The earlier session recorded this as a "`Version` concurrency-token defect (Critical-candidate)". That
hypothesis was **wrong**. It was overturned by a dedicated reproduction test
(`ConcurrencyTokenReproTests`, Testcontainers PostgreSQL) that exercised the exact production flow with EF
command logging (`EnableSensitiveDataLogging` + `LogTo`) and a change-tracker dump.

### 1.1 The reproduction (Payment.VerifyPayment, tracked path)

```
insert Payment via repo.AddAsync + uow.CommitAsync   → DB Version = 2 (matches in-memory)
load via GetByAuthorityAsync (tracked)
  change tracker: Payment.Version current=2 original=2       ← correct
payment.VerifyPayment(...)   (appends a Verification Transaction)
  change tracker: Payment.Version current=3 original=2       ← correct
CommitAsync → SaveChanges → DbUpdateConcurrencyException: expected 1 row, affected 0
```

Generated SQL batch (parameters shown):

```sql
-- Command A (the one that fails):
UPDATE "ServiceCatalog"."PaymentTransactions" SET ... WHERE "Id" = @p16;   -- @p16 = NEW transaction guid
-- Command B (correct, never reached):
UPDATE "ServiceCatalog"."Payments" SET ..., "Version"=@p22 /*=3*/ WHERE "PaymentId"=@p26 AND "Version"=@p27 /*=2*/;
```

`ex.Entries` on the thrown exception pointed at the **`Transaction`** entity in `State=Modified`, not the
`Payment`. So:

- The `Payments` UPDATE (Command B) used `WHERE Version = 2`, DB had `2` — it **would have matched**. The
  `Version` token is fine.
- The failure is **Command A**: a phantom `UPDATE` of a `PaymentTransactions` row whose `Id` is a brand-new
  GUID that does not exist yet → 0 rows.

### 1.2 Why the new child is `Modified` instead of `Added`

`Transaction : Entity<Guid>` sets `Id = Guid.NewGuid()` in its constructor. The EF mapping
(`PaymentConfiguration`, `OwnsMany(p => p.Transactions)`) declared `HasKey("Id")` but **did not** declare the
key `ValueGeneratedNever()`. EF therefore treats the GUID key as store-generated and applies its graph-attach
heuristic: when it discovers an untracked entity reachable from a tracked parent and the key value is **set**
(non-default), it assumes the entity **already exists** and assigns `Modified`. A client-generated GUID is
always "set", so every newly-appended child is mis-classified.

This is a known EF Core behavior and the codebase already guards against it elsewhere:
`ServiceOption.Id` → `.ValueGeneratedNever().IsConcurrencyToken(false)`, `GalleryImage.Id` →
`.ValueGeneratedNever()` ("Important: Since we generate it in code"). The pattern was simply missing on
`Transaction`, `DeliveryAttempt`, and `PriceTier`.

### 1.3 The compounding owned-value-object aliasing

`Payment.VerifyPayment` did `PaidAmount = Amount;` and `Transaction.CreateVerification(Amount, …)` — the same
`Money` CLR instance became owned by `Payment.Amount`, `Payment.PaidAmount`, and `Transaction.Amount`. EF owned
entities must be reference-distinct per navigation; sharing raised
`DuplicateDependentEntityTypeInstanceWarning` and produced `Added`+`Deleted` churn on `PaidAmount#Money`. This
alone did not cause the 0-row failure (removing the aliasing did not fix the test), but it is a real latent
corruption that must be removed for a clean change tracker and to avoid "two store changes" on a single value.

### 1.4 The `UpdateAsync` re-stamp

`RefundPaymentCommandHandler` and `CapturePaymentCommandHandler` load the payment tracked, mutate it, then call
`_paymentRepository.UpdateAsync(payment)`, which called `Context.Update(payment)`. `Update()` re-stamps the
entire reachable graph to `Modified` for every entity whose key is "set". With `ValueGeneratedNever` now making
keys always "set", `Update()` would turn the correctly-`Added` new `Transaction` back into `Modified` — so the
`ValueGeneratedNever` fix is necessary **but not sufficient** unless `UpdateAsync` stops re-stamping tracked
graphs. The two fixes are co-dependent.

## 2. Options considered

| Option | Verdict |
|---|---|
| **A. Blame the `Version` token; switch to `xmin`/rowversion or drop `HasDefaultValue`.** | **Rejected.** Reproduction proved the token is correct on the tracked path. Changing it would be a schema-touching change that fixes nothing and risks the detached path. |
| **B. Map `Money`/child collections as EF Core *complex types* (no identity, no tracking).** | **Rejected for now.** The idiomatic long-term fix, but `Payment.Fee` is `Money?` and **nullable complex types are not supported in EF Core 9** (the pinned runtime). Broad blast radius across every value-object mapping. Documented as a future migration. |
| **C. `ValueGeneratedNever` on client-GUID child keys + de-alias `Money` + tracked-safe `UpdateAsync`.** | **Chosen.** Directly removes the primary defect, matches the pattern already used for `ServiceOption`/`GalleryImage`, needs **no migration**, and is provably correct (all reproduction paths green). |

## 3. The fix (chosen: Option C)

1. **`ValueGeneratedNever()`** on `Transaction.Id` (`PaymentConfiguration`), `DeliveryAttempt.Id`
   (`NotificationConfiguration`), `PriceTier.Id` (`ServiceConfiguration`). — primary fix.
2. **`Money.Clone()`** (reference-distinct copy) + de-aliasing: `Transaction` constructor defensively clones its
   incoming `Money`; `Payment.VerifyPayment`/`Capture`/`ProcessCharge` assign `Amount.Clone()` to `PaidAmount`.
3. **Tracked-safe `UpdateAsync`**: no-op when `Context.Entry(entity).State != Detached`; only attach detached
   entities. Applied to `PaymentWriteRepository.UpdateAsync` and `EfRepositoryBase.UpdateAsync`.

### Not fixed because not broken
- `BusinessHours.Breaks`: child key is a **store-generated `int`** (`ValueGeneratedOnAdd`); new breaks get
  `Id=0` (default) → correctly `Added`. No change.
- `Booking.Services` (`ToJson`) and `Booking.History` (unmapped): no relational child rows. No change.

## 4. Affected aggregates / blast radius

| Aggregate | Owned collection | Child key | Status |
|---|---|---|---|
| Payment | Transactions | client GUID | **fixed** (was broken: verify/capture/refund) |
| Notification | DeliveryAttempts | client GUID | **fixed** (was broken: every send attempt) |
| Service | PriceTiers | client GUID | **fixed** (was broken: edit tiers on existing service) |
| Service | Options | client GUID | already correct (`ValueGeneratedNever`) |
| Provider | GalleryImages | client GUID | already correct (`ValueGeneratedNever`) |
| BusinessHours | Breaks | store int | correct by construction |
| Booking | Services / History | JSON / unmapped | not applicable |

## 5. Migration strategy

**No database migration.** `ValueGeneratedNever` changes only the EF model's opinion of who supplies the key
value; the `Id` columns are already client-populated GUIDs with no DB default/identity/sequence, so the physical
schema is byte-for-byte unchanged. `DbContext` already suppresses `PendingModelChangesWarning`. No data
backfill; existing rows are untouched.

## 6. Rollback strategy

Pure code revert (no DB state to reverse):
- Remove the three `.ValueGeneratedNever()` lines.
- Revert `Money.Clone()` and its call sites.
- Restore `UpdateAsync` to `Context.Update(entity)` / `DbSet.Update(entity)`.

Rolling back re-introduces the defect; there is no data-corruption risk from either direction.

## 7. Test strategy

- **Reproduction/regression (integration, Testcontainers):** load-mutate-append-save for `Payment` verify (adds
  a Transaction) must succeed and reach `Paid`; production refund flow (tracked load + `UpdateAsync` + commit)
  must succeed and reach `Refunded`. (Delivered as `PaymentAggregatePersistenceTests`, promoted from the
  throwaway `ConcurrencyTokenReproTests` diagnostic.)
- **Notification:** recording ≥2 delivery attempts on a loaded notification then saving must persist both
  attempts (no `DbUpdateConcurrencyException`).
- **Service:** adding a price tier to an existing (persisted, reloaded) service must insert the tier.
- **Regression guard:** full domain unit suite (309) stays green; a baseline diff of the
  Payments/Notifications/ServiceManagement/Bookings integration filter shows **no newly-failing test** vs. the
  pre-change baseline (pre-existing reds are tracked separately — e.g. the `Booking.TotalPrice#Price.BookingId`
  owned-key mapping issue and gateway/harness gaps).

## 8. Architectural rationale

- **Correctness first, minimal blast radius.** The chosen fix removes the exact mis-tracking the reproduction
  isolated, using a pattern already present in the codebase, with zero schema/data risk. That is preferable to a
  speculative concurrency-model overhaul (`xmin`) that the evidence does not justify.
- **Defense in depth.** The three fixes are layered: `ValueGeneratedNever` makes new children insert correctly;
  de-aliasing keeps the change tracker's owned-entity identity map clean; tracked-safe `UpdateAsync` prevents a
  caller from re-breaking it. Any one missing would leave a live failure path (proven: `ValueGeneratedNever`
  alone still failed refund/capture through `UpdateAsync`).
- **Future direction (documented, not done):** migrate multi-field value objects (`Money`, `Price`) to EF Core
  **complex types** once nullable complex types are available (EF Core 10+). That eliminates the owned-entity
  identity-aliasing class entirely and is the true long-term home for value objects. Tracked as a follow-up in
  `booking-data-and-migration-hygiene`.
