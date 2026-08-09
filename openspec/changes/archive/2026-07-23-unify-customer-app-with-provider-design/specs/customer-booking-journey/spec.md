# Spec: customer-booking-journey

## ADDED Requirements

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
