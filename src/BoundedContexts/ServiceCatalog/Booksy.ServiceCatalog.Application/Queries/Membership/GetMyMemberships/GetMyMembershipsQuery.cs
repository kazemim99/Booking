using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Membership.GetMyMemberships;

/// <summary>
/// The authenticated person's organization memberships (excluding terminated ones).
/// Feeds the provider app's salon switcher / multi-membership session.
/// </summary>
public sealed record GetMyMembershipsQuery() : IQuery<GetMyMembershipsResult>;

public sealed record GetMyMembershipsResult(IReadOnlyList<MyMembershipDto> Memberships);

public sealed record MyMembershipDto(
    Guid MembershipId,
    Guid OrganizationId,
    string OrganizationName,
    string? OrganizationLogo,
    IReadOnlyCollection<string> Roles,
    string Status,
    bool ProvidesServices,
    DateTime? JoinedAt);
