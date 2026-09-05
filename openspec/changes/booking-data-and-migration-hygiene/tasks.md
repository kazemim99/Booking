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
- [x] 0.3 **DONE, verified 2026-08-24.** `dotnet list package --vulnerable --include-transitive` now reports
      **zero** vulnerable packages for every project in the solution except two moderates
      (`OpenTelemetry.Exporter.Jaeger`, `.OpenTelemetryProtocol`) in `Booksy.Infrastructure.Monitoring`, which
      `project.md` records as dead code referenced by no project (`grep -r "ProjectReference.*Monitoring"`
      returns nothing) — inert in the running system. `AutoMapper` is at 15.1.3 (was 15.0.1 HIGH,
      GHSA-rvv3-g6hj-g44x), `MimeKit` at 4.17.0 (was 4.14.0 moderate, GHSA-g7hc-96xr-gvvx). Both fixed in an
      earlier pass of this same test-hardening effort; this task just confirms nothing regressed.

## 1. Indexes
- [x] 1.1 **DONE, migration `20260905083540_RestoreBookingIndexes`.** Restored `CustomerId`, `ProviderId`,
      `StaffId`, `Status` as plain single-column indexes — these had been commented out entirely (only
      `ServiceId`/`IndividualProviderId` were live), so `my-bookings`, provider-dashboard, and history queries
      full-scanned the table.

      **Deliberately did NOT restore** the `(StaffId, Status)` composite or the further composite filtered to
      `WHERE Status IN ('Requested', 'Confirmed')` for availability/conflict checks. Two independent reasons:
      (a) the original text used SQL Server bracket syntax (`[Status]`) in `HasFilter`, which is invalid on
      PostgreSQL and would have failed to apply at host startup — a real outage risk if uncommented as-is,
      confirmed against this codebase's own working convention (`"ColumnName" IS NOT NULL`, double-quoted, in
      `NotificationConfiguration`/`ProviderAvailabilityConfiguration`); (b) more importantly, it is now
      unnecessary — ADR-004 (`booking-slot-integrity`, migration `20260728064537_AddBookingSlotOverlapConstraint`,
      archived 2026-08-19) added a GiST exclusion constraint over
      `(StaffId WITH =, tstzrange(StartTime, EndTime) WITH &&) WHERE Status IN ('Requested', 'Confirmed')` —
      verified against `BookingReadRepository.GetConflictingBookingsAsync` to be the *exact* predicate that
      method filters on. That GiST index already serves the query, and serves it better than a B-tree composite
      could (it also covers the time-range comparison, which `StaffId`+`Status` alone cannot). Adding a parallel
      composite would have been pure write overhead on every booking mutation with no query left to serve.

      **Verified applies cleanly**: `Booksy.Host.CompositionTests` (15/15) boots the real Host — migrating both
      schemas — against a fresh Testcontainers Postgres.
- [x] 1.2 **DONE**, folded into 1.1 — `BookingConfiguration`'s index block now matches the migration exactly
      (see the code comment there for the full rationale, not just the summary above).

## 2. Migration history
- [x] 2.1 **DONE — and the premise was wrong.** `dotnet ef migrations list` was run directly against the real
      project/DbContext (not inferred from the file tree): it returns **one single, correctly-ordered,
      chronologically-interleaved history of 23 migrations**, including all four files under
      `Infrastructure/Persistence/Migrations/` (`AddOwnerNamesToProvider`/`2`/`3`, `RemoveStaff`) sitting exactly
      where their timestamps place them among the `Infrastructure/Migrations/` lineage. EF Core discovers
      migrations by scanning the whole compiled assembly for `[Migration]`-attributed classes tied to the target
      `DbContext` — it does not care about namespace or folder. This is not two competing/ambiguous histories;
      it is one coherent history split across two directories for what looks like an accidental tooling
      `--output-dir` default change partway through the project's life. A full Host boot (`Booksy.Host.
      CompositionTests`, 15/15) confirms all of it — the old folder's migrations included — applies cleanly
      against a fresh database.
- [x] 2.2 **DONE, per the task's own conditional.** The instruction was "consolidate to one folder only if
      provably unapplied/duplicate; otherwise document the dual-folder state and leave intact." 2.1 proved
      neither condition (not unapplied, not duplicate — every migration is live and necessary), so the correct
      action is the documentation half: added a code comment on
      `Persistence/Migrations/20251223143438_RemoveStaff.cs` (the newest file in that folder, where someone
      confused by the split would most likely land) explaining the finding, so a future contributor does not
      re-open this as a suspected defect or, worse, delete the folder thinking it is dead. **Not done, and
      deliberately not attempted**: physically moving the four files into the other folder. That is a pure
      file/namespace relocation with zero behavioural effect — safe, but it is unrequested churn on a
      proven-working migration history that this task's own instructions do not ask for once "leave intact" is
      the applicable branch.

## 3. Reschedule payment carry-over
- [x] 3.1 **DONE — already implemented by `fix-reschedule-membership-staff` (archived 2026-08-19), confirmed
      by direct code read.** `Booking.Reschedule` copies `PaymentInfo` to the successor booking:
      `PaymentInfo = PaymentInfo.Clone(), // Transfer payment state (deposit, intents)` — alongside
      `TotalPrice.Clone()` and `Policy.Clone()`, the same defensive-copy pattern ADR-005 requires for every
      owned value object handed to a new aggregate instance. The product decision this task asked about
      ("carry the deposit to the successor, or re-collect?") was made during that change: carry it over. This
      is exercised by a real, passing scenario — `RescheduleBooking.feature`'s "Reschedule a booking held
      against an organization member", which explicitly asserts "the original booking still has its own policy
      and payment information" — one of the five Reqnroll feature files confirmed fully green in the
      2026-08-24 test-hardening pass.
- [x] 3.2 **DONE — already implemented, confirmed by direct code read.** `Booking.RescheduledToBookingId` is
      set on the original booking (`RescheduledToBookingId = newBooking.Id;`) at the same point in
      `Booking.Reschedule` that clones the payment/price/policy state.

## 4. Tests + verify
- [x] 4.1 **DONE — see 3.1.** "Reschedule carries payment (no orphaned payment)" is exactly what
      `RescheduleBooking.feature`'s membership-booking scenario asserts, and it passes.
- [ ] 4.2 **Not verifiable in this environment.** A real perf smoke test (confirming the query planner actually
      chooses the new indexes over a sequential scan) needs `EXPLAIN ANALYZE` against a table with realistic
      production-scale row counts; a fresh Testcontainer has none. Structural correctness is verified (1.1); the
      *effectiveness* claim is not measured here and should not be assumed from that alone.
- [x] 4.3 **DONE.** Build green; 820/820 unit + architecture tests pass; `Booksy.Host.CompositionTests` 15/15
      (proves the migration applies against a real database, both schemas).
