# Proposal: notification-delivery-reliability

## Why

Real SMS/push senders exist (Kavenegar, Rahyab, Firebase), and lifecycle handlers are wired, but the audit could not verify **delivery guarantees**, and found structural debt:

- No evidence of **retry, de-duplication, or preference enforcement** on customer notifications (booking confirmed/cancelled/reminder/payment/refund) → silent misses or duplicates.
- **Three duplicate `ISmsNotificationService` interfaces** across layers (`Application/Services`, `Application/Services/Notifications`, `UserManagement`) → DI ambiguity and drift.

## What Changes

- Route customer lifecycle notifications through the **CAP outbox with retry + dedup keys**, so a transient sender failure is retried and an event never notifies twice.
- **Enforce `NotificationPreferences`** per channel before sending.
- **Consolidate** to a single `ISmsNotificationService`.
- Add delivery **logging/metrics** for observability.

## Capabilities

### New Capabilities
- `notification-delivery`: customer lifecycle notifications SHALL be delivered reliably — retried on transient failure, de-duplicated per event+channel, gated by the customer's channel preferences, and observable.

### Modified Capabilities
- (none.)

## Impact

- **Code**: notification dispatch via CAP outbox with retry/dedup; preference check; consolidate `ISmsNotificationService` (update DI + callers); delivery log/metrics.
- **DB**: optional `NotificationDeliveries` log with a dedup key.
- **API/Flutter**: none (preferences already exist).
- **Depends on**: none (independent; can run in parallel with others).
