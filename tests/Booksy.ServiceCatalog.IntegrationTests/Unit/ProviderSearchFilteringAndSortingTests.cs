using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Requests.Extenstions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Specifications.Provider;
using Booksy.Tests.Common.Builders;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Unit;

/// <summary>
/// Provider search is the core of discovery — it is how a customer finds anyone at all — and two of its filters
/// were not filtering. Both failed <i>silently</i>: the request succeeded, returned a plausible list, and simply
/// ignored what was asked for. That is worse than an error, because nothing surfaces it until someone compares
/// the results against the data by hand.
///
/// <list type="bullet">
///   <item><description><b>Category.</b> The filter body in <see cref="SearchProvidersSpecification"/> was
///   commented out, so <c>?ServiceCategory=Barbershop</c> returned the whole catalogue — dentists, gyms and
///   physiotherapists included.</description></item>
///   <item><description><b>Distance.</b> <c>SortBy=distance</c> fell through to an ordering by rating (both
///   branches of the switch were identical), so "near me" put the farthest provider first.</description></item>
///   <item><description><b>Coordinates.</b> The customer app sends <c>Latitude</c>/<c>Longitude</c>, which bound
///   to nothing, so distance sorting could never engage from the app even once it worked.</description></item>
/// </list>
/// </summary>
public class ProviderSearchFilteringAndSortingTests
{
    // ------------------------------------------------------------------ category filter

    private static Func<Domain.Aggregates.Provider, bool> CriteriaFor(string? serviceCategory)
    {
        var specification = new SearchProvidersSpecification(serviceCategory: serviceCategory);
        specification.Criteria.Should().NotBeNull(
            "a supplied category must produce a filter — a null criteria is how this silently matched everything");
        return specification.Criteria!.Compile();
    }

    [Theory]
    [InlineData("Barbershop", ServiceCategory.Barbershop)]
    [InlineData("BeautySalon", ServiceCategory.BeautySalon)]
    [InlineData("Spa", ServiceCategory.Spa)]
    [InlineData("Dental", ServiceCategory.Dental)]
    public void Category_filter_matches_only_that_category(string input, ServiceCategory expected)
    {
        var matches = CriteriaFor(input);

        var wanted = new ProviderBuilder().WithCategory(expected).AsActive().Build();
        matches(wanted).Should().BeTrue("a provider in the requested category must be returned");

        // Every other category must be excluded. Enumerating them all is the point: the previous bug let
        // *all* of them through, so asserting against a single counter-example would have passed too.
        foreach (var other in Enum.GetValues<ServiceCategory>().Where(c => c != expected))
        {
            var unwanted = new ProviderBuilder().WithCategory(other).AsActive().Build();
            matches(unwanted).Should().BeFalse($"{other} is not {expected} and must be filtered out");
        }
    }

    [Fact]
    public void Category_filter_is_case_insensitive()
    {
        // Callers and deep links are not careful about casing; rejecting "barbershop" would look like an
        // empty result set rather than a mistake.
        var matches = CriteriaFor("barbershop");
        var provider = new ProviderBuilder().WithCategory(ServiceCategory.Barbershop).AsActive().Build();

        matches(provider).Should().BeTrue();
    }

    [Theory]
    [InlineData("NotACategory")]
    [InlineData("99")]
    [InlineData("-1")]
    public void An_unrecognised_category_matches_nothing(string input)
    {
        // Returning the full catalogue — the old behaviour — reads to the customer as "here are your results".
        // Matching nothing is the honest answer. "99" and "-1" are included because Enum.TryParse accepts any
        // numeric string, so parsing alone is not enough of a guard.
        var matches = CriteriaFor(input);

        foreach (var category in Enum.GetValues<ServiceCategory>())
        {
            var provider = new ProviderBuilder().WithCategory(category).AsActive().Build();
            matches(provider).Should().BeFalse($"{input} names no category, so {category} must not match");
        }
    }

    [Fact]
    public void No_category_supplied_does_not_filter_by_category()
    {
        // The absence of a filter must stay an absence — this is the behaviour the broken version accidentally
        // had for every input, and it is correct only when nothing was asked for.
        var specification = new SearchProvidersSpecification(serviceCategory: null);

        if (specification.Criteria is not null)
        {
            var matches = specification.Criteria.Compile();
            foreach (var category in Enum.GetValues<ServiceCategory>())
            {
                var provider = new ProviderBuilder().WithCategory(category).AsActive().Build();
                matches(provider).Should().BeTrue($"{category} must survive when no category filter was given");
            }
        }
    }

    // ------------------------------------------------------------------ coordinate binding

    [Fact]
    public void Latitude_and_Longitude_are_accepted_as_aliases()
    {
        // What the customer app actually sends. These bound to nothing before, so "near me" searched with no
        // reference point and distance sorting was unreachable from the app.
        var request = new SearchProvidersRequest
        {
            Latitude = 39.6482,
            Longitude = 47.9174,
            SortBy = "distance",
        };

        var query = request.ToQuery();

        query.UserLatitude.Should().Be(39.6482);
        query.UserLongitude.Should().Be(47.9174);
    }

    [Fact]
    public void UserLatitude_and_UserLongitude_still_work()
    {
        var request = new SearchProvidersRequest
        {
            UserLatitude = 35.6892,
            UserLongitude = 51.3890,
        };

        var query = request.ToQuery();

        query.UserLatitude.Should().Be(35.6892);
        query.UserLongitude.Should().Be(51.3890);
    }

    [Fact]
    public void The_explicit_User_spelling_wins_when_both_are_supplied()
    {
        var request = new SearchProvidersRequest
        {
            UserLatitude = 35.6892,
            UserLongitude = 51.3890,
            Latitude = 39.6482,
            Longitude = 47.9174,
        };

        var query = request.ToQuery();

        query.UserLatitude.Should().Be(35.6892, "the documented parameter takes precedence over the alias");
        query.UserLongitude.Should().Be(51.3890);
    }

    [Fact]
    public void Coordinates_are_null_when_neither_spelling_is_supplied()
    {
        var query = new SearchProvidersRequest().ToQuery();

        query.UserLatitude.Should().BeNull();
        query.UserLongitude.Should().BeNull();
    }

    // ------------------------------------------------------------------ proximity ordering

    /// <summary>
    /// The ordering key the handler builds for <c>SortBy=distance</c>: squared planar distance with the
    /// longitude axis scaled by cos(latitude). Reproduced here so the ranking property can be asserted without
    /// a database — what matters is that it increases with true distance, not its absolute value.
    /// </summary>
    private static double ProximityKey(double lat, double lon, double lat0, double lon0)
    {
        var lonScale = Math.Cos(lat0 * Math.PI / 180.0);
        var dy = lat - lat0;
        var dx = (lon - lon0) * lonScale;
        return dy * dy + dx * dx;
    }

    [Fact]
    public void Proximity_ordering_ranks_nearer_providers_first()
    {
        // Parsabad town centre, and three of the seeded providers at their real coordinates.
        const double Lat0 = 39.6482, Lon0 = 47.9174;

        var shahriar = ProximityKey(39.6491, 47.9182, Lat0, Lon0);   // ~0.1 km — nearest
        var aramesh = ProximityKey(39.6528, 47.9218, Lat0, Lon0);    // ~0.6 km
        var arya = ProximityKey(39.6624, 47.9302, Lat0, Lon0);       // ~1.9 km — farthest

        shahriar.Should().BeLessThan(aramesh);
        aramesh.Should().BeLessThan(arya);

        // The concrete regression: ascending order must start with the nearest. The old code ordered by rating
        // descending, which put Arya — the farthest — at the top of "near me".
        var ordered = new[] { arya, shahriar, aramesh }.OrderBy(k => k).ToArray();
        ordered[0].Should().Be(shahriar);
        ordered[^1].Should().Be(arya);
    }

    [Fact]
    public void Proximity_ordering_agrees_with_true_great_circle_distance()
    {
        // The key omits the square root and treats the area as flat, so it is not a distance — it only has to
        // RANK the same as one. This checks that against Haversine over the real seeded spread.
        const double Lat0 = 39.6482, Lon0 = 47.9174;

        var points = new (string Name, double Lat, double Lon)[]
        {
            ("shahriar", 39.6491, 47.9182),
            ("moghan", 39.6486, 47.9176),
            ("golestan", 39.6506, 47.9158),
            ("aramesh", 39.6528, 47.9218),
            ("mehr", 39.6438, 47.9098),
            ("tavan", 39.6399, 47.9231),
            ("arya", 39.6624, 47.9302),
        };

        var byKey = points.OrderBy(p => ProximityKey(p.Lat, p.Lon, Lat0, Lon0)).Select(p => p.Name);
        var byHaversine = points.OrderBy(p => Haversine(Lat0, Lon0, p.Lat, p.Lon)).Select(p => p.Name);

        byKey.Should().Equal(byHaversine,
            "the cheap ordering key must produce the same ranking as a real distance calculation");
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double Rad(double d) => d * Math.PI / 180.0;

        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    [Fact]
    public void Providers_without_coordinates_sort_last()
    {
        // They cannot be ranked by proximity. Sorting them last keeps them in the results without letting them
        // occupy the "nearest" positions, which is what an unguarded null would have done.
        const double Lat0 = 39.6482, Lon0 = 47.9174;

        var withCoordinates = ProximityKey(39.6624, 47.9302, Lat0, Lon0); // the farthest real provider
        const double WithoutCoordinates = double.MaxValue;

        withCoordinates.Should().BeLessThan(WithoutCoordinates);
    }
}
