using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingInvitations
{
    public sealed record GetPendingInvitationsQuery(
        Guid OrganizationId) : IQuery<GetPendingInvitationsResult>;

    public sealed record GetPendingInvitationsResult(
        Guid OrganizationId,
        IReadOnlyList<InvitationDto> Invitations);

    public sealed record InvitationDto(
        Guid InvitationId,
        string PhoneNumber,
        string? InviteeName,
        string? Message,
        string Status,
        DateTime CreatedAt,
        DateTime ExpiresAt);
}
