using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for confirming a booking with payment
/// </summary>
public class ConfirmBookingRequest
{
    /// <summary>
    /// Stripe payment method ID for processing deposit
    /// </summary>
    [Required]
    [StringLength(200)]
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Optional staff notes for the booking
    /// </summary>
    [StringLength(1000)]
    public string? StaffNotes { get; set; }
}
