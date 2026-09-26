## Why

User request 2026-09-25 (as architect): review logging and caching end to end so the apps load data faster with
the lowest latency, following best practice; make the whole system's behaviour observable and analysable by AI;
and show every log in the admin panel, with log levels set from there.

The review (2026-09-25, against `a5f4955`) found that both systems are partly dead and partly harmful:

**Logging**
- Serilog runs through `UseSerilog`, whose `SerilogLoggerFactory` bypasses Microsoft.Extensions.Logging filters, and
  `appsettings.json` has no `Serilog` section. Every `Logging:LogLevel` value in the file is therefore ignored and
  production logs **everything at Information** — every EF Core SQL command, every ASP.NET Core routing step —
  synchronously to console and file on the request thread.
- `LoggingBehavior` serialises every MediatR request to JSON at Information: passwords (`AuthenticateUserCommand`,
  `ChangePasswordCommand`), OTP codes, refresh tokens and phone numbers go into the logs. `PerformanceBehavior`
  repeats it for slow requests. The Rahyab sandbox logs the SMS body, which contains the OTP.
- One failing command writes up to five Error events with the same stack trace (LoggingBehavior, TransactionBehavior,
  RequestLoggingMiddleware, ExceptionHandlingMiddleware twice); expected 4xx exceptions are logged as Errors.
- No correlation id: `RequestLoggingMiddleware` invents a GUID nobody else sees; nothing returns a trace id to the
  client, so a user-reported error cannot be found in the logs.
- Seq is not wired (`Seq__ServerUrl` in prod compose is read by nothing); `AsanRezerve.Infrastructure.Monitoring`
  (OpenTelemetry, Prometheus) is referenced by no project; `BookingMetrics` counters go nowhere.

**Caching**
- `CachedProviderReadRepository`/`CachedServiceReadRepository` put domain **aggregates** into Redis as JSON. They have
  private setters and private constructors, so they cannot round-trip (FOLLOW-UPS #70). It has not exploded only
  because production's cache connection string defaults to `localhost` inside the container (compose sets only
  `ConnectionStrings__Redis`), so the cache never reaches Redis; the circuit breaker opens and a request pays a Redis
  connect timeout every 30 s.
- The MediatR query cache uses **sliding** expiration with **no invalidation**: the provider availability calendar (a
  customer can be offered a slot that is already booked), customer details, favourites and user details stay stale for
  as long as they keep being read. `SearchUsersQuery`'s hand-built key omits five filters, so different searches share
  one entry. The query cache lands in Redis under the rate limiter's `RateLimit_` prefix.
- Provider invalidation runs `KEYS Provider:owner:*` over the whole keyspace on every provider event, before the
  transaction commits (a concurrent read can re-cache the old row), and is a no-op in the in-memory implementation.
- Nothing caches what the customer apps actually read most (salon page, salon lists, categories, locations), and the
  response envelope middleware parses and re-serialises every JSON response.

## What Changes

**Logging pipeline (system-logging).** Microsoft.Extensions.Logging becomes the single level gate (so
`Logging:LogLevel` is live again) and Serilog the pipeline: asynchronous console and compact-JSON rolling-file sinks
with size limits, an optional Seq sink (opt-in by `Seq:ServerUrl`), and a database log store. A masking enricher
redacts secrets (passwords, OTP codes, tokens, keys) and partially masks phone numbers and e-mail addresses in every
event before any sink sees it. Every request gets one completion event (method, route, status, elapsed, user, trace
id) and an `X-Trace-Id` response header; the same trace id is in the success and error envelopes. An exception is
logged once, at the right level (4xx Information, 5xx Error). MediatR request payloads are logged only at Debug,
masked.

**Runtime log levels (runtime-log-levels).** An admin can set the level of any category (or the default) from the
admin panel, optionally for a limited time after which it reverts by itself. Overrides persist across restarts and
every change is audited (who, what, from → to).

**Log explorer (log-explorer).** Events at Information and above are written in batches to `observability.log_events`
through a bounded, non-blocking queue (a slow or down database never slows a request) and kept for **14 days**
(decision 2026-09-25). Admins (**AdminOnly**, decision 2026-09-25) search by time, level, text, source, trace id,
path and status; open one event; see every event of one request; and read a system overview (request rates, error
rates, p50/p95 latency by route, level counts, cache statistics, log-store health, process health).

**AI analysis (log-explorer).** An AI digest endpoint (JSON and Markdown) summarises a time window for an LLM: error
groups by fingerprint with counts, first/last seen and sample trace ids; the slowest routes; level counts; cache hit
ratios. A dependency-light **MCP server** (`tools/observability-mcp`) exposes search, trace, digest, overview, log
levels and cache tools to Claude Code / Claude Desktop through the admin API with an admin token (decision
2026-09-25: MCP + digest; the server itself sends logs nowhere).

**Caching (read-caching).**
- One cache: .NET `HybridCache` — in-process L1 plus Redis L2, stampede protection (one factory run per key under
  concurrent misses), **tag** invalidation, absolute expiration. L2 sits behind a circuit breaker so a Redis outage
  costs a cache miss, never a timeout per request. The Redis connection comes from `ConnectionStrings:Redis` (one
  multiplexer shared by the cache, rate limiting and OTP state), which fixes production's localhost cache.
- Cache read models, not aggregates: the aggregate decorators and `ICacheService` are removed; queries opt in with a
  key, tags and a lifetime. Keys include every query parameter by default.
- Invalidation hangs off the save pipeline and is commit-safe: any saved change to a salon, its services or staff
  evicts its tags after the save and again when the request scope ends (after commit), so a concurrent read cannot
  pin the old row.
- Cached: salon page (`GET Providers/{id}`), non-geographic salon lists, categories, locations.
  Caching removed where it was wrong: availability calendar (must be live), customer/user details, favourites,
  admin user search.
- Admins see hit ratios per region, L1/L2 state, and can purge a tag or everything.

**Hot path.** The response envelope embeds the controller's JSON without re-parsing it, and JSON output stops
escaping Persian text as `\uXXXX` (smaller payloads, less CPU).

**Admin panel.** The Logs page (placeholder today) gets three tabs: Events (filters, table, detail drawer, whole
request by trace id, auto-refresh), Log levels (per category, temporary overrides, reset), Overview (health, latency,
errors, cache with purge).

**Cleanup.** Delete `AsanRezerve.Infrastructure.Monitoring` (dead, misleading), the dead Seq/App Insights/Sentry
config block with its committed Seq API key, and the old `RequestLoggingMiddleware`.

## Impact

- Affected specs (new): `system-logging`, `runtime-log-levels`, `log-explorer`, `read-caching`.
- Affected code: `src/Host/AsanRezerve.Host/Program.cs` + appsettings; new
  `src/Infrastructure/AsanRezerve.Infrastructure.Observability`; `AsanRezerve.Infrastructure.Core/Caching`;
  `AsanRezerve.Core.Application` behaviors + `IQuery`; `AsanRezerve.API` middleware; ServiceCatalog read repositories,
  invalidation handlers, cacheable queries, `LocationsController`; UserManagement query cache flags; SMS sandbox log.
- New schema `observability` (additive migration: `log_events`, `log_level_overrides`).
- New dependencies: `Microsoft.Extensions.Caching.Hybrid` 9.10.0, `Serilog.Sinks.Async`; the admin panel and MCP
  server add none beyond the MCP SDK in `tools/observability-mcp`.
- Admin panel: `asan-rezerve-admin` Logs page. MCP: `tools/observability-mcp`.
- Operations: Redis key prefix moves from `RateLimit_` to `asanrezerve:` (rate-limit windows restart once on
  deploy); Seq becomes opt-in (`SEQ_SERVER_URL`); log volume drops sharply (no SQL at Information).
- Out of scope (recorded as follow-ups): Prometheus `/metrics` and OpenTelemetry tracing export, an in-panel
  "Analyze with AI" button (declined 2026-09-25), per-endpoint HTTP output caching.
