# Proposal: customer-app-ux-redesign

## Why

The Flutter customer app (`booksy-customer-app`) lags far behind the Vue web app in UX maturity: only seven screens exist (splash, login, OTP, navigation shell, home, explore, appointments), core journeys (provider detail, booking creation, reschedule/cancel, profile, reviews) are missing or stubbed, and what exists has no shared component library, duplicate implementations (`home_page.dart` vs `home_page_new.dart`), and no defined loading/empty/error/offline states. A screen-by-screen UX audit plus a proper mobile design system is needed before the app can ship as a polished, world-class product.

## What Changes

- **UX/UI audit**: Critically evaluate every existing screen (splash, login, OTP verification, main navigation, home, explore/search, appointments) against Material 3 / Apple HIG / WCAG and the web app's equivalent flows; document strengths, weaknesses, and prioritized fixes with scores and a phased roadmap.
- **Mobile design system**: Establish a single source of truth for colors, typography, spacing, elevation, radius, icons, and reusable components (buttons, inputs, cards, lists, bottom sheets, dialogs, snackbars, skeletons) in `lib/core/widgets` + `lib/config/theme`, replacing ad-hoc per-screen styling. Remove the duplicate home page implementation.
- **Auth flow redesign**: Streamline splash → phone login → OTP with proper keyboard behavior, autofill/OTP auto-read, inline validation, resend timer, and clear error recovery — mirroring the web app's sandbox-OTP flow adapted for touch.
- **Home & discovery redesign**: Rework home (upcoming booking card, categories, top providers, promotions) and explore/search (search-as-you-type, filters, empty/no-results states) for thumb-first use with skeleton loading and image placeholders.
- **Booking & appointments UX**: Define the mobile booking journey (provider detail → service selection → slot picker → confirm) and upgrade the appointments screen with status-driven cards, cancel/reschedule actions with confirmation + undo patterns, and empty states — reusing the web app's booking/reschedule semantics (same API).
- **App-shell & cross-cutting UX**: Bottom navigation with correct back-stack behavior, standardized loading/empty/error/success/offline states, pull-to-refresh, accessibility (contrast, 48dp touch targets, font scaling, screen readers, reduced motion), and micro-interaction guidelines.

No backend or API contract changes; the app consumes the existing `/api/v1` surface. Gaps discovered during the audit (e.g., missing mobile-friendly endpoints) are documented as findings, not implemented here.

## Capabilities

> **Capability naming (revised 2026-08-19, per `OPENSPEC-AUDIT-2026.md` finding F3).** This change was
> originally authored against five new `mobile-*` capabilities. Four of them collided directly with the
> `customer-app-*` / `customer-*-journey` capabilities that the archived
> `unify-customer-app-with-provider-design` change had meanwhile promoted into `openspec/specs/`. Promoting
> both families would have described the customer app twice. The deltas are therefore folded into the
> existing family, and narrowed to what those specs do not already cover.

### New Capabilities

- `customer-app-auth`: Splash, phone login, and OTP verification behavior — validation, keyboard/autofill
  handling, error recovery, session restore, and login-at-the-point-of-need. (Was `mobile-auth-ux`; it is
  the one delta with no existing counterpart — `specs/authentication` covers the *Vue provider* login.)

### Modified Capabilities

All additive (`ADDED` requirements only) — no existing requirement is replaced or removed:

- `customer-app-visual-tokens` (was part of `mobile-design-system`): theme as the single styling source,
  spacing scale + dark-theme-ready structure, RTL-first rendering, centralized strings.
- `customer-app-component-styling` (was part of `mobile-design-system`): the shared component library as the
  only styling surface, plus the component accessibility baseline.
- `customer-app-chrome-styling` (was `mobile-app-shell-ux`): router-driven shell, back behavior,
  standardized async states, offline awareness, mutation feedback, app-wide accessibility.
- `customer-booking-journey` (was `mobile-booking-ux`): step progress/back-preservation, day-browser
  availability indication, provider-detail deep linking and image fallbacks, appointments list, cancel,
  reschedule.
- `customer-discovery-journey` (was `mobile-discovery-ux`): home content hierarchy, pull-to-refresh,
  search-as-you-type with in-flight cancellation.

### Deliberately dropped as already specified

These requirements were subsumed by post-unify specs and are **not** promoted, to avoid two requirements
covering one subject:

| Dropped from | Already covered by |
|---|---|
| `mobile-design-system` → "Design token set" | `customer-app-visual-tokens`: brand palette, radius scale, elevation policy, motion tokens, icon ramp, typography (only the spacing scale survives) |
| `mobile-booking-ux` → "Stepped booking flow", "Jalali date and slot picker", "Confirmation summary before commit" | `customer-booking-journey`: service selection, staff selection incl. single-staff auto-skip, date/time selection, booking confirmation incl. slot-taken recovery, reservation completion |
| `mobile-discovery-ux` → "Provider detail screen" (core), "Explore search" (filter + state parts) | `customer-booking-journey`: provider details; `customer-discovery-journey`: category/service filtering, discovery states |

One factual correction was applied while merging: `mobile-design-system` asserted a `#1A365D` primary and a
lingering purple `primarySwatch`. Neither is true — the customer app's primary is `#3777BF`, identical to the
provider app (`app_colors.dart:13` vs `app_tokens.dart:78`), and `primarySwatch` is gone. The palette claim
was removed; the "styling is sourced only from the theme" guarantee was kept.

## Impact

- **Code**: `booksy-customer-app/lib/**` — `config/theme` (token overhaul), `core/widgets` (new component library, currently empty), `features/auth|home|search|bookings/presentation` (screen redesigns), `features/booking|profile` (currently empty scaffolds — new screens), `features/navigation` (shell rework). Deletion of `home_page_new.dart` duplicate.
- **APIs**: Read-only consumer of existing `/api/v1` endpoints (auth/OTP, categories, providers, bookings). No contract changes.
- **Backend / web frontend**: Untouched. The web app (`booksy-frontend`) serves as the UX reference for flow parity.
- **Dependencies**: Possible additions for skeleton loaders, cached images, OTP autofill — decided in design.md.
- **Docs**: `booksy-customer-app/CUSTOMER_APP_UX_FLOW.md` and `PROJECT_SUMMARY.md` will need updating after implementation.
