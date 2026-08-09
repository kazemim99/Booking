## ADDED Requirements

### Requirement: Person is the single identity across organizations

The system SHALL model a person as one `Person` account (the UserManagement `User`) uniquely identified by phone number, independent of any organization. A person MUST be able to hold memberships in zero, one, or many organizations without the system ever creating a duplicate person account.

#### Scenario: One person, many organizations
- **WHEN** a person who already has an account is added to a second organization
- **THEN** the system reuses the existing `PersonId`
- **AND** creates a new membership linking that person to the second organization
- **AND** does not create a second person account or profile

#### Scenario: Person data is independent of any organization
- **WHEN** a person's memberships change (added, terminated)
- **THEN** the person's identity, profile, media, and reputation remain unchanged

### Requirement: Organization Membership aggregate

The system SHALL represent the link between a person and an organization as an `OrganizationMembership` carrying a set of organization-scoped roles, a status, and join/leave history. At most one non-terminated membership MAY exist per `(PersonId, OrganizationId)`.

#### Scenario: Membership carries org-scoped roles
- **WHEN** a membership is created
- **THEN** it stores `PersonId`, `OrganizationId`, a set of roles (Owner, Manager, StaffProvider, Receptionist), a status, and timestamps
- **AND** the roles apply only within that organization

#### Scenario: One live membership per person per organization
- **WHEN** a person already has a non-terminated membership in an organization
- **AND** the system attempts to create another membership for the same person and organization
- **THEN** the operation is rejected

#### Scenario: An organization always retains an owner
- **WHEN** an operation would remove the Owner role from, or terminate, the last remaining Owner membership of an organization
- **THEN** the operation is rejected

### Requirement: Owner and staff roles coexist

A membership SHALL be able to hold the Owner role and a service-providing role simultaneously, and an owner MAY exist without providing services.

#### Scenario: Owner who also provides services
- **WHEN** an owner indicates they personally provide services
- **THEN** their membership holds both Owner and StaffProvider roles
- **AND** a StaffProfile is attached to that membership

#### Scenario: Owner who does not provide services
- **WHEN** an owner indicates they do not personally provide services
- **THEN** their membership holds only the Owner role
- **AND** no StaffProfile is attached

### Requirement: Staff Profile owned by a membership

A `StaffProfile` SHALL exist only as an owned part of a membership that provides services, holding the `providesServices` flag, an optional per-organization bio, service assignments, and a working schedule validated within the organization's operating hours.

#### Scenario: Staff profile schedule respects organization hours
- **WHEN** a staff working schedule is set on a membership's StaffProfile
- **THEN** the schedule must fall within the organization's operating hours
- **AND** hours outside the organization's hours are rejected

#### Scenario: Multi-salon staff keep separate schedules
- **WHEN** one person has active memberships in two organizations
- **THEN** each membership has its own StaffProfile and schedule
- **AND** both reference the same single `PersonId`

### Requirement: Membership lifecycle and rejoin

Membership status SHALL follow `Invited → Active → Suspended → Terminated`, recording `InvitedAt`, `JoinedAt`, and `LeftAt`. Changing organizations MUST terminate the previous membership and create a new one without creating a new person.

#### Scenario: Employee changes salon
- **WHEN** an employee leaves salon A and joins salon B
- **THEN** the membership in salon A is terminated with `LeftAt` recorded
- **AND** a new membership is created in salon B
- **AND** the person account, profile, history, and ratings are unchanged

#### Scenario: Rejoin a previous organization
- **WHEN** a person with a terminated membership in an organization is invited again
- **THEN** a new membership is created
- **AND** the prior terminated membership is retained as history

### Requirement: Invitation resolves a person by phone

Sending a staff invitation SHALL normalize and validate the phone number, then reuse an existing person account when one matches, or defer person creation until registration. The system MUST NOT create a duplicate person for a phone that already has an account.

#### Scenario: Invite a phone that already has an account
- **WHEN** an organization invites a phone number that belongs to an existing person
- **THEN** the system links that existing person to a new `Invited` membership
- **AND** no new person account is created

#### Scenario: Invite a phone with no account
- **WHEN** an organization invites a phone number that has no account
- **THEN** an invitation is recorded without creating a person yet
- **AND** the person is created only when they register and accept

#### Scenario: Prevent self-invitation
- **WHEN** an organization owner invites their own phone number, or a phone that is already an active member of that organization
- **THEN** the invitation is rejected

### Requirement: Accept invitation

An invited person SHALL become an active staff member by accepting the invitation — logging in if they already have an account, or registering (phone verification) if they do not — after which any missing profile fields are completed.

#### Scenario: Existing user accepts
- **WHEN** an invited existing user opens the invitation and accepts
- **THEN** their membership transitions to Active with `JoinedAt` recorded
- **AND** they appear in the organization's staff list

#### Scenario: New user registers and accepts
- **WHEN** an invited phone with no account registers, verifies the phone, and accepts
- **THEN** a single person account is created
- **AND** their membership transitions to Active
- **AND** they are prompted to complete any missing profile fields

#### Scenario: Expired invitation
- **WHEN** an invitation past its expiry is opened
- **THEN** acceptance is refused and the invitation is marked expired

### Requirement: Query a person's memberships

The system SHALL expose the set of organizations a person belongs to, so the provider app can present the person's memberships and an active-organization selection.

#### Scenario: List memberships for the current person
- **WHEN** an authenticated person requests their memberships
- **THEN** the system returns each membership's organization, roles, and status
