Feature: Working Hours Management
  As a provider
  I want to manage my business hours, breaks, and exceptions
  So that my availability is accurately reflected

  Background:
    Given a provider "Test Salon" exists with active status
    And I am authenticated as the provider

  @smoke @hours @view
  Scenario: View current business hours
    When I send a GET request to "/api/v1/providers/{providerId}/business-hours"
    Then the response status code should be 200
    And the response should contain hours for all 7 days of the week

  @hours @update
  Scenario: Update business hours for weekdays
    When I send a PUT request to update business hours with:
      | Day       | IsOpen | OpenTime | CloseTime |
      | Monday    | true   | 09:00    | 18:00     |
      | Tuesday   | true   | 09:00    | 18:00     |
      | Wednesday | true   | 09:00    | 18:00     |
      | Thursday  | true   | 09:00    | 18:00     |
      | Friday    | true   | 09:00    | 17:00     |
      | Saturday  | true   | 10:00    | 14:00     |
      | Sunday    | false  |          |           |
    Then the response status code should be 200
    And the business hours should be updated in the database

  @hours @breaks
  Scenario: Add break times to a day
    When I update business hours for Monday with breaks:
      | OpenTime | CloseTime | BreakStart | BreakEnd |
      | 09:00    | 18:00     | 12:00      | 13:00    |
    Then the response status code should be 200
    And the break should be configured for Monday

  @hours @multiple-breaks
  Scenario: Configure multiple breaks in a day
    When I set business hours with multiple breaks:
      | Day    | OpenTime | CloseTime | Breaks              |
      | Monday | 08:00    | 20:00     | 12:00-13:00, 16:00-16:30 |
    Then the response status code should be 200
    And both breaks should be saved

  @hours @holiday
  Scenario: Mark a specific date as holiday
    When I send a POST request to add a holiday:
      | Field       | Value              |
      | Date        | 2025-12-25         |
      | Description | Christmas Holiday  |
    Then the response status code should be 201
    And the holiday should be saved
    And no bookings should be allowed on that date

  @hours @holiday @recurring
  Scenario: Add recurring annual holiday
    When I add a recurring holiday:
      | Field       | Value              |
      | Date        | 12-25              |
      | Description | Christmas          |
      | Recurring   | Annual             |
    Then the response status code should be 201
    And the holiday should recur every year

  @hours @exception
  Scenario: Add exception hours for a specific date
    When I send a POST request to add an exception:
      | Field       | Value                |
      | Date        | 3 days from now      |
      | OpenTime    | 14:00                |
      | CloseTime   | 20:00                |
      | Reason      | Special event        |
    Then the response status code should be 201
    And the exception should override regular hours for that date

  @hours @exception @closed
  Scenario: Close business on a specific date
    When I add an exception to close on a specific date:
      | Field  | Value                     |
      | Date   | 3 days from now           |
      | Closed | true                      |
      | Reason | Staff training            |
    Then the response status code should be 201
    And no bookings should be allowed on that date

  @hours @negative @invalid-time
  Scenario: Cannot set invalid business hours
    When I send a PUT request with invalid hours:
      | Day    | OpenTime | CloseTime |
      | Monday | 18:00    | 09:00     |
    Then the response status code should be 400
    And the error should indicate close time must be after open time

  @hours @negative @overlapping-breaks
  Scenario: Cannot set overlapping breaks
    When I try to add overlapping breaks:
      | BreakStart | BreakEnd |
      | 12:00      | 13:00    |
      | 12:30      | 13:30    |
    Then the response status code should be 400

  @hours @negative @unauthorized
  Scenario: Non-owner cannot update business hours
    Given another provider exists
    And I am authenticated as the other provider
    When I try to update the first provider's business hours
    Then the response status code should be 403

  @hours @delete-holiday
  Scenario: Remove a holiday
    Given the provider has a holiday on 2025-12-25
    When I send a DELETE request to remove the holiday
    Then the response status code should be 200
    And the holiday should be deleted

  @hours @delete-exception
  Scenario: Remove an exception
    Given the provider has an exception for tomorrow
    When I send a DELETE request to remove the exception
    Then the response status code should be 200
    And regular hours should apply

  @hours @timezone
  Scenario: Business hours respect provider timezone
    Given the provider is in timezone "Asia/Tehran"
    When I view business hours
    Then all times should be in the provider's timezone

  @hours @staff-hours
  Scenario: Configure individual staff working hours
    Given the provider has staff member "John"
    When I set working hours for John:
      | Day     | OpenTime | CloseTime |
      | Monday  | 10:00    | 16:00     |
      | Tuesday | 10:00    | 16:00     |
    Then the response status code should be 200
    And John's availability should be limited to those hours
