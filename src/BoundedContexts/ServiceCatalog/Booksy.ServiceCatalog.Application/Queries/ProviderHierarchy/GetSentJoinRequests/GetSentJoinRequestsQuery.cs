using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetSentJoinRequests
{
    public sealed record GetSentJoinRequestsQuery(
        Guid IndividualProviderId) : IQuery<GetSentJoinRequestsResult>;

    public sealed record GetSentJoinRequestsResult(
        Guid IndividualProviderId,
        IReadOnlyList<SentJoinRequestDto> JoinRequests);

    public sealed record SentJoinRequestDto(
        Guid RequestId,
        Guid OrganizationId,
        string OrganizationName,
        string? OrganizationLogoUrl,
        string? Message,
        string Status,
        DateTime CreatedAt,
        DateTime? RespondedAt,
        string? RejectionReason);
}
