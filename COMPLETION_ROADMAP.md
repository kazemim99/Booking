# Booksy — Completion Roadmap

> Tracked plan to take Booksy from "MVP with uneven implementation" to a launchable
> product. Derived from a domain-expert review (2026-06-15) of the modular-monolith
> codebase after the microservices→monolith migration. Status legend: `[ ]` todo ·
> `[~]` in progress · `[x]` done.
>
> **Guiding principle:** complete and protect ONE end-to-end flow first —
> *"a provider can self-onboard and take a real booking"* — before adding anything
> else. Today that flow only works because the DB was hand-seeded
> (`scripts/seed-bookable-provider.sh`).

## Status snapshot (what's already true)

- [x] Backend is a single modular-monolith host (`Booksy.Host`), runs E2E, 3 schemas in one DB, CAP in-process.
- [x] Demand side works: customer OTP auth → browse → **booking create/confirm/cancel/complete/reschedule** handlers active.
- [x] 5 backend bugs fixed during review: HttpClient name collision, Swagger schemaId collision, `register-full` OwnerFirstName crash, `register-full` dropped-services, Kavenegar booking-notification ctor.
- [x] Frontends reskinned (Coliride) + shared `@booksy/tokens`; both build green.
- [ ] **Supply side (provider inventory pipeline) is fragmented / partly stubbed — see Phase 1.**

---

## Phase 1 — Make the supply side real *(keystone — do first)*

**Goal:** a provider can register → get approved → add staff → add services → have
availability → and be bookable, entirely through the product (no DB seeding).
**Done when:** the seed script can be deleted and the flow still works, guarded by an integration test.

### Epic 1.1 — Staff management (the hard blocker) — ✅ DONE (2026-06-15)
Booking requires an **active, qualified staff sub-provider**, but the handlers were commented out.
- [x] Decided canonical model: staff = Active Individual **sub-provider** (`ParentProviderId`=org). Booking already resolves staff this way; gives staff independent identity/availability.
- [x] Implemented `Provider.RegisterStaffMember(...)` factory + restored `AddStaffToProviderCommandHandler` (creates the staff sub-provider, qualifies it for all the org's services). Fresh owned-entity VOs (Address/ContactInfo/Email/Phone) to avoid EF re-parenting.
- [x] **List staff** (GetProviderStaff) reimplemented for sub-providers (was commented out); fixed `GetCurrentUserProviderId` null-`Guid.Parse` crash + added an in-process ownership fallback to `CanManageProvider` (the `refresh-token` endpoint is broken in the monolith — HTTP self-call to old `:5001` + `provider_id`/`providerId` claim mismatch — TODO). **Located exactly** (2026-07): `ProvidersController.RefreshProviderToken` (`src/BoundedContexts/ServiceCatalog/.../ProvidersController.cs`) calls out over HTTP to `Services:UserManagement:BaseUrl` (default `http://localhost:5001`) → `/api/v1/auth/generate-token`, which doesn't exist as a standalone service anymore. Confirmed real-world symptom: a freshly-registered provider's dashboard gallery (`/provider/gallery`) renders empty because `loadCurrentProvider()` needs this refresh to get the `providerId` claim and silently fails. Fix direction: replace the HTTP self-call with an in-process call into UserManagement's `IJwtTokenService` (mirroring `AuthController.GenerateToken`'s logic) — `Booksy.ServiceCatalog.Api` doesn't currently reference `Booksy.UserManagement.Application`, so this needs a small cross-context abstraction (e.g. a shared interface registered in `Booksy.Host`), not just a project-reference add.
- [ ] Implement update/remove staff handlers; reconcile/remove the disabled `AddStaffCommandHandler`, `SaveStep5StaffCommandHandler`, and the now-unused `Staff` entity + `provider.AddStaff(...)` path.
- [x] On add-staff: staff created **Active**, linked to org, qualified for the provider's services.
- [x] **Acceptance MET:** `POST /api/v1/Providers/{id}/staff` → 201; customer books that staff → **201, no DB seeding**. (Remaining gate to a fully-API booking is org approval + service activation — Epics 1.2 / 1.3 below; both are admin-gated, 403 today.)

### Epic 1.2 — Provider approval — ✅ MVP DONE via auto-approve (2026-06-15)
- [x] **Auto-approve on registration** (config `ServiceCatalog:AutoApproveProviders`, default true): `register-full` calls `provider.Activate()`. Unblocks the flow with no admin identity needed for MVP.
- [ ] (Future, when manual review is wanted) Wire `UpdateProviderVerificationCommandHandler` + an admin pending-queue UI and set the flag to false. Requires building an admin identity/login (none exists today; activate endpoints are `AdminOnly`).
- [x] **Acceptance MET:** a newly `register-full`'d provider is immediately Active and bookable (no admin/SQL).

### Epic 1.3 — Onboarding completeness
- [x] `register-full` persists its services (fixed during review).
- [x] **Service activation** wired: a Draft service auto-activates when it gains a qualified staff member (in AddStaffToProvider); fixed the `Service.Activate()` NRE on the unloaded `Provider` nav.
- [x] **Auto-generate `ProviderAvailability` slots** when staff is added (AddStaffToProvider): 30 days × 30-min slots from the org's business hours, keyed to the staff sub-provider. Verified: slot-picker (`/Bookings/available-slots`) returns real times; booking marks slots Booked.
- [ ] (Optional) Also generate availability when a service/working-hours change; expose a "regenerate availability" admin action; rolling-window job to extend the horizon.
- [ ] Reconcile the disabled `CreateServiceCommandHandler` / `UpdateBusinessInfoCommandHandler` / `VerifyPhoneCodeCommandHandler`.

### Epic 1.4 — Fix seed/test data integrity
- [ ] Seeded `ServiceCatalog.Providers` reference `OwnerId`s that don't exist in `user_management.users` — make seeders cross-context consistent (provider owners exist as users).
- [ ] Promote `scripts/seed-bookable-provider.sh` logic into the real `ApplicationDbSeeder` so dev/demo data is operable out of the box (provider + staff + services + availability + matching user).

---

## Phase 2 — Close the booking loop

### Epic 2.1 — Booking lifecycle gaps
- [x] `MarkNoShow` + `AssignStaffToBooking` handlers **already exist and are real** (the "missing" note was stale). **Fixed a real bug in `AssignStaffToBookingCommandHandler`**: it called `UpdateBookingAsync` but never committed/published — so the `StaffAssignedToBookingEvent` never fired (and persistence relied implicitly on the pipeline). Now calls `CommitAndPublishEventsAsync`, matching `Cancel`/`MarkNoShow`.
- [x] **Reschedule/cancel policy enforcement verified + a refund gap fixed.** The domain already enforces the rules: `Booking.Reschedule` blocks outside the reschedule window (`Policy.CanReschedule`) and the handler atomically releases the old slot + books the new one; `Booking.Cancel` honors `CanBeCancelled`, computes the cancellation fee, and the handler releases the slot. **Fixed:** `CancelBookingCommandHandler` previously refunded **nothing** when a cancellation occurred past the free window — but a `CancellationFeePercentage` means the customer *pays the fee*, not forfeits the whole deposit. It now issues a **partial refund** (`paid − fee`, floored at 0) past the window, and a full refund in-window / for provider-initiated cancels.

### Epic 2.2 — Payments E2E (revenue-critical, currently unverified)
- [x] **Payment command handlers corrected + unit-tested** (Create/Verify ZarinPal): don't persist a payment with no authority; capture the gateway fee at request time; return a graceful failure when a callback references an unknown authority; idempotent no-op on an already-Paid verify. (5 tests green.)
- [ ] Verify deposit-on-booking → ZarinPal sandbox → callback → confirm, end-to-end **through the running host/UI** (needs a sandbox merchant + browser; not reproducible in CI here).
- [x] Refund path on cancellation implemented (full in-window / provider; partial = paid − fee past window) — see Epic 2.1. Still needs an end-to-end sandbox-gateway run to confirm the gateway refund call itself.

### Epic 2.3 — Notifications resilience (correctness fix)
- [x] **Booking notifications are non-blocking**: all 5 `Booking*NotificationHandler`s already catch and swallow send failures (log, never rethrow), so an SMS/email failure can't roll back the booking. **Hardened `KavenegarSmsService`**: its ctor no longer throws on a missing API key when `Kavenegar:Enabled=false` (that DI-construction throw was the real cause of the booking 500 during review) — only requires a key when the gateway is actually enabled.
- [x] **Single global SMS sandbox switch** added: `Sms:SandboxMode=true` forces *every* provider into sandbox/disabled mode (both Rahyab services honor it alongside `Rahyab:SandboxMode`; Kavenegar treats it as `Enabled=false`). Additive — when the key is absent, the existing per-provider flags are unchanged. CI's `e2e-keystone` now uses just this one switch (replaced the 3 per-provider env vars). *(Still open: collapse the 3 `ISmsNotificationService` interfaces + the two parallel booking-handler sets — `Bookings/*NotificationHandler` vs `Booking/Sms/*SmsHandler` — behind one abstraction. Larger refactor; deferred as it needs a live host to verify the DI last-wins resolution safely.)*

---

## Phase 3 — Production hardening

### Epic 3.1 — Tests & CI (the safety net for Phases 1–2)
- [x] **Keystone E2E test committed**: `tests/e2e/keystone-booking-flow.sh` — provider signup → register-full → add staff → list staff → customer signup → book → my-bookings, 6 assertions, all green against the live host.
- [x] **Unit tests green** (solution-wide, CI `--filter UnitTests` = 333/333 pass): fixed stale `BusinessProfileGalleryTests` (50→20 limit); fixed the marker-interface compile regression; fixed the 3 ProviderHierarchy handler tests **and the real persistence bug behind them** (ApproveJoinRequest + ConvertToOrganization never committed — now `SaveAndPublishEventsAsync`); fixed 4 ZarinPal payment tests (Create: don't persist a payment with no authority + capture gateway fee via `RecordPaymentRequest(fee)`; Verify: return a failure result instead of throwing when payment not found + idempotent no-op when already Paid); fixed 2 `UpdateCustomerProfile` tests (handler was never constructed — drifted from single-repo to User-aggregate signature). (CI `deploy-staging.yml` `test` job runs `dotnet test --filter UnitTests` with a Postgres service.)
- [x] **Fixed `/Services/provider/{id}` 400** (real customer-facing bug): `GetServicesByProviderQueryHandler` did `Enum.Parse(request.Category)` on a null optional filter → null-safe now. Listing a provider's services works.
- [x] **Wired the keystone E2E into CI — both pipelines**: an `e2e-keystone` job (ephemeral Postgres+Redis) boots the real `Booksy.Host` with sandbox auth (`OTP_SANDBOX_CODE`, single `Sms:SandboxMode=true`), waits for `/health`, then runs `tests/e2e/keystone-booking-flow.sh`. Added to **`deploy-staging.yml`** (develop → `build-and-push needs: [test, e2e-keystone]`) **and `deploy.yml`** (master → production: `build-api`/`build-frontend` need `[test, e2e-keystone]`). Also **re-enabled the unit-test run in `deploy.yml`** (it had been commented out — production was deploying on a successful compile alone). A broken core flow now blocks both the staging and production deploys. *(Optional follow-up: also port it to an xUnit `WebApplicationFactory<Program>` integration test with Testcontainers; retarget the per-service integration test projects to `Booksy.Host` and re-enable the integration-test step.)*
- [ ] Retarget the existing per-service integration test projects to `Booksy.Host`.
- [x] **Triaged the `NotImplementedException`s + 53 TODOs.** Fixed the *crash-class* landmines (these threw in live request paths): 4 ServiceCatalog domain exceptions whose `ErrorCode` getter threw — the exception-handling middleware reads `.ErrorCode`, so any `ServiceNotFound`/`ProviderNotActive`/`ServiceNotAvailable`/`InvalidService` would have crashed the error handler instead of returning a clean 4xx (now return real codes); `UpdateUserProfileCommand.IdempotencyKey` threw — `IdempotencyBehavior` reads it on every command, so the command was 100% broken (now `null`); `CurrentUserService.Name/IpAddress/UserAgent/GetClaimValue` threw (now implemented from `HttpContext`). Confirmed-benign (left as-is): Ardalis `IncludeStrings` spec stubs, design-time `DbContextFactory`, commented-out repo bodies, dead `User.UpdateRoles/UpdateRefreshTokens` + `AddEntityFrameworkRepositories` (no callers), and the intentional Saman/Parsian "use ZarinPal" gateway placeholders.
- [x] **Removed the dangerous duplicate booking-SMS handler set** (`EventHandlers/Booking/Sms/*SmsHandler`, 5 files): it sent to a **hardcoded** phone (`"09123456789"`) with placeholder name/service — a privacy/correctness landmine if SMS were ever enabled — and double-processed Confirmed/Cancelled/Completed/Rescheduled alongside `Bookings/*NotificationHandler`. (Both were auto-discovered by assembly scan, so both ran.) Real SMS delivery still needs cross-context customer-contact resolution: the surviving `Bookings/*NotificationHandler`→`SendNotificationCommand` path requires `RecipientPhone`, which isn't populated because the customer's phone lives in UserManagement, not ServiceCatalog — that's a genuine feature (a cross-context contact query / integration event), deferred.
- [x] **`DeleteProviderService` now guards active bookings**: blocks deletion (throws `DomainValidationException`) when the service has Requested/Confirmed bookings, so upcoming appointments can't be orphaned; provider must deactivate instead.
- [ ] **Remaining TODOs are feature-completion, not crashes** (deferred, grouped): (a) placeholder query data: `GetProviderProfile` (ResponseRate=95, CustomerName="Anonymous", IsPopular/RepeatCustomers), `GetProvidersByStatus` TotalReviews=0, `ServiceQueryRepository` fake booking/txn stats. (b) stubbed customer queries `GetUpcomingBookings`/`GetBookingHistory`. (c) unimplemented gateways (Behpardakht SOAP simulated; Saman/Parsian throw) — fine while ZarinPal is the only enabled one. (d) Firebase push unimplemented; cross-context customer-contact resolution for real booking SMS; misc cosmetic.

### Epic 3.2 — Security & config hygiene
- [ ] Revert review-only sandbox toggles before prod: `OTP_SANDBOX_CODE`, `Rahyab:SandboxMode`, `Kavenegar:Enabled=false` (or supply real keys), frontend `:5050` targets → real host.
- [x] **Frontend auth hardening**: `auth.interceptor.ts` — added a **concurrent-refresh guard** (a shared in-flight `refreshPromise` so parallel 401s trigger only one `/refresh`; previously each 401 refreshed independently and, because the refresh token rotates, raced into a spurious logout) and removed token-bearing logs (the full refresh-response body — incl. new access+refresh tokens — was being `console.log`'d). `auth.store.ts` — deleted the raw "Decoded token payload" dump and gated all remaining token-derived identity logs behind `import.meta.env.DEV`. `vue-tsc --noEmit` → 0 errors.
- [ ] Review the custom permission/policy system for the provider-onboarding endpoints.

### Epic 3.3 — Dead-code & structure cleanup
- [x] **Deleted the dead `src/BoundedContexts/Booking` context** — no source, no `.csproj`, not in the solution (only stale `bin/obj` artifacts); plus a stray `src/BoundedContexts/obj`. Bookings live in ServiceCatalog. `Booksy.Gateway` was already gone (only a `logs/gateway` dir remains).
- [x] **Fully retired RabbitMQ/MassTransit**: rewrote `CapEventBusExtensions` to in-memory-only (removed the dead `UseRabbitMQ` fallback branch + `using RabbitMQ.Client`), dropped the `DotNetCore.CAP.RabbitMQ` and `AspNetCore.HealthChecks.Rabbitmq` (unwired) packages, removed the dead `.AddSource("MassTransit")` OTel trace source and the unused `RabbitMQ` connection string from the host `appsettings.json`. Build + 333 unit tests green. *(Left deployment `docker-compose*.yml` RabbitMQ remnants alone — they're actively deployed; clean those during the next deploy pass.)*
- [ ] Containerize: one real `docker build` of `src/Host/Booksy.Host/Dockerfile` (never verified).

### Epic 3.4 — Observability
- [x] **Booking-funnel metrics added**: `BookingMetrics` (static `Meter` `Booksy.ServiceCatalog.Bookings`) emits counters `booksy.bookings.created / confirmed / completed / cancelled / noshow`, incremented in the 5 booking command handlers right after their successful commit. OpenTelemetry already collects them via `AddMeter("Booksy.*")` — exported wherever `PROMETHEUS_ENDPOINT`/`OTLP_ENDPOINT` is configured (deployment-gated). Seq logging + health checks were already wired.
- [ ] Expose a Prometheus scraping endpoint (`MapPrometheusScrapingEndpoint`) / set an OTLP endpoint in deployment so the funnel counters are actually scraped (env/deploy config, not code).

---

## Phase 4 — Polish & launch

- [ ] Finish the design-system reskin sweep (mostly done) + self-host a geometric Latin font (needs `.woff2` assets).
- [ ] Promote `@booksy/tokens` to a real workspace dependency.
- [x] Harden the Flutter customer app ↔ backend contract — done via the `customer-app-ux-redesign` OpenSpec change (July 2026): full UX redesign + implementation of auth/home/explore/provider-detail/booking/appointments/profile against the existing `/api/v1` surface (M3 token theme, go_router shell, component library, 41 unit/widget tests). Remaining: on-device E2E sweep against a running stack; API gaps documented in `openspec/changes/customer-app-ux-redesign/findings.md`.
- [ ] Update living docs (`CLAUDE.md`, `API_ENDPOINTS.md`) — done for the monolith; refresh for the completed flows.

---

## Definition of done — MVP launch

1. A provider self-onboards through the app, is approved by an admin, and appears as bookable **with zero DB edits**.
2. A customer books, pays a deposit (sandbox), and both sides see the appointment through its lifecycle (confirm → complete / cancel / no-show).
3. The keystone flow is covered by an integration test running in CI.
4. All review-only sandbox/security toggles reverted; secrets externalized.
5. One successful container build + deploy to staging.

## Suggested sequencing

Phase 1 is the unlock and should be one or two OpenSpec changes (1.1 staff + 1.2 approval
are the critical pair). Phases 2–4 can proceed partly in parallel once Phase 1 lands. Do
**Epic 3.1 (tests/CI)** alongside Phase 1, not after — it protects the rest.
