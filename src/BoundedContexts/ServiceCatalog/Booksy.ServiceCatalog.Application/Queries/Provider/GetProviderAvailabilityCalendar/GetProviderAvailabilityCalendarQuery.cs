using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderAvailabilityCalendar;

/// <summary>
/// Query to get provider availability calendar with time slots and heatmap data
/// Supports 7, 14, or 30-day views for booking calendar display
/// </summary>
public sealed record GetProviderAvailabilityCalendarQuery(
    Guid ProviderId,
    DateOnly StartDate,
    int Days = 7) : IQuery<ProviderAvailabilityCalendarViewModel>
{
    // Enable Redis caching for availability data
    public bool IsCacheable => true;

    // Cache key includes provider, start date, and days for uniqueness
    public string CacheKey => $"provider-availability-calendar:{ProviderId}:{StartDate:yyyy-MM-dd}:{Days}";

    // Cache for 5 minutes (300 seconds) - balance between freshness and performance
    public int CacheExpirationSeconds => 300;
}

/// <summary>
/// View model containing availability calendar data with heatmap information
/// </summary>
public sealed record ProviderAvailabilityCalendarViewModel(
    Guid ProviderId,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    List<DayAvailabilityViewModel> Days,
    AvailabilityHeatmapViewModel Heatmap);

/// <summary>
/// Availability data for a single day
/// </summary>
public sealed record DayAvailabilityViewModel(
    DateOnly Date,
    DayOfWeek DayOfWeek,
    bool IsOpen,
    string? ClosedReason,
    List<TimeSlotViewModel> AvailableSlots,
    List<TimeSlotViewModel> BookedSlots,
    AvailabilityDayStatus Status);

/// <summary>
/// Individual time slot with status
/// </summary>
public sealed record TimeSlotViewModel(
    string StartTime,
    string EndTime,
    int DurationMinutes,
    string Status,
    Guid? StaffId,
    Guid? BookingId,
    bool CanBook);

/// <summary>
/// Availability heatmap for visual calendar display
/// Shows percentage distribution of time slot statuses
/// </summary>
public sealed record AvailabilityHeatmapViewModel(
    decimal AvailablePercentage,
    decimal BookedPercentage,
    decimal BlockedPercentage,
    int TotalSlots,
    int AvailableSlots,
    int BookedSlots,
    int BlockedSlots,
    List<DayHeatmapViewModel> DailyHeatmap);

/// <summary>
/// Heatmap data for a single day (for calendar color coding)
/// </summary>
public sealed record DayHeatmapViewModel(
    DateOnly Date,
    decimal AvailablePercentage,
    string HeatmapColor);

/// <summary>
/// Day status for quick filtering
/// </summary>
public enum AvailabilityDayStatus
{
    FullyBooked,     // No available slots
    HighDemand,      // < 30% available
    Moderate,        // 30-70% available
    MostlyAvailable, // > 70% available
    Closed           // Provider not open
}
