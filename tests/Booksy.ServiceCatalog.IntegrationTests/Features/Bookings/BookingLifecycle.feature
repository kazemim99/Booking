Feature: Complete Booking Lifecycle
  As a customer and provider
  I want to manage bookings through their complete lifecycle
  So that appointments can be properly tracked and managed

  Background:
    Given a provider "Test Salon" exists with active status
    And the provider has a service "Haircut" priced at 50.00 USD
    And the provider has business hours configured
    And the provider has at least one staff member

  @smoke @booking @complete
  Scenario: Customer completes a booking after service is rendered
    Given I am authenticated as a customer
    And I have a confirmed booking for "Haircut" scheduled for yesterday
    When I mark the booking as completed
    Then the response status code should be 200
    And the booking should have status "Completed" in the database

  @booking @reschedule
  Scenario: Customer reschedules booking to a new time
    Given I am authenticated as a customer
    And I have a booking for "Haircut" scheduled for tomorrow at 10:00
    When I send a POST request to reschedule the booking with:
      | Field        | Value                     |
      | NewStartTime | 3 days from now at 14:00  |
      | Reason       | Schedule conflict         |
    Then the response status code should be 200
    And the old booking should have status "Rescheduled" in the database
    And a new booking should exist for the new time slot

  @booking @no-show
  Scenario: Provider marks customer as no-show
    Given I am authenticated as a customer
    And I have a confirmed booking for "Haircut" scheduled for yesterday
    And I am authenticated as the provider
    When I mark the booking as no-show
    Then the response status code should be 200
    And the booking should have status "NoShow" in the database

  @booking @history
  Scenario: Customer views their booking history
    Given I am authenticated as a customer
    And I have 3 bookings in various states
    When I send a GET request to "/api/v1/bookings/my-bookings"
    Then the response status code should be 200
    And the response should contain 3 bookings

  @booking @history @filter
  Scenario Outline: Customer filters bookings by status
    Given I am authenticated as a customer
    And I have bookings with statuses: Requested, Confirmed, Cancelled
    When I send a GET request to "/api/v1/bookings/my-bookings?status=<Status>"
    Then the response status code should be 200
    And all returned bookings should have status "<Status>"

    Examples:
      | Status    |
      | Requested |
      | Confirmed |
      | Cancelled |

  @booking @confirm
  Scenario: Provider confirms a requested booking
    Given I am authenticated as a customer
    And I have a booking for "Haircut" scheduled for tomorrow at 10:00
    And I am authenticated as the provider
    When I confirm the booking
    Then the response status code should be 200
    And the booking should have status "Confirmed" in the database
    And the customer should receive a confirmation notification

  @booking @view
  Scenario: Customer views booking details
    Given I am authenticated as a customer
    And I have a confirmed booking for "Haircut"
    When I send a GET request to view the booking details
    Then the response status code should be 200
    And the response should contain complete booking information

  @booking @view @negative @authorization
  Scenario: Customer cannot view another customer's booking
    Given another customer has a booking for the same service
    When I send a GET request to view the other customer's booking
    Then the response status code should be 403

  @booking @provider-view
  Scenario: Provider views all their bookings
    Given I am authenticated as the provider
    And there are 5 bookings for the provider
    When I send a GET request to "/api/v1/bookings/provider/bookings"
    Then the response status code should be 200
    And the response should contain 5 bookings
