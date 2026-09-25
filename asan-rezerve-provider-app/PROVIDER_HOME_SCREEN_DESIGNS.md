# Provider Home — Screen Designs (Anchor Set)

> **Status:** Screen-design specification for review. Follows the approved [Home design](PROVIDER_HOME_TODAY_DESIGN.md) and [IA](PROVIDER_APP_INFORMATION_ARCHITECTURE.md). Grounded in the app's real tokens (`lib/config/theme/app_tokens.dart`) and the `AppCard` component.
>
> **This set covers the two anchor screens** — the extremes of the maturity range — because everything else interpolates between them:
> 1. **Setup — First login after onboarding** (scaffold-led)
> 2. **Operational — Active business day** (agenda-led, both booking modes)
>
> **Not** here: Flutter code, hi-fi/pixel mockups, or the remaining 8 states (they reuse the same widgets + treatments defined here). This is the mid-fidelity spec a mockup or Flutter build is produced *from*.

---

## 0. Shared foundations

### 0.1 Token legend (real values from `app_tokens.dart`)

| Role | Token | Value |
|------|-------|-------|
| Heading / body ink | `AppColors.ink` | `#4D5E80` navy |
| Secondary text | `AppColors.muted` | `#96A0B3` |
| Primary action / links | `AppColors.primary` | `#3777BF` blue |
| Success / checks | `AppColors.success` | `#0AC075` green |
| Danger / decline | `AppColors.danger` | `#FF6171` |
| Warning | `AppColors.warning` | `#FFCB33` |
| Info/pending tint | `AppColors.primarySoft` | `#E3F2FD` |
| Success tint | `AppColors.successSoft` | `#E9FFF6` |
| Soft item fill | `AppColors.surfaceSoft` | `#FAFAFA` |
| Card border / divider | `AppColors.border` / `divider` | `#EBEEF3` / `#E5E9F2` |
| Card | `AppCard` | white · 1px border · radius `AppRadius.card=15` · padding `AppSpacing.card=12` |
| Spacing scale | `AppSpacing` | xs4 · sm8 · md16 · lg24 · xl40 |
| Radii | `AppRadius` | sm8 · md12 · lg16 · button10 · panel16 |
| Icon sizes | `AppIconSize` | sm16 · action20 · md24 · hero72 |
| Motion | `AppMotion` | fast180ms · medium250ms · easeOutCubic |
| Button | `AppDimens` | height 46 · font 17 · radius 10 |

**Coliride language:** flat and shadowless — depth comes from **1px borders and tinted fills, never elevation shadows**. Soft 15px card corners. Navy ink on white.

### 0.2 Page skeleton (all Home states)

```
┌─ Scaffold (white) ───────────────────────────────┐
│  HomeAppBar            (fixed, light)             │
├───────────────────────────────────────────────────┤
│  ↕ Scroll view · RefreshIndicator (pull-to-refresh)│
│    padding: horizontal AppSpacing.md (16)          │
│    inter-widget gap: AppSpacing.md (16)            │
│    [ widget ]                                      │
│    [ widget ]   ← ordered by registry priority     │
│    …                                               │
│    bottom safe-area pad: AppSpacing.xl (40)        │
│    (clears the docked CreateAction)                │
├───────────────────────────────────────────────────┤
│  BottomNav + ⊕ CreateAction (center-docked)        │
└───────────────────────────────────────────────────┘
```

Each widget is an `AppCard` unless noted; the scroll body is the orchestrator's only layout responsibility.

### 0.3 Home app bar anatomy

> **Design decision (for review): the Home app bar is LIGHT (white surface, ink text), not the blue chrome (`AppColors.appBar #3777C0`) used on auth/onboarding.** Rationale: the Home is content-first and status-banner-heavy; a blue bar adds visual weight and competes with the banner rail directly beneath it. Blue chrome stays the signal for *primary actions* and *flow* screens. Flag if you want brand consistency to override.

```
RTL (right → left):
┌───────────────────────────────────────────────────┐
│  [avatar]  صبح بخیر، سالن رُز              [🔔•2]  │
│   40dp     ink 17/600      muted 13 (business)      │
└───────────────────────────────────────────────────┘
  ▲ tap → Account & Business sheet        ▲ tap → Notification Center
```
- **Avatar** (rightmost, first-read): 40dp circle, business logo or initial on `primarySoft`. Tap → account sheet.
- **Greeting block:** line 1 `صبح بخیر` / `عصر بخیر` (time-based) + business name, ink 17/600; line 2 optional business name muted 13 if greeting+name overflow.
- **Bell** (leftmost): `AppIconSize.md` icon in a 44dp target; unread badge = `danger` dot with count, mirrored on the Home tab icon. Hidden count when 0.
- On scroll: greeting collapses to a compact single-line title (`AppMotion.fast`).

### 0.4 The four-state recipe (every widget, consistently)

| Sub-state | Visual treatment |
|-----------|------------------|
| **Loading** | Skeleton shimmer *inside the card's own shape* — never a full-screen spinner. Card height ≈ its loaded height to avoid layout jump. |
| **Empty** | Collapses (removed from scroll) **unless** empty is meaningful for that widget (e.g. TodayAgenda's positive "no appointments today"). |
| **Error** | Localized inside the card: `danger`-tinted icon, one-line human message, `تلاش مجدد` (retry) text button. Siblings unaffected. |
| **Offline** | Card shows cached content + a muted `آخرین بروزرسانی…` (last updated) caption; mutating buttons gain a small `⏱ در صف` (queued) affordance. |

### 0.5 Action-button constraint (footgun guard)

Themed primary buttons are **infinite-width** (`Size.fromHeight`) and blow up inside a `Row`. **All in-card action rows MUST use width-constrained buttons** (intrinsic/wrapped width, or `Expanded` with explicit flex) — never the raw themed button in a `Row`. Verify every action row against the real theme.

### 0.6 RTL & motion rules

- Reading order, icon/label order, chevrons, and progress all flow **right-to-left**. Back/forward chevrons point *right* for "back".
- Success micro-celebrations (checklist tick, "all caught up", end-of-day) use `AppMotion.medium`; collapse/expand uses `AppMotion.fast`. All respect reduced-motion (instant, no travel).

---

## 1. Screen A — Setup: First login after onboarding

**Resolved `HomeContext`:** `system=OK · lifecycle=PENDING_VERIFICATION · maturity=SETUP · day=NO_APPTS · availability=OPEN`.
**Goal:** "what do I do next?" → reassure + guide to first value. **Hero:** ActivationChecklist. Agenda is muted.

### 1.1 Composition (top → bottom)

```
[ HomeAppBar — light, no bell badge ]
────────────────────────────────────────────────
[ ① PendingVerificationBanner ]        prio 1  (info tint)
[ ② ActivationChecklist  ★HERO ]       prio 2
[ ③ ComingUpPeek — elevated, empty ]   prio 3.5
[ ④ TodayAgenda — muted empty ]        prio 4
                ( ⊕ CreateAction )
```

### 1.2 ① PendingVerificationBanner

Not an `AppCard` — a full-width **banner** with `primarySoft #E3F2FD` fill, radius `AppRadius.md`, 1px `border`, `AppSpacing.card` padding.

```
┌─────────────────────────────────────────────────┐
│ ⓘ  کسب‌وکار شما در حال بررسی است                 │  ink 15/600
│    تا زمان تأیید، برای مشتریان قابل‌رزرو نیست؛     │  muted 13
│    می‌توانید همچنان پروفایل را کامل کنید.          │
│                                   [ تماس با پشتیبانی ]│  primary text btn
└─────────────────────────────────────────────────┘
```
- **Icon:** info glyph, `AppColors.primary`, in a 32dp `primarySoft` rounded square (leading, right side in RTL).
- **Copy:** title *"کسب‌وکار شما در حال بررسی است"* (your business is under review); body *"تا زمان تأیید، برای مشتریان قابل‌رزرو نیست؛ می‌توانید همچنان پروفایل خود را کامل کنید."*
- **Action:** `تماس با پشتیبانی` (contact support), primary text button.
- **Success transition:** on approval → morphs to `successSoft` fill, ✓ `success` icon, *"کسب‌وکار شما فعال شد!"* (your business is live), action `اشتراک‌گذاری لینک رزرو` (share booking link). Uses `AppMotion.medium`.
- **States:** loading n/a (from `Ctx`). error → if status unknown, keep last + tiny retry. offline → still shown (cached status).

### 1.3 ② ActivationChecklist — the hero

`AppCard`, full width, the visually dominant element (larger internal type + a progress affordance).

```
┌─ AppCard ────────────────────────────────────────┐
│ راه‌اندازی کسب‌وکار            ۱ از ۴ انجام شد      │  ink 17/700  · muted 13
│ ▓▓▓▓░░░░░░░░░░░░  (25%)                            │  progress: success fill on border track
│                                                    │
│  ✅ خدمات و قیمت‌ها            (انجام شد)      ›     │  done: successSoft row, strike-calm
│  ⭕ افزودن اعضای تیم                          ›     │  todo: surfaceSoft row
│  ⭕ افزودن تصاویر گالری                       ›     │
│  ⭕ اشتراک‌گذاری لینک رزرو                    ›     │
└────────────────────────────────────────────────────┘
```
- **Header:** title *"راه‌اندازی کسب‌وکار"* (set up your business) ink 17/700; counter *"۱ از ۴ انجام شد"* (1 of 4 done) muted 13, Persian numerals.
- **Progress bar:** track = `border`, fill = `success`, height 6, radius full.
- **Rows (44dp min):** leading status glyph → completed = `success` ✓ on `successSoft`, todo = hollow circle `icon` on `surfaceSoft`; label ink 15; trailing chevron `‹` (RTL) muted.
  - Row copy: `خدمات و قیمت‌ها` (services & prices, pre-done from onboarding) · `افزودن اعضای تیم` (add team) → `openStaff` · `افزودن تصاویر گالری` (add photos) → `openGallery` · `اشتراک‌گذاری لینک رزرو` (share booking link) → `shareBookingLink`.
- **Interaction:** tap row → routes to that setup surface (emits `output`, orchestrator navigates). On completion, row animates to done (`AppMotion.medium`), counter + bar update.
- **Collapsible:** once ≥ (config) complete, collapses to a single summary row (`راه‌اندازی — ۳ از ۴` + chevron); fully complete → one-time success flash then the widget removes itself (visibility rule fails at Growth/Traction).
- **States:** loading → 4 row skeletons + header skeleton. empty → n/a (hidden when not Setup). error → per-row inline retry if a completion write fails; other rows interactive. offline → rows visible; tapping a completing action shows `⏱ در صف`.

### 1.4 ③ ComingUpPeek — elevated (empty variant)

Compact `AppCard`, single row. Because today is empty, it's elevated (prio 3.5) to point forward.

```
┌─ AppCard ──────────────────────────────────────┐
│ 📅  هنوز نوبتی ثبت نشده                    ›     │  ink 15 · muted
│     با اشتراک لینک رزرو، اولین نوبت را بگیرید     │  muted 13
└──────────────────────────────────────────────────┘
```
- Copy (empty): *"هنوز نوبتی ثبت نشده"* (no bookings yet) + hint *"با اشتراک‌گذاری لینک رزرو، اولین نوبت را دریافت کنید"*. Tap → share link (reinforces the checklist CTA).
- **States:** load skeleton compact; error → hidden (non-critical); offline → cached.

### 1.5 ④ TodayAgenda — muted empty

`AppCard`, de-emphasized (this is Setup — nothing to operate yet). Positive, inviting empty state.

```
┌─ AppCard ──────────────────────────────────────┐
│                  🗓  (hero 72, icon tint)        │
│           امروز نوبتی ندارید                     │  ink 16/600, centered
│     اولین نوبت را به‌صورت دستی ثبت کنید           │  muted 13, centered
│              [  + افزودن نوبت  ]                 │  outline/secondary btn (constrained)
└──────────────────────────────────────────────────┘
```
- Empty copy: *"امروز نوبتی ندارید"* (no appointments today) + *"اولین نوبت را به‌صورت دستی ثبت کنید"*; action `+ افزودن نوبت` (add appointment) → opens booking composer (also reachable via ⊕).
- Muted = reduced vertical padding vs. hero checklist; icon in `icon`/`hint` tint, not full color.
- **States:** loading → 2 faint row skeletons; error → localized retry; offline → cached list (here, empty) + last-updated caption.

### 1.6 ⊕ CreateAction (shared, all Home states)

Center-docked in the bottom nav (not in scroll). Tap → bottom sheet (`AppRadius.bottomSheet=14`):
```
افزودن                                   (sheet title)
  📅  نوبت جدید           → newAppointment
  ⛔  مسدود کردن زمان     → blockTime
```
- Primary item `نوبت جدید` (new appointment) first. `مسدود کردن زمان` (block time) second.
- Offline: `نوبت جدید` allowed (queues) with a note; error state: disabled if no provider context.

### 1.7 Screen-A full-screen states

- **Initial load:** app bar renders immediately; body shows the composition's skeletons (banner strip + checklist skeleton + two faint agenda rows). No blocking spinner.
- **Total error (no cache):** centered `danger` hero icon, *"بارگذاری ناموفق بود"* (loading failed) + `تلاش مجدد`. Only when *everything* fails with no cache.
- **Offline:** `OfflineBanner` (neutral `surfaceSoft`, muted) pins above the pending banner; checklist/agenda show cached; a `بازگشت به شبکه` (back online) success toast on reconnect flushes queued completions.

---

## 2. Screen B — Operational: Active business day

**Resolved `HomeContext`:** `system=OK · lifecycle=none · maturity=OPERATIONAL · day=ACTIVE · availability=OPEN · bookingMode=REQUEST|INSTANT`.
**Goal:** run the day, miss nothing, act in ≤2 taps. Scaffold is gone. Booking mode reorders the top two widgets.

### 2.1 Composition — REQUEST mode (primary)

```
[ HomeAppBar — light, bell •2 ]
────────────────────────────────────────────────
[ ① ActionQueue  ★LOUDEST ]        prio 2   (pending requests)
[ ② NowNext ]                       prio 3
[ ③ TodayAgenda ]                   prio 4
[ ④ ComingUpPeek ]                  prio 6
                ( ⊕ CreateAction )
```

### 2.2 Composition — INSTANT mode (variation)

```
[ HomeAppBar — light ]
────────────────────────────────────────────────
[ ① NowNext  ★PRIMARY ]             prio 2
[ ② TodayAgenda ]                   prio 3
[ (ActionQueue — only if a conflict/exception exists, demoted) ]
[ ③ ComingUpPeek ]                  prio 6
                ( ⊕ CreateAction )
```
> Same widgets, same registry — **only the `priority` differs by `Ctx.bookingMode`.** No alternate layout code. This is the composition architecture paying off.

### 2.3 ① ActionQueue (Request mode = hero)

`AppCard` with a `primary`-accent header; the loudest element on the screen.

```
┌─ AppCard ────────────────────────────────────────┐
│ 🔔 درخواست‌های در انتظار              ۲          │  ink 16/700 · count chip (primary)
│ ─────────────────────────────────────────────────│
│ سارا محمدی · اصلاح مو · امروز ۱۴:۳۰               │  ink 14 · muted 12
│         [ رد ]              [ تأیید ]             │  danger-outline · primary-filled
│ ─────────────────────────────────────────────────│
│ رضا کریمی · رنگ · فردا ۱۰:۰۰                      │
│         [ رد ]              [ تأیید ]             │
│                              مشاهده همه (۵) ›     │  if > N, collapse
└────────────────────────────────────────────────────┘
```
- **Header:** *"درخواست‌های در انتظار"* (pending requests) + count chip (`primary` fill, white numeral).
- **Request row:** client name (ink 14/600) · service · time (muted 12); two constrained buttons — `تأیید` (confirm, `primary` filled) and `رد` (decline, `danger` outline). Confirm is the visually dominant (primary) of the pair; in RTL, confirm sits on the left (natural "forward").
- **Interaction:** `تأیید` → optimistic remove + row collapses with a `success` check flash; `رد` → confirm-destructive affordance then remove. Tap row body → booking detail.
- **Collapsible:** beyond N rows → `مشاهده همه (۵)` (view all).
- **Empty → success:** when the last request clears → brief `successSoft` "همه رسیدگی شد ✓" (all caught up) flash (`AppMotion.medium`), then the card collapses out.
- **States:** loading → 2 request-row skeletons. error → localized `danger` strip + retry; other cards fine. offline → rows from cache; `تأیید`/`رد` gain `⏱ در صف`, sync on reconnect; optimistic result rolls back on failed sync with an inline error.
- **Fully-booked variant:** a new request routes to `لیست انتظار` (waitlist) / `رد با پیشنهاد جایگزین` (decline-with-alternative) instead of confirm-into-slot.

### 2.4 ② NowNext (Instant mode = primary; Request mode = below queue)

`AppCard`, the actionable hero for the current/next appointment. In Instant mode it carries a subtle `primary` left-edge accent to read as primary.

```
┌─ AppCard ────────────────────────────────────────┐
│ اکنون / بعدی                        ۱۰:۰۰         │  muted 12 label · ink 17/700 time
│ سارا محمدی · اصلاح مو (۴۵ دقیقه)                  │  ink 15 · muted 13
│  [📞 تماس] [💬 پیام] [✓ تکمیل] [↻ جابه‌جایی]      │  icon action chips (44dp)
└────────────────────────────────────────────────────┘
```
- **Label:** *"اکنون"* if in progress, else *"بعدی"* (now / next). Time ink 17/700 (Persian numerals).
- **Actions (icon chips, constrained, 44dp):** `تماس` (call) · `پیام` (message) · `تکمیل` (complete, `success`) · `جابه‌جایی` (reschedule). Overflow `⋮` → `عدم حضور` (no-show, `danger`), `تخصیص کارمند` (assign staff), `یادداشت` (note).
- **≤2-tap promise:** complete = 1 tap (+ undo snackbar); call = 1 tap.
- **End-of-day success:** when all today's appts complete, NowNext is replaced by **EndOfDaySummary** (`successSoft` card, ✓, *"کارِ امروز تمام شد — ۷ نوبت انجام شد"* / today's work is done — 7 appointments; optional `مشاهده گزارش` → Insights).
- **States:** loading → hero skeleton; error → localized; offline → cached, mutations queue.

### 2.5 ③ TodayAgenda (the timeline)

`AppCard` (or a titled section of stacked rows). The full day.

```
┌─ AppCard ────────────────────────────────────────┐
│ برنامهٔ امروز                        ۷ نوبت        │  ink 16/700 · muted count
│ ─────────────────────────────────────────────────│
│ │ ۱۰:۰۰  سارا محمدی · اصلاح مو        ✓ انجام شد  │  done row: success tick, calm
│ │ ۱۱:۰۰  رضا کریمی · رنگ              ● اکنون     │  current: primary dot + tint
│ │ ۱۲:۰۰  ── استراحت ──                            │  break row: divider style
│ │ ۱۳:۰۰  مینا رستمی · براشینگ                    │  upcoming
│ │ …                                    نمایش قبلی‌ها ›│  past collapse
└────────────────────────────────────────────────────┘
```
- **Row:** time (leading, ink 15/600, RTL right) · client · service · status trailing (`✓ انجام شد` done / `● اکنون` now / plain upcoming). Current appointment row gets a faint `primarySoft` fill + `primary` leading dot.
- **Breaks** render as a distinct divider-style row (`مینای` calm, not a client).
- **Row actions:** swipe or tap → `تکمیل` / `عدم حضور` / open detail. Past appointments collapse behind `نمایش قبلی‌ها` (show earlier).
- **Fully-booked:** dense variant; breaks visually protected; a `تکمیل ظرفیت` (fully booked) chip in the header.
- **States:** loading → 4 row skeletons; **empty (No-Appointments day)** → the positive centered empty from §1.5 (this same widget renders it); error → localized retry; offline → cached + `آخرین بروزرسانی ۹:۴۱` caption.

### 2.6 ④ ComingUpPeek (normal)

Compact single-row `AppCard`: *"فردا ۶ نوبت"* (tomorrow, 6 appointments) + chevron → Calendar(next day). Non-critical: hides on error, cached offline.

### 2.7 Screen-B full-screen states

- **Initial load:** app bar instant; body skeletons in composition order (queue rows / hero / timeline rows). No blocking spinner.
- **Pull-to-refresh:** the one global gesture; fans out to each visible widget's source; per-widget skeletons, not a full reload.
- **Total error (no cache):** centered `danger` hero + `تلاش مجدد` — only when everything fails with no cache; otherwise partial per-card errors.
- **Offline:** neutral `OfflineBanner` pins at top; all cards show cached data + last-updated; `تأیید`/`تکمیل`/`رد` queue; reconnect → "بازگشت به شبکه" toast + flush.

---

## 3. Screen C — Growth: Empty business (verified, no traction)

**Resolved `HomeContext`:** `system=OK · maturity=GROWTH · day=NO_APPTS · availability=OPEN`.
**Goal:** get discovered → drive the first booking. **Hero:** GetDiscovered. Agenda muted.
**Distinct from Setup:** business is *live* (verified), so this is acquisition-led, not setup-led — no pending banner, no checklist; the job is "get clients," not "finish setup."

### 3.1 Composition
```
[ HomeAppBar — light ]
────────────────────────────────────────────────
[ ① GetDiscovered  ★HERO ]          prio 2
[ ② TodayAgenda — muted empty ]     prio 4
[ ③ ComingUpPeek — elevated empty ] prio 3.5
                ( ⊕ CreateAction )
```

### 3.2 ① GetDiscovered — the hero
`AppCard`, dominant. Same card grammar as ActivationChecklist (§1.3) — a header, a progress affordance, action rows — so the two maturity heroes feel like one family.
```
┌─ AppCard ────────────────────────────────────────┐
│ کسب‌وکار شما آماده است ✓                          │  ink 17/700
│ برای دریافت اولین نوبت، لینک رزرو را به‌اشتراک بگذارید│  muted 13
│                                                    │
│        [  🔗  اشتراک‌گذاری لینک رزرو  ]            │  primary filled (constrained)
│                                                    │
│ تکمیل پروفایل  ▓▓▓▓▓▓▓░░░  ۷۰٪                    │  progress: success fill
│  ⭕ افزودن تصاویر گالری  ›   (پروفایل جذاب‌تر)      │  todo row → openGallery
└────────────────────────────────────────────────────┘
```
- **Title:** *"کسب‌وکار شما آماده است"* (your business is ready) with a `success` ✓.
- **Primary CTA:** `اشتراک‌گذاری لینک رزرو` (share booking link) — `primary` filled, the single loudest action → opens native share sheet.
- **Completeness meter:** *"تکمیل پروفایل ۷۰٪"* (profile 70%) with `success` fill; below it, only the *remaining* gaps as tappable rows, each with a one-line "why it helps."
- **Secondary:** `+ افزودن نوبت دستی` (add a manual appointment) as a text button — immediate app value while waiting for inbound bookings.
- **Success:** first booking received → Home re-composes toward Traction (agenda populates, GetDiscovered demotes). Completeness reaching 100% → the meter block collapses, CTA remains.
- **Error:** link unavailable (e.g. not yet public) → inline explanation + corrective step, CTA disabled with reason. **Offline:** share disabled with reason; completeness cached.

### 3.3 ② TodayAgenda / ③ ComingUpPeek
Identical to §1.5 (muted positive empty agenda) and §1.4 (elevated empty peek) — reused verbatim.

---

## 4. Screen D — No appointments today (established, quiet day)

**Resolved `HomeContext`:** `system=OK · maturity=OPERATIONAL · day=NO_APPTS · availability=OPEN`.
**Goal:** confirm nothing's missed; fill the day or use it well. **This is NOT Growth** — the provider has history; the empty is *restful*, not *anxious*, and points to the next real appointment.

### 4.1 Composition
```
[ HomeAppBar — light ]
────────────────────────────────────────────────
[ ① TodayAgenda — positive empty ]  prio 4→ (elevated to top-of-content)
[ ② ComingUpPeek — ELEVATED ]        prio 3.5
[ ③ SetupNudges — optional ]         prio 8
                ( ⊕ CreateAction )
```

### 4.2 ① TodayAgenda — positive empty (the defining element)
```
┌─ AppCard ──────────────────────────────────────┐
│                  🌤  (hero 72, soft tint)        │
│            امروز نوبتی ندارید                    │  ink 16/600 centered
│        نوبت بعدی: فردا ۱۰:۰۰ — سارا محمدی        │  muted 13 — the key line
│     [ + افزودن نوبت ]      [ مشاهده تقویم ]       │  two constrained buttons
└──────────────────────────────────────────────────┘
```
- **Framing:** calm, not sad. Weather-ish glyph, `success`/soft tint (never `danger`).
- **The key line:** *"نوبت بعدی: فردا ۱۰:۰۰"* (next appointment: tomorrow 10:00) — turns "empty" into "here's what's next." Omitted only if truly no future bookings.
- **Actions:** `+ افزودن نوبت` (fill the day) + `مشاهده تقویم` (view calendar).

### 4.3 ② ComingUpPeek — elevated
Since today is empty, this rises (prio 3.5) and expands slightly: *"این هفته ۱۲ نوبت"* (12 appointments this week) + next few, tap → Calendar.

### 4.4 ③ SetupNudges — optional
Only if a genuine gap exists (e.g. *"۲ خدمت بدون قیمت"* / 2 unpriced services). Low priority, dismissible. **No review nudges** (reviews out of MVP). Otherwise collapses.

---

## 5. Screen E — Vacation mode

**Resolved `HomeContext`:** `system=OK · availability=VACATION`.
**Goal:** confirm the pause is active + not bookable; know when it ends; resolve any bookings inside the window; resume easily. Vacation is **backend-managed availability** the Home consumes.

### 5.1 Composition
```
[ HomeAppBar — light ]
────────────────────────────────────────────────
[ ① VacationBanner ]                 prio 1  (distinct calm tint)
[ ② BusinessAlerts — ELEVATED ]      prio 3  (bookings inside window — needs action)
[ ③ TodayAgenda — muted ]            prio 4
[ ④ ComingUpPeek — after vacation ]  prio 6
                ( ⊕ CreateAction — override allowed )
```

### 5.2 ① VacationBanner
Banner grammar (§1.2) with a **distinct, calm** tint (a muted teal/neutral, not warning-yellow — vacation is chosen, not an error). New token `vacationSoft` (proposed).
```
┌─────────────────────────────────────────────────┐
│ 🌴  در حالت مرخصی هستید                          │  ink 15/600
│     تا ۲۵ مرداد · نوبت جدید پذیرفته نمی‌شود        │  muted 13 (end date, Persian/Jalali)
│           [ ویرایش ]   [ پایان مرخصی ]           │  text btn · primary text btn
└─────────────────────────────────────────────────┘
```
- Copy: *"در حالت مرخصی هستید"* (you're on vacation) · *"تا ۲۵ مرداد · نوبت جدید پذیرفته نمی‌شود"* (until 25 Mordad · not accepting new bookings) — **Jalali date**.
- Actions: `ویرایش` (edit/extend) · `پایان مرخصی` (end vacation).
- **Success:** vacation ends → banner flips to `successSoft` *"به کار بازگشتید — نوبت‌ها دوباره فعال شد"* (welcome back — bookings active again), then removes; normal day-context resumes.

### 5.3 ② BusinessAlerts — elevated (only if bookings fall in the window)
Row grammar of ActionQueue (§2.3), `warning`-accented — these need action.
```
┌─ AppCard ────────────────────────────────────────┐
│ ⚠ نوبت‌های داخل بازهٔ مرخصی            ۲          │  ink 16/700 · count
│ ─────────────────────────────────────────────────│
│ سارا محمدی · ۲۲ مرداد ۱۴:۰۰                       │
│   [ اطلاع به مشتری ] [ جابه‌جایی ] [ لغو ]        │  constrained buttons
└────────────────────────────────────────────────────┘
```
- Copy: *"نوبت‌های داخل بازهٔ مرخصی"* (appointments within your vacation) — actions `اطلاع به مشتری` (notify) · `جابه‌جایی` (reschedule) · `لغو` (cancel).
- Collapses when none exist.

### 5.4 ③/④ Agenda muted · ComingUpPeek → after vacation
Agenda shows *"در مرخصی تا ۲۵ مرداد"* muted; ComingUpPeek previews the first day back.

---

## 6. Screens F & G — Closed today · Fully booked (concise)

### 6.1 Screen F — Closed today  (`availability=CLOSED_TODAY`)
Same grammar as Vacation but for a *scheduled non-working day* (backend availability), calmer, no "not accepting bookings."
```
[ ① ClosedTodayBanner ]  🌙 امروز تعطیل است · باز: شنبه   [ باز کردن استثنائی ] [ ویرایش ساعات ]
[ ② BusinessAlerts ]     only if a booking leaked onto the closed day (anomaly → resolve)
[ ③ ComingUpPeek — ELEVATED ]  next open day's agenda preview
[ ④ TodayAgenda — muted ]      "امروز تعطیل" empty
```
- Banner copy: *"امروز تعطیل است · باز: شنبه"* (closed today · open: Saturday). Actions: `باز کردن استثنائی` (open exceptionally) · `ویرایش ساعات` (edit hours).

### 6.2 Screen G — Fully booked  (`day=FULLY_BOOKED`)
A variant of the Active day (§2), not a new layout:
- Header chip on TodayAgenda: `تکمیل ظرفیت · ۱۲ نوبت` (fully booked · 12).
- **NowNext + dense TodayAgenda** are the heroes; breaks visually protected.
- **ActionQueue** routes new demand to `لیست انتظار` (waitlist) / `رد با پیشنهاد جایگزین` (decline-with-alternative) instead of confirm-into-slot.
- `⊕ CreateAction` → New appointment **warns** "امروز ظرفیت خالی ندارید" (no free slots today) before an explicit override.
- Completion of a packed day → a stronger EndOfDaySummary (§2.4).

---

## 7. Coverage map — all 10 states specified

| # | State | Spec | Built from |
|---|-------|------|-----------|
| 1 | First login (Setup) | §1 | PendingBanner + ActivationChecklist + muted agenda |
| 2 | Empty business (Growth) | §3 | GetDiscovered + muted agenda |
| 3 | No appointments today | §4 | TodayAgenda positive-empty + elevated peek |
| 4 | Pending verification | §1.2 (banner over any body) | PendingVerificationBanner |
| 5 | Active business day | §2 | ActionQueue / NowNext / TodayAgenda (mode-ordered) |
| 6 | Fully booked | §6.2 | Active-day variant + waitlist routing |
| 7 | Closed today | §6.1 | ClosedTodayBanner + elevated peek + muted agenda |
| 8 | Offline | §1.7 / §2.7 | OfflineBanner overlay + cached cards |
| 9 | Error | §1.7 / §2.7 | localized per-card / centered total-failure |
| 10 | Vacation | §5 | VacationBanner + BusinessAlerts |

The anchor set (§1–§2) defined the **component grammar**; every other state is a composition of it — the payoff of the widget architecture. All banners share one grammar (§1.2); all actionable lists share ActionQueue's row grammar (§2.3); all empties share the four-state recipe (§0.4).

---

## 8. Open design points for your review

1. **Light vs. blue Home app bar** (§0.3) — I've specified light for a content-first workspace; confirm or override for brand consistency.
2. **Confirm/decline emphasis & placement** (§2.3) — confirm as the filled primary on the RTL-left; acceptable, or should decline be lower-emphasis text only?
3. **EndOfDaySummary depth** (§2.4) — a light celebratory card now; do you want a small day-stat (count only, per "workflows over stats"), or nothing beyond the message?
4. **Persian microcopy** — all strings above are proposed; they'll be finalized in `AppStrings` (fa-IR) during build. Flag any wording.

---

*All 10 Home states now have a concrete screen spec (§7 coverage map). On approval, the next step is the **build change**: `HomeContext` resolver + `HomeWidget` contract + registry + thin orchestrator, then widgets implemented incrementally behind `/dashboard`, replacing the placeholder `ProviderDashboardPage`. No Flutter is written until that change is opened.*
