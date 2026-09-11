# Tasks: checkout-release-gates

Status: STOPPED(blocked)
Verify: FULL

> Carried out of `customer-payment-experience` §5.2, §5.4 and §6 when that change was archived on
> 2026-08-19. None of these are code tasks; each needs something the build environment cannot produce.
> `CHECKOUT_ENABLED` stays OFF until §1–§3 are all closed.

## Acceptance scenarios

- Given a deposit-required booking and a real ZarinPal sandbox MerchantId, when the customer completes
  payment at the gateway, then the server-side callback verifies it, the booking reaches `Confirmed`, and
  `LedgerEntries` for the payment sum to zero with ProviderPayable credited gross (T3).
- Given the gateway delivers the callback twice (duplicate delivery), when both are processed, then only one
  credit is recorded — idempotency holds against the real gateway (T3).
- Given the customer abandons payment at the gateway (NOK/cancel), when the flow returns, then the booking
  stays unconfirmed, no deposit is recorded, and retry remains possible (T3).
- Given an Android or iOS device/emulator, when a customer discovers → books → pays → receives a receipt,
  then a duplicate tap yields a single charge and a failed payment is recoverable (T4).

## Tasks

### 0. Housekeeping (done)

- [x] 0.1 Removed the Vue `paymentService.getPaymentsByBooking()` dead call — it requested
      `GET /api/v1/payments/booking/{id}`, which does not exist on `PaymentsController` (verified against its
      route table: `{id}`, `{id}/capture`, `{id}/refund`, `customer/{customerId}`, `calculate-pricing`,
      `zarinpal/create`, `callback`, `zarinpal/verify`, `customer/history`, `provider/{id}/revenue`,
      `reconciliation`). It had no call sites, so this is a latent-trap removal, not a bug fix.

### 1. Configuration (blocking — needs credentials)

- [-] 1.1 **ZarinPal sandbox MerchantId.** BLOCKED: no credential available in this environment. Re-verified
      2026-09-11 — `Payment:ZarinPal:MerchantId` in `appsettings.json` is still the literal placeholder
      `"your-zarinpal-merchant-id"`, and no `Payment__ZarinPal__MerchantId` environment override is set.
      Supply via env var or user-secrets when a sandbox account exists. **Never commit it.**
- [-] 1.2 **Publicly reachable HTTPS callback, at the correct path.** BLOCKED: no public tunnel/host in this
      environment. Re-verified 2026-09-11 — `Payment:ZarinPal:CallbackUrl` is still
      `https://localhost:7002/api/v1/payments/zarinpal/callback`: unreachable by the gateway *and* pointing at
      a path that does not exist. The real route is `GET /api/v1/Payments/callback`. Override with
      `Payment__ZarinPal__CallbackUrl=https://<public-host>/api/v1/Payments/callback`.
- [-] 1.3 **`Application:ClientUrl` pointed at the app under test.** BLOCKED: no test/staging host assigned
      yet. Re-verified 2026-09-11 — still `https://booksy.com`, so a sandbox payment would redirect the tester
      into production. Override with `Application__ClientUrl`.
- [x] 1.4 Confirmed the settings that must not drift, 2026-09-11: `Payment:ZarinPal:IsSandbox` = `true`;
      `Payments:UseFakeZarinPal` absent from both `appsettings.json` and `appsettings.Development.json` (⇒
      off, fail-closed in Production); `Payments:AllowStubGateways` absent (⇒ false, so
      `PaymentGatewayFactory` refuses non-functional gateways). No drift found.

### 2. T3 — real ZarinPal sandbox (blocking)

- [-] 2.1 BLOCKED: depends on 1.1–1.3 (no live MerchantId or reachable callback to pay against). One
      deposit-required booking paid end to end against the sandbox: create → redirect → pay → gateway
      callback → server-side verification → booking `Confirmed`.
- [-] 2.2 BLOCKED: depends on 2.1. Assert the ledger recorded a **balanced** charge for it (`LedgerEntries`
      for the payment sum to zero; ProviderPayable credited gross) — the ledger, not the gateway, is the
      record of truth.
- [-] 2.3 BLOCKED: depends on 2.1. Repeat the callback (duplicate delivery) and confirm a single credit —
      idempotency holds against the real gateway, not just the fake one.
- [-] 2.4 BLOCKED: depends on 1.1–1.3. Cancel/NOK path: customer abandons at the gateway → booking stays
      unconfirmed, deposit unrecorded, and the failure is retryable.

### 3. T4 — on-device (blocking for mobile release only)

- [-] 3.1 **Unblock the Android toolchain first.** BLOCKED: re-verified 2026-09-11 —
      `booksy-customer-app/android/settings.gradle.kts` and `booksy-provider-app/android/settings.gradle.kts`
      still declare only `google()` with no mirror, and `dl.google.com/dl/android/maven2` still returns 404
      for real artifact paths while Maven Central/Gradle Plugin Portal resolve, matching the prior diagnosis
      (machine mirrored to `flutter-io.cn` for Flutter/pub, but Gradle's `google()` is not mirrored — see
      memory `android-build-blocked-google-maven`). Needs a Google Maven mirror added to
      `settings.gradle.kts`. This also gates the merged-manifest check behind C4 §4.1a, which was accepted on
      structural parse + regression test rather than a real manifest merge.
- [-] 3.2 BLOCKED: depends on 3.1, and no device/emulator is attached — `flutter doctor` lists only
      `windows`, `chrome`, `edge` as connected devices (re-verified 2026-09-11). Android device/emulator:
      discover → book → pay → receipt; duplicate tap yields a single charge; failure is recoverable.
- [-] 3.3 BLOCKED: `booksy-customer-app` has no `ios/` directory (re-verified 2026-09-11: absent, while
      `booksy-provider-app/ios/` does exist — the customer checkout app still needs one generated) and no
      Apple Team ID is available in this environment. iOS: same journey as 3.2.

### 4. Release

- [-] 4.1 BLOCKED: depends on §1–§3 all closing. Flip `CHECKOUT_ENABLED` on for a small percentage of
      customers (staged rollout / TestFlight + Play internal track), with a real-money smoke test on one
      provider first.
- [-] 4.2 BLOCKED: depends on 4.1. Watch the idempotency/duplicate-payment dashboards and the
      `Pending`-payment age alert through the first rollout window before widening.

## Deliberately NOT in scope

- **Deep links (App Links / Universal Links).** Settled in C4 §4.1b as a UX enhancement, not a
  financial-correctness requirement: the deposit is recorded and the booking confirmed by the *server-side*
  callback with no client involvement (proved by T2 steps 8–11, which run with no client at all), and the app
  resolves state by asking the server on foreground resume plus an explicit "check status" action. The
  deferral is test-pinned by `android_manifest_test.dart` (asserts no `BROWSABLE` category, exactly one
  `<intent-filter>`), so adding inbound routing is a deliberate revisit rather than drift.
- **In-app WebView.** Rejected: a banking UI without a visible address bar / TLS indicator, and unsupported
  on Flutter web.

## Decisions

- Tier 1: Treated 1.4 as independently verifiable and completable now (it is a read-only config check, not a
  credential), rather than lumping it in with the genuinely blocked 1.1–1.3. Verified against the committed
  `appsettings.json`/`appsettings.Development.json`.

## Log

- 2026-09-11: Re-verified all four external blockers from the proposal are still true (placeholder
  MerchantId, unreachable/wrong-path CallbackUrl, production ClientUrl, no Google Maven mirror, no
  Android device/emulator attached, no `ios/` directory in `booksy-customer-app`). Closed 1.4 (config
  drift check — no drift found). No unblocked task remains: `Status: STOPPED(blocked)`. Nothing in this
  change required or received a code change beyond the already-done 0.1.
