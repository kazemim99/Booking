using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// Moderation is its own axis. <see cref="Review.IsVerified"/> means "came from a completed booking";
/// <see cref="Review.ModerationStatus"/> means "an administrator cleared it for display". Folding them
/// together would make hiding an abusive review also brand it unverified.
/// </summary>
public class ReviewModerationTests
{
    private const string Admin = "Admin:moderator";

    private static Review NewReview(bool verified = true) =>
        Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m, isVerified: verified);

    private static Review Published()
    {
        var review = NewReview();
        review.Publish(Admin);
        return review;
    }

    // ── A new review waits for a moderator ──

    [Fact]
    public void A_new_review_is_pending_and_verified()
    {
        var review = NewReview();

        Assert.Equal(ReviewModerationStatus.Pending, review.ModerationStatus);
        Assert.True(review.IsVerified);
        Assert.Null(review.ModeratedAt);
        Assert.Null(review.ModeratedBy);
    }

    // ── Moderation never touches verification ──

    [Fact]
    public void Publishing_leaves_verification_alone()
    {
        var review = Published();

        Assert.Equal(ReviewModerationStatus.Published, review.ModerationStatus);
        Assert.True(review.IsVerified);
    }

    [Fact]
    public void Hiding_a_verified_review_leaves_it_verified()
    {
        var review = Published();

        review.Hide("abusive language", Admin);

        Assert.Equal(ReviewModerationStatus.Hidden, review.ModerationStatus);
        Assert.True(review.IsVerified);
    }

    [Fact]
    public void Rejecting_a_verified_review_leaves_it_verified()
    {
        var review = NewReview();

        review.Reject("names a staff member", Admin);

        Assert.Equal(ReviewModerationStatus.Rejected, review.ModerationStatus);
        Assert.True(review.IsVerified);
    }

    [Fact]
    public void An_unverified_review_stays_unverified_when_published()
    {
        var review = NewReview(verified: false);

        review.Publish(Admin);

        Assert.False(review.IsVerified);
    }

    // ── Who, when and why is recorded ──

    [Fact]
    public void A_moderation_decision_records_who_and_when()
    {
        var review = NewReview();

        review.Publish(Admin);

        Assert.Equal(Admin, review.ModeratedBy);
        Assert.NotNull(review.ModeratedAt);
    }

    [Fact]
    public void A_rejection_records_its_reason()
    {
        var review = NewReview();

        review.Reject("  contains a phone number  ", Admin);

        Assert.Equal("contains a phone number", review.ModerationReason);
    }

    [Fact]
    public void Hiding_records_its_reason()
    {
        var review = Published();

        review.Hide("reported and upheld", Admin);

        Assert.Equal("reported and upheld", review.ModerationReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejecting_without_a_reason_is_refused(string reason)
    {
        var review = NewReview();

        Assert.Throws<DomainValidationException>(() => review.Reject(reason, Admin));
        Assert.Equal(ReviewModerationStatus.Pending, review.ModerationStatus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hiding_without_a_reason_is_refused(string reason)
    {
        var review = Published();

        Assert.Throws<DomainValidationException>(() => review.Hide(reason, Admin));
        Assert.Equal(ReviewModerationStatus.Published, review.ModerationStatus);
    }

    // ── Transitions that are not allowed ──

    [Fact]
    public void A_published_review_cannot_be_approved_again()
    {
        var review = Published();

        Assert.Throws<InvalidAggregateStateException>(() => review.Publish(Admin));
    }

    [Fact]
    public void A_rejected_review_cannot_be_published_because_rejection_is_permanent()
    {
        var review = NewReview();
        review.Reject("spam", Admin);

        Assert.Throws<InvalidAggregateStateException>(() => review.Publish(Admin));
        Assert.Equal(ReviewModerationStatus.Rejected, review.ModerationStatus);
    }

    [Fact]
    public void A_hidden_review_is_not_published_by_approve()
    {
        // Bringing a hidden review back is Restore — a separate, deliberate act — not a second approval.
        var review = Published();
        review.Hide("reported", Admin);

        Assert.Throws<InvalidAggregateStateException>(() => review.Publish(Admin));
    }

    [Fact]
    public void Only_a_pending_review_can_be_rejected()
    {
        var review = Published();

        Assert.Throws<InvalidAggregateStateException>(() => review.Reject("too late", Admin));
        Assert.Equal(ReviewModerationStatus.Published, review.ModerationStatus);
    }

    [Fact]
    public void Only_a_published_review_can_be_hidden()
    {
        var review = NewReview();

        Assert.Throws<InvalidAggregateStateException>(() => review.Hide("not yet public", Admin));
        Assert.Equal(ReviewModerationStatus.Pending, review.ModerationStatus);
    }

    [Fact]
    public void IsPubliclyVisible_is_true_only_when_published()
    {
        var pending = NewReview();
        var published = Published();
        var hidden = Published();
        hidden.Hide("x", Admin);
        var rejected = NewReview();
        rejected.Reject("x", Admin);

        Assert.False(pending.IsPubliclyVisible);
        Assert.True(published.IsPubliclyVisible);
        Assert.False(hidden.IsPubliclyVisible);
        Assert.False(rejected.IsPubliclyVisible);
    }
}
