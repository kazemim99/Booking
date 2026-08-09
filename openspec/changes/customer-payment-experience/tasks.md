# Tasks: customer-payment-experience

> Flutter-heavy. HARD dependency on C2 (payment integrity) + C3 (slot integrity). Behind `CHECKOUT_ENABLED`.

## Legend — implemented ≠ verified

- `[x]` = implemented **and** covered by a green automated check (named alongside).
- `[ ]` = not done, **or** done but still awaiting a verification tier that has not actually run.

Verification tiers referenced below:

| Tier | What it covers | Status |
|------|----------------|--------|
| **T1** | Unit/widget/bloc suites + static analysis | ✅ PASS — Domain 347/347, Application 96/96, Flutter 105/105, Vue payment-return 10/10, `flutter analyze` 0 errors |
| **T2** | Deterministic API E2E through the real HTTP stack, fake gateway at the bank boundary only (`tests/e2e/deposit-checkout-flow.sh`) | ✅ PASS — 30/30 assertions, twice consecutively |
| **T3** | Real ZarinPal **sandbox** gateway | ⛔ BLOCKED — needs a real sandbox MerchantId + a publicly reachable HTTPS callback (neither available; see §6) |
| **T4** | On-device E2E (Android / iOS) | ⛔ BLOCKED — no emulator, no `ios/` directory in this environment |

## 0. Preconditions
- [x] 0.1 C2 + C3 done and green (C2 §2 atomic-idempotency is a tracked GO-blocker; C5 ledger complete).
- [x] 0.2 **RESOLVED (product):** **create-then-pay, Pending + resumable.** A deposit-required booking is created
      immediately holding the slot (leveraging C3), stays unconfirmed until the deposit is verified, resumes on
      reopen, and its hold is auto-released on abandonment/timeout. Confirmation requires a verified deposit.

## 1. API client + models
- [x] 1.1 Payment/verify/receipt endpoints in `api_constants.dart` + DTOs (manual JSON, per the codegen constraint).
      Deliberately **no** "payments by booking" constant — the backend exposes no such route; the booking's own
      `PaymentInfo` is authoritative. *(T1)*
- [x] 1.2 Checkout data source + repository (create payment, verify, read payment, booking snapshot),
      `Either<Failure, T>`. *(T1)*

## 2. Checkout bloc + resumability
- [x] 2.1 `CheckoutBloc`: initiate → external-browser redirect → return/verify → success/failure. Idempotency key is a
      v4 GUID persisted **before** the gateway call, so a crash mid-create is still resumable. *(T1)*
- [x] 2.2 Resume: reopening a booking with a pending attempt verifies it instead of starting a second charge;
      `DuplicateRequestFailure` resolves from the booking rather than re-creating. *(T1)*
- [x] 2.3 Bloc tests: success, duplicate-submit (one charge), failure+retry, resume, offline/unknown.
      *(T1 — `test/features/checkout/checkout_bloc_test.dart`)*

## 3. Screens
- [x] 3.1 Review/deposit screen (deposit math, currency, RTL, Persian strings from `AppStrings`). *(T1)*
- [x] 3.2 Redirect handler + return capture — **implemented as an external browser** (`url_launcher`,
      `LaunchMode.externalApplication`) rather than the in-app WebView this task originally assumed. Deliberate: the
      gateway is never framed by the app, and the return is resolved by asking the server, never by reading the URL.
      Return is captured by app-lifecycle `resumed` plus an explicit "check status" action. *(T1)*
- [x] 3.3 Success/receipt + failure/retry screens; all six states rendered
      (busy / ready / awaiting / paid / failed / unknown / nothing-due). *(T1)*
- [x] 3.4 Widget tests: each state, plus deposit math and the never-charge-twice guards.
      *(T1 — `test/features/checkout/checkout_page_test.dart`)*

## 4. Platform + coupling
- [x] 4.1a **Android outbound launch — FIXED.** The manifest declared no `<queries>` entry for `https` `VIEW`
      intents, so under `targetSdk=36` Android package-visibility filtering (API 30+) could stop `url_launcher` from
      resolving a browser — `launchPaymentUrl` returns false and checkout never reaches the gateway. Added one
      `<intent>` (`action VIEW` + `data scheme="https"`), +16/−0, purely additive. Guarded by
      `test/config/android_manifest_test.dart` (4 tests), which was **proved to fail without the change**.
      *Caveat:* verified by XML structural parse + regression test, **not** by an APK manifest merge — see §6.6.
- [ ] 4.1b **Deep links (App Links / Universal Links) — DEFERRED BY DECISION, not outstanding work.** Deep linking
      is a **UX enhancement, not a financial-correctness requirement**: the deposit is recorded and the booking
      confirmed by the *server-side* callback with no client involvement (proved by T2 steps 8–11, which run with no
      client at all), and the app resolves state by asking the server on foreground resume plus an explicit
      "check status" action. Approved architecture: `ZarinPal → HTTPS callback → server-side verification/settlement
      → Vue return page → app foreground resume/verify`. A custom-scheme bounce is likewise deferred.
      The deferral is now **pinned by tests**: `android_manifest_test.dart` asserts no `BROWSABLE` category and
      exactly one `<intent-filter>`, so adding inbound routing becomes a deliberate revisit rather than drift.
      Revisit prerequisites when it is taken up: an `ios/` directory, an Apple Team ID, a signing-cert SHA-256 for
      `assetlinks.json`, and a confirmed production domain — none of which exist today.
- [x] 4.2a **DONE (backend coupling).** Create-then-pay deposit gate: `Booking.RecordDepositPaid` (idempotent) +
      `Confirm()` gates on a paid deposit; `ConfirmBookingOnDepositVerifiedHandler` wires `PaymentVerifiedEvent` →
      record deposit → confirm (self-committing, idempotent). *(T1 `BookingDepositGateTests` + T2 steps 8–11)*
- [x] 4.2b Flutter: booking confirm → checkout when the booking requires a deposit, in `booking_flow_page`,
      gated by the feature flag. *(T1)*
- [x] 4.3 Feature flag `CHECKOUT_ENABLED` — `const`, **defaults false**, tree-shaken when off, and pinned off by
      `test/config/routes/app_router_test.dart:193`. Not a bypass: the server's deposit gate applies regardless.
      *(T1)*

## 5. Tests + verify
- [x] 5.1 Integration: a deposit-required booking is not confirmed until the deposit is verified — plus percentage
      and fixed-amount deposits, idempotent re-submit, repeated callback, gateway refusal, NOK/cancel, retry after a
      settled failure, and the no-deposit control. *(T2 — 30/30, `tests/e2e/deposit-checkout-flow.sh`)*
- [ ] 5.2 **E2E (device): OPEN.** discover → book → pay → receipt; duplicate tap → single charge; failure →
      recoverable; on iOS + Android. Blocked by T4 (no emulator, no `ios/`) and by the §4.1 `<queries>` finding.
      The **web** leg's return surface is covered by the Vue payment-return specs (10/10); the Flutter-in-Chrome UI
      leg is **deferred by decision**, not passed — T2 covers the server/payment chain and T1 covers the checkout UI.
- [x] 5.3 `flutter analyze` clean (0 errors) and `flutter test` green (105/105). *(T1)*
- [ ] 5.4 **T3 — real ZarinPal sandbox: OPEN.** See §6. Not attempted; no credentials fabricated.

## 6. Release gates still requiring external input

These are **not** code tasks. Each needs something this environment cannot produce.

- [ ] 6.1 **ZarinPal sandbox MerchantId.** `Payment:ZarinPal:MerchantId` is the placeholder
      `"your-zarinpal-merchant-id"`. Supply via `Payment__ZarinPal__MerchantId` (env) or user-secrets — never commit.
- [ ] 6.2 **Publicly reachable HTTPS callback.** `Payment:ZarinPal:CallbackUrl` is
      `https://localhost:7002/api/v1/payments/zarinpal/callback`, which is unreachable by the gateway **and points at
      a path that does not exist**. The real route is `GET /api/v1/Payments/callback`
      (`PaymentsController`, `[Route("api/v{version:apiVersion}/[controller]")]` + `[HttpGet("callback")]`).
      Override with `Payment__ZarinPal__CallbackUrl=https://<public-tunnel>/api/v1/Payments/callback`.
- [ ] 6.3 **`Application:ClientUrl` pointed at the Vue app under test.** Currently `https://booksy.com`, so a sandbox
      payment would redirect the tester to production. Override with `Application__ClientUrl`.
- [x] 6.4 **Deep-link decision — SETTLED: ship without.** See §4.1b. Deferred deliberately and test-pinned.
- [ ] 6.5 **`CHECKOUT_ENABLED` stays OFF** until 6.1–6.3 are settled and T3 has actually run.
- [ ] 6.6 **Android APK build is blocked in this environment — infrastructure, not code.** `flutter build apk`
      cannot complete here, so the merged-manifest check behind §4.1a could not run. Two distinct causes were found,
      in order:
      1. *(resolved)* `~/.gradle/caches/8.14` was ~99.8% corrupt — 1,800 of 1,803 transform directories had their
         `metadata.bin` stripped, plus the same damage in `kotlin-dsl`. Cleared wholesale (176.83 MB, 63 files);
         sibling caches incl. `modules-2` preserved. Zero `metadata.bin` errors afterwards.
      2. *(open)* **Google Maven is unreachable from this machine.** Every path on `maven.google.com` and
         `dl.google.com/dl/android/maven2` returns HTTP 404 — including the group index — while Maven Central and
         the Gradle Plugin Portal return 200 and DNS resolves to genuine Google IPs. The machine is configured for
         Chinese mirrors (`FLUTTER_STORAGE_BASE_URL=storage.flutter-io.cn`, `PUB_HOSTED_URL=pub.flutter-io.cn`) but
         Gradle's `google()` repository is **not** mirrored, so AGP `8.11.1` (`settings.gradle.kts:20`) cannot
         resolve. `com.android.tools.build:gradle:8.11.1` *is* cached in `modules-2`; only the ~1 KB plugin-marker
         POM is missing.
      **Decision: §4.1a is accepted as verified-by-test-and-structural-parse, not by merge.** Residual risk is low
      (additive `<queries>` entry, no merge-conflict surface). Resolving this needs a Google Maven mirror in
      `settings.gradle.kts` — infrastructure work, out of C4 scope, and it blocks T4 regardless of hardware.
      *Note:* Flutter's migrator re-adds `android.builtInKotlin=false` / `android.newDsl=false` to
      `gradle.properties` on every Android build; these were reverted each time and are not part of this change.

> Already correct, no action: `Payments:UseFakeZarinPal` is absent (⇒ off, and fail-closed in Production);
> `Payments:AllowStubGateways` is absent (⇒ false); `Payment:ZarinPal:IsSandbox` is already `true`.

> **C4 status.** Backend + client are implemented and green at T1, and the whole server-side money chain is green at
> T2. What remains is **verification against a real gateway (T3) and a real device (T4)** — both externally blocked —
> plus the §4.1 platform decision. Nothing below T2 is claimed as verified.
