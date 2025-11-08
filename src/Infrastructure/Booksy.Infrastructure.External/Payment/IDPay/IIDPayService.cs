// ========================================
// IIDPayService.cs
// ========================================
using Booksy.Infrastructure.External.Payment.IDPay.Models;

namespace Booksy.Infrastructure.External.Payment.IDPay
{
    /// <summary>
    /// Interface for IDPay payment gateway service
    /// </summary>
    public interface IIDPayService
    {
        /// <summary>
        /// Creates a payment request and returns payment ID and payment URL
        /// </summary>
        Task<IDPayPaymentResult> CreatePaymentRequestAsync(
            decimal amount,
            string orderId,
            string description,
            string? name = null,
            string? phone = null,
            string? email = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies payment after customer completes payment
        /// </summary>
        Task<IDPayVerifyResult> VerifyPaymentAsync(
            string paymentId,
            string orderId,
            CancellationToken cancellationToken = default);
    }
}
