namespace Booksy.ServiceCatalog.API.Models.Requests;

/// <summary>
/// Request model for capturing an authorized payment
/// </summary>
public class CapturePaymentRequest
{
    /// <summary>
    /// Optional capture amount (if partial capture)
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Optional notes about the capture
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}
