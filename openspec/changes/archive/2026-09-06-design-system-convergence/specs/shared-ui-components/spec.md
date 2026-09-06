## ADDED Requirements

### Requirement: Card components
The system SHALL provide `AppCard`: a white, flat (elevation 0) container with 15dp corner radius, a 1px `AppColors.border` outline, and 12dp interior padding. The system SHALL also provide `AppInfoCard` (adapted from Coliride's tagged info card): an `AppCard` body with an attached top tag strip (`AppColors.border` background, top-rounded, small bold navy label + 16dp icon) and a 40×40dp rounded (8dp) tinted icon container beside a muted-label / navy-value column. `AppInfoCard` SHALL be the standard presentation for wizard preview-step section summaries.

#### Scenario: AppCard renders flat with border
- **WHEN** an `AppCard` is rendered
- **THEN** it shows a white surface, radius 15, 1px `AppColors.border` outline, and no shadow

#### Scenario: Preview step section summary
- **WHEN** the preview step displays a completed section (e.g., business info)
- **THEN** the section renders as an `AppInfoCard` with the section name in the tag strip and the section icon in the tinted container

### Requirement: List row component
The system SHALL provide `AppListRow`: a compact row (minimum height 48dp) with a `#FAFAFA` (`AppColors.surfaceSoft`) fill, 10dp corner radius, optional leading icon in `AppColors.icon`, a navy (`AppColors.ink`) title, an optional `AppColors.muted` subtitle, and an optional trailing widget (chevron by default when tappable). Tappable rows MUST provide ink feedback bounded by the row radius.

#### Scenario: Row anatomy
- **WHEN** an `AppListRow` renders with icon, title, and subtitle
- **THEN** the icon is `AppColors.icon`, the title is navy, the subtitle is `AppColors.muted`, on a `surfaceSoft` r10 surface

#### Scenario: Tappable row affordance
- **WHEN** an `AppListRow` has an `onTap` and no explicit trailing widget
- **THEN** it shows a trailing chevron and ripples within its rounded bounds when pressed

### Requirement: Section header component
The system SHALL provide `AppSectionHeader`: a bold navy title (14–16sp) on the leading edge with an optional trailing action slot. The standard trailing action SHALL be `AppInlineAddButton`: an inline green (`AppColors.success`) icon-plus-label text button with a shrink-wrapped tap target, used for "add another X" affordances.

#### Scenario: Header with add action
- **WHEN** the services step renders its list header with an add affordance
- **THEN** it uses `AppSectionHeader` with an `AppInlineAddButton` labeled for adding a service, rendered in `AppColors.success`

#### Scenario: Header without action
- **WHEN** an `AppSectionHeader` is given no action
- **THEN** only the bold navy title renders, with no trailing widget reserved space

### Requirement: Status badge component
The system SHALL provide `AppStatusBadge`: a 6dp-radius pill with 12×6dp padding and 12sp semi-bold text, colored by semantic status (success green, warning amber, danger red, neutral `AppColors.border` with navy text). Provider, service, and booking statuses displayed in the UI SHALL use this component.

#### Scenario: Status maps to semantic color
- **WHEN** a provider status of "active" is displayed as a badge
- **THEN** an `AppStatusBadge` renders with the success-green background and white 12sp semi-bold label

#### Scenario: Neutral badge readability
- **WHEN** an `AppStatusBadge` renders the neutral variant
- **THEN** the background is `AppColors.border` and the text is navy (not white)

### Requirement: Icon action button component
The system SHALL provide `AppIconButton`: a 44×44dp flat button with 12dp corner radius, a 20dp glyph, primary-tinted hover/splash feedback at 4–5% opacity, an optional count/badge slot, and an effective touch target of at least 48×48dp. App-bar actions and inline icon actions SHALL use this component.

#### Scenario: Badge on action button
- **WHEN** an `AppIconButton` is given a badge label
- **THEN** the badge renders attached to the button's top corner without shifting the 44×44 layout

#### Scenario: Flat press feedback
- **WHEN** an `AppIconButton` is pressed
- **THEN** feedback is a primary-tinted splash bounded by the 12dp radius, with no elevation change

### Requirement: Button size and variant system
`AppButton` SHALL support sizes `big` (46dp, 17sp — default), `dialog` (40dp, 15.5sp), and `small` (30dp, 14sp), and variants primary (filled blue), secondary (outlined, navy text), destructive (filled `AppColors.danger`), and text — all flat, bold, 10dp radius, with the loading state replacing the label with a spinner that contrasts the surface. Full-width expansion SHALL remain the default; `small` and `dialog` sizes MAY shrink-wrap.

#### Scenario: Dialog-size actions
- **WHEN** an `AppDialog` renders its action row
- **THEN** the buttons are `ButtonSize.dialog` (40dp height, 15.5sp bold labels)

#### Scenario: Destructive variant
- **WHEN** an `AppButton` renders the destructive variant
- **THEN** its fill is `AppColors.danger` with white bold text, radius 10, elevation 0

#### Scenario: Loading preserves contrast
- **WHEN** any variant enters the loading state
- **THEN** the spinner color contrasts its surface (white on filled variants, brand blue on outlined/text) and the button is non-interactive

### Requirement: Selection tile states
Selectable tiles (e.g., the category step's grid) SHALL express state through the Coliride three-state color logic: idle = neutral grey text (`#7F8696`) on a bordered surface; selected = brand blue (`AppColors.appBar`) text/border; completed (where applicable) = green text on a `#E9FFF6` tinted fill. State changes SHALL animate within `AppMotion.fast`.

#### Scenario: Selecting a category tile
- **WHEN** the user taps an unselected category tile
- **THEN** the tile transitions to blue text and border within 180ms, and the previously selected tile returns to idle

#### Scenario: Idle tile appearance
- **WHEN** a selectable tile is neither selected nor completed
- **THEN** it renders grey `#7F8696` content on a flat bordered surface

### Requirement: Dropdown trigger affordance
Fields that open a selection list (e.g., the city selector) SHALL display a trailing chevron that rotates 180° via `AnimatedRotation` over `AppMotion.fast` when the list opens and closes.

#### Scenario: Opening the city list
- **WHEN** the user focuses the city field and the inline result list appears
- **THEN** the field's chevron rotates 180° over 180ms; it rotates back when the list closes
