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
                PersonInfo? person = null;
                if (member.PersonId is not null)
                    people.TryGetValue(member.PersonId.Value, out person);

                // Only the real parts of the person's name leave this handler. The 2026-09-22 fix (3.4) left the
                // raw «ارائه‌دهنده» / «9123135143» here and an EMPTY FullName for a member with no display name —
                // the salon's owner — so GET /Providers/{id} rebuilt the placeholder from the parts and the
                // customer app's confirm step printed it (production QA 2026-09-23).
                var (first, last) = PersonName.RealParts(person?.FirstName, person?.LastName);
                var phone = person?.PhoneNumber ?? string.Empty;

                // Real name, else the salon's name for them, else the salon's own name — never empty, so no
                // client ever has a reason to assemble one itself.
                var fullName = PersonName.ForMember(
                    person, member.StaffProfile?.DisplayName, organization.Profile.BusinessName);

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
                    ProfilePhotoUrl = string.Empty,
                    // Roles + profile flag combined by the aggregate; see StaffDto.
                    ProvidesServices = member.ProvidesServices
                });
            }

            // (Legacy Individual sub-providers were listed here as well, until staff stopped
            // being Providers. The membership roster above is now the whole staff list.)


            return new GetProviderStaffResult(
                organization.Id.Value,
                organization.Profile.BusinessName,
                staffDtos);
        }
    }
}
