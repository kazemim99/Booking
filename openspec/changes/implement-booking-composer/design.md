## Context

Implements `provider-booking-composer` (spec in this change). The Home (change `implement-provider-home-core`) ships with the ⊕ create action stubbed. Backend surfaces verified in source: `POST /Bookings` (customer = JWT caller), `GET /Bookings/available-slots?providerId&serviceId&date[&staffId]` → `{availableSlots:[{startTime,endTime,durationMinutes}]}` (enveloped), `GET /Providers/{id}/staff`, `GET /Services/provider/{id}`. Blocker found: `BookingPolicy.Default.requireDeposit=true` in a payments-less product → all bookings unconfirmable (live-verified).

## Goals / Non-Goals

**Goals:** a fast one-screen composer (provider speed over wizard ceremony); bookings that are actually confirmable; reuse of the Home data layer and four-state treatments; deterministic tests including a slots race guard.
**Non-Goals:** first-class walk-in customer identity (backend gap, flagged); client picker over a customer directory (no provider-clients endpoint yet); block-time (needs an availability-override backend concept); payments.

## Decisions

- **D1 — One-screen composer, not a wizard.** Sections stack on one scrollable page (service → staff → date → slots grid → client/notes → submit). Providers compose bookings many times a day; a five-step wizard taxes exactly the users the IA promises two-tap speed. Pickers open as bottom sheets (established app pattern).
- **D2 — Backend fix = change `BookingPolicy.Default`, not the handler fallback.** `Default` is used exactly once (the CreateBooking fallback). Swapping the fallback to `Flexible` would silently change advance/cancellation windows too; changing only Default's deposit flag (`false`, 0%) is the minimal semantic correction: *the default policy of a product without payments cannot demand payment*. `Strict`/explicit policies unchanged. One domain test updated.
- **D3 — Composer lives inside the home feature** (`features/home/presentation/{cubit,pages}` + api/repo additions) because it is launched from Home, refreshes Home, and shares its data layer. When the Calendar tab lands, extraction into `features/booking/` is mechanical.
- **D4 — `ComposerCubit` with a monotonic slots-request sequence** (same stale-guard pattern as `HomeCubit`, spec-required). Slots re-fetch on service/staff/date change; selected slot cleared if absent from the new result.
- **D5 — Walk-in notes convention:** `مشتری حضوری: <name>[ — <phone>]` prepended to `customerNotes`, separated by `\n` from free-form notes. Parse-free, human-readable in any booking-details surface.
- **D6 — Submission uses the provider's own token** (customer = provider user). Accepted MVP semantics; statistics impact acknowledged and tracked with the walk-in-identity gap.

## Risks / Trade-offs

- **[Deposit default change affects all future default-policy bookings]** → Intended; verified by domain tests + live create→confirm. Explicit policies unaffected.
- **[Walk-in-as-provider pollutes customer stats]** → Accepted for MVP; flagged with the backend walk-in/identity work.
- **[Slots endpoint shape unverified live]** → Result record read from source; parsers tolerant + live verification is a task before completion.

## Migration Plan

1. Backend: flip Default deposit → run domain unit tests → restart local host.
2. Flutter: endpoints + api/repo methods (+ parser tests) → ComposerCubit (+ tests) → page + ⊕ wiring (+ widget tests).
3. Live verify: compose → create → **confirm succeeds** (was impossible before) → Home shows the booking.

## Open Questions

- None blocking. Future: first-class walk-in identity; block-time (availability-override concept); client directory picker.
