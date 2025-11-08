// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Booking/Sms/BookingCompletedSmsHandler.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Booking.Sms
{
    /// <summary>
    /// Sends SMS notification when a booking is completed with review request
    /// </summary>
    public sealed class BookingCompletedSmsHandler : IDomainEventHandler<BookingCompletedEvent>
    {
        private readonly ISmsNotificationService _smsService;
        private readonly ILogger<BookingCompletedSmsHandler> _logger;

        public BookingCompletedSmsHandler(
            ISmsNotificationService smsService,
            ILogger<BookingCompletedSmsHandler> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        public async Task HandleAsync(BookingCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending completion SMS for booking {BookingId}", notification.BookingId);

                // TODO: Fetch customer phone number, name, provider name, and service name
                var phoneNumber = "09123456789";
                var customerName = "Customer";
                var providerName = "Provider";
                var serviceName = "Service";

                await _smsService.SendBookingCompletedSmsAsync(
                    phoneNumber,
                    customerName,
                    providerName,
                    serviceName,
                    cancellationToken);

                _logger.LogInformation("Completion SMS sent successfully for booking {BookingId}", notification.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending completion SMS for booking {BookingId}", notification.BookingId);
            }
        }
    }
}
