Status: DONE
Verify: FAST

User report (2026-09-19, provider app, booking screen):
1. The staff picker shows «ارائه‌دهنده 9123135143» — meaningless.
2. A customer typed on the booking screen should become one of the salon's customers if new
   (no duplicate numbers), and
3. get a suitable SMS after the booking (with a payment link later — there is no gateway yet).

## Findings (measured)
- The name is the placeholder phone sign-in gives every account; «سالن نهال» registered before the
  draft started adopting owner names, and its provider row has no owner name either. Nothing in the
  app lets a person set their name: PUT /Users/{id}/profile has no handler (FOLLOW-UPS #64).
- Production sends no real SMS: Rahyab SandboxMode=true and no SMS credentials on the server (the
  same reason OTP 222222 works, #58). SMS built here reaches phones once a provider is configured.
- A salon-entered booking's CustomerId is the owner, so BookingConfirmedNotificationHandler
  addresses the "your booking is confirmed" notice (English) to the OWNER.

## Decisions (tier 1-2, recorded)
- Names: the person edits their own first/last name (More page); phone is NOT changeable there —
  it is the sign-in identity (a change is refused).
- Walk-in: server-side. When the salon books with a name + mobile and no picked customer, the
  number is looked up in the book (any spelling); found -> linked, saved name untouched; new ->
  saved (source Booking) and linked, in the same transaction as the booking. Invalid mobile -> 400.
- Customer identity is REQUIRED on a salon-entered booking (user, 2026-09-19): either a picked
  customer, or a name + mobile — the app marks both required inline, and the server refuses a
  provider-created booking without them (online customer bookings are unaffected).
- SMS: to the linked customer's phone, Persian, Jalali date: «{نام} عزیز، نوبت {خدمت} شما در {سالن}
  {روز} {تاریخ} ساعت {ساعت} ثبت شد.» A checkbox on the booking screen (default on, shown when a
  number is entered) lets the salon skip it. Never sent to the owner for their own walk-ins. The
  payment link is added when a gateway exists.

## Tasks
- [x] 1.1 Backend: UpdateUserProfileCommandHandler (first/last name; phone change refused); test
- [x] 1.2 Backend: walk-in name+phone on CreateBooking -> find-or-save customer + link; tests
- [x] 1.3 Backend: Persian booking SMS to the linked customer (opt-out flag); owner no longer gets
      the customer notice for their own walk-ins; tests with the capturing SMS fake
- [x] 1.4 App: composer sends walk-in fields + SMS checkbox; phone valid / name required when a
      phone is given (inline); tests
- [x] 1.5 App: More -> "نام شما" edit; tests
- [x] 1.6 Verify, deploy, confirm live

## Log
- 2026-09-19 1.1 UpdateUserProfileCommandHandler: name only, empty name 400, a different phone 400
  (sign-in identity), somebody else's profile 403. 4 tests.
- 1.2 A salon-entered booking must name its customer: picked from the book, or name + mobile, which
  is then found-or-saved (source Booking) and linked, in the booking's own transaction. Invalid or
  missing number 400; a saved name is never overwritten. 4 tests + the older walk-in test updated.
- 1.3 BookingSmsText (Persian, Jalali, salon wall-clock; 4 unit tests) sent from the booking handler
  — an event handler could not do it: events are dispatched BEFORE the save, so a handler that
  re-reads the booking finds nothing (corrected 2026-09-20, FOLLOW-UPS #66); the send is last, after every write, and never fatal.
  Booking.NotifyCustomer (migration AddBookingNotifyCustomer, default true) carries the salon's
  opt-out. The owner no longer gets the English "your booking is confirmed" notice for their own
  walk-ins. 2 integration tests with the capturing SMS fake.
- 1.4 Composer: name and mobile are required (red *, inline errors; an invalid mobile says so),
  the walk-in fields go to the server, and a checkbox (on by default) carries the SMS choice.
  3 new widget tests; the older submit/failure tests now name a customer, as the rule demands.
- 1.5 More -> «نام شما»: the same form without a phone (the sign-in number is not editable there),
  PUT /Users/{id}/profile, then the token is re-minted so this device shows the new name. 2 tests.
  App: 553 tests pass, analyze clean.
- 2026-09-20 verify FULL PASS (17 steps, 467 s). Deployed (6cfe7e7d).
  Live check on back.nahalkmi.ir as «سالن نهال»: a booking with no customer -> 400 «شماره موبایل
  مشتری الزامی است»; «12» -> 400 «معتبر نیست»; name + mobile -> 201 and the customer appears in the
  book as source Booking with 1 booking. Test booking cancelled, test customer removed; the salon's
  own three customers untouched.
- 2026-09-20 DEFECT found by the live check, fixed and deployed (0a72040c): UsersController,
  ProviderSettingsController and ReviewsController read the caller from "sub"/"userId", but the
  production JWT carries nameidentifier — so renaming yourself answered 403 for every real user.
  The test auth handler minted a "userId" claim the real token never issues, which is why the
  tests were green; that claim is gone. 521 integration tests pass. Naming yourself now returns
  201 on production.
- Note: production still runs SMS in sandbox mode with no gateway credentials, so the booking SMS
  is composed and dispatched but reaches no phone until an SMS provider is configured (FOLLOW-UPS
  #58 covers the same switch).
- 2026-09-20 CORRECTION, found by probing rather than reading: ServiceCatalog domain events ARE
  dispatched (a booking POST writes 2 Notifications rows) — but BEFORE SaveChangesAsync, so the
  guard shipped here, which re-read the booking to see whether the salon entered it, never fired:
  the owner kept getting the English "your booking is confirmed" notice. It now decides from the
  event and the provider (committed earlier), with a test that fails on the old behaviour.
  FOLLOW-UPS #66 rewritten with the measurement.
