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

  @service @negative @validation @negative-price
  Scenario: Cannot create service with negative price
    When I send a POST request to create a service with price -10.00
    Then the response status code should be 400
    And the error should indicate invalid price

  @service @negative @validation @invalid-duration
  Scenario: Cannot create service with zero duration
    When I send a POST request to create a service with duration 0
    Then the response status code should be 400

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
