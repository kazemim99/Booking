Feature: Service Availability
  As a customer
  I want to view available time slots for services
  So that I can book appointments at convenient times

  Background:
    Given a provider "Test Salon" exists with active status
    And the provider has a service "Haircut" priced at 50.00 USD
    And the provider has business hours configured for 9 AM to 5 PM
    And the provider has at least one staff member

  @smoke @availability
  Scenario: View available slots for a future date
    When I send a GET request to check availability for 3 days from now
    Then the response status code should be 200
    And the response should contain available time slots
    And all slots should be within business hours

  @availability @booked-excluded
  Scenario: Booked slots are excluded from availability
    Given there is a confirmed booking at 10:00 AM in 3 days
    When I send a GET request to check availability for that day
    Then the response status code should be 200
    And the 10:00 AM slot should not be available
    And other slots should remain available

  @availability @multiple-staff
  Scenario: Multiple staff increases availability
    Given the provider has 3 staff members
    And there is a booking at 10:00 AM with staff member 1
    When I check availability for that time
    Then the 10:00 AM slot should still be available with other staff

  @availability @negative @past-date
  Scenario: Cannot check availability for past dates
    When I send a GET request to check availability for yesterday
    Then the response status code should be 400
    And the error should indicate past dates not allowed

  @availability @negative @non-existent-provider
  Scenario: Cannot check availability for non-existent provider
    When I send a GET request to check availability for non-existent provider
    Then the response status code should be 404

  @availability @negative @non-existent-service
  Scenario: Cannot check availability for non-existent service
    When I send a GET request to check availability with invalid service ID
    Then the response status code should be 404

  @availability @closed-day
  Scenario: No availability on closed days
    Given the provider is closed on Sundays
    When I check availability for next Sunday
    Then the response status code should be 200
    And the response should contain no available slots

  @availability @break-times
  Scenario: Break times are excluded from availability
    Given the provider has a break from 12:00 PM to 1:00 PM
    When I check availability for today
    Then slots between 12:00 PM and 1:00 PM should not be available

  @availability @holiday
  Scenario: No availability on holidays
    Given the provider has marked tomorrow as a holiday
    When I check availability for tomorrow
    Then the response status code should be 200
    And the response should contain no available slots

  @availability @exception
  Scenario: Exception hours override regular business hours
    Given the provider has an exception for tomorrow: 2 PM to 6 PM only
    When I check availability for tomorrow
    Then only slots between 2 PM and 6 PM should be available

  @availability @duration
  Scenario: Availability respects service duration
    Given the service duration is 90 minutes
    When I check availability at 4:00 PM (business closes at 5:00 PM)
    Then the 4:00 PM slot should not be available
    And only slots ending before 5:00 PM should be available

  @availability @lead-time
  Scenario: Booking lead time is respected
    Given the provider requires 24 hours lead time
    When I check availability for tomorrow at this time
    Then slots within the next 24 hours should not be available

  @availability @buffer-time
  Scenario: Buffer time between bookings
    Given the provider requires 15 minutes buffer between bookings
    And there is a booking at 10:00 AM (30 min duration)
    When I check availability
    Then the 10:30 AM slot should not be available
    And the 10:45 AM slot should be available
