# customer-app-chrome-styling Specification

## Purpose
TBD - created by archiving change unify-customer-app-with-provider-design. Update Purpose after archive.
## Requirements
### Requirement: App bar rendered as blue chrome with white content
The Customer app's app bar SHALL render with a blue chrome background (`#3777C0`), white foreground content, a centered title, flat elevation (0, no scrolled-under shadow), and a white RTL-mirrored back icon supplied globally. The status-bar icons MUST switch to the light style so they remain legible over the blue chrome.

#### Scenario: App bar shows blue chrome and white content
- **WHEN** any screen with an app bar renders
- **THEN** the bar is `#3777C0`, its title and icons are white and centered, and it casts no shadow

#### Scenario: Back navigation shows a white mirrored icon
- **WHEN** a screen deeper than a tab root shows a back affordance
- **THEN** the back icon is white and mirrors correctly under RTL

#### Scenario: Status bar stays legible
- **WHEN** the blue app bar is visible
- **THEN** the status-bar icons render in the light style

### Requirement: Bottom navigation restyled to the floating blue pill without changing architecture
The bottom navigation SHALL render as a floating blue pill matching the Provider look — chrome-blue fill, radius 16, side gutters, raised above the bottom edge, white 24px icons, an active-item indicator, and count badges — while the underlying navigation architecture, shell, routes, tabs, and back-stack behavior remain unchanged.

#### Scenario: Bottom nav matches the pill visuals
- **WHEN** the bottom navigation renders
- **THEN** it appears as a floating blue pill with white icons, an active indicator, and badges

#### Scenario: Navigation behavior is unchanged
- **WHEN** the bar is restyled
- **THEN** tab switching, routes, and back-stack behavior are identical to before the restyle

### Requirement: Lists, rows, and dividers styled consistently
List rows and dividers SHALL use the aligned divider color (`#E5E9F2`), aligned spacing, and flat surfaces, so list-based screens read consistently with the Provider app.

#### Scenario: Divider uses the aligned color
- **WHEN** a list divider renders
- **THEN** it uses `#E5E9F2` rather than a translucent-black divider

### Requirement: Interaction and animation styling aligned
Ripple/overlay/pressed states and transition timings SHALL draw from the aligned motion tokens and palette, without changing what any interaction does.

#### Scenario: Interaction visuals use aligned motion and color
- **WHEN** a user presses an interactive surface
- **THEN** its ripple/overlay and transition use the aligned color and 180ms/`easeOutCubic` motion

#### Scenario: Interaction logic is unchanged
- **WHEN** the interaction styling is applied
- **THEN** the action performed by the interaction is identical to before

