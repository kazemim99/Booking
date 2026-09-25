using System.Net;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Host.Composition;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Seeders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Reviews;

/// <summary>
/// The demo salon's reviews, written against the real schema and read back through the public API
/// (openspec/changes/_inline/customer-reviews-and-nahal-seed). The 09-22 seeding task was checked off without this:
/// the review seeder only reviews completed bookings, no seeder makes any, and so it has never written a review.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class DemoSalonReviewsSeedTests : ReviewTestBase
{
    public DemoSalonReviewsSeedTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    /// <summary>A bookable salon with a name of its own — the seeder finds its salon by name.</summary>
    private async Task<Provider> SalonAsync()
    {
        var name = $"سالن نهال {Guid.NewGuid():N}"[..20];
        var salon = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()), name, "خدمات آرایش و زیبایی بانوان",
            Domain.Enums.ServiceCategory.BeautySalon,
            ContactInfo.Create(Email.Create("nahal-seed@test.com"), PhoneNumber.From("+989123135143")),
            BusinessAddress.Create("خیابان شهید بهشتی", "خیابان شهید بهشتی", "پارس‌آباد", "اردبیل", "56410", "Iran"));
        salon.SetSatus(Domain.Enums.ProviderStatus.Active);
        salon.SetBusinessHours(Enum.GetValues<Domain.Enums.DayOfWeek>().ToDictionary(
            day => day, _ => ((TimeOnly?)new TimeOnly(9, 0), (TimeOnly?)new TimeOnly(19, 0))));
        await CreateEntityAsync(salon);

        await CreateServiceForProviderAsync(salon, "کوتاهی مو", 350_000m, 45);
        await CreateServiceForProviderAsync(salon, "رنگ مو", 1_200_000m, 120);
        await MakeBookableAsync(salon);
        return salon;
    }

    private async Task<int> SeedAsync(Provider salon)
    {
        using var scope = Factory.Services.CreateScope();
        return await DemoSalonReviews.SeedAsync(scope.ServiceProvider, salon.Profile.BusinessName);
    }

    private async Task<JToken> PublicListingAsync(Provider salon)
    {
        ClearAuthenticationHeader();
        var response = await Client.GetAsync($"/api/v1/reviews/providers/{salon.Id.Value}?pageSize=50");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return Data(body);
    }

    [Fact]
    public async Task The_salon_shows_published_reviews_with_the_salons_replies_named_authors_and_votes()
    {
        var salon = await SalonAsync();

        (await SeedAsync(salon)).Should().Be(DemoSalonReviewsSeeder.ReviewCount);

        var listing = await PublicListingAsync(salon);
        var items = ((JArray)listing["reviews"]!["items"]!).ToList();
        items.Should().HaveCount(DemoSalonReviewsSeeder.ReviewCount, "every seeded review is published");
        items.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i["comment"]!.Value<string>()));
        items.Count(i => i["providerResponse"]?.Type == JTokenType.String)
            .Should().BeGreaterThanOrEqualTo(items.Count / 2, "the salon answers most of them, and its answers are approved");
        items.Select(i => i["rating"]!.Value<decimal>()).Distinct().Should().HaveCountGreaterThanOrEqualTo(4);
        items.Should().Contain(i => i["helpfulCount"]!.Value<int>() > 0);

        var firstNames = DemoSalonReviews.Reviewers.Select(r => r.FirstName).ToHashSet();
        items.Should().OnlyContain(i => firstNames.Contains(i["customerName"]!.Value<string>()!.Split(' ', StringSplitOptions.None)[0])
                                        && i["customerName"]!.Value<string>()!.EndsWith("."),
            "real people, named the way the listing names anyone («مریم ر.»)");

        var average = items.Average(i => i["rating"]!.Value<decimal>());
        listing["statistics"]!["averageRating"]!.Value<decimal>().Should().BeApproximately(average, 0.01m);

        var stored = await LoadProviderAsync(salon.Id);
        stored.PublishedReviewCount.Should().Be(DemoSalonReviewsSeeder.ReviewCount, "the salon's card shows its rating");
        stored.HasRating.Should().BeTrue();
    }

    [Fact]
    public async Task Seeding_again_adds_nothing()
    {
        var salon = await SalonAsync();
        await SeedAsync(salon);

        (await SeedAsync(salon)).Should().Be(0);

        (await PublicListingAsync(salon))["reviews"]!["totalCount"]!.Value<int>()
            .Should().Be(DemoSalonReviewsSeeder.ReviewCount);
    }

    [Fact]
    public async Task Each_review_follows_a_completed_visit_in_the_past()
    {
        var salon = await SalonAsync();
        await SeedAsync(salon);

        var reviews = await FreshAsync(db => db.Reviews.AsNoTracking().Where(r => r.ProviderId == salon.Id).ToListAsync());
        var visits = (await FreshAsync(db => db.Bookings.AsNoTracking().Where(b => b.ProviderId == salon.Id).ToListAsync()))
            .ToDictionary(b => b.Id.Value);
        var pairs = reviews.Select(r => new { r, b = visits[r.BookingId] }).ToList();

        pairs.Should().HaveCount(DemoSalonReviewsSeeder.ReviewCount);
        foreach (var pair in pairs)
        {
            pair.b.Status.Should().Be(Domain.Enums.BookingStatus.Completed);
            pair.b.CustomerId.Should().Be(pair.r.CustomerId, "the reviewer's own visit");
            pair.b.CompletedAt.Should().BeBefore(pair.r.CreatedAt, "written after the visit");
            pair.r.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddDays(-1), "dated when it happened, not at seeding time");
        }
    }

    [Fact]
    public async Task A_reviewer_sees_their_visit_as_already_reviewed()
    {
        var salon = await SalonAsync();
        await SeedAsync(salon);
        var review = await FreshAsync(db => db.Reviews.AsNoTracking().FirstAsync(r => r.ProviderId == salon.Id));

        var row = await MyBookingRowAsync(review.CustomerId.Value, review.BookingId);

        row["canReview"]!.Value<bool>().Should().BeFalse();
        row["reviewId"]!.Value<string>().Should().Be(review.Id.ToString());
        row["reviewStatus"]!.Value<string>().Should().Be("Published");
    }
}
