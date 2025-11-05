// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CancelBooking/CancelBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.External.Payment;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CancelBooking
{
    public sealed class CancelBookingCommandHandler : ICommandHandler<CancelBookingCommand, CancelBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CancelBookingCommandHandler> _logger;

        public CancelBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IPaymentGateway paymentGateway,
            IUnitOfWork unitOfWork,
            ILogger<CancelBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _paymentGateway = paymentGateway;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CancelBookingResult> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cancelling booking {BookingId}. Reason: {Reason}", request.BookingId, request.Reason);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Cancel booking (business logic handles fee calculation)
            booking.Cancel(request.Reason, request.ByProvider);

            // Process refund if applicable
            bool refundIssued = false;
            decimal refundAmount = 0;

            var canCancelWithoutFee = booking.Policy.CanCancelWithoutFee(
                booking.TimeSlot.StartTime,
                DateTime.UtcNow);

            if ((canCancelWithoutFee || request.ByProvider) && booking.PaymentInfo.IsDepositPaid())
            {
                // Issue full refund
                var refundMoney = Money.Create(
                    booking.PaymentInfo.PaidAmount.Amount,
                    booking.PaymentInfo.PaidAmount.Currency);

                try
                {
                    var refundResult = await _paymentGateway.RefundPaymentAsync(
                        booking.PaymentInfo.PaymentIntentId!,
                        refundMoney.Amount,
                        request.Reason,
                        cancellationToken);

                    if (refundResult.IsSuccessful)
                    {
                        booking.ProcessRefund(refundMoney, refundResult.RefundId, request.Reason);
                        refundIssued = true;
                        refundAmount = refundMoney.Amount;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process refund for booking {BookingId}", booking.Id);
                    // Continue with cancellation even if refund fails
                }
            }

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} cancelled successfully. Refund issued: {RefundIssued}",
                booking.Id, refundIssued);

            return new CancelBookingResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                RefundIssued: refundIssued,
                RefundAmount: refundAmount,
                CancelledAt: booking.CancelledAt!.Value);
        }
    }
}
