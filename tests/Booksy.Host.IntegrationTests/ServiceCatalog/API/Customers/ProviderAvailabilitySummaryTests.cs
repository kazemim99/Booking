using System.Net;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// "How many free times does this salon have?" on a results card
/// (openspec/changes/customer-app-discovery-pass). A card per request would be one round trip per
/// salon on screen, so the answer comes for a list of salons at once, and names the day it found —
/// a salon fully booked today but free tomorrow is still worth showing.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class ProviderAvailabilitySummaryTests : ServiceCatalogIntegrationTestBase
{
    public ProviderAvailabilitySummaryTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task<JArray> SummaryFor(params Guid[] providerIds)
    {
        var ids = string.Join("&providerIds=", providerIds);
        var response = await Client.GetAsync($"/api/v1/providers/availability-summary?providerIds={ids}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (JArray)JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;
    }

    [Fact]
    public async Task A_salon_with_open_days_reports_free_times_and_the_day_they_are_on()
    {
        var provider = await CreateTestProviderWithServicesAsync();

        var summary = (await SummaryFor(provider.Id.Value)).Single();

        summary["providerId"]!.Value<string>().Should().Be(provider.Id.Value.ToString());
        summary["freeSlotCount"]!.Value<int>().Should().BeGreaterThan(0);
        summary["date"]!.Type.Should().NotBe(JTokenType.Null, "the card says which day those times are on");
        summary["firstFreeTime"]!.Type.Should().NotBe(JTokenType.Null);
    }

    [Fact]
    public async Task A_salon_with_nothing_bookable_reports_none_rather_than_failing()
    {
        // A provider with no services at all can never be booked.
        var provider = await CreateAndAuthenticateAsProviderAsync("Empty", "empty@test.com");

        var summaries = await SummaryFor(provider.Id.Value);

        summaries.Should().ContainSingle(summaries.ToString());
        summaries.Single()["freeSlotCount"]!.Value<int>().Should().Be(0);
        summaries.Single()["firstFreeTime"]?.Type.Should().Be(JTokenType.Null);
    }

    [Fact]
    public async Task Several_salons_are_answered_in_one_request()
    {
        var first = await CreateTestProviderWithServicesAsync();
        var second = await CreateTestProviderWithServicesAsync();

        var summary = await SummaryFor(first.Id.Value, second.Id.Value);

        summary.Should().HaveCount(2);
        summary.Select(s => s["providerId"]!.Value<string>())
            .Should().BeEquivalentTo(new[] { first.Id.Value.ToString(), second.Id.Value.ToString() });
    }

    [Fact]
    public async Task Asking_about_nobody_is_an_empty_answer_not_an_error()
    {
        var response = await Client.GetAsync("/api/v1/providers/availability-summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ((JArray)JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!).Should().BeEmpty();
    }
}
