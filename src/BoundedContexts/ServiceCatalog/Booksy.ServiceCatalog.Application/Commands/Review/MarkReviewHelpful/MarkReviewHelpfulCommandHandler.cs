using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Review.MarkReviewHelpful;

/// <summary>
/// Handler for marking a review as helpful or not helpful
/// Increments the appropriate counter on the review
/// </summary>
public sealed class MarkReviewHelpfulCommandHandler
    : ICommandHandler<MarkReviewHelpfulCommand, MarkReviewHelpfulResult>
{
    private readonly IReviewWriteRepository _reviewWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkReviewHelpfulCommandHandler> _logger;

    public MarkReviewHelpfulCommandHandler(
        IReviewWriteRepository reviewWriteRepository,
        IUnitOfWork unitOfWork,
        ILogger<MarkReviewHelpfulCommandHandler> logger)
    {
        _reviewWriteRepository = reviewWriteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MarkReviewHelpfulResult> Handle(
        MarkReviewHelpfulCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Marking review {ReviewId} as {Helpful}",
            request.ReviewId,
            request.IsHelpful ? "helpful" : "not helpful");

        // 1. Get the review
        var review = await _reviewWriteRepository.GetByIdAsync(
            request.ReviewId,
            cancellationToken);

        if (review == null)
        {
            throw new NotFoundException($"Review with ID {request.ReviewId} not found");
        }

        // 2. Mark as helpful or not helpful
        if (request.IsHelpful)
        {
            review.MarkAsHelpful();
        }
        else
        {
            review.MarkAsNotHelpful();
        }

        // 3. Update the review
        await _reviewWriteRepository.UpdateAsync(review, cancellationToken);


        _logger.LogInformation(
            "Review {ReviewId} marked as {Helpful}. New counts: {HelpfulCount} helpful, {NotHelpfulCount} not helpful",
            request.ReviewId,
            request.IsHelpful ? "helpful" : "not helpful",
            review.HelpfulCount,
            review.NotHelpfulCount);

        // 5. Return result
        return new MarkReviewHelpfulResult(
            ReviewId: review.Id,
            HelpfulCount: review.HelpfulCount,
            NotHelpfulCount: review.NotHelpfulCount,
            HelpfulnessRatio: review.GetHelpfulnessRatio(),
            IsConsideredHelpful: review.IsConsideredHelpful());
    }
}
