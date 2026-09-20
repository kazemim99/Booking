# customer-discovery-journey

## MODIFIED Requirements

### Requirement: Explore searches as the user types
The explore search field SHALL search as the user types, debounced and without a submit action, cancelling any in-flight request when the query changes so a stale response can never overwrite a newer one. Loading SHALL use skeleton cards; result cards SHALL show image, name, rating, and distance/location.

The rating on a result card SHALL be the provider's average over their published reviews, together with the count of those reviews. Both SHALL be returned by the search API; the count SHALL NOT be a placeholder that is always zero. A provider with no published reviews SHALL be shown an explicit "no reviews yet" treatment and SHALL NOT be shown a rating of zero, a zero-star row, or a rating borrowed from any other source.

Sorting by rating SHALL order by that same published-review average, and providers with no published reviews SHALL be placed as a band after every rated provider in **both** directions — below the lowest-rated when sorting descending, and below the highest-rated when sorting ascending. They SHALL NOT be interleaved as though rated zero in either direction.

#### Scenario: Debounced typing
- **WHEN** the user types a query
- **THEN** results update after a short debounce without a submit action, and stale in-flight results never overwrite newer ones

#### Scenario: No results
- **WHEN** a search returns no matches
- **THEN** an empty state explains no results were found for that query and offers clearing the search/filters

#### Scenario: Rated provider in results
- **WHEN** a result card is shown for a provider with published reviews
- **THEN** it shows that provider's published-review average and the number of published reviews

#### Scenario: Unrated provider in results
- **WHEN** a result card is shown for a provider with no published reviews
- **THEN** it shows a "no reviews yet" treatment rather than a rating of zero

#### Scenario: Sorting by rating descending
- **WHEN** the user sorts results by rating, highest first, and some providers have no published reviews
- **THEN** rated providers are ordered by their published-review average and every unrated provider appears after all of them

#### Scenario: Sorting by rating ascending
- **WHEN** the user sorts results by rating, lowest first, and some providers have no published reviews
- **THEN** rated providers are ordered lowest average first and every unrated provider still appears after all of them, never ahead of the lowest-rated

#### Scenario: Review count is real
- **WHEN** a result card is shown for a provider with published reviews
- **THEN** the review count returned by the search API is that provider's published review count and not a constant zero
