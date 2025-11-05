// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/IntegrationEventHandlers/BookingCompletedIntegrationEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.IntegrationEventHandlers
{
    // Note: This assumes a BookingCompletedIntegrationEvent from the Booking context
    public sealed class BookingCompletedIntegrationEventHandler : IIntegrationEventHandler<BookingCompletedIntegrationEvent>
    {
        private readonly IServiceApplicationService _serviceApplicationService;
        private readonly Domain.Repositories.IPaymentReadRepository _paymentRepository;
        private readonly ILogger<BookingCompletedIntegrationEventHandler> _logger;

        public BookingCompletedIntegrationEventHandler(
            IServiceApplicationService serviceApplicationService,
            Domain.Repositories.IPaymentReadRepository paymentRepository,
            ILogger<BookingCompletedIntegrationEventHandler> logger)
        {
            _serviceApplicationService = serviceApplicationService;
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _logger = logger;
        }

        public async Task HandleAsync(BookingCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing booking completion: BookingId={BookingId}, ServiceId={ServiceId}",
                integrationEvent.BookingId,
                integrationEvent.ServiceId);

            // Update service statistics
            // - Increment booking count
            // - Update revenue tracking
            // - Calculate average rating if review included

            // Check payment status for payout eligibility
            var bookingId = Booksy.ServiceCatalog.Domain.ValueObjects.BookingId.From(integrationEvent.BookingId);
            var payments = await _paymentRepository.GetByBookingIdAsync(bookingId, cancellationToken);

            var paidPayment = payments.FirstOrDefault(p => p.Status == Domain.Enums.PaymentStatus.Paid);
            if (paidPayment != null)
            {
                _logger.LogInformation(
                    "Booking {BookingId} completed with paid payment {PaymentId}. Amount {Amount} {Currency} is now eligible for payout to provider {ProviderId}",
                    integrationEvent.BookingId,
                    paidPayment.Id.Value,
                    paidPayment.Amount.Amount,
                    paidPayment.Amount.Currency,
                    integrationEvent.ProviderId);

                // Note: Payment is automatically tracked for payout when booking is completed
                // Payout will be created via CreatePayoutCommand based on payout schedule
            }
            else
            {
                _logger.LogWarning(
                    "Booking {BookingId} completed but no paid payment found. Provider may not receive payout.",
                    integrationEvent.BookingId);
            }

            // This would typically involve updating read models or analytics data
            await Task.CompletedTask;

            _logger.LogInformation("Booking completion processed for service: {ServiceId}", integrationEvent.ServiceId);
        }
    }

    // Placeholder for integration event from Booking context
    public sealed record BookingCompletedIntegrationEvent(
        Guid BookingId,
        Guid ServiceId,
        Guid ProviderId,
        Guid ClientId,
        decimal Amount,
        string Currency,
        DateTime CompletedAt) : IntegrationEvent;
}