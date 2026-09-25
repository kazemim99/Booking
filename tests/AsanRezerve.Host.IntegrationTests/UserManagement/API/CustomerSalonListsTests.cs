using System.Net.Http.Json;
using System.Text.Json;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;
using SalonAggregate = AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider;

namespace AsanRezerve.UserManagement.IntegrationTests.API;

/// <summary>
/// The customer's two salon lists on Home — favourites (GET /customers/{id}/favorites) and recent
/// visits (GET /customers/{id}/recently-visited) — as the customer app reads them
/// (customer-app-ux-review-fixes P1).
///
/// The rows used to carry only the salon's id, and the app needs a name to draw a card: every
/// non-empty list failed to parse and Home showed «بارگذاری این بخش ناموفق بود». Each row now
/// carries the salon's name, photo, city and rating, read from ServiceCatalog in one batched query,
/// and a salon that is gone or no longer Active is left out rather than sent as a nameless row.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class CustomerSalonListsTests : UserManagementIntegrationTestBase
{
    public CustomerSalonListsTests(AsanRezerveHostFactory factory)
        : base(factory)
    {
    }

    // ------------------------------------------------------------------
    // Favourites
    // ------------------------------------------------------------------

    [Fact]
    public async Task Favorites_CarryEachSalonsNamePhotoCityAndRating()
    {
        var salon = await CustomerSalonSeeding.CreateActiveSalonAsync(
            Scope.ServiceProvider, "سالن آفتاب", city: "تهران",
            logoPath: "uploads/providers/aftab/logo.webp", averageRating: 4.5m, reviews: 2);
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        customer.AddFavoriteProvider(salon.Id.Value, "near work");
        await UpdateEntityAsync(customer);

        var rows = await GetRowsAsync($"/api/v1/customers/{customer.Id.Value}/favorites");

        rows.Should().ContainSingle();
        var row = rows[0];
        row.GetProperty("providerId").GetGuid().Should().Be(salon.Id.Value);
        row.GetProperty("providerName").GetString().Should().Be("سالن آفتاب");
        row.GetProperty("city").GetString().Should().Be("تهران");
        row.GetProperty("averageRating").GetDecimal().Should().Be(4.5m);
        row.GetProperty("totalReviews").GetInt32().Should().Be(2);
        var logo = row.GetProperty("logoUrl").GetString();
        logo.Should().StartWith("http", "a relative path resolves against the app's own host and never loads");
        logo.Should().EndWith("uploads/providers/aftab/logo.webp");
        // The fields the list always had are still there (additive change).
        row.GetProperty("notes").GetString().Should().Be("near work");
        row.TryGetProperty("addedAt", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Favorites_LeaveOutSalonsThatAreGoneOrNotActive()
    {
        var active = await CustomerSalonSeeding.CreateActiveSalonAsync(Scope.ServiceProvider, "Open Salon");
        var inactive = await CustomerSalonSeeding.CreateActiveSalonAsync(Scope.ServiceProvider, "Closed Salon");
        await CustomerSalonSeeding.SetStatusAsync(Scope.ServiceProvider, inactive, ProviderStatus.Inactive);
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        customer.AddFavoriteProvider(active.Id.Value);
        customer.AddFavoriteProvider(inactive.Id.Value);
        customer.AddFavoriteProvider(Guid.NewGuid()); // a salon that no longer exists
        await UpdateEntityAsync(customer);

        var rows = await GetRowsAsync($"/api/v1/customers/{customer.Id.Value}/favorites");

        rows.Select(r => r.GetProperty("providerId").GetGuid())
            .Should().Equal(active.Id.Value);
    }

    [Fact]
    public async Task Favorites_OfAnotherCustomer_AreForbidden()
    {
        var other = await CreateTestCustomerAsync("Other", "Customer", "+989222222222");
        await CreateAndAuthenticateAsCustomerAsync("Me", "Customer", "+989111111111");

        var response = await Client.GetAsync($"/api/v1/customers/{other.Id.Value}/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------
    // Recent visits
    // ------------------------------------------------------------------

    [Fact]
    public async Task RecentVisits_ShowEachSalonOnce_NewestFirst_WithItsName()
    {
        var aftab = await CustomerSalonSeeding.CreateActiveSalonAsync(
            Scope.ServiceProvider, "سالن آفتاب", city: "تهران", averageRating: 4.0m, reviews: 1);
        var mahtab = await CustomerSalonSeeding.CreateActiveSalonAsync(Scope.ServiceProvider, "سالن مهتاب");
        var customer = await CreateAndAuthenticateAsCustomerAsync();

        await VisitAsync(customer.Id.Value, aftab.Id.Value);
        await VisitAsync(customer.Id.Value, mahtab.Id.Value);
        await VisitAsync(customer.Id.Value, aftab.Id.Value); // the same salon again, most recently

        var rows = await GetRowsAsync($"/api/v1/customers/{customer.Id.Value}/recently-visited?limit=10");

        rows.Select(r => r.GetProperty("providerName").GetString())
            .Should().Equal(new[] { "سالن آفتاب", "سالن مهتاب" },
                "a salon visited twice is one row, placed by its newest visit");
        var first = rows[0];
        first.GetProperty("providerId").GetGuid().Should().Be(aftab.Id.Value);
        first.GetProperty("city").GetString().Should().Be("تهران");
        first.GetProperty("averageRating").GetDecimal().Should().Be(4.0m);
        first.GetProperty("totalReviews").GetInt32().Should().Be(1);
        first.GetProperty("visitCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        var lastVisitedAt = first.GetProperty("lastVisitedAt").GetDateTime();
        lastVisitedAt.Should().Be(first.GetProperty("visitedAt").GetDateTime(),
            "visitedAt is kept for older clients and is the same moment");
        lastVisitedAt.Should().BeAfter(rows[1].GetProperty("lastVisitedAt").GetDateTime());
    }

    [Fact]
    public async Task RecentVisits_LeaveOutGoneOrInactiveSalons_AndStillFillTheLimit()
    {
        var older1 = await CustomerSalonSeeding.CreateActiveSalonAsync(Scope.ServiceProvider, "Salon One");
        var older2 = await CustomerSalonSeeding.CreateActiveSalonAsync(Scope.ServiceProvider, "Salon Two");
        var closed = await CustomerSalonSeeding.CreateActiveSalonAsync(Scope.ServiceProvider, "Salon Closed");
        var customer = await CreateAndAuthenticateAsCustomerAsync();

        await VisitAsync(customer.Id.Value, older1.Id.Value);
        await VisitAsync(customer.Id.Value, older2.Id.Value);
        await VisitAsync(customer.Id.Value, closed.Id.Value);
        await VisitAsync(customer.Id.Value, Guid.NewGuid()); // the newest visit is to a salon that is gone
        await CustomerSalonSeeding.SetStatusAsync(Scope.ServiceProvider, closed, ProviderStatus.Inactive);

        var rows = await GetRowsAsync($"/api/v1/customers/{customer.Id.Value}/recently-visited?limit=2");

        rows.Select(r => r.GetProperty("providerName").GetString())
            .Should().Equal(new[] { "Salon Two", "Salon One" },
                "the limit counts the salons the customer can see, not the hidden ones");
    }

    [Fact]
    public async Task RecentVisits_OfAnotherCustomer_AreForbidden()
    {
        var other = await CreateTestCustomerAsync("Other", "Customer", "+989222222222");
        await CreateAndAuthenticateAsCustomerAsync("Me", "Customer", "+989111111111");

        var response = await Client.GetAsync($"/api/v1/customers/{other.Id.Value}/recently-visited");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------

    private async Task VisitAsync(Guid customerId, Guid providerId)
    {
        var post = await Client.PostAsJsonAsync(
            $"/api/v1/customers/{customerId}/recently-visited",
            new { providerId, viewSource = "profile" });
        post.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<List<JsonElement>> GetRowsAsync(string url)
    {
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return CustomerSalonSeeding.Rows(body);
    }
}

/// <summary>Salons in the ServiceCatalog schema for the UserManagement salon-list tests.</summary>
internal static class CustomerSalonSeeding
{
    public static async Task<SalonAggregate> CreateActiveSalonAsync(
        IServiceProvider services,
        string name,
        string city = "Test City",
        string? logoPath = null,
        decimal averageRating = 0m,
        int reviews = 0)
    {
        var salon = SalonAggregate.CreateDraft(
            UserId.From(Guid.NewGuid()),
            "Owner",
            "Test",
            name,
            $"Description for {name}",
            ServiceCategory.Barbershop,
            ContactInfo.Create(
                Email.Create($"{Guid.NewGuid():N}@test.com"),
                PhoneNumber.From("+989121234567")),
            BusinessAddress.Create("1 Test St", "1 Test St", city, "TS", "12345", "IR"),
            logoUrl: logoPath);
        salon.SetSatus(ProviderStatus.Active);
        salon.SetRatingAggregates(averageRating, reviews);

        var db = services.GetRequiredService<ServiceCatalogDbContext>();
        db.Providers.Add(salon);
        await db.SaveChangesAsync();
        return salon;
    }

    public static async Task SetStatusAsync(IServiceProvider services, SalonAggregate salon, ProviderStatus status)
    {
        var db = services.GetRequiredService<ServiceCatalogDbContext>();
        salon.SetSatus(status);
        await db.SaveChangesAsync();
    }

    /// <summary>The list, whether the API sends it bare or wrapped in <c>{ data: [...] }</c>.</summary>
    public static List<JsonElement> Rows(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data))
            root = data;
        return root.EnumerateArray().Select(e => e.Clone()).ToList();
    }
}
