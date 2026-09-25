using AsanRezerve.Core.Application.Abstractions.CQRS;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetProviderAvailabilityCalendar;

/// <summary>
/// Query to get provider availability calendar with time slots and heatmap data
/// Supports 7, 14, or 30-day views for booking calendar display
/// </summary>
public sealed record GetProviderAvailabilityCalendarQuery(
    Guid ProviderId,
    DateOnly StartDate,
    int Days = 7) : IQuery<ProviderAvailabilityCalendarViewModel>;
// Not cacheable. It was, for 5 minutes with sliding expiration and nothing evicting it on a booking, so a calendar
// kept being read kept offering slots that were already booked (add-observability-and-caching).

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
