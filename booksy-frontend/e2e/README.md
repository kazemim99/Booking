# Playwright E2E (browser) tests

The browser-level twin of `tests/e2e/keystone-booking-flow.sh`. Drives the real
Vue app in Chromium against a running monolith stack.

> Coexists with the existing Cypress setup (`cypress/`, `npm run test:e2e`).
> Playwright uses **`npm run e2e:pw`** and lives under `e2e/`.

## Prerequisites

The backend stack must be running with **sandbox auth** so OTP login is deterministic:

```bash
# from repo root — Postgres + Redis + host
OTP_SANDBOX_CODE=123456 Sms__SandboxMode=true ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project src/Host/Booksy.Host

# (Postgres + Redis via docker-compose; the host listens on :5050,
#  which the Vite dev proxy forwards /api to — see vite.config.ts)
```

The Vite dev server (port 3000) is started automatically by Playwright's
`webServer` config; you do **not** need to run `npm run dev` yourself.

## Run

```bash
cd booksy-frontend
npm install
npx playwright install --with-deps chromium   # first time only
npm run e2e:pw            # headless
npm run e2e:pw:ui         # interactive UI mode
npm run e2e:pw:report     # open the last HTML report
```

Env overrides: `E2E_BASE_URL` (default `http://localhost:3000`),
`E2E_OTP_CODE` (default `123456`).

## Structure

```
e2e/
  fixtures/test-base.ts   # extends `test`; stubs the Neshan map so it can't flake
  utils/identity.ts       # unique per-run 912…/913… phones + sandbox OTP code
  pages/                  # Page Object Model (one class per screen)
  specs/keystone.spec.ts  # provider onboard → staff → customer book → cancel
```

## Supply-side: seeded via API, not the UI

The keystone spec seeds the provider + active staff + service through the backend
(`e2e/utils/api-seed.ts`, mirroring the bash keystone) and drives only the
**customer journey** (browse → book → cancel) through the browser. Reason: the
provider registration wizard has many required per-step fields and the staff flow
is *invitation*-based (a pending invite, not a bookable staff member). A separate,
skipped "provider registration (UI)" spec is the placeholder for wizard coverage.

## Selector contract (`data-testid`)

The Page Objects select by `data-testid` only (never CSS/label). These are wired in
the components:

| Screen | `data-testid`s | Status |
| --- | --- | --- |
| Login / OTP | `phone-input`, `send-code-button`, `otp-input` (multi-cell; auto-submits on completion — no verify button) | wired |
| Browse / book | `provider-card`, `service-option`, `slot-option`, `staff-option`, `booking-advance` (next/review), `booking-confirm` | wired |
| My Bookings | `my-bookings-list`, `booking-row`, `booking-status`, `booking-cancel-button` (cancel uses a native `confirm()` dialog) | wired |
| Provider registration (optional UI spec) | `reg-business-name`, `reg-next` | wired (spec skipped pending tuning) |

`book-service-button` and `first-name-input`/`last-name-input` are referenced
defensively (clicked only if present) since the booking entry point and first-time
signup prompt vary; add them if your first run shows they're needed.

## CI

The `frontend-e2e` workflow boots Postgres + Redis + the host (sandbox env),
serves the built frontend, runs the suite headless, and uploads the HTML report
+ traces on failure.
