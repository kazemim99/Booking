# Tasks: unify-customer-app-with-provider-design

> Scope: `booksy-customer-app` presentation layer only. No changes to business logic, backend contracts, domain models, navigation architecture, project structure, design-system architecture, or widget names (rename only if unavoidable — flag it in the task). Provider app is read-only reference.

## 1. Foundation — token values (no renames)

- [x] 1.1 Re-value `config/theme/app_colors.dart` to the Provider palette per the design mapping (primary `#3777BF`, app-bar `#3777C0`, ink `#4D5E80`, success `#0AC075`, warning `#FFCB33`, error `#FF6171`, input-error `#E74A3B`, border `#EBEEF3`, divider `#E5E9F2`, white scaffold) — keep every `AppColors` field/class name unchanged
- [x] 1.2 Set component radii targets in `app_tokens.dart` (`AppRadius`) so button=10, field=12, card=15, bottomSheet=14, dialog/panel=16, snackbar=12 are expressible without renaming the scale
- [x] 1.3 Align `AppMotion` fast to 180ms and the emphasis curve to `easeOutCubic`; keep the class and its other fields
- [x] 1.4 Add an icon-size ramp (sm 16 / md 24 / action 20 / hero 72) consumable from tokens
- [x] 1.5 Keep `AppElevation` class intact but stop it driving shadows (values may stay; usage changes in Phase 2)
- [x] 1.6 Run the WCAG-AA contrast check on every re-valued text/background pairing; where navy ink `#4D5E80` fails ≥4.5:1, keep the accessible tone and note the exception (feeds Phase 8 findings)

## 2. Theme wiring (`app_theme.dart`)

- [x] 2.1 Rebuild the `ColorScheme` from the aligned tokens (primary blue, secondary success-green, error, navy-ink onSurface, white surface/scaffold)
- [x] 2.2 Set `appBarTheme` to blue chrome (`#3777C0`), white foreground, centered title, elevation 0, no scrolled-under shadow, and a global white RTL-mirrored back icon
- [x] 2.3 Apply the light `SystemUiOverlayStyle` so status-bar icons stay legible over the blue app bar
- [x] 2.4 Convert `cardTheme`, `dialogTheme`, and `bottomSheetTheme` to flat (elevation 0) with borders + aligned radii (card 15, dialog 16, sheet 14 top); remove `shadowLight`/`AppElevation` shadow usage
- [x] 2.5 Switch `inputDecorationTheme` to non-filled bordered inputs (resting `#EBEEF3` ~1.9px, focused `#C3CAD9` ~2.1px, error `#E74A3B`) at field radius 12
- [x] 2.6 Align button themes to the Provider metrics (primary blue fill radius 10 bold 17; secondary white + 2px primary outline; text button ink), keeping a ≥48dp effective tap target
- [x] 2.7 Align `dividerTheme` (`#E5E9F2`), `snackBarTheme` (floating radius 12), `chipTheme`, tab/checkbox/switch (green accent), and `listTileTheme` to the aligned palette/radii
- [x] 2.8 Add the theme-guard test (`test/config/theme/app_theme_test.dart`) asserting the aligned token values, the no-Material-shadow policy, and AA contrast; `flutter analyze` clean

## 3. Shared components restyle (`core/widgets`) — names/APIs frozen

> The components were already fully token/theme-driven (no hardcoded colors/shadows/radii), so the Phase 1–2 changes converged them automatically. The app has no golden tests; the existing widget tests cover render/48dp/1.3×/four-states/badge and all pass.

- [x] 3.1 `app_button.dart` inherits the aligned primary/secondary/text metrics from the theme; widget tests (loading, tap-disable, ≥48dp, 1.3×) pass
- [x] 3.2 `app_text_field.dart` inherits the non-filled bordered look; `otp_input.dart` error border switched to `inputErrorBorder`
- [x] 3.3 `app_card.dart` renders flat + bordered (radius 15) via the theme
- [x] 3.4 `status_badge.dart` conveys status by label + aligned tint/text color (never color alone); test passes
- [x] 3.5 `empty_state.dart`/`error_state.dart` use hero-size icons + a soft container (fixes the now-invisible white container on the white scaffold); `skeleton_loader.dart` + `state_switcher.dart` inherit aligned surfaces; four-state tests pass
- [x] 3.6 `app_bottom_sheet.dart`/dialogs/`app_snackbar.dart` inherit radius 14/16/12; **O3 resolved: keep the drag handle** (harmless mobile affordance)
- [x] 3.7 `offline_banner.dart` inherits aligned palette; Semantics + 1.3× verified in tests

## 4. Chrome & navigation visuals

- [x] 4.1 New `app_bottom_bar.dart` floating blue pill (chrome-blue, radius 16, side gutters, raised, white 24px icons, active dot, count badges) dropped into the existing `StatefulShellRoute` shell — routes/tabs/back-stack unchanged. **O1 resolved: label-less pill** (matches Provider; reversible)
- [x] 4.2 Widget tests assert the pill renders the active/selected icon and drives `goBranch` on tap; router redirect/back-stack tests still green
- [x] 4.3 App-bar sweep: confirmed no screen overrides the blue chrome (no per-screen `AppBar`/`Scaffold` background overrides)
- [x] 4.4 List-row/divider styling aligned app-wide via `dividerTheme`/`listTileTheme`; ripple/overlay via the aligned palette + motion tokens

## 5. Screen token sweep

- [x] 5.1 Swept `features/**/presentation` for hardcoded colors/paddings/radii/icon sizes — screens are token/theme-driven; only literals are conventional gold rating stars (`Colors.amber`, a domain convention shared with the Provider) and correct white-on-fill text; no off-brand literals to replace
- [ ] 5.2 Visual QA on device/emulator: verify `home`, `search/explore`, `bookings`, `profile`, `auth` render correctly on the flat/bordered surfaces (no lost separation) — requires running the app
- [x] 5.3 `flutter analyze` clean and full `flutter test` green (56 tests); no golden suite in this app

## 6. Discovery journey (existing endpoints only)

- [x] 6.1 Resolve O2 → **Neshan** (user decision); integration plan + external prerequisites (API key, native config, on-device verification) recorded in `findings.md`
- [x] 6.2 Extended the `search` datasource/repository/interface with optional `latitude`/`longitude`/`radiusKm`/`sortBy` over the existing `/Providers/search` contract — backward-compatible, no new endpoints/fields. (Area/district `GET /Locations/search` deferred — its contract is unverified; see findings)
- [x] 6.3 Nearby-me search **logic**: `core/location/LocationService` (geolocator wrapper → sealed `LocationResult`) + `NearbyProvidersCubit` (distance-sorted search, graceful permission-denied/service-disabled fallback) + DI + 5 cubit tests. On-device permission dialog + list UI wiring pending (6.7)
- [ ] 6.4 Map results view (map SDK corrected to **flutter_map + OSM**, keyless, mirroring the provider) — **BLOCKED by data: `/Providers/search` returns no per-provider coordinates** (confirmed in `ProviderDto`/`toEntity`), so there are no pins to plot. Needs a backend change to return provider lat/lng (out of scope). Logged in findings; `flutter_map`/`latlong2` intentionally not added until coords exist
- [x] 6.5 Area/district search **logic** via geocoding: `NominatimGeocodingService` (keyless OSM) + `AreaSearchCubit` (geocode name → coords → confirmed `/Providers/search` distance path), avoiding the unverified `/Locations/search` endpoint; DI + 5 cubit tests
- [x] 6.6 Discovery states rendered: `NearbyPage` handles loading/loaded/empty/permissionDenied/serviceDisabled/error (fallbacks route to area search); `AreaPage` handles initial/loading/loaded/empty/areaNotFound/error; `explore` keeps loading/empty(clear-filters)/error via `StateSwitcher`
- [x] 6.7 Wired nearby + area discovery into `explore`: "اطراف من"/"جستجو در محله" entry chips → `NearbyPage`/`AreaPage` routes **within the existing explore branch** (shell/tabs/back-stack unchanged). Extracted reusable `ProviderResultCard` (+ widget test); pages host their cubits via `BlocProvider`. On-device visual/permission pass still recommended (5.2)

## 7. Booking journey review & refine (no logic change)

- [x] 7.1 Reviewed `provider_detail_page.dart`: identity/rating/address/hours/services + reachable booking CTA; already token/theme-driven (inherited the convergence); no off-brand literals; no gaps
- [x] 7.2 Reviewed `booking_flow_page.dart`: service → staff (any-staff + single-staff auto-skip) → Jalali slot picker → confirmation (nothing commits early; point-of-need login; slot-taken snackbar recovery) → success; fully theme-driven, step order + rules preserved; no restyle changes needed
- [x] 7.3 `booking_bloc_test`/`appointments_bloc_test` pass unchanged in the suite (behavior preserved); no behavior-driven test edits

## 8. Verification, docs & findings

- [x] 8.1 `flutter analyze lib test` clean (only 2 pre-existing generated `.g.dart` warnings) + `flutter test` green (68 tests); no bloc/routing/domain test changed for behavior reasons (only the stale old-primary theme assertion updated)
- [~] 8.2 Accessibility: automated coverage done (theme-guard AA contrast on all text tones, ≥48dp button target, 1.3× text-scale component tests, reduced-motion skeleton); on-device overflow/RTL pass on the restyled screens still recommended (5.2)
- [x] 8.3 `findings.md` finalized (map-SDK correction, coords/distance backend gaps, AA exceptions, O1/O3 decisions, Phase 7 review)
- [x] 8.4 Updated `CUSTOMER_APP_UX_FLOW.md` (convergence + discovery note) and the change memory
- [x] 8.5 Completion report produced (final message)
