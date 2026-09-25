// ========================================
// AsanRezerve.ServiceCatalog.Domain/Aggregates/ReviewAggregate/Review.cs
// ========================================
using AsanRezerve.Core.Domain.Abstractions.Entities;
using AsanRezerve.Core.Domain.Base;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Events;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// Represents a customer review for a provider
    /// Reviews can only be created for completed bookings
    /// </summary>
    public sealed class Review : AggregateRoot<Guid>, IAuditableEntity
    {
        // Core Identity
        public ProviderId ProviderId { get; private set; }
        public UserId CustomerId { get; private set; }
        public Guid BookingId { get; private set; }
        
        // Overall rating (1.0 - 5.0) — the customer's own verdict, never computed from the dimensions
        public decimal RatingValue { get; private set; }

        // Optional dimension ratings (1.0 - 5.0 each). Null means "not rated", never zero.
        public decimal? CleanlinessRating { get; private set; }
        public decimal? SkillRating { get; private set; }
        public decimal? PunctualityRating { get; private set; }
        public decimal? ConductRating { get; private set; }

        // Comment (Persian and/or English)
        public string? Comment { get; private set; }
        
        // Verification — "came from a completed booking". Never changed by moderation.
        public bool IsVerified { get; private set; }

        // Moderation — "an administrator cleared it for display". A separate axis from IsVerified.
        public ReviewModerationStatus ModerationStatus { get; private set; }
        public DateTime? ModeratedAt { get; private set; }
        public string? ModeratedBy { get; private set; }
        public string? ModerationReason { get; private set; }

        /// <summary>First time this review went public. Set once; how a re-publication is told apart.</summary>
        public DateTime? FirstPublishedAt { get; private set; }

        /// <summary>Last time the author edited it. Null if never edited.</summary>
        public DateTime? EditedAt { get; private set; }

        /// <summary>Public, and counted toward the provider's rating. True only when published.</summary>
        public bool IsPubliclyVisible => ModerationStatus == ReviewModerationStatus.Published;

        // Provider Response — exactly one, moderated independently of the review
        public string? ProviderResponse { get; private set; }
        public DateTime? ProviderResponseAt { get; private set; }

        /// <summary>Null when there is no reply. Rejected is not terminal for a reply: the provider may rewrite it.</summary>
        public ReviewModerationStatus? ReplyModerationStatus { get; private set; }
        public string? ReplyModerationReason { get; private set; }

        /// <summary>A reply is public only when it is published AND the review it answers is public.</summary>
        public bool IsReplyPubliclyVisible =>
            ReplyModerationStatus == ReviewModerationStatus.Published && IsPubliclyVisible;

        // Helpfulness.
        // Legacy* are the counters from before per-user voting: bare totals with no record of who voted, so they
        // cannot become votes. Frozen forever — nothing writes them — and stored in the original HelpfulCount /
        // NotHelpfulCount columns (renamed only here, in the model, so a rolling deploy never breaks the old image).
        public int LegacyHelpfulCount { get; private set; }
        public int LegacyNotHelpfulCount { get; private set; }

        // Live tallies of ReviewVotes rows. No domain method writes them: the vote command moves them by an atomic
        // SQL delta in its own transaction, because a read-modify-write here would lose updates under concurrency.
        public int HelpfulVoteCount { get; private set; }
        public int NotHelpfulVoteCount { get; private set; }

        /// <summary>What is displayed and judged: the legacy baseline plus live votes. Not mapped.</summary>
        public int HelpfulCount => LegacyHelpfulCount + HelpfulVoteCount;

        /// <summary>See <see cref="HelpfulCount"/>.</summary>
        public int NotHelpfulCount => LegacyNotHelpfulCount + NotHelpfulVoteCount;
        
        // Audit Properties
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        
        // Private constructor for EF Core
        private Review() : base() { }
        
        /// <summary>
        /// Creates a new review for a completed booking
        /// </summary>
        public static Review Create(
            ProviderId providerId,
            UserId customerId,
            Guid bookingId,
            decimal ratingValue,
            string? comment = null,
            bool isVerified = true,
            string? createdBy = null,
            ReviewDimensionRatings? dimensions = null)
        {
            ValidateRating(ratingValue);
            dimensions ??= ReviewDimensionRatings.None;
            ValidateDimensions(dimensions);

            comment = NormalizeComment(comment);

            return new Review
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                CustomerId = customerId,
                BookingId = bookingId,
                RatingValue = ratingValue,
                CleanlinessRating = dimensions.Cleanliness,
                SkillRating = dimensions.Skill,
                PunctualityRating = dimensions.Punctuality,
                ConductRating = dimensions.Conduct,
                Comment = comment,
                IsVerified = isVerified, // True if from actual booking
                ModerationStatus = ReviewModerationStatus.Pending, // Not public until an administrator approves it
                LegacyHelpfulCount = 0,
                LegacyNotHelpfulCount = 0,
                HelpfulVoteCount = 0,
                NotHelpfulVoteCount = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }
        
        /// <summary>
        /// The provider's reply. Exactly one per review, only to a published review, and not public until an
        /// administrator approves it. Who may call this (the owning provider only) is enforced by the caller.
        /// </summary>
        public void AddProviderResponse(string response, string? modifiedBy = null)
        {
            if (ProviderResponse is not null)
                throw new InvalidAggregateStateException(
                    nameof(Review), nameof(AddProviderResponse), "HasReply", ReviewWords.AlreadyReplied);

            // A reply answers what the public can see. Replying to a pending review that may yet be rejected
            // would be moderation work spent on nothing.
            RequireModerationState(ReviewModerationStatus.Published, nameof(AddProviderResponse));

            SetReply(ValidateReply(response), modifiedBy);
        }

        /// <summary>
        /// The provider rewrites their reply. It goes back to the moderation queue whatever state it was in —
        /// including Rejected, which for a reply is a chance to rephrase rather than a final word.
        /// </summary>
        public void UpdateProviderResponse(string response, string? modifiedBy = null)
        {
            if (ProviderResponse is null)
                throw new InvalidAggregateStateException(
                    nameof(Review), nameof(UpdateProviderResponse), "NoReply", ReviewWords.NoReply);

            SetReply(ValidateReply(response), modifiedBy);
        }

        /// <summary>
        /// The provider withdraws their reply.
        /// </summary>
        public void RemoveProviderResponse(string? modifiedBy = null)
        {
            if (ProviderResponse is null)
                throw new InvalidAggregateStateException(
                    nameof(Review), nameof(RemoveProviderResponse), "NoReply", ReviewWords.NoReply);

            ProviderResponse = null;
            ProviderResponseAt = null;
            ReplyModerationStatus = null;
            ReplyModerationReason = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }

        /// <summary>
        /// An administrator clears the provider's pending reply for display.
        /// </summary>
        public void ApproveReply(string moderatedBy)
        {
            RequireReplyState(ReviewModerationStatus.Pending, nameof(ApproveReply));
            ReplyModerationStatus = ReviewModerationStatus.Published;
            ReplyModerationReason = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = moderatedBy;
        }

        /// <summary>
        /// An administrator refuses the provider's pending reply. The review it answers is unaffected.
        /// </summary>
        public void RejectReply(string reason, string moderatedBy)
        {
            var trimmed = RequireReason(reason);
            RequireReplyState(ReviewModerationStatus.Pending, nameof(RejectReply));
            ReplyModerationStatus = ReviewModerationStatus.Rejected;
            ReplyModerationReason = trimmed;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = moderatedBy;
        }

        private void RequireReplyState(ReviewModerationStatus required, string operation)
        {
            if (ReplyModerationStatus != required)
                throw new InvalidAggregateStateException(
                    nameof(Review), operation, ReplyModerationStatus?.ToString() ?? "NoReply",
                    ReplyModerationStatus is { } state ? ReviewWords.NotInReplyState(state) : ReviewWords.NoReply);
        }

        private static string ValidateReply(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                throw new DomainValidationException(nameof(ProviderResponse), "متن پاسخ نمی‌تواند خالی باشد.");

            var trimmed = response.Trim();
            if (trimmed.Length > 1000)
                throw new DomainValidationException(nameof(ProviderResponse), "پاسخ حداکثر ۱۰۰۰ نویسه می‌تواند باشد.");

            return trimmed;
        }

        private void SetReply(string text, string? modifiedBy)
        {
            ProviderResponse = text;
            ProviderResponseAt = DateTime.UtcNow;
            ReplyModerationStatus = ReviewModerationStatus.Pending;
            ReplyModerationReason = null;
            LastModifiedAt = ProviderResponseAt;
            LastModifiedBy = modifiedBy;
        }
        
        
        // ── Moderation ──
        // Each transition is legal from exactly one state. Rejected is terminal; Hidden comes back only
        // through Restore, a separate and deliberate act. None of them touches IsVerified.

        /// <summary>
        /// An administrator clears a pending review for public display.
        /// </summary>
        public void Publish(string moderatedBy)
        {
            RequireModerationState(ReviewModerationStatus.Pending, nameof(Publish));
            SetModeration(ReviewModerationStatus.Published, moderatedBy, reason: null);
            RaisePublished();
        }

        /// <summary>
        /// An administrator brings a hidden review back. Its votes and counts are untouched. A rejected review
        /// cannot be restored: rejection is permanent.
        /// </summary>
        public void Restore(string moderatedBy)
        {
            RequireModerationState(ReviewModerationStatus.Hidden, nameof(Restore));
            SetModeration(ReviewModerationStatus.Published, moderatedBy, reason: null);
            RaisePublished();
        }

        private void RaisePublished()
        {
            var isRepublication = FirstPublishedAt is not null;
            FirstPublishedAt ??= ModeratedAt;
            RaiseDomainEvent(new ReviewPublishedEvent(Id, ProviderId, CustomerId, BookingId, isRepublication));
        }

        private void RaiseUnpublished() =>
            RaiseDomainEvent(new ReviewUnpublishedEvent(Id, ProviderId, CustomerId, BookingId));

        /// <summary>
        /// An administrator refuses a pending review. Permanent: it will never be public.
        /// </summary>
        public void Reject(string reason, string moderatedBy)
        {
            var trimmed = RequireReason(reason);
            RequireModerationState(ReviewModerationStatus.Pending, nameof(Reject));
            SetModeration(ReviewModerationStatus.Rejected, moderatedBy, trimmed);
        }

        /// <summary>
        /// An administrator takes a published review down. Reversible.
        /// </summary>
        public void Hide(string reason, string moderatedBy)
        {
            var trimmed = RequireReason(reason);
            RequireModerationState(ReviewModerationStatus.Published, nameof(Hide));
            SetModeration(ReviewModerationStatus.Hidden, moderatedBy, trimmed);
            RaiseUnpublished();
        }

        // ── Author edit ──

        /// <summary>Whether this user wrote the review. The handler asks; the aggregate does not authorise.</summary>
        public bool IsAuthoredBy(UserId userId) => CustomerId == userId;

        /// <summary>
        /// The author changes their ratings and comment, inside the edit window, while the review is Pending or
        /// Published. A published review goes back to the queue, and so does a published reply attached to it —
        /// provider words are never shown under text the provider did not see.
        /// </summary>
        /// <remarks>
        /// Rejected and Hidden are not editable: an edit would otherwise be a route back to publication that
        /// moderation refused, or a way to undo an administrator's hide.
        /// </remarks>
        public void EditByAuthor(
            decimal ratingValue,
            ReviewDimensionRatings? dimensions,
            string? comment,
            string modifiedBy,
            DateTime utcNow)
        {
            if (ModerationStatus is not (ReviewModerationStatus.Pending or ReviewModerationStatus.Published))
                throw new InvalidAggregateStateException(
                    nameof(Review), nameof(EditByAuthor), ModerationStatus.ToString(),
                    $"نظرِ {ReviewWords.Label(ModerationStatus)} قابل ویرایش نیست.");

            if (!ReviewEditPolicy.IsInsideWindow(CreatedAt, utcNow))
                throw new BusinessRuleViolationException(
                    "ReviewEditWindow",
                    $"نظر را تا {ReviewEditPolicy.WindowDays} روز پس از نوشتنش می‌توانید ویرایش کنید.",
                    "REVIEW_EDIT_WINDOW_CLOSED");

            // Validate everything before changing anything, so a refused edit leaves the review as it was.
            ValidateRating(ratingValue);
            dimensions ??= ReviewDimensionRatings.None;
            ValidateDimensions(dimensions);
            comment = NormalizeComment(comment);

            var wasPublic = IsPubliclyVisible;

            RatingValue = ratingValue;
            CleanlinessRating = dimensions.Cleanliness;
            SkillRating = dimensions.Skill;
            PunctualityRating = dimensions.Punctuality;
            ConductRating = dimensions.Conduct;
            Comment = comment;
            EditedAt = utcNow;
            LastModifiedAt = utcNow;
            LastModifiedBy = modifiedBy;

            if (wasPublic)
            {
                ModerationStatus = ReviewModerationStatus.Pending;
                ModerationReason = null;
                RaiseUnpublished();
            }

            if (ReplyModerationStatus == ReviewModerationStatus.Published)
                ReplyModerationStatus = ReviewModerationStatus.Pending;
        }

        private void RequireModerationState(ReviewModerationStatus required, string operation)
        {
            if (ModerationStatus != required)
                throw new InvalidAggregateStateException(
                    nameof(Review), operation, ModerationStatus.ToString(),
                    operation == nameof(AddProviderResponse)
                        ? ReviewWords.ReplyWaitsForPublication
                        : ReviewWords.NotInState(ModerationStatus));
        }

        private static string RequireReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainValidationException(nameof(ModerationReason), "دلیل را بنویسید.");

            var trimmed = reason.Trim();
            if (trimmed.Length > 500)
                throw new DomainValidationException(nameof(ModerationReason), "دلیل حداکثر ۵۰۰ نویسه می‌تواند باشد.");

            return trimmed;
        }

        private void SetModeration(ReviewModerationStatus status, string moderatedBy, string? reason)
        {
            ModerationStatus = status;
            ModeratedAt = DateTime.UtcNow;
            ModeratedBy = moderatedBy;
            ModerationReason = reason;
            LastModifiedAt = ModeratedAt;
            LastModifiedBy = moderatedBy;
        }

        /// <summary>
        /// Verifies the review (admin action or automatic verification)
        /// </summary>
        public void Verify(string? modifiedBy = null)
        {
            IsVerified = true;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Unverifies the review (if flagged as suspicious)
        /// </summary>
        public void Unverify(string? modifiedBy = null)
        {
            IsVerified = false;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Share of helpful votes, over baseline plus live votes.
        /// </summary>
        public decimal GetHelpfulnessRatio() => ReviewHelpfulnessPolicy.Ratio(HelpfulCount, NotHelpfulCount);

        /// <summary>
        /// Helpful with at least 5 votes and a 60% helpful share — over baseline plus live votes, never either
        /// half alone. See <see cref="ReviewHelpfulnessPolicy"/>.
        /// </summary>
        public bool IsConsideredHelpful() => ReviewHelpfulnessPolicy.IsConsideredHelpful(HelpfulCount, NotHelpfulCount);
        
        /// <summary>
        /// Gets age of review in days
        /// </summary>
        public int GetAgeInDays()
        {
            return (DateTime.UtcNow - CreatedAt).Days;
        }
        
        /// <summary>
        /// Checks if review is recent (within 30 days)
        /// </summary>
        public bool IsRecent()
        {
            return GetAgeInDays() <= 30;
        }
        
        /// <summary>
        /// Validates rating value
        /// </summary>
        private static void ValidateRating(decimal ratingValue) => ValidateStarValue(ratingValue, "Rating");

        /// <summary>
        /// Validates each dimension that was given. Keyed by the request field name, so a client can tell the
        /// customer which of the four it was.
        /// </summary>
        private static void ValidateDimensions(ReviewDimensionRatings dimensions)
        {
            if (dimensions.Cleanliness is { } cleanliness) ValidateStarValue(cleanliness, nameof(CleanlinessRating));
            if (dimensions.Skill is { } skill) ValidateStarValue(skill, nameof(SkillRating));
            if (dimensions.Punctuality is { } punctuality) ValidateStarValue(punctuality, nameof(PunctualityRating));
            if (dimensions.Conduct is { } conduct) ValidateStarValue(conduct, nameof(ConductRating));
        }

        private static void ValidateStarValue(decimal value, string field)
        {
            if (value < 1.0m || value > 5.0m)
                throw new DomainValidationException(field, $"{ReviewWords.RatingLabel(field)} باید بین ۱ تا ۵ ستاره باشد.");

            // Allow only 0.5 increments (1.0, 1.5, 2.0, 2.5, etc.)
            if (value % 0.5m != 0)
                throw new DomainValidationException(field, $"{ReviewWords.RatingLabel(field)} باید مضربی از نیم ستاره باشد (مثلاً ۳٫۵ یا ۴).");
        }
        
        /// <summary>
        /// A comment is optional: no words at all is no comment. When there are words, the 10–2000 limits apply to
        /// the trimmed text — the length that counts is the one that is stored.
        /// </summary>
        private static string? NormalizeComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return null;
            var trimmed = comment.Trim();
            ValidateComment(trimmed);
            return trimmed;
        }

        private static void ValidateComment(string comment)
        {
            if (comment.Length < 10)
                throw new DomainValidationException(nameof(Comment), "متن نظر باید دست‌کم ۱۰ نویسه باشد.");

            if (comment.Length > 2000)
                throw new DomainValidationException(nameof(Comment), "متن نظر حداکثر ۲۰۰۰ نویسه می‌تواند باشد.");
        }
        
        public override string ToString()
        {
            return $"Review {Id}: {RatingValue}★ by Customer {CustomerId.Value} for Provider {ProviderId.Value}";
        }
    }
    /// <summary>
    /// What a review's refusals say, in Persian: the customer, the salon and the moderator all read them in the apps
    /// (openspec/changes/_inline/customer-reviews-and-nahal-seed). The English wording reached them verbatim.
    /// </summary>
    internal static class ReviewWords
    {
        public const string AlreadyReplied = "این نظر پاسخ دارد؛ همان پاسخ را ویرایش کنید.";
        public const string NoReply = "این نظر پاسخی ندارد.";
        public const string ReplyWaitsForPublication = "پس از تأیید این نظر می‌توانید به آن پاسخ دهید.";

        public static string Label(ReviewModerationStatus status) => status switch
        {
            ReviewModerationStatus.Pending => "در انتظار تأیید",
            ReviewModerationStatus.Published => "منتشرشده",
            ReviewModerationStatus.Rejected => "ردشده",
            ReviewModerationStatus.Hidden => "پنهان‌شده",
            _ => status.ToString(),
        };

        public static string NotInState(ReviewModerationStatus status) =>
            $"این کار برای نظرِ {Label(status)} ممکن نیست.";

        public static string NotInReplyState(ReviewModerationStatus status) =>
            $"این کار برای پاسخِ {Label(status)} ممکن نیست.";

        /// <summary>The request field a rating came in, as the customer knows it.</summary>
        public static string RatingLabel(string field) => field switch
        {
            "Rating" => "امتیاز کلی",
            nameof(Review.CleanlinessRating) => "امتیاز تمیزی و بهداشت",
            nameof(Review.SkillRating) => "امتیاز مهارت و کیفیت کار",
            nameof(Review.PunctualityRating) => "امتیاز وقت‌شناسی",
            nameof(Review.ConductRating) => "امتیاز برخورد و رفتار",
            _ => "امتیاز",
        };
    }
}
