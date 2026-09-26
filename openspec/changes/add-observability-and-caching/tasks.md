Status: DONE
Verify: FULL

Change: add-observability-and-caching. User request 2026-09-25 (architect review of logging + caching for lowest
latency, AI-analysable monitoring, logs and log levels in the admin panel). Decisions taken by the user the same day
are under `## Decisions`. Test-first: within each slice the test task precedes the implementation task.
`scripts/verify.sh fast` after each task (DOTNET_ROLL_FORWARD=Major in this container: only the .NET 10 runtime is
installable here), `full` to finish.

## Acceptance scenarios

- Given production config, when EF Core runs a command, then no Information SQL event is written.
- Given a command with Password/Code/RefreshToken/phone, when logged anywhere, then sinks see `***` / `0912*****67`.
- Given any API request, then one completion event is written and `X-Trace-Id` equals its trace id and the envelope's.
- Given a 500, then exactly one Error event carries the stack trace; given a 400, then no Error event.
- Given an admin sets `AsanRezerve.ServiceCatalog` to Debug for 30 min, then its Debug is written, others unchanged,
  and it reverts after 30 min; the change is audited; it survives a restart; a non-admin gets 403.
- Given logs are written, when an admin searches level ≥ Warning + text, then matches return newest first, paginated;
  a trace id returns the whole request in order; events older than 14 days are deleted.
- Given the log database is down or the queue is full, then requests are unaffected and drops are counted.
- Given 20 identical errors, then the AI digest shows one group with count 20 and sample trace ids.
- Given the MCP server without the write flag, then it exposes read tools only.
- Given a cached salon page, when the owner edits the profile or adds a service, then the next read shows it.
- Given a booked slot, then the availability calendar never serves it from cache; favourites/profile reads are fresh.
- Given two queries differing in one filter, then they never share a cache entry.
- Given 50 concurrent misses on one key, then the handler runs once; given Redis down, then no per-request timeout.
- Given production compose (only `ConnectionStrings__Redis`), then the cache uses that Redis, not localhost.
- Unchanged: API response shapes (envelope fields, data), OTP and rate-limit fail-closed behaviour, auth rules.

## Tasks

## 1. Caching core
- [x] 1.1 Unit tests: ResilientDistributedCache circuit breaker (port the 7 RedisCacheService breaker scenarios).
- [x] 1.2 ResilientDistributedCache (IDistributedCache + IBufferDistributedCache, TimeProvider, observable state).
- [x] 1.3 Unit tests: Redis connection resolution (explicit, ConnectionStrings:Redis, none → memory) + DI shape.
- [x] 1.4 AddAsanRezerveCaching: shared multiplexer, IDistributedCache, keyed resilient L2, HybridCache, metrics.
- [x] 1.5 Unit tests: ICacheInvalidator evicts tags now and again at scope end; InvalidateAll; CacheMetrics snapshot.
- [x] 1.6 HybridCacheInvalidator + CacheInvalidationScope + CacheMetrics; Program.cs drops its own Redis cache setup.

## 2. Query cache pipeline
- [x] 2.1 Unit tests: CachingBehavior (passthrough, hit, stampede once, null/exception not cached, tags, full key, fallback).
- [x] 2.2 IQuery.CacheTags; CachingBehavior on HybridCache (absolute expiry, q:{type}:{key|hash}, metrics).
- [x] 2.3 Unit tests: calendar/customer/favourites/user/admin-search queries are not cacheable (stale-data regressions).
- [x] 2.4 Remove IsCacheable from those five queries.

## 3. ServiceCatalog read caching
- [x] 3.1 Remove aggregate cache decorators, ICacheService + impls (+ their breaker tests, moved to 1.1); fixtures evict tags.
- [x] 3.2 Unit tests: salon page, lists, categories opt in with their tags/lifetimes; results round-trip the cache serializer.
- [x] 3.3 Cache GetProviderById (5 min), non-geo SearchProviders + featured (60 s), categories (10 min), locations (12 h).
- [x] 3.4 Integration tests: salon page fresh after profile/service/hours/staff edits and direct commits; repeat read is a hit.
- [x] 3.5 Save-pipeline invalidation (EF interceptor) replaces ProviderCacheInvalidationEventHandler: every provider change.

## 4. Logging pipeline
- [x] 4.1 Create AsanRezerve.Infrastructure.Observability + its unit test project; add both to sln and verify FAST.
- [x] 4.2 Unit tests: SensitiveDataMaskingEnricher (secrets, nested, phones by name and value, e-mail, keep non-secrets).
- [x] 4.3 SensitiveDataMaskingEnricher.
- [x] 4.4 Unit tests: MEL gate — appsettings.json levels applied (EF command Info off), Serilog provider receives events.
- [x] 4.5 Serilog as MEL provider: async console, compact rolling file with limits, opt-in Seq; appsettings Logging levels.
- [x] 4.6 Unit tests: RequestTelemetryMiddleware (one event, X-Trace-Id, level policy, health at Debug, 500 on throw).
- [x] 4.7 RequestTelemetryMiddleware outermost; delete old RequestLoggingMiddleware; traceId in both envelopes.
- [x] 4.8 Unit tests: log-once — ExceptionHandlingMiddleware 4xx Info/5xx Error; LoggingBehavior Debug + masked payload.
- [x] 4.9 LoggingBehavior/PerformanceBehavior/TransactionBehavior/ExceptionHandlingMiddleware log-once; Rahyab body.

## 5. Runtime log levels
- [x] 5.1 Unit tests: runtime overrides (set, child categories, reset, expiry, Default, validation, audit event).
- [x] 5.2 RuntimeLogLevelConfigurationProvider + LogLevelService + expiry sweep.

## 6. Log store
- [x] 6.1 ObservabilityDbContext + records + migration InitialObservabilityStore; migrate at startup.
- [x] 6.2 Unit tests: LogStoreWriter mapping (level, trace, promoted props, masked message) + bounded drop + flush.
- [x] 6.3 LogStoreSink + LogStoreWriter (Channel + binary COPY) + LogRetentionService; override persistence.
- [x] 6.4 Unit tests: AI digest Markdown/JSON builder (grouping, ordering, empty window).
- [x] 6.5 LogQueryService (search, get, trace, export, digest, overview) + SystemOverview (process, meters, cache).

## 7. Admin API
- [x] 7.1 Integration tests: observability endpoints — 401/403/200, search, trace, levels roundtrip, cache stats/purge.
- [x] 7.2 AdminObservabilityController + registration; X-Trace-Id + digest integration checks green.

## 8. Hot path
- [x] 8.1 Unit tests: ApiResponseMiddleware envelope identical shape, raw embed, non-JSON body as string, traceId.
- [x] 8.2 ApiResponseMiddleware Utf8JsonWriter raw embed; Unicode-friendly JSON encoder for MVC and envelope.

## 9. Admin panel (asan-rezerve-admin)
- [x] 9.1 observability.api.ts + types + unit tests (URLs, params, payloads).
- [x] 9.2 Composables useLogExplorer / useLogLevels / useSystemOverview + unit tests.
- [x] 9.3 Logs page with Events / Log levels / Overview tabs; fa/en keys; type-check + unit tests green.

## 10. MCP server
- [x] 10.1 tools/observability-mcp: tools over the admin API, read-only unless allowed; node:test unit tests.

## 11. Cleanup and docs
- [x] 11.1 Delete AsanRezerve.Infrastructure.Monitoring; drop dead Observability config + committed Seq key; Seq opt-in.
- [x] 11.2 docs/OBSERVABILITY.md; API_ENDPOINTS.md; project.md Logging/Caching facts; FOLLOW-UPS #70 closed + new.
- [x] 11.3 FULL verify green; tasks.md reflects reality; Status DONE.

## 12. Log store at scale (2026-09-26, after review of storing logs in the application database)
- [x] 12.1 Unit tests: partition plan — daily UTC partitions today-1..today+2 created, whole days older than the
  retention dropped, foreign/malformed names ignored, day boundaries in UTC.
- [x] 12.2 LogPartitionPlan (pure) for 12.1.
- [x] 12.3 Integration tests: log_events is partitioned; ensure is idempotent; rows parked in the default partition
  move into a new day's partition; retention drops old partitions and old default rows, keeps recent; an event is
  still found by id and trace id; storage size is reported on the overview.
- [x] 12.4 Migration creates log_events partitioned by day (+ default partition, sequence id, PK (timestamp, id));
  LogPartitions (ensure / retention by DROP / storage size) at startup and hourly; overview storage block; admin
  Overview shows it.
- [x] 12.5 Backups keep the log store's schema but not its rows (server-setup.sh, runbook); docs (OBSERVABILITY.md,
  design D6, log-explorer spec).
- [x] 12.6 FULL verify green; Status DONE.

## Decisions

- 2026-09-25 (user) Log retention in the database: 14 days.
- 2026-09-25 (user) AI analysis: MCP server + AI digest; no in-panel AI button, no logs sent out by the server.
- 2026-09-25 (user) Viewing logs and changing levels: AdminOnly (Admin, Administrator, SysAdmin), every change audited.
- Tier 1: MEL is the level gate, Serilog the pipeline (design D1); runtime levels via a reloadable config provider (D2).
- Tier 1: own bounded log-store writer with binary COPY instead of Serilog.Sinks.PostgreSQL (flush, drop counts, schema).
- Tier 2: remove aggregate caching (FOLLOW-UPS #70) and the stale query caches (calendar, customer, favourites, user,
  admin search) — correctness fixes.
- Tier 2: new dependencies Microsoft.Extensions.Caching.Hybrid 9.10.0, Serilog.Sinks.Async; MCP SDK in tools/ only.
- Tier 2: Redis key prefix `RateLimit_` → `asanrezerve:` (rate-limit windows restart once at deploy).
- Tier 2: JSON output stops escaping Persian (UnicodeRanges.All); semantically identical JSON.
- Tier 1: salon-page invalidation hangs off an EF SaveChanges interceptor (every tracked change), not domain events.
- Tier 1: cached handlers run under the caller's ExecutionContext; the public base URL is part of every query-cache key.
- Tier 2: delete dead AsanRezerve.Infrastructure.Monitoring and the committed Seq API key; Seq becomes opt-in.
- 2026-09-26 (user) "Fix it if you think it is better" on the log-store review: partition by day, keep log rows out
  of backups, show storage size. Tier 1: request-event sampling NOT built — the overview's request counts and p95
  are computed from the stored request events, so sampling would falsify them unless weighted; revisit with the
  storage size the overview now shows.
- Tier 1: the InitialObservabilityStore migration is edited in place (partitioned table) rather than followed by a
  second migration — it has never been applied outside test containers (branch unmerged, not deployed).

## Log

- 2026-09-26 CI, second round: with the host reachable the Playwright suite ran in CI for the first time; 5 failed.
  The new request/exception log lines named both causes: `send-verification-code` answered 429 from the sixth login
  (per-caller `phone-verification` policy, 5 per 5 min, every spec from one address) and the reschedule seed booked
  a hard-coded 2026-09-02 ("Cannot create a booking in the past"). Workflow sets `RateLimiting__Enabled=false` (as
  the integration host does); SEED_SLOTS are today+7 / today+8 at 10:00Z. Frontend type-check + lint clean.
  Third round: 4 passed, 3 failed. today+8 broke the platform's 7-day customer booking window
  (BookingHorizonPolicy; I had checked only the service's 90 days) → today+2 / today+3. The two registration specs
  fail on a pre-existing web bug: the wizard maps every category to a legacy ProviderType ("Salon") that
  ServiceCategoryResolver rejects (400 "Invalid category: Salon") — raised with the user, not fixed here.
- 2026-09-26 CI (user asked to carry it on this PR): `Playwright keystone (UI)` had failed on every run since it was
  added — `dotnet run` applied launchSettings.json (applicationUrl :5000) over the job's ASPNETCORE_URLS (:5050), so
  the health wait polled a closed port. Reproduced locally with the job's env; `--no-launch-profile` → healthy on
  :5050 in ~18 s. Workflow patched; the Playwright suite itself runs in CI for the first time with this push.
- 2026-09-26 Slice 12 (log store at scale): log_events partitioned by UTC day (hand-written in the regenerated
  InitialObservabilityStore migration; key (timestamp, id)); LogPartitions creates yesterday..today+2 at startup and
  hourly, moves events parked in the default partition into a new day in the same transaction, drops whole days past
  14 days; overview/admin show size and day range; dumps exclude the rows (checked on postgres:16: tables and
  overrides dumped, events not). Checked on postgres:16 first that identity columns work on a partitioned parent (my
  first draft assumed they did not). Sampling not built (tier-1 decision above). Unit 90 (+8 plan), integration
  901/901 (+4 partition tests, retention test moved), admin vitest 148/148. FULL verify PASS.
- 2026-09-26 Slices 10-11: MCP server `tools/observability-mcp` (7 read tools, 3 write tools behind
  ASANREZERVE_MCP_ALLOW_WRITES; node:test 10/10). Deleted the dead Infrastructure.Monitoring project, the dead
  Seq/App Insights/Sentry block and the committed Seq API key (still in git history — rotate it); Seq is opt-in via
  SEQ_SERVER_URL. Docs: docs/OBSERVABILITY.md, API_ENDPOINTS.md, project.md, FOLLOW-UPS (#70 closed, #72 added),
  runbook, knowledge map. FULL verify PASS 2026-09-26T00:43Z (14 steps: build, 10 unit/architecture projects,
  integration 898/898, admin type-check + vitest). The first FULL run reported BLOCKED only because the local
  `master` ref was 26 commits stale, so verify's touched() saw the frontend and Flutter apps as changed; after
  fast-forwarding the local ref to origin/master they are untouched and not run.
- 2026-09-26 Slices 8-9: envelope embeds raw JSON (Utf8JsonWriter) with a correct Content-Length; Persian as UTF-8
  in MVC and envelope; NDJSON export excluded from the envelope. The full suite caught an order-dependent digest
  test (other tests' errors outranked its group) → digest gained a `source` filter. Admin Logs page: Events / Log
  levels / Overview tabs; vitest 145/145, type-check clean, build OK. Integration 898/898.
- 2026-09-26 Slices 6-7: log store (observability schema, binary COPY writer, retention), query service, AI digest,
  overview, admin controller. Found and fixed: BoundedChannelFullMode.DropWrite reports success while discarding
  (drops were uncountable) → Wait mode with TryWrite; exception and message text now scrubbed of phones/e-mails
  before storage. Admin API integration tests 12/12; full integration 898/898; FAST green.
- 2026-09-25 Slices 4-5: logging pipeline, masking, request telemetry, log-once, runtime levels. The first
  integration run after slice 4 failed 871/886: the host could not start (MissingMethodException in the Seq sink —
  compiled against Serilog.Sinks.Seq 8.0.0, the host resolves 9.0.0 via UserManagement.API). Aligned to 9.0.0;
  integration 886/886, FAST green (11 projects).
- 2026-09-25 Slice 3: the round-trip test found `PagedResult<T>`'s JSON constructor could not bind (IEnumerable vs
  IReadOnlyList) — every cached salon list would have failed on each hit; fixed with a private [JsonConstructor].
  The full suite then caught HybridCache running the factory without the caller's ExecutionContext (no HttpContext:
  relative photo URLs, logs without trace id) — the behavior now runs the handler under the captured context, and
  ICacheKeyContributor varies keys by the public base URL. Integration 886/886 after the fix.
- 2026-09-25 Change opened. Toolchain: this container has no .NET 9 SDK/runtime reachable (dot.net blocked); .NET 10
  SDK from the Ubuntu archive builds the net9.0 solution (baseline build 0 errors) and runs tests with
  DOTNET_ROLL_FORWARD=Major. Docker daemon started locally for Testcontainers.
