// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/UpdateService/UpdateServiceCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.UpdateService
{
    public sealed class UpdateServiceCommandHandler : ICommandHandler<UpdateServiceCommand, UpdateServiceResult>
    {
        private readonly IServiceWriteRepository _serviceWriteRepository;
        private readonly IServiceReadRepository _serviceReadRepository;
        private readonly ILogger<UpdateServiceCommandHandler> _logger;

        public UpdateServiceCommandHandler(
            IServiceWriteRepository serviceWriteRepository,
            IServiceReadRepository serviceReadRepository,
            ILogger<UpdateServiceCommandHandler> logger)
        {
            _serviceWriteRepository = serviceWriteRepository;
            _serviceReadRepository = serviceReadRepository;
            _logger = logger;
        }

        public async Task<UpdateServiceResult> Handle(
            UpdateServiceCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating service: {ServiceId}", request.ServiceId);

            var serviceId = ServiceId.Create(request.ServiceId);
            var service = await _serviceReadRepository.GetByIdAsync(serviceId, cancellationToken);

            if (service == null)
                throw new InvalidServiceException("Service not found");

            var category = ServiceCategory.Create(request.CategoryName);
            var duration = Duration.FromMinutes(request.DurationMinutes);
            var preparationTime = request.PreparationMinutes.HasValue
                ? Duration.FromMinutes(request.PreparationMinutes.Value)
                : null;
            var bufferTime = request.BufferMinutes.HasValue
                ? Duration.FromMinutes(request.BufferMinutes.Value)
                : null;

            service.UpdateBasicInfo(request.Name, request.Description, category);
            service.UpdateDuration(duration, preparationTime, bufferTime);

            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                service.SetImage(request.ImageUrl);
            }

        

            await _serviceWriteRepository.UpdateServiceAsync(service, cancellationToken);

            _logger.LogInformation("Service updated successfully: {ServiceId}", service.Id);

            return new UpdateServiceResult(
                ServiceId: service.Id.Value,
                Name: service.Name,
                Description: service.Description,
                UpdatedAt: DateTime.UtcNow);
        }
    }
}