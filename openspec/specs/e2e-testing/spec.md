# e2e-testing Specification

## Purpose
TBD - created by archiving change add-playwright-e2e. Update Purpose after archive.
## Requirements
### Requirement: Playwright E2E harness

The system SHALL provide a Playwright (TypeScript) end-to-end test harness in `booksy-frontend/` that drives the real Vue application in a Chromium browser against a running monolith stack.

#### Scenario: Suite is runnable locally
- **WHEN** a developer runs `npm run test:e2e` in `booksy-frontend/` with the backend stack up and the sandbox toggles set
- **THEN** Playwright launches Chromium, auto-starts the frontend dev server via the `webServer` config, executes the specs, and produces an HTML report

#### Scenario: Failure diagnostics are captured
- **WHEN** a test fails
- **THEN** Playwright records a trace and video for that test and includes them in the report

### Requirement: Deterministic sandbox authentication

The E2E suite SHALL log in via the OTP sandbox path so that authentication is repeatable without a real SMS inbox.

#### Scenario: OTP login is deterministic
- **WHEN** a spec performs phone-number login and the backend runs with `OTP_SANDBOX_CODE=123456` and `Sms:SandboxMode=true`
- **THEN** entering the sandbox code authenticates the user and no real SMS is sent

#### Scenario: Unique identities per run
- **WHEN** a spec creates a provider and a customer
- **THEN** it uses unique per-run phone numbers (e.g. `912…`/`913…`) so repeated runs do not collide

### Requirement: Keystone journey coverage through the UI

The E2E suite SHALL cover the keystone booking journey end-to-end through the browser UI: a provider onboards and adds staff, and a customer books and then cancels.

#### Scenario: Provider onboards and adds staff
- **WHEN** a provider logs in via OTP and completes the registration wizard, then adds a staff member
- **THEN** the provider is active and the new staff member appears in the staff list

#### Scenario: Customer books an appointment
- **WHEN** a customer logs in via OTP, browses to the provider, selects a service, time, and staff, and confirms
- **THEN** the booking is created and appears in the customer's "My Bookings" list

#### Scenario: Customer cancels a booking
- **WHEN** the customer cancels that booking from "My Bookings"
- **THEN** the booking is shown as cancelled and the UI reflects the policy's refund outcome

### Requirement: Stable selectors and structure

The E2E suite SHALL use `data-testid` attributes for element selection and organize interactions with the Page Object Model.

#### Scenario: Tests do not depend on styling
- **WHEN** an Ant Design class or visible label changes but the `data-testid` is unchanged
- **THEN** the affected spec continues to locate the element and pass

#### Scenario: Selectors live in Page Objects
- **WHEN** a screen's markup changes
- **THEN** only that screen's Page Object needs updating, not the individual specs

### Requirement: External map widget is isolated

The E2E suite SHALL neutralize the Neshan map dependency so map tiles/geolocation cannot make runs flaky.

#### Scenario: Map network is stubbed
- **WHEN** a screen containing the Neshan map loads during a test
- **THEN** the suite stubs the map tile/geocode requests and grants a fixed geolocation, and the test does not assert on map internals

### Requirement: CI execution

The system SHALL provide a CI job that runs the Playwright suite headless against an ephemeral stack and surfaces failure artifacts.

#### Scenario: E2E job runs in CI
- **WHEN** the `frontend-e2e` CI job runs
- **THEN** it boots Postgres, Redis, and the host (sandbox env), builds/serves the frontend, runs Playwright headless, and uploads the trace/report artifacts on failure

### Requirement: Fast-fail preflight before the browser suite runs

The E2E suite SHALL verify the environment is reachable and correctly configured before Playwright launches any browser test, and SHALL fail fast with a clear diagnostic message rather than letting individual specs time out against a down or misconfigured stack.

#### Scenario: Environment is down
- **WHEN** `npm run e2e:pw` is invoked and the Booksy host is not reachable (or does not accept the sandbox OTP flow)
- **THEN** the run fails within seconds during global setup with a message identifying which check failed (host unreachable vs. auth not validating), before any Playwright test or browser starts

#### Scenario: Environment is healthy
- **WHEN** the host is up with `OTP_SANDBOX_CODE` and `Sms:SandboxMode` configured
- **THEN** the preflight check passes silently and the suite proceeds to seeding and the test specs

### Requirement: Centralized per-run seeding

The E2E suite SHALL seed its shared supply-side fixture (a bookable provider with an active staff member and service) once per run, in Playwright's `globalSetup`, rather than per spec file.

#### Scenario: Specs share one seeded provider
- **WHEN** multiple spec files need a bookable provider
- **THEN** they read the provider/staff/service ids produced by `globalSetup` instead of each calling `seedBookableProvider()` independently

### Requirement: My Bookings and cancellation journey coverage

The E2E suite SHALL cover, through the browser UI, a customer viewing a real seeded booking in "My Bookings" and cancelling it.

#### Scenario: Booking appears in My Bookings
- **WHEN** a customer with a seeded, confirmed booking opens "My Bookings"
- **THEN** the booking row is visible with the correct provider/service/time

#### Scenario: Customer cancels from My Bookings
- **WHEN** the customer cancels that booking
- **THEN** the UI reflects the cancelled state and the applicable refund messaging

### Requirement: Booking reschedule journey coverage

The E2E suite SHALL cover, through the browser UI, a customer rescheduling an existing booking to a new time via the bookings sidebar (the UI surface that actually implements reschedule — the `/customer/my-bookings` page only supports cancel).

#### Scenario: Customer reschedules a booking
- **WHEN** a customer with a seeded booking opens the bookings sidebar, chooses "تغییر زمان" (reschedule) on that booking, and confirms a different available slot
- **THEN** the reschedule request succeeds and the modal closes, reflecting the new time

