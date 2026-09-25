using System.ComponentModel.DataAnnotations;

namespace AsanRezerve.ServiceCatalog.API.Models.Requests;

/// <summary>The visit to price. Same shape as a booking request, without who does it (price does not depend on that).</summary>
public class QuoteBookingPriceRequest
{
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>Kept for single-service callers; <see cref="ServiceIds"/> wins when present.</summary>
    public Guid? ServiceId { get; set; }

    public List<Guid>? ServiceIds { get; set; }

    /// <summary>The appointment's start on the salon's clock, as for booking creation.</summary>
    [Required]
    public DateTime StartTime { get; set; }

    [MaxLength(40)]
    public string? PromotionCode { get; set; }
}
