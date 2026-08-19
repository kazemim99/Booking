# customer-booking-journey

> The existing requirements "Service selection", "Staff selection where applicable" (including single-staff
> auto-skip), "Date and time selection" (Jalali, no stale slots), "Booking confirmation" (nothing commits
> before explicit confirm; slot-taken recovery) and "Reservation completion" already specify this journey's
> steps. The deltas below add only what they do not cover.

## ADDED Requirements

### Requirement: Booking steps show progress and survive back-navigation
The booking journey SHALL render as sequential full-screen steps that show progress through the flow, preserve every prior selection when the user navigates backward and forward again, and recover from a failure in one step without discarding the others.

#### Scenario: Back navigation preserves choices
- **WHEN** the user goes back from slot selection to service selection and returns forward
- **THEN** previously selected values remain selected

#### Scenario: One step fails without losing the flow
- **WHEN** a request backing one step fails
- **THEN** that step shows its own error with retry, and selections made in other steps are retained

### Requirement: Day browser indicates and disables unavailable days
The date step SHALL present upcoming days as a horizontally browsable Jalali strip that visibly indicates availability and disables days with no availability. Changing the selected day SHALL show skeleton placeholders while its slots load.

#### Scenario: Unavailable day is not selectable
- **WHEN** a day in the strip has no availability
- **THEN** it renders visibly disabled and cannot be selected

#### Scenario: Switching day shows placeholders
- **WHEN** the user selects a different day
- **THEN** skeleton placeholders render while that day's slots load

### Requirement: Provider detail is deep-linkable and degrades without imagery
The provider detail screen SHALL be addressable by provider id as a deep link with a working back affordance to home, and SHALL render a branded placeholder wherever a gallery image is missing or fails to load. The primary booking CTA SHALL remain visible without scrolling on a standard viewport.

#### Scenario: Deep link to provider
- **WHEN** the app is opened via a provider deep link
- **THEN** the provider detail screen opens directly with a working back affordance to home

#### Scenario: Provider images unavailable
- **WHEN** a provider has no gallery images or an image fails to load
- **THEN** a branded placeholder renders instead of a broken or empty image area

### Requirement: Appointments list with status-driven cards
The appointments screen SHALL separate upcoming and past bookings, each rendered as a card with a semantic `StatusBadge` (confirmed, pending, completed, cancelled, no-show), service, provider, and Jalali date/time. The list SHALL provide skeleton loading, an empty state with a discover-providers CTA, an error state with retry, and pull-to-refresh.

#### Scenario: Empty appointments
- **WHEN** an authenticated user with no bookings opens the appointments tab
- **THEN** an empty state explains there are no bookings and offers a CTA to explore providers

#### Scenario: Status legibility
- **WHEN** bookings with different statuses render
- **THEN** each shows a badge whose colour and label distinguish the status without relying on colour alone

### Requirement: Cancel with confirmation
Cancelling a booking SHALL require a `ConfirmSheet` stating the consequence, SHALL execute via the existing cancel endpoint with an optional reason, and on success update the card status and show a confirmation snackbar. Cancel SHALL only be offered on bookings the API allows cancelling.

#### Scenario: Cancel an upcoming booking
- **WHEN** the user taps cancel on an eligible booking and confirms in the sheet
- **THEN** the booking is cancelled via the API, the card updates to cancelled, and a snackbar confirms

#### Scenario: Abort cancellation
- **WHEN** the user dismisses the confirmation sheet
- **THEN** no request is sent and the booking is unchanged

### Requirement: Reschedule reuses the slot picker
Rescheduling SHALL reuse the date/slot step pre-scoped to the existing booking's service and staff, mirroring the web application's reschedule semantics against the same endpoint. On success the appointment card SHALL reflect the new Jalali date/time.

#### Scenario: Reschedule an upcoming booking
- **WHEN** the user chooses reschedule, picks a new available slot, and confirms
- **THEN** the booking is updated via the existing reschedule endpoint and the list shows the new time
