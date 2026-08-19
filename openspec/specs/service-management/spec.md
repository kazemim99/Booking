# service-management Specification

## Purpose
TBD - created by archiving change complete-business-profile. Update Purpose after archive.
## Requirements
### Requirement: Service Catalog Display
The system SHALL display all provider services in an organized, manageable list with filtering and search capabilities.

#### Scenario: Service list display
- **WHEN** a provider views their service catalog
- **THEN** the system displays all services in a card-based grid layout
- **AND** shows key information for each service (name, price, duration, status, category)
- **AND** displays service thumbnail images when available
- **AND** indicates services that are inactive or draft status

#### Scenario: Service filtering and search
- **WHEN** a provider filters or searches services
- **THEN** the system allows filtering by category, status, price range, and availability
- **AND** provides a search box for name and description text search
- **AND** updates results in real-time as filters are applied
- **AND** displays count of filtered results

#### Scenario: Service sorting and ordering
- **WHEN** a provider reorders services
- **THEN** the system allows drag-and-drop to change service display order
- **AND** persists the custom ordering for the provider's profile
- **AND** provides sort options (alphabetical, price, duration, popularity)
- **AND** displays visual feedback during drag operations

### Requirement: Service Creation
The system SHALL allow providers to create new services with comprehensive configuration options.

#### Scenario: Create basic service
- **WHEN** a provider creates a new service
- **THEN** the system displays a form with required fields (name, description, category, price, duration)
- **AND** validates all required fields before allowing submission
- **AND** provides category selection from predefined list matching business type
- **AND** creates the service in draft status by default

#### Scenario: Service pricing configuration
- **WHEN** a provider configures service pricing
- **THEN** the system allows setting base price with currency selection
- **AND** supports multiple pricing tiers (e.g., student, senior, premium)
- **AND** allows deposit requirements with percentage or fixed amount
- **AND** displays pricing clearly with all tiers visible

#### Scenario: Service duration and timing
- **WHEN** a provider sets service timing
- **THEN** the system requires service duration in minutes
- **AND** allows optional preparation time before service
- **AND** allows optional buffer/cleanup time after service
- **AND** displays total booking slot duration combining all time components

#### Scenario: Service options and add-ons
- **WHEN** a provider adds service options
- **THEN** the system allows creating optional add-ons with additional price
- **AND** supports required options that customers must select
- **AND** allows option groups (e.g., "Choose one: Short/Medium/Long")
- **AND** displays option impact on total price and duration

### Requirement: Service Editing
The system SHALL allow providers to edit existing services while maintaining data integrity.

#### Scenario: Edit active service
- **WHEN** a provider edits an active service with existing bookings
- **THEN** the system displays a warning about impact on future bookings
- **AND** allows choosing to apply changes immediately or from specific date
- **AND** preserves historical booking data with original service details
- **AND** requires confirmation for significant changes (price increase >20%, duration changes)

#### Scenario: Service image management
- **WHEN** a provider manages service images
- **THEN** the system allows uploading multiple images per service
- **AND** provides image cropping and optimization tools
- **AND** allows selecting a primary image for list display
- **AND** supports drag-and-drop reordering of images
- **AND** validates file size (max 5MB per image) and format

### Requirement: Service Status Management
The system SHALL allow providers to activate, deactivate, and archive services.

#### Scenario: Activate draft service
- **WHEN** a provider activates a draft service
- **THEN** the system validates all required information is complete
- **AND** makes the service visible to customers for booking
- **AND** includes the service in provider's public catalog
- **AND** displays confirmation of activation

#### Scenario: Deactivate service
- **WHEN** a provider deactivates an active service
- **THEN** the system prevents new bookings for the service
- **AND** maintains existing bookings without changes
- **AND** hides the service from customer search and catalog
- **AND** allows reactivation at any time
- **AND** requires a reason for deactivation (optional but encouraged)

#### Scenario: Archive service
- **WHEN** a provider archives a service
- **THEN** the system moves the service to archived status
- **AND** prevents any future bookings
- **AND** maintains historical booking data and analytics
- **AND** removes from active service list but keeps in archives
- **AND** requires confirmation with warning about permanence

### Requirement: Staff Assignment to Services
The system SHALL allow providers to assign qualified staff members to services.

#### Scenario: Assign staff to service
- **WHEN** a provider assigns staff to a service
- **THEN** the system displays all active staff members
- **AND** allows multi-select of qualified staff
- **AND** indicates which staff are already assigned
- **AND** updates service availability based on assigned staff schedules

#### Scenario: Staff qualification requirements
- **WHEN** a provider sets staff qualifications for a service
- **THEN** the system allows marking service as requiring specific qualifications
- **AND** filters staff list to show only qualified members
- **AND** displays warnings when no qualified staff are available
- **AND** prevents booking when no qualified staff can fulfill service

### Requirement: Service Availability Settings
The system SHALL allow providers to configure when and how services are available for booking.

#### Scenario: Booking window configuration
- **WHEN** a provider configures service booking windows
- **THEN** the system allows setting minimum advance booking time (hours)
- **AND** allows setting maximum advance booking time (days)
- **AND** allows same-day booking enablement with cutoff time
- **AND** displays how settings affect customer booking availability

#### Scenario: Service location availability
- **WHEN** a provider sets service location options
- **THEN** the system allows enabling at-location service
- **AND** allows enabling mobile/on-site service
- **AND** allows enabling both location types with different pricing
- **AND** requires mobile service area configuration if mobile is enabled

#### Scenario: Concurrent booking limits
- **WHEN** a provider sets concurrent booking limits
- **THEN** the system allows specifying maximum simultaneous bookings
- **AND** accounts for multiple staff availability when calculating capacity
- **AND** prevents overbooking based on configured limits
- **AND** displays capacity warnings when approaching limits

### Requirement: Bulk Service Operations
The system SHALL support bulk operations for efficient service management.

#### Scenario: Bulk status change
- **WHEN** a provider selects multiple services for bulk action
- **THEN** the system displays available bulk actions (activate, deactivate, archive, delete)
- **AND** shows confirmation dialog with list of affected services
- **AND** processes all selected services with progress indicator
- **AND** displays summary of successful and failed operations

#### Scenario: Bulk price adjustment
- **WHEN** a provider applies bulk price changes
- **THEN** the system allows percentage increase/decrease or fixed amount change
- **AND** displays preview of new prices before applying
- **AND** allows exclusions from selected services
- **AND** requires confirmation with impact summary

### Requirement: Service Analytics and Insights
The system SHALL provide insights into service performance and popularity.

#### Scenario: Service performance metrics
- **WHEN** a provider views service analytics
- **THEN** the system displays booking count for each service
- **AND** shows revenue generated per service
- **AND** displays popularity trends over time
- **AND** highlights top-performing and underperforming services
- **AND** provides recommendations for service improvements

### Requirement: Service Category Property
Service entities SHALL use ServiceCategory enum (not value object) for categorization.

**Changes**:
- Service.Category changed from value object to enum
- Database storage changed from varchar to integer
- Better performance and type safety

#### Scenario: Service creation with category
- **WHEN** a provider creates a new service
- **THEN** the service requires a `Category` (ServiceCategory enum value)
- **AND** the category must be one of the predefined ServiceCategory enum values
- **AND** the service cannot be created without a valid category
- **AND** category is stored as integer in database (not string)

#### Scenario: Service category can be changed
- **WHEN** a provider changes a service's category via `UpdateBasicInfo`
- **THEN** the system allows the change and updates the category value
- **AND** the replacement must itself be a declared ServiceCategory, or the change is rejected
- **AND** a `ServiceUpdatedEvent` is raised
- **AND** the system does NOT validate alignment with Provider.PrimaryCategory (phase 2 feature)

### Requirement: Service Category Alignment (Future Phase)
Services SHALL align with their provider's primary category when Phase 2 validation is implemented.

**Note**: This requirement is documented but NOT enforced in Phase 1 implementation. Enforcement deferred to Phase 2.

#### Scenario: Same-category service creation (allowed)
- **WHEN** a HairSalon provider creates a service with Category = HairSalon
- **THEN** the service is created successfully
- **AND** no validation warnings are shown
- **AND** service appears in provider's service list

#### Scenario: Cross-category service creation (allowed)
- **WHEN** a HairSalon provider creates a service with Category = Massage
- **THEN** the service is created successfully (NO blocking in Phase 1)
- **AND** no mismatch warning is logged and no admin flag is raised — neither is implemented
- **AND** a future phase will add validation/compatibility rules

#### Scenario: Future category compatibility matrix
- **WHEN** Phase 2 category validation is implemented
- **THEN** the system will define compatibility rules:
  - BeautySalon can offer HairSalon services (compatible)
  - Spa can offer Massage services (compatible)
  - HairSalon can offer NailSalon services (compatible)
  - MedicalClinic can offer Dental services (NOT compatible - too specialized)
  - Gym can offer Yoga services (compatible)
- **AND** incompatible combinations will require admin approval
- **AND** existing services will be grandfathered in

### Requirement: Service Category Query Performance
Service category queries SHALL be optimized for fast filtering and aggregation.

#### Scenario: Query services by category
- **WHEN** the system queries services or providers by category
- **THEN** the comparison happens in the database on the integer column
- **AND** the predicate compares the enum directly — filtering on `Category.ToString()` has no SQL
  translation for a `HasConversion<int>()` column and must never be reintroduced
- **AND** provider category filtering is served by `IX_Providers_PrimaryCategory`
- **NOTE** the <50ms p95 target has not been measured; it needs a production-sized dataset

#### Scenario: Aggregate providers by category
- **WHEN** the system aggregates provider counts by category
- **THEN** the GROUP BY runs in the database over the integer category values
- **AND** the endpoint must not materialise every active provider to count them in memory
- **AND** results return the category enum values with their metadata
- **NOTE** the <100ms p95 target has not been measured
