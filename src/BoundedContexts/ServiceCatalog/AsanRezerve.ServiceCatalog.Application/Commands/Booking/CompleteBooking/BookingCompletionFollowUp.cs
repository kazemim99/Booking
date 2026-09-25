using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Booking.CompleteBooking
{
    /// <summary>
    /// What follows a visit becoming done, however it became done — the salon marking it, or the booking completing by
    /// itself 12 hours after its end (openspec/changes/_inline/reviews-and-reschedule-round2 D3): its unsent reminders
    /// withdrawn, the thank-you, and the review request and its one follow-up. One place, so the two ways to
    /// completion cannot drift apart.
    /// </summary>
    public sealed class BookingCompletionFollowUp
    {
        private readonly IBookingReminderScheduler _reminders;
        private readonly INotificationRaiser _notifications;
        private readonly IProviderReadRepository _providers;
        private readonly IBookingNotificationParameters _bookingParameters;

        public BookingCompletionFollowUp(
            IBookingReminderScheduler reminders,
            INotificationRaiser notifications,
            IProviderReadRepository providers,
            IBookingNotificationParameters bookingParameters)
        {
            _reminders = reminders;
            _notifications = notifications;
            _providers = providers;
            _bookingParameters = bookingParameters;
        }

        public async Task RaiseAsync(
            Domain.Aggregates.BookingAggregate.Booking booking, DateTime utcNow, CancellationToken cancellationToken)
        {
            // The appointment is no longer going to happen, so its unsent reminders must not go out.
            await _reminders.WithdrawAsync(booking.Id.Value, cancellationToken);

            // Thank the customer, and ask for a review LATER — asking the moment somebody walks out of the
            // salon is worse than not asking. The 3-day follow-up is withdrawn the moment a review is
            // submitted (tasks 7.6), including one still sitting in moderation.
            var completedProvider = await _providers.GetByIdAsync(booking.ProviderId, cancellationToken);
            var completionParameters = await _bookingParameters.ForAsync(
                booking, completedProvider?.Profile.BusinessName, cancellationToken);

            await _notifications.RaiseAsync(
                NotificationEventCode.BookingCompleted,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: completionParameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                cancellationToken: cancellationToken);

            await _notifications.RaiseAsync(
                NotificationEventCode.ReviewRequest,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: completionParameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                scheduledFor: utcNow.AddHours(2),
                cancellationToken: cancellationToken);

            // One follow-up, three days later, and never a third. It is withdrawn the moment a review is
            // submitted — see CreateReviewCommandHandler — so it only ever reaches somebody who did not
            // answer. Its own code rather than a second ReviewRequest: the wording differs, and the outbox
            // de-duplicates on (key, code, recipient), so a repeat under the same code would vanish anyway.
            await _notifications.RaiseAsync(
                NotificationEventCode.ReviewReminder,
                booking.CustomerId.Value,
                dedupKey: booking.Id.Value,
                parameters: completionParameters,
                subjectType: BookingReminderScheduler.BookingSubject,
                subjectId: booking.Id.Value,
                scheduledFor: utcNow.AddDays(3),
                cancellationToken: cancellationToken);
        }
    }
}
