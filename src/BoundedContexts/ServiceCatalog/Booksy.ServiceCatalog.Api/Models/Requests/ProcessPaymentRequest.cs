namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for processing a payment
/// </summary>
public class ProcessPaymentRequest
{
    /// <summary>
    /// Booking ID associated with this payment
    /// </summary>
    [Required]
    public Guid BookingId { get; set; }

    /// <summary>
    /// Provider ID receiving the payment
    /// </summary>
    [Required]
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR, GBP)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment method (e.g., CreditCard, DebitCard, BankTransfer)
    /// </summary>
    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Stripe payment method ID (for saved cards)
    /// </summary>
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// Whether to capture payment immediately or just authorize
    /// </summary>
    public bool CaptureImmediately { get; set; } = true;

    /// <summary>
    /// Optional payment description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Optional metadata for the payment
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
