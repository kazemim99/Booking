// ========================================
// VerifyZarinPalPaymentResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payment.VerifyZarinPalPayment
{
    /// <summary>
    /// Result of ZarinPal payment verification
    /// </summary>
    public sealed record VerifyZarinPalPaymentResult(
        Guid PaymentId,
        Guid? BookingId,
        bool IsSuccessful,
        string Status,
        long? RefNumber = null,
        string? CardPan = null,
        decimal? Fee = null,
        string? ErrorMessage = null,
        int? ErrorCode = null)
    {
        public string FailureReason { get; set; }
    }
}
