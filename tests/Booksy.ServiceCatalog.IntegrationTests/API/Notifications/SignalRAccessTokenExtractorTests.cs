using Booksy.Infrastructure.Security.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Notifications;

/// <summary>
/// C1 SignalR access-token negotiation. The JWT for a hub connection arrives as a `?access_token=` query-string
/// parameter (WebSocket handshakes cannot carry custom headers), so the extractor MUST read the query for /hubs
/// paths. Reading only a header (the previous behavior) would silently fail auth for every browser client.
/// </summary>
public class SignalRAccessTokenExtractorTests
{
    private static IQueryCollection Query(params (string, string)[] kv) =>
        new QueryCollection(kv.ToDictionary(x => x.Item1, x => new Microsoft.Extensions.Primitives.StringValues(x.Item2)));

    private static IHeaderDictionary Headers(params (string, string)[] kv)
    {
        var h = new HeaderDictionary();
        foreach (var (k, v) in kv) h[k] = v;
        return h;
    }

    [Fact]
    public void Reads_the_access_token_from_the_query_string_on_a_hub_path()
    {
        var token = SignalRAccessTokenExtractor.Extract(
            Query(("access_token", "jwt-123")), Headers(), new PathString("/hubs/notifications"));
        token.Should().Be("jwt-123");
    }

    [Fact]
    public void Falls_back_to_the_header_for_non_browser_hub_clients()
    {
        var token = SignalRAccessTokenExtractor.Extract(
            Query(), Headers(("access_token", "jwt-hdr")), new PathString("/hubs/notifications"));
        token.Should().Be("jwt-hdr");
    }

    [Fact]
    public void Ignores_the_access_token_query_on_non_hub_paths()
    {
        // On normal API paths the standard Authorization: Bearer header is authoritative; never trust a query token.
        var token = SignalRAccessTokenExtractor.Extract(
            Query(("access_token", "jwt-123")), Headers(), new PathString("/api/v1/payments"));
        token.Should().BeNull();
    }

    [Fact]
    public void Returns_null_when_no_token_is_present_on_a_hub_path()
    {
        SignalRAccessTokenExtractor.Extract(Query(), Headers(), new PathString("/hubs/notifications"))
            .Should().BeNull();
    }
}
