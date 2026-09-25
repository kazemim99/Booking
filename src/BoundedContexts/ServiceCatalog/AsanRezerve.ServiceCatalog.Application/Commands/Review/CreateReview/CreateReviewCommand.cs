using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Review.CreateReview;

/// <summary>
/// Command to create a review for a completed booking
/// </summary>
/// <param name="Rating">
/// The overall, when an older app sends it. Null — the current forms — derives it from the four aspects, which are
/// then all required (<see cref="Domain.Aggregates.Review.OverallFrom"/>).
/// </param>
/// <param name="ShowName">False signs the public review «مشتری» instead of the author's name.</param>
public sealed record CreateReviewCommand(
    Guid BookingId,
    Guid CustomerId,
    decimal? Rating,
    string? Comment = null,
    ReviewDimensionRatings? Dimensions = null,
    bool ShowName = true) : ICommand<CreateReviewResult>
{
    public Guid? IdempotencyKey { get; init; }
}

/// <summary>
/// Result of creating a review
/// </summary>
public sealed record CreateReviewResult(
    Guid ReviewId,
    Guid ProviderId,
    Guid CustomerId,
    Guid BookingId,
    decimal Rating,
    string? Comment,
    bool IsVerified,
    DateTime CreatedAt,
    ReviewModerationStatus ModerationStatus,
    ReviewDimensionRatings Dimensions,
    bool ShowName = true);
