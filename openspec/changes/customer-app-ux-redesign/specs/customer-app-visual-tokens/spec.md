# customer-app-visual-tokens

## ADDED Requirements

### Requirement: Single source of visual truth via ThemeData
The app SHALL derive all colors, typography, shapes, and component styling from a single Material 3 `ThemeData` built from the design tokens in `config/theme/`. Screens and feature widgets MUST NOT reference raw hex colors or ad-hoc `TextStyle`s; only the theme and the shared component library may consume tokens directly.

#### Scenario: Screen uses themed styling
- **WHEN** any screen renders a button, card, input, or text
- **THEN** its colors and typography resolve from `Theme.of(context)` (or a `core/widgets` component), with no inline hex values in the feature code

#### Scenario: No legacy ad-hoc theming remains
- **WHEN** the app starts
- **THEN** the applied theme is built from the token set, and no `primarySwatch` or inline `TextTheme` construction exists anywhere in the codebase

> Note: the brand palette itself is specified by "Brand palette aligned to the Provider app" in this same
> capability. This requirement governs only how styling is *sourced*, not which colors are used.

### Requirement: Spacing scale and future-dark-theme token structure
The token set SHALL include a 4dp-based spacing scale that shared components use for padding, gaps, and insets, and SHALL be structured so a dark theme can be introduced later by supplying alternate token values without changing consumer code.

#### Scenario: Spacing comes from the scale
- **WHEN** a shared component lays out padding or gaps
- **THEN** it uses values from the spacing scale rather than arbitrary numbers

#### Scenario: Consumers are theme-agnostic
- **WHEN** an alternate (dark) token set is supplied
- **THEN** no screen or component source change is required for it to take effect

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
