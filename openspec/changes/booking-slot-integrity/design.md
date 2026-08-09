## Context

`CreateBooking` and `Reschedule` run in one READ COMMITTED transaction (single `SaveChanges` + commit). The active guard is optimistic concurrency on the per-slot `ProviderAvailability.Version` token: two writers touching the same slot row collide and the loser's `SaveChanges` throws `DbUpdateConcurrencyException`, rolling back its whole transaction (including the booking insert). This works **only when a slot row exists** and it surfaces as an unhandled exception. The `Bookings` table has no unique constraint.

## Goals / Non-Goals

**Goals:** single-occupancy of a (staff, time) slot on every path; clean 409 on contention; a DB backstop independent of availability-row presence.
**Non-Goals:** replacing the availability model; changing slot granularity; the customer UI (C4 consumes the 409).

## Decisions

- **D1 — Fail closed on missing availability.** Replace the "log warning and return" branch (`CreateBookingCommandHandler.cs:255`) with a rejection when no slot covers the time, behind `Booking:RequireAvailabilitySlot`. *Rationale:* the no-slots path is the only route to an unguarded double-booking. *Prerequisite:* confirm slots are generated for all bookable windows (else legitimate bookings break) — validated before enabling.
- **D2 — DB filtered unique index as backstop.** `CREATE UNIQUE INDEX … ON Bookings (IndividualProviderId/StaffId, StartTime) WHERE Status IN ('Requested','Confirmed')`. *Rationale:* defense-in-depth independent of availability rows and application logic; the database becomes the final arbiter. *Note:* must choose the correct staff column — `IndividualProviderId` (hierarchy) is the performing staff; align with how conflicts are actually keyed.
- **D3 — Contention → 409.** Catch `DbUpdateConcurrencyException` and unique-violation (`DbUpdateException` with the index name) in the handler (or a global exception mapper) → `ConflictException("slot just taken")` → 409 with a machine-readable code so the client can refresh availability.
- **D4 — Reschedule parity.** Reschedule already releases the old slot and books the new one; apply the same fail-closed + 409 mapping to its "mark new slot booked" step so it cannot create an unguarded booking either.

## Decisions discovered during implementation (refinement)

- **D5 — Use a GiST EXCLUSION constraint, not a unique index (supersedes D2).** A unique index on `(StaffId, StartTime)` only catches *identical* start times; two bookings at 10:00 and 10:15 for a 30-min service overlap but slip past it. The correct DB-level guarantee is a Postgres exclusion constraint `EXCLUDE USING gist ("StaffId" WITH =, tstzrange("StartTime","EndTime") WITH &&) WHERE "Status" IN ('Requested','Confirmed')` (migration `AddBookingSlotOverlapConstraint`; needs `btree_gist`). It mirrors the app conflict check exactly and rejects **all** overlaps. *Verified:* `BookingSlotIntegrityTests` proves an overlapping active booking for the same staff is rejected (error carries `EXC_Bookings_Staff_NoOverlap`) and a non-overlapping one is allowed.
- **D6 — The exclusion constraint supersedes fail-closed (D1) and reschedule-specific code (D4).** Because the constraint enforces single-occupancy on *every* insert unconditionally — including the no-slots path and reschedule (which just inserts a new Bookings row) — the risky behavioral change (rejecting bookings when no availability row exists, which could block legitimate bookings) is **not implemented**. The database is the single arbiter; the availability-slot optimistic concurrency remains as an additional early guard. *Rationale:* stronger guarantee, smaller/less-risky diff, no new failure mode for providers who book outside generated availability.
- **D7 — 409 mapping is global, not per-handler (refines D3).** The save is deferred to the `TransactionBehavior` commit (outside the handler), so the violation surfaces there. Mapped centrally in `ExceptionHandlingMiddleware`: exclusion violation → `409 SLOT_TAKEN`; `DbUpdateConcurrencyException` → `409 CONCURRENCY_CONFLICT`. Covers create and reschedule uniformly.

## Risks / Trade-offs

- [Fail-closed rejects legitimate bookings if slots aren't pre-generated] → Mitigation: flag-gated; verify generation coverage first; optionally auto-generate the slot on demand within the same transaction rather than reject.
- [Unique index conflicts with existing duplicate data] → Mitigation: pre-migration audit query; cleanup migration; create index `CONCURRENTLY`.
- [Choosing the wrong staff column makes the index too strict/loose] → Mitigation: derive from `GetConflictingBookingsAsync`'s actual key; test multi-service and org-direct bookings.

## Migration Plan

1. Audit for existing overlapping active bookings; 2. cleanup migration if any; 3. add the filtered unique index online; 4. enable fail-closed via flag after generation coverage is confirmed. Rollback: drop index + flag off.

## Open Questions

- On missing slot: **reject** vs **auto-generate the slot then occupy it**. Auto-generate is more forgiving but must respect business-hours/holidays validation. Lean: reject first (safe), revisit auto-generate as a follow-up. **(Implementation decision; no product input required unless providers rely on booking outside generated availability — flag for confirmation if evidence appears.)**
