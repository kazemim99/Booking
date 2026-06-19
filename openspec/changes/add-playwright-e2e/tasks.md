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
- [ ] 3.2 Add `data-testid` to the registration wizard steps and submit (`reg-business-name`, `reg-next`, `reg-submit`)
- [ ] 3.3 Add `data-testid` to staff add/list, provider list/cards, booking flow (service/time/staff/confirm), my-bookings list + cancel dialog

## 4. Keystone spec

- [x] 4.1 Implement `e2e/specs/keystone.spec.ts`: provider OTP login → registration → add staff → assert staff listed
- [x] 4.2 Extend: customer OTP login → browse provider → select service/time/staff → confirm → assert in My Bookings
- [x] 4.3 Extend: cancel the booking → assert cancelled state + refund messaging

## 5. CI

- [x] 5.1 Add a `frontend-e2e` GitHub Actions workflow: Postgres+Redis services, boot host (`dotnet run`, sandbox env), wait for `/health`, `npm ci`, `npx playwright install --with-deps chromium`, `npm run e2e:pw` (Playwright auto-starts the Vite dev server)
- [x] 5.2 Upload Playwright HTML report + traces as artifacts on failure

## 6. Verify

- [ ] 6.1 `npm run e2e:pw` green locally against the running stack — **requires the user to run it** (this environment can't boot the full Docker stack + browser). First run will surface the remaining §3.2/§3.3 selectors to wire.
- [x] 6.2 Updated `.gitignore` for `playwright-report/`, `test-results/`, `playwright/.cache/`. (TS type-check of the e2e files needs `npm install` first since `@playwright/test` isn't installed in this environment.)
