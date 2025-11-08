Feature: ZarinPal Payment Refunds
    As a provider or admin
    I want to refund ZarinPal payments
    So that I can return money to customers when needed

Background:
    Given a registered provider exists with:
        | Field        | Value          |
        | BusinessName | Beauty Center  |
        | BusinessType | BeautySalon    |
    And a completed ZarinPal payment exists with:
        | Field       | Value           |
        | Amount      | 500000          |
        | Currency    | IRR             |
        | Status      | Paid            |
        | Authority   | A123456789      |
        | RefNumber   | 987654321       |
        | CardPan     | 6274****1234    |

Scenario: Successfully refund full payment amount
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value                      |
        | Amount | 500000                     |
        | Reason | Customer requested refund  |
    Then the response status code should be 200
    And the response should indicate refund success
    And the payment should have status "Refunded" in the database
    And the refunded amount should be 500000

Scenario: Successfully process partial refund
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value                  |
        | Amount | 200000                 |
        | Reason | Partial service refund |
    Then the response status code should be 200
    And the payment should have status "PartiallyRefunded" in the database
    And the refunded amount should be 200000
    And the payment net amount should be 300000

Scenario: Process multiple partial refunds
    When I refund 150000 Rials with reason "First partial refund"
    And I refund 100000 Rials with reason "Second partial refund"
    Then the total refunded amount should be 250000
    And the payment should have status "PartiallyRefunded" in the database
    And the payment net amount should be 250000

Scenario: Full refund after partial refunds
    Given the payment has been partially refunded 200000 Rials
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value               |
        | Amount | 300000              |
        | Reason | Complete the refund |
    Then the response status code should be 200
    And the payment should have status "Refunded" in the database
    And the total refunded amount should be 500000

Scenario: Fail to refund more than payment amount
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value          |
        | Amount | 600000         |
        | Reason | Invalid refund |
    Then the response status code should be 400
    And the response should contain error "Refund amount exceeds available amount"

Scenario: Fail to refund already fully refunded payment
    Given the payment has been fully refunded
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value         |
        | Amount | 100000        |
        | Reason | Double refund |
    Then the response status code should be 400
    And the response should contain error "Payment is already fully refunded"

Scenario: Refund with detailed description
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field       | Value                                |
        | Amount      | 250000                               |
        | Reason      | Service cancelled                    |
        | Description | Customer cancelled due to emergency  |
    Then the response status code should be 200
    And the refund should have the description stored

Scenario: Refund creates transaction record
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value          |
        | Amount | 300000         |
        | Reason | Test refund    |
    Then a refund transaction should exist with:
        | Field  | Value     |
        | Type   | Refund    |
        | Amount | 300000    |
        | Status | Succeeded |

Scenario: Calculate net amount correctly after fee and refund
    Given the payment has fee of 5000 Rials
    When I refund 200000 Rials
    Then the payment net amount should be 295000
    # 500000 - 5000 (fee) - 200000 (refund) = 295000

Scenario: Refund minimum amount validation
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value        |
        | Amount | 0            |
        | Reason | Zero refund  |
    Then the response status code should be 400
    And the response should contain validation error for "Amount"

Scenario: Refund pending payment should fail
    Given a payment exists with status "Pending"
    When I attempt to refund the payment
    Then the response status code should be 400
    And the response should contain error "Cannot refund payment that is not paid"

Scenario: Refund failed payment should fail
    Given a payment exists with status "Failed"
    When I attempt to refund the payment
    Then the response status code should be 400
    And the response should contain error "Cannot refund failed payment"

Scenario: ZarinPal API refund failure handling
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value        |
        | Amount | 100000       |
        | Reason | Test refund  |
    And ZarinPal API returns refund error
    Then the response status code should be 500
    And the payment should still have status "Paid"
    And the refunded amount should remain 0

Scenario: Idempotent refund requests
    Given a refund request with idempotency key "refund-123"
    When I send the same refund request twice
    Then only one refund should be processed
    And both responses should return the same result

Scenario: Refund authorization check
    Given I am logged in as a customer
    When I attempt to refund a payment
    Then the response status code should be 403
    And the response should contain error "Unauthorized"

Scenario: Provider can refund their own payments
    Given I am logged in as the provider
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value               |
        | Amount | 100000              |
        | Reason | Provider initiated  |
    Then the response status code should be 200
    And the refund should be processed successfully

Scenario: Admin can refund any payment
    Given I am logged in as an admin
    When I send a POST request to "/api/v1/payments/refund" with:
        | Field  | Value           |
        | Amount | 150000          |
        | Reason | Admin refund    |
    Then the response status code should be 200
    And the refund should be processed successfully
