## ADDED Requirements

### Requirement: Week Navigation and Day Timeline

The Calendar tab SHALL present an RTL 7-day week strip with a booking-count badge per day and a selected-day timeline listing that day's bookings in start-time order with a time gutter (local wall-clock). Selecting a day MUST switch the timeline without a full-screen reload; moving to the previous/next week MUST fetch that week's bookings. An "امروز" (today) affordance SHALL jump the strip and selection back to the current day. The app bar MUST show the visible period.

#### Scenario: Selecting a day switches the timeline
- **WHEN** the provider taps another day in the week strip
- **THEN** the timeline shows that day's bookings in start order
- **AND** only the timeline region re-renders (no full-screen spinner)

#### Scenario: Week paging fetches and race-guards
- **WHEN** the provider pages to the next week and quickly pages again
- **THEN** only the most recent week's result is displayed (stale responses discarded)

#### Scenario: Today jump
- **WHEN** the provider taps "امروز" from another week
- **THEN** the strip returns to the current week with today selected

#### Scenario: Empty day is actionable
- **WHEN** the selected day has no bookings
- **THEN** a calm empty state offers adding an appointment for that day

#### Scenario: Failure and offline degrade per the shared recipe
- **WHEN** the week fetch fails or the device is offline
- **THEN** the calendar shows the shared error-with-retry or cached/stale treatment within its own bounds

### Requirement: Booking Action Sheet

Tapping a booking on the calendar SHALL open a bottom sheet showing the booking's time, client/service labels, status, and notes, with the operational quick actions appropriate to its state: confirm/decline for pending requests; complete/no-show for upcoming confirmed bookings; call when a client phone exists. Actions MUST reuse the same handlers/semantics as the Home (success refreshes the calendar; failures surface the mapped message and change nothing).

#### Scenario: Pending booking offers confirm/decline
- **WHEN** the provider taps a pending booking and confirms it
- **THEN** the sheet closes, the calendar refreshes, and the booking shows as confirmed

#### Scenario: Action failure preserves state
- **WHEN** an action fails
- **THEN** the error message is shown and the booking's displayed state is unchanged

### Requirement: Calendar-Initiated Creation

The calendar's create affordances (center ⊕ and the empty-day CTA) SHALL open the booking composer pre-set to the calendar's selected day. A booking created this way MUST appear on the calendar upon return.

#### Scenario: Composer opens on the selected day
- **WHEN** the provider has Thursday selected and taps create
- **THEN** the composer opens with Thursday as its date and fetches that day's slots

#### Scenario: Created booking appears on return
- **WHEN** the composer pops with a created booking
- **THEN** the calendar refreshes and the new booking is visible on its day

### Requirement: Shared Bottom Navigation

Home and Calendar SHALL share one bottom navigation bar component with the active tab indicated; tapping the other tab navigates between `/dashboard` and `/calendar`. Tabs without destinations (Clients, More) remain visibly inactive with the "coming soon" notice. The bar MUST keep the center-docked create action on both tabs.

#### Scenario: Switching tabs
- **WHEN** the provider taps تقویم from the Home
- **THEN** the Calendar page opens with the تقویم tab active
- **AND** tapping خانه returns to the Home
