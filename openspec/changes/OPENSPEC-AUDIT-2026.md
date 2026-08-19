# OpenSpec Change Audit — 2026-08-19

> **Scope.** Every change directory under `openspec/changes/` (excluding `archive/`), reviewed against the
> current working tree, `openspec/specs/`, `openspec/project.md` (verified 2026-08-16 @ `a8966c3`),
> `ARCHITECTURAL_DECISIONS.md` (ADR-001…007), `PRODUCTION_READINESS_AUDIT.md` (2026-07-30, updated 2026-08-09),
> `COMPLETION_ROADMAP.md`, and git history.
>
> **Method.** Decisions are grounded in what the code actually does today, not in what a change's tasks.md
> claims. Where a task list and the repository disagree, the repository wins and the discrepancy is called out.
> Every implementation claim below was checked against a file, migration, test, or `openspec` CLI output.
>
> **Nothing was modified, archived, deleted, or implemented.** This is audit + recommendation only.

---

## 0. State of the tree at audit time

| Fact | Value |
|---|---|
| Branch | `feat/provider-auth-flutter` (main branch: `master`) |
| Uncommitted paths | **310** (≈89 backend `src/` files, plus Flutter/Vue/admin work) |
| Active changes | **15** (was 16 — see below) |
| Main specs | 29 capabilities |
| `openspec validate --all --strict` | **44 passed / 0 failed** — every change and spec is structurally valid |

**Two archive operations are already staged but uncommitted:**

- `notification-delivery-reliability` → `archive/2026-08-19-notification-delivery-reliability` (14/14 tasks, complete).
  This closes audit blocker **P0-2** (notification dispatch). New spec `openspec/specs/notification-delivery/` is untracked.
- `refactor-provider-category-model` → `archive/2026-08-16-refactor-provider-category-model` (modified
  `provider-management`, `provider-registration`, `service-management` main specs).

**A concurrent session is active.** `add-playwright-e2e/tasks.md` and `fix-reschedule-membership-staff/tasks.md`
were both written within ~3 minutes of this audit starting, closing out their last verification tasks.
The verdicts below reflect the post-edit state (`openspec list` re-read after the edits).

---

## 1. Executive decision table

`openspec list` progress shown for reference; the **Decision** column is this audit's judgement, which in
several cases differs from the checkbox count because the task list is stale.

| # | Change | Progress | Decision | One-line reason |
|---|---|---|---|---|
| 1 | `add-playwright-e2e` | ✓ Complete | **Already implemented → archive** | Harness, keystone spec, CI job all shipped; suite verified 7 pass / 1 skip / 0 fail |
| 2 | `harden-e2e-test-coverage` | 17/19 | **Already implemented → archive** (after #1) | Preflight + globalSetup + reschedule spec all present; last gap satisfied by #3's real run |
| 3 | `fix-reschedule-membership-staff` | 27/29 | **Already implemented → archive** | Both defects fixed; 4 resource kinds green; keystone 16/16; residual isn't this change's |
| 4 | `fix-aggregate-persistence-concurrency` | 14/15 | **Already implemented → archive** | ADR-005 accepted; 41 red tests repaired; sole open item explicitly deferred to #10 |
| 5 | `booking-slot-integrity` | 11/12 | **Already implemented → archive** | ADR-004 accepted; GiST constraint migration in tree; property-tested under concurrency |
| 6 | `harden-resource-authorization` | 15/22 | **Already implemented → fix stale tasks.md, then archive** | §6 "mandatory acceptance" is done (ADR-003 + 2 test files) but unchecked |
| 7 | `payment-consistency-and-idempotency` | 7/17 | **Already implemented → fix stale tasks.md, then archive** | §2 GO-blocker is closed (ADR-006, migration + store + 10 tests); checkboxes never updated |
| 8 | `financial-ledger-and-settlement` | 13/19 | **Already implemented → archive** | ADR-007; change's own summary says "C5 is COMPLETE (22 tests)"; residuals are decisions, not code |
| 9 | `customer-payment-experience` | 18/26 | **Revise — split** | Code done + T1/T2 green; every open item is an external gate (no merchant ID, no emulator), not work |
| 10 | `booking-data-and-migration-hygiene` | 2/12 | **Revise, then implement — highest-value remaining backend work** | Still genuinely undone and still true; but 3 of its tasks need restating |
| 11 | `refactor-identity-and-membership` | 68/94 | **Implement as-is — prune stale duplicates first** | The one strategically live change; ~26 real tasks remain, several already superseded internally |
| 12 | `add-provider-hierarchy` | 132/190 | **Superseded → archive `--skip-specs`** | Its identity/parent mechanism is retired by #11; its spec deltas would corrupt main specs |
| 13 | `implement-customer-mobile-app` | 8/105 | **Obsolete → archive `--skip-specs`** | Superseded twice over; its premises now contradict the shipped app |
| 14 | `customer-app-ux-redesign` | 53/54 | **Already implemented → resolve capability naming, then archive** | Fully delivered; 5 new capability names collide with 5 existing ones |
| 15 | `design-system-convergence` | 11/34 | **Revise, then defer** | §3/§4 were silently delivered by later work; only adoption sweep + polish remain |

**Summary:** 9 archive-ready · 2 revise-then-implement · 1 revise-then-defer · 1 revise-and-split · 1 continue · 2 obsolete/superseded.

---

## 2. Cross-cutting findings

These matter more than any individual verdict. They are why the change directory feels stale.

### F1 — Spec-sync debt: the entire backend core is missing from `openspec/specs/`

Nine substantively-complete changes were never archived, so their delta specs were never promoted. As a result
`openspec/specs/` contains **no requirements at all** for:

`financial-ledger` · `payment-reconciliation` · `payment-idempotency` · `payment-processing-integrity` ·
`resource-authorization` · `booking-slot-integrity` · `aggregate-persistence-integrity` · `booking-reschedule` ·
`customer-checkout` · `e2e-testing`

Meanwhile the 29 capabilities that *are* in `specs/` are dominated by 2025-era Figma/UI work
(`provider-dashboard`, `provider-registration`, `customer-app-chrome-styling`, …). The money-safety,
authorization, and booking-integrity core — the parts the production audit rates 90–95% and calls
"rigorously correct" — is invisible to anyone reading the specs.

**This is the single highest-leverage cleanup in this audit.** `docs/KNOWLEDGE.md` names
`openspec/specs/<capability>/spec.md` as the authority for "what does it do"; today it cannot answer that
question for anything financial.

### F2 — Main specs actively describe retired behavior

- `specs/provider-staff-management/spec.md` still specifies **`POST /Providers/{id}/staff`** creating a team
  member from a first name. That endpoint is the legacy synthetic-`UserId` path that
  `refactor-identity-and-membership` §8.3 is retiring, and §11.1 already re-backed its *read* side with the
  membership roster.
- `specs/staff-management/spec.md` (Vue-side) describes the pre-membership staff directory.
- The word "membership" appears in **zero** main specs, despite `OrganizationMembership`,
  `MembershipAuditEntry`, `staff_profiles`, and three migrations being live in the codebase.

A developer implementing from specs today would build the wrong staff model.

### F3 — Capability-name collisions must be resolved before archiving

| Overlapping set | Owner A | Owner B |
|---|---|---|
| Customer-app visual/design tokens | `customer-app-visual-tokens`, `customer-app-component-styling` *(main, from archived unify change)* | `mobile-design-system` *(pending in #14)* |
| Customer-app shell/chrome | `customer-app-chrome-styling` *(main)* | `mobile-app-shell-ux` *(pending in #14)* |
| Customer booking journey | `customer-booking-journey` *(main)* | `mobile-booking-ux` *(pending in #14)* |
| Customer discovery | `customer-discovery-journey` *(main)* | `mobile-discovery-ux` *(pending in #14)* |
| Staff / team model | `staff-management`, `provider-staff-management` *(main)* | `organization-membership` *(pending in #11)* |
| Provider-app design system | *(none in main)* | `design-system-foundations`, `shared-ui-components`, `feedback-states`, `overlays-and-navigation` *(pending in #15)* |
| Customer-mobile duplicates | all of the above | `customer-mobile-{home,search,booking,bookings-management,profile}` *(pending in #13 — obsolete)* |

Archiving #14 and #13 as-authored would leave the customer app described by **three parallel capability
families**. Pick one convention — I recommend an app prefix (`customer-app-*`, `provider-app-*`) — and
rename deltas before promotion.

### F4 — Spec hygiene: **26 of 29** main specs still carry a placeholder purpose

`## Purpose` reads `TBD - created by archiving change <name>. Update Purpose after archive.` in 26 of the 29
capabilities — everything except `notification-delivery`, `project-documentation`, and
`provider-registration`. The archiver's TODO has never once been done. Every capability promoted in Step 1
below will add another unless the habit changes; consider making "write the Purpose" part of the archive
routine rather than a later sweep.

### F5 — Real, verified defects with **no owning change**

Each is referenced by two or more changes as "out of scope, tracked elsewhere" — and "elsewhere" doesn't exist.

| Defect | Evidence | Referenced by |
|---|---|---|
| **`RefreshProviderToken` HTTP self-calls a retired service.** `ProvidersController.cs:620` reads `Services:UserManagement:BaseUrl` (default `http://localhost:5001`) and posts to `/api/v1/auth/generate-token` — a standalone host that no longer exists post-monolith. A freshly-registered provider's dashboard/gallery therefore renders empty. **Still present today.** | `ProvidersController.cs:620–633`; the one remaining `test.skip` in `provider-registration-realistic.spec.ts:69` | ROADMAP Epic 1.1 · #2 §4.2 · #11 §8.8 · #3 §4.3 |
| **`FindOverlappingSlotsAsync` never filters `ProviderAvailability.StaffId`** — an organization's overlapping slots are returned and marked regardless of which member owns them; per-member slot narrowing is not expressible. | #3 §2.5 (found while extracting the resolver) | #3 hands it to #5, which is archive-ready |
| **Duplicate `ApiResponseMiddleware`** (`Core.Domain/Infrastructure/Middleware` **and** `Infrastructure.API/Middleware`) emitting a generic `"Request completed successfully"`, breaking two integration assertions. | both files line ~136/168 | #3 §4.4b ("left for whoever owns that envelope change") |
| **Booking `DateTime` round-trips through a +3:30 Tehran conversion** between `TimeSlot.Create` and materialization (10:00 reads back as 13:30). | #3 §4 finding | #3 (recorded, not chased) |
| **Dependency CVEs** — AutoMapper 15.0.1 **HIGH**, System.Security.Cryptography.Xml **HIGH**, System.Formats.Asn1 **HIGH**, Microsoft.Data.SqlClient **HIGH** (transitive, Postgres-only ⇒ likely removable), Azure.Identity + MimeKit moderate. ImageSharp already remediated 3.1.5→3.1.12. | `PRODUCTION_READINESS_AUDIT.md` P0-3; #10 §0.3 | only partially in #10 |
| **~46 red ServiceCatalog integration tests** + per-service integration projects never retargeted to `Booksy.Host`; `dotnet.yml` runs **no tests at all**. | audit P1-1; `project.md` §Testing; ROADMAP 3.1 | no change owns it |

### F6 — Substantial uncommitted work outside any change

The customer app has a **new map-discovery feature** in the tree with no OpenSpec change:
`map_discovery_page.dart`, `map_discovery_cubit.dart`, `map_clustering.dart`, `map_pin.dart`,
`map_provider_card.dart`, `category_filter_row.dart`, plus deletions of `area_page.dart`,
`nearby_page.dart`, `area_search_cubit.dart` and 8 new test files. Also uncommitted: `booksy-admin`
test infrastructure (`vitest.config.ts`, 4 new `__tests__` dirs, `utils/date.ts`, locale store rework).

The archived `unify-customer-app-with-provider-design` change named discovery/map/area search as *the gap* —
so this is very likely continued work on an already-archived change. Either way, ~89 backend files and a
whole feature are being built without a live spec.

### F7 — Delta-shape defects that will damage main specs on archive

- **#12 `add-provider-hierarchy`** carries `## MODIFIED Requirements` **and `## REMOVED Requirements`** for
  `provider-management`, `provider-registration`, and `staff-management`. Per `openspec/AGENTS.md`, the
  archiver *replaces the entire requirement* with the delta's text. Archiving normally would overwrite three
  main specs with the **retired sub-provider staff model** and delete existing requirements. **Must use
  `--skip-specs`.**
- **#9 `customer-payment-experience`** has a `## MODIFIED Requirements` section (`Deposit-required booking
  confirmation`) inside `customer-checkout` — a capability that **does not exist in main specs**. There is
  nothing to modify. Convert it to `ADDED` before archiving, or point it at the capability that actually
  owns booking confirmation.

---

## 3. Per-change verdicts

### 1. `add-playwright-e2e` — ALREADY IMPLEMENTED → archive

**Created** 2026-06-19 · `openspec list`: **✓ Complete**

**Verified in tree:** `booksy-frontend/e2e/{fixtures,pages,specs,utils}`, `global-setup.ts`,
`playwright.config.ts`, 4 spec files, `.github/workflows/frontend-e2e.yml`,
`src/core/router/__tests__/routes.spec.ts`.

**Why archive.** Its last two open tasks were closed minutes before this audit: §6.1 now reports
**7 passed / 1 skipped / 0 failed** against a running stack, and §6.4's backend blocker was cleared by #3.
Along the way it found and fixed **two real production bugs** worth preserving in the spec record:
the registration wizard was permanently invalid (E.164 phone prefilled into a `^09\d{9}$`-validated disabled
field), and `/customer/my-bookings` 404'd because two routes shared the name `MyBookings` and vue-router's
`addRoute()` evicts on duplicate names.

**Archive first** — it creates the `e2e-testing` base capability that #2 extends.

**Residual (not blocking):** the single `test.skip` (gallery main-image) is blocked on **F5's
refresh-token defect**, not on anything in this change.

---

### 2. `harden-e2e-test-coverage` — ALREADY IMPLEMENTED → archive (immediately after #1)

**Progress** 17/19 · both open tasks are verification-only.

**Verified in tree:** `PREFLIGHT_ONLY=1` branch at `tests/e2e/keystone-booking-flow.sh:63`,
`globalSetup: './e2e/global-setup.ts'` at `playwright.config.ts:22`, `e2e/utils/seed-fixture.ts`,
`e2e/pages/reschedule.page.ts`, `e2e/specs/booking-reschedule.spec.ts`.

**§7.1 is effectively satisfied.** It asked for a green local `npm run e2e:pw` including the un-skipped
My Bookings assertions and the new reschedule spec. #3 §4.3 reports exactly that run: **7 passed / 1 skipped
/ 0 failed**, reschedule included. Only §7.2 (the CI workflow's first run with `globalSetup`) is genuinely
untested — and `frontend-e2e.yml` is advisory, not a deploy gate.

**Backend fix delivered here is real and shipped:** `BookingsController.GetMyBookings` now binds
`from`/`to` as `DateTimeOffset?` (was silently reinterpreting UTC as server-local) and binds
`pageNumber`/`pageSize` — the keys the frontend actually sends, versus the `page`/`size` the
`PaginationRequest` complex binder expected. That was a real customer-facing empty-list bug.

**Delta shape is safe:** ADDED-only, so archive order relative to #1 cannot lose content. Still prefer
#1 → #2 so the capability reads coherently.

---

### 3. `fix-reschedule-membership-staff` — ALREADY IMPLEMENTED → archive

**Untracked directory, created ~2026-08-19** · 27/29 · the newest change in the tree.

This one is the reason several older changes can now close. It found that reschedule was broken by **two**
independent defects, the second hidden behind the first:

1. Reschedule resolved staff as an individual sub-provider (`GetByIdAsync(ProviderId.From(booking.StaffId))`),
   404ing **every** membership booking — because `CreateBookingCommandHandler` stores
   `StaffId = MembershipId` for member-staff since #11 §10.5.
2. `Booking.Reschedule` handed the successor booking the original's **owned** value objects
   (`TotalPrice`, `PaymentInfo`, `Policy`), which EF rejects as re-parenting. That broke **every** resource
   kind. Fixed with typed `Clone()` on `Price`, `PaymentInfo` (deep — four nested `Money`) and `BookingPolicy`.

It also found a **third instance of the same defect family**: `Booking.CreateBookingRequest` stored the
caller's `totalPrice`/`policy` instances directly, so two bookings for one service shared one owned
`Price`/`BookingPolicy`. This is the fourth documented occurrence of ADR-005's rule and argues for making
that rule enforceable (see §5, P6).

**Delivered:** `BookableResource` + `IBookableResourceResolver` (membership | organization-direct | legacy
sub-provider), consumed by both create and reschedule. 4 resource-kind Reqnroll scenarios green,
`keystone-booking-flow.sh` **16/16**, `booking-reschedule.spec.ts` green in-browser.

**Why archive despite 27/29.** §2.4 (behavior-neutral regression check) is covered by the 16/16 keystone run
plus the green browser suite. §4.4b is explicitly **not this change's work** — two tests now fail only on
response *wording* from the duplicate `ApiResponseMiddleware` envelope (F5). Move that to the new
`fix-api-response-envelope` change rather than holding this open.

**Also worth promoting into a spec:** ADR-005's rule already prevented this class once. Its recurrence
in `Booking.Reschedule` *and* `CreateBookingRequest` means `aggregate-persistence-integrity` (from #4)
should be promoted **before** anyone touches owned entities again.

---

### 4. `fix-aggregate-persistence-concurrency` — ALREADY IMPLEMENTED → archive

**Progress** 14/15 · **ADR-005 accepted 2026-07-28**

Root-caused empirically (not by inspection) with EF SQL + change-tracker logging: a client-generated GUID key
on an owned child not mapped `ValueGeneratedNever()` makes EF's graph-attach heuristic classify a *new* child
as `Modified` → phantom `UPDATE … WHERE Id=<new-guid>` → 0 rows → `DbUpdateConcurrencyException`. Fixed on
`Transaction.Id`, `DeliveryAttempt.Id`, `PriceTier.Id`, plus `Money.Clone()` de-aliasing and a tracked-safe
`UpdateAsync`. **41 previously-red integration tests repaired, 0 regressions.**

It also correctly **retracted** the earlier "`Version` optimistic-concurrency token is broken" hypothesis —
reproduction proved the `Version` WHERE clause matched. That retraction is valuable institutional memory and
belongs in the promoted spec.

**Sole open item** (§4.1, migrate `Money`/`Price` to EF Core complex types once nullable complex types land in
EF 10+) is explicitly a future follow-up, already assigned to #10. Not a reason to hold the change open.

**Priority note:** promote this change's `aggregate-persistence-integrity` capability **early**. It is the only
place the rule *"any owned entity/collection with a domain-generated key ⇒ `ValueGeneratedNever`; any owned
value object flowing into more than one slot ⇒ `Clone()`"* is written down, and the rule has already been
violated three more times since (see #3).

---

### 5. `booking-slot-integrity` — ALREADY IMPLEMENTED → archive

**Progress** 11/12 · **ADR-004 accepted 2026-07-28**

**Verified in tree:** migration `20260728064537_AddBookingSlotOverlapConstraint`
(`CREATE EXTENSION btree_gist` + `EXCLUDE USING gist ("StaffId" WITH =, tstzrange(...) WITH &&)` partial on
Requested/Confirmed), 409 `SLOT_TAKEN` mapping in `ExceptionHandlingMiddleware`.

The design refinement here was correct and better than the proposal: a GiST **exclusion constraint** rejects
*all* overlaps rather than only identical start times, covers create **and** reschedule (both just insert a
`Bookings` row), and made the originally-planned risky "fail-closed handler" change unnecessary. Validated by
3 randomized seeds × 40 truly-simultaneous inserts — the no-overlap invariant held under every interleaving.

**§5.2** (full integration suite in CI) is blocked by the pre-existing red suite (F5), not by this change.

**Two follow-ups must be carried forward when archiving — do not lose them:**
- The **missing `ProviderAvailability.StaffId` filter** in `FindOverlappingSlotsAsync`, handed here by #3 §2.5.
- **Pre-deploy data audit**: the exclusion constraint fails to create if production holds overlapping active
  bookings. §1.3 marks this a deployment step; it belongs in the deployment checklist, which already has the
  equivalent line for the payment dedup index.

---

### 6. `harden-resource-authorization` — ALREADY IMPLEMENTED; **tasks.md is stale** → correct, then archive

**Progress** 15/22 · **ADR-003 accepted 2026-07-28**

**tasks.md understates reality.** §6 is labelled *"MANDATORY acceptance before C1 is closed"* with 6.1–6.3
unchecked — but ADR-003's close-out section and the repository both say that work is done:

- `src/Infrastructure/Booksy.Infrastructure.Security/Authentication/SignalRAccessTokenExtractor.cs`
- `tests/…/API/Notifications/SignalRAccessTokenExtractorTests.cs` (4 unit)
- `tests/…/API/Notifications/NotificationHubAuthTests.cs` (2 real-`HubConnection` E2E: anonymous rejected,
  authenticated connects **and reconnects**)

And it fixed two genuinely serious holes: the hub had **no `[Authorize]`** at all (accepting anonymous
connections), and JWT `OnMessageReceived` read `access_token` from a **header** — which browsers cannot set on
a WebSocket handshake, so the standard SignalR `?access_token=` negotiation never authenticated a real client.

**Action:** tick 6.1–6.3 with the test names as evidence, then archive.

**Genuinely open, small — carve into a follow-up:** §2.2b (`ByProvider` server-derivation; currently inert,
defaulted false, controller-unset) and §2.3 (`AddBookingNotesCommand` into the ownership set). §4.2b/4.3/4.4
are HTTP-level nice-to-haves; the behavior is already proven by 7 unit + 9 boundary integration tests.

---

### 7. `payment-consistency-and-idempotency` — ALREADY IMPLEMENTED; **tasks.md is stale** → correct, then archive

**Progress** 7/17 — **this number is wrong** · **ADR-006 accepted 2026-07-28**

The §2 block carries a bold header reading *"**DONE + PRODUCTION-PROVEN** (GO-blocker cleared)"* immediately
above four unchecked boxes labelled `(original plan, for reference)`. The work landed; the checkboxes never did.

**Verified in tree:** migration `20260730044721_AddIdempotencyReservations`; migration
`20260728074920_AddPaymentPerBookingDedupConstraint` (partial unique `UX_Payments_OneCapturedPerBooking`).
ADR-006 records the composite-PK `(RequestType, Key)` reservation store, the `Idempotency-Key` header wiring
on `PaymentsController`, and **10 tests including 24-way concurrency (exactly one winner) and stale reclaim**.

The most valuable finding in this change is easy to miss and should survive into the spec: `EnableRetryOnFailure`
+ `TransactionBehavior` wrapped **every** command in a retrying transaction, so a transient DB fault re-ran the
handler *including its gateway call* — a live **double-charge / double-refund**. Money commands now implement
`INonTransactionalCommand`, bypass the retried ambient transaction, and self-commit one retry-safe unit.
"Money can never be duplicated" was **not** satisfied by reconciliation alone: reconciliation prevents a
duplicate *record*, never a duplicate *gateway charge*.

**Actions before archive:**
1. Tick §2.1–2.4 and §4.1/4.2 (covered by `IdempotencyBehaviorTests` + `IdempotencyStoreTests`).
2. Mark §1.2 (full intent/outbox phrasing) **superseded** — the change itself argues this, correctly.
3. Move §4.4/4.5/4.6 (commit-failure-after-gateway-success, ZarinPal duplicate-callback, and the
   "no gateway call inside a DB transaction" architecture test) into the new test-hardening change.
   The architecture test is worth building for real — `Booksy.ArchitectureTests` is currently an empty stub
   with `NetArchTest` referenced and no rule enforced anywhere.

---

### 8. `financial-ledger-and-settlement` — ALREADY IMPLEMENTED → archive

**Progress** 13/19 · **ADR-007 + ADR-002 accepted 2026-07-29** · the change's own summary states
**"C5 is COMPLETE (22 tests: 11 domain + 11 integration)"**.

**Verified in tree:** `Domain/Aggregates/LedgerAggregate/` (`LedgerEntry`, `LedgerTransaction`,
`LedgerAccount`, `LedgerEventKeys`), migration `20260728212431_AddLedgerEntries`, `LedgerReconciler`,
`LedgerMaintenanceBackgroundService` (`project.md` §Financial ledger).

Append-only enforced at the DbContext (`EnforceLedgerAppendOnly` throws on Modified/Deleted), balanced
double-entry property-tested (3 seeds × 500 iterations), idempotent posting keyed by a stable `LedgerEventKeys`
id against a unique `(EventId, Account)` index, commission recognized **at payout** not at charge, and payout
computed **from the ledger** (`GetProviderPayableBalanceAsync`) rather than by re-summing `Payments`.

The unchecked boxes are misleading: §3.4 ("Pending intent recovered to Paid; abandoned → Failed") is
`PaymentReconciler`, delivered and tested in #7. §4.1 duplicates the summary's own verification.

**Genuinely open — all three are decisions or additive scope, not defects:**
- Refund→commission **reversal entry** when a refund follows commission recognition at payout. Today the
  refund debits ProviderPayable, correctly reducing/negativing the balance; a separate PlatformRevenue
  reversal entry is a refinement.
- **Payout clawback policy** (negative balance → real transfer) — a finance decision, gated off. Safe default
  (block-future-payouts) is active. Audit P1-3.
- **Ledger-backed financial reporting endpoints** (admin/finance scope) — additive, out of customer scope.

Recommend one small deferred change: `financial-reporting-and-clawback-policy`.

**Also record ADR-002's deliberate trade-off in the promoted spec:** the domain-event dispatcher runs handlers
in a separate DI scope, so ledger posts **commit their own write** and cannot enlist in the command transaction.
That is intended — posting is idempotent by event id, and a reconciler + drift check close the gap. Someone
will otherwise "fix" this later as a bug.

---

### 9. `customer-payment-experience` — REVISE (split): archive the delivered scope, track the gates separately

**Progress** 18/26. Every one of the 8 open items is an **external gate**, not engineering work.

**Delivered and green:** T1 (Domain 347/347, Application 96/96, Flutter 105/105, Vue payment-return 10/10,
`flutter analyze` clean) and **T2 — 30/30 assertions twice consecutively** through the real HTTP stack with a
fake gateway only at the bank boundary (`tests/e2e/deposit-checkout-flow.sh`).

**Verified in tree:** `booksy-customer-app/lib/features/checkout/{data,domain,presentation}`,
`test/features/checkout/`, `FeatureFlags.checkoutEnabled` (`feature_flags.dart:16`, `defaultValue: false`),
gated call site at `booking_flow_page.dart:346`; migration
`20260809135725_AddDepositTypeAndProviderBookingPolicy`.

Two decisions here are sound and should be recorded rather than revisited:
- **Deep links deferred deliberately, and test-pinned.** Deposit recording and booking confirmation happen in
  the *server-side* callback with no client involvement (proved by T2 steps 8–11, which run with no client at
  all). `android_manifest_test.dart` asserts no `BROWSABLE` category and exactly one `<intent-filter>`, so
  adding inbound routing becomes a deliberate revisit rather than silent drift.
- **External browser over WebView** — a banking UI without a visible address bar / TLS indicator is not
  acceptable, and WebView is unsupported on Flutter web anyway.

**Blocked by things this repository cannot produce:**
- **T3** — `Payment:ZarinPal:MerchantId` is the literal placeholder `"your-zarinpal-merchant-id"`;
  `CallbackUrl` is `https://localhost:7002/api/v1/payments/zarinpal/callback`, unreachable by the gateway
  **and pointing at a path that does not exist** (the real route is `GET /api/v1/Payments/callback`);
  `Application:ClientUrl` is `https://booksy.com`, so a sandbox payment would redirect the tester to production.
- **T4** — no emulator, no `ios/` directory, and `flutter build apk` cannot complete because
  **Google Maven 404s everything** on this machine (Flutter/pub are mirrored to `flutter-io.cn`; Gradle's
  `google()` is not). That is infrastructure, not code.

**Recommended split:**
1. Fix the `MODIFIED`-in-a-new-capability defect (F7), then **archive** so `customer-checkout` becomes a real
   spec describing delivered behavior.
2. Open a tiny `checkout-release-gates` change (or a release-checklist entry) holding 6.1–6.5 + 5.2 + 5.4.
   These are ops/credential steps, and parking them inside a code change makes the change look 70% done when
   the code is 100% done.
3. **Keep `CHECKOUT_ENABLED` off** until T3 has actually run. Also fix the dead Vue call
   `paymentService.getPaymentsByBooking()` → `GET /Payments/booking/{id}`, which does not exist on the backend.

---

### 10. `booking-data-and-migration-hygiene` — REVISE, then IMPLEMENT · **highest-value remaining backend work**

**Progress** 2/12 · created 2026-07-28 · audit **P1-4**

Every premise re-verified as still true today:

- **Booking indexes are still commented out** — `BookingConfiguration.cs` has `CustomerId`, `ProviderId`,
  `StaffId`, `Status`, `(StaffId, Status)` and the availability composite all commented. Only `ServiceId` and
  `IndividualProviderId` are live. `my-bookings`, provider queries, and the conflict check full-scan.
- **Both migration folders still exist** — `Infrastructure/Migrations/` (**18** migrations, current) and
  `Infrastructure/Persistence/Migrations/` (**4**, ending `20251223143438_RemoveStaff`). Two "Init"-style
  migrations, one history table.

**Three revisions needed before implementing:**

1. **§1.1 must be rewritten, not uncommented.** The commented availability composite is
   `.HasFilter("[Status] IN ('Requested', 'Confirmed')")` — **SQL Server bracket syntax**. On PostgreSQL that
   is invalid; it needs `"Status" IN (...)`. Blindly uncommenting produces a migration that fails to apply at
   host startup — and migrations run at startup, so that is an outage, not a test failure.
2. **§3.1 is largely resolved — restate as verification.** #3 §2a.1 made `Booking.Reschedule` deep-`Clone()`
   the successor's `TotalPrice`/`PaymentInfo`/`Policy`, so payment linkage now carries. The remaining question
   is narrower: is carrying the deposit to the successor the *desired* business rule, or should it be
   re-collected? That is a product decision, not an investigation.
3. **§0.3 is superseded** by the audit's fuller P0-3 CVE list (AutoMapper HIGH plus three more HIGH
   transitives). Move CVE remediation into the new dependency/test-hardening change so it isn't split
   across two owners.

**Also fold in** the deduplicated index coordination with #5's exclusion constraint (§1.1 already notes this)
and #4 §4.1's EF-complex-types follow-up, which is already assigned here.

**Why this is the top backend priority:** it is the last un-owned *correctness-and-performance* item in the
production audit's P1 list, it touches the hot paths (`my-bookings`, provider queries, conflict checks), and
its migration risk is real enough that discovering the bracket-syntax problem at deploy time would be bad.

---

### 11. `refactor-identity-and-membership` — IMPLEMENT AS-IS · prune stale duplicates first

**Progress** 68/94 · the strategically most important active change · target design in
`IDENTITY_AND_STAFF_ARCHITECTURE.md`

This change is the reason several others are superseded, and it has delivered a great deal:

- **One person per phone** (§9): `IPersonProvisioningService` is the single guarded phone→Person path;
  `User.EnsureCanActAs` promotes a person to `UserType.Both` instead of minting a second account; both OTP
  handlers rewired and their ad-hoc upserts deleted; email/password registration now stores the canonical
  phone on the `User`, closing the hole where such accounts were invisible to phone lookup.
- **`OrganizationMembership` + `StaffProfile`** aggregates, migrations
  (`AddOrganizationMembership`, `AddMembershipAuditTrail`, `AddStaffProfileDisplayName`), append-only
  `MembershipAuditEntry`, FluentValidation, invitation revoke/expiry.
- **Synthetic-`UserId` path retired** (§18): `AddStaffToProviderCommandHandler` no longer calls
  `UserId.CreateNew()`. Account-less staff are modelled as an **unclaimed membership** (`PersonId = null` +
  `StaffProfile.DisplayName`), claimable later while **preserving the membership id** so existing bookings
  survive. Every bookable person in the system is now a membership.
- **Booking core is membership-native** (§10): `AvailabilityService` refactored to bookable **resources**
  rather than `Provider` records; slots are org-owned with `StaffId = MembershipId`; a member is bookable the
  moment they join, in the same transaction.
- **One roster platform-wide** (§11): legacy `GET /Providers/{id}/staff` now reads the membership roster with
  the DTO shape unchanged, so the Vue admin keeps working while seeing the same people the Flutter app sees.
- Backend **440 green** / Flutter **387 green** at last recorded run; keystone extended to the full
  membership chain (invite → self-invite refused → invitee accepts with their own account → personId asserted
  identical → roster → bookable slots → customer books that member → last-owner termination refused).

**Prune these before continuing — they are already done elsewhere in the same file:**

| Stale | Superseded by |
|---|---|
| §8.5.1 – 8.5.3, 8.5.5 (staff-specific member booking) | §10 (WS5a) — delivered in full |
| §7.3 (extend keystone to membership chain) | §16.1 — authored and passing 16/16 |
| §4.7 (FluentValidation for new commands) | §15.1 — already self-marked superseded |
| §13.6 (remaining audit hooks) | §15.3 — OwnerCreated / StaffProfileEnabled / Accepted all wired |
| §4.3 duplicated (self-invite guard portion) | §4.1 |

**Genuine remaining work, in dependency order:**

1. **§1.2 + §1.3 — phone partial-unique index, gated behind the dedupe report.** The staged
   `phone_uniqueness.sql` exists. The index is deliberately *not* applied yet because migrations run at host
   startup and it would fail if the dev DB already holds duplicate phones. This is the last structural hole in
   the "one person per phone" guarantee — app-level guards are in place, the DB backstop is not.
2. **§3.4 — membership backfill** (`backfill_memberships.sql`, staged, not auto-applied) + §3.5 repository
   integration tests.
3. **§8.7 → §8.3 — migrate the Vue frontend to memberships**, which unblocks retiring
   `POST /Providers/{id}/staff` and `AddStaffToProviderCommandHandler`. **This is the critical-path item**:
   #12 is blocked on it, F2's wrong main spec is caused by it, and it's the last thing keeping two staff
   models alive.
4. **§8.8 — replace `InvitationRegistrationService`'s self-HTTP with in-process calls**, and build the
   two-schema integration harness (concurrent double-accept must not create two persons; reuse-by-phone at
   the DB boundary). Same architectural seam as F5's refresh-token defect — **do them together.**
5. §4.3 (join-request → membership), §4.4 (AddStaff shim), §5.3/5.4 (JWT `memberships[]` +
   `activeMembershipId`), §5.5, §6.4/6.5/6.6/6.8 (Flutter register-and-accept UI, complete-staff-profile,
   staff-list sourcing, tests), §16.3, §17.6 (staff workspace — needs UX definition), §7.4.

**Keep as-is otherwise.** The design is right, the decisions in §12 (placeholder member) and §17.3
(only Owner memberships switchable — honest rather than a 403-ing entry point) are well-reasoned, and the
change is tracking its own scope honestly.

---

### 12. `add-provider-hierarchy` — SUPERSEDED → archive with `--skip-specs`

**Created** 2025-11-22 · 132/190 · no `.openspec.yaml` · 9 months old · the oldest change in the tree

**What actually shipped from it (Nov–Dec 2025):** §1–10 are real. Migrations
`20251122131949_AddProviderHierarchy` and `20251122145237_AddIndividualProviderIdToBookings` are in the tree;
`Provider.ParentProviderId` exists (`Provider.cs:39`); `ProviderInvitation` and `ProviderJoinRequest` are two
of ServiceCatalog's 14 aggregate roots. The Vue UI shipped too — `ProviderHierarchy.vue`, `InviteStaffModal.vue`,
`InvitationCard.vue`, `JoinRequestCard.vue`, `RequestToJoinModal.vue`, `SearchOrganizations.vue`,
`hierarchy.service.ts`, `hierarchy.guard.ts`.

**Why it's superseded.** #11's proposal states it explicitly: *"Supersedes the identity/parent mechanism of
`add-provider-hierarchy`: its `ProviderInvitation`/`ProviderJoinRequest` aggregates and per-staff booking
attribution are retained and rewired; **`ParentProviderId`-as-membership is retired**."* The core premise of
this change — staff are a second `Provider` linked by a scalar `ParentProviderId` — is exactly what
`IDENTITY_AND_STAFF_ARCHITECTURE.md` identifies as broken: a person can't belong to two salons, can't be
owner-and-staff, has no leave/rejoin lifecycle, and the wired add-staff path minted a throwaway
`UserId.CreateNew()`.

**Its remaining 58 tasks should not be executed as written.**
- §11.9/12.9/15.7 (component unit tests), §13.6/14.6/16.6/17.7/20.x (E2E) would pin the **retired** model.
- §12.7/13.3/14.5/17.4 (notification templates & wiring) are now owned by the archived
  `notification-delivery-reliability`.
- §18 (search indexing/SEO for hierarchy), §19 (admin hierarchy dashboards, orphaned-individual tooling),
  §21 (7 doc tasks), §22 (10-step feature-flag rollout), §23 (8 post-launch monitoring tasks) are aspirational
  planning for a 5-6-week/2-3-developer project shape that does not match how this repo is actually worked.
- §12.8 is self-labelled "future phase".

**The one genuinely-needed remnant** — migrating the Vue hierarchy UI off sub-providers — is already owned by
#11 §8.7. Don't duplicate it here.

**⚠ Archive with `--skip-specs`.** This change's three delta specs carry `MODIFIED` **and `REMOVED`**
requirements against `provider-management`, `provider-registration`, and `staff-management`. Per
`openspec/AGENTS.md` the archiver replaces whole requirements with the delta text — a normal archive would
write the retired sub-provider staff model into main specs and delete existing requirements. Also delete or
clearly stamp `README.md`, which still reads *"Status: 📋 Proposal Ready for Review"* with four unresolved
"Decision Points Needed" that were in fact decided long ago (payment distribution, owner-as-staff,
service-catalog control, admin approval).

---

### 13. `implement-customer-mobile-app` — OBSOLETE → archive with `--skip-specs` (or delete)

**Created** 2026-01-02 (per README "Last Updated: January 2, 2026") · 8/105 · no `.openspec.yaml` ·
README self-reports *"60% complete (3 of 5 specs done)"* — i.e. the **proposal** was never even finished.

**Superseded twice:**
- **#14 `customer-app-ux-redesign`** (2026-07) delivered the entire app: theme, component library,
  go_router shell, auth, home, explore, provider detail, booking flow, appointments, profile, 41 tests.
- The **archived `unify-customer-app-with-provider-design`** (2026-07-23) then converged its visual language
  onto the provider/Coliride design and produced the `customer-app-*` / `customer-*-journey` main specs.

**Its tasks now actively contradict the shipped app** — this is the clearest signal it's obsolete:

| Task says | Reality |
|---|---|
| 1.1.2 add `pull_to_refresh` ✅ | **Removed** by #14 §1.6; `pubspec.yaml` has no such dependency |
| 1.2.3 build buttons in `lib/shared/components/buttons/` | No `lib/shared/` exists; components live in `lib/core/widgets/` (19 files) |
| 1.2.2 "Use ScreenUtil for responsive sizing" | #14 §9.2 **deliberately removed `.sp` from all text styles**; sizes come from `TextTheme` |
| Design: single `#1A365D`, flat, no shadows, gender-neutral | Superseded by the Coliride/provider-aligned token set (`app_tokens.dart`) |
| 1.1.3 "Configure code generation ✅ successful" | `CLAUDE.md` records `build_runner` codegen as **broken** (retrofit_generator/SDK incompatibility); new services use manual JSON + manual `get_it` |

Its "Questions for Stakeholders" were all answered months ago (ZarinPal, Neshan, external-browser payment
return). Its five `customer-mobile-*` capability specs would add a **third** parallel description of the
customer app (F3).

**Recommendation:** archive with `--skip-specs` for the historical record, or delete outright. Do **not**
promote its specs. If any of its 80+ scenarios contain requirements not covered by `customer-*-journey` or
#14's deltas, harvest those individually rather than promoting the capability.

---

### 14. `customer-app-ux-redesign` — ALREADY IMPLEMENTED → resolve capability naming, then archive

**Progress** 53/54 · created 2026-07-14 · already credited in `COMPLETION_ROADMAP.md` Phase 4

Fully delivered: M3 token theme, 19-component library in `lib/core/widgets/`, `StatefulShellRoute` go_router
shell with auth redirect + return-to-intent, OTP flow with autofill + resend timer, per-section-failure home,
debounced explore with stale-result guards, provider detail, stepped booking flow with slot-taken recovery,
appointments with optimistic cancel/rollback and reschedule, profile, `OfflineBanner`, accessibility sweep
(1.3× font scale, semantics, reduced motion, direction-aware icons). `flutter analyze` clean, 41 tests green.

**Sole open task** is §10.2, a manual on-device sweep — environment-blocked (no emulator; see #9's Google
Maven finding). Not a reason to hold the change.

**Two things to settle first:**

1. **Capability naming (F3).** Archiving as-authored adds `mobile-design-system`, `mobile-app-shell-ux`,
   `mobile-booking-ux`, `mobile-discovery-ux`, `mobile-auth-ux` alongside the existing
   `customer-app-visual-tokens`, `customer-app-component-styling`, `customer-app-chrome-styling`,
   `customer-booking-journey`, `customer-discovery-journey`. Four of the five overlap directly. Decide the
   convention and rename the deltas before promoting. (`mobile-auth-ux` has no main-spec counterpart —
   `specs/authentication` is the *Vue provider* login — so it needs a home either way.)
2. **F6 — the uncommitted map-discovery work.** `map_discovery_page`, `map_discovery_cubit`, `map_clustering`,
   `map_pin`, `map_provider_card`, `category_filter_row` are new in the tree; `area_page`, `nearby_page`,
   `area_search_cubit` are deleted. That is a real feature change to discovery. Decide whether it belongs to
   the archived unify change (whose stated gap was discovery/map/area search) or needs its own change — and do
   it **before** archiving #14, so the discovery capability isn't promoted already out of date.

Also worth keeping: `findings.md` documents API gaps discovered during the audit. Make sure those survive
archiving — either promote them into the relevant specs or move them into `COMPLETION_ROADMAP.md`.

---

### 15. `design-system-convergence` — REVISE (much of it silently landed), then DEFER

**Progress** 11/34 · created 2026-07-15 · scope: `booksy-provider-app` presentation only

**§1–2 done as recorded.** `AppMotion` and `AppIconSize` exist in `app_tokens.dart`; `AppLoading`,
`AppEmptyState`, `AppErrorState` exist with tests (`test/core/widgets/feedback_states_test.dart`).

**§3.1–3.7 and §4.1–4.3 are already implemented — by later work, not by this change.** The
`2026-07-17` provider-home-workspace batch built them. Present in `lib/core/widgets/`:
`app_card.dart`, `app_info_card.dart`, `app_list_row.dart`, `app_section_header.dart`,
`app_status_badge.dart`, `app_icon_button.dart`, `app_dashed_divider.dart`, `app_sheet.dart`
(`showAppSheet`), `app_dialog_header.dart`, plus `test/core/widgets/design_system_components_test.dart`.
`app_button.dart` already has `AppButtonSize {small, dialog, medium, big}` × roles
`{primary, secondary, destructive, text}`, which is §4.1's ask (plus a `medium` the task didn't anticipate).

**What is genuinely still open is the adoption sweep, not the components:**

- **§3.8 call-site migration has not happened.** Only `lib/features/home/presentation/widgets/booking_card.dart`
  consumes any of the shared structure components. The onboarding steps named in §3.8
  (`preview_step`, `services_step`, `category_step`, `location_step`, `gallery_step`) still compose their own.
- **§4.4** — no `showAppDialog`/`AppDialog` exists (only `app_dialog_header.dart`); `grep showDialog(` returns
  **0**, so the logout-confirmation consumer was never wired.
- **§5** — no `AnimatedRotation` anywhere; no selection-state tokens in `lib/features/`.
- **§6.2's own guard already fails**: **8** raw `CircularProgressIndicator` remain in `lib/features/`.
  (`BoxShadow` = 0 ✓ and `showDialog(` = 0 ✓, so two-thirds of the guard passes.)

**Revisions:** mark §3.1–3.7 and §4.1–4.3 as delivered-by-other-work (naming the actual files, and noting
`AppSheetScaffold`/`showAppSheet` as the accepted form of §4.3 and `app_dialog_header.dart` as partial §4.2);
drop `app_inline_add_button.dart` if the section-header component already carries the add affordance;
re-scope the change to what it now is — **a shared-component adoption sweep**.

**Then defer.** It's presentation-only, zero user-visible breakage, and it sits behind every launch blocker in
§5. Its main ongoing value is preventing drift, which the existing components already largely achieve.

---

## 4. Recommended cleanup

### Step 0 — commit what's already staged (do this first)

The `notification-delivery-reliability` and `refactor-provider-category-model` archive moves are sitting
uncommitted alongside 310 other changed paths, including the new untracked
`openspec/specs/notification-delivery/`. Commit the archive moves and their spec promotions on their own so
they don't get lost in the next large commit.

### Step 1 — archive the 6 clean ones (no decisions needed)

Order matters only for the first pair.

```
openspec archive add-playwright-e2e --yes                       # creates the e2e-testing capability
openspec archive harden-e2e-test-coverage --yes                 # extends it
openspec archive fix-reschedule-membership-staff --yes          # promote booking-reschedule
openspec archive fix-aggregate-persistence-concurrency --yes    # promote FIRST among the money set — see P6
openspec archive booking-slot-integrity --yes
openspec archive financial-ledger-and-settlement --yes
openspec validate --all --strict
```

Before each: carry the named follow-ups forward (F5's slot `StaffId` filter and the pre-deploy overlap audit
from #5; the clawback/reversal/reporting decisions from #8) into the new changes in Step 4 — an archive should
never be where a known defect goes to die.

### Step 2 — correct two stale task lists, then archive

- **`harden-resource-authorization`**: tick §6.1–6.3, citing `SignalRAccessTokenExtractorTests` (4) and
  `NotificationHubAuthTests` (2). Archive.
- **`payment-consistency-and-idempotency`**: tick §2.1–2.4 and §4.1/4.2; mark §1.2 superseded; move
  §4.4/4.5/4.6 to the test-hardening change. Archive.

### Step 3 — retire the superseded pair (spec-safe)

```
openspec archive add-provider-hierarchy --skip-specs --yes       # MODIFIED+REMOVED deltas would corrupt main specs
openspec archive implement-customer-mobile-app --skip-specs --yes
```

Then update `IDENTITY_AND_STAFF_ARCHITECTURE.md` to mark `add-provider-hierarchy` superseded
(this is #11 task 7.4).

### Step 4 — open the changes that currently own nothing

| New change | Contents | Why |
|---|---|---|
| `fix-provider-token-refresh-in-process` | Replace `ProvidersController.RefreshProviderToken`'s HTTP self-call with an in-process cross-context seam into UserManagement's `IJwtTokenService` (mirroring `IProviderInfoService` in reverse). Un-skip the gallery e2e. | F5 · blocks freshly-registered providers' dashboards · referenced by 4 documents, owned by none · same seam as #11 §8.8, so **do them together** |
| `harden-test-suite-and-dependencies` | Triage the ~46 red ServiceCatalog integration tests to green; retarget per-service integration projects to `Booksy.Host` and re-enable the CI step; fix the EF owned-`Money` insert quirk poisoning payment aggregates; bump AutoMapper + the three HIGH transitives; add a CI gate failing on any HIGH advisory; add the real `NetArchTest` rules (starting with "no `IPaymentGateway` call inside a DB transaction") to the empty `Booksy.ArchitectureTests` stub | audit P0-3 + P1-1 · ROADMAP Epic 3.1 · a red suite hides regressions and `dotnet.yml` runs no tests at all |
| `fix-api-response-envelope` | Consolidate the duplicate `ApiResponseMiddleware` (Core.Domain vs Infrastructure.API); restore per-action success wording | F5 · #3 §4.4b explicitly hands this off |
| `financial-reporting-and-clawback-policy` | Refund→commission reversal entry; payout clawback policy decision; ledger-backed finance/admin reporting endpoints | #8's three documented residuals · P1-3 |
| `checkout-release-gates` *(or a release checklist entry)* | ZarinPal sandbox MerchantId, public HTTPS callback at the **correct** path, `Application:ClientUrl`, T3 run, then flip `CHECKOUT_ENABLED`; remove the dead Vue `getPaymentsByBooking()` call | #9's §6 — credentials and infrastructure, not code |
| `customer-app-map-discovery` *(scope first)* | The uncommitted map-discovery feature | F6 · a whole feature with no spec |

### Step 5 — spec hygiene (cheap, high payoff)

1. Resolve the F3 capability-name collisions and rename #14's and #15's deltas accordingly.
2. Fix #9's `MODIFIED`-in-a-new-capability defect.
3. Replace the **26** `TBD - created by archiving change …` purposes with one real sentence each, and add
   "write the Purpose" to the archive routine so newly-promoted capabilities don't inherit the same placeholder.
4. Once #11 §8.7 lands, rewrite `specs/provider-staff-management` and `specs/staff-management` onto the
   membership model, or fold them into `organization-membership` (F2).

---

## 5. Implementation priorities

Ordered by risk-adjusted value, given the audit's **NO-GO (conditional, ~76%)** verdict and that
**P0-2 (notifications) closed today**.

**P1 — `refactor-identity-and-membership` §8.7 → §8.3: migrate the Vue frontend to memberships.**
The critical path. Two staff models are alive simultaneously; the Vue app is the only reason the synthetic
`POST /Providers/{id}/staff` path can't be retired. It also blocks #12's clean retirement and is the direct
cause of F2's wrong main spec. Everything else in #11 is additive by comparison.

**P2 — `fix-provider-token-refresh-in-process` + `refactor-identity-and-membership` §8.8.**
Same architectural seam (in-process cross-context calls replacing HTTP self-calls), so one change should do
both. Fixes a live user-visible bug (freshly-registered providers see an empty dashboard/gallery), un-skips
the last e2e test, and removes the last self-HTTP call from the monolith.

**P3 — `harden-test-suite-and-dependencies`.**
Audit P0-3 (four HIGH CVEs) and P1-1 (~46 red integration tests). A red suite plus a CI workflow that runs no
tests means every change above ships on hope. Cheapest large risk reduction available.

**P4 — `booking-data-and-migration-hygiene` (revised).**
Restore the booking indexes — **rewriting** the availability composite filter for Postgres, not uncommenting
SQL Server syntax — and settle the dual migration folder. Last un-owned P1 correctness/performance item.

**P5 — `refactor-identity-and-membership` §1.2/§1.3 + §3.4: the phone unique index and backfills.**
The DB-level backstop for "one person per phone". Sequenced after the dedupe report by design; do not
auto-apply, since migrations run at host startup.

**P6 — Promote `aggregate-persistence-integrity` early, and make its rule enforceable.**
Not a feature, but the highest-value *spec* promotion. ADR-005's rule has now been violated **three more
times** since it was written (`Booking.TotalPrice` + `Booking.Id`, `BookingHistoryEntry.Id`, and in #3 both
`Booking.Reschedule`'s three owned VOs and `CreateBookingRequest`'s shared `Price`/`BookingPolicy`). A
documented rule that keeps being broken wants a test, not another paragraph: add an EF-model assertion that
every owned entity with a domain-generated key is `ValueGeneratedNever`.

**P7 — `checkout-release-gates`, when credentials exist.**
Audit P0-1 is now "verify", not "build": the code is done and T1/T2 green. The blockers are a sandbox
MerchantId, a public HTTPS callback, and a device — none obtainable here. Keep `CHECKOUT_ENABLED` off.

**P8 — `financial-reporting-and-clawback-policy`** (needs a finance decision) and
**P9 — `design-system-convergence` (re-scoped adoption sweep)**. Both genuinely deferrable.

---

## 6. Evidence index

Claims above are checkable with these:

| Claim | Check |
|---|---|
| Change progress | `openspec list` |
| 26/29 specs have a placeholder purpose | `grep -rl "TBD - created by archiving" openspec/specs/ \| wc -l` → 26 |
| Structural validity | `openspec validate --all --strict` → 44/44 |
| Booking indexes still commented | `openspec/../src/…/Persistence/Configurations/BookingConfiguration.cs` (search `//builder.HasIndex`) |
| SQL-Server bracket syntax in the commented filter | same file, `HasFilter("[Status] IN (...)")` |
| Dual migration folders | `…/Booksy.ServiceCatalog.Infrastructure/Migrations` (18) vs `…/Persistence/Migrations` (4) |
| Slot / dedup / ledger / idempotency / deposit migrations | `20260728064537`, `20260728074920`, `20260728212431`, `20260730044721`, `20260809135725` |
| Refresh-token self-call still live | `…/ServiceCatalog.Api/Controllers/V1/ProvidersController.cs:620` |
| SignalR auth acceptance done | `tests/…/API/Notifications/{SignalRAccessTokenExtractorTests,NotificationHubAuthTests}.cs` |
| Provider-app design components exist | `booksy-provider-app/lib/core/widgets/` (22 files) + `test/core/widgets/design_system_components_test.dart` |
| §6.2 guard fails / partly passes | `grep -rc CircularProgressIndicator lib/features` → 8; `BoxShadow` → 0; `showDialog(` → 0 |
| Customer app superseded #13's premises | no `lib/shared/`; no `pull_to_refresh` in `pubspec.yaml`; 19 files in `lib/core/widgets/` |
| Checkout built and flag-gated off | `lib/features/checkout/`, `lib/config/feature_flags.dart:16` |
| E2E harness present | `booksy-frontend/e2e/global-setup.ts`, `playwright.config.ts:22`, `tests/e2e/keystone-booking-flow.sh:63` |
| Notification change archived complete | `changes/archive/2026-08-19-notification-delivery-reliability/tasks.md` → 14 checked / 0 open |
| No membership in main specs | `grep -ril membership openspec/specs/` → no matches |
