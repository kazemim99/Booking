// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Booking/Sms/BookingCancelledSmsHandler.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Booking.Sms
{
    /// <summary>
    /// Sends SMS notification when a booking is cancelled
    /// </summary>
    public sealed class BookingCancelledSmsHandler : INotificationHandler<BookingCancelledEvent>
    {
        private readonly ISmsNotificationService _smsService;
        private readonly ILogger<BookingCancelledSmsHandler> _logger;

        public BookingCancelledSmsHandler(
            ISmsNotificationService smsService,
            ILogger<BookingCancelledSmsHandler> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        public async Task Handle(BookingCancelledEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Sending cancellation SMS for booking {BookingId}", notification.BookingId);

                // TODO: Fetch customer phone number and name
                var phoneNumber = "09123456789";
                var customerName = "Customer";

                await _smsService.SendBookingCancelledSmsAsync(
                    phoneNumber,
                    customerName,
                    notification.CancelledAt,
                    notification.Reason,
                    cancellationToken);

                _logger.LogInformation("Cancellation SMS sent successfully for booking {BookingId}", notification.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending cancellation SMS for booking {BookingId}", notification.BookingId);
            }
        }
    }
}
