using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Review.CreateReview;

/// <summary>
/// Command to create a review for a completed booking
/// </summary>
public sealed record CreateReviewCommand(
    Guid BookingId,
    Guid CustomerId,
    decimal Rating,
    string? Comment = null) : ICommand<CreateReviewResult>;

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
    DateTime CreatedAt);
