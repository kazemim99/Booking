using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Common;

/// <summary>
/// Step definitions for setting up test data
/// </summary>
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
        var businessName = table.Rows[0]["Value"];

        // Create provider with default settings
        var provider = await _testBase.CreateTestProviderWithServicesAsync(serviceCount: 0);

        // Store in context
        _scenarioContext.Set(provider, $"Provider:{providerName}");
        _scenarioContext.Set(provider, "Provider:Current");
        _scenarioContext.Set(provider.Id.Value, "CurrentProviderId");
    }

    [Given(@"a provider ""(.*)"" exists with active status")]
    public async Task GivenAProviderExistsWithActiveStatus(string providerName)
    {
        var provider = await _testBase.CreateTestProviderWithServicesAsync(serviceCount: 0);
        provider.SetSatus(ProviderStatus.Active);
        await _testBase.UpdateEntityAsync(provider);

        _scenarioContext.Set(provider, $"Provider:{providerName}");
        _scenarioContext.Set(provider, "Provider:Current");
        _scenarioContext.Set(provider.Id.Value, "CurrentProviderId");
    }

    [Given(@"the provider has a service ""(.*)"" with:")]
    public async Task GivenTheProviderHasAServiceWith(string serviceName, Table table)
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");

        var name = table.Rows[0]["Value"];  // First row, Field = Name
        var price = decimal.Parse(table.Rows[1]["Value"]);  // Second row, Field = Price
        var duration = int.Parse(table.Rows[2]["Value"]);  // Third row, Field = Duration

        var service = await _testBase.CreateServiceForProviderAsync(provider, name, price, duration);

        _scenarioContext.Set(service, $"Service:{serviceName}");
        _scenarioContext.Set(service, "Service:Current");
    }

    [Given(@"the provider has a service ""(.*)"" priced at (.*) USD")]
    public async Task GivenTheProviderHasAServicePricedAt(string serviceName, decimal price)
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");

        var service = await _testBase.CreateServiceForProviderAsync(provider, serviceName, price, 60);

        _scenarioContext.Set(service, $"Service:{serviceName}");
        _scenarioContext.Set(service, "Service:Current");
    }

    [Given(@"the provider has business hours configured")]
    public async Task GivenTheProviderHasBusinessHoursConfigured()
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");

        provider.SetBusinessHours(new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
        {
            { DayOfWeek.Monday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { DayOfWeek.Tuesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { DayOfWeek.Wednesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { DayOfWeek.Thursday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { DayOfWeek.Friday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) }
        });

        await _testBase.UpdateEntityAsync(provider);
    }

    [Given(@"the provider has at least one staff member")]
    public async Task GivenTheProviderHasAtLeastOneStaffMember()
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");

        if (!provider.Staff.Any())
        {
            provider.AddStaff(
                "Test",
                "Staff",
                StaffRole.ServiceProvider,
                PhoneNumber.From("+1234567890"));

            await _testBase.UpdateEntityAsync(provider);
        }
    }

    [Given(@"I have a booking for ""(.*)"" scheduled for tomorrow at (.*)")]
    public async Task GivenIHaveABookingScheduledFor(string serviceName, string time)
    {
        var service = _scenarioContext.Get<Service>($"Service:{serviceName}");
        var provider = _scenarioContext.Get<Provider>("Provider:Current");
        var customerId = _scenarioContext.Get<Guid>("CurrentUserId");

        var timeOnly = TimeOnly.Parse(time);
        var startTime = DateTime.UtcNow.AddDays(1).Date.Add(timeOnly.ToTimeSpan());

        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Staff.First().Id,
            startTime,
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Test booking");

        await _testBase.CreateEntityAsync(booking);

        _scenarioContext.Set(booking, "Booking:Current");
        _scenarioContext.Set(booking.Id.Value, "CurrentBookingId");
    }

    [Given(@"I have a confirmed booking for ""(.*)""")]
    public async Task GivenIHaveAConfirmedBookingFor(string serviceName)
    {
        var service = _scenarioContext.Get<Service>($"Service:{serviceName}");
        var provider = _scenarioContext.Get<Provider>("Provider:Current");
        var customerId = _scenarioContext.Get<Guid>("CurrentUserId");

        var startTime = DateTime.UtcNow.AddDays(2).Date.AddHours(10);

        var booking = Booking.CreateBookingRequest(
            UserId.From(customerId),
            provider.Id,
            service.Id,
            provider.Staff.First().Id,
            startTime,
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Test booking");

        booking.Confirm();
        await _testBase.CreateEntityAsync(booking);

        _scenarioContext.Set(booking, "Booking:Current");
        _scenarioContext.Set(booking.Id.Value, "CurrentBookingId");
    }

    [Given(@"I have a completed payment of (.*) USD for the booking")]
    public async Task GivenIHaveACompletedPaymentForTheBooking(decimal amount)
    {
        var booking = _scenarioContext.Get<Booking>("Booking:Current");
        var provider = _scenarioContext.Get<Provider>("Provider:Current");
        var customerId = _scenarioContext.Get<Guid>("CurrentUserId");

        var payment = Payment.CreateForBooking(
            booking.Id,
            UserId.From(customerId),
            provider.Id,
            Money.Create(amount, "USD"),
            PaymentMethod.CreditCard);

        payment.ProcessCharge("pi_test_123", "pm_test_card");
        await _testBase.CreateEntityAsync(payment);

        _scenarioContext.Set(payment, "Payment:Current");
        _scenarioContext.Set(payment.Id.Value, "CurrentPaymentId");
    }
}
