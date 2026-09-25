namespace AsanRezerve.ServiceCatalog.Domain.Policies;

/// <summary>
/// How long a review's author may change it.
/// </summary>
/// <remarks>
/// Seven days is what the customer-profile spec and the shipped web client already promise
/// ("فقط نظرات کمتر از ۷ روز قابل ویرایش هستند"). The bound is inclusive: the last instant of day seven
/// is still inside. Time is passed in rather than read, so the rule is testable without a clock.
/// </remarks>
public static class ReviewEditPolicy
{
    public const int WindowDays = 7;

    public static bool IsInsideWindow(DateTime createdAtUtc, DateTime utcNow) =>
        utcNow <= createdAtUtc.AddDays(WindowDays);

    /// <summary>
    /// Whether the author may edit it right now — the two rules <c>Review.EditByAuthor</c> enforces: still Pending or
    /// Published (a rejected or hidden review is not editable), and inside the window.
    /// </summary>
    public static bool CanEdit(Enums.ReviewModerationStatus status, DateTime createdAtUtc, DateTime utcNow) =>
        status is Enums.ReviewModerationStatus.Pending or Enums.ReviewModerationStatus.Published
        && IsInsideWindow(createdAtUtc, utcNow);
}
