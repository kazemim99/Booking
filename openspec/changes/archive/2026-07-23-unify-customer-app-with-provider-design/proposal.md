# Proposal: unify-customer-app-with-provider-design

## Why

Two gaps keep the Customer app (`booksy-customer-app`) from feeling like part of the same product as the now-feature-complete Provider app (`booksy-provider-app`):

1. **Visual divergence.** The Provider app was reskinned to the Coliride visual language — mid-blue chrome (`#3777BF`), navy-ink text (`#4D5E80`), flat shadowless surfaces with borders over shadows, component-specific corner radii, a defined icon/motion ramp. The Customer app runs its *own* independent Material 3 system (near-black navy primary `#1A365D`, Material elevation/shadows, generic radius scale), so the two apps no longer read as one family.
2. **Incomplete customer journey.** The booking half of the journey (provider detail → service → staff → date/time → confirmation → completion) exists, but the **discovery half is missing**: there is no map-based search, nearby search, or area/district search in the app, even though the map/location dependencies are already declared and the backend already exposes the endpoints for it (`POST /Providers/search` with `latitude`/`longitude`/`radiusKm`/`sortBy=distance`, `GET /Locations/search`, `GET /Locations/nearby`). The Vue web frontend already implements this full discovery experience (`ProviderSearchView`, `ProviderFilters`, `MapViewResults`, `NeshanMapPicker`); the Flutter app has not caught up.

This change (a) aligns the Customer app's **entire UI** to the Provider app as the visual reference, and (b) reviews and **completes/refines the end-to-end customer booking journey** so it is intuitive and visually consistent — all **without changing business rules, backend contracts, domain models, or navigation architecture.**

## What Changes

### Pillar 1 — UI convergence (visual layer only)

Re-point the Customer app's *existing* design tokens and *existing* shared components at the Provider app's Coliride-derived visual values. Token class/field names, widget names, files, constructors, and public APIs are **unchanged** — only values and paint properties move.

- **Visual tokens** — align `config/theme/` (`AppColors`, `AppTextStyles`, `AppSpacing`, `AppRadius`, `AppElevation`, `AppMotion`, icon sizing) to the Provider's blue/navy-ink palette, flat **borders-over-shadows** elevation policy (retiring Material shadow usage), component-specific radii, spacing rhythm, icon ramp, and motion durations/curves.
- **Components** — restyle existing shared widgets (`app_button`, `app_text_field`, `app_card`, `app_bottom_sheet`, dialogs, `app_snackbar`, `status_badge`, `empty_state`, `error_state`, `skeleton_loader`, `state_switcher`, `otp_input`, `offline_banner`) so fills, borders, radii, typography, spacing, and icons match the Provider counterparts — names/APIs untouched.
- **Chrome, navigation visuals & interaction** — align app-bar styling, bottom-navigation colors/indicator/icons, list-row/divider styling, ripple/overlay/pressed states, and animation durations/curves to the Provider look. Navigation **architecture and routing are untouched** — only their visual attributes.
- **Icons** — standardize the icon set/style/sizing across restyled surfaces to the Provider usage via the existing icon ramp.

### Pillar 2 — Customer journey review & refinement (UX layer, existing backend only)

Audit the full journey against the Provider app's product standard and the web frontend's flow, then refine each step for completeness, intuitiveness, and visual consistency. Missing discovery surfaces are **built on the existing backend endpoints** (no new contracts); every step is restyled with the Pillar-1 visual language.

- **Provider discovery** — refine the home/explore entry points into discovery.
- **Nearby & map-based search** — add a map view of results and a nearby-me search using `geolocator` + the existing `GET /Locations/nearby` and `POST /Providers/search` (`latitude`/`longitude`/`radiusKm`, `sortBy=distance`). (Map provider choice — Neshan, as the web app uses, vs `google_maps_flutter` already in pubspec — is a design.md decision.)
- **Area / district search** — search within a selected area/district via the existing `GET /Locations/search`.
- **Category / service filtering** — refine category and service filters on the results surface.
- **Provider details → service selection → staff selection (where applicable) → date & time selection → booking confirmation → reservation completion** — review the existing screens for flow gaps, empty/loading/error handling, and intuitiveness; refine copy, states, and transitions; ensure visual consistency. Business rules and step sequence are preserved.

Any journey step that would require a **new backend endpoint, contract, or domain change** to complete is documented as a gap in `findings.md` and **not implemented here**.

### Explicit Non-Goals (out of scope)

This change does **not** modify: business logic or business rules; backend contracts or API behavior; domain models; navigation *architecture* (the shell/tab/back-stack structure and routing framework — adding a route/screen *within* the existing architecture to complete a journey step is in scope, re-architecting it is not); existing project structure; the design-system *architecture* (token class layout, theme wiring pattern, `core/widgets` structure); or widget naming (rename only if a value genuinely cannot be expressed otherwise, flagged explicitly in tasks). No changes to the Provider app (read-only visual + product reference), the backend, the database, events, or the Vue frontend/admin apps.

## Capabilities

### New Capabilities

- `customer-app-visual-tokens`: Customer-app design-token *values* and theme output as a visual contract aligned to the Provider app — color, typography, spacing rhythm, corner-radius scale, the flat borders-over-shadows elevation policy, icon-size ramp, and motion. Token class/field names preserved.
- `customer-app-component-styling`: Visual styling contract for the Customer app's existing shared components (buttons, inputs, cards, bottom sheets, dialogs, snackbars, status badges, and the empty/error/loading/skeleton state views) matching the Provider counterparts — component names and APIs unchanged.
- `customer-app-chrome-styling`: Visual styling contract for app bars, bottom navigation, list rows/dividers, and interaction/animation styling — the surrounding chrome and motion aligned to the Provider look, without altering navigation architecture, routing, or interaction logic.
- `customer-discovery-journey`: The end-to-end provider-discovery experience — home/explore discovery entry, nearby search, map-based search, area/district search, and category/service filtering — built and refined on the existing search/location backend endpoints and rendered in the aligned visual language.
- `customer-booking-journey`: The end-to-end reservation experience — provider details, service selection, staff selection (where applicable), date/time selection, booking confirmation, and reservation completion — reviewed and refined for completeness, intuitiveness, and visual consistency, with business rules and step sequence preserved.

### Modified Capabilities

<!-- None. Existing specs (authentication, customer-profile, provider-*, service-management, staff-management, working-hours-management) describe backend/web/provider business behavior; none describe the Customer app's UI/UX layer, and none of their requirements change. This change is additive presentation + journey-completion styling on the existing API surface. -->

## Impact

- **Code (styling)**: `booksy-customer-app/lib/config/theme/**` (token *values* + `app_theme.dart` re-aligned) and `booksy-customer-app/lib/core/widgets/**` (paint/style properties). No files renamed, moved, or deleted for Pillar 1.
- **Code (journey)**: `booksy-customer-app/lib/features/{home,search,booking}/presentation/**` — refine existing discovery/booking screens; add map/nearby/area-search presentation and their blocs/cubits, consuming existing datasources/repositories (extended only to call already-defined endpoints, no new contracts). New routes registered *within* the existing router shell. May activate the already-declared `google_maps_flutter`/`geolocator`/`geocoding` deps.
- **Reference (read-only)**: `booksy-provider-app/lib/config/theme/**` and `lib/core/widgets/**` (visual values/treatment); `booksy-frontend/src/modules/**` discovery components (journey/flow reference). Never imported or copied wholesale.
- **Tests**: Existing customer-app widget/golden tests updated for new visual values; theme guard tests extended (aligned tokens + no-Material-shadow policy). New widget/bloc tests for the discovery surfaces (map/nearby/area search, filtering) and for any refined booking-step behavior. Bloc/routing/domain tests for unchanged behavior stay green.
- **Docs / findings**: journey gaps requiring backend/domain work captured in `findings.md`; `booksy-customer-app/CUSTOMER_APP_UX_FLOW.md` updated on completion; [[coliride-design-reference]] memory updated.
- **No impact**: backend, APIs, database, events, background jobs, the Provider app, the Vue frontend/admin apps, navigation architecture, booking-flow business rules, and all business behavior.
- **Relationship to prior work**: layers on the completed `customer-app-ux-redesign` (Customer-app design system + booking flow) and treats `design-system-convergence` (Provider → Coliride) as the source of target visual values.
