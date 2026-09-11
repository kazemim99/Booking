# Test Architecture Audit — Booksy (2026-09-11)

Analysis and plan only. Nothing in this document has been implemented. It was written the day after
Reqnroll/SpecFlow was retired (`openspec/changes/_inline/retire-reqnroll`), from the code as it stood
at `7af2e9fc` on `feat/provider-auth-flutter`, and from a measured run of the ServiceCatalog suite.
Every number below is either read from `.verify/status.json` / `.verify/logs` of the last green FULL
verify (run 2026-09-11 10:59–11:15) or measured in this audit; estimates are labelled as estimates.

The intended reader is the implementing agent. Section 9 is the roadmap; sections 1–8 are the evidence
for it. Where a decision is not the implementer's to make it is marked **tier-3** per
`AGENTS.md › Operating Model`.

---

## A. Executive summary

The suite is in far better shape than a month ago: 1 400 backend tests, all green, an empty
known-failures list, and a FULL verify that is a real gate. The cost of that gate is the problem.
A FULL verify takes **15.5 minutes**, and **8.2 of those minutes are one project**
(`Booksy.ServiceCatalog.IntegrationTests`, 513 tests) running strictly serially because the fixture
design gave it no other way to be deterministic.

The root cause is structural, not tuning: **every test class boots its own copy of the whole
monolith** — a new `WebApplicationFactory`, a new database, two sets of EF migrations (one of them
redundant), the CAP outbox schema, both hosted background services, and — because the test environment
is named `Test` and the host seeds whenever the environment name contains `Test` — the **full
development seed set** (providers, staff, services, 800 lines of notification templates, provinces
and cities, payments, payouts, reviews). Fifty-three classes, fifty-three boots. Test isolation is then
nominal (`CleanDatabaseAsync` is a no-op), so parallelism had to be switched off to stop the suite
racing itself.

Three findings matter more than everything else:

1. **The UserManagement integration suite tests a host that does not exist in production.** It
   references only `Booksy.UserManagement.API` (the retired per-service host) and boots that
   project's `Program`, not `Booksy.Host`. The ServiceCatalog suite was retargeted in August; this one
   was not. Its 42 green tests say nothing about the composed monolith's DI, middleware, auth policy or
   cross-context adapters.
2. **Fixture cost is the suite.** Measured (§2.2, §8): of 443 s of test time in the ServiceCatalog
   suite, **407 s (92 %) is the ≈ 9 s host boot paid by each of 43 classes**; the other 460 tests run
   in 36 s at a median of 21 ms. One host per collection with a per-test database reset brings the
   suite to roughly 100–120 s **serially**, before any parallelism is reintroduced.
3. **The tooling wastes ~2.5 minutes of every verify on repeated MSBuild evaluation.** `verify.ps1`
   builds the solution once and then runs ten separate `dotnet test` invocations *without*
   `--no-build`. Unit projects whose tests run in 7 s total take 61 s of wall clock; the three DB
   projects lose another ~90 s the same way.

Target after the roadmap: FULL verify in **4–6 minutes with touched Vue/Flutter apps, 2.5–4 minutes
without**, with genuine per-test isolation, one integration project that boots the one production
host, and rules a new developer can apply without reading 900 lines of base class.

---

## B. Current problems ranked by impact

| # | Problem | Impact | Evidence |
|---|---------|--------|----------|
| 1 | Host booted **per test class** in all three DB projects (53 + 8 + 4 boots per FULL run), with seeding on | Time: the majority of the 8.2 min SC step; memory; 53 hosts appending to the same `logs/booksy-host-*.txt` | `IntegrationTestBase : IClassFixture<TFactory>`; `Program.cs:269` `seed = … EnvironmentName.Contains("Test")`; `TestWebApplicationFactory` sets `UseEnvironment("Test")` |
| 2 | **No test isolation**: `CleanDatabaseAsync` is a no-op in both bases; state accumulates within a class; parallelism disabled to compensate | Reliability: nondeterministic suite documented in FOLLOW-UPS #45; `AssemblyInfo.cs` serialises the suite; any "list/count" assertion depends on ordering | `IntegrationTestBase.cs:74-79`, `ServiceCatalogIntegrationTestBase.cs:36-47`, `AssemblyInfo.cs` |
| 3 | **UserManagement suite boots the retired per-service host** | Correctness of the gate: 42 tests validate a composition that never ships; cross-context in-process adapters (`InProcess*`) are absent from what they test | `Booksy.UserManagement.IntegrationTests.csproj` references `Booksy.UserManagement.API`, not `Booksy.Host`; `UserManagementIntegrationTestBase` uses `Program` from that assembly |
| 4 | `verify.ps1`/`.sh` run every `dotnet test` **without `--no-build`** after already building the solution | ~140 s per FULL, ~50 s per FAST, pure waste | `verify.ps1:169,185`; unit steps 61 s wall vs 7 s test time; SC step 492.6 s vs 464 s test time |
| 5 | Duplicate `Migrate()` + `services.BuildServiceProvider()` inside `ConfigureServices` | Time per class; a throw-away second container of singletons per factory (ASP0000 anti-pattern); the host migrates anyway | `TestWebApplicationFactory.cs:138-141` |
| 6 | Three fixture stacks for one host: `ServiceCatalogTestWebApplicationFactory` (env `Test`, seeds), `UserManagementTestWebApplicationFactory` (retired host), `HostCompositionFactory` (env `Staging`, no seed) | Maintainability: three ways to boot, three environments, three sets of overrides; a fix lands in one and not the others | the three factory files |
| 7 | Dead and orphaned test code in the tree | Confusion for every new contributor; `verify.ps1` silently skips a listed project | `tests/Booksy.Tests.Common/` (not in `.sln`, csproj at `…/Booksy.Tests.Common.csproj/Booksy.Tests.Common1.csproj`, 90 package refs incl. BenchmarkDotNet/Verify/Bogus); `tests/Booksy.ServiceCatalog.UnitTests/` has one `.cs` file and **no csproj** (9 `[Fact]`s never compiled; listed in both verify scripts); `UnitTest1.cs` placeholder |
| 8 | 8 pure unit-test files live inside the Docker-gated integration project (`SC.IntegrationTests/Unit/`) | They run only in FULL, serially, and CI's `~UnitTests` filter never sees them (namespace is `IntegrationTests.Unit`); `TokenServiceTests` covers the HTTP `TokenService` that `Booksy.Host` replaced with `InProcessTokenService` | file list in §1.2 |
| 9 | Authentication is a **per-factory singleton** (`TestUserContext`) mutated by tests; the handler mints a real JWT on every request to feed loopback HTTP calls that no longer exist | Shared mutable state; impossible to run two identities concurrently; the per-test reset in `InitializeAsync` is a patch over the design | `IntegrationTestAuthenticationHandler.cs`, `TestWebApplicationFactory.cs:111` |
| 10 | Test-data construction duplicated six times in the SC base (`Provider.RegisterProvider(...)` with the same literals) plus `ProviderBuilder`/`ServiceBuilder` in Commons (used 7 times, reflection to set `Service.Status`, `WithCategory(string)` ignores its argument, US-shaped address in an Iranian product) | Fixture drift is exactly how the suite went red before (`MakeBookableAsync` remarks); `CreateProviderWithStatusAsync` passes `BusinessAddress.Create` arguments in the wrong order | `ServiceCatalogIntegrationTestBase.cs:302-331, 359-398, 403-453, 575-605, 608-630, 763-795`; `ServiceBuilder.cs:56-60,110-117` |
| 11 | HTTP helpers deserialise with **Newtonsoft** while the host serialises with **System.Text.Json** (camelCase, string enums); `AssertStatusCode` is a no-op with its body commented out (0 callers); `FindEntityAsync(Func<>)` evaluates client-side | Latent mismatches (enum casing, `DateTimeOffset`), silent non-assertions | `IntegrationTestBase.cs:125-296, 399-403` |
| 12 | Package drift, no central package management: FluentAssertions **6.12 in five projects, 8.7 in four**; Moq in 6 files, NSubstitute in 30; Testcontainers 3.7 in the dead project vs 4.7 | Two assertion dialects; **FluentAssertions ≥ 8 is under Xceed's commercial licence for commercial use** — a tier-3 decision (see §7) | csproj grep in §1.1 |
| 13 | Timing and randomness: `Task.Delay(100)` to force a timestamp change; `Random.Shared` phone numbers in 13 places; domain reads `OTP_SANDBOX_CODE` straight from `Environment`; unit tests mutate process env vars | Flake sources once parallelism returns; untestable time | `GalleryManagementTests.cs:743`; `OtpCode.cs:51`; `OtpCodeTests.cs` |
| 14 | Two tests skipped with a stale reason ("EFCore composite key issue" — the ADR-005 fix addressed exactly that key) | Coverage gap on `GET /bookings/my-bookings`, hidden behind `Skip` | `BookingsControllerTests.cs:312,337` |
| 15 | Two raw-`DbContext` persistence tests build the context with substituted services; one migrates, one uses `EnsureCreated` to dodge EF's pending-model-changes check — so a concurrency invariant is tested against a schema that lacks the migration-only constraints | A "one person per phone" test that cannot see the partial unique index it exists to prove | `UserRepositorySaveTests.cs:125-146`, `PersonProvisioningConcurrencyTests.cs:42-56` |
| 16 | CI: the integration job is disabled with a comment that is now false (the suite passes 513/513); no per-test timing is ever collected (`-v q`, no trx) | The strongest gate runs only on developer machines; nobody can see which tests are slow | `dotnet.yml:103-118`; `verify.ps1:139` |
| 17 | Every booted host logs at production verbosity to the console **and** to a rolling file under the test bin directory: one suite run emits **4.57 million lines / 1.4 GB** of console output | Wall time (writing 1.4 GB to the verify log), unreadable failure output, disk churn; the trx logger produced no file for this project on two attempts, plausibly for the same reason | measured in §8; `Program.cs:41-49` Serilog `WriteTo.Console()` + `WriteTo.File(...)` with no environment gate |

---

## 1. Current state audit

### 1.1 Projects

| Project | Kind | Tests | Test time | Verify step | Boots host? | Notes |
|---|---|---|---|---|---|---|
| `Booksy.Core.Domain.UnitTests` | unit | 44 | 0.45 s | 5.1 s | no | FA 6.12 |
| `Booksy.Infrastructure.Core.UnitTests` | unit | 11 | 0.40 s | 4.6 s | no | FA 6.12 |
| `Booksy.ServiceCatalog.Domain.UnitTests` | unit | 548 | 0.19 s | 4.4 s | no | exemplary: pure aggregate/VO tests |
| `Booksy.ServiceCatalog.Application.UnitTests` | unit | 205 | 0.62 s | 16.3 s | no | NSubstitute; contains `UnitTest1.cs` placeholder |
| `Booksy.UserManagement.Application.UnitTests` | unit | 44 | 3 s | 12.5 s | no | `OtpCodeTests` mutates env vars |
| `Booksy.ArchitectureTests` | unit | 8 | 2 s | 18 s | no | builds EF model in-memory; NetArchTest |
| `Booksy.Host.CompositionTests` | db | 21 (4 classes) | 45 s | 75.6 s | yes ×4, env `Staging` | only project already targeting `Booksy.Host` correctly |
| `Booksy.ServiceCatalog.IntegrationTests` | db | 513 (53 classes, 2 skipped) | 7 m 44 s | 492.6 s | yes ×53, env `Test` (seeds) | parallelism disabled; 8 unit-test files inside |
| `Booksy.UserManagement.IntegrationTests` | db | 42 (8 classes) | 35 s | 66.5 s | yes ×6 (retired `UserManagement.API` host) + 2 raw-context classes | not retargeted to the monolith |
| `Booksy.Tests.Commons` | shared | – | – | – | – | fixtures, factory, auth handler, 2 builders, AutoFixture |
| `Booksy.Tests.Common` | **dead** | – | – | – | – | not in `.sln`; nested csproj path; kitchen-sink packages |
| `Booksy.ServiceCatalog.UnitTests/` | **orphan dir** | 9 `[Fact]` never compiled | – | skipped | – | listed in `verify.ps1:162` and `verify.sh:77` |

Verify step time minus test time is MSBuild re-evaluation (no `--no-build`): 5–16 s per unit project,
~30 s per DB project.

Non-.NET: Vue `booksy-frontend` (12 vitest spec files, 4 Playwright specs, 1 Cypress spec),
`booksy-admin` (vitest), Flutter customer app (26 test files), provider app (44). Vue/Flutter steps
cost 193 s in the last FULL run (all four apps touched). E2E: `tests/e2e/keystone-booking-flow.sh`
(curl, deploy gate in `deploy.yml`), `deposit-checkout-flow.sh` (externally blocked), Playwright
(`frontend-e2e.yml`, advisory).

### 1.2 What is where (ServiceCatalog integration project, 59 files)

- **Infrastructure**: `ServiceCatalogTestWebApplicationFactory` (adds `FakePaymentGateway` + four
  notification fakes), `ServiceCatalogIntegrationTestBase` (901 lines: DB helpers, auth helpers,
  six provider factories, `MakeBookableAsync`, notification helpers, endpoint helpers), `GlobalUsing`
  (aliases `Startup` → `Booksy.Host.HostEntryPoint`), `AssemblyInfo` (`DisableTestParallelization`).
- **API tests** (44 classes) under `API/{Authorization,Bookings,Categories,Memberships,Notifications,
  Payments,Persistence}` and 14 more at the root (`ProvidersControllerTests` 949 lines,
  `GalleryManagementTests` 831, `WorkingHoursManagementTests` 733, `ProviderSettingsControllerTests`
  647 **and** `ProviderSettingsTests` 498 — same area, two files, …). Mixed naming: `Method_Scenario_
  ShouldReturnX` and sentence style `Owner_Can_Terminate_A_Staff_Member` coexist, sometimes in one file.
- **`Unit/`** (8 files, no I/O): `ClaimsPrincipalGetUserIdTests`, `FakeZarinPalGuardTests`,
  `FakeZarinPalServiceTests`, `PaymentGatewayFactoryTests`, `ProviderSearchFilteringAndSortingTests`,
  `ProvidersByStatusMappingTests`, `ServicesControllerAuthorizationTests`, `TokenServiceTests`.
- **Concurrency tests** (share the connection pool; the first to flake under parallelism):
  `BookingSlotIntegrityTests` (3 × 40 simultaneous inserts), `IdempotencyStoreTests` (24),
  `PaymentDedupTests`, `RegisterAndAcceptInvitationTests.Concurrent_*` (2), and in UM
  `PersonProvisioningConcurrencyTests`.
- Stray `[Collection("Integration Tests")]` on four classes with no matching `[CollectionDefinition]`
  (harmless today because everything is serial; misleading).

### 1.3 Infrastructure, in the order a request meets it

1. **Container**: `PostgresTestContainerFixture` — one static `postgres:16-alpine` per test process
   (good; the earlier design started 102 containers), a fresh `CREATE DATABASE booksy_test_<guid>` per
   fixture, `DROP … WITH (FORCE)` on dispose. The `PostgresTestCollection` collection definition it
   declares has **zero users**.
2. **Factory**: `TestWebApplicationFactory<TStartup,TDbContext>` — constructs the fixture
   synchronously in its constructor (`.GetAwaiter().GetResult()`), overrides connection strings via
   `UseSetting` (correct — the comment explains why `ConfigureAppConfiguration` is too late),
   swaps auth, swaps `IDistributedCache`, raises OTP abuse limits, then **builds a throw-away service
   provider and runs `Migrate()` for one context**, then `UseEnvironment("Test")`.
3. **Host startup** (`Booksy.Host/Program.cs:269-276`): migrates **both** contexts again, seeds both
   because `"Test".Contains("Test")`, starts CAP (Postgres outbox, `cap` schema, in-memory transport,
   dashboard) and two `BackgroundService`s (`PaymentReconciliation`, `LedgerMaintenance`).
4. **Per test** (`IntegrationTestBase.InitializeAsync`): new DI scope, test-owned `DbContext`, clear
   the singleton test user, `CleanDatabaseAsync()` (no-op).
5. **Auth**: `TestUserContext` singleton per factory; `IntegrationTestAuthenticationHandler` reads it,
   builds claims, **signs a JWT with a fallback secret and writes it into the request's
   `Authorization` header** so downstream loopback HTTP calls could reuse it — those calls were
   retired by the in-process adapters, so this is dead weight. Admin carries three role spellings
   (production debt, FOLLOW-UPS #46).
6. **External dependencies**: payment gateway and four notification channels faked in SC; SMS faked
   (capturing variant) in UM; Redis replaced by in-memory cache; `EventBus:Provider=InMemory` in host
   config; CAP still requires Postgres (creates its schema per database).
7. **Data**: hand-rolled aggregate construction in the base + `CreateEntityAsync` through the test's
   own `DbContext` (so domain events never dispatch — `MakeBookableAsync` has to evict the provider
   cache by hand); `Random.Shared` phone numbers; `Guid.NewGuid()` for uniqueness.

### 1.4 Duplicated patterns

- Six copies of "register an active provider with the same literal address" in the SC base, two of them
  overloads of the same method name with different bodies (`CreateTestProviderWithServicesAsync()`
  authenticates; `CreateTestProviderWithServicesAsync(int)` does not).
- Two SMS fakes (`FakeSmsGateway` non-capturing in SC, `FakeSmsNotificationService` capturing in UM)
  for what the memory notes describe as a three-interface SMS subsystem.
- Two `Tests.Common*` helper projects, one dead, containing near-identical `PostgresTestContainerFixture`
  and `IntegrationTestBase` copies (`Class1.cs`/`Class2.cs` are the builders under other names).
- Three host factories, three environments (`Test`, `Staging`, and the UM API's own).
- `AuthenticateAsProviderOwner` (SC base) vs `TestUser.Provider(email, providerId)` (Commons) vs
  `AuthenticateAsCustomerWithId` (UM base) — three ways to say "act as this person".
- `ProviderSettingsControllerTests` and `ProviderSettingsTests`; `AvailabilityControllerTests` and
  `AvailabilityStaffIsolationTests`; root-level vs `API/`-folder placement for the same kind of test.

### 1.5 Slow, flaky, and poorly isolated

- **Slow**: everything in the SC project by construction (§2). Individually heavy: the three
  `BookingSlotIntegrityTests` stress seeds (40 concurrent inserts each), `GalleryManagementTests`
  (file uploads in batches of 10), `MakeBookableAsync` callers (production sync + cache eviction +
  read-back per provider).
- **Flaky history**: FOLLOW-UPS #45 (60 vs 61 failures on identical code with different sets); fixed
  by serialising, not by isolating. `known-failures.txt` header records "the FULL verify has twice
  failed on an off-baseline test that passes in isolation".
- **Isolation gaps**: within-class state accumulation; singleton fakes accumulate captured messages
  across tests; singleton `TestUserContext`; host log file shared by every factory in the process;
  `Environment.SetEnvironmentVariable` in `OtpCodeTests` (process-global, guarded only by a
  `[Collection]` that other classes could join by accident).
- **Wrong schema under test**: `PersonProvisioningConcurrencyTests` uses `EnsureCreated`, so any
  migration-only DDL (partial unique indexes, the GiST exclusion constraint pattern used elsewhere) is
  absent from the database it tests against.

### 1.6 Unnecessary integration boundaries

- Validation permutations tested through HTTP (`RegisterProvider_WithMissingBusinessName_…`,
  `…WithInvalidPhoneNumber_…`, and their siblings across `Providers`, `WorkingHours`, `Bookings`,
  `Payments`): each costs a full request against a booted host to exercise a FluentValidation rule that
  a validator unit test covers in microseconds. One 400-path per endpoint proves the wiring; the rest
  belongs in `*.Application.UnitTests/Validators`.
- `Unit/` folder inside the integration project (above).
- Composition tests that only resolve a service from DI (`The_Host_Resolves_The_InProcess_Adapter`,
  `The_Adapter_Is_Scoped`) do not need four separate host boots; one shared host per collection is
  enough.

---

## 2. Test execution performance

### 2.1 Where the 932 seconds went (last FULL, 2026-09-11)

| Step | Seconds | Share | Of which pure test time |
|---|---|---|---|
| build | 22.8 | 2 % | – |
| 6 unit projects | 61.0 | 7 % | ~7 |
| `db:Host.CompositionTests` | 75.6 | 8 % | 45 |
| `db:ServiceCatalog.IntegrationTests` | 492.6 | **53 %** | 464 |
| `db:UserManagement.IntegrationTests` | 66.5 | 7 % | 35 |
| Vue type-check/lint (2 apps) | 48.5 | 5 % | – |
| Flutter analyze/test (2 apps) | 145.0 | 16 % | – |
| **Total** | **931.7** | | |

Steps run strictly sequentially. The Vue and Flutter steps only run when those apps are touched.

### 2.2 Measured: per-class boot cost in the ServiceCatalog suite

Measured in this audit (`dotnet test … --no-build --logger "console;verbosity=normal"`, same machine
and Docker as the FULL run above; 513 tests, 53 classes, wall clock 7 m 58 s; full table in §8):

| Quantity | Value |
|---|---|
| Sum of all per-test durations | 443.3 s |
| Sum of the **first test of every class** (where xUnit attributes `IClassFixture` construction: container/database creation, host boot, both migrations, seeding, CAP init) | **407.2 s (92 %)** |
| Sum of the **other 460 tests** | **36.1 s** |
| Median first-test-in-class (= host boot cost per class) | **9.0 s** (min 9, max 19 — the 19 s class was the first to run and paid the container start) |
| Median / mean / p90 of every other test | 21 ms / 78 ms / 145 ms |
| Unattributed wall time (class dispose: host shutdown, `DROP DATABASE … WITH (FORCE)`) | ≈ 35 s |
| Console output produced by the 53 hosts during the run | **4.57 million lines, 1.4 GB** |

Read plainly: **the tests are fast; the fixture is the suite.** Nine seconds × 53 classes is the
whole problem. The 10 unit-test classes hiding in this project (`Unit/`, `VerifyZarinPalEndpointWiring`,
`SignalRAccessTokenExtractor`) cost 0.2 s in total and are the only classes that do not boot a host.
The slowest genuine test outside a class boundary is `GalleryManagementTests.GalleryManagement_AllowsReUpload_AfterDeletion`
at 3 s (file uploads); everything else is under 1 s.

Consequence for the plan: with **one host boot per collection** and a per-test reset costing
~50–150 ms, this suite's serial floor is roughly 9 s (container) + 9 s (one boot) + 36 s (tests) +
513 × ~0.1 s (reset) ≈ **100–120 s** — a 4× reduction with **no parallelism at all**. Parallelism (L8)
then becomes an optimisation to take the number under a minute, not a prerequisite.

### 2.3 Levers, with expected impact

| # | Lever | Current | Expected after | Complexity | Risk |
|---|---|---|---|---|---|
| L1 | `--no-build` on every `dotnet test` in `verify.ps1`/`.sh` once the solution build step has run (fall back to building only when `-SkipBuild` was **not** given and the build step failed) | unit 61 s, db overhead ~90 s | unit ~12 s, db overhead ~5 s → **≈ 135 s saved per FULL, ≈ 50 s per FAST** | trivial | none; `--no-build` requires the build step, which the script already has |
| L2 | Run the unit projects in one `dotnet test` over a solution filter (`tests/unit.slnf`) so vstest parallelises assemblies | six sequential invocations | one invocation, ~8 s | low | verify's per-step log parsing changes; keep per-project lines by parsing the combined log or accept one "unit" step |
| L3 | Turn seeding off under test: replace `EnvironmentName.Contains("Test")` (Host `Program.cs:269`, SC `Startup.cs:222`) with an explicit `Database:SeedOnStartup` setting (default `true` in Development, unset elsewhere); factory sets `false` | full dev seed per class × 53 | seed never runs in tests | low | Development behaviour unchanged if the default is kept; e2e workflows use `ASPNETCORE_ENVIRONMENT=Development` and still seed |
| L4 | Delete the duplicate `Migrate()` + `BuildServiceProvider()` in `TestWebApplicationFactory.ConfigureServices` | 2 migrates per class per context | 1 | trivial | none; the host migrates at startup (`MigrateAndSeedDatabaseAsync`, `InitializeDatabaseAsync`) |
| L5 | **Template database**: migrate one `booksy_template` per process, then `CREATE DATABASE … TEMPLATE booksy_template` per fixture (≈ 100–300 ms) instead of running 24 migrations + CAP DDL per fixture | migrations per class | one migration per process | medium | CAP's storage initialiser still runs per host (idempotent); Postgres refuses `TEMPLATE` while another session is connected to the template — keep the template connection-free after migrating (`WITH (FORCE)` drop of stray connections, or `pg_terminate_backend`) |
| L6 | **One host per collection instead of per class** (`ICollectionFixture<HostFixture>`), with per-test reset (L7) | 43 boots × ≈ 9 s = 407 s | 1–7 boots → **≈ 350–390 s saved, the single largest lever** | medium | needs L7 for isolation; tests that today rely on class-level singleton state (`TestUserContext`, fakes) must be made per-test (§4) |
| L13 | Silence host logging under test: `appsettings.Testing.json` sets Serilog minimum level `Warning`, no file sink; the factory routes what remains to xUnit output (§6.3) | 1.4 GB console output per SC run | a few thousand lines | trivial | none; also the likely fix for the missing trx |
| L7 | **Respawn** (or an equivalent `TRUNCATE … CASCADE` over both schemas, excluding `__EFMigrationsHistory` and `cap.*`) in `InitializeAsync` | no-op cleanup | real isolation, ~50–150 ms per test | low | reference data seeded by migrations must be excluded; verify with a "second test sees empty tables" smoke test |
| L8 | **Re-enable parallelism at collection granularity**: remove `DisableTestParallelization`, define collections `ServiceCatalog.Bookings`, `.Payments`, `.Providers`, `.Memberships`, `UserManagement`, `Composition`, `Concurrency` (serial, its own host); `xunit.runner.json` `maxParallelThreads: 4` | serial | up to 4–6 collections concurrently, each with its own host + database | medium | CPU contention on a laptop; the `Concurrency` collection must own its host so the pool is not shared; process-global state (env vars, `Npgsql` global config, static caches) must be audited — see §5 |
| L9 | Composition tests: one `HostCompositionFactory` per collection | 4 boots, 45 s | 1 boot, ~15 s | trivial | none |
| L10 | Run the Vue/Flutter steps concurrently with the DB steps in `verify.ps1` (PowerShell jobs, separate log files) | +193 s serial when touched | hidden behind the DB steps | low-medium | log/exit-code plumbing; CPU contention with L8 — cap parallel threads |
| L11 | Retarget the UM suite to `Booksy.Host` and merge the three DB projects (§4) | three assemblies, three container/host stacks | one | medium | mostly moves; namespaces change |
| L12 | Collect trx + `--blame-hang-timeout 5m` in verify and CI | no per-test data | slow-test report per run | trivial | none |

**Sequencing matters**: L1/L3/L4/L9/L12 are independent quick wins. L5–L8 are one design and should
be done together (§4.6), after L11 so it is built once.

### 2.4 Expected FULL verify after the roadmap (estimate)

| Phase | Backend-only FULL | With all four apps touched |
|---|---|---|
| today | ~12.3 min | 15.5 min |
| after Phase 1 (L1, L3, L4, L9, L12, L13, quick moves) — **measured 2026-09-11, see below** | **~5 min** | **12 min (contended) / ~8 min clean** |
| after Phase 2 serial (L5–L7, L11; no parallelism) | ~3–3.5 min | ~4.5–5.5 min (apps concurrent, L10) |
| after Phase 2 with L8 (4 collections in parallel) | ~2–2.5 min | ~3.5–4.5 min |

Basis: the measured 36 s of non-boot test time in the SC suite (§2.2), ≈ 9 s per host boot, a per-test
reset at ~0.1 s, the UM and composition suites shrinking the same way (their boots are the same 9 s),
plus build (23 s) and units (~12 s after L1/L2). Do L8 last and only if the serial number is not
already acceptable on CI.

### 2.4a Phase 1, measured (2026-09-11, `openspec/changes/test-architecture-phase-1`)

FULL verify green at **720 s** against the 931.7 s baseline, with 1 444 backend tests and no skips.
The per-step comparison, and the reason the headline number understates the change:

| step | baseline | after Phase 1 | note |
|---|---|---|---|
| build | 22.8 | 66.4 | contended by two peer sessions; unrelated to this work |
| unit projects (6 → 8) | 61.0 | 36.6 | `--no-build`, plus 105 tests that moved *into* this tier |
| db:Host.CompositionTests | 75.6 | 33.9 | four host boots → one |
| db:ServiceCatalog.IntegrationTests | 492.6 | 156.2 contended / **130.7 alone** | 420 tests (93 moved out), 464 s → 121 s of test time |
| db:UserManagement.IntegrationTests | 66.5 | 105.9 contended / **28.7 alone** | see below |
| **FULL total** | **931.7** | **720.0** | contended; `flutter analyze` alone was 103 s against 64 s |

Two things are worth keeping from this measurement:

- **Logging cost more than seeding.** The audit assumed the full development seed was the bulk of the
  9 s per-class boot. It was worth ~1.8 s. The larger share was the 4.57 M lines of host console output
  a suite run produced: quieting it took the ServiceCatalog suite from 464 s to 141 s of test time, far
  beyond what removing the seed explains. Boot cost per class is now ~6 s, still dominated by the 26
  migrations and CAP's schema — which is exactly what Phase 2's template database removes.
- **Measure a parallel suite alone.** UserManagement is the only suite whose classes run in parallel, so
  it is the one that degrades under a busy machine: 105.9 s inside the contended FULL run, 28.7 s alone
  against a 35 s baseline. A single number from a shared developer machine is not evidence on its own.

---

## 3. Test pyramid review

### 3.1 What belongs where — the rule

Ask, in order:

1. **Can the behaviour be observed on a domain object or a handler with its ports faked, with no I/O?**
   → **Unit test.** Aggregates, value objects, policies, calculators, validators, command/query
   handlers (repositories substituted), pipeline behaviours, specifications, mapping.
   *Rule of thumb: if the test needs `Testcontainers`, `WebApplicationFactory` or a `DbContext`, it is
   not a unit test.*
2. **Does correctness depend on something only the real stack provides?** EF mapping and conventions,
   SQL constraints (GiST exclusion, partial unique indexes, FKs), transactions and concurrency,
   the CAP outbox, middleware (`ApiResponseMiddleware`, exception mapping, auth policy, rate limits),
   the composed DI graph, cross-context in-process adapters, the HTTP contract (status + envelope +
   DTO shape) → **Integration test against `Booksy.Host`.** One integration test per endpoint per
   *outcome class* (success, unauthenticated, forbidden, not found, one representative 400) plus one
   per business scenario that spans components (deposit gating, reschedule resolution, payment →
   ledger). Not one per validation rule.
3. **Does the value come from the whole deployed stack — real network, real OTP sandbox, real
   browser?** → **E2E**, and only for the keystone journeys already scripted (provider sign-up →
   staff → customer booking → membership chain; deposit checkout when unblocked). Nothing else.

Corollaries:

- A bug fix's regression test goes at the **lowest level that reproduces it**. A 500 from the API
  caused by a domain rule gets a domain test *and* one HTTP test for the status mapping, not five HTTP
  tests.
- Concurrency invariants that depend on the database (`BookingSlotIntegrityTests`,
  `IdempotencyStoreTests`) are integration tests and live in the `Concurrency` collection.
- Composition tests (DI shape) are integration tests that assert on `IServiceProvider`, not HTTP.
- Widget/bloc tests in Flutter and vitest specs in Vue follow the same three questions in their own
  stacks (`AGENTS.md › Mobile App Testing` already says this).

### 3.2 What should move

| From | To | Why |
|---|---|---|
| `SC.IntegrationTests/Unit/ClaimsPrincipalGetUserIdTests` | `Core.Domain.UnitTests` (or `Infrastructure.Core.UnitTests`, wherever the extension lives) | no I/O |
| `…/Unit/ProviderSearchFilteringAndSortingTests`, `ProvidersByStatusMappingTests` | `SC.Application.UnitTests/Specifications`, `/Mapping` | specifications and mapping are pure |
| `…/Unit/PaymentGatewayFactoryTests`, `FakeZarinPal*Tests` | new `Booksy.Infrastructure.External.UnitTests` (also home for the orphaned `ZarinPalServiceTests`) | infrastructure adapters with `HttpMessageHandler` fakes |
| `…/Unit/ServicesControllerAuthorizationTests` | `SC.Application.UnitTests/Authorization` if it tests the policy, else keep as integration | read it before moving |
| `…/Unit/TokenServiceTests` | **delete together with the retired HTTP `TokenService`** (Host registers `InProcessTokenService`) — production change, separate task; until then, `Infrastructure.External.UnitTests` | protects dead code |
| Validation permutations in `ProvidersControllerTests`, `WorkingHoursManagementTests`, `BookingsControllerTests`, `PaymentsControllerTests`, `ProviderSettings*Tests` | validator unit tests (`*CommandValidatorTests`); keep one 400 per endpoint | §1.6 |
| Handler tests that build an `HttpContext` to convey the caller (`TerminateMembershipCommandHandlerTests` and siblings) | unchanged tests, but flag: handlers depending on `IHttpContextAccessor` instead of `ICurrentUserService` is a production smell that makes every such test heavier | note for the backlog |
| Cypress spec | delete; Playwright is the one browser E2E stack | two stacks for one job |

### 3.3 Target shape (backend)

```
unit          ~1 000 tests   < 10 s      every commit, FAST tier, CI PR gate
integration    ~450 tests    2–4 min     FULL tier, CI PR gate (Testcontainers)
composition      ~20 tests   in the above
e2e (API)        1 script    ~2 min      deploy gate (unchanged)
e2e (browser)    3–4 specs   advisory / nightly
```

---

## 4. Integration test architecture (target)

### 4.1 One project, one host

`tests/Booksy.Host.IntegrationTests` replaces `Booksy.ServiceCatalog.IntegrationTests`,
`Booksy.UserManagement.IntegrationTests` and `Booksy.Host.CompositionTests`. There is one production
composition (`Booksy.Host`); there is one integration project that boots it.

```
tests/
  Booksy.Tests.Shared/                 (rename of Booksy.Tests.Commons; NO Testcontainers, NO Mvc.Testing)
    Builders/      ProviderBuilder, ServiceBuilder, UserBuilder, BookingBuilder …   (domain objects, no reflection)
    Mothers/       Providers.ActiveSalon(), Users.Customer(), Services.Haircut() …  (canonical shapes on top of builders)
    Clock/         TestClock : IDateTimeProvider
    Ids/           UniquePhone(), UniqueEmail()                                       (deterministic uniqueness)
  Booksy.Host.IntegrationTests/
    Infrastructure/
      Postgres/    PostgresServerFixture (static container + template DB), TestDatabase (clone/drop)
      Host/        BooksyHostFactory (the ONE WebApplicationFactory<HostEntryPoint>), HostFixture (collection fixture)
      Auth/        TestAuthHandler (per-request header), TestUser, HttpClient.As(user)
      Http/        ApiClient (System.Text.Json, host options, ApiResponse<T> envelope)
      Fakes/       FakePaymentGateway, FakeNotificationChannels (email/sms/push/in-app, capturing, resettable)
      Reset/       DatabaseReset (Respawn checkpoint)
      Given/       ScenarioBuilder: Given.ABookableSalon(), Given.ACustomer(), Given.APendingInvitation() …
      IntegrationTest.cs   (base: per-test reset, scope, ReadFresh<T>(), Given, Api)
      Collections.cs       ([CollectionDefinition] ServiceCatalog.*, UserManagement, Composition, Concurrency)
    ServiceCatalog/
      Bookings/ Payments/ Providers/ Memberships/ Notifications/ Availability/ Persistence/
    UserManagement/
      Auth/ Customers/ Persistence/
    Composition/
    Concurrency/
    xunit.runner.json    ({ "maxParallelThreads": 4, "parallelizeTestCollections": true })
```

`Booksy.Tests.Shared` is referenced by unit projects too, so it must stay free of Testcontainers,
`Mvc.Testing` and EF providers. Delete `Booksy.Tests.Common` outright.

### 4.2 Fixture hierarchy

| Level | Type | Lifetime | Responsibility |
|---|---|---|---|
| process | `PostgresServerFixture` (static, lazy, gated) | test run | start `postgres:16-alpine` once; create `booksy_template`; migrate both contexts into it **once** using the host's own `DbContextOptions` (not hand-rolled — that is what produced the pending-model-changes discrepancy); disconnect |
| collection | `HostFixture : ICollectionFixture` | xUnit collection | `CREATE DATABASE <name> TEMPLATE booksy_template`; boot one `BooksyHostFactory` against it; expose `Services`, `Api`, `Fakes`, `Clock`; drop the database on dispose |
| test | `IntegrationTest` base (`IAsyncLifetime`) | one test | `DatabaseReset.ResetAsync()`, `Fakes.Reset()`, `Clock.Reset()`, fresh scope; no `DbContext` field — use `ReadFresh<T>()` / `InScope(sp => …)` |

Why not transaction rollback: the SUT commits in request scopes on its own connections, the CAP outbox
writes in the request transaction and dispatches after commit, and the concurrency tests need real
commits racing. Rollback cannot see any of that. Database reset per test is the only honest isolation
here; with Respawn it costs tens of milliseconds.

Why template-clone rather than one shared database per process: it is what makes collection-level
parallelism possible with zero cross-talk, and cloning is cheaper than the migrations it replaces.

### 4.3 The one factory

`BooksyHostFactory : WebApplicationFactory<HostEntryPoint>`:

- `UseEnvironment("Testing")` and ship `src/Host/Booksy.Host/appsettings.Testing.json` so the test
  configuration is a visible file (in-memory cache, `Database:SeedOnStartup=false`, no Redis, OTP
  sandbox, rate limiting off, `EventBus:Provider=InMemory`) instead of eleven `UseSetting` calls in
  code. Keep `UseSetting` only for the connection string (registration-time consumer, as the existing
  comment documents).
- `ConfigureTestServices`: auth scheme, fakes, `TestClock` as `IDateTimeProvider`, `IDistributedCache`
  → memory, xUnit logger provider (§6.4). **No** `BuildServiceProvider`, **no** `Migrate()`.
- Fix the seed gate in production code (L3) rather than relying on an environment-name string.

### 4.4 Authentication

Replace the singleton `TestUserContext` + JWT minting with a header-driven scheme:

```csharp
// Infrastructure/Auth
var client = Host.Api.As(TestUsers.ProviderOwner(salon));   // sets X-Test-User: <base64 json claims>
var anon   = Host.Api.Anonymous();
```

`TestAuthHandler` reads the header, builds the `ClaimsPrincipal`, and succeeds; no header → `NoResult`.
Identity becomes per request, so two identities can race in one test, nothing is shared between
tests, and the per-test "clear user" reset disappears. Keep `TestUser` factories (`Customer`,
`Provider`, `Admin`, `MemberOf(salon)`), and drop the triple admin spelling once FOLLOW-UPS #46 lands.

### 4.5 Data: builders, mothers, and Given

- **Builders** (`Tests.Shared/Builders`) construct domain objects through domain factories only
  (`Provider.RegisterProvider`, `Service.Create`, `service.Activate()`); no reflection. Defaults are
  product-realistic (Tehran address, `+98` phone, IRR).
- **Object Mothers** (`Tests.Shared/Mothers`) name canonical states: `Providers.ActiveSalon()`,
  `Services.BookableHaircut(provider)`, `Users.CustomerWithPhone(phone)`. They compose builders; tests
  read as prose.
- **Given** (integration only) persists mothers through the **production write path** (repository +
  unit of work, or the API), never through the test's own `DbContext` — so domain events dispatch and
  caches invalidate, and `MakeBookableAsync`'s manual cache eviction is no longer needed. Returns a
  small record of ids (`BookableSalon { ProviderId, OwnerId, OwnerMembershipId, ServiceIds }`).
- **Uniqueness** comes from `Ids.UniquePhone()` (counter + run seed) and `UniqueEmail()`, not
  `Random.Shared`.
- Assertions on persisted state go through `ReadFresh<T>(id)` (new scope, `AsNoTracking`,
  aggregate-shaped includes) — the footgun documented in `FindProviderAsync`'s remarks becomes
  impossible.

### 4.6 Conventions

- **Class per capability**, not per controller: `BookingCancellationTests`, `DepositGatingTests`,
  `ProviderRegistrationTests`. ≤ 25 tests and ≤ 400 lines per class; split by scenario group.
- **Test names**: `Scenario_Outcome` in words, underscores between words, no `Should`:
  `Cancel_by_another_customer_is_forbidden`, `Overlapping_bookings_for_one_member_are_rejected`.
  Existing `Method_Scenario_ShouldReturn` names are migrated opportunistically when a file is touched.
- **AAA** with blank-line separation; comments only when a step is non-obvious. One behaviour per
  test; assert the HTTP outcome **and** the persisted effect when the endpoint writes.
- **Endpoint literals** live once, in `Routes` constants next to the tests that use them.
- **Traits**: `[Trait("Category","Concurrency")]`, `[Trait("Category","Composition")]`,
  `[Trait("Quarantine","<issue>")]` (see §5.3). Verify and CI filter on these.
- **No `Task.Delay`, `Thread.Sleep`, `DateTime.Now`, `Random`** in tests — enforced by
  `BannedSymbols.txt` (`Microsoft.CodeAnalysis.BannedApiAnalyzers`) in the test `Directory.Build.props`.
- **Fakes** are the only doubles in integration tests; NSubstitute is the one mocking library for unit
  tests (migrate the 6 Moq files). One assertion library (§7).

### 4.7 A new test in ten lines (what "easy to add" must look like)

```csharp
[Collection(Collections.ServiceCatalogBookings)]
public class BookingCancellationTests(HostFixture host) : IntegrationTest(host)
{
    [Fact]
    public async Task Cancel_by_another_customer_is_forbidden()
    {
        var salon   = await Given.ABookableSalon();
        var booking = await Given.ABooking(salon, by: TestUsers.Customer());

        var response = await Api.As(TestUsers.Customer()).PostAsync(Routes.Cancel(booking.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadFresh<Booking>(booking.Id)).Status.Should().Be(BookingStatus.Confirmed);
    }
}
```

---

## 5. Flaky test prevention

### 5.1 Sources present today

| Source | Where | Fix |
|---|---|---|
| Shared database with no reset; class-level accumulation | all three DB projects | §4.2 per-test reset |
| Singleton mutable auth context | `TestUserContext` | §4.4 |
| Singleton capturing fakes never cleared | `FakeSmsNotificationService`, `FakeEmailNotificationService` | `Fakes.Reset()` per test |
| Wall-clock dependence | `Task.Delay(100)`; "3 days from now"/"20 days from now" slot arithmetic; OTP expiry | `TestClock` registered as `IDateTimeProvider`; assert `>` against the clock, never sleep |
| Random identifiers | `Random.Shared.Next` phones (13 sites) | `Ids.UniquePhone()` |
| Process-global state | `Environment.SetEnvironmentVariable` in `OtpCodeTests`; domain reads `OTP_SANDBOX_CODE` from `Environment` (`OtpCode.cs:51`); `JwtAuthenticationExtensions` reads `ASPNETCORE_ENVIRONMENT` directly | keep such tests in a dedicated serial collection **and** file the production debt: configuration should be injected, not read from the process environment inside a value object |
| Connection-pool contention from stress tests | `BookingSlotIntegrityTests` (120 concurrent inserts), `IdempotencyStoreTests` (24), `PaymentDedupTests` | `Concurrency` collection with its own host and pool; `Max Pool Size` sized ≥ the largest fan-out |
| Ordering assumptions | any `GetAllProvidersAsync().Should().HaveCount(n)` after other tests in the class | reset per test; assert on ids, not counts |
| Async fire-and-forget in the SUT (CAP dispatch after commit, background services) | notification lifecycle tests | await the observable effect through the fake with a bounded poll (`WaitUntilAsync`, ≤ 5 s), never a fixed delay; disable the two hosted services under test unless a test targets them |
| Host log file shared across 53 hosts | `logs/booksy-host-*.txt` under the test bin dir | xUnit logger provider; no file sink in `Testing` |
| Schema built by `EnsureCreated` instead of migrations | `PersonProvisioningConcurrencyTests` | template DB migrated once with production options; delete the hand-rolled context construction |

### 5.2 Reliability checklist (paste into `tests/README.md`)

Before a test is merged it must be true that:

1. It passes alone, twice in a row, and inside its collection.
2. It creates every row it reads; it never asserts on totals it did not create.
3. It reads persisted state through a fresh scope, never through a context that wrote it.
4. Time comes from `TestClock`; ids from `Ids.*`; no `Random`, no `Delay`, no `Sleep` (analyzer-enforced).
5. It identifies its caller per request (`Api.As(user)`); it never relies on who the previous test was.
6. It touches no process-global state (env vars, statics) unless it is in the serial collection made for that.
7. Concurrent fan-out ≤ the pool size of the `Concurrency` collection, and it lives there.
8. It waits for asynchronous effects with a bounded condition, not a fixed delay.
9. Its failure message says what business rule broke, not just "expected 200 got 400".
10. It is not skipped. A test that cannot pass is quarantined with a trait and an issue (§5.3), not `Skip`.

### 5.3 Quarantine, replacing `Skip` and superseding `known-failures.txt`

`[Trait("Quarantine", "FOLLOW-UPS#nn")]`. Verify and CI run quarantined tests with
`--filter Quarantine!~.` excluded from the gate and report them separately; a quarantined test older
than 14 days fails the nightly. `tests/known-failures.txt` stays as documentation of the 2026-09 debt
pay-down and is retired from `verify.ps1` once the trait exists (its parsing is the one thing in verify
that reads test names out of a `-v q` log).

---

## 6. CI/CD test strategy

### 6.1 Gates

| Gate | Trigger | Runs | Target | Today |
|---|---|---|---|---|
| **PR fast** (`dotnet.yml` build-and-test) | every PR / push | build + all unit & architecture projects, `--no-build`, trx | ≤ 4 min | exists (≈ this) |
| **PR full** (new job in `dotnet.yml`) | every PR | `Booksy.Host.IntegrationTests` via Testcontainers (Docker is on `ubuntu-latest`; no `services:` block needed), `--blame-hang-timeout 5m`, trx artefact; quarantined excluded | ≤ 8 min now, ≤ 5 min after Phase 2 | **disabled with a stale comment** — enable in Phase 1 |
| **Deploy gate** (`deploy.yml` e2e-keystone) | push to master | keystone curl flow against a booted host | ≤ 5 min | exists |
| **Nightly** (new `nightly.yml`) | schedule | full integration **run twice** and diff results (flake detector); quarantine age check; Playwright suite; Flutter `analyze`+`test`; dependency audit; coverage collection (coverlet is already referenced, never used) — report only | ≤ 30 min | none |
| **Release validation** | manual dispatch on staging | keystone + deposit-checkout (when unblocked) + Playwright smoke against staging URL | ≤ 15 min | partial (`deploy-staging.yml`) |

### 6.2 What runs where (local)

- `scripts/verify -Tier fast`: build once, unit projects with `--no-build` (L1/L2).
- `scripts/verify -Tier full`: FAST + integration project (+ Vue/Flutter when touched, concurrently, L10).
- `scripts/verify -Tier full -Filter "FullyQualifiedName~Bookings"` unchanged.
- New: `scripts/verify -Collection ServiceCatalogBookings` sugar over `--filter`.

### 6.3 Failure diagnosis workflow

1. Verify prints the failed test names and the trx path; `.verify/slowest.txt` lists the 20 slowest
   tests of the run (from the trx) so regressions in fixture cost are visible.
2. Host logs for the failing test are in the test output (xUnit `ITestOutputHelper` sink registered
   in the factory — today they are in a rolling file nobody opens).
3. Re-run the class alone: `dotnet test … --no-build --filter "FullyQualifiedName~<Class>"`. If it
   passes alone and fails in the run, it is an isolation bug — checklist §5.2, items 2–7.
4. `--blame-hang-timeout` turns a hung concurrency test into a dump with a stack instead of a 10-minute
   CI timeout.
5. A failure on a `Quarantine`-tagged test is not a gate failure; a *new* failure is a regression
   until proven otherwise (the rule from `known-failures.txt` survives unchanged).

---

## 7. Standards documentation — proposed changes

### 7.1 `AGENTS.md`

- **Replace** `### Test Pyramid & Test Selection` and `### Coverage Expectations` with the three-question
  rule from §3.1 and the "one integration test per outcome class" rule. The current text lists
  fourteen test types and coverage percentages (90 %+) that nothing measures; unmeasured numbers are
  not policy.
- **Add** `### Integration test contract` (one project, one host, per-test reset, per-request auth,
  `Given`/mothers/builders, `ReadFresh`, no reflection, no sleeps) — a compressed §4.
- **Add** `### Flake policy` (§5.2 checklist by reference, quarantine trait, 14-day limit, no `Skip`).
- **Amend** `### Verification tiers`: FAST = unit `.slnf` with `--no-build`; FULL adds the single
  integration project; the known-failures sentence becomes the quarantine sentence.
- **Amend** `### Test Quality Standards`: name the banned APIs and the analyzer that enforces them.

### 7.2 `CLAUDE.md`

- Rewrite `## Test suites` for the new layout (`Booksy.Host.IntegrationTests`, collections, how to run
  one collection/class, where fakes and Given live). Remove the mention of the three retired DB projects.
- Point to `tests/README.md`.

### 7.3 New `tests/README.md` (developer guide, ≤ 2 pages)

Where a test goes (§3.1 table), the ten-line example (§4.7), the fixture levels (§4.2), how to add a
fake, how to add a `Given`, the reliability checklist (§5.2), how to read a failure (§6.3). This is the
document a new developer reads instead of the 900-line base class.

### 7.4 Permanent engineering rules (the short list)

1. One production host, one integration project, one factory.
2. Every integration test starts from an empty database and an anonymous client.
3. Tests construct data through domain factories and persist it through production write paths.
4. No sleeps, no wall clock, no randomness, no reflection into domain state — analyzer-enforced.
5. One integration test per endpoint per outcome class; rules go to unit tests.
6. A test that cannot pass is quarantined with an issue for at most 14 days, never skipped or deleted.
7. Verify builds once; everything after that is `--no-build`.
8. Per-test timing is collected on every run and the slowest 20 are visible.

### 7.5 Tier-3 items (need a human decision; not blocking anything else)

- **FluentAssertions licence.** Versions ≥ 8.0 (four projects are on 8.7.0) require a paid Xceed
  licence for commercial use. Options: (a) pin everything to 7.x (last Apache-2.0 release) — simplest;
  (b) move to AwesomeAssertions (Apache fork of 7.x, drop-in namespace); (c) buy the licence. The
  audit recommends (a) now, (b) if 7.x ever blocks a .NET upgrade.
- **Retired hosts in the solution.** `Booksy.UserManagement.API` and `Booksy.ServiceCatalog.Api`
  keep `Program`/`Startup` and appsettings as runnable hosts. They are needed as *application parts*
  (controllers, DTOs), not as hosts; removing the entry points is a production change that also
  removes the `CS0433 Program` ambiguity the composition tests work around.

---

## 8. Performance measurement appendix

### 8.1 Method

`dotnet test tests/Booksy.ServiceCatalog.IntegrationTests --no-build --logger trx` on the audit machine
(same machine and Docker as the FULL verify quoted above), results parsed from the trx.

The `trx` logger wrote no file for this project on two attempts (it works for the unit projects — the
1.4 GB of console output during the run is the likely cause; item 1.2 should verify after item 1.14
lands). Per-test durations were taken from the console logger's `Passed … [n ms]` lines instead.

### 8.2 Per class (sorted by total; `first` = duration of the first test, which carries the fixture boot; `medrest` = median of the remaining tests)

```
class                                      n   sum(s)  first  medrest
GalleryManagementTests                     31    21.0  10.00   0.25
DepositGatingTests                          4    19.2  19.00   0.06   <- first class run: + container start
PayoutsControllerTests                     14    11.8  10.00   0.16
WorkingHoursManagementTests                18    11.6  11.00   0.04
ProgressiveRegistrationTests               10    11.5  11.00   0.03
InvitationBoundaryTests                     2    11.3  11.00   0.34
PaymentsControllerTests                    17    11.0  10.00   0.05
StepBasedRegistrationTests                 11    10.8  10.00   0.07
ProvidersControllerTests                   37    10.7  10.00   0.01
ProviderStaffTests                         12    10.5  10.00   0.03
ProviderSettingsTests                      15    10.4  10.00   0.02
RegisterAndAcceptInvitationTests            2    10.3  10.00   0.32
CategoriesControllerTests                  20    10.3  10.00   0.01
RescheduleResourceResolutionTests           3    10.2  10.00   0.11
AggregatePersistenceRegressionTests         4    10.2  10.00   0.05
MembershipsControllerAuthorizationTests     3    10.1  10.00   0.06
AvailabilityStaffIsolationTests             6    10.0  10.00   0.01
BookingsControllerTests                    13     9.9   0.00   0.09   <- first test is Skipped; boot landed on the second (9 s)
NotificationsControllerTests               25     9.9   9.00   0.02
AvailabilityControllerTests                14     9.8   9.00   0.04
ProviderSettingsControllerTests            27     9.8   9.00   0.02
BookingSlotIntegrityTests                   4     9.6   9.00   0.20
FinancialControllerTests                    9     9.6   9.00   0.07
ProviderManagementTests                    13     9.5   9.00   0.03
LedgerIntegrationTests                     11     9.4   9.00   0.03
ProviderCategoryPersistenceTests           10     9.4   9.00   0.02
ProviderDepositPolicyTests                 12     9.3   9.00   0.02
ServiceManagementTests                     13     9.3   9.00   0.02
FixtureBookabilityTests                     4     9.3   9.00   0.08
AuthorizationBoundaryTests                  9     9.2   9.00   0.01
MembershipAuthorizationTests                6     9.2   9.00   0.03
BookingCancellationOwnershipTests           3     9.2   9.00   0.08
PaymentReconciliationTests                  5     9.1   9.00   0.04
PaymentDedupTests                           3     9.1   9.00   0.04
VerifyZarinPalStatusHandlingTests           4     9.1   9.00   0.03
NotificationDeliveryReliabilityTests        4     9.1   9.00   0.03
MembershipSchedulePersistenceTests          6     9.1   9.00   0.01
IdempotencyStoreTests                       5     9.1   9.00   0.01
NotificationHubAuthTests                    2     9.1   9.00   0.05
MembershipUniquenessPersistenceTests        3     9.0   9.00   0.02
BookingUpdatePersistenceTests               2     9.0   9.00   0.05
MembershipInfoServiceCrossContextTests      3     9.0   9.00   0.02
DepositCheckoutCouplingTests                1     9.0   9.00   0.00
ProvidersByStatusMappingTests              13     0.1   0.07   0.00   (no host)
ProviderSearchFilteringAndSortingTests     16     0.1   0.03   0.00   (no host)
TokenServiceTests                           5     0.0   0.04   0.00   (no host)
ServicesControllerAuthorizationTests       19     0.0   0.02   0.00   (no host)
VerifyZarinPalEndpointWiringTests           5     0.0   0.01   0.00   (no host)
FakeZarinPalGuardTests                     11     0.0   0.00   0.00   (no host)
PaymentGatewayFactoryTests                  4     0.0   0.01   0.00   (no host)
FakeZarinPalServiceTests                    8     0.0   0.00   0.00   (no host)
ClaimsPrincipalGetUserIdTests               8     0.0   0.00   0.00   (no host)
SignalRAccessTokenExtractorTests            4     0.0   0.00   0.00   (no host)
```

Forty-three classes each pay ≈ 9 s to boot a host they then use for a median of 21 ms per test.
Thirteen of them contain **four or fewer tests**; `DepositCheckoutCouplingTests` boots a host for one.

### 8.3 Slowest tests once the class-boundary cost is excluded

```
 3.00  GalleryManagementTests.GalleryManagement_AllowsReUpload_AfterDeletion
 1.00  GalleryManagementTests.GalleryManagement_SupportsMultipleBatches_UpToLimit
 1.00  GalleryManagementTests.UploadGalleryImages_ExceedsMaxLimit_ReturnsBadRequest
 0.48  GalleryManagementTests.GetGalleryImages_OnlyReturnsActiveImages
 0.48  GalleryManagementTests.SetPrimaryGalleryImage_UnsetsOtherPrimaryImages
 0.48  GalleryManagementTests.SetPrimaryGalleryImage_PrimaryImageAppearsFirst_InQueryResults
 0.46  GalleryManagementTests.GalleryManagement_MaintainsDisplayOrderConsistency_AfterDeletion
 0.45  GalleryManagementTests.ReorderGalleryImages_WithValidOrder_ReordersSuccessfully
 0.43  GalleryManagementTests.GetGalleryImages_WithExistingImages_ReturnsAllImages
 0.34  InvitationBoundaryTests.An_Owner_Cannot_Invite_Their_Own_Phone_Number
 0.33  GalleryManagementTests.SetPrimaryGalleryImage_MultipleTimes_OnlyLastOneIsPrimary
 0.32  RegisterAndAcceptInvitationTests.RegisterAndAccept_Reuses_An_Existing_Account_By_Phone_Never_A_Duplicate_Person
```

No individual test needs optimising. Gallery tests upload real multipart files through the API; that is
the right level for them and 0.5 s is acceptable.

---

## 9. Implementation roadmap (for the implementing agent)

Each item lists **why · files/areas · risk · benefit · priority**. Items marked ⟂ are independent of
everything else in their phase and can be done in any order. Create one OpenSpec change per phase
(`openspec/changes/test-architecture-phase-1` …) with these items as its `tasks.md`; each task ends
with `scripts/verify -Tier fast`, the phase ends with `-Tier full`, and the FULL time is recorded in the
change log so the gains are measured, not assumed.

### Phase 1 — quick wins (hours to two days)

| # | Item | Why | Files / areas | Risk | Benefit | Priority |
|---|---|---|---|---|---|---|
| 1.1 ⟂ | `--no-build` on every `dotnet test` in `scripts/verify.ps1` and `verify.sh`; when `-SkipBuild` is given, keep the current behaviour | L1 | `scripts/verify.ps1:169,185`, `scripts/verify.sh:75-90` | none | ≈ 135 s per FULL, ≈ 50 s per FAST | P0 |
| 1.2 ⟂ | Add `--logger trx --results-directory .verify/trx` and `--blame-hang-timeout 5m` to the DB steps; write `.verify/slowest.txt` (top 20 by duration) from the trx | L12; nobody can see slow tests today | `scripts/verify.*`, `.gitignore` (`.verify/` already ignored?) | none | visibility for every later item | P0 |
| 1.3 ⟂ | Explicit seed gate: `Database:SeedOnStartup` replaces `EnvironmentName.Contains("Test")` in `Booksy.Host/Program.cs:269` and `Booksy.ServiceCatalog.Api/Startup.cs:222`; default `true` when Development; `TestWebApplicationFactory` sets `false`; `HostCompositionFactory` can drop its `Staging` workaround | L3 — part of the measured 9 s per-class boot (§2.2); the share is unknown until this lands, so record the new per-class first-test time | the two entry points, both test factories, `appsettings.Development.json` (document the key) | Development still seeds by default; CI e2e jobs run as Development → unchanged | up to several seconds × 43 classes | P0 |
| 1.14 ⟂ | Quiet the hosts under test: `src/Host/Booksy.Host/appsettings.Testing.json` (Serilog `MinimumLevel: Warning`, console sink only, no file sink) and `UseEnvironment("Testing")` in both factories; confirm the trx logger now writes a file | L13; problem #17 — 1.4 GB per run | Host appsettings, both factories, `verify.ps1` log tail | none | faster runs, readable logs, working trx | P0 |
| 1.4 ⟂ | Remove `services.BuildServiceProvider()` + `dbContext.Database.Migrate()` from `TestWebApplicationFactory.ConfigureServices` | L4; ASP0000; host migrates anyway | `tests/Booksy.Tests.Commons/TestWebApplicationFactory.cs:138-141` | none | ~0.5–1 s × 53 | P0 |
| 1.5 ⟂ | Delete `tests/Booksy.Tests.Common/` (dead), `SC.Application.UnitTests/UnitTest1.cs`; remove `tests/Booksy.ServiceCatalog.UnitTests` from both verify scripts and move its one file (see 1.7) | problem #7 | those paths, `scripts/verify.*` | none | clarity | P0 |
| 1.6 ⟂ | Composition tests share one host: `[CollectionDefinition]` + `ICollectionFixture<HostCompositionFactory>` on the four classes | L9 | `tests/Booksy.Host.CompositionTests/*.cs` | none | 45 s → ~15 s | P1 |
| 1.7 | New `tests/Booksy.Infrastructure.External.UnitTests` (NSubstitute, FA pinned); move `SC.IntegrationTests/Unit/*` there or to the Application/Core unit projects per §3.2; move the orphaned `ZarinPalServiceTests` in (port Moq → NSubstitute while moving); add the project to the `.sln`, both verify scripts and the CI unit filter | problem #8 | new project, `Booksy.sln`, `scripts/verify.*`, `.github/workflows/dotnet.yml` | `TokenServiceTests` tests a retired adapter — move now, delete with the adapter later | 8 files leave the Docker tier; unit gate gets ~40 more tests | P1 |
| 1.8 ⟂ | Resolve the two `Skip`s in `BookingsControllerTests` (312, 337): remove `Skip`, run; if red, fix or quarantine with a trait per §5.3 | problem #14 | `BookingsControllerTests.cs` | low | `my-bookings` covered below e2e | P1 |
| 1.9 ⟂ | Replace `Task.Delay(100)` in `GalleryManagementTests.cs:743` with a `TestClock` step (register a `TestClock : IDateTimeProvider` in the SC factory; this is the first user of the clock that §4 relies on) | §5.1 | `GalleryManagementTests.cs`, SC factory, `Tests.Commons/Clock` | low | removes the only sleep | P1 |
| 1.10 ⟂ | Remove the four stray `[Collection("Integration Tests")]` attributes and the unused `PostgresTestCollection` | confusion | four SC test files, `PostgresTestContainerFixture.cs:155-167` | none | clarity | P2 |
| 1.11 ⟂ | Enable the integration job in `dotnet.yml` (Testcontainers on `ubuntu-latest`, same job as the build, `--no-build`, trx upload); delete the stale comment | problem #16 | `.github/workflows/dotnet.yml` | CI minutes (~9 min today, falls with Phase 2) | the strongest gate runs on every PR | P1 |
| 1.12 ⟂ | Central package management: `Directory.Packages.props` at the root with one version per package; **FluentAssertions pinned to 7.x everywhere** (tier-3 confirmation from the user, §7.5); Test.Sdk/xunit/Testcontainers unified | problem #12 | root `Directory.Packages.props`, every test csproj | FA 8 → 7 API differences are few (`Should().BeEquivalentTo` options unchanged); build tells you | one dialect, licence clarity | P1 |
| 1.13 ⟂ | `tests/Directory.Build.props` with `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt` (`Task.Delay`, `Thread.Sleep`, `DateTime.Now`, `DateTime.Today`, `Random`) — warnings first, errors after Phase 2 | §4.6 | new files under `tests/` | existing violations (13 `Random`, 1 `Delay`) surface as warnings until fixed in Phase 2 | permanent enforcement | P2 |

Expected FULL after Phase 1: ~9–10.5 min with all apps touched, ~6–7.5 min backend-only.
Record the actual number, and the new per-class first-test time, in the change log.

### Phase 2 — structural (one to two weeks)

| # | Item | Why | Files / areas | Risk | Benefit | Priority |
|---|---|---|---|---|---|---|
| 2.1 | Create `tests/Booksy.Host.IntegrationTests` with `Infrastructure/` per §4.1: `PostgresServerFixture` (container + **template database** migrated once with the host's real `DbContextOptions`), `TestDatabase` (clone/drop), `BooksyHostFactory` (one factory, env `Testing`, `appsettings.Testing.json`), `HostFixture` collection fixture, `Collections.cs`, `xunit.runner.json` | L5, L6, L11; the core of the redesign | new project; `src/Host/Booksy.Host/appsettings.Testing.json` | template + parallel clone edge cases (§2.3 L5) — cover with a fixture self-test | one boot per collection; migrations once | P0 |
| 2.2 | `DatabaseReset` with Respawn (`Respawn` NuGet, Postgres adapter) over schemas `ServiceCatalog`, `user_management` (verify actual schema names in the migrations), excluding `__EFMigrationsHistory`, `cap.*`, and any migration-seeded reference tables; called from `IntegrationTest.InitializeAsync`; add a self-test proving a second test sees empty tables | L7; problem #2 | `Infrastructure/Reset`, `IntegrationTest.cs` | reference tables must be enumerated once | real isolation; parallelism becomes safe | P0 |
| 2.3 | Header-driven `TestAuthHandler` + `Api.As(user)` / `Api.Anonymous()`; delete `TestUserContext`, JWT minting, `AuthenticateAs*` families | §4.4; problem #9 | `Infrastructure/Auth`, every test that calls `AuthenticateAs*` (mechanical) | large mechanical diff; do it with a regex pass and compile | per-request identity, no shared state | P0 |
| 2.4 | `ApiClient` on System.Text.Json using the host's `JsonOptions` (resolve `IOptions<JsonOptions>` from the factory) for `ApiResponse<T>`; delete the Newtonsoft helpers, `AssertStatusCode`, `FindEntityAsync(Func<>)` | problem #11 | `Infrastructure/Http`, callers | enum/DTO deserialisation differences will surface — they are bugs in the tests today | one serializer, the production one | P1 |
| 2.5 | `Given` scenario API + `Tests.Shared` mothers/builders (no reflection; realistic defaults; fix `ServiceBuilder.WithCategory`; delete the six provider copies and `CreateProviderWithStatusAsync`'s argument-order bug); `Ids.UniquePhone()/UniqueEmail()` replacing `Random.Shared` (13 sites) | §4.5; problems #10, #13 | `Tests.Shared`, `Infrastructure/Given`, base class | the fixture is where the suite drifted before — `Given.ABookableSalon()` must keep the read-back assertion `MakeBookableAsync` has today | one way to build a salon | P0 |
| 2.6 | Move the SC tests into the new project under `ServiceCatalog/…`, assign collections (`Bookings`, `Payments`, `Providers`, `Memberships`, `Notifications`, `Availability`, `Persistence`), move the five concurrency classes to `Concurrency/` with its own host; merge `ProviderSettings*` and split files > 400 lines | §4.1, §4.6 | mass move (git mv to keep history) | big diff; do it collection by collection, verify after each | | P0 |
| 2.7 | Retarget and move the UM tests (`UserManagement/…`); rebuild `UserRepositorySaveTests`/`PersonProvisioningConcurrencyTests` on the template database (migrated schema, production options) instead of hand-built contexts; delete `UserManagementTestWebApplicationFactory` and its base | problem #3, #15 | UM test files | tests that only passed against the per-service host will fail against the monolith — those failures are the point | the gate tests the shipped composition | P0 |
| 2.8 | Move the composition tests (`Composition/`) onto `HostFixture`; delete `HostCompositionFactory` | §4.1 | 5 files | none | third factory gone | P1 |
| 2.9 | Fakes: single capturing implementation per channel (email/SMS/push/in-app) implementing every interface the two contexts define; `Fakes.Reset()` per test; `WaitUntilAsync` helper for asynchronous effects | §5.1 | `Infrastructure/Fakes` | none | one fake per channel | P1 |
| 2.10 | xUnit logger provider in the factory (Serilog → `ITestOutputHelper` via a per-test sink) and no file sink under `Testing` | §6.3 | factory, `appsettings.Testing.json` | none | logs where failures are read | P1 |
| 2.11 | Re-enable parallelism: delete `AssemblyInfo.cs`'s `DisableTestParallelization`; `maxParallelThreads: 4`; `Concurrency` collection with `Max Pool Size` ≥ 150; measure on the audit machine and on CI, pick the thread count | L8 | `xunit.runner.json`, connection string in `HostFixture` | contention; watch for the process-global items in §5.1 | the remaining 2–3× | P1 |
| 2.12 | Delete `Booksy.ServiceCatalog.IntegrationTests`, `Booksy.UserManagement.IntegrationTests`, `Booksy.Host.CompositionTests`; update `.sln`, both verify scripts, `dotnet.yml`, `CLAUDE.md`, `AGENTS.md`, `docs/KNOWLEDGE_MAP.md`; write `tests/README.md` | §7 | docs and scripts | none | | P0 (closes the phase) |
| 2.13 | Migrate the 6 Moq files to NSubstitute; remove the Moq package | one mocking library | `VerifyZarinPal*Tests`, `ProvidersByStatusMappingTests`, `ServicesControllerAuthorizationTests`, `TokenServiceTests`, `ZarinPalServiceTests` | none | | P2 |

Expected FULL after Phase 2: ~4.5–5.5 min with apps and ~3–3.5 min backend-only while still serial;
~3.5–4.5 / ~2–2.5 min once 2.11 (parallel collections) is on.

### Phase 3 — long-term (ongoing, opportunistic)

| # | Item | Why | Areas | Risk | Benefit | Priority |
|---|---|---|---|---|---|---|
| 3.1 | Push validation permutations down to validator unit tests; enforce "one integration test per outcome class" in review; delete the HTTP duplicates as files are touched | §1.6, §3.2 | `ProvidersControllerTests`, `WorkingHoursManagementTests`, `BookingsControllerTests`, `PaymentsControllerTests`, `ProviderSettings*` | none | fewer, faster integration tests | P1 |
| 3.2 | Quarantine trait + 14-day nightly check; retire `known-failures.txt` parsing from verify (keep the file as history) | §5.3 | `scripts/verify.*`, `nightly.yml` | none | one mechanism | P1 |
| 3.3 | `nightly.yml`: run-twice flake detector, Playwright, Flutter, dependency audit, coverage report (report only) | §6.1 | `.github/workflows/nightly.yml` | CI minutes | flakes found before they block a PR | P1 |
| 3.4 | Verify runs Vue/Flutter steps concurrently with the DB step (PowerShell jobs; `verify.sh` with `&` + `wait`) | L10 | `scripts/verify.*` | log plumbing | hides ~3 min when apps are touched | P2 |
| 3.5 | Retire Cypress (one spec) in favour of Playwright; keep Playwright at 3–4 keystone journeys; promote it to a deploy gate only if it stays green for a month of nightlies | §3.2 | `booksy-frontend/cypress`, `package.json`, `frontend-e2e.yml` | none | one browser stack | P2 |
| 3.6 | Production debts surfaced by this audit, each its own change: (a) `OtpCode` reads `Environment` (inject configuration); (b) handlers depend on `IHttpContextAccessor` rather than `ICurrentUserService`; (c) three admin role spellings (#46); (d) retired HTTP `TokenService` + `TokenServiceTests`; (e) two/three SMS interfaces; (f) retired per-service host entry points (tier-3, §7.5) | testability and correctness | `src/…` | product code | tests get simpler as each lands | P2 |
| 3.7 | Contract snapshot tests for the API envelope and top-level DTOs (`Verify.Xunit` or plain JSON fixtures) so `DTO_MAPPING.md` drift is caught by a test | API contract protection | new folder `Contracts/` in the integration project | snapshot churn | clients (Vue, Flutter) stop discovering shape changes at runtime | P3 |
| 3.8 | Evaluate xUnit v3 (assembly fixtures, richer parallelism controls, `Microsoft.Testing.Platform`) once the collection design is stable | tooling | all test csproj | migration cost | | P3 |

---

## C. Target testing architecture (one page)

- **Unit** (`*.UnitTests`, one per assembly under test, plus `Booksy.Tests.Shared` for builders/mothers/clock): pure, NSubstitute, no I/O, run by FAST and the PR fast gate in under 10 s of test time.
- **Integration** (`Booksy.Host.IntegrationTests`): the one production host, one factory, template database migrated once, one database and one host per xUnit collection, Respawn reset per test, per-request auth header, System.Text.Json API client, `Given`/mothers/builders through production write paths, fakes for every external channel, `TestClock`, xUnit-routed host logs, collections run in parallel, concurrency tests in their own serial collection, quarantine by trait. Run by FULL and the PR full gate in 2–4 minutes.
- **E2E**: the keystone curl script as the deploy gate; Playwright for 3–4 browser journeys, advisory then nightly.
- **Tooling**: verify builds once and runs everything else `--no-build`, collects trx, lists the slowest tests, runs app checks concurrently; CI mirrors verify; nightly hunts flakes.
- **Rules**: §7.4, enforced by analyzers where a rule can be mechanical and by review where it cannot.

## D. Performance optimisation plan — see §2.3 (levers L1–L12) and the phase estimates in §2.4.

## E. Maintainability rules — see §4.6 (conventions), §5.2 (reliability checklist) and §7.4 (permanent rules).

## F. Implementation roadmap — see §9.
