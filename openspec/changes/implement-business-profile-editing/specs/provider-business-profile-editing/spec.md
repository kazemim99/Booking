## ADDED Requirements

### Requirement: Edit Business Name and Description

The app SHALL provide a business-profile screen (More → مشخصات کسب‌وکار) pre-filled with the current business name and description from the provider details endpoint. Saving MUST require a non-empty business name and persist via `PUT /Providers/business`; success confirms and returns, failure surfaces a message and preserves the edited values.

#### Scenario: Pre-filled edit saves
- **WHEN** the provider changes the description and saves
- **THEN** the update is persisted and a confirmation is shown

#### Scenario: Save is gated on the business name
- **WHEN** the business name field is emptied
- **THEN** the save action is disabled

#### Scenario: Failure preserves edits
- **WHEN** the save fails
- **THEN** an error is shown and the edited values remain in the form

### Requirement: Business Update Works Without the Provider Claim

The business-update endpoint SHALL resolve the caller's provider from ownership when the `providerId` JWT claim is absent (first session after onboarding), instead of failing. Callers with no provider at all receive a not-found error.

#### Scenario: First-session token can update
- **WHEN** a provider whose token predates registration updates their business info
- **THEN** the update succeeds via the ownership lookup
