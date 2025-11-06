Feature: Notification Management
  As a system and provider
  I want to send and manage notifications
  So that users are informed about important events

  Background:
    Given a provider "Test Salon" exists with active status
    And I am authenticated as a customer

  @smoke @notification @send @email
  Scenario: Send an email notification
    When I send a POST request to create a notification with:
      | Field            | Value                            |
      | Type             | BookingConfirmation              |
      | Channel          | Email                            |
      | Subject          | Booking Confirmed                |
      | Body             | Your booking has been confirmed  |
      | Priority         | High                             |
      | RecipientEmail   | customer@test.com                |
    Then the response status code should be 200
    And the notification should be created successfully
    And the notification should exist in the database

  @notification @send @sms
  Scenario: Send an SMS notification
    When I send a POST request to create an SMS notification with:
      | Field           | Value                           |
      | Type            | BookingReminder                 |
      | Channel         | SMS                             |
      | Body            | Reminder: Appointment tomorrow  |
      | Priority        | Normal                          |
      | RecipientPhone  | +1234567890                     |
    Then the response status code should be 200
    And the SMS notification should be created

  @notification @schedule
  Scenario: Schedule a future notification
    When I send a POST request to schedule a notification:
      | Field          | Value                          |
      | Type           | BookingReminder                |
      | Channel        | Email                          |
      | Subject        | Upcoming Appointment           |
      | Body           | You have an appointment tomorrow|
      | ScheduledFor   | tomorrow at 09:00              |
      | Priority       | Normal                         |
      | RecipientEmail | customer@test.com              |
    Then the response status code should be 200
    And the notification should be scheduled for the future
    And the notification status should be "Scheduled"

  @notification @bulk
  Scenario: Send bulk notifications to multiple recipients
    Given there are 10 customers in the system
    And I am authenticated as the provider
    When I send a POST request to send bulk notification:
      | Field     | Value                    |
      | Type      | Announcement             |
      | Channel   | Email                    |
      | Subject   | New Services Available   |
      | Body      | Check out our new services|
      | Priority  | Low                      |
    Then the response status code should be 200
    And 10 notifications should be created
    And all notifications should be queued for sending

  @notification @cancel
  Scenario: Cancel a scheduled notification
    Given I have a scheduled notification for tomorrow
    When I send a POST request to cancel the notification with reason "Customer cancelled appointment"
    Then the response status code should be 200
    And the notification status should be "Cancelled"

  @notification @resend
  Scenario: Resend a failed notification
    Given I have a failed notification
    When I send a POST request to resend the notification
    Then the response status code should be 200
    And a new notification attempt should be created

  @notification @history @user
  Scenario: User views their notification history
    Given I have received 5 notifications in the past week
    When I send a GET request to "/api/v1/notifications/history"
    Then the response status code should be 200
    And the response should contain 5 notifications
    And notifications should be ordered by date descending

  @notification @history @filter @type
  Scenario: Filter notification history by type
    Given I have notifications of various types
    When I send a GET request to "/api/v1/notifications/history?type=BookingConfirmation"
    Then the response status code should be 200
    And all returned notifications should be of type "BookingConfirmation"

  @notification @history @filter @channel
  Scenario: Filter notification history by channel
    Given I have notifications sent via Email and SMS
    When I send a GET request to "/api/v1/notifications/history?channel=Email"
    Then the response status code should be 200
    And all returned notifications should be Email notifications

  @notification @status
  Scenario: Check notification delivery status
    Given I have sent a notification
    When I send a GET request to check the notification status
    Then the response status code should be 200
    And the response should contain delivery status and timestamps

  @notification @analytics
  Scenario: View notification analytics
    Given I am authenticated as an admin
    And there have been 100 notifications sent in the last month
    When I send a GET request to "/api/v1/notifications/analytics"
    Then the response status code should be 200
    And the response should include:
      | Metric            |
      | TotalSent         |
      | SuccessfulDeliveries|
      | FailedDeliveries  |
      | OpenRate          |
      | ClickRate         |

  @notification @analytics @provider
  Scenario: Provider views their notification analytics
    Given I am authenticated as the provider
    When I request notification analytics for my provider
    Then the response should show statistics for my notifications only

  @notification @template
  Scenario: Use notification template
    Given a notification template "booking_confirmation" exists
    When I send a notification using the template with variables:
      | Variable        | Value            |
      | customerName    | John Doe         |
      | bookingTime     | Tomorrow at 2 PM |
      | serviceName     | Haircut          |
    Then the notification should be populated with template content
    And variables should be replaced with actual values

  @notification @preferences
  Scenario: Respect user notification preferences
    Given I have disabled SMS notifications in my preferences
    When the system tries to send me an SMS notification
    Then the notification should not be sent
    And an Email notification should be sent instead

  @notification @negative @missing-recipient
  Scenario: Cannot send notification without recipient
    When I send a POST request to create a notification without recipient info
    Then the response status code should be 400
    And the error should indicate missing recipient

  @notification @negative @invalid-channel
  Scenario: Cannot send notification with invalid channel
    When I send a POST request with channel "InvalidChannel"
    Then the response status code should be 400

  @notification @negative @unauthorized @bulk
  Scenario: Customer cannot send bulk notifications
    Given I am authenticated as a customer
    When I try to send bulk notifications
    Then the response status code should be 403

  @notification @retry
  Scenario: Failed notifications are automatically retried
    Given a notification failed to send
    When the retry mechanism runs
    Then the notification should be attempted again
    And retry count should be incremented

  @notification @priority @high
  Scenario: High priority notifications are sent immediately
    When I send a high priority notification
    Then it should be processed immediately
    And it should not be queued

  @notification @priority @low
  Scenario: Low priority notifications are batched
    When I send a low priority notification
    Then it should be queued for batch processing
    And it may be delayed up to configured batch interval

  @notification @unsubscribe
  Scenario: User unsubscribes from marketing notifications
    When I click the unsubscribe link in a marketing email
    Then my preference should be updated
    And I should not receive marketing notifications
    And I should still receive transactional notifications
