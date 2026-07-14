# Design: customer-app-ux-redesign

## Context

`booksy-customer-app` is a Flutter app (BLoC + Clean Architecture, Retrofit/Dio, get_it) targeting Persian-speaking users: forced RTL, `fa_IR` locale, Vazir font, `shamsi_date` for Jalali dates. It consumes the same `/api/v1` backend as the Vue web app.

Current state (verified in code):

- **7 screens exist**: splash, login, OTP, main-navigation shell, home, explore, appointments. `features/booking` and `features/profile` are empty scaffolds; the Profile tab renders a `Text('Profile Page - TODO')`.
- **Theme is contradictory**: `main.dart` uses `primarySwatch: Colors.purple` + a 3-entry `TextTheme`, while `config/theme/app_colors.dart` defines a dark-blue (#1A365D) professional palette that screens use directly. Screens also hardcode hex values inline (e.g., the bottom nav bar).
- **Navigation is hand-rolled**: `go_router` is in pubspec but unused; `config/routes/` is empty. `MainNavigationPage` swaps an `IndexedStack`; auth pages are pushed imperatively. No deep links, no per-tab back stacks, Android back button exits from anywhere.
- **Duplicate home**: `home_page.dart` and `home_page_new.dart` coexist; the shell uses `HomePageNew`.
- **`core/widgets/` is empty** — no shared components; every screen styles its own buttons/cards/states.
- **Useful deps already present but underused**: `shimmer`, `cached_network_image`, `connectivity_plus`, `pull_to_refresh`, `flutter_screenutil`.

Constraints: no backend/API changes; preserve the BLoC + Clean Architecture layering; web app (`booksy-frontend`) is the flow-parity reference for auth (sandbox OTP), booking, and cancel/reschedule semantics.

## Goals / Non-Goals

**Goals:**

- One source of truth for visual design: tokens → `ThemeData` → components; zero inline hex/ad-hoc text styles in screens.
- A reusable component library in `core/widgets` covering the states every screen needs (loading skeleton, empty, error, offline, success feedback).
- Deep-linkable, back-stack-correct navigation via go_router.
- Redesigned auth, home, explore, and appointments screens; new provider-detail → booking flow and profile screens matching web-app semantics.
- WCAG-aligned accessibility baseline: contrast, ≥48dp touch targets, font scaling, Semantics labels, reduced motion.

**Non-Goals:**

- No backend, API, or web-frontend changes (gaps get documented as findings only).
- No payments, notifications-center, reviews-authoring, or support/chat screens — the backend surface for these isn't ready; specs cover only what the existing API supports.
- No full localization pass (English UI); the app stays fa_IR/RTL-first, but nothing may *break* under LTR.
- No dark mode shipping in this change (tokens must be structured so it can be added later — `AppColors` already sketches dark variants).

## Decisions

### D1. Material 3 `ThemeData` built from existing tokens, purple swatch removed

Build a complete `ColorScheme` + `TextTheme` + component themes (`ElevatedButtonTheme`, `InputDecorationTheme`, `CardTheme`, `BottomSheetTheme`, `NavigationBarTheme`, …) from `AppColors`/`AppTextStyles`, set once in `main.dart`. Screens consume `Theme.of(context)`; direct `AppColors.*` use is allowed only inside the theme and component library.

- *Why*: the purple `primarySwatch` visibly conflicts with the dark-blue brand; inline hex in screens is the root cause of inconsistency. Centralizing in ThemeData is the idiomatic Flutter mechanism — components then inherit correct styling for free.
- *Alternative considered*: keep per-screen `AppColors` usage and just delete the purple swatch. Rejected: doesn't fix drift; every new screen re-invents styles.

### D2. Adopt go_router with `StatefulShellRoute` for the tab shell

Replace the `IndexedStack` shell with go_router's `StatefulShellRoute.indexedStack` (preserves per-tab state, gives per-tab back stacks), declare all routes in `config/routes/app_router.dart`, and add redirect logic for auth-gated routes. Provider detail (`/providers/:id`), booking flow, and appointment detail become deep-linkable.

- *Why*: go_router is already a dependency (v13) and the empty `config/routes/` shows this was the intent. Correct Android back behavior and deep links are impossible to retrofit cleanly onto manual `Navigator.push` + IndexedStack.
- *Alternative considered*: keep IndexedStack and add `PopScope` handling. Rejected: still no deep links, auth redirects stay scattered in widgets.

### D3. Component library in `core/widgets`, built in-house on Material 3

Initial set: `AppButton` (primary/secondary/text/destructive, loading state, min 48dp), `AppTextField`, `OtpInput`, `AppCard`, `StatusBadge` (booking statuses, semantic colors), `SkeletonLoader` (shimmer-based), `EmptyState`, `ErrorState` (retry action), `OfflineBanner` (connectivity_plus), `AppBottomSheet` (drag handle, safe-area), `ConfirmSheet` (destructive confirmation), `AppSnackbar` (success/error/undo). Each widget wraps Material with token styling and required `Semantics`.

- *Why*: the state-handling widgets (skeleton/empty/error/offline) are the highest-leverage consistency fix — every spec requires them. Wrapping Material (not drawing from scratch) keeps a11y and platform behavior for free.
- *Alternative considered*: third-party UI kit (e.g., shadcn-like Flutter kits). Rejected: RTL/Vazir/Jalali needs and the small component count don't justify an external dependency.

### D4. Standardized screen-state pattern on top of BLoC

Each screen bloc exposes the same state shape (loading / loaded / empty / error), rendered through one `StateSwitcher` widget that maps states to `SkeletonLoader`/content/`EmptyState`/`ErrorState`. Pull-to-refresh uses Flutter's built-in `RefreshIndicator`; the stale `pull_to_refresh` package is dropped.

- *Why*: appointments, home, and explore each currently improvise loading/error rendering. One pattern means specs can require states declaratively and reviews can enforce them.
- *Alternative considered*: keep `pull_to_refresh`. Rejected: unmaintained (last release 2021), built-in `RefreshIndicator` suffices and honors Material 3 theming.

### D5. Home consolidation: `home_page_new.dart` wins, then is renamed

The shell already uses `HomePageNew`; it becomes `home_page.dart` (old file deleted) and is refactored onto the component library. Section order: search entry → upcoming-booking card (if any) → categories → top providers → promotions.

- *Why*: upcoming booking is the highest-intent content for a returning user (web app leads with it too); the duplicate file is dead weight and a contributor trap.

### D6. Booking flow as a stepped bottom-sheet-free page flow

Provider detail → service selection → staff (optional) → Jalali date + slot picker → confirmation summary → success. Each step is a route (deep-linkable, restorable), not a mega-bottom-sheet. Cancel/reschedule from appointments use `ConfirmSheet` with explicit consequence text; reschedule reuses the slot-picker step, matching the web `RescheduleBookingModal` semantics and the same endpoints.

- *Why*: full-screen steps beat sheets for a 4–5 step mobile journey (keyboard, scroll, back-button semantics); the web app's modal-based flow is a desktop pattern we deliberately do *not* copy.
- *Alternative considered*: single-screen booking configurator. Rejected: too much cognitive load on 375dp width; steps give clear progress and error isolation.

### D7. Auth UX: `pinput` for OTP + platform autofill, guest-first tabs

Login/OTP get inline validation, a resend countdown, and OTP auto-read (`Pinput` with `autofillHints: [AutofillHints.oneTimeCode]` on iOS, SMS Retriever/User Consent on Android). Browsing (home/explore/provider detail) stays guest-accessible; auth is demanded only at booking confirmation and the appointments/profile tabs — the existing "Profile tab shows LoginPage for guests" pattern is kept but implemented via go_router redirects.

- *Why*: demanding login up-front is the single biggest conversion killer in booking apps; the backend's OTP flow (same as web sandbox mode) already supports this ordering.
- *Alternative considered*: hand-rolled OTP boxes (current). Rejected: no autofill, weak paste handling, a11y gaps — `pinput` is the de-facto standard and tiny.

### D8. Accessibility & RTL as spec-level requirements, not polish

Every component ships with: ≥48×48dp touch target, contrast-checked colors (AppColors already targets this — verify with tooling), `Semantics` labels in Persian, support for `MediaQuery.textScaler` up to 1.3 without overflow (ScreenUtil's `minTextAdapt` stays, but no `.sp` on body text that must scale), and `MediaQuery.disableAnimations` honored by all transitions. Directionality-sensitive icons (back arrows, chevrons) must use direction-aware variants.

- *Why*: retrofitting a11y is 10× the cost; baking it into the component library makes every screen inherit it.

## Risks / Trade-offs

- **[Navigation rewrite touches every screen]** → Land D2 as its own early task with the existing screens unchanged inside the new shell; screen redesigns then proceed independently per feature.
- **[go_router redirect + BLoC auth-state coupling can flicker on cold start]** → Splash stays the initial route until `CheckAuthStatusEvent` resolves; router `refreshListenable` bound to the auth bloc stream.
- **[ScreenUtil `.sp` scaling can fight OS font scaling]** → Rule: `.w/.h/.r` for layout only; text sizes come from `TextTheme` and respect `textScaler`. Enforced in component library, checked in review.
- **[No booking-flow API for staff selection granularity may differ from web]** → Verify endpoint parity against `API_ENDPOINTS.md` during implementation; any gap becomes a documented finding, and the step is hidden when a provider has a single staff member anyway.
- **[Scope creep: "audit everything" vs. 7 real screens]** → Specs fix the scope: five capabilities, listed screens only; new journeys limited to provider-detail/booking/profile which the API already supports.
- **[Persian-only copy hardcoded in widgets]** → Trade-off accepted for this change (no i18n pass), but all user-facing strings move to a single `app_strings.dart` so extraction later is mechanical.

## Migration Plan

1. **Foundation (no visual change)**: ThemeData from tokens, delete purple swatch; component library + `StateSwitcher`; `app_strings.dart`.
2. **Shell**: go_router + `StatefulShellRoute`, auth redirects, existing screens mounted as-is; delete `home_page.dart` duplicate.
3. **Screen redesigns** (independent, per capability): auth flow → home → explore → appointments.
4. **New journeys**: provider detail → booking steps → profile tab.
5. **A11y/polish pass**: contrast verification, textScaler sweep, reduced-motion, semantics audit.

The app is unreleased, so there is no rollback concern beyond git; each phase merges independently behind a compiling app (`flutter analyze` + widget tests green).

## Open Questions

- **Slot-picker data source**: does the availability endpoint return day-granular slots suitable for a mobile picker, or does the app need client-side chunking like the web `RescheduleBookingModal` does? (Resolve from `API_ENDPOINTS.md` in tasks phase.)
- **Guest checkout**: web requires an authenticated customer to book; confirm whether OTP-at-confirmation creates the customer record in one step or needs the separate registration call.
- **Lottie**: `assets/lottie/` is declared but no `lottie` package is installed — adopt it for empty/success states or drop the asset folder?
- **pgAdmin-style provider imagery**: provider photos come from the gallery endpoints; confirm image URL shape/CDN sizing params for `cached_network_image` placeholders.
