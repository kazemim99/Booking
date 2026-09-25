using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// A review's refusals reach the customer, the salon and the moderator verbatim, so they are Persian — and still keyed
/// by the request field they are about, which is how a client knows which input to point at
/// (openspec/changes/_inline/customer-reviews-and-nahal-seed).
/// </summary>
public class ReviewRefusalsSpeakPersianTests
{
    private static string Only(DomainValidationException e, string field)
    {
        Assert.True(e.ValidationErrors.ContainsKey(field), $"keyed by {field}");
        return Assert.Single(e.ValidationErrors[field]);
    }

    private static Review Create(decimal rating = 4.0m, string? comment = null, ReviewDimensionRatings? dimensions = null) =>
        Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), rating, comment, dimensions: dimensions);

    [Fact]
    public void A_too_short_comment()
    {
        var e = Assert.Throws<DomainValidationException>(() => Create(comment: "خوب بود"));

        Assert.Equal("متن نظر باید دست‌کم ۱۰ نویسه باشد.", Only(e, "Comment"));
    }

    [Fact]
    public void An_overall_rating_out_of_range()
    {
        var e = Assert.Throws<DomainValidationException>(() => Create(rating: 6.0m));

        Assert.Equal("امتیاز کلی باید بین ۱ تا ۵ ستاره باشد.", Only(e, "Rating"));
    }

    [Fact]
    public void A_dimension_off_the_half_star_grid_names_the_dimension()
    {
        var e = Assert.Throws<DomainValidationException>(
            () => Create(dimensions: new ReviewDimensionRatings(null, 3.7m, null, null)));

        Assert.StartsWith("امتیاز مهارت و کیفیت کار", Only(e, "SkillRating"));
    }

    [Fact]
    public void A_reply_to_a_review_still_awaiting_approval()
    {
        var review = Create();

        var e = Assert.Throws<InvalidAggregateStateException>(() => review.AddProviderResponse("ممنون از شما"));

        Assert.Equal("پس از تأیید این نظر می‌توانید به آن پاسخ دهید.", e.Message);
        Assert.Equal("Pending", e.CurrentState);
    }

    [Fact]
    public void Editing_a_rejected_review()
    {
        var review = Create();
        review.Reject("حاوی شماره تلفن", "admin");

        var e = Assert.Throws<InvalidAggregateStateException>(
            () => review.EditByAuthor(4.0m, null, null, "author", DateTime.UtcNow));

        Assert.Equal("نظرِ ردشده قابل ویرایش نیست.", e.Message);
    }
}
