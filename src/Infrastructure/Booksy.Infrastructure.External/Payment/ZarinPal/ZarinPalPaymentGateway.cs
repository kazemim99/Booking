// ========================================
// ZarinPalPaymentGateway.cs
// ========================================
using Booksy.Infrastructure.External.Payment.ZarinPal.Models;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Payment.ZarinPal
{
    /// <summary>
    /// ZarinPal payment gateway adapter implementing IPaymentGateway
    /// </summary>
    public class ZarinPalPaymentGateway : IPaymentGateway
    {
        private readonly IZarinPalService _zarinPalService;
        private readonly ILogger<ZarinPalPaymentGateway> _logger;

        public ZarinPalPaymentGateway(
            IZarinPalService zarinPalService,
            ILogger<ZarinPalPaymentGateway> logger)
        {
            _zarinPalService = zarinPalService;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(
            PaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // For ZarinPal, ProcessPayment creates a payment request
                // The actual payment happens on ZarinPal's website
                var result = await _zarinPalService.CreatePaymentRequestAsync(
                    request.Amount,
                    request.Description,
                    request.Metadata?.GetValueOrDefault("mobile")?.ToString(),
                    request.Metadata?.GetValueOrDefault("email")?.ToString(),
                    cancellationToken);

                return new PaymentResult
                {
                    IsSuccessful = result.IsSuccessful,
                    PaymentId = result.Authority,
                    Status = result.IsSuccessful ? "pending_redirect" : "failed",
                    ErrorMessage = result.ErrorMessage,
                    ErrorCode = result.ErrorCode.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZarinPal payment processing failed");
                return new PaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    ErrorCode = "-1"
                };
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(
            string paymentId,
            decimal amount,
            string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _zarinPalService.RefundPaymentAsync(
                    paymentId, // paymentId is the authority
                    amount,
                    reason,
                    cancellationToken);

                return new RefundResult
                {
                    IsSuccessful = result.IsSuccessful,
                    RefundId = paymentId,
                    Amount = result.IsSuccessful ? amount : 0,
                    ErrorMessage = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZarinPal refund failed");
                return new RefundResult
                {
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PaymentDetails> GetPaymentDetailsAsync(
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            // ZarinPal doesn't provide a direct API to get payment details by authority
            // This would typically be retrieved from the database
            _logger.LogWarning("GetPaymentDetailsAsync not directly supported by ZarinPal API");

            return new PaymentDetails
            {
                PaymentId = paymentId,
                Status = "unknown",
                Amount = 0,
                Currency = "IRR",
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(
            decimal amount,
            string currency,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _zarinPalService.CreatePaymentRequestAsync(
                    amount,
                    metadata?.GetValueOrDefault("description")?.ToString() ?? "Payment",
                    metadata?.GetValueOrDefault("mobile")?.ToString(),
                    metadata?.GetValueOrDefault("email")?.ToString(),
                    cancellationToken);

                return new PaymentIntent
                {
                    Id = result.Authority,
                    ClientSecret = result.PaymentUrl, // Use PaymentUrl as ClientSecret for redirect
                    Amount = amount,
                    Currency = currency,
                    Status = result.IsSuccessful ? "requires_action" : "failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZarinPal payment intent creation failed");
                throw;
            }
        }

        public async Task<PaymentResult> ConfirmPaymentIntentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            // For ZarinPal, confirmation happens via callback from their gateway
            // This method would be called internally after verification
            _logger.LogWarning("ConfirmPaymentIntentAsync requires external verification via callback");

            return new PaymentResult
            {
                IsSuccessful = false,
                PaymentId = paymentIntentId,
                Status = "requires_verification",
                ErrorMessage = "Payment must be verified via ZarinPal callback"
            };
        }

        public Task<PayoutResult> CreatePayoutAsync(
            PayoutRequest request,
            CancellationToken cancellationToken = default)
        {
            // ZarinPal doesn't support payouts through their API
            // Payouts would need to be handled separately
            _logger.LogWarning("Payouts not supported by ZarinPal");

            return Task.FromResult(new PayoutResult
            {
                IsSuccessful = false,
                ErrorMessage = "Payouts not supported by ZarinPal payment gateway"
            });
        }

        public Task<PayoutDetails> GetPayoutDetailsAsync(
            string payoutId,
            CancellationToken cancellationToken = default)
        {
            // ZarinPal doesn't support payouts
            _logger.LogWarning("Payouts not supported by ZarinPal");

            throw new NotSupportedException("Payouts not supported by ZarinPal payment gateway");
        }
    }
}
