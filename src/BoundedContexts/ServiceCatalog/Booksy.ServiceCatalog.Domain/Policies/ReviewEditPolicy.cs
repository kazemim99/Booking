namespace Booksy.ServiceCatalog.Domain.Policies;

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
}
