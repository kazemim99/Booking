Feature: Behpardakht Settlement and Reversal
    As a system
    I want to handle Behpardakht payment settlement and reversal
    So that payments are properly finalized or reversed when needed

Background:
    Given a registered provider exists with:
        | Field         | Value                |
        | BusinessName  | Test Beauty Salon    |
        | BusinessType  | BeautySalon          |
        | Email         | provider@example.com |

Scenario: Successfully settle verified payment
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 500000          |
        | Currency       | IRR             |
        | RefId          | REF456789       |
        | SaleOrderId    | 200001          |
        | SaleReferenceId| 987654321       |
        | Status         | Verified        |
    When the system settles the payment
    Then the response status code should be 200
    And the payment should have status "Settled" in the database

Scenario: Handle duplicate settlement request
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 400000          |
        | Currency       | IRR             |
        | RefId          | REF456790       |
        | SaleOrderId    | 200002          |
        | SaleReferenceId| 987654322       |
        | Status         | Verified        |
    And the payment has already been settled
    When the system attempts to settle the payment again
    Then the settlement should succeed with code 45
    And the response should indicate "Transaction already settled"

Scenario: Fail to settle unverified payment
    Given a Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 350000          |
        | Currency       | IRR             |
        | RefId          | REF456791       |
        | Status         | Pending         |
    When the system attempts to settle the payment
    Then the response status code should be 400
    And the response should contain error "Payment must be verified before settlement"

Scenario: Successfully reverse verified payment
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 300000          |
        | Currency       | IRR             |
        | RefId          | REF456792       |
        | SaleOrderId    | 200003          |
        | SaleReferenceId| 987654323       |
        | Status         | Verified        |
    And the payment cannot be settled due to service cancellation
    When the system reverses the payment
    Then the response status code should be 200
    And the payment should have status "Reversed" in the database

Scenario: Handle duplicate reversal request
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 450000          |
        | Currency       | IRR             |
        | RefId          | REF456793       |
        | SaleOrderId    | 200004          |
        | SaleReferenceId| 987654324       |
        | Status         | Verified        |
    And the payment has already been reversed
    When the system attempts to reverse the payment again
    Then the reversal should succeed with code 48
    And the response should indicate "Transaction already reversed"

Scenario: Fail to reverse settled payment
    Given a completed Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 550000          |
        | Currency       | IRR             |
        | RefId          | REF456794       |
        | SaleOrderId    | 200005          |
        | SaleReferenceId| 987654325       |
        | Status         | Settled         |
    When the system attempts to reverse the payment
    Then the response status code should be 400
    And the response should contain error "Cannot reverse settled payment"

Scenario: Auto-reverse payment after verification timeout
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 280000          |
        | Currency       | IRR             |
        | RefId          | REF456795       |
        | SaleOrderId    | 200006          |
        | SaleReferenceId| 987654326       |
        | Status         | Verified        |
    And 25 minutes have passed since verification
    And settlement was not requested
    When the auto-reversal job runs
    Then the payment should be automatically reversed
    And the payment should have status "AutoReversed" in the database

Scenario: Inquiry payment status before settlement
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 420000          |
        | Currency       | IRR             |
        | RefId          | REF456796       |
        | SaleOrderId    | 200007          |
        | SaleReferenceId| 987654327       |
        | Status         | Verified        |
    When the system inquires the payment status
    Then the inquiry should return "Verified"
    And the inquiry response should confirm transaction details

Scenario: Inquiry failed payment status
    Given a Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 320000          |
        | Currency       | IRR             |
        | RefId          | REF456797       |
        | Status         | Failed          |
    When the system inquires the payment status
    Then the inquiry should return "Failed"
    And the inquiry should provide failure reason

Scenario: Settlement with automatic booking confirmation
    Given a verified Behpardakht payment exists for a booking with:
        | Field          | Value           |
        | Amount         | 600000          |
        | Currency       | IRR             |
        | RefId          | REF456798       |
        | SaleOrderId    | 200008          |
        | SaleReferenceId| 987654328       |
        | BookingStatus  | PendingPayment  |
    When the system settles the payment
    Then the payment should have status "Settled" in the database
    And the booking should have status "Confirmed"
    And a "BookingConfirmedEvent" should be published

Scenario: Reversal with automatic booking cancellation
    Given a verified Behpardakht payment exists for a booking with:
        | Field          | Value           |
        | Amount         | 380000          |
        | Currency       | IRR             |
        | RefId          | REF456799       |
        | SaleOrderId    | 200009          |
        | SaleReferenceId| 987654329       |
        | BookingStatus  | PendingPayment  |
    When the system reverses the payment
    Then the payment should have status "Reversed" in the database
    And the booking should have status "Cancelled"
    And a "BookingCancelledEvent" should be published

Scenario: Handle settlement network timeout
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 470000          |
        | Currency       | IRR             |
        | RefId          | REF456800       |
        | SaleOrderId    | 200010          |
        | SaleReferenceId| 987654330       |
        | Status         | Verified        |
    And Behpardakht gateway has network timeout
    When the system attempts to settle the payment
    Then the system should retry settlement
    And the payment should remain in "PendingSettlement" status

Scenario: Reversal time window validation
    Given a verified Behpardakht payment exists with:
        | Field          | Value           |
        | Amount         | 290000          |
        | Currency       | IRR             |
        | RefId          | REF456801       |
        | SaleOrderId    | 200011          |
        | SaleReferenceId| 987654331       |
        | Status         | Verified        |
    And 4 hours have passed since verification
    When the system attempts to reverse the payment
    Then the response status code should be 400
    And the response should contain "Reversal time window expired"
