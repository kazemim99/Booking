using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// The salon page (<c>GET providers/{id}</c>) is the customer apps' most-read screen and is served from the query
/// cache — and it must never show a salon as it was before its owner saved a change
/// (openspec/changes/add-observability-and-caching, read-caching).
/// <para>Eviction hangs off the save pipeline, not off domain events: many mutators raise none (a price or
/// duration change, a staff member leaving), and a change committed straight through <c>DbContext</c> raises none
/// either. Each test reads the page first so that it is cached, changes one thing, and reads again.</para>
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class SalonPageCacheTests : ServiceCatalogIntegrationTestBase
{
    public SalonPageCacheTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private async Task<JToken> SalonPageAsync(Guid providerId, string query = "")
    {
        var response = await Client.GetAsync($"/api/v1/providers/{providerId}{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;
    }

    private CacheRegionStats SalonPageStats() =>
        Factory.Services.GetRequiredService<CacheMetrics>().Snapshot()
            .SingleOrDefault(r => r.Region == "GetProviderByIdQuery")
        ?? new CacheRegionStats("GetProviderByIdQuery", 0, 0, 0);

    [Fact]
    public async Task Reading_the_same_salon_again_is_served_from_the_cache()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        await SalonPageAsync(provider.Id.Value);
        var before = SalonPageStats();

        var again = await SalonPageAsync(provider.Id.Value);

        var after = SalonPageStats();
        (after.Hits - before.Hits).Should().Be(1, "nothing changed, so the second read must not query the database");
        again["businessName"]!.Value<string>().Should().Be(provider.Profile.BusinessName);
    }

    [Fact]
    public async Task Renaming_the_salon_shows_on_the_next_read()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        await SalonPageAsync(provider.Id.Value);

        var response = await Client.PutAsJsonAsync("/api/v1/providers/business", new
        {
            businessName = "سالن نهال نو",
            description = "توضیح تازه",
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        (await SalonPageAsync(provider.Id.Value))["businessName"]!.Value<string>().Should().Be("سالن نهال نو");
    }

    [Fact]
    public async Task A_new_service_shows_on_the_next_read()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var before = await SalonPageAsync(provider.Id.Value, "?includeServices=true");

        var response = await Client.PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/services", new
        {
            serviceName = "رنگ مو",
            description = "رنگ کامل",
            durationHours = 1,
            duration = 30,
            basePrice = 900_000m,
            currency = "IRR",
            category = "Beauty",
            isMobileService = false,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var after = await SalonPageAsync(provider.Id.Value, "?includeServices=true");
        after["serviceCount"]!.Value<int>().Should().Be(before["serviceCount"]!.Value<int>() + 1);
    }

    [Fact]
    public async Task New_opening_hours_show_on_the_next_read()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var before = (await SalonPageAsync(provider.Id.Value))["businessHours"]!.ToString();

        var days = Enumerable.Range(0, 7).Select(day => new
        {
            dayOfWeek = day,
            isOpen = true,
            openTime = new { hours = 10, minutes = 0 },
            closeTime = new { hours = 14, minutes = 30 },
            breaks = (object?)null,
        });
        var response = await Client.PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/business-hours", new { businessHours = days });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var after = (await SalonPageAsync(provider.Id.Value))["businessHours"]!.ToString();
        after.Should().NotBe(before, "the page must show the hours the salon just saved");
    }

    [Fact]
    public async Task A_change_committed_straight_through_the_DbContext_shows_on_the_next_read()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        await SalonPageAsync(provider.Id.Value);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var tracked = await db.Set<Provider>().SingleAsync(p => p.Id == provider.Id);
            tracked.UpdateBusinessProfile("سالن مستقیم", tracked.Profile.BusinessDescription, tracked.Profile.ProfileImageUrl);
            await db.SaveChangesAsync();
        }

        (await SalonPageAsync(provider.Id.Value))["businessName"]!.Value<string>().Should().Be("سالن مستقیم");
    }

    [Fact]
    public async Task A_staff_member_who_leaves_disappears_from_the_next_read()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var memberId = await GetBookableMemberIdAsync(provider);
        var before = (JArray)(await SalonPageAsync(provider.Id.Value, "?includeStaff=true"))["staff"]!;
        before.Select(s => s["id"]!.Value<string>()).Should().Contain(memberId.ToString());

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var membership = await db.Set<OrganizationMembership>().SingleAsync(m => m.Id == memberId);
            membership.Terminate("left the salon");
            await db.SaveChangesAsync();
        }

        var after = (JArray)(await SalonPageAsync(provider.Id.Value, "?includeStaff=true"))["staff"]!;
        after.Select(s => s["id"]!.Value<string>()).Should().NotContain(memberId.ToString());
    }
}
