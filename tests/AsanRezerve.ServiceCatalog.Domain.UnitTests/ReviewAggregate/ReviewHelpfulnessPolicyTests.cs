using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Policies;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.ReviewAggregate;

/// <summary>
/// Everything derived from vote counts is derived from baseline plus live votes.
/// </summary>
/// <remarks>
/// If only the display added the frozen legacy baseline to the live tally, every review written after the
/// freeze would have a baseline of 0/0 and could never become "considered helpful" however many people voted.
/// The rule takes totals, so the caller must pass legacy + live — and <see cref="Review"/> does.
/// </remarks>
public class ReviewHelpfulnessPolicyTests
{
    [Fact]
    public void With_no_votes_the_ratio_is_zero_and_nothing_is_helpful()
    {
        Assert.Equal(0m, ReviewHelpfulnessPolicy.Ratio(0, 0));
        Assert.False(ReviewHelpfulnessPolicy.IsConsideredHelpful(0, 0));
    }

    [Fact]
    public void The_ratio_is_helpful_over_all_votes()
    {
        // e.g. a baseline of 7/2 plus one live not-helpful vote: 7 helpful out of 10.
        Assert.Equal(0.7m, ReviewHelpfulnessPolicy.Ratio(7, 3));
    }

    [Theory]
    [InlineData(4, 0, false)] // too few votes to judge, however unanimous
    [InlineData(3, 2, true)]  // five votes, exactly 60%
    [InlineData(2, 3, false)] // five votes, 40%
    [InlineData(6, 4, true)]
    public void Helpful_needs_five_votes_and_sixty_percent(int helpful, int notHelpful, bool expected)
    {
        Assert.Equal(expected, ReviewHelpfulnessPolicy.IsConsideredHelpful(helpful, notHelpful));
    }

    // The two aggregate-level checks that lived here — "a review written after the freeze can become helpful on live
    // votes alone" and "the displayed counts are baseline plus live" — moved to ReviewVoteTests (integration). Live
    // tallies have no domain writer any more (the vote command moves them by an atomic SQL delta), so the only honest
    // way to exercise them is the real vote path.
}
