// ========================================
// IDPayPaymentGateway.cs
// ========================================
using Booksy.Infrastructure.External.Payment.IDPay.Models;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Payment.IDPay
{
    /// <summary>
    /// IDPay payment gateway adapter implementing IPaymentGateway
    /// </summary>
    public class IDPayPaymentGateway : IPaymentGateway
    {
        private readonly IIDPayService _idPayService;
        private readonly ILogger<IDPayPaymentGateway> _logger;

        public IDPayPaymentGateway(
            IIDPayService idPayService,
            ILogger<IDPayPaymentGateway> logger)
        {
            _idPayService = idPayService;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(
            PaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // For IDPay, ProcessPayment creates a payment request
                // The actual payment happens on IDPay's website
                var orderId = Guid.NewGuid().ToString();
                var result = await _idPayService.CreatePaymentRequestAsync(
                    request.Amount,
                    orderId,
                    request.Description,
                    request.Metadata?.GetValueOrDefault("name")?.ToString(),
                    request.Metadata?.GetValueOrDefault("mobile")?.ToString(),
                    request.Metadata?.GetValueOrDefault("email")?.ToString(),
                    cancellationToken);

                return new PaymentResult
                {
                    IsSuccessful = result.IsSuccessful,
                    PaymentId = result.PaymentId ?? string.Empty,
                    Status = result.IsSuccessful ? "pending_redirect" : "failed",
                    ErrorMessage = result.ErrorMessage,
                    ErrorCode = result.ErrorCode.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IDPay payment processing failed");
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
            // IDPay doesn't support automated refunds through their API
            // Refunds would need to be handled manually through their dashboard
            _logger.LogWarning("Automated refunds not supported by IDPay");

            return await Task.FromResult(new RefundResult
            {
                IsSuccessful = false,
                ErrorMessage = "Automated refunds not supported by IDPay payment gateway. Please process refunds manually through IDPay dashboard."
            });
        }

        public async Task<PaymentDetails> GetPaymentDetailsAsync(
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            // IDPay doesn't provide a direct API to get payment details by payment ID
            // This would typically be retrieved from the database
            _logger.LogWarning("GetPaymentDetailsAsync not directly supported by IDPay API");

            return await Task.FromResult(new PaymentDetails
            {
                PaymentId = paymentId,
                Status = "unknown",
                Amount = 0,
                Currency = "IRR",
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(
            decimal amount,
            string currency,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var orderId = Guid.NewGuid().ToString();
                var result = await _idPayService.CreatePaymentRequestAsync(
                    amount,
                    orderId,
                    metadata?.GetValueOrDefault("description")?.ToString() ?? "Payment",
                    metadata?.GetValueOrDefault("name")?.ToString(),
                    metadata?.GetValueOrDefault("mobile")?.ToString(),
                    metadata?.GetValueOrDefault("email")?.ToString(),
                    cancellationToken);

                return new PaymentIntent
                {
                    Id = result.PaymentId ?? string.Empty,
                    ClientSecret = result.PaymentUrl ?? string.Empty, // Use PaymentUrl as ClientSecret for redirect
                    Amount = amount,
                    Currency = currency,
                    Status = result.IsSuccessful ? "requires_action" : "failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IDPay payment intent creation failed");
                throw;
            }
        }

        public async Task<PaymentResult> ConfirmPaymentIntentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            // For IDPay, confirmation happens via callback from their gateway
            // This method would be called internally after verification
            _logger.LogWarning("ConfirmPaymentIntentAsync requires external verification via callback");

            return await Task.FromResult(new PaymentResult
            {
                IsSuccessful = false,
                PaymentId = paymentIntentId,
                Status = "requires_verification",
                ErrorMessage = "Payment must be verified via IDPay callback"
            });
        }

        public Task<PayoutResult> CreatePayoutAsync(
            PayoutRequest request,
            CancellationToken cancellationToken = default)
        {
            // IDPay doesn't support payouts through their API
            // Payouts would need to be handled separately
            _logger.LogWarning("Payouts not supported by IDPay");

            return Task.FromResult(new PayoutResult
            {
                IsSuccessful = false,
                ErrorMessage = "Payouts not supported by IDPay payment gateway"
            });
        }

        public Task<PayoutDetails> GetPayoutDetailsAsync(
            string payoutId,
            CancellationToken cancellationToken = default)
        {
            // IDPay doesn't support payouts
            _logger.LogWarning("Payouts not supported by IDPay");

            throw new NotSupportedException("Payouts not supported by IDPay payment gateway");
        }
    }
}
