using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.Api.Models.Requests;


public class UpdateWorkingHours
{
    public Dictionary<string, DayHoursRequest?> BusinessHours { get;  set; }
    public int WorkingDaysCount { get;  set; }
    public DateTime UpdatedAt { get;  set; }
}
/// <summary>
/// Request to update provider working hours
/// </summary>
public sealed class UpdateWorkingHoursRequest
{
    [Required]
    public Dictionary<string, DayHoursRequest?> BusinessHours { get; set; } = new();
    //public Dictionary<string, DayScheduleRequest> WorkingHours { get; set; }
}

//public class DayScheduleRequest
//{
//    public bool IsOpen { get; set; }
//    public string OpenTime { get; set; }
//    public string CloseTime { get; set; }
//}

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
