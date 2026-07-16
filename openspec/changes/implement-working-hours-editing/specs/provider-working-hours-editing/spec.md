## ADDED Requirements

### Requirement: Weekly Hours Editing

The app SHALL provide a working-hours screen (More → ساعات کاری) listing the week Saturday-first, each day with an open/closed toggle and, when open, editable open/close times. It MUST pre-fill from the provider's current hours and persist the full week via the business-hours endpoint; success confirms and returns, failure preserves the edits.

#### Scenario: Closing a day persists
- **WHEN** the provider toggles Tuesday closed and saves
- **THEN** the persisted week shows Tuesday closed and other days unchanged

#### Scenario: Changing a time persists
- **WHEN** the provider changes Monday's opening time and saves
- **THEN** the persisted Monday reflects the new opening time

#### Scenario: Failure preserves edits
- **WHEN** the save fails
- **THEN** an error is shown and the edited week remains on screen

### Requirement: Break Preservation

Existing per-day breaks SHALL be displayed on the editor and MUST be included unchanged in the save payload, so saving hours never silently erases breaks. (Break editing is a separate change.)

#### Scenario: Breaks survive a save
- **WHEN** Monday has a 13:00–14:00 break and the provider saves after changing Friday
- **THEN** Monday's break is still present afterwards

### Requirement: Provider-Settings Writes Are Owner-Guarded

All mutating provider-settings endpoints (hours, business info, location, holidays, availability exceptions, service settings) SHALL reject callers who are neither the provider's owner nor an admin with 403, including first-session tokens resolved via the ownership fallback.

#### Scenario: Stranger cannot rewrite hours
- **WHEN** an authenticated user who does not own the provider PUTs its business-hours
- **THEN** the request is rejected with 403

#### Scenario: First-session owner can edit
- **WHEN** the owner's token predates registration (no provider claim)
- **THEN** their hours update succeeds via the ownership fallback
