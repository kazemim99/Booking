// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/CapturePayment/CapturePaymentCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.CapturePayment
{
    /// <summary>
    /// Handler for capturing previously authorized payments
    /// </summary>
    public sealed class CapturePaymentCommandHandler : ICommandHandler<CapturePaymentCommand, CapturePaymentResult>
    {
        private readonly IPaymentWriteRepository _paymentRepository;
        private readonly ILogger<CapturePaymentCommandHandler> _logger;

        public CapturePaymentCommandHandler(
            IPaymentWriteRepository paymentRepository,
            ILogger<CapturePaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CapturePaymentResult> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Capturing payment {PaymentId}, amount: {Amount}",
                request.PaymentId, request.AmountToCapture?.ToString() ?? "full");

            // Load payment
            var paymentId = PaymentId.From(request.PaymentId);
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);

            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} not found");
            }

            // Validate payment state
            if (payment.AuthorizedAt == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} has not been authorized");
            }

            if (payment.CapturedAt != null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} has already been captured");
            }

            // Capture the payment in domain
            // Note: Stripe payment intents are captured automatically when created with Confirm=true
            // This handler is for manual capture scenarios or when authorization was done separately
            payment.Capture(payment.PaymentIntentId);

            // Update in repository (TransactionBehaviour will save)
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            _logger.LogInformation("Payment {PaymentId} captured successfully, status: {Status}",
                payment.Id.Value, payment.Status);

            return new CapturePaymentResult(
                payment.Id.Value,
                payment.BookingId?.Value ?? Guid.Empty,
                payment.CustomerId.Value,
                payment.ProviderId.Value,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.Status.ToString(),
                payment.Method.ToString(),
                payment.PaymentIntentId,
                payment.CapturedAt!.Value,
                payment.CreatedAt,
                true,
                null);
        }
    }
}
