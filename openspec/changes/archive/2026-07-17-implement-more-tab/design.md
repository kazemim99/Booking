## Context

Implements `provider-more-hub`, the last IA nav destination. All data comes from endpoints already live-verified by earlier changes: `GET /Services/provider/{id}` (composer catalog), `GET /Providers/{id}/staff` (composer), `GET /Bookings/statistics` (Home maturity signals). No backend work.

## Goals / Non-Goals

**Goals:** complete the bottom nav; expose the three read surfaces cheaply; keep the hub itself network-free.
**Non-Goals:** any editing (profile/hours/gallery/service/staff CRUD — each its own change); notification/language settings; push; Jalali digits.

## Decisions

- **D1 — Hub is session-only.** Name/phone/status come from the `AuthBloc` session; no cubit, no fetch, instant render. Coming-soon rows are visibly disabled (muted, no chevron action) rather than hidden — the IA's browsable-path promise.
- **D2 — Three small cubits in one file** (`more_cubits.dart`: `InsightsCubit`, `ServicesCubit`, `StaffCubit`) — identical load/retry shape over different repository calls. A generic list-cubit would obscure DI and test names for zero real reuse.
- **D3 — Repository additions reuse existing API methods** (`getProviderServices`, `getProviderStaff`, `getBookingStatistics`) and map to typed entities: `ComposerService` is reused for services; new `ProviderStaffMember` and `InsightsSummary`. Insights = two statistics calls (all-time + trailing 30d), same as the maturity signals.
- **D4 — Routes are flat** (`/more`, `/more/insights`, `/more/services`, `/more/staff`) with normal push navigation from the hub; the nav bar marks More active on all four (prefix match).

## Risks / Trade-offs

- **[Statistics revenue counts pending bookings]** the endpoint's `totalRevenue` includes unpaid/pending amounts → label it گردش مالی (turnover) rather than درآمد, and show completed revenue where available.
- **[Read-only surfaces may frustrate]** rows state their read-only nature by offering no edit affordance; CRUD changes are queued.

## Migration Plan

1. Entities + repository + tests → 2. cubits + DI → 3. pages/nav/router/strings → 4. widget tests, analyze, suite → 5. commit.

## Open Questions

- None blocking.
