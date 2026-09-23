using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Abstractions;
using Booksy.ServiceCatalog.Application.Abstractions.Identity;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.GetOrganizationMemberships;

public sealed class GetOrganizationMembershipsQueryHandler
    : IQueryHandler<GetOrganizationMembershipsQuery, GetOrganizationMembershipsResult>
{
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IPersonDirectory _personDirectory;
    private readonly ILogger<GetOrganizationMembershipsQueryHandler> _logger;
    private readonly IUrlService _urlService;

    public GetOrganizationMembershipsQueryHandler(
        IOrganizationMembershipRepository membershipRepository,
        IPersonDirectory personDirectory,
        ILogger<GetOrganizationMembershipsQueryHandler> logger,
        IUrlService urlService)
    {
        _membershipRepository = membershipRepository;
        _personDirectory = personDirectory;
        _logger = logger;
        _urlService = urlService;
    }

    public async Task<GetOrganizationMembershipsResult> Handle(
        GetOrganizationMembershipsQuery request,
        CancellationToken cancellationToken)
    {
        var organizationId = ProviderId.From(request.OrganizationId);

        // The staff directory shows current members only; terminated (ex-)staff are
        // history, consistent with GetMyMemberships.
        var memberships = (await _membershipRepository.GetByOrganizationAsync(organizationId, cancellationToken))
            .Where(m => m.Status != MembershipStatus.Terminated)
            .ToList();

        var personIds = memberships
            .Where(m => m.PersonId is not null)
            .Select(m => m.PersonId!.Value)
            .Distinct()
            .ToList();

        var people = personIds.Count > 0
            ? await _personDirectory.FindByIdsAsync(personIds, cancellationToken)
            : new Dictionary<Guid, PersonInfo>();

        var members = memberships.Select(m =>
        {
            string? name = null;
            string? phone = null;
            if (m.PersonId is not null && people.TryGetValue(m.PersonId.Value, out var person))
            {
                // Not the raw parts: an account made by OTP is «ارائه‌دهنده <digits>», and the salon app printed
                // it as the owner's name (QA 2026-09-23). The number stays available — as PhoneNumber.
                name = PersonName.RealOrNull(person.FirstName, person.LastName);
                phone = person.PhoneNumber;
            }

            // Unclaimed member (added by the salon, no app account yet), or a person with no real name: the
            // salon's name for them, if it is one. Otherwise empty, and each app shows its own "no name" label —
            // on the salon's own roster the salon's name would read as if the member were the salon.
            name ??= PersonName.Sanitize(m.StaffProfile?.DisplayName) ?? string.Empty;

            return new OrganizationMemberDto(
                MembershipId: m.Id,
                PersonId: m.PersonId?.Value,
                Name: name,
                PhoneNumber: phone,
                Roles: m.Roles.Select(r => r.ToString()).ToList(),
                Status: m.Status.ToString(),
                IsOwner: m.IsOwner,
                ProvidesServices: m.ProvidesServices,
                JoinedAt: m.JoinedAt,
                // Per-salon presentation, so the staff screen can render a member without
                // a second round-trip, and can tell "no app account yet" from "inactive".
                BioOverride: m.StaffProfile?.BioOverride,
                PhotoUrl: _urlService.AbsoluteOrNull(m.StaffProfile?.PhotoUrl),
                IsUnclaimed: m.IsUnclaimed);
        }).ToList();

        _logger.LogDebug("Organization {OrgId} has {Count} membership(s)", organizationId.Value, members.Count);

        return new GetOrganizationMembershipsResult(organizationId.Value, members);
    }
}
