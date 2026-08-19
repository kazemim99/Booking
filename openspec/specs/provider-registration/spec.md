# provider-registration Specification

## Purpose

This specification defines the provider registration flow for the Booksy platform. The registration process guides new service providers through a comprehensive 9-step onboarding experience, collecting all necessary business information to create a complete provider profile.

**Status**: ✅ Production Ready (as of 2025-11-11)

**Recent Updates**:
- Gallery image submission now properly integrated with registration flow
- UI fixes for CompletionStep and OptionalFeedbackStep
- Registration progress query handles completed registrations correctly
- Provider status transitions properly handled (Drafted → PendingVerification)

**Related Documentation**:
- [Gallery Implementation Summary](/openspec/changes/add-provider-image-gallery/IMPLEMENTATION_SUMMARY.md)
- [CHANGELOG.md](/CHANGELOG.md)
## Requirements
### Requirement: Registration step sequence
The provider registration flow MUST follow the new Figma-designed sequence with 11 total steps. The system SHALL enforce sequential navigation and prevent skipping incomplete steps.

#### Scenario: User completes full registration flow
**Given** the user has been authenticated via phone verification
**When** the user proceeds through registration
**Then** the steps appear in this order:
1. Business Info (name, owner info)
2. Category Selection (business type)
3. Location (address, map)
4. Services (service catalog)
5. Staff (team members)
6. Working Hours (business hours)
7. Gallery (portfolio images)
8. Optional Feedback (user experience survey)
9. Completion (success screen)

**And** each step displays a visual progress indicator
**And** the user can navigate back to previous steps
**And** the user cannot skip to future steps without completing current step

### Requirement: Visual progress tracking
Each registration step MUST display a progress indicator showing current position in the flow. The system SHALL update the progress indicator in real-time as users navigate between steps.

#### Scenario: User views progress on any step
**Given** the user is on any registration step
**When** the page loads
**Then** a progress indicator displays:
- Current step number
- Total number of steps (9 content steps)
- Visual progress bar or step indicators
- RTL-compatible layout

#### Scenario: Progress updates as user advances
**Given** the user is on step 3
**When** the user completes step 3 and clicks "بعدی"
**Then** the progress indicator updates to show step 4
**And** the visual progress bar fills proportionally

### Requirement: Business information step UI
The Business Info step MUST match the Figma design with improved form layout and RTL styling. The system SHALL validate all required fields and display errors in Persian.

#### Scenario: User enters business information
**Given** the user is on the Business Info step
**When** the page loads
**Then** the user sees:
- RTL form layout
- Fields for business name, owner name
- Modern input styling matching Figma design
- Proper labels in Persian
- Validation messages in Persian
- "بعدی" and "قبلی" navigation buttons

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

### Requirement: Location selection UI
The Location step MUST include map integration with improved UX. The system SHALL integrate Neshan Maps for location selection and display address fields in RTL layout.

#### Scenario: User enters business location
**Given** the user is on the Location step
**When** the page loads
**Then** the user sees:
- Address input fields (RTL)
- Map integration (Neshan Maps)
- Location marker placement
- "بعدی" and "قبلی" navigation buttons

### Requirement: Services configuration UI
The Services step MUST allow adding/editing services with modern UI. The system SHALL provide CRUD operations for services with validation for required fields.

#### Scenario: User adds services
**Given** the user is on the Services step
**When** the page loads
**Then** the user sees:
- List of added services (empty initially)
- "افزودن سرویس" button
- Service cards with name, price, duration
- Edit/delete options for each service
- "بعدی" and "قبلی" navigation buttons

### Requirement: Staff management UI
The Staff step MUST allow adding team members with improved form design. The system SHALL support adding, editing, and removing staff members with RTL form layout.

#### Scenario: User adds staff members
**Given** the user is on the Staff step
**When** the page loads
**Then** the user sees:
- List of added staff members
- "افزودن کارمند" button
- Staff cards with name, role
- Edit/delete options for each staff member
- "بعدی" and "قبلی" navigation buttons

### Requirement: Working hours configuration UI
The Working Hours step MUST display weekly schedule with improved visual design. The system SHALL allow configuration of daily hours and breaks with time validation.

#### Scenario: User sets business hours
**Given** the user is on the Working Hours step
**When** the page loads
**Then** the user sees:
- Weekly calendar view (Saturday to Friday)
- Toggle for each day (open/closed)
- Time pickers for open/close times (RTL)
- Break time configuration (optional)
- "بعدی" and "قبلی" navigation buttons

### Requirement: Gallery upload step
Providers MUST be able to upload portfolio images during onboarding to showcase their work. The system SHALL support multiple image formats and provide visual feedback during upload.

#### Scenario: User uploads gallery images
**Given** the user reaches the Gallery step
**When** the page loads
**Then** the user sees:
- Upload area for selecting multiple images
- Preview of uploaded images
- Option to remove uploaded images
- "بعدی" button to proceed
- "قبلی" button to go back

#### Scenario: User uploads image files
**Given** the user is on the Gallery step
**When** the user selects image files (JPEG, PNG, WebP)
**Then** the images upload and display as thumbnails
**And** the user can add more images
**And** the user can remove any uploaded image

#### Scenario: User skips gallery step
**Given** the user is on the Gallery step
**When** the user clicks "بعدی" without uploading images
**Then** the user proceeds to the next step
**And** the gallery remains empty (optional step)

### Requirement: Optional feedback step
Providers MUST have the option to provide feedback about the onboarding experience. The system SHALL allow users to skip this step without blocking registration completion.

#### Scenario: User provides feedback
**Given** the user reaches the Optional Feedback step
**When** the page loads
**Then** the user sees:
- Optional feedback form or survey
- Text area or rating inputs
- "بعدی" button to proceed
- "رد کردن" or "قبلی" button to skip or go back

#### Scenario: User submits feedback
**Given** the user is on the Optional Feedback step
**When** the user enters feedback text
**And** clicks "بعدی"
**Then** the feedback is submitted to the backend (or logged)
**And** the user proceeds to the Completion step

#### Scenario: User skips feedback
**Given** the user is on the Optional Feedback step
**When** the user clicks "بعدی" without entering feedback
**Then** the user proceeds to the Completion step
**And** no feedback is recorded

### Requirement: Completion screen with dashboard navigation
After completing all steps, users MUST see a success screen with clear next steps. The system SHALL provide a clear call-to-action button to navigate to the provider dashboard.

#### Scenario: User completes registration
**Given** the user has completed all registration steps including feedback
**When** the Completion step loads
**Then** the user sees:
- Success message confirming registration
- Information about admin approval process (if applicable)
- "رفتن به داشبورد" button to navigate to provider dashboard
- Celebratory or confirmation UI elements

#### Scenario: User navigates to dashboard
**Given** the user is on the Completion step
**When** the user clicks "رفتن به داشبورد"
**Then** the user navigates to the provider dashboard
**And** can begin managing their business profile

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
