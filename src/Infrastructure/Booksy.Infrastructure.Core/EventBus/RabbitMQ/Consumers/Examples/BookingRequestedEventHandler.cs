// ========================================
// EventBus/RabbitMQ/Consumers/Examples/BookingRequestedEventHandler.cs
// ========================================
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.Core.EventBus.RabbitMQ.Consumers.Examples;

/// <summary>
/// Example event handler for BookingRequestedEvent
/// This demonstrates how to create a handler for domain events consumed from RabbitMQ
/// </summary>
public class BookingRequestedEventHandler : IDomainEventHandler<BookingRequestedEvent>
{
    private readonly ILogger<BookingRequestedEventHandler> _logger;

    public BookingRequestedEventHandler(ILogger<BookingRequestedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(BookingRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling BookingRequestedEvent - BookingId: {BookingId}, CustomerId: {CustomerId}, ProviderId: {ProviderId}, StartTime: {StartTime}",
            domainEvent.BookingId,
            domainEvent.CustomerId,
            domainEvent.ProviderId,
            domainEvent.StartTime);

        try
        {
            // Example: Send notification to customer
            _logger.LogInformation(
                "Sending booking confirmation notification to customer {CustomerId}",
                domainEvent.CustomerId);

            // Example: Update analytics or metrics
            _logger.LogInformation(
                "Recording booking metrics for provider {ProviderId}",
                domainEvent.ProviderId);

            // Example: Trigger other business processes
            _logger.LogInformation(
                "Triggering downstream processes for booking {BookingId}",
                domainEvent.BookingId);

            // Simulate async processing
            await Task.Delay(100, cancellationToken);

            _logger.LogInformation(
                "Successfully handled BookingRequestedEvent for booking {BookingId}",
                domainEvent.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling BookingRequestedEvent for booking {BookingId}",
                domainEvent.BookingId);
            throw;
        }
    }
}
