using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for getting available time slots
/// </summary>
public class GetAvailableSlotsRequest
{
    /// <summary>
    /// Provider ID
    /// </summary>
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Service ID
    /// </summary>
    [Required]
    public Guid ServiceId { get; set; }

    /// <summary>
    /// Date to check availability for
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Optional specific staff member ID
    /// </summary>
    public Guid? StaffId { get; set; }
}
