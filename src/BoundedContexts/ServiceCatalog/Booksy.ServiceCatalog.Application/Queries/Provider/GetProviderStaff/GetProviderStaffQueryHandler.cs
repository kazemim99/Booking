// ========================================
// Application/Queries/Provider/GetProviderStaff/GetProviderStaffQueryHandler.cs
// Returns an organization's staff. MEMBERSHIPS are the source of truth for who works
// here; this legacy endpoint keeps its response shape for existing clients (Vue admin)
// but reads the membership roster, plus any not-yet-migrated legacy sub-providers.
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStaff
{
    public sealed class GetProviderStaffQueryHandler
        : IQueryHandler<GetProviderStaffQuery, GetProviderStaffResult>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IOrganizationMembershipRepository _membershipRepository;
        private readonly IPersonDirectory _personDirectory;

        public GetProviderStaffQueryHandler(
            IProviderReadRepository providerRepository,
            IOrganizationMembershipRepository membershipRepository,
            IPersonDirectory personDirectory)
        {
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
            _membershipRepository = membershipRepository ?? throw new ArgumentNullException(nameof(membershipRepository));
            _personDirectory = personDirectory ?? throw new ArgumentNullException(nameof(personDirectory));
        }

        public async Task<GetProviderStaffResult> Handle(
            GetProviderStaffQuery request,
            CancellationToken cancellationToken)
        {
            var organizationId = ProviderId.From(request.ProviderId);

            var organization = await _providerRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization is null)
                throw new KeyNotFoundException($"Provider {request.ProviderId} not found");

            var includeInactive = request.IncludeInactive ?? false;
            var staffDtos = new List<StaffDto>();

            var memberships = await _membershipRepository.GetByOrganizationAsync(organizationId, cancellationToken);
            var visible = memberships
                .Where(m => m.Status != MembershipStatus.Terminated)
                .Where(m => includeInactive || m.Status == MembershipStatus.Active)
                .ToList();

            var personIds = visible
                .Where(m => m.PersonId is not null)
                .Select(m => m.PersonId!.Value)
                .Distinct()
                .ToList();

            var people = personIds.Count > 0
                ? await _personDirectory.FindByIdsAsync(personIds, cancellationToken)
                : new Dictionary<Guid, PersonInfo>();

            foreach (var member in visible)
            {
                string first = string.Empty, last = string.Empty, phone = string.Empty;
                if (member.PersonId is not null && people.TryGetValue(member.PersonId.Value, out var person))
                {
                    first = person.FirstName ?? string.Empty;
                    last = person.LastName ?? string.Empty;
                    phone = person.PhoneNumber ?? string.Empty;
                }

                // Unclaimed member: the salon-provided display name is the identity.
                var fullName = $"{first} {last}".Trim();
                if (string.IsNullOrEmpty(fullName))
                    fullName = member.StaffProfile?.DisplayName ?? string.Empty;

                staffDtos.Add(new StaffDto(
                    member.Id,                       // the bookable resource id
                    first,
                    last,
                    fullName,
                    string.IsNullOrEmpty(phone) ? null : phone,
                    member.IsOwner ? "Owner" : "Staff",
                    member.Status == MembershipStatus.Active,
                    member.JoinedAt ?? member.CreatedAt,
                    member.LeftAt,
                    null)
                {
                    Biography = member.StaffProfile?.BioOverride ?? string.Empty,
                    ProfilePhotoUrl = string.Empty
                });
            }

            // Legacy individual sub-providers that have not been migrated yet.
            var legacyStaff = await _providerRepository.GetStaffByOrganizationIdAsync(organizationId, cancellationToken);
            foreach (var sp in legacyStaff.Where(x => includeInactive || x.Status == ProviderStatus.Active))
            {
                if (staffDtos.Any(d => d.Id == sp.Id.Value))
                    continue;

                staffDtos.Add(new StaffDto(
                    sp.Id.Value,
                    sp.OwnerFirstName,
                    sp.OwnerLastName,
                    $"{sp.OwnerFirstName} {sp.OwnerLastName}".Trim(),
                    sp.ContactInfo?.PrimaryPhone?.Value,
                    "Staff",
                    sp.Status == ProviderStatus.Active,
                    sp.RegisteredAt,
                    null,
                    null)
                {
                    Biography = string.Empty,
                    ProfilePhotoUrl = sp.Profile?.ProfileImageUrl ?? string.Empty
                });
            }

            return new GetProviderStaffResult(
                organization.Id.Value,
                organization.Profile.BusinessName,
                staffDtos);
        }
    }
}
