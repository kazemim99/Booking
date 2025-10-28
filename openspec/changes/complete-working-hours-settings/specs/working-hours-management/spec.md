# working-hours-management Specification

## Purpose
Comprehensive working hours management for providers including regular hours, holidays, exceptions, breaks, and calendar visualization.

## ADDED Requirements

### Requirement: Base Business Hours Configuration
The system SHALL allow providers to configure regular weekly operating hours with breaks.

#### Scenario: Set daily operating hours
- **WHEN** a provider sets operating hours for a day
- **THEN** the system allows selecting open and close times
- **AND** validates that open time is before close time
- **AND** allows marking the day as closed
- **AND** supports 24-hour operation (open time = close time next day)
- **AND** displays total hours per day
- **AND** calculates weekly hours total

#### Scenario: Configure break periods
- **WHEN** a provider adds break periods to a day
- **THEN** the system allows setting break start and end times
- **AND** allows multiple breaks per day (lunch, afternoon break)
- **AND** validates breaks fall within operating hours
- **AND** prevents overlapping breaks
- **AND** allows labeling breaks (e.g., "Lunch Break")
- **AND** subtracts break time from available booking hours
- **AND** displays net available hours after breaks

#### Scenario: Copy schedule across days
- **WHEN** a provider copies schedule from one day to others
- **THEN** the system allows selecting source day
- **AND** allows multi-selecting target days
- **AND** copies hours, breaks, and settings together
- **AND** confirms action before applying
- **AND** shows preview of affected days
- **AND** allows individual adjustments after copy

### Requirement: Holiday Management
The system SHALL allow providers to mark specific dates as closed for holidays.

#### Scenario: Mark single date as holiday
- **WHEN** a provider marks a date as holiday
- **THEN** the system allows selecting date from calendar
- **AND** requires entering holiday reason/name
- **AND** marks the entire day as unavailable
- **AND** displays holiday visually on calendar
- **AND** prevents bookings on that date
- **AND** warns if existing bookings exist

#### Scenario: Create recurring holiday
- **WHEN** a provider creates recurring holiday
- **THEN** the system allows selecting recurrence pattern (yearly, monthly, weekly)
- **AND** shows preview of dates affected by recurrence
- **AND** allows setting recurrence end date or count
- **AND** displays all occurrences on calendar
- **AND** allows editing individual occurrences
- **AND** allows removing individual occurrences from pattern

#### Scenario: Manage holiday list
- **WHEN** a provider views holidays
- **THEN** the system displays chronological list of upcoming holidays
- **AND** shows past holidays in history
- **AND** allows filtering by year or date range
- **AND** allows editing holiday details
- **AND** allows deleting holidays with confirmation
- **AND** shows count of bookings affected by deletion

### Requirement: Exception Schedule Management
The system SHALL allow providers to set temporary schedule overrides for specific dates.

#### Scenario: Set exception hours for date
- **WHEN** a provider sets exception hours
- **THEN** the system allows selecting specific date
- **AND** allows setting different open/close times for that date
- **AND** allows marking date as closed (exception closure)
- **AND** requires entering reason for exception
- **AND** overrides regular hours for that date
- **AND** displays exception visually distinct from regular hours

#### Scenario: Set exception for date range
- **WHEN** a provider sets exception for multiple dates
- **THEN** the system allows selecting start and end date
- **AND** applies same exception hours to all dates in range
- **AND** allows different hours per weekday within range
- **AND** excludes dates that are already holidays
- **AND** shows count of days affected
- **AND** confirms action before applying

#### Scenario: View and manage exceptions
- **WHEN** a provider views exception list
- **THEN** the system displays upcoming exceptions chronologically
- **AND** shows past exceptions in history
- **AND** indicates dates with bookings
- **AND** allows editing exception details
- **AND** allows deleting exceptions with confirmation
- **AND** warns if deletion enables bookings in past dates

### Requirement: Calendar Visualization
The system SHALL provide visual calendar interface for managing hours.

#### Scenario: Week calendar view
- **WHEN** a provider views week calendar
- **THEN** the system displays 7-day grid with hours
- **AND** shows regular hours for each day
- **AND** visually indicates breaks within hours
- **AND** highlights holidays and exceptions
- **AND** uses color coding (open=green, closed=gray, exception=yellow, holiday=red)
- **AND** allows clicking day to edit hours
- **AND** shows tooltips with details on hover

#### Scenario: Month calendar view
- **WHEN** a provider views month calendar
- **THEN** the system displays month grid with all dates
- **AND** indicates days with regular hours (green)
- **AND** indicates holidays (red) and exceptions (yellow)
- **AND** shows closed days (gray)
- **AND** displays booking count per day
- **AND** allows clicking date to view/edit details
- **AND** allows navigating between months

#### Scenario: Calendar interaction
- **WHEN** a provider interacts with calendar
- **THEN** the system allows clicking date cell to edit
- **AND** opens edit panel or modal with date pre-selected
- **AND** shows current schedule for that date
- **AND** allows quick toggle open/closed
- **AND** saves changes immediately or on confirm
- **AND** updates calendar display in real-time

### Requirement: Availability Calculation
The system SHALL accurately calculate provider availability considering all schedule layers.

#### Scenario: Calculate availability for date
- **WHEN** the system calculates availability for a date
- **THEN** it checks if date is marked as holiday (highest priority)
- **AND** if holiday, returns unavailable with reason
- **AND** if not holiday, checks for exception schedule
- **AND** if exception exists, uses exception hours instead of regular
- **AND** if no exception, uses regular hours for that weekday
- **AND** subtracts break periods from available hours
- **AND** returns time slots available for booking

#### Scenario: Multi-day availability query
- **WHEN** customer or provider queries availability for date range
- **THEN** the system calculates availability for each date
- **AND** returns map of date to availability windows
- **AND** performs calculation efficiently (cached when possible)
- **AND** includes reason when unavailable (holiday, exception, closed)
- **AND** handles large date ranges (months) performantly
- **AND** updates cache when schedule changes

### Requirement: Booking Conflict Detection
The system SHALL detect conflicts when schedule changes affect existing bookings.

#### Scenario: Detect conflicts on schedule change
- **WHEN** a provider modifies schedule (hours, holiday, exception)
- **THEN** the system checks for existing bookings on affected dates
- **AND** identifies bookings that fall outside new availability
- **AND** identifies bookings that fall within new break times
- **AND** displays count of affected bookings
- **AND** provides list of affected bookings with details
- **AND** requires confirmation before saving conflicting changes

#### Scenario: Preview affected bookings
- **WHEN** provider views affected bookings
- **THEN** the system displays booking details (customer, service, time)
- **AND** highlights conflict reason (outside hours, in break, closed)
- **AND** shows booking status (upcoming, past, cancelled)
- **AND** allows bulk actions (reschedule all, cancel all, notify)
- **AND** allows individual booking actions
- **AND** tracks which bookings were notified

#### Scenario: Resolve booking conflicts
- **WHEN** provider confirms schedule change with conflicts
- **THEN** the system applies the schedule change
- **AND** marks affected bookings as "schedule_changed"
- **AND** generates notification for each affected customer
- **AND** logs conflict resolution in audit trail
- **AND** allows provider to undo change within time window
- **AND** provides bulk reschedule tool for provider

### Requirement: Customer Notification
The system SHALL notify customers when schedule changes affect their bookings.

#### Scenario: Notify customer of schedule change
- **WHEN** schedule change creates booking conflict
- **THEN** the system sends notification to customer
- **AND** includes what changed (closed, different hours, break)
- **AND** includes how it affects their booking
- **AND** provides options (reschedule, cancel, contact provider)
- **AND** includes deadline for action if required
- **AND** sends via customer's preferred channels
- **AND** logs notification delivery

#### Scenario: Batch notification for multiple customers
- **WHEN** schedule change affects multiple bookings
- **THEN** the system batches notifications by customer
- **AND** combines multiple affected bookings in one message
- **AND** prioritizes by booking date (soonest first)
- **AND** includes all affected dates and times
- **AND** avoids duplicate notifications
- **AND** tracks delivery and read status

### Requirement: Quick Actions and Templates
The system SHALL provide quick actions for common schedule operations.

#### Scenario: Apply schedule template
- **WHEN** a provider applies schedule template
- **THEN** the system offers predefined templates (9-5 weekdays, 24/7, etc.)
- **AND** shows preview of template schedule
- **AND** allows customization before applying
- **AND** applies to all days or selected days
- **AND** preserves existing exceptions and holidays
- **AND** confirms action before applying

#### Scenario: Bulk schedule operations
- **WHEN** a provider performs bulk schedule change
- **THEN** the system allows selecting multiple dates
- **AND** allows applying same hours to all selected
- **AND** allows marking all selected as holiday
- **AND** allows clearing schedule for all selected
- **AND** shows count of days affected
- **AND** checks for booking conflicts across all days
- **AND** requires confirmation with conflict summary

### Requirement: Mobile-Optimized Interface
The system SHALL provide mobile-friendly interface for schedule management.

#### Scenario: Mobile calendar view
- **WHEN** provider views calendar on mobile
- **THEN** the system displays single day per screen in week view
- **AND** allows swiping to navigate between days
- **AND** shows mini month calendar for date selection
- **AND** uses touch-friendly controls (min 44x44px)
- **AND** supports long-press for additional options
- **AND** adapts layout to screen size
- **AND** maintains feature parity with desktop

#### Scenario: Mobile hour editing
- **WHEN** provider edits hours on mobile
- **THEN** the system shows bottom sheet or modal
- **AND** uses native time pickers when available
- **AND** provides large tap targets for time selection
- **AND** allows quick open/closed toggle
- **AND** displays changes immediately in calendar
- **AND** supports swipe gestures for common actions
- **AND** validates input before saving

### Requirement: Audit Trail and History
The system SHALL maintain complete history of schedule changes for auditing.

#### Scenario: Log schedule changes
- **WHEN** provider modifies schedule
- **THEN** the system logs change with timestamp
- **AND** records user who made change
- **AND** stores previous values (before change)
- **AND** stores new values (after change)
- **AND** records reason if provided
- **AND** logs affected booking count
- **AND** retains logs for compliance period

#### Scenario: View schedule history
- **WHEN** provider views schedule history
- **THEN** the system displays chronological log
- **AND** shows what changed (hours, holiday, exception, break)
- **AND** shows who made change
- **AND** allows filtering by date range or change type
- **AND** allows searching log
- **AND** allows exporting history for auditing
- **AND** displays trend of schedule changes over time
