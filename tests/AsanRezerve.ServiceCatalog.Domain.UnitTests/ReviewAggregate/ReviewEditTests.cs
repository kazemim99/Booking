using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// The author's bounded edit: 7 days, only while Pending or Published, and back to the queue afterwards.
/// </summary>
/// <remarks>
/// Who may edit is the command handler's check (<c>ForbiddenException</c> → 403), matching how Booking keeps
/// ownership out of the aggregate; <see cref="Review.IsAuthoredBy"/> is what it asks. The non-author path is
/// covered end-to-end by the edit integration tests (5.3).
/// </remarks>
public class ReviewEditTests
{
    private const string Admin = "Admin:moderator";
    private static readonly DateTime Created = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Review ReviewCreatedAt(DateTime createdAt, UserId? author = null)
    {
        var review = Review.Create(ProviderId.New(), author ?? UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m,
            comment: "خیلی خوب بود، ممنون از همه");
        review.CreatedAt = createdAt;
        return review;
    }

    private static Review PublishedAt(DateTime createdAt)
    {
        var review = ReviewCreatedAt(createdAt);
        review.Publish(Admin);
        return review;
    }

    private static void Edit(Review review, DateTime utcNow, decimal rating = 2.0m, string? comment = "دیر شروع کردند و عجله داشتند") =>
        review.EditByAuthor(rating, new ReviewDimensionRatings(Punctuality: 1.0m), comment, "Customer:author", utcNow);

    [Fact]
    public void Editing_inside_the_window_updates_the_ratings_and_comment()
    {
        var review = ReviewCreatedAt(Created);

        Edit(review, Created.AddDays(2));

        Assert.Equal(2.0m, review.RatingValue);
        Assert.Equal(1.0m, review.PunctualityRating);
        Assert.Null(review.CleanlinessRating);
        Assert.Equal("دیر شروع کردند و عجله داشتند", review.Comment);
        Assert.Equal(Created.AddDays(2), review.EditedAt);
    }

    [Fact]
    public void The_last_moment_of_the_seventh_day_is_still_inside_the_window()
    {
        var review = ReviewCreatedAt(Created);

        Edit(review, Created.AddDays(ReviewEditPolicy.WindowDays));

        Assert.Equal(2.0m, review.RatingValue);
    }

    [Fact]
    public void Editing_after_the_window_is_refused_and_leaves_the_review_unchanged()
    {
        var review = ReviewCreatedAt(Created);

        Assert.Throws<BusinessRuleViolationException>(() => Edit(review, Created.AddDays(8)));

        Assert.Equal(4.0m, review.RatingValue);
        Assert.Null(review.EditedAt);
    }

    [Fact]
    public void Editing_a_published_review_returns_it_to_pending()
    {
        var review = PublishedAt(Created);

        Edit(review, Created.AddDays(2));

        Assert.Equal(ReviewModerationStatus.Pending, review.ModerationStatus);
        Assert.False(review.IsPubliclyVisible);
    }

    [Fact]
    public void Editing_a_pending_review_leaves_it_pending()
    {
        var review = ReviewCreatedAt(Created);

        Edit(review, Created.AddDays(1));

        Assert.Equal(ReviewModerationStatus.Pending, review.ModerationStatus);
    }

    [Fact]
    public void A_rejected_review_cannot_be_edited_even_inside_the_window()
    {
        // Otherwise the edit window is a route back to publication that moderation already refused.
        var review = ReviewCreatedAt(Created);
        review.Reject("contains a phone number", Admin);

        Assert.Throws<InvalidAggregateStateException>(() => Edit(review, Created.AddDays(1)));

        Assert.Equal(ReviewModerationStatus.Rejected, review.ModerationStatus);
        Assert.Equal(4.0m, review.RatingValue);
    }

    [Fact]
    public void A_hidden_review_cannot_be_edited_even_inside_the_window()
    {
        // Otherwise an author could undo an administrator's decision.
        var review = PublishedAt(Created);
        review.Hide("reported and upheld", Admin);

        Assert.Throws<InvalidAggregateStateException>(() => Edit(review, Created.AddDays(1)));

        Assert.Equal(ReviewModerationStatus.Hidden, review.ModerationStatus);
    }

    [Fact]
    public void An_edit_is_validated_like_a_new_review()
    {
        var review = ReviewCreatedAt(Created);

        var ex = Assert.Throws<DomainValidationException>(() =>
            review.EditByAuthor(4.0m, new ReviewDimensionRatings(Skill: 7m), null, "Customer:author", Created.AddDays(1)));

        Assert.True(ex.ValidationErrors.ContainsKey("SkillRating"));
        Assert.Equal(4.0m, review.RatingValue);
    }

    [Fact]
    public void Editing_can_remove_the_comment()
    {
        var review = ReviewCreatedAt(Created);

        Edit(review, Created.AddDays(1), comment: null);

        Assert.Null(review.Comment);
    }

    [Fact]
    public void Editing_a_review_with_a_published_reply_returns_the_reply_to_pending_too()
    {
        // Published provider words must never be displayed under text the provider never saw.
        var review = PublishedAt(Created);
        review.AddProviderResponse("ممنون از لطف شما", "Provider:owner");
        review.ApproveReply(Admin);

        Edit(review, Created.AddDays(2));

        Assert.Equal(ReviewModerationStatus.Pending, review.ReplyModerationStatus);
        Assert.False(review.IsReplyPubliclyVisible);
    }

    [Fact]
    public void Editing_leaves_a_rejected_reply_rejected()
    {
        var review = PublishedAt(Created);
        review.AddProviderResponse("ممنون", "Provider:owner");
        review.RejectReply("off-topic", Admin);

        Edit(review, Created.AddDays(2));

        Assert.Equal(ReviewModerationStatus.Rejected, review.ReplyModerationStatus);
    }

    [Fact]
    public void IsAuthoredBy_is_true_only_for_the_author()
    {
        var author = UserId.From(Guid.NewGuid());
        var review = ReviewCreatedAt(Created, author);

        Assert.True(review.IsAuthoredBy(author));
        Assert.False(review.IsAuthoredBy(UserId.From(Guid.NewGuid())));
    }
}
