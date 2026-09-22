using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// The rating model: one overall star the customer states, plus four dimensions they may give or leave out.
/// </summary>
/// <remarks>
/// "Overall rating is missing" is not tested here: at the domain the overall is a non-nullable decimal, so an
/// absent overall can only exist at the request boundary and is covered by the controller tests (6.1).
/// </remarks>
public class ReviewRatingTests
{
    private static Review CreateReview(decimal overall, ReviewDimensionRatings? dimensions = null) =>
        Review.Create(
            ProviderId.New(),
            UserId.From(Guid.NewGuid()),
            Guid.NewGuid(),
            overall,
            comment: null,
            dimensions: dimensions);

    [Fact]
    public void Overall_only_records_every_dimension_as_absent()
    {
        var review = CreateReview(4.0m);

        Assert.Equal(4.0m, review.RatingValue);
        Assert.Null(review.CleanlinessRating);
        Assert.Null(review.SkillRating);
        Assert.Null(review.PunctualityRating);
        Assert.Null(review.ConductRating);
    }

    [Fact]
    public void A_subset_of_dimensions_records_those_and_leaves_the_rest_absent()
    {
        var review = CreateReview(4.5m, new ReviewDimensionRatings(Cleanliness: 5.0m, Punctuality: 3.5m));

        Assert.Equal(5.0m, review.CleanlinessRating);
        Assert.Equal(3.5m, review.PunctualityRating);
        Assert.Null(review.SkillRating);
        Assert.Null(review.ConductRating);
    }

    [Fact]
    public void All_four_dimensions_are_recorded_when_given()
    {
        var review = CreateReview(4.0m, new ReviewDimensionRatings(4.0m, 3.0m, 5.0m, 1.5m));

        Assert.Equal(4.0m, review.CleanlinessRating);
        Assert.Equal(3.0m, review.SkillRating);
        Assert.Equal(5.0m, review.PunctualityRating);
        Assert.Equal(1.5m, review.ConductRating);
    }

    [Fact]
    public void Dimensions_never_override_the_customers_overall_verdict()
    {
        // "They were late and I still loved it" — averaging this to 3.5 would put words in the customer's mouth.
        var review = CreateReview(5.0m, new ReviewDimensionRatings(3.0m, 3.0m, 3.0m, 3.0m));

        Assert.Equal(5.0m, review.RatingValue);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(5.5)]
    [InlineData(4.3)]
    public void An_invalid_overall_is_rejected_by_name(double overall)
    {
        var ex = Assert.Throws<DomainValidationException>(() => CreateReview((decimal)overall));

        Assert.True(ex.ValidationErrors.ContainsKey("Rating"), $"errors named: {string.Join(", ", ex.ValidationErrors.Keys)}");
    }

    public static TheoryData<string, ReviewDimensionRatings> InvalidDimensions => new()
    {
        { "CleanlinessRating", new ReviewDimensionRatings(Cleanliness: 0.5m) },
        { "CleanlinessRating", new ReviewDimensionRatings(Cleanliness: 4.2m) },
        { "SkillRating", new ReviewDimensionRatings(Skill: 5.5m) },
        { "SkillRating", new ReviewDimensionRatings(Skill: 2.25m) },
        { "PunctualityRating", new ReviewDimensionRatings(Punctuality: 0m) },
        { "PunctualityRating", new ReviewDimensionRatings(Punctuality: 3.7m) },
        { "ConductRating", new ReviewDimensionRatings(Conduct: 6.0m) },
        { "ConductRating", new ReviewDimensionRatings(Conduct: 1.1m) },
    };

    [Theory]
    [MemberData(nameof(InvalidDimensions))]
    public void An_out_of_range_or_off_increment_dimension_is_rejected_by_name(
        string expectedField, ReviewDimensionRatings dimensions)
    {
        var ex = Assert.Throws<DomainValidationException>(() => CreateReview(4.0m, dimensions));

        Assert.True(
            ex.ValidationErrors.ContainsKey(expectedField),
            $"expected an error naming {expectedField}; got: {string.Join(", ", ex.ValidationErrors.Keys)}");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(5.0)]
    public void Every_half_step_in_range_is_accepted_for_every_dimension(double value)
    {
        var v = (decimal)value;
        var review = CreateReview(v, new ReviewDimensionRatings(v, v, v, v));

        Assert.Equal(v, review.CleanlinessRating);
        Assert.Equal(v, review.SkillRating);
        Assert.Equal(v, review.PunctualityRating);
        Assert.Equal(v, review.ConductRating);
    }

    // ── The comment (spec: "A review may carry a comment") ──

    private static Review WithComment(string? comment) =>
        Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m, comment: comment);

    [Fact]
    public void A_persian_comment_is_kept_exactly_as_written()
    {
        const string comment = "کار بسیار تمیز و دقیقی بود، از برخوردشان هم راضی بودم";

        Assert.Equal(comment, WithComment(comment).Comment);
    }

    [Fact]
    public void A_comment_shorter_than_ten_characters_after_trimming_is_refused()
    {
        // Padded past ten with spaces: the length that counts is what would be stored.
        Assert.Throws<DomainValidationException>(() => WithComment("خوب" + new string(' ', 8)));
        Assert.Throws<DomainValidationException>(() => WithComment("خوب بود"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_review_without_words_has_no_comment(string? comment)
    {
        Assert.Null(WithComment(comment).Comment);
    }
}
