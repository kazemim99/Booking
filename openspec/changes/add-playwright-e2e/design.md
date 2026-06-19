## Context

`booksy-frontend` is a Vue 3 + TypeScript SPA (Vite, Ant Design Vue, Pinia) that talks to the modular-monolith host at `/api` (Vite proxy → `:5000`/`:5050` in dev). The keystone booking journey is verified only at the API level (`tests/e2e/keystone-booking-flow.sh`) and by 333 backend unit tests; the UI itself has no automated coverage. The backend already supports deterministic browser testing: `OTP_SANDBOX_CODE=123456` makes OTP login predictable, `Sms:SandboxMode=true` suppresses real messages, and providers auto-approve on registration. The only non-deterministic UI dependency is the Neshan map widget (external tiles + geolocation).

## Goals / Non-Goals

**Goals:**
- A runnable Playwright (TypeScript) suite in `booksy-frontend/` that drives real Chromium through the keystone journey against a running stack.
- Deterministic, repeatable runs (sandbox OTP, stubbed map, unique per-run phone numbers).
- Stable selectors via `data-testid`, organized with the Page Object Model.
- Trace + video + HTML report on failure; a CI job that boots the stack and runs headless.

**Non-Goals:**
- Gating the production deploy on this suite (it needs the full stack; decide later — the API keystone job already gates deploys).
- Cross-browser matrix (Firefox/WebKit) in the first cut — Chromium only.
- Visual-regression / screenshot-diffing.
- Replacing the API keystone bash test (this is the UI-level twin, not a replacement).
- Component/unit tests (Vitest territory) — this is full E2E only.

## Decisions

- **Playwright over Cypress.** True multi-origin/multi-tab, first-class parallelism/sharding, the trace viewer, and `codegen` for authoring. Better CI story for a Vue/TS app in 2026. *Alternative:* Cypress (good DX) — rejected for weaker multi-origin support and scaling cost.
- **Real backend, not mocked.** Tests run against the actual host + Postgres + Redis (via the existing dev compose) so they exercise real wiring. *Alternative:* mock `/api` with Playwright routes — rejected for E2E (would re-create the gap we're closing); route-mocking is reserved only for the Neshan map.
- **Hybrid setup: seed via API, assert via UI.** Where UI setup is slow/irrelevant to the assertion, create state through the API (OTP + register) and drive only the *under-test* screens in the browser. Keeps tests fast and focused. The first keystone spec drives the whole journey via UI; later specs may seed via API.
- **`data-testid` selectors + Page Object Model.** Add `data-testid` to the handful of elements the suite touches; never select by Ant Design CSS/text. One Page Object per screen centralizes selectors.
- **Determinism helpers.** A unique per-run phone suffix (mirrors the bash test's `912…`/`913…` scheme); `page.route()` stubs Neshan tile/geocode requests; `OTP_CODE` read from env (default `123456`).
- **`webServer` auto-start for the frontend; stack via compose in CI.** Locally, Playwright's `webServer` boots `npm run dev`; the backend stack is assumed up (documented). In CI the job starts Postgres+Redis services + the host (`dotnet run`, same as the keystone job) and the built frontend, then runs Playwright headless.

## Risks / Trade-offs

- [Flaky external map widget] → stub Neshan tile/geocode/geolocation via `page.route` + `context.grantPermissions(['geolocation'])` with a fixed position; never assert on map internals.
- [Selector drift as the Coliride reskin evolves] → `data-testid` contract + Page Objects isolate churn to one layer.
- [Full-stack CI run is slow (~minutes) and heavier than unit tests] → keep it a separate, non-deploy-gating job initially; parallelize/shard later if it grows.
- [Sandbox toggles are review-only] → the suite *depends* on them; this reinforces that they must stay enabled in dev/CI and be reverted for prod (already tracked, Epic 3.2). Document the required env explicitly in the e2e README.
- [Rotating refresh-token / auth races in the SPA] → the recently added concurrent-refresh guard makes this deterministic; tests still wait on explicit post-login UI state, not timers.

## Migration Plan

Additive only. New devDependency + files under `booksy-frontend/e2e/`; small `data-testid` attributes on existing components (no behavior change). CI adds one job. Rollback = revert the change; nothing else depends on it.

## Open Questions

- Should the suite eventually gate deploys, or stay an advisory/nightly job? (Default: advisory first.)
- Seed strategy long-term: keep driving full UI journeys, or move setup to a shared API-seeding fixture as the suite grows?
