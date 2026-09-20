using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// The salon's photos must reach the people choosing a salon
/// (openspec/changes/customer-app-discovery-pass). The provider app uploads them to the gallery,
/// but search and detail carried only LogoUrl/ProfileImageUrl — which that flow never sets — so
/// every customer-facing card showed a placeholder while three photos sat on the server.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class ProviderPhotosReachCustomersTests : ServiceCatalogIntegrationTestBase
{
    public ProviderPhotosReachCustomersTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task<string> WithGalleryAsync(string primaryUrl, string otherUrl)
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();
        provider.Profile.AddGalleryImage(provider.Id, otherUrl, otherUrl, otherUrl);
        provider.Profile.AddGalleryImage(provider.Id, primaryUrl, primaryUrl, primaryUrl);
        var chosen = provider.Profile.GalleryImages.Single(i => i.MediumUrl == primaryUrl);
        provider.Profile.SetPrimaryGalleryImage(chosen.Id);
        await UpdateEntityAsync(provider);
        return provider.Id.Value.ToString();
    }

    [Fact]
    public async Task The_photo_the_salon_chose_is_what_a_customer_sees_on_its_card()
    {
        const string primary = "https://back.nahalkmi.ir/uploads/providers/x/gallery/chosen_medium.webp";
        const string other = "https://back.nahalkmi.ir/uploads/providers/x/gallery/other_medium.webp";
        var providerId = await WithGalleryAsync(primary, other);

        var search = await Client.GetAsync("/api/v1/providers/search?pageSize=50");
        var items = (JArray)JObject.Parse(await search.Content.ReadAsStringAsync())["data"]!["items"]!;
        var mine = items.Single(i => i["id"]!.Value<string>() == providerId);

        var shown = mine["logoUrl"]?.Value<string>() ?? mine["profileImageUrl"]?.Value<string>();
        shown.Should().Be(primary, "the customer app shows logoUrl ?? profileImageUrl");
    }

    [Fact]
    public async Task The_profile_carries_every_photo_so_they_can_be_paged_through()
    {
        const string primary = "https://back.nahalkmi.ir/uploads/providers/y/gallery/chosen_medium.webp";
        const string other = "https://back.nahalkmi.ir/uploads/providers/y/gallery/other_medium.webp";
        var providerId = await WithGalleryAsync(primary, other);

        var detail = await Client.GetAsync($"/api/v1/providers/{providerId}");
        var data = JObject.Parse(await detail.Content.ReadAsStringAsync())["data"]!;

        var images = (JArray)data["images"]!;
        images.Select(i => i["mediumUrl"]!.Value<string>()).Should().Contain(new[] { primary, other });
        images.First()["mediumUrl"]!.Value<string>().Should().Be(primary, "the chosen photo leads");
        (data["logoUrl"]?.Value<string>() ?? data["profileImageUrl"]?.Value<string>())
            .Should().Be(primary);
    }
}
