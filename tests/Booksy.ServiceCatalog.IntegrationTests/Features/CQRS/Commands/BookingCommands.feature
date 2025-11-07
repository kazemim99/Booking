Feature: Booking Command Handlers - Complete Coverage
  As a system
  I want to test all booking command handlers
  So that every code path and validation is covered

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has a service "Haircut" priced at 50.00 USD
    And the provider has business hours configured
    And the provider has at least one staff member

  # ==================== CreateBookingCommandHandler ====================

  @command @booking @create @happy-path
  Scenario: CreateBookingCommand - Success path
    Given I am authenticated as a customer
    When I execute CreateBookingCommand with valid data
    Then the command should succeed
    And a Booking aggregate should be created
    And BookingRequestedEvent should be published
    And the booking should be saved to database

  @command @booking @create @validation @provider-not-found
  Scenario: CreateBookingCommand - Provider not found
    Given I am authenticated as a customer
    When I execute CreateBookingCommand with non-existent provider ID
    Then the command should fail with NotFoundException
    And the error message should contain "Provider with ID"

  @command @booking @create @validation @service-not-found
  Scenario: CreateBookingCommand - Service not found
    Given I am authenticated as a customer
    When I execute CreateBookingCommand with non-existent service ID
    Then the command should fail with NotFoundException
    And the error message should contain "Service with ID"

  @command @booking @create @validation @service-provider-mismatch
  Scenario: CreateBookingCommand - Service doesn't belong to provider
    Given I am authenticated as a customer
    And another provider has a different service
    When I execute CreateBookingCommand with service from different provider
    Then the command should fail with ConflictException
    And the error message should contain "Service does not belong to the specified provider"

  @command @booking @create @validation @staff-not-found
  Scenario: CreateBookingCommand - Staff member not found
    Given I am authenticated as a customer
    When I execute CreateBookingCommand with non-existent staff ID
    Then the command should fail with NotFoundException
    And the error message should contain "Staff member with ID"

  @command @booking @create @validation @slot-not-available
  Scenario: CreateBookingCommand - Time slot not available
    Given I am authenticated as a customer
    And there is already a booking at 10:00 AM tomorrow
    When I execute CreateBookingCommand for the same time slot
    Then the command should fail with ConflictException
    And the error message should contain "time slot is not available"

  @command @booking @create @validation @constraints-failed
  Scenario: CreateBookingCommand - Validation constraints fail
    Given I am authenticated as a customer
    When I execute CreateBookingCommand for a time in the past
    Then the command should fail with ConflictException
    And the error message should contain "Booking validation failed"

  @command @booking @create @business-logic @booking-policy
  Scenario: CreateBookingCommand - Uses service booking policy
    Given I am authenticated as a customer
    And the service has a custom booking policy
    When I execute CreateBookingCommand
    Then the booking should use the service's booking policy
    And the deposit requirement should match the policy

  @command @booking @create @business-logic @default-policy
  Scenario: CreateBookingCommand - Uses default policy when service has none
    Given I am authenticated as a customer
    And the service has no booking policy
    When I execute CreateBookingCommand
    Then the booking should use the default booking policy

  # ==================== ConfirmBookingCommandHandler ====================

  @command @booking @confirm @happy-path
  Scenario: ConfirmBookingCommand - Success path
    Given I am authenticated as the provider
    And there is a requested booking
    When I execute ConfirmBookingCommand
    Then the command should succeed
    And the booking status should be "Confirmed"
    And BookingConfirmedEvent should be published
    And customer should receive confirmation notification

  @command @booking @confirm @validation @booking-not-found
  Scenario: ConfirmBookingCommand - Booking not found
    Given I am authenticated as the provider
    When I execute ConfirmBookingCommand with non-existent booking ID
    Then the command should fail with NotFoundException

  @command @booking @confirm @validation @wrong-status
  Scenario: ConfirmBookingCommand - Booking already confirmed
    Given I am authenticated as the provider
    And there is a confirmed booking
    When I execute ConfirmBookingCommand again
    Then the command should fail with InvalidOperationException
    And the error should indicate invalid booking status

  @command @booking @confirm @authorization
  Scenario: ConfirmBookingCommand - Only provider can confirm
    Given I am authenticated as a customer
    And there is a requested booking
    When I execute ConfirmBookingCommand
    Then the command should fail with ForbiddenException

  # ==================== CancelBookingCommandHandler ====================

  @command @booking @cancel @happy-path
  Scenario: CancelBookingCommand - Customer cancels own booking
    Given I am authenticated as a customer
    And I have a confirmed booking
    When I execute CancelBookingCommand with reason "Schedule conflict"
    Then the command should succeed
    And the booking status should be "Cancelled"
    And BookingCancelledEvent should be published
    And the cancellation reason should be saved

  @command @booking @cancel @provider-cancels
  Scenario: CancelBookingCommand - Provider cancels booking
    Given I am authenticated as the provider
    And there is a confirmed booking
    When I execute CancelBookingCommand with reason "Provider unavailable"
    Then the command should succeed
    And the booking should be marked as cancelled by provider
    And customer should be notified

  @command @booking @cancel @validation @past-cancellation-window
  Scenario: CancelBookingCommand - Outside cancellation window
    Given I am authenticated as a customer
    And I have a booking starting in 2 hours
    And the cancellation policy requires 24 hours notice
    When I execute CancelBookingCommand
    Then the command should fail with PolicyViolationException
    And a cancellation fee may be applied

  @command @booking @cancel @refund
  Scenario: CancelBookingCommand - Triggers refund if payment exists
    Given I am authenticated as a customer
    And I have a paid booking
    When I execute CancelBookingCommand
    Then the command should succeed
    And BookingRefundProcessedEvent should be published

  # ==================== RescheduleBookingCommandHandler ====================

  @command @booking @reschedule @happy-path
  Scenario: RescheduleBookingCommand - Success path
    Given I am authenticated as a customer
    And I have a confirmed booking for tomorrow at 10:00
    When I execute RescheduleBookingCommand to move it to next week
    Then the command should succeed
    And the old booking should be marked as "Rescheduled"
    And a new booking should be created with new time
    And BookingRescheduledEvent should be published

  @command @booking @reschedule @validation @new-slot-unavailable
  Scenario: RescheduleBookingCommand - New time slot unavailable
    Given I am authenticated as a customer
    And I have a booking
    When I execute RescheduleBookingCommand to an already booked slot
    Then the command should fail with ConflictException

  @command @booking @reschedule @validation @past-time
  Scenario: RescheduleBookingCommand - Cannot reschedule to past
    Given I am authenticated as a customer
    And I have a booking
    When I execute RescheduleBookingCommand to a past time
    Then the command should fail with ValidationException

  @command @booking @reschedule @authorization
  Scenario: RescheduleBookingCommand - Only customer can reschedule
    Given another customer has a booking
    And I am authenticated as a different customer
    When I execute RescheduleBookingCommand for the other booking
    Then the command should fail with ForbiddenException

  # ==================== CompleteBookingCommandHandler ====================

  @command @booking @complete @happy-path
  Scenario: CompleteBookingCommand - Mark booking as completed
    Given I am authenticated as the provider
    And there is a confirmed booking that has passed
    When I execute CompleteBookingCommand
    Then the command should succeed
    And the booking status should be "Completed"
    And BookingCompletedEvent should be published
    And payout should be eligible for inclusion

  @command @booking @complete @validation @future-booking
  Scenario: CompleteBookingCommand - Cannot complete future booking
    Given I am authenticated as the provider
    And there is a confirmed booking for tomorrow
    When I execute CompleteBookingCommand
    Then the command should fail with ValidationException
    And the error should indicate booking hasn't occurred yet

  @command @booking @complete @validation @wrong-status
  Scenario: CompleteBookingCommand - Can only complete confirmed bookings
    Given I am authenticated as the provider
    And there is a cancelled booking
    When I execute CompleteBookingCommand
    Then the command should fail with InvalidOperationException

  # ==================== MarkNoShowCommandHandler ====================

  @command @booking @no-show @happy-path
  Scenario: MarkNoShowCommand - Mark customer as no-show
    Given I am authenticated as the provider
    And there is a confirmed booking that has passed
    And the customer didn't show up
    When I execute MarkNoShowCommand
    Then the command should succeed
    And the booking status should be "NoShow"
    And BookingNoShowEvent should be published
    And a no-show penalty may be applied

  @command @booking @no-show @validation @future-booking
  Scenario: MarkNoShowCommand - Cannot mark future booking as no-show
    Given I am authenticated as the provider
    And there is a confirmed booking for tomorrow
    When I execute MarkNoShowCommand
    Then the command should fail with ValidationException

  @command @booking @no-show @authorization
  Scenario: MarkNoShowCommand - Only provider can mark no-show
    Given I am authenticated as a customer
    And there is a confirmed booking
    When I execute MarkNoShowCommand
    Then the command should fail with ForbiddenException

  @command @booking @no-show @policy
  Scenario: MarkNoShowCommand - No-show fee charged according to policy
    Given I am authenticated as the provider
    And there is a confirmed paid booking
    And the policy specifies a no-show fee
    When I execute MarkNoShowCommand
    Then the command should succeed
    And the no-show fee should be charged
    And the customer should not receive a refund
