## ADDED Requirements

### Requirement: Manage Services

The Services screen SHALL support adding a service (name required; duration in minutes and price numeric and positive; optional description), editing an existing one through a pre-filled form (round-tripping the description), and deleting one behind an explicit confirmation. Mutations refresh the list on success; failures surface a message and preserve the form's input.

#### Scenario: Add a service
- **WHEN** the provider submits a valid new service
- **THEN** it appears in the refreshed list with its duration and price

#### Scenario: Edit round-trips untouched fields
- **WHEN** the provider changes only the price and saves
- **THEN** the service keeps its name, duration, and description with the new price

#### Scenario: Delete requires confirmation
- **WHEN** the provider taps delete
- **THEN** nothing is removed until confirmed

#### Scenario: Form gating
- **WHEN** the name is empty or duration/price are not positive numbers
- **THEN** the submit action is disabled
