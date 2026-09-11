# CLAUDE.md

Routing guidance for Claude Code in this repository. It is *guidance*, not an authoritative
source: policy lives in `AGENTS.md`, facts live in `openspec/project.md` and the code.

## Read first

1. **[AGENTS.md](AGENTS.md)** — the governing policy for every AI assistant here: source-of-truth
   ordering, the test-first standard, and the **Operating Model** (the autonomous implementation
   loop, decision tiers, stop conditions, the `tasks.md` contract, verification tiers).
2. **[openspec/project.md](openspec/project.md)** — what this system *is*, verified against source.
3. **[docs/KNOWLEDGE.md](docs/KNOWLEDGE.md)** — which document wins when two disagree.

Your memory is a cache. Verify recalled facts against git; when memory and code disagree, the
code is right.

## How work runs here

- **Loop state is a file.** Features: `openspec/changes/<id>/tasks.md`. Bugs and small changes:
  `openspec/changes/_inline/<slug>/tasks.md`. Start with `/implement <change-id>`.
- **Definition of done is a command.** `scripts/verify.ps1 -Tier fast` after each task (build +
  unit/architecture tests, no Docker); `scripts/verify.ps1 -Tier full` to finish (adds Host
  composition, both integration suites via Testcontainers, and touched Vue/Flutter apps). POSIX
  twin: `scripts/verify.sh fast|full`. Result in `.verify/status.json`.
- **The Stop hook** (`.claude/hooks/stop-gate`) blocks ending a turn while `[ ]` tasks remain in
  the active change or the FULL verify is missing, stale, or red. `Status: DONE` or
  `Status: STOPPED(<condition>)` as the first line of `tasks.md` is the explicit way out.
- **Protected operations ask** (`.claude/settings.json`): push, PR create/merge,
  `dotnet ef database update`, prod/staging compose, ssh/scp.
- **Unattended runs:** `scripts/agent-run.ps1 <change-id>` repeats `/implement` until DONE/STOPPED.
- Full design and rationale: [docs/AUTONOMOUS_OPERATING_MODEL.md](docs/AUTONOMOUS_OPERATING_MODEL.md).

## What this repository is

Booksy, a **modular-monolith** booking platform: one ASP.NET Core host (`src/Host/Booksy.Host`,
`booksy-api` on :5000) composes the UserManagement and ServiceCatalog bounded contexts in-process
(DDD + CQRS, CAP in-memory integration events, one PostgreSQL database with schema-per-context,
Redis, Seq). Clients: Vue web app (`booksy-frontend`), Vue admin (`booksy-admin`), Flutter
customer and provider apps (`booksy-customer-app`, `booksy-provider-app`). Docker Compose and
GitHub Actions deploy it. Migration history: [MONOLITH_MIGRATION_PLAN.md](MONOLITH_MIGRATION_PLAN.md).

## Test suites

Which level a test belongs at, and the plan for this architecture:
[docs/TEST_ARCHITECTURE_AUDIT.md](docs/TEST_ARCHITECTURE_AUDIT.md) (§3 the rule, §9 the roadmap).
Short version: if it needs Testcontainers, `WebApplicationFactory` or a `DbContext` it is not a unit
test; everything else belongs in a unit project.

- **Backend unit/architecture**: the eight projects `scripts/verify` runs in FAST — no Docker, ~10 s
  of test time for ~960 tests. Two of them (`Booksy.ServiceCatalog.Api.UnitTests`,
  `Booksy.Infrastructure.External.UnitTests`) hold controller, specification, mapping and gateway-adapter
  tests that used to sit inside the integration project behind Docker.
- **Integration** (`tests/Booksy.ServiceCatalog.IntegrationTests`, `tests/Booksy.UserManagement.IntegrationTests`, `tests/Booksy.Host.CompositionTests`): real composed host against Testcontainers Postgres, plain xUnit. Reqnroll/Gherkin BDD was retired 2026-09-11 — see `openspec/changes/_inline/retire-reqnroll/tasks.md`.
- **Test-project conventions**: versions come from `tests/Directory.Packages.props` (one version per
  package; a `Version=` in a test csproj is a mistake). `tests/BannedSymbols.txt` fails the build's
  warning bar on `Task.Delay`, `Thread.Sleep`, `DateTime.Now/Today` and unseeded `Random` — a seeded
  `new Random(seed)` stays legal for property tests. Test hosts run as `ASPNETCORE_ENVIRONMENT=Testing`,
  which loads `appsettings.Testing.json` (quiet logging, `Database:SeedOnStartup=false`, in-memory cache).
- **Per-test timings**: every FULL run writes `.verify/trx/*.trx` and `.verify/slowest.txt`. The first
  test of a class carries that class's fixture, so a 9-second "first test" is a class booting a host.
- **API keystone smoke test** (`tests/e2e/keystone-booking-flow.sh`): curl script over the full provider→staff→customer→booking flow; CI deploy gate (`e2e-keystone`).
- **Playwright E2E** (`booksy-frontend/e2e/`, `npm run e2e:pw`) and **Cypress** (`npm run test:e2e`): advisory, not deploy gates.
- **Flutter**: `flutter analyze` + `flutter test` in each app; policy detail in `AGENTS.md › Mobile App Testing`.

## Where the documents are

- Root (living): [API_ENDPOINTS.md](API_ENDPOINTS.md), [DTO_MAPPING.md](DTO_MAPPING.md), [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md), [COMPLETION_ROADMAP.md](COMPLETION_ROADMAP.md), [ARCHITECTURAL_DECISIONS.md](ARCHITECTURAL_DECISIONS.md), [GEOLOCATION_GUIDE.md](GEOLOCATION_GUIDE.md), [VISUAL_STUDIO_DEBUGGING.md](VISUAL_STUDIO_DEBUGGING.md).
- Operations: [docs/DEPLOYMENT_RUNBOOK.md](docs/DEPLOYMENT_RUNBOOK.md) — compose commands, health checks, resource limits, environment variables, troubleshooting (moved out of this file 2026-09-08).
- Navigation and staleness: [docs/KNOWLEDGE_MAP.md](docs/KNOWLEDGE_MAP.md). `docs/archive/` and `docs-site/` are historical.
- Apps: [booksy-customer-app/PROJECT_SUMMARY.md](booksy-customer-app/PROJECT_SUMMARY.md), [booksy-customer-app/FLUTTER_BACKEND_CONNECTION.md](booksy-customer-app/FLUTTER_BACKEND_CONNECTION.md).
- Procedures: `.hermes/skills/` (`implementation-loop`, `openspec-change-lifecycle`, `verify-before-claiming`).
