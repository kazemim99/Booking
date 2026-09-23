# Customer app: fixes from the 2026-09-23 UI/UX review

## Why

A deep UI/UX review of `booksy-customer-app` (2026-09-23, code read end to end plus a guest walk of a local `master`
build against the production API at 390×844, 360×640 and 1440×900) found problems on the booking path, colour defects
in the theme that repeat on many screens, and a web deployment that shows a blank page for several seconds. It builds on
`openspec/changes/_inline/qa-walkthrough-2026-09-22` (all DONE); nothing here repeats that list.

The user approved every finding ("apply all", 2026-09-23) and answered the four open questions:

1. The two test salons in production are **deactivated**, not deleted.
2. The bottom navigation **shows labels**.
3. The brand name is **not touched** (the user will change it later).
4. The booking wizard **leaves the tab shell**, as checkout already does.

## What changes

- **Booking path**: services on a salon profile open the booking flow with that service already chosen; the time step
  opens on the first day with free times, marks closed weekdays, and an empty day offers the next free one; the confirm
  and success screens say what happens next (the salon still has to accept); the wizard runs without the tab bar.
- **Appointments**: the detail screen can cancel and reschedule, finds bookings older than the newest fifty, and a past
  visit can be booked again; reschedule shows the current time and uses the salon's booking window.
- **Theme**: `secondaryContainer` no longer falls back to the green accent; error/destructive colours, input hints, field
  icons, inactive navigation and app-bar text buttons meet WCAG AA; the step progress bar is readable on the app bar;
  navigation items carry labels.
- **Discovery**: one provider meta line on every card (Persian digits, "no reviews yet", free slots); the map card opens
  the salon; the map search field has one border; stars have one colour; home's recent/favourite cards open the salon;
  a signed-in customer's profile visits are recorded and a salon can be favourited.
- **Salon profile**: reviews and the map render even without an address; prices never truncate; the `$` price band is
  replaced by the starting price.
- **Copy & formatting**: past-tab empty text, weekday spelling, Persian digits and thousands separator, detail titles.
- **Web**: Persian `index.html` metadata, a splash until the first frame, a centred maximum width on wide screens,
  CanvasKit served from our own domain, precompressed assets and gzip in the vhost.
- **Production data**: an integration-tested script deactivates the two test salons.

## Out of scope (recorded, not done)

- A note to the salon on a booking: the backend accepts `customerNotes`, but the salon's app never displays it, so the
  field would be a message nobody reads. Needs a provider-app change first.
- Promotions: the home section is fed by a stub that returns `[]` and no backend exists; it never renders.
- Account deletion and terms/privacy links: tier-3 decisions (see tasks.md).

## Surfaces

- **Customer (Flutter)**: everything above.
- **Customer (Vue web)**: not changed. It was not part of the review; the same checks (price band glyphs, test salons in
  results) apply to it only through data — deactivating the test salons removes them there too.
- **Provider app**: not changed; the provider app remains the visual reference. The theme fixes here are customer-only
  (the provider app has its own copy of the tokens).
- **Admin panel**: not changed. It still has no deactivate action (`providers.api.ts` says so); the script covers the
  two salons now, and an admin endpoint is a follow-up.
