using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// The platform sells in Toman (openspec/changes/customer-app-discovery-pass). Prices were stamped
/// "USD" at every creation site, so the customer app showed a 1,500,000-Toman haircut as
/// «USD ۱۵۰۰۰۰۰» — the amounts were always Toman and the label was simply wrong.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class PricesAreInTomanTests : ServiceCatalogIntegrationTestBase
{
    public PricesAreInTomanTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_service_the_salon_adds_is_priced_in_toman()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();

        var created = await Client.PostAsJsonAsync($"/api/v1/services/{provider.Id.Value}", new
        {
            serviceName = "اصلاح مو",
            description = "تست",
            durationMinutes = 30,
            price = 250000m,
            category = "Hair",
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());

        var list = await Client.GetAsync($"/api/v1/services/provider/{provider.Id.Value}");
        var data = JObject.Parse(await list.Content.ReadAsStringAsync())["data"]!;
        var services = (JArray)(data["items"] ?? data)!;
        services.Should().NotBeEmpty();
        services.Select(s => s["currency"]!.Value<string>())
            .Should().OnlyContain(c => c == "IRT", "the platform's money is Toman, never dollars");
    }
}
