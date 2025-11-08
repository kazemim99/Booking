Feature: ZarinPal Payment Creation
    As a customer
    I want to create a payment request through ZarinPal
    So that I can pay for my booking using Iranian payment gateway

Background:
    Given a registered provider exists with:
        | Field         | Value                    |
        | BusinessName  | Test Beauty Salon        |
        | BusinessType  | BeautySalon              |
        | Email         | provider@example.com     |
    And a booking exists for the provider with:
        | Field       | Value      |
        | Amount      | 500000     |
        | Currency    | IRR        |
        | Status      | Confirmed  |

Scenario: Successfully create ZarinPal payment request for booking
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value                 |
        | Amount      | 500000                |
        | Description | Booking payment       |
        | Mobile      | 09123456789           |
        | Email       | customer@example.com  |
    Then the response status code should be 200
    And the response should contain:
        | Field        | Value                  |
        | IsSuccessful | true                   |
        | Currency     | IRR                    |
        | Amount       | 500000                 |
    And the response should contain "Authority"
    And the response should contain "PaymentUrl"
    And a payment should exist in the database with:
        | Field    | Value     |
        | Status   | Pending   |
        | Method   | ZarinPal  |
        | Amount   | 500000    |

Scenario: Create ZarinPal payment request with minimum amount
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value                |
        | Amount      | 1000                 |
        | Description | Minimum payment      |
        | Mobile      | 09121234567          |
    Then the response status code should be 200
    And the response should contain:
        | Field        | Value  |
        | IsSuccessful | true   |
        | Amount       | 1000   |

Scenario: Fail to create ZarinPal payment with amount below minimum
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value              |
        | Amount      | 500                |
        | Description | Too small payment  |
    Then the response status code should be 400
    And the response should contain validation error for "Amount"

Scenario: Create direct payment without booking
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value                 |
        | BookingId   | null                  |
        | Amount      | 250000                |
        | Description | Direct service payment|
        | Mobile      | 09127654321           |
    Then the response status code should be 200
    And the response should contain:
        | Field        | Value  |
        | IsSuccessful | true   |
    And a payment should exist in the database with:
        | Field     | Value    |
        | Status    | Pending  |
        | BookingId | null     |

Scenario: Create payment with Iranian mobile number validation
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value          |
        | Amount      | 100000         |
        | Description | Test payment   |
        | Mobile      | 09991234567    |
    Then the response status code should be 200
    And the response should contain:
        | Field        | Value  |
        | IsSuccessful | true   |

Scenario: Fail to create payment with invalid mobile format
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value          |
        | Amount      | 100000         |
        | Description | Test payment   |
        | Mobile      | 1234567890     |
    Then the response status code should be 400
    And the response should contain validation error for "Mobile"

Scenario: Create payment with metadata
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field          | Value              |
        | Amount         | 150000             |
        | Description    | Payment with metadata |
        | Mobile         | 09123456789        |
    And with metadata:
        | Key        | Value          |
        | ip_address | 192.168.1.1    |
        | user_agent | Mozilla/5.0    |
        | device     | mobile         |
    Then the response status code should be 200
    And the payment metadata should be stored correctly

Scenario: Create payment with email for receipt
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value                   |
        | Amount      | 200000                  |
        | Description | Payment with email      |
        | Mobile      | 09123456789             |
        | Email       | receipt@customer.com    |
    Then the response status code should be 200
    And the payment should have email stored

Scenario: Fail to create payment with invalid currency
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value        |
        | Amount      | 100000       |
        | Currency    | USD          |
        | Description | USD payment  |
    Then the response status code should be 400
    And the response should contain validation error "Currency must be IRR for ZarinPal"

Scenario: Create payment and verify PaymentRequestCreated event is published
    When I send a POST request to "/api/v1/payments/zarinpal/create" with:
        | Field       | Value              |
        | Amount      | 300000             |
        | Description | Event test payment |
        | Mobile      | 09123456789        |
    Then the response status code should be 200
    And a "PaymentRequestCreatedEvent" domain event should be published with:
        | Field      | Value    |
        | Authority  | not-null |
        | PaymentUrl | not-null |
