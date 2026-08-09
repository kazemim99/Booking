# Architectural Decisions

A running log of significant architectural decisions for the Booksy backend, focused on the production-readiness
hardening of the Customer Booking + financial domains. Each entry records the decision, why, and — where it
**replaced an earlier design** — the rationale for the change. Newest first.

Format: ADR-NNN — Title (status) — date.

---

## ADR-007 — Ledger is the single source of truth for all financial calculation (accepted) — 2026-07-29

**Decision.** Every money movement (charge, refund, payout, commission) is recorded in an **append-only,
immutable, double-entry ledger** (`LedgerEntry` / `LedgerTransaction`, OpenSpec `financial-ledger-and-settlement`).
All financial figures — provider balances, settlements, commissions, refunds, payouts, reporting — are **derived
from the ledger**, never re-summed from `Payments`/`Bookings`.

- **Immutable/append-only:** `LedgerEntry` is never updated or deleted; the `ServiceCatalogDbContext` hard-enforces
  this (`EnforceLedgerAppendOnly` throws on any Modified/Deleted ledger entry). Corrections are **compensating
  transactions**, not edits.
- **Balanced double-entry:** each money event posts a `LedgerTransaction` whose signed amounts sum to exactly zero
  (property-tested). Accounts: GatewayClearing, ProviderPayable, PlatformRevenue, Customer.
- **Idempotent posting:** handlers post keyed by a **stable** `LedgerEventKeys` id → unique `(EventId, Account)`
  index, so replays/duplicate deliveries are no-ops. The ledger post is **decoupled** (its own scope/commit) from
  the command transaction (the domain-event dispatcher runs handlers in a separate scope), with a reconciler
  closing any partial-failure gap.
- **Commission is recognized at payout**, not at charge — charge credits the provider the full gross; payout
  clears the payable, credits PlatformRevenue the commission, and pays the net.
- **Payout is ledger-derived:** `CreatePayoutCommandHandler` reads `GetProviderPayableBalanceAsync` (was: summed
  `Payments` in a period). Refunds debit ProviderPayable, so they automatically reduce payouts; a provider whose
  refunds exceed unpaid charges (balance ≤ 0) is **blocked from further payouts** (safe default; real clawback is
  a gated finance decision).
- **Drift detection:** `DetectBalanceDriftAsync` verifies Σ(signed) = 0 across the ledger and alerts on drift.

**Why replace payment-summing with the ledger?** Auditability and correctness: balances reconstructable and
tamper-evident; refunds/commissions consistently reflected; no divergence between "what we paid out" and "what we
recorded."

**Deviation from the change's wording:** the design said "Hangfire" for background jobs. The repo has no Hangfire;
`LedgerMaintenanceBackgroundService` uses a `BackgroundService` (consistent with the C2 `PaymentReconciler`),
gated by `Finance:ReconciliationEnabled`. Adopting Hangfire solely for this was unjustified.

---

## ADR-006 — Payment money-safety: DB-enforced dedup + reconciliation + non-transactional money commands (accepted) — 2026-07-28

**Decision (OpenSpec `payment-consistency-and-idempotency`).** Correctness under failure is the primary goal —
money must never be lost, duplicated, or untraceable.

- **Never duplicated (record):** a partial unique index `UX_Payments_OneCapturedPerBooking` guarantees at most one
  captured payment per booking at the database (not just in app code) → `409 DUPLICATE_PAYMENT`.
- **Never lost/untraceable:** the DB (not the gateway) is the record of truth. `PaymentReconciler` treats the
  gateway as a read-only oracle and converges stale `Pending` payments to Paid/Failed, idempotently, on a
  background sweep. Fault-injection tested (gateway timeout → stays recoverable-Pending; duplicate → idempotent).
- **Never duplicated (gateway charge) — §1, done:** `EnableRetryOnFailure` + `TransactionBehavior` wrapped every
  command in a retried transaction, so a transient DB fault re-ran the handler **including its gateway call** →
  double-charge/refund. Money-moving commands now implement `INonTransactionalCommand`: they bypass the retried
  ambient transaction and self-commit a single retry-safe unit. The gateway call happens exactly once.
- **Fail-closed gateways:** `PaymentGatewayFactory` refuses non-functional stub gateways (Behpardakht mock,
  Parsian/Saman) unless `Payments:AllowStubGateways=true` — a stub that "succeeds" without charging would record
  phantom-paid bookings.
- **§2 atomic idempotency reservation — DONE (GO-blocker cleared).** Replaced the non-atomic distributed-cache
  check-then-act with an atomic DB reservation: `IIdempotencyStore` inserts against a composite PK
  `(RequestType, Key)` (the serialization point), so exactly one concurrent identical request runs the handler;
  the rest get 409 (in-flight) or the stored result (completed). Failures release the reservation; a crashed
  in-flight reservation is reclaimed after a DB-clock staleness window. `PaymentsController` honours the
  client's `Idempotency-Key` header. Proven by 10 tests incl. 24-way concurrency (one winner) and stale reclaim.

---

## ADR-005 — Aggregate persistence: owned child-collection keys must be `ValueGeneratedNever`; the "Version defect" was a misdiagnosis (accepted) — 2026-07-28

**Decision (OpenSpec `fix-aggregate-persistence-concurrency`).** Updating a materialized aggregate that appends an
owned child entity (`Payment.Transactions`, `Notification.DeliveryAttempts`, `Service.PriceTiers`, and the new
`LedgerEntries`) threw `DbUpdateConcurrencyException` ("0 rows affected") in production.

- **Root cause (empirically proven, not inspected):** the owned child's client-generated GUID key was **not**
  mapped `ValueGeneratedNever()`, so EF's graph-attach heuristic classified a newly-appended child as `Modified`
  → phantom UPDATE of a non-existent row. Compounded by owned-`Money` instance aliasing and an `UpdateAsync` that
  re-stamped tracked graphs.
- **Fix:** `ValueGeneratedNever()` on the child keys (the codebase already used this for `ServiceOption`/
  `GalleryImage`), `Money.Clone()` de-aliasing, and a tracked-safe `UpdateAsync` (no-op when already tracked).
- **Retraction:** the earlier hypothesis that the app-managed `int Version` optimistic-concurrency token was the
  cause is **withdrawn** — reproduction proved the `Version` WHERE clause matched; the failure was the phantom
  child UPDATE. No concurrency-model change was made.
- **Impact:** repaired 41 previously-red integration tests, 0 regressions. No DB migration (model-side only).
- **Further instances found in C6 (same class, same fix):** `Booking.TotalPrice` owned `Price` + `Booking.Id`
  (owned-key "cannot be modified" on every booking update), `BookingHistoryEntry.Id` (client GUID that was
  `ValueGeneratedOnAdd` → phantom UPDATE when a history entry is appended on any state transition), and
  `PaymentInfo` reusing owned `Money` instances across its replaced/added owned VO (nulled columns on save →
  fixed with defensive `Money.Clone()` in the ctor). Net effect: booking confirm/cancel/reschedule/complete now
  persist. Rule to carry forward: **any owned entity/collection with a domain-generated key ⇒ `ValueGeneratedNever`;
  any owned value object that flows into more than one slot ⇒ `Clone()`.**

---

## ADR-004 — Booking slot integrity via a Postgres GiST exclusion constraint (accepted) — 2026-07-28

**Decision (OpenSpec `booking-slot-integrity`).** Prevent overlapping active bookings for the same staff member at
the database with an `EXCLUDE USING gist ("StaffId" WITH =, tstzrange("StartTime","EndTime") WITH &&)` constraint
(over Requested/Confirmed), mapped to `409 SLOT_TAKEN`. Chosen over app-level checks because only a DB constraint
is race-proof; validated with randomized high-concurrency property tests (no overlap under any interleaving).

---

## ADR-003 — Resource-ownership authorization + global authenticated fallback (accepted) — 2026-07-28

**Decision (OpenSpec `harden-resource-authorization`).** A MediatR `AuthorizationBehavior` enforces resource
ownership for money/booking mutations (only the owning customer/provider or an admin), closing IDOR gaps. A global
`FallbackPolicy = RequireAuthenticatedUser` denies-by-default; every endpoint was audited and classified, with
explicit `[AllowAnonymous]` for the genuinely public surface. Two real holes fixed (unauthenticated
`Services.Update/Delete`; anonymous token minting).

**SignalR NotificationHub (C1 close-out, done).** Two more real defects fixed + validated: (1) the hub had **no
`[Authorize]`** — it accepted anonymous connections (`OnConnectedAsync` even logged and proceeded); added
`[Authorize]`. (2) JWT-bearer `OnMessageReceived` read `access_token` from a **header** — but browsers can't set
headers on a WebSocket handshake, so the standard SignalR `?access_token=` query-string negotiation never
authenticated a real client; extracted to `SignalRAccessTokenExtractor` reading the **query string** (header
fallback), hub paths only. Validated by 6 tests: 4 unit (extractor) + 2 real-HubConnection E2E over the test
server (anonymous rejected; authenticated connects + reconnects). Real-JWT expiry already returns a
`Token-Expired` response header via `OnAuthenticationFailed`.

---

## ADR-002 — Ledger posting is decoupled from the command transaction (accepted) — 2026-07-29

**Decision.** The domain-event dispatcher (`SimpleDomainEventDispatcher`) runs handlers in a **separate DI scope**
(their own `DbContext`) and before the outer save. Ledger-posting handlers therefore **commit their own** write
(they cannot enlist in the command's transaction). This is acceptable and intended: the ledger is decoupled,
posting is idempotent by event id, and a reconciler + drift check close any gap. Attempting atomic same-context
posting would require reworking the dispatcher (dispatch-after-save + shared context) — deferred as out of scope.

---

## ADR-001 — Spec-driven hardening via OpenSpec changes (accepted) — 2026-07-28

**Decision.** The production-readiness remediation is organized as discrete OpenSpec changes (proposal / design /
specs / tasks) under `openspec/changes/`, implemented in dependency order with comprehensive tests before moving
on. Demonstrably-better production-grade designs replace the original proposal where justified, with the rationale
recorded here and in the change's design doc.
