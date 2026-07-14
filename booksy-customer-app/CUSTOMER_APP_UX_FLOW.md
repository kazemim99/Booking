# Booksy Customer App — UX Flow & Redesign Audit

## App Purpose

**Customer-Only Booking App**
- For: People who want to book beauty/wellness services (Persian-first, RTL, Jalali dates)
- Not for: Service providers (they use the web app)

This document records the July 2026 screen-by-screen UX audit and the redesign implemented under the OpenSpec change `customer-app-ux-redesign` (see `openspec/changes/customer-app-ux-redesign/` for proposal, design decisions, specs, and API-parity findings).

---

## Current UX Flow (Browse-First, Router-Driven)

```
Cold start
  └─ SplashPage (branding only; router holds until stored session resolves)
       └─ /home  ← guests and authenticated users both land here
            ├─ Home tab ......... search entry → next-booking card → categories
            │                     → top providers → promotions → recent/favorites
            ├─ Explore tab ...... debounced search-as-you-type + category chips
            ├─ Appointments tab . upcoming/past cards (guests: login prompt in place)
            └─ Profile tab ...... profile + edit + logout (guests: login in place)

Booking journey (guest-friendly until the last step)
  provider card → /providers/:id (detail) → رزرو نوبت
    → service → staff (auto-skipped if ≤1) → Jalali day + slot → confirm
    → [guest? phone/OTP login, selections preserved] → success → appointments
```

Auth is demanded only at: booking confirmation, appointments tab, profile tab, appointment detail (deep link). Post-login return-to-intent is handled by the router (`?redirect=`).

---

## Screen-by-Screen Audit Outcome

### Splash
- **Before**: Navigated imperatively from a BlocListener; guests and users could see flashes of the wrong screen; purple `primarySwatch` theme.
- **After**: Branding-only; router's `AuthNotifier` latches session resolution — no login flash, no double navigation. *(Priority: High — Frontend)*

### Login
- **Before**: Generic `TextFormField`, submit-only validation, Persian digits rejected, snackbar-only errors, hard-coded strings/styles.
- **After**: `AppTextField` with on-blur inline validation, live Persian/Arabic digit normalization, phone keyboard + autofill hints, loading CTA, terms notice, usable embedded (tabs) or routed (`?redirect=`). *(Priority: Critical — Frontend)*

### OTP Verification
- **Before**: Plain 6-char text field, no resend timer, no autofill, navigation by `pushAndRemoveUntil(HomePage)` which bypassed the shell.
- **After**: Segmented `OtpInput` (pinput) with paste + platform SMS autofill, auto-submit on completion, 60s resend countdown with confirmation snackbar, edit-number returns pre-filled, inline error clears + refocuses. Navigation is router-redirect driven. *(Priority: Critical — Frontend)*

### Main Navigation
- **Before**: Hand-rolled `IndexedStack`, inline hex colors, Android back exited from anywhere, no deep links, `go_router` installed but unused.
- **After**: `StatefulShellRoute.indexedStack` — per-tab back stacks and scroll preservation, back falls to home tab then exits, all destinations deep-linkable, Material 3 `NavigationBar` themed from tokens, offline banner mounted in shell. *(Priority: Critical — Frontend)*

### Home
- **Before**: Single spinner for the whole screen, one section (recent/favorites), duplicate implementations (`home_page.dart` vs `home_page_new.dart`), dead search bar.
- **After**: One implementation; content-shaped skeleton; sections load/fail independently with inline per-section retry; order: search entry → next-booking card (Jalali, tap → detail) → category chips (deep-link to explore) → top providers → promotions → recent/favorites; pull-to-refresh. *(Priority: High — Frontend)*

### Explore
- **Before**: Static category tabs over a permanent "no results" placeholder; search field not wired; fake filters ("کجا؟/کی؟") that did nothing.
- **After**: Live debounced search (350ms) with stale-result race guarded in the bloc, category filter chips, image result cards with placeholders, skeleton loading, no-results empty state with clear-filters CTA, error retry, pull-to-refresh, `?category=` deep link. Dead filters removed rather than left misleading. *(Priority: Critical — Frontend)*

### Provider Detail (new screen)
- Deep-linkable `/providers/:id`; header image with branded placeholder, rating/reviews, address, working hours, services with duration+price; booking CTA pinned so it never scrolls away. *(Priority: Critical — Frontend)*

### Booking Flow (new journey)
- Full-screen steps (not desktop-style modals): service → staff (auto-skipped when ≤1; «فرقی نمی‌کند» supported via slot-assigned staff) → 14-day Jalali day browser + slot chips (per-day skeleton, no stale slots) → confirmation summary (nothing committed before explicit confirm) → success screen.
- Slot-taken conflict returns to the slot step with refreshed availability and selections intact.
- Guest checkout: login at confirmation; selections survive the round-trip (app-scoped bloc). *(Priority: Critical — Frontend)*

### Appointments
- **Before**: Static empty states only; "یافتن سالن" buttons were dead TODOs; no data wiring at all.
- **After**: Upcoming/past segmentation, status cards (`StatusBadge`: label+icon+tint — never color alone), cancel via destructive `ConfirmSheet` with optimistic update + rollback on failure, reschedule reusing the shared slot picker (same endpoint semantics as web), pull-to-refresh, guest login prompt in place, appointment detail screen. *(Priority: Critical — Frontend)*

### Profile (new screen)
- **Before**: `Text('Profile Page - TODO')`.
- **After**: Identity card (name + LTR phone), edit-profile bottom sheet against `PUT /Customers/{id}`, logout with confirmation; guests see login in place and the tab swaps after auth. *(Priority: High — Frontend)*

---

## Global Scores

| Dimension | Before | After |
|---|---|---|
| UX | 3/10 — core journeys missing or dead-ended | 7.5/10 — all journeys complete; polish pending device pass |
| UI | 4/10 — purple/blue theme conflict, inline styles | 8/10 — token-driven M3 theme, one component library |
| Accessibility | 2/10 — no semantics, `.sp` fought OS font scale, AA failures | 7.5/10 — AA-verified tokens, 48dp targets, semantics, reduced-motion, 1.3× tested |
| Consistency | 3/10 — every screen self-styled | 8.5/10 — `StateSwitcher` + shared components enforced |
| Product maturity | 2.5/10 — 7 screens, 3 features empty | 7/10 — MVP-complete pending E2E device verification |

## Roadmap Status

- **Phase 1 (Critical)** — theme unification, navigation shell, auth flow, explore search, booking journey, appointments actions: ✅ implemented.
- **Phase 2 (High)** — home sectioning, provider detail, profile, offline handling: ✅ implemented.
- **Phase 3 (Polish)** — deferred (below).

## Deferred Findings

1. **Promotions**: no backend endpoint; section hidden until one ships.
2. **Staff-per-service filter**: API returns all provider staff; slot availability is the effective filter. A dedicated endpoint would improve the staff step.
3. **Gallery**: provider detail uses the single profile image; multi-image gallery blocked on gallery endpoints (see `GALLERY_BACKEND_REQUIREMENTS.md`).
4. **codegen**: retrofit_generator incompatible with current Dart SDK — new code uses manual JSON + manual DI; upgrade tracked as tech debt.
5. **Dark mode**: tokens structured for it (`AppColors.*Dark` exist); not shipped.
6. **i18n**: all strings centralized in `AppStrings`; extraction to ARB is now mechanical.
7. **Slot-taken status code** (409/422 assumed): verify against the real backend during the device E2E pass.
8. **Reviews, notifications, payments**: out of scope until backend surfaces exist.

---

**Last Updated**: July 14, 2026
**Status**: Redesign implemented (`customer-app-ux-redesign`); pending on-device E2E sweep
**App Type**: Customer booking app (not provider management)
