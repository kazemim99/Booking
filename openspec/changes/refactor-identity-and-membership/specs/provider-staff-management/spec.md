## MODIFIED Requirements

### Requirement: Add a Team Member

The staff screen SHALL offer an invite action opening a phone-first form (phone required; optional name and role). Submitting sends an invitation via the membership invitation API, which reuses an existing person account when the phone already has one and otherwise defers account creation to registration. The pending invitation MUST appear in the list, and self-invitation or inviting an existing active member MUST be refused with a clear message. Failures surface a message and preserve the form input.

#### Scenario: Successful invite
- **WHEN** the provider submits the form with a valid phone number
- **THEN** an invitation is sent and appears in the list as pending
- **AND** no duplicate person account is created if that phone already has one

#### Scenario: Submit is gated on the required phone
- **WHEN** the phone number is empty or invalid
- **THEN** the submit action is disabled

#### Scenario: Self-invite is refused
- **WHEN** the provider enters their own phone number, or that of an existing active member
- **THEN** the invitation is refused with a clear message

#### Scenario: Failure preserves input
- **WHEN** the invitation fails
- **THEN** an error message is shown and the entered values remain editable

## ADDED Requirements

### Requirement: Accept a Staff Invitation

The provider app SHALL let an invited person become an active staff member from an invitation link: an existing user logs in (phone OTP) and accepts; a new user registers (phone verification) and accepts. After acceptance the person completes any missing profile fields before becoming active.

#### Scenario: Existing user accepts an invitation
- **WHEN** an invited existing user opens the invitation and authenticates
- **THEN** an accept action activates their membership
- **AND** they land on the organization's workspace as active staff

#### Scenario: New user registers and accepts
- **WHEN** an invited phone with no account opens the invitation
- **THEN** they register and verify their phone
- **AND** accept the invitation, creating exactly one person account
- **AND** are prompted to complete any missing profile fields

#### Scenario: Expired or revoked invitation
- **WHEN** an invitation is expired or cancelled
- **THEN** the app shows an appropriate message and offers no accept action

### Requirement: Staff list reflects membership roles and status

The staff list SHALL display each member's organization-scoped roles and membership status (Invited, Active, Suspended, Terminated), distinguishing owners and service-providing staff, and MUST source its data from the organization's memberships rather than standalone staff records.

#### Scenario: Roles and status are visible
- **WHEN** the provider views the staff list
- **THEN** each member shows their role(s) and current membership status
- **AND** the owner is shown, marked as owner, and as service-providing when applicable

#### Scenario: Pending invitations are visible
- **WHEN** invitations are outstanding
- **THEN** they appear as pending entries the provider can resend or cancel
