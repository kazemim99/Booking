# Staff Management Spec Deltas

## ADDED Requirements

### Requirement: Staff Detail View
The system SHALL provide a detailed view showing complete staff member information.

#### Scenario: View staff member details
- **WHEN** a provider clicks on a staff member card
- **THEN** the system displays a detailed view with all staff information
- **AND** shows profile photo, name, role, contact information, and status
- **AND** displays hire date and tenure calculation
- **AND** shows assigned services with qualification levels
- **AND** displays weekly schedule summary
- **AND** shows biography and internal notes sections
- **AND** provides action buttons for edit, deactivate, and delete

#### Scenario: View staff performance summary
- **WHEN** a provider views staff details
- **THEN** the system displays key performance indicators
- **AND** shows total bookings count for current month
- **AND** displays revenue generated (if applicable)
- **AND** shows customer rating average
- **AND** provides link to full performance dashboard

### Requirement: Staff Profile Photo Management
The system SHALL allow providers to upload and manage staff profile photos.

#### Scenario: Upload staff profile photo
- **WHEN** a provider uploads a staff profile photo during creation or editing
- **THEN** the system validates file type (JPG, PNG, WebP, max 5MB)
- **AND** provides image cropping interface with 1:1 aspect ratio
- **AND** generates optimized versions (thumbnail 100x100, display 400x400)
- **AND** stores images using the existing image storage service
- **AND** updates staff profile with new photo URL

#### Scenario: Remove staff profile photo
- **WHEN** a provider removes a staff profile photo
- **THEN** the system deletes all photo versions from storage
- **AND** reverts to default avatar placeholder
- **AND** maintains photo change history for audit

### Requirement: Extended Staff Profile Fields
The system SHALL support extended staff profile information beyond basic contact details.

#### Scenario: Manage staff biography
- **WHEN** a provider edits staff biography
- **THEN** the system allows up to 500 characters for public biography
- **AND** displays character counter in real-time
- **AND** shows biography on customer-facing staff profile
- **AND** supports basic text formatting (line breaks)

#### Scenario: Manage internal notes
- **WHEN** a provider edits internal notes
- **THEN** the system allows up to 1000 characters for private notes
- **AND** notes are only visible to provider and managers
- **AND** displays character counter in real-time
- **AND** maintains note edit history with timestamps

#### Scenario: Set hire date
- **WHEN** a provider sets or updates hire date
- **THEN** the system validates date is not in the future
- **AND** calculates and displays tenure automatically
- **AND** uses hire date for anniversary notifications
- **AND** displays tenure in staff list and detail views

### Requirement: Staff API Endpoints
The system SHALL provide RESTful API endpoints for all staff CRUD operations.

#### Scenario: Get staff member by ID
- **WHEN** a GET request is made to `/api/v1/providers/{providerId}/staff/{staffId}`
- **THEN** the system returns complete staff details including profile, schedule, and services
- **AND** returns 200 OK with staff data
- **AND** returns 404 Not Found if staff does not exist
- **AND** returns 403 Forbidden if user cannot access this provider's staff

#### Scenario: Create staff member
- **WHEN** a POST request is made to `/api/v1/providers/{providerId}/staff` with firstName, lastName, and optional fields
- **THEN** the system validates all required fields
- **AND** creates staff member with IsActive=true by default
- **AND** sets HiredAt to current date if not provided
- **AND** returns 201 Created with created staff data
- **AND** returns 400 Bad Request for validation errors
- **AND** returns 409 Conflict if email already exists for this provider

#### Scenario: Update staff member
- **WHEN** a PUT request is made to `/api/v1/providers/{providerId}/staff/{staffId}` with updated fields
- **THEN** the system validates updated fields
- **AND** updates only provided fields (partial update)
- **AND** maintains audit trail of changes
- **AND** returns 200 OK with updated staff data
- **AND** returns 404 Not Found if staff does not exist
- **AND** returns 400 Bad Request for validation errors

#### Scenario: Delete/deactivate staff member
- **WHEN** a DELETE request is made to `/api/v1/providers/{providerId}/staff/{staffId}` with optional deactivation reason
- **THEN** the system marks staff as IsActive=false
- **AND** sets TerminatedAt to current timestamp
- **AND** records TerminationReason if provided
- **AND** does NOT permanently delete staff record
- **AND** returns 204 No Content on success
- **AND** returns 404 Not Found if staff does not exist

#### Scenario: Reactivate staff member
- **WHEN** a POST request is made to `/api/v1/providers/{providerId}/staff/{staffId}/activate`
- **THEN** the system sets IsActive=true
- **AND** clears TerminatedAt and TerminationReason
- **AND** restores previous schedule and service assignments
- **AND** returns 200 OK with reactivated staff data
- **AND** returns 404 Not Found if staff does not exist
- **AND** returns 400 Bad Request if staff is already active

### Requirement: Staff Photo Upload Endpoint
The system SHALL provide API endpoint for uploading staff profile photos.

#### Scenario: Upload staff profile photo via API
- **WHEN** a POST request is made to `/api/v1/providers/{providerId}/staff/{staffId}/photo` with multipart form data
- **THEN** the system validates file is an image (JPG, PNG, WebP)
- **AND** validates file size does not exceed 5MB
- **AND** generates thumbnail (100x100) and display (400x400) versions
- **AND** stores images in provider's staff folder structure
- **AND** updates Staff.ProfilePhotoUrl with new URL
- **AND** returns 200 OK with photo URLs
- **AND** returns 400 Bad Request for invalid files
- **AND** returns 413 Payload Too Large for oversized files

## MODIFIED Requirements

### Requirement: Staff Member Creation
The system SHALL allow providers to add new staff members with complete profile information.

#### Scenario: Add new staff member with extended fields
- **WHEN** a provider adds a new staff member
- **THEN** the system displays a comprehensive form requiring firstName and lastName
- **AND** allows optional email, phoneNumber, countryCode fields
- **AND** provides role selection dropdown with predefined roles (Owner, Manager, Stylist, Specialist, Receptionist, Other)
- **AND** allows custom role entry for flexibility
- **AND** includes hire date picker (defaults to today)
- **AND** provides biography text area (500 char max) for public description
- **AND** provides notes text area (1000 char max) for internal use
- **AND** includes profile photo upload with 1:1 crop interface
- **AND** validates email format and uniqueness within provider
- **AND** validates phone number format if provided
- **AND** creates staff member with IsActive=true by default
- **AND** optionally sends invitation email if email provided
- **AND** displays success message with staff name on creation

#### Scenario: Staff profile photo upload during creation
- **WHEN** a provider uploads a staff profile photo during creation
- **THEN** the system provides image cropping interface for 1:1 aspect ratio
- **AND** validates file size (max 5MB) and format (JPG, PNG, WebP)
- **AND** generates thumbnail (100x100) and display (400x400) versions
- **AND** stores optimized versions using image storage service
- **AND** displays preview of cropped photo before creation
- **AND** includes photo URL in staff creation request

#### Scenario: Staff role assignment during creation
- **WHEN** a provider assigns a role to new staff during creation
- **THEN** the system provides predefined roles dropdown (Owner, Manager, Stylist, Specialist, Receptionist, Cleaner, Security, Maintenance, ServiceProvider, Assistant, Other)
- **AND** allows selecting "Custom" to enter custom role name
- **AND** stores role as string in Staff.Role field
- **AND** displays role badge with appropriate color in staff list
- **AND** uses role for default permissions and filtering

#### Scenario: Validation during staff creation
- **WHEN** a provider submits the staff creation form
- **THEN** the system validates firstName is not empty (required)
- **AND** validates lastName is not empty (required)
- **AND** validates email format if provided (optional)
- **AND** validates email is unique within provider if provided
- **AND** validates phone number format if provided (optional)
- **AND** validates biography length does not exceed 500 characters
- **AND** validates notes length does not exceed 1000 characters
- **AND** validates hire date is not in the future
- **AND** displays field-level error messages for validation failures
- **AND** prevents submission until all validation passes

### Requirement: Staff Member Editing
The system SHALL allow providers to update staff member information and settings.

#### Scenario: Edit staff contact and profile information
- **WHEN** a provider edits staff member via detail view or edit modal
- **THEN** the system pre-populates form with current staff data
- **AND** allows updating firstName, lastName, email, phoneNumber, countryCode
- **AND** allows changing role selection
- **AND** allows updating hire date
- **AND** allows editing biography (500 char max)
- **AND** allows editing notes (1000 char max)
- **AND** allows uploading new profile photo
- **AND** validates email format and uniqueness on change
- **AND** validates phone number format if changed
- **AND** requires confirmation for email changes
- **AND** displays character counters for biography and notes
- **AND** shows unsaved changes warning if navigating away
- **AND** maintains audit trail of all changes with timestamps

#### Scenario: Update staff profile photo
- **WHEN** a provider updates staff profile photo
- **THEN** the system displays current photo with "Change Photo" button
- **AND** opens image cropping interface on photo selection
- **AND** maintains 1:1 aspect ratio for cropping
- **AND** generates new optimized versions
- **AND** deletes previous photo versions from storage
- **AND** updates Staff.ProfilePhotoUrl immediately
- **AND** shows updated photo in UI without page refresh

#### Scenario: Update staff role
- **WHEN** a provider changes staff member's role
- **THEN** the system displays role selection dropdown
- **AND** allows selecting from predefined roles or custom
- **AND** shows confirmation dialog for role changes
- **AND** updates role immediately upon confirmation
- **AND** may affect service assignments if new role is not qualified
- **AND** notifies staff member of role change if email configured
- **AND** maintains role change history for audit

### Requirement: Staff Status Management
The system SHALL allow providers to activate, deactivate, and manage staff member status with proper workflows.

#### Scenario: Deactivate staff member with reason
- **WHEN** a provider deactivates a staff member
- **THEN** the system displays a deactivation dialog requiring reason selection
- **AND** provides reason options (Resignation, Terminated, EndOfContract, Retirement, Relocation, CareerChange, Disciplinary, Restructuring, TemporaryLeave, Other)
- **AND** allows optional termination date (defaults to today)
- **AND** allows optional notes for additional context
- **AND** warns about future bookings assigned to the staff
- **AND** provides option to reassign bookings to other staff
- **AND** blocks creation of new bookings for deactivated staff
- **AND** sets IsActive=false and TerminatedAt timestamp
- **AND** records TerminationReason in staff record
- **AND** maintains full staff data and history after deactivation
- **AND** hides deactivated staff from default list view
- **AND** shows deactivated staff in "Show Inactive" filter

#### Scenario: Reactivate deactivated staff member
- **WHEN** a provider reactivates a previously deactivated staff member
- **THEN** the system displays reactivation confirmation dialog
- **AND** shows previous termination date and reason
- **AND** allows optional reactivation notes
- **AND** restores staff to IsActive=true status
- **AND** clears TerminatedAt and TerminationReason fields
- **AND** restores previous service assignments
- **AND** restores previous working schedule
- **AND** makes staff available for new bookings immediately
- **AND** logs reactivation with timestamp and reason
- **AND** displays success message with staff name

#### Scenario: View staff status history
- **WHEN** a provider views staff status history
- **THEN** the system displays timeline of status changes
- **AND** shows activation/deactivation events with dates
- **AND** displays reasons for each status change
- **AND** shows who performed each status change
- **AND** allows filtering history by date range
- **AND** supports exporting history as CSV

### Requirement: Frontend Staff Form Validation
The system SHALL provide comprehensive client-side validation for staff forms.

#### Scenario: Real-time form validation
- **WHEN** a provider fills out staff creation or edit form
- **THEN** the system validates firstName field on blur (required)
- **AND** validates lastName field on blur (required)
- **AND** validates email format on blur if provided
- **AND** validates phone number format on blur if provided
- **AND** displays field-level error messages immediately
- **AND** disables submit button until all required fields are valid
- **AND** shows character count for biography (500 max)
- **AND** shows character count for notes (1000 max)
- **AND** prevents exceeding character limits for text fields
- **AND** highlights invalid fields with red border
- **AND** shows checkmark for valid fields

#### Scenario: Form submission validation
- **WHEN** a provider submits staff form
- **THEN** the system performs final validation of all fields
- **AND** shows loading state on submit button
- **AND** disables form inputs during submission
- **AND** displays API validation errors if returned
- **AND** scrolls to first error field if validation fails
- **AND** shows success message and closes form on success
- **AND** refreshes staff list to show new/updated staff

### Requirement: Staff List Filtering and Search
The system SHALL provide comprehensive filtering and search capabilities for staff list.

#### Scenario: Filter staff by status
- **WHEN** a provider uses status filter
- **THEN** the system provides filter options (All, Active, Inactive)
- **AND** defaults to showing Active staff only
- **AND** updates list immediately on filter change
- **AND** shows count of staff in each status category
- **AND** persists filter selection in URL query params
- **AND** displays empty state if no staff match filter

#### Scenario: Filter staff by role
- **WHEN** a provider uses role filter
- **THEN** the system provides role filter dropdown with all roles found in current staff
- **AND** includes option for "All Roles"
- **AND** updates list immediately on selection
- **AND** shows count of staff per role
- **AND** supports multiple role selection
- **AND** combines with other active filters

#### Scenario: Search staff by name, email, or phone
- **WHEN** a provider enters search term
- **THEN** the system searches across firstName, lastName, fullName, email, and phoneNumber
- **AND** performs case-insensitive partial matching
- **AND** updates results in real-time as user types (debounced 300ms)
- **AND** highlights matching text in results
- **AND** shows "X results found" count
- **AND** displays "No staff found" message if no matches
- **AND** provides "Clear search" button when search is active
- **AND** combines search with active filters

#### Scenario: Clear all filters
- **WHEN** a provider clicks "Clear Filters" button
- **THEN** the system resets all filters to default (Active, All Roles, empty search)
- **AND** updates staff list to show default view
- **AND** clears URL query parameters
- **AND** hides "Clear Filters" button when no filters active

## REMOVED Requirements

None - this change is fully additive.
