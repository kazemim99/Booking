Feature: Data Validation and Business Constraints
  As a system
  I want to validate all input data and enforce business rules
  So that data integrity is maintained

  @validation @booking @future
  Scenario: Cannot book in the past
    Given I am authenticated as a customer
    When I try to create a booking for yesterday
    Then the response status code should be 400
    And the error should indicate past dates not allowed

  @validation @booking @too-far
  Scenario: Cannot book too far in advance
    Given the provider allows booking up to 90 days in advance
    When I try to create a booking for 100 days from now
    Then the response status code should be 400
    And the error should indicate booking window exceeded

  @validation @payment @currency-mismatch
  Scenario: Payment currency must match service currency
    Given a service priced in USD
    When I try to pay in EUR
    Then the response status code should be 400
    And the error should indicate currency mismatch

  @validation @payment @amount-mismatch
  Scenario: Payment amount must match booking total
    Given a booking total is 100.00 USD
    When I try to pay 50.00 USD
    Then the response status code should be 400
    And the error should indicate incorrect amount

  @validation @refund @exceeds-paid
  Scenario: Cannot refund more than paid amount
    Given a payment of 100.00 USD exists
    When I try to refund 150.00 USD
    Then the response status code should be 400
    And the error should indicate refund exceeds paid amount

  @validation @refund @already-refunded
  Scenario: Cannot refund an already fully refunded payment
    Given a payment has been fully refunded
    When I try to refund it again
    Then the response status code should be 400
    And the error should indicate payment already refunded

  @validation @service @negative-price
  Scenario: Service price must be positive
    When I try to create a service with price -10.00
    Then the response status code should be 400
    And the error should indicate invalid price

  @validation @service @zero-duration
  Scenario: Service duration must be positive
    When I try to create a service with duration 0
    Then the response status code should be 400
    And the error should indicate invalid duration

  @validation @hours @invalid-range
  Scenario: Business hours close time must be after open time
    When I try to set hours with close time before open time
    Then the response status code should be 400

  @validation @phone @format
  Scenario: Phone number must be valid format
    When I try to register with phone number "abc123"
    Then the response status code should be 400
    And the error should indicate invalid phone format

  @validation @email @format
  Scenario: Email must be valid format
    When I try to register with email "notanemail"
    Then the response status code should be 400
    And the error should indicate invalid email format

  @validation @required-fields
  Scenario: Required fields must be provided
    When I try to create a booking without required fields
    Then the response status code should be 400
    And the error should list all missing required fields

  @validation @string-length
  Scenario: String fields respect maximum length
    When I try to create a service with 1000-character description
    Then it should succeed if under limit
    Or fail if exceeding maximum length

  @constraint @unique-business-name
  Scenario: Business name must be unique per city
    Given a provider "Best Salon" exists in "Tehran"
    When another provider tries to register as "Best Salon" in "Tehran"
    Then the response status code should be 400
    And the error should indicate duplicate business name

  @constraint @service-dependency
  Scenario: Cannot delete service with active bookings
    Given a service has 3 upcoming bookings
    When I try to delete the service
    Then the response status code should be 400
    And the error should indicate service has dependencies

  @constraint @provider-active
  Scenario: Cannot book with inactive provider
    Given a provider is inactive
    When I try to create a booking
    Then the response status code should be 400
    And the error should indicate provider not available

  @constraint @staff-available
  Scenario: Staff must be available during booking time
    Given a staff member is not scheduled to work on Monday
    When I try to book with that staff on Monday
    Then the response status code should be 400

  @constraint @capacity
  Scenario: Respect provider capacity limits
    Given a provider has capacity for 5 concurrent bookings per hour
    When the 6th booking is attempted for the same hour
    Then the response status code should be 409
    And the error should indicate capacity exceeded

  @constraint @minimum-notice
  Scenario: Respect minimum cancellation notice period
    Given a booking requires 24 hours notice for cancellation
    When I try to cancel 2 hours before the appointment
    Then the response status code should be 400
    And a cancellation fee may apply
