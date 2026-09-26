## ADDED Requirements

### Requirement: Log events are stored without affecting requests
Events at or above the store's minimum level (default Information) SHALL be written to the database log store through
a bounded asynchronous queue; a slow, failing or unavailable database SHALL never block, slow or fail a request, and
dropped events SHALL be counted.

#### Scenario: Database unavailable
- **WHEN** the log store cannot write
- **THEN** requests complete normally, console and file still receive events, and the failure is counted

#### Scenario: Burst beyond capacity
- **WHEN** more events arrive than the queue holds
- **THEN** the excess is dropped and counted, and logging calls return immediately

### Requirement: Stored logs are kept for 14 days
The store SHALL remove events older than the configured retention (14 days) by whole UTC days, and SHALL NOT remove an
event younger than the retention. Events SHALL be stored in one partition per UTC day, so that removing a day leaves
no dead rows; an event whose day has no partition yet SHALL still be stored and SHALL move into its day when the day
is created. Database backups SHALL keep the log store's tables but not its events.

#### Scenario: Old days are removed
- **WHEN** the retention job runs
- **THEN** every day that ended more than 14 days ago is removed with its events, and newer events remain

#### Scenario: An event arrives for a day without a partition
- **WHEN** an event is stored for a day that has no partition, and later that day's partition is created
- **THEN** the event is stored meanwhile and afterwards is found in its day's partition

#### Scenario: The store's size is visible
- **WHEN** an admin opens the system overview
- **THEN** it shows the log store's size on disk and the range of days it holds

### Requirement: Admins search and inspect logs
Administrators (AdminOnly) SHALL be able to search stored events by time window, minimum level, text, source,
trace id, request path and status code (newest first, paginated), open one event with its properties and exception,
and list every event of one trace in order.

#### Scenario: Filtered search
- **WHEN** an admin searches the last hour for Warning and above containing "booking"
- **THEN** only matching events are returned, newest first, with a total count

#### Scenario: Whole request by trace id
- **WHEN** an admin opens a trace id
- **THEN** every stored event of that request is returned in timestamp order

#### Scenario: Unauthenticated or non-admin caller
- **WHEN** the logs API is called without a token or by a non-admin
- **THEN** the response is 401 or 403 respectively

### Requirement: System overview and AI digest
The host SHALL provide admins an overview (request rate, error rate, latency p50/p95 by route, level counts, cache
statistics, log-store health, process health) and an AI digest of a time window (error groups by fingerprint with
counts, first/last seen and sample trace ids; slowest routes; level counts; cache hit ratios) as JSON or Markdown, and
an NDJSON export capped at 50 000 events.

#### Scenario: Digest groups repeated errors
- **WHEN** the same exception is logged 20 times in the window
- **THEN** the digest shows one group with count 20, first and last seen, and sample trace ids

### Requirement: AI assistants can query observability through MCP
The repository SHALL ship an MCP server that exposes log search, event, trace, digest, overview, log-level and cache
tools backed by the admin API and authenticated with an admin token; mutating tools SHALL be disabled unless
explicitly enabled.

#### Scenario: Read-only by default
- **WHEN** the MCP server runs without the write flag
- **THEN** it lists no tool that changes log levels or purges the cache
