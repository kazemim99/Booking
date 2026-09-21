# Notification Raising

How a notification comes into existence. Covers the transactional outbox that records a notification intent
alongside the business write and the sweep that turns intents into dispatched notifications.

Delivery mechanics — channel sending, retry, de-duplication — belong to `notification-delivery`. What each
notification says and who receives it belongs to `notification-event-catalog`.

## ADDED Requirements

### Requirement: A notification intent is recorded in the business transaction
When a business operation must notify someone, the system SHALL record a notification intent in the same
database transaction as the business write. The intent SHALL carry the notification's event code, its
recipient, the entity it concerns, and the parameters its copy needs.

A notification SHALL NOT be sent from inside the request path, and SHALL NOT be sent by a gateway call made
before the business transaction commits.

#### Scenario: Business write and intent commit together
- **WHEN** a booking is created and its confirmation notification is raised
- **THEN** the booking row and the notification intent are committed in one transaction

#### Scenario: Rolled-back work notifies nobody
- **WHEN** a booking creation fails after its notification intent was recorded
- **THEN** the transaction rolls back and no notification is ever sent

#### Scenario: A slow gateway does not fail the business operation
- **WHEN** the SMS gateway is unreachable at the moment a booking is created
- **THEN** the booking is created successfully and the notification is sent later by the sweep

### Requirement: Intents are swept into notifications exactly once
A background sweep SHALL claim pending intents, resolve each against the notification catalogue, create the
corresponding `Notification`, and dispatch it through `INotificationDispatcher`. An intent SHALL be claimed by
at most one sweep at a time, and a claimed intent SHALL be marked processed so that it is never swept twice.

An intent whose processing fails SHALL be left in a retryable state rather than lost, and SHALL be abandoned
to a dead-letter state once its attempt budget is spent.

#### Scenario: Each intent produces one notification
- **WHEN** the sweep runs twice over the same pending intent
- **THEN** exactly one notification is created and dispatched for it

#### Scenario: Concurrent sweeps do not double-send
- **WHEN** two sweep instances run at the same time against the same pending intents
- **THEN** each intent is claimed by exactly one of them

#### Scenario: A failed intent is retried, not dropped
- **WHEN** processing an intent throws
- **THEN** the intent remains pending and is attempted again on a later sweep

#### Scenario: A permanently failing intent is dead-lettered
- **WHEN** an intent has failed more times than its attempt budget allows
- **THEN** it is marked dead-lettered and is no longer retried

### Requirement: Raising a notification requires a catalogue entry
The raise API SHALL accept a notification event code and SHALL derive audience, channels, criticality and
suppressibility from the catalogue. It SHALL NOT accept a caller-supplied channel set or criticality, and
raising a code with no catalogue entry SHALL fail loudly rather than send with defaults.

#### Scenario: Unknown code is rejected
- **WHEN** code attempts to raise a notification for an event code with no catalogue entry
- **THEN** the attempt fails with an error naming the missing code

#### Scenario: Channels come from the catalogue
- **WHEN** a notification is raised for a code the catalogue marks as push and in-app only
- **THEN** no SMS is sent for it regardless of what the calling code requested
