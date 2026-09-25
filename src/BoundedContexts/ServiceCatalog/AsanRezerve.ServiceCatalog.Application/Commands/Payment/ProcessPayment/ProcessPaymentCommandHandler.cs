// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Payment/ProcessPayment/ProcessPaymentCommandHandler.cs
// ========================================
using System.Globalization;
using AsanRezerve.Core.Application.Abstractions;
using AsanRezerve.Core.Domain.Enums;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Infrastructure.External.Payment;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Payment.ProcessPayment
{
    public sealed class ProcessPaymentCommandHandler : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResult>
    {
        /// <summary>What a payment notification is about, for the inbox's tap target.</summary>
        private const string PaymentSubject = "Payment";

        private readonly IPaymentWriteRepository _paymentRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly INotificationRaiser _notifications;
        private readonly IProviderReadRepository _providers;
        private readonly ILogger<ProcessPaymentCommandHandler> _logger;

        public ProcessPaymentCommandHandler(
            IPaymentWriteRepository paymentRepository,
            IPaymentGateway paymentGateway,
            IServiceCatalogUnitOfWork unitOfWork,
            INotificationRaiser notifications,
            IProviderReadRepository providers,
            ILogger<ProcessPaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProcessPaymentResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing payment for customer {CustomerId}, amount {Amount} {Currency}",
                request.CustomerId, request.Amount, request.Currency);

            // Create Money value object
            var amount = Money.Create(request.Amount, request.Currency);

            // Create Payment aggregate
            var payment = request.BookingId.HasValue
                ? Domain.Aggregates.PaymentAggregate.Payment.CreateForBooking(
                    BookingId.From(request.BookingId.Value),
                    UserId.From(request.CustomerId),
                    ProviderId.From(request.ProviderId),
                    amount,
                    request.Method,
                    PaymentProvider.ZarinPal, // Default provider
                    request.Description,
                    request.Metadata)
                : Domain.Aggregates.PaymentAggregate.Payment.CreateDirect(
                    UserId.From(request.CustomerId),
                    ProviderId.From(request.ProviderId),
                    amount,
                    request.Method,
                    PaymentProvider.ZarinPal, // Default provider
                    request.Description,
                    request.Metadata);

            // Process payment through gateway
            var paymentRequest = new PaymentRequest
            {
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethodId = request.PaymentMethodId,
                CustomerId = request.CustomerId.ToString(),
                Description = request.Description ?? $"Payment for booking {request.BookingId}",
                Metadata = request.Metadata
            };

            var result = await _paymentGateway.ProcessPaymentAsync(paymentRequest, cancellationToken);

            if (result.IsSuccessful)
            {
                // Mark payment as successful
                payment.ProcessCharge(result.PaymentId, request.PaymentMethodId);
                _logger.LogInformation("Payment {PaymentId} processed successfully via gateway", result.PaymentId);
            }
            else
            {
                // Mark payment as failed
                payment.MarkAsFailed(result.ErrorMessage ?? "Unknown error");
                _logger.LogWarning("Payment processing failed: {Error}", result.ErrorMessage);

                // Told before the commit below, so the failed payment and the notice of it are written by
                // the same CommitAsync. A customer whose card was declined and who is never told simply
                // believes they have paid — which is the worst of the three outcomes available here.
                var salon = await _providers.GetByIdAsync(
                    ProviderId.From(request.ProviderId), cancellationToken);

                await _notifications.RaiseAsync(
                    Domain.Enums.NotificationEventCode.PaymentFailed,
                    recipientId: request.CustomerId,
                    dedupKey: payment.Id.Value,
                    parameters: new Dictionary<string, string>
                    {
                        [NotificationParameter.BusinessName] = salon?.Profile.BusinessName ?? "سالن",
                        [NotificationParameter.Amount] = request.Amount.ToString("0.##", CultureInfo.InvariantCulture),
                        [NotificationParameter.Reason] = result.ErrorMessage ?? string.Empty,
                    },
                    subjectType: PaymentSubject,
                    subjectId: payment.Id.Value,
                    cancellationToken: cancellationToken);
            }

            // Persist. This command is INonTransactionalCommand (money-moving): the gateway charge above ran
            // outside any retried transaction, and this single CommitAsync is a retry-safe unit — a transient DB
            // fault can never re-invoke the gateway and double-charge.
            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Payment {PaymentId} saved with status {Status}",
                payment.Id.Value, payment.Status);

            return new ProcessPaymentResult(
                payment.Id.Value,
                request.CustomerId,
                request.ProviderId,
                request.Amount,
                request.Currency,
                payment.Status.ToString(),
                payment.PaymentIntentId,
                payment.CreatedAt,
                result.IsSuccessful,
                result.ErrorMessage);
        }
    }
}
