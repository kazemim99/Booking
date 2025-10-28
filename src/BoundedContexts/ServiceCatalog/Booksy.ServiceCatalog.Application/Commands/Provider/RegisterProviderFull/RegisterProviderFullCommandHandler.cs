// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProviderFull/RegisterProviderFullCommandHandler.cs
// Complete provider registration with all multi-step data
// ========================================

using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull
{
    public sealed class RegisterProviderFullCommandHandler : ICommandHandler<RegisterProviderFullCommand, RegisterProviderFullResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IProviderRegistrationService _registrationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RegisterProviderFullCommandHandler> _logger;

        public RegisterProviderFullCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            IProviderRegistrationService registrationService,
            IUnitOfWork unitOfWork,
            ILogger<RegisterProviderFullCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
            _registrationService = registrationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RegisterProviderFullResult> Handle(
            RegisterProviderFullCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Starting full provider registration for owner: {OwnerId}, Business: {BusinessName}",
                request.OwnerId,
                request.BusinessInfo.BusinessName);

            // ====================================
            // STEP 1: Validation
            // ====================================

            // Check if owner already has a provider
            var ownerId = UserId.From(request.OwnerId);
            var existingProvider = await _providerReadRepository.GetByOwnerIdAsync(ownerId, cancellationToken);
            if (existingProvider != null)
            {
                throw new InvalidOperationException(
                    $"User {request.OwnerId} already has a registered provider with ID {existingProvider.Id}");
            }

            // Validate business rules
            ValidateBusinessRules(request);

            // ====================================
            // STEP 2: Create Value Objects
            // ====================================

            var email = !string.IsNullOrEmpty(request.BusinessInfo.PhoneNumber)
                ? Email.Create($"{request.BusinessInfo.PhoneNumber}@temp.booksy.com") // Temporary email
                : null;

            var primaryPhone = PhoneNumber.Create(request.BusinessInfo.PhoneNumber);

            var contactInfo = ContactInfo.Create(
                email,
                primaryPhone,
                null, // Secondary phone
                null); // Website

            // Build full street address
            var fullStreet = string.IsNullOrWhiteSpace(request.Address.AddressLine2)
                ? request.Address.AddressLine1
                : $"{request.Address.AddressLine1}, {request.Address.AddressLine2}";

            var address = BusinessAddress.Create(
                fullStreet,
                request.Address.City,
                "", // State - not provided in frontend
                request.Address.ZipCode,
                "Iran", // Default country
                request.Location?.Latitude,
                request.Location?.Longitude);

            // Determine provider type from category
            var providerType = MapCategoryToProviderType(request.CategoryId);

            // ====================================
            // STEP 3: Create Provider Aggregate
            // ====================================

            var provider = ServiceCatalog.Domain.Aggregates.Provider.RegisterProvider(
                ownerId,
                request.BusinessInfo.BusinessName,
                $"Professional {request.CategoryId.Replace('_', ' ')} services", // Auto-generate description
                providerType,
                contactInfo,
                address);

            // ====================================
            // STEP 4: Add Business Hours
            // ====================================

            var hours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();

            foreach (var (dayOfWeekInt, dayHours) in request.BusinessHours)
            {
                var day = (DayOfWeek)dayOfWeekInt;

                if (dayHours?.IsOpen == true && dayHours.OpenTime != null && dayHours.CloseTime != null)
                {
                    var open = new TimeOnly(dayHours.OpenTime.Hours, dayHours.OpenTime.Minutes);
                    var close = new TimeOnly(dayHours.CloseTime.Hours, dayHours.CloseTime.Minutes);
                    hours[day] = (open, close);

                    _logger.LogDebug(
                        "Set business hours for {DayOfWeek}: {OpenTime} - {CloseTime}",
                        day,
                        open,
                        close);
                }
                else
                {
                    hours[day] = (null, null);
                    _logger.LogDebug("Set {DayOfWeek} as closed", day);
                }
            }

            provider.SetBusinessHours(hours);

            // ====================================
            // STEP 5: Save Provider First
            // ====================================
            // Provider must be saved before creating services (ProviderId FK constraint)

            //await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

            //_logger.LogInformation("Provider {ProviderId} created successfully", provider.Id);

            // ====================================
            // STEP 6: Create Services (Separate Aggregates)
            // ====================================
            // Services are separate aggregates linked by ProviderId

            var servicesCreated = 0;
            foreach (var serviceDto in request.Services)
            {
                var totalMinutes = (serviceDto.DurationHours * 60) + serviceDto.DurationMinutes;
                var duration = Duration.FromMinutes(totalMinutes);
                var price = Price.Create(serviceDto.Price, "IRR");

                var service = Domain.Aggregates.Service.Create(
                    provider.Id,
                    serviceDto.Name,
                    serviceDto.Name, // Use name as description
                    ServiceCategory.Beauty, // Default category - you can create from frontend
                    ServiceType.Standard, // Default type
                    price,
                    duration);

                // Save service separately (it's an aggregate root)
                // Note: You'll need a service repository for this
                // For now, services will be added via a separate endpoint or service

                servicesCreated++;

                _logger.LogDebug(
                    "Service defined: {ServiceName}, Price: {Price}, Duration: {Duration}",
                    service.Name,
                    service.BasePrice,
                    service.Duration);
            }

            // ====================================
            // STEP 7: Add Team Members (Staff)
            // ====================================

            var staffAdded = 0;
            if (request.TeamMembers.Count > 0)
            {
                foreach (var memberDto in request.TeamMembers)
                {
                    if (memberDto.IsOwner)
                        continue; // Owner is already set

                    var staffPhone = PhoneNumber.Create(memberDto.PhoneNumber);
                    var staffEmail = Email.Create(memberDto.Email);

                    // Parse staff name
                    var nameParts = memberDto.Name.Split(' ', 2);
                    var firstName = nameParts[0];
                    var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                    // Determine role
                    var role = DetermineStaffRole(memberDto.Position);

                    provider.AddStaff(firstName, lastName, role, staffPhone);

                    staffAdded++;

                    _logger.LogDebug(
                        "Added staff member: {Name}, Position: {Position}, Role: {Role}",
                        memberDto.Name,
                        memberDto.Position,
                        role);
                }
            }

            // ====================================
            // STEP 7.5: Add Provider to DbContext
            // ====================================
            // CRITICAL: Provider must be added to ChangeTracker so domain events can be dispatched

            _logger.LogDebug("Adding provider {ProviderId} to DbContext with {EventCount} domain events",
                provider.Id,
                provider.DomainEvents.Count());

            await _providerWriteRepository.SaveProviderAsync(provider, cancellationToken);

            _logger.LogDebug("Provider {ProviderId} added to DbContext (EntityState marked for insert)", provider.Id);

            // ====================================
            // STEP 8: Persist Changes & Dispatch Events
            // ====================================
            // This will:
            // 1. Dispatch domain events (ProviderRegisteredEvent)
            // 2. Persist changes to database
            // 3. Clear domain events from aggregates

            _logger.LogInformation("Committing transaction and dispatching domain events for provider {ProviderId}", provider.Id);

                var savedCount = await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Provider {ProviderId} fully registered. Services: {ServiceCount}, Staff: {StaffCount}, Status: {Status}, Changes saved: {SavedCount}",
                provider.Id,
                servicesCreated,
                staffAdded,
                provider.Status,
                savedCount);

            // ====================================
            // STEP 9: Return Result
            // ====================================

            return new RegisterProviderFullResult(
                ProviderId: provider.Id.Value,
                BusinessName: provider.Profile.BusinessName,
                Status: provider.Status,
                RegisteredAt: provider.RegisteredAt,
                ServicesCount: servicesCreated,
                StaffCount: staffAdded);
        }

        // ====================================
        // Private Helper Methods
        // ====================================

        private void ValidateBusinessRules(RegisterProviderFullCommand request)
        {
            var validationErrors = new Dictionary<string, List<string>>();

            // Validate at least one service
            if (request.Services == null || request.Services.Count == 0)
            {
                validationErrors["services"] = new List<string> { "At least one service is required" };
            }

            // Validate at least one working day
            var hasWorkingDay = request.BusinessHours.Values.Any(h => h != null && h.IsOpen);
            if (!hasWorkingDay)
            {
                validationErrors["businessHours"] = new List<string> { "At least one working day is required" };
            }

            // Validate business hours logic
            foreach (var (dayOfWeek, hours) in request.BusinessHours)
            {
                if (hours == null || !hours.IsOpen)
                    continue;

                if (hours.OpenTime == null || hours.CloseTime == null)
                {
                    var field = $"businessHours[{dayOfWeek}]";
                    if (!validationErrors.ContainsKey(field))
                        validationErrors[field] = new List<string>();
                    validationErrors[field].Add("Day is marked as open but missing open/close times");
                    continue;
                }

                // Validate open time is before close time
                var openMinutes = hours.OpenTime.Hours * 60 + hours.OpenTime.Minutes;
                var closeMinutes = hours.CloseTime.Hours * 60 + hours.CloseTime.Minutes;

                if (openMinutes >= closeMinutes)
                {
                    var field = $"businessHours[{dayOfWeek}]";
                    if (!validationErrors.ContainsKey(field))
                        validationErrors[field] = new List<string>();
                    validationErrors[field].Add("Opening time must be before closing time");
                }
            }

            // Validate service durations and prices
            for (int i = 0; i < (request.Services?.Count ?? 0); i++)
            {
                var service = request.Services![i];
                var totalMinutes = (service.DurationHours * 60) + service.DurationMinutes;

                if (totalMinutes <= 0)
                {
                    var field = $"services[{i}].duration";
                    if (!validationErrors.ContainsKey(field))
                        validationErrors[field] = new List<string>();
                    validationErrors[field].Add($"Service '{service.Name}' must have a duration greater than 0");
                }

                if (service.Price < 0)
                {
                    var field = $"services[{i}].price";
                    if (!validationErrors.ContainsKey(field))
                        validationErrors[field] = new List<string>();
                    validationErrors[field].Add($"Service '{service.Name}' price cannot be negative");
                }

                if (string.IsNullOrWhiteSpace(service.Name))
                {
                    var field = $"services[{i}].name";
                    if (!validationErrors.ContainsKey(field))
                        validationErrors[field] = new List<string>();
                    validationErrors[field].Add("Service name is required");
                }
            }

            // Throw validation exception if any errors found
            if (validationErrors.Any())
            {
                throw new ValidationException(validationErrors);
            }

            _logger.LogDebug("Business rules validation passed");
        }

        private ProviderType MapCategoryToProviderType(string categoryId)
        {
            return categoryId.ToLowerInvariant() switch
            {
                "nail_salon" => ProviderType.Salon,
                "hair_salon" => ProviderType.Salon,
                "brows_lashes" => ProviderType.Salon,
                "braids_locs" => ProviderType.Salon,
                "massage" => ProviderType.Spa,
                "barbershop" => ProviderType.Salon,
                "aesthetic_medicine" => ProviderType.Medical,
                "dental_orthodontics" => ProviderType.Medical,
                "hair_removal" => ProviderType.Spa,
                "health_fitness" => ProviderType.GymFitness,
                "home_services" => ProviderType.HomeServices,
                _ => ProviderType.Salon // Default
            };
        }

        private StaffRole DetermineStaffRole(string position)
        {
            var pos = position.ToLowerInvariant();

            if (pos.Contains("manager") || pos.Contains("owner"))
                return StaffRole.Manager;
            if (pos.Contains("receptionist") || pos.Contains("front desk"))
                return StaffRole.Receptionist;
            if (pos.Contains("stylist") || pos.Contains("technician") || pos.Contains("specialist"))
                return StaffRole.Specialist;
            if (pos.Contains("assistant"))
                return StaffRole.Assistant;
            if (pos.Contains("cleaner") || pos.Contains("cleaning"))
                return StaffRole.Cleaner;

            return StaffRole.ServiceProvider; // Default
        }
    }
}
