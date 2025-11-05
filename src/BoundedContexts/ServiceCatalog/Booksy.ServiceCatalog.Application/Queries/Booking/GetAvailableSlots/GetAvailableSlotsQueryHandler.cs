// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots
{
    public sealed class GetAvailableSlotsQueryHandler : IQueryHandler<GetAvailableSlotsQuery, GetAvailableSlotsResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly ILogger<GetAvailableSlotsQueryHandler> _logger;

        public GetAvailableSlotsQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            IAvailabilityService availabilityService,
            ILogger<GetAvailableSlotsQueryHandler> logger)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _availabilityService = availabilityService;
            _logger = logger;
        }

        public async Task<GetAvailableSlotsResult> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Getting available slots for Provider {ProviderId}, Service {ServiceId} on {Date}",
                request.ProviderId, request.ServiceId, request.Date);

            // Load provider
            var provider = await _providerRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId),
                cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            // Load service
            var service = await _serviceRepository.GetByIdAsync(
                ServiceId.Create(request.ServiceId),
                cancellationToken);

            if (service == null)
                throw new NotFoundException($"Service with ID {request.ServiceId} not found");

            // Get specific staff if requested
            Domain.Entities.Staff? staff = null;
            if (request.StaffId.HasValue)
            {
                staff = provider.Staff.FirstOrDefault(s => s.Id == request.StaffId.Value);
                if (staff == null)
                    throw new NotFoundException($"Staff member with ID {request.StaffId.Value} not found");
            }

            // Get available slots
            var availableSlots = await _availabilityService.GetAvailableTimeSlotsAsync(
                provider,
                service,
                request.Date,
                staff,
                cancellationToken);

            // Map to DTOs
            var slotDtos = availableSlots
                .Select(slot => new TimeSlotDto(
                    slot.StartTime,
                    slot.EndTime,
                    slot.Duration.Value,
                    slot.StaffId,
                    slot.StaffName))
                .ToList();

            _logger.LogInformation("Found {Count} available slots", slotDtos.Count);

            return new GetAvailableSlotsResult(
                request.ProviderId,
                request.ServiceId,
                request.Date,
                slotDtos);
        }
    }
}
