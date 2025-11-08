Feature: ZarinPal Payment Reconciliation
    As a finance admin
    I want to reconcile ZarinPal payments
    So that I can ensure payment records match ZarinPal gateway records

Background:
    Given I am logged in as a finance admin
    And multiple providers have ZarinPal payments

Scenario: Generate daily reconciliation report
    Given there are payments on 2024-01-15
    When I send a GET request to "/api/v1/payments/reconciliation" with parameters:
        | Parameter | Value      |
        | StartDate | 2024-01-15 |
        | EndDate   | 2024-01-15 |
    Then the response status code should be 200
    And the report should contain all payments from that date

Scenario: View reconciliation summary for date range
    Given there are these payments:
        | Date       | Amount  | Status | Method   |
        | 2024-01-15 | 500000  | Paid   | ZarinPal |
        | 2024-01-16 | 300000  | Paid   | ZarinPal |
        | 2024-01-17 | 200000  | Failed | ZarinPal |
        | 2024-01-18 | 400000  | Paid   | ZarinPal |
    When I send a GET request to "/api/v1/payments/reconciliation" with parameters:
        | Parameter | Value      |
        | StartDate | 2024-01-15 |
        | EndDate   | 2024-01-18 |
    Then the response should contain:
        | Field            | Value   |
        | TotalPayments    | 4       |
        | SuccessfulCount  | 3       |
        | FailedCount      | 1       |
        | TotalAmount      | 1400000 |
        | SuccessfulAmount | 1200000 |

Scenario: Filter reconciliation by payment method
    Given there are payments with different methods
    When I send a GET request to "/api/v1/payments/reconciliation" with parameters:
        | Parameter     | Value      |
        | PaymentMethod | ZarinPal   |
        | StartDate     | 2024-01-01 |
        | EndDate       | 2024-01-31 |
    Then the report should only include ZarinPal payments

Scenario: View detailed transaction list in reconciliation
    Given there are 10 payments on 2024-01-15
    When I request detailed reconciliation report
    Then the response should include all transaction details:
        | Field       | Present |
        | PaymentId   | yes     |
        | Authority   | yes     |
        | RefNumber   | yes     |
        | Amount      | yes     |
        | Fee         | yes     |
        | Status      | yes     |
        | CardPan     | yes     |
        | CreatedAt   | yes     |

Scenario: Identify discrepancies in payment amounts
    Given a payment has different amounts in database and gateway
    When I run reconciliation check
    Then the discrepancy should be flagged
    And the report should show:
        | Field          | Present |
        | ExpectedAmount | yes     |
        | ActualAmount   | yes     |
        | Difference     | yes     |

Scenario: Group reconciliation by provider
    Given multiple providers have payments
    When I send a GET request to "/api/v1/payments/reconciliation" with parameters:
        | Parameter | Value      |
        | GroupBy   | Provider   |
        | StartDate | 2024-01-01 |
        | EndDate   | 2024-01-31 |
    Then the report should be grouped by provider
    And each group should show provider statistics

Scenario: Calculate total fees collected by ZarinPal
    Given there are these completed payments with fees:
        | Amount  | Fee  |
        | 500000  | 5000 |
        | 300000  | 3000 |
        | 200000  | 2000 |
    When I request reconciliation report
    Then the response should contain:
        | Field     | Value |
        | TotalFees | 10000 |

Scenario: Track refunds in reconciliation
    Given there are these payments:
        | Amount  | Status            | RefundedAmount |
        | 500000  | Paid              | 0              |
        | 400000  | Refunded          | 400000         |
        | 300000  | PartiallyRefunded | 150000         |
    When I request reconciliation report
    Then the response should contain:
        | Field            | Value  |
        | TotalRefunds     | 550000 |
        | RefundedPayments | 2      |

Scenario: Identify missing verification for payment requests
    Given a payment request was created but never verified
    When I run reconciliation check
    Then the unverified payment should be flagged
    And it should appear in "Pending Verification" section

Scenario: Export reconciliation report as Excel
    Given there is reconciliation data
    When I send a GET request to "/api/v1/payments/reconciliation/export" with parameters:
        | Parameter | Value      |
        | Format    | Excel      |
        | StartDate | 2024-01-01 |
        | EndDate   | 2024-01-31 |
    Then the response should be an Excel file
    And the file should contain multiple sheets:
        | Sheet            |
        | Summary          |
        | Transactions     |
        | Refunds          |
        | Discrepancies    |

Scenario: Daily automated reconciliation check
    Given it is end of day
    When the automated reconciliation job runs
    Then a reconciliation report should be generated
    And any discrepancies should be reported

Scenario: Compare with ZarinPal settlement report
    Given I have ZarinPal's settlement report
    When I upload the settlement report for comparison
    Then the system should match payments
    And highlight any mismatches

Scenario: Track payment status changes
    Given a payment changed status during the day
    When I view reconciliation report
    Then the status history should be shown

Scenario: Calculate net settlement amount
    Given there are these payments and refunds:
        | Type    | Amount  |
        | Payment | 500000  |
        | Payment | 300000  |
        | Refund  | 150000  |
        | Payment | 200000  |
    When I request reconciliation report
    Then the response should contain:
        | Field            | Value   |
        | TotalPayments    | 1000000 |
        | TotalRefunds     | 150000  |
        | NetSettlement    | 850000  |

Scenario: View reconciliation by hour for fraud detection
    Given there are payments throughout the day
    When I send a GET request to "/api/v1/payments/reconciliation" with parameters:
        | Parameter | Value      |
        | Date      | 2024-01-15 |
        | GroupBy   | Hour       |
    Then the response should show hourly breakdown
    And unusual patterns should be highlighted

Scenario: Reconcile failed payment attempts
    Given there are these payment attempts:
        | Amount  | Status  | Attempts |
        | 500000  | Paid    | 1        |
        | 300000  | Failed  | 3        |
        | 200000  | Paid    | 2        |
    When I request reconciliation report
    Then it should show:
        | Field          | Value |
        | TotalAttempts  | 6     |
        | FailureRate    | 50    |

Scenario: Monthly reconciliation summary
    Given there are payments throughout January 2024
    When I send a GET request to "/api/v1/payments/reconciliation/monthly" with parameters:
        | Parameter | Value   |
        | Year      | 2024    |
        | Month     | 1       |
    Then the response should contain monthly summary
    And daily breakdowns should be included

Scenario: Unauthorized access to reconciliation
    Given I am logged in as a customer
    When I attempt to access reconciliation report
    Then the response status code should be 403
    And the response should contain error "Unauthorized"

Scenario: Unauthorized access for regular provider
    Given I am logged in as a provider
    When I attempt to access system-wide reconciliation
    Then the response status code should be 403
    And I should only see my own payment data

Scenario: Finance admin has full reconciliation access
    Given I am logged in as a finance admin
    When I request reconciliation report
    Then I should see all providers' payments
    And I should see all payment methods

Scenario: Schedule automatic reconciliation reports
    Given I configure automatic reports
    When I set schedule to daily at 23:00
    Then reconciliation reports should be generated daily
    And emailed to finance team

Scenario: Audit trail for reconciliation access
    Given I access a reconciliation report
    When the report is generated
    Then an audit log entry should be created
    And it should record:
        | Field      | Present |
        | UserId     | yes     |
        | Timestamp  | yes     |
        | DateRange  | yes     |
        | ReportType | yes     |

Scenario: Handle timezone differences in reconciliation
    Given payments were made in different timezones
    When I generate reconciliation report
    Then all timestamps should be normalized to UTC
    And displayed in selected timezone

Scenario: Real-time reconciliation dashboard
    Given I am viewing reconciliation dashboard
    When new payments are processed
    Then the dashboard should update in real-time
    And show current day statistics
