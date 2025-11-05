namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for refunding a payment
/// </summary>
public class RefundPaymentRequest
{
    /// <summary>
    /// Refund amount (optional - defaults to full refund)
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    [Required]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about the refund
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}
