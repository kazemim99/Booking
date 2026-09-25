namespace AsanRezerve.ServiceCatalog.Domain.ValueObjects;

/// <summary>
/// The four aspects a customer rates: cleanliness, skill, punctuality, conduct.
/// </summary>
/// <remarks>
/// <para>A null means "not rated", never "rated zero", and a provider's per-dimension average is computed only over
/// the reviews that rated it. Reviews from before the four became required, and from older app versions, may leave
/// any out.</para>
///
/// <para>When the customer sends no overall rating (the current forms), the overall is these four's average to the
/// nearest half star (<see cref="Policies.ReviewOverallRating"/>; openspec/changes/_inline/reviews-and-reschedule-round2
/// D1). An overall that IS sent — older apps — is kept as stated, whatever the aspects say.</para>
///
/// <para>The set is fixed and small on purpose. A fifth dimension is a product decision and a migration,
/// not a data entry — which is why these are four named values rather than a keyed collection.</para>
/// </remarks>
public sealed record ReviewDimensionRatings(
    decimal? Cleanliness = null,
    decimal? Skill = null,
    decimal? Punctuality = null,
    decimal? Conduct = null)
{
    public static readonly ReviewDimensionRatings None = new();
}
