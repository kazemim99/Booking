# Provider Registration Spec Deltas

## MODIFIED Requirements

### Requirement: Category selection UI
The Category Selection step MUST display business categories that map directly to backend ServiceCategory enum values. The system SHALL provide clear visual feedback for the selected category.

**Changes**:
- Category IDs now map 1:1 to ServiceCategory enum
- Category metadata (name, icon, color) matches backend enum extension methods
- Selection saves as ServiceCategory enum value (not string)

#### Scenario: User selects business category
- **GIVEN** the user is on the Category Selection step (step 2)
- **WHEN** the page loads
- **THEN** the user sees a grid of business categories:
  - HairSalon: "آرایشگاه زنانه" with icon 💇‍♀️
  - Barbershop: "آرایشگاه مردانه" with icon 💇‍♂️
  - BeautySalon: "سالن زیبایی" with icon ✨
  - NailSalon: "آرایش ناخن" with icon 💅
  - Spa: "اسپا" with icon 🧖
  - Massage: "ماساژ" with icon 💆
  - Gym: "باشگاه ورزشی" with icon 🏋️
  - MedicalClinic: "کلینیک پزشکی" with icon 🏥
  - Dental: "دندانپزشکی" with icon 🦷
  - (additional categories...)
- **AND** categories display in RTL layout with proper Persian typography
- **AND** each category card shows icon, name, and optional description
- **AND** visual selection indicator appears when category is clicked

#### Scenario: Category selection validation
- **WHEN** the user clicks "بعدی" on the Category Selection step
- **THEN** the system validates that exactly one category is selected
- **AND** displays error message if no category selected: "لطفاً یک دسته‌بندی انتخاب کنید"
- **AND** prevents navigation to next step until valid category selected
- **AND** selected category value is a valid ServiceCategory enum value

#### Scenario: Category value transmission to backend
- **WHEN** the user completes registration
- **THEN** the selected category is sent to backend as ServiceCategory enum integer value
- **AND** backend validates the category is a valid enum value
- **AND** backend creates Provider with `PrimaryCategory` set to selected enum value
- **AND** registration fails if invalid category value is sent

## ADDED Requirements

### Requirement: Category Selection Mapping
The frontend category selection SHALL map directly to backend ServiceCategory enum with no ambiguity.

#### Scenario: Frontend-backend category mapping
- **WHEN** the frontend CategorySelectionStep renders
- **THEN** each category button/card has:
  - `id`: ServiceCategory enum name (e.g., "HairSalon", "Barbershop")
  - `value`: ServiceCategory enum integer ID (e.g., 1, 2)
  - `name`: Persian display name from backend metadata
  - `icon`: Emoji icon from backend metadata
  - `color`: Color hex code from backend metadata
- **AND** frontend TypeScript types match backend C# enum exactly

#### Scenario: Category metadata synchronization
- **WHEN** backend ServiceCategory enum metadata changes
- **THEN** frontend TypeScript types are regenerated/updated
- **AND** category display names, icons, and colors stay in sync
- **AND** build fails if frontend references undefined category

### Requirement: Category Selection Persistence
The selected category SHALL be persisted through the registration flow and saved to the provider profile.

#### Scenario: Category persists across steps
- **WHEN** user selects category on step 2 and navigates to step 3
- **AND** clicks "قبلی" to return to step 2
- **THEN** the previously selected category is still highlighted
- **AND** user can change the selection before proceeding
- **AND** new selection overrides previous selection

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
