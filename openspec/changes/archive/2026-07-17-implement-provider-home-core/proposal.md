## Why

The Home design is complete and build-ready (`design-provider-home-workspace`; docs in `booksy-provider-app/PROVIDER_HOME_*.md`). This change begins implementation with the **thin composition core** — the fully-specified, UI-free foundation that de-risks the whole feature — behind the existing `/dashboard` route, eventually replacing the placeholder `ProviderDashboardPage`.

Building the pure domain first (resolver + registry) means the adaptive behaviour is proven by fast, deterministic unit tests *before* any widget or data wiring exists.

## What Changes

- New `home` feature module under `booksy-provider-app/lib/features/home/`.
- **Implemented in this increment (domain/composition, no UI):**
  - `HomeInputs` / `HomeContext` value objects + composition enums.
  - `HomeContextResolver` — pure `resolve`, `classifyMaturity`, `orderBanners` (spec §3–§5).
  - `HomeWidgetRegistry` — per-widget visibility + priority as pure expressions; `compose` returns the ordered visible widget ids (spec §6).
  - Unit tests covering the §7 fixture matrix (16 rows) + `classifyMaturity` boundaries + banner ordering.
- **Not yet (follow-on increments):** the `HomeCubit`/data wiring, the orchestrator page, the 12 zone widgets, DI registration, router swap.

## Capabilities

### New Capabilities
<!-- None new here. Behaviour is specified by the `provider-home-workspace` capability from change `design-provider-home-workspace`; this change implements it. -->

### Modified Capabilities
- `provider-home-workspace`: two requirement-level behaviors surfaced during implementation are ADDED — the **maturity signal-degradation bias** (a statistics outage synthesizes signals and can only promote toward the agenda, never demote to the scaffold) and the **superseded-refresh guard** (a slower stale request may never overwrite a newer result). Everything else is implementation of existing requirements, unchanged.

## Impact

- **Added:** `lib/features/home/domain/{entities,services,composition}/*.dart`; `test/features/home/home_context_resolver_test.dart`.
- **Unchanged yet:** `provider_dashboard_page.dart`, `app_router.dart`, `injection.dart` — touched in later increments when the orchestrator + widgets land.
- **No breaking changes.** Pure additive domain code; `flutter analyze` clean; new tests green.
