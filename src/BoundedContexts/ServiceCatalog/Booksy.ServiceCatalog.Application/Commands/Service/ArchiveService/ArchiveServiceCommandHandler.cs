//===========================================
// Commands/Service/ArchiveService/ArchiveServiceCommand.cs
//===========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;



namespace Booksy.ServiceCatalog.Application.Commands.Service.ArchiveService
{
    public sealed class ArchiveServiceCommandHandler : ICommandHandler<ArchiveServiceCommand, ArchiveServiceResult>
    {
        private readonly IServiceWriteRepository _serviceWriteRepository;
        private readonly IServiceReadRepository _serviceReadRepository;
        private readonly ILogger<ArchiveServiceCommandHandler> _logger;

        public ArchiveServiceCommandHandler(
            IServiceWriteRepository serviceWriteRepository,
            IServiceReadRepository serviceReadRepository,
            ILogger<ArchiveServiceCommandHandler> logger)
        {
            _serviceWriteRepository = serviceWriteRepository;
            _serviceReadRepository = serviceReadRepository;
            _logger = logger;
        }

        public async Task<ArchiveServiceResult> Handle(
            ArchiveServiceCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Archiving service: {ServiceId} with reason: {Reason}",
                request.ServiceId, request.Reason);

            var serviceId = ServiceId.From(request.ServiceId);
            var service = await _serviceReadRepository.GetByIdAsync(serviceId, cancellationToken);

            if (service == null)
                throw new InvalidServiceException("Service not found");

            service.Archive();

            await _serviceWriteRepository.UpdateServiceAsync(service, cancellationToken);

            _logger.LogInformation("Service archived successfully: {ServiceId}", service.Id);

            return new ArchiveServiceResult(
                ServiceId: service.Id.Value,
                Name: service.Name,
                ProviderId: service.ProviderId.Value,
                Reason: request.Reason);
        }
    }
}
