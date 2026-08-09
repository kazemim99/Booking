using System.Net;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Authorization;

/// <summary>
/// Validates the C1 global fallback authorization boundary against the live API
/// surface: every endpoint explicitly marked [AllowAnonymous] must remain reachable
/// without authentication, and every other endpoint must be rejected (401) when
/// called anonymously — so the fallback never accidentally blocks a public endpoint,
/// and never leaves a protected one open.
///
/// A raw <see cref="Factory"/> client sends no X-Test-UserId header, so the
/// IntegrationTest auth handler returns NoResult → the request is anonymous.
/// </summary>
public class AuthorizationBoundaryTests : ServiceCatalogIntegrationTestBase
{
    public AuthorizationBoundaryTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Theory]
    // Discovery / reference data — must stay public
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/categories/popular")]
    [InlineData("/api/v1/locations/provinces")]
    [InlineData("/api/v1/platform/statistics")]
    [InlineData("/api/v1/services/popular")]
    public async Task Public_endpoints_are_reachable_anonymously(string url)
    {
        var anonymous = Factory.CreateClient();

        var response = await anonymous.GetAsync(url);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            $"{url} is [AllowAnonymous] and must not be blocked by the global fallback");
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            $"{url} is public and must not be forbidden");
    }

    [Theory]
    // Customer/provider/admin-scoped — must be rejected without authentication
    [InlineData("/api/v1/bookings/my-bookings")]
    [InlineData("/api/v1/payments/customer/history")]
    [InlineData("/api/v1/payouts/pending")]
    [InlineData("/api/v1/notifications/history")]
    public async Task Protected_endpoints_require_authentication(string url)
    {
        var anonymous = Factory.CreateClient();

        var response = await anonymous.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{url} carries no [AllowAnonymous] and must be protected by the global fallback");
    }
}
