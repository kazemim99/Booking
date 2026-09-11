using Booksy.API;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Xunit;
using DomainDayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// Tests the TEST FIXTURE, because a whole cluster of integration failures turned out to be
/// the fixture lying rather than the application misbehaving.
///
/// <para><c>CreateTestProviderWithServicesAsync()</c> is the arrange step for most of this
/// suite. Under the membership model a bookable salon needs four things — an Active provider
/// that allows online booking, business hours that are actually OPEN on the day under test, an
/// Active service, and an active member who provides services — and the fixture used to supply
/// only the first. Every availability and booking test then failed on "service is not active"
/// and "closed on this day", which says nothing about the behaviour those tests exist to
/// protect.</para>
///
/// <para>These assertions are deliberately about the fixture's own output. If one of them
/// breaks, every test built on it is about to fail for a reason unrelated to its subject, and
/// this test says so directly instead of leaving fifteen misleading failures.</para>
/// </summary>
[Collection(ServiceCatalogTestCollection.Name)]
public class FixtureBookabilityTests : ServiceCatalogIntegrationTestBase
{
    public FixtureBookabilityTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task The_fixture_produces_a_provider_that_is_open_every_day()
    {
        var provider = await CreateTestProviderWithServicesAsync();

        DbContext.ChangeTracker.Clear();
        var reloaded = await DbContext.Set<Provider>()
            .Include(p => p.BusinessHours)
            .FirstAsync(p => p.Id == provider.Id);

        reloaded.Status.Should().Be(ProviderStatus.Active);
        reloaded.AllowOnlineBooking.Should().BeTrue();

        reloaded.BusinessHours.Should().HaveCount(7, "SetBusinessHours writes a row per weekday");
        foreach (DomainDayOfWeek day in Enum.GetValues<DomainDayOfWeek>())
        {
            reloaded.BusinessHours.Single(h => h.DayOfWeek == day)
                .IsOpen.Should().BeTrue($"the fixture opens every day, including {day}, so a test " +
                                        "that picks a date a few days out is never accidentally " +
                                        "landing on a closed day");
        }
    }

    [Fact]
    public async Task The_fixture_produces_an_active_member_who_provides_services()
    {
        var provider = await CreateTestProviderWithServicesAsync();

        DbContext.ChangeTracker.Clear();
        var memberships = await DbContext.Set<OrganizationMembership>()
            .Where(m => m.OrganizationId == provider.Id)
            .ToListAsync();

        var member = memberships.Should().ContainSingle(
            "the salon's owner is its one member").Subject;

        member.Status.Should().Be(MembershipStatus.Active);
        member.ProvidesServices.Should().BeTrue(
            "availability is generated per service-providing member; without one the salon " +
            "has no bookable resource at all");
    }

    [Fact]
    public async Task A_salon_the_fixture_built_returns_slots_over_HTTP()
    {
        // The same arrange the availability tests use, then the same request, so a divergence
        // between "bookable in the database" and "bookable through the API" shows up here
        // rather than as a bare "0 slots" fifteen tests away.
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var date = DateTime.UtcNow.AddDays(3).Date;

        var response = await GetAsync(
            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={date:yyyy-MM-dd}");

        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("startTime",
            $"the fixture asserted this salon is bookable, so the API must agree. " +
            $"provider={provider.Id.Value} service={service.Id.Value} date={date:yyyy-MM-dd}. Body: {body}");
    }

    [Fact]
    public async Task The_fixture_produces_a_service_that_can_actually_be_booked()
    {
        var provider = await CreateTestProviderWithServicesAsync();

        DbContext.ChangeTracker.Clear();
        var service = await DbContext.Set<Service>()
            .FirstAsync(s => s.ProviderId == provider.Id);

        // Service.Create leaves a service in Draft, and Activate() refuses without a qualified
        // member — so this only holds if the fixture ran the real bookability sync.
        service.Status.Should().Be(ServiceStatus.Active);
        service.QualifiedStaff.Should().NotBeEmpty();
        service.CanBeBooked().Should().BeTrue();
    }
}
