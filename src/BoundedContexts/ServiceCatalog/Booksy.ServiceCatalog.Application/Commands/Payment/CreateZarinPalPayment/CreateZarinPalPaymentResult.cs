// ========================================
// CreateZarinPalPaymentResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Payment.CreateZarinPalPayment
{
    /// <summary>
    /// Result of creating a ZarinPal payment request
    /// </summary>
    public sealed record CreateZarinPalPaymentResult(
        Guid PaymentId,
        string Authority,
        string PaymentUrl,
        decimal Amount,
        string Currency,
        bool IsSuccessful,
        string? ErrorMessage = null,
        int? ErrorCode = null);
}
