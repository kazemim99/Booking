Feature: Concurrency and Race Conditions
  As a system
  I want to handle concurrent operations correctly
  So that data integrity is maintained

  Background:
    Given a provider "Test Salon" exists with active status
    And the provider has a service "Haircut" priced at 50.00 USD
    And the provider has business hours configured

  @concurrency @booking @double-booking
  Scenario: Prevent double-booking of the same time slot
    Given there is one available slot at 10:00 AM tomorrow
    When two customers simultaneously try to book the same slot
    Then only one booking should succeed with status 201
    And the other should fail with status 409 (Conflict)

  @concurrency @payment @duplicate
  Scenario: Prevent duplicate payment processing
    Given I have a confirmed booking
    When I submit the same payment twice simultaneously
    Then only one payment should be processed
    And the duplicate should be rejected with idempotency check

  @concurrency @service @update
  Scenario: Handle concurrent service updates
    Given I am authenticated as the provider
    And I have a service "Haircut"
    When two requests update the service simultaneously
    Then both updates should be handled correctly
    And the final state should be consistent

  @concurrency @availability @race
  Scenario: Availability check race condition
    Given there is one slot available at 2 PM
    When 3 users check availability simultaneously
    And all 3 try to book the slot
    Then only the first should succeed
    And others should receive proper error messages

  @concurrency @payout @once
  Scenario: Ensure payout is executed only once
    Given a pending payout exists
    When two admins try to execute the payout simultaneously
    Then only one execution should succeed
    And the payout should be marked as completed only once

  @concurrency @refund @duplicate
  Scenario: Prevent duplicate refunds
    Given I have a paid payment
    When two refund requests are submitted simultaneously
    Then only one refund should be processed
    And the duplicate should be rejected

  @concurrency @staff @booking-race
  Scenario: Multiple bookings for same staff at same time
    Given a staff member has one available slot
    When 5 customers try to book with that staff simultaneously
    Then only one booking should succeed
    And others should fail or be assigned to different staff

  @concurrency @notification @duplicate-send
  Scenario: Prevent duplicate notification sends
    When the same notification is triggered twice in quick succession
    Then the notification should be sent only once
    And duplicate detection should prevent the second send

  @concurrency @optimistic-locking
  Scenario: Optimistic locking prevents stale updates
    Given a booking exists with version 1
    When user A reads the booking (version 1)
    And user B updates the booking (version becomes 2)
    And user A tries to update with stale version 1
    Then user A's update should fail with conflict error

  @concurrency @inventory @service-capacity
  Scenario: Handle service capacity limits under load
    Given a service has capacity for 10 concurrent bookings
    When 20 users try to book simultaneously
    Then only 10 bookings should be accepted
    And 10 should be rejected with capacity exceeded
