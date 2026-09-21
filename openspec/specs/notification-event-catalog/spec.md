# notification-event-catalog Specification

## Purpose
TBD - created by archiving change notification-system. Update Purpose after archive.
## Requirements
### Requirement: Every notification has a catalogue entry
The system SHALL define a stable notification event code for each distinct notification, and SHALL hold for
each code: the audience (customer, provider, or staff member), the channel set, the criticality, whether it
is suppressible, and its tap destination kind.

The code SHALL be stable across releases and SHALL be exposed to clients so a client can select presentation
from it without parsing copy or inferring from a category string.

#### Scenario: Catalogue resolves a code
- **WHEN** the system looks up a notification event code
- **THEN** it returns that code's audience, channels, criticality, suppressibility and destination kind

#### Scenario: Client selects presentation from the code
- **WHEN** a client receives a notification
- **THEN** the payload carries the event code, and the client selects its icon and layout from that code alone

### Requirement: Criticality governs suppressibility only
A notification marked critical SHALL be sent regardless of the recipient's channel preferences. Every other
notification SHALL honour those preferences.

Criticality SHALL be a recorded product decision. The critical set SHALL be limited to notifications whose
suppression would strand the recipient mid-flow or withhold a financial record they are entitled to:
one-time codes and account-security notices, money movement, and the notifications the product has explicitly
designated non-suppressible.

#### Scenario: Critical notification ignores a disabled channel
- **WHEN** a recipient has disabled SMS and a critical notification is raised on SMS
- **THEN** the SMS is sent

#### Scenario: Non-critical notification honours a disabled channel
- **WHEN** a recipient has disabled push and a non-critical notification is raised on push
- **THEN** no push is sent and the skip is recorded

### Requirement: SMS is reserved for critical notifications
The catalogue SHALL NOT assign the SMS channel to a notification that is not critical.

#### Scenario: Non-critical entry cannot carry SMS
- **WHEN** the catalogue is validated
- **THEN** any non-critical entry naming SMS is reported as invalid

### Requirement: The catalogue covers customer and provider notifications
The catalogue SHALL include entries for the booking lifecycle as seen by both sides (requested, confirmed,
rejected, rescheduled, cancelled — distinguishing who cancelled — completed, no-show), for money movement
(payment taken, payment failed, refund, payout), for account and verification changes, for staff and
organisation membership changes, and for time-based reminders.

A notification SHALL address the party who can act on it: a cancellation by the customer notifies the
provider, and a cancellation by the provider notifies the customer.

#### Scenario: Cancellation notifies the counterparty
- **WHEN** a customer cancels a booking
- **THEN** the provider is notified, and the customer receives an acknowledgement rather than the same notice

#### Scenario: Provider learns a booking request arrived
- **WHEN** a booking is requested against a provider that requires approval
- **THEN** the provider is notified that a request is awaiting their decision

### Requirement: Copy is rendered from templates, localised and in local time
Notification copy SHALL be rendered from stored templates with parameters supplied by the raise site, not
assembled in handler code. Copy for this product SHALL be rendered in Persian, dates SHALL be rendered in the
Jalali calendar, and times SHALL be rendered in the salon's local wall-clock.

#### Scenario: Booking notification renders Persian and Jalali
- **WHEN** a booking confirmation is rendered for a booking at a known local time
- **THEN** the text is Persian and the date is Jalali, showing the salon's wall-clock time

#### Scenario: Copy is not built in the handler
- **WHEN** a notification is raised
- **THEN** the raise site supplies parameters only, and the text comes from the template

