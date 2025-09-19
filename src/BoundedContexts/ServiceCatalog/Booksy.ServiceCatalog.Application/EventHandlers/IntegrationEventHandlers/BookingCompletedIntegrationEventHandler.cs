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
        private readonly ILogger<BookingCompletedIntegrationEventHandler> _logger;

        public BookingCompletedIntegrationEventHandler(
            IServiceApplicationService serviceApplicationService,
            ILogger<BookingCompletedIntegrationEventHandler> logger)
        {
            _serviceApplicationService = serviceApplicationService;
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