// ========================================
// ParsianPaymentGateway.cs
// ========================================
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Payment.Parsian
{
    /// <summary>
    /// Parsian payment gateway adapter implementing IPaymentGateway
    /// Placeholder implementation - to be completed when Parsian integration is needed
    /// </summary>
    public class ParsianPaymentGateway : IPaymentGateway
    {
        private readonly ILogger<ParsianPaymentGateway> _logger;

        public ParsianPaymentGateway(ILogger<ParsianPaymentGateway> logger)
        {
            _logger = logger;
        }

        public Task<PaymentResult> ProcessPaymentAsync(
            PaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Parsian payment gateway is not yet implemented");
            throw new NotImplementedException("Parsian payment gateway integration is not yet implemented. Please use ZarinPal, IDPay, or Behpardakht.");
        }

        public Task<RefundResult> RefundPaymentAsync(
            string paymentId,
            decimal amount,
            string reason,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Parsian payment gateway is not yet implemented");
            throw new NotImplementedException("Parsian payment gateway integration is not yet implemented. Please use ZarinPal, IDPay, or Behpardakht.");
        }

        public Task<PaymentDetails> GetPaymentDetailsAsync(
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Parsian payment gateway is not yet implemented");
            throw new NotImplementedException("Parsian payment gateway integration is not yet implemented. Please use ZarinPal, IDPay, or Behpardakht.");
        }

        public Task<PaymentIntent> CreatePaymentIntentAsync(
            decimal amount,
            string currency,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Parsian payment gateway is not yet implemented");
            throw new NotImplementedException("Parsian payment gateway integration is not yet implemented. Please use ZarinPal, IDPay, or Behpardakht.");
        }

        public Task<PaymentResult> ConfirmPaymentIntentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Parsian payment gateway is not yet implemented");
            throw new NotImplementedException("Parsian payment gateway integration is not yet implemented. Please use ZarinPal, IDPay, or Behpardakht.");
        }

        public Task<PayoutResult> CreatePayoutAsync(
            PayoutRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Parsian payment gateway is not yet implemented");
            throw new NotImplementedException("Parsian payment gateway integration is not yet implemented. Please use ZarinPal, IDPay, or Behpardakht.");
        }

        public Task<PayoutDetails> GetPayoutDetailsAsync(
            string payoutId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Parsian payment gateway is not yet implemented");
            throw new NotImplementedException("Parsian payment gateway integration is not yet implemented. Please use ZarinPal, IDPay, or Behpardakht.");
        }
    }
}
