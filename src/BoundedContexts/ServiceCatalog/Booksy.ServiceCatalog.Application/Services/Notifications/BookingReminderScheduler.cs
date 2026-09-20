using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;

namespace Booksy.ServiceCatalog.Application.Services.Notifications
{
    /// <summary>
    /// Schedules and withdraws the reminders attached to an appointment.
    /// </summary>
    public interface IBookingReminderScheduler
    {
        /// <summary>
        /// Schedules a booking's reminders. Safe to call again for the same booking — the outbox
        /// de-duplicates, so a re-confirmed booking does not get two sets.
        /// </summary>
        Task ScheduleAsync(Booking booking, CancellationToken cancellationToken = default);

        /// <summary>
        /// Withdraws every reminder not yet sent for a booking. Called when the appointment stops being a
        /// future event — cancelled, completed, or marked no-show.
        /// </summary>
        Task WithdrawAsync(Guid bookingId, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class BookingReminderScheduler : IBookingReminderScheduler
    {
        /// <summary>The subject type reminders are filed under, so they can be withdrawn together.</summary>
        public const string BookingSubject = "Booking";

        /// <summary>
        /// When each reminder goes out, relative to the appointment.
        /// </summary>
        /// <remarks>
        /// <para><b>These offsets are an assumption, not a settled product decision</b> (tasks.md 3.4). The
        /// two-hour one is the reminder that carries an SMS, so it sets a recurring per-booking cost; the
        /// others ride push and in-app and are effectively free. They are gathered here, in one list, so
        /// changing them is a one-line edit rather than a hunt through handlers.</para>
        ///
        /// <para>The provider's is deliberately much later than the customer's: the salon is looking at
        /// today's schedule, and telling them the day before adds noise to a screen they already read.</para>
        /// </remarks>
        private static readonly (NotificationEventCode Code, TimeSpan Before, bool ToCustomer)[] Offsets =
        {
            (NotificationEventCode.BookingReminder24h, TimeSpan.FromHours(24), true),
            (NotificationEventCode.BookingReminder2h, TimeSpan.FromHours(2), true),
            (NotificationEventCode.NextAppointmentReminder, TimeSpan.FromMinutes(30), false),
        };

        private readonly INotificationRaiser _raiser;
        private readonly IProviderReadRepository _providers;
        private readonly IServiceReadRepository _services;

        public BookingReminderScheduler(
            INotificationRaiser raiser,
            IProviderReadRepository providers,
            IServiceReadRepository services)
        {
            _raiser = raiser;
            _providers = providers;
            _services = services;
        }

        /// <remarks>
        /// The salon and service names are looked up here rather than passed in, so that every caller does
        /// not have to fetch them, and so the copy's inputs are decided in one place. They are captured into
        /// the intent now — the sweep must never re-read them later, because by then the service may have
        /// been renamed or deleted.
        /// </remarks>
        public async Task ScheduleAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(booking);

            var provider = await _providers.GetByIdAsync(booking.ProviderId, cancellationToken);
            var service = await _services.GetByIdAsync(booking.ServiceId, cancellationToken);

            var businessName = provider?.Profile.BusinessName ?? "سالن";
            var serviceName = service?.Name;
            string? customerName = null;

            var start = booking.TimeSlot.StartTime;
            var now = DateTime.UtcNow;

            var parameters = new Dictionary<string, string>
            {
                [NotificationParameter.BusinessName] = businessName,
                [NotificationParameter.StartTime] = start.ToString("o"),
            };

            if (!string.IsNullOrWhiteSpace(serviceName))
                parameters[NotificationParameter.ServiceName] = serviceName;

            if (!string.IsNullOrWhiteSpace(customerName))
                parameters[NotificationParameter.CustomerName] = customerName;

            foreach (var (code, before, toCustomer) in Offsets)
            {
                var due = start - before;

                // A booking made an hour beforehand must not immediately fire the reminder meant for the
                // day before. Skipping is the only sensible reading: the moment for that message has passed.
                if (due <= now)
                    continue;

                await _raiser.RaiseAsync(
                    code,
                    recipientId: toCustomer ? booking.CustomerId.Value : booking.ProviderId.Value,

                    // Keyed on the booking and the code, not on an event id: reminders are not caused by an
                    // event, and this is what makes re-confirming a booking idempotent.
                    dedupKey: booking.Id.Value,
                    parameters: parameters,
                    subjectType: BookingSubject,
                    subjectId: booking.Id.Value,
                    scheduledFor: due,
                    cancellationToken: cancellationToken);
            }
        }

        public Task WithdrawAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            _raiser.WithdrawPendingForSubjectAsync(BookingSubject, bookingId, cancellationToken);
    }
}
