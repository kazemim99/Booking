using AsanRezerve.ServiceCatalog.Domain.Enums;

namespace AsanRezerve.ServiceCatalog.Domain.Policies;

/// <summary>One review's ratings as the calculator needs them — nothing else is loaded.</summary>
public sealed record ReviewRatingSnapshot(
    ReviewModerationStatus Status,
    decimal Overall,
    decimal? Cleanliness,
    decimal? Skill,
    decimal? Punctuality,
    decimal? Conduct);

/// <summary>One dimension's average and how many reviews rated it. <c>None</c> means nobody has — never zero.</summary>
public readonly record struct DimensionRating(decimal? Average, int Count)
{
    public static readonly DimensionRating None = new(null, 0);
}

/// <summary>A provider's rating, derived from their published reviews.</summary>
/// <param name="Average">Overall average; 0 when <paramref name="PublishedCount"/> is 0, which means unrated.</param>
public sealed record ProviderRating(
    decimal Average,
    int PublishedCount,
    DimensionRating Cleanliness,
    DimensionRating Skill,
    DimensionRating Punctuality,
    DimensionRating Conduct);

/// <summary>
/// Derives a provider's rating from their reviews. The single statement of the rule.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Only <see cref="ReviewModerationStatus.Published"/> reviews count — pending, rejected and hidden ones
/// contribute to nothing, including the dimensions.</item>
/// <item>A dimension is averaged only over the reviews that rated it. Treating an unrated dimension as zero would
/// punish a provider for customers who simply skipped the optional row.</item>
/// <item>Averages are rounded to two decimals, half away from zero.</item>
/// </list>
/// The result is recomputed from scratch every time the set of published reviews changes — never incremented, so
/// there is nothing to drift (design D6).
/// </remarks>
public static class ProviderRatingCalculator
{
    public static ProviderRating Compute(IEnumerable<ReviewRatingSnapshot> reviews)
    {
        var published = reviews.Where(r => r.Status == ReviewModerationStatus.Published).ToList();

        return new ProviderRating(
            published.Count == 0 ? 0m : Round(published.Average(r => r.Overall)),
            published.Count,
            Dimension(published.Select(r => r.Cleanliness)),
            Dimension(published.Select(r => r.Skill)),
            Dimension(published.Select(r => r.Punctuality)),
            Dimension(published.Select(r => r.Conduct)));
    }

    private static DimensionRating Dimension(IEnumerable<decimal?> values)
    {
        var rated = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return rated.Count == 0 ? DimensionRating.None : new DimensionRating(Round(rated.Average()), rated.Count);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
