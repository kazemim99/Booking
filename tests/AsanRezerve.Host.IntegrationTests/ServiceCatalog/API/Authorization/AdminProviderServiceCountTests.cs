using System.Net;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Authorization;

/// <summary>
/// The admin panel showed «سالن نهال» with zero services although it had 8 active ones
/// (production, 2026-09-19). The provider queries counted the Provider aggregate's own Services
/// collection, which stays empty since services became their own aggregate; the services live in
/// the services table and must be counted there.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class AdminProviderServiceCountTests : ServiceCatalogIntegrationTestBase
{
    public AdminProviderServiceCountTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Provider_details_count_the_providers_real_services()
    {
        var (provider, _) = await CreateProviderWithServicesAsync(serviceCount: 3);
        AuthenticateAsAdmin();

        var response = await Client.GetAsync($"/api/v1/Providers/{provider.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;
        data["serviceCount"]!.Value<int>().Should().Be(3);
    }

    [Fact]
    public async Task The_admin_provider_list_counts_each_providers_real_services()
    {
        var (withThree, _) = await CreateProviderWithServicesAsync(serviceCount: 3);
        AuthenticateAsAdmin();

        var response = await Client.GetAsync("/api/v1/Providers/by-status/Active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = (JArray)JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;
        var row = rows.Single(r => r["id"]!.Value<string>() == withThree.Id.Value.ToString());
        row["serviceCount"]!.Value<int>().Should().Be(3);
    }
}
