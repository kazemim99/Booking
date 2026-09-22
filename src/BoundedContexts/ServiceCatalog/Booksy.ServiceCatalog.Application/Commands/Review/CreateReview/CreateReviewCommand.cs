using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Review.CreateReview;

/// <summary>
/// Command to create a review for a completed booking
/// </summary>
public sealed record CreateReviewCommand(
    Guid BookingId,
    Guid CustomerId,
    decimal Rating,
    string? Comment = null,
    ReviewDimensionRatings? Dimensions = null) : ICommand<CreateReviewResult>
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
    ReviewDimensionRatings Dimensions);
