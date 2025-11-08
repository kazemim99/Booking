// ========================================
// IZarinPalService.cs
// ========================================
using Booksy.Infrastructure.External.Payment.ZarinPal.Models;

namespace Booksy.Infrastructure.External.Payment.ZarinPal
{
    /// <summary>
    /// Interface for ZarinPal payment gateway service
    /// </summary>
    public interface IZarinPalService
    {
        /// <summary>
        /// Creates a payment request and returns authority code and payment URL
        /// </summary>
        Task<ZarinPalPaymentResult> CreatePaymentRequestAsync(
            decimal amount,
            string description,
            string? mobile = null,
            string? email = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies payment after customer completes payment
        /// </summary>
        Task<ZarinPalVerifyResult> VerifyPaymentAsync(
            string authority,
            decimal amount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refunds a payment (full or partial)
        /// </summary>
        Task<ZarinPalRefundResult> RefundPaymentAsync(
            string authority,
            decimal amount,
            string? description = null,
            CancellationToken cancellationToken = default);
    }
}
