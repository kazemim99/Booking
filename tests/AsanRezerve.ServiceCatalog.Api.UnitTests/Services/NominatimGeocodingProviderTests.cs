using System.Net;
using AsanRezerve.ServiceCatalog.Infrastructure.Services.Geocoding;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AsanRezerve.ServiceCatalog.Api.UnitTests.Services;

/// <summary>
/// The provider app used to call nominatim.openstreetmap.org straight from the browser. That fails on
/// networks that cannot reach the host (reported 2026-09-19), and makes every visitor an anonymous
/// Nominatim client, which its usage policy does not permit. The server now makes the call.
/// <para>These tests pin what the policy requires of us — one identified caller, cached results — and
/// the best-effort contract the app relies on: any upstream trouble yields null, never an exception.</para>
/// </summary>
public class NominatimGeocodingProviderTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _reply;
        public readonly List<HttpRequestMessage> Requests = new();
        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) => _reply = reply;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(_reply(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private static NominatimGeocodingProvider Build(FakeHandler handler)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org") },
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<NominatimGeocodingProvider>.Instance);

    [Fact]
    public async Task Search_passes_the_query_through_and_returns_the_upstream_json()
    {
        var handler = new FakeHandler(_ => Json("""[{"lat":"39.64","lon":"47.91"}]"""));

        var result = await Build(handler).SearchAsync("پارس آباد, اردبیل", limit: 1, CancellationToken.None);

        result.Should().Be("""[{"lat":"39.64","lon":"47.91"}]""");
        var uri = handler.Requests.Single().RequestUri!.ToString();
        uri.Should().Contain("/search").And.Contain("countrycodes=ir").And.Contain("format=jsonv2");
        Uri.UnescapeDataString(uri).Should().Contain("پارس آباد, اردبیل");
    }

    [Fact]
    public async Task Identifies_this_application_to_Nominatim()
    {
        var handler = new FakeHandler(_ => Json("[]"));

        await Build(handler).SearchAsync("تهران", limit: 1, CancellationToken.None);

        var agent = handler.Requests.Single().Headers.UserAgent.ToString();
        agent.Should().NotBeEmpty("Nominatim's usage policy requires an identifying User-Agent");
        agent.Should().Contain("AsanRezerve");
    }

    [Fact]
    public async Task Repeated_lookups_hit_the_upstream_once()
    {
        var handler = new FakeHandler(_ => Json("""[{"lat":"1","lon":"2"}]"""));
        var provider = Build(handler);

        var first = await provider.SearchAsync("کاشان", limit: 1, CancellationToken.None);
        var second = await provider.SearchAsync("کاشان", limit: 1, CancellationToken.None);

        second.Should().Be(first);
        handler.Requests.Should().ContainSingle("a cached answer must not spend the shared Nominatim budget");
    }

    [Fact]
    public async Task Reverse_geocoding_asks_for_the_tapped_point()
    {
        var handler = new FakeHandler(_ => Json("""{"display_name":"x"}"""));

        var result = await Build(handler).ReverseAsync(39.643, 47.897, CancellationToken.None);

        result.Should().Be("""{"display_name":"x"}""");
        var uri = handler.Requests.Single().RequestUri!.ToString();
        uri.Should().Contain("/reverse").And.Contain("lat=39.643").And.Contain("lon=47.897");
    }

    [Fact]
    public async Task An_upstream_failure_yields_null_rather_than_throwing()
    {
        var handler = new FakeHandler(_ => Json("nope", HttpStatusCode.ServiceUnavailable));

        var result = await Build(handler).SearchAsync("تهران", limit: 1, CancellationToken.None);

        result.Should().BeNull("the app keeps whatever the user typed when geocoding is unavailable");
    }

    [Fact]
    public async Task A_network_error_yields_null_rather_than_throwing()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("dns"));

        var result = await Build(handler).ReverseAsync(1, 2, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task An_empty_query_never_reaches_the_upstream()
    {
        var handler = new FakeHandler(_ => Json("[]"));

        var result = await Build(handler).SearchAsync("   ", limit: 1, CancellationToken.None);

        result.Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }
}
