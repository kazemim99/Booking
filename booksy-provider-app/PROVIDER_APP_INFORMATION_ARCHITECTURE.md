# Booksy Provider App — Information Architecture & User Flow

> **Status:** Proposal for approval. This document defines *how the provider app is organized* — navigation, screen hierarchy, and the flows that connect them. It deliberately does **not** design screens, produce mockups, or specify Flutter code. Screen-by-screen UI design follows *after* this IA is approved.
>
> **Audience:** Product + design review.
> **Author role:** Senior Product Designer / Product Architect.
> **Platform:** Flutter, mobile-first, **Persian (fa-IR), RTL**.
> **Visual reference:** Coliride design language (tokens, spacing, typography, components, interaction patterns) — reused for *look and feel only*, never for business flows.

---

## 1. The one sentence that governs everything

> **A provider opens this app to _run their day_, not to _read a report_.**

Every decision below is derived from that sentence. When two designs compete, the one that gets the provider to the next *action* faster wins — even if it shows fewer numbers.

The home screen must answer **"What do I need to do right now?"** — not "How much did I earn last month?". Earnings, trends, and analytics are real and valuable, but they are a *Tuesday-evening-on-the-couch* activity, not a *9 a.m.-with-a-client-walking-in* activity. The app is optimized for the second moment.

---

## 2. Design principles (the rules every screen must obey)

| # | Principle | Consequence for the IA |
|---|-----------|------------------------|
| P1 | **Operational home, not analytics home** | The default tab is *Today*, an agenda + action queue — never a KPI dashboard. |
| P2 | **Time-critical first** | Information is ranked by *how soon the provider must act on it*, not by how interesting it is. |
| P3 | **Two taps to the common act** | Confirm a booking, add a walk-in, block time, call a client — each reachable in ≤2 taps from launch. |
| P4 | **Calm by default, loud only when it matters** | Alerts (new requests, conflicts) are visually prominent; vanity stats are quiet or absent from the primary surface. |
| P5 | **Progressive disclosure** | Setup, catalog, and configuration live in secondary navigation and are pulled in when needed — they don't compete with daily operation. |
| P6 | **The app works while "Pending Verification"** | Onboarding finishing ≠ publicly bookable. The IA must gracefully host the "set up while you wait" state. |
| P7 | **RTL-native** | Reading order, back-gesture direction, nav order, and progress all flow right-to-left. This is not a mirrored afterthought. |

---

## 3. How the professionals organize (competitive teardown)

Before proposing our structure, here is how the category leaders answer the same question. The pattern is remarkably consistent, and the differences are instructive.

| App | Default landing | Primary nav model | Where "New booking" lives | Analytics placement |
|-----|-----------------|-------------------|---------------------------|---------------------|
| **Booksy Biz** | **Calendar (day agenda)** | Bottom tabs: Calendar · Clients · Sales/Commerce · More (+ prominent add) | Center/prominent **+** → new appointment | Under "Sales"/"Stats", secondary |
| **Fresha (Partner)** | **Calendar / "Today"** | Bottom tabs: Calendar · Sales · Clients · Catalog · More | **+** on calendar | "Analytics" in More, secondary |
| **Treatwell Connect** | **Agenda (today's diary)** | Diary-centric, lightweight tabs | **+** on the diary | Reporting is a secondary section |
| **Square Appointments** | **Calendar** | Calendar-first, Checkout, Clients, Items, More | **+** on calendar | Reports under account/more |
| **Vagaro Pro** | **Calendar / Home digest** | Calendar · Customers · Marketing · More | **+** appointment on calendar | Reports secondary |
| **GlossGenius** | **Home ("Today" digest)** then Calendar | Home · Calendar · Clients · More | **+** on Home/Calendar | "Insights" secondary |

**What every one of them agrees on:**
1. **The operational surface is the home** — either a raw calendar or a "Today" digest. Nobody opens these apps to a chart.
2. **Booking creation is a first-class, always-visible action** — a prominent `+`, almost always a floating/center button.
3. **Clients are a top-level tab** — the book of business is core, not buried.
4. **Catalog / services / staff / settings are secondary** ("More") — you configure them occasionally, you don't live in them.
5. **Analytics is one level down** — present, but never the front door.

**Where they differ (and what we learn):**
- **Raw Calendar (Booksy/Square) vs. "Today" digest (GlossGenius/Vagaro).** A raw calendar is powerful but cold — it shows *time*, not *tasks*. A "Today" digest shows *what needs attention* (unconfirmed requests, next client, gaps, alerts) and *then* the agenda. **For a Persian-market, solo-and-small-team audience that is often new to structured booking software, the digest is the more humane front door.** We adopt a **"Today" operational home** as the default *and* keep a full **Calendar** as its own tab for planning ahead. This is the GlossGenius/Vagaro lineage, chosen deliberately over Booksy's calendar-as-home.
- **Sales/POS as a tab (Fresha/Square).** Only justified once payments/checkout exist in-app. Our backend has **bookings + stats but no in-app checkout yet**, so Sales does *not* earn a tab at launch. We reserve the slot.

> **Design decision D1 — Home = "Today", a workspace, not a dashboard.** We follow the digest lineage (GlossGenius/Vagaro) rather than calendar-as-home (Booksy/Square) because our audience benefits from being *told what needs doing* before being handed a grid. **Why:** it directly satisfies "what do I need to do right now?" with zero interpretation cost.

---

## 4. What information is time-critical? (the priority model)

Everything the app can show, ranked by *how soon the provider must act*. This ranking is the backbone of the Home screen and of notification urgency.

| Tier | Horizon | Examples | Where it surfaces |
|------|---------|----------|-------------------|
| **T0 — Act now** | Minutes | Unconfirmed booking **requests** awaiting my approval; a client arriving next; a double-booking/conflict; a slot-taken failure | **Top of Home**, as an *action queue*; push notification; badge |
| **T1 — Today** | Hours | The rest of today's appointments; no-show risk; gaps I could fill; a client running late | Home agenda (below the action queue) |
| **T2 — Soon** | 1–7 days | Tomorrow/this week's bookings; upcoming schedule changes; low-availability warnings | Calendar tab; a "coming up" peek on Home |
| **T3 — Housekeeping** | Days–weeks | Incomplete profile, missing photos, no staff added, services to price, respond to a review | Home "setup/nudges" strip (dismissible); More tab |
| **T4 — Reflective** | Weeks–months | Revenue, bookings count, retention, busiest hours, top services | **Insights** (secondary). *Never* the front door. |

> **Design decision D2 — The Home screen is literally this table, top to bottom.** Action queue (T0) → today's agenda (T1) → a compact "coming up" (T2) → dismissible nudges (T3). T4 is intentionally *absent* from Home. **Why:** the screen's vertical order = the provider's priority order. No cognitive translation needed.

---

## 5. The two entry moments

### 5.1 Immediately after onboarding finishes (first run)

Onboarding already collected: business info, category, location, services, working hours, and (optionally) gallery. The provider status is now most likely **`PendingVerification`** (see `provider_status.dart` — `canUseDashboard` includes `pendingVerification`).

**What should happen:** land on **Home (Today)** in a **first-run / activation state** — *not* a raw empty calendar, and *not* a celebration dead-end.

Home first-run composition (top to bottom):
1. **A "you're pending review" status banner** (only while `PendingVerification`) — reassuring, explains the business isn't publicly bookable *yet*, and that they can keep setting up.
2. **A short activation checklist** (T3), e.g.: *Add your team* · *Add photos to attract clients* · *Review your services & prices* · *Share your booking link*. Items completed during onboarding show as already ticked. This is the "get to first value" scaffold.
3. **An empty-but-inviting agenda** — "No appointments yet. Add your first booking or share your link to start receiving requests," with the **+ New Appointment** action right there.

> **Design decision D3 — First run reuses the Home *structure*, not a special screen.** The activation checklist occupies the T3 slot that will later hold nudges; the empty agenda occupies the T1 slot. **Why:** the provider learns the home layout on day one, and the screen *transforms* into the operational home as real bookings arrive — no jarring switch, no throwaway onboarding-success screen to design and maintain.

### 5.2 Returning provider on subsequent days

Straight to **Home (Today)**, now in its **operational state**:
- **T0 action queue** at the very top if anything needs approval/attention (else this block collapses).
- **Today's agenda** — the next appointment emphasized ("Now / Next"), then the rest of the day.
- **Compact "coming up"** peek (tomorrow's count / next free slot).
- Nudges (T3) only if something is genuinely incomplete; otherwise absent.

> **Design decision D4 — Session restore lands on Home, never on the last-viewed deep screen.** **Why:** the question "what do I need to do right now?" must be re-answered every launch; resuming three levels deep in a client's history from yesterday fails that test. (Deep-link launches — e.g. tapping a push notification — are the deliberate exception; see §12.)

---

## 6. Navigation architecture

### 6.1 Bottom navigation — 4 tabs + a center action

This is the app's spine. Five slots total; the **center slot is a create action, not a destination**.

```
RTL order (right → left, as the provider reads):

  ┌─────────────────────────────────────────────────────────┐
  │  خانه        تقویم       ➕        مشتریان      بیشتر      │
  │  Home       Calendar   (New)     Clients      More       │
  │  (Today)                                                  │
  └─────────────────────────────────────────────────────────┘
        ●            ○         ⊕          ○            ○
```

| Slot | Tab | Purpose | Why it earns a tab |
|------|-----|---------|--------------------|
| 1 | **Home / خانه (Today)** | The operational "what now" workspace | The default landing; the single most-visited surface (P1, D1). |
| 2 | **Calendar / تقویم** | Full day/week scheduling, blocks, availability | Planning ahead is a distinct mode from "today"; providers live here when managing future time. Universal top-level tab across all 6 competitors. |
| 3 | **➕ New (center)** | Create appointment / block time | Booking creation is the #1 proactive provider action (phone + walk-in bookings). Every competitor makes it prominent. See §6.3. |
| 4 | **Clients / مشتریان** | Customer book, history, contact | The book of business; core asset. Top-level in every competitor. |
| 5 | **More / بیشتر** | Services, Staff, Gallery, Business profile, Insights, Settings | Configuration + reflection — frequent enough to reach easily, rare enough to not deserve a dedicated tab (P5). |

> **Design decision D5 — Exactly 4 tabs + 1 center action. No fifth destination tab.**
> - **Why not put Services/Catalog as a 5th tab (like Fresha)?** Catalog is edited occasionally, not operated daily. Promoting it steals a slot from daily value and clutters the bar. It lives one tap into *More*.
> - **Why not a "Sales" tab (like Fresha/Square)?** There is no in-app checkout/POS yet. A tab with only stats behind it violates P1 ("workflows over statistics"). We **reserve** this slot: when payments ship, "More" → or a promoted **Sales** tab can replace one slot. Documented as a deliberate future hook, not an omission.
> - **Why 4 and not 3 or 5?** Three under-serves (Clients or Calendar would get demoted into More, adding taps to core work). Five destinations + a create action overflows the bar and shrinks touch targets below comfortable size — a real risk in RTL Persian labels which run longer than English.

**RTL note:** the bar reads **right-to-left**: Home is the *rightmost* (primary, first-read) slot. The selected-tab indicator, labels, and any badges follow RTL. This is a first-class requirement (P7), specified now so it is not retrofitted.

### 6.2 Top app bar behavior (per tab)

The app bar is **contextual** — it is not one static bar. Behavior is defined per tab so it is unambiguous during screen design.

| Tab | Leading (start / right in RTL) | Title | Trailing (end / left in RTL) | Scroll behavior |
|-----|-------------------------------|-------|------------------------------|-----------------|
| **Home** | **Business avatar/logo** → opens Account & Business sheet (§6.7, profile shortcut) | Greeting + business name (e.g. "صبح بخیر، سالن ...") | **🔔 Notifications** (with unread badge) | Greeting collapses to a compact title on scroll |
| **Calendar** | **Date / "Today" jump** control | Current period (day/week) | **View toggle** (day/week) + **🔍 find appointment** | Sticky date header; week strip may pin |
| **Clients** | (none / back when nested) | "مشتریان" + count | **🔍 Search clients** (persistent search field on scroll) | Search field pins to top |
| **More** | (none) | "بیشتر" | (none) | Standard scroll |
| **Deep screens** (e.g. a booking, a client) | **Back** (RTL: chevron pointing right, gesture from left edge) | Contextual entity title | Contextual overflow **⋮** (edit, cancel, share…) | Title may collapse; primary action may pin to bottom |

> **Design decision D6 — Notifications bell lives only on Home's app bar, not globally.** **Why:** a bell on every screen is visual noise and competes with contextual actions (Calendar needs its view toggle; Clients needs search). Home is the guaranteed daily touchpoint, so the bell there is sufficient; push notifications cover urgency between launches. The unread badge is also mirrored on the Home tab icon so it's visible from any tab.

> **Design decision D7 — Profile is a shortcut on Home *and* a section in More.** The Home avatar opens a quick Account & Business sheet; the full Business Profile / Account settings also live in More. **Why:** the avatar is the universally-learned "me/my business" affordance (matches all 6 competitors), but discoverability shouldn't depend on knowing that — More provides the browsable path (P5).

### 6.3 Floating / center action button (FAB usage)

> **Design decision D8 — Use a center-docked create action in the bottom bar, surfacing a "New" menu — not a per-screen floating FAB scattered across screens.**

- **Where:** the **center slot** of the bottom nav (§6.1), present on **Home and Calendar** (the operational tabs). Hidden on Clients/More where "create appointment" is not the primary intent (Clients has its own contextual "+ new client").
- **What it does:** taps open a small **action menu** (bottom sheet), because "create" has more than one meaning for a provider:
  - **New appointment** (book a walk-in / phone customer onto the calendar) — the dominant action.
  - **Block time** (lunch, personal, day off).
  - *(Later)* Add client, create sale.
- **Why a center-docked button and not a corner FAB?**
  - A corner FAB in RTL collides with the natural thumb path and with the back-gesture edge; a center-docked action is symmetric and unambiguous in both text directions.
  - It matches the mental model every competitor has trained (Booksy's prominent center **+**, Fresha/Square/Vagaro calendar **+**).
  - Docking it in the nav keeps a single, predictable "create" locus rather than a FAB that appears/disappears/relocates per screen.
- **Why a menu and not a direct jump to "new appointment"?** Blocking time and (later) other creates are frequent enough that forcing them through a different path would be inconsistent. The menu keeps *all* proactive creation behind one button. The most common item (New appointment) is first and one tap away.

### 6.4 Quick actions (the ≤2-tap promises)

Beyond the create button, these are the recurring micro-actions that must stay cheap. Each is listed with its home so screen design has a checklist.

| Quick action | Reachable from | Interaction | Backend |
|--------------|----------------|-------------|---------|
| **Confirm / decline a request** | Home T0 action queue (inline) | One tap confirm; swipe or tap for decline | `POST /Bookings/{id}/confirm`, `/cancel` |
| **Call / message next client** | Home "Now/Next" card; booking detail | Tap phone/chat icon | client contact from booking |
| **Mark complete / no-show** | Booking detail; agenda row overflow | Row action / detail action | `/complete`, `/no-show` |
| **Reschedule / reassign staff** | Booking detail | Opens slot/staff picker | `/reschedule`, `/assign-staff` |
| **Add a note to a booking** | Booking detail | Inline note field | `/{id}/notes` |
| **Block time** | Center create menu; long-press a calendar slot | Sheet | (availability) |
| **Add walk-in booking** | Center create menu; tap empty calendar slot | Booking composer | `POST /Bookings` |
| **Jump to today** | Calendar app bar "Today" | One tap | — |

> **Design decision D9 — Agenda rows and the action queue expose inline actions; the FAB is only for *creation*.** **Why:** confirming/declining/completing are *reactions to existing items* and belong *on* those items (inline/swipe), keeping the create button semantically pure (P3).

### 6.5 Contextual, contextual, contextual — no global omni-search

> **Design decision D10 — Search is scoped per tab, not a single global search icon.**

| Search need | Where | Why here |
|-------------|-------|----------|
| **Find a client** | Clients tab (persistent search field) | Providers search *people* far more than anything else; it deserves a dedicated, always-present field. |
| **Find an appointment** | Calendar app bar 🔍 (by client name / date) | Appointment lookup is inherently calendar-context. |
| **Find a service / staff** | Within Services / Staff screens (in More) | Low frequency; in-list filter suffices. |

**Why not one global search?** A single omni-search forces the provider to first decide "what am I searching?" and then disambiguate mixed results — added friction for a low-ambiguity need. Scoped search is faster and matches how Booksy/Fresha/Square actually ship it. A global search can be revisited only if usage shows providers hunting across types.

### 6.6 Notifications architecture

- **Surfaces:**
  - **Push** (OS-level) for T0/T1 events — the between-sessions channel. *(Requires wiring push infra; flagged as a dependency, not assumed present.)*
  - **In-app bell** on Home app bar → **Notification Center** screen (a deep screen, §7).
  - **Badges** on the Home tab icon + bell (unread count).
  - **Inline** T0 items also live in the Home action queue (a notification about a request *and* the request itself are the same job — see D9).
- **Categories & urgency (mapped to §4 tiers):**

  | Category | Tier | Default channel |
  |----------|------|-----------------|
  | New booking request / instant booking | T0 | Push + bell + Home queue + badge |
  | Cancellation by client | T0/T1 | Push + bell |
  | Upcoming appointment reminder | T1 | Push (opt) + bell |
  | Client reschedule request | T1 | Push + bell + Home queue |
  | Review received *(when reviews exist)* | T3 | Bell |
  | Verification status change (approved!) | T2/T3 | Push + bell + Home banner update |
  | Payout / sales *(future)* | T4 | Bell |

- **Notification Center screen:** grouped by day, tap-through deep-links to the underlying entity (booking, client, review). Read/unread state; "mark all read."

> **Design decision D11 — Notifications and the Home action queue are two views of the same underlying "things needing attention," not separate inboxes.** **Why:** avoids the classic trap where a provider clears the queue but the bell still shows unread (or vice-versa). One source of truth, two entry points.

### 6.7 Profile & account access

- **Entry points:** Home app bar **avatar** (quick sheet) + **More** tab (browsable).
- **Quick sheet (from avatar):** business name & status chip (e.g. "در انتظار تأیید" while pending), *View public profile*, *Business profile*, *Account & settings*, *Log out*. Multi-location/business switcher slot reserved here for the future.
- **Full sections (in More):** *Business Profile* (name, description, category, address/map, hours, gallery, logo — editing what onboarding created), *My Account* (personal details, phone/OTP-based), *Notifications settings*, *Language/Region*, *Help & Support*, *Legal*, *Log out*.

> **Design decision D12 — "Business Profile" (the public-facing entity) and "My Account" (the person/login) are distinct sections.** **Why:** a provider mentally separates "my salon's page that customers see" from "my login and preferences." Conflating them (a single "Profile") is a known source of confusion, especially once staff/multi-user access arrives.

---

## 7. Complete screen map (primary → secondary → deep)

Legend: **P** = primary (bottom-nav destination) · **S** = secondary (one level in) · **D** = deep (contextual/detail) · *(F)* = future/reserved.

```
Booksy Provider App
│
├── ● HOME / خانه  (Today) ...................................... [P]  ← default landing
│   ├── Status banner (Pending Verification)  ................... [inline state]
│   ├── T0 Action queue (requests / conflicts) ................. [inline] → Booking detail [D]
│   ├── Now / Next appointment card  .......................... [inline] → Booking detail [D]
│   ├── Today agenda (list)  .................................. [inline] → Booking detail [D]
│   ├── "Coming up" peek  ..................................... [inline] → Calendar [P]
│   ├── Activation checklist / nudges (T3, first-run & partial) [inline] → relevant setup [S]
│   ├── App bar: Account/Business sheet  ..................... [S]
│   └── App bar: Notification Center  ........................ [D]
│       └── Notification item → underlying entity  ........... [D]
│
├── ○ CALENDAR / تقویم  ....................................... [P]
│   ├── Day view / Week view (toggle)  ....................... [inline]
│   ├── Per-staff columns (when multi-staff)  ................ [inline]
│   ├── Tap slot → New appointment composer  ................. [D]
│   ├── Long-press slot → Block time  ........................ [D]
│   ├── Tap appointment → Booking detail  .................... [D]
│   ├── Find appointment (search)  ........................... [D]
│   └── Manage availability / working hours override  ........ [S]
│
├── ⊕ NEW (center action menu)  .............................. [action, not a screen]
│   ├── New appointment → Booking composer  .................. [D]
│   │   ├── Pick client (or quick-add)  ...................... [D]
│   │   ├── Pick service(s)  ................................. [D]
│   │   ├── Pick staff  ...................................... [D]
│   │   ├── Pick date/time (available slots)  ................ [D]
│   │   └── Confirm  ......................................... → Booking detail [D]
│   ├── Block time  .......................................... [D]
│   └── (F) Add client / Create sale
│
├── ○ CLIENTS / مشتریان  ..................................... [P]
│   ├── Client list (search + filter)  ...................... [inline]
│   ├── Add client  ......................................... [D]
│   └── Client detail  ...................................... [D]
│       ├── Contact / call / message  ....................... [action]
│       ├── Booking history  ................................ [D]
│       ├── Upcoming bookings  .............................. [D]
│       ├── Notes / preferences  ............................ [D]
│       └── Book again → Booking composer  .................. [D]
│
└── ○ MORE / بیشتر  ......................................... [P]
    ├── Business Profile  ................................... [S]
    │   ├── Basic info / description / category  ............ [D]
    │   ├── Location & map  ................................. [D]
    │   ├── Working hours  .................................. [D]
    │   ├── Gallery (manage/reorder/primary)  ............... [D]
    │   ├── Logo / cover image  ............................. [D]
    │   └── View public profile (preview)  ................. [D]
    ├── Services / Catalog  ................................. [S]
    │   ├── Service list  ................................... [inline]
    │   ├── Add / edit service (name, duration, price)  ..... [D]
    │   └── (F) Categories / packages
    ├── Staff / Team  ....................................... [S]
    │   ├── Staff list  ..................................... [inline]
    │   ├── Add / edit staff (+ photo, role)  ............... [D]
    │   └── Per-staff working hours  ........................ [D]
    ├── Insights / گزارش‌ها  (T4)  .......................... [S]  ← analytics lives HERE, not on Home
    │   ├── Bookings & revenue summary  ..................... [D]
    │   ├── Busy hours / top services  ...................... [D]
    │   └── (F) Retention / marketing stats
    ├── (F) Sales / Payments  ............................... [S] reserved
    ├── My Account  ......................................... [S]
    ├── Notification settings  .............................. [S]
    ├── Language & Region  .................................. [S]
    ├── Help & Support  ..................................... [S]
    └── Log out  ............................................ [action]
```

### 7.1 Special / non-tab states (already partly built)

These sit *outside* the tabbed shell and are gated by auth/status (see `app_router.dart`, `provider_status.dart`):

| State | Screen | Trigger |
|-------|--------|---------|
| Splash | resolving session | app launch |
| Login / OTP | phone auth | unauthenticated |
| Onboarding wizard | 8-step business setup | `Drafted` |
| **Account blocked** | "account unavailable" | `Suspended` / `Inactive` / `Archived` |
| **Pending verification** | *not a separate screen* — a **banner inside Home** | `PendingVerification` |

> **Design decision D13 — "Pending verification" is a Home banner, not a gate screen.** The router already lets `PendingVerification` into the dashboard (`canUseDashboard`). **Why:** blocking a freshly-onboarded provider behind a "waiting" wall kills momentum; letting them set up staff, photos, and prices while they wait converts waiting time into activation. Blocked/suspended states *do* gate (they're punitive/terminal), and already have a dedicated screen.

---

## 8. Primary user flows

### 8.1 Post-onboarding → first value
```
Onboarding step 8 (completion)
   → [Enter dashboard]
   → HOME (first-run): pending banner + activation checklist + empty agenda
   → Provider taps "Add your team" (or "Share booking link", or ⊕ New appointment)
   → completes a setup task / creates first booking
   → Home reflects progress (checklist item ticks; agenda populates)
```

### 8.2 Daily operation (returning provider)
```
Launch → HOME (Today)
   → T0 queue shows 2 new requests
   → tap "Confirm" on each (inline, 1 tap)  [/Bookings/{id}/confirm]
   → "Now/Next" shows the 10:00 client → tap to call if needed
   → mid-day: walk-in arrives → ⊕ New → New appointment → pick client/service/time → Confirm
   → client leaves → agenda row → Mark complete  [/Bookings/{id}/complete]
```

### 8.3 Handle a cancellation / reschedule
```
Push: "Client cancelled 14:00"  → tap
   → Booking detail (cancelled)  → gap now on Calendar
   → optional: message waitlist / block the gap
OR client asks to move:
   → Booking detail → Reschedule → available-slots picker → confirm  [/Bookings/{id}/reschedule]
```

### 8.4 Manage the book of business
```
CLIENTS → search name → Client detail
   → see history + notes → "Book again" → Booking composer (client pre-filled)
```

### 8.5 Configure the business (occasional)
```
MORE → Services → edit price
MORE → Staff → add team member + photo + hours
MORE → Business Profile → Gallery → add/reorder photos, set primary
```

### 8.6 Reflect (deliberately secondary)
```
MORE → Insights → this week's bookings / revenue / busy hours
```
> Note the *depth*: reflection is **two taps down from a non-default tab**. That is the intended friction — it keeps analytics available without letting it hijack the daily surface (P1, D2).

---

## 9. Deep linking & app-open targeting

> **Design decision D14 — Push/notification taps deep-link to the *entity*, then rebuild the tab stack beneath it.**

| Source | Opens | Back stack rebuilt to |
|--------|-------|-----------------------|
| "New request" push | Booking detail (or Home T0 queue) | Home |
| "Cancellation" push | Booking detail | Home → Calendar (day of booking) |
| "Verification approved" push | Home (banner now positive) | Home |
| Notification Center item | underlying entity | Notification Center → Home |
| Cold launch (no deep link) | **Home (Today)** | — (per D4) |

**Why rebuild the stack:** landing deep from a notification is correct (that *is* the current "what now?"), but the provider must still be able to press Back into a sensible place rather than exiting the app — so we synthesize the natural parent chain.

---

## 10. Mapping to existing backend (feasibility check)

The proposed IA is buildable today — every primary surface maps to a shipped endpoint (from `API_ENDPOINTS.md`):

| IA surface | Backend |
|------------|---------|
| Home agenda / Calendar | `GET /Bookings/provider/{providerId}`, `/Bookings/search`, `/Bookings/available-slots` |
| T0 confirm/decline | `/Bookings/{id}/confirm`, `/cancel` |
| Complete / no-show / reschedule / assign-staff / notes | `/{id}/complete`, `/no-show`, `/reschedule`, `/assign-staff/{staffId}`, `/{id}/notes` |
| New appointment | `POST /Bookings` |
| Clients | *(customer/booking data; a dedicated provider-clients list may need a thin endpoint — flag)* |
| Services (More) | ServiceCatalog services endpoints |
| Staff (More) | `/Providers/{id}/staff` CRUD + photo |
| Gallery (More) | `/Providers/{providerId}/gallery` CRUD/reorder/primary |
| Business Profile (More) | `/Providers/profile`, `/business`, `/business/logo`, `/profile/image` |
| Insights (More) | `/Bookings/{...}` **Booking Statistics** |
| Status banner / routing | `/Providers/current/status`, JWT `provider_status` |

**Gaps to confirm with backend (not blockers for IA approval):**
1. A **provider-scoped client list** endpoint (Clients tab) — may need to derive from bookings or add an endpoint.
2. **Push notification** infrastructure (device token registration, send pipeline).
3. **Reviews** surface (referenced as future; confirm availability).

---

## 11. What we deliberately are NOT doing (and why)

| Excluded from launch IA | Why | Where it's reserved |
|-------------------------|-----|---------------------|
| Analytics/KPIs on Home | Violates P1; home is for action | Insights (More) |
| A "Sales/POS" tab | No in-app checkout yet | Reserved 5th slot / More |
| Global omni-search | Adds disambiguation friction | Scoped search per tab (D10) |
| Marketing/promotions hub | Post-MVP; not daily-critical | Future More section |
| Multi-location switcher UI | Not needed until multi-location exists | Slot reserved in avatar sheet |
| A separate onboarding-success screen | Home first-run absorbs it (D3) | — |

---

## 12. Open questions for you (before we design screens)

These are genuine product forks where your answer changes the design. None block approving the *structure* above; they refine it.

1. **Primary audience:** solo practitioners, or multi-staff businesses? This tunes how prominent per-staff calendar columns and Staff management are. *(Backend supports staff; question is emphasis.)*
2. **Payments/checkout scope for MVP:** in-app or not? Decides whether the reserved "Sales" slot activates soon or stays dormant.
3. **Booking model:** are new bookings **request-and-approve** (provider confirms) or **instant** (auto-confirmed)? This sizes the T0 action queue — it's central if approval-based, minor if instant.
4. **Reviews:** in scope for the provider app at launch? Affects Notifications + a possible Home nudge.
5. **Push notifications:** is device-token infra planned for MVP? If not, T0 relies on in-app queue only and we design around that.
6. **Client list source:** confirm whether a provider-clients endpoint exists or Clients is derived from booking history for now (affects Clients-tab richness).

---

## 13. Summary of the recommendation

- **Home = "Today", an operational workspace** (action queue → agenda → coming-up → nudges). Analytics is *not* here. *(D1, D2)*
- **Bottom nav: Home · Calendar · ⊕New · Clients · More** — 4 destinations + one center create action, RTL-ordered. *(D5, D8)*
- **Contextual top app bar**, notifications bell on Home, scoped search per tab, profile via Home avatar + More. *(D6, D7, D10, D12)*
- **First-run and returning both land on Home**, which transforms from activation-checklist to live agenda. *(D3, D4)*
- **Pending-verification is a Home banner, not a wall**; blocked is a gate. *(D13)*
- **Every surface maps to a shipped backend endpoint** (a few gaps flagged, none blocking). *(§10)*
- **Analytics, Sales/POS, global search, marketing are deliberately deferred**, with reserved homes. *(§11)*

This mirrors where Booksy, Fresha, Treatwell, Square, Vagaro, and GlossGenius converged — *operational surface as home, prominent create, clients top-level, everything else one level down* — while choosing the **"Today" digest** front door (GlossGenius/Vagaro lineage) over calendar-as-home because it best answers **"What do I need to do right now?"** for our audience.

---

> **Next step (pending your approval):** once you approve this IA, we design screens one by one — starting, per this architecture, with **Home (Today)** in both its first-run and operational states. No Flutter code or mockups will be produced before then.

---

## Related design documents

- **[PROVIDER_HOME_TODAY_DESIGN.md](PROVIDER_HOME_TODAY_DESIGN.md)** — the Home ("Today") workspace design: all 10 Home states, the maturity-adaptive composition, and the widget-composition architecture. Backed by the OpenSpec change `design-provider-home-workspace` (capability `provider-home-workspace`).
