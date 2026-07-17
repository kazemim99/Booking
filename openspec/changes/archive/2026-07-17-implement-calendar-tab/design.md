## Context

Implements `provider-calendar` (spec in this change) per the IA's Calendar-tab definition. All data comes from live-verified surfaces: `GET /Bookings/provider/{id}?from&to` (enveloped, enriched app-side with service names) and the booking mutations. No backend changes.

## Goals / Non-Goals

**Goals:** a fast week-strip + day-timeline calendar; booking actions from the calendar with Home-identical semantics; create-on-selected-day; one shared nav bar.
**Non-Goals:** block-time/availability overrides (no backend concept); full week-grid and per-staff columns (multi-staff emphasis comes later); Jalali digits (calendar-package follow-up); push/live updates (manual refresh + refetch-on-navigation, consistent with MVP polling decision).

## Decisions

- **D1 — Calendar lives in the home feature module.** It shares the repository, booking row model, enrichment, and action handlers. Extraction into `features/calendar/` is mechanical if it grows (same reasoning as the composer).
- **D2 — Week strip + day timeline, not a positioned grid.** A time-gutter list per selected day delivers the IA's "planning" value with none of the layout risk of a positioned-grid timeline (overlaps, drag targets). The strip carries the week context (badges per day). The full grid is a later enhancement, not an MVP requirement.
- **D3 — `CalendarCubit` owns a week window.** State: `weekStart`, `selectedDay`, `bookingsByDay` (map), status/error. One fetch per visible week (`fetchBookings(from: weekStart, to: weekStart+7d)`), monotonic sequence guard for week paging, `refresh()` after mutations/composer returns. Day selection is pure state (no fetch).
- **D4 — Shared `ProviderNavBar` + `context.go`, not `StatefulShellRoute` yet.** With two real tabs, a full shell refactor buys per-tab stack preservation at the cost of restructuring scaffolds/FABs mid-stream. Tab switches rebuild pages (Home re-fetches via its cubit; Calendar refetches its week) — acceptable at current data sizes and consistent with poll-on-foreground. The shell lands when Clients/More make deep per-tab stacks real. Documented trade-off against the IA's per-tab stacks.
- **D5 — Booking action sheet reuses `HomeCubit` mutation methods** via a lightweight handler passed by the page (the calendar creates its own `HomeCubit`? No — the mutations live on `HomeRepository`; the calendar cubit exposes the same four action methods delegating to the repository, mirroring `HomeCubit`'s `_mutate` incl. offline refusal). Identical failure mapping (`BOOKING_DEPOSIT_NOT_PAID` → Persian) for free.
- **D6 — Composer initial date via query param** (`/booking/new?date=YYYY-MM-DD`): deep-linkable, no route-extra object coupling. `ComposerCubit` accepts `initialDate` and seeds `state.date`.

## Risks / Trade-offs

- **[Tab switches lose scroll/selection state]** (D4) → acceptable now; StatefulShellRoute later.
- **[Badges need the whole week's bookings]** → one range call per week is small (a provider's week), already enriched; no N+1.
- **[Stale availability display]** (flagged in the composer change) also affects calendar-initiated composing → unchanged behavior, tracked once.

## Migration Plan

1. Repository range method (+ fetchSnapshot reuse) + tests.
2. CalendarCubit + tests.
3. ProviderNavBar extraction (Home keys preserved → existing widget tests keep passing) + CalendarPage + router + composer date.
4. Widget tests; analyze; full suite.

## Open Questions

- None blocking. Follow-ups tracked: block-time backend concept, week-grid/per-staff columns, Jalali digits, StatefulShellRoute shell.
