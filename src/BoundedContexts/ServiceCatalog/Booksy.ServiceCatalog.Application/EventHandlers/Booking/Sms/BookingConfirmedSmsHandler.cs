// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/Booking/Sms/BookingConfirmedSmsHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Services;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.Booking.Sms
{
    /// <summary>
    /// Sends SMS notification when a booking is confirmed
    /// </summary>
    public sealed class BookingConfirmedSmsHandler : IDomainEventHandler<BookingConfirmedEvent>
    {
        private readonly ISmsNotificationService _smsService;
        private readonly IBookingReadRepository _bookingRepository;
        private readonly ILogger<BookingConfirmedSmsHandler> _logger;

        public BookingConfirmedSmsHandler(
            ISmsNotificationService smsService,
            IBookingReadRepository bookingRepository,
            ILogger<BookingConfirmedSmsHandler> logger)
        {
            _smsService = smsService;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task HandleAsync(BookingConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending confirmation SMS for booking {BookingId}", domainEvent.BookingId);

                // TODO: Fetch customer phone number, provider name, and service name from repositories
                // For now, using placeholder logic
                var phoneNumber = "09123456789"; // TODO: Get from customer
                var customerName = "Customer"; // TODO: Get from customer
                var providerName = "Provider"; // TODO: Get from provider
                var serviceName = "Service"; // TODO: Get from service

                await _smsService.SendBookingConfirmedSmsAsync(
                    phoneNumber,
                    customerName,
                    domainEvent.StartTime,
                    providerName,
                    serviceName,
                    cancellationToken);

                _logger.LogInformation("Confirmation SMS sent successfully for booking {BookingId}", domainEvent.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending confirmation SMS for booking {BookingId}", domainEvent.BookingId);
                // Don't throw - SMS failures shouldn't fail the booking process
            }
        }
    }
}
