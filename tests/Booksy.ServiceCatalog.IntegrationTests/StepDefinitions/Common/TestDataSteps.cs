using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
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
    private readonly ScenarioContextHelper _helper;

    public TestDataSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogReqnrollTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
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

    /// <summary>
    /// Registers an active provider with an explicit business name, category and (optional)
    /// contact email — used by the payment-gateway feature backgrounds
    /// (ZarinPal/Behpardakht), which all declare the same table shape:
    /// <c>BusinessName</c>, <c>BusinessType</c>, and an optional <c>Email</c>. Also authenticates
    /// the scenario as the new provider's owner.
    /// </summary>
    /// <remarks>
    /// This step text was unbound in seven feature files across both gateways with identical
    /// wording — it carries no gateway-specific behaviour, so it is implemented once here
    /// rather than duplicated per gateway.
    ///
    /// <para>Authenticating here (rather than requiring a separate step) matches every scenario
    /// that actually uses this step: none of the ten gateway-creation/verification scenarios that
    /// consume it declare a separate "I am authenticated as..." step, and the create-payment
    /// endpoints are <c>[Authorize]</c>d — omitting this made every one of them 401. It also
    /// mirrors the established test-helper convention elsewhere in this suite
    /// (<c>CreateAndAuthenticateAsProviderAsync</c>), which always couples creation with
    /// authentication as the caller.</para>
    /// </remarks>
    [Given(@"a registered provider exists with:")]
    public async Task GivenARegisteredProviderExistsWith(Table table)
    {
        var data = _helper.BuildDictionaryFromTable(table);
        var businessName = (string)data["BusinessName"];
        var category = Enum.Parse<ServiceCategory>((string)data["BusinessType"]);
        var email = data.TryGetValue("Email", out var e) ? (string)e : "provider@example.com";

        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            businessName,
            $"Description for {businessName}",
            category,
            ContactInfo.Create(
                Email.Create(email),
                PhoneNumber.From("+989120000000")),
            BusinessAddress.Create(
                "123 Test St", "123 Test St", "Test City", "TS", "12345", "Iran"));

        provider.SetSatus(ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);
        await _testBase.CreateEntityAsync(provider);

        _scenarioContext.Set(provider, "Provider:Current");
        _scenarioContext.Set(provider.Id.Value, "CurrentProviderId");
        _testBase.AuthenticateAsProviderOwner(provider);
    }

    /// <summary>
    /// Seeds a Confirmed booking against <c>Provider:Current</c> with the given amount and
    /// currency — the minimum a payment-gateway scenario needs to attach a payment request to.
    /// </summary>
    /// <remarks>
    /// Booked directly against the organisation (<c>staffId == provider.Id</c>), matching the
    /// "organization-direct" resource kind that <c>CreateBooking.feature</c> and
    /// <c>RescheduleBooking.feature</c> already exercise. Built via
    /// <see cref="Booking.CreateConfirmedByProvider"/> rather than
    /// <c>CreateBookingRequest</c> + <c>Confirm()</c>: the latter enforces a deposit-paid gate
    /// and a booking-window check that this seed has no reason to satisfy — the gateway
    /// scenarios are testing payment creation against an existing confirmed booking, not
    /// booking-confirmation rules. A throwaway service is created only to satisfy the
    /// booking's <c>ServiceId</c> foreign key; its own price is irrelevant because the
    /// booking's <c>TotalPrice</c> is passed explicitly.
    ///
    /// <para>Every scenario using this step requests <c>Status: Confirmed</c> — no other value
    /// is exercised anywhere in the two gateway feature directories. A different value throws
    /// rather than silently producing a Confirmed booking, so a future scenario that needs one
    /// fails loudly instead of asserting against the wrong state.</para>
    /// </remarks>
    [Given(@"a booking exists for the provider with:")]
    public async Task GivenABookingExistsForTheProviderWith(Table table)
    {
        var data = _helper.BuildDictionaryFromTable(table);
        // BuildDictionaryFromTable already converts numeric-looking cell text to decimal (via
        // ScenarioContextHelper.ConvertValueToType), so Amount arrives as a decimal, not a string
        // to re-parse — casting straight to string threw InvalidCastException on every call.
        var amount = Convert.ToDecimal(data["Amount"]);
        var currency = data["Currency"].ToString()!;
        var status = data.TryGetValue("Status", out var s) ? s.ToString()! : "Confirmed";

        if (status != "Confirmed")
        {
            throw new NotSupportedException(
                $"'a booking exists for the provider with: Status={status}' is not implemented — " +
                "every current usage of this step requests Confirmed. Extend this step rather than " +
                "guessing what a different status should seed.");
        }

        var provider = _scenarioContext.Get<Provider>("Provider:Current");
        var service = await _testBase.CreateServiceForProviderAsync(
            provider, "Gateway Test Service", price: 1m, durationMinutes: 60);

        var booking = Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()),
            provider.Id,
            service.Id,
            provider.Id.Value, // organization-direct: staffId == provider.Id
            DateTime.UtcNow.AddDays(1),
            service.Duration,
            Price.Create(amount, currency),
            BookingPolicy.Default,
            "Gateway test booking");

        await _testBase.CreateEntityAsync(booking);

        _scenarioContext.Set(booking, "Booking:Current");
        _scenarioContext.Set(booking.Id.Value, "CurrentBookingId");
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
