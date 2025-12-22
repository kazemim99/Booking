# Provider Management Spec Deltas

## MODIFIED Requirements

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

#### Scenario: Provider category is immutable by default
- **WHEN** a provider attempts to change their primary category
- **THEN** the system prevents the change by default
- **AND** requires admin approval workflow for category changes (future phase)
- **AND** preserves the original category in audit log

#### Scenario: Provider search by category
- **WHEN** customers search for providers by category
- **THEN** the system filters providers using the `PrimaryCategory` property
- **AND** returns results grouped by category
- **AND** displays category badge/icon for each provider
- **AND** category filter performs efficiently (<50ms p95)

## ADDED Requirements

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
- **WHEN** a customer navigates to category browse page
- **THEN** the system displays all available categories
- **AND** shows provider count for each category
- **AND** categories with zero providers are shown but marked as "Coming Soon"
- **AND** clicking a category shows all providers in that category

#### Scenario: Filter search results by category
- **WHEN** a customer searches for providers
- **THEN** category filter chips display available categories
- **AND** selecting a category filters results to that category only
- **AND** category filter persists across pagination
- **AND** category filter combines with location filter

#### Scenario: Multi-category vs single category
- **WHEN** a provider offers services in multiple domains (e.g., hair + beauty)
- **THEN** they must choose one primary category
- **AND** their services can span categories (validation in future phase)
- **AND** they appear only in their primary category search results

## REMOVED Requirements

### Requirement: Provider Type Enum
**Removed**: The ProviderType enum (Individual, Clinic, Salon, Spa, GymFitness, etc.)

**Reason**: ProviderType conflated two orthogonal concerns:
1. Business structure (Individual vs Organization) - now handled by `ProviderHierarchyType`
2. Service category (Salon, Spa, Clinic, etc.) - now handled by `PrimaryCategory: ServiceCategory`

**Migration**: All existing `ProviderType` values mapped to equivalent `ServiceCategory` values:
- `ProviderType.Salon` → `ServiceCategory.HairSalon`
- `ProviderType.Spa` → `ServiceCategory.Spa`
- `ProviderType.Clinic` → `ServiceCategory.MedicalClinic`
- `ProviderType.GymFitness` → `ServiceCategory.Gym`
- `ProviderType.Individual` → Inferred from services or default to `ServiceCategory.HairSalon`
- etc.

**Impact**: All code referencing `Provider.ProviderType` must be updated to use `Provider.PrimaryCategory` instead.
