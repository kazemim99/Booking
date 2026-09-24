Status: DONE
Verify: FULL

User report (2026-09-24 evening): a 3:14 screen recording (23:35–23:38 Tehran) of production after the 09-24 deploy: a
new customer signs up, enters a name, books «اصلاح کامل» at سالن نهال for Friday 10:00, the salon confirms in the
provider app, then two reschedule attempts fail. User said "go" on the triage.

## Findings

- F1 Reschedule refuses a slot its own screen offers («The requested time slot is not available»). Since the 09-22 QA
  the free-slot grid and booking creation keep a ZERO gap after an appointment (`AvailabilityService.BufferTimeMinutes
  = 0`); `RescheduleBookingCommandHandler` still has `BufferMinutes = 15`, so a 30-minute move to 12:30 "conflicts"
  with anything starting at 13:00. Same class as the 09-22 grid-vs-conflict fix, missed in this handler.
- F2 The 24-hour reschedule rule (`BookingPolicy.RescheduleWindowHours`, default 24) is discovered only at the last
  step: button, screen and time picker all proceed, and the server checks slot availability BEFORE the window, so the
  first attempts showed the misleading F1 error instead of the window. The rule itself fired correctly (booking Fri
  10:00, attempted Thu night, ~10 h ahead).
- F3 Errors shown to customers and salons are English server messages («The requested time slot is not available»,
  «Rescheduling must be done at least 24 hours before the booking», «Booking validation failed: …», …).
- F4 The salon's booking list (`GET /Bookings/provider/{id}`) carries no customer name at all: the provider app read
  `customerName` and got nothing, so the request card and sheet showed «بدون نام» for «ناصر عابدی» while the
  notification (which resolves the name) said «ناصر عابدی». The 09-24 request-row change made the missing name louder.
- F5 (customer app) After the post-signup «نام شما» save the profile page showed only the phone and «ویرایش پروفایل»
  opened with empty fields: the page had saved (its own success gate) but the profile screen kept its pre-save state.

## Acceptance scenarios

- Given a confirmed booking at 13:00 and a pending booking Friday 10:00–10:30, when the customer moves the second to
  12:30 (a slot the grid offers), then it is accepted.
- Given a reschedule attempt inside the reschedule window, when the slot is ALSO taken, the answer is the window
  message, not "slot not available".
- Every error a customer or salon can meet while booking, cancelling or rescheduling reads in Persian.
- Given a booking by a customer named «ناصر عابدی», the salon's booking list carries `customerName` = that name; a
  walk-in carries the salon's own book name; a placeholder-named customer carries none (never a phone).
- Given the customer saved their name on the post-signup page, the profile page shows it and the edit sheet is
  prefilled.
- A booking inside its reschedule window shows «تغییر زمان» disabled with the reason, not a dead end at the end.

## Tasks

- [x] 1 F1 reschedule uses the same zero gap as creation and the grid (test: every offered slot is accepted)
- [x] 2 F2 server checks the reschedule window before slot availability
- [x] 3 F2 customer apps (Flutter + Vue): «تغییر زمان» disabled with the reason inside the window
- [x] 4 F3 Persian messages for the booking/cancel/reschedule errors
- [x] 5 F4 salon's booking list carries the customer's real name
- [x] 6 F5 profile shows the saved name after the post-signup save
- [x] 7 FULL verify

## Decisions

- D1 (tier 2) The reschedule window stays at the platform/salon policy (default 24 h); the recording showed the rule
  worked. The gap is the UX: inside the window the button is disabled with the reason and «برای تغییر با سالن تماس
  بگیرید». Whether the default should be shorter is a product call, flagged to the user, not changed.
- D2 (tier 2) `customerName` only — no phone on the salon's booking list (the salon's book already has phones; adding
  them to every list row is a privacy call nobody made).
- D3 (tier 2) Errors are translated at the source (server messages), so every client — web, Flutter, admin — benefits.

## Log

- Seen failing first: 9 Persian-rule unit tests; 5 integration (offered slot refused, window hidden behind slot error,
  English overlap message, no customerName x2); profile-follows-session (proved again by disabling the fix). Written
  before the code but first run after it: the reschedule-reason tests (server DTOs, Flutter parser/card/detail, Vue
  RescheduleAction/mapper) — the field did not exist, so they could not have passed.
- Old English assertions updated: BookingAggregateTests deposit test now checks «بیعانه».
- Vue BookingCard.vue is unreferenced dead code (its own «تغییر زمان» left alone); the live web reschedule is the
  My Bookings sidebar (now RescheduleAction).

- FULL PASS on 2ef9d7c8 (20 steps, 725 s): integration 851/851, Vue web+admin, customer + provider apps.
  Not pushed or deployed: waits for the user's "deploy and push".
- 2026-09-25 branch fix/qa-walkthrough-2026-09-25 on 64e1d3d1 (deployed master) in worktree .claude/worktrees/qa0924.
