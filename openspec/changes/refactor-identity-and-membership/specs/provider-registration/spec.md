## ADDED Requirements

### Requirement: Owner service-provision onboarding branch

Provider onboarding SHALL ask the owner whether they personally provide services, and use the answer to decide whether the owner becomes the first active staff member. This step appears after Working Hours and requires no invitation.

#### Scenario: Owner provides services
- **WHEN** the owner answers "Yes, I personally provide services"
- **THEN** the owner's membership is created with both Owner and StaffProvider roles
- **AND** a StaffProfile with `providesServices = true` is attached
- **AND** the owner appears as the first active staff member with no invitation sent

#### Scenario: Owner does not provide services
- **WHEN** the owner answers "No, I only manage the business"
- **THEN** the owner's membership is created with only the Owner role
- **AND** no StaffProfile is created
- **AND** the salon completes onboarding with zero staff and an option to invite staff later

#### Scenario: Completed salon is not treated as broken when owner-only
- **WHEN** an owner-only salon finishes onboarding with zero staff
- **THEN** the app presents an "invite your team" action rather than a missing-setup error
