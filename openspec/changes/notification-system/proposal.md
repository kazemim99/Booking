# Notification system

## Why

Booksy's notification subsystem is more built than it looks and less trustworthy than it reads. The
dispatcher, delivery log, de-duplication, retry, templates and preferences are real and tested. What sits on
top of them is not.

**One channel lies.** `FirebasePushNotificationService` logs a warning and returns
`(true, Guid.NewGuid(), null)` without sending. Every push is recorded in the delivery log as delivered, and
the falsehood is undetectable from the data — the log is worthless exactly where it is meant to be evidence.
There is no device-token registry to push to anyway.

**Notifications are raised from a handler that runs before the commit.** `TransactionBehavior` routes every
booking and payment command through `EfCoreUnitOfWork.ExecuteInTransactionAsync<T>`, which calls
`CommitAndPublishEventsAsync` — that dispatches domain events and only *then* saves. So a notification
handler fires while the booking that triggered it is not yet in the database. Two consequences, both live
today: a handler that re-reads its subject by id gets null and silently does nothing, and a notification can
escape for work that is subsequently rolled back. Membership, staff and all UserManagement commands use
`SaveAndPublishEventsAsync` and get the opposite, correct order — so the ordering is inconsistent per
command, which is worse than being uniformly wrong. Recorded as FOLLOW-UPS #66.

**Nobody is ever reminded of an appointment.** The `BookingReminder` type, its seeded template and the
`ProcessScheduledNotificationsJob` sweep all exist; nothing ever creates the scheduled row.

**A provider is never told a booking request arrived.** `BookingRequestedEvent` has no handler, so the moment
a salon turns on `Provider.RequiresApproval`, requests expire in silence.

**Nothing can be read back.** In-app notification is fire-and-forget SignalR with a throwaway id: no
persistence, no read-state, no unread count. A client that was offline when it fired never learns of it.

**And there is no statement of what the product sends.** Audience, channel, urgency and suppressibility are
scattered across handlers, discoverable only by grep. A survey of the domain-event surface turns up **37
notifications a customer or provider needs**; the copy that exists is English HTML with Gregorian dates,
assembled inline, for an Iranian audience reached over Kavenegar/Rahyab SMS.

## What Changes

- **A transactional outbox for raising notifications.** The command handler records a notification intent in
  the same transaction as the business write; a background sweep feeds it to the existing dispatcher. This
  replaces the dead event handlers without depending on the dispatch defect being fixed, and without putting
  a gateway call on the request path. `design.md` records why this was chosen over turning dispatch on
  globally (it would wake 29 never-executed handlers, including ledger posting) or sending inline.
- **A typed notification catalogue.** One table mapping each notification to its audience, channels,
  criticality and tap destination. A notification cannot be added without declaring these. This is what
  makes the 37 entries above reviewable instead of discoverable only by grep.
- **Provider-side coverage.** New booking request, cancellation by customer (actor-aware, today's handler
  cannot tell who cancelled), staff invitation accepted, join request approved, staff assigned to a booking,
  payout failed/on-hold, and verification status changed — `ProviderVerificationStatusChangedEvent` is a
  rich event that nothing currently consumes.
- **Scheduled booking reminders.** T-24h and T-2h for the customer, T-30m next-appointment for the provider,
  scheduled when a booking is confirmed and cancelled when it is not. Uses the existing sweep.
- **Real push.** FCM replacing the stub, plus a device-token registry (register/refresh/revoke) and
  per-platform handling. Removing the fake success is itself a correctness fix.
- **A persisted inbox.** Read-state, unread count, mark-read/mark-all-read, backed by the `Notification`
  rows that `GET /notifications/history` already reads.
- **Persian copy via templates.** Notification text moves out of handlers into the existing (seeded,
  unused) template table, rendered with Jalali dates and salon-local times.
- **SMS restricted to critical notifications.** Per-message cost in Iran makes this a budget decision as
  much as a product one: OTP, booking confirmed, cancellation, the T-2h reminder, and money movement.
  Everything else rides push and in-app.
- **BREAKING — `NotificationType` stops being a `[Flags]` enum.** Its members from `16777216` up are
  sequential integers rather than distinct bits, so `HasFlag` reports false positives (`RefundIssued`
  "contains" `RefundProcessed`). `NotificationSuppressionPolicy` already documents this and works around it
  with a `HashSet`; anything masking *preferences* on this enum is silently unreliable. Persisted values
  and preference masks need a migration.

Out of scope, deliberately: wiring the Flutter apps. Neither app has `firebase_messaging`, any inbox UI or
an unread badge, and Android builds cannot be verified in this environment (Google Maven 404s here). The
server contract lands first; the apps follow in their own change.

## Capabilities

### New Capabilities
- `notification-raising`: the transactional outbox — a notification intent recorded with the business write,
  swept into the dispatcher, exactly once.
- `notification-event-catalog`: the per-notification contract — audience, channels, criticality,
  suppressibility and tap destination — and the rule that a notification cannot ship without one.
- `notification-inbox`: persisted in-app notifications with read-state, unread count and mark-read.
- `push-device-registry`: device-token lifecycle (register, refresh, revoke, prune stale) and real FCM delivery.
- `booking-reminders`: scheduling and cancellation of time-based reminders across the booking lifecycle.

### Modified Capabilities
- `notification-delivery`: push must actually send rather than report a fabricated success; channel
  preference masks must be evaluated against a type representation that supports reliable membership.

## Impact

**Code.** `ServiceCatalog.Application/EventHandlers/` (booking, payment, provider-hierarchy handlers),
`ServiceCatalog.Application/Services/Notifications/`, `ServiceCatalog.Infrastructure/Notifications/`
(Push, InApp, templates), `ServiceCatalog.Domain/Enums/NotificationType.cs` and
`Domain/Policies/NotificationSuppressionPolicy.cs`, plus a new aggregate for device tokens and one
ServiceCatalog migration.

**API.** Additive: device-token register/revoke, inbox list/unread-count/mark-read. `GET /notifications/history`
gains read-state fields.

**Dependencies.** FirebaseAdmin, and FCM service-account credentials per environment.

**Sequencing.** `_inline/walk-in-customer-name-sms` landed on 2026-09-20 (Status: DONE, FULL verify green,
deployed). This change builds on it rather than around it: `Services/Notifications/BookingSmsText.cs` is the
established seam for Persian/Jalali copy and the catalogue extends it; `Booking.NotifyCustomer` is the
per-booking opt-out already in the schema; and its direct-send-from-the-handler pattern is what the outbox
generalises. Its migration is in, so this change adds the next one.

**Risk.** The `NotificationType` change is the only breaking one and touches persisted preference masks;
it needs a data migration and should land as its own task with the mask rewrite tested both directions.
