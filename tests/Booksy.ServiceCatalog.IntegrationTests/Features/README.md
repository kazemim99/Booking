# Reqnroll Feature Files

This directory contains Gherkin feature files that describe the behavior of the Booksy Service Catalog API from a user's perspective.

## Directory Structure

```
Features/
├── Bookings/          # Booking lifecycle scenarios
├── Payments/          # Payment processing scenarios
├── Providers/         # Provider management scenarios (coming soon)
└── Services/          # Service management scenarios (coming soon)
```

## Running Feature Files

### Run all scenarios
```bash
dotnet test
```

### Run specific feature
```bash
dotnet test --filter "FullyQualifiedName~CreateBooking"
```

### Run scenarios by tag
```bash
# Run only smoke tests
dotnet test --filter "Category=smoke"

# Run only booking tests
dotnet test --filter "Category=booking"

# Run only negative test scenarios
dotnet test --filter "Category=negative"
```

## Available Tags

- `@smoke` - Critical path scenarios that must always pass
- `@booking` - Booking-related scenarios
- `@payment` - Payment-related scenarios
- `@provider` - Provider-related scenarios
- `@negative` - Error/validation scenarios
- `@authorization` - Authorization/permission scenarios
- `@validation` - Input validation scenarios
- `@create`, `@cancel`, `@refund`, etc. - Operation-specific tags

## Writing New Feature Files

### Feature File Template

```gherkin
Feature: <Feature Name>
  As a <role>
  I want to <action>
  So that <benefit>

  Background:
    Given <common setup steps>

  @tag1 @tag2
  Scenario: <Scenario description>
    Given <preconditions>
    When <action>
    Then <expected outcome>
```

### Best Practices

1. **Use business language** - Write scenarios that business stakeholders can understand
2. **One scenario = one behavior** - Keep scenarios focused and independent
3. **Use descriptive names** - Scenario names should clearly describe what's being tested
4. **Tag appropriately** - Use tags to organize and filter scenarios
5. **Leverage Background** - Put common setup in Background to avoid repetition
6. **Use Scenario Outlines** - For testing multiple input combinations
7. **Keep scenarios independent** - Each scenario should be runnable in isolation

### Example: Good vs Bad Scenarios

❌ **Bad - Too technical**
```gherkin
Scenario: POST to /api/v1/bookings returns 201
  When I POST to "/api/v1/bookings" with JSON payload
  Then HTTP status code should be 201
```

✅ **Good - Business focused**
```gherkin
Scenario: Customer creates a valid booking
  Given I am authenticated as a customer
  When I create a booking for "Haircut" service tomorrow at 10:00
  Then the booking should be created successfully
  And I should receive a confirmation email
```

## Placeholder Syntax

Feature files support context placeholders that get replaced at runtime:

- `[Provider:Current:Id]` - ID of the current provider
- `[Service:Haircut:Id]` - ID of the "Haircut" service
- `[Booking:Current:Id]` - ID of the current booking

### Relative Time Expressions

- `2 days from now at 10:00` - Future datetime
- `1 day ago at 14:00` - Past datetime
- `tomorrow at 10:00` - Next day at specified time

## Step Definition Locations

Step definitions are implemented in C# under `StepDefinitions/`:

- **Common/AuthenticationSteps.cs** - Authentication scenarios
- **Common/TestDataSteps.cs** - Test data setup
- **Bookings/BookingSteps.cs** - Booking operations
- **Payments/PaymentSteps.cs** - Payment operations

## Troubleshooting

### Scenario fails with "No matching step definition"
- Check that step text matches exactly (including spacing/punctuation)
- Ensure step definition assembly is being scanned
- Look for typos in regular expression patterns

### Placeholder not replaced
- Ensure the entity was created in a previous step
- Check the context key matches the placeholder pattern
- Verify the entity has the property being accessed

### Relative time parsing fails
- Use supported formats (see above)
- Ensure time is in 24-hour format (e.g., 14:00, not 2:00 PM)

## Contributing

When adding new scenarios:

1. Create feature file in appropriate subdirectory
2. Use existing step definitions where possible
3. Add new step definitions only when needed
4. Keep step definitions reusable and generic
5. Update this README if introducing new patterns

## Resources

- [Reqnroll Documentation](https://docs.reqnroll.net/)
- [Gherkin Reference](https://cucumber.io/docs/gherkin/reference/)
- [BDD Best Practices](https://cucumber.io/docs/bdd/)
- [Migration Plan](../../../REQNROLL_MIGRATION_PLAN.md)
