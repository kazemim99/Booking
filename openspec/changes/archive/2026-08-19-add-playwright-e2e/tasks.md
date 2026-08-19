## 1. Harness setup

- [x] 1.1 Add `@playwright/test` as a devDependency and `e2e:pw` / `e2e:pw:ui` / `e2e:pw:report` scripts in `booksy-frontend/package.json` (kept the existing Cypress scripts)
- [x] 1.2 Create `playwright.config.ts` (Chromium project, `baseURL` :3000, `webServer` auto-starting `npm run dev`, `trace: 'on-first-retry'`, `video: 'retain-on-failure'`, HTML reporter, retries in CI, fixed geolocation)
- [x] 1.3 Add `booksy-frontend/e2e/README.md` documenting required env (`OTP_SANDBOX_CODE`, `Sms:SandboxMode`, stack up), the selector contract, and how to run

## 2. Test infrastructure

- [x] 2.1 Add `e2e/fixtures/test-base.ts` — extend `test` with a Neshan-map route stub (geolocation granted via config)
- [x] 2.2 Add `e2e/utils/identity.ts` — unique per-run phone generator (`912…`/`913…`) and the sandbox OTP code from env
- [x] 2.3 Add Page Objects under `e2e/pages/`: `LoginPage`, `ProviderRegistrationPage`, `StaffPage`, `BookingFlowPage`, `MyBookingsPage`

## 3. data-testid attributes

- [x] 3.1 Add `data-testid` to login/OTP components: `phone-input` (PhoneNumberInput), `send-code-button` (PhoneVerificationFlow), `otp-input` (OtpInput container). Note: this flow auto-submits on OTP completion (no verify button).
- [x] 3.2 Added `reg-business-name` (BusinessInfoStep) + `reg-next` (shared NavigationButtons). Full wizard UI coverage is a skipped optional spec; the keystone seeds the provider via API instead (`e2e/utils/api-seed.ts`).
- [x] 3.3 Added customer-journey testids: `provider-card`; booking flow `service-option`/`slot-option`/`staff-option`/`booking-advance`/`booking-confirm`; my-bookings `my-bookings-list`/`booking-row`/`booking-status`/`booking-cancel-button`. (Cancel uses a native `confirm()` dialog; staff is API-seeded since the UI flow is invitation-based.)

## 4. Keystone spec

- [x] 4.1 Implement `e2e/specs/keystone.spec.ts`: provider OTP login → registration → add staff → assert staff listed
- [x] 4.2 Extend: customer OTP login → browse provider → select service/time/staff → confirm → assert in My Bookings
- [x] 4.3 Extend: cancel the booking → assert cancelled state + refund messaging

## 5. CI

- [x] 5.1 Add a `frontend-e2e` GitHub Actions workflow: Postgres+Redis services, boot host (`dotnet run`, sandbox env), wait for `/health`, `npm ci`, `npx playwright install --with-deps chromium`, `npm run e2e:pw` (Playwright auto-starts the Vite dev server)
- [x] 5.2 Upload Playwright HTML report + traces as artifacts on failure

## 6. Verify

- [x] 6.1 `npm run e2e:pw` against the running stack — **7 passed / 1 skipped / 0 failed.** (Was 6/1/1 until the backend blocker in §6.4 was fixed; `booking-reschedule.spec.ts` is now green end-to-end.) Five defects were found and fixed while getting here:
  - **PROD BUG — provider registration wizard was unusable.** The verified phone is stored in E.164 (`+9891…`) and prefilled into a *disabled* field validated as `^09\d{9}$`, so step 1 was permanently invalid with no way for the user to correct it. Fixed by normalising via the existing `toLocalFormat()` in both `OrganizationRegistrationFlow.vue` and `IndividualRegistrationFlow.vue`.
  - **PROD BUG — customers could not open My Bookings at all** (`/customer/my-bookings` 404'd). Two routes shared the name `MyBookings`, and vue-router's `addRoute()` *evicts* the earlier record on a duplicate name, deleting the customer route at startup. Same bug hit `/customer/providers` via a duplicate `ProviderList`. Fixed by renaming the provider-side routes to `StaffMyBookings` / `ProviderBrowse`.
  - **Test defect — parallel specs raced for one slot.** `keystone` and `booking-reschedule` both seeded a booking at the same hardcoded time against the same shared staff → 409. Slots are now explicit per spec (`SEED_SLOTS`).
  - **Test defect — reschedule date picker never matched.** `.vpd-day:not([disabled])` matches nothing: the picker renders days as `<div disabled="true|false">` (Vue only omits *special* boolean attrs, and `disabled` on a `div` isn't one), so every day carries the attribute. Now `:not([disabled="true"])`.
  - **Test defect — cancel assertion looked in the wrong tab.** Cancelling works, but the row leaves the default "upcoming" tab; the page object now asserts on the "لغو شده" tab (added `bookings-tab-*` testids).
  - **Doc gap fixed:** `e2e/README.md` now documents the mandatory `Services__{UserManagement,ServiceCatalog}__BaseUrl=http://localhost:5050/api` overrides. Without them `Registration/step-9/complete` 500s, because `TokenService` HTTP-self-calls the port in `appsettings.json` (`:5000`) rather than the `:5050` local dev actually uses.
- [x] 6.3 Added `src/core/router/__tests__/routes.spec.ts` — pins route-name uniqueness plus the four paths involved, so the silent route-eviction bug above cannot recur. Verified it fails on the original code (`expected 'NotFound' to be 'MyBookings'`) and passes on the fix. Required exporting `routes` from `core/router/index.ts`.
- [x] 6.4 **Backend blocker cleared in `openspec/changes/fix-reschedule-membership-staff`.** It was *two* defects, not one: (a) reschedule resolved staff as an individual sub-provider, 404ing every membership booking, and (b) `Booking.Reschedule` handed the successor booking the original's owned value objects (`TotalPrice`/`PaymentInfo`/`Policy`), which EF rejects as re-parenting — that one broke *every* resource kind and was hidden behind (a). Both are fixed: all five resource-kind scenarios pass at the API level, `keystone-booking-flow.sh` is 16/16, and `booking-reschedule.spec.ts` is green in the browser. The spec was deliberately left failing (never skipped) until then, so the defect stayed visible.
- [x] 6.2 Updated `.gitignore` for `playwright-report/`, `test-results/`, `playwright/.cache/`. (TS type-check of the e2e files needs `npm install` first since `@playwright/test` isn't installed in this environment.)
