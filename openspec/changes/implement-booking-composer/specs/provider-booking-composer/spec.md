## ADDED Requirements

### Requirement: Provider Booking Composition Flow

The provider app SHALL provide a booking composer reachable from the Home create action (⊕ → "نوبت جدید"), the empty-agenda action, and the Growth walk-in CTA. The composer MUST require a service, a staff member, a date, and an available time slot before submission, and MUST offer optional walk-in client name/phone and notes. Submission SHALL create the booking via `POST /Bookings` and, on success, MUST return to Home and refresh it so the new booking appears.

#### Scenario: Successful walk-in booking
- **WHEN** the provider selects a service, staff, date, and an available slot, and submits
- **THEN** the booking is created and the composer closes
- **AND** the Home refreshes and shows the new booking with a success confirmation

#### Scenario: Submit is gated on required inputs
- **WHEN** service, staff, or slot is not yet selected
- **THEN** the submit action is disabled

#### Scenario: Creation failure surfaces and preserves input
- **WHEN** the create call fails
- **THEN** a human-readable error is shown and all selections are preserved for retry

### Requirement: Live Slot Selection

The composer SHALL fetch available slots from `GET /Bookings/available-slots` for the selected provider/service/date (and staff when selected), and MUST re-fetch whenever any of those selections change, discarding stale in-flight results. Times SHALL be displayed in local wall-clock time.

#### Scenario: Slots follow the selection
- **WHEN** the provider changes the service, staff, or date
- **THEN** the slot list reloads for the new selection
- **AND** a previously selected slot that no longer exists is cleared

#### Scenario: No availability is stated plainly
- **WHEN** the selected day has no available slots
- **THEN** an empty state says so and suggests picking another day

#### Scenario: Stale slot responses are discarded
- **WHEN** a slower slots request for a previous selection completes after a newer one
- **THEN** the newer selection's slots remain displayed

### Requirement: Walk-In Identity Convention (MVP)

Until a first-class walk-in customer concept exists in the backend, composer bookings SHALL be created under the provider's own account, and the walk-in client's name/phone (when entered) MUST be prepended to `customerNotes` in a recognizable convention so staff can identify the client from booking details.

#### Scenario: Walk-in details are carried in notes
- **WHEN** the provider enters a client name and phone and submits
- **THEN** the created booking's notes begin with the walk-in client identification, followed by any free-form notes

### Requirement: Bookings Are Confirmable Without a Payment System

While no payment flow exists in the product, the default booking policy (used when a service defines none) SHALL NOT require a deposit, so provider confirmation of requested bookings succeeds. Services that explicitly configure a deposit-requiring policy retain that requirement.

#### Scenario: Default-policy booking can be confirmed
- **WHEN** a booking is created for a service with no explicit booking policy and the provider confirms it
- **THEN** the confirmation succeeds and the booking becomes Confirmed

#### Scenario: Explicit deposit policies still enforce
- **WHEN** a service's own policy requires a deposit that is unpaid
- **THEN** confirmation is still rejected with the deposit error
