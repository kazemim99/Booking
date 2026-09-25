using System.ComponentModel.DataAnnotations;

namespace AsanRezerve.ServiceCatalog.API.Models.Requests;

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
    /// Optional: every service bundled in this visit (multi-service booking,
    /// e.g. cut + color). When set, supersedes ServiceId; slot length and
    /// total price are the sums over these services.
    /// </summary>
    public List<Guid>? ServiceIds { get; set; }

    /// <summary>
    /// Staff provider ID (required - individual provider in hierarchy who will perform the service)
    /// </summary>
    [Required]
    public Guid StaffProviderId { get; set; }

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

    /// <summary>
    /// Optional, provider-side only: the salon's customer-book entry this booking is for.
    /// </summary>
    public Guid? ProviderCustomerId { get; set; }

    /// <summary>
    /// Provider-side only: the customer being booked, when they are not picked from the book.
    /// A salon-entered booking must name its customer — either <see cref="ProviderCustomerId"/>
    /// or these — and the number joins the salon's customer book.
    /// </summary>
    [MaxLength(100)]
    public string? WalkInFirstName { get; set; }

    [MaxLength(100)]
    public string? WalkInLastName { get; set; }

    [MaxLength(20)]
    public string? WalkInPhone { get; set; }

    /// <summary>
    /// Provider-side only: send the customer the confirmation SMS. On by default; the salon turns
    /// it off for someone standing at the counter.
    /// </summary>
    public bool? NotifyCustomer { get; set; }

    /// <summary>A coupon code the customer typed. The server decides the price; this only asks for the code.</summary>
    [MaxLength(40)]
    public string? PromotionCode { get; set; }

    /// <summary>
    /// Alias for StaffProviderId (backward compatibility)
    /// </summary>
    [Required]
    public Guid StaffId { get => StaffProviderId; set => StaffProviderId = value; }
}
