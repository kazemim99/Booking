// ========================================
// Booksy.ServiceCatalog.Application/Commands/Booking/CompleteBooking/CompleteBookingCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    public sealed class CompleteBookingCommandHandler : ICommandHandler<CompleteBookingCommand, CompleteBookingResult>
    {
        private readonly IBookingWriteRepository _bookingRepository;
        private readonly IBookingReminderScheduler _reminders;
        private readonly INotificationRaiser _notifications;
        private readonly Domain.Repositories.IProviderReadRepository _providers;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly ILogger<CompleteBookingCommandHandler> _logger;

        public CompleteBookingCommandHandler(
            IBookingWriteRepository bookingRepository,
            IServiceCatalogUnitOfWork unitOfWork,
            ILogger<CompleteBookingCommandHandler> logger,
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

        public async Task<CompleteBookingResult> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Completing booking {BookingId}", request.BookingId);

            // Load booking
            var booking = await _bookingRepository.GetByIdAsync(
                BookingId.From(request.BookingId),
                cancellationToken);

            if (booking == null)
                throw new NotFoundException($"Booking with ID {request.BookingId} not found");

            // Complete booking
            booking.Complete(request.StaffNotes);

            // Update booking
            await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);

            // The appointment is no longer going to happen, so its unsent reminders must not go out.
            await _reminders.WithdrawAsync(booking.Id.Value, cancellationToken);

            // Thank the customer, and ask for a review LATER — asking the moment somebody walks out of the
            // salon is worse than not asking. The 3-day follow-up is withdrawn the moment a review is
            // submitted (tasks 7.6), including one still sitting in moderation.
            var completedProvider = await _providers.GetByIdAsync(booking.ProviderId, cancellationToken);
            var completionParameters = new Dictionary<string, string>
            {
                [NotificationParameter.BusinessName] = completedProvider?.Profile.BusinessName ?? "سالن",
                [NotificationParameter.StartTime] = booking.TimeSlot.StartTime.ToString("o"),
            };

            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.BookingCompleted,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: completionParameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                cancellationToken: cancellationToken);

            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.ReviewRequest,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: completionParameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                scheduledFor: DateTime.UtcNow.AddHours(2),
                cancellationToken: cancellationToken);

            // One follow-up, three days later, and never a third. It is withdrawn the moment a review is
            // submitted — see CreateReviewCommandHandler — so it only ever reaches somebody who did not
            // answer. Its own code rather than a second ReviewRequest: the wording differs, and the outbox
            // de-duplicates on (key, code, recipient), so a repeat under the same code would vanish anyway.
            await _notifications.RaiseAsync(
                Domain.Enums.NotificationEventCode.ReviewReminder,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: completionParameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                scheduledFor: DateTime.UtcNow.AddDays(3),
                cancellationToken: cancellationToken);



            _logger.LogInformation("Booking {BookingId} completed successfully", booking.Id);
            Telemetry.BookingMetrics.BookingCompleted();

            return new CompleteBookingResult(
                BookingId: booking.Id.Value,
                Status: booking.Status.ToString(),
                CompletedAt: booking.CompletedAt!.Value);
        }
    }
}
