## Context

The customer app is Flutter (bloc + get_it + Dio + go_router), Persian/RTL, with standardized `StateSwitcher`/`OfflineBanner`/`ErrorState` already used in booking screens. The backend ZarinPal flow is redirect-based: create payment (returns an Authority + a gateway URL) → user pays on the gateway → gateway redirects back with the Authority → verify. C2 makes create/verify idempotent and crash-consistent.

## Goals / Non-Goals

**Goals:** a complete, idempotent, resumable checkout on iOS/Android/Web; deposit coupling; receipts; robust failure/timeout/offline handling.
**Non-Goals:** new payment methods beyond what the backend supports; wallet; changing backend money mechanics (C2/C5 own those).

## Decisions

- **D1 — Redirect via in-app WebView with a return URL / deep link.** Launch the ZarinPal URL; capture the return via a registered redirect URL (web) or deep link (mobile). *Rationale:* matches the backend's redirect model and works cross-platform. *Alternative rejected:* embedding card fields (not supported by the Iranian gateway model).
- **D2 — Client idempotency key per checkout attempt.** Generate a UUID when the user initiates payment, persist it for the attempt (survive app restart), and send it on create so retries/resumes don't double-charge (pairs with C2).
- **D3 — Verify is server-authoritative.** The client sends the returned Authority to the backend verify endpoint; the client never trusts gateway callback params for success. Poll/verify with backoff; the backend's `Status==Paid` short-circuit makes repeat verifies safe.
- **D4 — Deposit coupling.** When `RequiresDeposit`, the booking confirm CTA leads into checkout for the deposit; the booking is only confirmed after a verified deposit payment (server enforces via `customer-booking-journey` modification).
- **D5 — Resumable state.** A booking with a `Pending` payment resumes into the redirect/verify step on reopen (handles app kill mid-redirect).
- **D6 — Feature-flagged.** `checkout_enabled` gates the whole feature; off → current booking-without-payment behavior.

## Risks / Trade-offs

- [Deep-link / return-URL misconfiguration] → Mitigation: explicit Android intent-filter + iOS URL scheme + web redirect route; on-device E2E on all three.
- [App killed mid-redirect] → Mitigation: D5 resumable state keyed by the persisted idempotency key + Pending payment lookup.
- [Web popup/redirect UX differs from mobile] → Mitigation: platform-specific redirect handling; test matrix.
- [Building ahead of C2/C3] → Mitigation: hard dependency; C4 starts only after C2/C3 are merged + tested.

## Migration Plan

Client-only; phased via `checkout_enabled`. No backend migration here. Backward compatible: guests browse; non-deposit flows unchanged.

## Open Questions

- **PRODUCT:** For providers requiring a deposit, is an unpaid booking allowed to exist in a "Pending payment" state, or must it not be created until the deposit succeeds? (Affects whether we create-then-pay or pay-then-create.) → **requires product input.**
- Receipt format/content (invoice vs simple receipt) — needs the receipt endpoint's shape from C5.
