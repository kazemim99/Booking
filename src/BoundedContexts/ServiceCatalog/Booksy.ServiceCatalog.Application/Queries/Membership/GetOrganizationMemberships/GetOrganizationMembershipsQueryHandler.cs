using Booksy.Core.Application.Abstractions.CQRS;
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

    public GetOrganizationMembershipsQueryHandler(
        IOrganizationMembershipRepository membershipRepository,
        IPersonDirectory personDirectory,
        ILogger<GetOrganizationMembershipsQueryHandler> logger)
    {
        _membershipRepository = membershipRepository;
        _personDirectory = personDirectory;
        _logger = logger;
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
            string name = string.Empty;
            string? phone = null;
            if (m.PersonId is not null && people.TryGetValue(m.PersonId.Value, out var person))
            {
                name = $"{person.FirstName} {person.LastName}".Trim();
                phone = person.PhoneNumber;
            }

            // Unclaimed member (added by the salon, no app account yet).
            if (string.IsNullOrEmpty(name))
                name = m.StaffProfile?.DisplayName ?? string.Empty;

            return new OrganizationMemberDto(
                MembershipId: m.Id,
                PersonId: m.PersonId?.Value,
                Name: name,
                PhoneNumber: phone,
                Roles: m.Roles.Select(r => r.ToString()).ToList(),
                Status: m.Status.ToString(),
                IsOwner: m.IsOwner,
                ProvidesServices: m.ProvidesServices,
                JoinedAt: m.JoinedAt);
        }).ToList();

        _logger.LogDebug("Organization {OrgId} has {Count} membership(s)", organizationId.Value, members.Count);

        return new GetOrganizationMembershipsResult(organizationId.Value, members);
    }
}
