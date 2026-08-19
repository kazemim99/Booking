# customer-booking-journey Specification

## Purpose
TBD - created by archiving change unify-customer-app-with-provider-design. Update Purpose after archive.
## Requirements
### Requirement: Provider details reviewed and refined
The provider-details screen SHALL present the provider's identity, rating, address/hours, services (with price and duration), and a clearly reachable booking call-to-action, rendered in the aligned visual language, without changing the underlying data or business rules.

#### Scenario: Booking CTA is reachable
- **WHEN** a user opens a provider's details
- **THEN** the provider identity, services, and a booking CTA are visible and styled consistently with the Provider app

### Requirement: Service selection
The service-selection step SHALL let the user choose a service with its price and duration, preserving the existing selection behavior and step order.

#### Scenario: Selecting a service advances the flow
- **WHEN** a user selects a service
- **THEN** the choice is retained and the flow proceeds to the next existing step

### Requirement: Staff selection where applicable
The staff-selection step SHALL let the user choose a qualified staff member where applicable and MUST auto-skip when only one staff member qualifies, preserving the existing behavior.

#### Scenario: Single qualified staff auto-skips
- **WHEN** only one staff member qualifies for the chosen service
- **THEN** the staff step is skipped automatically

#### Scenario: Multiple staff are selectable
- **WHEN** more than one staff member qualifies
- **THEN** the user can pick one, styled in the aligned language

### Requirement: Date and time selection
The date/time step SHALL present available days and slots (Jalali calendar) from the existing availability endpoint, refreshing per day without showing stale slots.

#### Scenario: Picking a day loads its slots
- **WHEN** a user selects a day
- **THEN** that day's available slots load from the existing availability endpoint with no stale slots shown

### Requirement: Booking confirmation
The confirmation step SHALL present a full summary and commit nothing until the user explicitly confirms; a slot taken between selection and confirmation MUST return the user to the slot step with refreshed availability, preserving existing behavior.

#### Scenario: Nothing commits before explicit confirm
- **WHEN** a user reaches the confirmation step
- **THEN** no reservation is created until the user explicitly confirms

#### Scenario: Slot-taken recovery
- **WHEN** the chosen slot was taken before confirmation
- **THEN** the user is returned to the slot step with refreshed availability

### Requirement: Reservation completion
On successful confirmation the app SHALL show a clear completion/success state and route the user consistently with the existing flow.

#### Scenario: Successful reservation shows completion
- **WHEN** a reservation is created successfully
- **THEN** an aligned success/completion state is shown and the existing post-booking routing occurs

### Requirement: Booking journey preserves business rules and step sequence
The booking journey review SHALL change only presentation, states, copy, and transitions — never the business rules, step sequence, or backend contracts. Functional gaps discovered SHALL be recorded in `findings.md` rather than implemented here.

#### Scenario: Refinement does not alter rules or order
- **WHEN** the booking journey is refined
- **THEN** the step sequence and business rules are identical to before, and any functional gap is logged in `findings.md`

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

