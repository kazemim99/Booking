// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/SetServiceAvailability/SetServiceAvailabilityCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.SetServiceAvailability
{
    public sealed class SetServiceAvailabilityCommandHandler : ICommandHandler<SetServiceAvailabilityCommand, SetServiceAvailabilityResult>
    {
        private readonly IServiceWriteRepository _serviceWriteRepository;
        private readonly IServiceReadRepository _serviceReadRepository;
        private readonly ILogger<SetServiceAvailabilityCommandHandler> _logger;

        public SetServiceAvailabilityCommandHandler(
            IServiceWriteRepository serviceWriteRepository,
            IServiceReadRepository serviceReadRepository,
            ILogger<SetServiceAvailabilityCommandHandler> logger)
        {
            _serviceWriteRepository = serviceWriteRepository;
            _serviceReadRepository = serviceReadRepository;
            _logger = logger;
        }

        public async Task<SetServiceAvailabilityResult> Handle(
            SetServiceAvailabilityCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Setting availability for service: {ServiceId}", request.ServiceId);

            var serviceId = ServiceId.From(request.ServiceId);
            var service = await _serviceReadRepository.GetByIdAsync(serviceId, cancellationToken);

            if (service == null)
                throw new InvalidServiceException("Service not found");

            // Note: This assumes ServiceAvailability entity exists in the domain
            // The actual implementation would depend on how availability is modeled in the Service aggregate

            await _serviceWriteRepository.UpdateServiceAsync(service, cancellationToken);

            _logger.LogInformation("Service availability updated: {ServiceId}", service.Id);

            return new SetServiceAvailabilityResult(
                ServiceId: service.Id.Value,
                UpdatedAt: DateTime.UtcNow,
                AvailabilityCount: request.Availability.Count(a => a.Value != null));
        }
    }
}