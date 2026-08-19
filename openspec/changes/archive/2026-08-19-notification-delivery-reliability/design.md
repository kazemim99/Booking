## Context

Lifecycle events already publish via CAP (in-process outbox). Notification handlers call real SMS/push services directly. There are three `ISmsNotificationService` interfaces. `NotificationPreferences` exist (`NotificationPreferencesController`) but enforcement at send time is unverified.

## Goals / Non-Goals

**Goals:** at-least-once delivery with retry; exactly-once effect per event+channel (dedup); preference-gated sends; observability; one SMS interface.
**Non-Goals:** new channels; a notification-center redesign; template management overhaul.

## Decisions

- **D1 — Dispatch through the CAP outbox with retry.** Notification sends are triggered by CAP-consumed lifecycle events; CAP provides at-least-once + retry on handler failure. Transient sender errors surface as handler failures so CAP retries. *Rationale:* reuse the existing durable transport instead of a bespoke retry loop.
- **D2 — Dedup key per (event id, channel, recipient).** A `NotificationDeliveries` row (or cache key) with a unique `(EventId, Channel, Recipient)` makes redelivery/retry idempotent — the sender is invoked once per unique tuple. *Rationale:* at-least-once transport + dedup = effectively-once.
- **D3 — Preference gate at send time.** Before dispatching a channel, check the customer's `NotificationPreferences`; skip disabled channels (but never skip legally/operationally critical notifications if policy dictates — document which are non-suppressible).
- **D4 — Consolidate to one `ISmsNotificationService`.** Keep the canonical interface (Application layer), delete the duplicates, update DI registrations and callers. Mechanical; covered by build + tests.
- **D5 — Delivery log/metrics.** Record attempt/success/failure per notification for observability and support.

## Risks / Trade-offs

- [Interface consolidation touches many call sites] → Mitigation: compiler-driven refactor; integration tests for each lifecycle notification.
- [Preference gate suppresses a critical notification] → Mitigation: a documented non-suppressible set (e.g. payment/refund receipts).
- [Dedup store unavailable] → Mitigation: fail-open on send (prefer a duplicate over a miss) but log; or fail-closed for non-critical — decide per channel.

## Migration Plan

Additive `NotificationDeliveries` table; new pipeline behind `Notifications:ReliableDispatch`. Old senders retained one release. Rollback: flag off.

## Open Questions

- The non-suppressible notification set (which channels/events ignore preferences) may have a **product/legal** dimension → confirm before enabling preference suppression on financial notifications.
