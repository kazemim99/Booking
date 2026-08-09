# Design: unify-customer-app-with-provider-design

## Context

Two Flutter apps share a domain and a font (`Vazir`) but were themed independently. The **Provider app** (`booksy-provider-app`) renders the Coliride visual language via `AppTheme.light` + `app_tokens.dart`: blue app-bar chrome (`#3777C0`), navy-ink text (`#4D5E80`), **flat & shadowless surfaces with borders over shadows**, `FilledButton`-based primary buttons, non-filled bordered inputs, component-specific radii (button 10, field 12, card 15, sheet 14, panel 16), a floating blue bottom-nav pill, and a small icon/motion ramp. The **Customer app** (`booksy-customer-app`) renders its own Material 3 system: light app bar with dark text (`#1A202C`), near-black-navy primary (`#1A365D`), **Material elevation/shadows** (`AppElevation` 1/3/6 applied to cards, nav bar, dialogs), `ElevatedButton` primaries, filled inputs, generic radii (md 12 / lg 16 / xl 24), and a labeled Material `NavigationBar`.

On the journey side, the Customer app's booking half (provider detail → service → staff → date/time → confirm → complete) exists, but discovery is thin: `explore_page.dart` does text/category search only. There is **no map view, no nearby-me search, and no area/district search**, although the map/location packages (`google_maps_flutter`, `geolocator`, `geocoding`) are already declared and the backend already exposes `POST /Providers/search` (with `latitude`/`longitude`/`radiusKm`/`sortBy=distance`), `GET /Locations/search`, and `GET /Locations/nearby`. The Vue web frontend already implements the full discovery experience (`ProviderSearchView`, `ProviderFilters`, `ProviderSearchResults`, `MapViewResults`, `NeshanMapPicker`), which is the functional reference.

**Constraints (user-stated):** change nothing in business logic/rules, backend contracts, domain models, navigation architecture, project structure, design-system architecture (token classes, theme wiring, `core/widgets` layout), or widget names (unless truly unavoidable). The Provider app is a read-only visual + product reference.

## Goals / Non-Goals

**Goals:**
- Make the Customer app's UI visually indistinguishable in "voice" from the Provider app: same palette, typography scale, spacing rhythm, radii, flat elevation policy, icons, and motion.
- Restyle the Customer app's existing shared components and chrome (app bar, bottom nav, dialogs, sheets, lists) to the Provider look **without renaming files/classes/APIs**.
- Complete and refine the end-to-end customer journey — including the missing map/nearby/area-district discovery surfaces — on the **existing** backend endpoints, in the aligned visual language.

**Non-Goals:**
- No business-rule, backend-contract, domain-model, or booking-flow-logic changes.
- No navigation-architecture rework (the `StatefulShellRoute` shell, tabs, back-stack, and routes stay; restyling the bar and adding routes *within* the shell to complete a journey step is allowed, re-architecting is not).
- No new design-system architecture — token *values* change in place; class/field names are preserved.
- No changes to the Provider app, backend, DB, events, or Vue apps.
- Not building any journey step that would require a new backend endpoint/contract (documented in `findings.md` instead).

## Decisions

### D1 — Align token *values* in place; never rename or restructure tokens
Re-point the existing token classes (`AppColors`, `AppTextStyles`, `AppSpacing`, `AppRadius`, `AppElevation`, `AppMotion`) at the Provider's values rather than importing the Provider's `app_tokens.dart` or refactoring the customer's four-file token layout into the Provider's two-file layout. **Rationale:** honors the "no design-system-architecture change / no rename" constraint; keeps the diff to values + theme wiring; every screen already consumes tokens via `Theme.of(context)` or `AppColors`, so re-valuing propagates automatically. **Alternative rejected:** copy the Provider's token files verbatim — would rename classes/fields, ripple through every import, and violate the constraint.

**Token mapping (provider reference → customer target):**

| Concern | Provider (reference) | Customer (current) | Target |
|---|---|---|---|
| Primary | `#3777BF` | `#1A365D` | `#3777BF` |
| App-bar chrome | `#3777C0` bg, white fg | `#FAFAFA` bg, dark fg | `#3777C0` bg, white fg |
| Text / ink | `#4D5E80` (navy) | `#1A202C` (near-black) | `#4D5E80` (AA-verified, see D7) |
| Secondary accent | success green `#0AC075` | trust blue `#1976D2` | success green `#0AC075` |
| Success | `#0AC075` | `#059669` | `#0AC075` |
| Error (toasts/badges/buttons) | `#FF6171` | `#DC2626` | `#FF6171` |
| Error (input border only) | `#E74A3B` | `#DC2626` | `#E74A3B` |
| Warning | `#FFCB33` | `#D97706` | `#FFCB33` |
| Resting border | `#EBEEF3` | `#E2E8F0` | `#EBEEF3` |
| Divider | `#E5E9F2` | 12% black | `#E5E9F2` |
| Scaffold bg | white | `#FAFAFA` | white |
| Radius — button | 10 | md 12 | 10 |
| Radius — field | 12 | md 12 | 12 (already aligned) |
| Radius — card | 15 | lg 16 | 15 |
| Radius — bottom sheet | 14 | xl 24 | 14 |
| Radius — dialog/panel | 16 | xl 24 | 16 |
| Radius — snackbar | 12 | md 12 | 12 (aligned) |
| Elevation | flat (0), borders | 1/3/6 shadows | flat (0), borders (see D2) |
| Motion — fast | 180ms | 150ms | 180ms |
| Motion — curve | `easeOutCubic` | `easeInOutCubic` | `easeOutCubic` |
| Icon ramp | sm16 / md24 / action20 / hero72 | none (inline 24) | adopt ramp |

### D2 — Flat "borders over shadows" elevation policy
Set every themed surface (`cardTheme`, `dialogTheme`, `appBarTheme`, bottom nav, sheets) to `elevation: 0` and express separation with the resting border (`#EBEEF3`) / divider (`#E5E9F2`) and, where a fill is needed, `surfaceSoft`. Stop applying `AppElevation.low/medium/high` and `shadowLight` in the theme. **The `AppElevation` class stays** (no rename) — its values simply cease to drive shadows. **Rationale:** the single most defining Coliride trait; without it the apps still look different even with matched colors. **Alternative rejected:** keep subtle shadows — leaves a visible material mismatch.

### D3 — App bar becomes blue chrome with white content
Adopt the Provider's `appBarTheme`: `backgroundColor #3777C0`, `foregroundColor white`, `centerTitle: true`, `elevation 0`, and a global white RTL-mirrored back icon via `actionIconTheme`. Drop `scrolledUnderElevation`. **Rationale:** the app bar is the most prominent chrome; a light app bar vs blue app bar is the loudest inconsistency. **Trade-off:** status-bar icon brightness must flip to light (`SystemUiOverlayStyle.light`) — handled in the theme/app shell.

### D4 — Bottom navigation restyled to the floating blue pill (visual only)
The Provider uses a custom `AppBottomBar`: a floating blue pill (68dp, 16px side gutters, 20px above the edge, radius 16, chrome-blue fill, white 24px icons, coral count badges, active = full opacity + 4px white dot, inactive = `white70`, **no text labels**). Restyle the Customer app's shell bottom bar to this look. Because the shell/routes must not change, this is a **presentation swap inside the existing `StatefulShellRoute`** — the bar widget is replaced/restyled, the navigation architecture is untouched. **Open question O1:** the Provider pill is label-less; a customer app may want labels for discoverability. Default = match the pill (visual consistency is the goal); revisit if usability testing objects.

### D5 — Component styling alignment (names/APIs frozen)
Restyle the customer's existing `core/widgets` to the Provider metrics: primary button height 46 / radius 10 / bold 17 Vazir / blue fill / elevation 0; secondary = white fill + 2px primary outline; inputs = **non-filled**, border widths 1.9 / 2.1-focus, `inputError` border on error; cards flat + radius 15 + border; bottom sheets radius 14 top; dialogs flat + radius 16; snackbars floating radius 12. Files/classes/constructors keep their current names (`empty_state.dart`, `error_state.dart`, `app_bottom_sheet.dart`, etc.) — only paint moves. **Rationale:** satisfies "no rename" while achieving component parity. **Note:** the Provider primary uses `FilledButton`; the customer's `AppButton` may keep its current base — only the resolved visual metrics must match, not the underlying Material widget.

### D6 — Spacing: align at the component level, not a global re-value
The base grid mostly matches (both 4dp-based; both share md 16 / lg 24), but names/values diverge at the low end (provider `sm 8` / `xs 4` vs customer `sm 12` / `xs 8`) and the Provider adds `card 12` intra-card padding. **Blanket re-valuing `AppSpacing.sm` etc. would silently shift every screen**, risking layout regressions across the whole app. Decision: keep the customer's spacing scale and names; align spacing **where it visibly matters** — component interior padding (adopt a 12px card interior), section gaps, and any screen using off-token magic numbers. **Rationale:** contains blast radius; the visible rhythm difference lives mostly in card/list interiors, not the global grid. **Alternative rejected:** global scale rename/re-value — high regression risk for low visual gain.

### D7 — Typography: same font, align scale/weights/color; accessibility wins on color
Both apps use `Vazir`, so only sizes/weights/colors converge. Adopt the Provider's navy ink (`#4D5E80`) for headings/body **only after verifying ≥4.5:1 on white** (WCAG AA — a documented Customer-app requirement). If a specific pairing fails AA, keep the accessible tone rather than the exact Provider hex, and record it. **Rationale:** the Customer app's accessibility contract (contrast, 1.3× scale) must not regress for the sake of a color match.

### D8 — Discovery journey built on existing endpoints; map provider is the key unknown
Extend the existing `search` feature (bloc/repo/datasource) — no new architecture — to add three surfaces on already-defined endpoints: **nearby-me** (`geolocator` location → `GET /Locations/nearby` and/or `POST /Providers/search` with lat/lng/radius, `sortBy=distance`), **map results view** (plot `POST /Providers/search` results), and **area/district search** (`GET /Locations/search`). Category/service filtering already partially exists and is refined, not rebuilt. **Map SDK decision (O2):** recommend **Neshan** to match the web app and for Iran coverage/availability (Google Maps has poor Iran coverage and access constraints), *contingent on a viable Flutter integration* (native platform-view package or a Neshan-JS `WebView`); fall back to `google_maps_flutter` (already in pubspec) if no acceptable Neshan Flutter path exists. This is resolved by a short spike before implementation and recorded in `findings.md`. Location permission handled via `geolocator` with a graceful denied/disabled fallback to manual area/district selection.

### D9 — Booking journey: review-and-refine only
The existing provider-detail and booking-flow screens are reviewed for flow gaps, missing empty/loading/error states, copy clarity, and transition smoothness, then restyled to the aligned language. **No step is added/removed/reordered and no business rule changes.** Anything that reads as a functional gap (not a styling/UX-polish gap) is logged in `findings.md`, not implemented.

### D10 — Gaps go to findings.md, not into scope
Any discovery/booking step needing a new backend endpoint, contract field, or domain change to be truly complete is documented in `findings.md` (mirroring `customer-app-ux-redesign`'s pattern) and explicitly left unbuilt.

## Risks / Trade-offs

- **Navy ink lowers text contrast** → D7: AA-verify every token pairing; accessibility tone wins over exact hex; extend the theme-guard test to assert AA.
- **Removing Material shadows flattens busy customer screens, hurting perceived separation** → D2: compensate with resting borders, dividers, and `surfaceSoft` fills; review dense screens (home, explore results) during the screen sweep.
- **Global spacing re-value would ripple unpredictably** → D6: component-level alignment only; no scale rename.
- **Neshan has no first-party Flutter SDK** → D8: time-boxed spike; documented fallback to the already-wired `google_maps_flutter`.
- **Button height 46 < the 48dp touch-target floor** → keep the effective tap target ≥48dp (hit padding) even at 46 visual height; assert in widget tests.
- **Label-less nav pill may hurt customer discoverability** → O1: default to the pill for consistency; keep the decision reversible (labels can be re-added without touching routing).
- **Golden-test churn across every restyled component/screen** → regenerate goldens per component as part of each phase; treat golden diffs as expected, review visually.
- **Location permission denial / no GPS** → graceful fallback to area/district search; never block discovery on permission.

## Migration Plan

Phased, presentation-only, each phase independently revertible (visual-only ⇒ low risk):
1. **Tokens** — re-value `AppColors`/`AppRadius`/`AppMotion` (+ icon ramp) to targets; keep names.
2. **Theme** — rewire `app_theme.dart` (flat elevations, blue app bar, non-filled inputs, radii) + status-bar overlay style.
3. **Components** — restyle `core/widgets`; regenerate goldens; assert AA + ≥48dp tap targets.
4. **Chrome** — restyle bottom nav to the floating pill; app-bar sweep.
5. **Screen sweep** — replace remaining hardcoded colors/paddings/radii/icons with tokens.
6. **Discovery journey** — resolve O2 (map spike), then build nearby/map/area-district search + refine filtering on existing endpoints.
7. **Booking journey** — review-and-refine existing steps; restyle; log gaps to `findings.md`.
8. **Docs** — update `CUSTOMER_APP_UX_FLOW.md`; update the [[coliride-design-reference]] / change memory.

**Rollback:** revert the offending phase's commit; because no data/contract/behavior changes, rollback is a pure UI revert with no migration state.

## Open Questions

- **O1 — Bottom nav labels:** match the Provider's label-less floating pill (default), or keep text labels for customer discoverability?
- **O2 — Map SDK:** Neshan (web parity, Iran coverage) via platform-view/WebView, or the already-wired `google_maps_flutter`? Resolved by a pre-implementation spike; recorded in `findings.md`.
- **O3 — Bottom-sheet drag handle:** the customer currently shows a drag handle; the Provider does not. Keep the handle (mobile affordance) or drop it to match? Default: keep (harmless affordance), revisit if it reads inconsistent.
- **O4 — Text recolor scope:** apply navy ink app-wide, or only to headings/chrome while keeping the higher-contrast body tone? Resolved by the D7 AA audit.
