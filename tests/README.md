# tests/

Which project a test belongs in, and how the suite runs. Policy and rationale live in
[AGENTS.md](../AGENTS.md) and [docs/TEST_ARCHITECTURE_AUDIT.md](../docs/TEST_ARCHITECTURE_AUDIT.md);
this file is the short, practical map.

## The rule

If a test needs Testcontainers, `WebApplicationFactory`, or a `DbContext`, it is not a unit test —
it belongs in `AsanRezerve.Host.IntegrationTests`. Everything else (pure logic, a handler with its
dependencies substituted, a controller with the mediator substituted, a specification, a mapping)
belongs in one of the unit projects below.

## Projects

**Unit / architecture** — no Docker, run by `scripts/verify -Tier fast`, ~10 s for ~965 tests:

| Project | Covers |
|---|---|
| `AsanRezerve.Core.Domain.UnitTests` | Core value objects, domain exceptions |
| `AsanRezerve.Infrastructure.Core.UnitTests` | Shared infrastructure (caching, persistence base) |
| `AsanRezerve.ServiceCatalog.Domain.UnitTests` | ServiceCatalog aggregates, value objects, domain events |
| `AsanRezerve.ServiceCatalog.Application.UnitTests` | ServiceCatalog command/query handlers, services |
| `AsanRezerve.ServiceCatalog.Api.UnitTests` | ServiceCatalog controllers, specifications, mapping |
| `AsanRezerve.Infrastructure.External.UnitTests` | Gateway adapters (payment, notifications) |
| `AsanRezerve.UserManagement.Application.UnitTests` | UserManagement application layer, including `JwtTokenService` |
| `AsanRezerve.ArchitectureTests` | Cross-cutting architecture rules (NetArchTest) |

**Integration** — real Postgres via Testcontainers, run by `scripts/verify -Tier full`:

| Project | Covers |
|---|---|
| `AsanRezerve.Host.IntegrationTests` | Everything that boots the real composed host: ServiceCatalog and UserManagement API/persistence behavior (`ServiceCatalog/`, `UserManagement/` folders, one shared `AsanRezerveHostFactory`), and host composition itself (`Composition/` folder, its own unfaked `HostCompositionFactory`) |

One project, not three, since `docs/TEST_ARCHITECTURE_AUDIT.md` Phase 2 slice 4 — it used to be
`AsanRezerve.ServiceCatalog.IntegrationTests`, `AsanRezerve.UserManagement.IntegrationTests` and
`AsanRezerve.Host.CompositionTests`, each booting its own host. Inside it, `ServiceCatalog/` and
`UserManagement/` tests share one collection (`AsanRezerveHostTestCollection`) and one faked host
(payment gateway, notification senders replaced with capturing fakes); `Composition/` tests run
against the real, unfaked production DI graph in their own collection
(`HostCompositionCollection`). The two collections run in parallel with each other
(`xunit.runner.json`, `[assembly: CollectionBehavior(MaxParallelThreads = 2)]`); classes within a
collection never run concurrently with each other.

**Shared test infrastructure**: `AsanRezerve.Tests.Commons` — `PostgresTestContainerFixture` (one
Postgres server per test process, one database per factory), `DatabaseReset` (per-test
`TRUNCATE`), `ResettableDistributedCache`, `IResettableFake`, `IntegrationTestAuthenticationHandler`
(claims-based test auth — no JWT is minted or needed), builders (`ProviderBuilder`,
`ServiceBuilder`), AutoFixture customizations. No project references this for its own sake; it
exists because `AsanRezerve.Host.IntegrationTests` needs it.

## Conventions

- **Package versions** come from [Directory.Packages.props](Directory.Packages.props) — one
  version per package across every test project. A `Version=` attribute on a `PackageReference` in
  a test `.csproj` is a mistake; add the version there instead.
- **Banned symbols** ([BannedSymbols.txt](BannedSymbols.txt)) fail the build's warning bar:
  `Task.Delay`, `Thread.Sleep`, `DateTime.Now`/`.Today`, and unseeded `Random` (a seeded
  `new Random(seed)` stays legal — used deliberately in a few property-style concurrency tests, see
  `BookingSlotIntegrityTests`).
- **Test isolation**: every integration test starts from an empty database (`DatabaseReset`, one
  `TRUNCATE` per test, run from `IntegrationTestBase.InitializeAsync`), empty caches (`IMemoryCache`
  cleared, `ResettableDistributedCache` swapped), every capturing fake reset, and no signed-in user.
  A test that depends on another test's leftover data is a bug in that test, not something to work
  around at the fixture level.
- **Environment**: test hosts run as `ASPNETCORE_ENVIRONMENT=Testing`, which loads
  `appsettings.Testing.json` (quiet logging, `Database:SeedOnStartup=false`, in-memory cache,
  rate limiting effectively unlimited).
- **Per-test timings**: every FULL run writes `.verify/trx/*.trx` and `.verify/slowest.txt`. A
  slow "first test" in a trx is usually a class's shared collection fixture booting the host, not
  that test itself.
- **`known-failures.txt`**: tests that are known-red and quarantined, with a reason and an owning
  change — not a place to park a failure without a plan to fix it.

## Running

```
scripts/verify.ps1 -Tier fast    # unit + architecture, no Docker
scripts/verify.ps1 -Tier full    # + AsanRezerve.Host.IntegrationTests, + touched frontend/mobile apps
```

POSIX twin: `scripts/verify.sh fast|full`. Result: `.verify/status.json`. A `-Filter`ed FULL run is
recorded as filtered and does not satisfy the Stop hook's FULL-verify requirement — filters are for
iterating on one failure, not for claiming a change is done.
