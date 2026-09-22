using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Reviews;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Review.EditReview;

/// <summary>
/// The author changes their review: within 7 days, while it is pending or published. A published review goes back
/// to the moderation queue, and leaves the provider's rating until it is approved again.
/// </summary>
public sealed record EditReviewCommand(
    Guid ReviewId,
    Guid EditorId,
    decimal Rating,
    string? Comment,
    ReviewDimensionRatings? Dimensions) : ICommand<EditReviewResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record EditReviewResult(Guid ReviewId, ReviewModerationStatus ModerationStatus, DateTime EditedAt);

public sealed class EditReviewCommandHandler : ICommandHandler<EditReviewCommand, EditReviewResult>
{
    private readonly IReviewWriteRepository _reviews;
    private readonly IProviderRatingRecomputer _ratings;

    public EditReviewCommandHandler(IReviewWriteRepository reviews, IProviderRatingRecomputer ratings)
    {
        _reviews = reviews;
        _ratings = ratings;
    }

    public async Task<EditReviewResult> Handle(EditReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviews.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException($"Review with ID {request.ReviewId} not found");

        // Ownership is checked here, not in the aggregate — the same split Booking uses — so a non-author is a
        // 403, not a validation error.
        if (!review.IsAuthoredBy(UserId.From(request.EditorId)))
            throw new ForbiddenException("You can only edit reviews you wrote");

        var wasPublic = review.IsPubliclyVisible;
        var now = DateTime.UtcNow;

        review.EditByAuthor(request.Rating, request.Dimensions, request.Comment, $"Customer:{request.EditorId}", now);
        await _reviews.UpdateAsync(review, cancellationToken);

        // An edit to a published review is a customer-triggered unpublish: the rating must drop it now, inside
        // this command, not one action later (design D6).
        if (wasPublic)
            await _ratings.RecomputeAsync(review.ProviderId, cancellationToken);

        return new EditReviewResult(review.Id, review.ModerationStatus, review.EditedAt ?? now);
    }
}
