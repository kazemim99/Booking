// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/IntegrationEventHandlers/BookingConfirmedPaymentIntegrationEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.IntegrationEventHandlers
{
    /// <summary>
    /// Handles booking confirmation events to check payment status
    /// </summary>
    public sealed class BookingConfirmedPaymentIntegrationEventHandler : IIntegrationEventHandler<BookingConfirmedIntegrationEvent>
    {
        private readonly IPaymentReadRepository _paymentRepository;
        private readonly ILogger<BookingConfirmedPaymentIntegrationEventHandler> _logger;

        public BookingConfirmedPaymentIntegrationEventHandler(
            IPaymentReadRepository paymentRepository,
            ILogger<BookingConfirmedPaymentIntegrationEventHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(BookingConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing booking confirmation for payment: BookingId={BookingId}, Amount={Amount}",
                integrationEvent.BookingId,
                integrationEvent.TotalAmount);

            // Check if payment already exists for this booking
            var bookingId = Core.Domain.ValueObjects.BookingId.From(integrationEvent.BookingId);
            var payments = await _paymentRepository.GetByBookingIdAsync(bookingId, cancellationToken);

            if (payments.Any())
            {
                _logger.LogInformation(
                    "Payment already exists for booking {BookingId}. Payment status: {Status}",
                    integrationEvent.BookingId,
                    payments.First().Status);
            }
            else
            {
                _logger.LogWarning(
                    "Booking {BookingId} confirmed but no payment found. Payment may need to be processed.",
                    integrationEvent.BookingId);
            }

            // Note: Actual payment processing should be triggered by the client/API
            // This handler is for monitoring and validation purposes
            // In a production system, you might want to:
            // 1. Verify payment status matches booking status
            // 2. Send notifications if payment is missing
            // 3. Update analytics/reporting

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Integration event published when a booking is confirmed
    /// </summary>
    public sealed record BookingConfirmedIntegrationEvent(
        Guid BookingId,
        Guid CustomerId,
        Guid ProviderId,
        Guid ServiceId,
        decimal TotalAmount,
        string Currency,
        DateTime ConfirmedAt) : IntegrationEvent;
}
