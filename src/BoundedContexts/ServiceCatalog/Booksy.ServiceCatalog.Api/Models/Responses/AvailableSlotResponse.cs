namespace Booksy.ServiceCatalog.Api.Models.Responses;

/// <summary>
/// Available time slot response model
/// </summary>
public class AvailableSlotResponse
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsAvailable { get; set; }
    public Guid? AvailableStaffId { get; set; }
    public string? AvailableStaffName { get; set; }
}

/// <summary>
/// Available slots response with validation messages
/// </summary>
public class AvailableSlotsResponse
{
    public Guid ProviderId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime Date { get; set; }
    public List<AvailableSlotResponse> Slots { get; set; } = new();
    public List<string>? ValidationMessages { get; set; }
}
