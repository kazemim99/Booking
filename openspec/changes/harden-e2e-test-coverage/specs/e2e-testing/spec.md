## ADDED Requirements

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
