## ADDED Requirements

### Requirement: Structural design tokens
The design system SHALL define structural (non-brand) tokens in `lib/config/theme/app_tokens.dart` alongside the existing brand tokens: motion durations and curves (`AppMotion.fast = 180ms`, `AppMotion.medium = 250ms`, `AppMotion.curve = easeOutCubic`, `AppMotion.reverseCurve = easeInCubic`), icon sizes (`AppIconSize.sm = 16`, `AppIconSize.md = 24`, `AppIconSize.action = 20` glyph in a 44dp container, `AppIconSize.hero = 72`), and overlay barrier colors (dialog `0x24000000`, bottom sheet `0x47000000`). All animated affordances and icon usages in shared components MUST consume these tokens rather than inline literals.

#### Scenario: Tokens are the single source for motion values
- **WHEN** a shared component animates (chevron rotation, sheet slide, keyboard padding)
- **THEN** its duration and curve are referenced from `AppMotion`, and no `Duration(...)` or curve literal appears in the component

#### Scenario: Theme guard test covers structural tokens
- **WHEN** the theme guard test suite runs
- **THEN** it asserts the `AppMotion`, `AppIconSize`, and barrier-color token values, failing on any regression

### Requirement: Spacing scale and layout rhythm
Layouts SHALL use the existing spacing scale (4 / 8 / 16 / 24 / 40) via `AppSpacing`. Page-level content padding SHALL be 24 (`AppSpacing.lg`); spacing between form fields SHALL be 16 (`AppSpacing.md`); padding inside cards and list rows SHALL be 12 (the single permitted off-scale value, defined as a named token). Arbitrary numeric spacing literals MUST NOT be introduced in shared components.

#### Scenario: Card interior uses the intra-card token
- **WHEN** an `AppCard` renders its child
- **THEN** the interior padding is the named 12dp intra-card token, not an inline literal

#### Scenario: Wizard step keeps page rhythm
- **WHEN** any onboarding step renders inside `StepScaffold`
- **THEN** page padding is `AppSpacing.lg` (24) and inter-field gaps are `AppSpacing.md` (16)

### Requirement: Elevation and shadow policy
All surfaces SHALL be flat: elevation 0 on buttons, cards, dialogs, sheets, and app bars, with no `BoxShadow` usage in shared components. Visual depth SHALL be conveyed only by 1px borders (`AppColors.border`) and modal barrier dimming.

#### Scenario: New component ships flat
- **WHEN** any shared component in `core/widgets/` renders
- **THEN** its effective elevation is 0 and it declares no box shadows

#### Scenario: Depth via border
- **WHEN** a card or grouped surface must be visually separated from the page
- **THEN** it uses a 1px `AppColors.border` outline instead of elevation or shadow

### Requirement: Divider styles
The standard divider SHALL be a solid 1px line in `AppColors.divider` (`#E5E9F2`), themed globally. A dashed divider component (`AppDashedDivider`, 5px dash segments) SHALL exist and MUST be used only for receipt-like summary contexts (e.g., the preview step's price summary), not as a general separator.

#### Scenario: Default divider is solid brand grey
- **WHEN** a `Divider` widget renders without explicit color
- **THEN** it draws 1px in `AppColors.divider`

#### Scenario: Dashed divider in the price summary
- **WHEN** the preview step renders its price/summary block
- **THEN** line-item groups are separated by `AppDashedDivider`

### Requirement: Icon style and color roles
Shared components SHALL use Material rounded icons (the Coliride proprietary icon font is not adopted). Decorative/leading icons SHALL render in `AppColors.icon` (`#D2DBEB`); functional/interactive icons SHALL render in `AppColors.ink` or the semantic state color; hero icons in empty/feedback states SHALL render at `AppIconSize.hero`.

#### Scenario: List row leading icon is decorative grey
- **WHEN** an `AppListRow` renders a leading icon without an explicit color
- **THEN** the icon color is `AppColors.icon`

#### Scenario: Feedback-state hero icon sizing
- **WHEN** an empty or error state view renders its icon
- **THEN** the icon size is `AppIconSize.hero` (72)

### Requirement: Content density
Form controls SHALL keep the data-entry density established by the brand reskin (46dp buttons, ~55dp field zones). List rows SHALL use compact density: minimum row height 48dp, 12dp internal padding. Every interactive element MUST retain an effective touch target of at least 48×48dp regardless of its visual size.

#### Scenario: Compact row respects the accessibility floor
- **WHEN** an `AppListRow` renders in compact density
- **THEN** its hit-testable height is at least 48dp

#### Scenario: 44dp icon button keeps a 48dp target
- **WHEN** an `AppIconButton` renders at its 44×44 visual size
- **THEN** its effective gesture target is at least 48×48dp
