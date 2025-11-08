namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for restoring a booking to a previous state
/// </summary>
public sealed class RestoreBookingRequest
{
    /// <summary>
    /// Reason for restoring the booking state
    /// </summary>
    public string? Reason { get; set; }
}
