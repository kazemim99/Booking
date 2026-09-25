// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.DomainServices;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using ProviderAggregate = AsanRezerve.ServiceCatalog.Domain.Aggregates.Provider;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Booking.GetAvailableSlots
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

            // Load provider (organization)
            var provider = await _providerRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId),
                cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            // Load every service in the visit (ServiceIds supersedes the
            // single ServiceId); slots must span their combined duration.
            var requestedServiceIds =
                (request.ServiceIds is { Count: > 0 }
                    ? request.ServiceIds
                    : new[] { request.ServiceId })
                .Distinct()
                .ToList();

            var services = new List<Domain.Aggregates.Service>();
            foreach (var id in requestedServiceIds)
            {
                var loaded = await _serviceRepository.GetByIdAsync(
                    ServiceId.From(id),
                    cancellationToken);

                if (loaded == null)
                    throw new NotFoundException($"Service with ID {id} not found");

                services.Add(loaded);
            }

            var service = services[0];
            var totalDuration = Duration.FromMinutes(
                services.Sum(x => x.Duration.Value));

            // The requested staff id is a BOOKABLE RESOURCE id — a MembershipId, or the
            // organization's own id for a solo direct booking. The availability engine
            // validates it against the organization's resources, so no provider lookup
            // (and no sub-provider assumption) is needed here.
            var requestedResourceId = request.StaffId;

            // Validate date-level constraints (not time-level since we're just selecting a date)
            var validationResult = await _availabilityService.ValidateDateConstraintsAsync(
                provider,
                service,
                request.Date,
                cancellationToken);

            // Get available slots - using hierarchy model
            var availableSlots = await _availabilityService.GetAvailableTimeSlotsAsync(
                provider,
                service,
                request.Date,
                requestedResourceId,
                durationOverride: totalDuration,
                cancellationToken: cancellationToken);

            // Map to DTOs
            var slotDtos = availableSlots
                .Select(slot => new TimeSlotDto(
                    slot.StartTime,
                    slot.EndTime,
                    slot.Duration.Value,
                    slot.StaffId,
                    slot.StaffName)
                {
                    IsAvailable = true,
                    AvailableStaffId = slot.StaffId,
                    AvailableStaffName = slot.StaffName
                })
                .ToList();

            _logger.LogInformation("Found {Count} available slots", slotDtos.Count);

            // Include validation messages if no slots are available
            List<string>? validationMessages = null;
            if (slotDtos.Count == 0)
            {
                if (!validationResult.IsValid)
                {
                    // Date-level validation failed
                    validationMessages = validationResult.Errors;
                    _logger.LogInformation(
                        "No slots available due to validation constraints: {ValidationErrors}",
                        string.Join(", ", validationMessages));
                }
                else
                {
                    // Validation passed but no slots were generated
                    // This happens when there's no qualified staff
                    validationMessages = new List<string>();

                    // The salon has no bookable slot for this service. Distinguishing "no
                    // staff at all" from "staff, but none qualified" used to mean counting
                    // sub-provider rows; a salon with no service-providing member is also
                    // bookable as itself (CanAcceptDirectBookings), so an empty result here
                    // is about qualification and open hours rather than headcount.
                    // Name the reason the provider can act on: a day shorter than the service used
                    // to be reported as "no qualified staff" (2026-09-19).
                    var dayHours = provider.BusinessHours.FirstOrDefault(
                        h => (int)h.DayOfWeek == (int)request.Date.DayOfWeek);
                    validationMessages.Add(EmptyDayReason.Describe(
                        dayHours?.OpenTime, dayHours?.CloseTime, totalDuration.Value));

                    _logger.LogInformation(
                        "No slots available: {Reason}",
                        string.Join(", ", validationMessages));
                }
            }

            return new GetAvailableSlotsResult(
                request.ProviderId,
                request.ServiceId,
                request.Date,
                slotDtos,
                validationMessages);
        }
    }
}
