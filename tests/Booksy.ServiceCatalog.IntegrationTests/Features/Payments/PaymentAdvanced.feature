Feature: Payment Capture and Advanced Operations
  As a system and provider
  I want to handle complex payment scenarios
  So that financial transactions are properly managed

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has a service "Haircut" priced at 100.00 USD
    And I am authenticated as a customer
    And I have a confirmed booking for "Haircut"

  @payment @capture
  Scenario: Capture authorized payment
    Given I have an authorized but not captured payment of 100.00 USD
    When I send a POST request to capture the payment with full amount
    Then the response status code should be 200
    And the payment should have status "Paid" in the database

  @payment @capture @partial
  Scenario: Partial capture of authorized payment
    Given I have an authorized but not captured payment of 100.00 USD
    When I send a POST request to capture the payment with:
      | Field  | Value  |
      | Amount | 50.00  |
    Then the response status code should be 200
    And the payment should have captured amount of 50.00

  @payment @history
  Scenario: Customer views payment history
    Given I have 3 completed payments
    When I send a GET request to "/api/v1/payments/customer/{customerId}"
    Then the response status code should be 200
    And the response should contain 3 payments

  @payment @details
  Scenario: View payment details with transaction history
    Given I have a completed payment of 100.00 USD for the booking
    When I send a GET request to view the payment details
    Then the response status code should be 200
    And the response should include transaction history

  @payment @pricing
  Scenario: Calculate pricing with tax and fees
    When I send a POST request to calculate pricing with:
      | Field                  | Value |
      | BaseAmount             | 100.00|
      | Currency               | USD   |
      | TaxPercentage          | 10    |
      | TaxInclusive           | false |
      | DiscountPercentage     | 15    |
      | PlatformFeePercentage  | 5     |
      | DepositPercentage      | 30    |
    Then the response status code should be 200
    And the pricing breakdown should be calculated correctly

  @payment @pricing @vat
  Scenario: Calculate pricing with inclusive tax (VAT style)
    When I send a POST request to calculate pricing with:
      | Field         | Value |
      | BaseAmount    | 120.00|
      | Currency      | USD   |
      | TaxPercentage | 20    |
      | TaxInclusive  | true  |
    Then the response status code should be 200
    And the tax amount should be 20.00

  @payment @pricing @anonymous
  Scenario: Anonymous user can calculate pricing
    Given I am not authenticated
    When I send a POST request to calculate pricing with:
      | Field         | Value  |
      | BaseAmount    | 100.00 |
      | Currency      | USD    |
      | TaxPercentage | 10     |
    Then the response status code should be 200

  @payment @multiple
  Scenario: Customer has multiple payments for different bookings
    Given I have confirmed bookings for multiple services
    And I have payments for each booking
    When I send a GET request to view my payment history
    Then the response status code should be 200
    And each payment should be correctly associated with its booking
