## Why

`customer-payment-experience` (C4) is **code-complete and green at every tier this repository can run**:
T1 (Domain 347/347, Application 96/96, Flutter 105/105, Vue payment-return 10/10, `flutter analyze` clean)
and T2 (`tests/e2e/deposit-checkout-flow.sh` — 30/30 assertions, twice consecutively, through the real HTTP
stack with a fake gateway only at the bank boundary). The whole server-side money chain — deposit gate →
gateway → callback → verification → confirmation → ledger — is proven.

What is left is **not engineering work**. It is four pieces of configuration nobody in this environment can
produce, and two verification tiers that depend on them:

- there is no ZarinPal sandbox MerchantId (the config holds the literal placeholder
  `"your-zarinpal-merchant-id"`),
- there is no publicly reachable HTTPS callback for the gateway to call back to,
- the configured callback path is wrong even for a reachable host,
- `Application:ClientUrl` still points at production,
- and no Android/iOS device or emulator exists here, with `flutter build apk` additionally blocked by Google
  Maven returning 404 for every artifact on this machine.

Keeping these inside C4 made a finished change read as 70% done and made the real blocker — *credentials and
infrastructure* — look like unwritten code. This change separates them so C4 can archive as the delivered
capability it is, and the remaining gates stay visible as what they are: a release checklist with an owner.

`CHECKOUT_ENABLED` stays **off** until every gate below is closed.

## What Changes

- Supply and verify the four configuration values (merchant id, public callback URL at the correct path,
  client URL, and confirmation that `Payment:ZarinPal:IsSandbox` is still true).
- Run **T3** — one real deposit through the ZarinPal sandbox, end to end, and confirm the ledger records a
  balanced charge and the booking reaches `Confirmed`.
- Run **T4** — the on-device journey on Android and iOS, once a device and an unblocked Android toolchain
  exist.
- Only then flip `CHECKOUT_ENABLED` on, behind a staged rollout.
- Housekeeping already done in this change: removed the Vue `paymentService.getPaymentsByBooking()` dead
  call, which requested `GET /api/v1/payments/booking/{id}` — a route that does not exist on
  `PaymentsController` and would have 404'd on first use.

## Capabilities

### Modified Capabilities

<!-- None. This change adds no behavior. `customer-checkout` (promoted by `customer-payment-experience`)
     already specifies what checkout does; this change only verifies it against a real gateway and real
     devices, and supplies deployment configuration. Nothing here changes a requirement. -->

## Impact

- **Config only** (never committed): `Payment__ZarinPal__MerchantId`, `Payment__ZarinPal__CallbackUrl`,
  `Application__ClientUrl`, and the `CHECKOUT_ENABLED` dart-define at build time.
- **Code**: one deletion in `booksy-frontend/src/core/api/services/payment.service.ts` (dead call). No
  backend or Flutter source changes are expected — if T3/T4 uncover defects, those become their own changes.
- **Depends on**: `customer-payment-experience` (archived 2026-08-19) for the implementation, and on
  `Payments:AllowStubGateways=false` remaining absent/false in production so a stub can never report a
  phantom-paid booking.
- **Blocked by (external)**: a ZarinPal sandbox merchant account; a public HTTPS tunnel or staging host; an
  Android emulator/device plus a Google Maven mirror in `settings.gradle.kts`; an `ios/` directory and Apple
  Team ID for the iOS leg.
