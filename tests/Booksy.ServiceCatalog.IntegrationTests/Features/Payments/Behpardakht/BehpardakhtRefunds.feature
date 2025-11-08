Feature: Behpardakht Payment Refunds
    As a merchant
    I want to refund Behpardakht payments
    So that I can return money to customers when needed

Background:
    Given a registered provider exists with:
        | Field         | Value                |
        | BusinessName  | Test Beauty Salon    |
        | BusinessType  | BeautySalon          |
        | Email         | provider@example.com |

Scenario: Successfully refund full amount
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 500000          |
        | Currency       | IRR             |
        | RefId          | REF123456       |
        | SaleOrderId    | 100001          |
        | SaleReferenceId| 123456789       |
        | CardPan        | 6104****1234    |
    When I refund 500000 Rials with reason "Full refund requested"
    Then the response status code should be 200
    And the refunded amount should be 500000
    And the payment should have status "Refunded" in the database

Scenario: Successfully refund partial amount
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 500000          |
        | Currency       | IRR             |
        | RefId          | REF123457       |
        | SaleOrderId    | 100002          |
        | SaleReferenceId| 123456790       |
        | CardPan        | 6104****5678    |
    When I refund 200000 Rials with reason "Partial service cancellation"
    Then the response status code should be 200
    And the refunded amount should be 200000
    And the payment should have status "PartiallyRefunded" in the database

Scenario: Refund multiple partial amounts
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 600000          |
        | Currency       | IRR             |
        | RefId          | REF123458       |
        | SaleOrderId    | 100003          |
        | SaleReferenceId| 123456791       |
        | CardPan        | 6104****9012    |
    And the payment has been partially refunded 150000 Rials
    When I refund 250000 Rials with reason "Additional refund"
    Then the response status code should be 200
    And the total refunded amount should be 400000

Scenario: Fail to refund more than payment amount
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 300000          |
        | Currency       | IRR             |
        | RefId          | REF123459       |
        | SaleOrderId    | 100004          |
        | SaleReferenceId| 123456792       |
        | CardPan        | 6104****3456    |
    When I refund 400000 Rials with reason "Exceeds amount"
    Then the response status code should be 400
    And the response should contain error "Refund amount exceeds payment amount"

Scenario: Fail to refund more than remaining amount
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 500000          |
        | Currency       | IRR             |
        | RefId          | REF123460       |
        | SaleOrderId    | 100005          |
        | SaleReferenceId| 123456793       |
        | CardPan        | 6104****7890    |
    And the payment has been partially refunded 300000 Rials
    When I refund 300000 Rials with reason "Exceeds remaining"
    Then the response status code should be 400
    And the response should contain error "Refund amount exceeds remaining amount"

Scenario: Fail to refund already fully refunded payment
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 400000          |
        | Currency       | IRR             |
        | RefId          | REF123461       |
        | SaleOrderId    | 100006          |
        | SaleReferenceId| 123456794       |
        | CardPan        | 6104****2345    |
    And the payment has been fully refunded
    When I refund 100000 Rials with reason "Already refunded"
    Then the response status code should be 400
    And the response should contain error "Payment already fully refunded"

Scenario: Refund with specific refund reason codes
    Given a completed Behpardakht payment exists with:
        | Field          | Value                  |
        | Amount         | 350000                 |
        | Currency       | IRR                    |
        | RefId          | REF123462              |
        | SaleOrderId    | 100007                 |
        | SaleReferenceId| 123456795              |
        | CardPan        | 6104****6789           |
    When I refund 350000 Rials with reason "Customer dissatisfaction"
    Then the response status code should be 200
    And the refund should have reason "Customer dissatisfaction"

Scenario: Handle Behpardakht refund gateway error
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 450000          |
        | Currency       | IRR             |
        | RefId          | REF123463       |
        | SaleOrderId    | 100008          |
        | SaleReferenceId| 123456796       |
        | CardPan        | 6104****0123    |
    And Behpardakht gateway returns error for refund
    When I refund 450000 Rials with reason "Gateway error test"
    Then the response status code should be 500
    And the response should contain "Refund failed"

Scenario: Refund unsettled payment should fail
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 250000          |
        | Currency       | IRR             |
        | RefId          | REF123464       |
        | SaleOrderId    | 100009          |
        | Status         | Verified        |
    When I refund 250000 Rials with reason "Unsettled refund"
    Then the response status code should be 400
    And the response should contain error "Payment must be settled before refund"

Scenario: Track refund transaction details
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 520000          |
        | Currency       | IRR             |
        | RefId          | REF123465       |
        | SaleOrderId    | 100010          |
        | SaleReferenceId| 123456797       |
        | CardPan        | 6104****4567    |
    When I refund 520000 Rials with reason "Full service cancellation"
    Then the response status code should be 200
    And a refund transaction should be recorded with:
        | Field          | Value                      |
        | Amount         | 520000                     |
        | Reason         | Full service cancellation  |
        | Status         | Completed                  |
        | RefundMethod   | Behpardakht                |
