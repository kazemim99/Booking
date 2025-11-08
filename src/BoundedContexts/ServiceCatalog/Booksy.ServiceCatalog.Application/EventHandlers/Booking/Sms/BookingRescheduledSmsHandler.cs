// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Booking/Sms/BookingRescheduledSmsHandler.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
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

        public async Task HandleAsync(BookingRescheduledEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending reschedule SMS for booking {OldBookingId}", notification.OldBookingId);

                // TODO: Fetch customer phone number, name, and provider name
                var phoneNumber = "09123456789";
                var customerName = "Customer";
                var providerName = "Provider";

                await _smsService.SendBookingRescheduledSmsAsync(
                    phoneNumber,
                    customerName,
                    notification.OldStartTime,
                    notification.NewStartTime,
                    providerName,
                    cancellationToken);

                _logger.LogInformation("Reschedule SMS sent successfully for booking {OldBookingId}", notification.OldBookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reschedule SMS for booking {OldBookingId}", notification.OldBookingId);
            }
        }
    }
}
