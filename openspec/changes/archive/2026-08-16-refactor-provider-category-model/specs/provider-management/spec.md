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
