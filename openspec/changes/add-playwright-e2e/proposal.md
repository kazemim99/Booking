## Why

The keystone booking flow is currently protected only at the API level (`tests/e2e/keystone-booking-flow.sh`) and by backend unit tests. There is **no automated coverage of the actual Vue UI** — the provider registration wizard, the booking flow, OTP login screens, and the cancel/reschedule dialogs can regress (broken selectors, routing, state, API wiring) without any test catching it before users do. The app is already set up for deterministic, sandbox-able browser testing (`OTP_SANDBOX_CODE`, `Sms:SandboxMode`, auto-approve providers), so a real-browser E2E suite is low-friction and high-value.

## What Changes

- Add a **Playwright** E2E test harness to `booksy-frontend/` (TypeScript), driving real Chromium against the running monolith stack.
- Cover the **keystone flow through the UI**: provider OTP login → registration wizard (auto-approved) → add staff → customer OTP login → browse providers → select service/time/staff → book → see it in "My Bookings" → cancel (verify refund messaging).
- Use the **Page Object Model** (one class per screen) and **`data-testid`** selectors; add the minimal `data-testid` attributes needed to the relevant Vue components.
- Determinism: drive OTP login via `OTP_SANDBOX_CODE=123456`; rely on `Sms:SandboxMode` so no real messages fire; stub the Neshan map tiles/geolocation so map widgets don't flake.
- Capture **trace + video on failure** and produce an HTML report.
- Add a **CI job** (GitHub Actions) that boots the stack (host + Postgres + Redis) and the frontend, runs the suite headless, and uploads traces on failure.

## Capabilities

### New Capabilities
- `e2e-testing`: Automated browser end-to-end testing of the customer- and provider-facing web UI — harness setup, the keystone UI journey, deterministic sandbox auth, selector/stability conventions, and CI execution.

### Modified Capabilities
<!-- None. E2E tests exercise existing behavior; they do not change any capability's requirements.
     (Adding data-testid attributes to components is an implementation detail, not a spec change.) -->

## Impact

- **New**: `booksy-frontend/` gains `@playwright/test` (devDependency), `playwright.config.ts`, `e2e/` (Page Objects + specs), and Playwright's browser binaries in CI.
- **Modified (non-breaking)**: small `data-testid` attribute additions to existing Vue components (login, registration steps, provider list, booking flow, my-bookings, cancel dialog).
- **New CI**: a `frontend-e2e` job (or workflow) that stands up the Docker stack + dev server and runs Playwright; trace artifacts on failure. Optionally gated before deploy later (separate decision — the run needs the full stack, ~minutes).
- **Config dependency**: the test run relies on the existing review-only sandbox toggles; these are already documented as must-revert-for-prod (COMPLETION_ROADMAP Epic 3.2) and are unaffected by this change.
- **No backend/runtime code changes** beyond `data-testid` attributes; no API or DB impact.
