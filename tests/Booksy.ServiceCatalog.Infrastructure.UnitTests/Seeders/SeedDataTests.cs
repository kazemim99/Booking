using Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.Infrastructure.UnitTests.Seeders;

/// <summary>
/// The demo catalogue a fresh environment starts with (QA walkthrough 2026-09-22: "seed سالن نهال with all its
/// data, so when we move to a new server everything is seeded there", and "seed a few reviews with likes,
/// dislikes and the salon's reply, just to test").
/// </summary>
public class SeedDataTests
{
    [Fact]
    public void The_demo_catalogue_includes_the_salon_the_product_is_demonstrated_with()
    {
        var providers = ProviderSeeder.DemoProviders();

        var nahal = providers.SingleOrDefault(p => p.Profile.BusinessName == "سالن نهال");
        nahal.Should().NotBeNull("a new environment must come up with the salon used for demos");
        nahal!.Address.City.Should().Be("پارس‌آباد");
        nahal.Address.Latitude.Should().NotBeNull("the map and «nearest» need a real point");
        nahal.AllowOnlineBooking.Should().BeTrue();
    }

    [Fact]
    public void Every_demo_salon_sits_on_its_own_point()
    {
        var points = ProviderSeeder.DemoProviders()
            .Select(p => (p.Address.Latitude, p.Address.Longitude))
            .ToList();

        points.Should().OnlyHaveUniqueItems("identical points make distance meaningless");
    }

    [Theory]
    [InlineData(5.0)]
    [InlineData(4.5)]
    public void A_well_liked_review_collects_mostly_helpful_votes(decimal rating)
    {
        var random = new Random(1234);

        var helpful = 0;
        var total = 0;
        for (var i = 0; i < 200; i++)
        {
            foreach (var vote in ReviewSeeder.PlanVotes(rating, random))
            {
                total++;
                if (vote) helpful++;
            }
        }

        total.Should().BeGreaterThan(0, "a demo review with no votes cannot show the control working");
        (helpful / (double)total).Should().BeGreaterThan(0.6);
    }

    [Fact]
    public void A_poor_review_collects_some_dislikes()
    {
        var random = new Random(4321);

        var notHelpful = 0;
        for (var i = 0; i < 200; i++)
        {
            notHelpful += ReviewSeeder.PlanVotes(1.5m, random).Count(v => !v);
        }

        notHelpful.Should().BeGreaterThan(0);
    }

    [Fact]
    public void A_review_never_collects_an_implausible_crowd()
    {
        var random = new Random(99);

        for (var i = 0; i < 500; i++)
        {
            ReviewSeeder.PlanVotes(4.0m, random).Count.Should().BeLessThanOrEqualTo(ReviewSeeder.MaxSeededVotes);
        }
    }
}
