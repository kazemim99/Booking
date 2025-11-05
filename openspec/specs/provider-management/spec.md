# provider-management Specification

## Purpose
TBD - created by archiving change complete-business-profile. Update Purpose after archive.
## Requirements
### Requirement: Business Profile Hub
The system SHALL provide a centralized hub for providers to access all business management features through a tabbed interface.

#### Scenario: Provider navigates to business profile
- **WHEN** a provider accesses their business profile page
- **THEN** the system displays a tabbed interface with sections for Business Info, Hours, Services, Staff, Gallery, and Settings
- **AND** the current active tab is visually highlighted
- **AND** navigation between tabs preserves unsaved form state with confirmation prompts

#### Scenario: Profile completion indicator
- **WHEN** a provider views their business profile hub
- **THEN** the system displays a profile completion percentage
- **AND** shows which sections are incomplete or missing required information
- **AND** provides quick links to complete missing sections

### Requirement: Enhanced Business Information Management
The system SHALL allow providers to update their business profile with rich media and comprehensive information.

#### Scenario: Logo upload
- **WHEN** a provider uploads a business logo
- **THEN** the system provides image cropping tools to ensure proper aspect ratio (1:1 square)
- **AND** generates optimized versions for different display contexts (thumbnail, preview, full)
- **AND** validates file size (max 5MB) and format (PNG, JPG, WebP)
- **AND** displays a real-time preview of the uploaded logo

#### Scenario: Cover image upload
- **WHEN** a provider uploads a cover image
- **THEN** the system provides image cropping tools for banner aspect ratio (16:9 or 3:1)
- **AND** generates optimized versions for responsive display
- **AND** validates file size (max 10MB) and format (PNG, JPG, WebP)
- **AND** displays a real-time preview across different device sizes

#### Scenario: Business information validation
- **WHEN** a provider submits updated business information
- **THEN** the system validates all required fields in real-time
- **AND** displays inline error messages for validation failures
- **AND** prevents submission until all validation rules are satisfied
- **AND** preserves successfully validated fields when correcting errors

### Requirement: Advanced Business Hours Management
The system SHALL provide advanced business hours management capabilities including visual calendar interface, holiday scheduling, and exception handling.

#### Scenario: Calendar view of business hours
- **WHEN** a provider views business hours in calendar mode
- **THEN** the system displays week grid with visual hour blocks
- **AND** shows month calendar for overview
- **AND** color-codes days by status (open=green, closed=gray, exception=yellow, holiday=red)
- **AND** allows toggling between calendar and list view
- **AND** displays breaks visually within operating hours
- **AND** shows tooltips with detailed info on hover/tap
- **AND** allows clicking date to edit schedule

#### Scenario: Holiday and exception dates
- **WHEN** a provider manages holiday and exception dates
- **THEN** the system allows marking specific dates as holidays
- **AND** allows setting exception hours for specific dates
- **AND** allows recurring holidays (yearly, monthly patterns)
- **AND** displays holidays and exceptions on calendar
- **AND** prevents bookings on holiday dates
- **AND** applies exception hours instead of regular hours
- **AND** warns if existing bookings are affected

#### Scenario: Recurring exceptions
- **WHEN** a provider sets recurring pattern for closures
- **THEN** the system supports weekly recurrence (every Monday)
- **AND** supports monthly recurrence (1st of month, last Friday)
- **AND** supports yearly recurrence (same date annually)
- **AND** shows preview of dates affected by pattern
- **AND** allows setting pattern end date or occurrence count
- **AND** allows editing individual occurrences
- **AND** clearly displays which dates are affected

### Requirement: Business Profile Preview
The system SHALL provide a live preview of how the business profile appears to customers.

#### Scenario: Real-time profile preview
- **WHEN** a provider edits any business profile information
- **THEN** the system displays a side-by-side or toggle view of the customer-facing profile
- **AND** updates the preview in real-time as changes are made
- **AND** shows both desktop and mobile views
- **AND** allows previewing before saving changes

### Requirement: Profile Visibility Controls
The system SHALL allow providers to control the visibility and public availability of their profile.

#### Scenario: Profile visibility settings
- **WHEN** a provider accesses visibility settings
- **THEN** the system allows toggling profile visibility (public, unlisted, private)
- **AND** displays a clear explanation of each visibility level
- **AND** warns about impact on bookings and discovery when changing visibility
- **AND** requires confirmation for making profile private or unlisted

#### Scenario: Feature-specific visibility
- **WHEN** a provider configures feature visibility
- **THEN** the system allows hiding specific features from public view (e.g., pricing, staff names, gallery)
- **AND** displays how each setting affects customer experience
- **AND** maintains booking functionality even when some features are hidden

### Requirement: Location Map Synchronization
The system SHALL provide two-way synchronization between the interactive map and location selector dropdowns when providers set their business location.

#### Scenario: Map click updates location selectors
- **WHEN** a provider clicks a location on the map
- **THEN** the system performs reverse geocoding to detect the province and city
- **AND** automatically selects the detected province in the province dropdown
- **AND** loads cities for the detected province
- **AND** automatically selects the detected city in the city dropdown
- **AND** normalizes province names by removing "استان" prefix if present
- **AND** logs detection results for debugging

#### Scenario: Province selection centers map
- **WHEN** a provider selects a province from the dropdown
- **THEN** the system geocodes the province name to coordinates
- **AND** centers the map on the province location
- **AND** updates the map marker to the new coordinates
- **AND** resets the city selection

#### Scenario: City selection centers map
- **WHEN** a provider selects a city from the dropdown
- **THEN** the system geocodes "city, province" for better accuracy
- **AND** centers the map on the city location
- **AND** updates the map marker to the new coordinates
- **AND** zooms the map to an appropriate level

#### Scenario: Geocoding failure handling
- **WHEN** geocoding fails for a location name
- **THEN** the system logs an error to the console
- **AND** does not update the map position
- **AND** maintains existing coordinates
- **AND** does not prevent the user from continuing with location setup

