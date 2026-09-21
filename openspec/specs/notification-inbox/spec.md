# notification-inbox Specification

## Purpose
TBD - created by archiving change notification-system. Update Purpose after archive.
## Requirements
### Requirement: In-app notifications are persisted and readable
An in-app notification SHALL be persisted when it is dispatched and SHALL remain readable afterwards. A user
SHALL be able to list their notifications, newest first, in pages.

A user who was offline when a notification was sent SHALL see it on their next read.

#### Scenario: Offline user sees the notification later
- **WHEN** a notification is dispatched while the recipient's app is closed
- **AND** the recipient later opens the app
- **THEN** the notification appears in their inbox

#### Scenario: Inbox is paged newest first
- **WHEN** a user requests their inbox
- **THEN** notifications are returned newest first, in pages

### Requirement: A user reads only their own notifications
Every inbox read SHALL be scoped to the authenticated caller. A user SHALL NOT be able to read another user's
notifications or another user's delivery status by supplying an identifier.

#### Scenario: Another user's notification is not readable
- **WHEN** a user requests the delivery status of a notification addressed to someone else
- **THEN** the request is refused

#### Scenario: Listing is scoped to the caller
- **WHEN** a user lists their inbox
- **THEN** only notifications addressed to that user are returned

### Requirement: Read-state and unread count
A notification SHALL carry per-recipient read-state. A user SHALL be able to mark one notification read, mark
all read, and retrieve their unread count. Marking read SHALL be idempotent.

#### Scenario: Marking read reduces the unread count
- **WHEN** a user marks an unread notification as read
- **THEN** their unread count decreases by one

#### Scenario: Marking read twice changes nothing further
- **WHEN** a user marks an already-read notification as read
- **THEN** the request succeeds and the unread count is unchanged

#### Scenario: Mark-all clears the count
- **WHEN** a user marks all notifications read
- **THEN** their unread count is zero

### Requirement: Tap destinations are resolved when the inbox is read
A notification's title and body SHALL be immutable once written. Its tap destination SHALL be recomputed from
current entity state each time the inbox is read, for the whole page in one batched lookup.

A notification whose target no longer exists, or no longer belongs to the reader, SHALL be returned as
non-actionable rather than linking to a target that cannot be opened.

#### Scenario: Stale target becomes non-actionable
- **WHEN** a user reads a notification about a booking that has since been deleted
- **THEN** the notification is returned as non-actionable and its text is unchanged

#### Scenario: Text does not change when the entity changes
- **WHEN** a booking is rescheduled after its confirmation notification was sent
- **THEN** the original notification still reads as it did when sent

