namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request to update a single day's business hours for a provider
/// </summary>
public class UpdateProviderBusinessHoursRequest
{
    public bool IsOpen { get; set; }

    public TimeOnly? OpenTime { get; set; }

    public TimeOnly? CloseTime { get; set; }

    public TimeOnly? BreakStart { get; set; }

    public TimeOnly? BreakEnd { get; set; }
}

