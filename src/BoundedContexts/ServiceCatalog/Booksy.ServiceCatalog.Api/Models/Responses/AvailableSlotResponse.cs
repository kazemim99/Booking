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
