namespace Booksy.ServiceCatalog.Domain.Enums.Extensions;

/// <summary>
/// Extension methods for AvailabilityStatus enum
/// </summary>
public static class AvailabilityStatusExtensions
{
    /// <summary>
    /// Check if the status allows new bookings
    /// </summary>
    public static bool AllowsBooking(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Available => true,
            AvailabilityStatus.LimitedAvailability => true,
            AvailabilityStatus.Overtime => true,
            AvailabilityStatus.OnCall => false, // Only for urgent/emergency bookings
            _ => false
        };
    }

    /// <summary>
    /// Check if the status allows urgent/priority bookings
    /// </summary>
    public static bool AllowsUrgentBooking(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Available => true,
            AvailabilityStatus.LimitedAvailability => true,
            AvailabilityStatus.Overtime => true,
            AvailabilityStatus.OnCall => true,
            AvailabilityStatus.OnBreak => true, // Can interrupt break for urgent
            _ => false
        };
    }

    /// <summary>
    /// Check if the status is considered "working" (present and potentially productive)
    /// </summary>
    public static bool IsWorking(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Available => true,
            AvailabilityStatus.Busy => true,
            AvailabilityStatus.Blocked => true,
            AvailabilityStatus.OnBreak => true,
            AvailabilityStatus.AtLunch => true,
            AvailabilityStatus.InTraining => true,
            AvailabilityStatus.Tentative => true,
            AvailabilityStatus.LimitedAvailability => true,
            AvailabilityStatus.Preparing => true,
            AvailabilityStatus.Overtime => true,
            AvailabilityStatus.OnCall => true,
            AvailabilityStatus.Maintenance => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if the status indicates temporary unavailability (will return to available)
    /// </summary>
    public static bool IsTemporary(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Busy => true,
            AvailabilityStatus.Blocked => true,
            AvailabilityStatus.OnBreak => true,
            AvailabilityStatus.AtLunch => true,
            AvailabilityStatus.InTraining => true,
            AvailabilityStatus.Tentative => true,
            AvailabilityStatus.Emergency => true,
            AvailabilityStatus.Preparing => true,
            AvailabilityStatus.Maintenance => true,
            _ => false
        };
    }

    /// <summary>
    /// Get display color for UI representation
    /// </summary>
    public static string GetDisplayColor(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Available => "#22C55E", // Green
            AvailabilityStatus.LimitedAvailability => "#84CC16", // Light Green
            AvailabilityStatus.Busy => "#EF4444", // Red
            AvailabilityStatus.Blocked => "#F97316", // Orange
            AvailabilityStatus.OnBreak => "#3B82F6", // Blue
            AvailabilityStatus.AtLunch => "#8B5CF6", // Purple
            AvailabilityStatus.OutOfOffice => "#6B7280", // Gray
            AvailabilityStatus.Offline => "#374151", // Dark Gray
            AvailabilityStatus.InTraining => "#06B6D4", // Cyan
            AvailabilityStatus.Tentative => "#FBBF24", // Yellow
            AvailabilityStatus.Emergency => "#DC2626", // Dark Red
            AvailabilityStatus.Preparing => "#10B981", // Emerald
            AvailabilityStatus.Overtime => "#F59E0B", // Amber
            AvailabilityStatus.OnCall => "#8B5CF6", // Purple
            AvailabilityStatus.Maintenance => "#64748B", // Slate
            _ => "#9CA3AF" // Default Gray
        };
    }

    /// <summary>
    /// Get user-friendly display text
    /// </summary>
    public static string GetDisplayText(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Available => "Available",
            AvailabilityStatus.Busy => "In Appointment",
            AvailabilityStatus.Blocked => "Blocked",
            AvailabilityStatus.OnBreak => "On Break",
            AvailabilityStatus.AtLunch => "At Lunch",
            AvailabilityStatus.OutOfOffice => "Out of Office",
            AvailabilityStatus.Offline => "Offline",
            AvailabilityStatus.InTraining => "In Training",
            AvailabilityStatus.Tentative => "Tentatively Booked",
            AvailabilityStatus.LimitedAvailability => "Limited Availability",
            AvailabilityStatus.Emergency => "Emergency Unavailable",
            AvailabilityStatus.Preparing => "Preparing",
            AvailabilityStatus.Overtime => "Overtime Available",
            AvailabilityStatus.OnCall => "On Call",
            AvailabilityStatus.Maintenance => "Maintenance",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get icon representation for UI
    /// </summary>
    public static string GetIcon(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Available => "✅",
            AvailabilityStatus.Busy => "🔴",
            AvailabilityStatus.Blocked => "🚫",
            AvailabilityStatus.OnBreak => "☕",
            AvailabilityStatus.AtLunch => "🍽️",
            AvailabilityStatus.OutOfOffice => "📅",
            AvailabilityStatus.Offline => "⚪",
            AvailabilityStatus.InTraining => "📚",
            AvailabilityStatus.Tentative => "❓",
            AvailabilityStatus.LimitedAvailability => "⚠️",
            AvailabilityStatus.Emergency => "🚨",
            AvailabilityStatus.Preparing => "⏳",
            AvailabilityStatus.Overtime => "🌙",
            AvailabilityStatus.OnCall => "📞",
            AvailabilityStatus.Maintenance => "🔧",
            _ => "❔"
        };
    }

    /// <summary>
    /// Get priority level for status (1 = highest priority to show, 5 = lowest)
    /// </summary>
    public static int GetPriorityLevel(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Emergency => 1,
            AvailabilityStatus.OutOfOffice => 1,
            AvailabilityStatus.Busy => 2,
            AvailabilityStatus.Blocked => 2,
            AvailabilityStatus.OnBreak => 3,
            AvailabilityStatus.AtLunch => 3,
            AvailabilityStatus.InTraining => 3,
            AvailabilityStatus.Tentative => 3,
            AvailabilityStatus.Preparing => 4,
            AvailabilityStatus.Maintenance => 4,
            AvailabilityStatus.Available => 5,
            AvailabilityStatus.LimitedAvailability => 5,
            AvailabilityStatus.Overtime => 5,
            AvailabilityStatus.OnCall => 4,
            AvailabilityStatus.Offline => 1,
            _ => 3
        };
    }

    /// <summary>
    /// Check if status change is allowed from current status
    /// </summary>
    public static bool CanTransitionTo(this AvailabilityStatus currentStatus, AvailabilityStatus newStatus)
    {
        // Emergency can override almost anything
        if (newStatus == AvailabilityStatus.Emergency)
            return true;

        // Cannot change from emergency without explicit intervention
        if (currentStatus == AvailabilityStatus.Emergency && newStatus != AvailabilityStatus.Available)
            return false;

        // Cannot book over existing busy time
        if (currentStatus == AvailabilityStatus.Busy && newStatus == AvailabilityStatus.Busy)
            return false;

        // Can always return to available from temporary states
        if (currentStatus.IsTemporary() && newStatus == AvailabilityStatus.Available)
            return true;

        // Specific business rules
        return (currentStatus, newStatus) switch
        {
            // From Available
            (AvailabilityStatus.Available, _) => true,

            // From Offline - can only go to Available or Emergency
            (AvailabilityStatus.Offline, AvailabilityStatus.Available) => true,
            (AvailabilityStatus.Offline, AvailabilityStatus.Emergency) => true,
            (AvailabilityStatus.Offline, _) => false,

            // From OutOfOffice - limited transitions
            (AvailabilityStatus.OutOfOffice, AvailabilityStatus.Available) => true,
            (AvailabilityStatus.OutOfOffice, AvailabilityStatus.Emergency) => true,
            (AvailabilityStatus.OutOfOffice, _) => false,

            // Default - most transitions allowed for flexibility
            _ => true
        };
    }

    /// <summary>
    /// Get estimated duration for temporary statuses
    /// </summary>
    public static TimeSpan? GetTypicalDuration(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.OnBreak => TimeSpan.FromMinutes(15),
            AvailabilityStatus.AtLunch => TimeSpan.FromMinutes(60),
            AvailabilityStatus.Preparing => TimeSpan.FromMinutes(10),
            AvailabilityStatus.Maintenance => TimeSpan.FromMinutes(30),
            AvailabilityStatus.InTraining => TimeSpan.FromHours(2),
            _ => null
        };
    }

    /// <summary>
    /// Group statuses by category for filtering
    /// </summary>
    public static AvailabilityCategory GetCategory(this AvailabilityStatus status)
    {
        return status switch
        {
            AvailabilityStatus.Available or AvailabilityStatus.LimitedAvailability
                or AvailabilityStatus.Overtime or AvailabilityStatus.OnCall => AvailabilityCategory.Available,

            AvailabilityStatus.Busy or AvailabilityStatus.Tentative => AvailabilityCategory.Booked,

            AvailabilityStatus.OnBreak or AvailabilityStatus.AtLunch
                or AvailabilityStatus.Preparing => AvailabilityCategory.Break,

            AvailabilityStatus.OutOfOffice or AvailabilityStatus.Offline
                or AvailabilityStatus.Emergency => AvailabilityCategory.Unavailable,

            AvailabilityStatus.Blocked or AvailabilityStatus.InTraining
                or AvailabilityStatus.Maintenance => AvailabilityCategory.Blocked,

            _ => AvailabilityCategory.Unknown
        };
    }
}
