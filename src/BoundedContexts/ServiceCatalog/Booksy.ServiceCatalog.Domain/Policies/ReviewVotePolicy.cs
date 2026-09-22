namespace Booksy.ServiceCatalog.Domain.Policies;

public enum ReviewVoteOutcome
{
    Added,
    Withdrawn,
    Changed,
}

/// <summary>What a vote request does, and how it moves the review's two live tallies.</summary>
/// <param name="VoteAfter">The vote the user holds afterwards: true helpful, false not helpful, null none.</param>
public sealed record ReviewVoteDecision(ReviewVoteOutcome Outcome, int HelpfulDelta, int NotHelpfulDelta, bool? VoteAfter);

/// <summary>
/// One vote per user per review, as a toggle: a first vote adds; repeating the vote you hold withdraws it; the
/// opposite vote moves it. A user's request can therefore never add more than one vote.
/// </summary>
public static class ReviewVotePolicy
{
    public static ReviewVoteDecision Decide(bool? existing, bool requested) => existing switch
    {
        null => new(ReviewVoteOutcome.Added, requested ? 1 : 0, requested ? 0 : 1, requested),
        var held when held == requested => new(ReviewVoteOutcome.Withdrawn, requested ? -1 : 0, requested ? 0 : -1, null),
        _ => new(ReviewVoteOutcome.Changed, requested ? 1 : -1, requested ? -1 : 1, requested),
    };
}
