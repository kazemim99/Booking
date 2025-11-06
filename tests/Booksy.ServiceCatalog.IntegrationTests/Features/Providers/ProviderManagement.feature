Feature: Provider Management
  As a provider and admin
  I want to manage provider profiles and settings
  So that provider information is accurate and up-to-date

  Background:
    Given a provider "Test Salon" exists with active status

  @smoke @provider @profile @view
  Scenario: View provider profile
    When I send a GET request to "/api/v1/providers/{providerId}"
    Then the response status code should be 200
    And the response should contain complete provider information

  @provider @profile @update
  Scenario: Provider updates their own profile
    Given I am authenticated as the provider
    When I send a PUT request to update provider profile with:
      | Field           | Value                    |
      | FullName        | John Doe                 |
      | Email           | newemail@test.com        |
      | ProfileImageUrl | /uploads/profile.png     |
    Then the response status code should be 204
    And the provider profile should be updated in the database

  @provider @business @update
  Scenario: Provider updates business information
    Given I am authenticated as the provider
    When I send a PUT request to update business info with:
      | Field          | Value                     |
      | BusinessName   | Updated Salon Name        |
      | Description    | New description           |
      | PhoneNumber    | +989123456789             |
      | Website        | https://newsalon.com      |
    Then the response status code should be 204
    And the business information should be updated

  @provider @image @upload @profile
  Scenario: Provider uploads profile image
    Given I am authenticated as the provider
    When I upload a valid profile image
    Then the response status code should be 200
    And the response should contain the image URL
    And the image should be accessible at the URL

  @provider @image @upload @logo
  Scenario: Provider uploads business logo
    Given I am authenticated as the provider
    When I upload a valid business logo
    Then the response status code should be 200
    And the logo should be stored correctly

  @provider @image @upload @negative @size
  Scenario: Cannot upload image exceeding size limit
    Given I am authenticated as the provider
    When I upload an image larger than 10MB
    Then the response status code should be 400
    And the error should indicate file size exceeded

  @provider @image @upload @negative @format
  Scenario: Cannot upload invalid image format
    Given I am authenticated as the provider
    When I upload a file with unsupported format
    Then the response status code should be 400
    And the error should indicate invalid file type

  @provider @search
  Scenario: Search providers by business name
    Given there are 10 providers in the system
    When I send a GET request to "/api/v1/providers/search?searchTerm=Beauty"
    Then the response status code should be 200
    And the results should contain only providers matching "Beauty"

  @provider @search @pagination
  Scenario: Search with pagination
    Given there are 15 providers in the system
    When I send a GET request to "/api/v1/providers/search?pageNumber=1&pageSize=5"
    Then the response status code should be 200
    And the response should contain exactly 5 providers
    And pagination metadata should show correct total count

  @provider @location
  Scenario: Find providers by location
    Given providers exist in various locations
    When I send a GET request to find providers near latitude 35.6892, longitude 51.3890 within 10km
    Then the response status code should be 200
    And all returned providers should be within the specified radius

  @provider @status @activate @admin
  Scenario: Admin activates a provider
    Given I am authenticated as an admin
    And the provider status is "PendingVerification"
    When I send a POST request to activate the provider
    Then the response status code should be 200
    And the provider status should be "Active" in the database

  @provider @status @deactivate
  Scenario: Admin deactivates a provider
    Given I am authenticated as an admin
    When I send a POST request to deactivate the provider with reason "Policy violation"
    Then the response status code should be 200
    And the provider status should be "Inactive"

  @provider @status @view
  Scenario: Provider views their own status
    Given I am authenticated as the provider
    When I send a GET request to "/api/v1/providers/current/status"
    Then the response status code should be 200
    And the response should contain current status and details

  @provider @status @bystatus @admin
  Scenario: Admin views providers by status
    Given I am authenticated as an admin
    And there are providers with various statuses
    When I send a GET request to "/api/v1/providers/by-status/Active"
    Then the response status code should be 200
    And all returned providers should have status "Active"

  @provider @negative @unauthorized @update
  Scenario: Provider cannot update another provider's profile
    Given another provider "Competitor" exists
    And I am authenticated as the first provider
    When I send a PUT request to update the competitor's profile
    Then the response status code should be 403

  @provider @negative @invalid-phone
  Scenario: Cannot update with invalid phone number
    Given I am authenticated as the provider
    When I send a PUT request to update business info with invalid phone "invalid"
    Then the response status code should be 400

  @provider @settings
  Scenario: Provider updates their settings
    Given I am authenticated as the provider
    When I update provider settings:
      | Setting                 | Value |
      | AllowOnlineBooking      | true  |
      | RequireBookingApproval  | false |
      | BookingLeadTime         | 24    |
    Then the response status code should be 204
    And the settings should be persisted
