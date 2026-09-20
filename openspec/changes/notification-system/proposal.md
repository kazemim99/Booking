# Notification system

## Why

Booksy tells its users almost nothing. A survey of the domain event surface turns up **37 notifications a
customer or provider needs. Two of them actually reach a human.**

Those two are the OTP SMS and the Persian booking SMS added by `walk-in-customer-name-sms`, both sent
directly from a command handler. Everything else is dead, and not for the reason it looks like: **ServiceCatalog
domain events are never dispatched.** `TransactionBehavior` commits every command through
`EfCoreUnitOfWork.ExecuteInTransactionAsync`, which does not dispatch, while the four methods that do
dispatch are called only by five gallery-image handlers. UserManagement does not share the defect — its
`DbContext.SaveChangesAsync` dispatches — so the two contexts behave differently. The result is that all ten
notification event handlers (five booking, four payment, one invitation) have never executed. `design.md`
has the full trace.

On top of that dead plumbing sit the ordinary gaps: a provider is never told a booking request arrived, and
nobody is ever reminded of an appointment — the `BookingReminder` type, its seeded template and the
`ProcessScheduledNotificationsJob` sweep all exist, and nothing ever creates the scheduled row.

Worse than the gaps, one path lies. `FirebasePushNotificationService` logs a warning and returns
`(true, Guid.NewGuid(), null)` — so every push is recorded in the delivery log as delivered, and the
falsehood is undetectable from the data. There is no device-token registry to push to and no persisted
inbox to read on app open; in-app notification is fire-and-forget SignalR with a throwaway id.

And the ten dead handlers were all written to send English HTML, assembled inline, with Gregorian dates —
to an Iranian audience reached over Kavenegar/Rahyab SMS. Reviving them as-is would ship that; the copy has
to move into templates as part of bringing them back.

The delivery *mechanics* are sound — `NotificationDispatcher` already enforces preference gating,
per-(event, channel, recipient) de-duplication, retry with backoff and dead-lettering, all covered by the
existing `notification-delivery` spec. It is correct, tested machinery that **nothing feeds**. What is
missing is the layer above it: something that raises notifications reliably, and a statement of **what gets
sent, to whom, on which channel, and how urgently**.

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
