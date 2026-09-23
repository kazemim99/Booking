using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Specifications.Provider;
using Booksy.Tests.Common.Builders;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.Api.UnitTests.Specifications;

/// <summary>
/// Which salons a customer may find (customer-app-ux-review-fixes, task I.0).
/// </summary>
/// <remarks>
/// <see cref="SearchProvidersSpecification"/> said "default to active providers only" while filtering only
/// <c>!= Archived</c>, so on production (2026-09-23) a Drafted test salon and a PendingVerification medical-supplies
/// shop sat in customer search beside the one real salon. The product has a verification step — the admin panel
/// activates a salon — so a public listing shows Active salons only. Admin callers that need the rest ask for them
/// with <c>includeInactive</c>.
/// </remarks>
public class PublicProviderListingStatusTests
{
    private const double Lat = 35.70, Lon = 51.40;

    public static TheoryData<ProviderStatus> EveryStatusButActive()
    {
        var data = new TheoryData<ProviderStatus>();
        foreach (var status in Enum.GetValues<ProviderStatus>().Where(s => s != ProviderStatus.Active))
            data.Add(status);
        return data;
    }

    private static Domain.Aggregates.Provider ProviderIn(ProviderStatus status)
    {
        var provider = new ProviderBuilder().WithStatus(status).Build();
        provider.UpdateAddress(provider.Address.WithCoordinates(Lat, Lon));
        return provider;
    }

    private static Func<Domain.Aggregates.Provider, bool> Matches(
        System.Linq.Expressions.Expression<Func<Domain.Aggregates.Provider, bool>>? criteria)
    {
        criteria.Should().NotBeNull("a public listing must always filter by status");
        return criteria!.Compile();
    }

    [Fact]
    public void Search_by_default_lists_an_active_salon()
    {
        Matches(new SearchProvidersSpecification().Criteria)(ProviderIn(ProviderStatus.Active)).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(EveryStatusButActive))]
    public void Search_by_default_hides_a_salon_that_is_not_active(ProviderStatus status)
    {
        Matches(new SearchProvidersSpecification().Criteria)(ProviderIn(status))
            .Should().BeFalse($"a {status} salon has not been (or is no longer) approved for customers");
    }

    [Theory]
    [MemberData(nameof(EveryStatusButActive))]
    public void Search_with_includeInactive_still_returns_every_status(ProviderStatus status)
    {
        // The admin panel lists providers through search with includeInactive=true — it must keep seeing the
        // salons it has yet to activate.
        var spec = new SearchProvidersSpecification(includeInactive: true);

        if (spec.Criteria is not null)
            spec.Criteria.Compile()(ProviderIn(status)).Should().BeTrue();
    }

    [Fact]
    public void By_location_lists_an_active_salon()
    {
        Matches(new ProvidersByLocationSpecification(Lat, Lon, 5).Criteria)(ProviderIn(ProviderStatus.Active)).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(EveryStatusButActive))]
    public void By_location_hides_a_salon_that_is_not_active(ProviderStatus status)
    {
        Matches(new ProvidersByLocationSpecification(Lat, Lon, 5).Criteria)(ProviderIn(status)).Should().BeFalse();
    }
}
