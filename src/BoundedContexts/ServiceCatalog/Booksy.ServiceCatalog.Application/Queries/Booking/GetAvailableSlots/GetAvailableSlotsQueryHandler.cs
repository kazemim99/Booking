// ========================================
// Booksy.ServiceCatalog.Application/Queries/Booking/GetAvailableSlots/GetAvailableSlotsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.DomainServices;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using ProviderAggregate = Booksy.ServiceCatalog.Domain.Aggregates.Provider;

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

            // Load provider (organization)
            var provider = await _providerRepository.GetByIdAsync(
                ProviderId.From(request.ProviderId),
                cancellationToken);

            if (provider == null)
                throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            // Load service
            var service = await _serviceRepository.GetByIdAsync(
                ServiceId.From(request.ServiceId),
                cancellationToken);

            if (service == null)
                throw new NotFoundException($"Service with ID {request.ServiceId} not found");

            // Get specific individual provider (staff) if requested - USING HIERARCHY
            ProviderAggregate? individualProvider = null;
            if (request.StaffId.HasValue)
            {
                var staffProviderId = ProviderId.From(request.StaffId.Value);

                // Load the individual provider
                individualProvider = await _providerRepository.GetByIdAsync(
                    staffProviderId,
                    cancellationToken);

                if (individualProvider == null)
                    throw new NotFoundException($"Individual provider with ID {request.StaffId.Value} not found");

                // Verify they belong to this organization
                if (individualProvider.ParentProviderId != provider.Id)
                    throw new NotFoundException(
                        $"Individual provider {request.StaffId.Value} does not belong to organization {request.ProviderId}");

                // Verify they are actually an individual (not an organization)
                if (individualProvider.HierarchyType != ProviderHierarchyType.Individual)
                    throw new NotFoundException(
                        $"Provider {request.StaffId.Value} is not an individual provider");
            }

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
                individualProvider,
                cancellationToken);

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

                    // Check if organization has staff (individual providers) - USING HIERARCHY
                    var staffCount = await _providerRepository.CountStaffByOrganizationAsync(
                        provider.Id,
                        cancellationToken);

                    if (staffCount == 0)
                    {
                        validationMessages.Add("این ارائه‌دهنده هنوز کارمندی اضافه نکرده است. لطفاً بعداً دوباره تلاش کنید.");
                    }
                    else
                    {
                        validationMessages.Add("متأسفانه هیچ کارمند واجد شرایطی برای این سرویس در دسترس نیست.");
                    }

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
