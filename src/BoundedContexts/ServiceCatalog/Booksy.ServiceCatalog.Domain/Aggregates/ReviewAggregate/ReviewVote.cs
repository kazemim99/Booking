using Booksy.Core.Domain.Base;

namespace Booksy.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// One authenticated user's helpful / not-helpful vote on one review.
    /// </summary>
    /// <remarks>
    /// <para>Its own table with a unique index on (review, user) — that index, not a check in a handler, is what
    /// enforces one vote per user under concurrency. It is deliberately not a collection on <see cref="Review"/>:
    /// casting one vote must not load every other vote, and owned collections with GUID keys carry the
    /// phantom-UPDATE hazard this codebase has already paid for once.</para>
    ///
    /// <para>Whether a request adds, replaces or withdraws a vote is decided by the vote command, which also
    /// keeps the review's denormalised counts in step in the same transaction.</para>
    /// </remarks>
    public sealed class ReviewVote : AggregateRoot<Guid>
    {
        public Guid ReviewId { get; private set; }
        public UserId UserId { get; private set; } = default!;
        public bool IsHelpful { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastModifiedAt { get; private set; }

        private ReviewVote() : base() { }

        public static ReviewVote Cast(Guid reviewId, UserId userId, bool isHelpful, DateTime utcNow) => new()
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            UserId = userId,
            IsHelpful = isHelpful,
            CreatedAt = utcNow,
        };

        public void ChangeTo(bool isHelpful, DateTime utcNow)
        {
            IsHelpful = isHelpful;
            LastModifiedAt = utcNow;
        }
    }
}
