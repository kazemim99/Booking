using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.DeleteProviderService;

public sealed class DeleteProviderServiceCommandHandler : ICommandHandler<DeleteProviderServiceCommand, DeleteProviderServiceResult>
{
    private readonly IServiceWriteRepository _serviceWriteRepository;
    private readonly IBookingReadRepository _bookingReadRepository;
    private readonly IProviderReadRepository _providerReadRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteProviderServiceCommandHandler> _logger;

    public DeleteProviderServiceCommandHandler(
        IServiceWriteRepository serviceWriteRepository,
        IBookingReadRepository bookingReadRepository,
        IProviderReadRepository providerReadRepository,
        ICurrentUserService currentUserService,
        ILogger<DeleteProviderServiceCommandHandler> logger)
    {
        _serviceWriteRepository = serviceWriteRepository;
        _bookingReadRepository = bookingReadRepository;
        _providerReadRepository = providerReadRepository;
        _currentUserService = currentUserService;
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
            throw new ForbiddenException("Service does not belong to this provider");
        }

        // The caller must own request.ProviderId (or be an admin) -- without this, any
        // authenticated Provider-role caller could pass someone else's providerId/serviceId
        // pair (the check above only proves the pair is internally consistent, not that the
        // caller has any right to it) and delete that provider's service. Confirmed reachable:
        // ServicesController's route for this command only requires the "ProviderOrAdmin"
        // policy (any provider), unlike ProviderSettingsController's equivalent route, which
        // separately checks CanManageProvider(id) before ever constructing this command.
        var isAdmin = _currentUserService.IsInRole("Admin")
            || _currentUserService.IsInRole("SysAdmin")
            || _currentUserService.IsInRole("Administrator");
        if (!isAdmin)
        {
            var callerId = UserId.From(_currentUserService.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated"));
            var provider = await _providerReadRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId), cancellationToken)
                ?? throw new KeyNotFoundException($"Provider with ID {request.ProviderId} not found");
            if (provider.OwnerId != callerId)
                throw new ForbiddenException("You are not authorized to manage this provider's services");
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
