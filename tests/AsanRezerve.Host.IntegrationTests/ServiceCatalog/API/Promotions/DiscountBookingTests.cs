using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Promotions;

/// <summary>
/// Discounts end to end through the composed host and a real database (openspec/changes/add-discounts-and-campaigns):
/// a salon's offer priced by the quote and by booking creation alike, the booking's snapshot, the redemption
/// released by a cancellation and carried by a reschedule, opt-in campaigns, walk-ins left alone, the usage limit
/// under concurrency, and who may reach which endpoint.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class DiscountBookingTests : ServiceCatalogIntegrationTestBase
{
    private static int _phoneCounter;

    public DiscountBookingTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static string UniquePhone() => $"+98912{2000000 + Interlocked.Increment(ref _phoneCounter):D7}";

    private static DateTime Slot(int daysAhead, int hour)
    {
        var day = DateTime.UtcNow.Date.AddDays(daysAhead);
        return day.AddHours(hour);
    }

    private static Guid G(JToken token) => Guid.Parse(token.Value<string>()!);

    /// <summary>The envelope's message, decoded — the raw body escapes every Persian letter.</summary>
    private static async Task<string> MessageAsync(HttpResponseMessage response) =>
        JObject.Parse(await response.Content.ReadAsStringAsync())["message"]!.Value<string>()!;

    private static async Task<JToken> DataAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected, body);
        var root = JToken.Parse(body);
        return root is JObject o && o["data"] is { } data ? data : root;
    }

    private async Task<(AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider Provider, AsanRezerve.ServiceCatalog.Domain.Aggregates.Service Service)> SalonAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        return (provider, service);
    }

    private async Task<Guid> CreatePromotionAsync(
        AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider provider, object terms)
    {
        AuthenticateAsProviderOwner(provider);
        var created = await DataAsync(
            await Client.PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/promotions", terms),
            HttpStatusCode.Created);
        ClearAuthenticationHeader();
        return G(created["id"]!);
    }

    private static object Percent(decimal value, string activation = "Automatic", string? code = null,
        int? totalUsageLimit = null, int? perCustomerLimit = null) => new
    {
        title = "تخفیف آزمایشی",
        activation,
        code,
        discountKind = "Percentage",
        discountValue = value,
        totalUsageLimit,
        perCustomerLimit,
    };

    private async Task<HttpResponseMessage> BookAsync(
        AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider provider, AsanRezerve.ServiceCatalog.Domain.Aggregates.Service service, DateTime start, string? code = null) =>
        await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = start,
            promotionCode = code,
        });

    private async Task<JToken> QuoteAsync(
        AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider provider, AsanRezerve.ServiceCatalog.Domain.Aggregates.Service service, DateTime start, string? code = null) =>
        await DataAsync(await Client.PostAsJsonAsync("/api/v1/bookings/quote", new
        {
            providerId = provider.Id.Value,
            serviceIds = new[] { service.Id.Value },
            startTime = start,
            promotionCode = code,
        }), HttpStatusCode.OK);

    private async Task<int> RedemptionCountAsync(Guid promotionId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context.ServiceCatalogDbContext>();
        return await db.Promotions.AsNoTracking().Where(p => p.Id == promotionId).Select(p => p.RedemptionCount).SingleAsync();
    }

    private async Task<Guid> SuccessorOfAsync(Guid bookingId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context.ServiceCatalogDbContext>();
        var previous = AsanRezerve.ServiceCatalog.Domain.ValueObjects.BookingId.From(bookingId);
        var successor = await db.Bookings.AsNoTracking().Where(b => b.PreviousBookingId == previous).Select(b => b.Id).SingleAsync();
        return successor.Value;
    }

    [Fact]
    public async Task A_customer_is_quoted_and_booked_at_the_salons_discounted_price()
    {
        var (provider, service) = await SalonAsync();
        var price = service.BasePrice.Amount;
        var promotionId = await CreatePromotionAsync(provider, Percent(20));
        var start = Slot(3, 11);

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var quote = await QuoteAsync(provider, service, start);
        quote["subtotal"]!.Value<decimal>().Should().Be(price);
        quote["discount"]!.Value<decimal>().Should().Be(Math.Floor(price * 0.2m));
        G(quote["appliedDiscount"]!["promotionId"]!).Should().Be(promotionId);

        var booking = await DataAsync(await BookAsync(provider, service, start), HttpStatusCode.Created);
        booking["totalPrice"]!.Value<decimal>().Should().Be(quote["total"]!.Value<decimal>());
        booking["discountAmount"]!.Value<decimal>().Should().Be(quote["discount"]!.Value<decimal>());
        booking["subtotal"]!.Value<decimal>().Should().Be(price);

        var details = await DataAsync(await Client.GetAsync($"/api/v1/bookings/{G(booking["id"]!)}"), HttpStatusCode.OK);
        details["discountTitle"]!.Value<string>().Should().Be("تخفیف آزمایشی");
        (await RedemptionCountAsync(promotionId)).Should().Be(1);
    }

    [Fact]
    public async Task A_booking_made_without_any_promotion_is_priced_exactly_as_before()
    {
        var (provider, service) = await SalonAsync();

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var booking = await DataAsync(await BookAsync(provider, service, Slot(3, 12)), HttpStatusCode.Created);

        booking["totalPrice"]!.Value<decimal>().Should().Be(service.BasePrice.Amount);
        booking["discountAmount"]!.Value<decimal>().Should().Be(0m);
    }

    [Fact]
    public async Task Cancelling_gives_a_single_use_code_back_to_the_customer()
    {
        var (provider, service) = await SalonAsync();
        var promotionId = await CreatePromotionAsync(provider, Percent(30, "Code", "ONCE-ONLY", perCustomerLimit: 1));

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var booking = await DataAsync(await BookAsync(provider, service, Slot(4, 10), "once-only"), HttpStatusCode.Created);
        (await QuoteAsync(provider, service, Slot(4, 13), "ONCE-ONLY"))["codeOutcome"]!.Value<string>()
            .Should().Be("NotEligible", "the customer already holds their one use");

        var cancelled = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{G(booking["id"]!)}/cancel", new { reason = "تغییر برنامه" });
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK, await cancelled.Content.ReadAsStringAsync());

        (await QuoteAsync(provider, service, Slot(4, 13), "ONCE-ONLY"))["codeOutcome"]!.Value<string>().Should().Be("Applied");
        (await RedemptionCountAsync(promotionId)).Should().Be(0);
    }

    [Fact]
    public async Task A_reschedule_keeps_the_price_and_the_redemption()
    {
        var (provider, service) = await SalonAsync();
        var promotionId = await CreatePromotionAsync(provider, Percent(20));

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var booking = await DataAsync(await BookAsync(provider, service, Slot(5, 10)), HttpStatusCode.Created);

        var originalId = G(booking["id"]!);
        await DataAsync(await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{originalId}/reschedule",
            new { newStartTime = Slot(5, 14), reason = "کار پیش آمد" }), HttpStatusCode.OK);
        var successorId = await SuccessorOfAsync(originalId);

        var successor = await DataAsync(await Client.GetAsync($"/api/v1/bookings/{successorId}"), HttpStatusCode.OK);
        successor["discountAmount"]!.Value<decimal>().Should().Be(booking["discountAmount"]!.Value<decimal>());
        (await RedemptionCountAsync(promotionId)).Should().Be(1);

        // The redemption followed the booking: cancelling the successor releases it.
        await Client.PostAsJsonAsync($"/api/v1/bookings/{successorId}/cancel", new { reason = "لغو" });
        (await RedemptionCountAsync(promotionId)).Should().Be(0);
    }

    [Fact]
    public async Task A_code_the_customer_cannot_have_fails_the_booking_with_the_reason()
    {
        var (provider, service) = await SalonAsync();

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var response = await BookAsync(provider, service, Slot(3, 15), "NO-SUCH-CODE");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MessageAsync(response)).Should().Contain("کد تخفیف معتبر نیست");
    }

    [Fact]
    public async Task A_walk_in_the_salon_enters_is_never_discounted()
    {
        var (provider, service) = await SalonAsync();
        var promotionId = await CreatePromotionAsync(provider, Percent(20));

        AuthenticateAsProviderOwner(provider);
        var booking = await DataAsync(await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = Slot(3, 16),
            walkInFirstName = "سارا",
            walkInLastName = "احمدی",
            walkInPhone = UniquePhone(),
            notifyCustomer = false,
        }), HttpStatusCode.Created);

        booking["totalPrice"]!.Value<decimal>().Should().Be(service.BasePrice.Amount);
        (await RedemptionCountAsync(promotionId)).Should().Be(0);
    }

    [Fact]
    public async Task A_platform_campaign_applies_only_where_the_salon_joined()
    {
        var (provider, service) = await SalonAsync();

        AuthenticateAsTestAdmin();
        var campaign = await DataAsync(await Client.PostAsJsonAsync("/api/v1/admin/promotions", Percent(15)), HttpStatusCode.Created);
        var campaignId = G(campaign["id"]!);

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        (await QuoteAsync(provider, service, Slot(6, 11)))["discount"]!.Value<decimal>().Should().Be(0m);

        AuthenticateAsProviderOwner(provider);
        var joined = await DataAsync(await Client.PostAsync(
            $"/api/v1/providers/{provider.Id.Value}/campaigns/{campaignId}/enrollment", null), HttpStatusCode.OK);
        joined["isJoined"]!.Value<bool>().Should().BeTrue();

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var quote = await QuoteAsync(provider, service, Slot(6, 11));
        G(quote["appliedDiscount"]!["promotionId"]!).Should().Be(campaignId);
        quote["appliedDiscount"]!["owner"]!.Value<string>().Should().Be("Platform");
    }

    [Fact]
    public async Task Two_bookings_racing_for_the_last_use_never_both_get_it()
    {
        var (provider, service) = await SalonAsync();
        var promotionId = await CreatePromotionAsync(provider, Percent(20, totalUsageLimit: 1));

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var responses = await Task.WhenAll(
            BookAsync(provider, service, Slot(7, 10)),
            BookAsync(provider, service, Slot(7, 13)));

        var discounted = 0;
        foreach (var response in responses)
        {
            if (response.StatusCode != HttpStatusCode.Created)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Conflict, "a lost race is a conflict to retry");
                var body = await response.Content.ReadAsStringAsync();
                // Lost on the promotion's counter it is PROMOTION_UNAVAILABLE; lost on the row itself, the generic
                // concurrency conflict. Either way: retry, never a discount granted twice.
                body.Should().MatchRegex("PROMOTION_UNAVAILABLE|CONCURRENCY_CONFLICT");
                continue;
            }

            var booking = await DataAsync(response, HttpStatusCode.Created);
            if (booking["discountAmount"]!.Value<decimal>() > 0)
                discounted++;
        }

        discounted.Should().BeLessThanOrEqualTo(1);
        (await RedemptionCountAsync(promotionId)).Should().Be(discounted);
    }

    [Fact]
    public async Task The_public_offers_list_needs_no_sign_in_and_never_shows_codes()
    {
        var (provider, _) = await SalonAsync();
        await CreatePromotionAsync(provider, Percent(10));
        await CreatePromotionAsync(provider, Percent(30, "Code", "SECRET"));

        ClearAuthenticationHeader();
        var offers = (JArray)await DataAsync(
            await Client.GetAsync($"/api/v1/providers/{provider.Id.Value}/offers"), HttpStatusCode.OK);

        offers.Should().HaveCount(1);
        offers.ToString().Should().NotContain("SECRET");
    }

    [Fact]
    public async Task Only_the_salon_manages_its_promotions_and_only_admins_run_campaigns()
    {
        var (provider, _) = await SalonAsync();

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        (await Client.GetAsync($"/api/v1/providers/{provider.Id.Value}/promotions")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync("/api/v1/admin/promotions", Percent(10))).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);

        ClearAuthenticationHeader();
        (await Client.GetAsync($"/api/v1/providers/{provider.Id.Value}/promotions")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await Client.PostAsJsonAsync("/api/v1/bookings/quote", new { providerId = provider.Id.Value })).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_invalid_promotion_is_refused_with_a_persian_message()
    {
        var (provider, _) = await SalonAsync();

        AuthenticateAsProviderOwner(provider);
        var response = await Client.PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/promotions", Percent(95));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MessageAsync(response)).Should().Contain("درصد تخفیف باید بین ۱ تا ۹۰ باشد");
    }
}
