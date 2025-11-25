using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetStaffMembers
{
    public sealed record GetStaffMembersQuery(
        Guid OrganizationId) : IQuery<GetStaffMembersResult>;

    public sealed record GetStaffMembersResult(
        Guid OrganizationId,
        IReadOnlyList<StaffMemberDto> StaffMembers);

    public sealed record StaffMemberDto(
        Guid ProviderId,
        string BusinessName,
        string? ProfileImageUrl,
        string Status,
        DateTime JoinedAt);
}
