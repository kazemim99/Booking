## ADDED Requirements

### Requirement: Home State Resolution and Precedence

The Home ("Today") screen SHALL be composed at render time from three layers evaluated in strict precedence order — **System** (offline, error, loading), then **Lifecycle & Availability** (pending verification, vacation, closed-today, blocked), then **Maturity & Day-Context** (setup/growth/traction/operational, and today's bookings). When multiple conditions are active simultaneously, the Home MUST render all applicable treatments composed by this precedence rather than showing only one and hiding the rest.

The persistent banner rail at the top of Home MUST order concurrent banners by severity: **Error → Offline → Vacation → Closed-today → Pending-verification → maturity nudge**. No more than the two highest-severity banners are shown expanded at once; lower-severity ones collapse into a single tappable "more" affordance.

#### Scenario: Offline and pending verification are both active
- **WHEN** the provider is offline AND their provider status is `PendingVerification`
- **THEN** the Home shows the offline banner at the top (higher severity)
- **AND** the pending-verification banner is still present below it (collapsed if space-constrained)
- **AND** cached agenda content is shown beneath the banner rail

#### Scenario: Blocked status never renders Home
- **WHEN** the provider status is `Suspended`, `Inactive`, or `Archived`
- **THEN** the Home is not rendered at all
- **AND** the router shows the dedicated account-blocked screen instead

#### Scenario: A single condition renders a single treatment
- **WHEN** exactly one non-default condition is active (e.g. only offline)
- **THEN** only that condition's banner is shown
- **AND** no placeholder or empty banner slot is rendered for inactive conditions

### Requirement: Business-Maturity Adaptation

The Home SHALL classify the provider into one maturity phase — **Setup**, **Growth**, **Traction**, or **Operational** — and adapt which zones dominate accordingly, so the screen evolves with the business instead of remaining static. Classification MUST be derived from observable signals: provider verification status, profile-completeness (staff added, gallery images, services priced), total historical bookings, and recency of bookings.

As maturity increases, the scaffold/activation zone MUST recede (shrink and move down, then disappear) while the operational agenda zone MUST grow and rise to the top. The maturity phase MUST NOT be a manual toggle; it is a computed property that updates as the underlying signals change.

#### Scenario: Setup phase leads with activation scaffold
- **WHEN** the provider has just completed onboarding and has no bookings and an incomplete profile
- **THEN** the Home classifies the provider as **Setup**
- **AND** the activation checklist is the visually dominant zone at the top
- **AND** the (empty) agenda is present but visually de-emphasized below it

#### Scenario: Operational phase leads with the agenda
- **WHEN** the provider has a steady history of recent bookings and a complete profile
- **THEN** the Home classifies the provider as **Operational**
- **AND** the action queue and today's agenda are the dominant top zones
- **AND** activation/nudge content is absent or reduced to an occasional dismissible strip

#### Scenario: First booking advances maturity
- **WHEN** a provider in **Growth** (verified, zero prior bookings) receives and confirms their first booking
- **THEN** the Home re-classifies toward **Traction**
- **AND** the "get discovered" acquisition zone is demoted below the now-populated agenda

### Requirement: Home Zone Composition

The Home SHALL be composed of named zones, each of which independently appears, adapts, or collapses per the resolved state: **App Bar** (business avatar → account shortcut, greeting/business name, notifications bell with unread badge), **Banner Rail** (system/lifecycle banners), **Action Queue** (T0 items needing immediate provider action), **Now/Next** (the current or upcoming appointment), **Today Agenda** (today's timeline), **Coming-Up Peek** (next open day / tomorrow summary), **Activation & Nudges** (maturity-driven setup tasks), and a **center-docked create action** ("New appointment / Block time").

A zone with no content for the current state MUST collapse and yield its space to lower zones; it MUST NOT render as an empty container. The App Bar is the only always-present zone.

#### Scenario: Empty action queue collapses
- **WHEN** there are no items requiring immediate action
- **THEN** the Action Queue zone is not rendered
- **AND** the Now/Next zone moves up to occupy the freed space

#### Scenario: Notifications badge visible from Home
- **WHEN** there are unread notifications
- **THEN** the bell in the App Bar shows an unread count badge
- **AND** the same unread state is mirrored on the Home bottom-nav tab icon

#### Scenario: Create action is present on Home
- **WHEN** the Home is displayed in any non-error, non-blocked state
- **THEN** the center-docked create action is available
- **AND** tapping it opens a menu offering at least "New appointment" and "Block time"

### Requirement: Widget-Based Composition Architecture

The Home SHALL be built as a composition of reusable, independent zone widgets rather than a single monolithic screen; the Home page itself MUST act only as an orchestrator that selects, orders, and renders these widgets based on the resolved application state and business maturity, and MUST NOT contain zone-specific business logic. Each zone widget (e.g. Status Banner, Action Queue, Now/Next Appointment, Today's Agenda, Coming-Up Peek, Setup Nudges, Business Alerts) MUST be a standalone component that conforms to a common contract declaring: purpose, inputs (data dependencies), outputs (user actions/events it emits), visibility rule, screen priority, and its own loading, empty, error, and offline treatments, its refresh strategy (live/polling/manual/cached), and its collapsible/expandable behavior where applicable. The architecture MUST allow widgets to be added, removed, reordered, or replaced without redesigning the Home page. This requirement is architectural only and MUST NOT introduce new business features or expand MVP scope.

#### Scenario: Each zone is an independent, contract-conforming widget
- **WHEN** any Home zone is implemented
- **THEN** it is a standalone widget exposing the common contract (purpose, inputs, outputs, visibility rule, priority, loading/empty/error/offline states, refresh strategy, collapsible behavior)
- **AND** it does not depend on the internal implementation of any sibling widget

#### Scenario: Home page only orchestrates
- **WHEN** the Home page renders
- **THEN** it resolves an ordered set of widgets from the current state and maturity and renders them by priority
- **AND** it contains no zone-specific business logic of its own

#### Scenario: Widgets fail and load independently
- **WHEN** one widget is loading, errored, or offline
- **THEN** its own loading/error/offline treatment is shown within its bounds
- **AND** sibling widgets continue to render and function normally

#### Scenario: Widgets self-report visibility
- **WHEN** a widget's visibility rule evaluates to hidden for the current state
- **THEN** the orchestrator omits it and reclaims its space
- **AND** no empty placeholder is rendered for it

#### Scenario: Composition changes without redesign
- **WHEN** a widget is added, removed, reordered, or replaced
- **THEN** the change is expressed through the orchestrator's widget registry/ordering
- **AND** no other widget or the Home page layout must be redesigned to accommodate it

### Requirement: Booking-Mode Adaptation

The Home SHALL adapt to the provider's configured booking mode, which is a hybrid, provider-selectable setting with two values: **Instant Booking** and **Request & Approval**. The booking mode is an input to Home composition and MUST re-compose the Home automatically when it changes. In **Request** mode, pending booking requests (the Action Queue) MUST be the highest-priority operational widget. In **Instant** mode, Today's schedule and the Next Appointment MUST be the primary operational focus, and the Action Queue MUST be limited to genuine exceptions (e.g. scheduling conflicts) rather than routine approvals.

#### Scenario: Request mode elevates pending requests
- **WHEN** the provider's booking mode is Request & Approval and unconfirmed requests exist
- **THEN** the Action Queue is the top operational widget with inline confirm/decline
- **AND** it is prioritized above the Now/Next and agenda widgets

#### Scenario: Instant mode elevates the schedule
- **WHEN** the provider's booking mode is Instant Booking
- **THEN** the Now/Next appointment and Today's agenda are the primary operational focus
- **AND** the Action Queue surfaces only exceptions (conflicts), not routine approvals

#### Scenario: Changing booking mode re-composes the Home
- **WHEN** the provider changes their booking mode
- **THEN** the Home re-composes automatically to reflect the new mode's priorities
- **AND** no manual refresh or restart is required

### Requirement: First Login After Onboarding State

The Home SHALL implement a dedicated First-Login-After-Onboarding treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** understand what to do next, feel reassured that setup succeeded, and take the first meaningful step toward receiving bookings. **Information priority:** verification/status reassurance → activation checklist → how to get the first booking (share link / add walk-in) → empty agenda. **Visual hierarchy:** the activation checklist is the hero; the empty agenda is muted at the bottom.

On the first Home session after onboarding, the Home SHALL present the activation state rather than a raw empty calendar or a throwaway success screen, and it MUST reuse the standard Home zone structure so the layout is learned once and transforms as bookings arrive.

#### Scenario: First session shows activation scaffold
- **WHEN** the provider reaches Home for the first time after completing onboarding
- **THEN** an activation checklist is the dominant zone (e.g. add team, add photos, review services & prices, share booking link)
- **AND** items already satisfied during onboarding are shown as completed
- **AND** an inviting empty agenda with a "New appointment" action is shown below

#### Scenario: Completing a checklist item confirms progress
- **WHEN** the provider completes an activation task
- **THEN** the item is marked complete with a brief success confirmation
- **AND** the checklist reflects remaining tasks

#### Scenario: Checklist item fails to save
- **WHEN** saving an activation task fails
- **THEN** the item shows an inline error with a retry affordance
- **AND** other checklist items remain interactive

### Requirement: Empty Business (Growth) State

The Home SHALL implement a dedicated Empty-Business (Growth) treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** attract the first clients and make the business attractive enough to be discovered/booked. **Information priority:** get-discovered (share booking link + profile-completeness) → remaining setup nudges → immediate app value (add a walk-in) → empty agenda. **Visual hierarchy:** the acquisition ("get discovered") card is the hero; the agenda is muted.

When the provider is verified/active but has produced no bookings and has thin traction, the Home SHALL lead with acquisition guidance rather than an operational agenda.

#### Scenario: Growth Home leads with acquisition
- **WHEN** the provider is verified with zero historical bookings
- **THEN** the "get discovered" zone (share booking link, profile-completeness meter) is dominant
- **AND** an "add walk-in appointment" action is offered as an immediate way to use the app
- **AND** the empty agenda is shown de-emphasized below

#### Scenario: Sharing the booking link succeeds
- **WHEN** the provider taps "share booking link"
- **THEN** the platform share sheet opens with the provider's public booking link

#### Scenario: Booking link cannot be generated
- **WHEN** the booking link cannot be produced (e.g. profile not yet public)
- **THEN** the share action explains why and offers the corrective step (complete profile / await verification)

### Requirement: No Appointments Today State

The Home SHALL implement a dedicated No-Appointments-Today treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** confirm nothing was missed, and either fill the day or use the quiet time productively. **Information priority:** a calm "you're all clear today" confirmation → opportunities to fill the day (open slots, add walk-in) → coming-up (next appointment) → light admin nudges. **Visual hierarchy:** a calm, positive empty confirmation is central; the coming-up peek is elevated because today is empty.

When an established provider has no bookings for the current day, the Home SHALL present a positive, non-alarming empty-day treatment — never a bare "nothing here" — and MUST surface the next scheduled appointment when one exists.

#### Scenario: Empty day is framed positively
- **WHEN** an operational provider has zero appointments today
- **THEN** the Home shows a calm "no appointments today" state with the next appointment (e.g. "Next: tomorrow 10:00")
- **AND** actions to add an appointment, block time, or view the calendar are available

#### Scenario: Adding an appointment populates the day
- **WHEN** the provider creates an appointment for today from the empty state
- **THEN** the empty treatment is replaced by the agenda timeline showing that appointment

### Requirement: Pending Verification State

The Home SHALL implement a dedicated Pending-Verification treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** know the business is under review, that this is temporary, and what can be done in the meantime. **Information priority:** status banner → what the provider CAN do now (finish setup, add manual bookings) → what happens next. **Visual hierarchy:** an informative (not alarming) banner sits at the top of the banner rail; the rest of Home follows the maturity/day-context beneath it.

While provider status is `PendingVerification`, the Home SHALL remain fully usable and MUST present a persistent, reassuring banner rather than gating the provider behind a waiting wall.

#### Scenario: Pending banner is informative, not blocking
- **WHEN** the provider status is `PendingVerification`
- **THEN** a persistent banner explains the business is under review and not yet publicly bookable
- **AND** the provider can still complete setup and add manual bookings
- **AND** a path to contact support is available

#### Scenario: Verification approval flips the banner
- **WHEN** the provider's status changes to `Verified`/`Active`
- **THEN** the pending banner is replaced by a positive "your business is live" confirmation
- **AND** the Home nudges the provider to share their booking link

#### Scenario: Status refresh fails
- **WHEN** refreshing the provider status fails
- **THEN** the last known status is retained and shown
- **AND** a non-blocking retry is offered

### Requirement: Active Business Day State

The Home SHALL implement a dedicated Active-Business-Day treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** run the day efficiently and never miss an appointment or a request. **Information priority:** action queue (requests/conflicts) → now/next appointment → today's timeline → coming-up peek. **Visual hierarchy:** the action queue is loudest at the top when present; the now/next card is the dominant hero; the timeline follows; nudges are suppressed.

On a normal working day with bookings, the Home SHALL prioritize immediate operational actions and MUST make the current/next appointment and its quick actions reachable without scrolling.

#### Scenario: Requests appear at the top for action
- **WHEN** there are unconfirmed booking requests
- **THEN** the action queue is the top zone with inline confirm/decline actions
- **AND** confirming or declining updates the item without leaving Home

#### Scenario: Now/Next drives quick actions
- **WHEN** there is a current or upcoming appointment
- **THEN** the Now/Next card exposes call/message, complete, no-show, and reschedule actions
- **AND** these actions require at most two taps

#### Scenario: A quick action fails
- **WHEN** an inline action (e.g. confirm) fails
- **THEN** the optimistic update is rolled back
- **AND** an inline error with retry is shown for that item only

#### Scenario: All of today's work is done
- **WHEN** every appointment for today is completed and the queue is empty
- **THEN** the Home shows an end-of-day success summary (e.g. count completed)
- **AND** the coming-up peek for the next day is elevated

### Requirement: Fully Booked Day State

The Home SHALL implement a dedicated Fully-Booked-Day treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** execute a packed day, protect breaks, avoid overbooking, and handle demand that cannot be accommodated. **Information priority:** "fully booked" acknowledgment → now/next → dense timeline with clear breaks → requests routed to waitlist/decline → conflict warnings. **Visual hierarchy:** a positive "fully booked" indicator near the top; the dense timeline is the hero; conflict warnings are prominent.

When the day has no remaining open capacity, the Home SHALL indicate the fully-booked condition positively and MUST steer new demand toward waitlist/alternatives instead of silently allowing overbooking.

#### Scenario: Fully booked is acknowledged positively
- **WHEN** the day has no remaining open slots
- **THEN** the Home shows a positive "fully booked" indicator with the appointment count
- **AND** the timeline renders densely with breaks visually distinct

#### Scenario: New demand is routed to waitlist/decline
- **WHEN** a new request arrives on a fully-booked day
- **THEN** the request surfaces with waitlist or decline-with-alternative options rather than a normal confirm-into-slot

#### Scenario: Overbooking attempt is guarded
- **WHEN** the provider attempts to add an appointment into an occupied slot
- **THEN** the Home warns of the conflict before allowing an explicit override

### Requirement: Closed Business (Today) State

The Home SHALL implement a dedicated Closed-Business (Today) treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** confirm today is a non-working day, verify nothing leaked onto it, and plan the next open day. **Information priority:** "closed today" confirmation + next open day → any anomalous bookings on the closed day → coming-up (next open day) → admin time. **Visual hierarchy:** a calm closed-today confirmation is central; the next-open-day peek is elevated; the agenda is muted.

When the current day is outside the provider's configured working hours (a scheduled day off, distinct from vacation), the Home SHALL present a calm closed-today treatment and MUST surface the next open day.

#### Scenario: Closed day shows next open day
- **WHEN** today is a configured non-working day
- **THEN** the Home shows "closed today" with the next open day (e.g. "Next open: Saturday")
- **AND** actions to open exceptionally, edit hours, or view the next open day are available

#### Scenario: Anomalous booking on a closed day is surfaced
- **WHEN** an appointment exists on a day marked closed
- **THEN** the Home surfaces it as an anomaly needing attention rather than hiding it

### Requirement: Offline State

The Home SHALL implement a dedicated Offline treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** know they are offline, still read cached information, and understand which actions are queued vs unavailable. **Information priority:** offline banner → cached content with a staleness indicator → which actions are unavailable/queued. **Visual hierarchy:** a neutral offline banner pins to the top; cached content is tagged as last-updated; mutating controls show an offline affordance.

When connectivity is lost, the Home SHALL degrade gracefully: it MUST show cached data with a clear last-updated indication, and it MUST clearly distinguish actions that are queued for later sync from actions that are blocked while offline.

#### Scenario: Cached content shown while offline
- **WHEN** the device is offline and cached Home data exists
- **THEN** the offline banner pins to the top of the banner rail
- **AND** the cached agenda is shown with a last-updated timestamp

#### Scenario: No cache while offline
- **WHEN** the device is offline and no cached data exists
- **THEN** the Home shows a "can't load — you're offline" state with a retry affordance

#### Scenario: Reconnection refreshes and syncs
- **WHEN** connectivity is restored
- **THEN** the Home refreshes automatically and shows a brief "back online" confirmation
- **AND** any actions queued while offline are synced

### Requirement: Error State

The Home SHALL implement a dedicated Error treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** understand that something went wrong, recover via retry without losing context, and reach support if it persists. **Information priority:** a human-readable error message → retry → any partial content that did load → support path when repeated. **Visual hierarchy:** a full error state with a primary retry only when nothing loaded; otherwise a localized error confined to the failed zone while the rest of Home stays operational.

When Home data fails to load for reasons other than connectivity, the Home SHALL prefer graceful partial degradation over a full-screen failure, and error messages MUST be human-readable (no raw codes/stack traces).

#### Scenario: Partial load degrades gracefully
- **WHEN** one Home zone fails to load but others succeed (e.g. queue fails, agenda loads)
- **THEN** only the failed zone shows an inline error with retry
- **AND** the successfully loaded zones remain fully operational

#### Scenario: Total load failure shows recoverable error
- **WHEN** all Home data fails to load and no cache is available
- **THEN** a centered error state is shown with retry as the primary action
- **AND** a support path is offered

#### Scenario: Retry restores content
- **WHEN** the provider retries and the request succeeds
- **THEN** the error treatment is cleared and the normal Home content is shown

#### Scenario: Persistent failure escalates guidance
- **WHEN** retries continue to fail beyond a small threshold
- **THEN** the messaging escalates to suggest contacting support

### Requirement: Vacation Mode State

The Home SHALL implement a dedicated Vacation-Mode treatment with the goals, priorities, actions, and hierarchy specified below.

**User goal:** confirm vacation is active and clients cannot book, know when it ends, handle any bookings already inside the vacation window, and resume easily. **Information priority:** vacation banner (with dates + "not accepting bookings") → bookings already inside the vacation window needing action → countdown/next steps → coming-up after vacation. **Visual hierarchy:** a distinct, calm vacation banner at the top; an affected-bookings alert is elevated because it needs action; the agenda is muted.

When the provider has activated vacation mode (a provider-initiated multi-day pause, distinct from a scheduled closed day), the Home SHALL clearly indicate the active pause and MUST surface any pre-existing bookings that fall within the vacation window for resolution.

#### Scenario: Active vacation is clearly indicated
- **WHEN** vacation mode is active
- **THEN** a distinct vacation banner shows the vacation window and that bookings are not being accepted
- **AND** actions to edit, extend, or end vacation are available

#### Scenario: Bookings inside the vacation window need action
- **WHEN** appointments already exist within the active vacation window
- **THEN** the Home elevates them with actions to reschedule, cancel, or notify the client

#### Scenario: Ending vacation restores acceptance
- **WHEN** vacation mode ends (manually or by reaching its end date)
- **THEN** the Home shows a "welcome back — accepting bookings again" confirmation
- **AND** the banner is removed and normal day-context resumes

### Requirement: RTL and Accessibility Conformance

The Home SHALL be authored RTL-native for Persian (fa-IR): reading order, zone order, banner and badge placement, back-gesture direction, and any progress/temporal ordering flow right-to-left. All interactive targets MUST meet minimum touch-target sizing, expose semantic labels for screen readers, and remain free of layout overflow at increased font scale; motion/micro-celebrations MUST respect a reduced-motion preference.

#### Scenario: Home renders RTL
- **WHEN** the app locale is Persian (fa-IR)
- **THEN** all Home zones, banners, and badges lay out right-to-left
- **AND** no zone overflows horizontally at 1.3× font scale

#### Scenario: Reduced motion is honored
- **WHEN** the provider has reduced-motion enabled
- **THEN** success micro-celebrations and transitions are minimized or disabled
- **AND** all information remains conveyed without relying on motion
