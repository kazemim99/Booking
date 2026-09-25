using AsanRezerve.ServiceCatalog.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AsanRezerve.ServiceCatalog.Api.UnitTests.Services;

/// <summary>
/// Image URLs handed to the apps must be the address a browser can load. Production terminates TLS in
/// nginx and passes the request on over plain http, so a URL built from the request came out as
/// http://back.nahalkmi.ir/uploads/... — which an HTTPS page refuses as mixed content. None of a
/// provider's gallery photos showed in the admin panel (2026-09-19). A configured public base address
/// wins over the proxied request.
/// </summary>
public class UrlServiceTests
{
    private static UrlService Build(string? publicBaseUrl, string scheme = "http", string host = "back.nahalkmi.ir")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);

        var settings = new Dictionary<string, string?>();
        if (publicBaseUrl != null) settings["App:PublicBaseUrl"] = publicBaseUrl;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new UrlService(new HttpContextAccessor { HttpContext = context }, configuration);
    }

    [Fact]
    public void A_configured_public_address_wins_over_the_proxied_http_request()
    {
        var url = Build("https://back.nahalkmi.ir").ToAbsoluteUrl("/uploads/p/gallery/a_thumb.webp");

        url.Should().Be("https://back.nahalkmi.ir/uploads/p/gallery/a_thumb.webp");
    }

    [Fact]
    public void A_trailing_slash_in_the_setting_does_not_double_up()
    {
        Build("https://back.nahalkmi.ir/").ToAbsoluteUrl("uploads/a.webp")
            .Should().Be("https://back.nahalkmi.ir/uploads/a.webp");
    }

    [Fact]
    public void Without_the_setting_the_request_address_is_used_as_before()
    {
        Build(publicBaseUrl: null, scheme: "http", host: "localhost:5000").ToAbsoluteUrl("/uploads/a.webp")
            .Should().Be("http://localhost:5000/uploads/a.webp");
    }

    [Fact]
    public void An_already_absolute_url_is_left_alone()
    {
        Build("https://back.nahalkmi.ir").ToAbsoluteUrl("https://cdn.example/a.webp")
            .Should().Be("https://cdn.example/a.webp");
    }
}
