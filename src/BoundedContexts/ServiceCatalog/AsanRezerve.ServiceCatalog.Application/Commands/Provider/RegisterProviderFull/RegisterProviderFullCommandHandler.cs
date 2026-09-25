// ========================================
// AsanRezerve.ServiceCatalog.Application/Commands/Provider/RegisterProviderFull/RegisterProviderFullCommandHandler.cs
// Complete provider registration with all multi-step data
// ========================================

using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Entities;
using AsanRezerve.ServiceCatalog.Application.Common;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.RegisterProviderFull
{
    public sealed class RegisterProviderFullCommandHandler : ICommandHandler<RegisterProviderFullCommand, RegisterProviderFullResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly IServiceWriteRepository _serviceWriteRepository;
        private readonly IProviderRegistrationService _registrationService;
        private readonly IOrganizationMembershipRepository _membershipRepository;
        private readonly IMembershipAuditRepository _auditRepository;
        private readonly IPersonDirectory _personDirectory;
        private readonly IMemberBookabilityService _memberBookability;
        private readonly IServiceCatalogUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterProviderFullCommandHandler> _logger;

        public RegisterProviderFullCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            IServiceWriteRepository serviceWriteRepository,
            IProviderRegistrationService registrationService,
            IOrganizationMembershipRepository membershipRepository,
            IMembershipAuditRepository auditRepository,
            IPersonDirectory personDirectory,
            IMemberBookabilityService memberBookability,
            IServiceCatalogUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<RegisterProviderFullCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
            _serviceWriteRepository = serviceWriteRepository;
            _registrationService = registrationService;
            _membershipRepository = membershipRepository;
            _auditRepository = auditRepository;
            _personDirectory = personDirectory;
            _memberBookability = memberBookability;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
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
                ? Email.Create($"{request.BusinessInfo.PhoneNumber}@temp.asanrezerve.com") // Temporary email
                : null;

            var primaryPhone = PhoneNumber.From(request.BusinessInfo.PhoneNumber);

            var contactInfo = ContactInfo.Create(
                email,
                primaryPhone,
                null, // Secondary phone
                null); // Website

            // Build full street address
            var fullStreet = string.IsNullOrWhiteSpace(request.Address.AddressLine2)
                ? request.Address.AddressLine1
                : $"{request.Address.AddressLine1}, {request.Address.AddressLine2}";

            var formattedAddress = $"{fullStreet}, {request.Address.City}";

            var address = BusinessAddress.Create(
                formattedAddress,
                fullStreet,
                request.Address.City,
                "", // State - not provided in frontend
                request.Address.ZipCode,
                "Iran", // Default country
                null, // ProvinceId
                null, // CityId
                request.Location?.Latitude,
                request.Location?.Longitude);

            // Determine service category from category ID
            var serviceCategory = MapCategoryToServiceCategory(request.CategoryId);

            // ====================================
            // STEP 3: Create Provider Aggregate
            // ====================================

            var provider = ServiceCatalog.Domain.Aggregates.Provider.RegisterProvider(
                ownerId,
                request.BusinessInfo.BusinessName,
                $"Professional {request.CategoryId.Replace('_', ' ')} services", // Auto-generate description
                serviceCategory,
                contactInfo,
                address,
                ownerFirstName: request.BusinessInfo.OwnerFirstName ?? string.Empty,
                ownerLastName: request.BusinessInfo.OwnerLastName ?? string.Empty);

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

            // Auto-approve on registration (MVP). When ServiceCatalog:AutoApproveProviders is false,
            // providers stay PendingVerification / services stay Draft for manual admin approval.
            var autoApprove = _configuration.GetValue("ServiceCatalog:AutoApproveProviders", true);
            if (autoApprove)
                provider.Activate();

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
                    ServiceCategory.BeautySalon, // Default category - you can create from frontend
                    ServiceType.Standard, // Default type
                    price,
                    duration);

                // Services stay Draft here — an Organization service can only be activated once it
                // has a qualified staff member, which happens via AddStaffToProvider.

                // Persist the service (separate aggregate root) via its write repository.
                await _serviceWriteRepository.SaveServiceAsync(service, cancellationToken);

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

            // Every person the salon works with is a MEMBERSHIP of this organization —
            // never a second Provider. Two shapes, matching AddStaffToProviderCommandHandler:
            //   • the phone identifies an existing person → membership linked to them
            //   • otherwise                               → an unclaimed membership carrying
            //     a display name, bookable now and claimable later by invitation.
            // Before this, the loop below ended in a commented-out `provider.AddStaff(...)`:
            // register-full accepted teamMembers, counted them, and silently discarded them.
            var newMemberships = new List<OrganizationMembership>();

            foreach (var memberDto in request.TeamMembers)
            {
                if (memberDto.IsOwner)
                    continue; // the owner's own membership is created below

                var displayName = (memberDto.Name ?? string.Empty).Trim();
                if (displayName.Length == 0)
                    continue;

                UserId? memberPersonId = null;
                if (!string.IsNullOrWhiteSpace(memberDto.PhoneNumber))
                {
                    var person = await _personDirectory.FindByPhoneAsync(memberDto.PhoneNumber, cancellationToken);
                    if (person is not null)
                        memberPersonId = UserId.From(person.PersonId);
                }

                // A team member listed twice (or one who is also the owner) must not
                // violate ux_membership_person_org_active.
                if (memberPersonId is not null &&
                    (memberPersonId.Equals(ownerId) ||
                     newMemberships.Any(m => memberPersonId.Equals(m.PersonId))))
                {
                    _logger.LogDebug("Skipping duplicate team member {Name} during registration", displayName);
                    continue;
                }

                OrganizationMembership membership;
                if (memberPersonId is not null)
                {
                    membership = OrganizationMembership.InviteExisting(memberPersonId, provider.Id);
                    membership.Accept();
                    membership.EnableStaffProfile();
                }
                else
                {
                    membership = OrganizationMembership.CreateUnclaimed(provider.Id, displayName);
                }

                newMemberships.Add(membership);

                _logger.LogDebug(
                    "Registered team member {Name} (position {Position}) as membership {MembershipId}, unclaimed={Unclaimed}",
                    displayName,
                    memberDto.Position,
                    membership.Id,
                    membership.IsUnclaimed);
            }

            var staffAdded = newMemberships.Count;

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
            // STEP 7.6: Owner membership — the durable anchor of the membership model
            // ====================================
            // Ownership is expressed as a membership carrying the Owner role, NOT only by
            // Provider.OwnerId. The step-9 wizard path already did this; register-full did
            // not, so one-shot registrations produced a salon whose owner was invisible to
            // every membership-based read (/memberships/me, the roster, role checks) until
            // they happened to call /registration/owner-provides-services.
            //
            // providesServices: false — owning a salon does not imply working in it. The
            // owner opts in separately via SetOwnerProvidesServices, which adds the
            // StaffProvider role + StaffProfile and makes them bookable.
            var ownerMembership = OrganizationMembership.CreateOwner(ownerId, provider.Id, providesServices: false);
            await _membershipRepository.SaveAsync(ownerMembership, cancellationToken);
            await _auditRepository.AppendAsync(
                MembershipAuditEntry.Record(
                    ownerMembership.Id,
                    ownerMembership.OrganizationId,
                    MembershipAuditAction.OwnerCreated,
                    ownerMembership.Status,
                    subjectPersonId: ownerMembership.PersonId,
                    actorPersonId: ownerId,
                    roles: ownerMembership.Roles),
                cancellationToken);

            foreach (var membership in newMemberships)
            {
                await _membershipRepository.SaveAsync(membership, cancellationToken);
                await _auditRepository.AppendAsync(
                    MembershipAuditEntry.Record(
                        membership.Id,
                        membership.OrganizationId,
                        MembershipAuditAction.MemberAdded,
                        membership.Status,
                        subjectPersonId: membership.PersonId,
                        actorPersonId: ownerId,
                        roles: membership.Roles,
                        reason: membership.IsUnclaimed ? "added without an app account" : null),
                    cancellationToken);

                // Qualify for the org's services and generate availability, so a member
                // listed at registration is bookable without a second round-trip.
                await _memberBookability.SyncAsync(membership, cancellationToken: cancellationToken);
            }

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

        // Delegates to ServiceCategoryResolver so every registration path agrees on what a
        // category string means. The local map used to miss the wizard's "barber" id and
        // silently filed men's barbershops under BeautySalon.
        private ServiceCategory MapCategoryToServiceCategory(string categoryId)
            => ServiceCategoryResolver.Resolve(categoryId, ServiceCategory.BeautySalon);

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
