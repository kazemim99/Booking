using Booksy.Core.Domain.Errors;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Exceptions;

/// <summary>
/// Exception thrown when there is a conflict with business hours or schedule settings
/// </summary>
public sealed class BusinessHoursConflictException : DomainException
{
    public ProviderId? ProviderId { get; }
    public Guid? StaffId { get; }
    public DayOfWeek? ConflictDay { get; }
    public TimeSpan? ConflictStartTime { get; }
    public TimeSpan? ConflictEndTime { get; }
    public TimeSpan? ExistingStartTime { get; }
    public TimeSpan? ExistingEndTime { get; }
    public BusinessHoursConflictType ConflictType { get; }
    public string? ConflictDetails { get; }
    public DateTime? ConflictDate { get; }

    /// <summary>
    /// Initializes a new instance for overlapping business hours
    /// </summary>
    public BusinessHoursConflictException(
        ProviderId providerId,
        DayOfWeek conflictDay,
        TimeSpan conflictStartTime,
        TimeSpan conflictEndTime,
        TimeSpan existingStartTime,
        TimeSpan existingEndTime)
        : base($"Business hours conflict for provider '{providerId}' on {conflictDay}. New hours {conflictStartTime:hh\\:mm}-{conflictEndTime:hh\\:mm} overlap with existing hours {existingStartTime:hh\\:mm}-{existingEndTime:hh\\:mm}.")
    {
        ProviderId = providerId;
        ConflictDay = conflictDay;
        ConflictStartTime = conflictStartTime;
        ConflictEndTime = conflictEndTime;
        ExistingStartTime = existingStartTime;
        ExistingEndTime = existingEndTime;
        ConflictType = BusinessHoursConflictType.OverlappingHours;

        Data.Add("ProviderId", providerId.ToString());
        Data.Add("ConflictDay", conflictDay.ToString());
        Data.Add("ConflictStartTime", conflictStartTime.ToString());
        Data.Add("ConflictEndTime", conflictEndTime.ToString());
        Data.Add("ExistingStartTime", existingStartTime.ToString());
        Data.Add("ExistingEndTime", existingEndTime.ToString());
        Data.Add("ConflictType", ConflictType.ToString());
    }

    /// <summary>
    /// Initializes a new instance for staff schedule conflicts
    /// </summary>
    public BusinessHoursConflictException(
        Guid staffId,
        ProviderId providerId,
        DayOfWeek conflictDay,
        TimeSpan conflictStartTime,
        TimeSpan conflictEndTime,
        string conflictDetails)
        : base($"Staff schedule conflict for staff '{staffId}' on {conflictDay} from {conflictStartTime:hh\\:mm} to {conflictEndTime:hh\\:mm}. {conflictDetails}")
    {
        StaffId = staffId;
        ProviderId = providerId;
        ConflictDay = conflictDay;
        ConflictStartTime = conflictStartTime;
        ConflictEndTime = conflictEndTime;
        ConflictDetails = conflictDetails;
        ConflictType = BusinessHoursConflictType.StaffScheduleConflict;

        Data.Add("StaffId", staffId.ToString());
        Data.Add("ProviderId", providerId.ToString());
        Data.Add("ConflictDay", conflictDay.ToString());
        Data.Add("ConflictStartTime", conflictStartTime.ToString());
        Data.Add("ConflictEndTime", conflictEndTime.ToString());
        Data.Add("ConflictDetails", conflictDetails);
        Data.Add("ConflictType", ConflictType.ToString());
    }

    /// <summary>
    /// Initializes a new instance for invalid time range
    /// </summary>
    public BusinessHoursConflictException(
        TimeSpan startTime,
        TimeSpan endTime,
        string validationMessage)
        : base($"Invalid time range: {startTime:hh\\:mm} to {endTime:hh\\:mm}. {validationMessage}")
    {
        ConflictStartTime = startTime;
        ConflictEndTime = endTime;
        ConflictDetails = validationMessage;
        ConflictType = BusinessHoursConflictType.InvalidTimeRange;

        Data.Add("ConflictStartTime", startTime.ToString());
        Data.Add("ConflictEndTime", endTime.ToString());
        Data.Add("ConflictDetails", validationMessage);
        Data.Add("ConflictType", ConflictType.ToString());
    }

    /// <summary>
    /// Initializes a new instance with custom message
    /// </summary>
    public BusinessHoursConflictException(string message, BusinessHoursConflictType conflictType = BusinessHoursConflictType.General)
        : base(message)
    {
        ConflictType = conflictType;
        Data.Add("ConflictType", ConflictType.ToString());
    }

    /// <summary>
    /// Initializes a new instance with custom message and inner exception
    /// </summary>
    public BusinessHoursConflictException(string message, Exception innerException, BusinessHoursConflictType conflictType = BusinessHoursConflictType.General)
        : base(message, innerException)
    {
        ConflictType = conflictType;
        Data.Add("ConflictType", ConflictType.ToString());
    }

    /// <summary>
    /// Creates exception for business hours outside operating limits
    /// </summary>
    public static BusinessHoursConflictException OutsideOperatingLimits(
        ProviderId providerId,
        DayOfWeek day,
        TimeSpan attemptedStartTime,
        TimeSpan attemptedEndTime,
        TimeSpan operatingStartLimit,
        TimeSpan operatingEndLimit)
    {
        var exception = new BusinessHoursConflictException(
            $"Business hours for provider '{providerId}' on {day} ({attemptedStartTime:hh\\:mm}-{attemptedEndTime:hh\\:mm}) are outside operating limits ({operatingStartLimit:hh\\:mm}-{operatingEndLimit:hh\\:mm}).",
            BusinessHoursConflictType.OutsideOperatingLimits);

        exception.Data.Add("ProviderId", providerId.ToString());
        exception.Data.Add("Day", day.ToString());
        exception.Data.Add("AttemptedStartTime", attemptedStartTime.ToString());
        exception.Data.Add("AttemptedEndTime", attemptedEndTime.ToString());
        exception.Data.Add("OperatingStartLimit", operatingStartLimit.ToString());
        exception.Data.Add("OperatingEndLimit", operatingEndLimit.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for break time conflicts
    /// </summary>
    public static BusinessHoursConflictException BreakTimeConflict(
        ProviderId providerId,
        Guid? staffId,
        TimeSpan breakStartTime,
        TimeSpan breakEndTime,
        TimeSpan workingStartTime,
        TimeSpan workingEndTime)
    {
        var staffContext = staffId != null ? $" for staff '{staffId}'" : "";
        var exception = new BusinessHoursConflictException(
            $"Break time ({breakStartTime:hh\\:mm}-{breakEndTime:hh\\:mm}) conflicts with working hours ({workingStartTime:hh\\:mm}-{workingEndTime:hh\\:mm}) for provider '{providerId}'{staffContext}.",
            BusinessHoursConflictType.BreakTimeConflict);

        exception.Data.Add("ProviderId", providerId.ToString());
        if (staffId != null)
            exception.Data.Add("StaffId", staffId.ToString());
        exception.Data.Add("BreakStartTime", breakStartTime.ToString());
        exception.Data.Add("BreakEndTime", breakEndTime.ToString());
        exception.Data.Add("WorkingStartTime", workingStartTime.ToString());
        exception.Data.Add("WorkingEndTime", workingEndTime.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for appointment time conflicts
    /// </summary>
    public static BusinessHoursConflictException AppointmentTimeConflict(
        ProviderId providerId,
        DateTime appointmentDate,
        TimeSpan appointmentStartTime,
        TimeSpan appointmentEndTime,
        string reason)
    {
        var exception = new BusinessHoursConflictException(
            $"Appointment on {appointmentDate:yyyy-MM-dd} from {appointmentStartTime:hh\\:mm} to {appointmentEndTime:hh\\:mm} conflicts with business hours for provider '{providerId}'. {reason}",
            BusinessHoursConflictType.AppointmentTimeConflict);

        exception.Data.Add("ProviderId", providerId.ToString());
        exception.Data.Add("AppointmentDate", appointmentDate.ToString("yyyy-MM-dd"));
        exception.Data.Add("AppointmentStartTime", appointmentStartTime.ToString());
        exception.Data.Add("AppointmentEndTime", appointmentEndTime.ToString());
        exception.Data.Add("Reason", reason);

        return exception;
    }

    /// <summary>
    /// Creates exception for holiday conflicts
    /// </summary>
    public static BusinessHoursConflictException HolidayConflict(
        ProviderId providerId,
        DateTime holidayDate,
        string holidayName,
        TimeSpan attemptedStartTime,
        TimeSpan attemptedEndTime)
    {
        var exception = new BusinessHoursConflictException(
            $"Cannot set business hours ({attemptedStartTime:hh\\:mm}-{attemptedEndTime:hh\\:mm}) for provider '{providerId}' on {holidayDate:yyyy-MM-dd} due to holiday: {holidayName}.",
            BusinessHoursConflictType.HolidayConflict);

        exception.Data.Add("ProviderId", providerId.ToString());
        exception.Data.Add("HolidayDate", holidayDate.ToString("yyyy-MM-dd"));
        exception.Data.Add("HolidayName", holidayName);
        exception.Data.Add("AttemptedStartTime", attemptedStartTime.ToString());
        exception.Data.Add("AttemptedEndTime", attemptedEndTime.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for minimum duration violations
    /// </summary>
    public static BusinessHoursConflictException MinimumDurationViolation(
        ProviderId providerId,
        DayOfWeek day,
        TimeSpan actualDuration,
        TimeSpan minimumDuration)
    {
        var exception = new BusinessHoursConflictException(
            $"Business hours duration for provider '{providerId}' on {day} ({actualDuration:hh\\:mm}) is below minimum required duration ({minimumDuration:hh\\:mm}).",
            BusinessHoursConflictType.MinimumDurationViolation);

        exception.Data.Add("ProviderId", providerId.ToString());
        exception.Data.Add("Day", day.ToString());
        exception.Data.Add("ActualDuration", actualDuration.ToString());
        exception.Data.Add("MinimumDuration", minimumDuration.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for maximum duration violations
    /// </summary>
    public static BusinessHoursConflictException MaximumDurationViolation(
        ProviderId providerId,
        DayOfWeek day,
        TimeSpan actualDuration,
        TimeSpan maximumDuration)
    {
        var exception = new BusinessHoursConflictException(
            $"Business hours duration for provider '{providerId}' on {day} ({actualDuration:hh\\:mm}) exceeds maximum allowed duration ({maximumDuration:hh\\:mm}).",
            BusinessHoursConflictType.MaximumDurationViolation);

        exception.Data.Add("ProviderId", providerId.ToString());
        exception.Data.Add("Day", day.ToString());
        exception.Data.Add("ActualDuration", actualDuration.ToString());
        exception.Data.Add("MaximumDuration", maximumDuration.ToString());

        return exception;
    }

    /// <summary>
    /// Creates exception for time zone conflicts
    /// </summary>
    public static BusinessHoursConflictException TimeZoneConflict(
        ProviderId providerId,
        string providerTimeZone,
        string systemTimeZone,
        string conflictDescription)
    {
        var exception = new BusinessHoursConflictException(
            $"Time zone conflict for provider '{providerId}'. Provider timezone: {providerTimeZone}, System timezone: {systemTimeZone}. {conflictDescription}",
            BusinessHoursConflictType.TimeZoneConflict);

        exception.Data.Add("ProviderId", providerId.ToString());
        exception.Data.Add("ProviderTimeZone", providerTimeZone);
        exception.Data.Add("SystemTimeZone", systemTimeZone);
        exception.Data.Add("ConflictDescription", conflictDescription);

        return exception;
    }

    /// <summary>
    /// Get error code for this exception
    /// </summary>
    public override ErrorCode ErrorCode => ErrorCode.BUSINESS_HOURS_CONFLICT;

    /// <summary>
    /// Get severity level for this exception
    /// </summary>
    public ExceptionSeverity Severity => ConflictType switch
    {
        BusinessHoursConflictType.AppointmentTimeConflict => ExceptionSeverity.Critical,
        BusinessHoursConflictType.HolidayConflict => ExceptionSeverity.High,
        BusinessHoursConflictType.StaffScheduleConflict => ExceptionSeverity.High,
        BusinessHoursConflictType.TimeZoneConflict => ExceptionSeverity.High,
        BusinessHoursConflictType.OverlappingHours => ExceptionSeverity.Medium,
        BusinessHoursConflictType.BreakTimeConflict => ExceptionSeverity.Medium,
        BusinessHoursConflictType.OutsideOperatingLimits => ExceptionSeverity.Medium,
        BusinessHoursConflictType.MinimumDurationViolation => ExceptionSeverity.Low,
        BusinessHoursConflictType.MaximumDurationViolation => ExceptionSeverity.Low,
        BusinessHoursConflictType.InvalidTimeRange => ExceptionSeverity.Low,
        _ => ExceptionSeverity.Medium
    };

    /// <summary>
    /// Get suggested actions for resolving this exception
    /// </summary>
    public IEnumerable<string> GetSuggestedActions()
    {
        return ConflictType switch
        {
            BusinessHoursConflictType.OverlappingHours => new[]
            {
                "Adjust the start or end time to avoid overlap",
                "Remove existing conflicting hours",
                "Merge overlapping time periods if appropriate"
            },
            BusinessHoursConflictType.StaffScheduleConflict => new[]
            {
                "Adjust staff working hours",
                "Resolve scheduling conflicts",
                "Update staff availability"
            },
            BusinessHoursConflictType.InvalidTimeRange => new[]
            {
                "Ensure start time is before end time",
                "Verify time format is correct",
                "Check for negative time spans"
            },
            BusinessHoursConflictType.OutsideOperatingLimits => new[]
            {
                "Adjust hours to fit within operating limits",
                "Update operating limit policies",
                "Request exemption for extended hours"
            },
            BusinessHoursConflictType.BreakTimeConflict => new[]
            {
                "Schedule breaks within working hours",
                "Adjust break times to avoid conflicts",
                "Review break policy requirements"
            },
            BusinessHoursConflictType.AppointmentTimeConflict => new[]
            {
                "Reschedule conflicting appointments",
                "Extend business hours if needed",
                "Cancel appointments outside business hours"
            },
            BusinessHoursConflictType.HolidayConflict => new[]
            {
                "Remove business hours for holiday dates",
                "Mark as special operating day if open",
                "Update holiday calendar settings"
            },
            BusinessHoursConflictType.MinimumDurationViolation => new[]
            {
                "Extend business hours to meet minimum duration",
                "Review minimum duration requirements",
                "Adjust operating schedule"
            },
            BusinessHoursConflictType.MaximumDurationViolation => new[]
            {
                "Reduce business hours to meet maximum duration",
                "Split long hours with mandatory breaks",
                "Request exemption for extended operations"
            },
            BusinessHoursConflictType.TimeZoneConflict => new[]
            {
                "Verify and correct time zone settings",
                "Update provider location information",
                "Synchronize system time zones"
            },
            _ => new[]
            {
                "Review business hours configuration",
                "Check for scheduling conflicts",
                "Contact support for assistance"
            }
        };
    }

    /// <summary>
    /// Check if this exception is recoverable
    /// </summary>
    public bool IsRecoverable => ConflictType switch
    {
        BusinessHoursConflictType.AppointmentTimeConflict => false,
        BusinessHoursConflictType.HolidayConflict => false,
        _ => true
    };

    /// <summary>
    /// Get the time span of the conflict
    /// </summary>
    public TimeSpan? GetConflictDuration()
    {
        if (ConflictStartTime.HasValue && ConflictEndTime.HasValue)
        {
            return ConflictEndTime.Value - ConflictStartTime.Value;
        }
        return null;
    }

    /// <summary>
    /// Get conflict impact level
    /// </summary>
    public BusinessHoursConflictImpact GetImpactLevel()
    {
        return ConflictType switch
        {
            BusinessHoursConflictType.AppointmentTimeConflict => BusinessHoursConflictImpact.Critical,
            BusinessHoursConflictType.StaffScheduleConflict => BusinessHoursConflictImpact.High,
            BusinessHoursConflictType.HolidayConflict => BusinessHoursConflictImpact.High,
            BusinessHoursConflictType.TimeZoneConflict => BusinessHoursConflictImpact.High,
            BusinessHoursConflictType.OverlappingHours => BusinessHoursConflictImpact.Medium,
            BusinessHoursConflictType.BreakTimeConflict => BusinessHoursConflictImpact.Medium,
            BusinessHoursConflictType.OutsideOperatingLimits => BusinessHoursConflictImpact.Medium,
            _ => BusinessHoursConflictImpact.Low
        };
    }
}

/// <summary>
/// Types of business hours conflicts
/// </summary>
public enum BusinessHoursConflictType
{
    General,
    OverlappingHours,
    StaffScheduleConflict,
    InvalidTimeRange,
    OutsideOperatingLimits,
    BreakTimeConflict,
    AppointmentTimeConflict,
    HolidayConflict,
    MinimumDurationViolation,
    MaximumDurationViolation,
    TimeZoneConflict
}

/// <summary>
/// Impact levels for business hours conflicts
/// </summary>
public enum BusinessHoursConflictImpact
{
    Low,        // Minor configuration issue
    Medium,     // Scheduling inconvenience
    High,       // Operational disruption
    Critical    // Service unavailable
}