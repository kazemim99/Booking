using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;
using ProviderStatus = AsanRezerve.ServiceCatalog.Domain.Enums.ProviderStatus;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// A customer finds only salons the platform has approved (customer-app-ux-review-fixes, task I.0).
/// </summary>
/// <remarks>
/// <para>Production, 2026-09-23: «سالن تست خودکار» (Drafted) and «تجهیزات پزشکی آسان مدیکال» (PendingVerification)
/// appeared in customer search next to the one real salon, because search hid only Archived salons. Salons go
/// through a verification step — the admin panel activates them — so every public listing (search, the category
/// page that is built on search, and by-location behind the map and "nearby") shows Active salons only.</para>
///
/// <para>Given one salon in every status, all at one spot and sharing a marker in their names,
/// when a customer searches for the marker, opens the category or looks at that spot on the map,
/// then only the Active salon is listed; and when the admin panel asks with <c>includeInactive=true</c>,
/// every one of them is still returned.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class PublicListingsShowActiveSalonsOnlyTests : ServiceCatalogIntegrationTestBase
{
    // A spot no other test uses, so a 1 km radius holds only the salons seeded here.
    private const double Lat = -48.876, Lon = -123.393;

    public PublicListingsShowActiveSalonsOnlyTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private async Task<(string Marker, Dictionary<ProviderStatus, string> Ids)> OneSalonPerStatusAsync()
    {
        var marker = $"pub{Guid.NewGuid():N}"[..12];
        var ids = new Dictionary<ProviderStatus, string>();
        foreach (var status in Enum.GetValues<ProviderStatus>())
        {
            var provider = await CreateProviderWithStatusAsync(Guid.NewGuid(), $"{marker} {status}", status);
            provider.UpdateAddress(provider.Address.WithCoordinates(Lat, Lon));
            await UpdateEntityAsync(provider);
            ids[status] = provider.Id.Value.ToString();
        }
        ClearAuthenticationHeader();
        return (marker, ids);
    }

    private async Task<List<string>> IdsFromAsync(string url)
    {
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        return ((JArray)JObject.Parse(body)["data"]!["items"]!).Select(i => i["id"]!.Value<string>()!).ToList();
    }

    [Fact]
    public async Task Search_lists_the_active_salon_and_none_of_the_others()
    {
        var (marker, ids) = await OneSalonPerStatusAsync();

        var listed = await IdsFromAsync($"/api/v1/providers/search?searchTerm={marker}&pageSize=50");

        listed.Should().BeEquivalentTo(new[] { ids[ProviderStatus.Active] },
            "Drafted, PendingVerification, Verified, Inactive, Suspended and Archived salons are not open to customers");
    }

    [Fact]
    public async Task The_map_and_nearby_list_the_active_salon_and_none_of_the_others()
    {
        var (_, ids) = await OneSalonPerStatusAsync();

        var listed = await IdsFromAsync($"/api/v1/providers/by-location?latitude={Lat}&longitude={Lon}&radiusKm=1&pageSize=50");

        listed.Should().BeEquivalentTo(new[] { ids[ProviderStatus.Active] });
    }

    [Fact]
    public async Task The_category_page_lists_the_active_salon_and_none_of_the_others()
    {
        // CreateProviderWithStatusAsync registers every salon as a barbershop.
        var (_, ids) = await OneSalonPerStatusAsync();

        var listed = await IdsFromAsync("/api/v1/categories/barbershop/providers?pageSize=100");

        listed.Should().Contain(ids[ProviderStatus.Active]);
        listed.Should().NotContain(ids.Where(kv => kv.Key != ProviderStatus.Active).Select(kv => kv.Value));
    }

    [Fact]
    public async Task Search_with_includeInactive_still_returns_every_salon_for_the_admin_panel()
    {
        var (marker, ids) = await OneSalonPerStatusAsync();

        var listed = await IdsFromAsync($"/api/v1/providers/search?searchTerm={marker}&pageSize=50&includeInactive=true");

        listed.Should().BeEquivalentTo(ids.Values);
    }
}
