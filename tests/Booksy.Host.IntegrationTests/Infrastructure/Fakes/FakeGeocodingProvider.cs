using Booksy.ServiceCatalog.Application.Services;

namespace Booksy.Host.IntegrationTests.Infrastructure.Fakes;

/// <summary>
/// Stands in for the Nominatim client so the suite never depends on a third-party service being
/// reachable — while still exercising our own contract: the JSON shape, the envelope the host wraps
/// it in, and the "unavailable" path.
/// </summary>
public sealed class FakeGeocodingProvider : IGeocodingProvider
{
    /// <summary>A term the fake refuses, standing for any upstream trouble (the real client returns null).</summary>
    public const string UnavailableTerm = "upstream-down";

    private const string SearchResult =
        """[{"place_id":208313567,"lat":"39.6461735","lon":"47.9185510","display_name":"پارس آباد"}]""";

    private const string ReverseResult =
        """{"display_name":"کوچه ۵ سهند، محله طالقانی","address":{"road":"کوچه ۵ سهند","neighbourhood":"محله طالقانی","town":"شهر پارس آباد","state":"استان اردبیل"}}""";

    public Task<string?> SearchAsync(string query, int limit, CancellationToken cancellationToken)
        => Task.FromResult(query.Contains(UnavailableTerm) ? null : SearchResult);

    public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken)
        => Task.FromResult<string?>(ReverseResult);
}
