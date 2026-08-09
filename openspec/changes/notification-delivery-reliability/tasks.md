# Tasks: notification-delivery-reliability

> Independent of the other changes. Behind `Notifications:ReliableDispatch`. Preserve existing senders.

## 1. Consolidate interfaces
- [ ] 1.1 Pick the canonical `ISmsNotificationService`; delete the two duplicates; update DI registrations + all callers; build clean

## 1b. Domain reliability model — DONE + TESTED
- [x] Exponential backoff between attempts (`DeliveryAttempt`: 5·3^(n-1) → 5/15/45/135/405s) — verified.
- [x] Bounded retry budget (`Notification.MaxRetryAttempts=5`, `HasExhaustedRetries`, `ShouldRetry` backoff-gated).
- [x] **Dead-letter queue**: `NotificationStatus.DeadLettered` + `MarkAsDeadLettered` (terminal; only after exhaustion;
      never from Sent/Delivered/Read) + `PrepareForRetry` (Failed→Queued only when eligible). Raises `NotificationFailedEvent`.
- [x] At-most-once send guarantee (dedup): a Sent notification cannot be re-sent (state guard).
- [x] 10 unit tests (`NotificationReliabilityTests`): backoff schedule, no-double-send, fresh-failure-not-retryable,
      cannot-dead-letter-before-exhaustion, exhausted→dead-letter→terminal, cannot-dead-letter-delivered.
> **Remaining (larger refactor in the tangled 3-interface SMS subsystem — see memory `sms-sandbox-switch`):**
> CAP-outbox-driven dispatch (§2.1), the `NotificationDeliveries` cross-event dedup tuple (§2.2), send-time preference
> gate (§2.3), a background retry/dead-letter dispatcher, and `ISmsNotificationService` consolidation (§1.1). Tracked
> for the final audit as C7-partial.

## 2. Reliable dispatch
- [ ] 2.1 Route lifecycle notifications through CAP-consumed events with retry on transient failure
- [ ] 2.2 `NotificationDeliveries` table (or cache) with unique `(EventId, Channel, Recipient)`; dispatch is idempotent per tuple
- [ ] 2.3 Preference gate at send time; define + document the non-suppressible set (payment/refund)
- [ ] 2.4 Delivery log/metrics (attempt/success/failure)

## 3. Tests
- [ ] 3.1 Unit: preference enforcement (disabled channel skipped; non-suppressible always sent); dedup tuple prevents second send
- [ ] 3.2 Integration: each lifecycle event → exactly-once notification per channel; transient sender failure → retried then delivered
- [ ] 3.3 DI test: exactly one `ISmsNotificationService` resolves

## 4. Verify
- [ ] 4.1 Build + tests green; confirm non-suppressible set with product before enabling preference suppression on financial notifications
