using AsanRezerve.ServiceCatalog.Domain.Policies;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>What one vote request does, given the vote the user already holds. The control is a toggle.</summary>
public class ReviewVotePolicyTests
{
    [Theory]
    [InlineData(true, 1, 0)]
    [InlineData(false, 0, 1)]
    public void With_no_vote_yet_the_request_adds_one(bool requested, int helpfulDelta, int notHelpfulDelta)
    {
        var decision = ReviewVotePolicy.Decide(existing: null, requested);

        Assert.Equal(ReviewVoteOutcome.Added, decision.Outcome);
        Assert.Equal(helpfulDelta, decision.HelpfulDelta);
        Assert.Equal(notHelpfulDelta, decision.NotHelpfulDelta);
    }

    [Theory]
    [InlineData(true, -1, 0)]
    [InlineData(false, 0, -1)]
    public void Repeating_the_vote_already_held_withdraws_it(bool held, int helpfulDelta, int notHelpfulDelta)
    {
        var decision = ReviewVotePolicy.Decide(existing: held, requested: held);

        Assert.Equal(ReviewVoteOutcome.Withdrawn, decision.Outcome);
        Assert.Equal(helpfulDelta, decision.HelpfulDelta);
        Assert.Equal(notHelpfulDelta, decision.NotHelpfulDelta);
        Assert.Null(decision.VoteAfter);
    }

    [Theory]
    [InlineData(true, false, -1, 1)]
    [InlineData(false, true, 1, -1)]
    public void The_opposite_vote_moves_it(bool held, bool requested, int helpfulDelta, int notHelpfulDelta)
    {
        var decision = ReviewVotePolicy.Decide(held, requested);

        Assert.Equal(ReviewVoteOutcome.Changed, decision.Outcome);
        Assert.Equal(helpfulDelta, decision.HelpfulDelta);
        Assert.Equal(notHelpfulDelta, decision.NotHelpfulDelta);
        Assert.Equal(requested, decision.VoteAfter);
    }

    [Fact]
    public void Every_outcome_changes_the_total_number_of_votes_by_at_most_one()
    {
        foreach (bool? held in new bool?[] { null, true, false })
        foreach (var requested in new[] { true, false })
        {
            var d = ReviewVotePolicy.Decide(held, requested);
            Assert.InRange(d.HelpfulDelta + d.NotHelpfulDelta, -1, 1);
        }
    }
}
