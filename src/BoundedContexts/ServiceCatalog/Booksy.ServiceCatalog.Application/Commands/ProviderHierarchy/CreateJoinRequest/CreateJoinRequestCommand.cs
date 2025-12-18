using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.CreateJoinRequest
{
    public sealed record CreateJoinRequestCommand(
        Guid OrganizationId,
        Guid RequesterId,
        string? Message = null,
        Guid? IdempotencyKey = null) : ICommand<CreateJoinRequestResult>;

    public sealed record CreateJoinRequestResult(
        Guid RequestId,
        Guid OrganizationId,
        Guid RequesterId,
        string Status,
        DateTime CreatedAt);
}
