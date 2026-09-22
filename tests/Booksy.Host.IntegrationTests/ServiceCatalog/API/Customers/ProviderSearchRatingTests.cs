using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// The rating a customer sees while choosing a salon, and the order "sort by rating" puts salons in
/// (customer-discovery-journey, as modified by provider-reviews-and-ratings).
/// </summary>
/// <remarks>
/// <para>Before this change every provider's rating was 0.0 — nothing wrote it — and <c>totalReviews</c> was
/// declared on the response but never assigned, so it shipped as a permanent 0. A naive sort on the stored
/// average then puts every new salon at the bottom on "highest first" and, worse, at the TOP on "lowest first".
/// Unrated providers are a band after every rated provider, whichever direction is asked for.</para>
///
/// <para>Ratings are set directly on the provider here: this is the search read path, not moderation. The
/// moderation → recompute → rating path is covered end-to-end elsewhere (task 4.3).</para>
///
/// <para>Each test scopes its results with a marker in the business name, because the shared host holds every
/// provider other test classes create.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class ProviderSearchRatingTests : ServiceCatalogIntegrationTestBase
{
    public ProviderSearchRatingTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static string NewMarker() => $"rtg{Guid.NewGuid():N}"[..12];

    private async Task<string> ProviderAsync(string marker, string label, decimal? average = null, int count = 0)
    {
        var provider = await CreateAndAuthenticateAsProviderAsync($"{marker} {label}", $"{marker}-{label}@test.com");
        if (count > 0)
        {
            provider.SetRatingAggregates(average!.Value, count);
            await UpdateEntityAsync(provider);
        }
        ClearAuthenticationHeader();
        return provider.Id.Value.ToString();
    }

    private async Task<JArray> SearchAsync(string marker, string query = "")
    {
        var response = await Client.GetAsync($"/api/v1/providers/search?searchTerm={marker}&pageSize=50{query}");
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (JArray)JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!["items"]!;
    }

    private static List<string> Order(JArray items) =>
        items.Select(i => i["businessName"]!.Value<string>()!.Split(' ')[1]).ToList();

    [Fact]
    public async Task A_rated_provider_comes_back_with_its_average_and_its_real_review_count()
    {
        var marker = NewMarker();
        var id = await ProviderAsync(marker, "rated", 4.5m, 12);

        var item = (await SearchAsync(marker)).Single(i => i["id"]!.Value<string>() == id);

        item["averageRating"]!.Value<decimal>().Should().Be(4.5m);
        item["totalReviews"]!.Value<int>().Should().Be(12);
    }

    [Fact]
    public async Task An_unrated_provider_comes_back_with_zero_reviews_and_a_numeric_average()
    {
        // Numeric, not null: the deployed web app calls rating.toFixed(1) unguarded. The count is how a client
        // tells "no reviews yet" from a real rating (design D8).
        var marker = NewMarker();
        var id = await ProviderAsync(marker, "new");

        var item = (await SearchAsync(marker)).Single(i => i["id"]!.Value<string>() == id);

        item["totalReviews"]!.Value<int>().Should().Be(0);
        item["averageRating"]!.Type.Should().NotBe(JTokenType.Null);
        item["averageRating"]!.Value<decimal>().Should().Be(0m);
    }

    [Fact]
    public async Task Highest_first_puts_every_unrated_provider_after_every_rated_one()
    {
        var marker = NewMarker();
        await ProviderAsync(marker, "three", 3.0m, 5);
        await ProviderAsync(marker, "unrated");
        await ProviderAsync(marker, "five", 5.0m, 2);
        await ProviderAsync(marker, "four", 4.0m, 1);

        var order = Order(await SearchAsync(marker, "&sortBy=rating&sortDescending=true"));

        order.Should().Equal("five", "four", "three", "unrated");
    }

    [Fact]
    public async Task Lowest_first_still_puts_unrated_providers_last_not_first()
    {
        // The trap: a stored 0.0 sorts ABOVE a real 3.0 on ascending, crowning every new salon "lowest rated".
        var marker = NewMarker();
        await ProviderAsync(marker, "unrated");
        await ProviderAsync(marker, "four", 4.0m, 3);
        await ProviderAsync(marker, "two", 2.0m, 4);

        var order = Order(await SearchAsync(marker, "&sortBy=rating&sortDescending=false"));

        order.Should().Equal("two", "four", "unrated");
    }

    // ── Every other path that shows a rating must show its count too ──
    // Each of these returned a real AverageRating beside a hardcoded TotalReviews = 0 (or, for by-location, an
    // unassigned dynamic that serialised as null) — a rating with nothing to say whether it means anything.

    private async Task<Domain.Aggregates.Provider> RatedProviderAsync(decimal average, int count)
    {
        var marker = NewMarker();
        var provider = await CreateAndAuthenticateAsProviderAsync($"{marker} detail", $"{marker}@test.com");
        provider.SetRatingAggregates(average, count);
        await UpdateEntityAsync(provider);
        ClearAuthenticationHeader();
        return provider;
    }

    [Fact]
    public async Task The_provider_detail_customers_open_carries_the_real_count()
    {
        var id = (await RatedProviderAsync(4.5m, 12)).Id.Value;

        var data = JObject.Parse(await (await Client.GetAsync($"/api/v1/providers/{id}")).Content.ReadAsStringAsync())["data"]!;

        data["averageRating"]!.Value<decimal>().Should().Be(4.5m);
        data["totalReviews"]!.Value<int>().Should().Be(12);
    }

    [Fact]
    public async Task The_by_owner_lookup_carries_the_real_count()
    {
        // The provider app's own lookup: the owner is signed in (the fallback policy requires it).
        var provider = await RatedProviderAsync(3.5m, 7);
        AuthenticateAsProviderOwner(provider);

        var response = await Client.GetAsync($"/api/v1/providers/by-owner/{provider.OwnerId.Value}");
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var data = JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;

        data["totalReviews"]!.Value<int>().Should().Be(7);
    }

    [Fact]
    public async Task The_admin_provider_list_carries_the_real_count()
    {
        var id = (await RatedProviderAsync(4.0m, 9)).Id.Value.ToString();
        AuthenticateAsTestAdmin();

        var response = await Client.GetAsync("/api/v1/providers/by-status/Active?maxResults=1000");
        var body = JToken.Parse(await response.Content.ReadAsStringAsync());
        var items = (JArray)(body is JObject o && o["data"] is JArray a ? a : body);
        var mine = items.Single(i => i["id"]!.Value<string>() == id);

        mine["totalReviews"]!.Value<int>().Should().Be(9);
    }

    [Fact]
    public async Task Searching_by_location_carries_the_real_count()
    {
        // Somewhere no other test puts a provider, so a 1 km radius returns only this one.
        const double lat = -61.123, lon = -123.456;
        var marker = NewMarker();
        var provider = await CreateAndAuthenticateAsProviderAsync($"{marker} located", $"{marker}@test.com");
        provider.UpdateAddress(provider.Address.WithCoordinates(lat, lon));
        provider.SetRatingAggregates(5.0m, 3);
        await UpdateEntityAsync(provider);
        ClearAuthenticationHeader();

        var response = await Client.GetAsync(
            $"/api/v1/providers/by-location?latitude={lat}&longitude={lon}&radiusKm=1&pageSize=50");
        var items = (JArray)JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!["items"]!;
        var mine = items.Single(i => i["id"]!.Value<string>() == provider.Id.Value.ToString());

        mine["averageRating"]!.Value<decimal>().Should().Be(5.0m);
        mine["totalReviews"]!.Value<int>().Should().Be(3);
    }

    [Fact]
    public async Task Distance_without_a_location_falls_back_to_rating_with_unrated_providers_last()
    {
        var marker = NewMarker();
        await ProviderAsync(marker, "unrated");
        await ProviderAsync(marker, "three", 3.0m, 2);
        await ProviderAsync(marker, "five", 5.0m, 2);

        var order = Order(await SearchAsync(marker, "&sortBy=distance"));

        order.Should().Equal("five", "three", "unrated");
    }
}
