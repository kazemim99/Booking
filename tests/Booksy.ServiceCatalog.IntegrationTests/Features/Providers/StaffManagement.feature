Feature: Staff Management
  As a provider
  I want to manage my staff members
  So that customers can book with specific staff

  Background:
    Given a provider "Beauty Salon" exists with active status
    And I am authenticated as the provider

  @smoke @staff @add
  Scenario: Add a staff member
    When I send a POST request to add staff with:
      | Field       | Value                |
      | FirstName   | John                 |
      | LastName    | Doe                  |
      | Email       | john@test.com        |
      | Phone       | +1234567890          |
      | Role        | ServiceProvider      |
    Then the response status code should be 201
    And the staff member should be added to the provider

  @staff @add @multiple-roles
  Scenario Outline: Add staff with different roles
    When I add a staff member with role "<Role>"
    Then the response status code should be 201
    And the staff member should have role "<Role>"

    Examples:
      | Role            |
      | ServiceProvider |
      | Assistant       |
      | Receptionist    |
      | Manager         |

  @staff @update
  Scenario: Update staff member information
    Given the provider has a staff member "Jane Smith"
    When I send a PUT request to update staff with:
      | Field     | Value              |
      | Email     | newemail@test.com  |
      | Phone     | +9876543210        |
      | Role      | Manager            |
    Then the response status code should be 200
    And the staff information should be updated

  @staff @delete
  Scenario: Remove a staff member
    Given the provider has a staff member "John Doe"
    When I send a DELETE request to remove the staff member
    Then the response status code should be 200
    And the staff member should be removed from the provider

  @staff @negative @delete-with-bookings
  Scenario: Cannot remove staff with upcoming bookings
    Given the provider has a staff member with upcoming bookings
    When I try to remove that staff member
    Then the response status code should be 400
    And the error should indicate active bookings exist

  @staff @view @list
  Scenario: View all staff members
    Given the provider has 5 staff members
    When I send a GET request to "/api/v1/providers/{providerId}/staff"
    Then the response status code should be 200
    And the response should contain 5 staff members

  @staff @services @assign
  Scenario: Assign services to staff member
    Given the provider has services: "Haircut", "Coloring", "Styling"
    And the provider has staff member "John"
    When I assign services to John:
      | Service  |
      | Haircut  |
      | Coloring |
    Then the response status code should be 200
    And John should be able to provide those services

  @staff @schedule
  Scenario: Set staff member schedule
    Given the provider has staff member "Jane"
    When I set Jane's schedule:
      | Day     | StartTime | EndTime |
      | Monday  | 09:00     | 17:00   |
      | Tuesday | 09:00     | 17:00   |
      | Friday  | 10:00     | 15:00   |
    Then the response status code should be 200
    And Jane should only be available during those hours

  @staff @negative @unauthorized
  Scenario: Cannot add staff to another provider
    Given another provider exists
    When I try to add staff to the other provider
    Then the response status code should be 403

  @staff @negative @duplicate-email
  Scenario: Cannot add staff with duplicate email
    Given the provider has staff with email "john@test.com"
    When I try to add another staff with email "john@test.com"
    Then the response status code should be 400

  @staff @commission
  Scenario: Set staff commission rate
    Given the provider has staff member "John"
    When I set John's commission rate to 60%
    Then the response status code should be 200
    And John's commission rate should be saved

  @staff @availability
  Scenario: View staff availability for bookings
    Given the provider has multiple staff members
    And some staff have bookings
    When I check staff availability for tomorrow at 2 PM
    Then the response should show which staff are available

  @staff @performance
  Scenario: View staff performance metrics
    Given the provider has staff with completed bookings
    When I request staff performance report for last 30 days
    Then the response should include:
      | Metric             |
      | TotalBookings      |
      | CompletedBookings  |
      | Revenue Generated  |
      | Customer Ratings   |

  @staff @deactivate
  Scenario: Deactivate staff member (soft delete)
    Given the provider has staff member "John"
    When I deactivate John's account
    Then the response status code should be 200
    And John should not appear in active staff list
    And historical bookings should still reference John
