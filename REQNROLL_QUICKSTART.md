# Reqnroll Quick Start Guide

## What is Reqnroll?

Reqnroll is a BDD (Behavior-Driven Development) testing framework for .NET that allows you to write tests in plain English using the Gherkin syntax. Tests are written as "feature files" that describe behavior from a user's perspective.

## Installation

Reqnroll has already been installed in the project. The following packages were added:

```xml
<PackageReference Include="Reqnroll" Version="2.2.0" />
<PackageReference Include="Reqnroll.xUnit" Version="2.2.0" />
<PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="2.2.0" />
```

## Running Tests

### Run All Tests (xUnit + Reqnroll)
```bash
dotnet test
```

### Run Only Reqnroll Tests
```bash
dotnet test --filter "FullyQualifiedName~Feature"
```

### Run Specific Tag
```bash
# Run only smoke tests
dotnet test --filter "Category=smoke"

# Run booking scenarios
dotnet test --filter "Category=booking"

# Run payment scenarios
dotnet test --filter "Category=payment"
```

### Run Specific Feature
```bash
dotnet test --filter "FullyQualifiedName~CreateBooking"
```

## Project Structure

```
tests/Booksy.ServiceCatalog.IntegrationTests/
├── Features/                    # Gherkin feature files
│   ├── Bookings/
│   │   ├── CreateBooking.feature
│   │   └── CancelBooking.feature
│   └── Payments/
│       ├── ProcessPayment.feature
│       └── RefundPayment.feature
├── StepDefinitions/             # C# step implementations
│   ├── Common/
│   │   ├── AuthenticationSteps.cs
│   │   └── TestDataSteps.cs
│   ├── Bookings/
│   │   └── BookingSteps.cs
│   └── Payments/
│       └── PaymentSteps.cs
├── Hooks/                       # Test lifecycle hooks
│   └── TestHooks.cs
├── Support/                     # Helper utilities
│   └── ScenarioContextHelper.cs
└── reqnroll.json               # Reqnroll configuration
```

## Feature File Basics

### Feature File Structure

```gherkin
Feature: Feature Name
  Short description of the feature

  Background:
    Given common setup step
    And another common setup step

  @tag1 @tag2
  Scenario: Scenario description
    Given precondition
    When action
    Then expected result
    And another expected result
```

### Example: Create Booking Feature

```gherkin
Feature: Create Booking
  As a customer
  I want to create bookings for services
  So that I can schedule appointments

  Background:
    Given a provider "Beauty Salon" exists
    And the provider has a service "Haircut" priced at 50.00 USD

  @smoke @booking
  Scenario: Customer creates a valid booking
    Given I am authenticated as a customer
    When I send a POST request to create a booking with:
      | Field     | Value                        |
      | ServiceId | [Service:Haircut:Id]         |
      | StartTime | 2 days from now at 10:00     |
    Then the response status code should be 201
    And the booking should exist in the database
```

## Common Step Patterns

### Authentication Steps

```gherkin
Given I am authenticated as a customer
Given I am authenticated as the provider
Given I am authenticated as an admin
Given I am not authenticated
```

### Test Data Setup Steps

```gherkin
Given a provider "Name" exists with active status
Given the provider has a service "ServiceName" priced at 50.00 USD
Given the provider has business hours configured
Given I have a confirmed booking for "ServiceName"
```

### API Request Steps

```gherkin
When I send a POST request to create a booking with:
  | Field  | Value |
  | ...    | ...   |

When I send a POST request to cancel the booking with:
  | Field  | Value |
  | ...    | ...   |
```

### Assertion Steps

```gherkin
Then the response status code should be 201
Then the response should contain a booking with:
  | Field  | Value |
  | Status | Requested |

Then the booking should exist in the database with status "Requested"
```

## Data Tables

Data tables allow you to pass structured data to steps:

```gherkin
When I send a POST request to create a booking with:
  | Field      | Value                    |
  | ServiceId  | [Service:Haircut:Id]     |
  | StartTime  | 2 days from now at 10:00 |
  | Notes      | First time customer      |
```

## Scenario Outlines (Data-Driven Tests)

Test the same scenario with multiple data sets:

```gherkin
Scenario Outline: Customer books at different times
  Given I am authenticated as a customer
  When I create a booking at <StartTime>
  Then the response status code should be <StatusCode>

  Examples:
    | StartTime                | StatusCode |
    | 2 days from now at 10:00 | 201        |
    | 2 days from now at 18:00 | 400        |
    | 1 day ago at 10:00       | 400        |
```

## Context Placeholders

Use placeholders to reference entities created in previous steps:

```gherkin
# Create a service
Given the provider has a service "Haircut" priced at 50.00 USD

# Reference the service ID later
When I create a booking for service [Service:Haircut:Id]
```

**Supported patterns:**
- `[Provider:Name:Property]` - Provider properties
- `[Service:Name:Property]` - Service properties
- `[Booking:Current:Property]` - Current booking properties
- `[Payment:Current:Property]` - Current payment properties

## Relative Time Expressions

Instead of hardcoding dates, use relative expressions:

```gherkin
# Future times
StartTime: 2 days from now at 10:00
StartTime: tomorrow at 14:00

# Past times
StartTime: 1 day ago at 10:00
StartTime: 3 days ago at 09:00
```

## Tags

Tags organize and filter scenarios:

```gherkin
@smoke @booking @create
Scenario: Create booking
  ...
```

**Common tags:**
- `@smoke` - Critical path tests
- `@booking`, `@payment`, `@provider` - Feature area
- `@create`, `@cancel`, `@refund` - Operation type
- `@negative` - Error/validation scenarios
- `@authorization` - Permission tests

**Run by tag:**
```bash
dotnet test --filter "Category=smoke"
dotnet test --filter "Category=booking&Category=negative"
```

## Writing New Scenarios

### 1. Create Feature File

Create a new `.feature` file in the appropriate subdirectory:

```
Features/Bookings/RescheduleBooking.feature
```

### 2. Write Scenario in Gherkin

```gherkin
Feature: Reschedule Booking
  As a customer
  I want to reschedule my bookings
  So that I can change appointment times

  @booking @reschedule
  Scenario: Customer reschedules their booking
    Given I am authenticated as a customer
    And I have a booking for "Haircut" scheduled for tomorrow
    When I reschedule the booking to 3 days from now at 14:00
    Then the response status code should be 200
    And the booking should be rescheduled in the database
```

### 3. Run Test (Will Show Pending Steps)

```bash
dotnet test --filter "RescheduleBooking"
```

Output will show which step definitions are missing.

### 4. Implement Step Definitions

Add missing steps to appropriate step definition class:

```csharp
[When(@"I reschedule the booking to (.*)")]
public async Task WhenIRescheduleTheBookingTo(string newTime)
{
    var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");
    var startTime = _helper.ParseRelativeTime(newTime);

    var request = new { NewStartTime = startTime };

    var response = await _testBase.PostAsJsonAsync(
        $"/api/v1/bookings/{bookingId}/reschedule", request);

    _scenarioContext.Set(response, "LastResponse");
}

[Then(@"the booking should be rescheduled in the database")]
public async Task ThenTheBookingShouldBeRescheduledInTheDatabase()
{
    var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");
    var booking = await _testBase.FindBookingAsync(bookingId);

    booking.Status.Should().Be(BookingStatus.Rescheduled);
}
```

### 5. Run Test Again

```bash
dotnet test --filter "RescheduleBooking"
```

## Best Practices

### ✅ DO

1. **Write scenarios in business language**
   ```gherkin
   # Good
   When I create a booking for tomorrow

   # Bad
   When I POST to /api/v1/bookings with StartTime=2025-11-07T10:00:00Z
   ```

2. **Keep scenarios independent**
   - Each scenario should set up its own data
   - Don't rely on execution order

3. **Use Background for common setup**
   ```gherkin
   Background:
     Given a provider exists
     And the provider has a service
   ```

4. **Make step definitions reusable**
   ```csharp
   [Given(@"I am authenticated as a (.*)")]
   public void GivenIAmAuthenticatedAs(string role)
   {
       // Handle "customer", "provider", "admin"
   }
   ```

5. **Tag scenarios appropriately**
   ```gherkin
   @smoke @booking @negative
   Scenario: Cannot create booking without auth
   ```

### ❌ DON'T

1. **Don't make scenarios too technical**
   ```gherkin
   # Bad
   When I set header "Authorization" to "Bearer xyz"

   # Good
   Given I am authenticated as a customer
   ```

2. **Don't test implementation details**
   ```gherkin
   # Bad
   Then the booking aggregate should raise a BookingCreatedEvent

   # Good
   Then the booking should exist with status "Requested"
   ```

3. **Don't create overly specific step definitions**
   ```csharp
   // Bad - too specific
   [When(@"I create a haircut booking for tomorrow at 10am")]

   // Good - reusable
   [When(@"I create a booking for ""(.*)"" at (.*)")]
   ```

4. **Don't put logic in feature files**
   ```gherkin
   # Bad
   When I create bookings for each service

   # Good - explicit
   When I create a booking for "Haircut"
   And I create a booking for "Coloring"
   ```

## Debugging

### View Generated Test Code

Reqnroll generates C# test classes from .feature files. To view them:

```bash
# Add to csproj
<PropertyGroup>
  <ReqnrollGeneratorAllowDebugGeneratedFiles>true</ReqnrollGeneratorAllowDebugGeneratedFiles>
</PropertyGroup>

# Rebuild
dotnet build
```

Generated files appear in `obj/` directory.

### Add Logging

```csharp
[BeforeStep]
public void LogStep()
{
    Console.WriteLine($"Executing: {_scenarioContext.StepContext.StepInfo.Text}");
}
```

### Inspect ScenarioContext

```csharp
[AfterScenario]
public void InspectContext()
{
    foreach (var key in _scenarioContext.Keys)
    {
        Console.WriteLine($"{key}: {_scenarioContext[key]}");
    }
}
```

## Common Issues

### "No matching step definition found"
- Check step text matches exactly (whitespace, punctuation)
- Ensure step definition is in a [Binding] class
- Verify regular expression pattern is correct

### "Ambiguous step definitions"
- Multiple step definitions match the same text
- Make patterns more specific or combine into one step

### "Placeholder not replaced"
- Entity not in ScenarioContext (check previous step)
- Wrong context key format
- Property doesn't exist on entity

## Next Steps

1. **Read the full migration plan:** [REQNROLL_MIGRATION_PLAN.md](./REQNROLL_MIGRATION_PLAN.md)
2. **Review existing feature files:** `tests/Booksy.ServiceCatalog.IntegrationTests/Features/`
3. **Study step definitions:** `tests/Booksy.ServiceCatalog.IntegrationTests/StepDefinitions/`
4. **Start writing your own scenarios!**

## Resources

- **Reqnroll Documentation:** https://docs.reqnroll.net/
- **Gherkin Reference:** https://cucumber.io/docs/gherkin/reference/
- **BDD Best Practices:** https://cucumber.io/docs/bdd/
- **Feature README:** [tests/Booksy.ServiceCatalog.IntegrationTests/Features/README.md](./tests/Booksy.ServiceCatalog.IntegrationTests/Features/README.md)

## Questions?

- Check the migration plan for detailed information
- Review existing feature files for examples
- Consult the team for guidance on scenario design
