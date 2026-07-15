## Context

This change implements the `provider-home-workspace` capability specified by the `design-provider-home-workspace` change (design docs: `booksy-provider-app/PROVIDER_HOME_TODAY_DESIGN.md`, `PROVIDER_HOME_SCREEN_DESIGNS.md`, `PROVIDER_HOME_RESOLVER_SPEC.md`). The behaviour is fully specified there; this document records the *implementation-level* technical decisions — layering, state management, data sourcing, degradation policy, and build order.

**Progress at time of writing:** groups 1–2 of `tasks.md` are implemented and green (domain composition core; cubit + data wiring; 191 tests passing, analyze clean). Groups 3–4 (orchestrator + widgets; route swap) follow these same decisions.

## Goals / Non-Goals

**Goals:**
- Translate the resolver spec into idiomatic Flutter/BLoC matching this app's established patterns (manual `get_it`, manual JSON, Equatable, mocktail/bloc_test).
- Keep the composition engine pure and UI-free so the §7 fixture matrix is a set of fast unit tests.
- Ship incrementally behind the existing `/dashboard` route with zero regression risk until the final swap.
- Degrade gracefully where backend concepts don't exist yet, with the bias "never demote an established provider to the scaffold."

**Non-Goals:**
- No backend changes; no push infrastructure (polling MVP, push-ready seam).
- No persistent (cross-launch) offline cache in the first increments — in-memory cache + staleness only; persistence lands with the widget/mutation increment where it pays off.
- No Calendar/Clients/More tabs; no bottom-nav shell redesign beyond hosting the Home.

## Decisions

### ID1 — Layering: pure domain core, thin state shell
`lib/features/home/domain/` holds `HomeContext`/`HomeInputs`/enums, `HomeContextResolver` (pure: 3-layer precedence, `classifyMaturity`, `orderBanners`) and `HomeWidgetRegistry` (visibility + priority as pure expressions; `compose(ctx)` → ordered widget ids). Nothing in domain imports Flutter or Dio. **Why:** the adaptive behaviour — the riskiest part — is proven by 24 deterministic unit tests before any UI exists; the registry is the single place composition changes happen (spec: "add/remove/reorder/replace without redesigning the screen").

### ID2 — `HomeCubit extends Cubit<HomeContext>`; the cubit owns only the system layer
The cubit tracks connectivity, load status, and the in-memory cached snapshot; every emission is `resolver.resolve(inputs)`. It contains no composition logic. Alternatives considered: a richer `HomeState` wrapper (rejected — the design mandates widgets read only `HomeContext`; wrapping invites zone logic to leak into the cubit); one bloc per widget now (rejected for the snapshot phase — a single snapshot fetch is simpler and the per-widget refresh seam still exists via the repository).
Concurrency: a monotonic request sequence discards superseded refresh results (tested); reconnect triggers an auto-sync refresh.

### ID3 — Snapshot-based data layer behind `HomeRepository`
`fetchSnapshot()` returns the Layer-1/2 inputs in one consistent value (`HomeSnapshot`). `HomeRepositoryImpl` composes: session (`AuthRepository.getCurrentSession` → providerId/status) + today's bookings (`GET /v1/Bookings/provider/{id}?from&to`) + statistics (all-time + trailing-30d). Parsing helpers are pure statics (`unwrapList`, `bookingStatus`, `bookingStart`, `readInt`) tolerant of envelope/key variations, unit-tested without Dio. **Why snapshot over per-widget fetch now:** one request cycle drives the whole composition decision; widgets that need rich data (agenda rows, request details) fetch via their own sources in the widget increment — the registry only needs counts/flags.

### ID4 — Degradation policy (the bias matters)
- Statistics outage → signals synthesized from today's bookings (`profileComplete=true`, totals=today count): an established provider is **never** demoted to Setup/Growth scaffold by a data gap; a genuinely-new provider still lands in Growth (0 bookings).
- Bookings endpoint failure → hard `ServerFailure` (the snapshot is meaningless without it); cubit falls back to cached snapshot (stale) or the error context.
- Backend concepts that don't exist yet get documented interim defaults in ONE place (`HomeRepositoryImpl`): `bookingMode=request`, `availability=open`, `openCapacity=1` (never fully-booked), no exceptions/alerts/nudges, `MaturityThresholds.fallback`. Each swaps in behind the repository without touching cubit/UI.

### ID5 — Refresh: 60s foreground polling configured at DI; push-ready seam
`HomeCubit(pollInterval:)` is opt-in (null in tests → deterministic). The DI registration passes 60s. When push lands, the live source calls `refresh()` (or feeds the repository) and DI passes `pollInterval: null` — no cubit change. Pull-to-refresh calls the same `refresh()`.

### ID6 — Orchestrator + widget contract (group 3, upcoming)
`HomePage` = `BlocBuilder<HomeCubit, HomeContext>` rendering `registry.compose(ctx)` into a scrollable column (RTL), plus the two full-screen chrome cases (LOADING → composition skeleton; ERROR → centered retry). Zone widgets are plain widgets keyed by `HomeWidgetId`, each receiving `HomeContext` + emitting intents via callbacks routed by the page (widgets never navigate/mutate globally). The shared four-state recipe (loading/empty/error/offline) is one reusable wrapper so treatments stay consistent. Build order: Setup set (BannerRail, ActivationChecklist, ComingUpPeek, TodayAgenda) → Operational set (ActionQueue, NowNext, EndOfDaySummary) → remaining. Every action row uses width-constrained buttons (infinite-width theme footgun).

### ID7 — Route strategy (group 4)
`/dashboard` keeps its path; the route builder swaps `ProviderDashboardPage` → `HomePage` only when the core widget set reaches parity. Until then the new code ships dark (registered in DI, fully tested, unreachable). Rollback = one-line builder revert.

## Risks / Trade-offs

- **[Unverified backend shapes]** Parsers are defensive but not validated against a live host → Mitigation: pure parsers accept known variants; integration verification is an explicit group-4 task before the route swap; failures degrade per ID4 rather than crash.
- **[Snapshot staleness between polls]** 60s polling can lag reality → Mitigation: pull-to-refresh; reconnect auto-sync; interval is DI config; push seam ready.
- **[In-memory cache only]** A cold offline launch has no cache → shows the offline-no-cache treatment rather than yesterday's data → Accepted for MVP; persistent cache is a group-3 line item where mutations/queueing land anyway.
- **[Interim defaults mask states]** `availability=open` means vacation/closed states can't trigger yet → Accepted: those backend concepts don't exist; the resolver/registry/tests already handle them, so enabling is a repository-only change.

## Migration Plan

1. ✅ Domain core + tests (group 1) — additive, dark.
2. ✅ Cubit + data + DI + tests (group 2) — additive, dark.
3. Orchestrator + Setup widgets + widget tests (group 3) — additive, dark.
4. Operational widgets; persistent cache + queued mutations where actions appear (group 3).
5. Live-backend verification of parsers; resolve screen-design open points; `AppStrings` additions (group 4).
6. Route swap `/dashboard` → `HomePage`; placeholder retained one release behind the builder for trivial rollback.

## Open Questions

- None blocking. Pending externals tracked in tasks.md group 4: the four screen-design review points (app-bar chrome, confirm/decline emphasis, EndOfDaySummary depth, microcopy) and live verification of booking/statistics response shapes.
