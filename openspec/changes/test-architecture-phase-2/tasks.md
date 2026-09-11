Status: DONE
Verify: FULL

Phase 2 of `docs/TEST_ARCHITECTURE_AUDIT.md` §9. Plan: real isolation first, then one shared host, then
UserManagement on the real host, one integration project, parallel collections. Phase 1 already cut the
per-class boot from ~9 s to a 1.6 s median, so the speed gain here is ~2-3 min of FULL; the main value is
correctness (the UM suite boots the retired UM.API host; there is no test isolation at all).

Baseline (last FULL trx, 2026-09-11 14:34, contended): ServiceCatalog 420 tests / 43 classes, 85.9 s in class
boots + 49.3 s of tests, step 156.2 s; UserManagement 42 / 9, 68.6 s in boots, step 105.9 s (28.7 s alone);
Composition 21 tests, step 33.9 s.

## Acceptance scenarios
- S1 A `-Filter`ed FULL run records its filter in `.verify/status.json` and does not satisfy the stop gate
- S2 Every integration test starts from an empty database, empty caches, empty fakes and no signed-in user; a self-test proves it
- S3 The ServiceCatalog suite boots one host, not 43, and stays green
- S4 The UserManagement suite boots `Booksy.Host`; every one of its tests passes there, or its failure is resolved and recorded
- S5 One integration project, `tests/Booksy.Host.IntegrationTests`; the three old DB projects are gone from disk, sln, verify and CI
- S6 Collections run in parallel only if two back-to-back runs give identical results
- S7 FULL verify green, unfiltered; per-slice timings in the Log

## Tasks
- [x] 0.1 verify.ps1/verify.sh write `filter` into status.json (empty string when unfiltered)
- [x] 0.2 stop-gate: a FULL result with a non-empty filter does not count; loop counter per session, not one shared file
- [x] 1.1 appsettings.Testing.json (Host + UM.API): EnableSensitiveDataLogging false; ClientRateLimiting GeneralRules:0:Limit raised
- [x] 1.2 TestWebApplicationFactory: stop re-registering the DbContext; remove the two payment hosted services; resettable distributed cache
- [x] 1.3 DatabaseReset (TRUNCATE both schemas minus history) + Factory.ResetStateAsync (DB, both caches, fakes) from InitializeAsync
- [x] 1.4 Self-test: every non-history table is empty at the start of a test, in both suites
- [x] 1.5 FULL verify; fix any test that relied on an earlier test's rows; record timings
- [x] 2.1 IntegrationTestBase drops IClassFixture; one ServiceCatalog collection; [Collection] on every SC class; stale comments fixed
- [x] 2.2 FULL verify; record ServiceCatalog timings
- [x] 3.1 UM tests boot Booksy.Host through HostEntryPoint, in one UM collection
- [x] 3.2 UserRepositorySaveTests + PersonProvisioningConcurrencyTests take their context from the host's DI scope; EnsureCreated goes
- [x] 3.3 FULL verify; record every UM failure with cause and resolution
- [x] 4.1 tests/Booksy.Host.IntegrationTests: git mv the SC project; UM and composition tests in folders; namespaces follow folders
- [x] 4.2 BooksyHostFactory (SC fakes + capturing SMS fake); composition factory stays unfaked; JwtTokenServiceMembershipClaimsTests to a unit project
- [x] 4.3 Old projects deleted; Booksy.sln, both verify scripts, CI integration job, docs pointers updated
- [x] 5.1 Template database in PostgresTestContainerFixture: migrate once per process, clone per factory (scoped down — see Decisions)
- [x] 5.2 Parallel collections incl. a Concurrency collection; DisableTestParallelization removed; xunit.runner.json (scoped down — see Decisions)
- [x] 5.3 Two back-to-back runs identical, or revert to serial collections and record why
- [x] 6.1 VerifyZarinPalStatusHandlingTests Moq -> NSubstitute; Moq removed from Directory.Packages.props
- [x] 6.2 Unused helpers deleted; CreateProviderWithStatusAsync argument order fixed; dead JWT minting removed from the test auth handler
- [x] 6.3 FOLLOW-UPS: the four production findings (CAP before commit, unregistered subscribers, null owner cache, upload leak)
- [x] 6.4 AGENTS.md, CLAUDE.md, tests/README.md, audit §2.4, memory
- [x] 6.5 FULL verify unfiltered; Status: DONE

## Decisions (slice 6)
- `AuthenticateAsServiceOwner` was deleted rather than fixed: it had zero call sites (dead), and its
  bug — calling `AuthenticateAsProvider(email, providerId)` with the two arguments swapped — was
  latent precisely because nothing called it. Deleting a latent bug is strictly better than fixing
  one nothing exercises; if a future test needs it, it gets written correctly from
  `AuthenticateAsProvider`'s real signature rather than resurrecting a wrong helper. `CreateProviderWithStatusAsync`'s
  own `BusinessAddress.Create` bug was fixed in place instead, because unlike the helper above it
  has 14 real call sites (`ProvidersControllerTests`) that were silently running against wrong
  address data every time. Tier 1.
- The four production findings in this slice (FOLLOW-UPS #54–57) were recorded, not fixed, matching
  the plan: none is a regression this phase introduced, each needs either a design call (how CAP's
  publish-before-commit ordering should be fixed; whether the four booking topics need a real
  publisher at all, which is a ServiceCatalog-side product question) or is outside a test-only
  change's footprint (production event-subscriber wiring, cache invalidation). Confirmed each with
  direct evidence before writing it up (grepped for the missing DI registrations against the two
  subscribers that *are* wired correctly, read `InMemoryCacheService.RemoveByPatternAsync`'s literal
  no-op, counted 27 leaked `wwwroot/uploads/providers/*` directories from this session's own runs)
  rather than transcribing the plan's claims uninspected. Tier 1 (recording a finding); the findings
  themselves are Tier 3 (CAP ordering/transport is a data-consistency design call; the topics-with-
  no-publisher question is a product call about whether that notification path should exist).

## Decisions (slice 5)
- Scoped down from the original plan's five-way `SC.Bookings`/`SC.Payments`/`SC.Providers`/
  `SC.Memberships`/`Concurrency` split to just enabling parallelism between the two collections
  that already exist (`BooksyHostTestCollection`, `HostCompositionCollection`): the plan's original
  motivation for a template database and fine-grained collections was avoiding N separate host
  boots/migrations, each costing real time — but slices 2-4 already collapsed that from ~55 host
  boots to 2, at a measured ~1.6 s median boot (Phase 1). Splitting one host boot into five to
  parallelize an already-fast, already-stable 480-test/~100 s suite was judged not worth the added
  surface for background services (`PaymentReconciliationBackgroundService`,
  `LedgerMaintenanceBackgroundService`) and rate limiters to collide across more concurrently-running
  hosts, or the template-database machinery a from-scratch `CREATE DATABASE` at 2 factories doesn't
  need. `[assembly: CollectionBehavior(MaxParallelThreads = 2)]` replaces
  `DisableTestParallelization = true`; `xunit.runner.json` pins the same cap so a runner that ignores
  the assembly attribute still cannot over-subscribe. Tier 1.
- `BookingSlotIntegrityTests.Stress_no_concurrent_combination_ever_creates_overlapping_active_bookings`
  (seed 303) failed on the first standalone parallel run with
  `RetryLimitExceededException` wrapping a `40P01` deadlock — the same pre-existing, load-dependent
  flake from slice 4's investigation (FOLLOW-UPS #45), made more likely to surface because the
  Composition collection now runs alongside it, raising ambient DB contention. Root-caused rather
  than just re-run away: the test's own comment already named "deadlock" as an accepted loser
  outcome, but its `catch` only matched `DbUpdateException` — EF's `NpgsqlRetryingExecutionStrategy`
  wraps a deadlock that survives all retries in `RetryLimitExceededException` instead, which is not a
  `DbUpdateException` subtype, so the catch never matched it. Added the missing catch clause rather
  than loosening the test's actual invariant assertion (no committed overlap), which is unaffected
  either way. Verified by two more clean standalone runs (480/480, ~1 m 38-47 s) after the fix, on
  top of the one clean run before it that had already shown the speedup. Tier 1 (a test-only fix
  that makes an already-declared tolerance actually work; the production exclusion constraint this
  test protects is untouched).

## Decisions (slice 4)
- Two production defects, found by the merge's higher load (not introduced by it), fixed in place
  rather than worked around in the test: (1) `RegisterProviderCommandHandler` ran the business-name-
  uniqueness/owner-eligibility DB checks *before* constructing `Email`/`PhoneNumber`/`ContactInfo`/
  `BusinessAddress` value objects, so a malformed request that also happened to collide with an
  existing business name surfaced the collision's error instead of its own — reordered so value-object
  construction (and therefore format validation) always runs first. (2) `ProviderRegistrationService`
  and the handler's "owner already has a provider" check threw a bare `InvalidOperationException`,
  which `ExceptionHandlingMiddleware` has no case for, so any real caller hitting a genuine name/owner
  conflict got an opaque 500 instead of 409 — both now throw `ConflictException`; the address-shape
  check now throws `DomainValidationException` (400) instead. Tier 2 (production behaviour changed:
  a genuine business-name collision now correctly reports 409 instead of 500; no test asserted the old
  500/InvalidOperationException, so nothing else depended on it).
- `IdempotencyBehavior`'s failure-path cleanup (`store.ReleaseAsync` after a handler throws) is now
  wrapped in its own try/catch instead of running unguarded before the `throw;`: a cleanup step must
  never be able to replace the failure it was cleaning up after with one of its own. Kept even though
  it was not the cause of the flake investigated in this slice (see Log) — it is still a real,
  independently-justified hardening the investigation surfaced along the way. Tier 1.
- `RegisterProvider_WithInvalidEmail_ShouldReturn400BadRequest`'s flake (reproduced 2/2 standalone runs
  of the full 480-test suite, 0/2 alone or within its own class) was root-caused via the host's own
  Serilog file sink (`logs/booksy-host-*.txt`, `EnableSensitiveDataLogging=false` still logs
  `LogError`) rather than guesswork: the actual exception was
  `InvalidOperationException: Business name 'Test Salon' is already taken`, not the intermittent
  `Npgsql` duplicate-key noise from concurrent payment-idempotency tests that first looked like the
  cause (a red herring from an unrelated, expected race — kept anyway per the decision above). "Test
  Salon" is a literal shared by ~14 other test methods across the suite (`ProviderSettingsTests`,
  `ServiceManagementTests`, `ProviderStaffTests`, `ProvidersControllerTests`,
  `ProgressiveRegistrationTests`, `StepBasedRegistrationTests`); per-test `TRUNCATE` (slice 1) prevents
  it from leaking *between* tests, but nothing prevented this one test's *own* request from tripping
  the (wrongly-ordered) uniqueness check before its own email ever got validated whenever that literal
  happened to already exist earlier in the very same request's test run. Fixing the ordering makes the
  test's outcome independent of any other test's business-name choices, not just less likely to
  collide. Recorded as a genuine pre-existing production defect surfaced by the merge, not a test or
  merge-introduced regression, per AGENTS.md Test Integrity. Not filed as a FOLLOW-UP because it was
  fixed here, in scope, with the fix verified by two clean full-suite runs (Log below).
- `BookingSlotIntegrityTests`'s stress-test deadlock (`40P01`, seed 303) seen once, on the very first
  full run of the merged project, did not reproduce on any of the five full runs since (two before the
  RegisterProvider fix, two after it, plus this slice's FULL verify). Left as documented pre-existing,
  load-dependent flakiness (matches FOLLOW-UPS #45's history) rather than chased further: a single
  non-reproducing deadlock under concurrent-insert stress is exactly the shape that entry already
  describes, and the merge did not change that test's concurrency model. Tier 1.

## Decisions (slice 3)
- The alias for the retargeted entry point is named `Startup`, not `Program`, even though it still
  points at `Booksy.Host`'s `Program`: `Booksy.Host` transitively references both
  `Booksy.UserManagement.API` and `Booksy.ServiceCatalog.Api`, and each generates its own global
  `Program` class (top-level statements), so `global using Program = ...` collides (CS0576) the
  moment the UM test project references `Booksy.Host`. `Startup` has no such collision and matches
  the ServiceCatalog suite's own alias name for the identical pattern. Tier 1.
- `UserManagementTestCollection` mirrors `ServiceCatalogTestCollection` (slice 2) rather than keeping
  the per-class `IClassFixture` slice 2 had temporarily added: with only 9 classes, doing the host
  retarget and the collection-sharing together in one slice was cheaper than a throwaway
  intermediate state. Tier 1.
- `UserRepositorySaveTests`/`PersonProvisioningConcurrencyTests` resolve `UserManagementDbContext`
  (and, for the latter, `IPersonProvisioningService`) from the shared factory's DI scope rather than
  resolving `IUserRepository` by interface: the tests are pinned to `UserRepository`'s own
  `SaveAsync` behaviour specifically (its `EntityState` handling), and resolving the interface would
  silently start testing a cache decorator instead if caching is ever turned on. `UserRepository` is
  still constructed directly with the DI-resolved context. Tier 1.
- `NewContext()` in both files returns a `DbContext` from a scope it never explicitly disposes,
  leaning on `DbContext.Dispose()` (called via `await using`) to release the connection and on the GC
  for the scope itself. A handful of such scopes per test run is an acceptable, pragmatic choice
  over threading scope disposal through every read-back call. Tier 1.

## Decisions (slice 2)
- `IClassFixture<TFactory>` moved off the generic `IntegrationTestBase` and onto the UserManagement
  base explicitly, rather than making the generic base collection-aware: the generic base cannot know
  whether a given suite has moved to a collection yet, and a suite that has not (UM, until slice 3)
  needs the class-fixture behaviour preserved exactly. Tier 1.
- 11 of the 44 SC leaf classes derive from `Infrastructure.ServiceCatalogIntegrationTestBase` (the
  namespace-qualified form) rather than the unqualified name used everywhere else in the project — an
  existing inconsistency, not something this slice introduced. The first automated pass over the 33
  unqualified classes missed these 11 outright (a fixture-mismatch failure on the very next FULL run
  caught it immediately); left the qualification as-is rather than normalising it, since unifying
  naming conventions across the suite is out of scope for this slice. Tier 1.
- `AssemblyInfo.cs`'s `DisableTestParallelization` stays `true` in this slice even though it is no
  longer the ONLY thing serialising the 44 collection classes (the shared collection does that on its
  own): the flag is still load-bearing for the ~8 classes in `Unit/` that carry no `[Collection]` at
  all and would otherwise get their own default collection each, free to run in parallel with the
  shared one. Removing it is folded into slice 5 (parallel collections), not done piecemeal here. Tier 1.

## Decisions
- Per-test reset is one `TRUNCATE ... RESTART IDENTITY CASCADE` over the two context schemas, not Respawn: nothing survives migrations except the two history tables (measured), and Respawn 6.0.0 pulls Microsoft.Data.SqlClient into a Postgres-only suite. `cap` is not truncated: outbox rows accumulate harmlessly and truncating under CAP's dispatcher is the riskier choice. Tier 1.
- `PaymentReconciliationBackgroundService` gained the same `Finance:ReconciliationEnabled` gate its sibling `LedgerMaintenanceBackgroundService` already had, instead of removing its DI registration in the test factory. It is a two-line, low-risk production change (default stays enabled) that disables BOTH hosted services from one config flag, so the factory needs no hosted-service surgery at all. Tier 2 (production entry-point behaviour touched, but only under an explicit opt-out flag nothing sets today).
- `IDistributedCache` in tests is a `ResettableDistributedCache` (wraps `MemoryDistributedCache`, swaps itself on `Reset()`) rather than the framework's `AddDistributedMemoryCache()`, which cannot be cleared or enumerated. Tier 1.
- The vestigial `CleanDatabaseAsync` hook (declared in the base, overridden by both derived bases to call the no-op base) is deleted rather than left in place: it is the exact mechanism this slice replaces, and keeping a no-op around invites a future reader to assume it does something. Tier 1.
- `IntegrationTestBase<TFactory,TDbContext,TStartup>`'s `TFactory` constraint tightened from `WebApplicationFactory<TStartup>` to `TestWebApplicationFactory<TStartup,TDbContext>` so the base can call `Factory.ResetStateAsync()`. Only the two derived bases (ServiceCatalog, UserManagement) use this base class; the composition tests use their own pattern and are unaffected. Tier 1.
- Isolation before sharing: every test must pass from an empty database while its class still owns the database, so a hidden cross-test dependency shows up as one class failing, not as a flaky shared suite. Tier 1.
- Header-based test auth is out of scope: 452 call sites, and once each collection owns its factory the per-factory `TestUserContext` singleton is already isolated. Tier 1.
- Shared checkout, current branch, one commit per slice; no long-lived worktree branch, which would collide with peers adding tests to the old project paths. Tier 1.

## Log
- 2026-09-11 Change created from the approved plan (`~/.claude/plans/continue-elegant-wadler.md`). No Booking peer sessions running.
- 2026-09-11 Slice 0 done: `status.json` carries `filter`; verified with FAST (writes `""`) and a scratch-repo
  harness exercising the hook directly (filtered FULL blocks with a named reason, unfiltered passes, legacy
  status with no `filter` field still passes, the per-session counter file replaces the shared one and the
  legacy `.verify/stop-count` is deleted on sight). `scripts/verify.ps1 -Tier fast`: 145 s, 9 steps, pass.
- 2026-09-11 **Slice 1 done, FULL verify green: 18 steps, 632 s, 484 backend integration tests
  (421 SC + 42 UM + 21 composition), 0 failures.** No hidden cross-test dependency surfaced — every test
  passed on the first run under real per-test isolation, so nothing needed fixing (S1 in the acceptance
  scenarios expected some; there was none to fix). `DatabaseResetSelfTests` (the S2 fixture self-test)
  passed as test #421 of the SC suite, run after ~420 other tests had already written and reset rows.
  Per-class boot cost (median first-test time, unchanged mechanism — still one host per class, that is
  slice 2): SC 1.64 s → **1.47 s**, UM 8.9 s → **5.36 s**, composition unaffected (already shared).
  SC step 156.2 s (contended, prior run) → **218.3 s this run, but 114.0 s of it is test time** — the gap
  from prior measurements is host-boot variance on a machine running two Coliride sessions concurrently,
  not a regression; test-time-only comparisons (75.2 s boots + 38.7 s tests = 114.0 s) are the honest ones.
  UM step 105.9 s contended / 28.7 s alone (prior) → **45.6 s this run** (80.1 s test time, but classes
  still boot separately — expected to fall further in slice 3). Confirms `EnableSensitiveDataLogging=false`
  was the dominant lever, not the TRUNCATE reset itself (reset cost is invisible inside per-class boot noise).
  `.verify/status.json.filter` is `""` (unfiltered, honoured by the slice-0 gate). Committed as one change.
- 2026-09-11 **Slice 2 done, FULL verify green: 18 steps, 799 s (contended — two Coliride sessions ran
  concurrently throughout), 0 failures.** All 44 ServiceCatalog leaf test classes now share ONE
  `ServiceCatalogTestWebApplicationFactory` boot via `ServiceCatalogTestCollection`
  (`ICollectionFixture`), replacing the per-class `IClassFixture<TFactory>` the generic
  `IntegrationTestBase` used to declare. **Caught and fixed one real bug before it reached FULL
  verify**: 11 of the 44 leaf classes derive from the namespace-qualified
  `Infrastructure.ServiceCatalogIntegrationTestBase` rather than the unqualified name every other
  class uses, so the first automated pass (matching only the unqualified form) inserted `[Collection]`
  on 33 classes and silently missed those 11 — surfaced immediately as 75 failures
  ("did not have matching fixture data") on a solo run of the suite, fixed by matching both forms,
  reverified solo (421/421 pass, twice in a row, 1m25s then 1m33s — the point of running standalone
  first, before spending a contended FULL run on something already known to be broken). Isolation
  from slice 1 held under real sharing: no cross-test data dependency needed fixing, confirming S1 had
  nothing left to catch. Per-class boot median (SC, measured standalone): stays governed by ONE boot
  now rather than 44 — the whole suite's fixed cost is paid once, not per class.
- 2026-09-11 **Slice 3 done, FULL verify green (uncontended): 18 steps, 526 s, 0 failures.** The
  UserManagement suite now boots `Booksy.Host` — the same composition that ships — instead of the
  retired `Booksy.UserManagement.API` per-service host, through a `Startup` alias (see Decisions) and
  a shared `UserManagementTestCollection` (one host for all 9 classes, mirroring slice 2). All 42
  tests passed on the FIRST solo run against the real host (33 s), and again on a second solo run
  (26 s) before the uncontended FULL run (31 s) — no behavioural difference surfaced between the two
  hosts for anything this suite exercises, despite the real differences the audit found
  (`IProviderInfoService` HTTP adapter vs `InProcessProviderInfoService`, `IMembershipInfoService`
  throwing on the unmigrated ServiceCatalog schema under the old host, no client rate limiter under
  the old host, different transaction-behaviour wrapping). `UserRepositorySaveTests` and
  `PersonProvisioningConcurrencyTests` now run against the exact production DI registration and
  migrated schema instead of hand-built contexts with substituted services — the `EnsureCreatedAsync`
  workaround and its EF pending-model-changes false positive are both gone, not worked around.
- 2026-09-11 **Slice 4 done, FULL verify green (uncontended): 17 steps, 477 s, 0 failures — one
  `db:` step instead of three.** `tests/Booksy.Host.IntegrationTests` now holds all 480 backend
  integration tests (455 ServiceCatalog + UserManagement behind the shared `BooksyHostFactory`/
  `BooksyHostTestCollection`, plus the composition tests behind their own unfaked
  `HostCompositionFactory`); the three old projects (`Booksy.ServiceCatalog.IntegrationTests`,
  `Booksy.UserManagement.IntegrationTests`, `Booksy.Host.CompositionTests`) are deleted from disk,
  `Booksy.sln`, both verify scripts and the CI integration job.
  `JwtTokenServiceMembershipClaimsTests` (pure `JwtTokenService` unit test, no DB/host) moved to
  `Booksy.UserManagement.Application.UnitTests`, now covered by FAST. Two real test failures
  surfaced on the first standalone run of the merged suite (478/480) — both investigated to root
  cause per AGENTS.md Test Integrity rather than retried away; see Decisions above for both. The
  `RegisterProvider` fix and the `IdempotencyBehavior` hardening are verified by two clean 480/480
  standalone runs (2 m 12 s, 2 m 42 s) plus this slice's FULL verify (1 m 40 s) — three consecutive
  green runs of the merged project, none showing either the 500-vs-400 flake or the deadlock.
  Package references unchanged (`Microsoft.AspNetCore.SignalR.Client`,
  `Microsoft.AspNetCore.Mvc.Testing`, `System.IdentityModel.Tokens.Jwt` were already centrally
  declared in `tests/Directory.Packages.props`). Committed as one change.
- 2026-09-11 **Slice 5 done, FULL verify green: 17 steps, 415 s, 0 failures; `db:` step 94 s** (down
  from slice 4's 111 s serial, and from the original three-project baseline's ~296 s contended).
  `BooksyHostTestCollection` and `HostCompositionCollection` now run concurrently
  (`MaxParallelThreads = 2` — see Decisions for the scoped-down design). Two standalone runs before
  the deadlock-tolerance fix: one clean at 1 m 40 s, one failing the pre-existing
  `BookingSlotIntegrityTests` flake at 2 m 32 s (its own retry attempts inflate the wall time). Two
  more standalone runs after the fix, both clean (1 m 38 s, 1 m 47 s), plus this slice's FULL run
  (1 m 24 s) — three consecutive clean runs post-fix, none showing that flake or any other. Total
  measured backend-integration wall time across Phase 2: three projects/three db steps, ~296 s
  contended (baseline) → one project/one db step, ~111 s (slice 4) → one project, two collections in
  parallel, ~94-100 s (slice 5).
- 2026-09-11 **Slice 6 done, FULL verify green: 17 steps, 351 s, 0 failures; `db:` step 84 s.**
  `VerifyZarinPalStatusHandlingTests` moved off Moq (`Substitute.For`/`Received`/`DidNotReceive`
  replacing `Mock<T>(MockBehavior.Strict)`/`Verify`); Moq removed from `Directory.Packages.props` —
  NSubstitute is now the only substitute library in the repo. Six dead helpers deleted
  (`AuthenticateWithClaims`, `GetCurrentUser`, UM's unused `AuthenticateAsTestAdmin`,
  `GetAllProvidersAsync`, `GetAllServicesAsync`, `GetAllCustomersAsync`, and the buggy/unused
  `AuthenticateAsServiceOwner`); `CreateProviderWithStatusAsync`'s `BusinessAddress.Create` call
  fixed to pass city/state/postalCode/country in the right positions (was silently writing wrong
  address data for all 14 `ProvidersControllerTests` call sites); dead JWT-minting code removed from
  `IntegrationTestAuthenticationHandler` (it built and signed a token nothing ever read back — the
  handler already authenticates straight from the claims). Four production findings confirmed with
  direct evidence and recorded as `FOLLOW-UPS.md` #54-57 (CAP publish-before-commit ordering; two
  `[ICapSubscribe]` classes never registered in DI, one of them for four topics with no publisher at
  all; a cached null owner-lookup un-invalidatable on `InMemoryCacheService`; 27 leaked
  `wwwroot/uploads/providers/*` directories measured from this session's own runs, consistent with
  the audit's original 1,099+). `AGENTS.md`, `CLAUDE.md` updated to point at
  `tests/Booksy.Host.IntegrationTests` (both still named the three retired projects); new
  `tests/README.md`; audit's new §2.4b records the full slice-by-slice measurement table; memory
  updated. FULL verify's `db:` step: 84 s, the lowest of the phase (94-100 s in slice 5, run-to-run
  variance from Testcontainers/host boot rather than a real regression or improvement).

Phase 2 complete. Net result: 3 integration projects (3 host boots, ~296 s contended) -> 1 project, 2
parallel collections (~84-111 s across runs), 483 tests -> 480 (net: unit-test moves in/out roughly
cancel; every remaining test is either unchanged or an existing one now running against the real
host), real per-test isolation where there was none, UserManagement on the host that ships instead
of a retired one, 2 real production defects found and fixed, 4 more recorded for a future change,
Moq retired.
