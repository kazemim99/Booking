Feature: Create Booking
  As a customer
  I want to create bookings for services
  So that I can schedule appointments with service providers

  Background:
    Given a provider "Beauty Salon Alpha" exists with the following details:
      | Field        | Value                |
      | BusinessName | Beauty Salon Alpha   |
      | Type         | Salon                |
      | Status       | Active               |
    And the provider has a service "Haircut" with:
      | Field    | Value   |
      | Name     | Haircut |
      | Price    | 50.00   |
      | Duration | 60      |
      | Currency | USD     |
    And the provider has business hours configured
    And the provider has at least one staff member

  @smoke @booking @create
  Scenario: Customer creates a valid booking
    Given I am authenticated as a customer
    When I send a POST request to create a booking with:
      | Field      | Value                            |
      | ServiceId  | [Service:Haircut:Id]             |
      | StartTime  | 2 days from now at 10:00         |
      | Notes      | First time customer              |
    Then the response status code should be 201
    And the response should contain a booking with:
      | Field      | Value                            |
      | Status     | Requested                        |
      | ServiceId  | [Service:Haircut:Id]             |
    And the booking should exist in the database with status "Requested"

  @smoke @booking @create @negative
  Scenario: Customer cannot create booking without authentication
    Given I am not authenticated
    When I send a POST request to create a booking with:
      | Field      | Value                            |
      | ServiceId  | [Service:Haircut:Id]             |
      | StartTime  | 2 days from now at 10:00         |
    Then the response status code should be 401

  @booking @create @negative @validation
  Scenario: Customer cannot create booking in the past
    Given I am authenticated as a customer
    When I send a POST request to create a booking with:
      | Field      | Value                            |
      | ServiceId  | [Service:Haircut:Id]             |
      | StartTime  | 1 day ago at 10:00               |
    Then the response status code should be 400

  @booking @create @negative
  Scenario: Customer cannot create booking for non-existent service
    Given I am authenticated as a customer
    When I send a POST request to create a booking with:
      | Field      | Value                            |
      | ServiceId  | 00000000-0000-0000-0000-000000000000 |
      | StartTime  | 2 days from now at 10:00         |
    Then the response status code should be 404

  @booking @create
  Scenario Outline: Customer books at different times
    Given I am authenticated as a customer
    When I send a POST request to create a booking with:
      | Field      | Value              |
      | ServiceId  | [Service:Haircut:Id] |
      | StartTime  | <StartTime>        |
    Then the response status code should be <StatusCode>

    Examples:
      | StartTime                    | StatusCode |
      | 2 days from now at 10:00     | 201        |
      | 2 days from now at 14:00     | 201        |
      | 2 days from now at 18:00     | 400        |
      | 1 day ago at 10:00           | 400        |

  @smoke @booking @create @walkin
  Scenario: Provider-created walk-in is born Confirmed
    # The provider IS the approver: a booking they enter on their own
    # calendar must never wait in their own pending queue.
    Given I am authenticated as the provider
    When I send a POST request to create a booking with:
      | Field      | Value                            |
      | ServiceId  | [Service:Haircut:Id]             |
      | StartTime  | 2 days from now at 10:00         |
      | Notes      | Walk-in client                   |
    Then the response status code should be 201
    And the response should contain a booking with:
      | Field      | Value                            |
      | Status     | Confirmed                        |
    And the booking should exist in the database with status "Confirmed"

  @booking @create @multiservice
  Scenario: Multi-service visit sums duration and price
    Given the provider has a service "Hair color" with:
      | Field    | Value      |
      | Name     | Hair color |
      | Price    | 120.00     |
      | Duration | 90         |
      | Currency | USD        |
    And I am authenticated as a customer
    When I send a POST request to create a booking with:
      | Field      | Value                                        |
      | ServiceIds | [Service:Haircut:Id],[Service:Hair color:Id] |
      | StartTime  | 2 days from now at 10:00                     |
    Then the response status code should be 201
    And the booking should exist in the database with status "Requested"
    And the stored booking should have 2 service lines, 150 minutes and total price 170.00
