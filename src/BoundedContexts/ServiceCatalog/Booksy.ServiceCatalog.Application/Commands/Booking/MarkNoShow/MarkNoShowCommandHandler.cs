// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/MarkNoShow/MarkNoShowCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.MarkNoShow
{
    /// <summary>
    /// Handler for marking a booking as no-show
    /// </summary>
    public sealed class MarkNoShowCommandHandler : ICommandHandler<MarkNoShowCommand, MarkNoShowResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IBookingReminderScheduler _reminders;
        private readonly INotificationRaiser _notifications;
        private readonly Domain.Repositories.IProviderReadRepository _providers;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<MarkNoShowCommandHandler> _logger;

        public MarkNoShowCommandHandler(
            IBookingWriteRepository bookingRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<MarkNoShowCommandHandler> logger,
            IBookingReminderScheduler reminders,
            INotificationRaiser notifications,
            Domain.Repositories.IProviderReadRepository providers)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _reminders = reminders;
            _notifications = notifications;
            _providers = providers;
        }

        public async Task<MarkNoShowResult> Handle(MarkNoShowCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Marking booking {BookingId} as no-show", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Mark as no-show
            booking.MarkAsNoShow(request.Notes);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // The appointment is no longer going to happen, so its unsent reminders must not go out.
            await _reminders.WithdrawAsync(booking.Id.Value, cancellationToken);

            // Both sides: the customer so the record is not a silent mark against them, and the salon
            // because it is their loss to act on.
            var noShowProvider = await _providers.GetByIdAsync(booking.ProviderId, cancellationToken);
            var noShowParameters = new Dictionary<string, string>
            {
                [NotificationParameter.BusinessName] = noShowProvider?.Profile.BusinessName ?? "سالن",
                [NotificationParameter.StartTime] = booking.TimeSlot.StartTime.ToString("o"),
            };

            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.BookingNoShow,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: noShowParameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                cancellationToken: cancellationToken);

            if (noShowProvider is not null)
            {
                await _notifications.RaiseAsync(
                    Domain.Enums.NotificationEventCode.CustomerNoShow,
                    noShowProvider.OwnerId.Value,
                    dedupKey: booking.Id.Value,
                    parameters: noShowParameters,
                    subjectType: BookingReminderScheduler.BookingSubject,
                    subjectId: booking.Id.Value,
                    cancellationToken: cancellationToken);
            }


            // Commit transaction and publish events
            await _unitOfWork.CommitAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Booking {BookingId} marked as no-show successfully", booking.Id);
            Telemetry.BookingMetrics.BookingNoShow();

            return new MarkNoShowResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                MarkedAt: DateTime.UtcNow);
        }
    }
}
