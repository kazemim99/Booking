using System.Net;
using Booksy.Tests.Common;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Authorization;

/// <summary>
/// The admin panel's provider pages answered 403 to the real administrator (2026-09-19). The
/// "AdminOnly" policy required "Administrator" or "SysAdmin", while the role production actually
/// grants an administrator — and the one the [Authorize(Roles = "Admin,...")] endpoints check — is
/// "Admin". The shared test admin carries all three names (FOLLOW-UPS #46), which is exactly why no
/// test saw it. These sign in with the single role a production admin has.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class AdminRoleNameTests : ServiceCatalogIntegrationTestBase
{
    public AdminRoleNameTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private void AuthenticateAsProductionAdmin() => AuthenticateAs(new TestUser
    {
        UserId = Guid.NewGuid().ToString(),
        Email = "kazemi.mst@nahalkmi.ir",
        Name = "kazemi.mst",
        Role = "Admin",
    });

    [Fact]
    public async Task An_admin_can_list_providers_by_status()
    {
        AuthenticateAsProductionAdmin();

        var response = await Client.GetAsync("/api/v1/Providers/by-status/Verified");

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "the admin panel's provider list runs on this endpoint");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_provider_still_cannot_list_providers_by_status()
    {
        AuthenticateAs(new TestUser
        {
            UserId = Guid.NewGuid().ToString(),
            Email = "owner@test.com",
            Name = "owner",
            Role = "Provider",
        });

        var response = await Client.GetAsync("/api/v1/Providers/by-status/Verified");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
