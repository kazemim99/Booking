
namespace Booksy.ServiceCatalog.API.Models.Requests;

public class BusinessHoursRequest
{
    public bool IsOpen { get; set; }

    public TimeOnly? OpenTime { get; set; }

    public TimeOnly? CloseTime { get; set; }

    public TimeOnly? BreakStart { get; set; }

    public TimeOnly? BreakEnd { get; set; }
}

