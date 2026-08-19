## Why

`booksy-frontend/e2e/` (Playwright, added by the in-flight `add-playwright-e2e` change, 15/16 tasks done) and `tests/e2e/keystone-booking-flow.sh` together already give Booksy the browser- and API-level coverage that `C:\Repos\Coliride\e2e` provides for CoRide, adapted to a booking form/wizard app instead of a live-GPS ride simulation (see `design.md` for the comparison). Two structural patterns from Coliride are still missing — a fast-fail preflight before the (slow) browser suite runs, and re-seeding via Playwright's `globalSetup` instead of per-spec `beforeAll` — and the suite's own README documents two real, already-diagnosed product bugs that currently force two assertions to stay `test.skip`: the My Bookings list renders empty for a real backend booking, and the dashboard gallery renders empty for a freshly-registered provider's uploaded images. Both block coverage of flows (cancel-with-refund, gallery main-image) that already have failing/skipped scaffolding in place.

## What Changes

- Add a `PREFLIGHT_ONLY` early-exit mode to `tests/e2e/keystone-booking-flow.sh` (health check + one OTP round-trip, no full flow) and call it from a new `booksy-frontend/e2e/global-setup.ts`, wired into `playwright.config.ts` via `globalSetup`, so a misconfigured environment fails in seconds with a clear message instead of a 120s `webServer` timeout or a confusing mid-suite failure.
- Move provider/staff/service seeding from per-spec `test.beforeAll` calls into that same `global-setup.ts`, seeding once per run and sharing the result via a JSON fixture file; keep the `seedBookableProvider()` / `seedCustomerBooking()` implementations in `api-seed.ts` unchanged, just call them centrally.
- Investigate and fix the `GET /Bookings/my-bookings` param-binding bug and un-skip the My Bookings + cancel assertions in `keystone.spec.ts`.
- Investigate the dashboard gallery empty-render bug for `PendingVerification` providers. **Outcome differs from the original plan**: root-caused to `COMPLETION_ROADMAP.md`'s already-tracked Epic 1.1 `refresh-token`/`provider_id` bug (an HTTP self-call to a UserManagement service that no longer exists post-monolith-migration) — not a gallery-specific defect. Fixing it properly needs a cross-context abstraction (`Booksy.ServiceCatalog.Api` has no reference to `Booksy.UserManagement.Application`, and shouldn't gain a direct one) and touches shared authentication code, which is a larger, separate architectural decision — judged out of scope here. Left `test.skip`, with the skip comment rewritten to point at the real cause and at Epic 1.1, plus the concrete finding (exact file/line, fix direction) added to `COMPLETION_ROADMAP.md` so it's actionable as its own change.
- Add a `booking-reschedule.spec.ts`. **Outcome differs from the original plan**: the `/customer/my-bookings` page (covered by `keystone.spec.ts`) turned out to have no reschedule UI at all — reschedule only exists in a separate "bookings sidebar" reached via the desktop user-menu (`BookingsSidebar.vue` + `RescheduleBookingModal.vue`). The spec drives that path instead.
- Update `booksy-frontend/e2e/README.md`'s "Status" section to match reality (it undersold work already merged in commits `81042ec`…`85ab0e8` — full 8-step registration wizard is green, gallery upload during registration works).

## Capabilities

### Modified Capabilities
- `e2e-testing`: adds preflight/global-setup requirements, closes the My Bookings coverage gap, and adds reschedule journey coverage (via the bookings sidebar, not `/customer/my-bookings`). Does **not** add gallery main-image coverage — that requirement was dropped from this change's delta since it isn't being delivered (see `specs/e2e-testing/spec.md`). (The base capability is still pending in the unarchived `add-playwright-e2e` change; this change's delta is additive.)

## Impact

- **New**: `booksy-frontend/e2e/global-setup.ts`, `e2e/utils/seed-fixture.ts`, `e2e/pages/reschedule.page.ts`, `e2e/specs/booking-reschedule.spec.ts`; a `PREFLIGHT_ONLY` branch in `tests/e2e/keystone-booking-flow.sh`.
- **Modified**: `booksy-frontend/playwright.config.ts` (add `globalSetup`); `e2e/specs/keystone.spec.ts` (un-skip My Bookings, use shared seed); `e2e/specs/provider-registration-realistic.spec.ts` (skip comment corrected, still skipped); `e2e/pages/login.page.ts` (added `accessToken()`); `e2e/README.md`; `.gitignore`.
- **Backend fix**: the `GET /Bookings/my-bookings` query/param-binding bug in `BookingsController.GetMyBookings` (bounded context: ServiceCatalog) — `from`/`to` now bind as `DateTimeOffset` (was silently-wrong-timezone `DateTime`), pagination now binds the query keys the frontend actually sends.
- **Frontend testid-only changes** (no behavior change) to enable the reschedule spec: `RoleBasedUserMenu.vue`, `BookingsSidebar.vue`, `RescheduleBookingModal.vue`.
- **Not fixed** (see above): the dashboard gallery empty-render bug — tracked instead as a concrete addendum to `COMPLETION_ROADMAP.md` Epic 1.1.
- **Depends on**: `add-playwright-e2e` (provides the harness this change extends) should land/archive first, or be archived alongside this change if still open.
- **No changes** to the Cypress suite or to CI gating policy (`frontend-e2e.yml` stays advisory).
- **Not verified against a live stack** — this environment has no Docker/Postgres/Redis/browser. Tasks 7.1/7.2 require the user to run `npm run e2e:pw` and the CI workflow.
