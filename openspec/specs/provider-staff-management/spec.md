# provider-staff-management Specification

## Purpose
TBD - created by archiving change implement-staff-management. Update Purpose after archive.
## Requirements
### Requirement: Add a Team Member

The staff screen SHALL offer an add action opening a form with first name (required), last name, phone, and role; submitting creates the member via `POST /Providers/{id}/staff` and the list MUST refresh to include them. Failures surface a message and preserve the form input.

#### Scenario: Successful add
- **WHEN** the provider submits the form with a first name
- **THEN** the member is created and appears in the refreshed list

#### Scenario: Submit is gated on the required name
- **WHEN** the first name is empty
- **THEN** the submit action is disabled

#### Scenario: Failure preserves input
- **WHEN** creation fails
- **THEN** an error message is shown and the entered values remain editable

### Requirement: Edit and Remove a Team Member

Tapping a member SHALL open the same form pre-filled; submitting updates via `PUT /Providers/{id}/staff/{staffId}`. The edit form MUST offer a remove action guarded by an explicit confirmation, deleting via `DELETE /Providers/{id}/staff/{staffId}`. Both refresh the list on success.

#### Scenario: Edit pre-fills and saves
- **WHEN** the provider opens a member and changes the role
- **THEN** the form opened pre-filled and the updated role shows in the refreshed list

#### Scenario: Remove requires confirmation
- **WHEN** the provider taps remove
- **THEN** nothing is deleted until they confirm
- **AND** on confirmation the member disappears from the list

### Requirement: Activation Checklist Path

The Setup checklist's staff item SHALL navigate to the staff screen (no more "coming soon"), so a new provider can complete «افزودن اعضای تیم» from the Home.

#### Scenario: Checklist opens staff management
- **WHEN** a Setup-phase provider taps the checklist's staff item
- **THEN** the staff screen opens with the add action available

