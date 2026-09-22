Status: ACTIVE
Verify: FAST

User report (2026-09-22), an 11:45 screen recording of an end-to-end walk on production (commit 680a67ea,
`origin/master`): customer app at customer.nahalkmi.ir (Flutter web) — browse, book at «سالن نهال», OTP sign-up,
profile — then the provider app at provider.nahalkmi.ir as the salon (09123135143). Today's reviews work
(`provider-reviews-and-ratings`) is not deployed, so it is not what the recording tested.

Every issue the recording raises is listed, by timestamp, and triaged: **B** = broken (a defect, fix now),
**R** = a product rule the tester stated (implement), **U** = UX/copy polish, **D** = data/seed.
Each surface — customer (Flutter + Vue), provider, admin — is checked per item (see memory: plan all three
frontends).

## Findings
_Root causes land here as they are confirmed, with evidence._

- 1.1 provider app 403: one person per phone — a salon owner whose phone is also used as a customer becomes
  `UserType.Both` (`User.EnsureCanActAs`). OTP sign-in hard-codes `user_type=Provider`, but the token refresh issues
  `user.Type` = "Both", and `ProviderOnly`/`ProviderOrAdmin` admitted only "Provider"/"Admin". So after the first
  silent refresh every provider route (Home, calendar: `GET /Bookings/provider/{id}`) answers 403 while the inbox
  (plain `[Authorize]`) keeps working — exactly frame 18. Reproduced (RED 403) and fixed; whether THIS account is
  `Both` in production is not confirmed (needs a DB read or its token), but the defect is real for any such person.
- NEW 3.0 booking time shift: the customer app sent `startTime.toUtc()`; slots are zone-less salon wall-clock, so in
  Tehran a "14:00" slot was booked — and conflict-checked — at 10:30, leaving the real 14:00 open (double-booking
  risk; `EXC_Bookings_Staff_NoOverlap` compares stored values). Reschedule had the same defect. The provider app was
  fixed for this on 2026-09-19; the Vue wizard tags wall-clock digits with Z and is correct. Bookings already made
  from the customer app are stored 3h30 early — see Decisions.
- 1.3/1.5 provider notifications: `NewBookingRequest` IS raised to the owner. It reads «مشتری گرامی» because no booking
  raise site sets `CustomerName` (or `ServiceName`). Nothing is tappable because `NotificationDestinationResolver`
  treats the reader as a party only when `booking.ProviderId == readerId` — an organisation id never equals the
  owner's user id — and the provider app routes a booking only to the calendar, which 403'd anyway.
- 1.4 English HTML: legacy `Notification` rows written by the deleted `BookingCancelledNotificationHandler` (removed
  in f9502127); they have no `EventCode`. Walk-ins use the owner as `CustomerId`, which is how they reached the salon.
- 2.2 dates: `BookingSmsText.PersianDate` renders "جمعه 1405/07/03" — Latin digits and a slash date that RTL text
  scrambles. Shared with the walk-in SMS.
- 3.1 no slots: slots are computed live; the customer app discards the server's `validationMessages` and always says
  "no free time", so the real reason (closed weekday, holiday, service too long for the day, no bookable member,
  service `MaxAdvanceBookingDays`…) is invisible. The salon's actual cause needs its live response.
- 3.2 grid: starts are a fixed 30-minute grid from opening; each booking blocks its span plus a hard-coded 15-minute
  buffer, so after 14:00–14:45 the next offer is 15:00.
- 3.3 horizon: only `Service.MaxAdvanceBookingDays` (nullable) is enforced when listing and booking; the provider's
  `BookingPolicy.MaxAdvanceBookingDays` (default 90) is checked only at `Booking.Confirm()`, i.e. after the customer
  booked. The customer app shows 14 days, the Vue modal allows 3 months.
- 3.4 "متخصص" + phone: data — a member created by OTP without a name is "ارائه‌دهنده <digits>" (same cause as 4.3).
- 4.1 photos: uploads are stored as RELATIVE paths (`LocalFileStorageService` → `uploads/providers/…_medium.webp`, no
  leading slash, by design: "the UrlService will convert these"). Only the gallery and registration-progress handlers
  call `IUrlService.ToAbsoluteUrl`; provider search, by-id, by-location and profile pass the relative path through as
  `logoUrl`/`images[]`. On Flutter web a relative URL resolves against customer.nahalkmi.ir, whose nginx answers
  `index.html`, so every image fails to decode. The 2026-09-19 fix (`admin-panel-data-fixes`) made the gallery
  endpoint absolute and proxied /uploads on the Vue container; the provider read models were never covered.
  Surfaces: customer app (broken), Vue web (has its own `toAbsoluteUrl`, so hidden there), provider app and admin
  read the same DTOs.
- 4.2 nearest: the customer app sends `radiusKm`, but `SearchProvidersRequest` has no such property, so it is dropped
  and every active provider comes back sorted by planar distance — the closest one anywhere is "nearest". Search
  returns no distance either. `/Providers/by-location` already applies a radius and returns `distanceKm`. The nearby
  cubit also accepts an IP-derived fix of any accuracy (the map cubit already rejects them).
- 4.3 name: `PersonProvisioningService.CreatePerson` names a new person "مشتری <digits>" or "ارائه‌دهنده <digits>" by
  the capacity it was first created for; a later customer sign-in reuses the person and never renames. This phone
  was first used on the provider app. No client ever asks for a name.
- 4.4 chevrons: `Icons.chevron_left/right` are `matchTextDirection: true`, so the manual RTL swap in
  `profile_tab_page.dart` mirrors twice. Only occurrence in the customer app.
- 4.8 breaks: the API sends breaks and the profile renders "استراحت" rows when a salon saved any; `isOpenNow` ignores
  them, so it says "open" during a break and the header never shows today's break.

## Tasks

### Provider app — blocking (B)
- [x] 1.1 [10:33, frame 18] Home shows "بارگذاری ناموفق بود" and the calendar "دریافت نوبت‌ها خطا دارد" — the
      provider app cannot load its own bookings. Reproduce, find the root cause, fix with a test.
- [x] 1.2 [09:18–09:30] A booking the customer just made (status "در انتظار تایید") appears nowhere for the salon.
- [x] 1.3 [09:09] The salon gets no "new booking request" notification for it.
- [x] 1.4 [11:10] A provider notification body contains raw English HTML ("<h2>Your booking has been cancelled</h2>").
- [x] 1.5 [11:20] Tapping a notification does nothing; it should open the booking (to confirm/decline).

### Notifications — copy (U)
- [x] 2.1 [10:53] Notifications say "مشتری گرامی" where the customer has a real name.
- [x] 2.2 [10:59] Dates read like "۳ هفته ۴ ساعت ۵ جمعه"; use "سه‌شنبه ۲۸ مهر، ساعت ۱۴:۰۰".

### Booking flow (B / R)
- [x] 3.5 B (found by 3.2's test) A booking held against the SALON was invisible to the slot list, which kept
      offering that time while creating it answered 409.
- [x] 3.0 B (found while tracing 3.2) The customer app shifted every booked time by the device's UTC offset.
- [x] 3.1 B [03:53–04:11] A normal open day ("۱ مهر") offers no free time.
- [x] 3.2 R [05:08–05:40] Slots after a booking must start when it ends: a 45-minute booking at 14:00 makes the next
      slot 14:45, not 14:30.
- [x] 3.3 R [04:15–04:44] No booking more than 7 days ahead; the date picker shows/enables only those days.
- [x] 3.4 B [04:50–05:03] The confirmation step labels the person "متخصص" and shows a phone number instead of a name.

### Customer app (B / U)
- [x] 4.1 B [00:08–00:13] Salon photos do not load (سالن نهال has 3).
- [x] 4.2 B [01:15–01:27] "نزدیک‌ترین‌ها" lists a salon near Tehran for a user in Pars-Abad; show the distance.
- [x] 4.3 B [08:00–08:22] A customer registered by OTP is named "ارائه‌دهنده <phone>"; ask for a real name.
- [x] 4.4 U [07:52] RTL chevrons point the wrong way ("ویرایش پروفایل" and elsewhere).
- [ ] 4.5 U [07:33–07:44] No visible way to reach notifications outside Home.
- [ ] 4.6 U [06:23–06:58] OTP success is a plain "ورود موفق" message; use a short animated check.
- [ ] 4.7 U [01:35–01:50] "مشاهده پروفایل" is a large button on every card; compact affordance, use the room for rating.
- [ ] 4.8 U [02:23–02:39] Working hours show open/closed but not the break.
- [ ] 4.9 U [02:00, 02:09] Profile opens slowly; photos should be a carousel at the top.
- [ ] 4.10 U [00:19–00:27] The map does not load on first open; loads after a few seconds.

### Provider app (U)
- [x] 5.1 [09:30–09:46] The calendar should let the salon look ahead day by day (tomorrow, the day after).

### Data (D)
- [ ] 6.1 [00:45, 03:03–03:31] Seed سالن نهال with test reviews (likes/dislikes, a provider reply), and make the
      whole salon part of the seeder so a new server starts with it.

## Decisions

- T2 (3.2) The 15-minute gap after each appointment is now zero, because the tester asked for the next slot to
  start when the previous booking ends ("14:00 for 45 minutes → 14:45"), and the hidden gap also made the offered
  grid and the conflict check disagree. A per-salon turnaround time is a product decision, not a constant — open
  question for the user.
- T2 (3.3) The 7-day window binds CUSTOMERS only. A salon books its own diary as far ahead as it likes; the rule
  the tester stated was about "کسی" booking online. A service or salon may set a shorter window; a longer stored
  one is capped, so the rule holds without a data migration.
- T2 (3.5) A booking held against the organisation blocks every resource of that salon; a member's booking blocks
  only that member. Over-blocking a multi-chair salon is possible in theory, but in practice organisation-level
  bookings are one-person salons (the apps send a member id when members exist), and offering a time that cannot
  be booked is worse.

- T2 (1.1) "Both" satisfies the provider policies, because the domain already says a Both person can act as a
  provider (`User.CanActAs`); the per-request ownership/membership checks still decide which salon. Chosen over
  issuing the session's capacity in the refresh token, which would need the refresh to know which app asked.
- T1 (3.0) Existing customer-app bookings stored 3h30 early are NOT rewritten: which rows came from the Flutter app
  (vs Vue, provider app, walk-in) is not recorded on the booking, and a wrong guess moves a correct booking. Listed
  as an open question for the user.

## Log

- 2026-09-22 FULL verify: one run failed on two review tests with Postgres `40P01: deadlock detected` — the two
  parallel collections deadlocking, not a logic failure (both pass alone, and the re-run was 20/20 PASS, 782
  integration tests). Second flake of the day in that suite; if it recurs it deserves its own look.

- 2026-09-22 3.3 RED `BookingHorizonTests` (a booking 14 days out was accepted; no window was published) →
  `BookingHorizonPolicy` (7 days, 7 unit tests): enforced for CUSTOMER bookings only — a salon still writes its own
  book months ahead — and published on provider detail so the clients bound their pickers. Customer app date strip
  and Vue `TimeSlotModal` (which allowed three months) now stop at the window; `bookingWindowMaxDate` has its own
  spec. A salon or service that sets a SHORTER window keeps it; a longer stored one (every salon says 90) is capped.
- 2026-09-22 3.2 RED unit tests on `EnumerateSlotStartMinutes` → candidate starts now include each booking's end, so
  a 45-minute booking at 14:00 offers 14:45 instead of the next grid mark at 15:00; starts that would run into a
  booking, and gaps too short for the visit, are left out. The hidden 15-minute buffer is 0 (see Decisions).
- 2026-09-22 3.5 found while proving 3.2 end to end: `BackToBackSlotsTests` showed the slot list offering a time
  that creation then refused with 409. Slots are computed per bookable member, but a salon-made booking is held
  against the ORGANISATION, so it blocked nothing. An organisation-level booking now blocks that salon's resources.
- 2026-09-22 4.2 RED `nearby_providers_cubit_test` → "nearest" uses `/Providers/by-location`, which applies the
  radius server-side and returns each salon's distance; `/Providers/search` has no radius parameter at all, so the
  10 km was silently dropped and the nearest salon in the country won. A fix coarser than 20 km (an IP/VPN guess)
  is no longer called "nearest" — the map cubit already refused those.
- 2026-09-22 4.3 RED `person_name_test` + `complete_name_page_test` → a customer whose account still carries the OTP
  placeholder is asked for a name once, right after sign-up, carrying their return-to-intent target; skipping is
  allowed. `otp_return_to_intent_test`'s session fixture was given a real name (it tests return-to-intent, not
  naming) and a new case covers the nameless path.
- 2026-09-22 `ReviewModerationTests.A_second_approval_of_the_same_provider_averages_both` failed once in a full
  parallel run (1 ms) and passed alone and on a full re-run (782/782). Flake, not a regression — worth watching.

- 2026-09-22 3.1 RED (datasource kept the messages; repository dropped them) → `DaySlots{slots, reason}` through
  repository, bloc and `SlotPicker`, so an empty day shows the salon's own answer («مجموعه در این روز تعطیل است»,
  «ساعات کاری این روز … کافی نیست») instead of the generic line. The "day too short" text no longer tells the reader
  to change settings, which a customer cannot do. The salon's own cause for 1 Mehr still needs its live response.
- 2026-09-22 3.4 `PersonName.RealOrNull` (7 unit tests) applied to the staff roster and the bookable-resource names,
  so a member whose account still carries «ارائه‌دهنده <digits>» shows the salon's display name instead of a phone
  number; the customer app's label is «ارائه‌دهنده», not «متخصص». Fixing the stored placeholder itself is 4.3.

- 2026-09-22 1.4 RED `NotificationInboxTests` (a legacy HTML row shows) → the inbox list and the unread count both
  skip rows with no event code and an HTML body; a legacy PLAIN-TEXT row still shows, so this is not a blanket purge
  of pre-outbox rows. Nothing is deleted from production data. 178 notification tests green.
- 2026-09-22 4.1 RED `ProviderPhotosReachCustomersTests` with the shape storage really writes (relative paths — the
  two older tests used absolute fixtures, which is why they never caught it) → `IUrlService.AbsoluteOrNull` applied in
  search, by-location, by-id (logo, profile image, every gallery size), profile, by-owner, by-status, service detail
  and the author's review list. 729 provider/service/admin integration tests green.
- 2026-09-22 4.4 `ForwardChevron` (customer app) + tests: `Icons.chevron_right` already mirrors in RTL, so picking
  `chevron_left` by hand flipped it twice. Provider app: the same double-flip in five places — the week arrows were
  an inverted pair (previous pointed left, next right) and four row affordances pointed right; fixed with a test on
  the week pair. Customer app 334 green, provider app 614 green, both analyze clean.

- 2026-09-22 1.2/1.3 closed by 1.1 + 1.5 + 2.1: the booking and its `NewBookingRequest` both existed; the salon could
  not load bookings (403), the notice read «مشتری گرامی», and it could not be tapped. Home still shows only today and
  tomorrow by design; a booking further out is on the calendar, which a tap now opens.
- 2026-09-22 2.1 RED `BookingNotificationNamesTests` (customer and walk-in names absent) → `IBookingNotificationParameters`
  builds salon, time, service and the customer's real name for all seven booking raise sites and the reminder
  scheduler (walk-ins from the customer book; placeholder «مشتری <digits>» is no name). Salon-facing copy falls back
  to «یک مشتری», not «مشتری گرامی». 258 booking/notification integration tests green.
- 2026-09-22 2.2 `BookingSmsText.PersianDate` → «دوشنبه ۳۰ شهریور», `PersianDateTime` → «…، ساعت ۰۹:۰۰», shared by the
  notification copy and the walk-in SMS. The tests pinning the old slash format were changed deliberately (the
  requirement changed); they and the salon-fallback tests were written in the same step as the code, not observed
  red first. Application 245/245, SMS + notification integration 178/178.

- 2026-09-22 1.5 RED `SalonInboxActionabilityTests` (owner's own booking notice not actionable) → the resolver
  counts the reader as a party when they own the booking's salon or hold an active membership there; another
  salon's owner still cannot open it; 19 notification tests green. Provider app: a booking notice routes to
  `/calendar?booking=<id>`; `CalendarCubit.openBooking` fetches the booking (`GET /Bookings/{id}`), moves to its
  week and day, and the view opens its confirm/decline sheet once. RED first (destination, cubit, view); 613 green.
- 2026-09-22 5.1 closed without code: the calendar already pages week by week and selects any day; the tester saw it
  empty because every call 403'd (1.1).

- 2026-09-22 3.0 RED `booking_request_payload_test` / `reschedule_payload_test` (14:00 went out as 10:30Z, 16:30 as
  13:00Z) → `wallClockIso` in `core/utils/wall_clock.dart` for create and reschedule; 57 booking tests, analyze clean.
- 2026-09-22 1.1 RED `TwoSidedPersonTests` (owner with user_type=Both → 403 on its own bookings) → `ProviderOnly`,
  `ClientOrProvider`, `ProviderOrAdmin` admit "Both"; a two-sided owner is still 403 on another salon. 3/3 + admin and
  membership authorization tests green.
