// ========================================
// BehpardakhtPaymentGateway.cs
// ========================================
using Booksy.Infrastructure.External.Payment.Behpardakht.Models;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Payment.Behpardakht
{
    /// <summary>
    /// Behpardakht payment gateway adapter implementing IPaymentGateway
    /// </summary>
    public class BehpardakhtPaymentGateway : IPaymentGateway
    {
        private readonly IBehpardakhtService _behpardakhtService;
        private readonly ILogger<BehpardakhtPaymentGateway> _logger;

        public BehpardakhtPaymentGateway(
            IBehpardakhtService behpardakhtService,
            ILogger<BehpardakhtPaymentGateway> logger)
        {
            _behpardakhtService = behpardakhtService;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(
            PaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // For Behpardakht, ProcessPayment creates a payment request
                // The actual payment happens on Behpardakht's website
                var result = await _behpardakhtService.CreatePaymentRequestAsync(
                    request.Amount,
                    request.Description,
                    payerId: 0,
                    request.Metadata?.GetValueOrDefault("mobile")?.ToString(),
                    request.Metadata?.GetValueOrDefault("email")?.ToString(),
                    request.Metadata?.GetValueOrDefault("additionalData")?.ToString(),
                    cancellationToken);

                return new PaymentResult
                {
                    IsSuccessful = result.IsSuccessful,
                    PaymentId = result.RefId ?? string.Empty,
                    Status = result.IsSuccessful ? "pending_redirect" : "failed",
                    ErrorMessage = result.ErrorMessage,
                    ErrorCode = result.ErrorCode.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Behpardakht payment processing failed");
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
                // Extract order IDs from paymentId (would be stored in metadata)
                // For simplicity, using dummy values here
                long orderId = DateTime.UtcNow.Ticks;
                long saleOrderId = orderId - 1;
                long saleReferenceId = long.Parse(paymentId.Replace("REF", "").Substring(0, 10));

                var result = await _behpardakhtService.RefundPaymentAsync(
                    orderId,
                    saleOrderId,
                    saleReferenceId,
                    amount,
                    cancellationToken);

                return new RefundResult
                {
                    IsSuccessful = result.IsSuccessful,
                    RefundId = orderId.ToString(),
                    Amount = result.IsSuccessful ? amount : 0,
                    ErrorMessage = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Behpardakht refund failed");
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
            // Behpardakht doesn't provide a direct API to get payment details by RefId
            // This would typically be retrieved from the database
            _logger.LogWarning("GetPaymentDetailsAsync not directly supported by Behpardakht API");

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
                var result = await _behpardakhtService.CreatePaymentRequestAsync(
                    amount,
                    metadata?.GetValueOrDefault("description")?.ToString() ?? "Payment",
                    payerId: 0,
                    metadata?.GetValueOrDefault("mobile")?.ToString(),
                    metadata?.GetValueOrDefault("email")?.ToString(),
                    metadata?.GetValueOrDefault("additionalData")?.ToString(),
                    cancellationToken);

                return new PaymentIntent
                {
                    Id = result.RefId ?? string.Empty,
                    ClientSecret = result.PaymentUrl ?? string.Empty,
                    Amount = amount,
                    Currency = currency,
                    Status = result.IsSuccessful ? "requires_action" : "failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Behpardakht payment intent creation failed");
                throw;
            }
        }

        public async Task<PaymentResult> ConfirmPaymentIntentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            // For Behpardakht, confirmation happens via callback from their gateway
            // This method would be called internally after verification
            _logger.LogWarning("ConfirmPaymentIntentAsync requires external verification via callback");

            return new PaymentResult
            {
                IsSuccessful = false,
                PaymentId = paymentIntentId,
                Status = "requires_verification",
                ErrorMessage = "Payment must be verified via Behpardakht callback"
            };
        }

        public Task<PayoutResult> CreatePayoutAsync(
            PayoutRequest request,
            CancellationToken cancellationToken = default)
        {
            // Behpardakht doesn't support payouts through their API
            // Payouts would need to be handled separately
            _logger.LogWarning("Payouts not supported by Behpardakht");

            return Task.FromResult(new PayoutResult
            {
                IsSuccessful = false,
                ErrorMessage = "Payouts not supported by Behpardakht payment gateway"
            });
        }

        public Task<PayoutDetails> GetPayoutDetailsAsync(
            string payoutId,
            CancellationToken cancellationToken = default)
        {
            // Behpardakht doesn't support payouts
            _logger.LogWarning("Payouts not supported by Behpardakht");

            throw new NotSupportedException("Payouts not supported by Behpardakht payment gateway");
        }
    }
}
