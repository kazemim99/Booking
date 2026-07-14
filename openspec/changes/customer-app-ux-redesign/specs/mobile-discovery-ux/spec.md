# mobile-discovery-ux

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

### Requirement: Explore search with live results
The explore screen SHALL provide a search field with debounced search-as-you-type across providers and services, category filter chips, and result cards showing image, name, rating, and distance/location. Loading SHALL use skeleton cards; typing a new query SHALL cancel the in-flight search.

#### Scenario: Debounced typing
- **WHEN** the user types a query
- **THEN** results update after a short debounce without a submit action, and stale in-flight results never overwrite newer ones

#### Scenario: No results
- **WHEN** a search returns no matches
- **THEN** an empty state explains no results were found for that query and offers clearing the search/filters

#### Scenario: Filter by category
- **WHEN** the user selects a category chip
- **THEN** results are constrained to that category and the active chip is visually distinct with accessible contrast

### Requirement: Provider detail screen
The app SHALL provide a provider detail screen (deep-linkable by provider id) showing gallery imagery with placeholders, name, rating, address with map affordance, working hours, and the bookable services list with prices and durations. The primary booking CTA SHALL remain visible without scrolling on a standard viewport.

#### Scenario: Open provider from explore
- **WHEN** the user taps a provider card
- **THEN** the detail screen loads with skeleton placeholders resolving to gallery, info, and services

#### Scenario: Provider images unavailable
- **WHEN** a provider has no gallery images or an image fails to load
- **THEN** a branded placeholder renders instead of a broken or empty image area

#### Scenario: Deep link to provider
- **WHEN** the app is opened via a provider deep link
- **THEN** the provider detail screen opens directly with a working back affordance to home
