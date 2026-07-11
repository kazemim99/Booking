# Reqnroll BDD Testing Guide

How to write and run the Gherkin/BDD integration tests in `tests/Booksy.ServiceCatalog.IntegrationTests/`.

## What is Reqnroll?

Reqnroll is a BDD testing framework for .NET: tests are written as `.feature` files in plain-English Gherkin syntax, backed by C# step definitions (xUnit under the hood).

```xml
<PackageReference Include="Reqnroll" Version="2.2.0" />
<PackageReference Include="Reqnroll.xUnit" Version="2.2.0" />
<PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="2.2.0" />
```

## Running tests

```bash
dotnet test                                          # everything (xUnit + Reqnroll)
dotnet test --filter "FullyQualifiedName~Feature"     # Reqnroll only
dotnet test --filter "Category=smoke"                 # critical-path scenarios
dotnet test --filter "Category=booking"                # one feature area
dotnet test --filter "FullyQualifiedName~CreateBooking" # one feature file
dotnet test --filter "Category=negative"                # error/validation scenarios only
```

## Coverage today

The suite has grown well past its original scope — as of this writing there are **40 feature files** under `Features/`, organized by area:

```
Features/
├── API/                    # controller-level feature tests
├── Availability/
├── Bookings/                (create, cancel, lifecycle/reschedule)
├── CQRS/
│   ├── Commands/             # per-command coverage (Booking/Payment/Payout/Provider/Service/Notification/ProgressiveRegistration)
│   └── Queries/
├── EdgeCases/                (authorization/security, concurrency/race conditions, data validation)
├── Notifications/
├── Payments/
│   ├── Behpardakht/           # Behpardakht gateway (creation, verification, refunds, settlement/reversal)
│   ├── ZarinPal/               # ZarinPal gateway (creation, verification, refunds, reconciliation, revenue, history)
│   └── (core payment/refund/payout/financial features)
├── Providers/                (registration, management, working hours, gallery, staff)
└── Services/
```

Business areas covered: the full booking lifecycle (create → confirm → complete/cancel/reschedule/no-show), payment processing including two gateway integrations, provider onboarding and management (registration, working hours, gallery, staff), service CRUD, availability calculation (business hours, breaks, holidays, exceptions, lead/buffer time), multi-channel notifications, and cross-cutting edge cases (authorization, concurrency/race conditions, validation).

Per-scenario counts are intentionally not tracked here — a hand-maintained tally drifts fast (the suite roughly doubled in feature-file count since it was first documented). To see current scope: `dotnet test --list-tests` or browse `Features/` directly.

## Writing a new scenario

### 1. Feature file structure

```gherkin
Feature: Feature Name
  Short description

  Background:
    Given common setup step

  @tag1 @tag2
  Scenario: Scenario description
    Given precondition
    When action
    Then expected result
```

Example:
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

### 2. Reusable step patterns already available

```gherkin
# Auth
Given I am authenticated as a customer / the provider / an admin
Given I am not authenticated

# Test data
Given a provider "Name" exists with active status
Given the provider has a service "ServiceName" priced at 50.00 USD
Given I have a confirmed booking for "ServiceName"

# Assertions
Then the response status code should be 201
Then the booking should exist in the database with status "Requested"
```

### 3. Context placeholders

Reference entities created by earlier steps instead of hardcoding IDs:

```
[Provider:Name:Property]
[Service:Name:Property]
[Booking:Current:Property]
[Payment:Current:Property]
```

### 4. Relative time expressions

```
2 days from now at 10:00 | tomorrow at 14:00 | 1 day ago at 10:00
```

### 5. Scenario outlines for data-driven cases

```gherkin
Scenario Outline: Customer books at different times
  Given I am authenticated as a customer
  When I create a booking at <StartTime>
  Then the response status code should be <StatusCode>

  Examples:
    | StartTime                | StatusCode |
    | 2 days from now at 10:00 | 201        |
    | 1 day ago at 10:00       | 400        |
```

### 6. Tags

`@smoke` (critical path) · feature-area tags (`@booking`, `@payment`, `@provider`, …) · operation tags (`@create`, `@cancel`, `@refund`) · `@negative` (error cases) · `@authorization`.

### 7. Run it, then implement missing steps

```bash
dotnet test --filter "RescheduleBooking"   # shows pending/missing step definitions
```

```csharp
[When(@"I reschedule the booking to (.*)")]
public async Task WhenIRescheduleTheBookingTo(string newTime)
{
    var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");
    var response = await _testBase.PostAsJsonAsync(
        $"/api/v1/bookings/{bookingId}/reschedule", new { NewStartTime = _helper.ParseRelativeTime(newTime) });
    _scenarioContext.Set(response, "LastResponse");
}
```

## Best practices

**Do**: write scenarios in business language ("When I create a booking for tomorrow", not raw HTTP calls); keep scenarios independent (no ordering dependencies); use `Background:` for shared setup; write reusable step definitions (`[Given(@"I am authenticated as a (.*)")]` handling multiple roles); tag consistently.

**Don't**: assert on implementation details (domain events, internal state) instead of observable behavior; write overly-specific one-off step definitions; put branching logic inside feature files.

## Debugging

- **View generated code**: add `<ReqnrollGeneratorAllowDebugGeneratedFiles>true</ReqnrollGeneratorAllowDebugGeneratedFiles>` to the csproj, rebuild, inspect `obj/`.
- **Log each step**: `[BeforeStep]` hook printing `_scenarioContext.StepContext.StepInfo.Text`.
- **Inspect scenario state**: `[AfterScenario]` hook iterating `_scenarioContext.Keys`.

### Common errors
- *"No matching step definition found"* — check exact text match (whitespace/punctuation), confirm the class has `[Binding]`, check the regex.
- *"Ambiguous step definitions"* — two patterns match the same text; narrow or merge them.
- *"Placeholder not replaced"* — the entity isn't in `ScenarioContext` yet, or the context key/property name is wrong.

## Resources

- Reqnroll docs: https://docs.reqnroll.net/
- Gherkin reference: https://cucumber.io/docs/gherkin/reference/
- Feature files: `tests/Booksy.ServiceCatalog.IntegrationTests/Features/`
- Step definitions: `tests/Booksy.ServiceCatalog.IntegrationTests/StepDefinitions/`
- Feature area README: `tests/Booksy.ServiceCatalog.IntegrationTests/Features/README.md`
- Historical migration writeup (xUnit → Reqnroll, Nov 2025): `docs/archive/REQNROLL_MIGRATION_PLAN.md`
