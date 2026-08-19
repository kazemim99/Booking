# Tasks: checkout-release-gates

> Carried out of `customer-payment-experience` §5.2, §5.4 and §6 when that change was archived on
> 2026-08-19. None of these are code tasks; each needs something the build environment cannot produce.
> `CHECKOUT_ENABLED` stays OFF until §1–§3 are all closed.

## 0. Housekeeping (done)

- [x] 0.1 Removed the Vue `paymentService.getPaymentsByBooking()` dead call — it requested
      `GET /api/v1/payments/booking/{id}`, which does not exist on `PaymentsController` (verified against its
      route table: `{id}`, `{id}/capture`, `{id}/refund`, `customer/{customerId}`, `calculate-pricing`,
      `zarinpal/create`, `callback`, `zarinpal/verify`, `customer/history`, `provider/{id}/revenue`,
      `reconciliation`). It had no call sites, so this is a latent-trap removal, not a bug fix.

## 1. Configuration (blocking — needs credentials)

- [ ] 1.1 **ZarinPal sandbox MerchantId.** `Payment:ZarinPal:MerchantId` is the placeholder
      `"your-zarinpal-merchant-id"`. Supply via the `Payment__ZarinPal__MerchantId` environment variable or
      user-secrets. **Never commit it.**
- [ ] 1.2 **Publicly reachable HTTPS callback, at the correct path.** `Payment:ZarinPal:CallbackUrl` is
      `https://localhost:7002/api/v1/payments/zarinpal/callback` — unreachable by the gateway *and* pointing
      at a path that does not exist. The real route is `GET /api/v1/Payments/callback`. Override with
      `Payment__ZarinPal__CallbackUrl=https://<public-host>/api/v1/Payments/callback`.
- [ ] 1.3 **`Application:ClientUrl` pointed at the app under test.** Currently `https://booksy.com`, so a
      sandbox payment would redirect the tester into production. Override with `Application__ClientUrl`.
- [ ] 1.4 Confirm the settings that are already correct have not drifted: `Payment:ZarinPal:IsSandbox` true,
      `Payments:UseFakeZarinPal` absent (⇒ off, and fail-closed in Production), `Payments:AllowStubGateways`
      absent (⇒ false, so `PaymentGatewayFactory` refuses non-functional gateways).

## 2. T3 — real ZarinPal sandbox (blocking)

- [ ] 2.1 One deposit-required booking paid end to end against the sandbox: create → redirect → pay →
      gateway callback → server-side verification → booking `Confirmed`.
- [ ] 2.2 Assert the ledger recorded a **balanced** charge for it (`LedgerEntries` for the payment sum to
      zero; ProviderPayable credited gross) — the ledger, not the gateway, is the record of truth.
- [ ] 2.3 Repeat the callback (duplicate delivery) and confirm a single credit — idempotency holds against
      the real gateway, not just the fake one.
- [ ] 2.4 Cancel/NOK path: customer abandons at the gateway → booking stays unconfirmed, deposit unrecorded,
      and the failure is retryable.

## 3. T4 — on-device (blocking for mobile release only)

- [ ] 3.1 **Unblock the Android toolchain first.** `flutter build apk` cannot complete here: every path on
      `maven.google.com` and `dl.google.com/dl/android/maven2` returns 404 while Maven Central and the Gradle
      Plugin Portal return 200, because the machine is configured for Chinese mirrors
      (`FLUTTER_STORAGE_BASE_URL`, `PUB_HOSTED_URL`) and Gradle's `google()` is not mirrored. Needs a Google
      Maven mirror in `settings.gradle.kts`. This also gates the merged-manifest check behind C4 §4.1a, which
      was accepted on structural parse + regression test rather than a real manifest merge.
- [ ] 3.2 Android device/emulator: discover → book → pay → receipt; duplicate tap yields a single charge;
      failure is recoverable.
- [ ] 3.3 iOS: same journey. Requires an `ios/` directory (none exists today) and an Apple Team ID.

## 4. Release

- [ ] 4.1 Flip `CHECKOUT_ENABLED` on for a small percentage of customers (staged rollout / TestFlight +
      Play internal track), with a real-money smoke test on one provider first.
- [ ] 4.2 Watch the idempotency/duplicate-payment dashboards and the `Pending`-payment age alert through the
      first rollout window before widening.

## Deliberately NOT in scope

- **Deep links (App Links / Universal Links).** Settled in C4 §4.1b as a UX enhancement, not a
  financial-correctness requirement: the deposit is recorded and the booking confirmed by the *server-side*
  callback with no client involvement (proved by T2 steps 8–11, which run with no client at all), and the app
  resolves state by asking the server on foreground resume plus an explicit "check status" action. The
  deferral is test-pinned by `android_manifest_test.dart` (asserts no `BROWSABLE` category, exactly one
  `<intent-filter>`), so adding inbound routing is a deliberate revisit rather than drift.
- **In-app WebView.** Rejected: a banking UI without a visible address bar / TLS indicator, and unsupported
  on Flutter web.
