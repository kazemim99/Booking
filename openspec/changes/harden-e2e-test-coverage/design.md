## Context

Booksy already has two layers of "Coliride-style" E2E coverage:

- `tests/e2e/keystone-booking-flow.sh` — a pure-curl, dependency-free API smoke test (provider signup → register → add staff → customer signup → book), directly analogous to Coliride's `scripts/preflight.mjs` + `scripts/seed.mjs` combined.
- `booksy-frontend/e2e/` — a Playwright (TypeScript) browser suite with a Page Object Model, `data-testid` selectors, sandboxed OTP auth, and a `frontend-e2e` CI workflow. This is the browser-level analog of Coliride's `tests/*.spec.mjs`.

This is the correct shape for Booksy: Coliride's `e2e/` is built around streaming GPS positions over WebSockets for a multi-participant ride simulation (`scripts/stream-passenger.mjs`, `scripts/drive-gamma-ride.mjs`, `playwright.gps-scenarios.config.js`) against a remote Gamma environment with real Keycloak accounts — none of that has a Booksy equivalent, so it is **not** being ported. What Coliride does have that Booksy's suite lacks are two structural patterns worth adopting, plus the existing suite has two real, already-discovered bugs blocking full coverage.

## Goals / Non-Goals

**Goals**
- Adopt Coliride's fast-fail preflight + `globalSetup` re-seed pattern.
- Un-skip the two coverage gaps the existing suite already found and documented (My Bookings list, dashboard gallery main-image) by fixing their root causes.
- Add one more real user journey (reschedule) to close the gap between "keystone booking" and the documented cancellation/reschedule capabilities.

**Non-Goals**
- No GPS/streaming/multi-participant infrastructure (not applicable to a booking platform).
- No change to the Cypress suite (`booksy-frontend/cypress/`) — out of scope, not mentioned by either capability.
- No new CI gating decision (stays advisory per the existing `frontend-e2e.yml` comment); promoting it to a deploy gate is a separate decision noted in that file already.

## Decisions

### 1. Preflight: extend the existing bash script, don't duplicate it in Node

Coliride's `scripts/preflight.mjs` is a *separate* script from its seed/test scripts, run manually or in CI before the suite, checking realm reachability + auth + issuer-match in under 30s. Booksy's `tests/e2e/keystone-booking-flow.sh` already does an equivalent (and stronger — it exercises the real flow) check, but only as a full smoke run (~seconds, no browser). Rather than writing a second, parallel Node preflight script, `booksy-frontend/e2e`'s new `globalSetup` will shell out to a trimmed preflight mode of the existing bash script (`PREFLIGHT_ONLY=1 bash tests/e2e/keystone-booking-flow.sh`, added as a new early-exit branch: health check + one OTP login) so there is one source of truth for "is the environment even up," not two.

**Alternative considered**: a standalone `booksy-frontend/e2e/scripts/preflight.ts`. Rejected — it would duplicate host/OTP/health logic already in the bash script and drift from it over time.

### 2. Seeding: move to Playwright `globalSetup`, keep `api-seed.ts` helpers

Coliride's `global-setup.mjs` re-seeds a fresh ride before every run so time-gated state is always aligned. Booksy's suite currently seeds ad hoc per spec file (`test.beforeAll` calling `seedBookableProvider()`), which means every new spec re-implements "do I need a provider" bookkeeping and specs can't safely run in parallel against shared seeded state. Add a `playwright.config.ts` `globalSetup` that seeds one bookable provider once per run and hands its id via an env var / JSON fixture file to specs, keeping `api-seed.ts`'s functions as the implementation (no logic duplication) but calling them once centrally instead of per-spec.

### 3. Fix-before-cover: the two skipped assertions

- **My Bookings empty list**: `GET /Bookings/my-bookings` filters by `StartTime >= from` and returns empty for a booking known to exist for the token's user — documented in `booksy-frontend/e2e/README.md` as needing "a focused param-binding / query investigation." This blocks all My-Bookings and cancel-flow UI coverage.
- **Dashboard gallery empty render**: gallery images uploaded during registration persist server-side but the dashboard gallery manager renders empty for a freshly-registered (`PendingVerification`) provider — documented in commit `40dff3d`.

Both are pre-existing product bugs the E2E work surfaced, not test-harness issues. Fixing them is in scope here because un-skipping the tests they block is the most direct way to "harden" (not just extend) the suite — this is the same discovery-fix-verify loop used across the recent `test(e2e)+fix(ui)` commits (`81042ec`, `9058da4`) already on this branch.

**Update after implementation**: the My Bookings fix landed as planned (a genuine, self-contained ASP.NET Core query-string binding bug in `BookingsController.GetMyBookings`). The gallery fix did not — investigation traced it to `COMPLETION_ROADMAP.md`'s already-tracked Epic 1.1 `refresh-token` bug (`ProvidersController.RefreshProviderToken`'s HTTP self-call to a UserManagement service that no longer exists), not a gallery-specific issue. That fix requires a cross-context abstraction between ServiceCatalog and UserManagement (currently no project reference exists, deliberately, per the bounded-context separation) and touches shared authentication code — a real architectural decision, not a param-binding correction, and not verifiable without a live stack. Left skipped; the concrete finding was added to `COMPLETION_ROADMAP.md` Epic 1.1 instead of being patched blind here.

### 4. New journey: booking reschedule

`BOOKING_RESCHEDULING_IMPLEMENTATION.md` (being archived by the parallel `consolidate-project-documentation` change) documents a shipped reschedule capability with no UI E2E coverage. Add it as a third journey alongside the keystone booking and provider-registration specs, following the same seed-via-API-drive-via-UI pattern as the customer booking flow.
