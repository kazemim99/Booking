using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ApproveJoinRequest
{
    public sealed record ApproveJoinRequestCommand(
        Guid RequestId,
        Guid ReviewerId,
        string? Note = null,
        Guid? IdempotencyKey = null) : ICommand<ApproveJoinRequestResult>;

    public sealed record ApproveJoinRequestResult(
        Guid RequestId,
        Guid OrganizationId,
        Guid RequesterId,
        DateTime ApprovedAt);
}
