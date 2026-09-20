# Design — notification system

## The finding that shapes this change

**Corrected 2026-09-20.** An earlier draft of this document claimed ServiceCatalog domain events are never
dispatched, and built the case for an outbox on that. **That was wrong, twice over**, and the record of how
matters more than the conclusion — which survives.

I grepped for direct `DispatchDomainEventsAsync` call sites, mapped them to their enclosing methods, then
checked whether those methods had callers by searching for `_unitOfWork.<name>`. That search could not find
`CommitAndPublishEventsAsync`, which is called *internally* in the same file, and I never grepped
`SaveAndPublishEventsAsync` by name at all — it has about 35 callers. A peer session settled it empirically:
a `POST /api/v1/bookings` takes `ServiceCatalog."Notifications"` from 0 to 2 rows, which only
`BookingConfirmedNotificationHandler` writes. The handlers run.

### What is actually true

Domain events **are** dispatched, and the order differs per command:

| Path | Route | Order |
|---|---|---|
| Booking, payment — every command via `TransactionBehavior` | `ExecuteInTransactionAsync<T>` (225) → `CommitAndPublishEventsAsync` (344) | **dispatch, then save** |
| Non-generic overload | `ExecuteInTransactionAsync` (298) → `SaveChangesAsync` (36) | **dispatch, then save** |
| Membership, staff, provider-customer, all UserManagement | `SaveAndPublishEventsAsync` (368), called directly | save, then dispatch |
| UserManagement generally | `UserManagementDbContext.SaveChangesAsync` (74) → `CollectDomainEvents` (126) | save, then dispatch |

So the defect is not absence, it is **ordering, applied inconsistently**. On the booking and payment paths a
handler runs while the aggregate that raised the event is not yet in the database.

### Why that is the hazard this change must design around

Two failures follow from dispatch-before-save, and both are live:

1. **A handler that re-reads its subject gets null and silently does nothing.** This has already bitten
   twice in this codebase: the booking SMS (which is why it is sent from the command handler instead) and the
   guard meant to stop a salon owner being notified about their own walk-in, which shipped broken.
2. **A side effect can escape for work that is then rolled back.** The handler sends before the transaction
   commits, so a failed booking can still produce a delivered SMS. This is the same family as publishing an
   integration event before commit.

A notification system built on these handlers would inherit both. That — not a dead dispatcher — is the
reason it must not be built on them.

Recorded as FOLLOW-UPS #66. Fixing the order globally is explicitly *not* this change's business: handlers
have run against pre-save state for as long as they have existed, and some of them post to the ledger.

## Decision 1 — how notifications get raised

Three options:

**(a) Keep raising from domain event handlers, and fix the ordering.** Flip the booking/payment path to
save-then-dispatch, as `SaveAndPublishEventsAsync` already does for membership and UserManagement.

*Rejected for this change.* The flip is one line and its blast radius is the whole context. Every
ServiceCatalog handler has run against **pre-save** state for as long as it has existed, and some were
written — knowingly or not — around that. Changing what they observe is a behavioural change to cache
invalidation, provider activation, staff management and `LedgerPostingEventHandlers`, which posts to the
ledger. That is a real change worth doing, per-handler, with a test per handler proving what it now sees. It
is not a notification feature's business, and a notification feature must not be the thing that discovers
whether ledger posting survives the flip.

**(b) Send inline from command handlers**, as the walk-in change does.

*Rejected as the general rule.* It works for one SMS, but it puts delivery inside the request path: the send
is either inside the transaction (so a gateway timeout rolls back a real booking) or outside it (so a crash
between commit and send loses the notification silently — which is what the walk-in code accepts today by
being "never fatal"). It also scatters recipient/channel/copy decisions across handlers, which is precisely
the state this change exists to end.

**(c) A transactional outbox. — chosen.**

The command handler records a *notification intent* row in the same transaction as the business write. A
background sweep turns intents into `Notification` aggregates and dispatches them through the existing
`INotificationDispatcher`.

Why this one:
- **It is immune to the ordering defect.** The sweep runs after the transaction has committed, by
  construction. It does not matter whether the handler that raised the intent ran before or after the save,
  and it will keep working unchanged when someone does fix the ordering.
- **Nothing escapes for rolled-back work.** The intent dies with the transaction. Raising from a pre-save
  handler cannot give that: the send has already left.
- **A handler need not re-read its subject.** The intent carries its parameters, so the null-on-re-read trap
  that broke the booking SMS and the walk-in owner guard cannot recur.
- **Off the request path.** A slow gateway cannot slow down or fail a booking.
- **Feeds machinery that already exists.** `ProcessScheduledNotificationsJob` already sweeps due
  notifications through the dispatcher with preference gating, de-duplication, retry and dead-lettering. The
  outbox gives it something to sweep.
- **Reminders fall out of it.** A scheduled reminder is an intent with a future `ScheduledFor` — the same
  row shape, no second mechanism.

The ordering defect is documented as FOLLOW-UPS #66 and left alone. This change neither depends on it being
fixed nor is disturbed when it is.

## Decision 2 — the catalogue is the contract

One static table keyed by a stable `NotificationEventCode`, holding for each notification: **audience**
(customer / provider / staff), **channels**, **criticality**, **suppressibility**, and **tap destination**.
A notification cannot be raised without an entry, because the raise API takes the code and reads everything
else from the table.

This is the shape Coliride arrived at (`NotificationEventCatalog` + `AppNotificationEventCode`), and the
rule worth copying verbatim is theirs: **criticality means "the recipient may not switch this off", so
membership is a product decision** — not a styling one, and not inferred from a design file.

Per the user's decision (2026-09-19), SMS is reserved for critical notifications: OTP, booking confirmed,
cancellation, the T-2h reminder, and money movement. Everything else is push + in-app.

### Why a new code enum rather than extending `NotificationType`

`NotificationType` is declared `[Flags]` but its members from `16777216` up are sequential integers, not
distinct bits, so `HasFlag` reports false positives — `RefundIssued` "contains" `RefundProcessed`.
`NotificationSuppressionPolicy` already documents this and works around it with a `HashSet`. Rather than
extend a broken type, `NotificationEventCode` is introduced as a plain (non-flags) enum, and `NotificationType`
is reduced to what it is actually good at: the coarse preference category a user toggles. The migration of
persisted preference masks is its own task.

## Decision 3 — destination resolved on read

A notification's title and body are history and stay as written. Its tap destination is not: a row about a
booking that was later cancelled must not still open the old detail screen. Destinations are therefore
recomputed when a page of the inbox is read, from current entity state, in one batched lookup per page —
Coliride's `INotificationDestinationResolver` pattern. History stays immutable, links never go stale, and no
fan-out write is needed when a booking changes.

## Decision 4 — push is server-side only in this change

Real FCM replaces the stub, plus a device-token registry. The Flutter apps are **not** wired here: neither
has `firebase_messaging`, and Android builds cannot be verified in this environment (Google Maven 404s). The
server contract lands and is testable; the apps follow in their own change.

Removing the stub is itself a correctness fix. `FirebasePushNotificationService` currently returns
`(true, Guid.NewGuid(), null)` without sending, so the delivery log records deliveries that never happened —
a falsehood that is undetectable from the data.

## Shape

```
command handler ──(same transaction)──> NotificationOutbox row
                                              │
                                   background sweep
                                              │
                                   NotificationEventCatalog  ── audience, channels, criticality
                                              │
                                   Notification aggregate (existing)
                                              │
                                   INotificationDispatcher (existing)
                                    ├─ preference gate + suppression policy (existing)
                                    ├─ de-duplication (existing)
                                    ├─ Email / SMS (existing, real)
                                    ├─ Push  → FCM + device-token registry   (NEW)
                                    └─ InApp → persisted inbox + SignalR      (NEW persistence)
                                              │
                                   NotificationDeliveryLog (existing)
```

Everything below the catalogue already exists and works. This change builds the two boxes above it and the
two marked NEW.

## What this change deliberately does not do

- Fix the dispatch-ordering defect. Documented here and in FOLLOW-UPS #66, not depended on either way.
- Retire the existing notification event handlers wholesale. They currently work, after a fashion, and the
  outbox supersedes them path by path: each is removed only once its coverage is replaced and tested, so the
  product never has a window with neither.
- Wire the Flutter apps.
- Replace CAP or change the integration-event path.

## Open questions for product

- **Reminder timing.** T-24h and T-2h for the customer, T-30m for the provider are assumptions, not decisions.
  They set SMS cost directly (the T-2h one is the SMS).
- **Daily provider digest.** Worth sending at all, and at what hour in salon-local time?
- **Review requests.** How long after completion, and how many times.

These do not block the catalogue, the outbox, the inbox or push: each is a row's timing, not a structural
choice. They are listed in `tasks.md` as `[?] DECISION:` rather than guessed at.
