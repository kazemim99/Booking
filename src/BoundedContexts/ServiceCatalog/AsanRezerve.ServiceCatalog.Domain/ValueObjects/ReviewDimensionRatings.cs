namespace AsanRezerve.ServiceCatalog.Domain.ValueObjects;

/// <summary>
/// The four optional dimensions a customer may rate alongside their overall verdict.
/// </summary>
/// <remarks>
/// <para>Every dimension is independently omittable: a null means "not rated", never "rated zero", and a
/// provider's per-dimension average is computed only over the reviews that rated it.</para>
///
/// <para>These never feed the overall rating. The overall is the customer's own statement; someone who gives
/// 5 overall and 3 for punctuality is saying "they were late and I still loved it", and averaging that away
/// would put words in their mouth.</para>
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
