Feature: Service Command Handlers - Complete Coverage
  As a system
  I want to test all service command handlers
  So that every code path and validation is covered

  Background:
    Given a provider "Test Provider" exists with active status
    And I am authenticated as the provider

  # ==================== CreateServiceCommandHandler ====================

  @command @service @create @happy-path
  Scenario: CreateServiceCommand - Success path
    When I execute CreateServiceCommand with valid data:
      | Field                  | Value            |
      | Name                   | Haircut          |
      | Description            | Basic haircut    |
      | CategoryName           | Hair Services    |
      | BasePrice              | 50.00            |
      | Currency               | USD              |
      | DurationMinutes        | 30               |
    Then the command should succeed
    And a Service aggregate should be created
    And ServiceCreatedEvent should be published
    And the service should be saved to database

  @command @service @create @validation @provider-not-found
  Scenario: CreateServiceCommand - Provider not found
    When I execute CreateServiceCommand with non-existent provider ID
    Then the command should fail with InvalidOperationException
    And the error message should contain "Provider"
    And the error message should contain "not found"

  @command @service @create @validation @provider-inactive
  Scenario: CreateServiceCommand - Provider must be active
    Given the provider is inactive
    When I execute CreateServiceCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "Provider must be active to create services"

  @command @service @create @business-logic @deposit-enabled
  Scenario: CreateServiceCommand - Enable deposit requirement
    When I execute CreateServiceCommand with:
      | Field              | Value    |
      | RequiresDeposit    | true     |
      | DepositPercentage  | 25       |
    Then the command should succeed
    And the service should require 25% deposit

  @command @service @create @business-logic @timing-configuration
  Scenario: CreateServiceCommand - Configure preparation and buffer time
    When I execute CreateServiceCommand with:
      | Field              | Value    |
      | DurationMinutes    | 60       |
      | PreparationMinutes | 10       |
      | BufferMinutes      | 15       |
    Then the command should succeed
    And the service duration should be 60 minutes
    And preparation time should be 10 minutes
    And buffer time should be 15 minutes

  @command @service @create @business-logic @availability
  Scenario: CreateServiceCommand - Configure location and mobile availability
    When I execute CreateServiceCommand with:
      | Field               | Value  |
      | AvailableAtLocation | true   |
      | AvailableAsMobile   | true   |
    Then the command should succeed
    And the service should be available at location
    And the service should be available as mobile

  @command @service @create @business-logic @booking-rules
  Scenario: CreateServiceCommand - Configure booking rules
    When I execute CreateServiceCommand with:
      | Field                  | Value |
      | MaxAdvanceBookingDays  | 90    |
      | MinAdvanceBookingHours | 24    |
      | MaxConcurrentBookings  | 5     |
    Then the command should succeed
    And the service booking rules should be configured correctly

  @command @service @create @business-logic @with-image
  Scenario: CreateServiceCommand - Service with image URL
    When I execute CreateServiceCommand with:
      | Field    | Value                              |
      | ImageUrl | https://example.com/haircut.jpg    |
    Then the command should succeed
    And the service image should be set

  @command @service @create @validation @business-rules
  Scenario: CreateServiceCommand - Business validation fails
    When I execute CreateServiceCommand with invalid business rules
    Then the command should fail with ValidationException
    And the validation service should be invoked

  # ==================== UpdateServiceCommandHandler ====================

  @command @service @update @happy-path
  Scenario: UpdateServiceCommand - Success path
    Given there is a service "Haircut"
    When I execute UpdateServiceCommand with:
      | Field          | Value               |
      | Name           | Premium Haircut     |
      | Description    | Premium hair styling|
      | BasePrice      | 75.00               |
    Then the command should succeed
    And the service should be updated
    And ServiceUpdatedEvent should be published

  @command @service @update @validation @service-not-found
  Scenario: UpdateServiceCommand - Service not found
    When I execute UpdateServiceCommand with non-existent service ID
    Then the command should fail with InvalidServiceException
    And the error message should contain "Service not found"

  @command @service @update @business-logic @pricing
  Scenario: UpdateServiceCommand - Update pricing
    Given there is a service with price 50.00 USD
    When I execute UpdateServiceCommand with:
      | Field     | Value  |
      | BasePrice | 100.00 |
      | Currency  | USD    |
    Then the command should succeed
    And the service price should be 100.00 USD

  @command @service @update @business-logic @duration
  Scenario: UpdateServiceCommand - Update duration and timing
    Given there is a service
    When I execute UpdateServiceCommand with:
      | Field              | Value |
      | DurationMinutes    | 90    |
      | PreparationMinutes | 15    |
      | BufferMinutes      | 10    |
    Then the command should succeed
    And the service timing should be updated

  @command @service @update @business-logic @category
  Scenario: UpdateServiceCommand - Update category
    Given there is a service in "Hair Services" category
    When I execute UpdateServiceCommand with:
      | Field        | Value          |
      | CategoryName | Beauty Services|
    Then the command should succeed
    And the service category should be updated

  @command @service @update @business-logic @image
  Scenario: UpdateServiceCommand - Update service image
    Given there is a service
    When I execute UpdateServiceCommand with:
      | Field    | Value                           |
      | ImageUrl | https://example.com/new-pic.jpg |
    Then the command should succeed
    And the service image should be updated

  @command @service @update @authorization
  Scenario: UpdateServiceCommand - Only provider can update own service
    Given another provider has a service
    When I try to update the other provider's service
    Then the command should fail with ForbiddenException

  # ==================== ActivateServiceCommandHandler ====================

  @command @service @activate @happy-path
  Scenario: ActivateServiceCommand - Activate inactive service
    Given there is an inactive service
    When I execute ActivateServiceCommand
    Then the command should succeed
    And the service status should be "Active"
    And ServiceActivatedEvent should be published

  @command @service @activate @validation @service-not-found
  Scenario: ActivateServiceCommand - Service not found
    When I execute ActivateServiceCommand with non-existent service ID
    Then the command should fail with NotFoundException

  @command @service @activate @idempotency
  Scenario: ActivateServiceCommand - Already active service
    Given there is an active service
    When I execute ActivateServiceCommand
    Then the command should succeed
    And no state change should occur

  @command @service @activate @authorization
  Scenario: ActivateServiceCommand - Only provider can activate own service
    Given another provider has an inactive service
    When I try to activate the other provider's service
    Then the command should fail with ForbiddenException

  # ==================== DeactivateServiceCommandHandler ====================

  @command @service @deactivate @happy-path
  Scenario: DeactivateServiceCommand - Deactivate active service
    Given there is an active service
    When I execute DeactivateServiceCommand
    Then the command should succeed
    And the service status should be "Inactive"
    And ServiceDeactivatedEvent should be published

  @command @service @deactivate @validation @service-not-found
  Scenario: DeactivateServiceCommand - Service not found
    When I execute DeactivateServiceCommand with non-existent service ID
    Then the command should fail with NotFoundException

  @command @service @deactivate @business-logic @upcoming-bookings
  Scenario: DeactivateServiceCommand - Service with upcoming bookings
    Given there is a service with 3 upcoming bookings
    When I execute DeactivateServiceCommand
    Then the command should succeed
    And the service should be deactivated
    And existing bookings should remain valid

  @command @service @deactivate @authorization
  Scenario: DeactivateServiceCommand - Only provider can deactivate own service
    Given another provider has a service
    When I try to deactivate the other provider's service
    Then the command should fail with ForbiddenException

  # ==================== ArchiveServiceCommandHandler ====================

  @command @service @archive @happy-path
  Scenario: ArchiveServiceCommand - Archive service
    Given there is a service with no upcoming bookings
    When I execute ArchiveServiceCommand
    Then the command should succeed
    And the service status should be "Archived"
    And ServiceArchivedEvent should be published

  @command @service @archive @validation @service-not-found
  Scenario: ArchiveServiceCommand - Service not found
    When I execute ArchiveServiceCommand with non-existent service ID
    Then the command should fail with NotFoundException

  @command @service @archive @validation @has-upcoming-bookings
  Scenario: ArchiveServiceCommand - Cannot archive with upcoming bookings
    Given there is a service with upcoming bookings
    When I execute ArchiveServiceCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "upcoming bookings"

  @command @service @archive @authorization
  Scenario: ArchiveServiceCommand - Only provider can archive own service
    Given another provider has a service
    When I try to archive the other provider's service
    Then the command should fail with ForbiddenException

  # ==================== AddPriceTierCommandHandler ====================

  @command @service @price-tier @add @happy-path
  Scenario: AddPriceTierCommand - Add price tier to service
    Given there is a service
    When I execute AddPriceTierCommand with:
      | Field       | Value          |
      | TierName    | Premium        |
      | Price       | 100.00         |
      | Currency    | USD            |
      | Description | Premium option |
    Then the command should succeed
    And the service should have a "Premium" price tier
    And PriceTierAddedEvent should be published

  @command @service @price-tier @add @validation @service-not-found
  Scenario: AddPriceTierCommand - Service not found
    When I execute AddPriceTierCommand with non-existent service ID
    Then the command should fail with NotFoundException

  @command @service @price-tier @add @validation @duplicate-tier
  Scenario: AddPriceTierCommand - Duplicate tier name
    Given there is a service with a "Premium" price tier
    When I execute AddPriceTierCommand with tier name "Premium"
    Then the command should fail with ConflictException
    And the error message should contain "already exists"

  @command @service @price-tier @add @validation @currency-mismatch
  Scenario: AddPriceTierCommand - Tier currency must match service
    Given there is a service priced in USD
    When I execute AddPriceTierCommand with currency EUR
    Then the command should fail with ValidationException
    And the error message should contain "currency"

  # ==================== SetServiceAvailabilityCommandHandler ====================

  @command @service @availability @set @happy-path
  Scenario: SetServiceAvailabilityCommand - Set availability rules
    Given there is a service
    When I execute SetServiceAvailabilityCommand with:
      | Field                  | Value   |
      | AvailableAtLocation    | true    |
      | AvailableAsMobile      | true    |
      | MaxDailyBookings       | 10      |
      | MaxConcurrentBookings  | 3       |
    Then the command should succeed
    And the service availability rules should be updated
    And ServiceAvailabilityChangedEvent should be published

  @command @service @availability @set @validation @service-not-found
  Scenario: SetServiceAvailabilityCommand - Service not found
    When I execute SetServiceAvailabilityCommand with non-existent service ID
    Then the command should fail with NotFoundException

  @command @service @availability @set @validation @both-disabled
  Scenario: SetServiceAvailabilityCommand - Cannot disable both location and mobile
    Given there is a service
    When I execute SetServiceAvailabilityCommand with:
      | Field               | Value |
      | AvailableAtLocation | false |
      | AvailableAsMobile   | false |
    Then the command should fail with ValidationException
    And the error message should contain "at least one availability option"

  @command @service @availability @set @authorization
  Scenario: SetServiceAvailabilityCommand - Only provider can set availability
    Given another provider has a service
    When I try to set availability for the other provider's service
    Then the command should fail with ForbiddenException

  # ==================== AddProviderServiceCommandHandler ====================

  @command @service @provider-service @add @happy-path
  Scenario: AddProviderServiceCommand - Add service to provider catalog
    Given there is a global service template "Haircut"
    When I execute AddProviderServiceCommand to add it to my catalog
    Then the command should succeed
    And the service should be added to provider's catalog
    And I can customize the pricing

  @command @service @provider-service @add @validation @service-not-found
  Scenario: AddProviderServiceCommand - Service template not found
    When I execute AddProviderServiceCommand with non-existent service template
    Then the command should fail with NotFoundException

  @command @service @provider-service @add @validation @already-exists
  Scenario: AddProviderServiceCommand - Service already in catalog
    Given I have "Haircut" service in my catalog
    When I execute AddProviderServiceCommand to add "Haircut" again
    Then the command should fail with ConflictException

  # ==================== UpdateProviderServiceCommandHandler ====================

  @command @service @provider-service @update @happy-path
  Scenario: UpdateProviderServiceCommand - Update provider-specific service details
    Given I have a service in my catalog
    When I execute UpdateProviderServiceCommand with:
      | Field          | Value             |
      | CustomPrice    | 80.00             |
      | CustomDuration | 45                |
    Then the command should succeed
    And the provider-specific details should be updated

  @command @service @provider-service @update @validation @service-not-found
  Scenario: UpdateProviderServiceCommand - Service not in provider catalog
    When I execute UpdateProviderServiceCommand with non-existent service
    Then the command should fail with NotFoundException

  # ==================== DeleteProviderServiceCommandHandler ====================

  @command @service @provider-service @delete @happy-path
  Scenario: DeleteProviderServiceCommand - Remove service from catalog
    Given I have a service with no upcoming bookings
    When I execute DeleteProviderServiceCommand
    Then the command should succeed
    And the service should be removed from my catalog
    And ServiceRemovedEvent should be published

  @command @service @provider-service @delete @validation @has-bookings
  Scenario: DeleteProviderServiceCommand - Cannot delete with upcoming bookings
    Given I have a service with upcoming bookings
    When I execute DeleteProviderServiceCommand
    Then the command should fail with InvalidOperationException
    And the error message should contain "upcoming bookings"

  @command @service @provider-service @delete @soft-delete
  Scenario: DeleteProviderServiceCommand - Soft delete preserves history
    Given I have a service with completed bookings
    When I execute DeleteProviderServiceCommand
    Then the command should succeed
    And the service should be soft deleted
    And historical bookings should remain accessible
