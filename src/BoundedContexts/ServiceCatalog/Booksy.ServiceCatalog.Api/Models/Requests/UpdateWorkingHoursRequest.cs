using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.Api.Models.Requests;

/// <summary>
/// Request to update provider working hours
/// </summary>
public sealed class UpdateWorkingHoursRequest
{
    [Required]
    public Dictionary<int, DayHoursRequest?> BusinessHours { get; set; } = new();
}

public sealed class DayHoursRequest
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    public bool IsOpen { get; set; }

    public TimeSlotRequest? OpenTime { get; set; }

    public TimeSlotRequest? CloseTime { get; set; }

    public List<BreakTimeRequest> Breaks { get; set; } = new();
}

public sealed class TimeSlotRequest
{
    [Range(0, 23)]
    public int Hours { get; set; }

    [Range(0, 59)]
    public int Minutes { get; set; }
}

public sealed class BreakTimeRequest
{
    [Required]
    public TimeSlotRequest Start { get; set; } = new();

    [Required]
    public TimeSlotRequest End { get; set; } = new();
}
