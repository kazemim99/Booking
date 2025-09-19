namespace Booksy.ServiceCatalog.Domain.Enums.Extensions;

/// <summary>
/// Extension methods for BookingPolicy enum
/// </summary>
public static class BookingPolicyExtensions
{
    /// <summary>
    /// Get the minimum notice period required for free cancellation
    /// </summary>
    public static TimeSpan GetMinimumNoticeRequired(this BookingPolicy policy)
    {
        return policy switch
        {
            BookingPolicy.NoChanges => TimeSpan.MaxValue,
            BookingPolicy.Flexible24Hours => TimeSpan.FromHours(24),
            BookingPolicy.Flexible48Hours => TimeSpan.FromHours(48),
            BookingPolicy.Flexible72Hours => TimeSpan.FromHours(72),
            BookingPolicy.FlexibleOneWeek => TimeSpan.FromDays(7),
            BookingPolicy.Moderate24Hours => TimeSpan.FromHours(24),
            BookingPolicy.Moderate48Hours => TimeSpan.FromHours(48),
            BookingPolicy.StrictWithFee => TimeSpan.FromHours(24),
            BookingPolicy.SameDayWithFee => TimeSpan.Zero,
            BookingPolicy.EmergencyOnly => TimeSpan.MaxValue,
            BookingPolicy.Custom => TimeSpan.Zero,
            _ => TimeSpan.FromHours(24)
        };
    }

    /// <summary>
    /// Check if free cancellation is allowed for the given notice period
    /// </summary>
    public static bool AllowsFreeCancellation(this BookingPolicy policy, TimeSpan noticeGiven)
    {
        if (policy == BookingPolicy.NoChanges || policy == BookingPolicy.EmergencyOnly)
        {
            return false;
        }

        if (policy == BookingPolicy.Custom)
        {
            return true; // Delegate to custom policy implementation
        }

        return noticeGiven >= policy.GetMinimumNoticeRequired();
    }

    /// <summary>
    /// Get the cancellation fee percentage (0-100)
    /// </summary>
    public static decimal GetCancellationFeePercentage(this BookingPolicy policy, TimeSpan noticeGiven)
    {
        return policy switch
        {
            BookingPolicy.NoChanges => 100m,
            BookingPolicy.EmergencyOnly => 100m,

            BookingPolicy.Flexible24Hours or BookingPolicy.Flexible48Hours or
            BookingPolicy.Flexible72Hours or BookingPolicy.FlexibleOneWeek when
            noticeGiven >= policy.GetMinimumNoticeRequired() => 0m,

            BookingPolicy.Moderate24Hours or BookingPolicy.Moderate48Hours when
            noticeGiven >= policy.GetMinimumNoticeRequired() => 50m,

            BookingPolicy.StrictWithFee => 25m,
            BookingPolicy.SameDayWithFee => 100m,
            BookingPolicy.Custom => 0m, // Delegate to custom implementation

            _ => 100m // Default to full fee for late cancellations
        };
    }

    /// <summary>
    /// Check if rescheduling is allowed
    /// </summary>
    public static bool AllowsRescheduling(this BookingPolicy policy, TimeSpan noticeGiven)
    {
        return policy switch
        {
            BookingPolicy.NoChanges => false,
            BookingPolicy.EmergencyOnly => false,
            BookingPolicy.StrictWithFee => noticeGiven >= TimeSpan.FromHours(24), // Only once with notice
            _ => policy.AllowsFreeCancellation(noticeGiven)
        };
    }

    /// <summary>
    /// Get maximum number of reschedules allowed
    /// </summary>
    public static int GetMaximumReschedules(this BookingPolicy policy)
    {
        return policy switch
        {
            BookingPolicy.NoChanges => 0,
            BookingPolicy.EmergencyOnly => 0,
            BookingPolicy.StrictWithFee => 1,
            BookingPolicy.Custom => int.MaxValue, // Delegate to custom implementation
            _ => 3 // Default to 3 reschedules for flexible policies
        };
    }

    /// <summary>
    /// Get user-friendly description of the policy
    /// </summary>
    public static string GetDescription(this BookingPolicy policy)
    {
        return policy switch
        {
            BookingPolicy.NoChanges => "No cancellations or changes allowed once booked",
            BookingPolicy.Flexible24Hours => "Free cancellation up to 24 hours before your appointment",
            BookingPolicy.Flexible48Hours => "Free cancellation up to 48 hours before your appointment",
            BookingPolicy.Flexible72Hours => "Free cancellation up to 72 hours before your appointment",
            BookingPolicy.FlexibleOneWeek => "Free cancellation up to 1 week before your appointment",
            BookingPolicy.Moderate24Hours => "50% refund for cancellations made 24+ hours in advance",
            BookingPolicy.Moderate48Hours => "50% refund for cancellations made 48+ hours in advance",
            BookingPolicy.StrictWithFee => "Cancellation fee applies. One reschedule allowed with 24hr notice",
            BookingPolicy.SameDayWithFee => "Same-day cancellations charged full fee",
            BookingPolicy.EmergencyOnly => "Cancellations allowed only for documented emergencies",
            BookingPolicy.Custom => "Custom cancellation policy - see provider details",
            _ => "Standard cancellation policy applies"
        };
    }

    /// <summary>
    /// Check if policy is customer-friendly
    /// </summary>
    public static bool IsCustomerFriendly(this BookingPolicy policy)
    {
        return policy switch
        {
            BookingPolicy.Flexible24Hours or BookingPolicy.Flexible48Hours or
            BookingPolicy.Flexible72Hours or BookingPolicy.FlexibleOneWeek => true,
            BookingPolicy.Moderate24Hours or BookingPolicy.Moderate48Hours => true,
            _ => false
        };
    }

    /// <summary>
    /// Get policy severity level (1 = most flexible, 5 = most strict)
    /// </summary>
    public static int GetSeverityLevel(this BookingPolicy policy)
    {
        return policy switch
        {
            BookingPolicy.FlexibleOneWeek => 1,
            BookingPolicy.Flexible72Hours => 1,
            BookingPolicy.Flexible48Hours => 2,
            BookingPolicy.Flexible24Hours => 2,
            BookingPolicy.Moderate48Hours => 3,
            BookingPolicy.Moderate24Hours => 3,
            BookingPolicy.StrictWithFee => 4,
            BookingPolicy.SameDayWithFee => 4,
            BookingPolicy.NoChanges => 5,
            BookingPolicy.EmergencyOnly => 5,
            BookingPolicy.Custom => 3, // Neutral for custom policies
            _ => 3
        };
    }
}