Feature: Provider Registration
  As a new service provider
  I want to register on the platform
  So that I can offer my services to customers

  @smoke @provider @register
  Scenario: Register provider with basic information
    Given I am authenticated as a customer
    When I send a POST request to register a provider with:
      | Field           | Value                  |
      | BusinessName    | Beautiful Hair Salon   |
      | Description     | Professional salon     |
      | Type            | Salon                  |
      | PrimaryPhone    | +989121234567          |
      | Email           | salon@test.com         |
      | Street          | 123 Main St            |
      | City            | Tehran                 |
      | State           | Tehran                 |
      | PostalCode      | 12345                  |
      | Country         | Iran                   |
      | Latitude        | 35.6892                |
      | Longitude       | 51.3890                |
    Then the response status code should be 201
    And the response should contain a provider with:
      | Field        | Value                  |
      | BusinessName | Beautiful Hair Salon   |
      | Status       | PendingVerification    |
    And the provider should exist in the database

  @provider @register @full
  Scenario: Register provider with complete information
    Given I am authenticated as a customer
    When I send a POST request to "/api/v1/providers/register-full" with complete details:
      | Field              | Value                  |
      | BusinessName       | Complete Salon         |
      | CategoryId         | beauty-salon           |
      | OwnerFirstName     | John                   |
      | OwnerLastName      | Doe                    |
      | PhoneNumber        | +989121234567          |
      | Services           | Haircut, Coloring      |
      | TeamMembers        | Staff1, Staff2         |
    Then the response status code should be 201
    And the provider should have services configured
    And the provider should have staff members configured

  @provider @register @negative @duplicate
  Scenario: Cannot register provider with duplicate business name
    Given a provider "Existing Salon" exists with active status
    And I am authenticated as a customer
    When I send a POST request to register a provider with business name "Existing Salon"
    Then the response status code should be 400
    And the error message should indicate duplicate business name

  @provider @register @negative @unauthorized
  Scenario: Cannot register provider without authentication
    Given I am not authenticated
    When I send a POST request to register a provider
    Then the response status code should be 401

  @provider @register @negative @invalid-phone
  Scenario: Cannot register with invalid phone number
    Given I am authenticated as a customer
    When I send a POST request to register a provider with invalid phone "12345"
    Then the response status code should be 400
    And the error should indicate invalid phone number format

  @provider @register @progressive
  Scenario: Progressive registration - Create draft provider
    Given I am authenticated as a customer
    When I send a POST request to "/api/v1/providers/draft" with basic info
    Then the response status code should be 201
    And a draft provider should be created with status "Drafted"
    And the registration step should be 3

  @provider @register @progressive @complete
  Scenario: Complete progressive registration
    Given I am authenticated as a customer
    And I have a draft provider
    And I have added services and hours to the draft
    When I send a POST request to complete the registration
    Then the response status code should be 200
    And the provider status should change to "PendingVerification"
