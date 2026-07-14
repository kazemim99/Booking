# Playwright E2E (browser) tests

The browser-level twin of `tests/e2e/keystone-booking-flow.sh`. Drives the real
Vue app in Chromium against a running monolith stack.

## Status

- ✅ **Customer OTP sign-in** — real backend: Vue login → `/api` → host →
  send-verification-code + complete-authentication → landing.
- ✅ **Customer provider-detail renders REAL backend data** — `ProviderDetailView`
  is wired to `providerService.getProviderById` + `serviceService.getServicesByProvider`.
- ✅ **My Bookings list + cancel** — was skipped ("list renders empty despite a
  seeded booking"); root-caused and fixed: `GET /Bookings/my-bookings`'s `from`/`to`
  query params bound as `DateTime` (silently reinterpreted a UTC "Z" value as local
  server time) and its pagination bound to the wrong query-string names (`page`/
  `size` instead of the `pageNumber`/`pageSize` the frontend actually sends). Fixed
  in `BookingsController.GetMyBookings`.
- ✅ **Full provider registration wizard** (all 8 steps: business info → category →
  location → services → working hours → gallery → preview → complete) is green,
  including gallery image upload during the wizard.
- ✅ **Booking reschedule** — via the "نوبت‌های من" sidebar (desktop user-menu →
  bookings; this path has reschedule support, the `/customer/my-bookings` page
  covered above only has cancel).
- ⛔ **Gallery "set a main image"** stays skipped. Uploaded images persist, but the
  dashboard gallery (`/provider/gallery`) renders empty for a freshly-registered
  `PendingVerification` provider. Root cause: this is **not** a gallery bug — it's
  `COMPLETION_ROADMAP.md` Epic 1.1's already-tracked `refresh-token`/`provider_id`
  claim issue (`ProvidersController.RefreshProviderToken` makes a now-broken HTTP
  self-call to the retired standalone UserManagement service). Fixing it needs a
  proper in-process cross-context call, which is a bigger change than this test
  suite's scope — see the spec's skip comment for detail.
- Browser: uses the **system Chrome** (`channel: 'chrome'`) so no Chromium download
  is needed; `PW_VIDEO=off` skips video where ffmpeg can't be installed.

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

## Global setup: preflight + shared seed

Before any spec runs, `e2e/global-setup.ts` (wired via `playwright.config.ts`'s
`globalSetup`):

1. **Preflight** — shells out to `tests/e2e/keystone-booking-flow.sh` in
   `PREFLIGHT_ONLY=1` mode (health check + one OTP round-trip, a few seconds) and
   fails the whole run immediately with a clear message if the stack is down or
   sandbox auth isn't configured, instead of letting individual browser tests
   time out against it.
2. **Seed** — creates one bookable provider (+staff+service) via the API and
   writes it to a gitignored `e2e/.seed-output.json` fixture. Specs read it via
   `e2e/utils/seed-fixture.ts` instead of each seeding their own provider.

This mirrors the pattern in `C:\Repos\Coliride\e2e` (a fast preflight script +
`global-setup.mjs` re-seeding once per run) — see
`openspec/changes/harden-e2e-test-coverage/design.md` for the comparison and why
Coliride's GPS/streaming-specific tooling wasn't ported (not applicable to a
booking platform).

## Structure

```
e2e/
  global-setup.ts              # preflight + one-time seed (see above)
  fixtures/test-base.ts        # extends `test`; stubs the Neshan map so it can't flake
  utils/identity.ts            # unique per-run 912…/913… phones + sandbox OTP code
  utils/api-seed.ts            # seedBookableProvider / seedCustomerBooking / seedBookingWithToken
  utils/seed-fixture.ts        # reads the shared provider global-setup seeded
  pages/                       # Page Object Model (one class per screen)
  specs/keystone.spec.ts                       # provider detail + customer My Bookings + cancel
  specs/provider-registration.spec.ts          # registration wizard steps 1-3
  specs/provider-registration-realistic.spec.ts # full wizard incl. map pin, gallery upload, dashboard
  specs/booking-reschedule.spec.ts             # customer reschedule via the bookings sidebar
```

## Supply-side: seeded via API, not the UI

The keystone and reschedule specs use the provider + active staff + service seeded
once by `global-setup.ts` (`e2e/utils/api-seed.ts`, mirroring the bash keystone)
and drive only the **customer journey** through the browser. Reason: the staff flow
is *invitation*-based through the UI (a pending invite, not a bookable staff
member), so it's seeded directly. The provider registration specs are the
exception — they register their own fresh provider each run, since driving the
wizard itself is exactly what they test.

## Selector contract (`data-testid`)

The Page Objects select by `data-testid` where possible (never CSS/label), except
inside vendored third-party components with no such hooks (noted below). These are
wired in the components:

| Screen | `data-testid`s | Status |
| --- | --- | --- |
| Login / OTP | `phone-input`, `send-code-button`, `otp-input` (multi-cell; auto-submits on completion — no verify button) | wired |
| Browse / book | `provider-card`, `service-option`, `slot-option`, `staff-option`, `booking-advance` (next/review), `booking-confirm` | wired |
| My Bookings (`/customer/my-bookings` page) | `my-bookings-list`, `booking-row`, `booking-status`, `booking-cancel-button` (cancel uses a native `confirm()` dialog) | wired |
| Bookings sidebar (desktop user-menu) | `user-menu-toggle`, `user-menu-item-bookings`, `sidebar-bookings-list`, `sidebar-booking-row`, `booking-reschedule-button` | wired |
| Reschedule modal | `reschedule-slot-option`, `reschedule-confirm`; **date selection uses the vendored `vue3-persian-datetime-picker`'s `.vpd-day` cells directly** (no testid hooks available inside the library) | wired; calendar step unverified against a live run |
| Provider registration wizard | `reg-business-name`, `reg-next`, plus the fuller `ProviderRegistrationPage` object driving all 8 steps (map pin, working hours, gallery upload, review, complete) | wired |
| Provider dashboard gallery | none yet — spec stays skipped, see Status above | not applicable while skipped |

`book-service-button` and `first-name-input`/`last-name-input` are referenced
defensively (clicked only if present) since the booking entry point and first-time
signup prompt vary.

## CI

The `frontend-e2e` workflow boots Postgres + Redis + the host (sandbox env),
serves the built frontend, runs the suite headless, and uploads the HTML report
+ traces on failure. Advisory (not a deploy gate) — see the workflow file's header
comment.
