using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Common;

/// <summary>
/// Step definitions for setting up test data
/// </summary>
[Binding]
public class TestDataSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogReqnrollTestBase _testBase;

    public TestDataSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogReqnrollTestBase testBase)
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

        // Services added AFTER staff exists (mid-scenario) must be qualified
        // and activated immediately; ones added before are handled when the
        // staff step runs.
        if (_scenarioContext.ContainsKey("Staff:Current"))
        {
            var staff = _scenarioContext.Get<Provider>("Staff:Current");
            service.AddQualifiedStaff(staff.Id.Value);
            service.Activate();
            await _testBase.UpdateEntityAsync(service);
        }

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

    /// <summary>
    /// Opens the provider every day of the week. Scenarios that move a booking to
    /// "N days from now" would otherwise pass or fail depending on which weekday the
    /// suite happens to run — "3 days from now" lands on a Saturday midweek, and the
    /// Mon-Fri step above then rejects it as outside business hours. Use this wherever
    /// the scenario is about something other than opening times.
    /// </summary>
    [Given(@"the provider is open every day")]
    public async Task GivenTheProviderIsOpenEveryDay()
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");

        var hours = Enum.GetValues<DayOfWeek>().ToDictionary(
            day => day,
            _ => ((TimeOnly?)TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
                  (TimeOnly?)TimeOnly.FromTimeSpan(TimeSpan.FromHours(18))));

        provider.SetBusinessHours(hours);

        await _testBase.UpdateEntityAsync(provider);
    }

    [Given(@"the provider has at least one staff member")]
    public async Task GivenTheProviderHasAtLeastOneStaffMember()
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");

        // A real Individual sub-provider in the hierarchy: bookings REQUIRE a
        // staff provider (StaffProviderId), so without this every creation
        // scenario dies with a 400 before reaching any business rule.
        var staff = Provider.RegisterStaffMember(
            provider,
            UserId.From(Guid.NewGuid()),
            "Test",
            "Staff");
        await _testBase.CreateEntityAsync(staff);

        _scenarioContext.Set(staff, "Staff:Current");
        _scenarioContext.Set(staff.Id.Value, "CurrentStaffId");

        // Services are born Draft and can only be Activated once they have a
        // qualified staff member — and the Background seeds services BEFORE
        // staff. Qualify + activate them here so they are bookable.
        await ActivateProviderServicesAsync(provider, staff);
    }

    /// <summary>
    /// Assigns <paramref name="staff"/> to every Draft service of the provider
    /// and activates it. Bookings reject non-Active services.
    /// </summary>
    private async Task ActivateProviderServicesAsync(Provider provider, Provider staff)
    {
        var services = await _testBase.GetProviderServicesAsync(provider.Id.Value);
        foreach (var service in services)
        {
            service.AddQualifiedStaff(staff.Id.Value);
            if (service.Status != Domain.Enums.ServiceStatus.Active)
                service.Activate();
            await _testBase.UpdateEntityAsync(service);
        }
    }

    /// <summary>
    /// Seeds a booking held against the ORGANISATION itself (<c>StaffId == provider.Id</c>) —
    /// a solo/direct booking with no specific staff member.
    /// </summary>
    [Given(@"I have a booking for ""(.*)"" scheduled for tomorrow at (.*)")]
    public async Task GivenIHaveABookingScheduledFor(string serviceName, string time)
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");
        await SeedBookingForResourceAsync(serviceName, time, provider.Id.Value);
    }

    /// <summary>
    /// Seeds a booking held against the legacy individual sub-provider created by
    /// "the provider has at least one staff member". Distinct from the step above,
    /// which books the ORGANISATION directly (StaffId == provider.Id).
    /// </summary>
    [Given(@"I have a booking for ""(.*)"" with the staff member scheduled for tomorrow at (.*)")]
    public async Task GivenIHaveABookingWithTheStaffMemberScheduledFor(string serviceName, string time)
    {
        var staff = _scenarioContext.Get<Provider>("Staff:Current");
        await SeedBookingForResourceAsync(serviceName, time, staff.Id.Value);
    }

    /// <summary>
    /// Seeds a booking held against an organization MEMBERSHIP — the current staff
    /// model, where Booking.StaffId carries a MembershipId rather than a ProviderId.
    /// This is what `POST /Providers/{id}/staff` produces in the live system.
    /// </summary>
    [Given(@"I have a booking for ""(.*)"" with the team member scheduled for tomorrow at (.*)")]
    public async Task GivenIHaveABookingWithTheTeamMemberScheduledFor(string serviceName, string time)
    {
        var provider = _scenarioContext.Get<Provider>("Provider:Current");

        var membership = OrganizationMembership.CreateUnclaimed(
            provider.Id, "Team Member", providesServices: true);
        await _testBase.CreateEntityAsync(membership);

        _scenarioContext.Set(membership, "Membership:Current");

        // Services reject staff they are not qualified for.
        await QualifyResourceForProviderServicesAsync(provider, membership.Id);

        await SeedBookingForResourceAsync(serviceName, time, membership.Id);
    }

    /// <summary>
    /// Seeds a booking whose StaffId matches no membership, organization, or
    /// sub-provider — the "resource no longer resolves" case.
    /// </summary>
    [Given(@"I have a booking for ""(.*)"" with an unresolvable resource scheduled for tomorrow at (.*)")]
    public async Task GivenIHaveABookingWithAnUnresolvableResourceScheduledFor(string serviceName, string time)
    {
        await SeedBookingForResourceAsync(serviceName, time, Guid.NewGuid());
    }

    /// <summary>
    /// Creates a Requested booking for <paramref name="resourceId"/> and records it as
    /// the scenario's current booking. `resourceId` is whatever kind of bookable
    /// resource the scenario is exercising (membership, organization, sub-provider).
    /// </summary>
    private async Task SeedBookingForResourceAsync(string serviceName, string time, Guid resourceId)
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
            resourceId,
            startTime,
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Test booking");

        await _testBase.CreateEntityAsync(booking);

        _scenarioContext.Set(booking, "Booking:Current");
        _scenarioContext.Set(booking.Id.Value, "CurrentBookingId");
        _scenarioContext.Set(resourceId, "CurrentBookingResourceId");
        // Record the PERSISTED start time, not the one we asked for: a booking seeded at
        // 10:00 reads back as 13:30 (the Tehran +3:30 offset), so comparing a later DB read
        // against the requested value would fail on the round-trip rather than on behaviour.
        var persisted = await _testBase.DbContext.Bookings
            .AsNoTracking()
            .FirstAsync(b => b.Id == booking.Id);
        _scenarioContext.Set(persisted.TimeSlot.StartTime, "CurrentBookingStartTime");
    }

    /// <summary>
    /// Seeds a second, unrelated booking occupying <paramref name="time"/> for the SAME resource,
    /// so a reschedule into that time is rejected as a conflict.
    /// </summary>
    [Given(@"another booking already occupies (.*) for the same resource")]
    public async Task GivenAnotherBookingAlreadyOccupies(string time)
    {
        var helper = _scenarioContext.Get<Support.ScenarioContextHelper>("Helper");
        var service = _scenarioContext.Get<Service>("Service:Current");
        var provider = _scenarioContext.Get<Provider>("Provider:Current");
        var resourceId = _scenarioContext.Get<Guid>("CurrentBookingResourceId");

        var blocker = Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()),
            provider.Id,
            service.Id,
            resourceId,
            helper.ParseRelativeTime(time),
            service.Duration,
            service.BasePrice,
            BookingPolicy.Default,
            "Occupies the target slot");

        await _testBase.CreateEntityAsync(blocker);
    }

    /// <summary>Qualifies <paramref name="resourceId"/> for every service so bookings are accepted.</summary>
    private async Task QualifyResourceForProviderServicesAsync(Provider provider, Guid resourceId)
    {
        var services = await _testBase.GetProviderServicesAsync(provider.Id.Value);
        foreach (var service in services)
        {
            service.AddQualifiedStaff(resourceId);
            if (service.Status != Domain.Enums.ServiceStatus.Active)
                service.Activate();
            await _testBase.UpdateEntityAsync(service);
        }
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
            provider.Id,
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
