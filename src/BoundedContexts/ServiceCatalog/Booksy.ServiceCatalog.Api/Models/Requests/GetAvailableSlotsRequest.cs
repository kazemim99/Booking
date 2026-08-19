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

    /// <summary>
    /// Every service the customer is booking in one appointment.
    ///
    /// <para>A multi-service booking needs a longer slot than a single-service one, so the slot search has to
    /// know the whole set. This property was missing while <see cref="GetAvailableSlotsQuery"/> already accepted
    /// it, so the <c>ServiceIds</c> the client sent were silently dropped by model binding and every caller got
    /// slots sized for one service — the customer would pick a time that could not fit their appointment.</para>
    ///
    /// <para>Optional and additive: when empty the query falls back to <see cref="ServiceId"/>, so
    /// single-service callers are unaffected.</para>
    /// </summary>
    public List<Guid>? ServiceIds { get; set; }
}
