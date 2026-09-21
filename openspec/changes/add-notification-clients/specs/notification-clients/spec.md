# notification-clients Specification

## ADDED Requirements

### Requirement: Notification preferences a person sets are the preferences the system keeps
A control that offers to change a notification preference SHALL persist that change, and a control the
backend cannot honour SHALL NOT be offered. A saved preference SHALL be the one returned when the screen is
re-opened.

#### Scenario: Turning a channel off keeps it off
- **WHEN** a provider clears the SMS checkbox and saves their preferences
- **THEN** the change is sent to the preferences endpoint
- **AND** re-opening the screen shows SMS still cleared

#### Scenario: No control makes a promise the backend cannot keep
- **WHEN** the preferences screen is rendered
- **THEN** every toggle shown maps to a preference the backend stores and applies
- **AND** any toggle whose effect the backend does not implement is either absent or visibly marked as not
  yet in effect

#### Scenario: A failed save is not reported as success
- **WHEN** saving preferences fails
- **THEN** the person is told it did not save
- **AND** the screen does not show the attempted values as if they had been kept

### Requirement: A signed-in device is reachable by push
Each mobile app SHALL register its push token with the backend after sign-in and whenever the token is
refreshed, and SHALL revoke it on sign-out, so that a push notification has somewhere to arrive.

#### Scenario: Signing in registers the device
- **WHEN** a person signs in to the app and grants notification permission
- **THEN** the app obtains its push token and registers it against their account

#### Scenario: A refreshed token replaces the old one
- **WHEN** the messaging service issues a new token for an installed app
- **THEN** the app registers the new token
- **AND** the account does not accumulate a second, dead entry for the same device

#### Scenario: Signing out stops the device receiving
- **WHEN** a person signs out
- **THEN** the app revokes its token
- **AND** a later notification for that account does not arrive on that device

#### Scenario: Refusing permission is not an error
- **WHEN** a person declines notification permission
- **THEN** the app continues to work normally and does not register a token
- **AND** nothing reports a failure to the person

#### Scenario: Registration failure does not block sign-in
- **WHEN** token registration fails
- **THEN** the person is signed in regardless
- **AND** registration is retried on the next launch

### Requirement: A received push opens what it is about
Tapping a push notification SHALL open the thing the notification concerns, and a notification arriving while
the app is in the foreground SHALL be shown without interrupting what the person is doing.

#### Scenario: Tapping a booking notification opens that booking
- **WHEN** a person taps a notification about a booking
- **THEN** the app opens that booking

#### Scenario: A notification whose subject is gone degrades gracefully
- **WHEN** a person taps a notification whose subject no longer exists or is no longer theirs
- **THEN** the app opens the notification list rather than an error screen

#### Scenario: Foreground delivery does not hijack the screen
- **WHEN** a notification arrives while the app is open
- **THEN** it is surfaced without navigating away from the current screen

### Requirement: A person can read their notifications
Each client SHALL show the signed-in person their notifications, newest first, with an unread count, and
SHALL let them mark one or all as read.

#### Scenario: The list shows what was actually sent
- **WHEN** a person opens their notifications
- **THEN** they see the notifications that have been sent to them, newest first
- **AND** a notification that has not been sent yet does not appear

#### Scenario: The badge and the list agree
- **WHEN** the unread count is shown
- **THEN** it equals the number of unread notifications in the list

#### Scenario: Reading a notification clears it from the count
- **WHEN** a person opens an unread notification
- **THEN** it is marked read
- **AND** the unread count decreases by one

#### Scenario: Marking all read is idempotent
- **WHEN** a person marks all notifications read twice
- **THEN** the second action neither fails nor changes anything

#### Scenario: An empty inbox says so
- **WHEN** a person with no notifications opens the list
- **THEN** they are told there is nothing, rather than shown a blank screen or a spinner

#### Scenario: Only the caller's notifications are shown
- **WHEN** the notification list is requested
- **THEN** it contains only notifications addressed to the signed-in person

### Requirement: Notification navigation targets exist
A client SHALL NOT offer navigation to a notification destination it does not implement.

#### Scenario: The admin menu's notifications entry resolves
- **WHEN** an administrator opens the user menu and selects notifications
- **THEN** a notifications screen opens

### Requirement: Client API constants match the deployed routes
Declared notification endpoint paths SHALL match the routes the backend serves, and SHALL be exercised by at
least one caller so a mismatch cannot survive unnoticed.

#### Scenario: Declared paths are the real paths
- **WHEN** a client requests the notification list or the unread count
- **THEN** the request reaches the backend route and returns data, rather than a 404
