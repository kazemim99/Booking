// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Booking/Sms/BookingCreatedSmsHandler.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Booking.Sms
{
    /// <summary>
    /// Sends SMS notification when a booking is created
    /// </summary>
    public sealed class BookingCreatedSmsHandler : IDomainEventHandler<BookingRequestedEvent>
    {
        private readonly ISmsNotificationService _smsService;
        private readonly ILogger<BookingCreatedSmsHandler> _logger;

        public BookingCreatedSmsHandler(ISmsNotificationService smsService, ILogger<BookingCreatedSmsHandler> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        public async Task HandleAsync(BookingRequestedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending creation SMS for booking {BookingId}", notification.BookingId);

                // TODO: Fetch customer phone number, provider name, and service name
                var phoneNumber = "09123456789";
                var customerName = "Customer";
                var providerName = "Provider";
                var serviceName = "Service";

                await _smsService.SendBookingCreatedSmsAsync(
                    phoneNumber,
                    customerName,
                    notification.StartTime,
                    providerName,
                    serviceName,
                    cancellationToken);

                _logger.LogInformation("Creation SMS sent successfully for booking {BookingId}", notification.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending creation SMS for booking {BookingId}", notification.BookingId);
            }
        }
    }
}
