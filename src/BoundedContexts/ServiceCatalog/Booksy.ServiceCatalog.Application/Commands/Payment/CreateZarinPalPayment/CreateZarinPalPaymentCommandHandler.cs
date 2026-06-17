// ========================================
// CreateZarinPalPaymentCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.Enums;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.CreateZarinPalPayment
{
    public sealed class CreateZarinPalPaymentCommandHandler
        : ICommandHandler<CreateZarinPalPaymentCommand, CreateZarinPalPaymentResult>
    {
        private readonly IPaymentWriteRepository _paymentRepository;
        private readonly IZarinPalService _zarinPalService;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<CreateZarinPalPaymentCommandHandler> _logger;

        public CreateZarinPalPaymentCommandHandler(
            IPaymentWriteRepository paymentRepository,
            IZarinPalService zarinPalService,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<CreateZarinPalPaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _zarinPalService = zarinPalService ?? throw new ArgumentNullException(nameof(zarinPalService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreateZarinPalPaymentResult> Handle(
            CreateZarinPalPaymentCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Creating ZarinPal payment for customer {CustomerId}, amount {Amount} {Currency}",
                request.CustomerId, request.Amount, request.Currency);

            try
            {
                // Create Money value object
                var amount = Money.Create(request.Amount, request.Currency);

                // Create Payment aggregate
                var payment = request.BookingId.HasValue
                    ? Domain.Aggregates.PaymentAggregate.Payment.CreateForBooking(
                        BookingId.From(request.BookingId.Value),
                        UserId.From(request.CustomerId),
                        ProviderId.From(request.ProviderId),
                        amount,
                        PaymentMethod.ZarinPal,
                        PaymentProvider.ZarinPal,
                        request.Description,
                        request.Metadata)
                    : Domain.Aggregates.PaymentAggregate.Payment.CreateDirect(
                        UserId.From(request.CustomerId),
                        ProviderId.From(request.ProviderId),
                        amount,
                        PaymentMethod.ZarinPal,
                        PaymentProvider.ZarinPal,
                        request.Description,
                        request.Metadata);

                // Create payment request with ZarinPal
                var zarinPalResult = await _zarinPalService.CreatePaymentRequestAsync(
                    request.Amount,
                    request.Description ?? $"Payment for booking {request.BookingId}",
                    request.Mobile,
                    request.Email,
                    cancellationToken);

                if (zarinPalResult.IsSuccessful)
                {
                    // Record payment request in aggregate (capture the gateway fee if returned)
                    payment.RecordPaymentRequest(zarinPalResult.Authority, zarinPalResult.PaymentUrl, zarinPalResult.Fee);

                    // Save payment
                    await _paymentRepository.AddAsync(payment, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "ZarinPal payment created successfully. PaymentId: {PaymentId}, Authority: {Authority}",
                        payment.Id.Value, zarinPalResult.Authority);

                    return new CreateZarinPalPaymentResult(
                        payment.Id.Value,
                        zarinPalResult.Authority,
                        zarinPalResult.PaymentUrl,
                        request.Amount,
                        request.Currency,
                        true);
                }
                else
                {
                    // ZarinPal never issued an authority — nothing to track. Do not persist a
                    // half-formed payment; surface the gateway error to the caller instead.
                    _logger.LogWarning(
                        "ZarinPal payment request failed. Error: {ErrorCode} - {ErrorMessage}",
                        zarinPalResult.ErrorCode, zarinPalResult.ErrorMessage);

                    return new CreateZarinPalPaymentResult(
                        Guid.Empty,
                        string.Empty,
                        string.Empty,
                        request.Amount,
                        request.Currency,
                        false,
                        zarinPalResult.ErrorMessage,
                        zarinPalResult.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception creating ZarinPal payment");
                throw;
            }
        }
    }
}
