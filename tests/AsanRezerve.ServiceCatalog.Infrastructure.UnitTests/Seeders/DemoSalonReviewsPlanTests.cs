using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Seeders;
using FluentAssertions;
using Xunit;

namespace AsanRezerve.ServiceCatalog.Infrastructure.UnitTests.Seeders;

/// <summary>
/// The shape of the demo salon's reviews (openspec/changes/_inline/customer-reviews-and-nahal-seed): «یکسری کامنت با
/// پاسخ، random و rate». Random from a fixed seed, so every environment shows the same salon; mostly good with an honest
/// few poor ones, comments that match their stars, and the salon answering most — every poor one.
/// </summary>
public class DemoSalonReviewsPlanTests
{
    /// <summary>One per review: a customer reviews a salon once (reviews-and-reschedule-round2 D4).</summary>
    private const int Reviewers = DemoSalonReviewsSeeder.ReviewCount;

    private static IReadOnlyList<DemoSalonReviewsSeeder.PlannedReview> ThePlan() =>
        DemoSalonReviewsSeeder.Plan(new Random(DemoSalonReviewsSeeder.Seed), Reviewers);

    private static IEnumerable<int> Seeds() => Enumerable.Range(1, 200);

    [Fact]
    public void Every_environment_gets_the_same_reviews()
    {
        ThePlan().Should().BeEquivalentTo(ThePlan(), o => o.WithStrictOrdering());
    }

    [Fact]
    public void The_salon_gets_a_believable_spread_of_ratings()
    {
        var ratings = ThePlan().Select(p => p.Rating).ToList();

        ratings.Should().HaveCount(DemoSalonReviewsSeeder.ReviewCount);
        ratings.Distinct().Should().HaveCountGreaterThanOrEqualTo(4, "one note repeated reads as fake");
        ratings.Average().Should().BeInRange(3.5m, 4.8m, "a salon worth showing, not a perfect one");
        ratings.Should().Contain(r => r <= 3.0m, "an honest salon has an unhappy customer or two");
    }

    [Fact]
    public void Across_seeds_most_reviews_are_good_and_some_are_poor()
    {
        var all = Seeds().SelectMany(s => DemoSalonReviewsSeeder.Plan(new Random(s), Reviewers)).ToList();

        all.Count(p => p.Rating >= 4.0m).Should().BeGreaterThan(all.Count / 2);
        all.Should().Contain(p => p.Rating <= 2.0m);
    }

    [Fact]
    public void The_salon_answers_most_reviews_and_every_poor_one()
    {
        foreach (var seed in Seeds())
        {
            var plan = DemoSalonReviewsSeeder.Plan(new Random(seed), Reviewers);
            plan.Where(p => p.Rating <= 2.0m).Should().NotContain(p => p.Reply == null, "an unhappy customer is answered");
        }

        var ours = ThePlan();
        ours.Count(p => p.Reply != null).Should().BeGreaterThanOrEqualTo(ours.Count / 2);
        ours.Should().Contain(p => p.Reply == null, "not every review gets an answer — that is how real ones look");
    }

    [Fact]
    public void Every_review_is_by_a_different_reviewer()
    {
        foreach (var seed in Seeds())
            DemoSalonReviewsSeeder.Plan(new Random(seed), Reviewers).Select(p => p.Reviewer)
                .Should().OnlyHaveUniqueItems("a customer reviews a salon once").And.OnlyContain(r => r >= 0 && r < Reviewers);
    }

    [Fact]
    public void More_reviews_than_reviewers_is_refused()
    {
        var act = () => DemoSalonReviewsSeeder.Plan(new Random(1), DemoSalonReviewsSeeder.ReviewCount - 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void A_few_authors_chose_not_to_show_their_name()
    {
        ThePlan().Count(p => !p.ShowName).Should().Be(DemoSalonReviewsSeeder.HiddenNames,
            "the demo shows «مشتری» next to named reviews");
    }

    [Fact]
    public void No_comment_is_said_twice_at_the_salon()
    {
        ThePlan().Select(p => p.Comment).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_four_aspects_are_rated_and_the_overall_is_their_average_to_the_half_star()
    {
        // As the review form does it (reviews-and-reschedule-round2 D1).
        foreach (var plan in Seeds().SelectMany(s => DemoSalonReviewsSeeder.Plan(new Random(s), Reviewers)))
        {
            var d = plan.Dimensions;
            var aspects = new[] { d.Cleanliness, d.Skill, d.Punctuality, d.Conduct };
            aspects.Should().OnlyContain(v => v.HasValue, "the form requires all four");

            foreach (var value in aspects.OfType<decimal>())
            {
                value.Should().BeInRange(1.0m, 5.0m);
                (value % 0.5m).Should().Be(0m);
                Math.Abs(value - plan.Rating).Should().BeLessThanOrEqualTo(1.0m);
            }

            Review.OverallFrom(null, d).Should().Be(plan.Rating);
        }
    }

    [Fact]
    public void Every_planned_review_is_one_the_domain_accepts_published_with_its_reply()
    {
        foreach (var plan in Seeds().SelectMany(s => DemoSalonReviewsSeeder.Plan(new Random(s), Reviewers)))
        {
            var review = Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), plan.Rating,
                plan.Comment, dimensions: plan.Dimensions);
            review.Publish("seed");
            if (plan.Reply is { } reply)
            {
                review.AddProviderResponse(reply, "seed");
                review.ApproveReply("seed");
            }

            review.IsPubliclyVisible.Should().BeTrue();
            plan.Reviewer.Should().BeInRange(0, Reviewers - 1);
            plan.DaysAgo.Should().BeGreaterThanOrEqualTo(3, "the visit is well in the past");
        }
    }

    [Theory]
    [InlineData("سالن نهال")]
    [InlineData("  سالن   نهال ")]
    [InlineData("سالن\u200cنهال")]
    [InlineData("سالن\u00a0نهال")]
    public void The_live_salon_is_found_however_its_name_was_typed(string typed)
    {
        DemoSalonReviewsSeeder.SameName(typed).Should().Be(DemoSalonReviewsSeeder.SameName(DemoSalonReviewsSeeder.DemoSalonName));
    }

    [Fact]
    public void Arabic_letters_read_as_their_persian_twins()
    {
        DemoSalonReviewsSeeder.SameName("سالن كيانا ي").Should().Be("سالن کیانا ی");
        DemoSalonReviewsSeeder.SameName("سالن نهال شعبه ۲").Should().NotBe(DemoSalonReviewsSeeder.SameName("سالن نهال"));
    }
}
