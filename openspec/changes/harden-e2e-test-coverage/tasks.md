## 1. Preflight

- [x] 1.1 Add a `PREFLIGHT_ONLY=1` early-exit branch to `tests/e2e/keystone-booking-flow.sh`: health check + one provider OTP login round-trip only, exits before the full provider/staff/customer/booking flow
- [x] 1.2 Add `booksy-frontend/e2e/global-setup.ts` that shells out to `PREFLIGHT_ONLY=1 bash ../tests/e2e/keystone-booking-flow.sh` and fails the run with a clear message on non-zero exit (uses `bash` directly via `execFileSync` — available in CI (`ubuntu-latest`) and locally via Git Bash, which this repo's tooling already assumes)
- [x] 1.3 Wire `globalSetup: './e2e/global-setup.ts'` into `playwright.config.ts`

## 2. Centralized seeding

- [x] 2.1 Extend `global-setup.ts` to call `seedBookableProvider()` once and write the result to `e2e/.seed-output.json` (gitignored)
- [x] 2.2 Add `e2e/utils/seed-fixture.ts` (`readSharedSeed()`) that specs use to read the shared seed instead of calling `seedBookableProvider()` themselves
- [x] 2.3 Updated `keystone.spec.ts` to use the shared seed (it was the only spec calling `seedBookableProvider()` directly). The registration specs (`provider-registration*.spec.ts`) don't seed via API at all — they register their own provider by driving the UI wizard, which is what they test — so nothing to change there.

## 3. Fix: My Bookings empty list

- [x] 3.1 Investigated via code reading (no live stack available in this session): traced `MyBookingsView.vue` → `booking.service.ts`'s `getMyBookings` → `BookingsController.GetMyBookings` → `GetCustomerBookingsQueryHandler` → `BookingReadRepository.GetCustomerBookingHistoryAsync`. Found two real bugs: (a) `from`/`to` bound as plain `DateTime`, which ASP.NET Core's query-string binder silently reinterprets a "Z"-suffixed UTC value as local server time (`Kind=Local`) instead of UTC; (b) pagination bound via `[FromQuery] PaginationRequest`, whose `page`/`size` query-key names (`[FromQuery(Name="page")]` etc.) don't match the `pageNumber`/`pageSize` the frontend actually sends (confirmed by grep: every other paginated frontend call site uses `pageNumber`/`pageSize`, and `BookingsController` is the only controller binding `PaginationRequest` as a complex `[FromQuery]` object).
- [x] 3.2 Fixed both in `BookingsController.GetMyBookings`: `from`/`to` now bind as `DateTimeOffset?` (unambiguous UTC parsing) and convert via `.UtcDateTime`; pagination now binds as explicit `pageNumber`/`pageSize`/`sort`/`sortDesc` query params and constructs `PaginationRequest` directly, matching the frontend's actual query keys.
- [x] 3.3 Un-skipped the My Bookings + cancel assertions in `keystone.spec.ts` (now seeds the booking against the logged-in browser session's own token via `seedBookingWithToken`, opens My Bookings, asserts the row appears, cancels it). **Not verified against a live stack** — this environment can't boot Postgres/Redis/the host/a browser. The fix is grounded in a real, demonstrable code defect either way; run `npm run e2e:pw` to confirm it resolves the observed empty-list symptom specifically.

## 4. Gallery empty render — investigated, NOT fixed here (scope change from the original plan)

- [x] 4.1 Investigated via code reading: `GalleryView.vue` calls `providerStore.loadCurrentProvider()`, which reads `providerId` from the JWT (`authStore.providerId`). A freshly-registered provider's *original* token (issued before registration) has no `providerId` claim, so the frontend calls `POST /Providers/current/refresh-token` to get a reissued one.
- [x] 4.2 Root cause found, **not fixed**: `ProvidersController.RefreshProviderToken` makes an `HttpClient` call to `Services:UserManagement:BaseUrl` (default `http://localhost:5001`) → `/api/v1/auth/generate-token` — a standalone UserManagement service that no longer exists post-monolith-migration. This is the exact issue `COMPLETION_ROADMAP.md` Epic 1.1 already tracks as a TODO ("the refresh-token endpoint is broken in the monolith"), not a fresh gallery-specific bug. The correct fix (call UserManagement's `IJwtTokenService` in-process instead of over HTTP) requires a cross-context abstraction — `Booksy.ServiceCatalog.Api` has no project reference to `Booksy.UserManagement.Application`, and adding one directly would violate the bounded-context separation this codebase deliberately maintains. That's a real architectural decision, not a param-binding fix, and touches shared authentication code I can't verify without a live stack — judged out of scope for an e2e-hardening change. Added the concrete file/line finding to `COMPLETION_ROADMAP.md` Epic 1.1 so it's actionable as its own change.
- [x] 4.3 Left `test.skip` — rewrote the skip comment to explain the real root cause (was previously a vague "frontend loading bug" placeholder) and point at Epic 1.1.

## 5. New journey: reschedule

- [x] 5.1 Added `RescheduleFlowPage` (`e2e/pages/reschedule.page.ts`); reused `seedBookingWithToken` (no new seed helper needed)
- [x] 5.2 Added `booksy-frontend/e2e/specs/booking-reschedule.spec.ts`
- [x] 5.3 Discovered during implementation: the `/customer/my-bookings` page (`MyBookingsView.vue`, what `keystone.spec.ts` already covered) has **no reschedule UI at all** — only cancel. Reschedule only exists in a separate, parallel "bookings sidebar" UI (`BookingsSidebar.vue` + `RescheduleBookingModal.vue`, opened via the desktop user-menu). Added `data-testid`s along that path: `user-menu-toggle`, `user-menu-item-{id}` (`RoleBasedUserMenu.vue`), `sidebar-bookings-list`, `sidebar-booking-row`, `booking-reschedule-button` (`BookingsSidebar.vue`), `reschedule-slot-option`, `reschedule-confirm` (`RescheduleBookingModal.vue`). Calendar date selection has no testid hooks available (vendored `vue3-persian-datetime-picker`) — drives its rendered `.vpd-day:not(.vpd-empty):not([disabled])` cells directly; this is the step most likely to need adjustment on first real run.

## 6. Docs and cleanup

- [x] 6.1 Rewrote `booksy-frontend/e2e/README.md`'s Status section (full registration wizard green, My Bookings fixed, gallery still skipped with the real reason, reschedule added) and documented `global-setup.ts` / preflight
- [x] 6.2 Added `/e2e/.seed-output.json` to `booksy-frontend/.gitignore`

## 7. Verify

- [ ] 7.1 `npm run e2e:pw` green locally against the running stack, including the newly-unskipped My Bookings assertions and the new reschedule spec — **requires the user to run it** (this environment can't boot the full Docker stack + browser). Most likely first-run friction points: the reschedule spec's `.vpd-day` calendar selector (task 5.3), and confirming the My Bookings fix (task 3.3) actually resolves the originally-observed symptom and not some other/additional cause.
- [ ] 7.2 `frontend-e2e` CI workflow still passes with the new `globalSetup` step — **requires the user's CI run**
