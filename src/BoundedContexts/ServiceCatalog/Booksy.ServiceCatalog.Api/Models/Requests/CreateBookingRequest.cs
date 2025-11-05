using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for creating a new booking
/// </summary>
public class CreateBookingRequest
{
    /// <summary>
    /// Provider ID offering the service
    /// </summary>
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Service ID to be booked
    /// </summary>
    [Required]
    public Guid ServiceId { get; set; }

    /// <summary>
    /// Staff member ID (optional - system can assign if not specified)
    /// </summary>
    public Guid? StaffId { get; set; }

    /// <summary>
    /// Desired booking start time
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Optional customer notes or special requests
    /// </summary>
    [StringLength(1000)]
    public string? CustomerNotes { get; set; }
}
