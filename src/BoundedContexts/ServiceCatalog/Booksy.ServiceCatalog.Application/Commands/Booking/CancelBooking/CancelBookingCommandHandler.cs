using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CancelBooking
{
    public sealed class CancelBookingCommandHandler : ICommandHandler<CancelBookingCommand, CancelBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IProviderAvailabilityWriteRepository _availabilityWriteRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CancelBookingCommandHandler> _logger;

        public CancelBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IProviderAvailabilityWriteRepository availabilityWriteRepository,
            IPaymentGateway paymentGateway,
            IUnitOfWork unitOfWork,
            ILogger<CancelBookingCommandHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _availabilityWriteRepository = availabilityWriteRepository;
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

            // ⚡ ATOMIC AVAILABILITY SLOT RELEASE (prevent orphaned booked slots)
            // Release the cancelled booking's availability slots back to Available status
            await ReleaseAvailabilitySlotsAsync(
                booking.ProviderId,
                booking.TimeSlot.StartTime,
                booking.TimeSlot.EndTime,
                booking.Id.Value,
                cancellationToken);

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

        /// <summary>
        /// Release availability slots back to Available status when booking is cancelled
        /// Prevents orphaned "Booked" slots that block future bookings
        /// </summary>
        private async Task ReleaseAvailabilitySlotsAsync(
            ProviderId providerId,
            DateTime startTime,
            DateTime endTime,
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            var date = DateOnly.FromDateTime(startTime);
            var startTimeOnly = TimeOnly.FromDateTime(startTime);
            var endTimeOnly = TimeOnly.FromDateTime(endTime);

            // Find all slots that overlap with the cancelled booking time range
            var overlappingSlots = await _availabilityWriteRepository.FindOverlappingSlotsAsync(
                providerId,
                date.ToDateTime(TimeOnly.MinValue),
                startTimeOnly,
                endTimeOnly,
                null,
                cancellationToken);

            foreach (var slot in overlappingSlots)
            {
                // Only release slots that are booked for THIS specific booking
                if (slot.Status == Domain.Enums.AvailabilityStatus.Booked &&
                    slot.BookingId == bookingId)
                {
                    _logger.LogDebug(
                        "Releasing availability slot {SlotId} for cancelled booking {BookingId}",
                        slot.Id, bookingId);

                    slot.Release("CancelBookingCommandHandler");
                    await _availabilityWriteRepository.UpdateAsync(slot, cancellationToken);
                }
            }

            _logger.LogInformation(
                "Released {SlotCount} availability slots for cancelled booking {BookingId}",
                overlappingSlots.Count, bookingId);
        }
    }
}
