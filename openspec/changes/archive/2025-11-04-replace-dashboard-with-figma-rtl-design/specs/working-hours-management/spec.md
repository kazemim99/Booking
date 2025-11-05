# Working Hours Management Specification - Extensions

## ADDED Requirements

### Requirement: Multiple Daily Breaks Support
The system SHALL support multiple break periods within a single business day.

#### Scenario: Multiple breaks can be configured per day
- **WHEN** a provider configures business hours for a day
- **THEN** they can add multiple break periods
- **AND** each break has start and end times
- **AND** breaks are validated to not overlap with each other or working hours

#### Scenario: Breaks are persisted and retrieved
- **WHEN** breaks are saved
- **THEN** they are stored in the database
- **AND** they are retrieved when loading business hours
- **AND** they are returned in the API response

### Requirement: Jalali Calendar Integration
The system SHALL support Jalali (Persian) calendar dates for special day scheduling.

#### Scenario: Jalali dates are accepted and converted
- **WHEN** special day schedules are created with Jalali dates
- **THEN** dates are correctly converted to Gregorian for storage
- **AND** dates are converted back to Jalali when retrieved
- **AND** conversion is accurate and handles leap years correctly

#### Scenario: Exception schedules support Jalali dates
- **WHEN** creating custom schedules for specific dates
- **THEN** Jalali calendar dates can be selected
- **AND** the system handles date conversions transparently
- **AND** stored dates maintain data integrity

