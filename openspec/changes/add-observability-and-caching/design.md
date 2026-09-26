## Context

Single ASP.NET Core host on a shared 2 vCPU / 3.8 GB VPS (API limited to 768 MB), one PostgreSQL, one Redis, Seq
off by default for RAM. Findings are in `proposal.md`. Constraints: nothing observability-related may slow or fail a
request; no extra always-on container; everything must be testable without a network (FAST) and against real
Postgres (FULL).

## Decisions

### D1 — Microsoft.Extensions.Logging is the level gate; Serilog is the pipeline
`UseSerilog` replaces `ILoggerFactory` with `SerilogLoggerFactory`, which ignores `Logging:LogLevel`. Instead Serilog
is registered as an ordinary `ILoggerProvider` (`SerilogLoggerProvider`, built from DI so sinks can use services), and
MEL's `LoggerFilterOptions` decide what is enabled per category. Serilog's own minimum is Verbose; it only enriches,
masks and fans out to sinks. Consequences: the existing `Logging:LogLevel` config becomes live; `IsEnabled` is exact
per category (a disabled Debug call costs one dictionary lookup, no formatting); runtime changes use MEL's supported
reload path. Rejected: Serilog `LoggingLevelSwitch` overrides — they must be declared at startup, so an admin could not
target an arbitrary class; a Serilog filter over a lowered global floor would format every Debug event everywhere
while one category is being debugged.

### D2 — Runtime overrides are a top-precedence configuration provider
`RuntimeLogLevelConfigurationProvider` is added last to the host configuration and holds keys
`Logging:LogLevel:<Category>`. Setting or clearing a key calls `OnReload()`; `LoggerFactory` re-applies filter rules to
every existing logger. Overrides are persisted in `observability.log_level_overrides` (category, level, expires_at,
updated_by, updated_at), re-applied at startup, and expired by a 30-second hosted sweep. Every change writes a Warning
audit event (`LogLevelChanged` with actor, category, from, to, expiry). Category `Default` maps to
`Logging:LogLevel:Default`. Categories are validated (`[A-Za-z0-9_.]`, ≤ 200 chars); levels are MEL `LogLevel` names.

### D3 — Mask in one enricher, before any sink
`SensitiveDataMaskingEnricher` walks every property (including destructured objects, sequences and dictionaries):
- redacted to `***`: names `password`/`*password`, `code`, `otp*`, `*otpcode`, `verificationcode`, `twofactorcode`,
  `*token`, `*secret`, `*apikey`, `authorization`, `cardnumber`, `cvv`, `pin`, `pincode`;
- partially masked: phone-like names (`phone*`, `*phone`, `mobile*`, `recipient`, `to`) keep the first 4 and last 2
  digits (`0912*****67`); e-mail names keep the first character and the domain (`a***@example.com`);
- value-based: any string that is an Iranian mobile number is masked whatever its property is called;
- already-masked values (containing `*`) are left alone. `StatusCode`, `PostalCode`, `PromotionCode` etc. are not
  secrets and stay intact (exact-name rule for `code`).
Call sites keep their templates; `LoggingBehavior` and `PerformanceBehavior` destructure (`{@Request}`) instead of
pre-serialising to a string, so the enricher can see inside. The Rahyab sandbox stops logging the SMS body.

### D4 — One request event, outermost, W3C trace id
`RequestTelemetryMiddleware` runs first in the pipeline (before compression, envelope and exception handling) so it
measures the whole request and sees the final status. It writes one event:
`HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs} ms` with `RouteTemplate`, `UserId`,
`ClientIp` (first `X-Forwarded-For` hop, else the socket). Level: 5xx Error, over `SlowRequestThresholdMs` (1000)
Warning, otherwise Information; `/health*` at Debug. It sets `X-Trace-Id` (the request `Activity`'s W3C trace id,
which Serilog also stamps on every event of the request). Envelopes carry `traceId` in `metadata`.

### D5 — Log an exception once
`ExceptionHandlingMiddleware` is the only place an HTTP request's exception is logged: 5xx at Error with the
exception, 4xx at Information without a stack trace. `LoggingBehavior` logs a failure at Error only when there is no
HTTP request (background jobs, CAP subscribers) and at Debug otherwise; `TransactionBehavior` logs a rollback at
Warning without the exception; the duplicate 500 log is removed.

### D6 — Log store: our own bounded writer, EF for schema and reads
`LogStoreSink` (Serilog `ILogEventSink`, restricted to `Observability:LogStore:MinimumLevel`, default Information)
enqueues into a bounded `Channel` (10 000; full → drop and count). `LogStoreWriter` (hosted) drains batches (≤ 500 or
2 s) and writes them with Npgsql binary `COPY` (no EF, no logging on its own path → no feedback loop). Failures are
counted and retried once, then the batch is dropped; the app is never affected. `FlushAsync` exists for tests and
shutdown. Table `observability.log_events`: id, timestamp, level, message (rendered, masked), message_template,
exception, source_context, trace_id, span_id, request_path, status_code, elapsed_ms, user_id, properties (jsonb).
Indexes: (level, timestamp), trace_id, (source_context, timestamp), id. `ObservabilityDbContext` (schema
`observability`) owns the migration and the admin queries.

*Revised 2026-09-26 (log store at scale, slice 12):* the table shares the application's database, so a `DELETE`-based
retention would leave the busiest table full of dead rows and every dump full of logs. `log_events` is **partitioned
by UTC day** (key `(timestamp, id)`; the migration writes the table by hand because EF cannot declare partitioning).
`LogPartitions` keeps yesterday … today+2 created (startup, then hourly) and drops a day once all of it is older than
`RetentionDays` (14, decision 2026-09-25). A default partition catches an event with no day partition; creating the day
moves its parked events into it in the same transaction (Postgres refuses to create a partition whose rows sit in the
default one). All partition DDL runs under one advisory lock. Backups use
`--exclude-table-data='observability.log_events*'`. The overview reports the store's size and day range. Sampling
successful request events was considered and rejected for now: the overview's request counts and p95 are computed
from those events, so sampling would falsify them unless every statistic were weighted. The table is not in the integration tests'
`DatabaseReset` (tests use unique markers; truncating a table a background writer is filling buys nothing).

### D7 — HybridCache with a resilient, keyed L2
`AddAsanRezerveCaching` registers: one lazily-connected `IConnectionMultiplexer` from `Cache:RedisConnectionString`
if set, else `ConnectionStrings:Redis` (abortConnect=false), shared by `IDistributedCache` (rate limiting, OTP state,
instance prefix `asanrezerve:`); `IDistributedCache` = memory when `Cache:Provider=InMemory` or no Redis is
configured; `HybridCache` with `DistributedCacheServiceKey` pointing at a keyed `ResilientDistributedCache` — a
circuit-breaker decorator (threshold 3, cooldown 30 s, half-open trial) over whatever unkeyed `IDistributedCache` DI
holds (so a test host that swaps it is honoured). The shared `IDistributedCache` itself is **not** wrapped: OTP and
rate-limit state must keep failing closed. Defaults: expiration 5 min, L1 1 min, payload ≤ 1 MiB.

### D8 — Cache read models through the query pipeline
`IQuery` keeps `IsCacheable`/`CacheKey`/`CacheExpirationSeconds` and gains `CacheTags`. `CachingBehavior` (innermost,
after authorization) uses `HybridCache.GetOrCreateAsync` with absolute expiration, key
`q:{QueryType}:{CacheKey ?? SHA-256(JSON of the whole query)}`, and the query's tags. A null result is returned but
not cached. A handler exception propagates and is not cached. A cache failure (serialisation, L1) is logged and the
handler runs uncached. Hits/misses are counted per query type (`CacheMetrics`, meter `AsanRezerve.Caching`).
`ICacheInvalidator.InvalidateAsync(tags)` (`HybridCacheInvalidator`, scoped) evicts now and again when the DI scope
ends (`IAsyncDisposable` + `IDisposable`), i.e. after the unit of work committed — domain events are dispatched before
`SaveChanges`, so an immediate-only eviction can be undone by a concurrent read of the uncommitted-old row.

*Revised during implementation (tier 1):* invalidation hangs off an **EF SaveChanges interceptor**
(`ReadModelCacheInvalidationInterceptor`), not domain events — many mutators raise none (service price/duration, a
member leaving, a gallery caption) and fixture commits raise none either. It traces every added/modified/deleted row
(owned types walked to their root, rows with a foreign key to `Provider`) to its salon. HybridCache runs the factory
on a pool thread without the caller's `ExecutionContext`; the behavior runs the handler under the captured context
(IHttpContextAccessor, trace ids). `ICacheKeyContributor` adds out-of-query inputs to every key (the public base URL
photo links are built from).

Cached (tags → invalidated by):
| Read | Lifetime | Tags |
|---|---|---|
| `GetProviderByIdQuery` (salon page) | 5 min | `provider:{id}` ← any saved change to the salon, its services, staff, hours |
| `SearchProvidersQuery` without coordinates | 60 s | `provider-directory` ← any provider change |
| `GetCategoriesWithCountsQuery` | 10 min | `categories` ← any provider change |
| `LocationsController` reads | 12 h | `locations` (reference data) |
Not cached any more: availability calendar, customer by id, favourites, user by id, admin user search.

### D9 — Envelope without re-parsing; Unicode-friendly JSON
`ApiResponseMiddleware` writes the envelope with `Utf8JsonWriter` and embeds the controller's JSON bytes via
`WriteRawValue` (validated; non-JSON bodies become a string, as before). MVC and the envelope use
`JavaScriptEncoder.Create(UnicodeRanges.All)`: Persian is written as UTF-8, HTML-sensitive characters are still
escaped. The JSON is semantically identical; only escaping changes.

### D10 — Admin API
`api/v1/admin/observability` (AdminOnly): `GET logs`, `GET logs/{id}`, `GET logs/trace/{traceId}`,
`GET logs/export` (NDJSON, ≤ 50 000 rows, excluded from the envelope), `GET digest?format=json|markdown&source=`,
`GET overview`, `GET log-levels`,
`PUT log-levels`, `DELETE log-levels/{category}`, `GET cache`, `POST cache/invalidate`. Lists are
`{ items, totalCount, page, pageSize }` like `admin/promotions`. Default window: last 24 h; maximum window 14 days.

### D11 — MCP server outside the host
`tools/observability-mcp` is a small Node package (`@modelcontextprotocol/sdk`, stdio) calling the admin API with
`ASANREZERVE_API_URL` + `ASANREZERVE_ADMIN_TOKEN`. Read tools always; `set_log_level`, `reset_log_level` and
`invalidate_cache` only with `ASANREZERVE_MCP_ALLOW_WRITES=true`. Keeping it out of the host adds no preview package to
production and means logs leave the server only when an admin's own AI client asks for them.

## Risks / Trade-offs
- Removing the aggregate cache: in production it never worked (localhost), so no latency regression; in tests it was
  active, and fixtures that evicted `Provider:{id}` now evict the `provider:{id}` tag.
- Query cache in the shared integration host: `ResetStateAsync` already clears `IMemoryCache` (HybridCache L1) and the
  resettable `IDistributedCache` (L2), so each test starts cold.
- Masking is best effort for free text: a secret interpolated into a message string (not a property) is not caught;
  call sites found by the review are fixed.
- Level overrides and cache are per process — correct for the single-node deployment; multi-node would need a
  broadcast (noted, not built).

## Migration
Additive: new schema and two tables (EF migration `InitialObservabilityStore`, applied at startup like the others).
Rollback: redeploy the previous image; the tables are ignored. Redis: new key prefix; old `RateLimit_*` keys expire.
