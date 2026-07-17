## Context

The approved Provider App Information Architecture (`booksy-provider-app/PROVIDER_APP_INFORMATION_ARCHITECTURE.md`) established that the Flutter provider app's home is an operational **"Today" workspace** answering *"What do I need to do right now?"*. The `specs/provider-home-workspace/spec.md` in this change then enumerated every Home state (first-login, empty business, no-appointments, pending verification, active day, fully booked, closed, offline, error, vacation) plus the foundational requirements: three-layer state resolution, business-maturity adaptation, zone composition, RTL/accessibility, and **widget-based composition**.

This design document explains **how** those requirements compose into a single adaptive screen without hardcoding a layout: a **state-resolution engine** produces an immutable `HomeContext`, a **widget registry** maps that context to an ordered set of independent zone widgets, and the **Home page orchestrates** — it owns no zone business logic. This is a design/architecture artifact only; it does not implement Flutter, produce high-fidelity mockups, or design other tabs.

**Current state:** `ProviderDashboardPage` is a placeholder (a centered welcome string). `app_router.dart` already routes `Authenticated` and `PendingVerification` providers to `/dashboard`; `Suspended/Inactive/Archived` are gated to the blocked screen; `Drafted` goes to onboarding.

**Constraints:** Persian/RTL, mobile-first, Coliride visual language for look/feel only. Known toolchain constraint (repo CLAUDE.md): `build_runner` codegen is broken — new services use manual JSON parsing and manual `get_it` registration. Known footgun (memory): themed buttons are infinite-width (`Size.fromHeight`) and blow up inside a `Row` — relevant when we build widget action rows.

## Goals / Non-Goals

**Goals:**
- A composition architecture where the Home is *assembled*, not *authored* — widgets are added/removed/reordered/replaced via a registry, never by rewriting the screen.
- A deterministic, testable rule for *which* widgets appear, in *what order*, for any combination of system/lifecycle/maturity/day-context conditions.
- A single common widget contract so every zone behaves consistently (independent loading/empty/error/offline, self-declared visibility and priority, declared refresh strategy).
- A maturity model that makes the screen evolve with the business (scaffold recedes, agenda grows) from computed signals — never a manual toggle.
- Framework-agnostic specification: expressed as contracts and rules a Flutter/BLoC implementation can follow, without prescribing line-by-line code.

**Non-Goals:**
- No Flutter implementation, no hi-fi mockups (low-fi zone wireframes only, to communicate hierarchy).
- No design of Calendar, Clients, or More tabs; no new business features beyond what the IA/spec already scope.
- No new backend endpoints designed here (dependency gaps are flagged, not resolved).
- Not defining the visual token *values* (colors/hex) — we reference Coliride semantic tokens by role.

## Decisions

### D1 — A pure state-resolution engine produces an immutable `HomeContext`

A single resolver computes one immutable value object, `HomeContext`, from the raw inputs. The Home renders *only* from `HomeContext`; widgets never re-derive global state. Resolution runs in strict layer order (matching the spec's precedence):

```
resolveHomeContext(inputs) -> HomeContext:
  # Layer 0 — System (highest precedence)
  system = ERROR            if dataLoad failed AND no cache
         = OFFLINE          if no connectivity        (cached data still surfaced)
         = LOADING          if first load in flight
         = OK               otherwise

  # Layer 1 — Lifecycle & Availability  (router already excludes Blocked)
  lifecycle    = PENDING_VERIFICATION if providerStatus == PendingVerification
  bookingMode  = INSTANT | REQUEST            # provider config (hybrid, selectable)
  availability = backend-provided state       # VACATION | CLOSED_TODAY | OPEN
              # Home CONSUMES this; it does NOT compute vacation/closed locally

  # Layer 2 — Maturity & Day-Context
  maturity = classifyMaturity(signals, remoteConfigThresholds)   # see D4
  day = FULLY_BOOKED   if open capacity today == 0 and appts today > 0
      = ACTIVE         if appts today > 0
      = NO_APPTS       if appts today == 0
  # (day is meaningful only when availability == OPEN)

  banners = orderBySeverity([error?, offline?, vacation?, closed?, pending?, maturityNudge?])
  return HomeContext(system, lifecycle, bookingMode, availability, maturity, day, banners, data…)
```

`HomeContext` is the *single source of truth* the orchestrator and every widget's visibility rule read. It is trivially unit-testable: input fixtures → expected context. This is the "adaptive, not static" mechanism made concrete.

**Alternatives considered:** (a) letting each widget decide its own visibility from raw repositories — rejected: duplicated logic, no global precedence, untestable stacking (offline + pending). (b) A finite set of named "screen modes" — rejected: the states are *orthogonal* (an Operational provider can be simultaneously No-Appointments + Offline); enumerating the cross-product explodes and is exactly the "static layout that fails most situations" the proposal warns against.

### D2 — Widget-composition architecture: orchestrator + contract + registry

The Home is a **composition of independent zone widgets**. Three pieces:

1. **The common widget contract** (`HomeWidget`) — every zone implements it:

   | Contract field | Meaning |
   |----------------|---------|
   | `id` | Stable identifier (for ordering, testing, telemetry) |
   | `purpose` | Single responsibility (one job per widget) |
   | `inputs` | Declared data dependencies (which repo/stream/`HomeContext` fields it reads) |
   | `outputs` | Events it *emits upward* (navigation intents, mutation requests) — the widget never navigates or mutates global state itself |
   | `visibilityRule(HomeContext) -> bool` | Pure predicate; the widget declares when it applies |
   | `priority` | Ordinal used by the orchestrator to place it (lower = higher on screen) |
   | `loading / empty / error / offline` | Its *own* four state treatments, rendered within its bounds |
   | `refreshStrategy` | One of `live` (stream), `poll(interval)`, `manual` (pull-to-refresh only), `cached` (static until invalidated) |
   | `collapsible` | Whether it can expand/collapse, and its default state |

2. **The orchestrator** (the Home page) — its entire job:
   - subscribe to `HomeContext`;
   - ask the registry for the ordered list of widgets whose `visibilityRule` passes;
   - lay them out top-to-bottom by `priority` (RTL);
   - route each widget's emitted `outputs` to navigation/mutation.
   It contains **no zone-specific business logic** and **no per-state layout code**.

3. **The widget registry** — a declarative list of widget descriptors (id + priority + factory). Add/remove/reorder/replace = edit the registry, not the page. This satisfies the spec's "composition changes without redesign" requirement directly.

**Independence guarantees:** each widget owns its data subscription, so one widget loading/erroring/offline shows *its* treatment inside *its* bounds while siblings keep working (spec: "widgets fail and load independently"). A hidden widget is omitted and its space reclaimed (spec: "self-report visibility"; "empty zone collapses").

**Alternatives considered:** a single `HomeBloc` emitting one monolithic view-model for the whole screen — rejected: couples all zones into one failure/loading domain (one slow call blocks the whole Home), and every new zone edits the god-bloc. The registry + per-widget contract keeps blast radius to one widget.

### D3 — The Home widget catalog

Each zone is one widget conforming to D2. `Ctx` = `HomeContext`. Priority is the on-screen order (1 = top, under the app bar). Outputs are intents the orchestrator routes.

**1. `HomeAppBar`** *(chrome, always present)*
- **Purpose:** identity + entry to account & notifications. **Inputs:** business name/avatar, provider status, unread-notification count. **Outputs:** `openAccountSheet`, `openNotificationCenter`. **Visibility:** always. **Priority:** 0 (fixed top). **Loading:** avatar/name skeleton. **Empty:** n/a. **Error:** show cached name. **Offline:** unchanged (badge may be stale). **Refresh:** `live` (status/badge). **Collapsible:** greeting collapses to compact title on scroll.

**2. `StatusBannerRail`** *(host)* + banner items
- **Purpose:** surface system/lifecycle conditions in severity order (D6). **Inputs:** `Ctx.banners`. **Outputs:** per-banner (`retry`, `contactSupport`, `manageVacation`, `editHours`, `viewNextOpen`). **Visibility:** `Ctx.banners` non-empty. **Priority:** 1. **Loading:** n/a (derived from Ctx). **Empty:** collapses. **Error:** the error *is* a banner. **Offline:** offline banner pins here. **Refresh:** reactive to Ctx. **Collapsible:** ≤2 banners expanded; overflow collapses into a "more" chip.
- Banner items (each a mini-widget behind the rail): `ErrorBanner`, `OfflineBanner`, `VacationBanner`, `ClosedTodayBanner`, `PendingVerificationBanner`, `MaturityNudgeBanner`.

**3. `ActionQueue` (T0)** *(booking-mode-sensitive)*
- **Purpose:** items needing immediate action. In **Request** mode this is unconfirmed booking requests (routine) + conflicts; in **Instant** mode it is exceptions only (conflicts, client-reschedule requests) — routine bookings never enter it. **Inputs:** `Ctx.bookingMode`, pending requests, conflict list (`/Bookings/provider/{id}`, `/Bookings/search`). **Outputs:** `confirm(id)`, `decline(id)`, `openBooking(id)`, `openWaitlist(id)` (fully-booked). **Visibility:** `system==OK && availability==OPEN && queue non-empty`. **Priority:** **2 in Request mode** (highest operational widget); **demoted below NowNext/TodayAgenda in Instant mode** (surfaces only when exceptions exist). **Loading:** inline row skeletons. **Empty:** collapses (with optional "all caught up" success flash — D5). **Error:** localized card + retry (siblings unaffected). **Offline:** actions become queued-for-sync with a badge; items still visible from cache. **Refresh:** `poll` + `manual` in MVP; push-ready to switch to `live` (D7). **Collapsible:** collapses beyond N items into "view all".

**4. `NowNext`**
- **Purpose:** the current/next appointment as an actionable hero — the **primary operational focus in Instant mode**. **Inputs:** today's next appointment + client contact. **Outputs:** `call`, `message`, `complete(id)`, `noShow(id)`, `reschedule(id)`, `openBooking(id)`. **Visibility:** `availability==OPEN && day∈{ACTIVE,FULLY_BOOKED} && a next appt exists`. **Priority:** 3 in Request mode; **2 (top operational) in Instant mode**. **Loading:** hero skeleton. **Empty:** collapses (handled by `TodayAgenda` empty). **Error:** localized. **Offline:** read from cache; mutations queue. **Refresh:** `poll`/push-ready. **Collapsible:** no.

**5. `TodayAgenda` (T1)**
- **Purpose:** today's timeline. **Inputs:** today's bookings. **Outputs:** per-row `openBooking`, `complete`, `noShow`, inline actions; `addAppointment`. **Visibility:** `availability==OPEN` (renders empty-state when no appts). **Priority:** 4. **Loading:** list skeleton. **Empty:** positive "no appointments today" + next-appt hint + add action (No-Appointments state). **Error:** localized + retry. **Offline:** cached list + staleness tag; mutations queue. **Refresh:** `live`/`poll` + pull-to-refresh (`manual`). **Collapsible:** long days collapse past-appointments.

**6. `ComingUpPeek` (T2)**
- **Purpose:** a glance at what's next (tomorrow / next open day count). **Inputs:** upcoming bookings summary; next open day. **Outputs:** `openCalendar(date)`. **Visibility:** always when `system==OK` (elevated when today is empty/closed/vacation). **Priority:** 6 (7 when elevated → 3.5). **Loading:** compact skeleton. **Empty:** "nothing scheduled yet". **Error:** hidden silently (non-critical). **Offline:** cached. **Refresh:** `cached` + refresh on pull. **Collapsible:** no.

**7. `ActivationChecklist`** *(maturity: Setup)*
- **Purpose:** guide first-value setup (add team, add photos, review prices, share link). **Inputs:** profile-completeness signals (staff count, gallery count, services). **Outputs:** `openStaff`, `openGallery`, `openServices`, `shareBookingLink`. **Visibility:** `maturity==SETUP`. **Priority:** 2 (hero in Setup; sits above/with agenda which is muted). **Loading:** item skeletons. **Empty:** when all complete → converts to a one-time success then hides. **Error:** per-item inline retry. **Offline:** items visible; completion queues. **Refresh:** reactive to completeness signals. **Collapsible:** yes (collapsed once mostly complete).

**8. `GetDiscovered`** *(maturity: Growth)*
- **Purpose:** drive first bookings (share link + profile-completeness meter). **Inputs:** public booking link, completeness %. **Outputs:** `shareBookingLink`, `openProfile`, `addWalkIn`. **Visibility:** `maturity==GROWTH`. **Priority:** 2 (hero). **Loading:** skeleton. **Empty:** n/a. **Error:** localized (e.g. link unavailable → explain corrective step). **Offline:** share disabled with reason; completeness cached. **Refresh:** `cached`. **Collapsible:** no.

**9. `SetupNudges` (T3)** *(maturity: Traction/Operational)*
- **Purpose:** occasional, dismissible housekeeping prompts (finish a profile gap). **Inputs:** derived nudge list. **Outputs:** `openTarget(nudge)`, `dismiss(nudge)`. **Visibility:** `maturity∈{TRACTION,OPERATIONAL} && nudges non-empty`. **Priority:** 8 (low; below agenda). **Loading:** none (cheap). **Empty:** collapses. **Error:** hidden silently. **Offline:** cached; dismiss queues. **Refresh:** `cached`. **Collapsible:** each dismissible.
- **MVP scope:** review-related nudges are **out of scope** (reviews are not in MVP). The nudge-source is extensible so a review nudge can be added later without changing the widget.

**10. `BusinessAlerts`**
- **Purpose:** anomalies that need resolution (bookings inside a vacation window; a booking on a closed day; scheduling conflicts). **Inputs:** appointments cross-checked against the **backend-provided availability state** (vacation/closed are consumed, not computed locally). **Outputs:** `reschedule`, `cancel`, `notifyClient`, `openBooking`. **Visibility:** anomalies exist (elevated during Vacation/Closed). **Priority:** 3 (elevated — needs action). **Loading:** skeleton. **Empty:** collapses. **Error:** localized + retry. **Offline:** cached; actions queue. **Refresh:** `poll`/push-ready. **Collapsible:** collapses past N.

**11. `EndOfDaySummary`**
- **Purpose:** positive closure when the day's work is done. **Inputs:** today's completed count / stats (`/Bookings/{...}` statistics). **Outputs:** `openInsights` (optional). **Visibility:** `day∈{ACTIVE,FULLY_BOOKED} && all appts completed`. **Priority:** 3 (replaces NowNext). **Loading:** none. **Empty:** n/a. **Error:** hidden. **Offline:** derive from cache. **Refresh:** reactive. **Collapsible:** no.

**12. `CreateAction`** *(center-docked, scaffold)*
- **Purpose:** the primary proactive create (New appointment / Block time). **Inputs:** none. **Outputs:** `newAppointment`, `blockTime`. **Visibility:** `system!=ERROR` (present on Home always otherwise). **Priority:** fixed (bottom-nav center, not in the scroll list). **Loading:** n/a. **Empty:** n/a. **Error:** disabled if no context. **Offline:** new appointment allowed (queues); explained. **Refresh:** n/a. **Collapsible:** menu.

### D4 — Business-maturity classification

`classifyMaturity(signals, thresholds)` is a pure function of observable signals; never a manual setting. **All thresholds are configurable values supplied by the backend / remote configuration — none are hardcoded.** The rules below give the *shape*; the numeric boundaries (`T`, gallery `N`, completeness definition) are injected at runtime, so tuning maturity never requires an app release.

| Signal | Source |
|--------|--------|
| `providerStatus` | JWT / `/Providers/current/status` |
| `profileComplete` | staff count > 0, gallery ≥ N, all services priced |
| `totalBookingsAllTime` | `/Bookings/provider/{id}` / statistics |
| `bookingsTrailing30d`, `hasRecentBooking` | statistics |

| Phase | Rule (proposed — thresholds to confirm) | Dominant widget | What recedes |
|-------|------------------------------------------|-----------------|--------------|
| **Setup** | just onboarded / `PendingVerification` **and** `totalBookingsAllTime == 0` **and** `!profileComplete` | `ActivationChecklist` (hero) | agenda muted |
| **Growth** | verified/active **and** `totalBookingsAllTime == 0` | `GetDiscovered` (hero) | agenda muted |
| **Traction** | `totalBookingsAllTime ≥ 1` **and** `bookingsTrailing30d < T` (e.g. 8) | agenda when present; `GetDiscovered`/nudges when idle | scaffold → nudges |
| **Operational** | `bookingsTrailing30d ≥ T` (sustained) | `ActionQueue` + `TodayAgenda` | nudges occasional/absent |

Maturity is **orthogonal** to day-context: an Operational provider still hits No-Appointments-Today on a quiet day, and the agenda's empty-state (not the scaffold) handles it. The see-saw — scaffold priority rises as maturity falls, agenda priority rises as maturity climbs — is implemented purely through each widget's `visibilityRule` + `priority`, so no phase needs a bespoke layout.

### D5 — Composed layouts (low-fi, RTL)

Wireframes show *priority order top→bottom*; actual layout is RTL (avatar/first-read on the right). Boxes are widgets from D3; `▸` = collapsible.

**Setup — first login, pending verification (Maturity=Setup):**
```
[ AppBar: avatar · صبح بخیر، سالن… · 🔔 ]
[ ⚠ Pending-verification banner                    ]   ← BannerRail
[ ✅ ACTIVATION CHECKLIST  (HERO)                   ]   ← ActivationChecklist
    ☑ services  ☐ team  ☐ photos  ☐ share link
[ Coming-up peek (elevated: nothing yet)           ]
[ ·· empty agenda: "no appointments — add one" ··  ]   ← TodayAgenda (muted)
                 ( ⊕ center create )
```

**Growth — verified, no traction (Maturity=Growth):**
```
[ AppBar ]
[ 📣 GET DISCOVERED (HERO): share link · profile 70% ]   ← GetDiscovered
[ + Add walk-in appointment                          ]
[ ·· empty agenda (muted) ··                          ]
                 ( ⊕ )
```

**Operational — active day (Maturity=Operational, day=ACTIVE):**
```
[ AppBar: 🔔•2 ]
[ 🔴 ACTION QUEUE: 2 requests  [confirm][decline]   ]   ← ActionQueue (loudest)
[ ★ NOW/NEXT: 10:00 Sara — [call][complete][resched] ]   ← NowNext (hero)
[ TODAY AGENDA (timeline)                          ▸ ]   ← TodayAgenda
[ Coming-up peek: tomorrow 6 appts                  ]
[ (nudges: absent)                                   ]
                 ( ⊕ )
```

**Operational — fully booked:**
```
[ AppBar ]
[ ✔ Fully booked — 12 appointments today            ]   ← chip in NowNext/agenda header
[ ★ NOW/NEXT                                         ]
[ TODAY AGENDA (dense, breaks distinct)            ▸ ]
[ (new request → routed to waitlist via ActionQueue) ]
                 ( ⊕ warns: no free slots )
```

**Operational — no appointments today:**
```
[ AppBar ]
[ 🌤 No appointments today — Next: tomorrow 10:00     ]   ← TodayAgenda empty (positive)
[ Coming-up peek (ELEVATED)                          ]   ← priority raised
[ nudges (optional)                                  ]
                 ( ⊕ )
```

**Overlay — offline (any maturity):** `OfflineBanner` pins at rail top; agenda shows cached data + "last updated"; mutating actions show queued affordance. **Overlay — vacation:** `VacationBanner` + `BusinessAlerts` elevated for bookings inside the window; agenda muted. **Total error:** all zones fail + no cache → orchestrator renders a single centered error+retry (the only case the composition yields to a full-screen treatment).

### D6 — Banner-rail precedence

Concurrent banners order by severity: **Error → Offline → Vacation → Closed-today → Pending-verification → Maturity-nudge**. At most the top two render expanded; the rest collapse into one tappable "more" chip. Rationale: Error/Offline undermine trust in *all* content, so they sit above lifecycle context; a maturity nudge is never allowed to outrank a real system/lifecycle condition.

### D7 — Refresh & data strategy

**MVP decision: polling + pull-to-refresh, architected push-ready.** Each widget declares its own strategy (D3) so the Home has no global polling loop:
- T0/T1 operational widgets (`ActionQueue`, `NowNext`, `TodayAgenda`, `BusinessAlerts`) use **`poll`** on a modest interval while the Home is foregrounded, plus **`manual`** pull-to-refresh.
- **`cached`** for maturity/peek/nudge widgets (cheap, low-volatility).
- **Push-ready:** `refreshStrategy` is a declared field per widget, so replacing `poll` with `live` (push/stream) later is a per-widget swap behind the same contract — **no Home redesign, no widget-API change**. When push lands, a widget flips `poll → live` and the polling timer is simply not scheduled.
- Offline: widgets read last-cached data and tag staleness; mutations enqueue and flush on reconnect (`ActionQueue`/`NowNext`/`TodayAgenda`).
Pull-to-refresh is the one global gesture the orchestrator owns; it fans out to each visible widget's source.

### D8 — Visual language (Coliride tokens, by role)

Referenced at the semantic level (values live in Coliride tokens / `app_tokens.dart`): spacing scale for zone gaps; card surface + radius + elevation for each widget container; semantic color roles — `info` (pending), `neutral/muted` (offline), `warning` (vacation/closed/conflict), `danger` (error/decline), `success` (confirm/end-of-day); type scale for greeting/section titles/rows; chip, banner, and list-row components; motion tokens for the success micro-celebrations and collapse/expand. No business flows are copied — only the visual/interaction vocabulary.

## Risks / Trade-offs

- **[Registry/priority drift]** Ad-hoc priorities could make ordering unpredictable as widgets grow → Mitigation: priorities are named constants in one registry file with gaps (10,20,30…) for insertion; ordering is snapshot-tested against `HomeContext` fixtures.
- **[Polling fallback if no push]** Without push infra, `live` degrades to polling — battery/network cost and staleness → Mitigation: poll only while foregrounded, coalesce widget refreshes, prefer push when it lands (Open Question).
- **[Maturity misclassification]** Wrong thresholds could strand an active provider in a Growth layout, or nag an established one → Mitigation: thresholds are config, not hardcoded; classification is pure and unit-tested; conservative default (first confirmed booking exits Growth).
- **[Cross-widget consistency]** Independent widgets could drift in look/spacing → Mitigation: all share the Coliride card/banner/row components and the same four-state (loading/empty/error/offline) treatment recipe.
- **[Infinite-width button footgun]** (repo memory) themed buttons expand to full width in a `Row` → Mitigation: flagged for the screen-design phase; widget action rows must be tested with the real theme, not defaults.
- **[Backend gaps]** provider-scoped client list, push, and reviews are not guaranteed → Mitigation: widgets depending on them (`SetupNudges` review prompt) degrade gracefully / stay hidden until the source exists; none block the core Home.

## Migration Plan

This is a design artifact; "migration" = the build-out sequence it unlocks (each a later change):
1. Approve this design.
2. Screen-design each widget one-by-one (starting with the Setup first-login and Operational active-day compositions, per the IA).
3. Implement the `HomeContext` resolver + `HomeWidget` contract + registry + orchestrator (thin), behind the existing `/dashboard` route.
4. Implement widgets incrementally; each registers into the registry and is independently testable — Home works with any subset present.
5. Replace `ProviderDashboardPage` once the core widget set (BannerRail, ActionQueue, NowNext, TodayAgenda, ComingUpPeek, ActivationChecklist/GetDiscovered, CreateAction) reaches parity.
- **Rollback:** the placeholder page remains available behind a flag until parity is confirmed; the router change is trivially reversible.

## Resolved Decisions

The six original open questions are resolved as follows and are reflected in the decisions above:

1. **Booking model — HYBRID (provider-selectable).** Providers choose Instant Booking or Request & Approval; `bookingMode` is a `HomeContext` input (D1). In **Request** mode the `ActionQueue` is the highest-priority operational widget; in **Instant** mode `NowNext` + `TodayAgenda` are primary and the queue holds exceptions only (D3 widgets 3–4; spec: *Booking-Mode Adaptation*). The Home re-composes automatically on mode change.
2. **Refresh — polling + pull-to-refresh for MVP, architected push-ready.** `refreshStrategy` is a per-widget declared field, so push replaces polling later as a per-widget `poll → live` swap with no Home redesign (D7).
3. **Maturity thresholds — configurable, never hardcoded.** Supplied by backend/remote config and injected into `classifyMaturity` at runtime (D1, D4); tuning requires no app release.
4. **Provider clients — derived from completed bookings initially.** The client source is abstracted behind a repository so a future dedicated endpoint swaps in without UI changes; no Home widget hard-depends on a client-list endpoint.
5. **Reviews — out of MVP scope.** No review widgets/actions are surfaced. `SetupNudges`' nudge-source remains extensible so a review nudge can be added later without changing the widget (D3 widget 9).
6. **Vacation & closed days — backend-managed availability.** The Home *consumes* the backend-provided availability state and does **not** compute vacation/closed locally; `VacationBanner`/`ClosedTodayBanner`/`BusinessAlerts` read it directly (D1, D3 widget 10).

## Open Questions

- None outstanding. Remaining unknowns (exact remote-config threshold values; the backend availability payload shape) are implementation-time details for the follow-on build change, not design decisions.
