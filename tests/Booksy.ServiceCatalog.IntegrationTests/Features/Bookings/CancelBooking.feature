Feature: Cancel Booking
  As a customer
  I want to cancel my bookings
  So that I can free up time slots when I can't attend

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has a service "Haircut" priced at 50.00 USD
    And the provider has business hours configured
    And I am authenticated as a customer
    And I have a booking for "Haircut" scheduled for tomorrow at 10:00

  @smoke @booking @cancel
  Scenario: Customer successfully cancels their own booking
    When I send a POST request to cancel the booking with:
      | Field  | Value              |
      | Reason | Change of plans    |
    Then the response status code should be 200
    And the response message should contain "cancelled successfully"
    And the booking should have status "Cancelled" in the database

  @booking @cancel @negative @authorization
  Scenario: Customer cannot cancel another customer's booking
    Given another customer has a booking for the same service
    When I send a POST request to cancel the other customer's booking with:
      | Field  | Value           |
      | Reason | Not authorized  |
    Then the response status code should be 403

  @booking @cancel
  Scenario: Provider can cancel customer booking
    Given I am authenticated as the provider
    When I send a POST request to cancel the booking with:
      | Field  | Value                      |
      | Reason | Provider schedule conflict |
    Then the response status code should be 200
    And the booking should have status "Cancelled" in the database

  @booking @cancel @negative
  Scenario: Cannot cancel booking without authentication
    Given I am not authenticated
    When I send a POST request to cancel the booking with:
      | Field  | Value    |
      | Reason | Testing  |
    Then the response status code should be 401
