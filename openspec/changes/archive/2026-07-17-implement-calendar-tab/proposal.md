## Why

The IA defines Calendar (تقویم) as a primary bottom-nav destination — "planning ahead is a distinct mode from 'today'" — but the tab currently shows "coming soon". With the Home and the booking composer shipped and the provider-bookings range endpoint live-verified, the Calendar can now be built entirely on proven backend surface. It completes the second of the four nav destinations and makes the bottom bar real navigation instead of stubs.

## What Changes

- **Calendar tab** (`/calendar`): an RTL week strip (7 days, booking-count badges) + a day timeline (time-gutter agenda of the selected day's bookings), with an "امروز" jump and period title in the app bar.
- **Booking action sheet**: tapping a calendar booking opens a bottom sheet with the booking's details and the operational quick actions (confirm/decline/complete/no-show, call) reusing the Home's cubit handlers.
- **Create from the calendar**: the center ⊕ and empty-day CTA open the booking composer **pre-dated to the selected day** (composer gains an optional initial date via `?date=`).
- **Shared bottom nav**: the bar is extracted from `HomeView` into a reusable `ProviderNavBar`; Home and Calendar navigate via `context.go`. (A full `StatefulShellRoute` with per-tab state is deliberately deferred until Clients/More exist — documented trade-off.)
- **Data**: `HomeRepository.fetchBookings({from,to})` exposes the already-built enriched booking mapping for arbitrary ranges; `fetchSnapshot` reuses it internally.
- **Deferred (no backend concept yet)**: block-time / availability overrides; full week-grid view (the week strip + day timeline is the MVP "week context"); Jalali day numbers (needs a calendar package — follow-up shared with the composer date strip).

## Capabilities

### New Capabilities
- `provider-calendar`: the provider app's Calendar tab — week navigation, day timeline, booking action sheet, calendar-initiated creation, and its loading/empty/error/stale treatments.

### Modified Capabilities
<!-- None. provider-home-workspace's nav/create requirements are unchanged; this implements the Calendar destination they reference. -->

## Impact

- Flutter only — no backend changes (bookings range + mutations already live-verified).
- New: `features/home/presentation/pages/calendar_page.dart`, `cubit/calendar_cubit.dart`, shared `widgets/provider_nav_bar.dart`; router `/calendar`; composer `initialDate`; `AppStrings` calendar section; cubit + widget tests.
- Modified: `home_page.dart` (nav extraction), `home_repository.dart(+impl)` (range fetch), `booking_composer_page.dart`/`composer_cubit.dart` (initial date), `app_router.dart`.
