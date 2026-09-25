using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Services.Reviews;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Review.EditReview;

/// <summary>
/// The author changes their review: within 7 days, while it is pending or published. A published review goes back
/// to the moderation queue, and leaves the provider's rating until it is approved again.
/// </summary>
/// <param name="Rating">As on create: null derives the overall from the four aspects, which are then required.</param>
/// <param name="ShowName">The author's name choice, changeable on every edit.</param>
public sealed record EditReviewCommand(
    Guid ReviewId,
    Guid EditorId,
    decimal? Rating,
    string? Comment,
    ReviewDimensionRatings? Dimensions,
    bool ShowName = true) : ICommand<EditReviewResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record EditReviewResult(
    Guid ReviewId,
    ReviewModerationStatus ModerationStatus,
    DateTime EditedAt,
    decimal Rating = 0,
    bool ShowName = true);

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
                     ?? throw new NotFoundException("این نظر پیدا نشد.");

        // Ownership is checked here, not in the aggregate — the same split Booking uses — so a non-author is a
        // 403, not a validation error.
        if (!review.IsAuthoredBy(UserId.From(request.EditorId)))
            throw new ForbiddenException("فقط نظرهای خودتان را می‌توانید ویرایش کنید.");

        var wasPublic = review.IsPubliclyVisible;
        var now = DateTime.UtcNow;

        var overall = Domain.Aggregates.Review.OverallFrom(request.Rating, request.Dimensions);
        review.EditByAuthor(
            overall, request.Dimensions, request.Comment, $"Customer:{request.EditorId}", now, request.ShowName);
        await _reviews.UpdateAsync(review, cancellationToken);

        // An edit to a published review is a customer-triggered unpublish: the rating must drop it now, inside
        // this command, not one action later (design D6).
        if (wasPublic)
            await _ratings.RecomputeAsync(review.ProviderId, cancellationToken);

        return new EditReviewResult(
            review.Id, review.ModerationStatus, review.EditedAt ?? now, review.RatingValue, review.ShowName);
    }
}
