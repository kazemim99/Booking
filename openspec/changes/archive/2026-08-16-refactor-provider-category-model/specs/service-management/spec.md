# Service Management Spec Deltas

## MODIFIED Requirements

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

## ADDED Requirements

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

## REMOVED Requirements

None - Service management requirements are additive and modified only.
