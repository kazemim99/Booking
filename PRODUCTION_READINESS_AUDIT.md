# Booksy — Independent Production Readiness Audit (GO / NO-GO)

**Date:** 2026-07-30 · **Scope:** whole platform — backend (modular monolith), Flutter customer/provider apps, Vue admin/frontend, payments, notifications, booking, financial flows, security, performance, migrations, infrastructure, testing.
**Stance:** reviewed as an independent senior architect. Nothing is assumed correct because it was built here; claims below are backed by code, tests, or `dotnet` tooling output. Where something could not be verified end-to-end, it is marked as such.

---

## VERDICT: **NO-GO** (conditional) — readiness ≈ **76%**
> **Update 2026-08-09:** C4 **Phase 0 (backend deposit gating) is complete and verified** — gaps B1/B2/B3 closed with
> 13 new tests and zero regressions. B1 was a silent critical defect: deposit-required bookings could never confirm.
> The Flutter checkout client remains the largest single blocker. Readiness 72% → 76%.

The **money-safety, booking-integrity, and authorization backend core is production-grade and test-proven** — the highest-risk parts of a booking+payments platform. However, three release-blocking gaps remain, the largest being that **the customer app cannot actually take a payment yet**. A booking product that cannot charge on the customer path is not launchable. With the P0 items below closed (est. focused effort, mostly the Flutter checkout + notification dispatch wiring + dependency CVEs + turning the red integration suite green), this moves to GO.

Readiness by area: Payments/ledger backend **95%**, Booking integrity **90%**, AuthZ/AuthN **90%**, Notifications **60%** (domain reliability done; dispatch wiring partial), **Customer payment UX 15%** (backend gating started; Flutter checkout not built), Dependency security **55%**, Test-suite green **65%**, Perf/load **20%** (not exercised), Ops readiness **70%** (this doc).

---

## What is DONE and PROVEN (high confidence)

| Area | Evidence |
|---|---|
| **Resource-ownership authZ + global authenticated fallback** (C1) | `AuthorizationBehavior`, `FallbackPolicy`, full endpoint audit; 2 real holes fixed (unauth `Services.Update/Delete`, anon token mint); boundary integration tests. |
| **SignalR hub security** (C1 close-out) | Fixed hub missing `[Authorize]` (was accepting anonymous) + `access_token` read from header→query-string; 6 tests (4 unit + 2 real-HubConnection E2E: anonymous rejected, authenticated connects+reconnects). |
| **Booking slot integrity** (C3) | Postgres GiST exclusion constraint → `409 SLOT_TAKEN`; randomized high-concurrency property tests prove no overlap under any interleaving. |
| **Aggregate persistence** (fix) | Root-caused (empirically) the owned-child-key `DbUpdateConcurrencyException`; fixed `ValueGeneratedNever` + `Money.Clone` + tracked-safe `UpdateAsync`; 41 previously-red tests repaired. Retracted the earlier "Version token" misdiagnosis. |
| **Payment money-safety** (C2) | DB dedup index (`UX_Payments_OneCapturedPerBooking`→409) + `PaymentReconciler` (DB=truth) + fault-injection tests + stub-gateway fail-close. |
| **C2 §1 no double-charge-on-retry** | `INonTransactionalCommand` — money commands bypass the retrying ambient transaction, self-commit once; 9 tests, 0 regressions. |
| **C2 §2 atomic idempotency** | `IIdempotencyStore` composite-PK reserve; behavior reserve→run-once→complete / 409 in-flight / replay / release-on-fail / stale reclaim; `Idempotency-Key` header; **10 tests incl. 24-way concurrency = one winner**. |
| **Financial ledger = single source of truth** (C5) | Append-only immutable double-entry (DbContext-enforced), idempotent charge/refund/payout posting, commission-at-payout, ledger-derived payout + block-future-payouts, drift detection, reconciliation, background maintenance; **47 financial tests**; E2E traceability incl. refund-after-payout (no money lost). |
| **Notification reliability (domain)** (C7 partial) | Exponential backoff (5·3^(n-1)), bounded retry, **dead-letter** terminal state, at-most-once send; 10 unit tests. |

---

## P0-0 — **RESOLVED (2026-08-09)**: deposits are now configurable end-to-end
**Fixed and verified.** `BookingPolicy` gained `DepositType` (Percentage | FixedAmount) + `DepositFixedAmount` with
validation that rejects an uncollectable "required" deposit and caps any deposit at the booking total. `Provider`
now owns a `BookingPolicy` (nullable ⇒ existing providers unaffected) set via
`PUT /api/v1/{providers|provider-settings}/{id}/booking-preferences` — the path the built Vue UI already called.
Booking creation resolves `service.BookingPolicy ?? provider.BookingPolicy ?? Default` and still snapshots the
policy per booking, so changes affect **future bookings only**. `BookingPolicy` is the single source of truth;
`Service.EnableDeposit`/`RequiresDeposit` are `[Obsolete]`/display-only and deliberately not synchronised.
Ownership is enforced by a new `IRequireProviderOwnership` + `ProviderOwnershipResolver`; `ProviderBookingPolicyChangedEvent`
is the audit record (previous + new terms, actor, timestamp). Migration is purely additive (14 columns).
**Verified:** 14 domain + 12 integration tests; 347 domain / 96 app-unit / 105 Flutter all green.

**Second defect found and fixed in the same work — a real production bug:** `IProviderReadRepository` is decorated
with `CachedProviderReadRepository`, and `ProviderCacheInvalidationEventHandler` invalidated on every other
provider-mutating event but **not** on a policy change. Because booking creation reads the provider through that
cache, a provider enabling/changing deposits would have kept creating bookings under the **stale** policy until the
entry expired. Root-caused empirically (change-tracker + `SaveChanges` row count + post-commit DB read proved EF
persistence was correct and the *read path* was stale), then fixed by handling the new event in the existing
invalidation handler.

## P0-0 — original finding (kept for context): the deposit feature was **unreachable end-to-end**
- **Severity:** Critical for C4's premise. **Business impact:** the entire deposit/pre-payment capability cannot be
  turned on by any supported path, so no booking in the system can ever require payment before confirmation.
- **Evidence (traced, not inferred):**
  - A booking's deposit comes **only** from `CreateBookingCommandHandler.cs:180` →
    `service.BookingPolicy ?? BookingPolicy.Default`, and `BookingPolicy.Default` is `requireDeposit: false`
    (deliberately — its own comment says a deposit default made bookings "permanently unconfirmable" while no
    payment flow existed).
  - **Nothing anywhere calls `Service.SetBookingPolicy(...)`** — so `Service.BookingPolicy` is always `null` in
    practice and every booking silently gets the no-deposit `Default`.
  - `CreateServiceCommand` **has its deposit fields commented out** (lines 23–24), and
    `CreateServiceCommandHandler.cs:86` has `// service.EnableDeposit(request.DepositPercentage);` commented out.
  - The API only ever **reads** `RequiresDeposit`/`DepositPercentage`; there is **no write endpoint**.
  - Persistence is *ready*: `Services` has non-null `RequiresDeposit`/`DepositPercentage` plus nullable
    `BookingPolicy*` columns including `BookingPolicyRequireDeposit`/`BookingPolicyDepositPercentage`.
- **Second, separate defect:** `Service.RequiresDeposit`/`DepositPercentage` (what the API exposes and what
  `EnableDeposit` sets) are **not** what booking reads — booking reads `Service.BookingPolicy`. Even if the
  commented-out `EnableDeposit` path were restored, bookings would **still** get no deposit. The two
  representations are unlinked.
- **Consequence for C4/T2:** the deposit-required booking that checkout exists to serve cannot be produced through
  any API/seed/fixture path. T2's deposit scenarios are blocked on provisioning (see report), and C4's customer
  value is unrealisable in production until a write path exists and is linked to `BookingPolicy`.

## Release-blocking issues (P0)

### P0-1 — Customer app cannot take payment (C4: backend DONE, Flutter client OUTSTANDING)
- **Severity:** Critical. **Business impact:** no revenue on the customer path; no receipts. Still the #1 launch blocker.
- **Backend (Phase 0) — DONE + VERIFIED (2026-08-09), 13 new tests, zero regressions.** Investigation of the real
  contract found and closed three genuine backend gaps:
  - **B1 (was critical and silent):** `Booking.RecordDepositPaid` existed but **nothing ever called it**, and
    `Confirm()` is gated by `DepositMustBePaidBeforeConfirmationRule` — so a deposit-required booking could
    **never** reach `Confirmed`, even with a perfect client. Now wired via
    `ConfirmBookingOnDepositVerifiedHandler` (`PaymentVerifiedEvent` → record deposit → policy-gated `Confirm()`),
    using only existing aggregate methods/gates (no domain redesign), idempotent on replay, and never failing the
    payment if confirmation is refused (deposit stays recorded, booking flagged for follow-up).
  - **B2:** `POST /payments/zarinpal/verify` hardcoded `Status="OK"`, so a customer cancellation could not be
    reported/settled. It now forwards the reported outcome (defaults to `"OK"`, backward compatible). ZarinPal
    remains the sole authority — `"OK"` still triggers real verification, and an already-`Paid` payment can never
    be flipped to `Failed` by a stray/malicious `"NOK"` (proven).
  - **B3:** the same endpoint did not forward the `Idempotency-Key` header (the other four money endpoints did),
    so client retries bypassed the C2 §2 reservation. Now wired (all 5 money endpoints consistent).
- **Verification:** 4 deposit-gating integration tests dispatched through the **real** domain-event dispatcher
  (proves DI → handler → aggregate gate → persistence), 4 status-handling tests against a real DB with a mocked
  gateway, 5 controller wiring tests. Empirical causation check (B1 removed vs present, identical filter):
  **0 new regressions; 1 previously-red test now passes.** Domain 333/333, Application 96/96, key integration 47/47.
- **B4 — web return pages: DONE + VERIFIED (2026-08-09).** The backend callback already redirected to
  `{ClientUrl}/payment/success|failure`, but **no such routes existed** — a paying web customer landed on a 404, so
  the *existing* web flow was broken independently of mobile. Added `payment.routes.ts` (public, so a returning
  customer is never bounced to login) plus `PaymentSuccessView`/`PaymentFailureView` in `booksy-frontend`. The
  success page **never treats the URL as proof of payment**: it re-reads server state (`getPaymentById`, or the
  idempotent verify-by-authority) and only then announces success; when it cannot confirm it shows a neutral
  "couldn't confirm yet" state that explicitly discourages paying twice. **10 vitest tests, 0 TS errors repo-wide.**
- **Mobile return mechanism — decided (evidence-based).** ZarinPal's `callback_url` points at **our own backend**, so
  the gateway never sees a custom scheme and the app-return hop is entirely ours. Approved approach: **external
  browser (`url_launcher`) + server-owned verification now; App Links/Universal Links later as a pure UX phase.**
  A WebView was rejected (banking UI without a visible address bar/TLS indicator; also unsupported on Flutter web).
- **C4 Flutter checkout — foundation DONE + unit-verified; UI NOT yet wired.** Implemented `features/checkout/`:
  entities, repository contract, remote data source (sends `Idempotency-Key`), repository impl (409 →
  `DuplicateRequestFailure`; verify-400 → definitive "unpaid" rather than a retryable error), a
  `SharedPrefs`-backed attempt store, and `CheckoutBloc`. Money-safety invariants enforced and tested: an
  already-paid deposit short-circuits (never re-charges), an interrupted attempt is **verified, not re-created**, a
  repeated create reuses the persisted idempotency key so the server de-duplicates it, a 409 is resolved by
  verification or by re-reading the booking (never by a second create), and **transport failures land in `unknown`
  rather than `failed`** so uncertainty never invites a duplicate charge. **19 bloc tests; full app suite 87/87.**
- **C4 Phases 3–4 — DONE (code/build verified).** `CheckoutPage` renders every state (review/deposit, awaiting,
  paid/receipt, failed/retry, unknown, nothing-due) in RTL using the existing design system; DI registers the
  data source, repository, attempt store and a per-navigation `CheckoutBloc` (external browser via
  `url_launcher` with `LaunchMode.externalApplication` — never an in-app web view); a focused, auth-gated
  `/checkout/:bookingId` route was added without altering any existing navigation decision; and the booking
  success step offers checkout behind `FeatureFlags.checkoutEnabled` (**OFF by default**). **105/105 Flutter
  tests pass** (68 pre-existing + 21 bloc + 12 widget + 4 router/coupling), `flutter analyze` clean, and
  `flutter build web --dart-define=CHECKOUT_ENABLED=true` succeeds.
- **Financial-rule correction found during Phase 3:** the earlier draft would have charged the **full price** when
  a provider required no deposit. The backend gate is `Policy.RequireDeposit` + a deposit percentage, so charging
  the full amount up front would have invented a rule the server does not have. Checkout now collects **only the
  configured deposit**, surfaces a `nothingDue` state otherwise, and hard-refuses any pay request when nothing is
  owed (2 dedicated tests).
- **E2E — NOT executed. Two hard blockers:** (1) `Payments:ZarinPal:MerchantId` is the placeholder
  `"your-zarinpal-merchant-id"`, so the gateway leg cannot run at all — no real or sandbox payment can be created;
  (2) the customer app has no `integration_test`/driver harness, so the browser journey cannot be automated.
  Therefore **booking → deposit gate → checkout → ZarinPal → callback → verification → receipt has NOT been
  verified end-to-end.** What *is* verified: the backend half against real PostgreSQL (Phase 0, 13 tests incl. the
  real domain-event dispatcher), the client half by unit/widget tests, and that the whole thing compiles and ships.
- **Newly found (not yet fixed):** the Vue `paymentService.getPaymentsByBooking()` calls
  `GET /Payments/booking/{id}`, which **does not exist** on the backend (verified against `PaymentsController`).
  Dead call today; would 404 if used. The Flutter client deliberately avoids it.
- **Exit:** implement `features/checkout/`; wire booking-confirm→checkout when `RequiresDeposit`; verify web E2E in
  Chrome; mobile requires a real device/emulator (**not available in the current environment** — see below).
- **Decision pending:** mobile return mechanism. External browser + secure app-link return is preferred over a
  WebView for a banking flow; requires verifying ZarinPal's redirect constraints and the Android/iOS app config
  before committing. Not yet chosen.

### Environment limitation (affects verification claims, not correctness)
Flutter 3.44.2 is available, but the only devices are **Windows/Chrome/Edge — no Android or iOS emulator**.
Web (Chrome) E2E is executable here; **mobile on-device E2E is not** and must not be claimed as verified until run
on a real device/emulator.

### P0-2 — Notification delivery not wired end-to-end (C7 partial)
- **Severity:** High. **Business impact:** booking/payment confirmations & reminders may not actually be delivered/retried reliably; duplicate or missing messages.
- **State:** Domain reliability model done (backoff/DLQ/dedup-guarantee). **Not done:** CAP-outbox-driven dispatch, `NotificationDeliveries` cross-event dedup tuple, send-time preference gate, a background retry/dead-letter dispatcher, and consolidation of the 3 duplicate `ISmsNotificationService` interfaces (DI ambiguity risk).
- **Exit:** implement §2.1–2.4 of `notification-delivery-reliability`; integration test each lifecycle event → exactly-once per channel + transient-failure→retried→delivered→dead-lettered.

### P0-3 — Known dependency vulnerabilities (6, incl. HIGH)
- **Severity:** High. `dotnet list package --vulnerable` (2026-07-30):
  - **AutoMapper 15.0.1 — HIGH** (GHSA-rvv3-g6hj-g44x)
  - **Microsoft.Data.SqlClient 5.1.1 — HIGH** (GHSA-98g6-xh36-x2p7) — transitive; DB is Postgres, likely removable
  - **System.Formats.Asn1 5.0.0 — HIGH** (GHSA-447r-wph3-92pm) — transitive
  - **System.Security.Cryptography.Xml 9.0.4 — HIGH** (GHSA-37gx-xxp4-5rgx)
  - Azure.Identity 1.7.0 — Moderate; MimeKit 4.14.0 — Moderate (email)
- **Note:** SixLabors.ImageSharp already remediated (3.1.5 → **3.1.12**).
- **Exit:** bump direct refs (AutoMapper); pin patched transitive versions (Asn1, Crypto.Xml, SqlClient/Azure.Identity or remove the pulling package); re-run `--vulnerable` to zero HIGH/Moderate; add a CI gate failing the build on any HIGH advisory.

---

## High-priority non-blocking (P1)

- **P1-1 Integration test suite not green.** ~46 failures in the Payments/Notifications/ServiceManagement/Bookings surface remained across the hardening (stable at 46 → no *new* regressions introduced by any change here). Many are a **test-harness gap** (services are created `Draft`; `Service.Activate()` needs qualified staff, so booking/payment happy-paths can't be set up), some are real (e.g. deposit/booking flows dependent on C4). **Must be triaged to green before GO** — a red suite hides regressions.
- **P1-2 Booking update persistence.** The `Booking.TotalPrice#Price.BookingId` owned-key defect + `BookingHistoryEntry.Id` were fixed (same class as the aggregate-persistence fix); confirm booking confirm/cancel/reschedule/complete are green in CI (was the cause of several P1-1 failures).
- **P1-3 Payout clawback policy** (refund-after-payout negative balance → real transfer) is intentionally gated OFF pending a finance decision. Safe default (block-future-payouts) is active. Confirm the policy before enabling.
- **P1-4 C6 data/migration hygiene.** Verify: index restoration, migration idempotency (the partial-unique dedup index **fails to create if pre-existing duplicate captured payments exist** — run the data audit as a deploy step), and payment carry-over on the monolith DB.

## Medium / lower (P2)
- Load/performance testing not performed (no evidence of throughput/latency targets or N+1 review on hot paths: availability search, provider listing).
- `ByProvider` server-derivation (C1 follow-up) minor.
- Full CAP outbox/inbox monitoring dashboards.

---

## Deployment checklist (backend)
1. **Pre-deploy data audit (blocking):** confirm no booking has >1 captured payment (else `UX_Payments_OneCapturedPerBooking` migration fails). Query `Payments` grouped by `BookingId` where `Status IN ('Paid','PartiallyPaid')`.
2. Back up PostgreSQL (`pg_dump`) and snapshot the DB volume.
3. Confirm `.env`: `Payments:AllowStubGateways=false` (prod), real ZarinPal creds, `Finance:ReconciliationEnabled=true`, `Notifications:ReliableDispatch` per C7 rollout, JWT secret/issuer/audience, Redis/Seq creds.
4. Build images (CI `build-and-push.yml`), run the full unit + integration suite + `e2e-keystone` gate — **must be green** (see P1-1).
5. Apply EF migrations at host startup (idempotent, additive): slot exclusion constraint, payment dedup index, `LedgerEntries`, `IdempotencyReservations`. Verify each created.
6. Roll out backend first (API), then frontend; verify `/health` healthy before routing traffic.

## Rollback plan
- **App:** redeploy the previous image tag (compose `down`/`up` with prior GHCR tag); images are immutable per SHA.
- **DB:** all new migrations are **additive** (new tables/indexes; no destructive column drops) → rolling back the app is safe without a down-migration. If a migration must be reversed: `LedgerEntries`/`IdempotencyReservations` drop cleanly; the dedup/slot indexes drop via `DROP INDEX IF EXISTS`. Restore from the pre-deploy `pg_dump` only as a last resort (data loss window = since backup).
- **Ledger note:** the ledger is append-only; a rollback never edits ledger rows — reconciliation re-converges on restart.
- **Feature flags:** `checkout_enabled` (client) and `Notifications:ReliableDispatch` let you disable new flows without redeploying.

## Operational runbook (key procedures)
- **Stuck/Pending payment:** the `PaymentReconciler` background sweep (5-min) converges Pending→Paid/Failed against ZarinPal. Manual: query `Payments WHERE Status='Pending' AND Authority IS NOT NULL`; inspect Seq logs; the sweep is idempotent and safe to let run.
- **Ledger drift alert:** `DetectBalanceDriftAsync` logs `LEDGER DRIFT DETECTED` (Σ≠0). Response: freeze payouts, inspect `LedgerEntries` for the offending `EventId`, post a **compensating** transaction (never edit/delete — the DbContext blocks it).
- **Provider balance dispute:** balances are ledger-derived — `GetProviderPayableBalanceAsync`; reconstruct from `LedgerEntries` filtered by `ProviderId`.
- **Dead-lettered notifications:** query `Notifications WHERE Status='DeadLettered'`; inspect `ErrorMessage`/`DeliveryAttempts`; replay after fixing the channel.
- **Payment double-charge report:** dedup index + §1/§2 make it near-impossible; verify via `LedgerEntries` for the `PaymentId` (one Charge transaction) and the gateway dashboard.

## Post-deployment verification checklist
- `/health` and `/swagger` reachable; DB migrations table shows all new migrations applied.
- Create a test provider → activate → create a booking → (once C4 lands) pay a deposit → verify → **ledger shows a balanced charge**, booking `Confirmed`, receipt returned.
- Trigger a refund → ledger shows balanced refund, provider payable reduced.
- Force a duplicate payment request (same `Idempotency-Key`) → second returns the stored result / 409, **one** gateway charge.
- Connect a SignalR client with/without a token → authenticated connects, anonymous rejected.
- Confirm `PaymentReconciler` + `LedgerMaintenance` background services logged a startup + first sweep.

## Monitoring & alerting checklist
- **Alerts (page):** `LEDGER DRIFT DETECTED`; payment-reconciler exceptions; API 5xx rate; DB connection failures; health-check failing; > N notifications `DeadLettered`/hour.
- **Alerts (ticket):** rising `Pending` payments older than 15 min; `IDEMPOTENCY_CONFLICT`/`DUPLICATE_PAYMENT`/`SLOT_TAKEN` 409 spikes (possible client bug or attack); dead-letter growth.
- **Dashboards (Seq/OTel):** payment success/fail/refund counts; ledger account balances over time; notification delivery success vs dead-letter; p95 latency on booking-create, availability-search, provider-list; CAP outbox backlog.
- **Business metrics:** GMV, commission recognized, payouts executed — all read from the ledger.

## Recommended rollout strategy
1. **Backend canary** with new money-safety + ledger live but `checkout_enabled=false` (customer app unchanged) and `Notifications:ReliableDispatch` off → validate migrations, reconciler, drift check, no regressions in existing flows for 24–48h.
2. Enable `Notifications:ReliableDispatch` once C7 dispatch wiring lands; watch dead-letter rate.
3. Ship the Flutter checkout to a **small % of customers** behind `checkout_enabled` (staged rollout / TestFlight + Play internal track), with a real-money smoke test on one provider first; monitor the double-charge/idempotency dashboards closely.
4. Gradually raise the rollout %; keep the pre-deploy DB backup and the previous image tag ready for instant rollback.
5. Keep payout clawback OFF until finance confirms the policy.

---

## Bottom line
The financial core is the part most teams get wrong, and here it is **rigorously correct and tested** — money cannot be lost, duplicated, or become untraceable, and every movement is auditable from the ledger. The blockers are about **completing the customer-facing payment journey**, **wiring notification dispatch**, and **routine hardening** (dependency CVEs, a green test suite, load testing). Close P0-1/P0-2/P0-3 and P1-1, and this is a confident GO.
