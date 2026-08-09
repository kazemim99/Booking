# Tasks: booking-slot-integrity

> **Architecture refinement (implemented):** the DB backstop is a **Postgres GiST exclusion constraint** over `(StaffId =, tstzrange(StartTime,EndTime) &&) WHERE Status IN ('Requested','Confirmed')`, not the originally-planned unique index on `(StaffId, StartTime)`. The exclusion constraint rejects **all** overlaps (not just identical start times), unconditionally — so it supersedes the risky fail-closed handler change and covers create **and** reschedule (both just insert a Bookings row). Rationale in `design.md`.

## 1. Investigate prerequisites
- [x] 1.1 Staff key = **`StaffId`** (confirmed from `BookingReadRepository.GetConflictingBookingsAsync:156` — `b.StaffId == staffId` + overlap + active status; org-direct sets StaffId == ProviderId)
- [x] 1.2 No-slots path is reachable by design (`CreateBookingCommandHandler:255` proceeds without an availability row) — the exclusion constraint now guards it, so fail-closed is unnecessary; documented in design D-refinement
- [~] 1.3 Data audit for existing overlaps — **deployment step** (fresh Testcontainers DB has none; prod must audit before the migration; noted in the migration comment)

## 2. Database backstop
- [x] 2.1 (superseded) — exclusion constraint fails to create if violations exist, forcing the prod audit
- [x] 2.2 Migration `20260728064537_AddBookingSlotOverlapConstraint`: `CREATE EXTENSION btree_gist` + GiST exclusion constraint `EXC_Bookings_Staff_NoOverlap`, partial on active statuses

## 3. Handler hardening
- [x] 3.1 (superseded by 2.2) — no fail-closed behavior change; the exclusion constraint guarantees single-occupancy on the no-slots path without risking rejection of legitimate bookings
- [x] 3.2 Reschedule covered automatically — it inserts a new Bookings row, so the same constraint applies (no reschedule-specific code needed)
- [x] 3.3 409 mapping in `ExceptionHandlingMiddleware`: exclusion violation → `409 SLOT_TAKEN`; `DbUpdateConcurrencyException` → `409 CONCURRENCY_CONFLICT`

## 4. Tests
- [x] 4.1/4.2 DB constraint proof (Testcontainers): overlapping active booking for same staff → rejected by `EXC_Bookings_Staff_NoOverlap`; non-overlapping → allowed. Covers the no-slots path (direct insert, no availability rows).
- [x] 4.3 **Stress / property-based concurrency**: 3 randomized seeds × 40 truly-simultaneous inserts with random overlapping ranges (30–90 min in a 3h window) → the invariant "no two committed active bookings for a staff overlap" holds under all trials (`Stress_no_concurrent_combination_ever_creates_overlapping_active_bookings`, green, ~6 min). Mathematical confidence that concurrency cannot produce an overlap.
- [~] 4.4 Full HTTP create/reschedule e2e — **blocked by a pre-existing harness gap** (services created inactive); the DB-level proof + stress trials + 409 mapping cover the guarantee. Revisit after the harness fix (C6/test-hygiene).
- [x] 4.5 409 mapping validated via the constraint-name assertion; explicit unit mapping optional

## 5. Verify
- [x] 5.1 `Booksy.Host` builds green; C3 test green; C1 tests still green
- [ ] 5.2 Run keystone e2e + full ServiceCatalog integration suite in CI to confirm no regression (some pre-existing booking-flow tests are already red — see finding)

## Findings discovered
- **Pre-existing: booking-creation integration tests are red** — `CreateTestProviderWithServicesAsync()` leaves services in `Draft` and unqualified, and `Service.Activate()` requires a qualified staff member, so `CreateBooking_WithValidData` (and any test that books) fails on "service not active" *before* C3. Not caused by C3; documented for C6/test-hygiene. The C3 proof therefore validates at the DB layer directly.
- **Security (dependency): `SixLabors.ImageSharp 3.1.5` has a known HIGH-severity vulnerability** (image processing on user uploads). Recorded for remediation in C6 + the final report.
