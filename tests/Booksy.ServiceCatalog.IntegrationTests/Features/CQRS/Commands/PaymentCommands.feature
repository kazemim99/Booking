Feature: Payment Command Handlers - Complete Coverage
  As a system
  I want to test all payment command handlers
  So that every code path, validation, and business rule is covered

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has a service "Haircut" priced at 100.00 USD
    And I am authenticated as a customer
    And I have a confirmed booking

  # ==================== ProcessPaymentCommandHandler ====================

  @command @payment @process @happy-path @with-booking
  Scenario: ProcessPaymentCommand - Process payment for booking
    When I execute ProcessPaymentCommand with:
      | Field              | Value        |
      | BookingId          | [Booking:Current:Id] |
      | Amount             | 100.00       |
      | Currency           | USD          |
      | Method             | CreditCard   |
      | PaymentMethodId    | pm_test      |
      | CaptureImmediately | true         |
    Then the command should succeed
    And a Payment aggregate should be created for the booking
    And PaymentCreatedEvent should be published
    And PaymentProcessedEvent should be published
    And the payment status should be "Paid"

  @command @payment @process @happy-path @direct
  Scenario: ProcessPaymentCommand - Process direct payment (no booking)
    When I execute ProcessPaymentCommand without booking ID:
      | Field              | Value        |
      | Amount             | 100.00       |
      | Currency           | USD          |
      | Method             | CreditCard   |
      | PaymentMethodId    | pm_test      |
    Then the command should succeed
    And a direct Payment aggregate should be created
    And the payment should not be linked to a booking

  @command @payment @process @gateway-success
  Scenario: ProcessPaymentCommand - Payment gateway succeeds
    When I execute ProcessPaymentCommand
    And the payment gateway returns success
    Then Payment.ProcessCharge should be called
    And PaymentProcessedEvent should be published
    And the payment should have a PaymentIntentId
    And the payment status should be "Paid"

  @command @payment @process @gateway-failure
  Scenario: ProcessPaymentCommand - Payment gateway fails
    When I execute ProcessPaymentCommand
    And the payment gateway returns failure with error "Insufficient funds"
    Then Payment.MarkAsFailed should be called
    And PaymentFailedEvent should be published
    And the payment should contain the error message
    And the command result should indicate failure

  @command @payment @process @validation @amount
  Scenario: ProcessPaymentCommand - Invalid amount validation
    When I execute ProcessPaymentCommand with amount 0.00
    Then the command should fail with ValidationException
    And the error should indicate amount must be greater than zero

  @command @payment @process @validation @amount-negative
  Scenario: ProcessPaymentCommand - Negative amount validation
    When I execute ProcessPaymentCommand with amount -50.00
    Then the command should fail with ValidationException
    And the error should indicate invalid amount

  @command @payment @process @validation @currency
  Scenario: ProcessPaymentCommand - Invalid currency validation
    When I execute ProcessPaymentCommand with currency "INVALID"
    Then the command should fail with ValidationException
    And the error should indicate invalid currency code

  @command @payment @process @validation @payment-method-id
  Scenario: ProcessPaymentCommand - Empty payment method ID
    When I execute ProcessPaymentCommand with empty PaymentMethodId
    Then the command should fail with ValidationException
    And the error should indicate PaymentMethodId is required

  @command @payment @process @validation @customer-id
  Scenario: ProcessPaymentCommand - Invalid customer ID
    When I execute ProcessPaymentCommand with invalid customer ID
    Then the command should fail with ValidationException

  @command @payment @process @metadata
  Scenario: ProcessPaymentCommand - Payment includes metadata
    When I execute ProcessPaymentCommand with custom metadata
    Then the command should succeed
    And the payment should contain the metadata
    And the metadata should be sent to payment gateway

  # ==================== CapturePaymentCommandHandler ====================

  @command @payment @capture @happy-path @full
  Scenario: CapturePaymentCommand - Capture full authorized amount
    Given I have an authorized payment of 100.00 USD
    When I execute CapturePaymentCommand for full amount
    Then the command should succeed
    And PaymentCapturedEvent should be published
    And the payment status should be "Paid"
    And the captured amount should equal authorized amount

  @command @payment @capture @happy-path @partial
  Scenario: CapturePaymentCommand - Capture partial amount
    Given I have an authorized payment of 100.00 USD
    When I execute CapturePaymentCommand for 50.00 USD
    Then the command should succeed
    And PaymentCapturedEvent should be published
    And the captured amount should be 50.00
    And the remaining authorized amount should be 50.00

  @command @payment @capture @validation @payment-not-found
  Scenario: CapturePaymentCommand - Payment not found
    When I execute CapturePaymentCommand with non-existent payment ID
    Then the command should fail with NotFoundException
    And the error should indicate payment not found

  @command @payment @capture @validation @not-authorized
  Scenario: CapturePaymentCommand - Payment not in authorized state
    Given I have a completed payment
    When I execute CapturePaymentCommand
    Then the command should fail with ConflictException
    And the error should indicate payment not authorized

  @command @payment @capture @validation @amount-exceeds
  Scenario: CapturePaymentCommand - Capture amount exceeds authorized
    Given I have an authorized payment of 100.00 USD
    When I execute CapturePaymentCommand for 150.00 USD
    Then the command should fail with ValidationException
    And the error should indicate amount exceeds authorized amount

  @command @payment @capture @validation @zero-amount
  Scenario: CapturePaymentCommand - Cannot capture zero amount
    Given I have an authorized payment
    When I execute CapturePaymentCommand for 0.00
    Then the command should fail with ValidationException

  @command @payment @capture @gateway-failure
  Scenario: CapturePaymentCommand - Gateway capture fails
    Given I have an authorized payment
    When I execute CapturePaymentCommand
    And the payment gateway capture fails
    Then the command should fail with PaymentGatewayException
    And the payment should remain in authorized state
    And CaptureFailedEvent should be published

  @command @payment @capture @authorization
  Scenario: CapturePaymentCommand - Only provider can capture
    Given I am authenticated as a customer
    And there is an authorized payment
    When I execute CapturePaymentCommand
    Then the command should fail with ForbiddenException

  # ==================== RefundPaymentCommandHandler ====================

  @command @payment @refund @happy-path @full
  Scenario: RefundPaymentCommand - Full refund
    Given I have a paid payment of 100.00 USD
    When I execute RefundPaymentCommand with:
      | Field  | Value              |
      | Amount | 100.00             |
      | Reason | CustomerRequest    |
      | Notes  | Customer requested |
    Then the command should succeed
    And PaymentRefundedEvent should be published
    And the payment status should be "Refunded"
    And the refunded amount should be 100.00

  @command @payment @refund @happy-path @partial
  Scenario: RefundPaymentCommand - Partial refund
    Given I have a paid payment of 100.00 USD
    When I execute RefundPaymentCommand for 50.00 USD
    Then the command should succeed
    And PaymentRefundedEvent should be published
    And the payment status should be "PartiallyRefunded"
    And the refunded amount should be 50.00
    And the remaining refundable amount should be 50.00

  @command @payment @refund @multiple-partial
  Scenario: RefundPaymentCommand - Multiple partial refunds
    Given I have a paid payment of 100.00 USD
    When I execute RefundPaymentCommand for 30.00 USD
    And I execute RefundPaymentCommand for 40.00 USD again
    Then both commands should succeed
    And the total refunded amount should be 70.00
    And the payment status should be "PartiallyRefunded"

  @command @payment @refund @validation @payment-not-found
  Scenario: RefundPaymentCommand - Payment not found
    When I execute RefundPaymentCommand with non-existent payment ID
    Then the command should fail with NotFoundException

  @command @payment @refund @validation @not-paid
  Scenario: RefundPaymentCommand - Cannot refund unpaid payment
    Given I have a pending payment
    When I execute RefundPaymentCommand
    Then the command should fail with ConflictException
    And the error should indicate payment not in refundable state

  @command @payment @refund @validation @already-refunded
  Scenario: RefundPaymentCommand - Cannot refund already fully refunded payment
    Given I have a fully refunded payment
    When I execute RefundPaymentCommand
    Then the command should fail with ConflictException
    And the error should indicate payment already refunded

  @command @payment @refund @validation @amount-exceeds
  Scenario: RefundPaymentCommand - Refund amount exceeds paid amount
    Given I have a paid payment of 100.00 USD
    When I execute RefundPaymentCommand for 150.00 USD
    Then the command should fail with ValidationException
    And the error should indicate refund exceeds paid amount

  @command @payment @refund @validation @zero-amount
  Scenario: RefundPaymentCommand - Cannot refund zero amount
    Given I have a paid payment
    When I execute RefundPaymentCommand for 0.00
    Then the command should fail with ValidationException

  @command @payment @refund @validation @no-reason
  Scenario: RefundPaymentCommand - Reason is required
    Given I have a paid payment
    When I execute RefundPaymentCommand without a reason
    Then the command should fail with ValidationException
    And the error should indicate reason is required

  @command @payment @refund @gateway-failure
  Scenario: RefundPaymentCommand - Gateway refund fails
    Given I have a paid payment
    When I execute RefundPaymentCommand
    And the payment gateway refund fails
    Then the command should fail with PaymentGatewayException
    And the payment should remain in paid state
    And RefundFailedEvent should be published

  @command @payment @refund @authorization @provider-only
  Scenario: RefundPaymentCommand - Only provider or admin can refund
    Given I am authenticated as a customer
    And there is a paid payment
    When I execute RefundPaymentCommand
    Then the command should fail with ForbiddenException

  @command @payment @refund @timing
  Scenario: RefundPaymentCommand - Records refund timestamp
    Given I have a paid payment
    When I execute RefundPaymentCommand
    Then the command should succeed
    And the refund should have a timestamp
    And the refund metadata should include reason and notes

  @command @payment @refund @idempotency
  Scenario: RefundPaymentCommand - Idempotent for same refund request
    Given I have a paid payment
    When I execute RefundPaymentCommand with idempotency key "ref-123"
    And I execute the same RefundPaymentCommand again with same key
    Then the second command should return the same result
    And only one refund should be processed
