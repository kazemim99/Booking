using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CancelJoinRequest
{
    public sealed record CancelJoinRequestCommand(
        Guid RequestId,
        Guid RequesterId,
        Guid? IdempotencyKey = null) : ICommand<CancelJoinRequestResult>;

    public sealed record CancelJoinRequestResult(
        Guid RequestId,
        string Status);
}
