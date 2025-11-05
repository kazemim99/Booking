using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for rescheduling a booking
/// </summary>
public class RescheduleBookingRequest
{
    /// <summary>
    /// New desired start time for the booking
    /// </summary>
    [Required]
    public DateTime NewStartTime { get; set; }

    /// <summary>
    /// Optional new staff member ID (leave null to keep current staff)
    /// </summary>
    public Guid? NewStaffId { get; set; }

    /// <summary>
    /// Reason for rescheduling
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}
