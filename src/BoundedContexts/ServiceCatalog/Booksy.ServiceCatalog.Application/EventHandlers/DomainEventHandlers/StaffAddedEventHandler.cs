// ========================================
// Booksy.ServiceCatalog.Application/EventHandlers/DomainEventHandlers/StaffAddedEventHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.Events;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.ServiceCatalog.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.EventHandlers.DomainEventHandlers
{
    public sealed class StaffAddedEventHandler : IDomainEventHandler<StaffAddedEvent>
    {
        private readonly ILogger<StaffAddedEventHandler> _logger;

        public StaffAddedEventHandler(ILogger<StaffAddedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(StaffAddedEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Staff member added to provider: {ProviderId}, StaffId: {StaffId}, Role: {Role}",
                domainEvent.ProviderId,
                domainEvent.StaffId,
                domainEvent.Role);

            // Business logic for new staff
            // - Send welcome email to new staff member
            // - Create user account if needed
            // - Set up permissions based on role
            // - Update capacity calculations

            await Task.CompletedTask;

            _logger.LogInformation("Staff addition processed: {StaffId}", domainEvent.StaffId);
        }
    }
}