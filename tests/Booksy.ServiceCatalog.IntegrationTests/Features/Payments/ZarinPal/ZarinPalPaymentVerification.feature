Feature: ZarinPal Payment Verification
    As a customer
    I want to verify my payment after completing it on ZarinPal
    So that my booking can be confirmed and payment recorded

Background:
    Given a registered provider exists with:
        | Field        | Value               |
        | BusinessName | Test Salon          |
        | BusinessType | BeautySalon         |
        | Email        | salon@example.com   |
    And a booking exists for the provider with:
        | Field    | Value     |
        | Amount   | 500000    |
        | Currency | IRR       |
        | Status   | Confirmed |
    And a ZarinPal payment request has been created with:
        | Field       | Value           |
        | Amount      | 500000          |
        | Description | Booking payment |
        | Mobile      | 09123456789     |

Scenario: Successfully verify payment after customer completes it on ZarinPal
    Given the customer completed the payment on ZarinPal gateway
    When ZarinPal redirects to callback with:
        | Parameter | Value |
        | Authority | {LastAuthority} |
        | Status    | OK    |
    Then the callback response should redirect to success page
    And the payment should have status "Paid" in the database
    And the payment should have "RefNumber" stored
    And the payment should have "CardPan" stored
    And a "PaymentVerifiedEvent" domain event should be published

Scenario: Handle user cancellation on ZarinPal payment page
    Given the customer navigated to ZarinPal payment page
    When the customer clicks cancel button
    And ZarinPal redirects to callback with:
        | Parameter | Value |
        | Authority | {LastAuthority} |
        | Status    | NOK   |
    Then the callback response should redirect to failure page
    And the payment should have status "Failed" in the database
    And the payment failure reason should be "User cancelled the payment"

Scenario: Verify payment with card information captured
    Given the customer completed the payment on ZarinPal gateway
    And ZarinPal captured card details:
        | Field   | Value        |
        | CardPan | 6274****1234 |
        | Fee     | 5000         |
    When ZarinPal redirects to callback with:
        | Parameter | Value |
        | Authority | {LastAuthority} |
        | Status    | OK    |
    Then the payment should have status "Paid" in the database
    And the payment should have these details:
        | Field   | Value        |
        | CardPan | 6274****1234 |
        | Fee     | 5000         |

Scenario: Calculate net amount after fee deduction
    Given the customer completed the payment on ZarinPal gateway
    And ZarinPal charged a fee of 5000 Rials
    When the payment is verified successfully
    Then the payment net amount should be 495000

Scenario: Handle payment verification failure from ZarinPal
    Given a payment request exists with authority
    When I verify the payment with ZarinPal API
    And ZarinPal returns verification error "-53"
    Then the payment should have status "Failed" in the database
    And the payment failure reason should contain "verification unsuccessful"

Scenario: Handle already verified payment (Code 101)
    Given a payment was already verified successfully
    When ZarinPal redirects to callback again with:
        | Parameter | Value |
        | Authority | {LastAuthority} |
        | Status    | OK    |
    Then the verification should succeed
    And the payment should remain "Paid"
    And no duplicate transaction should be created

Scenario: Fail verification when payment not found
    When ZarinPal redirects to callback with:
        | Parameter | Value                              |
        | Authority | A_NONEXISTENT_AUTHORITY_12345      |
        | Status    | OK                                 |
    Then the callback response should return error "Payment not found"
    And the response status code should be 404

Scenario: Verify payment and update booking status
    Given the customer completed the payment on ZarinPal gateway
    When the payment is verified successfully
    Then the payment should have status "Paid" in the database
    And the associated booking should be updated accordingly

Scenario: Handle verification with missing card PAN
    Given the customer completed the payment on ZarinPal gateway
    When ZarinPal verifies payment without CardPan
    Then the payment should have status "Paid" in the database
    And the payment CardPan should be null

Scenario: Verify payment creates transaction record
    Given the customer completed the payment on ZarinPal gateway
    When the payment is verified successfully
    Then a transaction record should exist with:
        | Field           | Value        |
        | Type            | Verification |
        | Status          | Succeeded    |
        | RefNumber       | not-null     |

Scenario: Frontend redirect to success page with payment details
    Given the customer completed the payment on ZarinPal gateway
    When the payment is verified successfully
    Then the callback should redirect to frontend success URL
    And the redirect URL should contain query parameters:
        | Parameter  | Value               |
        | paymentId  | {LastPaymentId}     |
        | refNumber  | {PaymentRefNumber}  |
        | status     | success             |

Scenario: Frontend redirect to failure page with error details
    Given the customer navigated to ZarinPal payment page
    When the customer clicks cancel button
    Then the callback should redirect to frontend failure URL
    And the redirect URL should contain query parameters:
        | Parameter | Value             |
        | paymentId | {LastPaymentId}   |
        | status    | failed            |
        | reason    | User cancelled    |

Scenario: Concurrent verification requests should be handled safely
    Given a payment request exists with authority
    When multiple verification requests are sent simultaneously
    Then only one verification should succeed
    And the payment should have status "Paid" in the database
    And no duplicate transactions should be created
