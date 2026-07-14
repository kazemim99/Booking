# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This repository contains both the **source code** and the **deployment configuration** for Booksy, a **modular-monolith** booking platform. The backend is a single ASP.NET Core host (`Booksy.Host`) that composes multiple bounded contexts (UserManagement, ServiceCatalog) in-process. The repository also contains the Vue frontend, Docker Compose configurations, deployment scripts, and GitHub Actions workflows for deploying the application to production servers.

> **Migration note**: The backend was migrated from a microservices architecture to a modular monolith. The Ocelot API Gateway and per-service hosts have been retired, and RabbitMQ has been removed in favor of in-process CAP events. See [MONOLITH_MIGRATION_PLAN.md](MONOLITH_MIGRATION_PLAN.md) for details.

## 📚 Developer Documentation

Docs are organized in three tiers: **root** (living — current architecture, API surface, active plans), **`docs/`** (living but secondary — deployment/testing how-tos), and **`docs/archive/`** (historical — point-in-time implementation write-ups for features that have since shipped; kept for context, not guaranteed current).

### Root (living)

- **[API_ENDPOINTS.md](API_ENDPOINTS.md)** - Complete API endpoint reference (single host on :5000)
  - UserManagement endpoints: Authentication, Customer Management
  - ServiceCatalog endpoints: Categories, Providers, Bookings, Services
  - All endpoints served from one host (`booksy-api`) under `/api/v1/...`
  - All request/response schemas, authentication requirements, and examples

- **[DTO_MAPPING.md](DTO_MAPPING.md)** - DTO mapping across all application layers
  - Backend C# DTOs ↔ Flutter Dart Models ↔ Vue TypeScript Interfaces
  - Type conversion guidelines (Guid → String, decimal → double, etc.)
  - JSON serialization best practices
  - Naming conventions and file locations

- **[TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)** - Architecture patterns, auth/OTP flow, event-driven design, EF Core owned-entity config, known issues & fixes, session history
- **[COMPLETION_ROADMAP.md](COMPLETION_ROADMAP.md)** - Current tracked plan to MVP launch (phases, epics, status)
- **[MONOLITH_MIGRATION_PLAN.md](MONOLITH_MIGRATION_PLAN.md)** - The microservices → modular-monolith migration
- **[GALLERY_BACKEND_REQUIREMENTS.md](GALLERY_BACKEND_REQUIREMENTS.md)** - Open requirements for provider-gallery admin moderation
- **[GEOLOCATION_GUIDE.md](GEOLOCATION_GUIDE.md)** - How homepage location auto-detection works, testing, and debugging
- **[VISUAL_STUDIO_DEBUGGING.md](VISUAL_STUDIO_DEBUGGING.md)** - Running/debugging `Booksy.Host` in Visual Studio against Docker infra

### `docs/` (secondary reference)

- **[docs/REQNROLL_TESTING.md](docs/REQNROLL_TESTING.md)** - Writing/running the Gherkin BDD integration tests
- **[docs/DOCS_SITE_DEPLOYMENT.md](docs/DOCS_SITE_DEPLOYMENT.md)** - Deploying the Docusaurus docs site (`docs-site/`) to GitHub Pages — unrelated to deploying the Booksy app itself

### `docs/archive/`

Point-in-time implementation guides, build-verification snapshots, and planning docs for features that have since shipped (auth flow, booking cancellation/reschedule, provider profile/search, real-time availability, the Reqnroll migration, the original business proposal/SRD, etc.). Useful for historical context on *why* something was built a certain way; not a source of truth for current behavior.

### Application-Specific Documentation

- **Flutter Customer App**: [booksy-customer-app/](booksy-customer-app/)
  - [PROJECT_SUMMARY.md](booksy-customer-app/PROJECT_SUMMARY.md) - Architecture & features
  - [FLUTTER_BACKEND_CONNECTION.md](booksy-customer-app/FLUTTER_BACKEND_CONNECTION.md) - Backend integration guide
  - [CUSTOMER_APP_UX_FLOW.md](booksy-customer-app/CUSTOMER_APP_UX_FLOW.md) - User experience flow

- **Vue Admin Panel**: [booksy-admin/](booksy-admin/) *(if applicable)*
  - Admin dashboard for managing providers, services, and bookings

- **Backend Source**: `src/Host/Booksy.Host` (single ASP.NET Core host) plus bounded contexts under `src/`
  - Domain-Driven Design with CQRS pattern
  - In-process integration events via CAP (DotNetCore.CAP) on the in-memory transport
  - Clean Architecture principles

## Test Suites

- **Playwright E2E** (`booksy-frontend/e2e/`): browser-level tests driving the real Vue app against a running stack (sandbox OTP auth, Page Object Model, `data-testid` selectors). Run with `cd booksy-frontend && npm run e2e:pw`; see `booksy-frontend/e2e/README.md`. CI: `.github/workflows/frontend-e2e.yml` (advisory, not a deploy gate).
- **Reqnroll BDD** (`tests/Booksy.ServiceCatalog.IntegrationTests/`): Gherkin feature files + C# step definitions covering ServiceCatalog business logic end-to-end at the API layer. Run with `dotnet test --filter "FullyQualifiedName~Feature"`; see [docs/REQNROLL_TESTING.md](docs/REQNROLL_TESTING.md).
- **API keystone smoke test** (`tests/e2e/keystone-booking-flow.sh`): dependency-free curl script exercising the full provider→staff→customer→booking flow against a running host; used as a CI deploy gate (`e2e-keystone` job in `deploy.yml`/`deploy-staging.yml`).
- **Cypress** (`booksy-frontend/cypress/`): coexists with the Playwright suite (`npm run test:e2e`).
- **Backend unit tests**: `dotnet test --filter UnitTests` (solution-wide; run in CI on every deploy).

## Architecture

Booksy is a **modular monolith**: a single backend host composes multiple bounded contexts in-process.

### Application Services
- **Booksy.Host** (`booksy-api`, Port 5000 → internal 80): Single ASP.NET Core host that composes both bounded contexts (UserManagement and ServiceCatalog) in-process and serves all of their controllers under `/api/v1/...`. (A Booking context exists only as empty scaffolding and is not built.) Database migrations run at host startup.
- **Frontend** (Ports 80/443): Web application frontend served via Nginx; its nginx config proxies `/api` to `booksy-api:80`.

### Infrastructure Services
- **PostgreSQL** (Port 5432): Single primary database (`booksy`) with schema-per-context (schemas: `user_management`, `ServiceCatalog`, `cap`). One connection string (`DefaultConnection`).
- **Redis** (Port 6379): Caching layer with LRU eviction policy (512MB limit)
- **Seq** (Ports 5341, 5342): Centralized structured logging platform
- **pgAdmin** (Port 5050): Database management interface

### Service Communication
- All containers connect via a Docker bridge network (`booksy-network`, subnet 172.25.0.0/16)
- Containers communicate using container names as DNS hostnames
- Cross-context integration events run **in-process** via CAP (DotNetCore.CAP) on its in-memory transport (`EventBus:Provider=InMemory`) — there is no message broker container
- Redis provides distributed caching and session management

## Common Commands

### Deployment
```bash
# Full deployment (pulls latest images and restarts all services)
cd /root/booksy && ./scripts/deploy.sh

# Manual deployment steps
cd /root/booksy
docker-compose -f docker-compose.prod.yml pull

# Clean up orphaned containers (prevents network removal errors)
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f || true

docker-compose -f docker-compose.prod.yml down --remove-orphans
docker-compose -f docker-compose.prod.yml up -d

# View all service status
docker-compose -f docker-compose.prod.yml ps

# View logs for specific service
docker-compose -f docker-compose.prod.yml logs -f [service-name]
# Example: docker-compose -f docker-compose.prod.yml logs -f booksy-api

# View logs for all services
docker-compose -f docker-compose.prod.yml logs -f
```

### Service Management
```bash
# Start all services
docker-compose -f docker-compose.prod.yml up -d

# Stop all services
docker-compose -f docker-compose.prod.yml down

# Restart a specific service
docker-compose -f docker-compose.prod.yml restart [service-name]

# Scale a service (if supported)
docker-compose -f docker-compose.prod.yml up -d --scale booksy-api=3
```

### Database Operations
```bash
# Access PostgreSQL shell
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_user_management

# Create database backup
docker exec booksy-postgres pg_dump -U booksy_admin booksy_user_management > backup_$(date +%Y%m%d_%H%M%S).sql

# Restore from backup
docker exec -i booksy-postgres psql -U booksy_admin booksy_user_management < backup.sql

# View database logs
docker logs booksy-postgres
```

### Redis Operations
```bash
# Access Redis CLI
docker exec -it booksy-redis redis-cli -a YourRedisPassword123!

# Monitor Redis commands in real-time
docker exec -it booksy-redis redis-cli -a YourRedisPassword123! MONITOR

# Check Redis memory usage
docker exec -it booksy-redis redis-cli -a YourRedisPassword123! INFO memory
```

### Integration Events (CAP)

Cross-context integration events run in-process via CAP on its in-memory transport — there is no RabbitMQ broker. CAP persists outbox/inbox state in the `cap` schema of the PostgreSQL database. To inspect published/received messages, query the CAP tables in Postgres or use the Seq logs.

### Monitoring and Logging
```bash
# Access Seq logging UI
# Open browser to: http://server-ip:5341

# View container resource usage
docker stats

# Check health status of all services
docker-compose -f docker-compose.prod.yml ps

# View specific service health
docker inspect --format='{{.State.Health.Status}}' booksy-[service-name]
```

### API Documentation (Swagger)
```bash
# Access Swagger UI on the single host:
# Booksy API: http://server-ip:5000/swagger

# Note: The service must be healthy for Swagger to be accessible
# Check service health: docker ps
```

### Cleanup and Maintenance
```bash
# Remove stopped containers and unused images
docker system prune -a

# Remove only unused images
docker image prune -f

# View disk usage by Docker
docker system df

# Clean up orphaned booksy containers (all containers with 'booksy-' prefix)
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f

# Clean up old backups (manual)
cd /root/booksy/backups && ls -lt | tail -n +10 | awk '{print $9}' | xargs rm -f
```

## GitHub Actions Workflows

### Build and Push (`build-and-push.yml`)
- Triggers on: Push to main/develop, PRs, or manual dispatch
- Builds Docker images for the backend host (`booksy-api`) and the Frontend
- Pushes images to GitHub Container Registry (ghcr.io)
- Uses Docker layer caching for faster builds
- Tags images with branch name, PR number, commit SHA, and 'latest' for main branch

### Deploy (`deploy.yml`)
- Triggers automatically when build-and-push completes successfully on main branch
- Can also be manually triggered via workflow_dispatch
- Uses SSH to connect to production server
- Pulls latest Docker images from GHCR
- **Forcibly removes orphaned containers** before docker-compose down to prevent network errors
- Performs zero-downtime deployment by stopping old containers and starting new ones
- Includes automatic cleanup of old Docker images

## Environment Configuration

All environment variables are stored in `/root/booksy/.env`. Key variables include:

- **Database**: `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`
- **Redis**: `REDIS_PASSWORD`
- **Seq**: `SEQ_FIRSTRUN_ADMINUSERNAME`, `SEQ_FIRSTRUN_ADMINPASSWORD`
- **Container Registry**: `GITHUB_REPOSITORY_OWNER` (currently: kazemim99)

Never commit the `.env` file to version control. The `.env.backup` file should also be excluded from commits.

## Health Checks

All services have health checks configured using **curl**:

- **Backend API** (`booksy-api`): HTTP check on `/health` endpoint using `curl -f` (30s interval, 10s timeout, 3 retries, 40s start period)
- **PostgreSQL**: `pg_isready` command (10s interval, 5s timeout, 5 retries, 10s start period)
- **Redis**: `redis-cli ping` (10s interval, 5s timeout, 3 retries, 5s start period)
- **Seq**: HTTP check on `/api/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)
- **Frontend**: HTTP check on `/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)

**Important**: All Docker images must have `curl` installed for health checks to work. The Dockerfiles in the source repository include curl installation:
- .NET host: `apt-get install -y curl`
- Frontend (nginx:alpine): `apk add --no-cache curl`

## Resource Limits

Services have CPU and memory constraints:

- **Backend API** (`booksy-api`): 1 CPU / 1GB RAM (reserved: 0.25 CPU / 256MB)
- **Frontend**: 0.5 CPU / 256MB RAM (reserved: 0.1 CPU / 64MB)
- **PostgreSQL**: 2 CPU / 2GB RAM (reserved: 0.5 CPU / 512MB)
- **Redis**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **Seq**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **pgAdmin**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)

## Service Dependencies

The startup order is enforced through Docker Compose dependencies:

1. **Infrastructure** (PostgreSQL, Redis, Seq) starts first
2. **Backend host** (`booksy-api`) waits for infrastructure health checks, then runs migrations at startup
3. **Frontend** waits for `booksy-api` to be healthy

## Security Considerations

- Database and cache ports (5432, 6379) are bound to localhost only (`127.0.0.1`)
- Passwords should be changed from defaults in `.env` before production use
- GitHub Container Registry authentication is required for pulling images
- SSH key-based authentication is used for deployment automation
- All services run in an isolated Docker network with defined subnet

## Troubleshooting

Common issues and solutions:

### Other Common Issues

1. **Service won't start**: Check logs with `docker-compose logs [service]` (e.g., `booksy-api`) and verify health check status
2. **Database connection errors**: Ensure PostgreSQL is healthy and connection string in `.env` is correct
3. **Out of memory**: Check `docker stats` and adjust resource limits in docker-compose.prod.yml
4. **Image pull failures**: Verify GHCR authentication with `docker login ghcr.io`
5. **Port conflicts**: Ensure no other services are using the required ports (5000, 80, 443, 5341, 5050)
6. **Swagger not accessible**: Verify `booksy-api` is healthy with `docker ps`. An unhealthy host cannot serve Swagger UI.

## Testing Policy

Testing is mandatory, not optional. Quality is a non-negotiable requirement: every change must maintain or improve the project's reliability, and every feature is incomplete until all relevant automated tests pass. Every code change must include appropriate automated tests unless there is a documented technical reason why a specific test type is not applicable.

### Existing Tests First

Whenever modifying existing code:

- First search for all existing tests related to the affected functionality.
- Prefer updating and extending existing tests instead of creating duplicate test files.
- Follow the project's existing testing architecture, naming conventions, folder structure, fixtures, helper utilities, and testing frameworks.
- Keep the test suite clean, maintainable, and consistent with the rest of the project.
- Avoid duplicate or redundant tests unless they provide additional coverage or protect against a different regression.
- If existing test coverage is insufficient, extend it before creating entirely new test suites.
- Explain which existing tests were reviewed, which were modified, and why new tests were necessary.
- Never introduce code changes without verifying that the relevant existing tests still pass.
- If a change affects business logic, APIs, data access, events, caching, background jobs, or concurrency, ensure the existing test suite is updated accordingly.
- At the end of every implementation, include a Testing Summary that lists: Existing tests reviewed, Tests modified, New tests added, Test types covered, Regression risks addressed.

### Test Integrity — Never Weaken the Suite

- Existing tests must NEVER be removed, weakened, skipped, or disabled simply to make CI pass. No ignored failures, no commented-out assertions.
- A test may be removed only when it is demonstrably obsolete (the behavior it protected no longer exists). Explain why, and replace it with equivalent or stronger protection when the behavior has a successor.
- When business logic changes: first identify every existing test affected by the change, update those tests to the new behavior, then add new tests for the new behavior. Do this before declaring the change done.

### Test Pyramid & Test Selection

Follow the Test Pyramid. Priority order:

1. **Unit Tests** — domain logic, use cases, blocs/handlers, validators
2. **Integration Tests** — interaction between layers (API + DB + events); prefer realistic integration tests over excessive mocking
3. **Component/Widget Tests** — reusable UI components and screen state rendering
4. **End-to-End Tests** — only for critical user journeys, cross-screen workflows, and regression protection; avoid E2E where a lower-level test provides equivalent confidence

Choose the correct test types per change from: Unit, Integration, API, Widget, Golden (UI consistency), E2E, Regression, Contract, Database/Repository, State Management, Smoke, Performance, Accessibility, Concurrency/Race Condition. If a normally-expected test type is not applicable, state why. Do not generate unnecessary tests — select the minimal set that provides high confidence while keeping the suite maintainable.

### Mandatory Engineering & Testing Policy

Every code change must prioritize correctness, reliability, maintainability, and regression prevention over implementation speed.

**1. Analyze before coding.** Before making any change: understand the existing business logic, identify all affected components, determine the risk of the change, and identify which tests must be added, updated, or verified. Never modify code until you understand the existing behavior.

**2. Mandatory testing.** Every change must be validated with the appropriate level of testing, selected per the Test Pyramid above.

**3. Regression prevention.** Every bug fix MUST follow this order: (a) reproduce the bug with a failing test, (b) implement the fix, (c) verify the new test passes, (d) verify no existing tests broke. Never fix a bug without adding regression protection.

**4. Business logic coverage.** Every new or modified business rule must be covered by automated tests, including: happy paths, failure scenarios, edge cases, boundary values, invalid inputs, error handling, exception paths, authorization/permission rules, null and empty values, time-based behavior, state transitions, and concurrent execution / race conditions (when applicable).

**5. Verify side effects.** After every implementation, verify the change does not unintentionally affect: backward compatibility, API compatibility, database behavior, event publishing/consumption, background jobs, caching, authentication & authorization, logging, error handling, performance.

**6. Testing-first mindset.** Before implementing, explain which tests already exist, which should be added, and why each is necessary. After implementing, write/update the required tests and ensure existing tests still pass. Do not consider a task complete until the appropriate automated tests are included.

**7. Completion checklist.** Every completed task must end with a report containing: Files Changed, Tests Added, Tests Updated, Test Types Covered, Scenarios Verified, Potential Risks, Recommended Future Tests (if any). Never state a task is complete unless the required tests have been implemented, or you explicitly explain why a particular test is unnecessary.

### Test Execution & Completion Workflow

At the end of every implementation, in order:

1. Determine which test types the change requires (per the Test Pyramid).
2. Implement or update those tests.
3. Execute all affected tests — Unit, Integration, Widget, and any impacted E2E — plus static analysis (`flutter analyze` for the mobile app, analyzers/linters for .NET and the frontend).
4. Investigate and fix the root cause of any failure — never work around it.
5. Verify no regressions were introduced elsewhere in the suite.
6. Only then consider the task complete.

### Test Quality Standards

Tests must be readable, deterministic, isolated/independent, fast, maintainable, and self-documenting (descriptive names; one behavior per test). Use Arrange-Act-Assert. Prefer factories/builders over large inline objects. Mock only external dependencies — never business logic; prefer real implementations in integration tests.

Avoid: flaky tests, arbitrary sleeps/delays (await deterministic conditions instead), fragile/unstable selectors (use `data-testid` on web, `Key`/finder-by-type in Flutter), shared mutable state between tests, duplicated setup, and magic numbers.

### Coverage Expectations

Target meaningful coverage that protects business behavior — never write tests just to raise a percentage. Expected minimums: business logic and core services 90%+, state management (blocs/cubits) 90%+, repositories 80%+, critical user flows 100% via integration and/or E2E tests. Coverage numbers alone never indicate quality.

### Mobile App Testing (`booksy-customer-app`)

Run with `flutter analyze` (must be error-free) and `flutter test`. In addition to the general policy, validate whenever the change touches them:

- **Navigation & deep links**: router redirects, per-tab back stacks, Android back behavior, return-to-intent (`test/config/routes/app_router_test.dart` is the pattern)
- **Authentication**: OTP flow, session restore, guest gating, auth-state transitions
- **Booking flows**: step transitions, selection preservation, slot-taken recovery, cancel/reschedule optimistic updates + rollback (see `test/features/booking/`, `test/features/bookings/`)
- **Payments**: when payment flows land, they are critical-path — integration + E2E required
- **State management**: every Bloc/Cubit gets dedicated tests — initial state, transitions, failure paths, retry behavior, race conditions (stale-result guards), unexpected user actions
- **Widget rendering**: every reusable `core/widgets` component — rendering, interactions, disabled/loading/error states, accessibility labels, theming (see `test/core/widgets/widgets_test.dart`)
- **Screen states**: loading skeleton / content / empty / error via `StateSwitcher`; pull-to-refresh
- **Offline & network failures**: offline banner, fail-fast `NetworkFailure` mapping, retry logic
- **Localization & RTL**: Persian strings from `AppStrings` only; RTL rendering; layouts must not break under LTR
- **Accessibility**: semantics labels, ≥48dp touch targets, 1.3× font scale without overflow, reduced-motion (`disableAnimations`)
- **Screen sizes & dark mode**: no overflow at small widths; when dark theme ships, both themes are tested
- **App lifecycle**: state restoration and background/foreground transitions for flows holding in-progress state (e.g. the booking flow)
- **Push notifications**: if/when added, cover receipt-driven navigation and permission states
- **Performance-critical flows**: list scrolling with images, search debouncing — verify no jank-inducing rebuilds in hot paths

Known constraint: `build_runner` codegen is currently broken (retrofit_generator/SDK incompatibility) — new services use manual JSON parsing and manual `get_it` registration in `core/di/injection.dart`; do not add `@JsonSerializable`/`@injectable` code that requires regeneration until the toolchain is fixed.

### CI & Release Quality Gates

Every Pull Request must leave the project in a releasable state:

- No failing tests; no skipped critical tests; no ignored failures; no known-flaky tests merged.
- Static analysis clean: `flutter analyze` (mobile), Roslyn analyzers/StyleCop (backend), ESLint (frontend). Fix warnings you introduce; never suppress diagnostics to silence them.
- Security: no secrets in code or config committed to the repo; authorization rules covered by tests when auth-adjacent code changes; validate all external input at API boundaries.
- Architecture: respect the existing layering (Clean Architecture / DDD boundaries; presentation → domain → data in Flutter). New code follows the established patterns of its module — deviations require an explicit, documented reason.
- Performance: for changes on hot paths (queries, list rendering, event handlers), state the expected impact and verify it — no unbounded queries, no N+1s, no per-frame allocations in scroll paths.
- Migrations remain idempotent; deploy gates (keystone E2E) must stay green.
