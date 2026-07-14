# mobile-app-shell-ux

## ADDED Requirements

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
Every screen SHALL pass the accessibility baseline defined by the design system: WCAG AA contrast, ≥48dp targets, meaningful Persian semantics labels announced by TalkBack/VoiceOver, no information conveyed by color alone, usable at 1.3× font scale, and reduced-motion honored for screen transitions.

#### Scenario: Screen reader traversal
- **WHEN** a screen-reader user traverses the appointments screen
- **THEN** each booking card announces service, provider, date, and status as a coherent Persian label, and action buttons announce their purpose
