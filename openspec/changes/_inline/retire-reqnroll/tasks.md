Status: ACTIVE
Verify: FULL

User directive, 2026-09-11: retire Reqnroll/SpecFlow entirely. No hybrid. xUnit integration tests
are the acceptance/business-behavior gate, unit tests cover pure logic, e2e stays thin (the
keystone curl script; Playwright/Cypress for a few critical journeys). Before deleting: audit every
feature file and step definition, migrate any real coverage that exists only in Reqnroll into
properly named xUnit tests, and confirm nothing is lost.

## Audit findings (measured, not assumed)

- 739 scenarios across 41 `.feature` files, 211 step bindings across 10 files.
- **707 of 739 (95%) have at least one unbound step** — Reqnroll reports them inconclusive; they
  have never run, so deleting them loses nothing. Already documented in
  `openspec/changes/REQNROLL-COVERAGE-GAP.md` (2026-08-24 audit).
- **Every payment scenario — all of Behpardakht (43) and ZarinPal (20, of the ones with 0 unbound
  steps), plus `ProcessPayment.feature`/`RefundPayment.feature` (10) — carries zero real coverage
  today**, for two independent reasons:
  - FOLLOW-UPS #31 (already on file): `ZarinPalSteps`/`BehpardakhtSteps` build a
    `Mock<IZarinPalService>`/`Mock<IBehpardakhtService>` and configure it, but never register it
    with the test factory's DI container. Every payment command therefore makes a REAL outbound
    HTTP call and fails on `Payment:ZarinPal:MerchantId` still being the placeholder value.
  - Behpardakht specifically is worse: `/api/v1/payments/behpardakht/create` (and `.../verify`,
    `.../settle`, `.../reverse`, `.../inquiry`) **has never existed as a controller route.**
    `CreateBehpardakhtPaymentRequest`/`Response` are orphaned DTOs with no consumer anywhere in
    `src` — confirmed by full-repo grep and `git log` (no `BehpardakhtController` ever existed).
    Those scenarios 404 before ever reaching the mock or the gateway.
  - `PaymentGatewayFactory` independently documents Behpardakht as a `NonFunctionalProviders`
    stub — "returns mock success without moving money" — refused unless
    `Payments:AllowStubGateways=true`.
- Of the remaining ~32 bound, non-payment scenarios, ~21 are exact duplicates of an existing xUnit
  test (matched by name and by reading both bodies): Availability (1), CreateBooking (4),
  CancelBooking (1), RescheduleBooking (1, and a second — "legacy individual sub-provider" — that
  is mechanically identical to the membership case under the current domain model:
  `BookableResourceResolver`'s own comment says the legacy sub-provider branch is gone), Provider
  registration (1), ServiceManagement (1).
- **11 scenarios are genuinely unique** — real assertions, confirmed by reading the actual
  production code path, with no xUnit equivalent anywhere in the repo:
  - Availability: holiday closes the day, an exception schedule overrides business hours (2)
  - Reschedule: booking held directly against the organization, the 409 all-or-nothing rejection,
    the 404 unresolvable-resource case (3)
  - Cancel: cross-customer 403, provider-owner 200, unauthenticated 401 — this exercises
    `BookingOwnershipResolver`, which has its own dedicated test nowhere else; the existing
    `AuthorizationBehaviorTests` only tests the generic pipeline against a stub resolver (3)
  - Create: provider-entered walk-in is born `Confirmed`, multi-service visit sums duration/price (2)
  - (One more, "Customer books at different times" outside-hours boundary, judged low-value/already
    implicitly covered by `AvailabilityControllerTests`' own boundary tests — not ported)

## Tasks
- [x] 1 Port the 11 unique scenarios into xUnit:
  - `AvailabilityControllerTests.cs`: `GetAvailableSlots_OnAHoliday_ReturnsNoSlots`,
    `GetAvailableSlots_WithAnExceptionSchedule_OnlyOffersSlotsWithinItsHours`
  - `API/Bookings/RescheduleResourceResolutionTests.cs` (new): direct-organization reschedule,
    rejected/all-or-nothing, unresolvable-resource/404
  - `API/Bookings/BookingCancellationOwnershipTests.cs` (new): cross-customer 403, provider-owner
    200, unauthenticated 401
  - `BookingsControllerTests.cs`: `CreateBooking_AsProviderOwner_IsBornConfirmed`,
    `CreateBooking_WithMultipleServices_SumsDurationAndPrice`
- [x] 2 Compile-check and run the new tests (both peer sessions ended; lock was free). All 10 pass,
      after fixing three defects the porting surfaced:
      - `CreateBookingCommandHandler`'s response `DurationMinutes`/`Currency`/`PaymentStatus`/`CreatedAt`
        are never set (FOLLOW-UPS #53). Not fixed here — the multi-service test asserts the stored
        row instead, matching what the original Reqnroll scenario actually checked
      - `ProviderReadRepository.GetByIdAsync` never included `Holidays`/`Exceptions` — a provider's
        holidays and exception hours have never once been visible to the availability engine in
        production, for any caller. FIXED (two `.Include()`s) — this is what the ported holiday/
        exception tests exist to catch, so it belongs with them, not filed as a separate follow-up
      - Two of my own test mistakes: mutating a DETACHED `Provider` (`MakeBookableAsync` clears the
        change tracker) and a `.Should().Be()` on exact `DateTime` ticks across a Postgres round-trip
- [ ] 3 Delete all `.feature` files, `Features/`, `StepDefinitions/`, `Hooks/TestHooks.cs`,
      `Infrastructure/ServiceCatalogReqnrollTestBase.cs`, `Support/ScenarioContextHelper.cs`,
      `StepDefinitions/Availability/README.md`, `Features/README.md`. Leave
      `Infrastructure/FakeNotificationGateways.cs`/`FakePaymentGateway.cs` — used by the real
      xUnit `ServiceCatalogTestWebApplicationFactory`, not Reqnroll-only
- [ ] 4 Remove `Reqnroll`/`Reqnroll.xUnit`/`Reqnroll.Tools.MsBuild.Generation` package refs from
      the csproj; delete `Directory.Build.props`/`.targets` in that project (100% Reqnroll
      workaround config, nothing else); keep Moq (used by real xUnit payment/auth tests)
- [ ] 5 `scripts/verify.ps1`/`.sh`: drop the `Features` exclusion filter and `-IncludeFeatures`/`--features`
- [ ] 6 Docs: `AGENTS.md` (testing-policy paragraph + verify-tier note), `CLAUDE.md` (Test Suites),
      `docs/AUTONOMOUS_OPERATING_MODEL.md`, `docs/KNOWLEDGE_MAP.md` (3 lines), `openspec/project.md`
      (Testing Strategy). Delete `docs/REQNROLL_TESTING.md`. Move
      `openspec/changes/REQNROLL-COVERAGE-GAP.md` to `docs/archive/` with a closing note (matches
      the existing `docs/archive/REQNROLL_MIGRATION_PLAN.md` precedent — historical, not deleted).
      `docs-site/docs/testing/reqnroll-quickstart.md` + its sidebar entry + README.md reference:
      delete (the site is otherwise explicitly "do not trust" per `KNOWLEDGE_MAP.md` and out of
      scope beyond this)
- [ ] 7 `git grep -i` for `reqnroll|specflow|gherkin` returns nothing outside `docs/archive/` and
      openspec change archives
- [ ] 8 FULL verify green

## Decisions
- 2026-09-11 Payment scenarios (Behpardakht + ZarinPal + generic ProcessPayment/RefundPayment) are
  NOT ported. They carry zero real coverage today (see audit above), so there is nothing to
  migrate — porting them would mean writing new tests from the Gherkin's stated intent rather than
  preserving anything that runs today. FOLLOW-UPS #31 remains open for whoever fixes the
  credentials/DI gap; Behpardakht additionally has no route to book against at all.
- 2026-09-11 The "legacy individual sub-provider" and "customer books at different times" Reqnroll
  scenarios are not ported — see audit above for why each is redundant.

## Log
- 2026-09-11 Opened directly from the user's instruction. Audit performed by: static step-binding
  analysis (739 scenarios, 211 bindings), full-repo grep + git log for Behpardakht's routes,
  reading `FOLLOW-UPS.md` #31 and `REQNROLL-COVERAGE-GAP.md` (pre-existing, 2026-08-24), and
  reading the actual handler/resolver code behind every scenario judged "unique" before writing
  its xUnit port, to avoid porting a duplicate or missing what the scenario actually asserted.
