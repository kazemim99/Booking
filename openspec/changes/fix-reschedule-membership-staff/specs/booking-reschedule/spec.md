# Spec: booking-reschedule

## ADDED Requirements

### Requirement: Rescheduling works for every bookable-resource kind
Rescheduling SHALL resolve the booking's staff reference using the same rules booking creation uses — an organization membership, the organization itself, or a legacy individual sub-provider — and SHALL NOT assume the reference is an individual provider.

#### Scenario: Booking against an organization member
- **GIVEN** a confirmed booking whose `StaffId` is a `MembershipId` of an active, service-providing member
- **WHEN** the customer reschedules it to an available time
- **THEN** the reschedule succeeds and the new booking is held for the same member

#### Scenario: Booking against a legacy individual sub-provider
- **GIVEN** a confirmed booking whose `StaffId` is a legacy individual sub-provider's `ProviderId`
- **WHEN** the customer reschedules it to an available time
- **THEN** the reschedule succeeds and the new booking is held for the same sub-provider

#### Scenario: Booking made directly against the organization
- **GIVEN** a confirmed booking whose `StaffId` is the organization's own `ProviderId`
- **WHEN** the customer reschedules it to an available time
- **THEN** the reschedule succeeds and the new booking is held against the organization

#### Scenario: Staff reference that resolves to nothing
- **WHEN** a booking's staff reference matches no membership, organization, or sub-provider
- **THEN** the request fails with a not-found error naming the unresolvable resource

### Requirement: Rescheduling moves the occupied slot for the correct resource
Releasing the old slot and occupying the new one SHALL be keyed the same way booking creation keys them, so a rescheduled booking never leaves its original slot occupied and never occupies a slot belonging to a different resource.

#### Scenario: Old slot is freed for the same resource
- **GIVEN** a booking occupying a slot for a member
- **WHEN** it is rescheduled to a different time
- **THEN** the original slot becomes available for that member again

#### Scenario: New slot is occupied for the same resource
- **WHEN** a member's booking is rescheduled to a new time
- **THEN** the new time is occupied against the same slot owner the original was

> Per-member slot isolation ("remains free for every other member") is deliberately NOT claimed here.
> `ProviderAvailability` carries a `StaffId`, but the overlap query does not filter on it, so slot
> lookups cannot be narrowed to one member today — for creation either. Reschedule is specified to
> match creation's keying, no more; making slots genuinely per-member belongs to
> `booking-slot-integrity`.

#### Scenario: Rescheduling into a taken time is rejected
- **WHEN** a booking is rescheduled to a time already occupied for that resource
- **THEN** the request is rejected as a conflict and the original booking keeps its original slot

### Requirement: The successor booking owns its own policy and payment state
The booking created by a reschedule SHALL carry its own copies of the original's owned state (booking policy, payment information) rather than references to the original's, so that persisting it neither fails nor mutates the original.

#### Scenario: Successor booking persists
- **WHEN** a booking is rescheduled
- **THEN** the successor booking is saved successfully, carrying the same policy terms and payment state as the original

#### Scenario: Original booking keeps its own policy
- **WHEN** a booking is rescheduled
- **THEN** the original booking still has its policy and payment information, unchanged

### Requirement: Rescheduling preserves or explicitly reassigns the resource
A reschedule that does not name a new staff member SHALL keep the booking's existing resource. A reschedule that names one SHALL validate it is bookable for the same organization before moving the booking.

#### Scenario: No staff change requested
- **WHEN** a reschedule request omits a new staff member
- **THEN** the rescheduled booking keeps the original staff reference

#### Scenario: Staff change to a bookable member
- **WHEN** a reschedule names another active, service-providing member of the same organization
- **THEN** the rescheduled booking is held for that member

#### Scenario: Staff change to a member of a different organization
- **WHEN** a reschedule names a member who does not belong to the booking's organization
- **THEN** the request is rejected and the booking is unchanged
