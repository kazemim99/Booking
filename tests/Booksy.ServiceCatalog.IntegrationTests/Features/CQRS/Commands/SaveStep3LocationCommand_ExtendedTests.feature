Feature: SaveStep3LocationCommand - Extended Validation Tests
  As a system
  I want to validate all edge cases for SaveStep3LocationCommand
  So that invalid data is caught before provider creation

  Background:
    Given I am authenticated as a new user

  # ==================== Validation Tests ====================

  @command @registration @step3 @validation @empty-business-name
  Scenario: SaveStep3LocationCommand - Empty business name
    When I execute SaveStep3LocationCommand with:
      | Field        | Value                 |
      | BusinessName |                       |
      | Category     | BeautyAndWellness     |
      | Email        | test@test.com         |
      | PhoneNumber  | +989123456789         |
      | AddressLine1 | 123 Main St           |
      | City         | Tehran                |
      | Province     | Tehran                |
      | PostalCode   | 1234567890            |
      | Latitude     | 35.6892               |
      | Longitude    | 51.3890               |
    Then the command should fail with ValidationException
    And the error message should contain "business name"

  @command @registration @step3 @validation @whitespace-business-name
  Scenario: SaveStep3LocationCommand - Whitespace only business name
    When I execute SaveStep3LocationCommand with BusinessName "   "
    Then the command should fail with ValidationException

  @command @registration @step3 @validation @invalid-email
  Scenario: SaveStep3LocationCommand - Invalid email format
    When I execute SaveStep3LocationCommand with:
      | Field   | Value            |
      | Email   | not-an-email     |
    Then the command should fail with ValidationException
    And the error message should contain "email"

  @command @registration @step3 @validation @invalid-phone
  Scenario: SaveStep3LocationCommand - Invalid phone number format
    When I execute SaveStep3LocationCommand with:
      | Field       | Value     |
      | PhoneNumber | 123       |
    Then the command should fail with ValidationException
    And the error message should contain "phone"

  @command @registration @step3 @validation @invalid-latitude
  Scenario: SaveStep3LocationCommand - Latitude out of range
    When I execute SaveStep3LocationCommand with:
      | Field    | Value    |
      | Latitude | 100.0    |
    Then the command should fail with ValidationException
    And the error message should contain "latitude"

  @command @registration @step3 @validation @invalid-longitude
  Scenario: SaveStep3LocationCommand - Longitude out of range
    When I execute SaveStep3LocationCommand with:
      | Field     | Value    |
      | Longitude | 200.0    |
    Then the command should fail with ValidationException
    And the error message should contain "longitude"

  @command @registration @step3 @validation @negative-latitude
  Scenario: SaveStep3LocationCommand - Negative latitude (valid for southern hemisphere)
    When I execute SaveStep3LocationCommand with:
      | Field    | Value   |
      | Latitude | -35.6892|
    Then the command should succeed
    And the latitude should be -35.6892

  @command @registration @step3 @validation @empty-postal-code
  Scenario: SaveStep3LocationCommand - Empty postal code
    When I execute SaveStep3LocationCommand with:
      | Field      | Value |
      | PostalCode |       |
    Then the command should fail with ValidationException
    And the error message should contain "postal code"

  @command @registration @step3 @validation @invalid-postal-format
  Scenario: SaveStep3LocationCommand - Invalid Iranian postal code format
    When I execute SaveStep3LocationCommand with:
      | Field      | Value    |
      | PostalCode | ABC-123  |
    Then the command should fail with ValidationException
    And the error message should contain "postal code"

  @command @registration @step3 @validation @empty-city
  Scenario: SaveStep3LocationCommand - Empty city
    When I execute SaveStep3LocationCommand with:
      | Field | Value |
      | City  |       |
    Then the command should fail with ValidationException
    And the error message should contain "city"

  @command @registration @step3 @validation @empty-province
  Scenario: SaveStep3LocationCommand - Empty province
    When I execute SaveStep3LocationCommand with:
      | Field    | Value |
      | Province |       |
    Then the command should fail with ValidationException
    And the error message should contain "province"

  @command @registration @step3 @validation @empty-address-line1
  Scenario: SaveStep3LocationCommand - Empty address line 1
    When I execute SaveStep3LocationCommand with:
      | Field        | Value |
      | AddressLine1 |       |
    Then the command should fail with ValidationException
    And the error message should contain "address"

  # ==================== Business Logic Tests ====================

  @command @registration @step3 @business-logic @address-formatting-no-line2
  Scenario: SaveStep3LocationCommand - Address with only line 1
    When I execute SaveStep3LocationCommand with:
      | Field        | Value       |
      | AddressLine1 | 123 Main St |
      | AddressLine2 |             |
    Then the command should succeed
    And the street should be "123 Main St"
    And the formatted address should be "123 Main St, Tehran, Tehran"

  @command @registration @step3 @business-logic @address-formatting-with-line2
  Scenario: SaveStep3LocationCommand - Address with both lines
    When I execute SaveStep3LocationCommand with:
      | Field        | Value       |
      | AddressLine1 | 123 Main St |
      | AddressLine2 | Suite 100   |
    Then the command should succeed
    And the street should be "123 Main St, Suite 100"
    And the formatted address should be "123 Main St, Suite 100, Tehran, Tehran"

  @command @registration @step3 @business-logic @country-code
  Scenario: SaveStep3LocationCommand - Country code is set to IR
    When I execute SaveStep3LocationCommand with valid data
    Then the command should succeed
    And the business address country should be "IR"

  @command @registration @step3 @business-logic @coordinates
  Scenario: SaveStep3LocationCommand - GPS coordinates are stored correctly
    When I execute SaveStep3LocationCommand with:
      | Field     | Value    |
      | Latitude  | 35.6892  |
      | Longitude | 51.3890  |
    Then the command should succeed
    And the provider location should have coordinates (35.6892, 51.3890)

  @command @registration @step3 @business-logic @category-case-insensitive
  Scenario: SaveStep3LocationCommand - Category parsing is case insensitive
    When I execute SaveStep3LocationCommand with:
      | Field    | Value             |
      | Category | beautYandWELLness |
    Then the command should succeed
    And the provider type should be "BeautyAndWellness"

  @command @registration @step3 @business-logic @profile-image-placeholder
  Scenario: SaveStep3LocationCommand - Profile image placeholder is set
    Given I have a draft provider from previous steps
    When I execute SaveStep3LocationCommand with updated location
    Then the command should succeed
    And the profile image URL should be set

  # ==================== Idempotency Tests ====================

  @command @registration @step3 @idempotency @same-user-different-data
  Scenario: SaveStep3LocationCommand - User updates draft with different business name
    Given I have a draft provider with business name "Original Salon"
    When I execute SaveStep3LocationCommand with business name "Updated Salon"
    Then the command should succeed
    And the business name should be "Updated Salon"
    And no duplicate provider should be created
    And the provider ID should remain the same

  @command @registration @step3 @idempotency @same-user-different-location
  Scenario: SaveStep3LocationCommand - User updates draft with different address
    Given I have a draft provider at address "123 Main St"
    When I execute SaveStep3LocationCommand with address "456 Oak Ave"
    Then the command should succeed
    And the address should be "456 Oak Ave"
    And the registration step should remain 3

  @command @registration @step3 @idempotency @same-user-different-contact
  Scenario: SaveStep3LocationCommand - User updates draft with different email
    Given I have a draft provider with email "old@test.com"
    When I execute SaveStep3LocationCommand with email "new@test.com"
    Then the command should succeed
    And the contact email should be "new@test.com"

  @command @registration @step3 @idempotency @idempotency-key
  Scenario: SaveStep3LocationCommand - Use idempotency key to prevent duplicates
    Given I execute SaveStep3LocationCommand with idempotency key "abc-123"
    When I execute SaveStep3LocationCommand again with same idempotency key "abc-123"
    Then the command should succeed
    And no duplicate provider should be created
    And the same provider ID should be returned

  # ==================== Provider Type Validation ====================

  @command @registration @step3 @provider-type @beauty-wellness
  Scenario: SaveStep3LocationCommand - Beauty and wellness provider type
    When I execute SaveStep3LocationCommand with category "BeautyAndWellness"
    Then the command should succeed
    And the provider type should be "BeautyAndWellness"

  @command @registration @step3 @provider-type @health-medical
  Scenario: SaveStep3LocationCommand - Health and medical provider type
    When I execute SaveStep3LocationCommand with category "HealthAndMedical"
    Then the command should succeed
    And the provider type should be "HealthAndMedical"

  @command @registration @step3 @provider-type @home-services
  Scenario: SaveStep3LocationCommand - Home services provider type
    When I execute SaveStep3LocationCommand with category "HomeServices"
    Then the command should succeed
    And the provider type should be "HomeServices"

  @command @registration @step3 @provider-type @invalid-type
  Scenario: SaveStep3LocationCommand - Invalid provider type
    When I execute SaveStep3LocationCommand with category "InvalidType"
    Then the command should fail with InvalidOperationException
    And the error message should contain "Invalid category: InvalidType"

  # ==================== Error Message Quality ====================

  @command @registration @step3 @error-messages @descriptive-error
  Scenario: SaveStep3LocationCommand - Error messages are descriptive
    When I execute SaveStep3LocationCommand with invalid category "XYZ"
    Then the command should fail
    And the error message should contain the invalid value "XYZ"
    And the error message should guide the user

  @command @registration @step3 @error-messages @multiple-validation-errors
  Scenario: SaveStep3LocationCommand - Multiple validation errors reported
    When I execute SaveStep3LocationCommand with:
      | Field        | Value           |
      | BusinessName |                 |
      | Email        | invalid-email   |
      | PhoneNumber  | 123             |
    Then the command should fail with ValidationException
    And the error message should contain all validation failures

  # ==================== Integration with Value Objects ====================

  @command @registration @step3 @value-objects @email-validation
  Scenario: SaveStep3LocationCommand - Email value object validates format
    When I execute SaveStep3LocationCommand with email "test@example.com"
    Then the Email value object should be created successfully
    And the email should be stored in normalized format

  @command @registration @step3 @value-objects @phone-validation
  Scenario: SaveStep3LocationCommand - PhoneNumber value object validates format
    When I execute SaveStep3LocationCommand with phone "+989123456789"
    Then the PhoneNumber value object should be created successfully
    And the phone should be stored in normalized format

  @command @registration @step3 @value-objects @business-address-validation
  Scenario: SaveStep3LocationCommand - BusinessAddress value object created correctly
    When I execute SaveStep3LocationCommand with complete address
    Then the BusinessAddress value object should be created
    And all address components should be set
    And coordinates should be within valid ranges

  # ==================== Registration Step Tracking ====================

  @command @registration @step3 @tracking @first-step
  Scenario: SaveStep3LocationCommand - Sets registration step to 3 for new draft
    When I execute SaveStep3LocationCommand for the first time
    Then the command should succeed
    And the registration step should be 3
    And the provider status should be "Drafted"

  @command @registration @step3 @tracking @update-step
  Scenario: SaveStep3LocationCommand - Updates registration step to 3 for existing draft
    Given I have a draft provider at step 2
    When I execute SaveStep3LocationCommand
    Then the command should succeed
    And the registration step should be updated to 3

  @command @registration @step3 @tracking @backward-step
  Scenario: SaveStep3LocationCommand - Can update step 3 even if user was at step 5
    Given I have a draft provider at step 5
    When I execute SaveStep3LocationCommand with updated data
    Then the command should succeed
    And the registration step should remain at least 3
    And the user can continue to later steps
