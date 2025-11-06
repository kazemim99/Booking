Feature: Authorization and Security
  As a system
  I want to enforce proper authorization
  So that users can only access their own data and perform allowed operations

  @security @authorization @customer
  Scenario: Customer can only view their own bookings
    Given I am authenticated as customer "alice@test.com"
    And another customer "bob@test.com" has bookings
    When I try to view Bob's bookings
    Then the response status code should be 403

  @security @authorization @provider
  Scenario: Provider can only manage their own services
    Given I am authenticated as provider "Salon A"
    And another provider "Salon B" exists
    When I try to add a service to Salon B
    Then the response status code should be 403

  @security @authorization @admin-only
  Scenario Outline: Only admins can access admin endpoints
    Given I am authenticated as a <Role>
    When I send a request to an admin-only endpoint
    Then the response status code should be <StatusCode>

    Examples:
      | Role     | StatusCode |
      | customer | 403        |
      | provider | 403        |
      | admin    | 200        |

  @security @data-isolation
  Scenario: Providers cannot see each other's financial data
    Given I am authenticated as provider "Salon A"
    And provider "Salon B" has financial data
    When I try to view Salon B's earnings
    Then the response status code should be 403

  @security @sql-injection
  Scenario: System is protected against SQL injection
    When I send a request with SQL injection in the search term
    Then the request should be safely handled
    And no database error should occur

  @security @xss
  Scenario: System sanitizes user input to prevent XSS
    When I create a service with malicious script in the description
    Then the script should be sanitized
    And the stored data should be safe

  @security @rate-limiting
  Scenario: Rate limiting prevents abuse
    When I send 100 requests in 1 minute
    Then after the limit, requests should be throttled
    And I should receive 429 status code

  @security @token-expiry
  Scenario: Expired authentication tokens are rejected
    Given I have an expired authentication token
    When I send a request with the expired token
    Then the response status code should be 401
    And the error should indicate token expired

  @security @password-reset
  Scenario: Password reset requires verification
    When I request a password reset for "user@test.com"
    Then a verification code should be sent
    And the password cannot be changed without the code

  @security @sensitive-data
  Scenario: Sensitive data is not exposed in responses
    When I view my user profile
    Then the response should not contain password hash
    And payment method details should be masked
