# Spec: notification-delivery

## ADDED Requirements

### Requirement: Lifecycle notifications are delivered reliably and de-duplicated
Customer lifecycle notifications (booking confirmed/cancelled/rescheduled, reminders, payment, refund) SHALL be dispatched via the durable outbox with retry on transient failure and de-duplicated per event, channel, and recipient, so each event notifies at most once per channel despite retries or redelivery.

#### Scenario: Transient sender failure is retried
- **WHEN** a notification send fails transiently
- **THEN** it is retried and eventually delivered

#### Scenario: Redelivery does not duplicate
- **WHEN** the same lifecycle event is delivered more than once
- **THEN** the customer receives at most one notification per channel for that event

### Requirement: Channel preferences are enforced
Before sending on a channel, the system SHALL check the customer's notification preferences and skip disabled channels, except for a documented set of non-suppressible notifications.

#### Scenario: Disabled channel is skipped
- **WHEN** a customer has disabled a channel and a suppressible notification is dispatched
- **THEN** that channel is not used

### Requirement: A single SMS notification interface
The codebase SHALL expose one `ISmsNotificationService` abstraction; duplicate interfaces SHALL be removed and all callers/registrations consolidated.

#### Scenario: One registration resolves
- **WHEN** the SMS notification service is resolved from DI
- **THEN** exactly one implementation is registered against one interface
