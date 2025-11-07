Feature: Progressive Provider Registration Command Handlers - Complete Coverage
  As a system
  I want to test all progressive provider registration command handlers
  So that the multi-step registration flow is fully validated

  Background:
    Given I am authenticated as a new user
    And I want to become a provider

  # ==================== SaveStep3LocationCommandHandler ====================

  @command @registration @step3 @location @happy-path @new-draft
  Scenario: SaveStep3LocationCommand - Create new draft provider
    When I execute SaveStep3LocationCommand with:
      | Field                 | Value                   |
      | BusinessName          | Luxury Salon & Spa      |
      | BusinessDescription   | Premium beauty services |
      | Category              | BeautyAndWellness       |
      | Email                 | salon@test.com          |
      | PhoneNumber           | +989123456789           |
      | AddressLine1          | 123 Main Street         |
      | City                  | Tehran                  |
      | Province              | Tehran                  |
      | PostalCode            | 1234567890              |
      | Latitude              | 35.6892                 |
      | Longitude             | 51.3890                 |
    Then the command should succeed
    And a draft Provider aggregate should be created
    And the registration step should be 3
    And the provider status should be "Drafted"
    And the location should be saved

  @command @registration @step3 @location @happy-path @update-existing
  Scenario: SaveStep3LocationCommand - Update existing draft
    Given I have a draft provider from previous steps
    When I execute SaveStep3LocationCommand with updated location
    Then the command should succeed
    And the existing draft should be updated
    And the registration step should be 3
    And no duplicate provider should be created

  @command @registration @step3 @location @validation @category
  Scenario: SaveStep3LocationCommand - Invalid category
    When I execute SaveStep3LocationCommand with:
      | Field    | Value          |
      | Category | InvalidCategory|
    Then the command should fail with InvalidOperationException
    And the error message should contain "Invalid category"

  @command @registration @step3 @location @business-logic @address-formatting
  Scenario: SaveStep3LocationCommand - Format address with two lines
    When I execute SaveStep3LocationCommand with:
      | Field        | Value         |
      | AddressLine1 | 123 Main St   |
      | AddressLine2 | Suite 100     |
      | City         | Tehran        |
      | Province     | Tehran        |
    Then the command should succeed
    And the street should be "123 Main St, Suite 100"
    And the formatted address should include city and province

  @command @registration @step3 @location @business-logic @contact-info
  Scenario: SaveStep3LocationCommand - Save contact information
    When I execute SaveStep3LocationCommand with:
      | Field       | Value            |
      | Email       | contact@test.com |
      | PhoneNumber | +989123456789    |
    Then the command should succeed
    And the contact info should be created
    And the email should be validated
    And the phone number should be validated

  @command @registration @step3 @location @authorization @unauthenticated
  Scenario: SaveStep3LocationCommand - Requires authentication
    Given I am not authenticated
    When I execute SaveStep3LocationCommand
    Then the command should fail with UnauthorizedAccessException
    And the error message should contain "User not authenticated"

  # ==================== SaveStep4ServicesCommandHandler ====================

  @command @registration @step4 @services @happy-path
  Scenario: SaveStep4ServicesCommand - Add services to draft provider
    Given I have a draft provider at step 3
    When I execute SaveStep4ServicesCommand with services:
      | ServiceName    | Category      | Price  | Duration |
      | Haircut        | Hair Services | 50.00  | 30       |
      | Hair Coloring  | Hair Services | 100.00 | 90       |
      | Manicure       | Nail Services | 30.00  | 45       |
    Then the command should succeed
    And 3 services should be created
    And the registration step should be 4
    And the services should belong to the provider

  @command @registration @step4 @services @validation @provider-not-found
  Scenario: SaveStep4ServicesCommand - Provider draft not found
    When I execute SaveStep4ServicesCommand with non-existent provider
    Then the command should fail with InvalidOperationException
    And the error message should contain "Provider not found"

  @command @registration @step4 @services @validation @empty-services
  Scenario: SaveStep4ServicesCommand - At least one service required
    Given I have a draft provider at step 3
    When I execute SaveStep4ServicesCommand with no services
    Then the command should fail with ValidationException
    And the error message should contain "at least one service"

  @command @registration @step4 @services @authorization @wrong-owner
  Scenario: SaveStep4ServicesCommand - Can only add services to own draft
    Given another user has a draft provider
    When I try to add services to their draft
    Then the command should fail with UnauthorizedAccessException

  # ==================== SaveStep5StaffCommandHandler ====================

  @command @registration @step5 @staff @happy-path
  Scenario: SaveStep5StaffCommand - Add staff to draft provider
    Given I have a draft provider at step 4
    When I execute SaveStep5StaffCommand with staff:
      | Name        | Email              | Role      |
      | John Smith  | john@salon.com     | Stylist   |
      | Jane Doe    | jane@salon.com     | Therapist |
    Then the command should succeed
    And 2 staff members should be added
    And the registration step should be 5
    And the staff should belong to the provider

  @command @registration @step5 @staff @validation @provider-not-found
  Scenario: SaveStep5StaffCommand - Provider draft not found
    When I execute SaveStep5StaffCommand with non-existent provider
    Then the command should fail with InvalidOperationException

  @command @registration @step5 @staff @validation @duplicate-email
  Scenario: SaveStep5StaffCommand - Cannot add staff with duplicate email
    Given I have a draft provider with staff "john@salon.com"
    When I execute SaveStep5StaffCommand to add another staff with "john@salon.com"
    Then the command should fail with ConflictException
    And the error message should contain "email already exists"

  @command @registration @step5 @staff @authorization
  Scenario: SaveStep5StaffCommand - Can only add staff to own draft
    Given another user has a draft provider
    When I try to add staff to their draft
    Then the command should fail with UnauthorizedAccessException

  @command @registration @step5 @staff @optional
  Scenario: SaveStep5StaffCommand - Staff is optional for solo providers
    Given I have a draft provider at step 4
    When I execute SaveStep5StaffCommand with no staff
    Then the command should succeed
    And the registration step should be 5
    And I should be the only staff member

  # ==================== SaveStep6WorkingHoursCommandHandler ====================

  @command @registration @step6 @hours @happy-path
  Scenario: SaveStep6WorkingHoursCommand - Set business hours
    Given I have a draft provider at step 5
    When I execute SaveStep6WorkingHoursCommand with:
      | DayOfWeek | OpenTime | CloseTime | IsOpen |
      | Monday    | 09:00    | 18:00     | true   |
      | Tuesday   | 09:00    | 18:00     | true   |
      | Wednesday | 09:00    | 18:00     | true   |
      | Thursday  | 09:00    | 18:00     | true   |
      | Friday    | 09:00    | 20:00     | true   |
      | Saturday  | 10:00    | 16:00     | true   |
      | Sunday    | 00:00    | 00:00     | false  |
    Then the command should succeed
    And business hours should be set for all 7 days
    And the registration step should be 6

  @command @registration @step6 @hours @validation @provider-not-found
  Scenario: SaveStep6WorkingHoursCommand - Provider draft not found
    When I execute SaveStep6WorkingHoursCommand with non-existent provider
    Then the command should fail with InvalidOperationException

  @command @registration @step6 @hours @validation @invalid-time
  Scenario: SaveStep6WorkingHoursCommand - Close time before open time
    Given I have a draft provider at step 5
    When I execute SaveStep6WorkingHoursCommand with:
      | DayOfWeek | OpenTime | CloseTime |
      | Monday    | 18:00    | 09:00     |
    Then the command should fail with ValidationException
    And the error message should contain "close time"

  @command @registration @step6 @hours @business-logic @closed-days
  Scenario: SaveStep6WorkingHoursCommand - Mark days as closed
    Given I have a draft provider at step 5
    When I execute SaveStep6WorkingHoursCommand with:
      | DayOfWeek | IsOpen |
      | Sunday    | false  |
    Then the command should succeed
    And Sunday should be marked as closed

  @command @registration @step6 @hours @authorization
  Scenario: SaveStep6WorkingHoursCommand - Can only set hours for own draft
    Given another user has a draft provider
    When I try to set hours for their draft
    Then the command should fail with UnauthorizedAccessException

  # ==================== SaveStep7GalleryCommandHandler ====================

  @command @registration @step7 @gallery @happy-path
  Scenario: SaveStep7GalleryCommand - Upload gallery images
    Given I have a draft provider at step 6
    When I execute SaveStep7GalleryCommand with 5 images
    Then the command should succeed
    And 5 gallery images should be uploaded
    And the registration step should be 7
    And images should be ordered by display order

  @command @registration @step7 @gallery @validation @provider-not-found
  Scenario: SaveStep7GalleryCommand - Provider draft not found
    When I execute SaveStep7GalleryCommand with non-existent provider
    Then the command should fail with InvalidOperationException

  @command @registration @step7 @gallery @validation @max-images
  Scenario: SaveStep7GalleryCommand - Cannot exceed maximum images
    Given I have a draft provider at step 6
    When I execute SaveStep7GalleryCommand with 15 images
    Then the command should fail with ValidationException
    And the error message should contain "maximum"

  @command @registration @step7 @gallery @validation @file-size
  Scenario: SaveStep7GalleryCommand - Image file size limit
    Given I have a draft provider at step 6
    When I execute SaveStep7GalleryCommand with image larger than 10MB
    Then the command should fail with ValidationException
    And the error message should contain "file size"

  @command @registration @step7 @gallery @validation @file-format
  Scenario: SaveStep7GalleryCommand - Only image formats allowed
    Given I have a draft provider at step 6
    When I execute SaveStep7GalleryCommand with non-image file
    Then the command should fail with ValidationException
    And the error message should contain "format"

  @command @registration @step7 @gallery @optional
  Scenario: SaveStep7GalleryCommand - Gallery is optional
    Given I have a draft provider at step 6
    When I execute SaveStep7GalleryCommand with no images
    Then the command should succeed
    And the registration step should be 7

  @command @registration @step7 @gallery @authorization
  Scenario: SaveStep7GalleryCommand - Can only upload to own draft
    Given another user has a draft provider
    When I try to upload images to their draft
    Then the command should fail with UnauthorizedAccessException

  # ==================== SaveStep8FeedbackCommandHandler ====================

  @command @registration @step8 @feedback @happy-path
  Scenario: SaveStep8FeedbackCommand - Provide registration feedback
    Given I have a draft provider at step 7
    When I execute SaveStep8FeedbackCommand with:
      | Field      | Value                              |
      | Rating     | 5                                  |
      | Comments   | Great registration process!        |
      | Source     | Google Search                      |
    Then the command should succeed
    And the registration step should be 8
    And the feedback should be saved

  @command @registration @step8 @feedback @validation @provider-not-found
  Scenario: SaveStep8FeedbackCommand - Provider draft not found
    When I execute SaveStep8FeedbackCommand with non-existent provider
    Then the command should fail with InvalidOperationException

  @command @registration @step8 @feedback @optional
  Scenario: SaveStep8FeedbackCommand - Feedback is optional
    Given I have a draft provider at step 7
    When I execute SaveStep8FeedbackCommand with no feedback
    Then the command should succeed
    And the registration step should be 8

  @command @registration @step8 @feedback @authorization
  Scenario: SaveStep8FeedbackCommand - Can only provide feedback for own draft
    Given another user has a draft provider
    When I try to provide feedback for their draft
    Then the command should fail with UnauthorizedAccessException

  # ==================== SaveStep9CompleteCommandHandler ====================

  @command @registration @step9 @complete @happy-path
  Scenario: SaveStep9CompleteCommand - Complete registration successfully
    Given I have a draft provider at step 8
    And the provider has business hours configured
    And the provider has at least one service
    And the provider has business name
    And the provider has address
    And the provider has contact email
    When I execute SaveStep9CompleteCommand
    Then the command should succeed
    And the provider status should transition to "PendingVerification"
    And the registration step should be 9
    And ProviderRegistrationCompletedEvent should be published

  @command @registration @step9 @complete @validation @provider-not-found
  Scenario: SaveStep9CompleteCommand - Provider draft not found
    When I execute SaveStep9CompleteCommand with non-existent provider
    Then the command should fail with InvalidOperationException
    And the error message should contain "Provider not found"

  @command @registration @step9 @complete @authorization @wrong-owner
  Scenario: SaveStep9CompleteCommand - Can only complete own registration
    Given another user has a draft provider
    When I try to complete their registration
    Then the command should fail with UnauthorizedAccessException
    And the error message should contain "not authorized"

  @command @registration @step9 @complete @validation @wrong-status
  Scenario: SaveStep9CompleteCommand - Provider must be in draft status
    Given I have a provider with status "PendingVerification"
    When I execute SaveStep9CompleteCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "not in draft status"

  @command @registration @step9 @complete @validation @missing-hours
  Scenario: SaveStep9CompleteCommand - Business hours required
    Given I have a draft provider at step 8
    And the provider has no business hours
    When I execute SaveStep9CompleteCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "Business hours are required"

  @command @registration @step9 @complete @validation @missing-services
  Scenario: SaveStep9CompleteCommand - At least one service required
    Given I have a draft provider at step 8
    And the provider has no services
    When I execute SaveStep9CompleteCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "At least one service is required"

  @command @registration @step9 @complete @validation @missing-name
  Scenario: SaveStep9CompleteCommand - Business name required
    Given I have a draft provider at step 8
    And the provider has no business name
    When I execute SaveStep9CompleteCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "Business name is required"

  @command @registration @step9 @complete @validation @missing-address
  Scenario: SaveStep9CompleteCommand - Business address required
    Given I have a draft provider at step 8
    And the provider has no address
    When I execute SaveStep9CompleteCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "Business address is required"

  @command @registration @step9 @complete @validation @missing-email
  Scenario: SaveStep9CompleteCommand - Contact email required
    Given I have a draft provider at step 8
    And the provider has no contact email
    When I execute SaveStep9CompleteCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "Contact email is required"

  @command @registration @step9 @complete @business-logic @status-transition
  Scenario: SaveStep9CompleteCommand - Status transitions to PendingVerification
    Given I have a complete draft provider
    When I execute SaveStep9CompleteCommand
    Then the provider should transition from "Drafted" to "PendingVerification"
    And the provider should await admin approval

  @command @registration @step9 @complete @authorization @unauthenticated
  Scenario: SaveStep9CompleteCommand - Requires authentication
    Given I am not authenticated
    When I execute SaveStep9CompleteCommand
    Then the command should fail with UnauthorizedAccessException

  # ==================== Integration Tests ====================

  @command @registration @flow @complete-journey
  Scenario: Complete provider registration flow from step 3 to 9
    Given I am a new user wanting to become a provider
    When I complete step 3 with location information
    And I complete step 4 with services
    And I complete step 5 with staff information
    And I complete step 6 with business hours
    And I complete step 7 with gallery images
    And I complete step 8 with feedback
    And I complete step 9 to finalize
    Then my provider registration should be complete
    And the status should be "PendingVerification"
    And all steps should be saved correctly

  @command @registration @flow @resume-draft
  Scenario: Resume registration from saved draft
    Given I started registration and saved draft at step 3
    And I log out
    When I log back in
    And I resume my registration
    Then I should be at step 3
    And my previous data should be preserved
    And I can continue to step 4

  @command @registration @flow @update-earlier-step
  Scenario: Update information from earlier step
    Given I have completed registration up to step 7
    When I go back to step 3 and update location
    And I continue to step 9
    Then the updated location should be saved
    And the registration should complete successfully
