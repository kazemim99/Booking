// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Service/GetQualifiedStaff/GetQualifiedStaffQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.ServiceCatalog.Application.Abstractions;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Identity;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Service.GetQualifiedStaff
{
    /// <summary>
    /// Who can perform this service at this salon.
    /// </summary>
    /// <remarks>
    /// Reads MEMBERSHIPS. It used to read the salon's Individual sub-providers, which meant
    /// anyone added through the invitation flow was invisible here — and, since this feeds
    /// the customer-facing staff picker, unbookable from the web.
    /// </remarks>
    public sealed class GetQualifiedStaffQueryHandler : IQueryHandler<GetQualifiedStaffQuery, GetQualifiedStaffResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IServiceReadRepository _serviceRepository;
        private readonly IOrganizationMembershipRepository _membershipRepository;
        private readonly IPersonDirectory _personDirectory;
        private readonly ILogger<GetQualifiedStaffQueryHandler> _logger;
        private readonly IUrlService _urlService;

        public GetQualifiedStaffQueryHandler(
            IProviderReadRepository providerRepository,
            IServiceReadRepository serviceRepository,
            IOrganizationMembershipRepository membershipRepository,
            IPersonDirectory personDirectory,
            ILogger<GetQualifiedStaffQueryHandler> logger,
            IUrlService urlService)
        {
            _providerRepository = providerRepository;
            _serviceRepository = serviceRepository;
            _membershipRepository = membershipRepository;
            _personDirectory = personDirectory;
            _logger = logger;
            _urlService = urlService;
        }

        public async Task<GetQualifiedStaffResult> Handle(GetQualifiedStaffQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Getting qualified staff for Provider {ProviderId}, Service {ServiceId}",
                request.ProviderId, request.ServiceId);

            var organizationId = ProviderId.From(request.ProviderId);

            var provider = await _providerRepository.GetByIdAsync(organizationId, cancellationToken)
                ?? throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

            var service = await _serviceRepository.GetByIdAsync(
                ServiceId.From(request.ServiceId), cancellationToken)
                ?? throw new NotFoundException($"Service with ID {request.ServiceId} not found");

            if (service.ProviderId.Value != request.ProviderId)
                throw new NotFoundException($"Service {request.ServiceId} does not belong to provider {request.ProviderId}");

            var memberships = await _membershipRepository.GetByOrganizationAsync(organizationId, cancellationToken);

            // Active members who provide services AND are qualified for this one. A member
            // with no qualification list entry is not offered: qualification is explicit,
            // maintained by MemberBookabilityService when a member becomes bookable.
            var bookable = memberships
                .Where(m => m.Status == MembershipStatus.Active
                            && m.ProvidesServices
                            && service.IsStaffQualified(m.Id))
                .ToList();

            var personIds = bookable
                .Where(m => m.PersonId is not null)
                .Select(m => m.PersonId!.Value)
                .Distinct()
                .ToList();

            var people = personIds.Count > 0
                ? await _personDirectory.FindByIdsAsync(personIds, cancellationToken)
                : new Dictionary<Guid, PersonInfo>();

            var qualifiedStaff = bookable
                .Select(m =>
                {
                    // Real name when the membership is claimed; the salon's display name for a member who has no
                    // account; the salon's own name otherwise. Never the OTP placeholder or a phone number: this
                    // joined the raw parts and named a nameless owner «ارائه‌دهنده 9123135143» (QA 2026-09-23).
                    PersonInfo? person = null;
                    if (m.PersonId is not null)
                        people.TryGetValue(m.PersonId.Value, out person);

                    return new StaffMemberDto(
                        m.Id,
                        PersonName.ForMember(person, m.StaffProfile?.DisplayName, provider.Profile.BusinessName),
                        _urlService.AbsoluteOrNull(m.StaffProfile?.PhotoUrl),
                        null, // Rating - not modelled per member yet
                        null, // ReviewCount - not modelled per member yet
                        null  // Specialization - not modelled per member yet
                    );
                })
                .ToList();

            _logger.LogInformation(
                "Found {TotalMembers} member(s), {QualifiedCount} qualified for service {ServiceId}",
                memberships.Count, qualifiedStaff.Count, service.Id);

            return new GetQualifiedStaffResult(
                request.ProviderId,
                request.ServiceId,
                qualifiedStaff);
        }
    }
}
