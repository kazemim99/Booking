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

#### Scenario: Service category immutability
- **WHEN** a provider attempts to change a service's category
- **THEN** the system allows the change (category can be updated)
- **AND** updates the category value
- **AND** logs the change in audit trail
- **AND** does NOT validate alignment with Provider.PrimaryCategory (phase 2 feature)

## ADDED Requirements

### Requirement: Service Category Alignment (Future Phase)
Services SHALL align with their provider's primary category when Phase 2 validation is implemented.

**Note**: This requirement is documented but NOT enforced in Phase 1 implementation. Enforcement deferred to Phase 2.

#### Scenario: Same-category service creation (allowed)
- **WHEN** a HairSalon provider creates a service with Category = HairSalon
- **THEN** the service is created successfully
- **AND** no validation warnings are shown
- **AND** service appears in provider's service list

#### Scenario: Cross-category service creation (allowed but flagged)
- **WHEN** a HairSalon provider creates a service with Category = Massage
- **THEN** the service is created successfully (NO blocking in Phase 1)
- **AND** backend logs a warning about category mismatch
- **AND** admin dashboard MAY flag this for review
- **AND** future phase will add validation/compatibility rules

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
- **WHEN** system queries services by category
- **THEN** database uses integer comparison (fast)
- **AND** category column is indexed for performance
- **AND** queries execute in <50ms p95
- **AND** no string parsing or conversion required

#### Scenario: Aggregate services by category
- **WHEN** system aggregates service counts by category
- **THEN** GROUP BY clause uses integer category values
- **AND** aggregation completes in <100ms p95
- **AND** results return category enum values with metadata

## REMOVED Requirements

None - Service management requirements are additive and modified only.
