Feature: ZarinPal Customer Payment History
    As a customer
    I want to view my payment history
    So that I can track all my transactions and receipts

Background:
    Given a registered provider exists with:
        | Field        | Value     |
        | BusinessName | Salon Pro |
        | BusinessType | BeautySalon |
    And I am logged in as a customer

Scenario: View empty payment history for new customer
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the response status code should be 200
    And the response should contain:
        | Field       | Value |
        | TotalCount  | 0     |
    And the payments list should be empty

Scenario: View payment history with successful payments
    Given I have completed these ZarinPal payments:
        | Amount  | Description      | Status | Date       |
        | 500000  | Haircut service  | Paid   | 2024-01-15 |
        | 300000  | Makeup service   | Paid   | 2024-01-20 |
        | 150000  | Nail service     | Paid   | 2024-01-25 |
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the response status code should be 200
    And the response should contain:
        | Field      | Value |
        | TotalCount | 3     |
    And the payments should be ordered by date descending

Scenario: Filter payment history by date range
    Given I have payments from January to March 2024
    When I send a GET request to "/api/v1/payments/customer/history" with parameters:
        | Parameter | Value      |
        | StartDate | 2024-02-01 |
        | EndDate   | 2024-02-29 |
    Then the response should only contain payments from February 2024

Scenario: Paginate through payment history
    Given I have 50 completed ZarinPal payments
    When I send a GET request to "/api/v1/payments/customer/history" with parameters:
        | Parameter | Value |
        | Page      | 1     |
        | PageSize  | 20    |
    Then the response should contain 20 payments
    And the response should indicate more pages available

Scenario: View second page of payment history
    Given I have 50 completed ZarinPal payments
    When I send a GET request to "/api/v1/payments/customer/history" with parameters:
        | Parameter | Value |
        | Page      | 2     |
        | PageSize  | 20    |
    Then the response should contain 20 payments
    And the payments should be different from page 1

Scenario: View payment history including all statuses
    Given I have these ZarinPal payments:
        | Amount  | Status            | Date       |
        | 500000  | Paid              | 2024-01-15 |
        | 300000  | Failed            | 2024-01-16 |
        | 200000  | Pending           | 2024-01-17 |
        | 400000  | Refunded          | 2024-01-18 |
        | 250000  | PartiallyRefunded | 2024-01-19 |
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the response should contain all 5 payments
    And each payment should show its correct status

Scenario: View payment details in history
    Given I have a completed ZarinPal payment with full details
    When I send a GET request to "/api/v1/payments/customer/history"
    Then each payment in the list should contain:
        | Field       |
        | PaymentId   |
        | Amount      |
        | Status      |
        | Method      |
        | Authority   |
        | RefNumber   |
        | CardPan     |
        | CreatedAt   |

Scenario: View refunded amount in payment history
    Given I have a payment that was partially refunded
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the payment should show:
        | Field          | Value            |
        | Amount         | 500000           |
        | RefundedAmount | 200000           |
        | Status         | PartiallyRefunded|

Scenario: View payment history with booking information
    Given I have payments linked to bookings
    When I send a GET request to "/api/v1/payments/customer/history"
    Then each payment should include:
        | Field         |
        | BookingId     |
        | ProviderName  |
        | ServiceName   |

Scenario: Customer cannot see other customers' payments
    Given another customer has payment history
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the response should only contain my payments
    And should not contain other customers' payments

Scenario: Filter by payment method
    Given I have payments with different methods:
        | Amount  | Method     |
        | 500000  | ZarinPal   |
        | 300000  | Stripe     |
        | 200000  | ZarinPal   |
    When I send a GET request to "/api/v1/payments/customer/history" with parameters:
        | Parameter     | Value    |
        | PaymentMethod | ZarinPal |
    Then the response should contain 2 payments
    And all payments should have method "ZarinPal"

Scenario: Default page size when not specified
    Given I have 30 completed payments
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the response should contain 20 payments
    And the default page size should be applied

Scenario: View payment with card information masked
    Given I have a completed ZarinPal payment with card details
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the CardPan should be masked like "6274****1234"

Scenario: View payment fees in history
    Given I have completed payments with fees
    When I send a GET request to "/api/v1/payments/customer/history"
    Then each payment should show:
        | Field | Present |
        | Fee   | yes     |

Scenario: Calculate total spent in date range
    Given I have these completed payments:
        | Amount  | Date       |
        | 500000  | 2024-01-15 |
        | 300000  | 2024-01-20 |
        | 200000  | 2024-01-25 |
    When I send a GET request to "/api/v1/payments/customer/history" with parameters:
        | Parameter | Value      |
        | StartDate | 2024-01-01 |
        | EndDate   | 2024-01-31 |
    Then the response should include summary:
        | Field      | Value   |
        | TotalSpent | 1000000 |

Scenario: Unauthorized access to payment history
    Given I am not logged in
    When I send a GET request to "/api/v1/payments/customer/history"
    Then the response status code should be 401

Scenario: Sort payment history by amount
    Given I have multiple payments
    When I send a GET request to "/api/v1/payments/customer/history" with parameters:
        | Parameter | Value  |
        | SortBy    | Amount |
        | SortOrder | Desc   |
    Then the payments should be sorted by amount descending
