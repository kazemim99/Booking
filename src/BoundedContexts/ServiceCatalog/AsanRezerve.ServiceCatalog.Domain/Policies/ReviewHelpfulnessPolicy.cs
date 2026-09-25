namespace AsanRezerve.ServiceCatalog.Domain.Policies;

/// <summary>
/// When a review counts as "helpful", from its vote totals.
/// </summary>
/// <remarks>
/// Takes TOTALS — the frozen legacy baseline plus the live per-user tally. Feeding it either half alone is the
/// defect this exists to prevent: on the baseline alone every review written after the freeze sits at 0/0
/// forever; on the live tally alone every pre-existing review loses the votes it was shown with.
/// </remarks>
public static class ReviewHelpfulnessPolicy
{
    /// <summary>Fewer votes than this and no verdict is drawn, however unanimous.</summary>
    public const int MinimumVotes = 5;

    /// <summary>Share of helpful votes at or above which a review is considered helpful.</summary>
    public const decimal HelpfulThreshold = 0.6m;

    public static decimal Ratio(int helpful, int notHelpful)
    {
        var total = helpful + notHelpful;
        return total == 0 ? 0m : (decimal)helpful / total;
    }

    public static bool IsConsideredHelpful(int helpful, int notHelpful) =>
        helpful + notHelpful >= MinimumVotes && Ratio(helpful, notHelpful) >= HelpfulThreshold;
}
