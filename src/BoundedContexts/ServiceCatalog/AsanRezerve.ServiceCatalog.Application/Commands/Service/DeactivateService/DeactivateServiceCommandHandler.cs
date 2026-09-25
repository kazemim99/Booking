using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;


namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.DeactivateService
{
    public sealed class DeactivateServiceCommandHandler : ICommandHandler<DeactivateServiceCommand, DeactivateServiceResult>
    {
        private readonly IServiceWriteRepository _serviceWriteRepository;
        private readonly IServiceReadRepository _serviceReadRepository;
        private readonly ILogger<DeactivateServiceCommandHandler> _logger;

        public DeactivateServiceCommandHandler(
            IServiceWriteRepository serviceWriteRepository,
            IServiceReadRepository serviceReadRepository,
            ILogger<DeactivateServiceCommandHandler> logger)
        {
            _serviceWriteRepository = serviceWriteRepository;
            _serviceReadRepository = serviceReadRepository;
            _logger = logger;
        }

        public async Task<DeactivateServiceResult> Handle(
            DeactivateServiceCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deactivating service: {ServiceId} with reason: {Reason}",
                request.ServiceId, request.Reason);

            var serviceId = ServiceId.From(request.ServiceId);
            var service = await _serviceReadRepository.GetByIdAsync(serviceId, cancellationToken);

            if (service == null)
                throw new InvalidServiceException("Service not found");

            service.Deactivate(request.Reason);

            await _serviceWriteRepository.UpdateServiceAsync(service, cancellationToken);

            _logger.LogInformation("Service deactivated successfully: {ServiceId}", service.Id);

            return new DeactivateServiceResult(
                ServiceId: service.Id.Value,
                Name: service.Name,
                ProviderId: service.ProviderId.Value,
                Reason: request.Reason);
        }
    }
}
