using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelInvitation
{
    public sealed record CancelInvitationCommand(
        Guid InvitationId,
        Guid OrganizationId,
        Guid? IdempotencyKey = null) : ICommand<CancelInvitationResult>;

    public sealed record CancelInvitationResult(
        Guid InvitationId,
        bool Success,
        string Message);
}
