Feature: Financial Reporting and Earnings
  As a provider and admin
  I want to view financial reports and earnings
  So that I can track business performance

  Background:
    Given a provider "Successful Salon" exists with active status
    And the provider has business hours configured
    And the provider has multiple services
    And the provider has completed bookings with payments over the last 30 days

  @smoke @financial @earnings
  Scenario: Provider views earnings for a date range
    Given I am authenticated as the provider
    When I send a GET request to provider earnings with:
      | Field     | Value      |
      | StartDate | 30 days ago|
      | EndDate   | today      |
    Then the response status code should be 200
    And the response should contain earnings summary:
      | Field           | Value    |
      | GrossEarnings   | 500.00   |
      | CommissionAmount| 75.00    |
      | NetEarnings     | 425.00   |
      | TotalPayments   | 5        |

  @financial @earnings @breakdown
  Scenario: Earnings report shows daily breakdown
    Given I am authenticated as the provider
    When I send a GET request to provider earnings for last 7 days
    Then the response status code should be 200
    And the response should include earnings by date
    And each date should show number of payments and amounts

  @financial @earnings @filter
  Scenario Outline: Filter earnings by date range
    Given I am authenticated as the provider
    When I send a GET request to earnings for "<Period>"
    Then the response status code should be 200
    And the response should only include payments from "<Period>"

    Examples:
      | Period        |
      | last 7 days   |
      | last 30 days  |
      | last 90 days  |

  @financial @negative @unauthorized
  Scenario: Provider cannot view other provider's earnings
    Given another provider "Competitor Salon" exists
    And I am authenticated as the first provider
    When I send a GET request to view the competitor's earnings
    Then the response status code should be 403

  @financial @admin @allproviders
  Scenario: Admin views earnings across all providers
    Given I am authenticated as an admin
    And there are 5 providers with earnings
    When I send a GET request to "/api/v1/financial/summary"
    Then the response status code should be 200
    And the response should show platform-wide financial summary

  @financial @currency
  Scenario: Earnings calculated in correct currency
    Given the provider operates in USD
    And all payments are in USD
    When I view provider earnings
    Then all amounts should be in USD
    And currency conversions should not be applied

  @financial @refunds-impact
  Scenario: Refunds correctly reduce earnings
    Given I am authenticated as the provider
    And the provider has 5 payments totaling 500.00 USD
    And 1 payment of 100.00 USD was refunded
    When I view provider earnings
    Then the gross earnings should reflect the refund
    And net earnings should be calculated after refunds
