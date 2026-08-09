# customer-discovery-journey Specification

## Purpose
TBD - created by archiving change unify-customer-app-with-provider-design. Update Purpose after archive.
## Requirements
### Requirement: Provider discovery entry points
The Customer app SHALL present clear entry points into provider discovery from the home and explore surfaces, rendered in the aligned visual language.

#### Scenario: Discovery is reachable from home and explore
- **WHEN** a user opens the home or explore surface
- **THEN** a discernible search/discovery entry is available and styled consistently with the Provider app

### Requirement: Nearby-me search on existing endpoints
The app SHALL support a nearby-me search that obtains device location via `geolocator` and queries the existing `POST /Providers/search` endpoint with `latitude`/`longitude`/`radiusKm` and `sortBy=distance` (the confirmed geo contract). It MUST NOT introduce new backend endpoints or contract fields. When location permission is denied or the location service is unavailable, it MUST fall back gracefully to manual area/district search.

#### Scenario: Nearby results sorted by distance
- **WHEN** a user with granted location runs a nearby-me search
- **THEN** providers near the device are returned via the existing endpoints, ordered by distance

#### Scenario: Permission denied falls back
- **WHEN** location permission is denied or GPS is unavailable
- **THEN** the app does not block discovery and offers manual area/district selection instead

### Requirement: Map-based results view (DEFERRED — pending backend coordinates)
The app SHALL provide a map view that plots provider results on OpenStreetMap tiles via `flutter_map` (keyless, mirroring the Provider app), with no new backend contract. **This requirement is deferred in this change:** `POST /Providers/search` currently returns no per-provider coordinates, so there are no pins to plot. It becomes buildable once the backend returns provider `latitude`/`longitude`. See `findings.md`.

#### Scenario: Search results render on the map (deferred)
- **WHEN** the search response includes per-provider coordinates and a user switches to the map view
- **THEN** the matching providers are plotted on the OpenStreetMap map from the existing search response

### Requirement: Area and district search
The app SHALL let a user search within a selected area or district. The area/district **name** is geocoded to coordinates via a keyless geocoder (OpenStreetMap Nominatim, mirroring the Provider app), then providers are searched around that point via the existing `POST /Providers/search` distance path — using no new backend contract and not depending on `GET /Locations/search`.

#### Scenario: Searching within a district
- **WHEN** a user enters an area/district name and searches
- **THEN** the name is geocoded and providers near that point are returned via `POST /Providers/search` (distance-sorted) and shown in the aligned list surface

### Requirement: Category and service filtering
The discovery surface SHALL support category and service filtering, refined and restyled in the aligned language, over the existing search endpoints.

#### Scenario: Filtering by category narrows results
- **WHEN** a user applies a category or service filter
- **THEN** the results update accordingly using the existing search endpoints

### Requirement: Discovery states rendered consistently
Discovery surfaces SHALL render loading, empty (no results with a clear next action), and error (with retry) states in the aligned component styling.

#### Scenario: No results shows a clear next action
- **WHEN** a discovery query returns no providers
- **THEN** an aligned empty state with a clear next action (e.g., clear filters / widen area) is shown

### Requirement: Discovery journey uses existing backend only
Any discovery capability that would require a new backend endpoint, contract field, or domain change SHALL NOT be implemented in this change and MUST be recorded in `findings.md`.

#### Scenario: Unsupported need is deferred, not built
- **WHEN** a discovery refinement would require a backend/domain change
- **THEN** it is documented in `findings.md` and left unbuilt

