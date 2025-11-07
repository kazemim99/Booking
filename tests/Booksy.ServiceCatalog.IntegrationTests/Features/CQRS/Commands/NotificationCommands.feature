Feature: Notification Command Handlers - Complete Coverage
  As a system
  I want to test all notification command handlers
  So that every code path and validation is covered

  Background:
    Given a provider "Test Provider" exists with active status
    And I am authenticated as a customer

  # ==================== SendNotificationCommandHandler ====================

  @command @notification @send @email @happy-path
  Scenario: SendNotificationCommand - Send email notification
    When I execute SendNotificationCommand with:
      | Field          | Value                           |
      | RecipientId    | [Customer:Id]                   |
      | Type           | BookingConfirmation             |
      | Channel        | Email                           |
      | Subject        | Booking Confirmed               |
      | Body           | Your booking has been confirmed |
      | Priority       | High                            |
      | RecipientEmail | customer@test.com               |
    Then the command should succeed
    And a Notification aggregate should be created
    And the notification should be queued
    And the email service should be invoked
    And NotificationSentEvent should be published

  @command @notification @send @sms @happy-path
  Scenario: SendNotificationCommand - Send SMS notification
    When I execute SendNotificationCommand with:
      | Field          | Value                  |
      | Type           | BookingReminder        |
      | Channel        | SMS                    |
      | Body           | Appointment tomorrow   |
      | Priority       | Normal                 |
      | RecipientPhone | +1234567890            |
    Then the command should succeed
    And the SMS service should be invoked
    And the notification should be sent

  @command @notification @send @push @happy-path
  Scenario: SendNotificationCommand - Send push notification
    When I execute SendNotificationCommand with:
      | Field   | Value                       |
      | Type    | PaymentReceived             |
      | Channel | PushNotification            |
      | Subject | Payment Received            |
      | Body    | Your payment was successful |
    Then the command should succeed
    And the push notification service should be invoked

  @command @notification @send @in-app @happy-path
  Scenario: SendNotificationCommand - Send in-app notification
    When I execute SendNotificationCommand with:
      | Field   | Value                    |
      | Type    | NewMessage               |
      | Channel | InApp                    |
      | Subject | New message from provider|
      | Body    | You have a new message   |
    Then the command should succeed
    And the in-app notification service should be invoked

  @command @notification @send @validation @missing-email
  Scenario: SendNotificationCommand - Email channel requires recipient email
    When I execute SendNotificationCommand with:
      | Field   | Value |
      | Channel | Email |
      | Subject | Test  |
      | Body    | Test  |
    And no recipient email is provided
    Then the command should fail with InvalidOperationException
    And the error message should contain "Recipient email is required"

  @command @notification @send @validation @missing-phone
  Scenario: SendNotificationCommand - SMS channel requires recipient phone
    When I execute SendNotificationCommand with:
      | Field   | Value |
      | Channel | SMS   |
      | Body    | Test  |
    And no recipient phone is provided
    Then the command should fail with InvalidOperationException
    And the error message should contain "Recipient phone is required"

  @command @notification @send @validation @unsupported-channel
  Scenario: SendNotificationCommand - Unsupported channel
    When I execute SendNotificationCommand with channel "InvalidChannel"
    Then the command should fail with NotSupportedException
    And the error message should contain "not supported"

  @command @notification @send @business-logic @template
  Scenario: SendNotificationCommand - Use template with data
    When I execute SendNotificationCommand with:
      | Field        | Value                  |
      | TemplateId   | booking_confirmation   |
    And template data:
      | Variable     | Value       |
      | customerName | John Doe    |
      | serviceName  | Haircut     |
      | bookingTime  | Tomorrow 2PM|
    Then the command should succeed
    And the notification should use the template
    And the template data should be applied

  @command @notification @send @business-logic @related-entities
  Scenario: SendNotificationCommand - Link to related entities
    When I execute SendNotificationCommand with:
      | Field      | Value           |
      | BookingId  | [Booking:Id]    |
      | PaymentId  | [Payment:Id]    |
      | ProviderId | [Provider:Id]   |
    Then the command should succeed
    And the notification should be linked to the booking
    And the notification should be linked to the payment
    And the notification should be linked to the provider

  @command @notification @send @business-logic @metadata
  Scenario: SendNotificationCommand - Include custom metadata
    When I execute SendNotificationCommand with metadata:
      | Key             | Value         |
      | campaign_id     | summer_promo  |
      | tracking_code   | ABC123        |
      | source          | mobile_app    |
    Then the command should succeed
    And the notification should include the metadata

  @command @notification @send @business-logic @recipient-info
  Scenario: SendNotificationCommand - Set recipient contact information
    When I execute SendNotificationCommand with:
      | Field          | Value              |
      | RecipientEmail | john@test.com      |
      | RecipientPhone | +1234567890        |
      | RecipientName  | John Doe           |
    Then the command should succeed
    And the notification recipient info should be set

  @command @notification @send @error-handling @service-failure
  Scenario: SendNotificationCommand - Email service fails but notification is saved
    When I execute SendNotificationCommand to send email
    And the email service fails with error "SMTP connection failed"
    Then the command should succeed
    And the notification should be saved to database
    And the notification status should be "Failed"
    And the error message should be saved
    And the notification can be retried later

  @command @notification @send @error-handling @sms-failure
  Scenario: SendNotificationCommand - SMS service fails
    When I execute SendNotificationCommand to send SMS
    And the SMS service fails with error "Invalid phone number"
    Then the command should succeed
    And the notification should be marked as failed
    And the error should be logged

  @command @notification @send @business-logic @delivery-confirmation
  Scenario: SendNotificationCommand - Service returns message ID
    When I execute SendNotificationCommand to send email
    And the email service succeeds with message ID "msg_abc123"
    Then the command should succeed
    And the notification should be marked as delivered
    And the gateway message ID should be "msg_abc123"

  @command @notification @send @business-logic @plain-text
  Scenario: SendNotificationCommand - Include plain text body for emails
    When I execute SendNotificationCommand with:
      | Field          | Value                    |
      | Channel        | Email                    |
      | Body           | <h1>HTML Content</h1>    |
      | PlainTextBody  | Plain text version       |
    Then the command should succeed
    And both HTML and plain text should be sent

  # ==================== ScheduleNotificationCommandHandler ====================

  @command @notification @schedule @happy-path
  Scenario: ScheduleNotificationCommand - Schedule future notification
    When I execute ScheduleNotificationCommand with:
      | Field       | Value                      |
      | Type        | BookingReminder            |
      | Channel     | Email                      |
      | Subject     | Upcoming Appointment       |
      | Body        | Your appointment is tomorrow|
      | ScheduledFor| tomorrow at 09:00          |
    Then the command should succeed
    And the notification should be scheduled for tomorrow at 09:00
    And the notification status should be "Scheduled"
    And NotificationScheduledEvent should be published

  @command @notification @schedule @validation @past-time
  Scenario: ScheduleNotificationCommand - Cannot schedule in the past
    When I execute ScheduleNotificationCommand with:
      | Field        | Value        |
      | ScheduledFor | yesterday    |
    Then the command should fail with ValidationException
    And the error message should contain "cannot schedule in the past"

  @command @notification @schedule @business-logic @timezone
  Scenario: ScheduleNotificationCommand - Schedule with timezone
    When I execute ScheduleNotificationCommand with:
      | Field        | Value                    |
      | ScheduledFor | 2025-02-01 10:00 PST     |
    Then the command should succeed
    And the notification should be scheduled in Pacific timezone

  @command @notification @schedule @business-logic @template
  Scenario: ScheduleNotificationCommand - Schedule notification with template
    When I execute ScheduleNotificationCommand with:
      | Field      | Value                  |
      | TemplateId | appointment_reminder   |
    And template data for scheduled notification
    Then the command should succeed
    And the template should be applied when sent

  # ==================== SendBulkNotificationCommandHandler ====================

  @command @notification @bulk @happy-path
  Scenario: SendBulkNotificationCommand - Send to multiple recipients
    Given I am authenticated as the provider
    And there are 10 customers
    When I execute SendBulkNotificationCommand with:
      | Field   | Value                  |
      | Type    | Announcement           |
      | Channel | Email                  |
      | Subject | New Services Available |
      | Body    | Check out our services |
    Then the command should succeed
    And 10 notifications should be created
    And all notifications should be queued
    And BulkNotificationSentEvent should be published

  @command @notification @bulk @validation @empty-recipients
  Scenario: SendBulkNotificationCommand - No recipients provided
    Given I am authenticated as the provider
    When I execute SendBulkNotificationCommand with no recipients
    Then the command should fail with ValidationException
    And the error message should contain "at least one recipient"

  @command @notification @bulk @business-logic @filters
  Scenario: SendBulkNotificationCommand - Filter recipients by criteria
    Given I am authenticated as the provider
    And there are 20 customers
    When I execute SendBulkNotificationCommand with filters:
      | Filter           | Value    |
      | CustomerType     | VIP      |
      | HasActiveBooking | true     |
    Then the command should succeed
    And only matching customers should receive notifications

  @command @notification @bulk @authorization
  Scenario: SendBulkNotificationCommand - Only providers can send bulk to their customers
    Given I am authenticated as a customer
    When I execute SendBulkNotificationCommand
    Then the command should fail with ForbiddenException

  @command @notification @bulk @business-logic @rate-limiting
  Scenario: SendBulkNotificationCommand - Respect rate limits
    Given I am authenticated as the provider
    When I execute SendBulkNotificationCommand to 1000 recipients
    Then the command should succeed
    And notifications should be queued in batches
    And rate limiting should be applied

  @command @notification @bulk @error-handling @partial-failure
  Scenario: SendBulkNotificationCommand - Some notifications fail
    Given I am authenticated as the provider
    And there are 10 customers
    When I execute SendBulkNotificationCommand
    And 3 notifications fail to send
    Then the command should succeed
    And 7 notifications should be delivered
    And 3 notifications should be marked as failed
    And retry should be scheduled for failed notifications

  # ==================== CancelNotificationCommandHandler ====================

  @command @notification @cancel @happy-path
  Scenario: CancelNotificationCommand - Cancel scheduled notification
    Given there is a scheduled notification for tomorrow
    When I execute CancelNotificationCommand with reason "Customer cancelled appointment"
    Then the command should succeed
    And the notification status should be "Cancelled"
    And the cancellation reason should be saved
    And NotificationCancelledEvent should be published

  @command @notification @cancel @validation @notification-not-found
  Scenario: CancelNotificationCommand - Notification not found
    When I execute CancelNotificationCommand with non-existent notification ID
    Then the command should fail with NotFoundException

  @command @notification @cancel @validation @already-sent
  Scenario: CancelNotificationCommand - Cannot cancel already sent notification
    Given there is a sent notification
    When I execute CancelNotificationCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "already sent"

  @command @notification @cancel @idempotency
  Scenario: CancelNotificationCommand - Already cancelled notification
    Given there is a cancelled notification
    When I execute CancelNotificationCommand again
    Then the command should succeed
    And no state change should occur

  @command @notification @cancel @authorization
  Scenario: CancelNotificationCommand - Only notification owner can cancel
    Given another user has a scheduled notification
    When I try to cancel their notification
    Then the command should fail with ForbiddenException

  # ==================== ResendNotificationCommandHandler ====================

  @command @notification @resend @happy-path
  Scenario: ResendNotificationCommand - Resend failed notification
    Given there is a failed notification
    When I execute ResendNotificationCommand
    Then the command should succeed
    And a new notification attempt should be created
    And the notification service should be invoked again
    And NotificationResentEvent should be published

  @command @notification @resend @validation @notification-not-found
  Scenario: ResendNotificationCommand - Notification not found
    When I execute ResendNotificationCommand with non-existent notification ID
    Then the command should fail with NotFoundException

  @command @notification @resend @business-logic @increment-attempts
  Scenario: ResendNotificationCommand - Increment retry count
    Given there is a failed notification with 2 retry attempts
    When I execute ResendNotificationCommand
    Then the command should succeed
    And the retry count should be 3

  @command @notification @resend @validation @max-retries
  Scenario: ResendNotificationCommand - Maximum retries exceeded
    Given there is a failed notification with 5 retry attempts
    When I execute ResendNotificationCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "maximum retries"

  @command @notification @resend @business-logic @delivered
  Scenario: ResendNotificationCommand - Cannot resend delivered notification
    Given there is a delivered notification
    When I execute ResendNotificationCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "already delivered"

  @command @notification @resend @authorization
  Scenario: ResendNotificationCommand - Admin can resend any notification
    Given I am authenticated as an admin
    And there is a failed notification
    When I execute ResendNotificationCommand
    Then the command should succeed

  # ==================== UpdatePreferencesCommandHandler ====================

  @command @notification @preferences @update @happy-path
  Scenario: UpdatePreferencesCommand - Update notification preferences
    When I execute UpdatePreferencesCommand with:
      | Preference        | Value |
      | EmailEnabled      | true  |
      | SmsEnabled        | false |
      | PushEnabled       | true  |
    Then the command should succeed
    And my notification preferences should be updated
    And PreferencesUpdatedEvent should be published

  @command @notification @preferences @update @business-logic @channel-disable
  Scenario: UpdatePreferencesCommand - Disable SMS notifications
    Given I have SMS notifications enabled
    When I execute UpdatePreferencesCommand to disable SMS
    Then the command should succeed
    And future SMS notifications should not be sent to me
    And I should receive email notifications instead

  @command @notification @preferences @update @business-logic @opt-out
  Scenario: UpdatePreferencesCommand - Opt out of marketing notifications
    When I execute UpdatePreferencesCommand with:
      | Preference           | Value |
      | MarketingEnabled     | false |
    Then the command should succeed
    And I should not receive marketing notifications
    And I should still receive transactional notifications

  @command @notification @preferences @update @validation @required-channel
  Scenario: UpdatePreferencesCommand - At least one channel must be enabled
    When I execute UpdatePreferencesCommand to disable all channels
    Then the command should fail with ValidationException
    And the error message should contain "at least one notification channel"

  @command @notification @preferences @update @authorization
  Scenario: UpdatePreferencesCommand - Can only update own preferences
    Given another user exists
    When I try to update their notification preferences
    Then the command should fail with ForbiddenException
