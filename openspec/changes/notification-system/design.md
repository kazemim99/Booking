# Design — notification system

## The finding that reshapes this change

A peer session finishing `walk-in-customer-name-sms` left this in its log:

> Booking's domain events are never dispatched in this codebase (no publisher), so an event handler would
> have been dead code.

That is correct, and the real scope is wider and more precise than "no publisher". There *are* three
dispatcher classes and a registered `IDomainEventDispatcher`. The break is that **the save paths the code
actually uses are not the save paths that dispatch.**

`EfCoreUnitOfWork` dispatches in exactly four methods
(`src/Infrastructure/Booksy.Infrastructure.Core/Persistence/Base/EfCoreUnitOfWork.cs`):

| Method | Line | Dispatches | Who calls it |
|---|---|---|---|
| `SaveChangesAsync` | 36 | **yes** (41) | 5 gallery-image command handlers — nothing else |
| `CommitAndPublishEventsAsync` | 344 | **yes** (349) | nobody |
| `SaveAndPublishEventsAsync` | 368 | **yes** (378) | nobody |
| `PublishEventsAsync` | 390 | **yes** (394) | nobody |
| `CommitAsync` | 210 | no | every payment and provider-registration handler |
| `ExecuteInTransactionAsync` | 225, 298 | no | `TransactionBehavior` — i.e. **every command** |
| `CommitTransactionAsync` | 75 | no | — |

`TransactionBehavior` wraps every non-query command in `ExecuteInTransactionAsync`, which commits without
dispatching. Booking handlers additionally persist through `BookingWriteRepository`, whose `SaveBookingAsync`
is a bare `DbSet.AddAsync`, and `ServiceCatalogDbContext.SaveChangesAsync` is a pass-through to `base`.

**The two contexts differ, and that asymmetry is the whole story:**

- **UserManagement dispatches.** `UserManagementDbContext.SaveChangesAsync` (line 74) calls
  `CollectDomainEvents` (126), which dispatches and clears. Its domain events reach their handlers.
- **ServiceCatalog does not.** No equivalent hook, and the UnitOfWork methods in use don't dispatch.

### Consequence

Every `IDomainEventHandler` in ServiceCatalog is dead code unless its event happens to be raised inside one
of the five gallery commands. That includes **all ten notification handlers**: the five under
`EventHandlers/Bookings/`, the four under `EventHandlers/Payments/`, and `InvitationSentNotificationHandler`.

So the earlier catalogue's "~15 wired" was wrong. What actually reaches a human today is:

1. The OTP SMS, sent directly from a UserManagement command handler.
2. The Persian booking SMS that `walk-in-customer-name-sms` just added, sent directly from
   `CreateBookingCommandHandler` — deliberately not via an event handler, for exactly this reason.

Everything else in the notification subsystem — dispatcher, delivery log, de-duplication, retry, templates,
preferences — is correct, tested machinery that **nothing ever feeds**.

## Decision 1 — how notifications get raised

Three options:

**(a) Turn on ServiceCatalog dispatch globally.** Add a `CollectDomainEvents` hook to
`ServiceCatalogDbContext` mirroring UserManagement's, or make `ExecuteInTransactionAsync` dispatch.
One small change; everything lights up.

*Rejected.* It would wake 29 handlers simultaneously — cache invalidation, ledger posting, provider
activation, staff changes — none of which has ever executed in production. Their correctness is unknown and
untested under real dispatch. Ledger posting in particular moves money. A notification feature must not be
the thing that discovers whether `LedgerPostingEventHandlers` is safe to run.

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
- **Atomic.** No notification for a rolled-back booking; no lost notification after a successful one. Neither
  (a) nor (b) gives both.
- **Off the request path.** A slow gateway cannot slow down or fail a booking.
- **Feeds the machinery that already exists.** `ProcessScheduledNotificationsJob` already sweeps due
  notifications through the dispatcher with preference gating, de-duplication, retry and dead-lettering. The
  outbox gives it something to sweep.
- **Independent of the dead event plumbing.** It neither depends on dispatch being fixed nor prevents fixing
  it later.
- **Reminders fall out of it.** A scheduled reminder is an intent with a future `ScheduledFor` — the same
  row shape, no second mechanism.

The dispatch defect is documented and left alone. It is a real bug worth its own change, with its own
per-handler testing; it is not this change's to fix, and this change must not depend on it.

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

- Fix the ServiceCatalog domain-event dispatch defect. Documented here, raised as a follow-up, not depended on.
- Wake the ten dead notification handlers. They stay dead; the outbox supersedes them. Deleting them is
  cleanup for the follow-up that fixes dispatch, since that is when their fate is actually decided.
- Wire the Flutter apps.
- Replace CAP or change the integration-event path.

## Open questions for product

- **Reminder timing.** T-24h and T-2h for the customer, T-30m for the provider are assumptions, not decisions.
  They set SMS cost directly (the T-2h one is the SMS).
- **Daily provider digest.** Worth sending at all, and at what hour in salon-local time?
- **Review requests.** How long after completion, and how many times.

These do not block the catalogue, the outbox, the inbox or push: each is a row's timing, not a structural
choice. They are listed in `tasks.md` as `[?] DECISION:` rather than guessed at.
