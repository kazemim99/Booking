// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/RefundPayment/RefundPaymentCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment
{
    public sealed class RefundPaymentCommandHandler : ICommandHandler<RefundPaymentCommand, RefundPaymentResult>
    {
        private readonly IPaymentWriteRepository _paymentRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<RefundPaymentCommandHandler> _logger;

        public RefundPaymentCommandHandler(
            IPaymentWriteRepository paymentRepository,
            IPaymentGateway paymentGateway,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<RefundPaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RefundPaymentResult> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing refund for payment {PaymentId}, amount {Amount}",
                request.PaymentId, request.RefundAmount);

            // Load payment
            var paymentId = PaymentId.From(request.PaymentId);
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);

            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} not found");
            }

            if (!payment.CanBeRefunded())
            {
                throw new InvalidOperationException($"Payment {request.PaymentId} cannot be refunded");
            }

            // Validate refund amount
            var refundableAmount = payment.GetRefundableAmount();
            if (request.RefundAmount > refundableAmount.Amount)
            {
                throw new InvalidOperationException(
                    $"Refund amount {request.RefundAmount} exceeds refundable amount {refundableAmount.Amount}");
            }

            // Process refund through gateway
            var gatewayPaymentId = payment.Method == Domain.Enums.PaymentMethod.ZarinPal
                ? payment.Authority ?? throw new InvalidOperationException("ZarinPal Authority is missing")
                : payment.PaymentIntentId ?? throw new InvalidOperationException("Payment intent ID is missing");

            var refundResult = await _paymentGateway.RefundPaymentAsync(
                gatewayPaymentId,
                request.RefundAmount,
                request.Reason.ToString(),
                cancellationToken);

            if (!refundResult.IsSuccessful)
            {
                _logger.LogWarning("Refund processing failed: {Error}", refundResult.ErrorMessage);
                throw new InvalidOperationException($"Refund failed: {refundResult.ErrorMessage}");
            }

            // Update payment aggregate
            var refundAmount = Money.Create(request.RefundAmount, payment.Amount.Currency);
            payment.Refund(refundAmount, refundResult.RefundId, request.Reason, request.Notes);

            // Persist the refund. This command is INonTransactionalCommand: the gateway refund above ran outside
            // any retried transaction, and this single CommitAsync is a retry-safe unit — so a transient DB fault
            // can never re-invoke the gateway and double-refund.
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Payment {PaymentId} refunded successfully, refund ID {RefundId}",
                payment.Id.Value, refundResult.RefundId);

            return new RefundPaymentResult(
                payment.Id.Value,
                payment.BookingId?.Value ?? Guid.Empty,
                payment.CustomerId.Value,
                payment.ProviderId.Value,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.Status.ToString(),
                payment.Method.ToString(),
                payment.PaymentIntentId,
                DateTime.UtcNow,
                payment.CreatedAt,
                true,
                null);
        }
    }
}
