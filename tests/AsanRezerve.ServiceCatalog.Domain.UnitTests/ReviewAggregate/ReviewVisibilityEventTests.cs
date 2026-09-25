using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Events;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// Restoring a hidden review, and the events raised whenever public visibility changes.
/// </summary>
/// <remarks>
/// These events feed notifications and auditing. They are deliberately NOT how the provider's rating is
/// recomputed: the unit of work dispatches events before it saves, on a fresh scope, so a handler would see
/// the row unwritten. See design D6.
/// </remarks>
public class ReviewVisibilityEventTests
{
    private const string Admin = "Admin:moderator";
    private static readonly DateTime Created = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private static Review NewReview()
    {
        var review = Review.Create(ProviderId.New(), UserId.From(Guid.NewGuid()), Guid.NewGuid(), 4.0m);
        review.CreatedAt = Created;
        return review;
    }

    private static Review Published()
    {
        var review = NewReview();
        review.Publish(Admin);
        review.ClearDomainEvents();
        return review;
    }

    // ── Restore ──

    [Fact]
    public void An_administrator_can_restore_a_hidden_review()
    {
        var review = Published();
        review.Hide("reported", Admin);

        review.Restore(Admin);

        Assert.Equal(ReviewModerationStatus.Published, review.ModerationStatus);
        Assert.True(review.IsPubliclyVisible);
        Assert.Null(review.ModerationReason);
    }

    [Fact]
    public void Restoring_keeps_the_votes_it_had()
    {
        // Legacy counters stand in for votes until per-user voting lands (3.6); either way, hide/restore
        // must not touch them.
        var review = Published();
        var helpful = review.HelpfulCount;
        var notHelpful = review.NotHelpfulCount;
        review.Hide("reported", Admin);

        review.Restore(Admin);

        Assert.Equal(helpful, review.HelpfulCount);
        Assert.Equal(notHelpful, review.NotHelpfulCount);
    }

    [Fact]
    public void A_rejected_review_cannot_be_restored()
    {
        var review = NewReview();
        review.Reject("spam", Admin);

        Assert.Throws<InvalidAggregateStateException>(() => review.Restore(Admin));
        Assert.Equal(ReviewModerationStatus.Rejected, review.ModerationStatus);
    }

    [Fact]
    public void Only_a_hidden_review_can_be_restored()
    {
        Assert.Throws<InvalidAggregateStateException>(() => NewReview().Restore(Admin));
        Assert.Throws<InvalidAggregateStateException>(() => Published().Restore(Admin));
    }

    // ── Events: becoming public ──

    [Fact]
    public void First_publication_raises_a_published_event_marked_as_first()
    {
        var review = NewReview();

        review.Publish(Admin);

        var e = Assert.IsType<ReviewPublishedEvent>(Assert.Single(review.DomainEvents));
        Assert.Equal(review.Id, e.ReviewId);
        Assert.Equal(review.ProviderId, e.ProviderId);
        Assert.Equal(review.BookingId, e.BookingId);
        Assert.False(e.IsRepublication);
    }

    [Fact]
    public void Approving_an_edited_review_is_marked_as_a_republication()
    {
        // The provider must be able to tell a changed review from a new one.
        var review = Published();
        review.EditByAuthor(2.0m, null, null, "Customer:author", Created.AddDays(1));
        review.ClearDomainEvents();

        review.Publish(Admin);

        var e = Assert.IsType<ReviewPublishedEvent>(Assert.Single(review.DomainEvents));
        Assert.True(e.IsRepublication);
    }

    [Fact]
    public void Restoring_raises_a_published_event()
    {
        var review = Published();
        review.Hide("reported", Admin);
        review.ClearDomainEvents();

        review.Restore(Admin);

        var e = Assert.IsType<ReviewPublishedEvent>(Assert.Single(review.DomainEvents));
        Assert.True(e.IsRepublication);
    }

    // ── Events: leaving the public ──

    [Fact]
    public void Hiding_raises_an_unpublished_event()
    {
        var review = Published();

        review.Hide("reported", Admin);

        var e = Assert.IsType<ReviewUnpublishedEvent>(Assert.Single(review.DomainEvents));
        Assert.Equal(review.ProviderId, e.ProviderId);
    }

    [Fact]
    public void Editing_a_published_review_raises_an_unpublished_event()
    {
        var review = Published();

        review.EditByAuthor(2.0m, null, null, "Customer:author", Created.AddDays(1));

        Assert.IsType<ReviewUnpublishedEvent>(Assert.Single(review.DomainEvents));
    }

    [Fact]
    public void Transitions_that_do_not_change_visibility_raise_nothing()
    {
        var pending = NewReview();
        pending.EditByAuthor(3.0m, null, null, "Customer:author", Created.AddDays(1));
        pending.Reject("spam", Admin);

        Assert.Empty(pending.DomainEvents);
    }
}
