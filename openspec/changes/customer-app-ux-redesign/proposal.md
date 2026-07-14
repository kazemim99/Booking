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

### New Capabilities

- `mobile-design-system`: Design tokens (color, type, spacing, elevation, radius, motion) and the reusable Flutter component library; consistency and theming rules all screens must follow.
- `mobile-auth-ux`: Splash, phone login, and OTP verification screen behavior — validation, keyboard/autofill handling, error recovery, and session-restore flow.
- `mobile-discovery-ux`: Home dashboard and explore/search screens — content hierarchy, category/provider browsing, search and filtering, loading/empty states.
- `mobile-booking-ux`: Provider detail → service → time-slot → confirmation journey plus appointments list with cancel/reschedule interactions and their confirmation/undo patterns.
- `mobile-app-shell-ux`: Bottom navigation, back-stack rules, and the standardized cross-cutting states (loading skeletons, empty, error, offline, success feedback) and accessibility requirements applying to every screen.

### Modified Capabilities

<!-- None. Existing specs (authentication, customer-profile, provider-*, service-management, staff-management, working-hours-management) describe backend/web behavior; their requirements are unchanged. The mobile app consumes the same APIs. -->

## Impact

- **Code**: `booksy-customer-app/lib/**` — `config/theme` (token overhaul), `core/widgets` (new component library, currently empty), `features/auth|home|search|bookings/presentation` (screen redesigns), `features/booking|profile` (currently empty scaffolds — new screens), `features/navigation` (shell rework). Deletion of `home_page_new.dart` duplicate.
- **APIs**: Read-only consumer of existing `/api/v1` endpoints (auth/OTP, categories, providers, bookings). No contract changes.
- **Backend / web frontend**: Untouched. The web app (`booksy-frontend`) serves as the UX reference for flow parity.
- **Dependencies**: Possible additions for skeleton loaders, cached images, OTP autofill — decided in design.md.
- **Docs**: `booksy-customer-app/CUSTOMER_APP_UX_FLOW.md` and `PROJECT_SUMMARY.md` will need updating after implementation.
