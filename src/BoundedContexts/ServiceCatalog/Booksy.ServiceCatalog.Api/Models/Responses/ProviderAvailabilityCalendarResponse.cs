namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Provider availability calendar response with time slots and heatmap data
/// Used for booking calendar UI with 7, 14, or 30-day views
/// </summary>
public class ProviderAvailabilityCalendarResponse
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Start date of availability window
    /// </summary>
    public string StartDate { get; set; } = string.Empty;

    /// <summary>
    /// End date of availability window
    /// </summary>
    public string EndDate { get; set; } = string.Empty;

    /// <summary>
    /// Total number of days in the calendar
    /// </summary>
    public int TotalDays { get; set; }

    /// <summary>
    /// Daily availability breakdown
    /// </summary>
    public List<DayAvailabilityResponse> Days { get; set; } = new();

    /// <summary>
    /// Overall availability heatmap for the entire period
    /// </summary>
    public AvailabilityHeatmapResponse Heatmap { get; set; } = new();
}

/// <summary>
/// Availability information for a single day
/// </summary>
public class DayAvailabilityResponse
{
    /// <summary>
    /// Date in ISO format (yyyy-MM-dd)
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Day of week (Monday, Tuesday, etc.)
    /// </summary>
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>
    /// Whether provider is open on this day
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Reason for closure (if applicable)
    /// </summary>
    public string? ClosedReason { get; set; }

    /// <summary>
    /// Available time slots that can be booked
    /// </summary>
    public List<TimeSlotResponse> AvailableSlots { get; set; } = new();

    /// <summary>
    /// Already booked time slots
    /// </summary>
    public List<TimeSlotResponse> BookedSlots { get; set; } = new();

    /// <summary>
    /// Overall status for the day
    /// Values: FullyBooked, HighDemand, Moderate, MostlyAvailable, Closed
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Count of available slots
    /// </summary>
    public int AvailableCount { get; set; }

    /// <summary>
    /// Count of booked slots
    /// </summary>
    public int BookedCount { get; set; }
}

/// <summary>
/// Individual time slot with booking information
/// </summary>
public class TimeSlotResponse
{
    /// <summary>
    /// Start time (HH:mm format)
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// End time (HH:mm format)
    /// </summary>
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// Duration in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Slot status (Available, Booked, Blocked, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Staff member ID (if assigned)
    /// </summary>
    public Guid? StaffId { get; set; }

    /// <summary>
    /// Booking ID (if slot is booked)
    /// </summary>
    public Guid? BookingId { get; set; }

    /// <summary>
    /// Whether this slot can be booked
    /// </summary>
    public bool CanBook { get; set; }
}

/// <summary>
/// Availability heatmap for calendar visualization
/// Shows distribution of available vs booked slots
/// </summary>
public class AvailabilityHeatmapResponse
{
    /// <summary>
    /// Percentage of slots that are available (0-100)
    /// </summary>
    public decimal AvailablePercentage { get; set; }

    /// <summary>
    /// Percentage of slots that are booked (0-100)
    /// </summary>
    public decimal BookedPercentage { get; set; }

    /// <summary>
    /// Percentage of slots that are blocked/unavailable (0-100)
    /// </summary>
    public decimal BlockedPercentage { get; set; }

    /// <summary>
    /// Total number of time slots across all days
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// Number of available slots
    /// </summary>
    public int AvailableSlots { get; set; }

    /// <summary>
    /// Number of booked slots
    /// </summary>
    public int BookedSlots { get; set; }

    /// <summary>
    /// Number of blocked slots
    /// </summary>
    public int BlockedSlots { get; set; }

    /// <summary>
    /// Daily heatmap for calendar color coding
    /// </summary>
    public List<DayHeatmapResponse> DailyHeatmap { get; set; } = new();
}

/// <summary>
/// Heatmap data for a single day (for calendar color coding)
/// </summary>
public class DayHeatmapResponse
{
    /// <summary>
    /// Date in ISO format
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Percentage of available slots for this day (0-100)
    /// </summary>
    public decimal AvailablePercentage { get; set; }

    /// <summary>
    /// Color code for calendar display
    /// Values: green (>70% available), yellow (30-70%), red (<30%), gray (fully booked)
    /// </summary>
    public string HeatmapColor { get; set; } = string.Empty;
}
