## Why

The brand reskin aligned Booking's color and typography with Coliride, but the two products still diverge at the **component and interaction level**: Coliride ships a mature shared widget library (standardized loading/empty/error state views, status badges, icon action buttons, dashed separators, responsive `DeviceScreenType` scaffolding, popup/modal/bottom-sheet helpers, a `Button` foundation with size variants), whereas the Booking provider app has only a thin ad-hoc set (`AppButton`, `AppTextField`, `OtpInput`, `AppSnackbar`, `StepScaffold`) and no shared vocabulary for cards, lists, section headers, or feedback states. Each new Booking screen therefore re-invents composition, spacing, and state handling, which will drift further from Coliride over time and produce an inconsistent product. This change captures a **shared visual language contract** so both products read as "designed by the same team" — without copying Coliride's ride-sharing flows.

## What Changes

- Conduct a component/interaction-level **convergence audit** of the Booking provider app against the Coliride Flutter implementation across ~24 dimensions (composition, layout rhythm, spacing, visual hierarchy, cards, lists, form layouts, section headers, dialogs, bottom sheets, empty/loading/error/success states, progress indicators, navigation, motion, elevation, shadows, dividers, icon usage, density, responsive behavior).
- For every pattern, record an explicit **adopt / adapt / diverge** decision with a booking-domain rationale. The audit findings live in `design.md`; the resulting contract lives in the new capability specs.
- Introduce a **structural design-system foundation** (spacing rhythm, elevation/shadow policy, divider style, density, icon sizing, motion principles) layered on top of the existing brand tokens — extending, not replacing, `app_tokens.dart`/`app_theme.dart`.
- Establish a **shared component contract** for the reusable primitives Booking is missing (cards, list rows, section headers, status badges/chips, icon action buttons, standardized feedback-state views for loading/empty/error/success), each modeled on its Coliride counterpart but composed for booking screens.
- Standardize **overlay and navigation patterns** (dialog styling, bottom sheets, progress/step indicators, app-bar behavior) and their motion so screens across the app feel coherent.
- **NON-GOAL / guardrail**: No business logic, user journeys, screen hierarchy, or domain flows change. This is purely a shared visual language. Coliride remains a visual reference only — its ride-sharing components are never transplanted. This change scopes to `booksy-provider-app` (the Vue apps were rebranded separately).

## Capabilities

### New Capabilities

- `design-system-foundations`: The structural (non-brand) layer of the design system — spacing scale & layout rhythm, radius scale, elevation/shadow policy (flat, borders-over-shadows), divider styling, content density, icon style & sizing, and motion/animation principles. Defines the contract the theme and every component must honor.
- `shared-ui-components`: Reusable presentation components Booking currently lacks — cards, list rows, section headers, status badges/chips, icon action buttons, and consistent form-field composition — each with a defined visual/interaction contract adapted from Coliride's shared widgets.
- `feedback-states`: Standardized loading, empty, error, and success state views (with retry affordances and progress indicators) so every screen renders the four canonical states consistently instead of ad-hoc spinners and inline text.
- `overlays-and-navigation`: Dialog, bottom-sheet, and app-bar/navigation styling and their entry/exit motion, plus step/progress indicators — the shared chrome and transitions that frame content across the app, including responsive/adaptive behavior expectations.

### Modified Capabilities

<!-- None. Existing feature specs (authentication, provider-registration, provider-dashboard, etc.) describe business behavior and flows, which this change deliberately does not alter. The design system is additive; feature screens consume it without changing their requirements. -->

## Impact

- **Code**: `booksy-provider-app/lib/config/theme/` (foundation tokens/theme extensions), new `booksy-provider-app/lib/core/widgets/` components (cards, list rows, section headers, badges, feedback-state views, overlay helpers). Existing screens (auth pages, 8-step onboarding wizard, dashboard) migrate to the shared components incrementally — presentation only.
- **Tests**: New widget tests per shared component and per feedback state; existing theme guard test (`test/config/theme/app_theme_test.dart`) extended for the foundation layer. No changes to bloc/cubit/routing/domain tests.
- **Reference (read-only)**: Coliride Flutter at `C:\Repos\Coliride\FrontendClient\lib\core\presentation\widgets` and `lib\core\config\constants` — mined for visual/interaction patterns, never imported.
- **Docs**: Audit matrix (adopt/adapt/diverge per pattern) captured in `design.md`; the convergence memory note ([[coliride-design-reference]]) updated on completion.
- **No impact**: backend, APIs, database, events, business flows, or the Vue frontend/admin apps.
