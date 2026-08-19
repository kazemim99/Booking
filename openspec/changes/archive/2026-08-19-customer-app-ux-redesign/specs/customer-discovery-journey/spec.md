# customer-discovery-journey

> The existing requirements "Provider discovery entry points", "Category and service filtering" and
> "Discovery states rendered consistently" already cover discovery entry, filtering, and loading/empty/error
> states. The deltas below add only what they do not cover.

## ADDED Requirements

### Requirement: Home content hierarchy
The home screen SHALL present, in order: a search entry point, the user's next upcoming booking as a prominent card (authenticated users with bookings only), service categories, top-rated providers, and promotions. Each section SHALL load independently with skeleton placeholders and fail independently with a section-level retry, so one failed section never blanks the screen. There SHALL be exactly one home implementation in the codebase.

#### Scenario: Returning user with an upcoming booking
- **WHEN** an authenticated user with a future booking opens home
- **THEN** the upcoming booking card appears above categories, showing service, provider, and Jalali date/time, and tapping it opens the appointment detail

#### Scenario: Section fails to load
- **WHEN** the top-providers request fails but categories succeed
- **THEN** categories render normally and the providers section shows an inline retry affordance

#### Scenario: Guest home
- **WHEN** a guest opens home
- **THEN** search, categories, top providers, and promotions render with no upcoming-booking section and no login prompt blocking content

### Requirement: Pull-to-refresh on content screens
Home, explore results, and appointments SHALL support pull-to-refresh using the platform-standard refresh indicator, re-fetching visible content while preserving scroll context where content is unchanged.

#### Scenario: Refresh home
- **WHEN** the user pulls down on home
- **THEN** a refresh indicator appears and all sections re-fetch

### Requirement: Explore searches as the user types
The explore search field SHALL search as the user types, debounced and without a submit action, cancelling any in-flight request when the query changes so a stale response can never overwrite a newer one. Loading SHALL use skeleton cards; result cards SHALL show image, name, rating, and distance/location.

#### Scenario: Debounced typing
- **WHEN** the user types a query
- **THEN** results update after a short debounce without a submit action, and stale in-flight results never overwrite newer ones

#### Scenario: No results
- **WHEN** a search returns no matches
- **THEN** an empty state explains no results were found for that query and offers clearing the search/filters
