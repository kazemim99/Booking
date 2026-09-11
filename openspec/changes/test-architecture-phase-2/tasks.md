Status: ACTIVE
Verify: FAST

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
- [ ] 1.1 appsettings.Testing.json (Host + UM.API): EnableSensitiveDataLogging false; ClientRateLimiting GeneralRules:0:Limit raised
- [ ] 1.2 TestWebApplicationFactory: stop re-registering the DbContext; remove the two payment hosted services; resettable distributed cache
- [ ] 1.3 DatabaseReset (TRUNCATE both schemas minus history) + Factory.ResetStateAsync (DB, both caches, fakes) from InitializeAsync
- [ ] 1.4 Self-test: every non-history table is empty at the start of a test, in both suites
- [ ] 1.5 FULL verify; fix any test that relied on an earlier test's rows; record timings
- [ ] 2.1 IntegrationTestBase drops IClassFixture; one ServiceCatalog collection; [Collection] on every SC class; stale comments fixed
- [ ] 2.2 FULL verify; record ServiceCatalog timings
- [ ] 3.1 UM tests boot Booksy.Host through HostEntryPoint, in one UM collection
- [ ] 3.2 UserRepositorySaveTests + PersonProvisioningConcurrencyTests take their context from the host's DI scope; EnsureCreated goes
- [ ] 3.3 FULL verify; record every UM failure with cause and resolution
- [ ] 4.1 tests/Booksy.Host.IntegrationTests: git mv the SC project; UM and composition tests in folders; namespaces follow folders
- [ ] 4.2 BooksyHostFactory (SC fakes + capturing SMS fake); composition factory stays unfaked; JwtTokenServiceMembershipClaimsTests to a unit project
- [ ] 4.3 Old projects deleted; Booksy.sln, both verify scripts, CI integration job, docs pointers updated
- [ ] 5.1 Template database in PostgresTestContainerFixture: migrate once per process, clone per factory
- [ ] 5.2 Parallel collections incl. a Concurrency collection; DisableTestParallelization removed; xunit.runner.json
- [ ] 5.3 Two back-to-back runs identical, or revert to serial collections and record why
- [ ] 6.1 VerifyZarinPalStatusHandlingTests Moq -> NSubstitute; Moq removed from Directory.Packages.props
- [ ] 6.2 Unused helpers deleted; CreateProviderWithStatusAsync argument order fixed; dead JWT minting removed from the test auth handler
- [ ] 6.3 FOLLOW-UPS: the four production findings (CAP before commit, unregistered subscribers, null owner cache, upload leak)
- [ ] 6.4 AGENTS.md, CLAUDE.md, tests/README.md, audit §2.4, memory
- [ ] 6.5 FULL verify unfiltered; Status: DONE

## Decisions
- Per-test reset is one `TRUNCATE ... RESTART IDENTITY CASCADE` over the two context schemas, not Respawn: nothing survives migrations except the two history tables (measured), and Respawn 6.0.0 pulls Microsoft.Data.SqlClient into a Postgres-only suite. `cap` is not truncated: outbox rows accumulate harmlessly and truncating under CAP's dispatcher is the riskier choice. Tier 1.
- Isolation before sharing: every test must pass from an empty database while its class still owns the database, so a hidden cross-test dependency shows up as one class failing, not as a flaky shared suite. Tier 1.
- Header-based test auth is out of scope: 452 call sites, and once each collection owns its factory the per-factory `TestUserContext` singleton is already isolated. Tier 1.
- Shared checkout, current branch, one commit per slice; no long-lived worktree branch, which would collide with peers adding tests to the old project paths. Tier 1.

## Log
- 2026-09-11 Change created from the approved plan (`~/.claude/plans/continue-elegant-wadler.md`). No Booking peer sessions running.
- 2026-09-11 Slice 0 done: `status.json` carries `filter`; verified with FAST (writes `""`) and a scratch-repo
  harness exercising the hook directly (filtered FULL blocks with a named reason, unfiltered passes, legacy
  status with no `filter` field still passes, the per-session counter file replaces the shared one and the
  legacy `.verify/stop-count` is deleted on sight). `scripts/verify.ps1 -Tier fast`: 145 s, 9 steps, pass.
