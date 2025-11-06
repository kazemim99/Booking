Feature: Provider Command Handlers - Complete Coverage
  As a system
  I want to test all critical provider command handlers
  So that every code path is validated

  # ==================== RegisterProviderCommandHandler ====================

  @command @provider @register @happy-path
  Scenario: RegisterProviderCommand - Successful registration
    Given I am authenticated as a customer
    When I execute RegisterProviderCommand with valid data:
      | Field        | Value              |
      | BusinessName | Beautiful Salon    |
      | Type         | Salon              |
      | Email        | salon@test.com     |
      | Phone        | +989121234567      |
      | City         | Tehran             |
    Then the command should succeed
    And a Provider aggregate should be created
    And ProviderRegisteredEvent should be published
    And the provider status should be "PendingVerification"
    And the provider should be owned by current user

  @command @provider @register @validation @duplicate-name
  Scenario: RegisterProviderCommand - Duplicate business name in same city
    Given a provider "Existing Salon" exists in "Tehran"
    And I am authenticated as a customer
    When I execute RegisterProviderCommand with business name "Existing Salon" in "Tehran"
    Then the command should fail with ConflictException
    And the error should indicate duplicate business name

  @command @provider @register @validation @empty-name
  Scenario: RegisterProviderCommand - Empty business name
    Given I am authenticated as a customer
    When I execute RegisterProviderCommand with empty business name
    Then the command should fail with ValidationException
    And the error should indicate business name is required

  @command @provider @register @validation @name-length
  Scenario: RegisterProviderCommand - Business name length validation
    Given I am authenticated as a customer
    When I execute RegisterProviderCommand with business name of 1 character
    Then the command should fail with ValidationException
    And the error should indicate business name must be 2-100 characters

  @command @provider @register @validation @invalid-phone
  Scenario: RegisterProviderCommand - Invalid phone format
    Given I am authenticated as a customer
    When I execute RegisterProviderCommand with invalid phone "12345"
    Then the command should fail with ValidationException
    And the error should indicate invalid phone format

  @command @provider @register @validation @invalid-email
  Scenario: RegisterProviderCommand - Invalid email format
    Given I am authenticated as a customer
    When I execute RegisterProviderCommand with invalid email "notanemail"
    Then the command should fail with ValidationException

  # ==================== ActivateProviderCommandHandler ====================

  @command @provider @activate @happy-path
  Scenario: ActivateProviderCommand - Admin activates pending provider
    Given I am authenticated as an admin
    And a provider exists with status "PendingVerification"
    When I execute ActivateProviderCommand
    Then the command should succeed
    And ProviderActivatedEvent should be published
    And the provider status should be "Active"
    And the provider should be notified of activation

  @command @provider @activate @validation @not-found
  Scenario: ActivateProviderCommand - Provider not found
    Given I am authenticated as an admin
    When I execute ActivateProviderCommand with non-existent provider ID
    Then the command should fail with NotFoundException

  @command @provider @activate @idempotency
  Scenario: ActivateProviderCommand - Activating already active provider
    Given I am authenticated as an admin
    And a provider exists with status "Active"
    When I execute ActivateProviderCommand
    Then the command should succeed
    And no ProviderActivatedEvent should be published
    And the status should remain "Active"

  @command @provider @activate @authorization
  Scenario: ActivateProviderCommand - Only admin can activate
    Given I am authenticated as a customer
    And a provider exists
    When I execute ActivateProviderCommand
    Then the command should fail with ForbiddenException

  # ==================== AddStaffCommandHandler ====================

  @command @provider @staff @add @happy-path
  Scenario: AddStaffCommand - Add staff member successfully
    Given I am authenticated as the provider
    When I execute AddStaffCommand with:
      | Field     | Value           |
      | FirstName | John            |
      | LastName  | Doe             |
      | Email     | john@test.com   |
      | Phone     | +1234567890     |
      | Role      | ServiceProvider |
    Then the command should succeed
    And StaffAddedEvent should be published
    And the staff member should be added to provider
    And the staff should have an ID assigned

  @command @provider @staff @add @validation @duplicate-email
  Scenario: AddStaffCommand - Duplicate email validation
    Given I am authenticated as the provider
    And the provider has staff with email "existing@test.com"
    When I execute AddStaffCommand with email "existing@test.com"
    Then the command should fail with ConflictException
    And the error should indicate duplicate email

  @command @provider @staff @add @validation @empty-name
  Scenario: AddStaffCommand - First name required
    Given I am authenticated as the provider
    When I execute AddStaffCommand with empty first name
    Then the command should fail with ValidationException

  @command @provider @staff @add @validation @invalid-role
  Scenario: AddStaffCommand - Invalid role validation
    Given I am authenticated as the provider
    When I execute AddStaffCommand with invalid role "InvalidRole"
    Then the command should fail with ValidationException

  @command @provider @staff @add @authorization
  Scenario: AddStaffCommand - Only provider owner can add staff
    Given I am authenticated as a different provider
    When I execute AddStaffCommand for another provider
    Then the command should fail with ForbiddenException

  # ==================== UpdateBusinessHoursCommandHandler ====================

  @command @provider @hours @update @happy-path
  Scenario: UpdateBusinessHoursCommand - Update business hours
    Given I am authenticated as the provider
    When I execute UpdateBusinessHoursCommand with:
      | Day    | IsOpen | OpenTime | CloseTime |
      | Monday | true   | 09:00    | 18:00     |
      | Sunday | false  |          |           |
    Then the command should succeed
    And BusinessHoursUpdatedEvent should be published
    And the business hours should be updated

  @command @provider @hours @update @validation @invalid-time-range
  Scenario: UpdateBusinessHoursCommand - Close time before open time
    Given I am authenticated as the provider
    When I execute UpdateBusinessHoursCommand with open 18:00 and close 09:00
    Then the command should fail with ValidationException
    And the error should indicate invalid time range

  @command @provider @hours @update @validation @overlapping-breaks
  Scenario: UpdateBusinessHoursCommand - Overlapping break times
    Given I am authenticated as the provider
    When I execute UpdateBusinessHoursCommand with overlapping breaks:
      | BreakStart | BreakEnd |
      | 12:00      | 13:00    |
      | 12:30      | 13:30    |
    Then the command should fail with ValidationException

  # ==================== UploadGalleryImagesCommandHandler ====================

  @command @provider @gallery @upload @happy-path
  Scenario: UploadGalleryImagesCommand - Upload images successfully
    Given I am authenticated as the provider
    When I execute UploadGalleryImagesCommand with 3 valid images
    Then the command should succeed
    And 3 GalleryImageUploadedEvents should be published
    And the images should be stored with thumbnails
    And display order should be assigned

  @command @provider @gallery @upload @validation @max-count
  Scenario: UploadGalleryImagesCommand - Exceeds maximum images
    Given I am authenticated as the provider
    When I execute UploadGalleryImagesCommand with 11 images
    Then the command should fail with ValidationException
    And the error should indicate maximum 10 images allowed

  @command @provider @gallery @upload @validation @file-size
  Scenario: UploadGalleryImagesCommand - File size exceeds limit
    Given I am authenticated as the provider
    When I execute UploadGalleryImagesCommand with 12MB image
    Then the command should fail with ValidationException
    And the error should indicate file size exceeded

  @command @provider @gallery @upload @validation @invalid-format
  Scenario: UploadGalleryImagesCommand - Invalid file format
    Given I am authenticated as the provider
    When I execute UploadGalleryImagesCommand with PDF file
    Then the command should fail with ValidationException
    And the error should indicate invalid file type

  # ==================== CreateProviderDraftCommandHandler ====================

  @command @provider @draft @create @happy-path
  Scenario: CreateProviderDraftCommand - Create draft provider
    Given I am authenticated as a customer
    When I execute CreateProviderDraftCommand with basic info
    Then the command should succeed
    And a draft provider should be created
    And the status should be "Drafted"
    And the registration step should be 3

  @command @provider @draft @create @existing-draft
  Scenario: CreateProviderDraftCommand - Return existing draft
    Given I am authenticated as a customer
    And I already have a draft provider
    When I execute CreateProviderDraftCommand
    Then the command should succeed
    And the existing draft should be returned
    And no new provider should be created

  # ==================== CompleteProviderRegistrationCommandHandler ====================

  @command @provider @registration @complete @happy-path
  Scenario: CompleteProviderRegistrationCommand - Complete registration
    Given I am authenticated as a customer
    And I have a draft provider
    And the draft has services added
    And the draft has staff added
    And the draft has working hours configured
    When I execute CompleteProviderRegistrationCommand
    Then the command should succeed
    And the provider status should change to "PendingVerification"
    And ProviderRegisteredEvent should be published

  @command @provider @registration @complete @validation @missing-requirements
  Scenario: CompleteProviderRegistrationCommand - Missing required data
    Given I am authenticated as a customer
    And I have a draft provider without services
    When I execute CompleteProviderRegistrationCommand
    Then the command should fail with ValidationException
    And the error should indicate missing required data

  @command @provider @registration @complete @validation @not-draft
  Scenario: CompleteProviderRegistrationCommand - Provider not in draft status
    Given I am authenticated as a customer
    And I have an active provider
    When I execute CompleteProviderRegistrationCommand
    Then the command should fail with ConflictException
