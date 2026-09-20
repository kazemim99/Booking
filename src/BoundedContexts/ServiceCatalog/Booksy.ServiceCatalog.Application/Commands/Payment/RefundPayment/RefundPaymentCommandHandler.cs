// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/RefundPayment/RefundPaymentCommandHandler.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
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
        private readonly INotificationRaiser _notifications;
        private readonly ILogger<RefundPaymentCommandHandler> _logger;

        public RefundPaymentCommandHandler(
            IPaymentWriteRepository paymentRepository,
            IPaymentGateway paymentGateway,
            IServiceCatalogUnitOfWork unitOfWork,
            INotificationRaiser notifications,
            ILogger<RefundPaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RefundPaymentResult> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing refund for payment {PaymentId}, amount {Amount}",
                request.PaymentId, request.RefundAmount);

            // Load payment
            var paymentId = PaymentId.From(request.PaymentId);
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);

            // As in CapturePayment: an unknown payment is a 404 and an unrefundable one a 400.
            // InvalidOperationException is unmapped by ExceptionHandlingMiddleware, so both used to
            // answer 500 and told the caller nothing.
            if (payment == null)
            {
                throw new NotFoundException("Payment", request.PaymentId);
            }

            if (!payment.CanBeRefunded())
            {
                throw new DomainValidationException(
                    nameof(request.PaymentId), $"Payment {request.PaymentId} cannot be refunded");
            }

            // Validate refund amount
            var refundableAmount = payment.GetRefundableAmount();
            if (request.RefundAmount > refundableAmount.Amount)
            {
                throw new DomainValidationException(
                    nameof(request.RefundAmount),
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

            // The customer is entitled to this record — a refund they are never told about is, from their
            // side, indistinguishable from one that never happened. Recorded BEFORE the commit so the same
            // CommitAsync writes it: the money move and the notice of it land together or neither does.
            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.RefundProcessed,
                payment.CustomerId.Value,
                dedupKey: payment.Id.Value,
                parameters: new Dictionary<string, string>
                {
                    [NotificationParameter.Amount] = refundAmount.Amount.ToString("N0"),
                },
                subjectType: "Payment",
                subjectId: payment.Id.Value,
                cancellationToken: cancellationToken);
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
