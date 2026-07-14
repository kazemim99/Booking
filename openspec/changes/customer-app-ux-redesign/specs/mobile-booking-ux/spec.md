# mobile-booking-ux

## ADDED Requirements

### Requirement: Stepped booking flow
The app SHALL provide a booking journey as sequential full-screen steps: service selection → staff selection (skipped automatically when only one staff member can perform the service) → date and time-slot selection → confirmation summary. Each step SHALL show progress, preserve prior selections when navigating back, and be individually recoverable on error. The flow SHALL use the existing booking endpoints unchanged.

#### Scenario: Complete a booking
- **WHEN** an authenticated user selects a service, staff, and an available slot, then confirms
- **THEN** the booking is created via the existing API and a success state shows the booking summary with Jalali date/time

#### Scenario: Single-staff provider
- **WHEN** the selected service has exactly one available staff member
- **THEN** the staff step is skipped and the flow proceeds directly to date/slot selection

#### Scenario: Back navigation preserves choices
- **WHEN** the user goes back from slot selection to service selection and returns forward
- **THEN** previously selected values remain selected

### Requirement: Jalali date and slot picker
The date/slot step SHALL present a horizontally browsable Jalali calendar of upcoming days with availability indication, and the available time slots for the selected day as tappable chips. Days with no availability SHALL be visibly disabled. Slot data SHALL refresh when the day changes, with skeleton loading and no stale slots from a previous day.

#### Scenario: Switch day
- **WHEN** the user selects a different day
- **THEN** slots for that day load with placeholders and slots from the previous day are never shown for the new day

#### Scenario: Slot taken between selection and confirm
- **WHEN** booking creation fails because the slot is no longer available
- **THEN** the user is returned to the slot step with an explanatory error and refreshed availability, and other selections are preserved

### Requirement: Confirmation summary before commit
The confirmation step SHALL display service, staff, provider, Jalali date/time, duration, and price before any commit action. Confirming SHALL show a loading state on the CTA; the flow MUST NOT create a booking before this explicit confirmation.

#### Scenario: User reviews before confirming
- **WHEN** the user reaches the confirmation step
- **THEN** all selections are displayed for review and nothing is booked until the confirm CTA is tapped

### Requirement: Appointments list with status-driven cards
The appointments screen SHALL separate upcoming and past bookings, each rendered as a card with a semantic `StatusBadge` (confirmed, pending, completed, cancelled, no-show), service, provider, and Jalali date/time. The list SHALL provide skeleton loading, an empty state with a discover-providers CTA, error state with retry, and pull-to-refresh.

#### Scenario: Empty appointments
- **WHEN** an authenticated user with no bookings opens the appointments tab
- **THEN** an empty state explains there are no bookings and offers a CTA to explore providers

#### Scenario: Status legibility
- **WHEN** bookings with different statuses render
- **THEN** each shows a badge whose color and label distinguish the status without relying on color alone

### Requirement: Cancel with confirmation
Cancelling a booking SHALL require a `ConfirmSheet` stating the consequence, SHALL execute via the existing cancel endpoint with an optional reason, and on success update the card status and show a confirmation snackbar. Cancel SHALL only be offered on bookings the API allows cancelling.

#### Scenario: Cancel an upcoming booking
- **WHEN** the user taps cancel on an eligible booking and confirms in the sheet
- **THEN** the booking is cancelled via the API, the card updates to cancelled, and a snackbar confirms

#### Scenario: Abort cancellation
- **WHEN** the user dismisses the confirmation sheet
- **THEN** no request is sent and the booking is unchanged

### Requirement: Reschedule reuses the slot picker
Rescheduling SHALL reuse the date/slot picker step pre-scoped to the existing booking's service and staff, mirroring the web application's reschedule semantics against the same endpoint. On success the appointment card SHALL reflect the new Jalali date/time.

#### Scenario: Reschedule an upcoming booking
- **WHEN** the user chooses reschedule, picks a new available slot, and confirms
- **THEN** the booking is updated via the existing reschedule endpoint and the list shows the new time
