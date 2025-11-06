Feature: Query Handlers - Complete Coverage
  As a system
  I want to test all query handlers
  So that all data retrieval logic is validated

  # ==================== Booking Queries ====================

  @query @booking @get-by-id @happy-path
  Scenario: GetBookingByIdQuery - Retrieve existing booking
    Given there is a booking with known ID
    When I execute GetBookingByIdQuery
    Then the query should return the booking
    And all booking details should be populated

  @query @booking @get-by-id @not-found
  Scenario: GetBookingByIdQuery - Booking not found
    When I execute GetBookingByIdQuery with non-existent ID
    Then the query should return null
    Or the query should throw NotFoundException

  @query @booking @customer-bookings @happy-path
  Scenario: GetCustomerBookingsQuery - Retrieve customer bookings
    Given I am authenticated as a customer
    And I have 5 bookings
    When I execute GetCustomerBookingsQuery
    Then the query should return 5 bookings
    And all bookings should belong to current customer

  @query @booking @customer-bookings @filter-status
  Scenario: GetCustomerBookingsQuery - Filter by status
    Given I have bookings with different statuses
    When I execute GetCustomerBookingsQuery with status filter "Confirmed"
    Then only confirmed bookings should be returned

  @query @booking @customer-bookings @pagination
  Scenario: GetCustomerBookingsQuery - Pagination support
    Given I have 20 bookings
    When I execute GetCustomerBookingsQuery with page 2, size 5
    Then 5 bookings should be returned
    And pagination metadata should show total count 20
    And results should be from second page

  @query @booking @customer-bookings @sorting
  Scenario: GetCustomerBookingsQuery - Sort by date
    Given I have bookings on different dates
    When I execute GetCustomerBookingsQuery sorted by StartTime DESC
    Then bookings should be ordered from newest to oldest

  @query @booking @provider-bookings @happy-path
  Scenario: GetProviderBookingsQuery - Retrieve provider bookings
    Given I am authenticated as a provider
    And there are 10 bookings for my provider
    When I execute GetProviderBookingsQuery
    Then all 10 bookings should be returned

  @query @booking @provider-bookings @date-range
  Scenario: GetProviderBookingsQuery - Filter by date range
    Given there are bookings across multiple months
    When I execute GetProviderBookingsQuery for last 30 days
    Then only bookings within the date range should be returned

  # ==================== Payment Queries ====================

  @query @payment @get-by-id @happy-path
  Scenario: GetPaymentByIdQuery - Retrieve payment
    Given there is a payment with known ID
    When I execute GetPaymentByIdQuery
    Then the payment should be returned with all details

  @query @payment @customer-payments @happy-path
  Scenario: GetCustomerPaymentsQuery - Retrieve all payments
    Given I have 5 payments
    When I execute GetCustomerPaymentsQuery
    Then all 5 payments should be returned

  @query @payment @provider-earnings @happy-path
  Scenario: GetProviderEarningsQuery - Calculate earnings
    Given I am authenticated as a provider
    And the provider has payments totaling 500.00 USD
    When I execute GetProviderEarningsQuery for last 30 days
    Then the gross earnings should be 500.00
    And commission should be calculated
    And net earnings should be gross minus commission

  @query @payment @provider-earnings @by-date
  Scenario: GetProviderEarningsQuery - Earnings by date breakdown
    Given payments exist on different dates
    When I execute GetProviderEarningsQuery with daily breakdown
    Then earnings should be grouped by date
    And each date should show payment count and amounts

  @query @payment @provider-earnings @empty
  Scenario: GetProviderEarningsQuery - No earnings in period
    Given there are no payments in the specified period
    When I execute GetProviderEarningsQuery
    Then gross earnings should be 0
    And the query should still return a valid result

  # ==================== Payout Queries ====================

  @query @payout @provider-payouts @happy-path
  Scenario: GetProviderPayoutsQuery - Retrieve payouts
    Given I am authenticated as a provider
    And the provider has 3 completed payouts
    When I execute GetProviderPayoutsQuery
    Then all 3 payouts should be returned

  @query @payout @pending-payouts @happy-path
  Scenario: GetPendingPayoutsQuery - Admin views pending payouts
    Given I am authenticated as an admin
    And there are 5 pending payouts across providers
    When I execute GetPendingPayoutsQuery
    Then all 5 pending payouts should be returned

  @query @payout @pending-payouts @authorization
  Scenario: GetPendingPayoutsQuery - Non-admin cannot view
    Given I am authenticated as a customer
    When I execute GetPendingPayoutsQuery
    Then the query should fail with ForbiddenException

  # ==================== Service Queries ====================

  @query @service @get-by-id @happy-path
  Scenario: GetServiceByIdQuery - Retrieve service
    Given there is a service with known ID
    When I execute GetServiceByIdQuery
    Then the service should be returned
    And options and price tiers should be included

  @query @service @provider-services @happy-path
  Scenario: GetProviderServicesQuery - Retrieve provider services
    Given a provider has 5 services
    When I execute GetProviderServicesQuery
    Then all 5 services should be returned
    And they should belong to the provider

  @query @service @search @happy-path
  Scenario: SearchServicesQuery - Search by category
    Given there are 10 services in "Hair Services" category
    When I execute SearchServicesQuery for category "Hair Services"
    Then 10 services should be returned

  @query @service @search @price-range
  Scenario: SearchServicesQuery - Filter by price range
    Given there are services with various prices
    When I execute SearchServicesQuery with minPrice 30 and maxPrice 70
    Then only services within price range should be returned

  @query @service @search @pagination
  Scenario: SearchServicesQuery - Paginated results
    Given there are 50 services
    When I execute SearchServicesQuery with page 1, size 10
    Then 10 services should be returned
    And pagination should indicate 5 total pages

  # ==================== Provider Queries ====================

  @query @provider @get-by-id @happy-path
  Scenario: GetProviderByIdQuery - Retrieve provider profile
    Given there is a provider with known ID
    When I execute GetProviderByIdQuery
    Then the provider should be returned
    And all profile information should be included

  @query @provider @search @happy-path
  Scenario: SearchProvidersQuery - Search by business name
    Given there are 5 providers with "Salon" in name
    When I execute SearchProvidersQuery for "Salon"
    Then 5 providers should be returned

  @query @provider @location @happy-path
  Scenario: GetProvidersByLocationQuery - Find nearby providers
    Given there are providers at various locations
    When I execute GetProvidersByLocationQuery for lat 35.6892, long 51.3890, radius 10km
    Then only providers within 10km should be returned
    And results should be sorted by distance

  @query @provider @current-status @happy-path
  Scenario: GetCurrentProviderStatusQuery - Get own provider status
    Given I am authenticated as a provider
    When I execute GetCurrentProviderStatusQuery
    Then my provider status should be returned

  @query @provider @current-status @validation
  Scenario: GetCurrentProviderStatusQuery - Invalid user ID
    When I execute GetCurrentProviderStatusQuery with invalid user ID
    Then the query should fail with ValidationException

  @query @provider @staff @happy-path
  Scenario: GetProviderStaffQuery - Retrieve staff list
    Given a provider has 3 staff members
    When I execute GetProviderStaffQuery
    Then all 3 staff members should be returned

  @query @provider @business-hours @happy-path
  Scenario: GetBusinessHoursQuery - Retrieve business hours
    Given a provider has configured business hours
    When I execute GetBusinessHoursQuery
    Then hours for all 7 days should be returned

  @query @provider @holidays @happy-path
  Scenario: GetHolidaysQuery - Retrieve holidays
    Given a provider has 3 holidays configured
    When I execute GetHolidaysQuery
    Then all 3 holidays should be returned

  @query @provider @exceptions @happy-path
  Scenario: GetExceptionsQuery - Retrieve exception hours
    Given a provider has 2 exceptions configured
    When I execute GetExceptionsQuery
    Then both exceptions should be returned

  @query @provider @gallery @happy-path
  Scenario: GetGalleryImagesQuery - Retrieve gallery images
    Given a provider has 5 gallery images
    When I execute GetGalleryImagesQuery
    Then all 5 images should be returned
    And images should be ordered by display order

  # ==================== Availability Queries ====================

  @query @availability @slots @happy-path
  Scenario: GetAvailableSlotsQuery - Get available time slots
    Given a provider has availability tomorrow
    When I execute GetAvailableSlotsQuery for tomorrow
    Then available slots should be returned
    And booked slots should be excluded

  @query @availability @check @happy-path
  Scenario: CheckAvailabilityQuery - Check specific time
    Given a provider is available at 10:00 tomorrow
    When I execute CheckAvailabilityQuery for that time
    Then the result should indicate availability

  @query @availability @check @unavailable
  Scenario: CheckAvailabilityQuery - Time slot not available
    Given there is a booking at 10:00 tomorrow
    When I execute CheckAvailabilityQuery for that time
    Then the result should indicate unavailable

  # ==================== Notification Queries ====================

  @query @notification @history @happy-path
  Scenario: GetNotificationHistoryQuery - Retrieve history
    Given I have 10 notifications
    When I execute GetNotificationHistoryQuery
    Then all 10 notifications should be returned

  @query @notification @delivery-status @happy-path
  Scenario: GetDeliveryStatusQuery - Check delivery status
    Given there is a sent notification
    When I execute GetDeliveryStatusQuery
    Then the delivery status should be returned
    And timestamps should be included

  @query @notification @analytics @happy-path
  Scenario: GetNotificationAnalyticsQuery - View analytics
    Given I am authenticated as an admin
    When I execute GetNotificationAnalyticsQuery
    Then aggregated statistics should be returned
    And metrics should include sent, delivered, failed counts
