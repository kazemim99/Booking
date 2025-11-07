# Reqnroll Migration Plan for Integration Tests

## Executive Summary

This document outlines the comprehensive plan to migrate existing xUnit integration tests to Reqnroll (BDD/Gherkin), ensuring tests are well-documented and accurately simulate frontend requests and responses.

**Migration Timeline:** Phased approach over 4-6 weeks
**Target Framework:** .NET 9.0
**Testing Framework:** Reqnroll (successor to SpecFlow)

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Benefits of Reqnroll Migration](#benefits-of-reqnroll-migration)
3. [Migration Strategy](#migration-strategy)
4. [Technical Implementation](#technical-implementation)
5. [Feature File Structure](#feature-file-structure)
6. [Step Definition Patterns](#step-definition-patterns)
7. [Migration Phases](#migration-phases)
8. [Testing Best Practices](#testing-best-practices)
9. [Rollout Plan](#rollout-plan)

---

## Current State Analysis

### Existing Test Infrastructure

**Test Framework:** xUnit 2.9.2
**Assertion Library:** FluentAssertions 8.7.0
**Target Framework:** .NET 9.0
**Test Count:** 29 test files (16 integration, 12 domain unit, 1 application unit)

### Test Categories

#### Integration Tests (16 files)
- **API/Bookings:** BookingsControllerTests.cs
- **API/Payments:** PaymentsControllerTests.cs, PayoutsControllerTests.cs, FinancialControllerTests.cs
- **API/Notifications:** NotificationsControllerTests.cs
- **Provider Management:** ProvidersControllerTests.cs, ProviderManagementTests.cs, ProviderSettingsTests.cs, ProviderStaffTests.cs, ProviderSettingsControllerTests.cs
- **Service Management:** ServiceManagementTests.cs, AvailabilityControllerTests.cs
- **Gallery:** GalleryManagementTests.cs
- **Registration:** ProgressiveRegistrationTests.cs, StepBasedRegistrationTests.cs
- **Working Hours:** WorkingHoursManagementTests.cs

### Current Test Patterns

```csharp
[Fact]
public async Task CreateBooking_WithValidData_ShouldReturn201Created()
{
    // Arrange
    var provider = await CreateTestProviderWithServicesAsync();
    var customerId = Guid.NewGuid();
    AuthenticateAsUser(customerId, "customer@test.com");

    var request = new CreateBookingRequest { /* ... */ };

    // Act
    var response = await PostAsJsonAsync<CreateBookingRequest, BookingResponse>(
        "/api/v1/bookings", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    response.Data.Should().NotBeNull();
    // ... more assertions
}
```

### Key Infrastructure Components

1. **Base Classes:**
   - `IntegrationTestBase<TFactory, TDbContext, TStartup>` - Generic base for all integration tests
   - `ServiceCatalogIntegrationTestBase` - Service Catalog-specific helpers

2. **Helper Methods:**
   - Authentication: `AuthenticateAsUser`, `AuthenticateAsProvider`, `AuthenticateAsAdmin`, `ClearAuthentication`
   - API calls: `PostAsJsonAsync`, `GetAsync`, `PutAsJsonAsync`, `DeleteAsync`
   - Entity creation: `CreateEntityAsync`, `CreateTestProviderWithServicesAsync`, `CreateServiceForProviderAsync`
   - Assertions: `AssertServiceExistsAsync`, `AssertProviderExistsAsync`, `AssertNotificationStatusAsync`

3. **Authentication Context:**
   - `TestUserContext` - Simulates authenticated users in tests
   - `TestUser` - Represents customer, provider, or admin users

4. **Response Handling:**
   - Custom `ApiResponse` and `ApiResponse<T>` wrappers
   - Newtonsoft.Json for serialization
   - Automatic error handling for 500 responses

---

## Benefits of Reqnroll Migration

### 1. **Living Documentation**
- Feature files serve as executable specifications
- Business stakeholders can read and understand test scenarios
- Gherkin syntax bridges technical and non-technical teams

### 2. **Better Frontend Simulation**
- Scenarios describe user journeys from frontend perspective
- Given-When-Then structure maps directly to user actions
- Data tables for complex request/response validation

### 3. **Improved Maintainability**
- Step definitions reused across multiple scenarios
- Changes to API structure require updating step definitions only
- Clear separation between test logic (steps) and test scenarios (features)

### 4. **Enhanced Traceability**
- Features map directly to user stories/requirements
- Tags enable selective test execution
- Scenario outlines for data-driven testing

### 5. **Collaboration Benefits**
- Product owners can review scenarios before implementation
- QA team can write scenarios without deep C# knowledge
- Developers implement step definitions with full API knowledge

---

## Migration Strategy

### Approach: **Gradual Migration with Coexistence**

We will migrate tests incrementally, allowing xUnit and Reqnroll tests to coexist during the transition period.

### Migration Priority

**Phase 1: High-Value API Scenarios (Weeks 1-2)**
- Booking lifecycle (create, confirm, cancel, reschedule)
- Payment processing (authorize, capture, refund)
- Provider registration (simple and progressive)

**Phase 2: Provider Management (Weeks 3-4)**
- Service management CRUD
- Staff management
- Working hours configuration
- Gallery management

**Phase 3: Complex Workflows (Weeks 5-6)**
- Notification delivery flows
- Financial transactions and payouts
- Search and filtering
- Authorization scenarios

**Phase 4: Edge Cases and Error Scenarios**
- Validation failures
- Unauthorized access attempts
- Concurrent operations
- Data consistency checks

### Migration Criteria

A test is a good candidate for migration if:
- ✅ It tests a complete user journey or workflow
- ✅ It involves multiple API calls
- ✅ It has clear business value
- ✅ It represents frontend user interactions

Tests that should remain as xUnit:
- ❌ Pure unit tests (domain logic, value objects)
- ❌ Infrastructure tests (database migrations, etc.)
- ❌ Performance/load tests
- ❌ Tests with complex setup that doesn't represent user flow

---

## Technical Implementation

### Required NuGet Packages

Add to `Booksy.ServiceCatalog.IntegrationTests.csproj`:

```xml
<PackageReference Include="Reqnroll" Version="2.2.0" />
<PackageReference Include="Reqnroll.xUnit" Version="2.2.0" />
<PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="2.2.0" />
```

### Project Structure

```
tests/Booksy.ServiceCatalog.IntegrationTests/
├── Features/
│   ├── Bookings/
│   │   ├── CreateBooking.feature
│   │   ├── CancelBooking.feature
│   │   └── RescheduleBooking.feature
│   ├── Payments/
│   │   ├── ProcessPayment.feature
│   │   ├── CapturePayment.feature
│   │   └── RefundPayment.feature
│   ├── Providers/
│   │   ├── RegisterProvider.feature
│   │   ├── ProviderSearch.feature
│   │   └── ProviderStatus.feature
│   └── Services/
│       ├── CreateService.feature
│       └── ManageService.feature
├── StepDefinitions/
│   ├── Common/
│   │   ├── AuthenticationSteps.cs
│   │   ├── CommonApiSteps.cs
│   │   └── TestDataSteps.cs
│   ├── Bookings/
│   │   └── BookingSteps.cs
│   ├── Payments/
│   │   └── PaymentSteps.cs
│   ├── Providers/
│   │   └── ProviderSteps.cs
│   └── Services/
│       └── ServiceSteps.cs
├── Hooks/
│   ├── TestHooks.cs
│   └── ScenarioContext.cs
├── Support/
│   ├── ScenarioContextExtensions.cs
│   ├── RequestBuilders.cs
│   └── ResponseAssertions.cs
└── reqnroll.json (configuration)
```

### Reqnroll Configuration

Create `reqnroll.json`:

```json
{
  "language": {
    "feature": "en"
  },
  "bindingCulture": {
    "name": "en-US"
  },
  "generator": {
    "allowDebugGeneratedFiles": true,
    "allowRowTests": true
  },
  "runtime": {
    "stopAtFirstError": false,
    "missingOrPendingStepsOutcome": "Inconclusive"
  },
  "trace": {
    "traceSuccessfulSteps": true,
    "traceTimings": true,
    "minTracedDuration": "0:0:0.1"
  }
}
```

---

## Feature File Structure

### Basic Feature Template

```gherkin
Feature: Booking Management
  As a customer
  I want to create, view, and manage my bookings
  So that I can schedule appointments with service providers

  Background:
    Given a provider "Beauty Salon Alpha" exists with the following details:
      | BusinessName       | Beauty Salon Alpha       |
      | Type               | Salon                    |
      | Status             | Active                   |
    And the provider has a service "Haircut" with:
      | Name      | Haircut |
      | Price     | 50.00   |
      | Duration  | 60      |
      | Currency  | USD     |
    And the provider has business hours:
      | Day       | OpenTime | CloseTime |
      | Monday    | 09:00    | 17:00     |
      | Tuesday   | 09:00    | 17:00     |
      | Wednesday | 09:00    | 17:00     |

  @smoke @booking
  Scenario: Customer creates a valid booking
    Given I am authenticated as customer "john@example.com"
    When I send a POST request to "/api/v1/bookings" with:
      | ProviderId  | [Provider:Beauty Salon Alpha:Id] |
      | ServiceId   | [Service:Haircut:Id]             |
      | StartTime   | 2025-11-10T10:00:00Z             |
      | Notes       | First time customer              |
    Then the response status code should be "201"
    And the response should contain:
      | Field      | Value                              |
      | Status     | Requested                          |
      | ProviderId | [Provider:Beauty Salon Alpha:Id]   |
      | ServiceId  | [Service:Haircut:Id]               |
    And a booking should exist in the database with status "Requested"
    And the customer should receive a confirmation email

  @negative @booking
  Scenario: Customer cannot create booking without authentication
    Given I am not authenticated
    When I send a POST request to "/api/v1/bookings" with:
      | ProviderId  | [Provider:Beauty Salon Alpha:Id] |
      | ServiceId   | [Service:Haircut:Id]             |
      | StartTime   | 2025-11-10T10:00:00Z             |
    Then the response status code should be "401"
    And the response should contain error "Unauthorized"

  @negative @booking
  Scenario: Customer cannot create booking in the past
    Given I am authenticated as customer "john@example.com"
    When I send a POST request to "/api/v1/bookings" with:
      | ProviderId  | [Provider:Beauty Salon Alpha:Id] |
      | ServiceId   | [Service:Haircut:Id]             |
      | StartTime   | 2020-01-01T10:00:00Z             |
    Then the response status code should be "400"
    And the response should contain validation error for "StartTime"

  @booking
  Scenario Outline: Customer books at different times
    Given I am authenticated as customer "john@example.com"
    When I send a POST request to "/api/v1/bookings" with:
      | ProviderId  | [Provider:Beauty Salon Alpha:Id] |
      | ServiceId   | [Service:Haircut:Id]             |
      | StartTime   | <StartTime>                      |
    Then the response status code should be "<StatusCode>"

    Examples:
      | StartTime            | StatusCode |
      | 2025-11-10T10:00:00Z | 201        |
      | 2025-11-10T14:00:00Z | 201        |
      | 2025-11-10T18:00:00Z | 400        |
      | 2020-01-01T10:00:00Z | 400        |
```

### Payment Feature Example

```gherkin
Feature: Payment Processing
  As a customer
  I want to process payments for my bookings
  So that I can secure my appointments

  Background:
    Given a provider "Test Provider" exists with active status
    And the provider has a service "Haircut" priced at 100.00 USD
    And I have a confirmed booking for "Haircut"
    And I am authenticated as the booking customer

  @smoke @payment
  Scenario: Process payment with immediate capture
    When I send a POST request to "/api/v1/payments" with:
      | BookingId         | [Booking:Current:Id]   |
      | ProviderId        | [Provider:Current:Id]  |
      | Amount            | 100.00                 |
      | Currency          | USD                    |
      | PaymentMethod     | CreditCard             |
      | PaymentMethodId   | pm_test_card           |
      | CaptureImmediately| true                   |
    Then the response status code should be "201"
    And the response should contain:
      | Field    | Value |
      | Status   | Paid  |
      | Amount   | 100.00|
      | Currency | USD   |
    And a payment should exist in the database with status "Paid"

  @payment
  Scenario: Authorize payment without immediate capture
    When I send a POST request to "/api/v1/payments" with:
      | BookingId         | [Booking:Current:Id]   |
      | ProviderId        | [Provider:Current:Id]  |
      | Amount            | 100.00                 |
      | Currency          | USD                    |
      | PaymentMethod     | CreditCard             |
      | PaymentMethodId   | pm_test_card           |
      | CaptureImmediately| false                  |
    Then the response status code should be "201"
    And the response should contain:
      | Field    | Value      |
      | Status   | Paid       |
    When I send a POST request to "/api/v1/payments/[Payment:Current:Id]/capture" with:
      | Amount | 100.00 |
    Then the response status code should be "200"
    And the payment status should be "Paid"

  @negative @payment
  Scenario: Cannot process payment with invalid amount
    When I send a POST request to "/api/v1/payments" with:
      | BookingId         | [Booking:Current:Id]   |
      | ProviderId        | [Provider:Current:Id]  |
      | Amount            | -50.00                 |
      | Currency          | USD                    |
      | PaymentMethod     | CreditCard             |
      | PaymentMethodId   | pm_test_card           |
      | CaptureImmediately| true                   |
    Then the response status code should be "400"
    And the response should contain validation error for "Amount"
```

---

## Step Definition Patterns

### Authentication Steps

```csharp
[Binding]
public class AuthenticationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;

    public AuthenticationSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
    }

    [Given(@"I am authenticated as customer ""(.*)""")]
    public void GivenIAmAuthenticatedAsCustomer(string email)
    {
        var userId = Guid.NewGuid();
        _testBase.AuthenticateAsUser(userId, email);
        _scenarioContext.Set(userId, "CurrentUserId");
        _scenarioContext.Set(email, "CurrentUserEmail");
        _scenarioContext.Set("Customer", "CurrentUserRole");
    }

    [Given(@"I am authenticated as provider ""(.*)""")]
    public void GivenIAmAuthenticatedAsProvider(string email)
    {
        var providerId = _scenarioContext.Get<Guid>("CurrentProviderId");
        _testBase.AuthenticateAsProvider(providerId.ToString(), email);
        _scenarioContext.Set("Provider", "CurrentUserRole");
    }

    [Given(@"I am authenticated as admin")]
    public void GivenIAmAuthenticatedAsAdmin()
    {
        _testBase.AuthenticateAsTestAdmin();
        _scenarioContext.Set("Admin", "CurrentUserRole");
    }

    [Given(@"I am not authenticated")]
    public void GivenIAmNotAuthenticated()
    {
        _testBase.ClearAuthenticationHeader();
        _scenarioContext.Remove("CurrentUserId");
        _scenarioContext.Remove("CurrentUserRole");
    }
}
```

### API Request Steps

```csharp
[Binding]
public class CommonApiSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;

    public CommonApiSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
    }

    [When(@"I send a POST request to ""(.*)"" with:")]
    public async Task WhenISendAPostRequestWithData(string endpoint, Table table)
    {
        // Build request object from table
        var requestData = BuildRequestFromTable(table);

        // Replace placeholders like [Provider:Current:Id]
        var processedEndpoint = ReplaceContextPlaceholders(endpoint);

        // Send request
        var response = await _testBase.PostAsJsonAsync(processedEndpoint, requestData);

        // Store response in context
        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequest(string endpoint)
    {
        var processedEndpoint = ReplaceContextPlaceholders(endpoint);
        var response = await _testBase.GetAsync(processedEndpoint);

        _scenarioContext.Set(response, "LastHttpResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [Then(@"the response status code should be ""(.*)""")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        var actualStatusCode = _scenarioContext.Get<HttpStatusCode>("LastStatusCode");
        actualStatusCode.Should().Be((HttpStatusCode)expectedStatusCode);
    }

    [Then(@"the response should contain:")]
    public void ThenTheResponseShouldContain(Table table)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = row["Value"];

            // Use reflection or dynamic to check field values
            var actualValue = GetFieldValue(response.Data, field);
            actualValue.Should().Be(expectedValue);
        }
    }

    [Then(@"the response should contain validation error for ""(.*)""")]
    public void ThenTheResponseShouldContainValidationError(string fieldName)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");
        response.Errors.Should().ContainKey(fieldName);
    }

    private object BuildRequestFromTable(Table table)
    {
        var request = new Dictionary<string, object>();

        foreach (var row in table.Rows)
        {
            var key = row.Keys.First();
            var value = row[key];

            // Replace context placeholders
            value = ReplaceContextPlaceholders(value);

            // Convert to appropriate type
            request[key] = ConvertValueToType(value);
        }

        return request;
    }

    private string ReplaceContextPlaceholders(string value)
    {
        // Replace patterns like [Provider:Current:Id]
        var regex = new Regex(@"\[(\w+):(\w+):(\w+)\]");

        return regex.Replace(value, match =>
        {
            var entityType = match.Groups[1].Value;
            var identifier = match.Groups[2].Value;
            var property = match.Groups[3].Value;

            var contextKey = $"{entityType}:{identifier}";

            if (_scenarioContext.ContainsKey(contextKey))
            {
                var entity = _scenarioContext.Get<object>(contextKey);
                return GetPropertyValue(entity, property)?.ToString() ?? value;
            }

            return value;
        });
    }
}
```

### Test Data Setup Steps

```csharp
[Binding]
public class TestDataSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;

    public TestDataSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
    }

    [Given(@"a provider ""(.*)"" exists with the following details:")]
    public async Task GivenAProviderExistsWithDetails(string providerName, Table table)
    {
        var businessName = table.Rows[0]["BusinessName"];
        var type = Enum.Parse<ProviderType>(table.Rows[0]["Type"]);
        var status = Enum.Parse<ProviderStatus>(table.Rows[0]["Status"]);

        var provider = await _testBase.CreateTestProviderWithServicesAsync();

        // Store in context for later reference
        _scenarioContext.Set(provider, $"Provider:{providerName}");
        _scenarioContext.Set(provider.Id.Value, "CurrentProviderId");
    }

    [Given(@"the provider has a service ""(.*)"" with:")]
    public async Task GivenTheProviderHasAService(string serviceName, Table table)
    {
        var providerId = _scenarioContext.Get<Guid>("CurrentProviderId");
        var provider = await _testBase.FindProviderAsync(providerId);

        var name = table.Rows[0]["Name"];
        var price = decimal.Parse(table.Rows[0]["Price"]);
        var duration = int.Parse(table.Rows[0]["Duration"]);

        var service = await _testBase.CreateServiceForProviderAsync(
            provider, name, price, duration);

        _scenarioContext.Set(service, $"Service:{serviceName}");
    }

    [Given(@"I have a confirmed booking for ""(.*)""")]
    public async Task GivenIHaveAConfirmedBooking(string serviceName)
    {
        var service = _scenarioContext.Get<Service>($"Service:{serviceName}");
        var customerId = _scenarioContext.Get<Guid>("CurrentUserId");

        // Create booking through domain model
        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId),
            service.ProviderId,
            service.Id,
            /* ... */);

        booking.Confirm();
        await _testBase.CreateEntityAsync(booking);

        _scenarioContext.Set(booking, "Booking:Current");
    }
}
```

### Database Assertion Steps

```csharp
[Binding]
public class DatabaseAssertionSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogIntegrationTestBase _testBase;

    public DatabaseAssertionSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogIntegrationTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
    }

    [Then(@"a booking should exist in the database with status ""(.*)""")]
    public async Task ThenABookingShouldExistWithStatus(string expectedStatus)
    {
        var response = _scenarioContext.Get<ApiResponse<BookingResponse>>("LastResponse");
        var bookingId = response.Data.Id;

        var booking = await _testBase.DbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == BookingId.From(bookingId));

        booking.Should().NotBeNull();
        booking.Status.ToString().Should().Be(expectedStatus);
    }

    [Then(@"a payment should exist in the database with status ""(.*)""")]
    public async Task ThenAPaymentShouldExistWithStatus(string expectedStatus)
    {
        var response = _scenarioContext.Get<ApiResponse<PaymentResponse>>("LastResponse");
        var paymentId = response.Data.PaymentId;

        var payment = await _testBase.DbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(paymentId));

        payment.Should().NotBeNull();
        payment.Status.ToString().Should().Be(expectedStatus);
    }

    [Then(@"the customer should receive a confirmation email")]
    public async Task ThenTheCustomerShouldReceiveConfirmationEmail()
    {
        var customerId = _scenarioContext.Get<Guid>("CurrentUserId");

        var notifications = await _testBase.GetUserNotificationsAsync(customerId);

        notifications.Should().Contain(n =>
            n.Type == NotificationType.BookingConfirmation &&
            n.Channel == NotificationChannel.Email);
    }
}
```

---

## Migration Phases

### Phase 1: Infrastructure Setup (Week 1)

**Tasks:**
1. ✅ Install Reqnroll NuGet packages
2. ✅ Create project structure (Features, StepDefinitions, Hooks, Support)
3. ✅ Configure reqnroll.json
4. ✅ Create base hook classes for test initialization/cleanup
5. ✅ Implement ScenarioContext extensions
6. ✅ Create common step definitions (authentication, API requests)
7. ✅ Set up CI/CD integration for Reqnroll tests

**Deliverables:**
- Working Reqnroll infrastructure
- One "Hello World" feature to validate setup
- Documentation for running Reqnroll tests

### Phase 2: Core Booking Scenarios (Week 2)

**Features to Migrate:**
- Create booking (happy path + negative scenarios)
- Cancel booking
- Reschedule booking
- Get booking details
- List customer bookings

**Step Definitions:**
- BookingSteps.cs
- Test data builders for bookings

### Phase 3: Payment Processing (Week 3)

**Features to Migrate:**
- Process payment with immediate capture
- Authorize and capture payment
- Partial capture
- Full refund
- Partial refund
- Payment validation errors

**Step Definitions:**
- PaymentSteps.cs
- Payment assertion helpers

### Phase 4: Provider Management (Week 4)

**Features to Migrate:**
- Provider registration (simple and full)
- Provider search
- Provider activation/deactivation
- Get provider status
- Update provider profile

**Step Definitions:**
- ProviderSteps.cs
- Provider data builders

### Phase 5: Service Management (Week 5)

**Features to Migrate:**
- Create service
- Update service
- Delete service
- Service availability
- Working hours configuration

**Step Definitions:**
- ServiceSteps.cs
- Availability calculation helpers

### Phase 6: Complex Workflows (Week 6)

**Features to Migrate:**
- Progressive registration flow
- Notification delivery
- Financial reporting
- Multi-step workflows

**Step Definitions:**
- Complex scenario orchestration
- End-to-end workflow helpers

---

## Testing Best Practices

### Gherkin Best Practices

1. **Use business language, not technical language**
   - ✅ Given I am authenticated as customer "john@example.com"
   - ❌ Given the JWT token is set in the Authorization header

2. **Keep scenarios focused and independent**
   - Each scenario should test one specific behavior
   - Scenarios should not depend on each other

3. **Use descriptive scenario names**
   - ✅ Scenario: Customer cannot create booking without authentication
   - ❌ Scenario: Test booking creation

4. **Leverage Background for common setup**
   - Put shared Given steps in Background
   - Keeps scenarios DRY (Don't Repeat Yourself)

5. **Use Scenario Outlines for data variations**
   - Test multiple input combinations efficiently
   - Keep examples table readable

6. **Tag scenarios appropriately**
   - @smoke - Critical path scenarios
   - @regression - Full regression suite
   - @negative - Error/validation scenarios
   - @wip - Work in progress
   - @ignore - Temporarily disabled

### Step Definition Best Practices

1. **Keep step definitions reusable**
   - Generic steps work across multiple scenarios
   - Use parameters and tables for flexibility

2. **Use ScenarioContext wisely**
   - Store entities/data needed across steps
   - Clear naming conventions for context keys

3. **Separate concerns**
   - API steps handle HTTP requests/responses
   - Database steps handle DB assertions
   - Test data steps handle entity creation

4. **Handle asynchronous operations properly**
   - All API calls are async/await
   - Database queries are async

5. **Provide clear error messages**
   - FluentAssertions with custom messages
   - Log request/response for debugging

---

## Rollout Plan

### Week 1: Infrastructure + Pilot
- Set up Reqnroll infrastructure
- Migrate 2-3 simple booking scenarios
- Validate approach with team
- Gather feedback

### Weeks 2-3: Core Scenarios
- Migrate booking lifecycle
- Migrate payment processing
- Build reusable step definitions
- Document patterns for team

### Weeks 4-5: Expand Coverage
- Migrate provider management
- Migrate service management
- Continue building step library
- Regular team review sessions

### Week 6: Complex Workflows + Polish
- Migrate complex multi-step scenarios
- Complete documentation
- Training sessions for team
- Establish maintenance processes

### Post-Migration
- Remove/archive old xUnit integration tests (after validation period)
- Continuous improvement of step definitions
- Regular grooming of feature files
- Keep scenarios aligned with product changes

---

## Success Criteria

✅ **Technical Success:**
- All critical user journeys covered by Reqnroll scenarios
- 90%+ pass rate on first execution
- Tests run in CI/CD pipeline
- Average scenario execution time < 5 seconds

✅ **Team Success:**
- All developers can write new scenarios
- Product owners can read and understand scenarios
- QA team can contribute to scenario writing
- Feature files used as living documentation

✅ **Quality Success:**
- Improved test coverage of frontend interactions
- Earlier defect detection
- Reduced manual testing effort
- Better traceability to requirements

---

## Appendices

### A. Reqnroll vs SpecFlow

Reqnroll is the successor to SpecFlow with:
- Native support for .NET 6+
- Better performance
- Open-source with active community
- Compatible with existing SpecFlow syntax

### B. Useful Resources

- Reqnroll Documentation: https://docs.reqnroll.net/
- Gherkin Reference: https://cucumber.io/docs/gherkin/reference/
- Best Practices: https://cucumber.io/docs/bdd/better-gherkin/

### C. Team Contacts

- **Migration Lead:** [Your Name]
- **Technical Questions:** Development Team
- **Scenario Review:** Product Team + QA Team

---

**Document Version:** 1.0
**Last Updated:** 2025-11-06
**Status:** Ready for Review and Implementation
