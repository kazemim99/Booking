// ========================================
// Booksy.ServiceCatalog.Application/Commands/Payment/ProcessPayment/ProcessPaymentCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.ProcessPayment
{
    public sealed class ProcessPaymentCommandHandler : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResult>
    {
        private readonly IPaymentWriteRepository _paymentRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<ProcessPaymentCommandHandler> _logger;

        public ProcessPaymentCommandHandler(
            IPaymentWriteRepository paymentRepository,
            IPaymentGateway paymentGateway,
            ILogger<ProcessPaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
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
                    request.Description,
                    request.Metadata)
                : Domain.Aggregates.PaymentAggregate.Payment.CreateDirect(
                    UserId.From(request.CustomerId),
                    ProviderId.From(request.ProviderId),
                    amount,
                    request.Method,
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
            }

            // Save payment
            await _paymentRepository.AddAsync(payment, cancellationToken);

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
                payment.CreatedAt);
        }
    }
}
