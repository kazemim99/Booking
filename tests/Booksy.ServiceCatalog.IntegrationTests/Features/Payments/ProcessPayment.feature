Feature: Process Payment
  As a customer
  I want to process payments for my bookings
  So that I can secure my appointments

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has a service "Haircut" priced at 100.00 USD
    And I am authenticated as a customer
    And I have a confirmed booking for "Haircut"

  @smoke @payment @process
  Scenario: Process payment with immediate capture
    When I send a POST request to process a payment with:
      | Field              | Value        |
      | Amount             | 100.00       |
      | Currency           | USD          |
      | PaymentMethod      | CreditCard   |
      | PaymentMethodId    | pm_test_card |
      | CaptureImmediately | true         |
    Then the response status code should be 201
    And the response should contain a payment with:
      | Field    | Value  |
      | Status   | Paid   |
      | Amount   | 100.00 |
      | Currency | USD    |
    And a payment should exist in the database with status "Paid"

  @payment @process
  Scenario: Authorize payment without immediate capture
    When I send a POST request to process a payment with:
      | Field              | Value        |
      | Amount             | 100.00       |
      | Currency           | USD          |
      | PaymentMethod      | CreditCard   |
      | PaymentMethodId    | pm_test_card |
      | CaptureImmediately | false        |
    Then the response status code should be 201
    And the response should contain a payment with:
      | Field  | Value |
      | Status | Paid  |

  @payment @process @negative @validation
  Scenario: Cannot process payment with invalid amount
    When I send a POST request to process a payment with:
      | Field              | Value        |
      | Amount             | -50.00       |
      | Currency           | USD          |
      | PaymentMethod      | CreditCard   |
      | PaymentMethodId    | pm_test_card |
      | CaptureImmediately | true         |
    Then the response status code should be 400

  @payment @process @negative @validation
  Scenario: Cannot process payment with invalid currency
    When I send a POST request to process a payment with:
      | Field              | Value        |
      | Amount             | 100.00       |
      | Currency           | INVALID      |
      | PaymentMethod      | CreditCard   |
      | PaymentMethodId    | pm_test_card |
      | CaptureImmediately | true         |
    Then the response status code should be 400

  @payment @process @negative
  Scenario: Cannot process payment without authentication
    Given I am not authenticated
    When I send a POST request to process a payment with:
      | Field              | Value        |
      | Amount             | 100.00       |
      | Currency           | USD          |
      | PaymentMethod      | CreditCard   |
      | PaymentMethodId    | pm_test_card |
      | CaptureImmediately | true         |
    Then the response status code should be 401
