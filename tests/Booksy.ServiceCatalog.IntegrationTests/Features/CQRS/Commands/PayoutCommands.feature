Feature: Payout Command Handlers - Complete Coverage
  As a system
  I want to test all payout command handlers
  So that every code path and validation is covered

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has completed bookings with payments

  # ==================== CreatePayoutCommandHandler ====================

  @command @payout @create @happy-path
  Scenario: CreatePayoutCommand - Success path
    Given I am authenticated as an admin
    And the provider has 5 paid payments totaling 500.00 USD
    When I execute CreatePayoutCommand with:
      | Field              | Value             |
      | ProviderId         | [Provider:Id]     |
      | PeriodStart        | 30 days ago       |
      | PeriodEnd          | today             |
      | CommissionPercentage | 15              |
    Then the command should succeed
    And a Payout aggregate should be created
    And gross amount should be 500.00 USD
    And commission should be 75.00 USD (15%)
    And net amount should be 425.00 USD
    And payout should include 5 payment IDs
    And PayoutCreatedEvent should be published

  @command @payout @create @validation @no-payments
  Scenario: CreatePayoutCommand - No completed payments in period
    Given I am authenticated as an admin
    And there are no completed payments in the period
    When I execute CreatePayoutCommand for last 30 days
    Then the command should fail with InvalidOperationException
    And the error message should contain "No completed payments found"

  @command @payout @create @validation @currency-mismatch
  Scenario: CreatePayoutCommand - Payments with different currencies
    Given I am authenticated as an admin
    And there are payments in both USD and EUR
    When I execute CreatePayoutCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "different currency"
    And the error message should contain "All payments must have the same currency"

  @command @payout @create @business-logic @commission-calculation
  Scenario: CreatePayoutCommand - Commission rate calculation
    Given I am authenticated as an admin
    And there are completed payments totaling 1000.00 USD
    When I execute CreatePayoutCommand with commission 20%
    Then the command should succeed
    And gross amount should be 1000.00 USD
    And commission should be 200.00 USD
    And net amount should be 800.00 USD

  @command @payout @create @business-logic @default-commission
  Scenario: CreatePayoutCommand - Use default commission rate
    Given I am authenticated as an admin
    And there are completed payments totaling 500.00 USD
    When I execute CreatePayoutCommand without specifying commission
    Then the command should succeed
    And commission should be 15% (default)
    And net amount should be 425.00 USD

  @command @payout @create @business-logic @scheduled-payout
  Scenario: CreatePayoutCommand - Schedule payout for future
    Given I am authenticated as an admin
    When I execute CreatePayoutCommand with:
      | Field       | Value             |
      | ScheduledAt | 3 days from now   |
    Then the command should succeed
    And the payout should be scheduled for 3 days from now
    And the payout status should be "Scheduled"

  @command @payout @create @business-logic @with-notes
  Scenario: CreatePayoutCommand - Include notes
    Given I am authenticated as an admin
    When I execute CreatePayoutCommand with:
      | Field | Value                          |
      | Notes | End of month payout - January  |
    Then the command should succeed
    And the payout notes should be saved

  @command @payout @create @validation @payment-ids
  Scenario: CreatePayoutCommand - Collect all payment IDs
    Given I am authenticated as an admin
    And there are 10 completed payments
    When I execute CreatePayoutCommand
    Then the command should succeed
    And the payout should reference all 10 payment IDs

  @command @payout @create @authorization
  Scenario: CreatePayoutCommand - Only admin can create payouts
    Given I am authenticated as a provider
    When I execute CreatePayoutCommand
    Then the command should fail with ForbiddenException

  # ==================== ExecutePayoutCommandHandler ====================

  @command @payout @execute @happy-path
  Scenario: ExecutePayoutCommand - Execute pending payout
    Given I am authenticated as an admin
    And there is a pending payout with net amount 500.00 USD
    When I execute ExecutePayoutCommand with:
      | Field               | Value                    |
      | PayoutId            | [Payout:Id]              |
      | ConnectedAccountId  | acct_provider123         |
    Then the command should succeed
    And the payout gateway should be invoked
    And the payout should be marked as "Processing"
    And the external payout ID should be saved
    And PayoutExecutedEvent should be published

  @command @payout @execute @validation @payout-not-found
  Scenario: ExecutePayoutCommand - Payout not found
    Given I am authenticated as an admin
    When I execute ExecutePayoutCommand with non-existent payout ID
    Then the command should fail with InvalidOperationException
    And the error message should contain "not found"

  @command @payout @execute @validation @invalid-status
  Scenario: ExecutePayoutCommand - Cannot execute non-pending payout
    Given I am authenticated as an admin
    And there is a completed payout
    When I execute ExecutePayoutCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "cannot be executed"
    And the error message should contain "Current status"

  @command @payout @execute @validation @zero-amount
  Scenario: ExecutePayoutCommand - Cannot execute payout with zero or negative amount
    Given I am authenticated as an admin
    And there is a pending payout with net amount 0.00 USD
    When I execute ExecutePayoutCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "invalid net amount"

  @command @payout @execute @gateway @success
  Scenario: ExecutePayoutCommand - Payment gateway succeeds
    Given I am authenticated as an admin
    And there is a pending payout
    When I execute ExecutePayoutCommand
    And the payment gateway returns success
    Then the payout should be marked as "Processing"
    And the external payout ID should be saved

  @command @payout @execute @gateway @instant-payout
  Scenario: ExecutePayoutCommand - Instant payout (gateway returns paid status)
    Given I am authenticated as an admin
    And there is a pending payout
    When I execute ExecutePayoutCommand
    And the payment gateway returns status "paid"
    Then the payout should be marked as "Paid"
    And PayoutCompletedEvent should be published

  @command @payout @execute @gateway @failure
  Scenario: ExecutePayoutCommand - Payment gateway fails
    Given I am authenticated as an admin
    And there is a pending payout
    When I execute ExecutePayoutCommand
    And the payment gateway returns failure with error "Insufficient funds"
    Then the payout should be marked as "Failed"
    And the failure reason should be saved
    And PayoutFailedEvent should be published

  @command @payout @execute @business-logic @metadata
  Scenario: ExecutePayoutCommand - Include metadata in gateway request
    Given I am authenticated as an admin
    And there is a pending payout for period Jan 1 to Jan 31
    When I execute ExecutePayoutCommand
    Then the gateway request should include metadata:
      | Field       | Value                |
      | PayoutId    | [Payout:Id]          |
      | ProviderId  | [Provider:Id]        |
      | PeriodStart | 2025-01-01           |
      | PeriodEnd   | 2025-01-31           |

  @command @payout @execute @business-logic @description
  Scenario: ExecutePayoutCommand - Gateway request includes period description
    Given I am authenticated as an admin
    And there is a pending payout for period Jan 1 to Jan 31
    When I execute ExecutePayoutCommand
    Then the gateway request description should contain "Payout for period 2025-01-01 to 2025-01-31"

  @command @payout @execute @authorization
  Scenario: ExecutePayoutCommand - Only admin can execute payouts
    Given I am authenticated as a provider
    And there is a pending payout
    When I execute ExecutePayoutCommand
    Then the command should fail with ForbiddenException

  @command @payout @execute @business-logic @connected-account
  Scenario: ExecutePayoutCommand - Requires provider's connected account ID
    Given I am authenticated as an admin
    And there is a pending payout
    And the provider has a Stripe connected account
    When I execute ExecutePayoutCommand with the connected account ID
    Then the command should succeed
    And the payout should be sent to the correct account

  @command @payout @execute @business-logic @arrival-date
  Scenario: ExecutePayoutCommand - Gateway returns estimated arrival date
    Given I am authenticated as an admin
    And there is a pending payout
    When I execute ExecutePayoutCommand
    And the gateway returns arrival date "2025-02-05"
    Then the command should succeed
    And the arrival date should be 2025-02-05

  @command @payout @execute @idempotency
  Scenario: ExecutePayoutCommand - Cannot execute same payout twice
    Given I am authenticated as an admin
    And there is a completed payout
    When I execute ExecutePayoutCommand again
    Then the command should fail with InvalidOperationException
    And no duplicate payout should be created
