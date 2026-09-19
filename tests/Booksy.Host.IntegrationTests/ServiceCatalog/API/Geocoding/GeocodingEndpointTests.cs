using System.Net;
using System.Text.Json;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Geocoding;

/// <summary>
/// The map picker's place lookup, through the real host.
///
/// <para>Unit tests over the Nominatim client cannot see what broke in production on 2026-09-19: the
/// controller answered a raw <c>ContentResult</c>, whose Content-Length the host's response-envelope
/// middleware then contradicted, and Kestrel failed the request with 500 — after a perfectly good
/// upstream answer. Only a request through the full pipeline shows that.</para>
///
/// <para>The upstream itself is faked (<c>FakeGeocodingProvider</c> in the test host): these tests are
/// about our contract, and a test suite must never depend on a third-party service being reachable.</para>
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class GeocodingEndpointTests : ServiceCatalogIntegrationTestBase
{
    public GeocodingEndpointTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Search_answers_the_upstream_results_without_authentication()
    {
        // Anonymous on purpose: onboarding picks a location before a provider exists.
        var response = await Client.GetAsync("/api/v1/Geocoding/search?q=%D9%BE%D8%A7%D8%B1%D8%B3%20%D8%A2%D8%A8%D8%A7%D8%AF&limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadAsByteArrayAsync();

        // The defect Kestrel rejected in production: the action declared a Content-Length for the raw
        // upstream JSON, then the envelope middleware wrote a longer body over it. TestServer does not
        // enforce that, so assert the header against the bytes actually written.
        Assert.True(
            response.Content.Headers.ContentLength is null
                || response.Content.Headers.ContentLength == payload.Length,
            $"Content-Length {response.Content.Headers.ContentLength} does not match the {payload.Length} bytes written");

        using var body = JsonDocument.Parse(payload);
        var results = body.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, results.ValueKind);
        Assert.Equal("39.6461735", results[0].GetProperty("lat").GetString());
    }

    [Fact]
    public async Task Reverse_answers_the_address_of_a_tapped_point()
    {
        var response = await Client.GetAsync("/api/v1/Geocoding/reverse?lat=39.64&lon=47.91");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var address = body.RootElement.GetProperty("data").GetProperty("address");
        Assert.Equal("کوچه ۵ سهند", address.GetProperty("road").GetString());
    }

    [Fact]
    public async Task A_query_the_upstream_cannot_answer_is_service_unavailable_not_a_server_fault()
    {
        // The fake returns null for this marker term, as the real client does for any upstream trouble.
        var response = await Client.GetAsync("/api/v1/Geocoding/search?q=upstream-down");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task An_empty_term_is_rejected_before_any_upstream_call()
    {
        var response = await Client.GetAsync("/api/v1/Geocoding/search?q=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
