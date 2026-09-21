# notification-delivery Specification

## Purpose
Guarantees for getting a customer-facing notification to the customer: that a transient gateway failure is
retried rather than lost, that an event redelivered by the transport does not notify twice, that the customer's
channel choices are honoured except where a notification is legally or operationally non-suppressible, and that
every attempt is observable after the fact.

Covers the delivery mechanics only — what triggers a notification and what it says belong to the capability
raising it (booking, payment, provider registration).
## Requirements
### Requirement: Lifecycle notifications are delivered reliably and de-duplicated
Recipient lifecycle notifications SHALL be persisted before sending, retried durably on transient failure,
and de-duplicated per event, channel and recipient, so each event notifies at most once per channel despite
retries or redelivery. This applies to customers, providers and staff members alike, and covers booking
requested, confirmed, rejected, cancelled, rescheduled and completed, reminders, payment, refund and payout.
A notification whose retry budget is exhausted SHALL be dead-lettered rather than retried indefinitely.

De-duplication SHALL distinguish recipients: one event that notifies both the customer and the provider SHALL
produce one notification for each, not one in total.

#### Scenario: Transient sender failure is retried
- **WHEN** a notification send fails transiently
- **THEN** it is retried and eventually delivered

#### Scenario: Redelivery does not duplicate
- **WHEN** the same lifecycle event is delivered more than once
- **THEN** the recipient receives at most one notification per channel for that event

#### Scenario: Both parties are notified for a shared event
- **WHEN** an event notifies both the customer and the provider
- **THEN** each receives their own notification, and neither suppresses the other

### Requirement: Channel preferences are enforced
Before sending on a channel, the system SHALL check the recipient's notification preferences and skip disabled
channels, except for a documented set of non-suppressible notifications.

Preference checks SHALL be evaluated using an exact membership test. A notification type SHALL NOT be tested
for membership by a bitwise flag operation over a representation whose members are not distinct bits, because
such a test reports false positives and can silently send a notification the recipient disabled — or suppress
one they did not.

#### Scenario: Disabled channel is skipped
- **WHEN** a recipient has disabled a channel and a suppressible notification is dispatched
- **THEN** that channel is not used

#### Scenario: Preference membership is exact
- **WHEN** a recipient has disabled one notification type and a different type with an overlapping numeric
  representation is dispatched
- **THEN** the dispatched notification is unaffected by the unrelated preference

### Requirement: A single SMS notification interface
The codebase SHALL expose one `ISmsNotificationService` abstraction; duplicate interfaces SHALL be removed and all callers/registrations consolidated.

#### Scenario: One registration resolves
- **WHEN** the SMS notification service is resolved from DI
- **THEN** exactly one implementation is registered against one interface

### Requirement: A channel reports delivery only when the gateway accepted it
A channel SHALL record a delivery only when its gateway accepted the message. A channel that is not
implemented, is disabled by configuration, or has no transport SHALL record the attempt as skipped and SHALL
NOT report success.

The delivery log SHALL therefore never contain a delivery that did not occur.

#### Scenario: An unimplemented channel does not claim success
- **WHEN** a notification names a channel that has no working transport
- **THEN** that channel is recorded as skipped and the delivery log shows no delivery for it

#### Scenario: A rejected send is recorded as failed
- **WHEN** the push gateway rejects a message
- **THEN** the attempt is recorded as failed, not delivered, and remains eligible for retry

#### Scenario: An accepted send is recorded as delivered
- **WHEN** the push gateway accepts a message
- **THEN** the attempt is recorded as delivered with the gateway's message id

