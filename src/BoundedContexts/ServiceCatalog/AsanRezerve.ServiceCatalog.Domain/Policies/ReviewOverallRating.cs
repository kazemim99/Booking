namespace AsanRezerve.ServiceCatalog.Domain.Policies;

/// <summary>
/// A review's overall rating when the customer rated the four aspects and not the overall: their average, to the
/// nearest half star (openspec/changes/_inline/reviews-and-reschedule-round2 D1).
/// </summary>
/// <remarks>
/// Four half-star values average to a multiple of 0.125, so the only ties are the quarters (3.25, 3.75): those round
/// away from zero, up to the next half star — 3.25 → 3.5, 3.75 → 4.0. Pure, so the apps' live «امتیاز کلی» can be
/// checked against it.
/// </remarks>
public static class ReviewOverallRating
{
    /// <summary>What the review form asks for when an aspect is missing and no overall was sent.</summary>
    public const string AllFourRequired = "به هر چهار مورد امتیاز بدهید.";

    public static decimal AverageToHalfStar(decimal cleanliness, decimal skill, decimal punctuality, decimal conduct)
    {
        var average = (cleanliness + skill + punctuality + conduct) / 4m;
        return Math.Round(average * 2m, MidpointRounding.AwayFromZero) / 2m;
    }
}
