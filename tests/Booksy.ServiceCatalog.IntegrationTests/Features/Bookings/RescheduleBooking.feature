Feature: Reschedule Booking
  As a customer
  I want to move my booking to a different time
  So that I can keep my appointment when my plans change

  # A booking's StaffId is a *bookable resource* id, and creation accepts three kinds:
  # an organization membership (the current staff model), the organization itself
  # (solo/direct booking), or a legacy individual sub-provider. Rescheduling must work
  # for every kind creation accepts — see openspec/changes/fix-reschedule-membership-staff.
  Background:
    Given a provider "Test Salon" exists with active status
    And the provider has a service "Haircut" priced at 50.00 USD
    # Open every day: these scenarios move a booking to "3 days from now", which lands
    # on a different weekday depending on when the suite runs. Opening times are not
    # what is under test here.
    And the provider is open every day
    And the provider has at least one staff member
    And I am authenticated as a customer

  @smoke @booking @reschedule @legacy-sub-provider
  Scenario: Reschedule a booking held against a legacy individual sub-provider
    Given I have a booking for "Haircut" with the staff member scheduled for tomorrow at 10:00
    When I send a POST request to reschedule the booking with:
      | Field        | Value                    |
      | NewStartTime | 3 days from now at 14:00 |
      | Reason       | Schedule conflict        |
    Then the response status code should be 200
    And the old booking should have status "Rescheduled" in the database
    And a new booking should exist for the new time slot
    And the new booking should be held for the same resource

  @booking @reschedule @membership
  Scenario: Reschedule a booking held against an organization member
    Given I have a booking for "Haircut" with the team member scheduled for tomorrow at 10:00
    When I send a POST request to reschedule the booking with:
      | Field        | Value                    |
      | NewStartTime | 3 days from now at 14:00 |
      | Reason       | Schedule conflict        |
    Then the response status code should be 200
    And the old booking should have status "Rescheduled" in the database
    And a new booking should exist for the new time slot
    And the new booking should be held for the same resource
    And the original booking still has its own policy and payment information

  @booking @reschedule @organization-direct
  Scenario: Reschedule a booking made directly against the organization
    Given I have a booking for "Haircut" scheduled for tomorrow at 10:00
    When I send a POST request to reschedule the booking with:
      | Field        | Value                    |
      | NewStartTime | 3 days from now at 14:00 |
      | Reason       | Schedule conflict        |
    Then the response status code should be 200
    And the old booking should have status "Rescheduled" in the database
    And a new booking should exist for the new time slot
    And the new booking should be held for the same resource

  # Rejection must be all-or-nothing: the handler releases the old slot before occupying the
  # new one, so a failure in between must not strand the booking with neither.
  @booking @reschedule @negative @atomicity
  Scenario: A rejected reschedule leaves the original booking untouched
    Given I have a booking for "Haircut" with the team member scheduled for tomorrow at 10:00
    And another booking already occupies 3 days from now at 14:00 for the same resource
    When I send a POST request to reschedule the booking with:
      | Field        | Value                    |
      | NewStartTime | 3 days from now at 14:00 |
      | Reason       | Schedule conflict        |
    Then the response status code should be 409
    And the booking should have status "Requested" in the database
    And the booking should still be scheduled for its original time

  @booking @reschedule @negative
  Scenario: Rescheduling a booking whose resource no longer resolves fails cleanly
    Given I have a booking for "Haircut" with an unresolvable resource scheduled for tomorrow at 10:00
    When I send a POST request to reschedule the booking with:
      | Field        | Value                    |
      | NewStartTime | 3 days from now at 14:00 |
      | Reason       | Schedule conflict        |
    Then the response status code should be 404
    And the booking should have status "Requested" in the database
