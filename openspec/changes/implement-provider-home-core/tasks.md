## 1. Composition core (domain) — DONE

- [x] 1.1 Composition enums (`home_enums.dart`): system/mode/availability/maturity/day/banner/widget-id
- [x] 1.2 `HomeInputs` + `MaturitySignals` + `MaturityThresholds` (config-driven) value objects
- [x] 1.3 `HomeContext` immutable value object (Equatable)
- [x] 1.4 `HomeContextResolver.resolve` — 3-layer precedence (spec §3)
- [x] 1.5 `HomeContextResolver.classifyMaturity` — config thresholds, decoupled from providerStatus (spec §4)
- [x] 1.6 `HomeContextResolver.orderBanners` — fixed severity order (spec §5)
- [x] 1.7 `HomeWidgetRegistry` — visibility + priority expressions + `compose` (spec §6)
- [x] 1.8 Unit tests: §7 fixture matrix (16 rows) + classifyMaturity boundaries + banner ordering — 24 passing, analyze clean

## 2. State & data wiring — DONE

- [x] 2.1 `HomeCubit` emitting `HomeContext` (load/refresh, stale-result guard, connectivity stream, reconnect auto-sync)
- [x] 2.2 `HomeRepository` abstraction + `HomeApiService`/`HomeRepositoryImpl` (provider bookings + statistics; documented interim defaults for bookingMode/availability/capacity until backend concepts ship; stats outage degrades to synthesized signals — never demotes to scaffold)
- [x] 2.3 Refresh strategy: `poll` MVP (60s, DI-configured) + pull-to-refresh via `refresh()`; push-ready (live source just calls refresh with polling off). In-memory cache + `isStale`; NOTE: persistent (cross-launch) cache and queued offline mutations deferred to group 3 where mutations first exist
- [x] 2.4 DI registration in `core/di/injection.dart` (manual get_it) + new `ApiConstants` endpoints
- [x] 2.5 Tests: cubit (initial/skeleton, success, error-no-cache, stale fallback, offline flip, reconnect sync, superseded-refresh guard) + repository (session guards, status composition, degradation) + pure parsers — full suite 191 passing, analyze clean

## 3. Orchestrator + widgets — DONE

- [x] 3.1 Thin `HomePage`/`HomeView` orchestrator: renders `registry.compose(ctx)` by priority (RTL); loading skeleton + total-error retry; routes all widget intents (confirm/decline/complete/no-show/share/call) to the cubit
- [x] 3.2 Widget contract lives in the registry (visibility/priority per widget) + shared treatments via existing core widgets (`AppCard`/`AppEmptyState`/`AppErrorState`); domain enriched with `HomeBooking` rows so zones render without their own fetch
- [x] 3.3 Zone widgets: StatusBannerRail (pending/offline, top-2 + overflow), ActivationChecklist (Setup hero), GetDiscovered (Growth hero), TodayAgenda (rows + positive empty + row actions), NowNext (hero + complete/no-show/call), ActionQueue (confirm/decline), EndOfDaySummary, ComingUpPeek, HomeSkeleton. BusinessAlerts/SetupNudges intentionally dormant (no data sources; visibility keeps them hidden)
- [x] 3.4 Center-docked ⊕ FAB + create sheet (نوبت جدید / مسدود کردن زمان — targets "coming soon" until composers exist) + BottomAppBar shell (Home active; Calendar/Clients/More placeholders) + avatar account sheet with status + logout
- [x] 3.5 Widget tests (10): system chrome (skeleton/error-retry), Setup composition (banner→checklist order, scrolled empty agenda), Growth hero, REQUEST ordering + real-theme footgun guard, INSTANT reordering, confirm-intent round-trip + snackbar, end-of-day swap, offline cached agenda, 1.3× font-scale no-overflow. NOTE: booking quick-action API mutations added (confirm/cancel/complete/no-show); persistent cross-launch cache + queued offline mutations remain deferred (offline mutations refuse with reason)

## 4. Integration

- [x] 4.1 `/dashboard` now builds `HomePage`; `ProviderDashboardPage` file retained one release for one-line rollback
- [x] 4.2 `AppStrings` Home section (fa-IR) shipped. Screen-design open points implemented per spec defaults (light app bar, confirm=filled/decline=danger-outline, EndOfDaySummary with count) — pending user override, not blocking. `vacationSoft`/`warningSoft` tokens deferred until vacation/closed states become reachable
- [x] 4.3 `flutter analyze` clean; full suite 201 tests passing, zero regressions
- [x] 4.4 Live-backend verification (2026-07-15, local `Booksy.Host` + docker infra) — full provider→staff→booking flow exercised; shapes captured and parsers aligned. **Findings & fixes:**
  - **FIXED (backend)**: `BookingsController.CanManageProvider` lacked the ownership fallback `ProvidersController` already had → provider-bookings/statistics 403'd for the whole first session after onboarding (token predates the `providerId` claim). Fallback replicated; keystone gate + 333 backend unit tests green.
  - **FIXED (app)**: statistics arrive wrapped in `{success, data:{…}}` → added `unwrapMap`; services arrive nested `{data:{items:[…]}}` → `unwrapList` recurses; timestamps carry `+03:30` offsets → `bookingStart` normalizes `.toLocal()`; booking status is `"Requested"` (parser already mapped it — now regression-tested).
  - **FIXED (app)**: booking rows carry only ids — service names now resolved from the provider's own catalog (`GET /Services/provider/{id}`, best-effort); domain error codes surfaced (BOOKING_DEPOSIT_NOT_PAID → Persian message).
  - **KNOWN BACKEND BUG (not fixed here)**: `POST /Providers/current/refresh-token` still calls the retired UserManagement microservice over HTTP (`localhost:5001`) → always fails in the monolith. The CanManageProvider fallback makes the Home work without it; a proper fix should make the call in-process.
  - **BACKEND GAP (flagged)**: no `customerName`/phone on provider booking rows (cross-context data) — agenda rows show service+time until a UserManagement enrichment ships.
  - **CONFIRMED DOMAIN RULE**: confirm requires the deposit paid (`DepositMustBePaidBeforeConfirmationRule`) — Request-mode queue must expect 400s for unpaid requests (message now mapped).
