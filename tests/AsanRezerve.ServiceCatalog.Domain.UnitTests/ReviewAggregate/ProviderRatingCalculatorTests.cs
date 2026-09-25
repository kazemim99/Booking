using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// How a provider's rating is derived from their reviews. The one place the rule is written down; the recompute
/// feeds it rows and stores what it returns.
/// </summary>
public class ProviderRatingCalculatorTests
{
    private static ReviewRatingSnapshot Published(decimal overall,
        decimal? cleanliness = null, decimal? skill = null, decimal? punctuality = null, decimal? conduct = null) =>
        new(ReviewModerationStatus.Published, overall, cleanliness, skill, punctuality, conduct);

    private static ReviewRatingSnapshot In(ReviewModerationStatus status, decimal overall) =>
        new(status, overall, 1.0m, 1.0m, 1.0m, 1.0m);

    [Fact]
    public void A_provider_with_no_reviews_is_unrated()
    {
        var rating = ProviderRatingCalculator.Compute([]);

        Assert.Equal(0, rating.PublishedCount);
        Assert.Equal(0m, rating.Average);
        Assert.Equal(DimensionRating.None, rating.Cleanliness);
        Assert.Equal(DimensionRating.None, rating.Conduct);
    }

    [Fact]
    public void Pending_rejected_and_hidden_reviews_are_not_counted()
    {
        var rating = ProviderRatingCalculator.Compute(
        [
            In(ReviewModerationStatus.Pending, 1.0m),
            In(ReviewModerationStatus.Rejected, 1.0m),
            In(ReviewModerationStatus.Hidden, 1.0m),
        ]);

        Assert.Equal(0, rating.PublishedCount);
        Assert.Equal(0m, rating.Average);
        Assert.Equal(DimensionRating.None, rating.Skill);
    }

    [Fact]
    public void The_overall_average_is_over_published_reviews_only()
    {
        var rating = ProviderRatingCalculator.Compute(
        [
            Published(4.0m), Published(5.0m), Published(3.0m),
            In(ReviewModerationStatus.Pending, 1.0m),
            In(ReviewModerationStatus.Hidden, 1.0m),
        ]);

        Assert.Equal(3, rating.PublishedCount);
        Assert.Equal(4.0m, rating.Average);
    }

    [Fact]
    public void A_dimension_is_averaged_only_over_the_reviews_that_rated_it()
    {
        // Three published, only two rated punctuality: 4.5 over 2, not 3.0 over 3.
        var rating = ProviderRatingCalculator.Compute(
        [
            Published(4.0m, punctuality: 4.0m),
            Published(5.0m, punctuality: 5.0m),
            Published(3.0m),
        ]);

        Assert.Equal(new DimensionRating(4.5m, 2), rating.Punctuality);
    }

    [Fact]
    public void A_dimension_nobody_rated_has_no_average_rather_than_zero()
    {
        var rating = ProviderRatingCalculator.Compute([Published(4.0m, cleanliness: 5.0m)]);

        Assert.Equal(new DimensionRating(5.0m, 1), rating.Cleanliness);
        Assert.Equal(DimensionRating.None, rating.Skill);
        Assert.Null(rating.Skill.Average);
    }

    [Fact]
    public void A_hidden_reviews_dimensions_do_not_count_either()
    {
        var rating = ProviderRatingCalculator.Compute(
        [
            Published(4.0m, conduct: 5.0m),
            new ReviewRatingSnapshot(ReviewModerationStatus.Hidden, 1.0m, null, null, null, 1.0m),
        ]);

        Assert.Equal(new DimensionRating(5.0m, 1), rating.Conduct);
    }

    [Fact]
    public void Averages_are_rounded_to_two_decimals_half_away_from_zero()
    {
        // (4.5 + 4.0 + 4.0) / 3 = 4.1666… → 4.17; (5 + 4 + 4) / 3 = 4.333… → 4.33
        var rating = ProviderRatingCalculator.Compute(
        [
            Published(5.0m, skill: 4.5m),
            Published(4.0m, skill: 4.0m),
            Published(4.0m, skill: 4.0m),
        ]);

        Assert.Equal(4.33m, rating.Average);
        Assert.Equal(new DimensionRating(4.17m, 3), rating.Skill);
    }
}
