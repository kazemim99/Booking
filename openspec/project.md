# Project Context

> **How to read this file.** Every claim below was verified directly against source code,
> project files, configuration, and tests — not against other documentation. Each claim cites
> the file that proves it. Where a package is referenced but *not wired into the running
> system*, that is stated explicitly rather than implied.
>
> **Verified on 2026-08-16 against the working tree at commit `a8966c3`.** The tree also held
> uncommitted work at the time, so a small number of counts here (notably ServiceCatalog
> migrations) can read one ahead of what that commit alone contains. Structural claims are
> unaffected. When a claim here conflicts with the code, the code wins and this file is the
> bug. See [docs/KNOWLEDGE.md](../docs/KNOWLEDGE.md) for the full source-of-truth hierarchy.

## Purpose

Booksy is a **modular monolith** service-booking and catalog platform. Service providers
register a business, manage services, staff, working hours, and bookings; customers discover
providers and book services. A single ASP.NET Core process (`Booksy.Host`) composes every
bounded context in-process — there are no per-service hosts and no API gateway.

## Tech Stack

### Backend

**Runtime & host**
- **C# / .NET 9.0** — `<TargetFramework>net9.0</TargetFramework>` in all 29 `.csproj` under `src/` and `tests/`
- **ASP.NET Core**, single entry point: `src/Host/Booksy.Host/Program.cs` (`Microsoft.NET.Sdk.Web`)
- Controllers are discovered from **two** bounded-context API assemblies via explicit
  `AddApplicationPart(...)` calls — `Booksy.UserManagement.API` and `Booksy.ServiceCatalog.Api`
  (`Program.cs`). Those assemblies' own `Program` types are inert; only the Host's runs
  (`Booksy.Host.csproj` comment + `ProjectReference` block).

**Persistence**
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` on **EF Core 9.0.4**
- **One database, one connection string** (`ConnectionStrings:DefaultConnection`), **three schemas**:
  - `user_management` — `UserManagementDbContext.cs:57` (`HasDefaultSchema`)
  - `ServiceCatalog` — migration designer files (`HasDefaultSchema("ServiceCatalog")`)
  - `cap` — `CapEventBusExtensions.cs:58` (`postgresOptions.Schema = "cap"`)
- **Migrations run at host startup**: `MigrateAndSeedDatabaseAsync<UserManagementDbContext, …>`
  then `InitializeDatabaseAsync` (`Program.cs`). Seeding happens **only** when the environment is
  Development or its name contains `Test`.
- Migration counts: ServiceCatalog **17**, UserManagement **3**
- `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` is set in `Program.cs`

**Messaging / integration events**
- **DotNetCore.CAP 8.0.0** with a **PostgreSQL outbox** (`DotNetCore.CAP.PostgreSql 8.0.0`)
  and the **in-process in-memory transport** (`Savorboard.CAP.InMemoryMessageQueue 8.0.0`)
  — `CapEventBusExtensions.cs:49-65`
- CAP is registered **once per process**, guarded by an `ICapPublisher` presence check, because
  every context's `AddXInfrastructure()` calls `AddCapEventBus` (`CapEventBusExtensions.cs:41-47`)
- Single consumer group `booksy`, `ConsumerThreadCount = 1`, retry 3× at 60s, CAP dashboard enabled
- **There is no message broker.** See "Explicitly not present" below.

**Application layer**
- **MediatR 13.0.0** — CQRS commands/queries
- **FluentValidation 12.0.0** (+ `.DependencyInjectionExtensions`)
- **AutoMapper 15.0.1** — used in **ServiceCatalog only**
  (`Mappings/ProviderMappingProfile.cs`, `Mappings/ServiceMappingProfile.cs`,
  `ServiceCatalogApplicationExtensions.cs`). UserManagement does not use it.
- **Dependency injection: the built-in `Microsoft.Extensions.DependencyInjection` container**,
  with **Scrutor 6.1.0** for assembly scanning. `Program.cs` never calls
  `UseServiceProviderFactory` / `AutofacServiceProviderFactory`.

**API surface**
- All controllers route under `api/v{version:apiVersion}/…` (23 controllers)
- **Microsoft.AspNetCore.Mvc.Versioning 5.1.0**; default `v1.0`, assumed when unspecified.
  Version readers combined: header `X-Api-Version`, query `api-version`, and URL segment (`Program.cs`)
- **Swashbuckle.AspNetCore 6.5.0** — Swagger UI per API version
- **SignalR** — one hub mapped at `/hubs/notifications`
- **Rate limiting** — `AspNetCoreRateLimit 5.0.0` + `.Redis 2.0.0`, Redis-backed, applied before auth
- Health probes `/health`, `/health/ready`, `/health/live`, all explicitly `AllowAnonymous()`
  (a global authenticated fallback policy would otherwise 401 container probes)

**Caching**
- **Redis** via `Microsoft.Extensions.Caching.StackExchangeRedis 9.0.4` / `StackExchange.Redis 2.8.0`

**Security**
- JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer 9.0.4`), `System.IdentityModel.Tokens.Jwt`
- `BCrypt.Net-Next 4.0.3` for password hashing, `Otp.NET 1.4.0` for OTP
- Policy-based authorization: `AddSecurity()` + `AddPolicyAuthorization()` (`Program.cs`)

**Logging**
- **Serilog** → **Console + rolling daily file** (`logs/booksy-host-.txt`), configured inline in
  `Program.cs` via `UseSerilog(...)` plus `ReadFrom.Configuration`
- `Serilog.Sinks.Seq` is referenced and an `Observability:Seq` config block exists in
  `appsettings.json`, but **no Serilog sink configuration section exists** in
  `appsettings.json` or `appsettings.Development.json` — the Seq sink is **not wired** in the Host today.

**Third-party integrations** (configured in `src/Host/Booksy.Host/appsettings.json`)
- Payments: **ZarinPal** (`Payment:DefaultProvider`), IDPay, Behpardakht enabled; Parsian, Saman disabled.
  `Stripe.net 43.12.0` is referenced.
- Email: `SendGrid 9.29.3`, `MimeKit 4.14.0`
- SMS: **Rahyab** (`Notifications:SMS:Provider`), Kavenegar present but `Enabled: false`
- Storage/images: `Azure.Storage.Blobs 12.19.1`, `SixLabors.ImageSharp 3.1.12`

### Explicitly not present

These were previously asserted in project documentation and are **false**. Verified absent:

| Claim | Reality |
|---|---|
| RabbitMQ message broker | **No** `DotNetCore.CAP.RabbitMQ` package; no broker service in any `docker-compose*.yml`. Transport is `UseInMemoryMessageQueue()`. |
| API Gateway (`Booksy.Gateway` / Ocelot) | **No such project** in `Booksy.sln`; no Ocelot package. The only mention is a historical comment in `Program.cs`. |
| Autofac as DI container | `Autofac.Extensions.DependencyInjection` is referenced, but the container is never swapped. The only `Autofac` usage in `src/` is a stray `using Autofac.Core;` in `ServiceQueryRepository.cs`. |
| snake_case DB naming via EFCore.NamingConventions | Package referenced by 2 projects, but **`UseSnakeCaseNamingConvention` is never called**. Schema names are inconsistent by hand (`user_management` vs `ServiceCatalog`). |
| Architecture tests enforcing layer rules | `NetArchTest.Rules 1.3.2` is referenced, but `tests/Booksy.ArchitectureTests/` contains **only an empty template test** (`UnitTest1.Test1()` with no body). No architecture rule is enforced anywhere. |
| OpenTelemetry / Jaeger / Prometheus / Sentry / App Insights in the running system | All wiring lives in `src/Infrastructure/Booksy.Infrastructure.Monitoring`, which **no project references** (`grep` for `ProjectReference.*Monitoring` returns nothing). It is dead code in the solution. `Program.cs` wires no telemetry. |
| Separate per-context databases | One database, three schemas, one connection string. |

### Frontend

Four client applications live in this repo. **Only `booksy-frontend` is containerized** —
`booksy-admin` and both Flutter apps appear in no `docker-compose*.yml`.

**`booksy-frontend/`** — customer/provider web app (the deployed one)
- Vue **3.5.22**, Pinia **3.0.3**, vue-router **4.5.1**, vue-i18n **10.0.8**, TypeScript, Vite
- Maps: `@neshan-maps-platform/ol` + `vue3-openlayers`; Persian calendar: `jalaali-js`,
  `vue3-persian-datetime-picker`, `@persian-tools/persian-tools`; charts: `echarts` + `vue-echarts`
- Tooling: ESLint + Prettier + `vue-tsc`
- Tests: Vitest (unit/integration), **Cypress** and **Playwright** (`@playwright/test`) for E2E

**`booksy-admin/`** — admin dashboard
- Vue **3.5.24**, **ant-design-vue 4.2.6**, Pinia **3.0.4**, vue-i18n, echarts, axios, dayjs
- Tests: Vitest only. **No ESLint or Prettier** configured (unlike `booksy-frontend`).

**`booksy-customer-app/`** — Flutter (Dart SDK `>=3.0.0 <4.0.0`)
- `flutter_bloc 8.1.3`, `dio`, `go_router 13`, `get_it 7.6.7`, `dartz`, `equatable`
- `retrofit` + `json_annotation` present (codegen), `flutter_map` + `google_maps_flutter`,
  `geolocator`, `flutter_secure_storage`, `shamsi_date`, `pinput`

**`booksy-provider-app/`** — Flutter
- `flutter_bloc 8.1.6`, `dio`, `go_router 13`, `get_it 7.6.7`, `dartz`, `flutter_map 8.1.1`, `pinput`

### DevOps & Infrastructure

**Containers** (`docker-compose.yml` dev / `docker-compose.prod.yml` prod) — six services, no broker:

| Service | Image / build | Ports |
|---|---|---|
| `postgres` | `postgres:16-alpine` | 5432 |
| `redis` | `redis:7-alpine` | 6379 |
| `seq` | `datalust/seq:latest` | 5341 |
| `pgadmin` | `dpage/pgadmin4` | 5050 |
| `booksy-api` | built from `src/Host/Booksy.Host/Dockerfile` | dev `5000:8080`, prod `5000:80` |
| `booksy-frontend` | built from `booksy-frontend/Dockerfile` | 80/443 |

Network: `booksy-network`. Note the internal API port **differs between dev (8080) and prod (80)** —
set by `ASPNETCORE_URLS` in each compose file.

**CI/CD** — GitHub Actions, 5 workflows in `.github/workflows/`:
- `dotnet.yml` — push/PR on `master`, `develop`, `claude/**`. **Builds only**; every test job is
  commented out under a "TEMPORARILY DISABLED TESTS" banner.
- `deploy.yml` — the workflow that actually gates releases:
  `test` (unit tests, `--filter "FullyQualifiedName~UnitTests"`) → `e2e-keystone`
  (`bash tests/e2e/keystone-booking-flow.sh`) → `build-api` + `build-frontend` → deploy.
  **Integration tests are commented out** — they still target the retired per-service hosts
  (tracked in COMPLETION_ROADMAP Epic 3.1).
- `deploy-staging.yml`, `frontend-e2e.yml` (Playwright, advisory), `deploy-docs.yml` (Docusaurus)

## Project Conventions

### Code Style

**Backend (C#)**
- **Nullable reference types: enabled** in 18 of the `src/` projects (not globally enforced —
  there is no `Directory.Build.props`)
- **`TreatWarningsAsErrors` is set in exactly one project**: `Booksy.Core.Domain`. It is *not*
  solution-wide.
- `GenerateDocumentationFile` in 3 projects; `.editorconfig` contains a single rule
  (`dotnet_diagnostic.CS1591.severity = none`)
- **No StyleCop, no Roslynator, no shared ruleset** in this repository
- Implicit usings enabled; per-project `GlobalUsing.cs` files carry shared namespaces
- Naming: PascalCase types/methods/properties, camelCase locals/parameters, `I`-prefixed interfaces

**Frontend (TypeScript/Vue)**
- Composition API with `<script setup>`; feature-based module folders (views, components, stores, types)
- ESLint + Prettier in `booksy-frontend` only

### Architecture Patterns

**Modular monolith over DDD layers.** Each bounded context is four projects —
`Domain`, `Application`, `Infrastructure`, `Api` — composed by the Host.

- **Domain**: aggregate roots deriving `AggregateRoot<TId>`, entities, value objects, domain events
- **Application**: MediatR command/query handlers, FluentValidation validators, event handlers
- **Infrastructure**: EF Core `DbContext`, repositories, Unit of Work, external services
- **Api**: controllers only (served by the Host, not self-hosted)

**Event flow** — domain event raised in an aggregate → domain event handler maps it to an
integration event → CAP writes to the PostgreSQL outbox in the same transaction → CAP delivers
it **in-process** via the in-memory queue → `[CapSubscribe]` handler runs. Handler discovery spans
all referenced assemblies regardless of which context registered the bus.

**Cross-context composition** happens in the Host, not over HTTP: `IProviderInfoService` is
re-registered as `InProcessProviderInfoService` **after** both contexts register, deliberately
replacing UserManagement's HTTP adapter (`Program.cs` + `Composition/InProcessProviderInfoService.cs`).

**Financial ledger.** Money is recorded in an append-only double-entry ledger
(`Domain/Aggregates/LedgerAggregate/` — `LedgerEntry`, `LedgerTransaction`, `LedgerAccount`,
`LedgerEventKeys`), with `LedgerReconciler` and `LedgerMaintenanceBackgroundService` in
Infrastructure. Background work uses `BackgroundService` — **there is no Hangfire in this repo**.

### Testing Strategy

**Backend** — xUnit **2.9.2**
- Assertions: FluentAssertions; mocking: **NSubstitute 5.1.0 and Moq 4.20.72** (both present);
  data: AutoFixture, Bogus; snapshots: Verify; coverage: coverlet; perf: BenchmarkDotNet
- **BDD**: **Reqnroll 2.4.0** (`Reqnroll.xUnit`) — **40 `.feature` files** in
  `tests/Booksy.ServiceCatalog.IntegrationTests/Features/` covering bookings, availability,
  payments, payouts, notifications, registration, authorization, and concurrency edge cases
- **Testcontainers.PostgreSql** for real-database integration tests
- Unit test projects by size: ServiceCatalog.Domain (25 files), ServiceCatalog.Application (17),
  UserManagement.Application (6), Host.CompositionTests (2), Core.Domain (1), Infrastructure.Core (1)
- `Booksy.ArchitectureTests` exists but is an **empty stub** — see "Explicitly not present"

**API-level smoke** — `tests/e2e/keystone-booking-flow.sh` (dependency-free curl script covering
provider → staff → customer → booking) and `tests/e2e/deposit-checkout-flow.sh`. The keystone
script is a **deploy gate** in `deploy.yml`.

**Frontend** — Vitest (unit/integration), Playwright (`booksy-frontend/e2e/`), Cypress.

**Current gap to be aware of**: the general CI workflow runs no tests; only `deploy.yml` gates on
unit tests + the keystone script. Integration tests do not run in CI.

### Git Workflow

- Main branch: **`master`**
- CI triggers on `master`, `develop`, and `claude/**`
- Conventional-style commit subjects are in use (`feat(...)`, `fix(...)`, `chore(...)`)

## Domain Context

### Bounded contexts

**UserManagement** (schema `user_management`) — 3 aggregate roots:
`User`, `Customer`, `PhoneVerification`.
Authentication, OTP phone verification, roles, and customer profiles.

**ServiceCatalog** (schema `ServiceCatalog`) — 14 aggregate roots:
`Provider`, `Service`, `Booking`, `Payment`, `Payout`, `Review`, `ProviderAvailability`,
`OrganizationMembership`, `MembershipAuditEntry`, `ProviderInvitation`, `ProviderJoinRequest`,
`Notification`, `NotificationTemplate`, `UserNotificationPreferences`.
Provider registration and profile, service catalog, staff/membership, availability and bookings,
payments/payouts/ledger, reviews, and notifications.

There is **no Booking bounded context** — `src/BoundedContexts/` contains only `ServiceCatalog`.
Bookings are an aggregate inside ServiceCatalog.

### API controllers (23)

`Auth`, `Authentication`, `Availability`, `Bookings`, `Categories`, `Customers`, `Financial`,
`Locations`, `Memberships`, `NotificationPreferences`, `Notifications`, `Payments`, `Payouts`,
`Platform`, `Profile`, `ProviderAvailability`, `ProviderHierarchy`, `ProviderRegistration`,
`ProviderSettings`, `Providers`, `Reviews`, `Services`, `Users`.

## Important Constraints

**Technical**
- CAP may be registered **only once per process** — respect the guard in `AddCapEventBus`
- Integration events are delivered **in-process**; there is no broker to scale consumers across nodes
- Both contexts share one database and one transaction scope per request — cross-context
  consistency is achieved through CAP's outbox, not distributed transactions
- Migrations run at startup, so they must be idempotent and safe to apply concurrently
- Health endpoints must remain anonymous or container probes fail

**Quality**
- Domain logic belongs in the Domain layer; writes go through MediatR command handlers;
  data access goes through repositories
- Ledger entries are append-only; corrections are compensating transactions, never edits
- Warnings-as-errors is **not** solution-wide today — do not assume the compiler will catch
  what only `Booksy.Core.Domain` enforces

## External Dependencies

**Infrastructure services** — PostgreSQL 16 (5432), Redis 7 (6379), Seq (5341), pgAdmin (5050).

**Third-party services** — ZarinPal / IDPay / Behpardakht (payment gateways, Iranian market),
Rahyab (SMS), SendGrid (email), Neshan Maps (web maps), Google Maps + OpenStreetMap via
`flutter_map` (mobile), Azure Blob Storage (optional image storage),
Azure Key Vault (optional secrets, `Booksy.Configuration.KeyVault`).
