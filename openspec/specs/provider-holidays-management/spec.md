# provider-holidays-management Specification

## Purpose
TBD - created by archiving change implement-holidays-management. Update Purpose after archive.
## Requirements
### Requirement: Manage Days Off

The app SHALL list the provider's holidays (date, reason, recurring marker) at More → تعطیلات و مرخصی, ordered soonest-first, and SHALL support adding a holiday (date required and not in the past, reason required, optional yearly recurrence) and removing one behind an explicit confirmation. Mutations refresh the list on success and surface failures without losing input.

#### Scenario: Add a day off
- **WHEN** the provider picks a future date, enters a reason, and submits
- **THEN** the holiday is created and appears in the refreshed list

#### Scenario: Remove requires confirmation
- **WHEN** the provider taps remove on a holiday
- **THEN** nothing is deleted until they confirm, and on confirmation the row disappears

### Requirement: Home Consumes Holidays as the Closed-Day State

The Home snapshot SHALL resolve `availability = closedToday` when today matches a holiday (exact date, or month/day for recurring ones), producing the designed closed-day Home: the closed banner shows, the agenda yields, and the coming-up peek elevates. Holiday lookup failures MUST degrade to `open` — a side-signal never blocks the operational Home.

#### Scenario: Today is a holiday
- **WHEN** the provider opens the Home on a date they marked as a holiday
- **THEN** the closed-today banner is shown and the day agenda is not rendered

#### Scenario: Recurring holiday matches annually
- **WHEN** a recurring holiday's month/day equals today's
- **THEN** the Home resolves closed-today regardless of year

#### Scenario: Holiday lookup failure degrades open
- **WHEN** the holidays call fails
- **THEN** the Home resolves as a normal open day

