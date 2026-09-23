using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// A salon that edits its name or description must keep its photos
/// (openspec/changes/_inline/salon-images-load, task 1). Both edits replaced the owned business profile with an
/// empty one, and the write repository deleted every gallery row missing from it — so a rename from the provider
/// app (More → business details) silently removed the salon's photos from every customer card.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class EditingTheSalonKeepsItsPhotosTests : ServiceCatalogIntegrationTestBase
{
    public EditingTheSalonKeepsItsPhotosTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task<(string ProviderId, string Primary)> SalonWithPhotosAsync()
    {
        var id = Guid.NewGuid().ToString("N");
        var primary = $"uploads/providers/{id}/gallery/chosen_medium.webp";
        var other = $"uploads/providers/{id}/gallery/other_medium.webp";

        var provider = await CreateAndAuthenticateAsProviderAsync();
        provider.Profile.AddGalleryImage(provider.Id, other, other, other);
        provider.Profile.AddGalleryImage(provider.Id, primary, primary, primary);
        provider.Profile.SetPrimaryGalleryImage(provider.Profile.GalleryImages.Single(i => i.MediumUrl == primary).Id);
        await UpdateEntityAsync(provider);
        return (provider.Id.Value.ToString(), primary);
    }

    private async Task<JToken> DetailAsync(string providerId)
    {
        var response = await Client.GetAsync($"/api/v1/providers/{providerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;
    }

    private async Task ShouldStillShowBothPhotosAsync(string providerId, string primary)
    {
        var detail = await DetailAsync(providerId);
        ((JArray)detail["images"]!).Should().HaveCount(2, "no edit of name or description may remove a photo");
        detail["logoUrl"]!.Value<string>().Should().EndWith("/" + primary, "the photo the salon chose still leads");

        var gallery = await Client.GetAsync($"/api/v1/providers/{providerId}/gallery");
        var data = JToken.Parse(await gallery.Content.ReadAsStringAsync());
        ((JArray)(data["data"] ?? data)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Renaming_the_salon_from_the_provider_app_keeps_every_photo()
    {
        var (providerId, primary) = await SalonWithPhotosAsync();

        // Exactly what the provider app sends (home_api_service.dart updateBusinessInfo).
        var response = await Client.PutAsJsonAsync("/api/v1/providers/business", new
        {
            businessName = "سالن نهال نو",
            description = "توضیح تازه",
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        (await DetailAsync(providerId))["businessName"]!.Value<string>().Should().Be("سالن نهال نو");
        await ShouldStillShowBothPhotosAsync(providerId, primary);
    }

    [Fact]
    public async Task Changing_the_profile_image_keeps_every_photo()
    {
        var (providerId, primary) = await SalonWithPhotosAsync();

        var response = await Client.PutAsJsonAsync("/api/v1/providers/profile", new
        {
            profileImageUrl = $"/uploads/providers/{providerId}/profile_new.jpg",
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        await ShouldStillShowBothPhotosAsync(providerId, primary);
    }
}
