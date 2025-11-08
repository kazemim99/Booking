Feature: Behpardakht Payment Verification
    As a system
    I want to verify Behpardakht payments after customer completes payment
    So that I can confirm the payment and update booking status

Background:
    Given a registered provider exists with:
        | Field         | Value                |
        | BusinessName  | Test Beauty Salon    |
        | BusinessType  | BeautySalon          |
        | Email         | provider@example.com |

Scenario: Successfully verify Behpardakht payment after customer payment
    Given a Behpardakht payment request has been created with:
        | Field       | Value             |
        | Amount      | 500000            |
        | Description | Booking payment   |
    And the customer completed the payment on Behpardakht gateway
    When Behpardakht redirects to callback with:
        | Parameter       | Value          |
        | RefId           | {LastRefId}    |
        | ResCode         | 0              |
        | SaleOrderId     | {LastOrderId}  |
        | SaleReferenceId | 123456789      |
    Then the callback response should redirect to success page
    And the payment should have status "Completed" in the database
    And the payment should have "RefNumber" stored
    And the payment should have "CardPan" stored
    And a PaymentRequest transaction should be recorded
    And a Verification transaction should be recorded

Scenario: Handle Behpardakht callback with failed payment
    Given a Behpardakht payment request has been created with:
        | Field       | Value             |
        | Amount      | 300000            |
        | Description | Failed payment    |
    And the customer navigated to Behpardakht payment page
    When the customer clicks cancel button
    And Behpardakht redirects to callback with:
        | Parameter   | Value         |
        | RefId       | {LastRefId}   |
        | ResCode     | 17            |
    Then the callback response should redirect to failure page
    And the payment should have status "Failed" in the database
    And the payment failure reason should be "Customer cancelled the transaction"
    And a PaymentRequest transaction should be recorded
    And a Failed transaction should be recorded

Scenario: Handle duplicate verification request
    Given a Behpardakht payment request has been created with:
        | Field       | Value                |
        | Amount      | 200000               |
        | Description | Duplicate verify test|
    And the customer completed the payment on Behpardakht gateway
    And the payment is verified successfully
    When I attempt to verify the payment again
    Then the verification should succeed with code 43
    And the response should indicate "Verification already done"
    And the payment should still have status "Completed" in the database

Scenario: Verify payment with card holder information
    Given a Behpardakht payment request has been created with:
        | Field       | Value             |
        | Amount      | 450000            |
        | Description | Card info test    |
    And Behpardakht captured card details:
        | Field   | Value        |
        | CardPan | 6104****1234 |
    And the customer completed the payment on Behpardakht gateway
    When the payment is verified successfully
    Then the payment should have these details:
        | Field   | Value        |
        | CardPan | 6104****1234 |

Scenario: Handle verification timeout
    Given a Behpardakht payment request has been created with:
        | Field       | Value              |
        | Amount      | 350000             |
        | Description | Timeout test       |
    And the customer navigated to Behpardakht payment page
    And 25 minutes have passed since payment request
    When Behpardakht redirects to callback with:
        | Parameter       | Value         |
        | RefId           | {LastRefId}   |
        | ResCode         | 0             |
        | SaleReferenceId | 123456789     |
    Then the verification should fail
    And the payment should have status "Failed" in the database
    And the payment failure reason should be "Verification timeout"

Scenario: Verify payment and trigger settlement
    Given a Behpardakht payment request has been created with:
        | Field       | Value              |
        | Amount      | 600000             |
        | Description | Settlement test    |
    And the customer completed the payment on Behpardakht gateway
    When the payment is verified successfully
    And the system automatically settles the payment
    Then the payment should have status "Settled" in the database

Scenario: Handle invalid RefId in callback
    Given a Behpardakht payment request has been created with:
        | Field       | Value           |
        | Amount      | 250000          |
        | Description | Invalid RefId   |
    When Behpardakht redirects to callback with:
        | Parameter   | Value        |
        | RefId       | INVALID123   |
        | ResCode     | 0            |
    Then the callback should return error
    And the response should contain "Payment not found"

Scenario: Verify payment with Iranian Rial amount validation
    Given a Behpardakht payment request has been created with:
        | Field       | Value           |
        | Amount      | 1500000         |
        | Currency    | IRR             |
        | Description | Rial validation |
    And the customer completed the payment on Behpardakht gateway
    When the payment is verified successfully
    Then the verified amount should match 1500000 Rials
    And the payment currency should be IRR

Scenario: Handle network error during verification
    Given a Behpardakht payment request has been created with:
        | Field       | Value          |
        | Amount      | 400000         |
        | Description | Network test   |
    And the customer completed the payment on Behpardakht gateway
    When Behpardakht gateway is unavailable
    And verification is attempted
    Then the system should retry verification
    And the payment should remain in "PendingVerification" status

Scenario: Verify payment and publish domain events
    Given a Behpardakht payment request has been created with:
        | Field       | Value               |
        | Amount      | 550000              |
        | Description | Domain event test   |
    And the customer completed the payment on Behpardakht gateway
    When the payment is verified successfully
    Then a "PaymentVerifiedEvent" domain event should be published
    And a "PaymentCompletedEvent" domain event should be published
