using System.Globalization;
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Application.Services.Reviews;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Review.ModerateReview;

/// <summary>What an administrator decided about a review or its provider reply.</summary>
public enum ReviewModerationAction
{
    Approve,
    Reject,
    Hide,
    Restore,
    ApproveReply,
    RejectReply,
}

/// <summary>
/// An administrator's decision on a review or on the provider's reply to it.
/// </summary>
/// <remarks>
/// One command rather than six: every action is the same load → one domain transition → recompute → save, and
/// the legality of each transition lives in the aggregate. Authorisation (administrators only) is the endpoint's.
/// </remarks>
public sealed record ModerateReviewCommand(
    Guid ReviewId,
    ReviewModerationAction Action,
    string ModeratedBy,
    string? Reason = null) : ICommand<ModerateReviewResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record ModerateReviewResult(
    Guid ReviewId,
    ReviewModerationStatus ModerationStatus,
    ReviewModerationStatus? ReplyModerationStatus);

public sealed class ModerateReviewCommandHandler : ICommandHandler<ModerateReviewCommand, ModerateReviewResult>
{
    private readonly IReviewWriteRepository _reviews;
    private readonly IProviderRatingRecomputer _ratings;
    private readonly IProviderReadRepository _providers;
    private readonly INotificationRaiser _notifications;
    private readonly ILogger<ModerateReviewCommandHandler> _logger;

    public ModerateReviewCommandHandler(
        IReviewWriteRepository reviews,
        IProviderRatingRecomputer ratings,
        IProviderReadRepository providers,
        INotificationRaiser notifications,
        ILogger<ModerateReviewCommandHandler> logger)
    {
        _reviews = reviews;
        _ratings = ratings;
        _providers = providers;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ModerateReviewResult> Handle(ModerateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviews.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException($"Review with ID {request.ReviewId} not found");

        var wasPublic = review.IsPubliclyVisible;
        var wasEverPublished = review.FirstPublishedAt is not null;
        var replyWasPublic = review.IsReplyPubliclyVisible;

        switch (request.Action)
        {
            case ReviewModerationAction.Approve: review.Publish(request.ModeratedBy); break;
            case ReviewModerationAction.Reject: review.Reject(request.Reason ?? string.Empty, request.ModeratedBy); break;
            case ReviewModerationAction.Hide: review.Hide(request.Reason ?? string.Empty, request.ModeratedBy); break;
            case ReviewModerationAction.Restore: review.Restore(request.ModeratedBy); break;
            case ReviewModerationAction.ApproveReply: review.ApproveReply(request.ModeratedBy); break;
            case ReviewModerationAction.RejectReply: review.RejectReply(request.Reason ?? string.Empty, request.ModeratedBy); break;
            default: throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unknown moderation action");
        }

        await _reviews.UpdateAsync(review, cancellationToken);

        // Inline, on this command's own unit of work — never from a domain event handler, which would run on
        // another scope before this change is saved and leave the rating one action stale (design D6).
        if (review.IsPubliclyVisible != wasPublic)
            await _ratings.RecomputeAsync(review.ProviderId, cancellationToken);

        // Only a reply APPROVAL announces a reply. Restoring a hidden review also makes its old reply visible again,
        // and telling the customer "the salon replied" a second time would be false.
        await NotifyAsync(review, becamePublic: review.IsPubliclyVisible && !wasPublic, wasEverPublished,
            replyBecamePublic: request.Action == ReviewModerationAction.ApproveReply
                               && review.IsReplyPubliclyVisible && !replyWasPublic,
            rejected: request.Action == ReviewModerationAction.Reject,
            cancellationToken);

        _logger.LogInformation(
            "Review {ReviewId} moderated: {Action} by {ModeratedBy}", review.Id, request.Action, request.ModeratedBy);

        return new ModerateReviewResult(review.Id, review.ModerationStatus, review.ReplyModerationStatus);
    }

    /// <summary>
    /// Tells people at the moment something became public — here, inline, on this command's unit of work, so the
    /// notification commits with the decision or not at all. Not from a domain event handler: those run on another
    /// scope before this change is saved. A rejection tells the author why (task 7.7, decided 2026-09-22); hiding
    /// stays silent, because it is usually someone else's report and it can be undone.
    /// </summary>
    private async Task NotifyAsync(
        Domain.Aggregates.Review review, bool becamePublic, bool wasEverPublished, bool replyBecamePublic,
        bool rejected, CancellationToken cancellationToken)
    {
        if (!becamePublic && !replyBecamePublic && !rejected)
            return;

        var provider = await _providers.GetByIdAsync(review.ProviderId, cancellationToken);
        var parameters = new Dictionary<string, string>
        {
            [NotificationParameter.BusinessName] = provider?.Profile.BusinessName ?? string.Empty,
            [NotificationParameter.Rating] = review.RatingValue.ToString(CultureInfo.InvariantCulture),
        };

        if (review.ModerationReason is { Length: > 0 } moderationReason)
            parameters[NotificationParameter.Reason] = moderationReason;

        if (becamePublic && provider is not null)
        {
            // Addressed to the owner's user id: the inbox and preferences are keyed by user, never by provider.
            // A first publication happens once per review, so the review id is its natural dedup key; every
            // re-publication is a distinct occasion the salon should hear about.
            await _notifications.RaiseAsync(
                wasEverPublished ? NotificationEventCode.ReviewRepublished : NotificationEventCode.ReviewPublished,
                provider.OwnerId.Value,
                dedupKey: wasEverPublished ? Guid.NewGuid() : review.Id,
                parameters: parameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: review.BookingId,
                cancellationToken: cancellationToken);
        }

        if (rejected)
        {
            // To the author, not the salon: the salon never saw this review published and has nothing to act on.
            // Rejection is permanent, so it happens once per review and the review id is its dedup key.
            await _notifications.RaiseAsync(
                NotificationEventCode.ReviewRejected,
                review.CustomerId.Value,
                dedupKey: review.Id,
                parameters: parameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: review.BookingId,
                cancellationToken: cancellationToken);
        }

        if (replyBecamePublic)
        {
            await _notifications.RaiseAsync(
                NotificationEventCode.ReviewReplyPublished,
                review.CustomerId.Value,
                dedupKey: Guid.NewGuid(),
                parameters: parameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: review.BookingId,
                cancellationToken: cancellationToken);
        }
    }
}
