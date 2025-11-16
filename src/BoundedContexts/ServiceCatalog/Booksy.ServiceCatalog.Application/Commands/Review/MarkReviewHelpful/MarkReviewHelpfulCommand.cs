using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Review.MarkReviewHelpful;

/// <summary>
/// Command to mark a review as helpful or not helpful
/// </summary>
public sealed record MarkReviewHelpfulCommand(
    Guid ReviewId,
    bool IsHelpful) : ICommand<MarkReviewHelpfulResult>
{
    public Guid? IdempotencyKey { get; init; }
}

/// <summary>
/// Result of marking a review as helpful
/// </summary>
public sealed record MarkReviewHelpfulResult(
    Guid ReviewId,
    int HelpfulCount,
    int NotHelpfulCount,
    decimal HelpfulnessRatio,
    bool IsConsideredHelpful);
