using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Review.CastReviewVote;

/// <summary>
/// An authenticated user marks a published review helpful or not helpful. One vote per user per review; repeating
/// the vote you hold withdraws it, and the opposite vote moves it.
/// </summary>
/// <remarks>
/// <para><b>Concurrency.</b> The live tallies move by an atomic SQL delta on this command's transaction, and the
/// unique (review, user) index is what makes a racing duplicate vote fail — rolling its delta back with it. A
/// read-modify-write of the counters would lose updates: the review's Version token does not protect them.</para>
///
/// <para>Replaces the old anonymous, unlimited <c>MarkReviewHelpfulCommand</c>.</para>
/// </remarks>
public sealed record CastReviewVoteCommand(Guid ReviewId, Guid VoterId, bool IsHelpful) : ICommand<CastReviewVoteResult>
{
    public Guid? IdempotencyKey { get; init; }
}

/// <param name="MyVote">The vote the caller holds now: true helpful, false not helpful, null none.</param>
public sealed record CastReviewVoteResult(
    Guid ReviewId,
    int HelpfulCount,
    int NotHelpfulCount,
    decimal HelpfulnessRatio,
    bool IsConsideredHelpful,
    bool? MyVote);

public sealed class CastReviewVoteCommandHandler : ICommandHandler<CastReviewVoteCommand, CastReviewVoteResult>
{
    private readonly IReviewReadRepository _read;
    private readonly IReviewWriteRepository _write;

    public CastReviewVoteCommandHandler(IReviewReadRepository read, IReviewWriteRepository write)
    {
        _read = read;
        _write = write;
    }

    public async Task<CastReviewVoteResult> Handle(CastReviewVoteCommand request, CancellationToken cancellationToken)
    {
        var review = await _read.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException("این نظر پیدا نشد.");
        var voter = UserId.From(request.VoterId);

        if (review.IsAuthoredBy(voter))
            throw new ForbiddenException("به نظر خودتان نمی‌توانید رأی بدهید.");

        if (!review.IsPubliclyVisible)
            throw new InvalidAggregateStateException(
                nameof(Domain.Aggregates.Review), "Vote", review.ModerationStatus.ToString(),
                "فقط به نظرهای منتشرشده می‌توانید رأی بدهید.");

        var existing = await _write.GetVoteAsync(review.Id, voter, cancellationToken);
        var decision = ReviewVotePolicy.Decide(existing?.IsHelpful, request.IsHelpful);
        var now = DateTime.UtcNow;

        switch (decision.Outcome)
        {
            case ReviewVoteOutcome.Added:
                await _write.AddVoteAsync(ReviewVote.Cast(review.Id, voter, request.IsHelpful, now), cancellationToken);
                break;
            case ReviewVoteOutcome.Withdrawn:
                _write.RemoveVote(existing!);
                break;
            case ReviewVoteOutcome.Changed:
                existing!.ChangeTo(request.IsHelpful, now);
                break;
        }

        await _write.AdjustVoteTalliesAsync(review.Id, decision.HelpfulDelta, decision.NotHelpfulDelta, cancellationToken);

        // Re-read on the same connection and transaction, so it sees the delta just applied.
        var after = await _read.GetByIdAsync(review.Id, cancellationToken) ?? review;
        return new CastReviewVoteResult(
            after.Id,
            after.HelpfulCount,
            after.NotHelpfulCount,
            after.GetHelpfulnessRatio(),
            after.IsConsideredHelpful(),
            decision.VoteAfter);
    }
}
