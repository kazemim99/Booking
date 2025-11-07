Feature: Service Authorization and Security
  As a system administrator
  I want to ensure proper authorization controls on service operations
  So that users can only perform actions they are permitted to do

  Background:
    Given the following providers exist:
      | Provider     | Email                  | Status |
      | Salon Alpha  | alpha@test.com         | Active |
      | Salon Beta   | beta@test.com          | Active |
      | Salon Gamma  | gamma@test.com         | Active |
    And provider "Salon Alpha" has the following services:
      | Service        | Status   | Price |
      | Haircut        | Active   | 50.00 |
      | Color          | Inactive | 80.00 |
      | Archived Trim  | Archived | 30.00 |

  # ==================== UNAUTHENTICATED ACCESS ====================

  @authorization @unauthenticated @create
  Scenario: Unauthenticated user cannot create service
    Given I am not authenticated
    When I attempt to create a service for provider "Salon Alpha"
    Then the response status code should be 401
    And the error message should contain "authentication"

  @authorization @unauthenticated @update
  Scenario: Unauthenticated user cannot update service
    Given I am not authenticated
    When I attempt to update service "Haircut" from "Salon Alpha"
    Then the response status code should be 401

  @authorization @unauthenticated @delete
  Scenario: Unauthenticated user cannot delete service
    Given I am not authenticated
    When I attempt to delete service "Haircut" from "Salon Alpha"
    Then the response status code should be 401

  @authorization @unauthenticated @activate
  Scenario: Unauthenticated user cannot activate service
    Given I am not authenticated
    When I attempt to activate service "Color" from "Salon Alpha"
    Then the response status code should be 401

  @authorization @unauthenticated @view-public
  Scenario: Unauthenticated user can view active services
    Given I am not authenticated
    When I request service details for "Haircut" from "Salon Alpha"
    Then the response status code should be 200
    And the service details should be returned

  @authorization @unauthenticated @view-inactive
  Scenario: Unauthenticated user cannot view inactive services
    Given I am not authenticated
    When I request service details for "Color" from "Salon Alpha"
    Then the response status code should be 404

  @authorization @unauthenticated @search
  Scenario: Unauthenticated user can search active services
    Given I am not authenticated
    When I search for services
    Then the response status code should be 200
    And only active services should be returned

  # ==================== CUSTOMER ROLE ====================

  @authorization @customer @create
  Scenario: Customer cannot create services
    Given I am authenticated as a customer
    When I attempt to create a service for provider "Salon Alpha"
    Then the response status code should be 403
    And the error message should contain "forbidden"

  @authorization @customer @update
  Scenario: Customer cannot update services
    Given I am authenticated as a customer
    When I attempt to update service "Haircut" from "Salon Alpha"
    Then the response status code should be 403

  @authorization @customer @delete
  Scenario: Customer cannot delete services
    Given I am authenticated as a customer
    When I attempt to delete service "Haircut" from "Salon Alpha"
    Then the response status code should be 403

  @authorization @customer @view-active
  Scenario: Customer can view active services
    Given I am authenticated as a customer
    When I request service details for "Haircut" from "Salon Alpha"
    Then the response status code should be 200

  @authorization @customer @view-inactive
  Scenario: Customer cannot view inactive services
    Given I am authenticated as a customer
    When I request service details for "Color" from "Salon Alpha"
    Then the response status code should be 404

  @authorization @customer @view-archived
  Scenario: Customer cannot view archived services
    Given I am authenticated as a customer
    When I request service details for "Archived Trim" from "Salon Alpha"
    Then the response status code should be 404

  @authorization @customer @search
  Scenario: Customer can only search active services
    Given I am authenticated as a customer
    When I search for all services from "Salon Alpha"
    Then the response status code should be 200
    And only active services should be returned

  # ==================== PROVIDER ROLE - OWN SERVICES ====================

  @authorization @provider @create-own
  Scenario: Provider can create services for own account
    Given I am authenticated as the owner of "Salon Alpha"
    When I create a new service for "Salon Alpha":
      | Field       | Value        |
      | ServiceName | New Service  |
      | BasePrice   | 60.00        |
      | Duration    | 45           |
    Then the response status code should be 201
    And the service should be created

  @authorization @provider @update-own
  Scenario: Provider can update own services
    Given I am authenticated as the owner of "Salon Alpha"
    When I update service "Haircut" with:
      | Field       | Value            |
      | ServiceName | Premium Haircut  |
      | BasePrice   | 75.00            |
    Then the response status code should be 200
    And the service should be updated

  @authorization @provider @delete-own
  Scenario: Provider can delete own services
    Given I am authenticated as the owner of "Salon Alpha"
    When I delete service "Color" from "Salon Alpha"
    Then the response status code should be 200
    And the service should be deleted

  @authorization @provider @activate-own
  Scenario: Provider can activate own inactive services
    Given I am authenticated as the owner of "Salon Alpha"
    When I activate service "Color" from "Salon Alpha"
    Then the response status code should be 200
    And the service status should be "Active"

  @authorization @provider @deactivate-own
  Scenario: Provider can deactivate own active services
    Given I am authenticated as the owner of "Salon Alpha"
    When I deactivate service "Haircut" from "Salon Alpha"
    Then the response status code should be 200
    And the service status should be "Inactive"

  @authorization @provider @archive-own
  Scenario: Provider can archive own services
    Given I am authenticated as the owner of "Salon Alpha"
    When I archive service "Color" from "Salon Alpha"
    Then the response status code should be 204
    And the service status should be "Archived"

  @authorization @provider @view-own-all-statuses
  Scenario: Provider can view own services in any status
    Given I am authenticated as the owner of "Salon Alpha"
    When I request my services including inactive
    Then the response status code should be 200
    And services in all statuses should be returned

  # ==================== PROVIDER ROLE - CROSS-PROVIDER ====================

  @authorization @provider @create-other
  Scenario: Provider cannot create services for another provider
    Given I am authenticated as the owner of "Salon Alpha"
    When I attempt to create a service for provider "Salon Beta"
    Then the response status code should be 403
    And the error message should contain "forbidden"

  @authorization @provider @update-other
  Scenario: Provider cannot update another provider's services
    Given I am authenticated as the owner of "Salon Alpha"
    And provider "Salon Beta" has a service "Manicure"
    When I attempt to update "Manicure" from "Salon Beta"
    Then the response status code should be 403

  @authorization @provider @delete-other
  Scenario: Provider cannot delete another provider's services
    Given I am authenticated as the owner of "Salon Alpha"
    And provider "Salon Beta" has a service "Manicure"
    When I attempt to delete "Manicure" from "Salon Beta"
    Then the response status code should be 403

  @authorization @provider @activate-other
  Scenario: Provider cannot activate another provider's services
    Given I am authenticated as the owner of "Salon Alpha"
    And provider "Salon Beta" has an inactive service "Spa Treatment"
    When I attempt to activate "Spa Treatment" from "Salon Beta"
    Then the response status code should be 403

  @authorization @provider @view-other-active
  Scenario: Provider can view other provider's active services
    Given I am authenticated as the owner of "Salon Alpha"
    And provider "Salon Beta" has an active service "Manicure"
    When I request service details for "Manicure" from "Salon Beta"
    Then the response status code should be 200

  @authorization @provider @view-other-inactive
  Scenario: Provider cannot view other provider's inactive services
    Given I am authenticated as the owner of "Salon Alpha"
    And provider "Salon Beta" has an inactive service "Draft Service"
    When I request service details for "Draft Service" from "Salon Beta"
    Then the response status code should be 404

  # ==================== ADMIN ROLE ====================

  @authorization @admin @create-any
  Scenario: Admin can create services for any provider
    Given I am authenticated as an admin
    When I create a service for provider "Salon Beta":
      | Field       | Value           |
      | ServiceName | Admin Service   |
      | BasePrice   | 100.00          |
      | Duration    | 60              |
    Then the response status code should be 201

  @authorization @admin @update-any
  Scenario: Admin can update any provider's services
    Given I am authenticated as an admin
    When I update service "Haircut" from "Salon Alpha" with:
      | Field     | Value |
      | BasePrice | 55.00 |
    Then the response status code should be 200

  @authorization @admin @delete-any
  Scenario: Admin can delete any provider's services
    Given I am authenticated as an admin
    And provider "Salon Beta" has a service "Test Service"
    When I delete "Test Service" from "Salon Beta"
    Then the response status code should be 200

  @authorization @admin @view-any-status
  Scenario: Admin can view services in any status
    Given I am authenticated as an admin
    When I request service details for "Archived Trim" from "Salon Alpha"
    Then the response status code should be 200

  @authorization @admin @search-all-statuses
  Scenario: Admin can search services by status
    Given I am authenticated as an admin
    When I search for services with status "Archived"
    Then the response status code should be 200
    And archived services should be returned

  @authorization @admin @bulk-operations
  Scenario: Admin can perform bulk status changes
    Given I am authenticated as an admin
    When I bulk deactivate multiple services
    Then the response status code should be 200
    And all specified services should be deactivated

  # ==================== STAFF ROLE ====================

  @authorization @staff @provider-staff
  Scenario: Provider staff can view assigned services
    Given I am authenticated as staff of "Salon Alpha"
    And I am assigned to service "Haircut"
    When I request service details for "Haircut"
    Then the response status code should be 200

  @authorization @staff @cannot-modify
  Scenario: Provider staff cannot modify services
    Given I am authenticated as staff of "Salon Alpha"
    When I attempt to update service "Haircut"
    Then the response status code should be 403

  @authorization @staff @cannot-create
  Scenario: Provider staff cannot create services
    Given I am authenticated as staff of "Salon Alpha"
    When I attempt to create a service
    Then the response status code should be 403

  # ==================== SECURITY SCENARIOS ====================

  @authorization @security @token-tampering
  Scenario: Tampered authentication token is rejected
    Given I have a tampered authentication token
    When I attempt to create a service
    Then the response status code should be 401

  @authorization @security @expired-token
  Scenario: Expired authentication token is rejected
    Given I have an expired authentication token
    When I attempt to update a service
    Then the response status code should be 401

  @authorization @security @stolen-token
  Scenario: Provider cannot use another provider's token
    Given I am authenticated with provider "Salon Alpha" token
    When I attempt to create a service for provider "Salon Beta"
    Then the response status code should be 403

  @authorization @security @privilege-escalation
  Scenario: Customer cannot escalate to provider privileges
    Given I am authenticated as a customer
    And I modify my token to claim provider role
    When I attempt to create a service
    Then the response status code should be 403

  @authorization @security @resource-id-manipulation
  Scenario: Cannot access service by manipulating IDs
    Given I am authenticated as the owner of "Salon Alpha"
    And provider "Salon Beta" has a service with known ID
    When I attempt to update the service using direct ID
    Then the response status code should be 403

  # ==================== CONCURRENT ACCESS ====================

  @authorization @concurrent @multiple-providers
  Scenario: Multiple providers can modify own services simultaneously
    Given provider "Salon Alpha" is updating their service
    And provider "Salon Beta" is updating their service
    When both updates are executed concurrently
    Then both operations should succeed with status 200
    And each service should be updated correctly

  @authorization @concurrent @conflicting-updates
  Scenario: Prevent concurrent conflicting updates
    Given I am authenticated as the owner of "Salon Alpha"
    And another session is authenticated as the same provider
    When both sessions update the same service concurrently
    Then one should succeed with status 200
    And one should fail with status 409

  # ==================== RATE LIMITING ====================

  @authorization @rate-limit @service-creation
  Scenario: Rate limiting on service creation
    Given I am authenticated as the owner of "Salon Alpha"
    When I create 100 services in rapid succession
    Then some requests should return status 429
    And the error message should contain "rate limit"

  @authorization @rate-limit @search
  Scenario: Rate limiting on search queries
    Given I am not authenticated
    When I perform 1000 search requests in 1 minute
    Then some requests should return status 429

  # ==================== AUDIT LOGGING ====================

  @authorization @audit @service-creation
  Scenario: Service creation is audit logged
    Given I am authenticated as the owner of "Salon Alpha"
    When I create a new service
    Then an audit log entry should be created
    And the log should contain user ID and provider ID

  @authorization @audit @unauthorized-attempt
  Scenario: Unauthorized access attempts are logged
    Given I am authenticated as the owner of "Salon Alpha"
    When I attempt to update a service from "Salon Beta"
    Then the unauthorized attempt should be logged
    And security team should be notified

  # ==================== OWNERSHIP TRANSFER ====================

  @authorization @ownership @transfer
  Scenario: Service ownership cannot be transferred directly
    Given I am authenticated as the owner of "Salon Alpha"
    When I attempt to change the provider ID of "Haircut" to "Salon Beta"
    Then the response status code should be 403
    And the service ownership should remain with "Salon Alpha"

  # ==================== INACTIVE PROVIDER ====================

  @authorization @provider-status @inactive
  Scenario: Inactive provider cannot create services
    Given provider "Salon Gamma" has status "Inactive"
    And I am authenticated as the owner of "Salon Gamma"
    When I attempt to create a service
    Then the response status code should be 403
    And the error message should contain "inactive"

  @authorization @provider-status @suspended
  Scenario: Suspended provider cannot modify services
    Given provider "Salon Gamma" has status "Suspended"
    And I am authenticated as the owner of "Salon Gamma"
    When I attempt to update an existing service
    Then the response status code should be 403
    And the error message should contain "suspended"

  # ==================== AUTHORIZATION MATRIX ====================

  @authorization @matrix @complete-coverage
  Scenario Outline: Authorization matrix for service operations
    Given I am authenticated with role "<Role>"
    And target is "<Target>"
    When I perform operation "<Operation>"
    Then the response status code should be <StatusCode>

    Examples:
      | Role         | Target       | Operation | StatusCode |
      | None         | Any          | Create    | 401        |
      | None         | Active       | Read      | 200        |
      | None         | Inactive     | Read      | 404        |
      | Customer     | Any          | Create    | 403        |
      | Customer     | Active       | Read      | 200        |
      | Customer     | Inactive     | Read      | 404        |
      | Customer     | Any          | Update    | 403        |
      | Provider     | Own-Active   | Create    | 201        |
      | Provider     | Own-Active   | Read      | 200        |
      | Provider     | Own-Inactive | Read      | 200        |
      | Provider     | Own-Active   | Update    | 200        |
      | Provider     | Own-Active   | Delete    | 200        |
      | Provider     | Other-Active | Read      | 200        |
      | Provider     | Other-Active | Update    | 403        |
      | Provider     | Other-Active | Delete    | 403        |
      | Admin        | Any          | Create    | 201        |
      | Admin        | Any          | Read      | 200        |
      | Admin        | Any          | Update    | 200        |
      | Admin        | Any          | Delete    | 200        |
      | Staff        | Assigned     | Read      | 200        |
      | Staff        | Assigned     | Update    | 403        |
      | Staff        | Assigned     | Delete    | 403        |
