# mobile-design-system

## ADDED Requirements

### Requirement: Single source of visual truth via ThemeData
The app SHALL derive all colors, typography, shapes, and component styling from a single Material 3 `ThemeData` built from the design tokens in `config/theme/`. Screens and feature widgets MUST NOT reference raw hex colors, ad-hoc `TextStyle`s, or `AppColors` directly; only the theme and the shared component library may consume tokens.

#### Scenario: Screen uses themed styling
- **WHEN** any screen renders a button, card, input, or text
- **THEN** its colors and typography resolve from `Theme.of(context)` (or a `core/widgets` component), with no inline hex values in the feature code

#### Scenario: Brand color is consistent
- **WHEN** the app starts
- **THEN** the applied theme uses the dark-blue brand palette (#1A365D primary) and the legacy purple `primarySwatch` no longer exists anywhere in the codebase

### Requirement: Design token set
The theme SHALL define a complete token set: color roles (primary/tint/shade, surface, background, borders, text hierarchy, semantic success/warning/error/info), a Vazir-based type scale, a 4dp-based spacing scale, radius scale, elevation levels, and standard motion durations/curves. Tokens SHALL be structured so a dark theme can be added later without changing consumer code.

#### Scenario: Spacing and radius come from the scale
- **WHEN** a shared component lays out padding, gaps, or corner rounding
- **THEN** it uses values from the spacing/radius token scales rather than arbitrary numbers

### Requirement: Shared component library
The app SHALL provide a component library in `core/widgets` including at minimum: `AppButton` (primary, secondary, text, destructive variants with a loading state), `AppTextField`, `OtpInput`, `AppCard`, `StatusBadge`, `SkeletonLoader`, `EmptyState`, `ErrorState` with retry, `OfflineBanner`, `AppBottomSheet`, `ConfirmSheet`, and `AppSnackbar` (success/error variants, optional undo action). All feature screens MUST compose these components instead of styling Material widgets locally.

#### Scenario: Button exposes loading state
- **WHEN** an `AppButton` is placed in loading state during an async action
- **THEN** it shows a progress indicator, disables further taps, and retains its layout size

#### Scenario: Destructive action uses confirmation
- **WHEN** a screen offers a destructive action (e.g., cancel booking, logout)
- **THEN** it presents `ConfirmSheet` stating the consequence before executing, and the confirming button uses the destructive variant

### Requirement: Component accessibility baseline
Every shared component SHALL provide: a touch target of at least 48×48dp, text contrast meeting WCAG AA against its background, a Persian `Semantics` label for non-text controls, correct rendering at OS text scale up to 1.3 without clipping or overflow, and animations that respect `MediaQuery.disableAnimations`.

#### Scenario: Font scaling does not break layout
- **WHEN** the OS font scale is set to 1.3
- **THEN** all shared components render without text clipping, overflow errors, or overlapping controls

#### Scenario: Reduced motion honored
- **WHEN** the OS reduce-motion/disable-animations setting is on
- **THEN** shared components skip or minimize their transition animations

### Requirement: RTL-first rendering
All shared components SHALL render correctly under RTL directionality: direction-sensitive icons (back, chevrons, progress direction) use direction-aware variants, and layouts mirror properly. Components MUST NOT break when rendered under LTR.

#### Scenario: Direction-aware navigation icon
- **WHEN** a back affordance renders in the RTL app
- **THEN** the arrow points in the RTL-correct direction (and would mirror automatically under LTR)

### Requirement: Centralized user-facing strings
All user-facing Persian strings SHALL live in a single strings module (`core/constants/app_strings.dart` or equivalent), not inline in widgets, so a future localization pass is mechanical.

#### Scenario: New screen adds copy
- **WHEN** a screen introduces new user-facing text
- **THEN** the text is defined in the strings module and referenced by name in the widget
