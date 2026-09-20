using FluentAssertions;

namespace Booksy.Host.CompositionTests;

/// <summary>
/// Every browser client deployed against this API must be an allowed CORS origin, or the browser
/// blocks each call before the API sees it — a failure that looks like "the app is broken" and never
/// reaches the server logs. The origin list lives in appsettings.json with nothing checking it, which
/// is how the admin panel at admin.nahalkmi.ir would have been deployed unable to log in (2026-09-19).
/// </summary>
[Collection(HostCompositionCollection.Name)]
public sealed class CorsOriginsCompositionTests
{
    private readonly HostCompositionFactory _factory;

    public CorsOriginsCompositionTests(HostCompositionFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("https://back.nahalkmi.ir")]      // Vue web app, same origin as the API
    [InlineData("https://provider.nahalkmi.ir")]  // provider app (Flutter web)
    [InlineData("https://customer.nahalkmi.ir")]  // customer app (Flutter web)
    [InlineData("https://admin.nahalkmi.ir")]     // admin panel
    public async Task A_deployed_client_origin_may_call_the_api(string origin)
    {
        var response = await Preflight(origin);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowed).Should().BeTrue(
            $"{origin} is a deployed client; without this header the browser blocks every call it makes");
        allowed.Should().ContainSingle().Which.Should().Be(origin);
    }

    [Fact]
    public async Task An_unknown_origin_is_not_allowed()
    {
        var response = await Preflight("https://evil.example");

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    private async Task<HttpResponseMessage> Preflight(string origin)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/Auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
        return await client.SendAsync(request);
    }
}
