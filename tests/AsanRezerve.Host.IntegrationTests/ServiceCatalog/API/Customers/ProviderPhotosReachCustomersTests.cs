using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// The salon's photos must reach the people choosing a salon
/// (openspec/changes/customer-app-discovery-pass). The provider app uploads them to the gallery,
/// but search and detail carried only LogoUrl/ProfileImageUrl — which that flow never sets — so
/// every customer-facing card showed a placeholder while three photos sat on the server.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class ProviderPhotosReachCustomersTests : ServiceCatalogIntegrationTestBase
{
    public ProviderPhotosReachCustomersTests(AsanRezerveHostFactory factory) : base(factory)
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

    // QA walkthrough 2026-09-22: "سالن نهال has 3 photos and none load". Uploads are STORED relative
    // ("uploads/providers/…_medium.webp", LocalFileStorageService) and only the gallery endpoint made them absolute,
    // so every read a customer makes carried a relative path — which a Flutter web page resolves against its OWN
    // host, where nginx answers index.html. The two tests above use absolute fixtures, which is why they passed.
    // These use the shape storage really writes.
    private static void ShouldBeAbsoluteFor(string? url, string relative)
    {
        url.Should().NotBeNull();
        url!.Should().StartWith("http", "a relative path resolves against the customer's host, not the API's");
        url.Should().EndWith("/" + relative);
    }

    [Fact]
    public async Task Search_and_detail_send_stored_photos_as_absolute_urls()
    {
        var id = Guid.NewGuid().ToString("N");
        var primary = $"uploads/providers/{id}/gallery/chosen_medium.webp";
        var other = $"uploads/providers/{id}/gallery/other_medium.webp";
        var providerId = await WithGalleryAsync(primary, other);

        var search = await Client.GetAsync("/api/v1/providers/search?pageSize=50");
        var items = (JArray)JObject.Parse(await search.Content.ReadAsStringAsync())["data"]!["items"]!;
        var card = items.Single(i => i["id"]!.Value<string>() == providerId);
        ShouldBeAbsoluteFor(card["logoUrl"]?.Value<string>(), primary);

        var detail = JObject.Parse(await (await Client.GetAsync($"/api/v1/providers/{providerId}")).Content.ReadAsStringAsync())["data"]!;
        ShouldBeAbsoluteFor(detail["logoUrl"]?.Value<string>(), primary);
        foreach (var image in (JArray)detail["images"]!)
        {
            image["mediumUrl"]!.Value<string>().Should().StartWith("http");
            image["thumbnailUrl"]!.Value<string>().Should().StartWith("http");
            image["originalUrl"]!.Value<string>().Should().StartWith("http");
        }
    }

    [Fact]
    public async Task Searching_by_location_sends_stored_photos_as_absolute_urls()
    {
        // Somewhere no other test puts a provider, so a 1 km radius returns only this one.
        const double lat = -62.321, lon = -124.654;
        var id = Guid.NewGuid().ToString("N");
        var primary = $"uploads/providers/{id}/gallery/chosen_medium.webp";
        var provider = await CreateAndAuthenticateAsProviderAsync($"located {id}", $"{id}@test.com");
        provider.UpdateAddress(provider.Address.WithCoordinates(lat, lon));
        provider.Profile.AddGalleryImage(provider.Id, primary, primary, primary);
        await UpdateEntityAsync(provider);
        ClearAuthenticationHeader();

        var response = await Client.GetAsync(
            $"/api/v1/providers/by-location?latitude={lat}&longitude={lon}&radiusKm=1&pageSize=50");
        var items = (JArray)JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!["items"]!;
        var card = items.Single(i => i["id"]!.Value<string>() == provider.Id.Value.ToString());
        ShouldBeAbsoluteFor(card["logoUrl"]?.Value<string>(), primary);
    }
}
