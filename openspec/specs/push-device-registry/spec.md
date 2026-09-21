# push-device-registry Specification

## Purpose
TBD - created by archiving change notification-system. Update Purpose after archive.
## Requirements
### Requirement: A device registers its push token
An authenticated user SHALL be able to register a push token for a device, together with the device's
platform. Registering a token that is already registered to the same user SHALL refresh it rather than create
a duplicate.

A token SHALL belong to exactly one user at a time: registering a token previously held by another user SHALL
move it, so a shared or re-used handset does not deliver one person's notifications to another.

#### Scenario: Token is registered
- **WHEN** a user registers a push token for their device
- **THEN** the token is stored against that user and is used for their subsequent push notifications

#### Scenario: Re-registering refreshes rather than duplicates
- **WHEN** a user registers a token they have already registered
- **THEN** the existing registration is refreshed and no second row is created

#### Scenario: A re-used handset moves with its new owner
- **WHEN** a token registered to one user is registered by a different user
- **THEN** the token is reassigned, and the previous owner no longer receives push on it

### Requirement: Tokens are revoked and pruned
A user SHALL be able to revoke a device token. Signing out SHALL revoke that device's token. A token the push
gateway reports as permanently invalid SHALL be removed automatically rather than retried.

#### Scenario: Sign-out stops push to that device
- **WHEN** a user signs out on a device
- **THEN** that device's token is revoked and receives no further push

#### Scenario: Gateway-rejected token is removed
- **WHEN** the push gateway reports a token as unregistered or invalid
- **THEN** the token is deleted and is not used again

### Requirement: Push is delivered to every live device
A push notification SHALL be sent to each of the recipient's registered tokens. A failure on one device SHALL
NOT prevent delivery to the recipient's other devices.

A recipient with no registered token SHALL be recorded as skipped for the push channel, not as failed, so that
the absence of a device does not consume the notification's retry budget.

#### Scenario: All devices receive it
- **WHEN** a user with three registered devices receives a push notification
- **THEN** it is sent to all three

#### Scenario: One bad device does not block the others
- **WHEN** one of a user's device tokens fails and the others succeed
- **THEN** the others are delivered and only the failing device is recorded as failed

#### Scenario: No device is a skip, not a failure
- **WHEN** a push notification is dispatched for a user with no registered device
- **THEN** the push channel is recorded as skipped and the retry budget is untouched

