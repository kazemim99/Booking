Feature: Refund Payment
  As a customer or provider
  I want to process refunds for payments
  So that I can return funds when services are cancelled

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has a service "Haircut" priced at 100.00 USD
    And I am authenticated as a customer
    And I have a confirmed booking for "Haircut"
    And I have a completed payment of 100.00 USD for the booking

  @smoke @payment @refund
  Scenario: Process full refund for a paid payment
    When I send a POST request to refund the payment with:
      | Field  | Value           |
      | Amount | 100.00          |
      | Reason | CustomerRequest |
      | Notes  | Customer requested refund |
    Then the response status code should be 200
    And the response should contain a payment with:
      | Field  | Value    |
      | Status | Refunded |
    And the payment should have status "Refunded" in the database
    And the refunded amount should be 100.00

  @payment @refund
  Scenario: Process partial refund for a paid payment
    When I send a POST request to refund the payment with:
      | Field  | Value                     |
      | Amount | 50.00                     |
      | Reason | ServiceCancellation       |
      | Notes  | Partial service cancelled |
    Then the response status code should be 200
    And the response should contain a payment with:
      | Field  | Value              |
      | Status | PartiallyRefunded  |
    And the payment should have status "PartiallyRefunded" in the database
    And the refunded amount should be 50.00

  @payment @refund @negative @validation
  Scenario: Cannot refund without a reason
    When I send a POST request to refund the payment with:
      | Field  | Value                |
      | Amount | 100.00               |
      | Reason |                      |
      | Notes  | Missing reason test  |
    Then the response status code should be 400

  @payment @refund @negative
  Scenario: Cannot refund non-existent payment
    When I send a POST request to refund payment "00000000-0000-0000-0000-000000000000" with:
      | Field  | Value           |
      | Amount | 100.00          |
      | Reason | CustomerRequest |
    Then the response status code should be 404

  @payment @refund @negative
  Scenario: Cannot refund payment without authentication
    Given I am not authenticated
    When I send a POST request to refund the payment with:
      | Field  | Value           |
      | Amount | 100.00          |
      | Reason | CustomerRequest |
    Then the response status code should be 401
