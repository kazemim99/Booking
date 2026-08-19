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

### Requirement: Router-driven navigation shell
The app SHALL use declarative routing (go_router) with a stateful bottom-navigation shell of four tabs — home, explore, appointments, profile — each maintaining its own navigation stack and scroll position when switching tabs. All primary destinations (provider detail, booking steps, appointment detail) SHALL be addressable routes.

#### Scenario: Tab state preserved
- **WHEN** the user scrolls deep into explore, switches to home, and returns to explore
- **THEN** explore shows the same scroll position and any pushed detail screen still on its stack

#### Scenario: Auth-gated route redirect
- **WHEN** a guest navigates to a route requiring authentication
- **THEN** the router redirects to login and, after success, continues to the originally requested route

### Requirement: Predictable back behavior
The Android back gesture/button SHALL pop the current tab's stack first; on a tab's root it SHALL return to the home tab; on home's root it SHALL exit the app. Back navigation MUST never skip intermediate screens or exit unexpectedly from a nested screen.

#### Scenario: Back from nested screen
- **WHEN** the user presses back on a provider detail screen inside the explore tab
- **THEN** the app returns to the explore results, not to home and not out of the app

#### Scenario: Back from a non-home tab root
- **WHEN** the user presses back at the root of the appointments tab
- **THEN** the app switches to the home tab instead of exiting

### Requirement: Standardized async screen states
Every data-backed screen SHALL render exactly one of the standardized states — skeleton loading, content, empty, or error — via the shared state-switching pattern. Skeletons SHALL approximate the shape of the loaded content. Error states SHALL always offer retry. Blank screens and indefinite spinners are prohibited.

#### Scenario: First load
- **WHEN** a data-backed screen opens before its request resolves
- **THEN** a content-shaped skeleton renders instead of a spinner or blank area

#### Scenario: Load failure
- **WHEN** the request fails
- **THEN** an error state with a retry action renders, and retry re-issues the request showing the skeleton again

### Requirement: Offline awareness
The app SHALL detect connectivity loss and show a non-blocking offline banner while offline. Actions requiring the network SHALL fail fast with a clear offline message instead of hanging, and previously loaded content SHALL remain visible.

#### Scenario: Connectivity drops while browsing
- **WHEN** the device goes offline on a loaded screen
- **THEN** the offline banner appears, loaded content stays visible, and pull-to-refresh reports the offline condition rather than spinning indefinitely

#### Scenario: Connectivity restored
- **WHEN** the device regains connectivity
- **THEN** the banner dismisses automatically

### Requirement: Success and failure feedback
Every user-initiated mutation (booking, cancel, reschedule, login) SHALL produce immediate visual feedback: a loading state on the triggering control, then a success snackbar or success screen, or an error message that names the problem and preserves user input. Silent failures are prohibited.

#### Scenario: Mutation fails
- **WHEN** a user-initiated action fails
- **THEN** the user sees an error with actionable wording, the triggering control returns to its enabled state, and entered data is not lost

### Requirement: App-wide accessibility conformance
Every screen SHALL meet the accessibility baseline defined for shared components, applied at screen level: WCAG AA contrast, ≥48dp targets, meaningful Persian semantics labels announced by TalkBack/VoiceOver, no information conveyed by color alone, usable at 1.3× font scale, and reduced-motion honored for screen transitions.

#### Scenario: Screen reader traversal
- **WHEN** a screen-reader user traverses the appointments screen
- **THEN** each booking card announces service, provider, date, and status as a coherent Persian label, and action buttons announce their purpose

