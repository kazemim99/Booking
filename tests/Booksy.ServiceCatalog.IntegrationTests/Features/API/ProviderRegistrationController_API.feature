Feature: ProviderRegistrationController API - Complete Coverage
  As a system
  I want to test all ProviderRegistrationController HTTP endpoints
  So that the API contract is properly validated

  Background:
    Given I am authenticated as a new user

  # ==================== GET /api/v1/registration/progress ====================

  @api @registration @progress @get @happy-path
  Scenario: GET /progress - Returns 200 with draft data when draft exists
    Given I have a draft provider at step 5
    When I send GET request to "/api/v1/registration/progress"
    Then the response status code should be 200
    And the response should contain JSON with:
      | Field       | Value |
      | HasDraft    | true  |
      | CurrentStep | 5     |
    And DraftData should be present
    And response content-type should be "application/json"

  @api @registration @progress @get @not-found
  Scenario: GET /progress - Returns 404 when no draft exists
    Given I have not started registration
    When I send GET request to "/api/v1/registration/progress"
    Then the response status code should be 404

  @api @registration @progress @get @unauthorized
  Scenario: GET /progress - Returns 401 when not authenticated
    Given I am not authenticated
    When I send GET request to "/api/v1/registration/progress"
    Then the response status code should be 401

  # ==================== POST /api/v1/registration/step-3/location ====================

  @api @registration @step3 @post @created
  Scenario: POST /step-3/location - Returns 201 Created for new draft
    When I send POST request to "/api/v1/registration/step-3/location" with:
      """
      {
        "businessName": "Luxury Salon",
        "businessDescription": "Premium services",
        "category": "BeautyAndWellness",
        "phoneNumber": "+989123456789",
        "email": "salon@test.com",
        "addressLine1": "123 Main St",
        "city": "Tehran",
        "province": "Tehran",
        "postalCode": "1234567890",
        "latitude": 35.6892,
        "longitude": 51.3890
      }
      """
    Then the response status code should be 201
    And the response should contain JSON with:
      | Field            | Value |
      | RegistrationStep | 3     |
    And response should contain message "created"
    And Location header should be "/api/v1/providers/{providerId}"

  @api @registration @step3 @post @ok
  Scenario: POST /step-3/location - Returns 200 OK for updating existing draft
    Given I have a draft provider from previous step
    When I send POST request to "/api/v1/registration/step-3/location" with updated data
    Then the response status code should be 200
    And response should contain message "updated"

  @api @registration @step3 @post @bad-request
  Scenario: POST /step-3/location - Returns 400 for invalid data
    When I send POST request to "/api/v1/registration/step-3/location" with:
      """
      {
        "businessName": "",
        "category": "InvalidCategory",
        "email": "not-an-email"
      }
      """
    Then the response status code should be 400
    And response should contain validation errors

  @api @registration @step3 @post @unauthorized
  Scenario: POST /step-3/location - Returns 401 when not authenticated
    Given I am not authenticated
    When I send POST request to "/api/v1/registration/step-3/location"
    Then the response status code should be 401

  @api @registration @step3 @post @location-header
  Scenario: POST /step-3/location - Location header contains provider ID
    When I send POST request to "/api/v1/registration/step-3/location" with valid data
    Then the response status code should be 201
    And Location header should match pattern "/api/v1/providers/[0-9a-f\-]{36}"
    And I can use the Location header to fetch the provider

  # ==================== POST /api/v1/registration/step-4/services ====================

  @api @registration @step4 @post @happy-path
  Scenario: POST /step-4/services - Returns 200 OK when services saved
    Given I have a draft provider at step 3
    When I send POST request to "/api/v1/registration/step-4/services" with:
      """
      {
        "providerId": "{currentProviderId}",
        "services": [
          {
            "name": "Haircut",
            "durationMinutes": 30,
            "price": 50.00,
            "currency": "USD"
          }
        ]
      }
      """
    Then the response status code should be 200
    And response should contain:
      | Field            | Value |
      | RegistrationStep | 4     |

  @api @registration @step4 @post @not-found
  Scenario: POST /step-4/services - Returns 404 when provider draft not found
    When I send POST request to "/api/v1/registration/step-4/services" with non-existent provider ID
    Then the response status code should be 404

  @api @registration @step4 @post @bad-request
  Scenario: POST /step-4/services - Returns 400 for empty services list
    Given I have a draft provider at step 3
    When I send POST request to "/api/v1/registration/step-4/services" with:
      """
      {
        "providerId": "{currentProviderId}",
        "services": []
      }
      """
    Then the response status code should be 400
    And error message should contain "at least one service"

  # ==================== POST /api/v1/registration/step-5/staff ====================

  @api @registration @step5 @post @happy-path
  Scenario: POST /step-5/staff - Returns 200 OK when staff saved
    Given I have a draft provider at step 4
    When I send POST request to "/api/v1/registration/step-5/staff" with:
      """
      {
        "providerId": "{currentProviderId}",
        "staff": [
          {
            "name": "John Smith",
            "email": "john@salon.com",
            "role": "Stylist"
          }
        ]
      }
      """
    Then the response status code should be 200
    And response should contain:
      | Field            | Value |
      | RegistrationStep | 5     |

  @api @registration @step5 @post @not-found
  Scenario: POST /step-5/staff - Returns 404 when provider draft not found
    When I send POST request to "/api/v1/registration/step-5/staff" with non-existent provider ID
    Then the response status code should be 404

  @api @registration @step5 @post @bad-request
  Scenario: POST /step-5/staff - Returns 400 for duplicate staff email
    Given I have a draft provider with staff "john@salon.com"
    When I send POST request to "/api/v1/registration/step-5/staff" adding another staff with same email
    Then the response status code should be 400

  # ==================== POST /api/v1/registration/step-6/working-hours ====================

  @api @registration @step6 @post @happy-path
  Scenario: POST /step-6/working-hours - Returns 200 OK when hours saved
    Given I have a draft provider at step 5
    When I send POST request to "/api/v1/registration/step-6/working-hours" with:
      """
      {
        "providerId": "{currentProviderId}",
        "businessHours": [
          {
            "dayOfWeek": 1,
            "isOpen": true,
            "openTime": "09:00",
            "closeTime": "18:00"
          }
        ]
      }
      """
    Then the response status code should be 200
    And response should contain:
      | Field            | Value |
      | RegistrationStep | 6     |

  @api @registration @step6 @post @not-found
  Scenario: POST /step-6/working-hours - Returns 404 when provider draft not found
    When I send POST request to "/api/v1/registration/step-6/working-hours" with non-existent provider ID
    Then the response status code should be 404

  @api @registration @step6 @post @bad-request
  Scenario: POST /step-6/working-hours - Returns 400 for invalid time range
    Given I have a draft provider at step 5
    When I send POST request to "/api/v1/registration/step-6/working-hours" with closeTime before openTime
    Then the response status code should be 400

  # ==================== POST /api/v1/registration/step-7/gallery ====================

  @api @registration @step7 @post @happy-path
  Scenario: POST /step-7/gallery - Returns 200 OK when gallery step completed
    Given I have a draft provider at step 6
    When I send POST request to "/api/v1/registration/step-7/gallery" with:
      """
      {
        "providerId": "{currentProviderId}",
        "imageUrls": [
          "https://example.com/image1.jpg",
          "https://example.com/image2.jpg"
        ]
      }
      """
    Then the response status code should be 200
    And response should contain:
      | Field            | Value |
      | RegistrationStep | 7     |

  @api @registration @step7 @post @not-found
  Scenario: POST /step-7/gallery - Returns 404 when provider draft not found
    When I send POST request to "/api/v1/registration/step-7/gallery" with non-existent provider ID
    Then the response status code should be 404

  @api @registration @step7 @post @optional
  Scenario: POST /step-7/gallery - Returns 200 OK even with no images (optional)
    Given I have a draft provider at step 6
    When I send POST request to "/api/v1/registration/step-7/gallery" with empty images
    Then the response status code should be 200

  # ==================== POST /api/v1/registration/step-8/feedback ====================

  @api @registration @step8 @post @happy-path
  Scenario: POST /step-8/feedback - Returns 200 OK when feedback saved
    Given I have a draft provider at step 7
    When I send POST request to "/api/v1/registration/step-8/feedback" with:
      """
      {
        "providerId": "{currentProviderId}",
        "rating": 5,
        "comments": "Great process!",
        "source": "Google Search"
      }
      """
    Then the response status code should be 200
    And response should contain:
      | Field            | Value |
      | RegistrationStep | 8     |

  @api @registration @step8 @post @not-found
  Scenario: POST /step-8/feedback - Returns 404 when provider draft not found
    When I send POST request to "/api/v1/registration/step-8/feedback" with non-existent provider ID
    Then the response status code should be 404

  @api @registration @step8 @post @optional
  Scenario: POST /step-8/feedback - Returns 200 OK even with no feedback (optional)
    Given I have a draft provider at step 7
    When I send POST request to "/api/v1/registration/step-8/feedback" with empty feedback
    Then the response status code should be 200

  # ==================== POST /api/v1/registration/step-9/complete ====================

  @api @registration @step9 @post @happy-path
  Scenario: POST /step-9/complete - Returns 200 OK when registration completed
    Given I have a draft provider at step 8 with all required data
    When I send POST request to "/api/v1/registration/step-9/complete" with:
      """
      {
        "providerId": "{currentProviderId}"
      }
      """
    Then the response status code should be 200
    And response should contain:
      | Field  | Value               |
      | Status | PendingVerification |
    And response message should contain "completed successfully"

  @api @registration @step9 @post @not-found
  Scenario: POST /step-9/complete - Returns 404 when provider draft not found
    When I send POST request to "/api/v1/registration/step-9/complete" with non-existent provider ID
    Then the response status code should be 404

  @api @registration @step9 @post @bad-request-missing-hours
  Scenario: POST /step-9/complete - Returns 400 when business hours missing
    Given I have a draft provider at step 8 with no business hours
    When I send POST request to "/api/v1/registration/step-9/complete"
    Then the response status code should be 400
    And error message should contain "Business hours are required"

  @api @registration @step9 @post @bad-request-missing-services
  Scenario: POST /step-9/complete - Returns 400 when services missing
    Given I have a draft provider at step 8 with no services
    When I send POST request to "/api/v1/registration/step-9/complete"
    Then the response status code should be 400
    And error message should contain "At least one service is required"

  @api @registration @step9 @post @bad-request-missing-name
  Scenario: POST /step-9/complete - Returns 400 when business name missing
    Given I have a draft provider at step 8 with no business name
    When I send POST request to "/api/v1/registration/step-9/complete"
    Then the response status code should be 400
    And error message should contain "Business name is required"

  # ==================== Content Negotiation ====================

  @api @registration @content-negotiation @json
  Scenario: All endpoints accept and return JSON
    When I send POST request to "/api/v1/registration/step-3/location" with Content-Type "application/json"
    Then the response should have Content-Type "application/json"

  @api @registration @content-negotiation @unsupported-media
  Scenario: Endpoints return 415 for unsupported content type
    When I send POST request to "/api/v1/registration/step-3/location" with Content-Type "text/xml"
    Then the response status code should be 415

  # ==================== Authorization ====================

  @api @registration @authorization @all-endpoints-require-auth
  Scenario Outline: All endpoints require authentication
    Given I am not authenticated
    When I send <Method> request to "<Endpoint>"
    Then the response status code should be 401

    Examples:
      | Method | Endpoint                                  |
      | GET    | /api/v1/registration/progress             |
      | POST   | /api/v1/registration/step-3/location      |
      | POST   | /api/v1/registration/step-4/services      |
      | POST   | /api/v1/registration/step-5/staff         |
      | POST   | /api/v1/registration/step-6/working-hours |
      | POST   | /api/v1/registration/step-7/gallery       |
      | POST   | /api/v1/registration/step-8/feedback      |
      | POST   | /api/v1/registration/step-9/complete      |

  @api @registration @authorization @token-required
  Scenario: Endpoints reject requests with invalid bearer token
    Given I have an invalid bearer token
    When I send GET request to "/api/v1/registration/progress"
    Then the response status code should be 401

  # ==================== CORS ====================

  @api @registration @cors @preflight
  Scenario: OPTIONS request returns CORS headers
    When I send OPTIONS request to "/api/v1/registration/progress"
    Then the response should contain CORS headers:
      | Header                       | Value |
      | Access-Control-Allow-Origin  | *     |
      | Access-Control-Allow-Methods | GET   |

  # ==================== Error Handling ====================

  @api @registration @error-handling @malformed-json
  Scenario: Endpoint returns 400 for malformed JSON
    When I send POST request to "/api/v1/registration/step-3/location" with malformed JSON:
      """
      { invalid json }
      """
    Then the response status code should be 400
    And error message should indicate JSON parsing error

  @api @registration @error-handling @missing-required-fields
  Scenario: Endpoint returns 400 for missing required fields
    When I send POST request to "/api/v1/registration/step-3/location" with:
      """
      {
        "businessName": "Test"
      }
      """
    Then the response status code should be 400
    And error message should list missing required fields

  @api @registration @error-handling @validation-errors-detailed
  Scenario: Validation errors return detailed error information
    When I send POST request to "/api/v1/registration/step-3/location" with multiple validation errors
    Then the response status code should be 400
    And response should contain array of validation errors
    And each error should have field name and error message

  # ==================== Rate Limiting ====================

  @api @registration @rate-limiting @too-many-requests
  Scenario: Endpoint returns 429 when rate limit exceeded
    Given I have made 100 requests in the last minute
    When I send POST request to "/api/v1/registration/step-3/location"
    Then the response status code should be 429
    And response should contain Retry-After header

  # ==================== Idempotency ====================

  @api @registration @idempotency @step3
  Scenario: POST /step-3/location is idempotent
    When I send POST request to "/api/v1/registration/step-3/location" with idempotency key "abc-123"
    And I send the same POST request again with same idempotency key "abc-123"
    Then both requests should return 200/201
    And no duplicate provider should be created
    And both responses should have same ProviderId

  # ==================== Cancellation Token ====================

  @api @registration @cancellation @request-timeout
  Scenario: Long-running request respects cancellation token
    When I send POST request to "/api/v1/registration/step-3/location"
    And I cancel the request after 100ms
    Then the server should stop processing
    And resources should be cleaned up

  # ==================== Complete Flow ====================

  @api @registration @flow @complete-journey
  Scenario: Complete registration flow through all API endpoints
    Given I am a new user
    When I POST to /step-3/location with location data
    Then response status should be 201
    When I POST to /step-4/services with services data
    Then response status should be 200
    When I POST to /step-5/staff with staff data
    Then response status should be 200
    When I POST to /step-6/working-hours with hours data
    Then response status should be 200
    When I POST to /step-7/gallery with gallery data
    Then response status should be 200
    When I POST to /step-8/feedback with feedback data
    Then response status should be 200
    When I POST to /step-9/complete
    Then response status should be 200
    And status should be "PendingVerification"
    When I GET /progress
    Then CurrentStep should be 9
    And all my data should be present

  @api @registration @flow @resume-after-disconnect
  Scenario: Resume registration after interruption
    Given I completed steps 3-5 yesterday
    And I log back in today
    When I GET /progress
    Then CurrentStep should be 5
    And all previous data should be intact
    When I continue with step 6
    Then I can complete the remaining steps
