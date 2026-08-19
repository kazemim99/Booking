# Final Visual QA — Booksy Provider reskin vs ColiRide Figma

**Date:** 2026-07-21 · **Verdict: PASS with scope caveats (below)**

## Method

- Full stack run locally (Postgres/Redis in Docker, `Booksy.Host` on :5000 with
  `OTP_SANDBOX_CODE`, Flutter web on :8765), logged in as a fully-onboarded
  seeded provider through the real OTP flow.
- Every screen captured headless at **375×794@2x — the Figma frame size** — so
  measurements compare 1:1. Colors and geometry verified by **pixel-sampling
  the PNGs**, not by reading tokens: token-correct code had already produced a
  wrong render once (see Critical-1), which is exactly why this pass validated
  output, not source.
- Reference: Figma file `7B8fFnJGVK2Dr65ymZQyVw`, nodes `11694-35866`
  (Profile) and `13461-48334` (sub-page anatomy).

### Scope caveats (honesty section)

- **Web renderer on Windows only.** No Android emulator or iOS simulator
  exists on this machine (iOS requires macOS). Platform-specific behavior
  (safe-area insets on notched devices, iOS swipe-back, Impeller rendering)
  is **not verified**; the widget tests cover semantics/RTL but not those.
- **Animations/transitions and touch-feedback timing** were smoke-checked
  (no exceptions, ink present on rows via `Material` sheet) but not
  frame-profiled. `AppMotion` tokens are our own convention — ColiRide defines
  none (DESIGN_LANGUAGE §9.2).
- States verified: populated, empty, loading (skeleton), error (retry) on
  Home/Calendar/lists via the test suite; visually spot-checked, not
  screenshot-diffed per state.

## Issues found in this pass — all fixed

| # | Severity | Screen(s) | Finding | Fix |
|---|---|---|---|---|
| 1 | **Critical** | Home, Calendar, Clients (all `AppPageScaffold` pages with the nav pill) | Nav pill rendered as a **full-width blue bar** — the blue scaffold background filled its gutters and bottom inset. Figma: white at x<16, pill floats. Introduced by the reskin itself (More hub, still on a white scaffold, was unaffected — which pinpointed it). | `AppPageScaffold` wraps `bottomNavigationBar` in a white `ColoredBox`. Verified by re-sampling: white at x=4–10, blue starts x=16, white below pill — matches Figma exactly. |
| 2 | Major | Home (empty agenda), Calendar (empty day) | Add-appointment action was a filled **blue pill**; design's empty-state add affordance is the **green circle-plus + green label**. | Both switched to `AppEmptyState.add`. |
| 3 | Major (UX, user-reported) | Home masthead | Greeting + account holder's name (fell back to phone-derived names) instead of the **business name**. | Header now shows business name (bold white 16) over the greeting (12 white70); avatar initial derives from it. Identity fetched per snapshot. |
| 4 | Major (trust, user-reported) | Home | "کسب‌وکار شما آماده است" (Growth hero) rendered while the pending-verification banner said the business is under review — contradictory; share CTA handed out a dead link. | `GetDiscovered` is pending-aware: swaps to «پروفایل شما کامل است» + hourglass, disables share until approval. Resolver's maturity/lifecycle decoupling untouched (it's spec'd and test-guarded). |
| 5 | Major (data, user-reported) | Home | Profile completeness hardcoded to 0%; Setup checklist done-flags hardcoded. | New `HomeIdentity` (businessName + 4 signals) fetched in parallel with existing snapshot calls (no added wall-clock); completeness = 25%/signal (description, services, staff, gallery — share excluded as unobservable). Checklist reads live flags. |
| 6 | Minor (UX, user-reported) | Services, Staff, Holidays | Only add affordance on populated lists was the small green chrome icon — missed on first use. | Green `+` add-link row pinned atop each populated list (ColiRide's list-add pattern); chrome icon retained. |

## Per-screen verdicts

| Screen | Chrome | Sheet r16 | Palette | Nav pill | Verdict |
|---|---|---|---|---|---|
| Login / OTP | themed blue | ✓ | ✓ | n/a | **Production-ready** |
| Home | blue, business-name masthead | ✓ | ✓ (sampled) | floats on white ✓ | **Production-ready** |
| Calendar | blue + week strip in chrome (inverted: selected day = white pill) | ✓ | ✓ | ✓ | **Production-ready** — note: week strip is our design judgement; Figma has no week-strip equivalent |
| Clients | blue, count in white70 | ✓ | ✓ | ✓ | **Production-ready** |
| More hub | tall chrome + identity | ✓ | ✓ | ✓ | **Production-ready** — bottom list padding clears the pill |
| Services / Staff / Holidays / Hours / Gallery / Insights | blue via `AppPageScaffold` | ✓ | ✓ (sampled vs node `13461-48334`) | n/a (pushed) | **Production-ready** |
| Composer | blue slim chrome | ✓ | ✓ | n/a | **Production-ready** |
| Onboarding wizard | themed blue | ✓ | ✓ | n/a | **Production-ready** (unchanged by reskin) |

**Not comparable to Figma (functional gaps, deliberate — see FUNCTIONAL_GAPS.md):**
notification bell + unread badge (no backend), Availability driver/passenger
tabs (N/A by domain), profile section-edit screens (product decision pending),
**multi-service appointments** (backend contract is single-service — newly
documented as gap #4).

## Test evidence

- `flutter analyze`: clean.
- `flutter test`: **373 passed** (1 pre-existing skip), including new
  regression tests for: the pill-on-white fix location, green add affordances,
  pending-aware Growth copy + disabled share, identity fetch (happy path +
  endpoint-failure degradation), live checklist flags, masthead business name
  + fallback, and the three list add-rows.
- Zero browser console errors across the full captured flow (login → OTP →
  all tabs → all sub-pages → composer).
