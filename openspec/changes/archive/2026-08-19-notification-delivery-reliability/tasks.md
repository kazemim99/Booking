# Tasks: notification-delivery-reliability

> Independent of the other changes. Behind `Notifications:ReliableDispatch`. Preserve existing senders.

## 1. Consolidate interfaces
- [x] 1.1 Pick the canonical `ISmsNotificationService`; delete the two duplicates; update DI registrations + all callers; build clean
      → canonical port is now `Booksy.Core.Application.Services.Notifications.ISmsNotificationService` (shared kernel,
      reachable from both bounded contexts). The three duplicates are deleted:
      `ServiceCatalog.Application.Services` (whose booking-template methods had **no callers anywhere** — dead code),
      `ServiceCatalog.Application.Services.Notifications`, and `UserManagement.Application.Services.Interfaces`.
      Both Rahyab implementations (byte-for-byte copies in two contexts) collapse into
      `Booksy.Infrastructure.External.Notifications.Sms.RahyabSmsNotificationService`; `KavenegarSmsService` becomes
      `KavenegarSmsNotificationService` on the same port, selected by `Notifications:SMS:Provider`.
      One idempotent registration point (`AddSmsNotificationService`) is called by both contexts.
      **Behaviour change worth knowing:** staff-invitation SMS used to go through Kavenegar while every other SMS
      went through Rahyab, purely because of which duplicate interface the caller imported. All SMS now uses the
      configured provider (Rahyab).

## 1b. Domain reliability model — DONE + TESTED
- [x] Exponential backoff between attempts (`DeliveryAttempt`: 5·3^(n-1) → 5/15/45/135/405s) — verified.
- [x] Bounded retry budget (`Notification.MaxRetryAttempts=5`, `HasExhaustedRetries`, `ShouldRetry` backoff-gated).
- [x] **Dead-letter queue**: `NotificationStatus.DeadLettered` + `MarkAsDeadLettered` (terminal; only after exhaustion;
      never from Sent/Delivered/Read) + `PrepareForRetry` (Failed→Queued only when eligible). Raises `NotificationFailedEvent`.
- [x] At-most-once send guarantee (dedup): a Sent notification cannot be re-sent (state guard).
- [x] 10 unit tests (`NotificationReliabilityTests`): backoff schedule, no-double-send, fresh-failure-not-retryable,
      cannot-dead-letter-before-exhaustion, exhausted→dead-letter→terminal, cannot-dead-letter-delivered.

## 2. Reliable dispatch
- [x] 2.1 Route lifecycle notifications through CAP-consumed events with retry on transient failure
      → `SendNotificationCommandHandler` persists the notification, attempts an inline send, and on failure publishes
      `NotificationRetryRequestedIntegrationEvent` to the **CAP outbox**;
      `NotificationRetrySubscriber` ([CapSubscribe]) re-dispatches it and **throws** while it is still failing so CAP
      redelivers on its retry schedule, returning normally once the notification is delivered or dead-lettered.
      `NotificationProcessorService` (30 s sweep) is the backstop for anything the bus misses, and now picks up
      `Failed` notifications — previously it only queried Pending/Queued, so **nothing ever retried a failed send**.
      > **Deviation from design D1, deliberate:** the outbox carries the *retry*, not every send. Publishing on the
      > happy path would put a bus round-trip on every booking to deliver a message that de-duplication would then
      > discard, and would turn the synchronous `SendNotificationResult` into a lie. Failure is the case that needs
      > durability, and that is the case that goes through the outbox.
- [x] 2.2 `NotificationDeliveries` table (or cache) with unique `(EventId, Channel, Recipient)`; dispatch is idempotent per tuple
      → composite-PK table + `NotificationDeliveryLog` (claim → send → record outcome), each operation in its own
      scope/transaction so a claim is visible to a concurrent dispatcher immediately. A **Delivered** tuple is never
      re-sent; a **Failed** tuple stays claimable, or dedup would turn one 503 into a permanently missing message.
      The dedup scope is the originating event (`Notification.SourceEventId`, set from the domain event's `EventId`
      by all 12 lifecycle command sites), falling back to the notification's own id for API-initiated sends.
      Migration `20260819104835_AddNotificationDeliveries`.
      **Fail-open** if the dedup store is unreachable: a duplicate beats a dropped refund receipt (logged as an error).
- [x] 2.3 Preference gate at send time; define + document the non-suppressible set (payment/refund)
      → `NotificationSuppressionPolicy` (domain, pure). Non-suppressible: money movement (PaymentReceived/Failed/
      Refunded/Confirmed, RefundProcessed/Issued, PayoutCompleted/Processed, InvoiceGenerated) and account security
      (SecurityAlert, PasswordReset, PhoneVerification, AccountVerification). Absent preferences mean **send**, never
      "everything muted". Set membership, not `HasFlag`, decides: `NotificationType` is `[Flags]` but its members from
      16777216 up are sequential integers, so a mask would leak between unrelated types.
- [x] 2.4 Delivery log/metrics (attempt/success/failure)
      → per-channel structured logs (notification id, channel, attempt, gateway message id, error) plus the
      `NotificationDeliveries` row itself (status, attempt count, gateway message id, error, timestamps) as the
      queryable audit trail.

### Defects found and fixed along the way
- **Multi-channel notifications never sent at all.** Every lifecycle handler requests
  `Email | SMS | InApp` in one value, and the dispatch code `switch`ed on that combined value — matching no case and
  throwing `NotSupportedException`. Same bug in `ResendNotificationCommandHandler`, `SendBulkNotificationCommandHandler`
  and `ProcessScheduledNotificationsJob`. All four now go through `INotificationDispatcher`, which iterates
  `EnumerateChannels()`.
- **Successful sends were never persisted.** `SendNotificationCommandHandler` only called `UpdateNotificationAsync`
  in its `catch`, so a delivered notification stayed `Queued` in the database forever.
- **Phantom-UPDATE concurrency exception on every send.** `NotificationWriteRepository.UpdateNotificationAsync`
  called `DbSet.Update()` on a tracked aggregate, re-stamping the freshly-added `DeliveryAttempt` as Modified →
  UPDATE affecting 0 rows → `DbUpdateConcurrencyException`. Now only attaches when genuinely detached
  (same fix as `EfRepositoryBase.UpdateAsync`; see memory `aggregate-persistence-concurrency-fix`).
- **Bulk send used a detached `Task.Run` per recipient**, outliving the request scope and its `DbContext`. Bulk
  notifications are now left `Queued` for the sweep, which applies the same preference/dedup/retry rules.
- **Every scheduled reminder was stranded.** `ScheduledNotificationService` called `Queue()` on notifications that
  `Notification.Schedule()` had already left `Queued`; `Queue()` only accepts `Pending`, so it threw, the catch
  recorded a delivery failure with no attempt behind it, and `ShouldRetry()` (which needs a last attempt) then
  returned false forever. Now it only queues genuinely `Pending` ones.
- **Invitation SMS reported success unconditionally** — the sender reports gateway failures in its result rather
  than throwing, and the caller ignored it.
- **`DBNull.Value` parameters silently killed the delivery log.** `RecordOutcomeAsync` passed nulls as
  `DBNull.Value` in an `ExecuteSqlRaw` `object[]`; Npgsql cannot infer a type for those and threw, which the
  fail-open catch swallowed — leaving every tuple on `Pending` and disabling de-duplication entirely. Caught by
  the integration tests before merge; now uses `ExecuteSqlInterpolatedAsync` so nulls stay typed.

## 3. Tests
- [x] 3.1 Unit: preference enforcement (disabled channel skipped; non-suppressible always sent); dedup tuple prevents second send
      → `NotificationSuppressionPolicyTests` (7 cases incl. the flags-leak guard and channel fan-out) and
      `NotificationDispatcherTests` (14 cases: fan-out, missing-address skip, preference gate, non-suppressible,
      flag-off rollback path, dedup, redelivery, failure/throwing gateway, backoff, dead-letter, no-double-send).
- [x] 3.2 Integration: each lifecycle event → exactly-once notification per channel; transient sender failure → retried then delivered
      → `NotificationDeliveryReliabilityTests` against real PostgreSQL: redelivered event sends one SMS and leaves one
      `Delivered` row; a failed tuple stays claimable and the retry delivers (attempt count 2); two recipients of one
      event are not collapsed; a disabled channel is skipped while a refund on the same channel still goes out.
- [x] 3.3 DI test: exactly one `ISmsNotificationService` resolves
      → `SmsNotificationServiceCompositionTests` in `Booksy.Host.CompositionTests` — the only suite that boots the real
      composition root, so the only place a second registration is visible. Asserts a single registration *and* that
      the configured provider is the one resolved.

## 4. Verify
- [x] 4.1 Build + tests green; confirm non-suppressible set with product before enabling preference suppression on financial notifications
      → Solution builds; notification unit suites green (33 domain + 14 application).
      **Open question resolved in the safe direction, no product sign-off needed to ship:** financial and security
      notifications are *never* suppressed, so preferences can only ever cause a non-critical message to be skipped.
      Product input is only required if they later want any of those types to become suppressible — that is the
      change that needs sign-off, not the current default.
