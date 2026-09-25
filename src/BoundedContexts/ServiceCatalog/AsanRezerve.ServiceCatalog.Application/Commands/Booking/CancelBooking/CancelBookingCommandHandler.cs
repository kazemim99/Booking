using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.CancelBooking
{
    public sealed class CancelBookingCommandHandler : ICommandHandler<CancelBookingCommand, CancelBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IBookingReminderScheduler _reminders;
        private readonly INotificationRaiser _notifications;
        private readonly Domain.Repositories.IProviderReadRepository _providers;
        private readonly IProviderAvailabilityWriteRepository _availabilityWriteRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<CancelBookingCommandHandler> _logger;

        private readonly IBookingNotificationParameters _bookingParameters;
        private readonly Promotions.IPromotionPricingService _promotions;

        public CancelBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IProviderAvailabilityWriteRepository availabilityWriteRepository,
            IPaymentGateway paymentGateway,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<CancelBookingCommandHandler> logger,
            IBookingReminderScheduler reminders,
            INotificationRaiser notifications,
            Domain.Repositories.IProviderReadRepository providers,
            IBookingNotificationParameters bookingParameters,
            Promotions.IPromotionPricingService promotions)
        {
            _promotions = promotions;
            _bookingParameters = bookingParameters;
            _bookingRepository = bookingRepository;
            _availabilityWriteRepository = availabilityWriteRepository;
            _paymentGateway = paymentGateway;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _reminders = reminders;
            _notifications = notifications;
            _providers = providers;
        }

        public async Task<CancelBookingResult> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cancelling booking {BookingId}. Reason: {Reason}", request.BookingId, request.Reason);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException("این نوبت پیدا نشد.");

            // Captured BEFORE the transition, because both a rejection and a cancellation land on Cancelled
            // and only the starting state tells them apart: a request the salon never accepted was never an
            // appointment, so calling it "cancelled" tells the customer they lost something they never had.
            var wasAwaitingApproval = booking.Status == Domain.Enums.BookingStatus.Requested;

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

            // Process refund per the booking policy.
            bool refundIssued = false;
            decimal refundAmount = 0;

            var canCancelWithoutFee = booking.Policy.CanCancelWithoutFee(
                booking.TimeSlot.StartTime,
                DateTime.UtcNow);

            if (booking.PaymentInfo.IsDepositPaid())
            {
                Money refundMoney;
                if (canCancelWithoutFee || request.ByProvider)
                {
                    // Inside the free window (or provider-initiated): refund everything paid.
                    refundMoney = Money.Create(
                        booking.PaymentInfo.PaidAmount.Amount,
                        booking.PaymentInfo.PaidAmount.Currency);
                }
                else
                {
                    // Past the free window: charge the cancellation fee and refund the remainder.
                    // A CancellationFeePercentage means the customer pays that fee, not that they
                    // forfeit the whole deposit. Refund = paid − fee, floored at zero.
                    var fee = booking.Policy.CalculateCancellationFee(
                        Money.Create(booking.TotalPrice.Amount, booking.TotalPrice.Currency));
                    var net = booking.PaymentInfo.PaidAmount.Amount - fee.Amount;
                    refundMoney = Money.Create(
                        net > 0 ? net : 0m,
                        booking.PaymentInfo.PaidAmount.Currency);
                }

                if (refundMoney.Amount > 0)
                {
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
            }

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // A cancelled booking gives its discount's use back to the promotion (decision 2026-09-25), committed with
            // the cancellation below. Customer or salon, it makes no difference.
            await _promotions.ReleaseForBookingAsync(booking.Id.Value, DateTime.UtcNow, cancellationToken);

            // The appointment is off, so its unsent reminders must not go out.
            await _reminders.WithdrawAsync(booking.Id.Value, cancellationToken);

            // Tell the party who did NOT cancel. Whether the salon cancelled is decided from the
            // authenticated caller, not from a request field a client could set for itself.
            var cancellingProvider = await _providers.GetByIdAsync(booking.ProviderId, cancellationToken);
            var salonCancelled = cancellingProvider is not null
                                 && cancellingProvider.OwnerId.Value == request.ActingUserId;

            var cancelParameters = await _bookingParameters.ForAsync(
                booking, cancellingProvider?.Profile.BusinessName, cancellationToken);
            cancelParameters[NotificationParameter.Reason] = request.Reason;

            if (salonCancelled)
            {
                await _notifications.RaiseAsync(
                    wasAwaitingApproval
                        ? Domain.Enums.NotificationEventCode.BookingRejected
                        : Domain.Enums.NotificationEventCode.BookingCancelledByProvider,
                    booking.CustomerId.Value,
                    dedupKey: booking.Id.Value,
                    parameters: cancelParameters,
                    subjectType: BookingReminderScheduler.BookingSubject,
                    subjectId: booking.Id.Value,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // Their own action, echoed back as a receipt — and the salon, whose day now has a hole.
                await _notifications.RaiseAsync(
                    Domain.Enums.NotificationEventCode.BookingCancelledAck,
                    booking.CustomerId.Value,
                    dedupKey: booking.Id.Value,
                    parameters: cancelParameters,
                    subjectType: BookingReminderScheduler.BookingSubject,
                    subjectId: booking.Id.Value,
                    cancellationToken: cancellationToken);

                if (cancellingProvider is not null)
                {
                    await _notifications.RaiseAsync(
                        Domain.Enums.NotificationEventCode.BookingCancelledByCustomer,
                        cancellingProvider.OwnerId.Value,
                        dedupKey: booking.Id.Value,
                        parameters: cancelParameters,
                        subjectType: BookingReminderScheduler.BookingSubject,
                        subjectId: booking.Id.Value,
                        cancellationToken: cancellationToken);
                }
            }


            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} cancelled successfully. Refund issued: {RefundIssued}",
                booking.Id, refundIssued);
            Telemetry.BookingMetrics.BookingCancelled();

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

            // Deliberately NOT staff-scoped. This handler has no resolver, and the only staff
            // key to hand is Booking.StaffId -- which is the MEMBERSHIP id for a member
            // booking but the ORGANIZATION id for one taken against the salon directly, and
            // the latter matches no slot's StaffId at all. Passing it would silently release
            // nothing for every direct booking. The release is exact regardless, because the
            // loop below only touches slots already carrying this booking's own BookingId.
            var overlappingSlots = await _availabilityWriteRepository.FindOverlappingSlotsAsync(
                providerId,
                date.ToDateTime(TimeOnly.MinValue),
                startTimeOnly,
                endTimeOnly,
                excludeSlotId: null,
                staffId: null,
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
