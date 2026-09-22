using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// The provider's single reply to a review, and its own moderation — independent of the review's.
/// </summary>
public class ReviewReplyTests
{
    private const string Admin = "Admin:moderator";
    private const string Provider = "Provider:owner";

    private static Review PublishedReview()
    {
        var review = Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m);
        review.Publish(Admin);
        return review;
    }

    private static Review WithPendingReply()
    {
        var review = PublishedReview();
        review.AddProviderResponse("Thank you for coming", Provider);
        return review;
    }

    [Fact]
    public void A_new_review_has_no_reply()
    {
        var review = PublishedReview();

        Assert.Null(review.ProviderResponse);
        Assert.Null(review.ReplyModerationStatus);
        Assert.False(review.IsReplyPubliclyVisible);
    }

    [Fact]
    public void Replying_records_the_trimmed_text_and_waits_for_a_moderator()
    {
        var review = PublishedReview();

        review.AddProviderResponse("  ممنون از حضورتان  ", Provider);

        Assert.Equal("ممنون از حضورتان", review.ProviderResponse);
        Assert.NotNull(review.ProviderResponseAt);
        Assert.Equal(ReviewModerationStatus.Pending, review.ReplyModerationStatus);
        Assert.False(review.IsReplyPubliclyVisible);
    }

    [Fact]
    public void A_second_reply_is_refused()
    {
        var review = WithPendingReply();

        Assert.Throws<InvalidAggregateStateException>(() => review.AddProviderResponse("again", Provider));
        Assert.Equal("Thank you for coming", review.ProviderResponse);
    }

    [Fact]
    public void A_review_that_is_not_published_cannot_be_replied_to()
    {
        var pending = Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m);

        Assert.Throws<InvalidAggregateStateException>(() => pending.AddProviderResponse("hello", Provider));
        Assert.Null(pending.ProviderResponse);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_reply_is_refused(string text)
    {
        var review = PublishedReview();

        Assert.Throws<DomainValidationException>(() => review.AddProviderResponse(text, Provider));
    }

    [Fact]
    public void A_reply_over_1000_characters_is_refused()
    {
        var review = PublishedReview();

        Assert.Throws<DomainValidationException>(() => review.AddProviderResponse(new string('x', 1001), Provider));
    }

    [Fact]
    public void Editing_a_published_reply_returns_it_to_pending()
    {
        var review = WithPendingReply();
        review.ApproveReply(Admin);

        review.UpdateProviderResponse("Thank you, see you soon", Provider);

        Assert.Equal("Thank you, see you soon", review.ProviderResponse);
        Assert.Equal(ReviewModerationStatus.Pending, review.ReplyModerationStatus);
        Assert.False(review.IsReplyPubliclyVisible);
    }

    [Fact]
    public void A_rejected_reply_can_be_rewritten_and_goes_back_to_the_queue()
    {
        var review = WithPendingReply();
        review.RejectReply("contains a discount code", Admin);

        review.UpdateProviderResponse("Thank you for your visit", Provider);

        Assert.Equal(ReviewModerationStatus.Pending, review.ReplyModerationStatus);
    }

    [Fact]
    public void Removing_a_reply_clears_it_entirely()
    {
        var review = WithPendingReply();
        review.ApproveReply(Admin);

        review.RemoveProviderResponse(Provider);

        Assert.Null(review.ProviderResponse);
        Assert.Null(review.ProviderResponseAt);
        Assert.Null(review.ReplyModerationStatus);
        Assert.False(review.IsReplyPubliclyVisible);
    }

    [Fact]
    public void Removing_a_reply_that_does_not_exist_is_refused()
    {
        var review = PublishedReview();

        Assert.Throws<InvalidAggregateStateException>(() => review.RemoveProviderResponse(Provider));
    }

    [Fact]
    public void Approving_a_reply_makes_it_public_and_leaves_the_review_alone()
    {
        var review = WithPendingReply();

        review.ApproveReply(Admin);

        Assert.Equal(ReviewModerationStatus.Published, review.ReplyModerationStatus);
        Assert.Equal(ReviewModerationStatus.Published, review.ModerationStatus);
        Assert.True(review.IsReplyPubliclyVisible);
    }

    [Fact]
    public void A_rejected_reply_leaves_the_review_published()
    {
        var review = WithPendingReply();

        review.RejectReply("insults the customer", Admin);

        Assert.Equal(ReviewModerationStatus.Rejected, review.ReplyModerationStatus);
        Assert.Equal("insults the customer", review.ReplyModerationReason);
        Assert.Equal(ReviewModerationStatus.Published, review.ModerationStatus);
        Assert.True(review.IsPubliclyVisible);
        Assert.False(review.IsReplyPubliclyVisible);
    }

    [Fact]
    public void Rejecting_a_reply_without_a_reason_is_refused()
    {
        var review = WithPendingReply();

        Assert.Throws<DomainValidationException>(() => review.RejectReply(" ", Admin));
        Assert.Equal(ReviewModerationStatus.Pending, review.ReplyModerationStatus);
    }

    [Fact]
    public void Only_a_pending_reply_can_be_approved_or_rejected()
    {
        var review = WithPendingReply();
        review.ApproveReply(Admin);

        Assert.Throws<InvalidAggregateStateException>(() => review.ApproveReply(Admin));
        Assert.Throws<InvalidAggregateStateException>(() => review.RejectReply("late", Admin));
    }

    [Fact]
    public void Moderating_a_reply_that_does_not_exist_is_refused()
    {
        var review = PublishedReview();

        Assert.Throws<InvalidAggregateStateException>(() => review.ApproveReply(Admin));
    }

    [Fact]
    public void A_published_reply_is_not_public_while_its_review_is_hidden()
    {
        var review = WithPendingReply();
        review.ApproveReply(Admin);

        review.Hide("reported", Admin);

        Assert.Equal(ReviewModerationStatus.Published, review.ReplyModerationStatus);
        Assert.False(review.IsReplyPubliclyVisible);
    }
}
