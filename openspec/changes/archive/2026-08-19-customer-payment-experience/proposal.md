# Proposal: customer-payment-experience

## Why

The audit's top blocker: the Flutter customer app **cannot take payment at all**. `api_constants.dart` exposes only booking CRUD (no payment/deposit/refund/receipt endpoints), and the booking confirm step fires `BookingSubmitted` only (`booking_flow_page.dart:335`). Deposits are modeled server-side (`BookingConfiguration.cs:130`, `Policy.RequireDeposit`) but never collected on the customer path; there are no receipts/invoices. A booking product that cannot charge cannot launch.

## What Changes

- Add a **customer checkout journey** to `booksy-customer-app`: choose payment method → deposit-or-full → gateway redirect (ZarinPal) → return/verify → receipt.
- Couple booking confirmation to payment where the provider's policy requires a deposit.
- Consume the **hardened** backend from C2 (intent + idempotency) and rely on C3 for slot safety; send a stable idempotency key per checkout attempt.
- Add deep-link / return-URL handling for the gateway redirect, plus loading/failure/timeout/offline states.

## Capabilities

### New Capabilities
- `customer-checkout`: the customer app SHALL support paying for (or depositing on) a booking via the gateway redirect flow, with idempotent submission, verified return, receipts, and full loading/error/failure/offline states.

### Modified Capabilities
- `customer-booking-journey`: booking confirmation SHALL require a paid deposit when the provider policy requires one.

## Impact

- **Code (Flutter)**: new `features/checkout/` (data source, repository, bloc, screens: method → review/deposit → redirect handler → success/receipt → failure/retry); `api_constants` payment endpoints; DI registration; deep-link config (Android intent-filter / iOS URL scheme); booking-flow coupling.
- **API**: consume existing hardened payment/ZarinPal endpoints; add customer `GET /payments/{id}/receipt` if not present (additive).
- **DB**: none beyond C2/C5.
- **Security**: send idempotency keys; the server (not the client) validates the gateway return signature/authority.
- **Rollout**: behind a client feature flag `checkout_enabled`; guests still browse; non-deposit providers keep the current flow.
- **Depends on**: **C2 and C3 (hard)** — do not build on the unsafe backend; C5 for receipts/ledger correctness.
