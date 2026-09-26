# Observability and caching

How the host logs, where the logs go, how an admin (or an AI assistant) reads them and changes log levels, and how
reads are cached. Built by `openspec/changes/add-observability-and-caching` (2026-09-25/26); the design decisions
and their reasons are in that change's `design.md`. Verified against the code on the date above — when this file
and the code disagree, the code is right.

## At a glance

```
 request ──► RequestTelemetryMiddleware (X-Trace-Id, one event per request)
             │
 ILogger<T> ─┴─► Microsoft.Extensions.Logging level gate  ◄── Logging:LogLevel (appsettings)
                   │                                      ◄── runtime overrides (admin panel / API / MCP)
                   ▼
                 Serilog: mask secrets & personal data ──► console (async)
                                                       ──► rolling CLEF file (async, 50 MB × 7)
                                                       ──► Seq (opt-in: Seq:ServerUrl)
                                                       ──► log store queue ──► observability.log_events (14 days)
                                                                                   │
            admin panel › Logs ◄── /api/v1/admin/observability/* ◄─────────────────┘
            AI assistant (MCP) ◄── tools/observability-mcp ◄──────┘

 query ──► MediatR … Authorization … CachingBehavior ──► HybridCache: L1 memory ──► L2 Redis (circuit breaker)
 SaveChanges ──► ReadModelCacheInvalidationInterceptor ──► evict tags now + when the scope ends (after commit)
```

## Logging

**What is logged is `Logging:LogLevel`** (appsettings, environment variables, plus runtime overrides). Serilog is an
ordinary logging provider behind that gate, so a disabled Debug call costs a dictionary lookup, not a formatted
message. Production defaults: `Default` and `AsanRezerve` at Information; `Microsoft`, `Microsoft.AspNetCore`,
`Microsoft.EntityFrameworkCore`, `System`, `System.Net.Http`, `DotNetCore.CAP`, `Npgsql` at Warning;
`Microsoft.Hosting.Lifetime` at Information. (Before this change Serilog ignored `Logging:LogLevel` and production
logged every SQL command at Information, synchronously.)

**Where it goes** (`Observability:Logging`): console and a rolling compact-JSON file (`logs/asanrezerve-host-*.clef`,
one event per line, 50 MB per file, 7 files), both written on a background thread; Seq when `Seq:ServerUrl` (or
`Observability:Logging:Seq:ServerUrl`) is set — empty by default, set `SEQ_SERVER_URL=http://seq:5341` in the
production `.env` when running the `observability` compose profile; and the database log store (below).

**Masking** (`SensitiveDataMaskingEnricher`, before any sink): properties named like a secret — `Password`,
`*Password`, `Code`, `Otp*`, `VerificationCode`, `TwoFactorCode`, `*Token`, `*Secret`, `*ApiKey`, `Authorization`,
`CardNumber`, `Cvv`, `Pin` (and plurals) — become `***` at any depth of a destructured object. Phone numbers keep
their first four and last two characters (`0912*****67`), e-mails their first character and domain
(`a***@example.com`), matched by property name *and* by value whatever the property is called. Exception text and
literal message text are scrubbed of phone numbers and e-mails when stored. **Never interpolate a secret into a
message string** — log it as a property (or not at all); a string cannot be masked by name.

**Per request**: one completion event, `HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs} ms`,
with `RouteTemplate`, `UserId`, `ClientIp` — Error for 5xx, Warning when slower than
`Observability:Logging:SlowRequestThresholdMs` (1000), Information otherwise, Debug for `/health*`. Every event of the
request carries the W3C trace id the response returns in `X-Trace-Id` and in `metadata.traceId`.

**Exceptions are logged once**: `ExceptionHandlingMiddleware` logs a 5xx as Error with the stack trace and a 4xx as
Information without one. `LoggingBehavior` logs MediatR requests at Debug (destructured, so masked); outside an HTTP
request (jobs, CAP subscribers) it logs a failure as Error, or an expected rejection as Warning.

**Adding logs**: use `ILogger<T>` with message templates (`"Booking {BookingId} confirmed"`), never string
interpolation; destructure objects with `{@Name}` only at Debug; keep Information for business events an operator
would want to see.

## Runtime log levels

Admin panel › Logs › **Log levels**, `PUT /api/v1/admin/observability/log-levels`, or the MCP tool `set_log_level`.
Any category (`Default`, a namespace such as `AsanRezerve.ServiceCatalog`, or a full type name) can be set to any level
for 15 minutes … 1 day (or until reset). The change applies immediately to every logger, survives a restart
(`observability.log_level_overrides`), reverts by itself when its time is up (checked every 30 s), and is audited as a
Warning `Log level for {Category} changed from {PreviousLevel} to {NewLevel} by {Actor}`.

Practice: raise **one** namespace to Debug **for 15–30 minutes** while reproducing a problem; never `Default` to Debug
in production. The sandbox SMS body (with the OTP) is only logged at Debug under
`AsanRezerve.Infrastructure.External.Notifications.Sms` (Development raises it by default).

## The log store

`observability.log_events` (own EF context and migration, applied at startup; a failed migration never stops the
API). Events at or above `Observability:LogStore:MinimumLevel` (Information) are queued (10 000) and written in
batches (500 or every 2 s) with binary COPY by a background writer; a full queue drops and counts, a failed batch is
retried once then dropped and counted — a slow or down database never slows a request. The Overview tab shows
written/dropped/failed counts, the last error, the store's size on disk and the days it holds.

**Partitioned by day.** `log_events` has one partition per UTC day (`log_events_p20260926`); yesterday, today and
the next two days always exist (created at startup and hourly). Retention (`RetentionDays`, 14, decided 2026-09-25)
drops a whole day once all of it is older than that — instant, and no dead rows for autovacuum to chase, unlike
`DELETE`. An event whose day has no partition yet (the hourly job failed for days, or a clock far off) waits in
`log_events_default` and moves into its day when the day is created; parked events past the retention are deleted.
Indexed by time (the key), level+time, trace id, source+time and id.

**Backups leave the rows out.** Stored logs are diagnostics and would make up most of a dump, so
`pg_dump --exclude-table-data='observability.log_events*'` (the backup script and runbook commands) keeps the tables
and the log-level overrides but not the events. The raw volume archive still has them.

**When to store less.** The size on the Overview is the number to watch. Every request writes one event, so at
roughly 1 KB per row 20 000 requests a day is ~350 MB over 14 days and 200 000 a day ~3.5 GB. If it grows too big
for the box: shorten `RetentionDays`, or raise `MinimumLevel` to `Warning` (the overview's request counts and p95 are
then empty, because they are computed from the stored request events — which is also why sampling successful
requests is not built: it would falsify them), or move the store to its own database or a log server (Loki).

| Setting (`Observability:LogStore:*`) | Default |
|---|---|
| `Enabled` | `true` |
| `MinimumLevel` | `Information` |
| `RetentionDays` | `14` |
| `QueueCapacity` / `BatchSize` / `FlushIntervalMs` | `10000` / `500` / `2000` |

## Admin panel

Logs (sidebar) has three tabs:

- **Events**: time range, minimum level, text, source, trace id, status; newest first; a row opens the full event
  (template, exception, properties); **Whole request** shows every event with the same trace id; auto-refresh; NDJSON
  export.
- **Log levels**: configured vs current level per category, overrides with expiry and author, add any category.
- **Overview**: last-hour requests/5xx/p95, a 24-hour event chart, slowest routes, most frequent errors, cache hit
  ratios with L1/L2 state and purge, log-store health, process health, application counters (bookings created,
  confirmed, cancelled…), and the **AI digest** to copy.

Access is `AdminOnly` (decided 2026-09-25).

## AI analysis

- **Digest**: `GET /api/v1/admin/observability/digest?format=markdown[&source=…]` (or the Overview tab's *Build
  digest*): level counts, warnings/errors grouped by source + message template + exception type with counts, first/last
  seen and sample trace ids, slowest routes with p50/p95, cache hit ratios, log-store health — compact Markdown for a
  model to read as-is.
- **MCP server** (`tools/observability-mcp`, see its README): gives Claude Code / Claude Desktop the tools
  `get_digest`, `search_logs`, `get_log_event`, `get_trace`, `get_overview`, `list_log_levels`, `get_cache_stats`
  (and, only with `ASANREZERVE_MCP_ALLOW_WRITES=true`, `set_log_level`, `reset_log_level`, `invalidate_cache`), using
  an admin token. The server itself sends logs nowhere (decided 2026-09-25); they leave only when an admin's assistant
  asks. A good first prompt: *"Use get_digest for the last 6 hours, explain the top errors and open a sample trace for
  each."*
- **Export**: NDJSON (`/logs/export`) for offline analysis.

## Caching

**One cache: .NET `HybridCache`** — in-process L1 (60 s by default, 1 h for locations) over Redis L2, one factory run per key under concurrent
misses (stampede protection), tag invalidation, absolute expiration. L2 is `ResilientDistributedCache`: after 3
consecutive Redis failures Redis is skipped for 30 s (reads miss, writes are dropped) and one trial call is let
through — a Redis outage costs cache misses, never a timeout per request. Redis comes from `ConnectionStrings:Redis`
(`Cache:RedisConnectionString` only to use a different Redis), over one lazily-opened connection shared with rate
limiting and OTP state; keys are prefixed `asanrezerve:`. The application's own `IDistributedCache` (OTP, rate
limits) is deliberately **not** behind the breaker: those must fail closed.

**What is cached** (read models only — never domain aggregates):

| Read | Lifetime | Evicted by |
|---|---|---|
| Salon page `GET providers/{id}` (`GetProviderByIdQuery`) | 5 min | `provider:{id}` |
| Salon lists `GET providers/search` without user coordinates | 60 s | `provider-directory` |
| Categories with counts | 10 min | `categories` |
| Provinces, cities, hierarchy (`LocationsController`) | 12 h | `locations` |
| Availability summary (`GetProviderAvailabilitySummary`, `IMemoryCache`) | 2 min | lifetime |

Not cached on purpose: availability calendar and slots, bookings, anything per user.

**Invalidation** hangs off the save pipeline: `ReadModelCacheInvalidationInterceptor` traces every added, modified or
deleted row — the provider and its owned parts (profile, gallery, address, policy), its services, staff memberships,
opening hours, holidays, exceptions — to its salon and evicts `provider:{id}`, `provider-directory` and `categories`
after the save, and again when the request/job scope ends (after the transaction committed). A change committed
straight through a `DbContext` is covered too; bulk `ExecuteUpdate/Delete` is not (entries then expire on their
lifetime, or purge from the admin page).

**Adding a cached query**: implement on the query record, with exactly these types —
`bool IsCacheable`, `int? CacheExpirationSeconds`, `IReadOnlyCollection<string>? CacheTags` (and optionally
`string? CacheKey`; leave it null so every parameter is part of the key). The result must be the same for every
caller allowed to run the query, and something must evict its tags on every change. Then add it to the allowlist in
`tests/AsanRezerve.ArchitectureTests/QueryCachePolicyTests.cs` — that test fails otherwise — and make sure its result
type round-trips through System.Text.Json (no non-public setters without `[JsonInclude]`; see
`ReadModelCachingTests`). Anything outside the query that the result depends on (the public base URL photo links are
built from) belongs in an `ICacheKeyContributor`.

Settings (`Cache:*`): `Provider` (`Redis`|`InMemory`), `KeyPrefix`, `DefaultExpirationMinutes` (5),
`LocalExpirationSeconds` (60), `MaximumPayloadBytes` (1 MiB), `CircuitBreakerFailureThreshold` (3),
`CircuitBreakerResetSeconds` (30).

## Runbooks

**A user reports an error.** Ask for the time (and, from the app or browser devtools, the `X-Trace-Id` header or the
error body's `metadata.traceId`). Logs › Events › paste the trace id → *Whole request*. Without a trace id: filter the
time range, minimum level Warning, text or path.

**Something is slow.** Overview › slowest routes (p95) and the last-hour p95. Then raise the route's namespace to
Debug for 15 minutes, reproduce, and read the request's events.

**Cache looks stale.** Overview › Cache: purge `provider:{id}` (or `provider-directory`); if the L2 circuit is Open,
Redis is unreachable (the API keeps working from L1 and the database).

**Logs page is empty.** Overview › Log store: *Not ready* means the `observability` migration failed at startup
(check the container log); growing *dropped* means the database cannot keep up or is down.

**Deploy notes.** Redis keys moved from `RateLimit_*` to `asanrezerve:*` (rate-limit windows restart once; old keys
expire). Seq is off unless `SEQ_SERVER_URL` is set. The `observability` schema is created by the first start.

## Not built (follow-ups)

Prometheus `/metrics` and OpenTelemetry trace export (the monitoring compose scrapes a `/metrics` the host does not
serve); cross-node cache/level broadcast (single node today); an in-panel "Analyze with AI" button (declined
2026-09-25); a long-lived, read-only API key for the MCP server instead of a 60-minute admin JWT.
