using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetInvitation
{
    public sealed record GetInvitationQuery(
        Guid OrganizationId,
        Guid InvitationId) : IQuery<GetInvitationResult>;

    public sealed record GetInvitationResult(
        Guid InvitationId,
        Guid OrganizationId,
        string OrganizationName,
        string? OrganizationLogo,
        string PhoneNumber,
        string? InviteeName,
        string? Message,
        string Status,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        DateTime? RespondedAt);
}
