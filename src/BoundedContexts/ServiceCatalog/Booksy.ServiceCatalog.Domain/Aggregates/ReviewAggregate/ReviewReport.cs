using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// A user's report that a published review should not be public.
    /// </summary>
    /// <remarks>
    /// A report never removes anything by itself: the review stays public until an administrator hides it.
    /// One report per user per review, enforced by a unique index.
    /// </remarks>
    public sealed class ReviewReport : AggregateRoot<Guid>
    {
        public const int MaxReasonLength = 500;

        public Guid ReviewId { get; private set; }
        public UserId ReportedByUserId { get; private set; } = default!;
        public string Reason { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }

        private ReviewReport() : base() { }

        public static ReviewReport File(Guid reviewId, UserId reportedBy, string reason, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainValidationException(nameof(Reason), "دلیل گزارش را بنویسید.");

            var trimmed = reason.Trim();
            if (trimmed.Length > MaxReasonLength)
                throw new DomainValidationException(nameof(Reason), $"دلیل گزارش حداکثر {MaxReasonLength} نویسه می‌تواند باشد.");

            return new ReviewReport
            {
                Id = Guid.NewGuid(),
                ReviewId = reviewId,
                ReportedByUserId = reportedBy,
                Reason = trimmed,
                CreatedAt = utcNow,
            };
        }
    }
}
