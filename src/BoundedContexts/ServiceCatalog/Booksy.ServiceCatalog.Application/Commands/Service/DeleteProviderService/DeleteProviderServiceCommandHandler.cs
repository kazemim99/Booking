using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.DeleteProviderService;

public sealed class DeleteProviderServiceCommandHandler : ICommandHandler<DeleteProviderServiceCommand, DeleteProviderServiceResult>
{
    private readonly IServiceWriteRepository _serviceWriteRepository;
    private readonly IBookingReadRepository _bookingReadRepository;
    private readonly ILogger<DeleteProviderServiceCommandHandler> _logger;

    public DeleteProviderServiceCommandHandler(
        IServiceWriteRepository serviceWriteRepository,
        IBookingReadRepository bookingReadRepository,
        ILogger<DeleteProviderServiceCommandHandler> logger)
    {
        _serviceWriteRepository = serviceWriteRepository;
        _bookingReadRepository = bookingReadRepository;
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
        var serviceId = ServiceId.From(request.ServiceId);
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

        // Block deletion while the service still has active (Requested/Confirmed) bookings —
        // deleting would orphan upcoming appointments. The provider should deactivate the
        // service (hide it from new bookings) and let the existing ones run their course.
        var serviceBookings = await _bookingReadRepository.GetByServiceIdAsync(serviceId, cancellationToken);
        var activeCount = serviceBookings.Count(b => b.Status.IsActive());
        if (activeCount > 0)
        {
            throw new DomainValidationException(
                $"Cannot delete service {request.ServiceId}: it has {activeCount} active booking(s). " +
                "Deactivate the service instead, or wait until those bookings are completed or cancelled.");
        }

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
