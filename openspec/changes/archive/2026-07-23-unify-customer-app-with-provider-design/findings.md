# Findings: unify-customer-app-with-provider-design

Running log of decisions resolved during implementation, accessibility exceptions, and gaps deferred to a later change.

## Resolved decisions

- **O1 — Bottom nav labels → RESOLVED: label-less floating pill.** Implemented `AppBottomBar` as a chrome-blue floating pill with white icons + an active white dot and no text labels, matching the Provider's `AppBottomBar`. Screen-reader labels are preserved via `Semantics(label: …)`. Reversible: labels can be re-added without touching routing if customer usability testing objects.
- **O3 — Bottom-sheet drag handle → RESOLVED: keep it.** The customer sheet retains `showDragHandle: true` (a harmless, useful mobile affordance) even though the Provider does not show one. Purely additive; does not read as inconsistent.

## Accessibility (WCAG AA) exceptions — per design D7

The navy-ink text tone (`#4D5E80`) passes AA on white (6.5:1) and was adopted for `textPrimary`. Where the Provider's tone would fail AA, the accessible tone was kept instead:

- **Secondary text** — Provider `muted #96A0B3` (~2.7:1) fails AA, so `textSecondary` uses the navy-family `#5A6B8C` (5.4:1) instead. Tertiary text uses `#667085` (4.77:1).
- **`warning #FFCB33` is a fills-only accent.** It fails contrast as text/icon-on-white, so it is used only for fills/badges/rating accents; warning **text** uses the accessible `warningText #B45309`. The theme guard test asserts the `*Text` variants meet AA.
- **`error #FF6171` is a fills/badges/buttons tone** (white text sits on it); red **text** uses `errorText #B91C1C`; input-error **borders** use the darker `inputErrorBorder #E74A3B`.

The theme-guard test (`test/config/theme/app_theme_test.dart`) enforces AA on `textPrimary`/`textSecondary`/`textTertiary` and the four `*Text` variants.

## Notes

- **Rating stars kept as `Colors.amber`.** Gold rating stars are a domain convention shared with the Provider; routing them through the semantic `warning` token would misuse it. Left as-is; not an off-brand literal.
- **Slot-picker `Colors.white`** is correct on-fill text (white on the selected primary chip) — left as-is.

## O2 — Map SDK → CORRECTED: mirror the Provider = flutter_map + OSM (keyless), NOT Neshan

**Correction (2026-07-22).** The earlier "use Neshan (needs API key)" framing was based on the *web* app. The **Provider Flutter app** — the actual reference for this convergence — does **not** use Neshan or Google Maps. It uses:
- **`flutter_map: ^8.1.1` + `latlong2: ^0.9.1`** with **OpenStreetMap tiles** (`https://tile.openstreetmap.org/{z}/{x}/{y}.png`) — free, **keyless**, cross-platform (web/android/ios), tap-to-pin (`booksy-provider-app/.../onboarding/presentation/steps/location_step.dart`).
- **OSM Nominatim** (`https://nominatim.openstreetmap.org`, `booksy-provider-app/.../onboarding/data/datasources/geocoding_service.dart`) for forward + reverse geocoding — also **keyless**. The provider explicitly **abandoned Neshan** because its demo key hit `code 481 "API Key limit exceeded"`.

**Revised decision: the Customer app map mirrors the Provider — `flutter_map` + OSM tiles + Nominatim. No API key, no native map config, works on web.** The user's earlier "Neshan" pick was under the mistaken premise that a key was needed; matching the Provider (the stated goal) means keyless OSM. The customer's unused `google_maps_flutter` dep can be dropped.

**This UNBLOCKS the two items previously marked blocked:**
- **6.4 map results view** — add `flutter_map` + `latlong2` (mirror provider), plot provider markers from the already-built distance-sorted `/Providers/search` results. Only external need is a device pass to eyeball the render.
- **6.5 area/district search** — geocode the area/district **name → coords via Nominatim** (mirror the provider's `GeocodingService`), then reuse the confirmed `/Providers/search` distance path. This **avoids the unverified `GET /Locations/search` endpoint entirely** — no fabricated contract needed.

Customer app still uses `geolocator` (already declared) for nearby-me device location, which the provider onboarding does not need.

## Phase 6 progress (this session) — nearby-me search LOGIC done + verified

Implemented and unit-tested against the **confirmed** contract (no fabricated shapes, no key/device needed):
- Extended the search data layer (`SearchRemoteDataSource`/`SearchRepository`/impl) with optional `latitude`/`longitude`/`radiusKm`/`sortBy` — **backward-compatible** (existing callers/tests unaffected), riding the existing `/Providers/search` `ProviderSearchRequest` geo fields.
- Added `core/location/location_service.dart` — a `LocationService` abstraction (+ `GeolocatorLocationService` impl) that maps every platform failure to a sealed `LocationResult` (success / permission-denied / service-disabled / error), so nearby search is testable without platform channels.
- Added `NearbyProvidersCubit` (search feature) — permission → location → `searchProviders(sortBy: 'distance', lat, lng, radius)`, with a graceful `permissionDenied`/`serviceDisabled` fallback (never blocks discovery). Registered manually in DI.
- 5 cubit tests (distance-sort contract asserted, empty, permission-denied-no-query, service-disabled, error).

## ⚠️ GAP: `/Providers/search` returns no per-provider coordinates → map pins blocked (backend)

Confirmed from the client model: `ProviderDto` (`core/api/models/provider_models.dart`) has **no latitude/longitude**, and the `toEntity()` mapping (`home/data/models/provider_model.dart`) comments `distance: null // Not provided in search response`. So the search response carries neither per-provider coordinates nor a distance value.

**Consequence:** the map *results* view cannot plot provider pins — there is nothing to place. This is a **backend/contract gap, not a map-SDK problem** (flutter_map+OSM is settled and keyless). Plotting providers requires the backend to include `latitude`/`longitude` per provider in `/Providers/search`; that is **out of scope** here (no backend changes). Logged for a future backend change. Nearby-me still works as an *ordered list* (the backend sorts by distance server-side from the lat/lng we send), just without a visible distance value or map pins.

**Decision:** defer the map results view (6.4) until the backend returns provider coordinates. Do **not** add `flutter_map`/`latlong2` yet — a pin-less map adds a dependency for no discovery value. Ship nearby + area/district as **list** results now.

## Phase 6 progress (this session, cont.) — area/district search done + verified

- Added `core/location/geocoding_service.dart` — `GeocodingService` abstraction + `NominatimGeocodingService` (keyless OSM Nominatim, `fa` + `countrycodes=ir`), mirroring the provider's geocoder.
- Added `AreaSearchCubit` — geocode area **name → coords**, then the confirmed `/Providers/search` distance path. **Avoids the unverified `/Locations/search` endpoint entirely.** Registered in DI; 5 cubit tests (geocode+search, area-not-found-no-query, empty, blank no-op, error).

## Still open (Phase 6–7 remaining)

- **Area/district search (6.5) — CONTRACT UNVERIFIED.** `GET /Locations/search` and `GET /Locations/nearby` exact request/response shapes could not be confirmed from the client code (only the `/Providers/search` geo fields are confirmed). Before building area/district search, verify these endpoints' contracts (Swagger/backend) — do **not** hardcode a guessed shape. Nearby-me above deliberately uses the confirmed `/Providers/search` path instead.
- **Map results view (6.4)** — blocked on the Neshan prerequisites above.
- **UI wiring (6.7) + on-device permission flow** — the nearby results still need a discovery surface (can render as a list now, no map) wired into the existing shell, plus real-device permission testing.
- **Booking-journey review (Phase 7)** — not yet started.

## Phase 7 — booking-journey review outcome

Reviewed `booking/presentation/pages/booking_flow_page.dart` and `search/presentation/pages/provider_detail_page.dart`:
- The flow already implements the full spec: **service → staff (with "any staff", single-staff auto-skip via `visibleSteps`) → Jalali date/slot picker → confirmation → success**, with a step progress indicator and `PopScope` back handling.
- **Nothing commits before the explicit confirm tap**; an unauthenticated user hits a point-of-need login that returns to the flow with selections intact (app-scoped bloc). **Slot-taken recovery** surfaces via an error snackbar (bloc-driven).
- Both screens are entirely token/theme-driven (`AppCard`/`AppButton`/`ErrorState`/`SkeletonLoader`/`AppSpacing`/`colorScheme`) — they **inherited the visual convergence automatically**; the success check now uses the green `secondary`. No off-brand literals (only the conventional gold rating star).
- **No functional gaps found; no restyle changes needed.** `booking_bloc_test`/`appointments_bloc_test` pass unchanged (behavior preserved).

## Deferred to a later change (backend/domain work — out of scope here)

- **Per-provider coordinates in `/Providers/search`** — needed to plot providers on a map (6.4). Backend must return `latitude`/`longitude` per provider. Until then the map results view is deferred (the map SDK is settled: flutter_map + OSM, keyless).
- **Distance value in search results** — `/Providers/search` returns no distance (`toEntity` maps `distance: null`); nearby/area results are ordered by distance server-side but can't show a "X km" figure. A backend change would enable displaying distance (`ProviderResultCard` already renders it when present).
