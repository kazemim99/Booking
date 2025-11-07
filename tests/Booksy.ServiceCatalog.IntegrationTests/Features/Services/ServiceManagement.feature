Feature: Service Management
  As a provider
  I want to manage my service offerings
  So that customers can book the services I provide

  Background:
    Given a provider "Test Salon" exists with active status
    And I am authenticated as the provider

  @smoke @service @create
  Scenario: Provider creates a new service
    When I send a POST request to create a service with:
      | Field           | Value                    |
      | ServiceName     | Haircut                  |
      | Description     | Professional haircut     |
      | Duration        | 30                       |
      | BasePrice       | 50.00                    |
      | Currency        | USD                      |
      | Category        | Hair Services            |
      | IsMobileService | false                    |
    Then the response status code should be 201
    And the response should contain a service with:
      | Field           | Value    |
      | Name            | Haircut  |
      | DurationMinutes | 30       |
      | Price           | 50.00    |
    And the service should exist in the database

  @service @create @multiple
  Scenario: Provider creates multiple services
    When I create 5 different services
    Then all services should be created successfully
    And the provider should have 5 services

  @service @update
  Scenario: Provider updates a service
    Given the provider has a service "Haircut"
    When I send a PUT request to update the service with:
      | Field       | Value                  |
      | ServiceName | Premium Haircut        |
      | BasePrice   | 75.00                  |
      | Description | Premium haircut service|
    Then the response status code should be 200
    And the service should be updated in the database

  @service @update @idempotent
  Scenario: Provider updates service with same values (idempotent)
    Given the provider has a service "Haircut" with price 50.00
    When I send a PUT request to update the service with:
      | Field       | Value   |
      | ServiceName | Haircut |
      | BasePrice   | 50.00   |
    Then the response status code should be 200
    And the service should remain unchanged in the database

  @service @update @optional-fields
  Scenario: Provider updates service with optional fields
    Given the provider has a service "Basic Haircut"
    When I send a PUT request to update the service with:
      | Field              | Value                              |
      | ServiceName        | Deluxe Haircut                     |
      | Description        | Premium styling with consultation  |
      | BasePrice          | 85.00                              |
      | Duration           | 45                                 |
      | PreparationMinutes | 10                                 |
      | BufferMinutes      | 15                                 |
      | ImageUrl           | https://example.com/deluxe.jpg     |
    Then the response status code should be 200
    And the service should have all updated fields in the database

  @service @delete
  Scenario: Provider deletes a service
    Given the provider has a service "Old Service"
    When I send a DELETE request to remove the service
    Then the response status code should be 200
    And the service should be deleted from the database

  @service @view @list
  Scenario: View all services for a provider
    Given the provider has 3 services
    When I send a GET request to "/api/v1/providers/{providerId}/services"
    Then the response status code should be 200
    And the response should contain 3 services

  @service @view @details
  Scenario: View service details
    Given the provider has a service "Haircut"
    When I send a GET request to view the service details
    Then the response status code should be 200
    And the response should contain complete service information

  @service @view @with-optional-fields
  Scenario: View service with all optional fields populated
    Given the provider has a service "Spa Package" with:
      | Field              | Value                          |
      | ImageUrl           | https://example.com/spa.jpg    |
      | PreparationMinutes | 15                             |
      | BufferMinutes      | 10                             |
      | MaxAdvanceBookingDays | 90                          |
      | MinAdvanceBookingHours | 24                         |
    When I send a GET request to view the service details
    Then the response status code should be 200
    And the response should contain all optional fields

  @service @options
  Scenario: Add options to a service
    Given the provider has a service "Haircut"
    When I add service options:
      | Option        | Price | Duration |
      | Beard Trim    | 10.00 | 10       |
      | Hair Styling  | 20.00 | 15       |
    Then the response status code should be 200
    And the service should have 2 options

  @service @price-tiers
  Scenario: Configure price tiers for a service
    Given the provider has a service "Massage"
    When I configure price tiers:
      | Duration | Price  |
      | 30       | 50.00  |
      | 60       | 90.00  |
      | 90       | 120.00 |
    Then the response status code should be 200
    And the service should have 3 price tiers

  @service @negative @unauthorized
  Scenario: Provider cannot create service for another provider
    Given another provider "Competitor" exists
    When I send a POST request to create a service for the competitor
    Then the response status code should be 403

  @service @negative @validation @empty-name
  Scenario: Cannot create service with empty name
    When I send a POST request to create a service with empty name
    Then the response status code should be 400
    And the error should indicate invalid service name

  @service @negative @validation @whitespace-name
  Scenario: Cannot create service with whitespace-only name
    When I send a POST request to create a service with:
      | Field       | Value |
      | ServiceName |       |
      | BasePrice   | 50.00 |
      | Duration    | 30    |
    Then the response status code should be 400
    And the error message should contain "name"

  @service @negative @validation @negative-price
  Scenario: Cannot create service with negative price
    When I send a POST request to create a service with price -10.00
    Then the response status code should be 400
    And the error should indicate invalid price

  @service @negative @validation @zero-price
  Scenario: Cannot create service with zero price
    When I send a POST request to create a service with price 0.00
    Then the response status code should be 400
    And the error should indicate invalid price

  @service @negative @validation @invalid-duration
  Scenario: Cannot create service with zero duration
    When I send a POST request to create a service with duration 0
    Then the response status code should be 400

  @service @negative @validation @negative-duration
  Scenario: Cannot create service with negative duration
    When I send a POST request to create a service with duration -30
    Then the response status code should be 400
    And the error message should contain "duration"

  @service @negative @validation @excessive-duration
  Scenario: Cannot create service with excessive duration
    When I send a POST request to create a service with duration 10000
    Then the response status code should be 400
    And the error message should contain "duration"

  @service @negative @delete-with-bookings
  Scenario: Cannot delete service with active bookings
    Given the provider has a service "Haircut"
    And there are 3 active bookings for the service
    When I send a DELETE request to remove the service
    Then the response status code should be 400
    And the error should indicate service has active bookings

  @service @search
  Scenario: Search services by category
    Given multiple providers have services in different categories
    When I send a GET request to "/api/v1/services/search?category=Hair Services"
    Then the response status code should be 200
    And all returned services should be in "Hair Services" category

  @service @search @price-range
  Scenario: Search services by price range
    Given multiple services exist with different prices
    When I send a GET request to "/api/v1/services/search?minPrice=30&maxPrice=70"
    Then the response status code should be 200
    And all returned services should be within the price range

  @service @mobile
  Scenario: Create mobile service
    When I send a POST request to create a service with:
      | Field           | Value          |
      | ServiceName     | Mobile Massage |
      | IsMobileService | true           |
      | TravelFee       | 20.00          |
    Then the response status code should be 201
    And the service should be marked as mobile service

  # ==================== ENHANCED VALIDATION SCENARIOS ====================

  @service @validation @xss-prevention
  Scenario: Cannot create service with HTML in description
    When I send a POST request to create a service with:
      | Field       | Value                                    |
      | ServiceName | Haircut                                  |
      | Description | <script>alert('xss')</script>            |
      | BasePrice   | 50.00                                    |
      | Duration    | 30                                       |
    Then the response status code should be 400
    And the error message should contain "invalid characters"

  @service @validation @xss-prevention-name
  Scenario: Cannot create service with script tags in name
    When I send a POST request to create a service with:
      | Field       | Value                           |
      | ServiceName | <img src=x onerror=alert(1)>    |
      | BasePrice   | 50.00                           |
      | Duration    | 30                              |
    Then the response status code should be 400
    And the error message should contain "invalid"

  @service @validation @unicode
  Scenario: Successfully create service with Unicode characters
    When I send a POST request to create a service with:
      | Field       | Value                    |
      | ServiceName | 理发 Corte de Pelo       |
      | Description | Multilingual service     |
      | BasePrice   | 50.00                    |
      | Duration    | 30                       |
      | Currency    | USD                      |
    Then the response status code should be 201
    And the service name should be stored correctly

  @service @validation @emoji
  Scenario: Successfully create service with emoji in name
    When I send a POST request to create a service with:
      | Field       | Value           |
      | ServiceName | Haircut ✂️💇    |
      | BasePrice   | 50.00           |
      | Duration    | 30              |
    Then the response status code should be 201

  @service @validation @currency-code
  Scenario Outline: Validate currency code format
    When I send a POST request to create a service with:
      | Field       | Value           |
      | ServiceName | Test Service    |
      | BasePrice   | 50.00           |
      | Duration    | 30              |
      | Currency    | <CurrencyCode>  |
    Then the response status code should be <StatusCode>

    Examples:
      | CurrencyCode | StatusCode |
      | USD          | 201        |
      | EUR          | 201        |
      | GBP          | 201        |
      | JPY          | 201        |
      | IRR          | 201        |
      | US           | 400        |
      | Dollar       | 400        |
      | $            | 400        |
      | 123          | 400        |

  @service @validation @sql-injection
  Scenario: Prevent SQL injection in service name
    When I send a POST request to create a service with:
      | Field       | Value                           |
      | ServiceName | '; DROP TABLE Services; --      |
      | BasePrice   | 50.00                           |
      | Duration    | 30                              |
    Then the response status code should be 400
    And no tables should be dropped

  @service @validation @excessive-length
  Scenario: Cannot create service with excessively long name
    When I send a POST request to create a service with a name of 300 characters
    Then the response status code should be 400
    And the error message should contain "name length"

  @service @validation @excessive-description
  Scenario: Cannot create service with excessively long description
    When I send a POST request to create a service with a description of 5000 characters
    Then the response status code should be 400
    And the error message should contain "description length"

  # ==================== AUTHORIZATION SCENARIOS ====================

  @service @authorization @unauthenticated
  Scenario: Unauthenticated user cannot create service
    Given I am not authenticated
    When I send a POST request to create a service with:
      | Field       | Value        |
      | ServiceName | Test Service |
      | BasePrice   | 50.00        |
      | Duration    | 30           |
    Then the response status code should be 401

  @service @authorization @customer-role
  Scenario: Customer cannot create service
    Given I am authenticated as a customer
    When I send a POST request to create a service with:
      | Field       | Value        |
      | ServiceName | Test Service |
      | BasePrice   | 50.00        |
      | Duration    | 30           |
    Then the response status code should be 403

  @service @authorization @cross-provider-update
  Scenario: Provider cannot update another provider's service
    Given another provider "Competitor" has a service "Their Service"
    When I send a PUT request to update the competitor's service with:
      | Field       | Value        |
      | ServiceName | Hacked Name  |
      | BasePrice   | 0.01         |
    Then the response status code should be 403

  @service @authorization @cross-provider-delete
  Scenario: Provider cannot delete another provider's service
    Given another provider "Competitor" has a service "Their Service"
    When I send a DELETE request to remove the competitor's service
    Then the response status code should be 403

  @service @authorization @admin-full-access
  Scenario: Admin can view any provider's services
    Given I am authenticated as an admin
    And another provider "Any Provider" has a service "Their Service"
    When I send a GET request to view the other provider's service details
    Then the response status code should be 200

  @service @authorization @customer-view-active-only
  Scenario: Customer can only view active services
    Given I am authenticated as a customer
    And a provider has an inactive service "Hidden Service"
    When I send a GET request to view the inactive service details
    Then the response status code should be 404

  @service @authorization @provider-view-own-inactive
  Scenario: Provider can view own inactive services
    Given the provider has an inactive service "Draft Service"
    When I send a GET request to view the service details
    Then the response status code should be 200

  # ==================== STATUS TRANSITION SCENARIOS ====================

  @service @status @activate
  Scenario: Activate an inactive service
    Given the provider has an inactive service "Seasonal Service"
    When I send a POST request to "/api/v1/services/{serviceId}/activate"
    Then the response status code should be 200
    And the service status should be "Active" in the database

  @service @status @deactivate
  Scenario: Deactivate an active service
    Given the provider has an active service "Temporary Service"
    When I send a POST request to "/api/v1/services/{serviceId}/deactivate"
    Then the response status code should be 200
    And the service status should be "Inactive" in the database

  @service @status @archive
  Scenario: Archive a service without bookings
    Given the provider has an inactive service "Old Service"
    And the service has no upcoming bookings
    When I send a DELETE request to archive the service
    Then the response status code should be 204
    And the service status should be "Archived" in the database

  @service @status @cannot-archive-with-bookings
  Scenario: Cannot archive service with upcoming bookings
    Given the provider has a service "Popular Service"
    And there are 5 upcoming bookings for the service
    When I send a DELETE request to archive the service
    Then the response status code should be 409
    And the error message should contain "upcoming bookings"

  @service @status @publish-incomplete
  Scenario: Cannot activate service with incomplete required fields
    Given the provider has a draft service missing required fields
    When I send a POST request to "/api/v1/services/{serviceId}/activate"
    Then the response status code should be 400
    And the error message should contain "required fields"

  # ==================== PAGINATION SCENARIOS ====================

  @service @pagination @basic
  Scenario: List services with pagination
    Given the provider has 25 services
    When I send a GET request to "/api/v1/providers/{providerId}/services?page=1&pageSize=10"
    Then the response status code should be 200
    And the response should contain 10 services
    And the response should include pagination metadata

  @service @pagination @last-page
  Scenario: Retrieve last page with fewer items
    Given the provider has 23 services
    When I send a GET request to "/api/v1/providers/{providerId}/services?page=3&pageSize=10"
    Then the response status code should be 200
    And the response should contain 3 services

  @service @pagination @empty-page
  Scenario: Request page beyond available data
    Given the provider has 10 services
    When I send a GET request to "/api/v1/providers/{providerId}/services?page=5&pageSize=10"
    Then the response status code should be 200
    And the response should contain 0 services

  @service @pagination @invalid-parameters
  Scenario Outline: Handle invalid pagination parameters
    Given the provider has 10 services
    When I send a GET request to "/api/v1/providers/{providerId}/services?page=<Page>&pageSize=<PageSize>"
    Then the response status code should be <StatusCode>

    Examples:
      | Page | PageSize | StatusCode |
      | 0    | 10       | 400        |
      | -1   | 10       | 400        |
      | 1    | 0        | 400        |
      | 1    | -5       | 400        |
      | 1    | 1001     | 400        |

  # ==================== CONCURRENCY SCENARIOS ====================

  @service @concurrency @optimistic-locking
  Scenario: Optimistic concurrency control on update
    Given the provider has a service "Haircut"
    And the service has version 1
    When two concurrent update requests are sent for the same service
    Then one request should succeed with status 200
    And one request should fail with status 409
    And the error message should contain "concurrency"

  @service @concurrency @stale-data
  Scenario: Cannot update service with stale version
    Given the provider has a service "Massage" at version 1
    And the service has been updated to version 2
    When I send an update request with version 1
    Then the response status code should be 409
    And the error message should contain "version"

  # ==================== BOUNDARY VALUE TESTING ====================

  @service @boundary @minimum-price
  Scenario: Create service with minimum valid price
    When I send a POST request to create a service with:
      | Field       | Value        |
      | ServiceName | Cheap Service|
      | BasePrice   | 0.01         |
      | Duration    | 15           |
    Then the response status code should be 201

  @service @boundary @maximum-price
  Scenario: Create service with high price
    When I send a POST request to create a service with:
      | Field       | Value           |
      | ServiceName | Luxury Service  |
      | BasePrice   | 99999.99        |
      | Duration    | 30              |
    Then the response status code should be 201

  @service @boundary @minimum-duration
  Scenario: Create service with minimum duration
    When I send a POST request to create a service with:
      | Field       | Value          |
      | ServiceName | Quick Service  |
      | BasePrice   | 10.00          |
      | Duration    | 5              |
    Then the response status code should be 201

  @service @boundary @maximum-duration
  Scenario: Create service with maximum duration
    When I send a POST request to create a service with:
      | Field       | Value           |
      | ServiceName | Marathon Service|
      | BasePrice   | 500.00          |
      | Duration    | 480             |
    Then the response status code should be 201

  # ==================== NULL VS MISSING FIELDS ====================

  @service @validation @null-optional-field
  Scenario: Create service with explicitly null optional field
    When I send a POST request with null ImageUrl
    Then the response status code should be 201
    And the service should have null ImageUrl

  @service @validation @missing-optional-field
  Scenario: Create service with missing optional field
    When I send a POST request without ImageUrl field
    Then the response status code should be 201
    And the service should have null ImageUrl

  # ==================== DOMAIN EVENTS ====================

  @service @events @service-created
  Scenario: ServiceCreatedEvent is published on creation
    When I send a POST request to create a service successfully
    Then ServiceCreatedEvent should be published
    And the event should contain correct service data

  @service @events @service-updated
  Scenario: ServiceUpdatedEvent is published on update
    Given the provider has a service "Test Service"
    When I send a PUT request to update the service
    Then ServiceUpdatedEvent should be published
    And the event should contain old and new values

  @service @events @service-archived
  Scenario: ServiceArchivedEvent is published on archive
    Given the provider has a service "Old Service"
    When I send a DELETE request to archive the service
    Then ServiceArchivedEvent should be published
    And the event should contain the reason
