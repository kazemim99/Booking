// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/ConfirmBooking/ConfirmBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.ConfirmBooking
{
    public sealed class ConfirmBookingCommandHandler : ICommandHandler<ConfirmBookingCommand, ConfirmBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IBookingReminderScheduler _reminders;
        private readonly INotificationRaiser _notifications;
        private readonly IBookingNotificationParameters _bookingParameters;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<ConfirmBookingCommandHandler> _logger;

        public ConfirmBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<ConfirmBookingCommandHandler> logger,
            IBookingReminderScheduler reminders,
            INotificationRaiser notifications,
            IBookingNotificationParameters bookingParameters)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _reminders = reminders;
            _notifications = notifications;
            _bookingParameters = bookingParameters;
        }

        public async Task<ConfirmBookingResult> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Confirming booking {BookingId}", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Process payment if payment intent provided
            if (!string.IsNullOrEmpty(request.PaymentIntentId))
            {
                booking.ProcessDepositPayment(request.PaymentIntentId);
            }

            // Confirm booking
            booking.Confirm();

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // The appointment is now real, so its reminders are scheduled. Raised on this unit of
            // work, so they commit with the confirmation or not at all.
            await _reminders.ScheduleAsync(booking, cancellationToken);

            // The salon accepted the customer's request, and the customer is the one who did not act — without
            // this they learn it only by reopening the booking (QA recording 2026-09-23: the booking turned
            // «تایید شده» while the inbox still held only «درخواست نوبت ثبت شد»). Only a request can be
            // confirmed, and a request is always the customer's own booking — a walk-in is born Confirmed — so
            // the aggregate's customer is the person to tell. Same unit of work: it commits with the confirmation.
            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.BookingConfirmed,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: await _bookingParameters.ForAsync(booking, cancellationToken: cancellationToken),
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Booking {BookingId} confirmed successfully", booking.Id);
            Telemetry.BookingMetrics.BookingConfirmed();

            return new ConfirmBookingResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                ConfirmedAt: booking.ConfirmedAt!.Value);
        }
    }
}
