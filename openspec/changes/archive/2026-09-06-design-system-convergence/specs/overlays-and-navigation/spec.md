## ADDED Requirements

### Requirement: Dialog presentation
The system SHALL provide `AppDialog` and a `showAppDialog()` helper: a white, flat, 16dp-radius panel over a light `0x24000000` barrier, with an optional modal header (centered navy title, close action, optional divider) and an action row of `ButtonSize.dialog` (40dp) buttons. Confirmation dialogs (e.g., logout, destructive deletes) SHALL use this presentation.

#### Scenario: Logout confirmation
- **WHEN** the user taps logout and confirmation is required
- **THEN** an `AppDialog` opens over a `0x24000000` barrier with a navy title, a message, and 40dp cancel/confirm actions (confirm styled destructive where the action is destructive)

#### Scenario: Dialog dismissal affordances
- **WHEN** an `AppDialog` with a header renders
- **THEN** it can be dismissed via the header close action and via barrier tap (unless explicitly marked blocking)

### Requirement: Bottom sheet presentation
The system SHALL provide `showAppBottomSheet()`: a white sheet with 14dp top radius over a `0x47000000` barrier, entering with a slide-up transition on `AppMotion.curve` (easeOutCubic) and exiting on `AppMotion.reverseCurve` (easeInCubic), and remaining keyboard-aware (content animates above the keyboard within 200ms). Sheets SHALL support an optional fractional height.

#### Scenario: Sheet entry motion
- **WHEN** a bottom sheet is presented
- **THEN** it slides up from the bottom edge with the easeOutCubic curve over the medium motion duration, over a `0x47000000` barrier

#### Scenario: Keyboard avoidance
- **WHEN** a text field inside an open sheet gains focus and the keyboard appears
- **THEN** the sheet content animates above the keyboard within 200ms without being obscured

### Requirement: App bar chrome and back affordance
All screens SHALL use the themed app bar: flat blue (`AppColors.appBar`) with white foreground and centered title. The back affordance SHALL be a white back icon supplied globally via the theme's `actionIconTheme` (matching Coliride's white back-icon treatment), never per-screen icon overrides.

#### Scenario: Back icon on a pushed screen
- **WHEN** any screen with a back affordance renders its app bar
- **THEN** the back icon is white, provided by the global `actionIconTheme`, on the blue app bar

#### Scenario: Consistent app bar across screens
- **WHEN** the login, OTP, wizard, or dashboard app bar renders
- **THEN** background, foreground, title alignment, and elevation come from the shared `appBarTheme` with no per-screen styling overrides

### Requirement: Navigation structure stability
The provider app SHALL NOT introduce a bottom navigation bar or tab shell as part of design-system convergence; its wizard + dashboard structure is a deliberate divergence from Coliride's tabbed main shell, driven by the provider onboarding domain. Any future tab bar SHALL follow the specced Coliride visual contract: green (`AppColors.success`) selected label with a 2dp green underline indicator, navy unselected labels, and an `AppColors.border` divider line.

#### Scenario: No invented navigation chrome
- **WHEN** design-system components are adopted across existing screens
- **THEN** the screen hierarchy and navigation structure remain unchanged (wizard + dashboard, go_router driven)

#### Scenario: Future tab bar follows the contract
- **WHEN** a tabbed surface is first introduced (e.g., a dashboard epic)
- **THEN** its tab bar renders green selected labels with a 2dp green underline, navy unselected labels, and a border-grey divider

### Requirement: Snackbar overlay styling
Transient overlays via `AppSnackbar` SHALL remain the single toast mechanism: floating behavior, 12dp radius, semantic background colors (success green, danger red, ink navy for info). No second toast/overlay library SHALL be introduced.

#### Scenario: Error toast styling
- **WHEN** a save error is surfaced as a snackbar
- **THEN** it floats with 12dp radius and the `AppColors.danger` background

### Requirement: Adaptive layout readiness
Shared overlay and layout components MUST NOT hardcode absolute widths; dialogs SHALL cap their width via constraints (not fixed sizes) so the mobile-only assumption can later relax to tablet without component API changes. Full responsive breakpoints (Coliride's `DeviceScreenType` framework) are intentionally NOT adopted.

#### Scenario: Dialog on a wide viewport
- **WHEN** an `AppDialog` renders on a viewport wider than a phone
- **THEN** the panel respects a maximum-width constraint and centers, rather than stretching edge-to-edge
