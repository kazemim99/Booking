## Context

`BookingConfiguration` has its indexes commented out (only `ServiceId` + `IndividualProviderId` active). ServiceCatalog has two migration folders with overlapping "Init" migrations; the applied set in prod is recorded in `__EFMigrationsHistory`. `Reschedule` returns a new `Requested` booking; payment carry-over is unconfirmed.

## Goals / Non-Goals

**Goals:** index-supported booking queries; one clean migration history; no lost payment linkage on reschedule.
**Non-Goals:** schema redesign; changing booking semantics beyond payment carry-over.

## Decisions

- **D1 — Restore indexes via a new migration, not by editing history.** Add `CustomerId`, `ProviderId`, `StaffId`/`IndividualProviderId`, `Status`, and the filtered availability composite. Coordinate with C3 so the unique slot index and these non-unique indexes don't duplicate. *Rationale:* forward-only migrations are safe; editing old migrations is not.
- **D2 — Migration-history consolidation is audit-first.** Read `__EFMigrationsHistory` on each environment; determine which folder's migrations are applied; retire the stray folder only if provably unapplied/duplicated. If ambiguous, **leave as-is and document** rather than risk a broken migration chain. *Rationale:* migration corruption is high-blast-radius; correctness over tidiness.
- **D3 — Reschedule carries payment.** `Booking.Reschedule` copies `PaymentInfo` (paid/deposit/refund amounts, intent ids) to the new booking so a paid-then-rescheduled booking keeps its payment; the old booking retains a reference. If product prefers re-collection, document that instead. *Coordinates with C2's payment model.*

## Risks / Trade-offs

- [Index creation locks tables] → Mitigation: create online/`CONCURRENTLY`; off-peak.
- [Migration consolidation breaks the chain] → Mitigation: audit-first; skip if ambiguous.
- [Payment carry-over interacts with the new intent model] → Mitigation: sequence after C2 so the payment fields are stable; test the paid→reschedule path.

## Migration Plan

Forward-only index migration; history consolidation only if the audit is conclusive. Rollback: drop indexes.

## Open Questions

- Reschedule payment semantics (carry vs re-collect) — technical default is carry; confirm if product wants re-collection.
