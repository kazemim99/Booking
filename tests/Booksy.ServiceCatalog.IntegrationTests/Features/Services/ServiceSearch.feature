Feature: Service Search and Filtering
  As a customer or provider
  I want to search and filter services
  So that I can find services that meet my specific criteria

  Background:
    Given the following providers exist with services:
      | Provider           | Service         | Category      | Price | Duration | Status   | Type     | Mobile |
      | Beauty Salon       | Haircut         | Hair Services | 50.00 | 30       | Active   | Standard | false  |
      | Beauty Salon       | Hair Color      | Hair Services | 120.00| 90       | Active   | Premium  | false  |
      | Spa & Wellness     | Swedish Massage | Spa Services  | 80.00 | 60       | Active   | Standard | true   |
      | Spa & Wellness     | Hot Stone       | Spa Services  | 110.00| 90       | Active   | Luxury   | false  |
      | Mobile Barber      | Mobile Haircut  | Hair Services | 60.00 | 40       | Active   | Standard | true   |
      | Nail Studio        | Manicure        | Nail Services | 35.00 | 45       | Active   | Standard | false  |
      | Nail Studio        | Pedicure        | Nail Services | 45.00 | 60       | Active   | Standard | false  |
      | Fitness Center     | Personal Training| Fitness      | 75.00 | 60       | Active   | Standard | false  |

  # ==================== BASIC SEARCH ====================

  @search @basic
  Scenario: Search services by name
    When I search for services with query "Haircut"
    Then the response status code should be 200
    And the response should contain 2 services
    And all services should have "Haircut" in the name

  @search @case-insensitive
  Scenario: Search is case-insensitive
    When I search for services with query "MASSAGE"
    Then the response status code should be 200
    And the response should contain 1 service
    And the service name should contain "Swedish Massage"

  @search @partial-match
  Scenario: Search with partial name match
    When I search for services with query "Mass"
    Then the response status code should be 200
    And the response should contain at least 1 service

  @search @empty-query
  Scenario: Search with empty query returns all active services
    When I search for services with empty query
    Then the response status code should be 200
    And the response should contain 8 services

  @search @no-results
  Scenario: Search with no matching results
    When I search for services with query "NonexistentService"
    Then the response status code should be 200
    And the response should contain 0 services

  # ==================== CATEGORY FILTERING ====================

  @search @filter @category
  Scenario: Filter services by single category
    When I search for services with:
      | Parameter | Value         |
      | Category  | Hair Services |
    Then the response status code should be 200
    And the response should contain 3 services
    And all services should be in category "Hair Services"

  @search @filter @category-empty
  Scenario: Filter by non-existent category
    When I search for services with:
      | Parameter | Value              |
      | Category  | Nonexistent Category|
    Then the response status code should be 200
    And the response should contain 0 services

  # ==================== PRICE FILTERING ====================

  @search @filter @price-range
  Scenario: Filter services by price range
    When I search for services with:
      | Parameter | Value |
      | MinPrice  | 40.00 |
      | MaxPrice  | 80.00 |
    Then the response status code should be 200
    And all services should have price between 40.00 and 80.00

  @search @filter @min-price-only
  Scenario: Filter services with minimum price
    When I search for services with:
      | Parameter | Value  |
      | MinPrice  | 100.00 |
    Then the response status code should be 200
    And all services should have price >= 100.00

  @search @filter @max-price-only
  Scenario: Filter services with maximum price
    When I search for services with:
      | Parameter | Value |
      | MaxPrice  | 50.00 |
    Then the response status code should be 200
    And all services should have price <= 50.00

  @search @filter @price-exact-match
  Scenario: Filter services with exact price
    When I search for services with:
      | Parameter | Value |
      | MinPrice  | 50.00 |
      | MaxPrice  | 50.00 |
    Then the response status code should be 200
    And all services should have price exactly 50.00

  # ==================== DURATION FILTERING ====================

  @search @filter @duration-range
  Scenario: Filter services by duration range
    When I search for services with:
      | Parameter     | Value |
      | MinDuration   | 45    |
      | MaxDuration   | 70    |
    Then the response status code should be 200
    And all services should have duration between 45 and 70 minutes

  @search @filter @short-services
  Scenario: Find quick services under 40 minutes
    When I search for services with:
      | Parameter   | Value |
      | MaxDuration | 40    |
    Then the response status code should be 200
    And all services should have duration <= 40 minutes

  @search @filter @long-services
  Scenario: Find extended services over 60 minutes
    When I search for services with:
      | Parameter   | Value |
      | MinDuration | 60    |
    Then the response status code should be 200
    And all services should have duration >= 60 minutes

  # ==================== TYPE FILTERING ====================

  @search @filter @service-type
  Scenario Outline: Filter services by type
    When I search for services with:
      | Parameter   | Value        |
      | ServiceType | <Type>       |
    Then the response status code should be 200
    And all services should be of type "<Type>"

    Examples:
      | Type     |
      | Standard |
      | Premium  |
      | Luxury   |

  # ==================== MOBILE SERVICE FILTERING ====================

  @search @filter @mobile-only
  Scenario: Filter for mobile services only
    When I search for services with:
      | Parameter        | Value |
      | AvailableAsMobile| true  |
    Then the response status code should be 200
    And the response should contain 2 services
    And all services should be available as mobile

  @search @filter @location-only
  Scenario: Filter for in-location services only
    When I search for services with:
      | Parameter           | Value |
      | AvailableAtLocation | true  |
      | AvailableAsMobile   | false |
    Then the response status code should be 200
    And all services should be location-based only

  # ==================== DEPOSIT FILTERING ====================

  @search @filter @requires-deposit
  Scenario: Filter services requiring deposit
    Given some services require a deposit
    When I search for services with:
      | Parameter       | Value |
      | RequiresDeposit | true  |
    Then the response status code should be 200
    And all services should require deposit

  @search @filter @no-deposit
  Scenario: Filter services without deposit requirement
    When I search for services with:
      | Parameter       | Value |
      | RequiresDeposit | false |
    Then the response status code should be 200
    And all services should not require deposit

  # ==================== COMBINED FILTERS ====================

  @search @filter @multiple-criteria
  Scenario: Search with multiple filter criteria
    When I search for services with:
      | Parameter   | Value         |
      | Category    | Hair Services |
      | MinPrice    | 40.00         |
      | MaxPrice    | 80.00         |
      | MinDuration | 30            |
      | MaxDuration | 50            |
    Then the response status code should be 200
    And all services should match all criteria

  @search @filter @complex-combination
  Scenario: Complex search with many filters
    When I search for services with:
      | Parameter         | Value         |
      | Category          | Spa Services  |
      | MinPrice          | 70.00         |
      | ServiceType       | Standard      |
      | AvailableAsMobile | true          |
    Then the response status code should be 200
    And all services should match all specified filters

  @search @filter @contradictory-filters
  Scenario: Search with contradictory filters returns empty
    When I search for services with:
      | Parameter | Value  |
      | MinPrice  | 100.00 |
      | MaxPrice  | 50.00  |
    Then the response status code should be 200
    And the response should contain 0 services

  # ==================== SORTING ====================

  @search @sort @price-ascending
  Scenario: Sort services by price ascending
    When I search for services sorted by "Price" in "Ascending" order
    Then the response status code should be 200
    And services should be ordered by price ascending

  @search @sort @price-descending
  Scenario: Sort services by price descending
    When I search for services sorted by "Price" in "Descending" order
    Then the response status code should be 200
    And services should be ordered by price descending

  @search @sort @duration-ascending
  Scenario: Sort services by duration ascending
    When I search for services sorted by "Duration" in "Ascending" order
    Then the response status code should be 200
    And services should be ordered by duration ascending

  @search @sort @duration-descending
  Scenario: Sort services by duration descending
    When I search for services sorted by "Duration" in "Descending" order
    Then the response status code should be 200
    And services should be ordered by duration descending

  @search @sort @name-alphabetical
  Scenario: Sort services by name alphabetically
    When I search for services sorted by "Name" in "Ascending" order
    Then the response status code should be 200
    And services should be ordered alphabetically by name

  @search @sort @popularity
  Scenario: Sort services by popularity (booking count)
    Given services have different booking counts
    When I search for services sorted by "Popularity" in "Descending" order
    Then the response status code should be 200
    And services should be ordered by booking count descending

  @search @sort @invalid-field
  Scenario: Invalid sort field returns error
    When I search for services sorted by "InvalidField" in "Ascending" order
    Then the response status code should be 400
    And the error message should contain "sort field"

  # ==================== SORTING WITH FILTERS ====================

  @search @sort @filter-and-sort
  Scenario: Combine filtering and sorting
    When I search for services with:
      | Parameter   | Value         |
      | Category    | Hair Services |
      | SortBy      | Price         |
      | SortOrder   | Ascending     |
    Then the response status code should be 200
    And all services should be in category "Hair Services"
    And services should be ordered by price ascending

  # ==================== PAGINATION WITH SEARCH ====================

  @search @pagination @first-page
  Scenario: Paginated search results - first page
    When I search for services with pagination:
      | Parameter | Value |
      | Page      | 1     |
      | PageSize  | 5     |
    Then the response status code should be 200
    And the response should contain 5 services
    And pagination metadata should indicate page 1

  @search @pagination @subsequent-page
  Scenario: Paginated search results - subsequent page
    When I search for services with pagination:
      | Parameter | Value |
      | Page      | 2     |
      | PageSize  | 5     |
    Then the response status code should be 200
    And the response should contain 3 services
    And pagination metadata should indicate page 2

  @search @pagination @with-filters
  Scenario: Paginated search with filters
    When I search for services with:
      | Parameter | Value         |
      | Category  | Hair Services |
      | Page      | 1             |
      | PageSize  | 2             |
    Then the response status code should be 200
    And the response should contain 2 services
    And pagination should reflect filtered total count

  # ==================== PROVIDER-SPECIFIC SEARCH ====================

  @search @provider @all-services
  Scenario: Get all services for specific provider
    When I search for services from provider "Beauty Salon"
    Then the response status code should be 200
    And the response should contain 2 services
    And all services should belong to "Beauty Salon"

  @search @provider @with-status-filter
  Scenario: Get provider services filtered by status
    Given provider "Spa & Wellness" has inactive services
    When I search for services from provider "Spa & Wellness" with status "Active"
    Then the response status code should be 200
    And all services should be active
    And all services should belong to "Spa & Wellness"

  # ==================== GEOGRAPHIC SEARCH ====================

  @search @location @by-city
  Scenario: Search services by provider city
    Given providers are in different cities
    When I search for services in city "New York"
    Then the response status code should be 200
    And all service providers should be in "New York"

  @search @location @by-state
  Scenario: Search services by state
    Given providers are in different states
    When I search for services in state "California"
    Then the response status code should be 200
    And all service providers should be in "California"

  @search @location @nearby
  Scenario: Search services near coordinates
    Given providers have location coordinates
    When I search for services near coordinates:
      | Latitude  | -73.935242 |
      | Longitude | 40.730610  |
      | Radius    | 5          |
    Then the response status code should be 200
    And all services should be within 5 km radius

  # ==================== PERFORMANCE & EDGE CASES ====================

  @search @performance @large-result-set
  Scenario: Handle search returning many results
    Given there are 1000 services in the database
    When I search for services with pageSize 100
    Then the response status code should be 200
    And the response should return within 2 seconds

  @search @edge-case @special-characters
  Scenario: Search with special characters
    When I search for services with query "Hair & Nail"
    Then the response status code should be 200
    And the search should handle special characters correctly

  @search @edge-case @very-long-query
  Scenario: Search with very long query string
    When I search for services with a query of 500 characters
    Then the response status code should be 400
    And the error message should contain "query length"

  # ==================== STATUS FILTERING ====================

  @search @status @active-only
  Scenario: Default search returns only active services
    Given there are services with different statuses
    When I search for services without status filter
    Then the response status code should be 200
    And all services should have status "Active"

  @search @status @include-inactive
  Scenario: Provider can search own inactive services
    Given I am authenticated as a provider
    And I have services with different statuses
    When I search for my services with status "Inactive"
    Then the response status code should be 200
    And all services should have status "Inactive"

  @search @status @admin-all-statuses
  Scenario: Admin can search services in any status
    Given I am authenticated as an admin
    And there are services in all statuses
    When I search for services with status "Archived"
    Then the response status code should be 200
    And all services should have status "Archived"

  # ==================== POPULAR SERVICES ====================

  @search @popular @default-limit
  Scenario: Get popular services with default limit
    Given services have varying booking counts
    When I request popular services
    Then the response status code should be 200
    And the response should contain up to 20 services
    And services should be ordered by popularity

  @search @popular @custom-limit
  Scenario: Get popular services with custom limit
    When I request popular services with limit 10
    Then the response status code should be 200
    And the response should contain up to 10 services

  @search @popular @by-category
  Scenario: Get popular services in specific category
    When I request popular services in category "Hair Services"
    Then the response status code should be 200
    And all services should be in category "Hair Services"
    And services should be ordered by popularity

  @search @popular @invalid-limit
  Scenario Outline: Validate popular services limit
    When I request popular services with limit <Limit>
    Then the response status code should be <StatusCode>

    Examples:
      | Limit | StatusCode |
      | 1     | 200        |
      | 50    | 200        |
      | 100   | 200        |
      | 0     | 400        |
      | -5    | 400        |
      | 101   | 400        |
