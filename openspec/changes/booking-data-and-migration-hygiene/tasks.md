# Tasks: booking-data-and-migration-hygiene

> Forward-only migrations. Coordinate indexes with C3. Sequence payment carry-over after C2.

## 0. Owned-entity persistence defects + dependency CVEs (DISCOVERED + fixed while in-area)
- [x] 0.1 **DONE.** `SixLabors.ImageSharp` 3.1.5 → 3.1.12 (fixes HIGH GHSA-2cmq-823j-5qj8 + moderate
      GHSA-rxmq-m78w-7wmc). Build no longer reports the ImageSharp advisory.
- [x] 0.2 **DONE (high-impact).** Fixed the `Booking.TotalPrice#Price.BookingId` owned-key defect that broke EVERY
      booking state transition (confirm/cancel/reschedule/complete) with "part of a key ... cannot be modified":
      `Booking.Id` + owned `TotalPrice` FK pinned `ValueGeneratedNever`. Also fixed `BookingHistoryEntry.Id`
      (client GUID, was `ValueGeneratedOnAdd`) → `ValueGeneratedNever` — a history entry appended on any transition
      was a phantom UPDATE (0 rows). Also `PaymentInfo` reused owned `Money` instances across the replaced/added
      owned VO → nulled columns on save; fixed by defensive `Money.Clone()` in the ctor. All the same class as
      ADR-005 (owned-entity aliasing / client-key). Proven: `BookingUpdatePersistenceTests` (confirm + cancel
      persist) + `DepositCheckoutCouplingTests` (deposit→confirm). 0 regressions (money-path filter 46→46).
- [ ] 0.3 **Remaining CVEs (need network for patched versions — remediate before GO):** `AutoMapper` 15.0.1 HIGH
      (GHSA-rvv3-g6hj-g44x), `MimeKit` 4.14.0 moderate (GHSA-g7hc-96xr-gvvx). No patched version cached offline; a
      downgrade is unsafe. Flagged for the final audit.

## 1. Indexes
- [ ] 1.1 Migration restoring booking indexes: `CustomerId`, `ProviderId`, `StaffId`/`IndividualProviderId`, `Status`, filtered availability composite — de-duplicated against C3's unique index
- [ ] 1.2 Uncomment/realign `BookingConfiguration` index definitions to match the migration

## 2. Migration history
- [ ] 2.1 Audit `__EFMigrationsHistory` across environments; map which of the two migration folders is applied
- [ ] 2.2 Consolidate to one folder only if provably unapplied/duplicate; otherwise document the dual-folder state and leave intact

## 3. Reschedule payment carry-over
- [ ] 3.1 Confirm current `Booking.Reschedule` payment behavior; make it copy `PaymentInfo` to the new booking (or document re-collection) — sequenced after C2's payment model
- [ ] 3.2 Old booking retains a reference to the new one (already `RescheduledToBookingId`)

## 4. Tests + verify
- [ ] 4.1 Integration: reschedule carries payment (no orphaned payment); migrations apply clean + on existing
- [ ] 4.2 Perf smoke: indexed columns used for `my-bookings`/provider/conflict queries
- [ ] 4.3 Build + tests green
