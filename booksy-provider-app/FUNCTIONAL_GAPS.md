# Booksy Provider — Functional Gaps

Features the ColiRide reference design implies but the Booksy Provider app does
**not** implement. Recorded here deliberately so the visual reskin never fakes
them with placeholder UI: a control that looks live but does nothing is worse
than an absent one, because it teaches the user to distrust the interface.

Each entry states what the design shows, what we actually have, and what
shipping it would require. Ordered by user-visible impact.

---

## 1. Notifications (bell + unread count) — NOT IMPLEMENTED

**Design shows:** every sub-page header carries a bell in the trailing chrome
position with a coral (`#FF6171`) count badge — e.g. `3` unread. Tapping it
presumably opens a notification list.

**We have:** nothing real.

- `home_page.dart` renders a bell (`Key('home-bell')`) that shows a
  "coming soon" snackbar. **It predates this work and shows no count.**
- No other screen has a bell at all.
- There is no notifications list screen, no unread state, no data source.

**Why it wasn't added during the reskin:** a bell with a hardcoded badge would
misrepresent real state. The count is the whole point of the control, and we
have no source of truth for it.

**To ship, needs:**
- Backend: an unread-count endpoint plus a notifications list endpoint
  (paged, with read/unread state and a mark-as-read mutation). Neither exists
  in `API_ENDPOINTS.md` today.
- Decision: push vs. poll. The Home workspace design already contemplates
  poll/push-ready wiring — notifications should reuse that, not invent a
  second mechanism.
- App: a `NotificationsCubit`, a list screen, an `AppBottomBar`-style badge on
  the chrome bell, and deep-link handling (tapping a booking notification
  should land on that booking).
- Tests: unread-count rendering, mark-as-read, empty/error states,
  receipt-driven navigation.

**Until then:** the Home bell keeps its honest "coming soon" behaviour and no
other header gains a bell.

---

## 2. Availability tabs ("As a driver" / "As a passenger") — N/A BY DOMAIN

**Design shows:** the Availability screen splits by the two ride-sharing roles
via text tabs.

**We have:** no equivalent, correctly. Booksy providers have one role. The
*tab component* was extracted (`AppTextTabs`, green active underline) and is
available, but the driver/passenger split is a ColiRide domain concept and must
not be copied — per the standing rule that we reuse ColiRide's visual language,
never its flows.

**Action:** none. Recorded so nobody "restores" it as a missing feature.

---

## 3. Provider profile detail/edit screens — PARTIALLY IMPLEMENTED

**Design shows:** a read view with per-section `Edit` pills (Personal, Address,
Company, Privacy, Biography) and a tabbed edit form with a completeness ring.

**We have:** the Profile *hub* (`more_page.dart`) reskinned to the design, and
the components the detail screens would need (`ProfileHeader` with completeness
ring, `AppTextTabs`, `AppChoiceChip`, `StickyActionBar`) — but the read/edit
detail screens themselves are not built. Business details are edited through
`more/business` instead, which is a different, simpler flow.

**To ship:** decide whether Booksy wants ColiRide's section-by-section edit
model at all, or whether the current single business-details form is the better
fit for the domain. This is a product decision, not a missing implementation.

---

## 4. Multi-service appointments — NOT SUPPORTED (backend contract)

**User expectation:** book several services in one appointment (e.g. cut +
color), as most salon platforms allow.

**We have:** one service per booking, end to end. `POST /v1/Bookings` accepts a
single `serviceId`; slot generation (`available-slots`) prices and lengths the
slot from that one service; the composer's service picker is single-select
accordingly.

**Why the app can't fake it:** duration is the heart of scheduling. Two
services in one visit means summed duration, combined price, and slot search
over the combined length — all backend concerns. Letting the app "select two"
and fire two bookings would double-book the second slot or leave gaps, and
cancellation/reschedule semantics would split across two records.

**To ship, needs (backend first):**
- `Bookings` accepts `serviceIds[]` (or line items) with derived total
  duration/price; `available-slots` takes the same set.
- Domain ruling on partial fulfilment (cancel one service of two?).
- Then the composer's picker becomes multi-select (`AppChoiceChip` is ready)
  and the confirmation shows a line-item summary.

**Until then:** the composer stays honestly single-select.

---

## Notes

- The **choice-chip pastel family** (`AppChipStyle`) and `AppChoiceChip` are
  built and tested but currently unused by any screen. They are ready for
  service tags / amenity toggles when those land — not a gap, just inventory.
- Keep this file updated when a reskin reveals a design element with no
  functional backing. The rule: **build the styling, never the pretence.**
