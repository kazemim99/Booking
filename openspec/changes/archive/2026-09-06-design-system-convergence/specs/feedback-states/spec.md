## ADDED Requirements

### Requirement: Loading state component
The system SHALL provide `AppLoading`: the single loading presentation for all screens, rendering a Material circular spinner (brand primary) with an optional muted message beneath it, in inline and centered layouts. Raw `CircularProgressIndicator` instances MUST NOT appear directly in feature screens; all existing call sites (splash, city-list loading, map geocoding overlay, button internals excepted via `AppButton`) SHALL migrate to `AppLoading` or a loading-aware shared component.

#### Scenario: Centered page loading
- **WHEN** the splash screen waits on session restore
- **THEN** it renders a centered `AppLoading` in the brand primary color

#### Scenario: Inline loading with message
- **WHEN** the location step is fetching the city list
- **THEN** an inline `AppLoading` renders with the loading message in `AppColors.muted`

#### Scenario: No raw spinners in screens
- **WHEN** feature presentation code is inspected
- **THEN** no direct `CircularProgressIndicator` usage exists outside shared components

### Requirement: Empty state component
The system SHALL provide `AppEmptyState`: a centered composition of a hero icon (72dp, `AppColors.icon`), a bold navy 16sp caption, an optional muted description, and an optional action button (`ButtonSize.small`). The icon slot SHALL accept an arbitrary widget so illustrations can replace icons later without an API change.

#### Scenario: Gallery step with no images
- **WHEN** the gallery step renders with zero uploaded images
- **THEN** an `AppEmptyState` shows a gallery icon, a navy bold caption, and the upload action

#### Scenario: Empty state with action
- **WHEN** an `AppEmptyState` is given an action
- **THEN** a small (30dp) brand button renders beneath the caption

### Requirement: Error state component
The system SHALL provide `AppErrorState`: a centered composition of a warning icon (50dp, `AppColors.muted`), a muted message, and an optional outlined retry button (10dp radius, per the Booking button system). Recoverable data-load failures in screens SHALL render `AppErrorState` with a retry callback instead of inline text.

#### Scenario: City list fails to load
- **WHEN** the location step's city fetch fails
- **THEN** an `AppErrorState` renders with the failure message and a retry button that re-triggers the fetch

#### Scenario: Non-recoverable error omits retry
- **WHEN** an `AppErrorState` is constructed without a retry callback
- **THEN** only the icon and message render, with no button

### Requirement: Success feedback
Success feedback SHALL remain toast-based via `AppSnackbar.success` (green `AppColors.success` background, floating, 12dp radius) for transient confirmations, and green check iconography (`AppColors.success`) for terminal completion screens. No additional success-overlay dependency SHALL be introduced.

#### Scenario: Transient save confirmation
- **WHEN** an operation succeeds and the user stays on the screen
- **THEN** a floating green snackbar with 12dp radius shows the confirmation

#### Scenario: Terminal completion
- **WHEN** the onboarding completion step renders
- **THEN** the hero check icon renders in `AppColors.success`

### Requirement: Step progress indicator
The onboarding wizard SHALL keep its linear step-progress bar in the app bar: green (`AppColors.success`) value on a translucent white track over the blue chrome, advancing proportionally to the current step. This intentionally diverges from Coliride (which has no multi-step wizard) as a booking-domain pattern.

#### Scenario: Advancing a step
- **WHEN** the user completes step 3 of 8 and moves to step 4
- **THEN** the app-bar progress bar advances from 3/8 to 4/8, green on the blue app bar

### Requirement: Screen state coverage in tests
Every screen that loads remote data SHALL have widget-test coverage for its loading, error (with retry behavior), and content states; screens with list content SHALL additionally cover the empty state.

#### Scenario: Location step state coverage
- **WHEN** the location step's test suite runs
- **THEN** it exercises the city-list loading state, the failure state with retry, and the loaded state
