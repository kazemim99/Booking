# Provider Dashboard Specification

## ADDED Requirements

### Requirement: Figma-Based Dashboard Layout
The system SHALL implement a complete provider dashboard redesign based on Figma RTL design specifications with comprehensive feature set.

#### Scenario: Dashboard displays tabbed navigation
- **WHEN** a provider accesses the dashboard
- **THEN** the dashboard displays a tabbed interface with the following tabs: Bookings, Profile, Business Hours, Gallery
- **AND** tabs are responsive and work correctly on mobile devices
- **AND** the active tab is visually highlighted

#### Scenario: Profile tab displays sub-tabs
- **WHEN** the provider selects the Profile tab
- **THEN** sub-tabs are displayed: Personal Info, Business Info, Location, Staff
- **AND** each sub-tab content loads without page reload
- **AND** changes are saved to backend

### Requirement: Dashboard Layout Components
The system SHALL provide a modern dashboard layout with sidebar navigation and responsive design.

#### Scenario: Sidebar navigation displays
- **WHEN** the dashboard loads on desktop
- **THEN** a sidebar displays with navigation items
- **AND** sidebar collapses on mobile devices
- **AND** mobile menu toggle button is visible on small screens

#### Scenario: Dashboard header displays
- **WHEN** the dashboard is rendered
- **THEN** a header displays with provider name and quick actions
- **AND** header is sticky and remains visible when scrolling
- **AND** header adapts to mobile layout

### Requirement: Business Hours Management with Jalali Calendar
The system SHALL enable providers to manage business hours with support for Persian calendar dates and multiple daily breaks.

#### Scenario: Business hours editor displays weekly schedule
- **WHEN** the provider accesses business hours management
- **THEN** a weekly schedule editor displays for each day
- **AND** users can set opening and closing times
- **AND** multiple breaks per day can be configured
- **AND** times are displayed in 12/24 hour format based on preference

#### Scenario: Jalali calendar date picker works
- **WHEN** selecting special dates or exceptions
- **THEN** a Jalali (Persian) calendar date picker displays
- **AND** dates are correctly converted between Jalali and Gregorian
- **AND** selected dates are properly stored

#### Scenario: Custom day schedules are managed
- **WHEN** the provider wants to set special day schedules
- **THEN** a modal allows selecting a Jalali date and setting custom hours
- **AND** custom schedules override the weekly schedule for that date
- **AND** custom schedules can be edited or deleted

### Requirement: Gallery Management
The system SHALL provide comprehensive gallery management with image upload, organization, and primary image selection.

#### Scenario: Gallery images display in grid
- **WHEN** the provider views the gallery
- **THEN** uploaded images display in a responsive grid
- **AND** grid adapts to different screen sizes
- **AND** each image shows metadata and actions

#### Scenario: Images can be uploaded
- **WHEN** the provider uploads images
- **THEN** images are processed and stored
- **AND** multiple images can be uploaded at once
- **AND** upload progress is shown to the user

#### Scenario: Primary image can be set
- **WHEN** the provider marks an image as primary
- **THEN** that image is set as the business's primary gallery image
- **AND** previously marked primary images are unset automatically
- **AND** the primary image is visually indicated in the grid

#### Scenario: Images can be edited and deleted
- **WHEN** the provider performs gallery actions
- **THEN** images can be edited (metadata, alt text)
- **AND** images can be deleted with confirmation dialog
- **AND** changes are reflected immediately in the UI

### Requirement: RTL Language Support
The system SHALL fully support Right-to-Left language layouts for Persian and Arabic users.

#### Scenario: All components respect RTL layout
- **WHEN** the application language is set to Persian/Arabic
- **THEN** all dashboard components display in RTL direction
- **AND** spacing and alignment adapt to RTL
- **AND** icons flip when directionally meaningful

#### Scenario: Persian number formatting applies
- **WHEN** numbers are displayed in the dashboard
- **THEN** numbers are converted to Persian numerals when viewing in Persian language
- **AND** numbers maintain correct values in calculations

### Requirement: Enhanced Booking Statistics
The system SHALL display booking statistics with charts and trend indicators (deferred for future implementation).

#### Scenario: Statistics cards display (placeholder)
- **WHEN** the provider views the dashboard
- **THEN** statistics cards display key metrics
- **AND** cards show trend indicators
- **AND** detailed statistics can be accessed (future enhancement)

