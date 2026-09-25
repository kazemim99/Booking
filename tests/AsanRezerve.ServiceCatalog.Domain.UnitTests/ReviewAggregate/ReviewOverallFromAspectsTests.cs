using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// openspec/changes/_inline/reviews-and-reschedule-round2 D1: the review form rates the four aspects, all required,
/// and the overall is their average to the nearest half star. An explicit overall (older app versions still in the
/// field) is kept as sent.
/// </summary>
public class ReviewOverallFromAspectsTests
{
    [Theory]
    [InlineData(4.0, 4.0, 4.0, 4.0, 4.0)]
    [InlineData(3.0, 4.0, 4.0, 4.0, 4.0)] // 3.75 → 4.0: the quarter rounds away from zero
    [InlineData(3.0, 3.0, 4.0, 4.0, 3.5)] // 3.5 exactly
    [InlineData(3.0, 3.0, 3.0, 4.0, 3.5)] // 3.25 → 3.5
    [InlineData(5.0, 5.0, 5.0, 4.5, 5.0)] // 4.875 → 5.0
    [InlineData(1.0, 1.0, 1.0, 1.5, 1.0)] // 1.125 → 1.0
    [InlineData(4.5, 4.0, 4.5, 4.0, 4.5)] // 4.25 → 4.5
    [InlineData(1.0, 1.0, 1.0, 1.0, 1.0)]
    [InlineData(5.0, 5.0, 5.0, 5.0, 5.0)]
    public void Without_an_overall_it_is_the_aspects_average_to_the_nearest_half_star(
        double cleanliness, double skill, double punctuality, double conduct, double expected)
    {
        var aspects = new ReviewDimensionRatings(
            (decimal)cleanliness, (decimal)skill, (decimal)punctuality, (decimal)conduct);

        Assert.Equal((decimal)expected, Review.OverallFrom(null, aspects));
    }

    [Fact]
    public void The_average_policy_on_its_own_rounds_quarters_up_to_the_half_star()
    {
        Assert.Equal(4.0m, ReviewOverallRating.AverageToHalfStar(3.0m, 4.0m, 4.0m, 4.0m));
        Assert.Equal(3.5m, ReviewOverallRating.AverageToHalfStar(3.0m, 3.0m, 3.0m, 4.0m));
    }

    [Fact]
    public void An_overall_that_is_sent_is_kept_as_the_customer_stated_it()
    {
        var aspects = new ReviewDimensionRatings(3.0m, 3.0m, 3.0m, 3.0m);

        Assert.Equal(5.0m, Review.OverallFrom(5.0m, aspects));
        Assert.Equal(4.5m, Review.OverallFrom(4.5m, null));
    }

    public static TheoryData<ReviewDimensionRatings?> Incomplete => new()
    {
        null,
        ReviewDimensionRatings.None,
        new ReviewDimensionRatings(Cleanliness: 4.0m),
        new ReviewDimensionRatings(4.0m, 4.0m, 4.0m, null),
    };

    [Theory]
    [MemberData(nameof(Incomplete))]
    public void Without_an_overall_every_aspect_is_required_keyed_as_the_rating(ReviewDimensionRatings? aspects)
    {
        var ex = Assert.Throws<DomainValidationException>(() => Review.OverallFrom(null, aspects));

        Assert.True(ex.ValidationErrors.ContainsKey("Rating"), $"errors named: {string.Join(", ", ex.ValidationErrors.Keys)}");
        Assert.Contains("به هر چهار مورد امتیاز بدهید.", ex.ValidationErrors["Rating"]);
    }

    [Fact]
    public void An_invalid_aspect_is_refused_by_its_own_name_not_as_the_overall()
    {
        var ex = Assert.Throws<DomainValidationException>(() =>
            Review.OverallFrom(null, new ReviewDimensionRatings(4.0m, 3.7m, 4.0m, 4.0m)));

        Assert.True(ex.ValidationErrors.ContainsKey(nameof(Review.SkillRating)));
    }

    // ---- «نامم نمایش داده نشود» ----

    [Fact]
    public void A_review_shows_its_authors_name_unless_they_say_otherwise()
    {
        var shown = Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m);
        var hidden = Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m, showName: false);

        Assert.True(shown.ShowName);
        Assert.False(hidden.ShowName);
    }

    [Fact]
    public void The_author_can_change_the_name_choice_when_editing()
    {
        var created = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        var review = Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m);
        review.CreatedAt = created;

        review.EditByAuthor(4.0m, null, null, "Customer:author", created.AddDays(1), showName: false);
        Assert.False(review.ShowName);

        review.EditByAuthor(4.0m, null, null, "Customer:author", created.AddDays(2), showName: true);
        Assert.True(review.ShowName);
    }
}
