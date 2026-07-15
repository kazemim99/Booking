# Booksy Provider App — Home ("Today" Workspace) Design

> **Status:** Design specification for review. Consolidates the OpenSpec change `design-provider-home-workspace` (proposal · specs · design · tasks) with the approved [Provider App Information Architecture](PROVIDER_APP_INFORMATION_ARCHITECTURE.md) into one reviewable document.
>
> **Scope:** the Home ("Today") screen only — its states, its adaptive composition, and its widget architecture. **Not** in scope: Flutter code, high-fidelity mockups, or the other tabs (Calendar, Clients, More).
>
> **Platform:** Flutter, mobile-first, **Persian (fa-IR), RTL**. **Visual reference:** Coliride design language (tokens, spacing, typography, components, interaction/motion) for look-and-feel only — no business flows copied.
>
> **Authoritative sources:** requirements live in `openspec/changes/design-provider-home-workspace/specs/provider-home-workspace/spec.md`; architecture rationale in that change's `design.md`. This doc is the readable synthesis.

---

## 1. The governing principle

> **A provider opens this app to _run their day_, not to _read a report_.**

The Home answers **"What do I need to do right now?"** — never "how much did I earn last month?". It is an **operational workspace**, not a dashboard and not a static welcome screen. Analytics lives two taps away in *Insights (More)*, by design.

The Home is **not one screen**. A day-one pending business, a growing business with no traction, and a fully-booked established salon each need a different top-of-screen. So the Home is **assembled at runtime** from independent widgets, ordered by the provider's current situation — it is *adaptive, not static*.

---

## 2. How the Home is built (three ideas)

The whole design rests on three mechanisms. Everything else follows from them.

1. **A state-resolution engine** turns raw inputs into one immutable `HomeContext` (§4).
2. **A business-maturity model** classifies the business and shifts emphasis as it grows (§5).
3. **A widget-composition architecture** renders the Home as independent widgets ordered by the context — so widgets can be added, removed, reordered, or replaced without redesigning the screen (§6–§7).

The Home page itself is a **thin orchestrator**: it holds no business logic and no per-state layout. It reads `HomeContext`, asks a registry which widgets apply, and lays them out by priority.

---

## 3. Resolved product decisions

These six decisions are settled and baked into the design.

| # | Decision | Consequence |
|---|----------|-------------|
| 1 | **Hybrid booking model** — provider chooses *Instant* or *Request & Approval* | Home adapts by `bookingMode`: **Request** → Pending Requests is the top operational widget; **Instant** → Today's schedule + Next Appointment lead; the queue then holds exceptions only |
| 2 | **Polling + pull-to-refresh for MVP, architected push-ready** | Each widget declares a `refreshStrategy`; swapping `poll → live` (push) later is a per-widget change, no Home redesign |
| 3 | **Configurable maturity thresholds** | Thresholds come from backend/remote config; tuning maturity needs no app release |
| 4 | **Clients derived from completed bookings initially** | Client source is repository-abstracted; a future endpoint swaps in without UI change |
| 5 | **Reviews out of MVP** | No review widgets/actions surfaced; nudge source stays extensible |
| 6 | **Vacation & closed days are backend-managed availability** | Home *consumes* availability state; it never computes vacation/closed locally |

---

## 4. The state model — one `HomeContext`, three layers

A single pure resolver computes an immutable `HomeContext` from raw inputs, in strict precedence order. The Home renders **only** from this object; widgets never re-derive global state.

```
resolveHomeContext(inputs) -> HomeContext:

  # Layer 0 — System (highest precedence)
  system = ERROR    if data load failed AND no cache
         = OFFLINE   if no connectivity            (cached data still surfaced)
         = LOADING   if first load in flight
         = OK        otherwise

  # Layer 1 — Lifecycle, Config & Availability   (router already excludes Blocked)
  lifecycle    = PENDING_VERIFICATION if providerStatus == PendingVerification
  bookingMode  = INSTANT | REQUEST           # provider config (hybrid)
  availability = backend-provided            # VACATION | CLOSED_TODAY | OPEN
                                             # consumed, never computed locally

  # Layer 2 — Maturity & Day-Context
  maturity = classifyMaturity(signals, remoteConfigThresholds)     # §5
  day = FULLY_BOOKED  if open capacity today == 0 and appts today > 0
      = ACTIVE        if appts today > 0
      = NO_APPTS      if appts today == 0        # meaningful only when OPEN

  banners = orderBySeverity([error?, offline?, vacation?, closed?, pending?, nudge?])
  return HomeContext(system, lifecycle, bookingMode, availability, maturity, day, banners, data…)
```

**Why a single resolver and not per-widget logic?** Because the states are **orthogonal** — an Operational provider can be simultaneously *No-Appointments-Today* **and** *Offline* **and** in *Instant* mode. Enumerating that cross-product as named "screen modes" explodes combinatorially and produces exactly the brittle static layout we are avoiding. One resolver + independent widget visibility rules handles any combination and is trivially unit-testable (input fixtures → expected context).

**Banner precedence** when several conditions co-occur: **Error → Offline → Vacation → Closed-today → Pending-verification → Maturity-nudge.** At most the top two show expanded; the rest collapse into a "more" chip. Rationale: system conditions (error/offline) undermine trust in *all* content, so they outrank lifecycle context; a maturity nudge never outranks a real condition.

---

## 5. Business-maturity adaptation

The Home evolves with the business. `classifyMaturity` is a **pure function of observable signals** (never a manual toggle); its thresholds are **remote-config values**.

**Signals:** provider status · profile-completeness (staff, gallery, priced services) · total bookings all-time · trailing-30-day bookings.

| Phase | Shape of the rule (thresholds via remote config) | Dominant widget | What recedes |
|-------|--------------------------------------------------|-----------------|--------------|
| **Setup** | just onboarded / pending **and** 0 bookings **and** profile incomplete | `ActivationChecklist` (hero) | agenda muted |
| **Growth** | verified/active **and** 0 bookings | `GetDiscovered` (hero) | agenda muted |
| **Traction** | ≥1 booking **and** trailing-30d < `T` | agenda when present; growth/nudges when idle | scaffold → nudges |
| **Operational** | trailing-30d ≥ `T` (sustained) | `ActionQueue` / `NowNext` / agenda | nudges occasional |

The **see-saw** — scaffold priority rises as maturity falls, agenda priority rises as maturity climbs — is implemented purely through each widget's *visibility rule* + *priority*. **No maturity phase needs a bespoke layout.**

Maturity is **orthogonal to day-context**: an Operational provider on a quiet day still hits *No-Appointments-Today*, handled by the agenda's empty-state (not by the scaffold reappearing).

---

## 6. Widget-composition architecture

The Home is a **composition of reusable, independent zone widgets**, not a monolithic screen.

### 6.1 The widget contract (`HomeWidget`)

Every zone widget conforms to one contract:

| Field | Meaning |
|-------|---------|
| `id` | stable identifier (ordering, testing, telemetry) |
| `purpose` | single responsibility — one job per widget |
| `inputs` | declared data dependencies (repo/stream/`HomeContext` fields it reads) |
| `outputs` | events it emits **upward** (navigation intents, mutation requests) — the widget never navigates or mutates global state itself |
| `visibilityRule(ctx)` | pure predicate — the widget declares *when* it applies |
| `priority` | ordinal placing it on screen (lower = higher) |
| `loading / empty / error / offline` | its **own** four state treatments, rendered within its bounds |
| `refreshStrategy` | `live` · `poll(interval)` · `manual` · `cached` |
| `collapsible` | expand/collapse support + default state |

### 6.2 The orchestrator (the Home page)

Its entire job: subscribe to `HomeContext` → ask the registry for the widgets whose `visibilityRule` passes → lay them out top-to-bottom by `priority` (RTL) → route each widget's `outputs` to navigation/mutation. **No zone business logic. No per-state layout code.**

### 6.3 The registry

A declarative list of widget descriptors (`id` + `priority` + factory + visibility). **Add / remove / reorder / replace = edit the registry, not the page.** Priorities are named constants with gaps (10, 20, 30…) so widgets can be inserted without renumbering, and ordering is snapshot-tested against `HomeContext` fixtures.

### 6.4 Independence guarantees

- Each widget owns its data subscription → one widget loading, erroring, or offline shows *its* treatment in *its* bounds while siblings keep working.
- A hidden widget is omitted and its space reclaimed — no empty placeholders.
- **Rejected alternative:** a single `HomeBloc` emitting one monolithic view-model — it couples all zones into one failure/loading domain (one slow call blocks the whole Home) and forces every new zone to edit a god-bloc. The registry keeps blast radius to one widget.

---

## 7. The widget catalog

Twelve widgets. `Ctx` = `HomeContext`; priority = on-screen order (1 = top, under the app bar). Outputs are intents the orchestrator routes.

| # | Widget | Purpose | Visibility | Priority | Refresh |
|---|--------|---------|-----------|----------|---------|
| 1 | **HomeAppBar** | identity + account & notifications entry | always | 0 (fixed) | live (status/badge) |
| 2 | **StatusBannerRail** | system/lifecycle banners in severity order | `Ctx.banners` non-empty | 1 | reactive to Ctx |
| 3 | **ActionQueue** | items needing immediate action | `OK && OPEN && queue non-empty` | **2 (Request) / demoted (Instant)** | poll → push-ready |
| 4 | **NowNext** | current/next appointment as actionable hero | `OPEN && day∈{ACTIVE,FULLY_BOOKED} && next exists` | 3 (Request) / **2 (Instant)** | poll → push-ready |
| 5 | **TodayAgenda** | today's timeline | `availability==OPEN` | 4 | poll + pull-to-refresh |
| 6 | **ComingUpPeek** | glance at next / next-open day | `OK` (elevated when today empty/closed/vacation) | 6 (→3.5 elevated) | cached |
| 7 | **ActivationChecklist** | first-value setup guide | `maturity==SETUP` | 2 (hero) | reactive to completeness |
| 8 | **GetDiscovered** | drive first bookings (share link + completeness) | `maturity==GROWTH` | 2 (hero) | cached |
| 9 | **SetupNudges** | occasional housekeeping prompts | `maturity∈{TRACTION,OPERATIONAL} && nudges` | 8 | cached |
| 10 | **BusinessAlerts** | anomalies needing resolution | anomalies exist (elevated in Vacation/Closed) | 3 (elevated) | poll → push-ready |
| 11 | **EndOfDaySummary** | positive closure when day's work done | `day∈{ACTIVE,FULLY_BOOKED} && all completed` | 3 (replaces NowNext) | reactive |
| 12 | **CreateAction** | primary create (New appointment / Block time) | `system != ERROR` | fixed (nav center) | n/a |

**Booking-mode sensitivity (widgets 3 & 4).** In **Request** mode, `ActionQueue` holds routine unconfirmed requests and is priority 2 (loudest). In **Instant** mode, routine bookings never enter the queue — `NowNext` + `TodayAgenda` lead (queue demotes below them and surfaces only conflicts / client-reschedule requests). The mode change re-composes the Home automatically.

**MVP notes.** `SetupNudges` excludes review nudges (reviews out of MVP), source kept extensible. `BusinessAlerts` reads the backend availability state. No widget hard-depends on a provider client-list endpoint.

Each widget's four state treatments (loading / empty / error / offline) are specified per-widget in the change's `design.md` §D3; they share one visual recipe (§10) for consistency.

---

## 8. Every Home state (the catalog)

Each state below gives **user goal · information priority · available actions · empty · success · error · visual hierarchy.** These map 1:1 to the requirements in the capability spec. States compose (a state is a resolved `HomeContext`, not a mutually-exclusive mode).

### 8.1 First Login After Onboarding *(maturity: Setup)*
- **Goal:** understand what to do next, feel setup succeeded, take the first step toward bookings.
- **Info priority:** verification reassurance → activation checklist → how to get the first booking → empty agenda.
- **Actions:** add team · add photos · review services & prices · share booking link · add first appointment · preview public profile.
- **Empty:** inviting empty agenda ("no appointments yet — add one / share your link").
- **Success:** per-item completion confirmation; when verification approves → banner flips positive.
- **Error:** checklist item save fails → inline retry; other items stay interactive.
- **Hierarchy:** pending banner → **ActivationChecklist (hero)** → coming-up peek → muted empty agenda. Reuses the standard Home structure so the layout is learned once and *transforms* as bookings arrive (no throwaway success screen).

### 8.2 Empty Business *(maturity: Growth)*
- **Goal:** attract the first clients; make the business attractive/discoverable.
- **Info priority:** get-discovered (share link + completeness) → remaining setup → immediate app value (add walk-in) → empty agenda.
- **Actions:** share booking link · complete profile · add walk-in · (invite clients).
- **Empty:** completeness meter + empty agenda + "0 clients yet" hint.
- **Success:** first booking received → Home advances toward Traction; completeness → 100%.
- **Error:** link generation fails → explain corrective step (complete profile / await verification).
- **Hierarchy:** **GetDiscovered (hero)** → add-walk-in → muted agenda.

### 8.3 No Appointments Today *(established, quiet day)*
- **Goal:** confirm nothing missed; fill the day or use the quiet time.
- **Info priority:** calm "all clear today" → fill opportunities (open slots, walk-in) → coming-up (next appt) → light admin.
- **Actions:** add appointment · block time · view calendar/next day.
- **Empty:** the defining state — positive framing + next-appointment hint ("Next: tomorrow 10:00"), never a bare "nothing here".
- **Success:** adding an appointment replaces the empty treatment with the timeline.
- **Error:** load fails → localized retry.
- **Hierarchy:** calm empty confirmation central → **ComingUpPeek elevated** (tomorrow matters more today) → optional nudges.

### 8.4 Pending Verification *(lifecycle overlay)*
- **Goal:** know the business is under review, that it's temporary, and what can be done meanwhile.
- **Info priority:** status banner → what you CAN do now (setup, manual bookings) → what happens next.
- **Actions:** continue setup · add manual booking · contact support · preview public profile.
- **Empty:** combines with first-login empty agenda.
- **Success:** approval → banner flips to "your business is live" + nudge to share link.
- **Error:** status refresh fails → keep last known status + retry.
- **Hierarchy:** informative (not alarming) banner at the top of the rail; the rest follows maturity/day beneath. **A banner, not a gate** — a freshly-onboarded provider keeps momentum instead of hitting a waiting wall.

### 8.5 Active Business Day *(day: ACTIVE)*
- **Goal:** run the day; never miss an appointment or request.
- **Info priority:** action queue (mode-dependent) → now/next → today timeline → coming-up.
- **Actions:** confirm/decline · call/message · complete · no-show · reschedule · reassign staff · add walk-in · block time.
- **Empty:** queue empty → collapses (optional "all caught up").
- **Success:** queue cleared → "all caught up"; appointment completed → row check + progress ("3 of 7"); all done → end-of-day summary.
- **Error:** inline action fails → optimistic rollback + per-item retry; partial load degrades gracefully.
- **Hierarchy:** *Request mode* → **ActionQueue loudest** → NowNext → timeline. *Instant mode* → **NowNext primary** → timeline → queue (exceptions only). Nudges suppressed during an active day.

### 8.6 Fully Booked Day *(day: FULLY_BOOKED)*
- **Goal:** execute a packed day, protect breaks, avoid overbooking, handle overflow.
- **Info priority:** "fully booked" acknowledgment → now/next → dense timeline with clear breaks → overflow to waitlist/decline → conflict warnings.
- **Actions:** operational actions + manage waitlist · protect/adjust breaks · decline-with-alternative.
- **Empty:** no open slots → positive "you're fully booked".
- **Success:** full day completed → strong end-of-day success.
- **Error:** overbook attempt → conflict warning before explicit override.
- **Hierarchy:** positive "fully booked" chip near top → dense timeline is hero → new requests route to waitlist; `CreateAction` warns "no free slots".

### 8.7 Closed Business Today *(availability: CLOSED_TODAY)*
- **Goal:** confirm it's a day off; verify nothing leaked onto it; plan the next open day.
- **Info priority:** "closed today" + next open day → any anomalous booking on the closed day → coming-up → admin time.
- **Actions:** view next open day · open exceptionally · edit hours · add appointment (with a closed-day warning).
- **Empty:** "Closed today. Next open: Saturday" — calm.
- **Success:** n/a.
- **Error:** load fails → localized retry.
- **Hierarchy:** calm closed confirmation central → **next-open peek elevated** → muted agenda. (Availability is backend-provided.)

### 8.8 Offline *(system overlay)*
- **Goal:** know they're offline; still read cached info; know what's queued vs blocked.
- **Info priority:** offline banner → cached content with staleness indicator → which actions are unavailable/queued.
- **Actions:** read cached data; mutating actions queue optimistically (sync later) or disable with a reason; retry/refresh.
- **Empty:** no cache → "can't load — you're offline" + retry.
- **Success:** reconnect → auto-refresh + "back online, synced"; queued actions flush.
- **Error:** action attempted offline → "will sync when back online" or blocked with reason.
- **Hierarchy:** neutral offline banner pinned top → content tagged stale → mutating controls show offline affordance.

### 8.9 Error *(system state)*
- **Goal:** understand something failed; recover via retry without losing context; reach support if persistent.
- **Info priority:** human-readable message (no codes) → retry → any partial content that loaded → support path if repeated.
- **Actions:** retry · refresh · contact support; use cache if available.
- **Empty:** full-screen error only when nothing loaded; otherwise inline per-zone error.
- **Success:** retry succeeds → error clears, content restored.
- **Error handling:** distinguish transient vs persistent — after N retries, escalate to "contact support".
- **Hierarchy:** **prefer partial degradation** — the failed zone shows a localized error while the rest stays operational; only a total failure with no cache yields a centered full-screen error+retry.

### 8.10 Vacation Mode *(availability: VACATION)*
- **Goal:** confirm vacation is active and clients can't book; know when it ends; handle bookings already in the window; resume easily.
- **Info priority:** vacation banner (dates + "not accepting bookings") → bookings inside the window needing action → countdown/next steps → coming-up after vacation.
- **Actions:** edit/extend/end vacation · reschedule/cancel/notify affected clients · add appointment anyway (override) · preview what clients see.
- **Empty:** vacation-day agenda → "You're on vacation until X".
- **Success:** vacation ends → "welcome back — accepting bookings again"; setting vacation → confirmation.
- **Error:** setting vacation fails / affected-bookings load fails → retry.
- **Hierarchy:** distinct calm vacation banner top → **BusinessAlerts elevated** (affected bookings need action) → muted agenda → resume info. (Vacation is a backend-managed availability concept the Home consumes.)

---

## 9. Composed layouts (low-fi, RTL)

Wireframes show *priority order top→bottom*; the real layout is RTL (avatar / first-read on the **right**). `▸` = collapsible.

**Setup — first login, pending verification:**
```
[ AppBar: avatar · صبح بخیر، سالن… · 🔔 ]
[ ⚠ Pending-verification banner                     ]  ← StatusBannerRail
[ ✅ ACTIVATION CHECKLIST  (HERO)                    ]  ← ActivationChecklist
      ☑ services   ☐ team   ☐ photos   ☐ share link
[ Coming-up peek (elevated: nothing yet)            ]
[ ·· empty agenda: "no appointments — add one" ··   ]  ← TodayAgenda (muted)
                 ( ⊕ center create )
```

**Growth — verified, no traction:**
```
[ AppBar ]
[ 📣 GET DISCOVERED (HERO): share link · profile 70% ]  ← GetDiscovered
[ + Add walk-in appointment                          ]
[ ·· empty agenda (muted) ··                          ]
                 ( ⊕ )
```

**Operational — active day, REQUEST mode:**
```
[ AppBar: 🔔•2 ]
[ 🔴 ACTION QUEUE: 2 requests  [confirm][decline]    ]  ← ActionQueue (loudest)
[ ★ NOW/NEXT: 10:00 Sara — [call][complete][resched] ]  ← NowNext
[ TODAY AGENDA (timeline)                           ▸ ]  ← TodayAgenda
[ Coming-up peek: tomorrow 6 appts                   ]
                 ( ⊕ )
```

**Operational — active day, INSTANT mode:**
```
[ AppBar ]
[ ★ NOW/NEXT: 10:00 Sara — [call][complete][resched] ]  ← NowNext (primary)
[ TODAY AGENDA (timeline)                           ▸ ]  ← TodayAgenda
[ (ActionQueue only if a conflict/exception exists)  ]  ← demoted
[ Coming-up peek                                     ]
                 ( ⊕ )
```

**Operational — fully booked:**
```
[ AppBar ]
[ ✔ Fully booked — 12 appointments today            ]  ← chip in NowNext/agenda header
[ ★ NOW/NEXT                                         ]
[ TODAY AGENDA (dense, breaks distinct)            ▸ ]
[ (new request → waitlist via ActionQueue)           ]
                 ( ⊕ warns: no free slots )
```

**Operational — no appointments today:**
```
[ AppBar ]
[ 🌤 No appointments today — Next: tomorrow 10:00     ]  ← TodayAgenda empty (positive)
[ Coming-up peek (ELEVATED)                          ]
[ nudges (optional)                                  ]
                 ( ⊕ )
```

**Overlays.** *Offline:* OfflineBanner pins at rail top; agenda shows cached data + "last updated"; mutating actions show a queued affordance. *Vacation:* VacationBanner + BusinessAlerts elevated for in-window bookings; agenda muted. *Total error:* all zones fail + no cache → single centered error+retry (the only full-screen fallback).

---

## 10. Refresh, visual language, RTL & accessibility

**Refresh (MVP: polling + pull-to-refresh, push-ready).** T0/T1 operational widgets `poll` while foregrounded + `manual` pull-to-refresh; maturity/peek/nudge widgets are `cached`. Because `refreshStrategy` is a declared per-widget field, replacing `poll` with `live` when push lands is a per-widget swap — no Home redesign. Pull-to-refresh is the one global gesture the orchestrator owns; it fans out to each visible widget's source. Offline: widgets read cache + tag staleness; mutations enqueue and flush on reconnect.

**Visual language (Coliride tokens, by role).** Spacing scale for zone gaps; card surface + radius + elevation per widget; semantic color roles — `info` (pending), `neutral/muted` (offline), `warning` (vacation/closed/conflict), `danger` (error/decline), `success` (confirm/end-of-day); type scale for greeting/section titles/rows; chip / banner / list-row components; motion tokens for success micro-celebrations and collapse/expand. Values live in `app_tokens.dart`. Only the visual/interaction vocabulary is reused — no business flows.

**RTL & accessibility (first-class).** Authored RTL-native for fa-IR (reading order, zone/banner/badge placement, back-gesture direction). Minimum touch targets; semantic labels; no overflow at 1.3× font scale; success micro-celebrations respect reduced-motion. All information is conveyed without relying on motion.

> **Implementation footgun (repo memory):** themed buttons are infinite-width (`Size.fromHeight`) and blow up inside a `Row` — widget action rows must be tested with the **real theme**, not defaults.

---

## 11. Backend mapping & dependencies

Every core surface maps to a shipped endpoint (`API_ENDPOINTS.md`):

| Home surface | Backend |
|--------------|---------|
| Agenda / NowNext / queue | `GET /Bookings/provider/{id}`, `/Bookings/search`, `/Bookings/available-slots` |
| Confirm / decline / complete / no-show / reschedule / assign-staff / notes | `/Bookings/{id}/…` |
| New appointment | `POST /Bookings` |
| EndOfDay / stats signals | Booking Statistics endpoints |
| Maturity signals · status banner | `/Providers/current/status`, statistics, JWT `provider_status` |
| Activation targets (staff/gallery/services/profile) | `/Providers/{id}/staff`, `/gallery`, services, `/profile` `/business` |

**Dependencies (non-blocking, degrade gracefully):** availability payload for vacation/closed (backend-managed); optional push infrastructure (else polling); client list derived from completed bookings until a dedicated endpoint exists.

---

## 12. Build sequence (what happens after this design is approved)

Each step is a separate change; **no Flutter is written under this design change.**

1. Approve this design.
2. Screen-design widgets one-by-one, starting with **Setup first-login** and **Operational active-day** compositions → see **[PROVIDER_HOME_SCREEN_DESIGNS.md](PROVIDER_HOME_SCREEN_DESIGNS.md)** (anchor set done).
3. Implement the thin core: `HomeContext` resolver + `HomeWidget` contract + registry + orchestrator, behind the existing `/dashboard` route → precise pseudocode + fixture matrix in **[PROVIDER_HOME_RESOLVER_SPEC.md](PROVIDER_HOME_RESOLVER_SPEC.md)**.
4. Implement widgets incrementally; each registers into the registry and is independently testable — the Home works with any subset present.
5. Replace `ProviderDashboardPage` once the core widget set (BannerRail, ActionQueue, NowNext, TodayAgenda, ComingUpPeek, ActivationChecklist/GetDiscovered, CreateAction) reaches parity. Placeholder stays behind a flag until then (trivial rollback).

**Testing obligations (per repo Testing Policy):** resolver + `classifyMaturity` unit tests; a `HomeContext` fixture matrix incl. orthogonal combinations (e.g. Operational + NoAppts + Offline; Growth + PendingVerification; Instant vs Request); per-widget widget tests covering all four state treatments; orchestrator ordering snapshot tests; RTL + accessibility widget tests.

---

*This document is a living design reference. The normative requirements are in the OpenSpec capability spec (`provider-home-workspace`); when they diverge, the spec wins and this synthesis is updated.*
