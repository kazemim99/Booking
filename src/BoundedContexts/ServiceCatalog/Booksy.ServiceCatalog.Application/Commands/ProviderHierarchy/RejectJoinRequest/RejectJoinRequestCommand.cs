using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RejectJoinRequest
{
    public sealed record RejectJoinRequestCommand(
        Guid RequestId,
        Guid ReviewerId,
        string? Reason = null,
        Guid? IdempotencyKey = null) : ICommand<RejectJoinRequestResult>;

    public sealed record RejectJoinRequestResult(
        Guid RequestId,
        Guid OrganizationId,
        Guid RequesterId,
        DateTime RejectedAt);
}
