# Provider Home — Resolver & Composition Spec

> **Status:** Implementation-ready design spec (language-neutral pseudocode). Makes the [Home design](PROVIDER_HOME_TODAY_DESIGN.md) precise enough to build and test without ambiguity. Consumed by the follow-on build change.
>
> **Covers:** the `HomeContext` value object, the `resolveHomeContext` algorithm, `classifyMaturity`, the banner-ordering function, the **widget registry** (visibility + priority as pure expressions over `HomeContext`), and a **fixture matrix** (inputs → expected context → expected ordered widgets) that is the basis for resolver unit tests and orchestrator ordering snapshot tests.
>
> **Authority:** where this diverges from an illustrative composition list in the screen-design doc, **this resolver is authoritative** (priority numbers, not hand-drawn order, decide layout).

---

## 1. Enums

```
SystemState   = OK | LOADING | OFFLINE | ERROR
BookingMode   = INSTANT | REQUEST
Availability  = OPEN | CLOSED_TODAY | VACATION
Maturity      = SETUP | GROWTH | TRACTION | OPERATIONAL
DayContext    = NONE | NO_APPTS | ACTIVE | FULLY_BOOKED     # NONE unless OPEN
BannerKind    = ERROR | OFFLINE | VACATION | CLOSED_TODAY | PENDING | NUDGE
```

> `ProviderStatus.{Suspended,Inactive,Archived}` (blocked) never reach Home — the router gates them. `Drafted` routes to onboarding. So the resolver only ever sees a provider allowed onto Home.

---

## 2. Inputs (`HomeInputs`)

What the resolver consumes. Sources in parentheses.

```
HomeInputs {
  connectivity      : ONLINE | OFFLINE                     (connectivity service)
  loadStatus        : LOADING | LOADED | FAILED            (data layer aggregate)
  hasCachedData     : bool                                 (cache)

  providerStatus    : ProviderStatus                       (JWT / /Providers/current/status)
  bookingMode       : BookingMode                          (provider config)
  availabilityState : OPEN | CLOSED_TODAY | VACATION        (backend availability — consumed, not computed)
  vacationEndDate?  : JalaliDate                           (backend, when VACATION)
  nextOpenDay?      : Date                                 (backend, when CLOSED_TODAY)

  signals {                                                (booking statistics + profile)
    profileComplete       : bool                           (see §4.1)
    totalBookingsAllTime  : int
    bookingsTrailing30d   : int
  }

  today {
    appointments   : List<Appt>                            (/Bookings/provider/{id})
    openCapacity   : int                                   (/Bookings/available-slots)
    allCompleted   : bool                                  (derived)
    pendingReqs    : List<Request>                         (request-mode inbound)
    exceptions     : List<Conflict>                        (conflicts / reschedule reqs)
    alerts         : List<Alert>                           (bookings in vacation window, on closed day, conflicts)
  }

  thresholds {                                             (backend / remote config — never hardcoded)
    operationalMinTrailing30d : int                        ("T")
    galleryMin                : int                        ("N")
    // profileComplete definition (which fields count) is also config-driven
  }
}
```

---

## 3. `resolveHomeContext` — the algorithm

Pure function. No I/O, no clock reads inside (time-derived inputs like "greeting" are passed in). Deterministic → same inputs always yield the same context.

```
resolveHomeContext(in: HomeInputs) -> HomeContext {

  # ---- Layer 0: System (highest precedence) ----
  system =
      ERROR    if in.loadStatus == FAILED and not in.hasCachedData
    ; OFFLINE  if in.connectivity == OFFLINE               # cached data still surfaced
    ; LOADING  if in.loadStatus == LOADING and not in.hasCachedData
    ; OK       otherwise

  # ---- Layer 1: Lifecycle · Config · Availability ----
  pendingVerification = (in.providerStatus == PendingVerification)
  bookingMode         = in.bookingMode
  availability        = in.availabilityState               # consumed as-is

  # ---- Layer 2: Maturity · Day-Context ----
  maturity = classifyMaturity(in.signals, in.thresholds)    # §4

  day =
      NONE          if availability != OPEN
    ; FULLY_BOOKED  if in.today.openCapacity == 0 and in.today.appointments.isNotEmpty
    ; ACTIVE        if in.today.appointments.isNotEmpty
    ; NO_APPTS      otherwise

  banners = orderBanners(system, availability, pendingVerification, maturity, in)   # §5

  return HomeContext {
    system, pendingVerification, bookingMode, availability, maturity, day, banners,
    # carried view-data (widgets read from here; they do not re-fetch global state):
    nextAppt        : firstUpcoming(in.today.appointments),
    todayAppts      : in.today.appointments,
    allCompleted    : in.today.allCompleted,
    pendingReqs     : in.today.pendingReqs,
    exceptions      : in.today.exceptions,
    alerts          : in.today.alerts,
    comingUp        : summarize(in.today, in.nextOpenDay),
    completeness    : profileCompletenessPct(in.signals, in.thresholds),
    vacationEndDate : in.vacationEndDate,
    nextOpenDay     : in.nextOpenDay,
    isStale         : (system == OFFLINE) or (in.loadStatus == FAILED and in.hasCachedData),
  }
}
```

---

## 4. `classifyMaturity`

```
classifyMaturity(s: Signals, t: Thresholds) -> Maturity {
  if s.totalBookingsAllTime == 0:
      return s.profileComplete ? GROWTH : SETUP
  if s.bookingsTrailing30d >= t.operationalMinTrailing30d:
      return OPERATIONAL
  return TRACTION
}
```

**Clarification of design D4 (deliberate):** maturity is driven **only by traction + profile-completeness signals** — it does **not** read `providerStatus`. Verification is the *orthogonal* lifecycle layer (a `PENDING` banner), so:
- A pending provider with a complete profile and 0 bookings resolves to **GROWTH** *and* shows a pending banner (both true at once — orthogonality working).
- A verified provider with an incomplete profile and 0 bookings resolves to **SETUP** (they still need to finish setup).

This is cleaner than tying SETUP to `PendingVerification` and avoids a provider being stuck in a scaffold layout purely because verification is slow.

### 4.1 `profileComplete` (config-driven)

```
profileComplete(s, t) = staffCount >= 1
                      and galleryCount >= t.galleryMin
                      and allServicesPriced
# which fields count is remote-config; the shape above is the default.

profileCompletenessPct(s, t) = round(100 * satisfied(criteria) / total(criteria))
```

---

## 5. `orderBanners`

Severity order (fixed). Returns an ordered list; the `StatusBannerRail` renders the **top 2 expanded**, the rest collapsed into a "more" chip.

```
orderBanners(system, availability, pending, maturity, in) -> [BannerKind] {
  out = []
  if system == ERROR:                     out += ERROR         # only when a zone total-fails with cache
  if system == OFFLINE:                   out += OFFLINE
  if availability == VACATION:            out += VACATION
  if availability == CLOSED_TODAY:        out += CLOSED_TODAY
  if pending:                             out += PENDING
  if maturity in {TRACTION,OPERATIONAL}
     and hasNudge(in):                    out += NUDGE          # low; often collapsed
  return out
}
```
Rationale: system conditions (error/offline) undermine trust in all content → above lifecycle context; a maturity nudge never outranks a real condition.

---

## 6. Widget registry — visibility & priority

Each widget declares a **pure visibility predicate** and a **priority** (lower = higher on screen; named constants with gaps so widgets insert without renumbering). The orchestrator renders `registry.where(w => w.visible(ctx)).sortBy(w => w.priority(ctx))`.

| Widget | `visible(ctx)` | `priority(ctx)` |
|--------|----------------|-----------------|
| `HomeAppBar` | always (chrome, outside scroll) | — |
| `StatusBannerRail` | `ctx.banners.isNotEmpty` | `10` |
| `ActivationChecklist` | `ctx.maturity == SETUP` | `20` |
| `GetDiscovered` | `ctx.maturity == GROWTH` | `20` |
| `NowNext` | `ctx.availability==OPEN && ctx.day∈{ACTIVE,FULLY_BOOKED} && ctx.nextAppt != null && !ctx.allCompleted` | `INSTANT ? 25 : 40` |
| `EndOfDaySummary` | `ctx.availability==OPEN && ctx.day∈{ACTIVE,FULLY_BOOKED} && ctx.allCompleted` | `INSTANT ? 25 : 40` |
| `BusinessAlerts` | `ctx.alerts.isNotEmpty` | `28` (elevated — needs action) |
| `ActionQueue` | `ctx.system∈{OK,OFFLINE} && ctx.availability==OPEN && queueItems(ctx).isNotEmpty` | `REQUEST ? 30 : 55` |
| `TodayAgenda` | `ctx.system ∈ {OK,OFFLINE}` (renders timeline / positive-empty / closed-empty / vacation-empty) | `agendaPriority(ctx)` |
| `ComingUpPeek` | `ctx.system ∈ {OK,OFFLINE}` (cached offline) | `peekElevated(ctx) ? 45 : 70` |
| `SetupNudges` | `ctx.maturity∈{TRACTION,OPERATIONAL} && ctx.nudges.isNotEmpty` | `80` |
| `CreateAction` | `ctx.system != ERROR` (docked in nav, not in scroll) | — |

```
queueItems(ctx)  = REQUEST ? ctx.pendingReqs : ctx.exceptions   # instant mode: exceptions only

agendaPriority(ctx):
  if ctx.availability != OPEN:                 return 60   # muted (closed/vacation empty)
  if ctx.maturity in {SETUP, GROWTH}:          return 60   # muted (scaffold is the hero)
  if ctx.day == NO_APPTS:                       return 40   # positive-empty is the message
  return 50                                                 # normal timeline (ACTIVE/FULLY_BOOKED)

peekElevated(ctx) = ctx.day == NO_APPTS
                 or ctx.availability in {CLOSED_TODAY, VACATION}
                 or ctx.maturity in {SETUP, GROWTH}
```

**Total-error fallback:** when `ctx.system == ERROR`, the orchestrator bypasses the registry and renders a single centered error+retry (the only full-screen case).

---

## 7. Fixture matrix (resolver unit tests + orchestrator ordering snapshots)

Each row: representative inputs → expected `HomeContext` (key fields) → expected **ordered visible widgets** (by resolved priority). `S`=system, `Mat`=maturity, `Day`=day, `Avail`=availability, `Mode`=bookingMode. `Q>0` = queue non-empty. These are the assertions the build change must satisfy.

| # | Scenario | Key inputs | Resolved (S · Mat · Avail · Day · Mode) | Banners | Expected ordered widgets |
|---|----------|-----------|------------------------------------------|---------|--------------------------|
| 1 | Setup first-login (pending) | pending, 0 bookings, !complete | OK · SETUP · OPEN · NO_APPTS · — | [PENDING] | Banner, ActivationChecklist, ComingUpPeek(45), TodayAgenda(60) |
| 2 | Growth empty (verified) | verified, 0 bookings, complete | OK · GROWTH · OPEN · NO_APPTS · — | [] | GetDiscovered, ComingUpPeek(45), TodayAgenda(60) |
| 3 | No-appts (operational) | ≥T/30d, 0 today | OK · OPERATIONAL · OPEN · NO_APPTS · REQUEST | [] | TodayAgenda(40 positive-empty), ComingUpPeek(45) |
| 4 | Pending standalone (operational) | pending, has traction | OK · OPERATIONAL · OPEN · ACTIVE · REQUEST, Q>0 | [PENDING] | Banner, ActionQueue(30), NowNext(40), TodayAgenda(50), ComingUpPeek(70) |
| 5 | Active day — REQUEST | traction, appts, Q>0 | OK · OPERATIONAL · OPEN · ACTIVE · REQUEST | [] | ActionQueue(30), NowNext(40), TodayAgenda(50), ComingUpPeek(70) |
| 6 | Active day — INSTANT | traction, appts, no exceptions | OK · OPERATIONAL · OPEN · ACTIVE · INSTANT | [] | NowNext(25), TodayAgenda(50), ComingUpPeek(70) |
| 7 | Active day — INSTANT + conflict | as #6, exceptions>0 | OK · OPERATIONAL · OPEN · ACTIVE · INSTANT | [] | NowNext(25), TodayAgenda(50), ActionQueue(55 exceptions), ComingUpPeek(70) |
| 8 | Fully booked | capacity 0, appts>0 | OK · OPERATIONAL · OPEN · FULLY_BOOKED · REQUEST | [] | ActionQueue(30 waitlist), NowNext(40), TodayAgenda(50 dense), ComingUpPeek(70) |
| 9 | End of day | all completed | OK · OPERATIONAL · OPEN · ACTIVE · REQUEST | [] | EndOfDaySummary(40), TodayAgenda(50), ComingUpPeek(70) |
| 10 | Closed today | avail CLOSED_TODAY | OK · OPERATIONAL · CLOSED_TODAY · NONE · — | [CLOSED_TODAY] | Banner, ComingUpPeek(45 next-open), TodayAgenda(60 closed-empty) |
| 11 | Vacation (no in-window appts) | avail VACATION | OK · OPERATIONAL · VACATION · NONE · — | [VACATION] | Banner, ComingUpPeek(45), TodayAgenda(60 vacation-empty) |
| 12 | Vacation + affected bookings | VACATION, alerts>0 | OK · OPERATIONAL · VACATION · NONE · — | [VACATION] | Banner, BusinessAlerts(28), ComingUpPeek(45), TodayAgenda(60) |
| 13 | **Offline + pending** (orthogonal) | offline, pending, cache | OFFLINE · … · OPEN · … | [OFFLINE, PENDING] | Banner(offline top, pending 2nd), + maturity/day body from cache |
| 14 | **Operational + NoAppts + Offline** | offline, 0 today, cache | OFFLINE · OPERATIONAL · OPEN · NO_APPTS | [OFFLINE] | Banner, TodayAgenda(40, stale caption), ComingUpPeek(45) |
| 15 | Total error (no cache) | load FAILED, no cache | ERROR · … | — | *(registry bypassed)* centered error + retry |
| 16 | First load (no cache) | LOADING, no cache | LOADING · … | — | app bar + composition skeletons |

**Notes for test authoring:**
- Rows 13–14 prove **orthogonality**: system + lifecycle + maturity + day co-resolve; banners stack by §5; body composes from cache.
- Rows 5–7 prove **booking-mode reordering** is priority-only (same widgets, different order) — the core architectural claim.
- Row 3 vs 2 prove **No-Appts (operational) ≠ Growth-empty**: different hero, different agenda framing, from different maturity.
- Determinism: because `resolveHomeContext` is pure, each row is a single assertion; ordering rows double as orchestrator snapshot tests.

---

## 8. What this unlocks

With §3–§7 fixed, the build change is unambiguous:
1. Implement `HomeInputs`/`HomeContext` value objects + `resolveHomeContext`/`classifyMaturity`/`orderBanners` as pure functions → **unit-test against the §7 matrix**.
2. Implement the registry (visibility + priority expressions from §6) + thin orchestrator → **snapshot-test ordering against §7**.
3. Implement widgets incrementally (per [screen designs](PROVIDER_HOME_SCREEN_DESIGNS.md)); each is independently widget-testable across its four states.
4. Wire behind `/dashboard`, replacing `ProviderDashboardPage`.

No design decisions remain in the build — only mechanical translation to Flutter/BLoC.
