// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Service/ActivateService/ActivateServiceCommandHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Application.Specifications.Service;
using AsanRezerve.ServiceCatalog.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Service.ActivateService
{
    public sealed class ActivateServiceCommandHandler : ICommandHandler<ActivateServiceCommand, ActivateServiceResult>
    {
        private readonly IServiceWriteRepository _serviceWriteRepository;
        private readonly IServiceReadRepository _serviceReadRepository;
        private readonly ILogger<ActivateServiceCommandHandler> _logger;

        public ActivateServiceCommandHandler(
            IServiceWriteRepository serviceWriteRepository,
            IServiceReadRepository serviceReadRepository,
            ILogger<ActivateServiceCommandHandler> logger)
        {
            _serviceWriteRepository = serviceWriteRepository;
            _serviceReadRepository = serviceReadRepository;
            _logger = logger;
        }

        public async Task<ActivateServiceResult> Handle(
            ActivateServiceCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Activating service: {ServiceId}", request.ServiceId);

            var serviceId = ServiceId.From(request.ServiceId);

            var spec = new ServiceGetByIdSpecification(serviceId);
            var service = await _serviceReadRepository.GetSingleAsync(spec, cancellationToken);


            if (service == null)
                throw new InvalidServiceException("Service not found");

            service.Activate();

            await _serviceWriteRepository.UpdateServiceAsync(service, cancellationToken);

            _logger.LogInformation("Service activated successfully: {ServiceId}", service.Id);

            return new ActivateServiceResult(
                ServiceId: service.Id.Value,
                Name: service.Name,
                ProviderId: service.ProviderId.Value,
                ActivatedAt: service.ActivatedAt!.Value);
        }
    }
}