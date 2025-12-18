using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.AcceptInvitation
{
    public sealed record AcceptInvitationCommand(
        Guid InvitationId,
        Guid IndividualProviderId,
        Guid? IdempotencyKey = null) : ICommand<AcceptInvitationResult>;

    public sealed record AcceptInvitationResult(
        Guid InvitationId,
        Guid OrganizationId,
        Guid IndividualProviderId,
        DateTime AcceptedAt);
}
