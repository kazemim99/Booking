# Provider Registration Spec Deltas

## MODIFIED Requirements

### Requirement: Category selection UI
The Category Selection step MUST display business categories that map directly to backend ServiceCategory enum values. The system SHALL provide clear visual feedback for the selected category.

**Changes**:
- Category cards are derived from the shared category metadata table, so they cannot drift from the backend enum
- Category metadata (name, icon, color) matches the backend enum extension methods
- Selection is transmitted as the canonical category slug, which the backend resolves to the enum

#### Scenario: User selects business category
- **GIVEN** the user is on the Category Selection step (step 2)
- **WHEN** the page loads
- **THEN** the user sees a card for each category in `ENABLED_CATEGORIES`
- **AND** onboarding currently enables a deliberate subset — HairSalon ("آرایشگاه زنانه", 💇‍♀️) and
  Barbershop ("آرایشگاه مردانه", 💇‍♂️) — rather than the full 15-category taxonomy
- **AND** each card's label, icon and slug come from the shared metadata table, never hardcoded
- **AND** categories display in RTL layout with proper Persian typography
- **AND** a visual selection indicator appears when a category is clicked

#### Scenario: Category selection validation
- **WHEN** the user clicks "بعدی" on the Category Selection step
- **THEN** the system validates that exactly one category is selected
- **AND** the "بعدی" button stays disabled until a category is chosen
- **AND** navigation to the next step is prevented until a valid category is selected

#### Scenario: Category value transmission to backend
- **WHEN** the user completes the Category Selection step
- **THEN** the step emits the canonical category slug (e.g. `barbershop`)
- **AND** the backend resolves it through `ServiceCategoryResolver`, which also accepts the enum
  member name, the numeric id, and the legacy wizard aliases that saved drafts still carry
- **AND** the Provider aggregate rejects any category that is not a declared enum member
- **AND** the backend creates the Provider with `PrimaryCategory` set to the resolved value
- **AND** registration fails if an unresolvable category value is sent

## ADDED Requirements

### Requirement: Category Selection Mapping
The frontend category selection SHALL map directly to backend ServiceCategory enum with no ambiguity.

#### Scenario: Frontend-backend category mapping
- **WHEN** the frontend CategorySelectionStep renders
- **THEN** each category card is a `CategoryMetadata` entry carrying:
  - `id`: the ProviderCategory enum value, matching the backend integer exactly
  - `slug`: the canonical URL slug, which is what the step emits
  - `persianName` / `englishName`, `icon`, `colorHex`, `gradient`, `description`
- **AND** the frontend `ProviderCategory` enum integers match the backend C# enum exactly

#### Scenario: Category metadata synchronization
- **WHEN** the backend ServiceCategory enum or its metadata changes
- **THEN** the frontend `ProviderCategory` enum and `CATEGORY_METADATA` must be updated by hand —
  there is no code generation between the two
- **AND** tests on both sides pin the integer ids and the spot-checked names, so a renumbering that
  would silently re-label existing provider rows fails the suite rather than shipping
- **AND** TypeScript rejects a reference to a category that is not declared in the enum

### Requirement: Category Selection Persistence
The selected category SHALL be persisted through the registration flow and saved to the provider profile.

#### Scenario: Category persists across steps
- **WHEN** user selects category on step 2 and navigates to step 3
- **AND** clicks "قبلی" to return to step 2
- **THEN** the previously selected category is still highlighted
- **AND** user can change the selection before proceeding
- **AND** new selection overrides previous selection

#### Scenario: Category is restored when a saved draft is resumed
- **GIVEN** a registration draft was saved with a category
- **WHEN** the registrant returns and the draft is loaded
- **THEN** the Category Selection step re-selects the saved category
- **AND** this holds regardless of the form the saved value takes — the draft endpoints return the
  enum member name (`"HairSalon"`), while older drafts hold wizard aliases such as `barber` and
  the wizard now writes slugs
- **AND** an unrecognised saved value leaves the step with nothing selected rather than guessing

#### Scenario: Category saved to provider
- **WHEN** user completes all registration steps
- **AND** submits the final registration
- **THEN** backend creates Provider entity with:
  - `PrimaryCategory` = selected ServiceCategory enum value
  - `HierarchyType` = Organization or Individual (from separate step)
- **AND** provider profile displays category badge
- **AND** provider appears in category-filtered search results

### Requirement: Category-Specific Registration Guidance
The system SHALL support category-specific guidance during registration as a future enhancement (Phase 2).

**Note**: This requirement documents planned future functionality and is NOT implemented in Phase 1.

#### Scenario: Category-specific service templates (future)
- **WHEN** user selects "HairSalon" category in future Phase 2
- **THEN** the Services step (step 4) SHALL suggest common hair salon services:
  - "کوتاهی مو" (Haircut)
  - "رنگ مو" (Hair coloring)
  - "هایلایت" (Highlights)
  - etc.
- **AND** user SHALL be able to accept suggestions or add custom services
- **AND** templates SHALL be optional and can be skipped

#### Scenario: Category-specific onboarding tips (future)
- **WHEN** user selects "Gym" category in future Phase 2
- **THEN** registration flow SHALL show category-specific tips:
  - "Add photos of your equipment and facilities"
  - "Specify class schedules and trainer availability"
  - etc.
- **AND** tips SHALL be informational, not blocking
