using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.Api.Models.Requests;

public class UpdateWorkingHours
{
    public Dictionary<string, RegistrationDayScheduleRequest?> BusinessHours { get;  set; }
    public int WorkingDaysCount { get;  set; }
    public DateTime UpdatedAt { get;  set; }
}

/// <summary>
/// Request to update provider working hours during registration
/// </summary>
public sealed class UpdateWorkingHoursRequest
{
    [Required]
    public Dictionary<string, RegistrationDayScheduleRequest?> BusinessHours { get; set; } = new();
}

/// <summary>
/// Day schedule in registration flow (uses hours/minutes components)
/// </summary>
public sealed class RegistrationDayScheduleRequest
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    public bool IsOpen { get; set; }

    public TimeComponentsRequest? OpenTime { get; set; }

    public TimeComponentsRequest? CloseTime { get; set; }

    public List<RegistrationBreakPeriodRequest> Breaks { get; set; } = new();
}

/// <summary>
/// Time represented as hours and minutes (for registration forms)
/// </summary>
public sealed class TimeComponentsRequest
{
    [Range(0, 23)]
    public int Hours { get; set; }

    [Range(0, 59)]
    public int Minutes { get; set; }
}

/// <summary>
/// Break period in registration flow
/// </summary>
public sealed class RegistrationBreakPeriodRequest
{
    [Required]
    public TimeComponentsRequest Start { get; set; } = new();

    [Required]
    public TimeComponentsRequest End { get; set; } = new();
}
