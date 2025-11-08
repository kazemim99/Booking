// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Booking/Sms/BookingRescheduledSmsHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Booking.Sms
{
    /// <summary>
    /// Sends SMS notification when a booking is rescheduled
    /// </summary>
    public sealed class BookingRescheduledSmsHandler : IDomainEventHandler<BookingRescheduledEvent>
    {
        private readonly ISmsNotificationService _smsService;
        private readonly ILogger<BookingRescheduledSmsHandler> _logger;

        public BookingRescheduledSmsHandler(
            ISmsNotificationService smsService,
            ILogger<BookingRescheduledSmsHandler> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        public async Task HandleAsync(BookingRescheduledEvent domainEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending reschedule SMS for booking {OldBookingId}", domainEvent.OldBookingId);

                // TODO: Fetch customer phone number, name, and provider name
                var phoneNumber = "09123456789";
                var customerName = "Customer";
                var providerName = "Provider";

                await _smsService.SendBookingRescheduledSmsAsync(
                    phoneNumber,
                    customerName,
                    domainEvent.OldStartTime,
                    domainEvent.NewStartTime,
                    providerName,
                    cancellationToken);

                _logger.LogInformation("Reschedule SMS sent successfully for booking {OldBookingId}", domainEvent.OldBookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reschedule SMS for booking {OldBookingId}", domainEvent.OldBookingId);
            }
        }
    }
}
