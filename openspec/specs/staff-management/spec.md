# staff-management Specification

## Purpose
TBD - created by archiving change complete-business-profile. Update Purpose after archive.
## Requirements
### Requirement: Staff Directory Display
The system SHALL display all staff members in an organized directory with comprehensive information.

#### Scenario: Staff list display
- **WHEN** a provider views their staff directory
- **THEN** the system displays all staff members in a card-based layout
- **AND** shows profile photo, name, role, and active status for each member
- **AND** indicates which staff members are currently active vs inactive
- **AND** displays assigned services count and schedule summary per staff member

#### Scenario: Staff filtering and search
- **WHEN** a provider searches or filters staff
- **THEN** the system allows filtering by role, active status, and assigned services
- **AND** provides text search for staff name and email
- **AND** updates results in real-time
- **AND** displays count of filtered results

### Requirement: Staff Member Creation
The system SHALL allow providers to add new staff members with complete profile information.

#### Scenario: Add new staff member
- **WHEN** a provider adds a new staff member
- **THEN** the system displays a form requiring first name, last name, and email
- **AND** allows optional phone number, role selection, and hire date
- **AND** validates email format and uniqueness within the provider
- **AND** creates staff member in active status by default
- **AND** sends optional invitation email to staff member

#### Scenario: Staff profile photo
- **WHEN** a provider uploads a staff profile photo
- **THEN** the system provides image cropping for square aspect ratio (1:1)
- **AND** validates file size (max 5MB) and format
- **AND** generates optimized versions for different display contexts
- **AND** displays preview of uploaded photo

#### Scenario: Staff role assignment
- **WHEN** a provider assigns a role to staff
- **THEN** the system provides predefined roles (Owner, Manager, Stylist, Specialist, Receptionist, Other)
- **AND** allows custom role creation for flexibility
- **AND** displays role clearly in staff profile and listings
- **AND** uses role for service qualification filtering

### Requirement: Staff Member Editing
The system SHALL allow providers to update staff member information and settings.

#### Scenario: Edit staff contact information
- **WHEN** a provider edits staff contact info
- **THEN** the system allows updating email and phone number
- **AND** validates new email format and uniqueness
- **AND** requires confirmation for email changes
- **AND** maintains audit trail of contact information changes

#### Scenario: Update staff role
- **WHEN** a provider changes a staff member's role
- **THEN** the system displays confirmation dialog
- **AND** updates service assignments if new role is not qualified
- **AND** notifies affected staff member of role change
- **AND** maintains history of role changes

#### Scenario: Staff notes and biography
- **WHEN** a provider manages staff notes
- **THEN** the system allows private internal notes (not visible to customers)
- **AND** allows public biography/description (visible on profile)
- **AND** provides character limits (notes: 1000, bio: 500)
- **AND** displays character count in real-time

### Requirement: Staff Service Assignment
The system SHALL allow providers to assign staff members to services they can perform.

#### Scenario: Assign services to staff
- **WHEN** a provider assigns services to a staff member
- **THEN** the system displays all available services in a searchable list
- **AND** allows multi-select of services
- **AND** shows which services are already assigned
- **AND** updates immediately and reflects in booking availability

#### Scenario: Service qualification levels
- **WHEN** a provider sets service qualification levels
- **THEN** the system allows marking staff as primary or backup for each service
- **AND** prioritizes primary staff in booking suggestions
- **AND** uses backup staff when primary unavailable
- **AND** displays qualification level clearly in assignments

### Requirement: Staff Schedule Management
The system SHALL allow providers to define and manage staff working schedules.

#### Scenario: Set staff working hours
- **WHEN** a provider sets staff working hours
- **THEN** the system displays a weekly schedule grid for the staff member
- **AND** allows different hours per day of week
- **AND** supports break times within working hours
- **AND** validates working hours are within business operating hours
- **AND** displays conflicts with business hours

#### Scenario: Copy schedule across days
- **WHEN** a provider copies a staff schedule
- **THEN** the system allows copying one day's schedule to selected days
- **AND** allows applying same schedule to all weekdays
- **AND** prompts for confirmation before overwriting existing schedules
- **AND** displays preview of changes before applying

#### Scenario: Staff time-off and absences
- **WHEN** a provider manages staff time-off
- **THEN** the system allows adding vacation, sick leave, or other absence types
- **AND** requires date range selection for time-off period
- **AND** blocks booking slots during time-off period
- **AND** warns about existing bookings during requested time-off
- **AND** displays time-off on staff calendar view

#### Scenario: Recurring schedule patterns
- **WHEN** a provider sets recurring schedule patterns
- **THEN** the system allows defining patterns (e.g., "every other weekend off")
- **AND** generates schedule based on pattern for specified date range
- **AND** allows overriding individual dates within pattern
- **AND** displays upcoming schedule based on pattern

### Requirement: Staff Availability View
The system SHALL provide visual calendar views of staff availability.

#### Scenario: Weekly staff availability calendar
- **WHEN** a provider views staff availability calendar
- **THEN** the system displays a week grid with all staff members
- **AND** shows working hours, breaks, and time-off for each staff
- **AND** highlights conflicts and gaps in coverage
- **AND** allows clicking time slots to view or edit details
- **AND** provides navigation to previous/next weeks

#### Scenario: Individual staff calendar
- **WHEN** a provider views individual staff calendar
- **THEN** the system displays the staff member's schedule for current week/month
- **AND** shows bookings, working hours, breaks, and time-off
- **AND** indicates available vs booked time slots
- **AND** allows adding time-off or editing schedule directly from calendar
- **AND** displays different view modes (day, week, month)

### Requirement: Staff Status Management
The system SHALL allow providers to activate, deactivate, and manage staff member status.

#### Scenario: Deactivate staff member
- **WHEN** a provider deactivates a staff member
- **THEN** the system requires a reason for deactivation
- **AND** allows setting termination date
- **AND** warns about future bookings assigned to the staff
- **AND** provides option to reassign bookings to other staff
- **AND** maintains staff data and history after deactivation

#### Scenario: Reactivate staff member
- **WHEN** a provider reactivates a previously deactivated staff member
- **THEN** the system restores the staff member to active status
- **AND** clears termination date and reason
- **AND** restores previous service assignments and schedules
- **AND** makes staff available for new bookings
- **AND** logs reactivation with timestamp and reason

### Requirement: Staff Performance Metrics
The system SHALL provide insights into staff member performance and utilization.

#### Scenario: Staff performance dashboard
- **WHEN** a provider views staff performance metrics
- **THEN** the system displays booking count per staff member
- **AND** shows revenue generated by each staff member
- **AND** calculates utilization rate (booked hours / available hours)
- **AND** displays customer ratings and reviews per staff
- **AND** highlights top performers and underutilized staff

#### Scenario: Staff booking history
- **WHEN** a provider views staff booking history
- **THEN** the system displays past bookings for the staff member
- **AND** shows booking status (completed, cancelled, no-show)
- **AND** provides filtering by date range and service
- **AND** displays total revenue and customer count
- **AND** allows exporting booking history as CSV

### Requirement: Bulk Staff Operations
The system SHALL support bulk operations for managing multiple staff members efficiently.

#### Scenario: Bulk schedule update
- **WHEN** a provider applies bulk schedule changes
- **THEN** the system allows selecting multiple staff members
- **AND** allows applying same schedule to all selected staff
- **AND** displays preview of changes before applying
- **AND** requires confirmation with list of affected staff
- **AND** processes changes with progress indicator

#### Scenario: Bulk service assignment
- **WHEN** a provider assigns services to multiple staff
- **THEN** the system allows selecting multiple staff members
- **AND** displays available services for bulk assignment
- **AND** allows adding or removing services from all selected staff
- **AND** shows confirmation with summary of changes
- **AND** applies changes atomically with success/failure summary

### Requirement: Staff Communication
The system SHALL provide tools for communicating with staff members.

#### Scenario: Send notification to staff
- **WHEN** a provider sends a notification to staff
- **THEN** the system allows selecting individual or multiple staff recipients
- **AND** supports email and in-app notification delivery
- **AND** allows message composition with subject and body
- **AND** provides message templates for common notifications
- **AND** logs all sent notifications with delivery status

#### Scenario: Staff invitation and onboarding
- **WHEN** a provider invites a staff member to the platform
- **THEN** the system sends an invitation email with registration link
- **AND** creates pending staff account awaiting confirmation
- **AND** provides onboarding checklist for new staff to complete
- **AND** allows resending invitation if not accepted
- **AND** displays invitation status (pending, accepted, expired)

