using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetPendingJoinRequests
{
    public sealed record GetPendingJoinRequestsQuery(
        Guid OrganizationId) : IQuery<GetPendingJoinRequestsResult>;

    public sealed record GetPendingJoinRequestsResult(
        Guid OrganizationId,
        IReadOnlyList<JoinRequestDto> JoinRequests);

    public sealed record JoinRequestDto(
        Guid RequestId,
        Guid RequesterId,
        string RequesterName,
        string? RequesterProfileImageUrl,
        string? Message,
        string Status,
        DateTime CreatedAt);
}
