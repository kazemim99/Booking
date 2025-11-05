using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for cancelling a booking
/// </summary>
public class CancelBookingRequest
{
    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// User ID who is cancelling (customer or provider)
    /// </summary>
    [Required]
    public Guid CancelledBy { get; set; }
}
