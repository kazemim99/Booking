Feature: ZarinPal Provider Revenue Analytics
    As a provider
    I want to view my revenue statistics from ZarinPal payments
    So that I can track my earnings and business performance

Background:
    Given a registered provider "Beauty Salon Pro" exists
    And I am logged in as the provider

Scenario: View revenue with no payments
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response status code should be 200
    And the response should contain:
        | Field               | Value |
        | TotalRevenue        | 0     |
        | TotalRefunds        | 0     |
        | NetRevenue          | 0     |
        | SuccessfulPayments  | 0     |
        | TotalPayments       | 0     |
        | SuccessRate         | 0     |

Scenario: Calculate total revenue from successful payments
    Given the provider has these completed ZarinPal payments:
        | Amount  | Status | Date       |
        | 500000  | Paid   | 2024-01-15 |
        | 300000  | Paid   | 2024-01-16 |
        | 200000  | Paid   | 2024-01-17 |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response should contain:
        | Field          | Value   |
        | TotalRevenue   | 1000000 |
        | NetRevenue     | 1000000 |

Scenario: Calculate net revenue after refunds
    Given the provider has these payments:
        | Amount  | Status            | RefundedAmount |
        | 500000  | Paid              | 0              |
        | 400000  | PartiallyRefunded | 150000         |
        | 300000  | Refunded          | 300000         |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response should contain:
        | Field        | Value   |
        | TotalRevenue | 1200000 |
        | TotalRefunds | 450000  |
        | NetRevenue   | 750000  |

Scenario: Calculate success rate with mixed payment statuses
    Given the provider has these payments:
        | Amount  | Status  |
        | 500000  | Paid    |
        | 400000  | Paid    |
        | 300000  | Failed  |
        | 200000  | Paid    |
        | 100000  | Failed  |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response should contain:
        | Field              | Value |
        | SuccessfulPayments | 3     |
        | TotalPayments      | 5     |
        | SuccessRate        | 60    |

Scenario: Filter revenue by date range
    Given the provider has payments from January to March 2024
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue" with parameters:
        | Parameter | Value      |
        | StartDate | 2024-02-01 |
        | EndDate   | 2024-02-29 |
    Then the response should only include February 2024 payments

Scenario: View revenue for specific month
    Given the provider has monthly payments throughout 2024
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue" with parameters:
        | Parameter | Value      |
        | StartDate | 2024-06-01 |
        | EndDate   | 2024-06-30 |
    Then the response should show June 2024 revenue statistics

Scenario: Exclude pending payments from revenue calculations
    Given the provider has these payments:
        | Amount  | Status  |
        | 500000  | Paid    |
        | 300000  | Pending |
        | 200000  | Paid    |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response should contain:
        | Field        | Value  |
        | TotalRevenue | 700000 |
    And pending payments should not be included

Scenario: View revenue broken down by payment method
    Given the provider accepts multiple payment methods:
        | Amount  | Method   | Status |
        | 500000  | ZarinPal | Paid   |
        | 300000  | Stripe   | Paid   |
        | 200000  | ZarinPal | Paid   |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue" with parameters:
        | Parameter     | Value    |
        | PaymentMethod | ZarinPal |
    Then the response should contain:
        | Field        | Value  |
        | TotalRevenue | 700000 |

Scenario: View average transaction value
    Given the provider has these completed payments:
        | Amount  |
        | 500000  |
        | 300000  |
        | 200000  |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response should contain:
        | Field               | Value  |
        | AverageTransaction  | 333333 |

Scenario: View revenue including ZarinPal fees
    Given the provider has payments with ZarinPal fees:
        | Amount  | Fee  | Status |
        | 500000  | 5000 | Paid   |
        | 300000  | 3000 | Paid   |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response should contain:
        | Field      | Value  |
        | TotalFees  | 8000   |
        | NetRevenue | 792000 |

Scenario: View monthly revenue trend
    Given the provider has payments across multiple months
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue/trend" with parameters:
        | Parameter | Value      |
        | StartDate | 2024-01-01 |
        | EndDate   | 2024-06-30 |
        | GroupBy   | Month      |
    Then the response should contain monthly breakdown

Scenario: Provider cannot view other providers' revenue
    Given another provider exists with revenue
    When I attempt to access their revenue
    Then the response status code should be 403
    And the response should contain error "Unauthorized"

Scenario: Admin can view any provider's revenue
    Given I am logged in as an admin
    And a provider exists with revenue data
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response status code should be 200
    And I should see the provider's revenue statistics

Scenario: View revenue with comparison to previous period
    Given the provider has revenue in current and previous periods
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue" with parameters:
        | Parameter       | Value      |
        | StartDate       | 2024-02-01 |
        | EndDate         | 2024-02-29 |
        | ComparePrevious | true       |
    Then the response should include comparison metrics:
        | Field              | Present |
        | RevenueGrowth      | yes     |
        | PaymentGrowth      | yes     |
        | SuccessRateChange  | yes     |

Scenario: Export revenue data as CSV
    Given the provider has revenue data
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue/export" with parameters:
        | Parameter | Value |
        | Format    | CSV   |
    Then the response should be a CSV file
    And the CSV should contain all revenue metrics

Scenario: View top earning days
    Given the provider has daily payment data
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue/top-days" with parameters:
        | Parameter | Value |
        | Limit     | 10    |
    Then the response should contain top 10 earning days
    And each day should show total revenue

Scenario: Real-time revenue updates
    Given the provider is viewing revenue dashboard
    When a new payment is completed
    Then the revenue statistics should be updated
    And the TotalRevenue should increase accordingly

Scenario: Handle timezone in revenue calculations
    Given the provider is in Tehran timezone (IRST)
    And payments were made at different times
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue" with date range
    Then the dates should be calculated in provider's timezone

Scenario: View refund rate statistics
    Given the provider has these payments:
        | Amount  | Status            |
        | 500000  | Paid              |
        | 400000  | Refunded          |
        | 300000  | PartiallyRefunded |
        | 200000  | Paid              |
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue"
    Then the response should contain:
        | Field       | Value |
        | RefundRate  | 37.5  |

Scenario: View payment count by status
    Given the provider has payments with various statuses
    When I send a GET request to "/api/v1/payments/provider/{providerId}/revenue/breakdown"
    Then the response should contain counts for each status:
        | Status            | Present |
        | Paid              | yes     |
        | Failed            | yes     |
        | Pending           | yes     |
        | Refunded          | yes     |
        | PartiallyRefunded | yes     |
