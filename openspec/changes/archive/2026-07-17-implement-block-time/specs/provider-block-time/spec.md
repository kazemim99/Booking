## ADDED Requirements

### Requirement: Block Time Via Availability Exceptions

The ⊕ create menu's «مسدود کردن زمان» SHALL open a sheet that creates a per-date availability exception: a required future-or-today date (pre-set to the Calendar's selected day when opened there), a mode — all-day closed or modified open/close hours — and a required reason. Success MUST refresh the host screen so availability reflects the block; failures surface a message and preserve input.

#### Scenario: All-day block
- **WHEN** the provider blocks a date as all-day closed with a reason
- **THEN** the exception is created and that date offers no bookable slots

#### Scenario: Modified-hours block
- **WHEN** the provider sets modified open/close hours for a date
- **THEN** slots outside those hours are not offered for that date

#### Scenario: Submit gating
- **WHEN** the reason is empty, or modified-hours mode lacks a time
- **THEN** the submit action is disabled

### Requirement: Exceptions Are Listed and Removable

The Holidays screen SHALL list availability exceptions (date, closed-or-hours, reason) beneath the days-off list, each removable behind an explicit confirmation.

#### Scenario: Removing an exception restores the day
- **WHEN** the provider confirms removal of an exception
- **THEN** it disappears from the list and the date's normal weekly hours apply again

### Requirement: Closed-Today Exception Reaches the Home

A closed-all-day exception dated today SHALL resolve the Home's `availability = closedToday` exactly like a holiday, with the same failure-degrades-to-open behavior.

#### Scenario: Today blocked all day
- **WHEN** today has a closed-all-day exception
- **THEN** the Home shows the closed-day composition
