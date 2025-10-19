using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.DeleteProviderService;

public sealed class DeleteProviderServiceCommandHandler : ICommandHandler<DeleteProviderServiceCommand, DeleteProviderServiceResult>
{
    private readonly IServiceWriteRepository _serviceWriteRepository;
    private readonly ILogger<DeleteProviderServiceCommandHandler> _logger;

    public DeleteProviderServiceCommandHandler(
        IServiceWriteRepository serviceWriteRepository,
        ILogger<DeleteProviderServiceCommandHandler> logger)
    {
        _serviceWriteRepository = serviceWriteRepository;
        _logger = logger;
    }

    public async Task<DeleteProviderServiceResult> Handle(
        DeleteProviderServiceCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deleting service {ServiceId} for provider {ProviderId}",
            request.ServiceId,
            request.ProviderId);

        // Get service
        var serviceId = ServiceId.Create(request.ServiceId);
        var service = await _serviceWriteRepository.GetByIdAsync(serviceId, cancellationToken);

        if (service == null)
        {
            throw new KeyNotFoundException($"Service with ID {request.ServiceId} not found");
        }

        // Verify service belongs to provider
        if (service.ProviderId.Value != request.ProviderId)
        {
            throw new UnauthorizedAccessException("Service does not belong to this provider");
        }

        // TODO: Check if service has active bookings before deletion
        // If there are active bookings, consider deactivating instead of deleting

        // Delete service
        await _serviceWriteRepository.DeleteServiceAsync(service, cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} deleted successfully",
            request.ServiceId);

        return new DeleteProviderServiceResult(
            request.ServiceId,
            true,
            "Service deleted successfully");
    }
}
