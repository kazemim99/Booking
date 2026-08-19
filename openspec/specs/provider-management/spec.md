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

### Requirement: Provider Core Properties
The Provider aggregate SHALL have clear identity and categorization properties that separate business structure from service offerings.

**Changes**:
- **REMOVED**: `ProviderType` property (conflated business structure with service category)
- **ADDED**: `PrimaryCategory` property (required ServiceCategory enum)
- **KEPT**: `ProviderHierarchyType` (Organization vs Individual)

#### Scenario: Provider creation with category
- **WHEN** a new provider is created
- **THEN** the system requires a `PrimaryCategory` (ServiceCategory enum value)
- **AND** the system requires a `HierarchyType` (Organization or Individual)
- **AND** the `PrimaryCategory` must be one of the predefined ServiceCategory enum values
- **AND** the provider cannot be created without a valid category

#### Scenario: Provider category is settled once registration completes
- **WHEN** a provider is still in `Drafted` status
- **THEN** the category can be corrected via `UpdateDraftInfo` (the registration wizard's "back" path)
- **AND** the replacement must itself be a valid ServiceCategory
- **WHEN** the provider has left `Drafted` status
- **THEN** no command exposes a category change, so the category is effectively immutable
- **AND** an admin-approval workflow for later changes is NOT implemented (future phase)

#### Scenario: Provider search by category
- **WHEN** customers search for providers by category
- **THEN** the system filters providers using the `PrimaryCategory` property
- **AND** the filter accepts the enum member name, the numeric id, or the category slug
- **AND** an unrecognised category matches nothing rather than returning every provider
- **AND** the query is served by the `IX_Providers_PrimaryCategory` index
- **AND** results are returned as a flat paginated list; grouping by category is NOT implemented

### Requirement: Service Category Enum
The system SHALL define service categories as a strongly-typed enum with predefined values grouped by business domain.

#### Scenario: Category enum values
- **WHEN** the system initializes
- **THEN** the ServiceCategory enum includes these values:
  - Beauty & Personal Care: HairSalon, Barbershop, BeautySalon, NailSalon, Spa
  - Health & Wellness: Massage, Gym, Yoga
  - Medical: MedicalClinic, Dental, Physiotherapy
  - Professional Services: Tutoring, Automotive, HomeServices, PetCare
- **AND** each enum value has an explicit integer ID for database storage
- **AND** enum values are immutable once defined

#### Scenario: Category metadata access
- **WHEN** the application needs to display category information
- **THEN** extension methods provide:
  - Persian name (e.g., "آرایشگاه زنانه" for HairSalon)
  - Icon emoji (e.g., "💇‍♀️" for HairSalon)
  - Color hex code (e.g., "#8B5CF6" for HairSalon)
  - URL slug (e.g., "hair-salon" for HairSalon)
- **AND** metadata is compile-time constant (no database lookups)

### Requirement: Provider Primary Category
Every provider SHALL have exactly one primary service category that defines their business focus.

#### Scenario: One category per provider
- **WHEN** a provider is created or updated
- **THEN** the provider has exactly one `PrimaryCategory`
- **AND** the category is non-nullable and required
- **AND** the category cannot be empty or undefined
- **AND** attempting to set multiple categories fails validation

#### Scenario: Category aligns with business model
- **WHEN** an Organization provider registers as a HairSalon
- **THEN** the system sets `PrimaryCategory = ServiceCategory.HairSalon`
- **AND** the provider can offer hair-related services
- **AND** search results show the provider in "Hair Salon" category

#### Scenario: Individual providers have categories
- **WHEN** an Individual provider (freelancer) registers
- **THEN** they must also select a `PrimaryCategory`
- **AND** Independent individuals appear in category search alongside organizations
- **AND** category is independent of `HierarchyType` (both Individual and Organization have categories)

### Requirement: Category-Based Provider Discovery
Customers SHALL be able to discover providers by browsing or filtering by service category.

#### Scenario: Browse providers by category
- **WHEN** a client requests `GET /api/v1/categories`
- **THEN** the system returns every category in the taxonomy, not only the populated ones
- **AND** each carries its id, key, Persian and English names, slug, description, icon, colour and gradient
- **AND** each carries the count of Active providers in that category
- **AND** categories with zero providers are returned with `isComingSoon: true` rather than omitted
- **AND** `GET /api/v1/categories/popular` instead returns only populated categories, ranked by count
- **AND** `GET /api/v1/categories/{idOrSlug}/providers` lists the providers in one category
- **AND** an unknown category on that route responds 404 rather than returning every provider

#### Scenario: Filter search results by category
- **WHEN** a customer searches for providers
- **THEN** the search UI offers the categories from the shared metadata table
- **AND** selecting a category filters results to that category only
- **AND** the category filter combines with the other search filters
- **AND** the filter travels as a query parameter, so it survives pagination

#### Scenario: Multi-category vs single category
- **WHEN** a provider offers services in multiple domains (e.g., hair + beauty)
- **THEN** they must choose one primary category
- **AND** their services can span categories (validation in future phase)
- **AND** they appear only in their primary category search results
