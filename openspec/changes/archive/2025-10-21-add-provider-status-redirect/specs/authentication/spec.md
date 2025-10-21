# Authentication

This specification defines authentication flow enhancements for Provider status-aware routing.

## MODIFIED Requirements

### Requirement: Post-Login Redirect
The system SHALL redirect authenticated users to appropriate destinations based on their roles and Provider status (for Provider users).

#### Scenario: Admin user login
- **WHEN** a user with Admin, Administrator, or SysAdmin role logs in successfully
- **THEN** the user SHALL be redirected to /admin/dashboard

#### Scenario: Provider user login with complete profile
- **WHEN** a user with Provider or ServiceProvider role logs in successfully
- **AND** their Provider status is Verified OR Active
- **THEN** the user SHALL be redirected to /provider/dashboard

#### Scenario: Provider user login with incomplete profile
- **WHEN** a user with Provider or ServiceProvider role logs in successfully
- **AND** their Provider status is Drafted OR no Provider record exists
- **THEN** the user SHALL be redirected to the Provider Registration Flow

#### Scenario: Provider user login with pending verification
- **WHEN** a user with Provider or ServiceProvider role logs in successfully
- **AND** their Provider status is PendingVerification
- **THEN** the user SHALL be redirected to /provider/dashboard
- **AND** a pending verification status message SHALL be displayed

#### Scenario: Customer user login
- **WHEN** a user with Customer or Client role logs in successfully
- **THEN** the user SHALL be redirected to /customer/dashboard

#### Scenario: User with no recognized role
- **WHEN** a user with no recognized role logs in successfully
- **THEN** the user SHALL be redirected to the home page (/)

## ADDED Requirements

### Requirement: Provider Status Storage
The system SHALL store Provider status information in the authentication state after successful login.

#### Scenario: Store provider status on login
- **WHEN** a user with Provider role logs in successfully
- **THEN** the system SHALL fetch the Provider status from the backend
- **AND** the Provider status SHALL be stored in the authentication store
- **AND** the Provider ID SHALL be stored if a Provider record exists

#### Scenario: Update provider status
- **WHEN** a provider completes their registration flow
- **THEN** the authentication store SHALL update the stored Provider status
- **AND** the user SHALL be redirected according to the new status
