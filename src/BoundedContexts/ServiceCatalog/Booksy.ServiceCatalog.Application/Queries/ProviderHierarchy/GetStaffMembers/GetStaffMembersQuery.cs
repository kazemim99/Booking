using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetStaffMembers
{
    public sealed record GetStaffMembersQuery(
        Guid OrganizationId) : IQuery<GetStaffMembersResult>;

    public sealed record GetStaffMembersResult(
        Guid OrganizationId,
        IReadOnlyList<StaffMemberDto> StaffMembers);

    public sealed record StaffMemberDto(
        Guid Id,
        Guid ProviderId,
        Guid OrganizationId,
        string FirstName,
        string LastName,
        string FullName,
        string? Email,
        string? PhoneNumber,
        string? PhotoUrl,
        string Role,
        string? Title,
        string? Bio,
        IReadOnlyList<string>? Specializations,
        bool IsActive,
        DateTime JoinedAt,
        DateTime? LeftAt);
}
