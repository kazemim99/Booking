Feature: GetRegistrationProgressQuery - Complete Coverage
  As a system
  I want to test GetRegistrationProgressQuery
  So that users can resume their provider registration from where they left off

  # ==================== No Draft Scenarios ====================

  @query @registration @progress @no-draft
  Scenario: GetRegistrationProgressQuery - No draft exists
    Given I am authenticated as a new user
    And I have not started provider registration
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And HasDraft should be false
    And CurrentStep should be null
    And DraftData should be null

  @query @registration @progress @unauthenticated
  Scenario: GetRegistrationProgressQuery - User not authenticated
    Given I am not authenticated
    When I execute GetRegistrationProgressQuery
    Then the command should fail with UnauthorizedAccessException
    And the error message should contain "User not authenticated"

  # ==================== Draft at Different Steps ====================

  @query @registration @progress @step3
  Scenario: GetRegistrationProgressQuery - Draft at step 3 (location only)
    Given I am authenticated as a new user
    And I have completed step 3 with:
      | Field                 | Value                   |
      | BusinessName          | Luxury Salon            |
      | BusinessDescription   | Premium services        |
      | Category              | BeautyAndWellness       |
      | Email                 | salon@test.com          |
      | PhoneNumber           | +989123456789           |
      | AddressLine1          | 123 Main St             |
      | City                  | Tehran                  |
      | Province              | Tehran                  |
      | PostalCode            | 1234567890              |
      | Latitude              | 35.6892                 |
      | Longitude             | 51.3890                 |
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And HasDraft should be true
    And CurrentStep should be 3
    And DraftData should contain business info
    And DraftData should contain location
    And DraftData should have empty services list
    And DraftData should have empty staff list
    And DraftData should have empty business hours
    And DraftData should have empty gallery images

  @query @registration @progress @step4
  Scenario: GetRegistrationProgressQuery - Draft at step 4 (with services)
    Given I am authenticated as a new user
    And I have completed step 3 with location
    And I have completed step 4 with 3 services:
      | ServiceName   | Price  | Duration |
      | Haircut       | 50.00  | 30       |
      | Hair Coloring | 100.00 | 90       |
      | Manicure      | 30.00  | 45       |
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And CurrentStep should be 4
    And DraftData should contain 3 services
    And DraftData services should include "Haircut" with price 50.00
    And DraftData services should include "Hair Coloring" with price 100.00
    And DraftData services should include "Manicure" with price 30.00

  @query @registration @progress @step5
  Scenario: GetRegistrationProgressQuery - Draft at step 5 (with staff)
    Given I am authenticated as a new user
    And I have completed steps 3-4
    And I have completed step 5 with 2 staff members:
      | Name       | Email            | Position  |
      | John Smith | john@salon.com   | Stylist   |
      | Jane Doe   | jane@salon.com   | Therapist |
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And CurrentStep should be 5
    And DraftData should contain 2 staff members
    And DraftData staff should include "John Smith" as "Stylist"
    And DraftData staff should include "Jane Doe" as "Therapist"

  @query @registration @progress @step6
  Scenario: GetRegistrationProgressQuery - Draft at step 6 (with working hours)
    Given I am authenticated as a new user
    And I have completed steps 3-5
    And I have completed step 6 with business hours for all 7 days
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And CurrentStep should be 6
    And DraftData should contain business hours for 7 days
    And DraftData should show Monday 09:00-18:00 as open
    And DraftData should show Sunday as closed

  @query @registration @progress @step7
  Scenario: GetRegistrationProgressQuery - Draft at step 7 (with gallery)
    Given I am authenticated as a new user
    And I have completed steps 3-6
    And I have completed step 7 with 5 gallery images
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And CurrentStep should be 7
    And DraftData should contain 5 gallery images

  @query @registration @progress @step8
  Scenario: GetRegistrationProgressQuery - Draft at step 8 (with feedback)
    Given I am authenticated as a new user
    And I have completed steps 3-7
    And I have completed step 8 with feedback
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And CurrentStep should be 8

  @query @registration @progress @step9
  Scenario: GetRegistrationProgressQuery - Draft at step 9 (completed)
    Given I am authenticated as a new user
    And I have completed all registration steps
    When I execute GetRegistrationProgressQuery
    Then the command should succeed
    And CurrentStep should be 9
    And DraftData status should be "PendingVerification"

  # ==================== Data Mapping Tests ====================

  @query @registration @progress @business-info-mapping
  Scenario: GetRegistrationProgressQuery - Business info is correctly mapped
    Given I am authenticated as a new user
    And I have a draft with:
      | Field               | Value                |
      | BusinessName        | Elite Beauty Center  |
      | BusinessDescription | Luxury spa services  |
      | Category            | BeautyAndWellness    |
      | PhoneNumber         | +989123456789        |
      | Email               | info@elite.com       |
    When I execute GetRegistrationProgressQuery
    Then DraftData.BusinessInfo.BusinessName should be "Elite Beauty Center"
    And DraftData.BusinessInfo.BusinessDescription should be "Luxury spa services"
    And DraftData.BusinessInfo.Category should be "BeautyAndWellness"
    And DraftData.BusinessInfo.PhoneNumber should be "+989123456789"
    And DraftData.BusinessInfo.Email should be "info@elite.com"

  @query @registration @progress @location-mapping
  Scenario: GetRegistrationProgressQuery - Location is correctly mapped
    Given I am authenticated as a new user
    And I have a draft with location:
      | Field        | Value       |
      | AddressLine1 | 123 Main St |
      | City         | Tehran      |
      | Province     | Tehran      |
      | PostalCode   | 1234567890  |
      | Latitude     | 35.6892     |
      | Longitude    | 51.3890     |
    When I execute GetRegistrationProgressQuery
    Then DraftData.Location.AddressLine1 should be "123 Main St"
    And DraftData.Location.City should be "Tehran"
    And DraftData.Location.Province should be "Tehran"
    And DraftData.Location.PostalCode should be "1234567890"
    And DraftData.Location.Latitude should be 35.6892
    And DraftData.Location.Longitude should be 51.3890

  @query @registration @progress @service-mapping
  Scenario: GetRegistrationProgressQuery - Services are correctly mapped
    Given I am authenticated as a new user
    And I have a draft with service:
      | Field    | Value         |
      | Name     | Premium Cut   |
      | Duration | 90 minutes    |
      | Price    | 150.00        |
      | Type     | FixedPrice    |
    When I execute GetRegistrationProgressQuery
    Then DraftData.Services should contain service with:
      | Field           | Value        |
      | Name            | Premium Cut  |
      | DurationHours   | 1            |
      | DurationMinutes | 30           |
      | Price           | 150.00       |
      | PriceType       | FixedPrice   |

  @query @registration @progress @staff-mapping
  Scenario: GetRegistrationProgressQuery - Staff is correctly mapped
    Given I am authenticated as a new user
    And I have a draft with staff member:
      | Field       | Value             |
      | Name        | Sarah Johnson     |
      | Email       | sarah@salon.com   |
      | PhoneNumber | +989987654321     |
      | Position    | SeniorStylist     |
    When I execute GetRegistrationProgressQuery
    Then DraftData.Staff should contain member with:
      | Field       | Value             |
      | Name        | Sarah Johnson     |
      | Email       | sarah@salon.com   |
      | PhoneNumber | +989987654321     |
      | Position    | SeniorStylist     |

  @query @registration @progress @business-hours-mapping
  Scenario: GetRegistrationProgressQuery - Business hours are correctly mapped
    Given I am authenticated as a new user
    And I have a draft with Monday hours:
      | OpenTime  | CloseTime | IsOpen |
      | 09:00     | 18:00     | true   |
    When I execute GetRegistrationProgressQuery
    Then DraftData.BusinessHours should contain Monday with:
      | DayOfWeek        | 1     |
      | IsOpen           | true  |
      | OpenTimeHours    | 9     |
      | OpenTimeMinutes  | 0     |
      | CloseTimeHours   | 18    |
      | CloseTimeMinutes | 0     |

  @query @registration @progress @closed-day-mapping
  Scenario: GetRegistrationProgressQuery - Closed days are correctly mapped
    Given I am authenticated as a new user
    And I have a draft with Sunday marked as closed
    When I execute GetRegistrationProgressQuery
    Then DraftData.BusinessHours should contain Sunday with:
      | DayOfWeek        | 0     |
      | IsOpen           | false |
      | OpenTimeHours    | null  |
      | OpenTimeMinutes  | null  |
      | CloseTimeHours   | null  |
      | CloseTimeMinutes | null  |

  # ==================== Edge Cases ====================

  @query @registration @progress @missing-optional-fields
  Scenario: GetRegistrationProgressQuery - Missing optional fields return empty strings
    Given I am authenticated as a new user
    And I have a draft with minimal data (no description, no staff phone)
    When I execute GetRegistrationProgressQuery
    Then DraftData.BusinessInfo.BusinessDescription should be ""
    And DraftData.Staff[0].PhoneNumber should be ""

  @query @registration @progress @duration-conversion
  Scenario: GetRegistrationProgressQuery - Service duration is converted to hours and minutes
    Given I am authenticated as a new user
    And I have a draft with service duration 135 minutes
    When I execute GetRegistrationProgressQuery
    Then DraftData.Services[0].DurationHours should be 2
    And DraftData.Services[0].DurationMinutes should be 15

  @query @registration @progress @duration-less-than-hour
  Scenario: GetRegistrationProgressQuery - Service duration less than 1 hour
    Given I am authenticated as a new user
    And I have a draft with service duration 45 minutes
    When I execute GetRegistrationProgressQuery
    Then DraftData.Services[0].DurationHours should be 0
    And DraftData.Services[0].DurationMinutes should be 45

  @query @registration @progress @multiple-services
  Scenario: GetRegistrationProgressQuery - Multiple services are all returned
    Given I am authenticated as a new user
    And I have a draft with 10 services
    When I execute GetRegistrationProgressQuery
    Then DraftData.Services should contain 10 items
    And all services should be mapped correctly

  @query @registration @progress @multiple-staff
  Scenario: GetRegistrationProgressQuery - Multiple staff members are all returned
    Given I am authenticated as a new user
    And I have a draft with 5 staff members
    When I execute GetRegistrationProgressQuery
    Then DraftData.Staff should contain 5 items
    And all staff members should be mapped correctly

  # ==================== Resume Flow ====================

  @query @registration @progress @resume-from-step3
  Scenario: GetRegistrationProgressQuery - User can resume from step 3
    Given I am authenticated as a new user
    And I started registration yesterday
    And I completed step 3 but stopped
    And I log back in today
    When I execute GetRegistrationProgressQuery
    Then HasDraft should be true
    And CurrentStep should be 3
    And I can continue to step 4

  @query @registration @progress @resume-from-step6
  Scenario: GetRegistrationProgressQuery - User can resume from step 6
    Given I am authenticated as a new user
    And I completed steps 3-6 last week
    And I log back in today
    When I execute GetRegistrationProgressQuery
    Then HasDraft should be true
    And CurrentStep should be 6
    And all my previous data should be intact
    And I can continue to step 7

  @query @registration @progress @backward-navigation
  Scenario: GetRegistrationProgressQuery - User can edit earlier steps
    Given I am authenticated as a new user
    And I have completed all steps up to step 7
    When I execute GetRegistrationProgressQuery
    Then I should be able to see all my data
    And I should be able to go back to step 3 to edit location
    And I should be able to go back to step 4 to edit services
    And my progress should not be lost

  # ==================== Authorization ====================

  @query @registration @progress @own-draft-only
  Scenario: GetRegistrationProgressQuery - User can only see their own draft
    Given user A has a draft provider
    And I am authenticated as user B
    When I execute GetRegistrationProgressQuery
    Then HasDraft should be false
    And I should not see user A's draft data

  @query @registration @progress @isolation
  Scenario: GetRegistrationProgressQuery - Drafts are isolated by user
    Given user A has a draft "Salon A"
    And user B has a draft "Salon B"
    And I am authenticated as user A
    When I execute GetRegistrationProgressQuery
    Then I should see "Salon A"
    And I should not see "Salon B"

  # ==================== Status Verification ====================

  @query @registration @progress @drafted-status
  Scenario: GetRegistrationProgressQuery - Draft status is Drafted before completion
    Given I am authenticated as a new user
    And I have completed steps 3-6
    When I execute GetRegistrationProgressQuery
    Then DraftData.Status should be "Drafted"

  @query @registration @progress @pending-verification-status
  Scenario: GetRegistrationProgressQuery - Status is PendingVerification after step 9
    Given I am authenticated as a new user
    And I have completed step 9 (registration complete)
    When I execute GetRegistrationProgressQuery
    Then DraftData.Status should be "PendingVerification"

  @query @registration @progress @provider-id-present
  Scenario: GetRegistrationProgressQuery - Provider ID is included in draft data
    Given I am authenticated as a new user
    And I have a draft provider
    When I execute GetRegistrationProgressQuery
    Then DraftData.ProviderId should not be null
    And DraftData.ProviderId should be a valid GUID
