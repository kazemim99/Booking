Status: DONE
Verify: FAST (PASS) + integration filtered to Notifications|API.Bookings (198 PASS) and *Booking*|*Reminder*|*Inbox* (154 PASS). FULL not run.

Production QA recording 2026-09-23: a customer booked at سالن نهال (Requested). The owner confirmed it in the
provider app (POST /api/v1/Bookings/{id}/confirm, «نوبت تأیید شد»). The customer's appointment turned «تایید شده»,
but their inbox held only the three «درخواست نوبت ثبت شد» notices. Expected: the customer is told when the salon
accepts, declines, cancels or moves their appointment.

## Findings (measured)
- Root cause: `ConfirmBookingCommandHandler` raised nothing. It scheduled reminders and stopped; no domain or
  integration handler listens to `BookingConfirmedEvent` either (the legacy English handler was deleted in
  notification-system 7.4). notification-system 7.1 wired create/cancel/complete/no-show and 7.7 reschedule;
  accepting a request was never on the list. `BookingConfirmed` only ever went out for a salon-entered booking,
  from `CreateBookingCommandHandler`. Reproduced: after confirm + sweep the customer's inbox held [bookingRequested].
- Not the cause: addressing (customer id = JWT nameidentifier — the request notices reached the same inbox), the
  catalogue (BookingConfirmed: Customer, SMS+Push+InApp, Critical, Booking destination), the copy («نوبت شما تأیید
  شد» existed), preferences (Critical is not suppressible), the legacy-HTML inbox filter (outbox rows carry a code).
- Decline (cancel of a Requested booking by the owner → BookingRejected), cancel of a Confirmed booking by the owner
  (BookingCancelledByProvider) and reschedule by the owner (BookingRescheduled) DID reach the inbox, named, with
  salon and Jalali wall-clock time — but none named the service.
- Clients: customer Flutter app `inboxDestination` maps destinationKind Booking → `/appointments/{id}`
  (inbox_page_test: "tapping a booking notice marks it read and opens that appointment"); Vue `destinationRoute`
  maps it to `BookingDetail` for a customer (destination.spec / NotificationList.spec). Neither filters by event
  code, so the new notice needs no client change. A reschedule notice points at the NEW booking and is actionable.

## Decisions (tier 1-2, recorded)
- Raise in the command handler, on its unit of work (the outbox pattern every other booking transition uses), not
  from a domain event handler (FOLLOW-UPS #66 ordering).
- No walk-in guard on confirm: only a Requested booking can be confirmed and a salon-entered booking is born
  Confirmed, so the aggregate's customer is always the real customer.
- Copy: the decline, salon-cancel and reschedule notices now name the service like the confirm/request notices do.

## Tasks
- [x] 1.1 Unit: a salon decision (confirm, reject, cancel-by-provider, reschedule) names customer, salon, service,
      day and time — failed for the three that lacked the service; copy fixed.
- [x] 1.2 Integration `SalonDecisionInboxTests`: named customer books through the API, owner acts through the API,
      sweep, CUSTOMER reads GET /Notifications/inbox. Confirm failed (inbox held only bookingRequested); decline,
      cancel and reschedule failed only on the service name. Confirm now raises BookingConfirmed; all 5 green,
      including "told once however often the sweep runs" and tap → the booking (reschedule → the new booking).
- [x] 1.3 Verify FAST + filtered integration.

## Open (not fixed here)
- ~~SECURITY: confirm had no ownership check~~ — FIXED in f5b23cf9 (qa-walkthrough-2026-09-23b): confirm, complete,
  no-show and assign-staff now require CanManageProvider on the booking's salon (the rule the salon's booking list
  uses); another salon gets 403. Tests: OnlyTheSalonActsOnItsBookingsTests.
- A deposit-paid booking confirmed by `ConfirmBookingOnDepositVerifiedHandler` gets no notice and no reminders.
  Deposits are parked (FOLLOW-UPS #64), so left alone.
- The dispatcher marks the whole notification Failed when any channel fails, and the inbox hides Failed rows: a
  Critical notice whose SMS fails is hidden from the inbox although its in-app channel succeeded, until a retry
  succeeds, and for good if it dead-letters. Production SMS is sandboxed (#58), so not the cause here.
