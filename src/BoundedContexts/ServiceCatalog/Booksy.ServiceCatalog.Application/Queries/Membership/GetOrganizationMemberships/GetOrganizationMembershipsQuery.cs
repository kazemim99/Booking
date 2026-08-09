using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.GetOrganizationMemberships;

/// <summary>
/// The organization's staff directory, sourced from memberships (the real model) and
/// enriched with each person's name/phone via the person-directory seam.
/// </summary>
public sealed record GetOrganizationMembershipsQuery(Guid OrganizationId)
    : IQuery<GetOrganizationMembershipsResult>;

public sealed record GetOrganizationMembershipsResult(
    Guid OrganizationId,
    IReadOnlyList<OrganizationMemberDto> Members);

public sealed record OrganizationMemberDto(
    Guid MembershipId,
    Guid? PersonId,
    string Name,
    string? PhoneNumber,
    IReadOnlyCollection<string> Roles,
    string Status,
    bool IsOwner,
    bool ProvidesServices,
    DateTime? JoinedAt);
