Status: ACTIVE
Verify: FAST

Phase 1 of `docs/TEST_ARCHITECTURE_AUDIT.md` §9: the quick wins. Measured baseline (2026-09-11 FULL
verify, all four apps touched): 931.7 s total; `db:Booksy.ServiceCatalog.IntegrationTests` 492.6 s of
which 407 s is the ≈ 9 s host boot paid by each of 43 test classes; unit steps 61 s for 7 s of tests.
Nothing structural changes here (no shared host, no Respawn, no project merge — that is Phase 2).

## Acceptance scenarios
- S1 `scripts/verify -Tier fast` runs no `dotnet test` without `--no-build` after the build step; FAST wall time drops by ≥ 30 s against the 2026-09-11 baseline
- S2 Every db step leaves a `.trx` under `.verify/trx/` and `.verify/slowest.txt` lists the 20 slowest tests of the run
- S3 A test host boots with environment `Testing`, seeds nothing, and a full ServiceCatalog run emits < 50 000 console lines (was 4.57 M)
- S4 `tests/Booksy.ServiceCatalog.IntegrationTests` contains no test class that does not boot the host; the moved tests run in the FAST tier and in CI's unit filter
- S5 No `[Fact(Skip)]` remains in the backend test projects
- S6 `tests/Booksy.Tests.Common/` and `tests/Booksy.ServiceCatalog.UnitTests/` no longer exist; every test project is in `Booksy.sln`
- S7 The four composition test classes share one host boot
- S8 Every test package has exactly one version, declared once, and FluentAssertions is on the last Apache-2.0 line (7.x)
- S9 CI runs the integration projects on pull requests
- S10 FULL verify green; per-class first-test time and total FULL time recorded in the Log

## Tasks
- [x] 1 verify.ps1/verify.sh: `--no-build` on every `dotnet test` when the build step ran; keep building when `-SkipBuild`/`--skip-build` is given
- [x] 2 verify.ps1/verify.sh: db steps log `trx` to `.verify/trx/` with `--blame-hang-timeout 5m`; write `.verify/slowest.txt` (top 20 by duration) after the FULL tier
- [x] 3 Host + UM.API `appsettings.Testing.json` (Serilog MinimumLevel Warning, SeedOnStartup false); both test factories `UseEnvironment("Testing")`
- [x] 4 Explicit seed gate `Database:SeedOnStartup` (default: Development) in Booksy.Host, ServiceCatalog.Api and UserManagement.API entry points; HostCompositionFactory drops the Staging workaround
- [x] 5 Remove the duplicate `Migrate()` and `BuildServiceProvider()` from `TestWebApplicationFactory.ConfigureServices`
- [x] 6 Delete `tests/Booksy.Tests.Common/`, `SC.Application.UnitTests/UnitTest1.cs`; drop `tests/Booksy.ServiceCatalog.UnitTests` from both verify scripts
- [ ] 7 Composition tests: one `[CollectionDefinition]` + `ICollectionFixture<HostCompositionFactory>`; the four classes join it
- [x] 8 New `tests/Booksy.Infrastructure.External.UnitTests`: ZarinPal fake/guard/factory tests from `SC.IntegrationTests/Unit` + the orphaned `ZarinPalServiceTests`, ported to NSubstitute
- [x] 9 New `tests/Booksy.ServiceCatalog.Api.UnitTests`: controller/spec/mapping/claims/SignalR/TokenService unit tests out of the integration project, ported to NSubstitute; both projects in sln, verify lists, CI filter
- [x] 9b Fix verify's shared temp git index: two overlapping runs raced for one `index.lock`, so the loser wrote no `status.json` at all
- [ ] 10 `BookingsControllerTests`: remove the two stale `Skip`s; fix or quarantine per audit §5.3 based on what actually fails
- [ ] 11 `GalleryManagementTests.SetPrimaryGalleryImage_UpdatesBusinessProfileTimestamp`: no `Task.Delay`; back-date the stored timestamp in arrange
- [ ] 12 Remove the four stray `[Collection("Integration Tests")]` attributes and the unused `PostgresTestCollection`
- [ ] 13 `.github/workflows/dotnet.yml`: integration job enabled (Testcontainers, `--no-build`, trx artefact); stale comment deleted
- [ ] 14 `tests/Directory.Packages.props` (central versions for test projects only); FluentAssertions 7.x everywhere; xunit/Test.Sdk/Testcontainers single versions
- [ ] 15 `tests/Directory.Build.props` + `BannedSymbols.txt` (Task.Delay, Thread.Sleep, DateTime.Now/Today, Random) as warnings
- [ ] 16 FULL verify; record total time, per-step times and the ServiceCatalog per-class first-test time in the Log; update `docs/TEST_ARCHITECTURE_AUDIT.md` §2.4 with the measured Phase-1 number

## Decisions
- Test environment named `Testing`, not `Test`: the name must select `appsettings.Testing.json` and must NOT contain the substring the old seed gate matched. Tier 1.
- Seed gate is `Database:SeedOnStartup` read with `IsDevelopment()` as its default, so no existing deployment changes behaviour. Tier 2 (production entry points touched).
- Host logging under test is quieted by Serilog's `MinimumLevel`, not by removing the file sink: the sink lives in `Program.cs` and gating it on the environment would put test-shaped branching in production startup for no further gain. Tier 1.
- Per-run temp git index in both verify scripts (bug fix, tier 2, found by this change: a peer session's verify run and this one collided, and the loser silently produced no `status.json`).
- FluentAssertions pinned to 7.x (last Apache-2.0 line) rather than licensing 8.x or switching to a fork: removes a licence obligation, no API churn in the tests that use it. Reversible by one version number. Flagged in the report.
- Central package management scoped to `tests/` (a `tests/Directory.Packages.props`), not the repo root: the root would force every `src` csproj to drop its versions in the same change, which is out of scope and collides with peer sessions.
- Unit tests leaving the integration project go to two new projects named after the assemblies they test (`Booksy.Infrastructure.External.UnitTests`, `Booksy.ServiceCatalog.Api.UnitTests`) rather than into existing projects whose names would then lie about their contents.

## Log
- 2026-09-11 **Measured after tasks 1–6, 8, 9** (filtered FULL, same machine as the audit's baseline):
  FAST 112 s including a 49 s build, for 961 unit tests (was 61 s of test steps alone for 856 tests, plus build).
  `CategoriesControllerTests` first-test 10.00 s → **8.22 s**; the composition suite 45 s → **29 s** for the same
  21 tests; the ServiceCatalog trx 32.7 MB → **0.46 MB**, which is the proof `appsettings.Testing.json` is being
  loaded. Seeding was therefore worth ~1.8 s per class, less than the audit assumed: the remaining ~8 s is
  migrations (24 ServiceCatalog + 2 UserManagement), CAP's schema, and host startup — all of which Phase 2's
  template database and shared host remove, not Phase 1.
- 2026-09-11 105 tests moved out of the Docker-gated tier into two new FAST projects (70 + 32 there, and the 9
  `ZarinPalServiceTests` that had never compiled at all). `Booksy.ServiceCatalog.Application.UnitTests` reports
  204 rather than 205 because `UnitTest1.Test1`, an empty placeholder, is gone.
- 2026-09-11 Change created from the audit's §9 Phase 1 table (items 1.1–1.14). Peer sessions warned that builds and test runs start now.
