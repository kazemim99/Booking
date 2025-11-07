Feature: Provider Payouts
  As a provider and admin
  I want to manage payouts to providers
  So that providers receive their earnings properly

  Background:
    Given I am authenticated as an admin
    And a provider "Beauty Salon" exists with active status
    And the provider has completed bookings with paid payments

  @smoke @payout @create
  Scenario: Admin creates a payout for provider
    When I send a POST request to create a payout with:
      | Field                 | Value                    |
      | ProviderId            | [Provider:Beauty Salon:Id] |
      | PeriodStart           | 31 days ago              |
      | PeriodEnd             | yesterday                |
      | CommissionPercentage  | 15                       |
      | Notes                 | Monthly payout           |
    Then the response status code should be 201
    And the response should contain a payout with:
      | Field    | Value   |
      | Status   | Pending |
    And the payout should exist in the database

  @payout @execute
  Scenario: Admin executes a pending payout
    Given a pending payout exists for the provider
    When I send a POST request to execute the payout with:
      | Field                | Value                |
      | ExternalTransactionId| bank_txn_12345       |
      | Notes                | Transferred to bank  |
    Then the response status code should be 200
    And the payout should have status "Completed" in the database

  @payout @view @provider
  Scenario: Provider views their payout history
    Given I am authenticated as the provider
    And the provider has 3 completed payouts
    When I send a GET request to "/api/v1/payouts/provider/{providerId}"
    Then the response status code should be 200
    And the response should contain 3 payouts
    And each payout should show gross, commission, and net amounts

  @payout @pending @admin
  Scenario: Admin views all pending payouts
    Given there are 5 providers with pending payouts
    When I send a GET request to "/api/v1/payouts/pending"
    Then the response status code should be 200
    And the response should contain all pending payouts

  @payout @calculate
  Scenario: System correctly calculates payout amounts
    Given a provider has 5 payments totaling 500.00 USD
    When I create a payout with 15% commission
    Then the gross amount should be 500.00 USD
    And the commission amount should be 75.00 USD
    And the net amount should be 425.00 USD

  @payout @negative @unauthorized
  Scenario: Non-admin cannot create payouts
    Given I am authenticated as a customer
    When I send a POST request to create a payout
    Then the response status code should be 403

  @payout @negative @provider-cannot-execute
  Scenario: Provider cannot execute their own payout
    Given I am authenticated as the provider
    And a pending payout exists for the provider
    When I send a POST request to execute the payout
    Then the response status code should be 403
