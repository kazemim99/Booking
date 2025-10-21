# Provider Onboarding

This specification defines the provider onboarding process, including registration status checking and routing based on profile completion state.

## ADDED Requirements

### Requirement: Provider Status Check on Login
The system SHALL check the Provider registration status for authenticated users with Provider role immediately after successful login.

#### Scenario: User with no Provider record
- **WHEN** a user with Provider role logs in successfully
- **AND** no Provider record exists for the user
- **THEN** the user SHALL be redirected to the Provider Registration Flow

#### Scenario: User with Drafted Provider status
- **WHEN** a user with Provider role logs in successfully
- **AND** their Provider record has status = Drafted
- **THEN** the user SHALL be redirected to the Provider Registration Flow to complete their profile

#### Scenario: User with PendingVerification Provider status
- **WHEN** a user with Provider role logs in successfully
- **AND** their Provider record has status = PendingVerification
- **THEN** the user SHALL be redirected to the Provider Dashboard
- **AND** a status message SHALL be displayed indicating verification is pending

#### Scenario: User with Verified or Active Provider status
- **WHEN** a user with Provider role logs in successfully
- **AND** their Provider record has status = Verified OR status = Active
- **THEN** the user SHALL be redirected to the Provider Dashboard with full access

#### Scenario: User with Inactive, Suspended, or Archived status
- **WHEN** a user with Provider role logs in successfully
- **AND** their Provider record has status = Inactive, Suspended, OR Archived
- **THEN** the user SHALL be redirected to an account status page explaining the restriction
- **AND** dashboard access SHALL be restricted

### Requirement: Provider Status Navigation Guard
The system SHALL enforce Provider status-based routing through navigation guards for all protected routes.

#### Scenario: Drafted provider accessing protected routes
- **WHEN** an authenticated user with Drafted Provider status attempts to navigate to any route other than Provider Registration
- **THEN** the navigation SHALL be intercepted
- **AND** the user SHALL be redirected to the Provider Registration Flow

#### Scenario: Provider Registration route access for completed profiles
- **WHEN** an authenticated user with status PendingVerification, Verified, or Active attempts to access Provider Registration route
- **THEN** the user SHALL be redirected to their Provider Dashboard
- **AND** access to the registration flow SHALL be denied

#### Scenario: Non-provider user accessing provider routes
- **WHEN** an authenticated user without Provider role attempts to access provider-specific routes
- **THEN** navigation SHALL be blocked
- **AND** the user SHALL be redirected to an unauthorized page or their appropriate dashboard

### Requirement: Get Current Provider Status API
The system SHALL provide an API endpoint to retrieve the authenticated user's Provider registration status.

#### Scenario: Get status for existing provider
- **WHEN** an authenticated Provider user requests their current status via GET /api/v1/providers/current/status
- **THEN** the system SHALL return HTTP 200 OK
- **AND** the response SHALL include the Provider status (Drafted, PendingVerification, Verified, Active, Inactive, Suspended, or Archived)
- **AND** the response SHALL include the Provider ID

#### Scenario: Get status for user without provider record
- **WHEN** an authenticated user with Provider role requests their status via GET /api/v1/providers/current/status
- **AND** no Provider record exists for the user
- **THEN** the system SHALL return HTTP 404 Not Found
- **AND** the response SHALL indicate no Provider record exists

#### Scenario: Unauthenticated user requests status
- **WHEN** an unauthenticated user attempts to access GET /api/v1/providers/current/status
- **THEN** the system SHALL return HTTP 401 Unauthorized

#### Scenario: Non-provider user requests status
- **WHEN** an authenticated user without Provider role requests status via GET /api/v1/providers/current/status
- **THEN** the system SHALL return HTTP 403 Forbidden
