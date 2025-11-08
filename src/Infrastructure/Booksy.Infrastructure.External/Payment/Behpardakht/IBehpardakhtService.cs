// ========================================
// IBehpardakhtService.cs
// ========================================
using Booksy.Infrastructure.External.Payment.Behpardakht.Models;

namespace Booksy.Infrastructure.External.Payment.Behpardakht
{
    /// <summary>
    /// Interface for Behpardakht (Bank Mellat) payment gateway service
    /// </summary>
    public interface IBehpardakhtService
    {
        /// <summary>
        /// Creates a payment request (bpPayRequest) and returns RefId
        /// </summary>
        Task<BehpardakhtPaymentResult> CreatePaymentRequestAsync(
            decimal amount,
            string description,
            long payerId = 0,
            string? mobile = null,
            string? email = null,
            string? additionalData = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies payment after customer completes payment (bpVerifyRequest)
        /// </summary>
        Task<BehpardakhtVerifyResult> VerifyPaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Settles the payment (bpSettleRequest)
        /// </summary>
        Task<BehpardakhtSettleResult> SettlePaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inquires about payment status (bpInquiryRequest)
        /// </summary>
        Task<BehpardakhtInquiryResult> InquiryPaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reverses a payment (bpReversalRequest)
        /// </summary>
        Task<BehpardakhtReversalResult> ReversePaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refunds a payment (full or partial) (bpRefundRequest)
        /// </summary>
        Task<BehpardakhtRefundResult> RefundPaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            decimal amount,
            CancellationToken cancellationToken = default);
    }
}
