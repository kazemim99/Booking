## ADDED Requirements

### Requirement: Edit Weekly Breaks

The hours editor SHALL allow adding a break to any open day (a start/end time pair) and removing an existing break, with both edits held as unsaved state until the week is saved — the save request carries the edited breaks exactly.

#### Scenario: Add a lunch break
- **WHEN** the provider adds a 13:00–14:00 break to Monday and saves
- **THEN** the saved week's Monday carries that break

#### Scenario: Remove a break
- **WHEN** the provider deletes a break chip and saves
- **THEN** the saved week's day no longer carries it

#### Scenario: Closed days offer no break editing
- **WHEN** a day is toggled closed
- **THEN** no break affordances are shown for it
