// ========================================
// VerifyZarinPalPaymentCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Payment.VerifyZarinPalPayment
{
    public sealed class VerifyZarinPalPaymentCommandHandler
        : ICommandHandler<VerifyZarinPalPaymentCommand, VerifyZarinPalPaymentResult>
    {
        private readonly IPaymentWriteRepository _paymentRepository;
        private readonly IZarinPalService _zarinPalService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VerifyZarinPalPaymentCommandHandler> _logger;

        public VerifyZarinPalPaymentCommandHandler(
            IPaymentWriteRepository paymentRepository,
            IZarinPalService zarinPalService,
            IUnitOfWork unitOfWork,
            ILogger<VerifyZarinPalPaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _zarinPalService = zarinPalService ?? throw new ArgumentNullException(nameof(zarinPalService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VerifyZarinPalPaymentResult> Handle(
            VerifyZarinPalPaymentCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Verifying ZarinPal payment with authority: {Authority}", request.Authority);

            try
            {
                // Find payment by authority
                var payment = await _paymentRepository.GetByAuthorityAsync(request.Authority, cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for authority: {Authority}", request.Authority);
                    throw new InvalidOperationException($"Payment not found for authority: {request.Authority}");
                }

                // If status is NOK (user cancelled), mark as failed
                if (request.Status != "OK")
                {
                    payment.MarkPaymentRequestAsFailed("USER_CANCELLED", "User cancelled the payment");
                    await _unitOfWork.CommitAsync(cancellationToken);

                    _logger.LogInformation("Payment {PaymentId} cancelled by user", payment.Id.Value);

                    return new VerifyZarinPalPaymentResult(
                        payment.Id.Value,
                        payment.BookingId?.Value,
                        false,
                        PaymentStatus: "Failed",
                        FailureReason: "User cancelled the payment",
                        ErrorMessage: "User cancelled the payment");
                }

                // Verify payment with ZarinPal
                var verifyResult = await _zarinPalService.VerifyPaymentAsync(
                    request.Authority,
                    payment.Amount.Amount,
                    cancellationToken);

                if (verifyResult.IsSuccessful)
                {
                    // Verify payment in aggregate
                    payment.VerifyPayment(
                        verifyResult.RefId.ToString(),
                        verifyResult.CardPan,
                        (decimal?)verifyResult.Fee);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Payment {PaymentId} verified successfully. RefNumber: {RefNumber}",
                        payment.Id.Value, verifyResult.RefId);

                    return new VerifyZarinPalPaymentResult(
                        payment.Id.Value,
                        payment.BookingId?.Value,
                        true,
                        PaymentStatus: "Paid",
                        RefNumber: verifyResult.RefId,
                        CardPan: verifyResult.CardPan,
                        Fee: (decimal?)verifyResult.Fee);
                }
                else
                {
                    // Mark payment as failed
                    var failureReason = $"[{verifyResult.ErrorCode}] {verifyResult.ErrorMessage ?? "Verification failed"}";
                    payment.MarkPaymentRequestAsFailed(
                        verifyResult.ErrorCode.ToString(),
                        verifyResult.ErrorMessage ?? "Verification failed");

                    await _unitOfWork.CommitAsync(cancellationToken);

                    _logger.LogWarning(
                        "Payment {PaymentId} verification failed. Error: {ErrorCode} - {ErrorMessage}",
                        payment.Id.Value, verifyResult.ErrorCode, verifyResult.ErrorMessage);

                    return new VerifyZarinPalPaymentResult(
                        payment.Id.Value,
                        payment.BookingId?.Value,
                        false,
                        PaymentStatus: "Failed",
                        FailureReason: failureReason,
                        ErrorMessage: verifyResult.ErrorMessage,
                        ErrorCode: verifyResult.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception verifying ZarinPal payment");
                throw;
            }
        }
    }
}
