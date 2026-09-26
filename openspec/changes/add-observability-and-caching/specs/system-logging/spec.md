## ADDED Requirements

### Requirement: Configured log levels are enforced per category
The host SHALL apply `Logging:LogLevel` (default and per-category) to every log event before it reaches any sink, and
production configuration SHALL keep framework chatter (ASP.NET Core, EF Core, HttpClient, CAP, Npgsql) at Warning.

#### Scenario: EF Core SQL is not logged at Information in production
- **WHEN** the host runs with production configuration and EF Core executes a command
- **THEN** no Information event from `Microsoft.EntityFrameworkCore.Database.Command` is written

#### Scenario: Application categories log at their configured level
- **WHEN** `AsanRezerve.ServiceCatalog` is configured at Information
- **THEN** its Information events are written and its Debug events are not

### Requirement: Secrets and personal data are masked before any sink
Every log event SHALL pass through one masking step that redacts secrets (passwords, OTP and verification codes,
tokens, secrets, API keys, authorization values, card data) to `***` and partially masks phone numbers and e-mail
addresses, at any nesting depth of destructured objects, before console, file, Seq or the log store receive it.

#### Scenario: A login command is logged without its password
- **WHEN** a command with `Email`, `Password` and `TwoFactorCode` is logged
- **THEN** every sink shows `Password` and `TwoFactorCode` as `***` and the e-mail as `a***@domain`

#### Scenario: A phone number is masked whatever its property is called
- **WHEN** an event carries `09121234567` in any string property
- **THEN** the stored value is `0912*****67`

#### Scenario: Non-secret codes stay readable
- **WHEN** an event carries `StatusCode`, `PostalCode` or `PromotionCode`
- **THEN** their values are unchanged

### Requirement: Each HTTP request is logged once with a trace id
The host SHALL write exactly one completion event per HTTP request with method, path, route template, status code,
elapsed milliseconds, user id and trace id, and SHALL return the same W3C trace id in an `X-Trace-Id` response header
and in the `metadata.traceId` of success and error envelopes.

#### Scenario: A successful request
- **WHEN** a client calls an API endpoint that returns 200
- **THEN** one Information completion event is written and the response has `X-Trace-Id` equal to that event's trace id

#### Scenario: A failing request is correlated
- **WHEN** a request fails with 500
- **THEN** the error envelope's `metadata.traceId` equals the `X-Trace-Id` header and every event of that request

#### Scenario: A slow request is flagged
- **WHEN** a request takes longer than the slow-request threshold
- **THEN** its completion event is written at Warning

### Requirement: An exception is logged once at a level matching its outcome
An exception that ends an HTTP request SHALL produce one exception log event: Error with the stack trace for 5xx,
Information without a stack trace for 4xx.

#### Scenario: A server error
- **WHEN** a handler throws an unexpected exception during a request
- **THEN** exactly one Error event carries the stack trace

#### Scenario: A validation failure
- **WHEN** a command fails validation (400)
- **THEN** no Error event is written for it
