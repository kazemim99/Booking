## ADDED Requirements

### Requirement: Phone number is the globally unique person identity

The system SHALL treat a normalized (canonical E.164) phone number as the globally unique identifier of a person, enforced at BOTH the database level (unique index) and the application level (uniqueness guard before any account creation). No two active person accounts MAY share the same phone number.

#### Scenario: Duplicate phone rejected at creation
- **WHEN** any registration path attempts to create a person with a phone number that already belongs to an active account
- **THEN** the creation is rejected and the existing account is used instead

#### Scenario: Phone normalized before lookup
- **WHEN** a phone number is provided in any accepted form (local `09…`, `+98…`, `0098…`)
- **THEN** it is normalized to canonical E.164 before uniqueness is checked

#### Scenario: Database enforces uniqueness
- **WHEN** two concurrent requests attempt to create accounts for the same phone number
- **THEN** at most one account is created
- **AND** the database unique index prevents the duplicate

#### Scenario: Every creation path is guarded
- **WHEN** an account would be created via OTP login, email/password registration, invitation acceptance, or seeding
- **THEN** the same phone canonicalization and uniqueness guard applies

### Requirement: OTP authentication is gated by account status

Passwordless (OTP) authentication SHALL verify the account status before issuing tokens and MUST refuse to authenticate `Banned`, `Suspended`, `Inactive`, or `Deleted` accounts.

#### Scenario: Blocked account cannot obtain tokens via OTP
- **WHEN** a person whose account is Banned or Suspended completes the OTP challenge
- **THEN** no tokens are issued and a clear status error is returned

#### Scenario: Active account authenticates
- **WHEN** an Active person completes the OTP challenge
- **THEN** tokens are issued as normal
