Feature: Service Validation and Data Integrity
  As a system
  I want to validate all service data thoroughly
  So that only valid, safe data is stored in the system

  Background:
    Given a provider "Validation Test Salon" exists with active status
    And I am authenticated as the provider

  # ==================== NAME VALIDATION ====================

  @validation @name @required
  Scenario: Service name is required
    When I create a service without a name
    Then the response status code should be 400
    And the error message should contain "name is required"

  @validation @name @empty-string
  Scenario: Service name cannot be empty string
    When I create a service with name ""
    Then the response status code should be 400
    And the error message should contain "name"

  @validation @name @whitespace-only
  Scenario: Service name cannot be whitespace only
    When I create a service with name "   "
    Then the response status code should be 400
    And the error message should contain "name"

  @validation @name @minimum-length
  Scenario: Service name must meet minimum length
    When I create a service with name "A"
    Then the response status code should be 400
    And the error message should contain "name length"

  @validation @name @maximum-length
  Scenario Outline: Service name length validation
    When I create a service with name of <Length> characters
    Then the response status code should be <StatusCode>

    Examples:
      | Length | StatusCode |
      | 2      | 201        |
      | 50     | 201        |
      | 100    | 201        |
      | 200    | 201        |
      | 201    | 400        |
      | 500    | 400        |

  @validation @name @special-characters
  Scenario Outline: Service name with special characters
    When I create a service with name "<Name>"
    Then the response status code should be <StatusCode>

    Examples:
      | Name                  | StatusCode |
      | Hair & Beauty         | 201        |
      | Nail's Spa            | 201        |
      | Men's Grooming        | 201        |
      | Cut + Style           | 201        |
      | Service #1            | 201        |
      | Service @ Home        | 201        |
      | <script>alert(1)</script> | 400    |
      | '; DROP TABLE Services; -- | 400   |
      | Service<br/>Test      | 400        |
      | Service\nTest         | 201        |

  @validation @name @unicode-support
  Scenario Outline: Service name with Unicode characters
    When I create a service with name "<Name>"
    Then the response status code should be 201
    And the name should be stored correctly as "<Name>"

    Examples:
      | Name           |
      | 理发服务       |
      | Corte de Pelo  |
      | Стрижка        |
      | قص الشعر       |
      | カット         |
      | 💇‍♀️ Haircut   |

  @validation @name @emoji
  Scenario: Service name with emoji
    When I create a service with name "Haircut ✂️💇‍♀️"
    Then the response status code should be 201

  @validation @name @duplicate-per-provider
  Scenario: Cannot create duplicate service name for same provider
    Given the provider has a service "Haircut"
    When I create another service with name "Haircut"
    Then the response status code should be 409
    And the error message should contain "already exists"

  @validation @name @duplicate-different-provider
  Scenario: Can create same service name for different provider
    Given the provider has a service "Haircut"
    And another provider "Other Salon" exists
    When the other provider creates a service "Haircut"
    Then the response status code should be 201

  @validation @name @case-sensitivity
  Scenario: Service name is case-sensitive for uniqueness
    Given the provider has a service "Haircut"
    When I create a service with name "HAIRCUT"
    Then the response status code should be 201

  @validation @name @trim-whitespace
  Scenario: Leading and trailing whitespace is trimmed
    When I create a service with name "  Haircut  "
    Then the response status code should be 201
    And the stored name should be "Haircut"

  # ==================== DESCRIPTION VALIDATION ====================

  @validation @description @optional
  Scenario: Description is optional
    When I create a service without description
    Then the response status code should be 201

  @validation @description @maximum-length
  Scenario Outline: Description length validation
    When I create a service with description of <Length> characters
    Then the response status code should be <StatusCode>

    Examples:
      | Length | StatusCode |
      | 0      | 201        |
      | 100    | 201        |
      | 1000   | 201        |
      | 2000   | 201        |
      | 2001   | 400        |
      | 5000   | 400        |

  @validation @description @xss-prevention
  Scenario Outline: Prevent XSS in description
    When I create a service with description "<Description>"
    Then the response status code should be 400
    And the error message should contain "invalid characters"

    Examples:
      | Description                           |
      | <script>alert('xss')</script>         |
      | <img src=x onerror=alert(1)>          |
      | <iframe src="javascript:alert(1)">    |
      | <svg onload=alert(1)>                 |
      | javascript:alert(1)                   |
      | <body onload=alert('XSS')>            |

  @validation @description @safe-html
  Scenario: Allow safe HTML-like text in description
    When I create a service with description "Use <code> for discount"
    Then the response status code should be 201

  @validation @description @line-breaks
  Scenario: Line breaks are preserved in description
    When I create a service with description:
      """
      First line
      Second line
      Third line
      """
    Then the response status code should be 201
    And line breaks should be preserved

  # ==================== PRICE VALIDATION ====================

  @validation @price @required
  Scenario: Price is required
    When I create a service without price
    Then the response status code should be 400
    And the error message should contain "price"

  @validation @price @positive
  Scenario Outline: Price must be positive
    When I create a service with price <Price>
    Then the response status code should be <StatusCode>

    Examples:
      | Price    | StatusCode |
      | 0.01     | 201        |
      | 1.00     | 201        |
      | 50.00    | 201        |
      | 99999.99 | 201        |
      | 0.00     | 400        |
      | -0.01    | 400        |
      | -50.00   | 400        |

  @validation @price @decimal-places
  Scenario Outline: Price decimal places validation
    When I create a service with price "<Price>"
    Then the response status code should be <StatusCode>

    Examples:
      | Price      | StatusCode |
      | 50.00      | 201        |
      | 50.50      | 201        |
      | 50.99      | 201        |
      | 50.001     | 400        |
      | 50.9999    | 400        |
      | 50         | 201        |

  @validation @price @maximum-value
  Scenario: Price has reasonable maximum
    When I create a service with price 1000000.00
    Then the response status code should be 400
    And the error message should contain "price too high"

  # ==================== CURRENCY VALIDATION ====================

  @validation @currency @required
  Scenario: Currency is required
    When I create a service without currency
    Then the response status code should be 400
    And the error message should contain "currency"

  @validation @currency @iso-4217
  Scenario Outline: Currency must be valid ISO 4217 code
    When I create a service with currency "<Currency>"
    Then the response status code should be <StatusCode>

    Examples:
      | Currency | StatusCode |
      | USD      | 201        |
      | EUR      | 201        |
      | GBP      | 201        |
      | JPY      | 201        |
      | IRR      | 201        |
      | CNY      | 201        |
      | INR      | 201        |
      | CAD      | 201        |
      | AUD      | 201        |
      | US       | 400        |
      | Dollar   | 400        |
      | $        | 400        |
      | €        | 400        |
      | 123      | 400        |
      | XXX      | 400        |
      | usd      | 400        |

  @validation @currency @uppercase-only
  Scenario: Currency code must be uppercase
    When I create a service with currency "usd"
    Then the response status code should be 400

  # ==================== DURATION VALIDATION ====================

  @validation @duration @required
  Scenario: Duration is required
    When I create a service without duration
    Then the response status code should be 400
    And the error message should contain "duration"

  @validation @duration @positive
  Scenario Outline: Duration must be positive
    When I create a service with duration <Duration> minutes
    Then the response status code should be <StatusCode>

    Examples:
      | Duration | StatusCode |
      | 5        | 201        |
      | 15       | 201        |
      | 30       | 201        |
      | 60       | 201        |
      | 120      | 201        |
      | 480      | 201        |
      | 0        | 400        |
      | -15      | 400        |

  @validation @duration @maximum
  Scenario: Duration has reasonable maximum
    When I create a service with duration 481 minutes
    Then the response status code should be 400
    And the error message should contain "duration too long"

  @validation @duration @increments
  Scenario: Duration should be in 5-minute increments
    When I create a service with duration 37 minutes
    Then the response status code should be 400
    And the error message should contain "duration must be in 5-minute increments"

  # ==================== CATEGORY VALIDATION ====================

  @validation @category @required
  Scenario: Category is required
    When I create a service without category
    Then the response status code should be 400
    And the error message should contain "category"

  @validation @category @valid-values
  Scenario Outline: Category must be from predefined list
    When I create a service with category "<Category>"
    Then the response status code should be <StatusCode>

    Examples:
      | Category            | StatusCode |
      | Hair Services       | 201        |
      | Nail Services       | 201        |
      | Spa Services        | 201        |
      | Beauty Services     | 201        |
      | Massage             | 201        |
      | Fitness             | 201        |
      | Invalid Category    | 400        |
      | Random Text         | 400        |

  @validation @category @case-sensitive
  Scenario: Category validation is case-sensitive
    When I create a service with category "hair services"
    Then the response status code should be 400

  # ==================== OPTIONAL FIELDS VALIDATION ====================

  @validation @optional @preparation-time
  Scenario Outline: Preparation time validation
    When I create a service with preparation time <Minutes> minutes
    Then the response status code should be <StatusCode>

    Examples:
      | Minutes | StatusCode |
      | 0       | 201        |
      | 5       | 201        |
      | 30      | 201        |
      | 60      | 201        |
      | -5      | 400        |
      | 121     | 400        |

  @validation @optional @buffer-time
  Scenario Outline: Buffer time validation
    When I create a service with buffer time <Minutes> minutes
    Then the response status code should be <StatusCode>

    Examples:
      | Minutes | StatusCode |
      | 0       | 201        |
      | 5       | 201        |
      | 30      | 201        |
      | 60      | 201        |
      | -5      | 400        |
      | 121     | 400        |

  @validation @optional @image-url
  Scenario Outline: Image URL validation
    When I create a service with image URL "<URL>"
    Then the response status code should be <StatusCode>

    Examples:
      | URL                                    | StatusCode |
      | https://example.com/image.jpg          | 201        |
      | https://cdn.example.com/img/photo.png  | 201        |
      | http://example.com/image.gif           | 201        |
      | invalid-url                            | 400        |
      | ftp://example.com/image.jpg            | 400        |
      | javascript:alert(1)                    | 400        |

  @validation @optional @max-advance-booking
  Scenario Outline: Max advance booking days validation
    When I create a service with max advance booking <Days> days
    Then the response status code should be <StatusCode>

    Examples:
      | Days | StatusCode |
      | 1    | 201        |
      | 30   | 201        |
      | 90   | 201        |
      | 365  | 201        |
      | 0    | 400        |
      | -7   | 400        |
      | 366  | 400        |

  @validation @optional @min-advance-booking
  Scenario Outline: Min advance booking hours validation
    When I create a service with min advance booking <Hours> hours
    Then the response status code should be <StatusCode>

    Examples:
      | Hours | StatusCode |
      | 0     | 201        |
      | 1     | 201        |
      | 24    | 201        |
      | 72    | 201        |
      | -1    | 400        |
      | 169   | 400        |

  @validation @optional @max-concurrent-bookings
  Scenario Outline: Max concurrent bookings validation
    When I create a service with max concurrent bookings <Count>
    Then the response status code should be <StatusCode>

    Examples:
      | Count | StatusCode |
      | 1     | 201        |
      | 5     | 201        |
      | 50    | 201        |
      | 100   | 201        |
      | 0     | 400        |
      | -1    | 400        |
      | 101   | 400        |

  # ==================== DEPOSIT VALIDATION ====================

  @validation @deposit @percentage-range
  Scenario Outline: Deposit percentage validation
    When I create a service with deposit percentage <Percentage>
    Then the response status code should be <StatusCode>

    Examples:
      | Percentage | StatusCode |
      | 0          | 201        |
      | 10         | 201        |
      | 50         | 201        |
      | 100        | 201        |
      | -1         | 400        |
      | 101        | 400        |

  @validation @deposit @requires-percentage
  Scenario: Deposit percentage required if deposit enabled
    When I create a service with:
      | Field           | Value |
      | RequiresDeposit | true  |
    And no deposit percentage
    Then the response status code should be 400
    And the error message should contain "deposit percentage"

  @validation @deposit @amount-vs-percentage
  Scenario: Cannot specify both deposit amount and percentage
    When I create a service with:
      | Field             | Value |
      | DepositAmount     | 25.00 |
      | DepositPercentage | 50    |
    Then the response status code should be 400
    And the error message should contain "deposit"

  # ==================== BOOKING RULES VALIDATION ====================

  @validation @booking-rules @logical-consistency
  Scenario: Min advance booking cannot exceed max advance booking
    When I create a service with:
      | Field                  | Value |
      | MinAdvanceBookingHours | 48    |
      | MaxAdvanceBookingDays  | 1     |
    Then the response status code should be 400
    And the error message should contain "booking rules inconsistent"

  # ==================== SERVICE TYPE VALIDATION ====================

  @validation @type @valid-values
  Scenario Outline: Service type validation
    When I create a service with type "<Type>"
    Then the response status code should be <StatusCode>

    Examples:
      | Type        | StatusCode |
      | Standard    | 201        |
      | Premium     | 201        |
      | Luxury      | 201        |
      | InvalidType | 400        |

  # ==================== TAGS VALIDATION ====================

  @validation @tags @maximum-count
  Scenario Outline: Service tags count validation
    When I create a service with <Count> tags
    Then the response status code should be <StatusCode>

    Examples:
      | Count | StatusCode |
      | 0     | 201        |
      | 5     | 201        |
      | 10    | 201        |
      | 11    | 400        |

  @validation @tags @empty-tag
  Scenario: Cannot add empty tag
    When I create a service with tags:
      | Tag      |
      | popular  |
      |          |
      | trending |
    Then the response status code should be 400
    And the error message should contain "empty tag"

  @validation @tags @whitespace-tag
  Scenario: Cannot add whitespace-only tag
    When I create a service with tags:
      | Tag      |
      | popular  |
      |          |
      | trending |
    Then the response status code should be 400

  @validation @tags @duplicate-tags
  Scenario: Cannot add duplicate tags
    When I create a service with tags:
      | Tag      |
      | popular  |
      | trending |
      | popular  |
    Then the response status code should be 400
    And the error message should contain "duplicate tag"

  @validation @tags @tag-length
  Scenario Outline: Tag length validation
    When I create a service with a tag of <Length> characters
    Then the response status code should be <StatusCode>

    Examples:
      | Length | StatusCode |
      | 2      | 201        |
      | 20     | 201        |
      | 30     | 201        |
      | 31     | 400        |

  # ==================== COMBINED VALIDATION ====================

  @validation @combined @multiple-errors
  Scenario: Multiple validation errors returned together
    When I create a service with:
      | Field       | Value |
      | ServiceName |       |
      | BasePrice   | -10   |
      | Duration    | 0     |
    Then the response status code should be 400
    And the error should list all validation failures

  @validation @combined @all-valid
  Scenario: Service with all fields valid
    When I create a service with:
      | Field                  | Value                          |
      | ServiceName            | Complete Service               |
      | Description            | Full description               |
      | BasePrice              | 75.00                          |
      | Currency               | USD                            |
      | Duration               | 60                             |
      | Category               | Hair Services                  |
      | ServiceType            | Premium                        |
      | PreparationMinutes     | 10                             |
      | BufferMinutes          | 15                             |
      | RequiresDeposit        | true                           |
      | DepositPercentage      | 25                             |
      | AvailableAtLocation    | true                           |
      | AvailableAsMobile      | false                          |
      | MaxAdvanceBookingDays  | 90                             |
      | MinAdvanceBookingHours | 24                             |
      | MaxConcurrentBookings  | 5                              |
      | ImageUrl               | https://example.com/image.jpg  |
    Then the response status code should be 201
    And all fields should be stored correctly

  # ==================== UPDATE VALIDATION ====================

  @validation @update @partial-update
  Scenario: Partial update validates only provided fields
    Given the provider has a service "Existing Service"
    When I update the service with only:
      | Field     | Value |
      | BasePrice | 60.00 |
    Then the response status code should be 200
    And only the price should be updated

  @validation @update @invalid-partial
  Scenario: Invalid partial update fails
    Given the provider has a service "Existing Service"
    When I update the service with only:
      | Field     | Value |
      | BasePrice | -10   |
    Then the response status code should be 400

  @validation @update @immutable-fields
  Scenario: Cannot update immutable fields
    Given the provider has a service "Existing Service"
    When I attempt to update the provider ID
    Then the response status code should be 400
    And the error message should contain "cannot be changed"

  # ==================== NULL vs MISSING ====================

  @validation @null-handling @explicit-null
  Scenario: Explicit null for optional field
    When I create a service with explicit null for ImageUrl
    Then the response status code should be 201
    And ImageUrl should be null

  @validation @null-handling @missing-field
  Scenario: Missing optional field treated as null
    When I create a service without ImageUrl field
    Then the response status code should be 201
    And ImageUrl should be null

  @validation @null-handling @null-required-field
  Scenario: Cannot provide null for required field
    When I create a service with explicit null for ServiceName
    Then the response status code should be 400

  # ==================== EDGE CASES ====================

  @validation @edge-case @very-large-numbers
  Scenario: Handle very large numbers safely
    When I create a service with price 999999999999.99
    Then the response status code should be 400

  @validation @edge-case @float-precision
  Scenario: Maintain decimal precision
    When I create a service with price 49.99
    Then the service should store exactly 49.99
    And not lose precision due to floating point

  @validation @edge-case @boundary-values
  Scenario: Test all boundary values together
    When I create services at all boundary values
    Then all valid boundaries should be accepted
    And all invalid boundaries should be rejected
