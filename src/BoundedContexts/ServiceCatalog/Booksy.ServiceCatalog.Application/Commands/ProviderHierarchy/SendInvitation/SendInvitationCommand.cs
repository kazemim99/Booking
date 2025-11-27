using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.SendInvitation
{
    public sealed record SendInvitationCommand(
        Guid OrganizationId,
        string PhoneNumber,
        string? InviteeName = null,
        string? Message = null,
        Guid? IdempotencyKey = null) : ICommand<SendInvitationResult>;

    public sealed record SendInvitationResult(
        Guid InvitationId,
        Guid OrganizationId,
        string OrganizationName,
        string? OrganizationLogo,
        string PhoneNumber,
        string? InviteeName,
        string? Message,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        string Status);
}
