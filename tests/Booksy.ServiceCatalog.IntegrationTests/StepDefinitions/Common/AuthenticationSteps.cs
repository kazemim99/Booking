using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Common;

/// <summary>
/// Step definitions for authentication scenarios
/// </summary>
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

    [Given(@"I am authenticated as a customer")]
    public void GivenIAmAuthenticatedAsACustomer()
    {
        var userId = Guid.NewGuid();
        var email = $"customer-{userId:N}@test.com";

        _testBase.AuthenticateAsUser(userId, email);

        _scenarioContext.Set(userId, "CurrentUserId");
        _scenarioContext.Set(email, "CurrentUserEmail");
        _scenarioContext.Set("Customer", "CurrentUserRole");
    }

    [Given(@"I am authenticated as customer ""(.*)""")]
    public void GivenIAmAuthenticatedAsCustomerWithEmail(string email)
    {
        var userId = Guid.NewGuid();

        _testBase.AuthenticateAsUser(userId, email);

        _scenarioContext.Set(userId, "CurrentUserId");
        _scenarioContext.Set(email, "CurrentUserEmail");
        _scenarioContext.Set("Customer", "CurrentUserRole");
    }

    [Given(@"I am authenticated as the provider")]
    public void GivenIAmAuthenticatedAsTheProvider()
    {
        // Get the provider from context (set by test data steps)
        if (!_scenarioContext.ContainsKey("Provider:Current"))
            throw new InvalidOperationException("No current provider in context. Use 'Given a provider exists' first.");

        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");

        _testBase.AuthenticateAsProviderOwner(provider);

        _scenarioContext.Set(provider.OwnerId.Value, "CurrentUserId");
        _scenarioContext.Set(provider.ContactInfo.Email.Value, "CurrentUserEmail");
        _scenarioContext.Set("Provider", "CurrentUserRole");
    }

    [Given(@"I am authenticated as an admin")]
    public void GivenIAmAuthenticatedAsAnAdmin()
    {
        _testBase.AuthenticateAsTestAdmin();

        _scenarioContext.Set("Admin", "CurrentUserRole");
    }

    [Given(@"I am not authenticated")]
    public void GivenIAmNotAuthenticated()
    {
        _testBase.ClearAuthenticationHeader();

        _scenarioContext.Remove("CurrentUserId");
        _scenarioContext.Remove("CurrentUserEmail");
        _scenarioContext.Remove("CurrentUserRole");
    }

    [Given(@"another customer has a booking for the same service")]
    public async Task GivenAnotherCustomerHasABookingForTheSameService()
    {
        var otherCustomerId = Guid.NewGuid();
        var service = _scenarioContext.Get<Domain.Aggregates.Service>("Service:Current");
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");

        // Create booking for another customer
        var booking = Domain.Aggregates.BookingAggregate.Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(otherCustomerId),
            provider.Id,
            service.Id,
            provider.Id,
            DateTime.UtcNow.AddDays(2),
            service.Duration,
            service.BasePrice,
            Domain.ValueObjects.BookingPolicy.Default,
            "Another customer's booking");

        await _testBase.CreateEntityAsync(booking);

        _scenarioContext.Set(booking, "OtherCustomerBooking");
        _scenarioContext.Set(otherCustomerId, "OtherCustomerId");
    }
}
