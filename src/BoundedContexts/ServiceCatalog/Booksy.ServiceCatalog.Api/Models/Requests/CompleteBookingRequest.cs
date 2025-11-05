using System.ComponentModel.DataAnnotations;

namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for marking a booking as completed
/// </summary>
public class CompleteBookingRequest
{
    /// <summary>
    /// Stripe payment method ID for final payment (if remaining balance exists)
    /// </summary>
    [StringLength(200)]
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// Optional completion notes
    /// </summary>
    [StringLength(1000)]
    public string? CompletionNotes { get; set; }
}
